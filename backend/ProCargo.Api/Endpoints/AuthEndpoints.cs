using System.Security.Claims;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using ProCargo.Api.Common;
using ProCargo.Api.Contracts;
using ProCargo.Api.Data;
using ProCargo.Api.Data.Entities;
using ProCargo.Api.Domain;
using ProCargo.Api.Services;

namespace ProCargo.Api.Endpoints;

/// <summary>
/// Sign-in and registration. Everyone signs in with their mobile number and a one-time code.
///   POST /api/auth/otp/send           send a code (Login or Signup)
///   POST /api/auth/login              sign in with the code
///   POST /api/auth/register/customer  create a customer account
///   POST /api/auth/register/owner     create a lorry owner account (KYC + bank)
///   POST /api/auth/register/driver    create a driver account
///   GET  /api/auth/me                 the signed-in user's profile
/// </summary>
public static class AuthEndpoints
{
    private static readonly Regex PanFormat = new("^[A-Z]{5}[0-9]{4}[A-Z]$");
    private static readonly Regex IfscFormat = new("^[A-Z]{4}0[A-Z0-9]{6}$");
    private static readonly Regex FourDigits = new("^[0-9]{4}$");
    private static readonly string[] LicenceClasses = { "LMV", "TRANSPORT", "HGMV", "HPMV" };

    public static void Map(RouteGroupBuilder api)
    {
        RouteGroupBuilder group = api.MapGroup("/auth");

        group.MapPost("/otp/send", SendCodeAsync);
        group.MapPost("/login", LoginAsync);
        group.MapPost("/register/customer", RegisterCustomerAsync);
        group.MapPost("/register/owner", RegisterOwnerAsync);
        group.MapPost("/register/driver", RegisterDriverAsync);
        group.MapGet("/me", GetMyProfileAsync).RequireAuthorization();
    }

    // ---------------------------------------------------------------- sign-in

    private static async Task<IResult> SendCodeAsync(
        SendOtpRequest request,
        ProCargoDbContext db,
        OtpService otp,
        IWebHostEnvironment environment,
        IConfiguration configuration,
        ILogger<OtpService> logger)
    {
        string mobile = MobileNumber.Normalize(request.Mobile);
        string purpose = request.Purpose == OtpPurpose.Signup ? OtpPurpose.Signup : OtpPurpose.Login;
        bool accountExists = await db.Users.AnyAsync(user => user.Mobile == mobile);

        if (purpose == OtpPurpose.Login && !accountExists)
        {
            throw ApiException.NotFound("No account uses this number yet. Register first.");
        }

        if (purpose == OtpPurpose.Signup && accountExists)
        {
            throw ApiException.Conflict("This number is already registered. Sign in instead.");
        }

        string code = await otp.IssueAsync(mobile, purpose);

        // TODO before launch: send the code by SMS through your DLT-registered provider.
        bool isDevelopment = environment.IsDevelopment();
        logger.LogInformation("Sign-in code for {Mobile}: {Code}", mobile, isDevelopment ? code : "******");

        // In development the code is shown on screen so you can test without SMS.
        bool showCode = isDevelopment && configuration.GetValue("Otp:ShowCodeInDevelopment", true);
        return Results.Ok(new { sent = true, devCode = showCode ? code : null });
    }

    private static async Task<IResult> LoginAsync(
        LoginRequest request,
        ProCargoDbContext db,
        OtpService otp,
        TokenService tokens)
    {
        string mobile = MobileNumber.Normalize(request.Mobile);
        await otp.VerifyAsync(mobile, request.Code, OtpPurpose.Login);

        User user = await db.Users.FirstOrDefaultAsync(row => row.Mobile == mobile)
            ?? throw ApiException.NotFound("Account not found.");

        if (user.Status is UserStatus.Blocked or UserStatus.Closed)
        {
            throw ApiException.Forbidden("This account is not active. Contact ProCargo support.");
        }

        user.LastLoginAt = DateTime.UtcNow;
        await db.SaveChangesAsync();

        return Results.Ok(new { token = tokens.CreateToken(user), user = await BuildProfileAsync(db, user) });
    }

    private static async Task<IResult> GetMyProfileAsync(ClaimsPrincipal principal, ProCargoDbContext db)
    {
        User user = await db.Users.FindAsync(principal.GetUserId())
            ?? throw ApiException.NotFound("Account not found.");

        return Results.Ok(await BuildProfileAsync(db, user));
    }

