using System.CodeDom;
using System.Collections.Generic;

namespace XmlSchemaClassGenerator.Tests
{
    internal class FileWatcherOutputWriter : FileOutputWriter
    {
        private readonly List<string> _files;

        public FileWatcherOutputWriter(string directory)
            : base(directory)
        {
            _files = new List<string>();
        }

        public IEnumerable<string> Files => _files;

        protected override void WriteFile(string path, CodeCompileUnit cu)
        {
            base.WriteFile(path, cu);
            _files.Add(path);
        }
    }
}
