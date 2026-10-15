using System.Xml.Linq;
using Copyloft.Desktop.Platform.MacOS;
using Microsoft.Extensions.Logging.Abstractions;

namespace Copyloft.Desktop.Tests;

public sealed class LaunchAgentTests : IDisposable
{
    private readonly string _home = Directory.CreateTempSubdirectory("copyloft-home").FullName;
    private string AgentPath => Path.Combine(_home, "Library", "LaunchAgents", "io.github.ai94iq.copyloft.plist");

    public void Dispose() => Directory.Delete(_home, recursive: true);

    // No byte-order mark: a plist starts with its XML declaration.
    [Fact]
    public void The_agent_file_starts_with_its_xml_declaration()
    {
        Agent("/x").Apply(true);

        Assert.StartsWith("<?xml", System.Text.Encoding.UTF8.GetString(File.ReadAllBytes(AgentPath)));
    }

    private LaunchAgentRegistration Agent(string exe) =>
        new(NullLogger<LaunchAgentRegistration>.Instance, _home, () => exe);

    [Fact]
    public void Enabling_writes_a_launch_agent_that_runs_this_executable_at_login()
    {
        Agent("/Applications/Copyloft & Co.app/Contents/MacOS/Copyloft").Apply(true);

        var dict = XDocument.Load(AgentPath).Root!.Element("dict")!.Elements().ToList();
        string After(string key) => dict.SkipWhile(e => e.Value != key).Skip(1).First().Value;
        Assert.Equal("io.github.ai94iq.copyloft", After("Label"));
        Assert.Equal("/Applications/Copyloft & Co.app/Contents/MacOS/Copyloft", After("ProgramArguments"));
        Assert.Equal("true", dict.SkipWhile(e => e.Value != "RunAtLoad").Skip(1).First().Name.LocalName);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void Applying_removes_the_ClipShelf_agent(bool enabled)
    {
        var legacy = Path.Combine(_home, "Library", "LaunchAgents", "io.github.ai94iq.clipshelf.plist");
        Directory.CreateDirectory(Path.GetDirectoryName(legacy)!);
        File.WriteAllText(legacy, "<plist/>");

        Agent("/x").Apply(enabled);

        Assert.False(File.Exists(legacy));
        Assert.Equal(enabled, File.Exists(AgentPath));
    }

    // launchd only loads what plutil accepts.
    [Fact]
    public void The_agent_is_a_valid_property_list()
    {
        Assert.SkipUnless(OperatingSystem.IsMacOS(), "plutil");
        Agent("/Applications/Copyloft.app/Contents/MacOS/Copyloft").Apply(true);

        using var lint = System.Diagnostics.Process.Start("plutil", ["-lint", AgentPath])!;
        lint.WaitForExit();

        Assert.Equal(0, lint.ExitCode);
    }

    [Fact]
    public void Disabling_removes_it_and_tolerates_a_missing_one()
    {
        var agent = Agent("/Applications/Copyloft.app/Contents/MacOS/Copyloft");
        agent.Apply(true);

        agent.Apply(false);
        agent.Apply(false);

        Assert.False(File.Exists(AgentPath));
    }
}
