using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using Xunit;

namespace XmlSchemaClassGenerator.Tests
{
    [PrioritizedFixture]
    public class XsdElsterDatenabholung5
    {
        static XsdElsterDatenabholung5()
        {
            // Ensure that the output directories are empty.
            Directory.Delete(GetOutputPath(string.Empty), true);
        }

        [Fact, TestPriority(1)]
        public void CanGenerateClasses()
        {
            Assert.DoesNotThrow(() =>
            {
                var outputPath = GetOutputPath("CanGenerateClasses");
                var gen = new Generator
                {
                    EnableDataBinding = true,
                    IntegerDataType = typeof(int),
                    UseXElementForAny = true,
                    CollectionType = typeof(IList<>),
                    CollectionImplementationType = typeof(List<>),
                    GenerateDesignerCategoryAttribute = false,
                    GenerateNullables = true,
                    GenerateSerializableAttribute = false,
                    OutputFolder = outputPath,
                    NamingScheme = NamingScheme.Direct,
                    DataAnnotationMode = DataAnnotationMode.None,
                    EmitOrder = true,
                    NamespaceProvider = new NamespaceProvider
                    {
                        GenerateNamespace = key =>
                        {
                            switch (Path.GetFileName(key.Source.LocalPath))
                            {
                                case "th000008_extern.xsd":
                                case "ndh000010_extern.xsd":
                                case "headerbasis000002.xsd":
                                    return "Elster.Basis";
                                case "datenabholung_5.xsd":
                                case "elster0810_datenabholung_5.xsd":
                                    switch (key.XmlSchemaNamespace)
                                    {
                                        case "http://www.elster.de/2002/XMLSchema":
                                            return "Elster.Datenabholung5";
                                        default:
                                            throw new NotSupportedException(string.Format("Namespace {0} for schema {1}", key.XmlSchemaNamespace, key.Source));
                                    }
                                default:
                                    throw new NotSupportedException(string.Format("Namespace {0} for schema {1}", key.XmlSchemaNamespace, key.Source));
                            }
                        }
                    }
                };
                var xsdFiles = new[]
                {
                    "headerbasis000002.xsd",
                    "ndh000010_extern.xsd",
                    "th000008_extern.xsd",
                    "datenabholung_5.xsd",
                    "elster0810_datenabholung_5.xsd",
                }.Select(x => Path.Combine(InputPath, x)).ToList();
                gen.Generate(xsdFiles);
            });
        }

        [Fact, TestPriority(2)]
        public void CanCompileClasses()
        {
            var inputPath = GetOutputPath("CanGenerateClasses");
            var outputPath = GetOutputPath("CanCompileClasses");
            var provider = CodeDomProvider.CreateProvider("CSharp");
            var assemblies = new[]
            {
                "System.dll",
                "System.Core.dll",
                "System.Xml.dll",
                "System.Xml.Linq.dll",
                "System.Xml.Serialization.dll",
                "System.ServiceModel.dll",
            };
            var outputName = Path.Combine(outputPath, "Elster.Test.dll");
            var fileNames = new DirectoryInfo(inputPath).GetFiles("*.cs").Select(x => x.FullName).ToArray();
            var results = provider.CompileAssemblyFromFile(new CompilerParameters(assemblies, outputName), fileNames);
            Assert.Equal(0, results.Errors.Count);
            Assert.DoesNotThrow(() => results.CompiledAssembly.GetType("Elster.Datenabholung5.Elster", true));
            Assert.DoesNotThrow(() => results.CompiledAssembly.GetType("Elster.Basis.TransferHeaderCType", true));
        }

        private static string InputPath
        {
            get { return Path.Combine(Directory.GetCurrentDirectory(), "xsd", "elster-xml-datenabholung5"); }
        }

        private static string GetOutputPath(string testCaseId)
        {
            var result = Path.Combine(Directory.GetCurrentDirectory(), "output", "elster-xml-datenabholung5", testCaseId);
            Directory.CreateDirectory(result);
            return result;
        }
    }
}
