using FluentAssertions;
using Nexhire.Modules.IdentityAccess.Core.Domain.ValueObjects;
using Nexhire.Shared.Core.Results;

namespace Nexhire.Modules.IdentityAccess.Tests.Unit.Domain.ValueObjects;

public class MobileNumberTests
{
    public class Create
    {
        [Fact]
        public void Should_Succeed_When_Mobile_Is_Valid_E164_With_Plus()
        {
            // Arrange
            var mobile = "+8801712345678";

            // Act
            var result = MobileNumber.Create(mobile);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Value.Value.Should().Be(mobile);
        }

        [Fact]
        public void Should_Succeed_When_Mobile_Is_Valid_Without_Plus()
        {
            // Arrange
            var mobile = "8801712345678";

            // Act
            var result = MobileNumber.Create(mobile);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Value.Value.Should().Be("+8801712345678");
        }

        [Fact]
        public void Should_Fail_When_Mobile_Is_Empty()
        {
            // Arrange
            var mobile = "";

            // Act
            var result = MobileNumber.Create(mobile);

            // Assert
            result.IsFailure.Should().BeTrue();
            result.Error.Code.Should().Be("Mobile.Empty");
        }

        [Fact]
        public void Should_Fail_When_Mobile_Is_Null()
        {
            // Arrange
            string? mobile = null;

            // Act
            var result = MobileNumber.Create(mobile!);

            // Assert
            result.IsFailure.Should().BeTrue();
            result.Error.Code.Should().Be("Mobile.Empty");
        }

        [Fact]
        public void Should_Fail_When_Mobile_Is_Whitespace()
        {
            // Arrange
            var mobile = "   ";

            // Act
            var result = MobileNumber.Create(mobile);

            // Assert
            result.IsFailure.Should().BeTrue();
            result.Error.Code.Should().Be("Mobile.Empty");
        }

        [Fact]
        public void Should_Fail_When_Mobile_Has_Invalid_Characters()
        {
            // Arrange
            var mobile = "+88017abcd5678";

            // Act
            var result = MobileNumber.Create(mobile);

            // Assert
            result.IsFailure.Should().BeTrue();
            result.Error.Code.Should().Be("Mobile.Invalid");
        }

        [Fact]
        public void Should_Fail_When_Mobile_Is_Too_Short()
        {
            // Arrange
            var mobile = "+880171";

            // Act
            var result = MobileNumber.Create(mobile);

            // Assert
            result.IsFailure.Should().BeTrue();
            result.Error.Code.Should().Be("Mobile.Invalid");
        }

        [Fact]
        public void Should_Fail_When_Mobile_Is_Too_Long()
        {
            // Arrange
            var mobile = "+88017123456789012345";

            // Act
            var result = MobileNumber.Create(mobile);

            // Assert
            result.IsFailure.Should().BeTrue();
            result.Error.Code.Should().Be("Mobile.Invalid");
        }

        [Fact]
        public void Should_Add_Default_Country_Code_When_Missing()
        {
            // Arrange
            var mobile = "01712345678";

            // Act
            var result = MobileNumber.Create(mobile, "+880");

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Value.Value.Should().Be("+8801712345678");
        }

        [Fact]
        public void Should_Not_Add_Country_Code_When_Present()
        {
            // Arrange
            var mobile = "+8801712345678";

            // Act
            var result = MobileNumber.Create(mobile, "+880");

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Value.Value.Should().Be("+8801712345678");
        }
    }
}