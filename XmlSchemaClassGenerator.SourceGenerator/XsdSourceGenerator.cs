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
            private readonly string schemaFileName;
            private readonly string prefix;

            public ICollection<(string Name, string Content)> Contents { get; private set; } = new List<(string, string)>();

            public MemoryOutputWriter(bool separateFiles, string schemaFileName, string prefix)
            {
                this.separateFiles = separateFiles;
                this.schemaFileName = schemaFileName;
                this.prefix = prefix;
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
                        Contents.Add(($"{this.prefix ?? string.Empty}{this.schemaFileName}.g.cs", writer.ToString()));
                    }
                }
            }

            private void WriteSeparateFiles(CodeNamespace cn)
            {
                var validName = GetSanitizedName(cn.Name);
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
                        Contents.Add(($"{this.prefix ?? string.Empty}{contentName}.g.cs", writer.ToString()));
                    }
                }
            }

            static readonly Regex InvalidCharacters = new Regex($"[{string.Join("", Path.GetInvalidFileNameChars())}]", RegexOptions.Compiled);

            private string GetSanitizedName(string name) => InvalidCharacters.Replace(name, "_");
        }

        public void Execute(GeneratorExecutionContext context)
        {
#if DEBUG
            if (!Debugger.IsAttached)
            {
                // Debugger.Launch();
            }
#endif
            var sources = GetConfigurations(context);

            foreach (var source in sources)
            {
                var schemaStr = source.AdditionalText.GetText().ToString();
                var stringReader = new StringReader(schemaStr);

                var schemaSet = new XmlSchemaSet();
                schemaSet.Add(null, XmlReader.Create(stringReader));

                var generator = new Generator();
                generator.NamespaceProvider.Add(new NamespaceKey(), source.Namespace);
                generator.SeparateClasses = source.GenerateSeparateFiles;
                MemoryOutputWriter memoryOutputWriter = new MemoryOutputWriter(
                    source.GenerateSeparateFiles,
                    Path.GetFileNameWithoutExtension(source.AdditionalText.Path),
                    source.Prefix);
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

        static IEnumerable<GenerationSource> GetConfigurations(GeneratorExecutionContext context)
        {
            foreach (AdditionalText file in context.AdditionalFiles)
            {
                AnalyzerConfigOptions fileOptions = context.AnalyzerConfigOptions.GetOptions(file);
                if (!fileOptions.TryGetValue("build_metadata.AdditionalFiles.xscgen_namespace", out var @namespace))
                {
                    @namespace = "Generated";
                }

                if (!fileOptions.TryGetValue("build_metadata.AdditionalFiles.xscgen_prefix", out var prefix))
                {
                    prefix = null;
                }

                bool generateSeparateFiles =
                    fileOptions.TryGetValue("build_metadata.AdditionalFiles.xscgen_separatefiles", out var generateSeparateFilesStr) &&
                    bool.TryParse(generateSeparateFilesStr, out var parsedGenerateSeparateFiles) &&
                    parsedGenerateSeparateFiles;

                if (Path.GetExtension(file.Path).Equals(".xsd", StringComparison.OrdinalIgnoreCase))
                {
                    yield return new GenerationSource(
                        file,
                        @namespace,
                        generateSeparateFiles,
                        prefix);
                }
            }
        }

        sealed class GenerationSource
        {
            public GenerationSource(
                AdditionalText additionalText,
                string @namespace,
                bool generateSeparateFiles,
                string prefix)
            {
                AdditionalText = additionalText;
                Namespace = @namespace;
                GenerateSeparateFiles = generateSeparateFiles;
                Prefix = prefix;
            }

            public AdditionalText AdditionalText { get; }
            public string Namespace { get; }
            public bool GenerateSeparateFiles { get; }
            public string Prefix { get; }
        }
    }
}
