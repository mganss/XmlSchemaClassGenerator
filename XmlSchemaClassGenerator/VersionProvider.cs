using System.Diagnostics;
using System.Reflection;

namespace XmlSchemaClassGenerator
{
    [DebuggerDisplay("{Title} - {Version}")]
    public class VersionProvider
    {
        public VersionProvider(string title, string version)
        {
            Title = title;
            Version = version;
        }

        public string Title { get; }

        public string Version { get; }

        public static VersionProvider CreateFromAssembly()
        {
            var executingAssembly = Assembly.GetExecutingAssembly();
            var title = executingAssembly.GetCustomAttribute<AssemblyTitleAttribute>().Title;
            var version = executingAssembly.GetCustomAttribute<AssemblyFileVersionAttribute>().Version;

            return new VersionProvider(title, version);
        }
    }
}