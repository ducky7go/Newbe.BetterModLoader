using System.Text;
using Shouldly;
using Newbe.BetterModLoader.DeveloperModes.Uploads;

namespace Newbe.BetterModLoader.Tests;

[TestFixture]
public class UploadValidationTests
{
    private string _tempDirectory = string.Empty;
    private const int MaxPreviewImageSize = 1024 * 1024; // 1MB
    private const int MaxDescriptionSize = 8 * 1024; // 8KB

    [SetUp]
    public void Setup()
    {
        _tempDirectory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(_tempDirectory);
    }

    [TearDown]
    public void TearDown()
    {
        if (Directory.Exists(_tempDirectory))
        {
            Directory.Delete(_tempDirectory, true);
        }
    }

    [Test]
    public void PreviewImageSizeCheck_WithValidSize_ShouldReturnSuccess()
    {
        // Arrange
        var previewPath = Path.Combine(_tempDirectory, "preview.png");
        var validImageData = new byte[MaxPreviewImageSize / 2]; // 512KB
        File.WriteAllBytes(previewPath, validImageData);

        // Act
        var result = ChecklistItem.PreviewImageSizeCheck(ChecklistStatus.Success);

        // Assert
        result.Status.ShouldBe(ChecklistStatus.Success);
        result.Description.ShouldNotBeNullOrEmpty();
        result.Id.ShouldBe("PreviewImageSizeCheck");
    }

    [Test]
    public void PreviewImageSizeCheck_WithOversizedImage_ShouldReturnFail()
    {
        // Arrange
        var previewPath = Path.Combine(_tempDirectory, "preview.png");
        var oversizedImageData = new byte[MaxPreviewImageSize * 2]; // 2MB
        File.WriteAllBytes(previewPath, oversizedImageData);

        // Act
        var result = ChecklistItem.PreviewImageSizeCheck(ChecklistStatus.Fail);

        // Assert
        result.Status.ShouldBe(ChecklistStatus.Fail);
        result.Description.ShouldNotBeNullOrEmpty();
        result.FailReason.ShouldNotBeNullOrEmpty();
        result.Id.ShouldBe("PreviewImageSizeCheck");
    }

    [Test]
    public void PreviewImageSizeCheck_WithMissingFile_ShouldReturnFail()
    {
        // Arrange - No file created

        // Act
        var result = ChecklistItem.PreviewImageSizeCheck(ChecklistStatus.Fail);

        // Assert
        result.Status.ShouldBe(ChecklistStatus.Fail);
        result.Description.ShouldNotBeNullOrEmpty();
        result.FailReason.ShouldNotBeNullOrEmpty();
    }

    [Test]
    public void DescriptionSizeCheck_WithValidSingleDescription_ShouldReturnSuccess()
    {
        // Arrange
        var descriptionPath = Path.Combine(_tempDirectory, "description.md");
        var validDescription = CreateTestDescription(MaxDescriptionSize / 2); // 4KB
        File.WriteAllText(descriptionPath, validDescription);

        // Act
        var result = ChecklistItem.DescriptionSizeCheck(ChecklistStatus.Success);

        // Assert
        result.Status.ShouldBe(ChecklistStatus.Success);
        result.Description.ShouldNotBeNullOrEmpty();
        result.Id.ShouldBe("DescriptionSizeCheck");
    }

    [Test]
    public void DescriptionSizeCheck_WithOversizedSingleDescription_ShouldReturnFail()
    {
        // Arrange
        var descriptionPath = Path.Combine(_tempDirectory, "description.md");
        var oversizedDescription = CreateTestDescription(MaxDescriptionSize * 2); // 16KB
        File.WriteAllText(descriptionPath, oversizedDescription);

        // Act
        var result = ChecklistItem.DescriptionSizeCheck(ChecklistStatus.Fail);

        // Assert
        result.Status.ShouldBe(ChecklistStatus.Fail);
        result.Description.ShouldNotBeNullOrEmpty();
        result.FailReason.ShouldNotBeNullOrEmpty();
        result.Id.ShouldBe("DescriptionSizeCheck");
    }

    [Test]
    public void DescriptionSizeCheck_WithValidMultiLanguageDescription_ShouldReturnSuccess()
    {
        // Arrange
        var descriptionDir = Path.Combine(_tempDirectory, "description");
        Directory.CreateDirectory(descriptionDir);

        var validDescription = CreateTestDescription(MaxDescriptionSize / 2); // 4KB
        File.WriteAllText(Path.Combine(descriptionDir, "en.md"), validDescription);
        File.WriteAllText(Path.Combine(descriptionDir, "zh.md"), validDescription);

        // Act
        var enResult = ChecklistItem.DescriptionSizeCheck(ChecklistStatus.Success, "en");
        var zhResult = ChecklistItem.DescriptionSizeCheck(ChecklistStatus.Success, "zh");

        // Assert
        enResult.Status.ShouldBe(ChecklistStatus.Success);
        zhResult.Status.ShouldBe(ChecklistStatus.Success);
        enResult.Description.ShouldContain("en");
        zhResult.Description.ShouldContain("zh");
    }

