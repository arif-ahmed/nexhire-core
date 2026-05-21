using FluentAssertions;
using Nexhire.Modules.EmployerProfiles.Core.Domain.ValueObjects;
using Xunit;

namespace Nexhire.Modules.EmployerProfiles.Tests.Unit;

public class ValueObjectsTests
{
    [Fact]
    public void CompanyName_Create_Should_Succeed_WhenInputIsValid()
    {
        // Act
        var result = CompanyName.Create("Nexhire Corp");

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Value.Should().Be("Nexhire Corp");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [MemberData(nameof(GetNullData))]
    public void CompanyName_Create_Should_Fail_WhenInputIsEmpty(string? name)
    {
        // Act
        var result = CompanyName.Create(name!);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("CompanyName.Empty");
    }

    [Fact]
    public void CompanyName_Create_Should_Fail_WhenInputExceeds200Chars()
    {
        // Arrange
        var longName = new string('A', 201);

        // Act
        var result = CompanyName.Create(longName);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("CompanyName.TooLong");
    }

    [Fact]
    public void EmailAddress_Create_Should_Succeed_WhenInputIsValid()
    {
        // Act
        var result = EmailAddress.Create("info@nexhire.com");

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Value.Should().Be("info@nexhire.com");
    }

    [Theory]
    [InlineData("invalid-email")]
    [InlineData("info@")]
    [InlineData("@nexhire.com")]
    public void EmailAddress_Create_Should_Fail_WhenInputIsInvalid(string email)
    {
        // Act
        var result = EmailAddress.Create(email);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("EmailAddress.Invalid");
    }

    [Fact]
    public void MobileNumber_Create_Should_Succeed_WhenInputIsValid()
    {
        // Act
        var result = MobileNumber.Create("+8801712345678");

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Value.Should().Be("+8801712345678");
    }

    [Theory]
    [InlineData("1712345678")]
    [InlineData("+880-1712-345678")]
    public void MobileNumber_Create_Should_Fail_WhenInputIsInvalid(string mobile)
    {
        // Act
        var result = MobileNumber.Create(mobile);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("MobileNumber.Invalid");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [MemberData(nameof(GetNullData))]
    public void MobileNumber_Create_Should_Fail_WhenInputIsEmpty(string? mobile)
    {
        // Act
        var result = MobileNumber.Create(mobile!);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("MobileNumber.Empty");
    }

    [Fact]
    public void CompanyIdentifier_Create_Should_Succeed_WhenInputIsValid()
    {
        // Act
        var result = CompanyIdentifier.Create("REG123456");

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Value.Should().Be("REG123456");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void CompanyIdentifier_Create_Should_Fail_WhenInputIsEmpty(string identifier)
    {
        // Act
        var result = CompanyIdentifier.Create(identifier);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("CompanyIdentifier.Empty");
    }

    [Fact]
    public void WebsiteUrl_Create_Should_Succeed_WhenInputIsValid()
    {
        // Act
        var result = WebsiteUrl.Create("https://nexhire.com");

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Value.Should().Be("https://nexhire.com");
    }

    [Theory]
    [InlineData("ftp://nexhire.com")]
    [InlineData("just-text")]
    public void WebsiteUrl_Create_Should_Fail_WhenInputIsInvalid(string url)
    {
        // Act
        var result = WebsiteUrl.Create(url);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("WebsiteUrl.Invalid");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [MemberData(nameof(GetNullData))]
    public void WebsiteUrl_Create_Should_Fail_WhenInputIsEmpty(string? url)
    {
        // Act
        var result = WebsiteUrl.Create(url!);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("WebsiteUrl.Empty");
    }

    [Fact]
    public void CompanySize_Create_Should_Succeed_WhenInputIsValid()
    {
        // Act
        var result = CompanySize.Create("Micro");

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Value.Should().Be(CompanySizeEnum.Micro);
    }

    [Fact]
    public void CompanySize_Create_Should_Fail_WhenInputIsInvalid()
    {
        // Act
        var result = CompanySize.Create("SuperLarge");

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("CompanySize.Invalid");
    }

    [Fact]
    public void Address_Create_Should_Succeed_WhenInputIsValid()
    {
        // Act
        var result = Address.Create("Line 1", "Line 2", "Dhaka", "Dhaka", "1212", "Bangladesh");

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Line1.Should().Be("Line 1");
    }

    [Theory]
    [InlineData("", "Dhaka", "Dhaka", "Bangladesh")]
    [InlineData("Line 1", "", "Dhaka", "Bangladesh")]
    [InlineData("Line 1", "Dhaka", "", "Bangladesh")]
    [InlineData("Line 1", "Dhaka", "Dhaka", "")]
    public void Address_Create_Should_Fail_WhenRequiredFieldsAreMissing(string line1, string city, string district, string country)
    {
        // Act
        var result = Address.Create(line1, null, city, district, "1212", country);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Address.RequiredFieldsMissing");
    }

    [Fact]
    public void CompanyDescription_Create_Should_Succeed_WhenInputIsValid()
    {
        // Act
        var result = CompanyDescription.Create("Excellent workplace.");

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Value.Should().Be("Excellent workplace.");
    }

    [Fact]
    public void CompanyDescription_Create_Should_Fail_WhenInputExceeds5000Chars()
    {
        // Arrange
        var longDesc = new string('D', 5001);

        // Act
        var result = CompanyDescription.Create(longDesc);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("CompanyDescription.TooLong");
    }

    [Fact]
    public void FileReference_Create_Should_Succeed_WhenInputIsValid()
    {
        // Act
        var result = FileReference.Create("key123", "logo.png", "image/png", 1024 * 1024);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.StorageKey.Should().Be("key123");
    }

    [Fact]
    public void FileReference_Create_Should_Fail_WhenSizeIsZeroOrNegative()
    {
        // Act
        var result = FileReference.Create("key123", "logo.png", "image/png", 0);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("FileReference.InvalidSize");
    }

    [Fact]
    public void VerificationState_ManualRejected_Should_Fail_WhenReasonIsMissing()
    {
        // Act
        var result = VerificationState.Create(VerificationOutcome.ManualRejected, VerificationMethod.Manual, null, null, DateTime.UtcNow);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("VerificationState.RejectionReasonRequired");
    }

    public static IEnumerable<object?[]> GetNullData()
    {
        yield return new object?[] { null };
    }
}