    // ---------------------------------------------------------------- registration

    private static async Task<IResult> RegisterCustomerAsync(
        RegisterCustomerRequest request,
        ProCargoDbContext db,
        OtpService otp,
        TokenService tokens)
    {
        Guard.Require(string.IsNullOrWhiteSpace(request.Gstin) || request.Gstin.Trim().Length == 15, "GSTIN has 15 characters.");
        string mobile = await VerifySignupAsync(db, otp, request.Mobile, request.Code, request.FullName);

        var user = new User
        {
            Role = Roles.Customer,
            FullName = request.FullName.Trim(),
            Mobile = mobile,
            Email = Clean(request.Email),
        };
        db.Users.Add(user);
        db.Customers.Add(new Customer
        {
            User = user,
            CompanyName = Clean(request.CompanyName),
            GSTIN = Clean(request.Gstin)?.ToUpperInvariant(),
        });
        await db.SaveChangesAsync();

        return Results.Ok(new { token = tokens.CreateToken(user), user = await BuildProfileAsync(db, user) });
    }

    private static async Task<IResult> RegisterOwnerAsync(
        RegisterOwnerRequest request,
        ProCargoDbContext db,
        OtpService otp,
        TokenService tokens,
        PersonalDataProtector protector)
    {
        string pan = (request.Pan ?? string.Empty).Trim().ToUpperInvariant();
        string ifsc = (request.Ifsc ?? string.Empty).Trim().ToUpperInvariant();
        string accountNumber = new((request.AccountNumber ?? string.Empty).Where(char.IsDigit).ToArray());

        Guard.Require(PanFormat.IsMatch(pan), "Enter a valid PAN, for example ABCDE1234F.");
        Guard.Require(FourDigits.IsMatch(request.AadhaarLast4 ?? string.Empty), "Enter only the last 4 digits of Aadhaar.");
        Guard.Require(accountNumber.Length is >= 9 and <= 18, "Enter a valid bank account number.");
        Guard.Require(IfscFormat.IsMatch(ifsc), "Enter a valid IFSC, for example HDFC0001234.");
        Guard.Require(!string.IsNullOrWhiteSpace(request.AccountHolder), "Enter the account holder's name.");

        string mobile = await VerifySignupAsync(db, otp, request.Mobile, request.Code, request.FullName);

        var user = new User
        {
            Role = Roles.Owner,
            FullName = request.FullName.Trim(),
            Mobile = mobile,
            Email = Clean(request.Email),
            Status = UserStatus.PendingKyc,
        };
        var owner = new Owner
        {
            User = user,
            BusinessName = Clean(request.BusinessName),
            PanEncrypted = protector.ProtectToBytes(pan),
            PanLast4 = pan[^4..],
            AadhaarLast4 = request.AadhaarLast4,
        };
        db.Users.Add(user);
        db.Owners.Add(owner);
        await db.SaveChangesAsync();

        db.OwnerBankAccounts.Add(new OwnerBankAccount
        {
            OwnerId = owner.OwnerId,
            AccountHolder = request.AccountHolder.Trim(),
            AccountNumberEncrypted = protector.ProtectToBytes(accountNumber),
            AccountLast4 = accountNumber[^4..],
            IFSC = ifsc,
            BankName = Clean(request.BankName),
        });
        await db.SaveChangesAsync();

        return Results.Ok(new { token = tokens.CreateToken(user), user = await BuildProfileAsync(db, user) });
    }