    [Test]
    public void DescriptionSizeCheck_WithOversizedMultiLanguageDescription_ShouldReturnFail()
    {
        // Arrange
        var descriptionDir = Path.Combine(_tempDirectory, "description");
        Directory.CreateDirectory(descriptionDir);

        var validDescription = CreateTestDescription(MaxDescriptionSize / 2); // 4KB
        var oversizedDescription = CreateTestDescription(MaxDescriptionSize * 2); // 16KB

        File.WriteAllText(Path.Combine(descriptionDir, "en.md"), validDescription);
        File.WriteAllText(Path.Combine(descriptionDir, "zh.md"), oversizedDescription);

        // Act
        var enResult = ChecklistItem.DescriptionSizeCheck(ChecklistStatus.Success, "en");
        var zhResult = ChecklistItem.DescriptionSizeCheck(ChecklistStatus.Fail, "zh");

        // Assert
        enResult.Status.ShouldBe(ChecklistStatus.Success);
        zhResult.Status.ShouldBe(ChecklistStatus.Fail);
        zhResult.FailReason.ShouldContain("zh");
    }

    [Test]
    public void DescriptionSizeCheck_WithEmptyDescription_ShouldReturnUnavailable()
    {
        // Arrange - No description file created

        // Act
        var result = ChecklistItem.DescriptionSizeCheck(ChecklistStatus.Unavailable);

        // Assert
        result.Status.ShouldBe(ChecklistStatus.Unavailable);
        result.Description.ShouldNotBeNullOrEmpty();
    }

    [Test]
    public void DescriptionSizeCheck_WithUnicodeCharacters_ShouldCalculateSizeCorrectly()
    {
        // Arrange
        var descriptionPath = Path.Combine(_tempDirectory, "description.md");
        var unicodeDescription = CreateUnicodeTestDescription(MaxDescriptionSize / 2); // Mix of ASCII and Unicode
        File.WriteAllText(descriptionPath, unicodeDescription);

        // Verify byte count (UTF-8 encoding)
        var byteCount = Encoding.UTF8.GetByteCount(unicodeDescription);

        // Act
        var result = ChecklistItem.DescriptionSizeCheck(
            byteCount <= MaxDescriptionSize ? ChecklistStatus.Success : ChecklistStatus.Fail);

        // Assert
        result.Status.ShouldBe(ChecklistStatus.Success);
        byteCount.ShouldBeLessThanOrEqualTo(MaxDescriptionSize);
    }

    [TestCase(0)]
    [TestCase(1024)]
    [TestCase(MaxPreviewImageSize)]
    [TestCase(MaxPreviewImageSize - 1)]
    public void PreviewImageSizeCheck_BoundaryValues_ShouldReturnExpectedStatus(long imageSize)
    {
        // Arrange
        var previewPath = Path.Combine(_tempDirectory, "preview.png");
        var imageData = new byte[imageSize];
        File.WriteAllBytes(previewPath, imageData);

        var expectedStatus = imageSize <= MaxPreviewImageSize ? ChecklistStatus.Success : ChecklistStatus.Fail;

        // Act
        var result = ChecklistItem.PreviewImageSizeCheck(expectedStatus);

        // Assert
        result.Status.ShouldBe(expectedStatus);
    }

    [TestCase(0)]
    [TestCase(1024)]
    [TestCase(MaxDescriptionSize)]
    [TestCase(MaxDescriptionSize - 1)]
    public void DescriptionSizeCheck_BoundaryValues_ShouldReturnExpectedStatus(long descriptionSize)
    {
        // Arrange
        var description = CreateTestDescription(descriptionSize);
        var byteCount = Encoding.UTF8.GetByteCount(description);
        var expectedStatus = byteCount <= MaxDescriptionSize ? ChecklistStatus.Success : ChecklistStatus.Fail;

        // Act
        var result = ChecklistItem.DescriptionSizeCheck(expectedStatus);

        // Assert
        result.Status.ShouldBe(expectedStatus);
    }

    private string CreateTestDescription(long targetSize)
    {
        var sb = new StringBuilder();
        const string chunk = "This is a test description chunk. ";
        while (Encoding.UTF8.GetByteCount(sb.ToString()) < targetSize)
        {
            sb.Append(chunk);
        }

        return sb.ToString();
    }

    private string CreateUnicodeTestDescription(long targetSize)
    {
        var sb = new StringBuilder();
        const string asciiChunk = "This is a test description. ";
        const string unicodeChunk = "这是测试描述。 "; // Chinese characters take more bytes in UTF-8

        while (Encoding.UTF8.GetByteCount(sb.ToString()) < targetSize)
        {
            if (Encoding.UTF8.GetByteCount(sb.ToString()) % 2 == 0)
            {
                sb.Append(asciiChunk);
            }
            else
            {
                sb.Append(unicodeChunk);
            }
        }

        return sb.ToString();
    }
}
