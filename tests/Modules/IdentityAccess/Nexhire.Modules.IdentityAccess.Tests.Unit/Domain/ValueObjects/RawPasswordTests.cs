using FluentAssertions;
using Nexhire.Modules.IdentityAccess.Core.Domain.ValueObjects;
using Nexhire.Shared.Core.Results;

namespace Nexhire.Modules.IdentityAccess.Tests.Unit.Domain.ValueObjects;

public class RawPasswordTests
{
    public class Create
    {
        [Fact]
        public void Should_Succeed_When_Password_Meets_All_Requirements()
        {
            // Arrange
            var password = "StrongP@ssw0rd123";

            // Act
            var result = RawPassword.Create(password);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Value.Value.Should().Be(password);
        }

        [Fact]
        public void Should_Fail_When_Password_Is_Empty()
        {
            // Arrange
            var password = "";

            // Act
            var result = RawPassword.Create(password);

            // Assert
            result.IsFailure.Should().BeTrue();
            result.Error.Code.Should().Be("Password.Empty");
        }

        [Fact]
        public void Should_Fail_When_Password_Is_Null()
        {
            // Arrange
            string? password = null;

            // Act
            var result = RawPassword.Create(password!);

            // Assert
            result.IsFailure.Should().BeTrue();
            result.Error.Code.Should().Be("Password.Empty");
        }

        [Fact]
        public void Should_Fail_When_Password_Is_Too_Short()
        {
            // Arrange
            var password = "Short1!";

            // Act
            var result = RawPassword.Create(password);

            // Assert
            result.IsFailure.Should().BeTrue();
            result.Error.Code.Should().Be("Password.TooShort");
        }

        [Fact]
        public void Should_Succeed_When_Password_Has_No_Lowercase_But_Has_Three_Classes()
        {
            // Arrange
            var password = "UPPERCASE123!";

            // Act
            var result = RawPassword.Create(password);

            // Assert
            result.IsSuccess.Should().BeTrue();
        }

        [Fact]
        public void Should_Succeed_When_Password_Has_No_Uppercase_But_Has_Three_Classes()
        {
            // Arrange
            var password = "lowercase123!";

            // Act
            var result = RawPassword.Create(password);

            // Assert
            result.IsSuccess.Should().BeTrue();
        }

        [Fact]
        public void Should_Succeed_When_Password_Has_No_Digit_But_Has_Three_Classes()
        {
            // Arrange
            var password = "NoDigitsHere!";

            // Act
            var result = RawPassword.Create(password);

            // Assert
            result.IsSuccess.Should().BeTrue();
        }

        [Fact]
        public void Should_Succeed_When_Password_Has_No_Special_Character_But_Has_Three_Classes()
        {
            // Arrange
            var password = "NoSpecialChars123";

            // Act
            var result = RawPassword.Create(password);

            // Assert
            result.IsSuccess.Should().BeTrue();
        }

        [Fact]
        public void Should_Fail_When_Password_Has_Only_One_Character_Class()
        {
            // Arrange
            var password = "onlylowercase";

            // Act
            var result = RawPassword.Create(password);

            // Assert
            result.IsFailure.Should().BeTrue();
            result.Error.Code.Should().Be("Password.MissingCharacterClass");
        }

        [Fact]
        public void Should_Fail_When_Password_Has_Only_Two_Character_Classes()
        {
            // Arrange
            var password = "onlylowercase123"; // only lowercase and digit = 2 classes

            // Act
            var result = RawPassword.Create(password);

            // Assert
            result.IsFailure.Should().BeTrue();
            result.Error.Code.Should().Be("Password.MissingCharacterClass");
        }

        [Fact]
        public void Should_Succeed_When_Password_Has_Exactly_Three_Character_Classes()
        {
            // Arrange
            var password = "threeClasses1";

            // Act
            var result = RawPassword.Create(password);

            // Assert
            result.IsSuccess.Should().BeTrue();
        }

        [Fact]
        public void Should_Fail_When_Password_Has_Trivial_Sequence()
        {
            // Arrange
            var password = "Abcdefgh123!";

            // Act
            var result = RawPassword.Create(password);

            // Assert
            result.IsFailure.Should().BeTrue();
            result.Error.Code.Should().Be("Password.WeakSequence");
        }

        [Fact]
        public void Should_Fail_When_Password_Has_Numeric_Sequence()
        {
            // Arrange
            var password = "Password12345!";

            // Act
            var result = RawPassword.Create(password);

            // Assert
            result.IsFailure.Should().BeTrue();
            result.Error.Code.Should().Be("Password.WeakSequence");
        }
    }
}