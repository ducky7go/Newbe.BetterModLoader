using Shouldly;
using Newbe.BetterModLoader.DeveloperInfos;

namespace Newbe.BetterModLoader.Tests;

[TestFixture]
public class DeveloperInfoPanelTests
{
    [SetUp]
    public void Setup()
    {
        // Clear any existing mod options before each test
        TestHelper.ClearModOptions();
    }

    [Test]
    public void Configs_ForceRestartOnOrderChanges_DefaultValue_ShouldBeTrue()
    {
        // Arrange - No saved value (default case)

        // Act
        var result = Configs.ForceRestartOnOrderChanges;

        // Assert - Default should be true to maintain existing behavior
        result.ShouldBeTrue();
    }

    [Test]
    public void Configs_ForceRestartOnOrderChanges_SetAndGet_ShouldWork()
    {
        // Arrange & Act - Set to false
        Configs.ForceRestartOnOrderChanges = false;

        // Assert
        Configs.ForceRestartOnOrderChanges.ShouldBeFalse();

        // Act - Set to true
        Configs.ForceRestartOnOrderChanges = true;

        // Assert
        Configs.ForceRestartOnOrderChanges.ShouldBeTrue();
    }

    [Test]
    public void Configs_ShouldForceRestartOnOrderChanges_ShouldMatchProperty()
    {
        // Arrange
        Configs.ForceRestartOnOrderChanges = false;

        // Act & Assert
        Configs.ShouldForceRestartOnOrderChanges().ShouldBeFalse();

        // Arrange
        Configs.ForceRestartOnOrderChanges = true;

        // Act & Assert
        Configs.ShouldForceRestartOnOrderChanges().ShouldBeTrue();
    }

    [Test]
    public void DeveloperInfoPanel_ShouldForceRestartOnOrderChanges_ShouldUseConfigs()
    {
        // Arrange
        Configs.ForceRestartOnOrderChanges = false;

        // Act
        var result = Configs.ShouldForceRestartOnOrderChanges();

        // Assert - Should use Configs class
        result.ShouldBeFalse();
    }

    [Test]
    public void DeveloperInfoVm_ReactiveProperty_ShouldPersistChanges()
    {
        // Arrange
        var data = TestHelper.CreateTestDeveloperInfoData();
        using var vm = new DeveloperInfoVm(data);

        // Act - Change the value through ViewModel
        vm.SetForceRestartOnOrderChanges(false);

        // Allow some time for the reactive subscription to save the value
        System.Threading.Thread.Sleep(100);

        // Assert - Verify the change was persisted through Configs
        Configs.ForceRestartOnOrderChanges.ShouldBeFalse();
    }

    [Test]
    public void Configs_ResetToDefaults_ShouldResetToTrue()
    {
        // Arrange
        Configs.ForceRestartOnOrderChanges = false;

        // Act
        Configs.ResetToDefaults();

        // Assert
        Configs.ForceRestartOnOrderChanges.ShouldBeTrue();
    }

    [Test]
    public void Configs_GetDebugInfo_ShouldReturnFormattedString()
    {
        // Arrange
        Configs.ForceRestartOnOrderChanges = false;

        // Act
        var debugInfo = Configs.GetDebugInfo();

        // Assert
        debugInfo.ShouldContain("ForceRestartOnOrderChanges: False");
    }
}

/// <summary>
/// Helper class for test utilities
/// </summary>
internal static class TestHelper
{
    /// <summary>
    /// Clears mod options for clean testing
    /// </summary>
    public static void ClearModOptions()
    {
        try
        {
            // Reset to default using Configs class
            Configs.ResetToDefaults();
        }
        catch
        {
            // Ignore cleanup errors
        }
    }

    /// <summary>
    /// Creates test DeveloperInfoData without Unity dependencies
    /// </summary>
    public static DeveloperInfoData CreateTestDeveloperInfoData()
    {
        return new DeveloperInfoData
        {
            LogDirectorySizeInMB = 0,
            OriginalGameLogsFolder = "/test/logs",
            LocalModsFolder = "/test/mods",
            SteamModsFolder = "/test/steam/mods",
            GamePersistentDataFolder = "/test/persistent",
            BetterModeLoaderConfigFolder = "/test/config",
            BetterModeLoaderConfigPath = "/test/config/config.json",
            ModOptionsForAllFolder = "/test/mod-options/all",
            ModOptionsForEachFolder = "/test/mod-options/each"
        };
    }
}
