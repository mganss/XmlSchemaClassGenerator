using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using System;
using System.CodeDom;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Schema;

namespace XmlSchemaClassGenerator
{
    [Generator]
    public class XsdSourceGenerator : ISourceGenerator
    {
        internal class MemoryOutputWriter : OutputWriter
        {
            private readonly bool separateFiles;

            public ICollection<(string Name, string Content)> Contents { get; private set; } = new List<(string, string)>();

            public MemoryOutputWriter(bool separateFiles)
            {
                this.separateFiles = separateFiles;
            }

            public override void Write(CodeNamespace cn)
            {
                var cu = new CodeCompileUnit();
                cu.Namespaces.Add(cn);

                if (separateFiles)
                {
                    WriteSeparateFiles(cn);
                }
                else
                {
                    using (var writer = new StringWriter())
                    {
                        Write(writer, cu);
                        Contents.Add(("Pocos", writer.ToString()));
                    }
                }
            }

            private void WriteSeparateFiles(CodeNamespace cn)
            {
                var validName = ValidateName(cn.Name);
                var ccu = new CodeCompileUnit();
                var cns = new CodeNamespace(validName);

                cns.Imports.AddRange(cn.Imports.Cast<CodeNamespaceImport>().ToArray());
                cns.Comments.AddRange(cn.Comments);
                ccu.Namespaces.Add(cns);

                foreach (CodeTypeDeclaration ctd in cn.Types)
                {
                    var contentName = ctd.Name;
                    cns.Types.Clear();
                    cns.Types.Add(ctd);
                    using (var writer = new StringWriter())
                    {
                        Write(writer, ccu);
                        Contents.Add((contentName, writer.ToString()));
                    }
                }
            }

            static readonly Regex InvalidCharacters = new Regex($"[{string.Join("", Path.GetInvalidFileNameChars())}]", RegexOptions.Compiled);

            private string ValidateName(string name) => InvalidCharacters.Replace(name, "_");
        }

        public void Execute(GeneratorExecutionContext context)
        {
#if DEBUG
            if (!Debugger.IsAttached)
            {
                // Debugger.Launch();
            }
#endif
            var configurations = GetConfigurations(context);
            bool generateSeparateFiles =
                context.AnalyzerConfigOptions.GlobalOptions.TryGetValue("build_property.xscgen_separatefiles", out var generateSeparateFilesStr) &&
                bool.TryParse(generateSeparateFilesStr, out var parsedGenerateSeparateFiles) &&
                parsedGenerateSeparateFiles;

            foreach (var (schemaFile, @namespace) in configurations)
            {
                var schemaStr = schemaFile.GetText().ToString();
                var stringReader = new StringReader(schemaStr);

                var schemaSet = new XmlSchemaSet();
                schemaSet.Add(null, XmlReader.Create(stringReader));

                var generator = new Generator();
                generator.NamespaceProvider.Add(new NamespaceKey(), @namespace);
                generator.SeparateClasses = generateSeparateFiles;
                MemoryOutputWriter memoryOutputWriter = new MemoryOutputWriter(generateSeparateFiles);
                generator.OutputWriter = memoryOutputWriter;
                generator.Generate(schemaSet);

                foreach (var (name, content) in memoryOutputWriter.Contents)
                {
                    context.AddSource(name, content);
                }
            }
        }

        public void Initialize(GeneratorInitializationContext context)
        {
            // do nothing
        }

        static IEnumerable<(AdditionalText SchemaFile, string Namespace)> GetConfigurations(GeneratorExecutionContext context)
        {
            if (!context.AnalyzerConfigOptions.GlobalOptions.TryGetValue("build_property.xscgen_namespace", out var @namespace))
            {
                @namespace = "Generated";
            }

            foreach (AdditionalText file in context.AdditionalFiles)
            {
                if (Path.GetExtension(file.Path).Equals(".xsd", StringComparison.OrdinalIgnoreCase))
                {
                    yield return (file, @namespace);
                }
            }
        }
    }
}
