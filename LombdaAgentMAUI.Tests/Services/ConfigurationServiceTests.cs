using LombdaAgentMAUI.Core.Services;
using Moq;

namespace LombdaAgentMAUI.Tests.Services
{
    [TestFixture]
    public class ConfigurationServiceTests
    {
        private ConfigurationService _configurationService;
        private Mock<ISecureStorageService> _mockSecureStorage;
        private const string TestApiUrl = "http://localhost:5000/";

        [SetUp]
        public void Setup()
        {
            _mockSecureStorage = new Mock<ISecureStorageService>();
            _configurationService = new ConfigurationService(_mockSecureStorage.Object);
        }

        [Test]
        public void ApiBaseUrl_DefaultValue_ShouldBeLocalhost()
        {
            // Arrange & Act
            var defaultUrl = _configurationService.ApiBaseUrl;

            // Assert
            Assert.That(defaultUrl, Is.EqualTo("https://localhost:5001/"));
        }

        [Test]
        public void ApiBaseUrl_SetValue_ShouldUpdateProperty()
        {
            // Arrange
            var newUrl = TestApiUrl;

            // Act
            _configurationService.ApiBaseUrl = newUrl;

            // Assert
            Assert.That(_configurationService.ApiBaseUrl, Is.EqualTo(newUrl));
        }

        [Test]
        public async Task SaveSettingsAsync_ShouldCallSecureStorage()
        {
            // Arrange
            _configurationService.ApiBaseUrl = TestApiUrl;

            // Act
            await _configurationService.SaveSettingsAsync();

            // Assert
            _mockSecureStorage.Verify(x => x.SetAsync("api_base_url", TestApiUrl), Times.Once);
        }

        [Test]
        public async Task LoadSettingsAsync_WithNoSavedData_ShouldKeepDefaultValue()
        {
            // Arrange
            var originalUrl = _configurationService.ApiBaseUrl;
            _mockSecureStorage.Setup(x => x.GetAsync("api_base_url")).ReturnsAsync((string?)null);

            // Act
            await _configurationService.LoadSettingsAsync();

            // Assert
            Assert.That(_configurationService.ApiBaseUrl, Is.EqualTo(originalUrl));
        }

        [Test]
        public async Task LoadSettingsAsync_WithSavedData_ShouldLoadSavedValue()
        {
            // Arrange
            _mockSecureStorage.Setup(x => x.GetAsync("api_base_url")).ReturnsAsync(TestApiUrl);

            // Act
            await _configurationService.LoadSettingsAsync();

            // Assert
            Assert.That(_configurationService.ApiBaseUrl, Is.EqualTo(TestApiUrl));
        }

        [Test]
        public async Task LoadSettingsAsync_WithException_ShouldUseDefaultValue()
        {
            // Arrange
            _mockSecureStorage.Setup(x => x.GetAsync("api_base_url")).ThrowsAsync(new Exception("Storage error"));
            _configurationService.ApiBaseUrl = "some-invalid-url";

            // Act
            await _configurationService.LoadSettingsAsync();

            // Assert - should fall back to default
            Assert.That(_configurationService.ApiBaseUrl, Is.EqualTo("https://localhost:5001/"));
        }

        [Test]
        public void ApiBaseUrl_SetEmptyString_ShouldAcceptValue()
        {
            // Arrange & Act
            _configurationService.ApiBaseUrl = string.Empty;

            // Assert
            Assert.That(_configurationService.ApiBaseUrl, Is.EqualTo(string.Empty));
        }

        [Test]
        public void ApiBaseUrl_SetNull_ShouldAcceptValue()
        {
            // Arrange & Act
            _configurationService.ApiBaseUrl = null!;

            // Assert
            Assert.That(_configurationService.ApiBaseUrl, Is.Null);
        }

        [Test]
        [TestCase("https://api.example.com/")]
        [TestCase("http://localhost:3000/")]
        [TestCase("https://192.168.1.100:5001/")]
        public async Task SaveAndLoadSettings_WithVariousUrls_ShouldPersistCorrectly(string testUrl)
        {
            // Arrange
            _mockSecureStorage.Setup(x => x.GetAsync("api_base_url")).ReturnsAsync(testUrl);
            _configurationService.ApiBaseUrl = testUrl;

            // Act
            await _configurationService.SaveSettingsAsync();
            await _configurationService.LoadSettingsAsync();

            // Assert
            Assert.That(_configurationService.ApiBaseUrl, Is.EqualTo(testUrl));
            _mockSecureStorage.Verify(x => x.SetAsync("api_base_url", testUrl), Times.Once);
        }

        [Test]
        public async Task LoadSettingsAsync_WithEmptyString_ShouldKeepDefaultValue()
        {
            // Arrange
            _mockSecureStorage.Setup(x => x.GetAsync("api_base_url")).ReturnsAsync(string.Empty);
            var originalUrl = _configurationService.ApiBaseUrl;

            // Act
            await _configurationService.LoadSettingsAsync();

            // Assert
            Assert.That(_configurationService.ApiBaseUrl, Is.EqualTo(originalUrl));
        }

        [Test]
        public async Task LoadSettingsAsync_WithWhitespace_ShouldKeepDefaultValue()
        {
            // Arrange
            _mockSecureStorage.Setup(x => x.GetAsync("api_base_url")).ReturnsAsync("   ");
            var originalUrl = _configurationService.ApiBaseUrl;

            // Act
            await _configurationService.LoadSettingsAsync();

            // Assert
            Assert.That(_configurationService.ApiBaseUrl, Is.EqualTo(originalUrl));
        }
    }
}