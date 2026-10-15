using System.Xml.Linq;

namespace Copyloft.Desktop.Platform.MacOS;

// A per-user LaunchAgent (~/Library/LaunchAgents) that starts Copyloft at login. Applying every
// launch also repairs the path after the app moves; launchd reads it at the next login.
public sealed class LaunchAgentRegistration(
    ILogger<LaunchAgentRegistration> log, string? home = null, Func<string?>? executable = null)
    : IStartupRegistration
{
    private const string Label = "io.github.ai94iq.copyloft";
    private const string LegacyLabel = "io.github.ai94iq.clipshelf";   // the app's name up to 0.4.1, removed on every apply

    private string AgentPath => Path.Combine(
        home ?? Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
        "Library", "LaunchAgents", Label + ".plist");

    public void Apply(bool enabled)
    {
        try
        {
            var path = AgentPath;
            var legacy = Path.Combine(Path.GetDirectoryName(path)!, LegacyLabel + ".plist");
            if (File.Exists(legacy)) File.Delete(legacy);
            if (!enabled)
            {
                if (File.Exists(path)) File.Delete(path);
                return;
            }

            var exe = (executable ?? (() => Environment.ProcessPath))();
            if (exe is null)
            {
                log.LogWarning("The executable path is unknown; the startup setting was not applied");
                return;
            }

            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            using var writer = System.Xml.XmlWriter.Create(path, new System.Xml.XmlWriterSettings
            {
                Indent = true,
                Encoding = new System.Text.UTF8Encoding(false),   // a plist starts with its declaration, no BOM
            });
            new XDocument(
                new XDocumentType("plist", "-//Apple//DTD PLIST 1.0//EN", "http://www.apple.com/DTDs/PropertyList-1.0.dtd", null),
                new XElement("plist", new XAttribute("version", "1.0"),
                    new XElement("dict",
                        new XElement("key", "Label"), new XElement("string", Label),
                        new XElement("key", "ProgramArguments"), new XElement("array", new XElement("string", exe)),
                        new XElement("key", "RunAtLoad"), new XElement("true"),
                        new XElement("key", "ProcessType"), new XElement("string", "Interactive"))))
                .Save(writer);
        }
        catch (Exception ex)
        {
            log.LogError(ex, "Applying the startup setting failed");
        }
    }
}
