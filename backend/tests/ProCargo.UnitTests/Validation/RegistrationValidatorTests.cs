using FluentValidation.Results;
using ProCargo.Application.Features.Registration;

namespace ProCargo.UnitTests.Validation;

public sealed class RegistrationValidatorTests
{
    private static RegisterOwnerRequest ValidOwner() => new(
        FullName: "Manjunath Patil",
        Mobile: "+91 98450 12345",
        Code: "123456",
        Email: null,
        BusinessName: "Patil Transport",
        Pan: "abcde1234f",
        AadhaarLast4: "1234",
        AccountHolder: "Manjunath Patil",
        AccountNumber: "1234 5678 9012",
        Ifsc: "hdfc0001234",
        BankName: "HDFC Bank");

    [Fact]
    public void Owner_with_lower_case_pan_and_spaced_account_number_is_valid()
    {
        Assert.True(new RegisterOwnerRequestValidator().Validate(ValidOwner()).IsValid);
    }

    [Theory]
    [InlineData("ABCDE12345", "Enter a valid PAN, for example ABCDE1234F.")]
    [InlineData("12345", "Enter a valid PAN, for example ABCDE1234F.")]
    public void Bad_pan_is_rejected(string pan, string message)
    {
        ValidationResult result = new RegisterOwnerRequestValidator().Validate(ValidOwner() with { Pan = pan });

        Assert.Contains(result.Errors, error => error.ErrorMessage == message);
    }

    [Fact]
    public void Full_aadhaar_number_is_refused()
    {
        ValidationResult result = new RegisterOwnerRequestValidator().Validate(ValidOwner() with { AadhaarLast4 = "123456789012" });

        Assert.Contains(result.Errors, error => error.ErrorMessage == "Enter only the last 4 digits of Aadhaar.");
    }

    [Fact]
    public void Driver_with_expired_licence_is_rejected()
    {
        var driver = new RegisterDriverRequest(
            "Ramesh Gowda", "9876543210", "123456", null,
            "KA01 20110012345", "HGMV", new DateOnly(2026, 10, 1),
            null, null, null, null);

        ValidationResult result = new RegisterDriverRequestValidator(TestClock.Default).Validate(driver);

        Assert.Contains(result.Errors, error => error.ErrorMessage == "This licence has expired.");
    }

    [Fact]
    public void Customer_gstin_must_have_15_characters()
    {
        var customer = new RegisterCustomerRequest("Anitha Rao", "9845012345", "123456", "anitha@example.com", "Rao Builders", "29ABCDE1234");

        ValidationResult result = new RegisterCustomerRequestValidator().Validate(customer);

        Assert.Contains(result.Errors, error => error.ErrorMessage == "GSTIN has 15 characters.");
    }
}
