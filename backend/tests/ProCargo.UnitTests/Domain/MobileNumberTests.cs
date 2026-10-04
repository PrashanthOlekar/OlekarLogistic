using ProCargo.Domain.Common;

namespace ProCargo.UnitTests.Domain;

public sealed class MobileNumberTests
{
    [Theory]
    [InlineData("9845012345", "9845012345")]
    [InlineData("+91 98450 12345", "9845012345")]
    [InlineData("098450-12345", "9845012345")]
    [InlineData("6000000000", "6000000000")]
    public void Valid_numbers_are_cleaned_to_ten_digits(string input, string expected)
    {
        Assert.Equal(expected, MobileNumber.TryNormalize(input));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("12345")]
    [InlineData("5845012345")]
    [InlineData("abcdefghij")]
    public void Invalid_numbers_are_rejected(string? input)
    {
        Assert.Null(MobileNumber.TryNormalize(input));
        Assert.False(MobileNumber.IsValid(input));
    }
}
