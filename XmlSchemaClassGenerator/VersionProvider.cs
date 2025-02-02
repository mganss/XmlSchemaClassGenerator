using System.Diagnostics;
using System.Reflection;

namespace XmlSchemaClassGenerator;

[DebuggerDisplay("{Title} - {Version}")]
public class VersionProvider(string title, string version)
{
    public string Title { get; } = title;

    public string Version { get; } = version;

    public static VersionProvider CreateFromAssembly()
    {
        var executingAssembly = Assembly.GetExecutingAssembly();
        var title = executingAssembly.GetCustomAttribute<AssemblyTitleAttribute>().Title;
        var version = executingAssembly.GetCustomAttribute<AssemblyFileVersionAttribute>().Version;

        return new VersionProvider(title, version);
    }
}