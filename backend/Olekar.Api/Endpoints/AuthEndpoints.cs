using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using Olekar.Api.Data;
using Olekar.Api.Services;

namespace Olekar.Api.Endpoints;

public record SendOtpRequest(string Mobile, string Purpose);
public record LoginRequest(string Mobile, string Code);
public record RegisterCustomerRequest(string FullName, string Mobile, string Code, string? Email, string? CompanyName, string? Gstin);
public record RegisterOwnerRequest(string FullName, string Mobile, string Code, string? Email, string? BusinessName,
    string Pan, string AadhaarLast4, string AccountHolder, string AccountNumber, string Ifsc, string? BankName);
public record RegisterDriverRequest(string FullName, string Mobile, string Code, string LicenceNumber, string LicenceClass,
    DateOnly LicenceExpiry, string? AadhaarLast4, string? EmergencyContactName, string? EmergencyContactPhone, string? OwnerMobile);

public static class AuthEndpoints
{
    public static void MapAuthEndpoints(this RouteGroupBuilder api)
    {
        var g = api.MapGroup("/auth");

        g.MapPost("/otp/send", async (SendOtpRequest r, OlekarDbContext db, OtpService otp, IWebHostEnvironment env, IConfiguration cfg, ILoggerFactory lf) =>
        {
            var mobile = Mobile.Normalize(r.Mobile);
            var purpose = r.Purpose == "Signup" ? "Signup" : "Login";
            var exists = await db.Users.AnyAsync(u => u.Mobile == mobile);
            if (purpose == "Login" && !exists) throw ApiException.NotFound("No account uses this number yet. Register first.");
            if (purpose == "Signup" && exists) throw ApiException.Conflict("This number is already registered. Sign in instead.");
            var code = await otp.IssueAsync(mobile, purpose);
            // TODO production: send through your DLT-registered SMS provider here.
            lf.CreateLogger("Otp").LogInformation("OTP for {Mobile}: {Code}", mobile, env.IsDevelopment() ? code : "******");
            var showCode = env.IsDevelopment() && cfg.GetValue("Otp:ShowCodeInDevelopment", true);
            return new { sent = true, devCode = showCode ? code : null };
        });

        g.MapPost("/login", async (LoginRequest r, OlekarDbContext db, OtpService otp, TokenService tokens) =>
        {
            var mobile = Mobile.Normalize(r.Mobile);
            await otp.VerifyAsync(mobile, r.Code, "Login");
            var user = await db.Users.FirstOrDefaultAsync(u => u.Mobile == mobile) ?? throw ApiException.NotFound("Account not found.");
            if (user.Status is "Blocked" or "Closed") throw ApiException.Forbidden("This account is not active. Contact Olekar support.");
            user.LastLoginAt = DateTime.UtcNow;
            await db.SaveChangesAsync();
            return new { token = tokens.Create(user), user = await Profile(db, user) };
        });

        g.MapPost("/register/customer", async (RegisterCustomerRequest r, OlekarDbContext db, OtpService otp, TokenService tokens) =>
        {
            var mobile = await StartSignup(db, otp, r.Mobile, r.Code, r.FullName);
            Guard.Require(string.IsNullOrWhiteSpace(r.Gstin) || r.Gstin.Trim().Length == 15, "GSTIN has 15 characters.");
            var user = new User { Role = Roles.Customer, FullName = r.FullName.Trim(), Mobile = mobile, Email = Clean(r.Email) };
            db.Users.Add(user);
            db.Customers.Add(new Customer { User = user, CompanyName = Clean(r.CompanyName), GSTIN = Clean(r.Gstin)?.ToUpperInvariant() });
            await db.SaveChangesAsync();
            return new { token = tokens.Create(user), user = await Profile(db, user) };
        });

        g.MapPost("/register/owner", async (RegisterOwnerRequest r, OlekarDbContext db, OtpService otp, TokenService tokens, PiiProtector pii) =>
        {
            var pan = (r.Pan ?? "").Trim().ToUpperInvariant();
            Guard.Require(System.Text.RegularExpressions.Regex.IsMatch(pan, "^[A-Z]{5}[0-9]{4}[A-Z]$"), "Enter a valid PAN, for example ABCDE1234F.");
            Guard.Require(System.Text.RegularExpressions.Regex.IsMatch(r.AadhaarLast4 ?? "", "^[0-9]{4}$"), "Enter only the last 4 digits of Aadhaar.");
            var acct = new string((r.AccountNumber ?? "").Where(char.IsDigit).ToArray());
            Guard.Require(acct.Length is >= 9 and <= 18, "Enter a valid bank account number.");
            var ifsc = (r.Ifsc ?? "").Trim().ToUpperInvariant();
            Guard.Require(System.Text.RegularExpressions.Regex.IsMatch(ifsc, "^[A-Z]{4}0[A-Z0-9]{6}$"), "Enter a valid IFSC, for example HDFC0001234.");
            Guard.Require(!string.IsNullOrWhiteSpace(r.AccountHolder), "Enter the account holder's name.");
            var mobile = await StartSignup(db, otp, r.Mobile, r.Code, r.FullName);

            var user = new User { Role = Roles.Owner, FullName = r.FullName.Trim(), Mobile = mobile, Email = Clean(r.Email), Status = "PendingKyc" };
            var owner = new Owner
            {
                User = user, BusinessName = Clean(r.BusinessName),
                PanEncrypted = pii.ProtectBytes(pan), PanLast4 = pan[^4..], AadhaarLast4 = r.AadhaarLast4
            };
            db.Users.Add(user);
            db.Owners.Add(owner);
            await db.SaveChangesAsync();
            db.OwnerBankAccounts.Add(new OwnerBankAccount
            {
                OwnerId = owner.OwnerId, AccountHolder = r.AccountHolder.Trim(), AccountNumberEncrypted = pii.ProtectBytes(acct),
                AccountLast4 = acct[^4..], IFSC = ifsc, BankName = Clean(r.BankName)
            });
            await db.SaveChangesAsync();
            return new { token = tokens.Create(user), user = await Profile(db, user) };
        });

        g.MapPost("/register/driver", async (RegisterDriverRequest r, OlekarDbContext db, OtpService otp, TokenService tokens) =>
        {
            var lic = (r.LicenceNumber ?? "").Replace(" ", "").Replace("-", "").ToUpperInvariant();
            Guard.Require(lic.Length is >= 10 and <= 20, "Enter the driving licence number as printed.");
            Guard.Require(new[] { "LMV", "TRANSPORT", "HGMV", "HPMV" }.Contains(r.LicenceClass), "Choose the licence class.");
            Guard.Require(r.LicenceExpiry > DateOnly.FromDateTime(DateTime.UtcNow), "This licence has expired.");
            Guard.Require(string.IsNullOrEmpty(r.AadhaarLast4) || System.Text.RegularExpressions.Regex.IsMatch(r.AadhaarLast4, "^[0-9]{4}$"), "Enter only the last 4 digits of Aadhaar.");
            Guard.Require(!await db.Drivers.AnyAsync(d => d.LicenceNumber == lic), "This licence is already registered.");
            long? ownerId = null;
            if (!string.IsNullOrWhiteSpace(r.OwnerMobile))
            {
                var ownerMobile = Mobile.Normalize(r.OwnerMobile);
                ownerId = await db.Owners.Where(o => o.User.Mobile == ownerMobile).Select(o => (long?)o.OwnerId).FirstOrDefaultAsync()
                    ?? throw ApiException.BadRequest("No lorry owner is registered with that mobile number.");
            }
            var mobile = await StartSignup(db, otp, r.Mobile, r.Code, r.FullName);
            var user = new User { Role = Roles.Driver, FullName = r.FullName.Trim(), Mobile = mobile, Status = "PendingKyc" };
            db.Users.Add(user);
            db.Drivers.Add(new Driver
            {
                User = user, OwnerId = ownerId, LicenceNumber = lic, LicenceClass = r.LicenceClass, LicenceExpiry = r.LicenceExpiry,
                AadhaarLast4 = Clean(r.AadhaarLast4), EmergencyContactName = Clean(r.EmergencyContactName), EmergencyContactPhone = Clean(r.EmergencyContactPhone)
            });
            await db.SaveChangesAsync();
            return new { token = tokens.Create(user), user = await Profile(db, user) };
        });

        g.MapGet("/me", async (ClaimsPrincipal me, OlekarDbContext db) =>
        {
            var user = await db.Users.FindAsync(me.UserId()) ?? throw ApiException.NotFound("Account not found.");
            return await Profile(db, user);
        }).RequireAuthorization();
    }

