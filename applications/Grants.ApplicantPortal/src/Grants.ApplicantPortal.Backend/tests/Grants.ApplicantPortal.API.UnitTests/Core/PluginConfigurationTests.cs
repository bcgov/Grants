using FluentAssertions;
using Grants.ApplicantPortal.API.Core.Plugins;

namespace Grants.ApplicantPortal.API.UnitTests.Core;

public class PluginConfigurationTests
{
    [Fact]
    public void PluginConfiguration_IsCaseInsensitive()
    {
        var config = new PluginConfiguration
        {
            ["UNITY"] = new PluginOptions { Enabled = true },
            ["DEMO"] = new PluginOptions { Enabled = false }
        };

        config["unity"].Enabled.Should().BeTrue();
        config["demo"].Enabled.Should().BeFalse();
    }

    [Fact]
    public void PluginOptions_DefaultsToEnabled()
    {
        var options = new PluginOptions();

        options.Enabled.Should().BeTrue();
        options.Configuration.Should().BeNull();
    }

    [Fact]
    public void PluginConfiguration_SectionName_IsCorrect()
    {
        PluginConfiguration.SectionName.Should().Be("Plugins");
    }
}
