using FluentValidation;
using Microsoft.Extensions.Logging;
using ProCargo.Application.Abstractions.Persistence;
using ProCargo.Application.Abstractions.Security;
using ProCargo.Application.Common;
using ProCargo.Application.Common.Exceptions;
using ProCargo.Application.Features.Auth;
using ProCargo.Domain.Common;
using ProCargo.Domain.Constants;
using ProCargo.Domain.Entities;

namespace ProCargo.Application.Features.Registration;

internal sealed class RegistrationService(
    IRegistrationRepository registrations,
    IUserRepository users,
    IOwnerRepository owners,
    IOtpService otp,
    ISessionIssuer sessions,
    IPersonalDataProtector protector,
    IValidator<RegisterCustomerRequest> customerValidator,
    IValidator<RegisterOwnerRequest> ownerValidator,
    IValidator<RegisterDriverRequest> driverValidator,
    ILogger<RegistrationService> logger) : IRegistrationService
{
    public async Task<AuthResponse> RegisterCustomerAsync(RegisterCustomerRequest request, CancellationToken cancellationToken)
    {
        await customerValidator.ValidateAndThrowAsync(request, cancellationToken);
        string mobile = await VerifySignupAsync(request.Mobile, request.Code, cancellationToken);

        var account = new NewCustomerAccount(
            request.FullName.Trim(),
            mobile,
            Text.Clean(request.Email),
            Text.Clean(request.CompanyName),
            Text.Clean(request.Gstin)?.ToUpperInvariant());

        long userId = await registrations.CreateCustomerAsync(account, cancellationToken);
        return await SignInNewUserAsync(userId, cancellationToken);
    }

    public async Task<AuthResponse> RegisterOwnerAsync(RegisterOwnerRequest request, CancellationToken cancellationToken)
    {
        await ownerValidator.ValidateAndThrowAsync(request, cancellationToken);

        string pan = IndianFormats.CleanUpper(request.Pan);
        string ifsc = IndianFormats.CleanUpper(request.Ifsc);
        string accountNumber = IndianFormats.CleanAccountNumber(request.AccountNumber);

        string mobile = await VerifySignupAsync(request.Mobile, request.Code, cancellationToken);

        var account = new NewOwnerAccount(
            request.FullName.Trim(),
            mobile,
            Text.Clean(request.Email),
            Text.Clean(request.BusinessName),
            protector.ProtectToBytes(pan),
            pan[^4..],
            request.AadhaarLast4,
            request.AccountHolder.Trim(),
            protector.ProtectToBytes(accountNumber),
            accountNumber[^4..],
            ifsc,
            Text.Clean(request.BankName));

        long userId = await registrations.CreateOwnerAsync(account, cancellationToken);
        return await SignInNewUserAsync(userId, cancellationToken);
    }

    public async Task<AuthResponse> RegisterDriverAsync(RegisterDriverRequest request, CancellationToken cancellationToken)
    {
        await driverValidator.ValidateAndThrowAsync(request, cancellationToken);
        string licenceNumber = IndianFormats.CleanLicence(request.LicenceNumber);

        if (await registrations.LicenceExistsAsync(licenceNumber, cancellationToken))
        {
            throw new ConflictException("This licence is already registered.");
        }

        long? ownerId = await FindOwnerIdByMobileAsync(request.OwnerMobile, cancellationToken);
        string mobile = await VerifySignupAsync(request.Mobile, request.Code, cancellationToken);

        var account = new NewDriverAccount(
            request.FullName.Trim(),
            mobile,
            Text.Clean(request.Email),
            ownerId,
            licenceNumber,
            request.LicenceClass,
            request.LicenceExpiry,
            Text.Clean(request.AadhaarLast4),
            Text.Clean(request.EmergencyContactName),
            Text.Clean(request.EmergencyContactPhone));

        long userId = await registrations.CreateDriverAsync(account, cancellationToken);
        return await SignInNewUserAsync(userId, cancellationToken);
    }

    /// <summary>Checks that the mobile is new and the sign-up code is right. Returns the clean mobile.</summary>
    private async Task<string> VerifySignupAsync(string rawMobile, string code, CancellationToken cancellationToken)
    {
        string mobile = MobileNumber.TryNormalize(rawMobile)!;

        if (await users.GetByMobileAsync(mobile, cancellationToken) is not null)
        {
            throw new ConflictException("This number is already registered. Sign in instead.");
        }

        await otp.VerifyAsync(mobile, code, OtpPurpose.Signup, cancellationToken);
        return mobile;
    }

    /// <summary>A driver links to their owner by the owner's mobile number. Empty = drives their own truck.</summary>
    private async Task<long?> FindOwnerIdByMobileAsync(string? ownerMobile, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(ownerMobile))
        {
            return null;
        }

        string mobile = MobileNumber.TryNormalize(ownerMobile)!;
        Owner owner = await owners.GetByMobileAsync(mobile, cancellationToken)
            ?? throw new BusinessRuleException("No lorry owner is registered with that mobile number.");

        return owner.OwnerId;
    }

    private async Task<AuthResponse> SignInNewUserAsync(long userId, CancellationToken cancellationToken)
    {
        User user = await users.GetByIdAsync(userId, cancellationToken)
            ?? throw new NotFoundException("Account not found.");

        logger.LogInformation("New {Role} account {UserId} registered", user.Role, user.UserId);
        return await sessions.StartSessionAsync(user, cancellationToken);
    }
}
