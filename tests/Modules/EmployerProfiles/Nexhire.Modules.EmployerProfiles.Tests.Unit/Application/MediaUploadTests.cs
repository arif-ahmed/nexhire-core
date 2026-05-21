using FluentAssertions;
using NSubstitute;
using Nexhire.Modules.EmployerProfiles.Core.Domain.Aggregates;
using Nexhire.Modules.EmployerProfiles.Core.Domain.Ports;
using Nexhire.Modules.EmployerProfiles.Core.Domain.Repositories;
using Nexhire.Modules.EmployerProfiles.Core.Domain.ValueObjects;
using Nexhire.Modules.EmployerProfiles.Core.EmployerProfiles.Commands.UploadEmployerLogo;
using Nexhire.Modules.EmployerProfiles.Core.EmployerProfiles.Commands.UploadCompanyImage;
using Nexhire.Modules.EmployerProfiles.Core.EmployerProfiles.Commands.RemoveCompanyImage;
using Nexhire.Modules.EmployerProfiles.Core.EmployerProfiles.Commands.UploadEmployerDocument;
using Nexhire.Modules.EmployerProfiles.Core.EmployerProfiles.Commands.RemoveEmployerDocument;
using Nexhire.Shared.Core.Results;
using Xunit;

namespace Nexhire.Modules.EmployerProfiles.Tests.Unit.Application;

public class MediaUploadTests
{
    private readonly IEmployerProfileRepository _repository = Substitute.For<IEmployerProfileRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly IObjectStorage _objectStorage = Substitute.For<IObjectStorage>();
    private readonly IVirusScanner _virusScanner = Substitute.For<IVirusScanner>();

    private EmployerProfile CreateActiveProfile(Guid userId)
    {
        var profile = EmployerProfile.Register(
            Guid.NewGuid(),
            userId,
            CompanyName.Create("Nexhire Inc.").Value,
            EmailAddress.Create("info@nexhire.com").Value,
            MobileNumber.Create("+8801712345678").Value,
            CompanyIdentifier.Create("REG123456").Value);

        profile.Activate();
        return profile;
    }

    [Fact]
    public async Task UploadEmployerLogo_ShouldSucceed_WhenValidFile()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var profile = CreateActiveProfile(userId);
        _repository.GetByUserIdAsync(userId, Arg.Any<CancellationToken>()).Returns(profile);

        var fileRef = FileReference.Create("logos/logo1.png", "logo.png", "image/png", 1024).Value;
        _objectStorage.StoreAsync(Arg.Any<byte[]>(), "logo.png", "image/png", Arg.Any<CancellationToken>())
            .Returns(Result.Success(fileRef));

        var scanResult = VirusScanResult.Create(VirusScanStatus.Clean).Value;
        _virusScanner.ScanAsync(fileRef, Arg.Any<CancellationToken>())
            .Returns(scanResult);

        var handler = new UploadEmployerLogoCommandHandler(_repository, _unitOfWork, _objectStorage, _virusScanner);
        var command = new UploadEmployerLogoCommand(userId, new byte[] { 1, 2, 3 }, "logo.png", "image/png");

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        profile.Logo.Should().Be(fileRef);
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UploadEmployerLogo_ShouldFail_WhenInfectedFileAndDeletedFromStorage()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var profile = CreateActiveProfile(userId);
        _repository.GetByUserIdAsync(userId, Arg.Any<CancellationToken>()).Returns(profile);

        var fileRef = FileReference.Create("logos/infected.png", "logo.png", "image/png", 1024).Value;
        _objectStorage.StoreAsync(Arg.Any<byte[]>(), "logo.png", "image/png", Arg.Any<CancellationToken>())
            .Returns(Result.Success(fileRef));

        var scanResult = VirusScanResult.Create(VirusScanStatus.Infected).Value;
        _virusScanner.ScanAsync(fileRef, Arg.Any<CancellationToken>())
            .Returns(scanResult);