    private static async Task<string> StartSignup(OlekarDbContext db, OtpService otp, string rawMobile, string code, string fullName)
    {
        Guard.Require(!string.IsNullOrWhiteSpace(fullName) && fullName.Trim().Length >= 2, "Enter your full name.");
        var mobile = Mobile.Normalize(rawMobile);
        if (await db.Users.AnyAsync(u => u.Mobile == mobile)) throw ApiException.Conflict("This number is already registered. Sign in instead.");
        await otp.VerifyAsync(mobile, code ?? "", "Signup");
        return mobile;
    }

    private static string? Clean(string? s) => string.IsNullOrWhiteSpace(s) ? null : s.Trim();

    /// <summary>What the portal needs to know about the signed-in user.</summary>
    public static async Task<object> Profile(OlekarDbContext db, User u)
    {
        object? detail = u.Role switch
        {
            Roles.Customer => await db.Customers.Where(c => c.UserId == u.UserId)
                .Select(c => new { c.CustomerId, c.CompanyName, c.GSTIN }).FirstOrDefaultAsync(),
            Roles.Owner => await db.Owners.Where(o => o.UserId == u.UserId)
                .Select(o => new { o.OwnerId, o.BusinessName, o.KycStatus, o.RejectionReason, o.PanLast4, o.AadhaarLast4 }).FirstOrDefaultAsync(),
            Roles.Driver => await db.Drivers.Where(d => d.UserId == u.UserId)
                .Select(d => new { d.DriverId, d.KycStatus, d.RejectionReason, d.LicenceNumber, d.LicenceClass, d.LicenceExpiry, ownerName = d.Owner != null ? d.Owner.User.FullName : null })
                .FirstOrDefaultAsync(),
            _ => null
        };
        return new { id = u.UserId, u.Role, u.FullName, u.Mobile, u.Email, u.Status, detail };
    }
}
