using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using Xunit;
using System.Text;

namespace XmlSchemaClassGenerator.Tests
{
    [TestCaseOrderer("XmlSchemaClassGenerator.Tests.PriorityOrderer", "XmlSchemaClassGenerator.Tests")]
    public class XsdElsterDatenabholung5
    {
        static XsdElsterDatenabholung5()
        {
            // Ensure that the output directories are empty.
            Directory.Delete(GetOutputPath(string.Empty), true);
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        }

        [Fact, TestPriority(1)]
        public void CanGenerateClasses()
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
                GenerateDescriptionAttribute = true,
                SeparateClasses = true,
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
                                return key.XmlSchemaNamespace switch
                                {
                                    "http://www.elster.de/2002/XMLSchema" => "Elster.Datenabholung5",
                                    _ => throw new NotSupportedException(string.Format("Namespace {0} for schema {1}", key.XmlSchemaNamespace, key.Source)),
                                };
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
            var encodings = System.Text.Encoding.GetEncodings();
            System.Text.Encoding.GetEncoding("ISO-8859-15");
            gen.Generate(xsdFiles);
        }

        [Fact, TestPriority(2)]
        public void CanCompileClasses()
        {
            var inputPath = GetOutputPath("CanGenerateClasses");
            var fileNames = new DirectoryInfo(inputPath).GetFiles("*.cs", SearchOption.AllDirectories).Select(x => x.FullName).ToArray();
            var assembly = Compiler.CompileFiles("Elster.Test", fileNames);

            assembly.GetType("Elster.Datenabholung5.Elster", true);
            assembly.GetType("Elster.Basis.TransferHeaderCType", true);
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