        var handler = new UploadEmployerLogoCommandHandler(_repository, _unitOfWork, _objectStorage, _virusScanner);
        var command = new UploadEmployerLogoCommand(userId, new byte[] { 1, 2, 3 }, "logo.png", "image/png");

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("E-UPLOAD-VIRUS");
        profile.Logo.Should().BeNull();
        await _objectStorage.Received(1).DeleteAsync(fileRef.StorageKey, Arg.Any<CancellationToken>());
        await _unitOfWork.DidNotReceiveWithAnyArgs().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UploadCompanyImage_ShouldSucceed_WhenValid()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var profile = CreateActiveProfile(userId);
        _repository.GetByUserIdAsync(userId, Arg.Any<CancellationToken>()).Returns(profile);

        var fileRef = FileReference.Create("images/gallery.jpg", "gallery.jpg", "image/jpeg", 2048).Value;
        _objectStorage.StoreAsync(Arg.Any<byte[]>(), "gallery.jpg", "image/jpeg", Arg.Any<CancellationToken>())
            .Returns(Result.Success(fileRef));

        var scanResult = VirusScanResult.Create(VirusScanStatus.Clean).Value;
        _virusScanner.ScanAsync(fileRef, Arg.Any<CancellationToken>())
            .Returns(scanResult);

        var handler = new UploadCompanyImageCommandHandler(_repository, _unitOfWork, _objectStorage, _virusScanner);
        var command = new UploadCompanyImageCommand(userId, new byte[] { 1 }, "gallery.jpg", "image/jpeg");

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        profile.Images.Should().HaveCount(1);
        profile.Images.First().File.Should().Be(fileRef);
    }

    [Fact]
    public async Task RemoveCompanyImage_ShouldSucceed_WhenImageExists()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var profile = CreateActiveProfile(userId);
        var fileRef = FileReference.Create("images/gallery.jpg", "gallery.jpg", "image/jpeg", 2048).Value;
        profile.AddCompanyImage(fileRef, VirusScanResult.Create(VirusScanStatus.Clean).Value);
        var imgId = profile.Images.First().Id;

        _repository.GetByUserIdAsync(userId, Arg.Any<CancellationToken>()).Returns(profile);

        var handler = new RemoveCompanyImageCommandHandler(_repository, _unitOfWork);
        var command = new RemoveCompanyImageCommand(userId, imgId);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        profile.Images.Should().BeEmpty();
    }

    [Fact]
    public async Task UploadEmployerDocument_ShouldSucceed_WhenValid()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var profile = CreateActiveProfile(userId);
        _repository.GetByUserIdAsync(userId, Arg.Any<CancellationToken>()).Returns(profile);

        var fileRef = FileReference.Create("docs/vat.pdf", "vat.pdf", "application/pdf", 5000).Value;
        _objectStorage.StoreAsync(Arg.Any<byte[]>(), "vat.pdf", "application/pdf", Arg.Any<CancellationToken>())
            .Returns(Result.Success(fileRef));

        _virusScanner.ScanAsync(fileRef, Arg.Any<CancellationToken>())
            .Returns(VirusScanResult.Create(VirusScanStatus.Clean).Value);

        var handler = new UploadEmployerDocumentCommandHandler(_repository, _unitOfWork, _objectStorage, _virusScanner);
        var command = new UploadEmployerDocumentCommand(userId, new byte[] { 1 }, "vat.pdf", "application/pdf", DocumentKind.VatCertificate);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        profile.Documents.Should().HaveCount(1);
        profile.Documents.First().File.Should().Be(fileRef);
        profile.Documents.First().Kind.Should().Be(DocumentKind.VatCertificate);
    }

    [Fact]
    public async Task RemoveEmployerDocument_ShouldSucceed_WhenDocumentExists()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var profile = CreateActiveProfile(userId);
        var fileRef = FileReference.Create("docs/vat.pdf", "vat.pdf", "application/pdf", 5000).Value;
        profile.AddSupplementaryDocument(fileRef, DocumentKind.VatCertificate, VirusScanResult.Create(VirusScanStatus.Clean).Value);
        var docId = profile.Documents.First().Id;

        _repository.GetByUserIdAsync(userId, Arg.Any<CancellationToken>()).Returns(profile);

        var handler = new RemoveEmployerDocumentCommandHandler(_repository, _unitOfWork);
        var command = new RemoveEmployerDocumentCommand(userId, docId);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        profile.Documents.Should().BeEmpty();
    }
}
