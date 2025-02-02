using System.CodeDom;
using System.Collections.Generic;

namespace XmlSchemaClassGenerator.Tests;

internal class FileWatcherOutputWriter(string directory) : FileOutputWriter(directory)
{
    private readonly List<string> _files = [];

    public IEnumerable<string> Files => _files;

    protected override void WriteFile(string path, CodeCompileUnit cu)
    {
        base.WriteFile(path, cu);
        _files.Add(path);
    }
}
