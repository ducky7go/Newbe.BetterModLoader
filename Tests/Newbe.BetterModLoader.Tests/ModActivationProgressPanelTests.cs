using Ducky.Sdk.Options;
using Shouldly;

namespace Newbe.BetterModLoader.Tests;

[TestFixture]
public class ModActivationProgressPanelTests
{
    private const string ForceRestartKey = "bml_developer_force_restart_setting";

    [SetUp]
    public void Setup()
    {
        // Reset to default state before each test
        ModOptions.Save(ForceRestartKey, true);
    }

    [TearDown]
    public void TearDown()
    {
        // Clean up after each test
        ModOptions.Save(ForceRestartKey, true); // Reset to default
    }

    [Test]
    public void Show_WithOrderChangeAndToggleEnabled_ShouldRequireShutdown()
    {
        // Arrange
        ModOptions.Save(ForceRestartKey, true); // Toggle enabled
        const bool requiresShutdownDueToOrderChange = true;

        // Act - In a real scenario, this would create and show the panel
        // For testing, we verify the logic that would be used
        var shouldForceRestart = Configs.ShouldForceRestartOnOrderChanges();
        var finalRequiresShutdown = requiresShutdownDueToOrderChange && shouldForceRestart;

        // Assert
        shouldForceRestart.ShouldBeTrue();
        finalRequiresShutdown.ShouldBeTrue();
    }

    [Test]
    public void Show_WithOrderChangeAndToggleDisabled_ShouldNotRequireShutdown()
    {
        // Arrange
        ModOptions.Save(ForceRestartKey, false); // Toggle disabled
        const bool requiresShutdownDueToOrderChange = true;

        // Act
        var shouldForceRestart = Configs.ShouldForceRestartOnOrderChanges();
        var finalRequiresShutdown = requiresShutdownDueToOrderChange && shouldForceRestart;

        // Assert
        shouldForceRestart.ShouldBeFalse();
        finalRequiresShutdown.ShouldBeFalse();
    }

    [Test]
    public void Show_WithoutOrderChangeAndToggleEnabled_ShouldNotRequireShutdown()
    {
        // Arrange
        ModOptions.Save(ForceRestartKey, true); // Toggle enabled
        const bool requiresShutdownDueToOrderChange = false;

        // Act
        var shouldForceRestart = Configs.ShouldForceRestartOnOrderChanges();
        var finalRequiresShutdown = requiresShutdownDueToOrderChange && shouldForceRestart;

        // Assert
        shouldForceRestart.ShouldBeTrue();
        finalRequiresShutdown.ShouldBeFalse(); // No order change, so no shutdown needed
    }

    [Test]
    public void Show_WithoutOrderChangeAndToggleDisabled_ShouldNotRequireShutdown()
    {
        // Arrange
        ModOptions.Save(ForceRestartKey, false); // Toggle disabled
        const bool requiresShutdownDueToOrderChange = false;

        // Act
        var shouldForceRestart = Configs.ShouldForceRestartOnOrderChanges();
        var finalRequiresShutdown = requiresShutdownDueToOrderChange && shouldForceRestart;

        // Assert
        shouldForceRestart.ShouldBeFalse();
        finalRequiresShutdown.ShouldBeFalse();
    }

    [Test]
    public void ForceRestartToggleLogic_CombinationMatrix_ShouldMatchExpected()
    {
        // Test all combinations of order change and toggle settings
        var testCases = new[]
        {
            new { OrderChanged = true, ToggleEnabled = true, ExpectedRequiresShutdown = true },
            new { OrderChanged = true, ToggleEnabled = false, ExpectedRequiresShutdown = false },
            new { OrderChanged = false, ToggleEnabled = true, ExpectedRequiresShutdown = false },
            new { OrderChanged = false, ToggleEnabled = false, ExpectedRequiresShutdown = false }
        };

        foreach (var testCase in testCases)
        {
            // Arrange
            ModOptions.Save(ForceRestartKey, testCase.ToggleEnabled);

            // Act
            var shouldForceRestart = Configs.ShouldForceRestartOnOrderChanges();
            var finalRequiresShutdown = testCase.OrderChanged && shouldForceRestart;

            // Assert
            finalRequiresShutdown.ShouldBe(testCase.ExpectedRequiresShutdown,
                $"Failed for OrderChanged={testCase.OrderChanged}, ToggleEnabled={testCase.ToggleEnabled}");
        }
    }
}
