using System.CodeDom;
using System.Collections.Generic;
using System.IO;

namespace XmlSchemaClassGenerator.Tests
{
    internal class MemoryOutputWriter : OutputWriter
    {
        private readonly List<string> _contents = new List<string>();

        public IEnumerable<string> Content => _contents;

        public override void Write(CodeNamespace cn)
        {
            var cu = new CodeCompileUnit();
            cu.Namespaces.Add(cn);

            using var writer = new StringWriter();
            Write(writer, cu);
            _contents.Add(writer.ToString());
        }
    }
}
