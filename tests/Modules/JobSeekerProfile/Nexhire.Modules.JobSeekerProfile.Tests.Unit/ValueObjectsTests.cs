using FluentAssertions;
using Nexhire.Modules.JobSeekerProfile.Core.Domain.ValueObjects;
using Xunit;

namespace Nexhire.Modules.JobSeekerProfile.Tests.Unit;

public class ValueObjectsTests
{
    private static object[] GetNullData() => new object[] { null! };

    [Fact]
    public void PersonName_Create_Should_Succeed_WhenInputIsValid()
    {
        // Act
        var result = PersonName.Create("John", "Doe");

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.First.Should().Be("John");
        result.Value.Last.Should().Be("Doe");
    }

    [Theory]
    [InlineData("", "Doe")]
    [InlineData("John", "")]
    [InlineData("   ", "Doe")]
    public void PersonName_Create_Should_Fail_WhenEitherNameIsEmpty(string first, string last)
    {
        // Act
        var result = PersonName.Create(first, last);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("PersonName.Empty");
    }

    [Fact]
    public void PersonName_Create_Should_Fail_WhenFirstExceeds100Chars()
    {
        // Arrange
        var longFirst = new string('A', 101);

        // Act
        var result = PersonName.Create(longFirst, "Doe");

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("PersonName.TooLong");
    }

    [Fact]
    public void EmailAddress_Create_Should_Succeed_WhenInputIsValid()
    {
        // Act
        var result = EmailAddress.Create("seeker@nexhire.com");

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Value.Should().Be("seeker@nexhire.com");
    }

    [Theory]
    [InlineData("invalid-email")]
    [InlineData("seeker@")]
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
    public void EmailAddress_Create_Should_LowercaseInput()
    {
        // Act
        var result = EmailAddress.Create("SEEKER@NEXHIRE.COM");

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Value.Should().Be("seeker@nexhire.com");
    }

    [Theory]
    [InlineData("+8801712345678", "+8801712345678")]
    [InlineData("01712345678", "+8801712345678")]
    [InlineData("1712345678", "+8801712345678")]
    public void MobileNumber_Create_Should_Succeed_AndFormatCorrectly_WhenInputIsValid(string input, string expected)
    {
        // Act
        var result = MobileNumber.Create(input);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Value.Should().Be(expected);
    }

    [Theory]
    [InlineData("invalid-number")]
    [InlineData("123456")]
    public void MobileNumber_Create_Should_Fail_WhenInputIsInvalid(string mobile)
    {
        // Act
        var result = MobileNumber.Create(mobile);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("MobileNumber.Invalid");
    }

    [Fact]
    public void DateRange_Create_Should_Succeed_WhenStartIsBeforeOrEqualEnd()
    {
        // Arrange
        var start = new DateTime(2020, 1, 1);
        var end = new DateTime(2023, 1, 1);

        // Act
        var result = DateRange.Create(start, end);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Start.Should().Be(start);
        result.Value.End.Should().Be(end);
    }

    [Fact]
    public void DateRange_Create_Should_Succeed_WhenEndIsNull()
    {
        // Arrange
        var start = new DateTime(2020, 1, 1);

        // Act
        var result = DateRange.Create(start, null);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Start.Should().Be(start);
        result.Value.End.Should().BeNull();
    }

    [Fact]
    public void DateRange_Create_Should_Fail_WhenStartIsAfterEnd()
    {
        // Arrange
        var start = new DateTime(2023, 1, 1);
        var end = new DateTime(2020, 1, 1);

        // Act
        var result = DateRange.Create(start, end);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("DateRange.Invalid");
    }

    [Fact]
    public void Money_Create_Should_Succeed_WhenAmountIsZeroOrPositive()
    {
        // Act
        var zero = Money.Create(0, "BDT");
        var positive = Money.Create(50000, "BDT");

        // Assert
        zero.IsSuccess.Should().BeTrue();
        zero.Value.Amount.Should().Be(0);
        positive.IsSuccess.Should().BeTrue();
        positive.Value.Amount.Should().Be(50000);
    }

    [Fact]
    public void Money_Create_Should_Fail_WhenAmountIsNegative()
    {
        // Act
        var result = Money.Create(-100, "BDT");

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Money.NegativeAmount");
    }

    [Fact]
    public void SalaryExpectation_Create_Should_Succeed_WhenMinIsLessOrEqualMax_AndCurrencyMatches()
    {
        // Arrange
        var min = Money.Create(40000, "BDT").Value;
        var max = Money.Create(60000, "BDT").Value;

        // Act
        var result = SalaryExpectation.Create(min, max);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Min.Should().Be(min);
        result.Value.Max.Should().Be(max);
    }

    [Fact]
    public void SalaryExpectation_Create_Should_Fail_WhenMinIsGreaterThanMax()
    {
        // Arrange
        var min = Money.Create(60000, "BDT").Value;
        var max = Money.Create(40000, "BDT").Value;

        // Act
        var result = SalaryExpectation.Create(min, max);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("SalaryExpectation.MinGreaterThanMax");
    }

    [Fact]
    public void SalaryExpectation_Create_Should_Fail_WhenCurrenciesMismatch()
    {
        // Arrange
        var min = Money.Create(40000, "BDT").Value;
        var max = Money.Create(500, "USD").Value;

        // Act
        var result = SalaryExpectation.Create(min, max);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("SalaryExpectation.CurrencyMismatch");
    }

    [Theory]
    [InlineData(0, true)]
    [InlineData(50, true)]
    [InlineData(100, true)]
    [InlineData(-5, false)]
    [InlineData(105, false)]
    public void CompletenessScore_Create_Should_ValidatePercentageCorrectly(int percentage, bool shouldSucceed)
    {
        // Act
        var result = CompletenessScore.Create(percentage, new List<string>());

        // Assert
        if (shouldSucceed)
        {
            result.IsSuccess.Should().BeTrue();
            result.Value.Percentage.Should().Be(percentage);
        }
        else
        {
            result.IsFailure.Should().BeTrue();
            result.Error.Code.Should().Be("CompletenessScore.InvalidPercentage");
        }
    }

    [Theory]
    [InlineData(0, false)]
    [InlineData(69, false)]
    [InlineData(70, true)]
    [InlineData(100, true)]
    public void ConfidenceScore_NeedsVerification_BoundaryCondition(int val, bool isHighConfidence)
    {
        // Act
        var score = ConfidenceScore.Create(val).Value;

        // Assert
        score.Value.Should().Be(val);
        score.NeedsVerification.Should().Be(!isHighConfidence);
    }
}