    private static async Task<IResult> RegisterDriverAsync(
        RegisterDriverRequest request,
        ProCargoDbContext db,
        OtpService otp,
        TokenService tokens)
    {
        string licenceNumber = (request.LicenceNumber ?? string.Empty)
            .Replace(" ", string.Empty)
            .Replace("-", string.Empty)
            .ToUpperInvariant();

        Guard.Require(licenceNumber.Length is >= 10 and <= 20, "Enter the driving licence number as printed.");
        Guard.Require(LicenceClasses.Contains(request.LicenceClass), "Choose the licence class.");
        Guard.Require(request.LicenceExpiry > DateOnly.FromDateTime(DateTime.UtcNow), "This licence has expired.");
        Guard.Require(
            string.IsNullOrEmpty(request.AadhaarLast4) || FourDigits.IsMatch(request.AadhaarLast4),
            "Enter only the last 4 digits of Aadhaar.");

        bool licenceTaken = await db.Drivers.AnyAsync(driver => driver.LicenceNumber == licenceNumber);
        Guard.Require(!licenceTaken, "This licence is already registered.");

        long? ownerId = await FindOwnerIdByMobileAsync(db, request.OwnerMobile);
        string mobile = await VerifySignupAsync(db, otp, request.Mobile, request.Code, request.FullName);

        var user = new User
        {
            Role = Roles.Driver,
            FullName = request.FullName.Trim(),
            Mobile = mobile,
            Status = UserStatus.PendingKyc,
        };
        db.Users.Add(user);
        db.Drivers.Add(new Driver
        {
            User = user,
            OwnerId = ownerId,
            LicenceNumber = licenceNumber,
            LicenceClass = request.LicenceClass,
            LicenceExpiry = request.LicenceExpiry,
            AadhaarLast4 = Clean(request.AadhaarLast4),
            EmergencyContactName = Clean(request.EmergencyContactName),
            EmergencyContactPhone = Clean(request.EmergencyContactPhone),
        });
        await db.SaveChangesAsync();

        return Results.Ok(new { token = tokens.CreateToken(user), user = await BuildProfileAsync(db, user) });
    }

    // ---------------------------------------------------------------- helpers

    /// <summary>Checks the name, that the mobile is new, and the sign-up code. Returns the clean mobile.</summary>
    private static async Task<string> VerifySignupAsync(
        ProCargoDbContext db,
        OtpService otp,
        string rawMobile,
        string code,
        string fullName)
    {
        Guard.Require(!string.IsNullOrWhiteSpace(fullName) && fullName.Trim().Length >= 2, "Enter your full name.");

        string mobile = MobileNumber.Normalize(rawMobile);
        if (await db.Users.AnyAsync(user => user.Mobile == mobile))
        {
            throw ApiException.Conflict("This number is already registered. Sign in instead.");
        }

        await otp.VerifyAsync(mobile, code ?? string.Empty, OtpPurpose.Signup);
        return mobile;
    }

    /// <summary>A driver links to their owner by the owner's mobile number. Empty = drives their own truck.</summary>
    private static async Task<long?> FindOwnerIdByMobileAsync(ProCargoDbContext db, string? ownerMobile)
    {
        if (string.IsNullOrWhiteSpace(ownerMobile))
        {
            return null;
        }

        string mobile = MobileNumber.Normalize(ownerMobile);
        long? ownerId = await db.Owners
            .Where(owner => owner.User.Mobile == mobile)
            .Select(owner => (long?)owner.OwnerId)
            .FirstOrDefaultAsync();

        return ownerId ?? throw ApiException.BadRequest("No lorry owner is registered with that mobile number.");
    }

    private static string? Clean(string? text) => string.IsNullOrWhiteSpace(text) ? null : text.Trim();

    /// <summary>What the portal needs to know about the signed-in user.</summary>
    private static async Task<object> BuildProfileAsync(ProCargoDbContext db, User user)
    {
        object? detail = user.Role switch
        {
            Roles.Customer => await db.Customers
                .Where(customer => customer.UserId == user.UserId)
                .Select(customer => new { customer.CustomerId, customer.CompanyName, customer.GSTIN })
                .FirstOrDefaultAsync(),

            Roles.Owner => await db.Owners
                .Where(owner => owner.UserId == user.UserId)
                .Select(owner => new
                {
                    owner.OwnerId,
                    owner.BusinessName,
                    owner.KycStatus,
                    owner.RejectionReason,
                    owner.PanLast4,
                    owner.AadhaarLast4,
                })
                .FirstOrDefaultAsync(),

            Roles.Driver => await db.Drivers
                .Where(driver => driver.UserId == user.UserId)
                .Select(driver => new
                {
                    driver.DriverId,
                    driver.KycStatus,
                    driver.RejectionReason,
                    driver.LicenceNumber,
                    driver.LicenceClass,
                    driver.LicenceExpiry,
                    ownerName = driver.Owner != null ? driver.Owner.User.FullName : null,
                })
                .FirstOrDefaultAsync(),

            _ => null,
        };

        return new
        {
            id = user.UserId,
            user.Role,
            user.FullName,
            user.Mobile,
            user.Email,
            user.Status,
            detail,
        };
    }
}
