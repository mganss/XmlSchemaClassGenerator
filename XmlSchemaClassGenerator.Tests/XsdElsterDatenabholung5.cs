using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using Xunit;

namespace XmlSchemaClassGenerator.Tests
{
    public class XsdElsterDatenabholung5
    {
        [Fact]
        public void CanGenerateClasses()
        {
            Assert.DoesNotThrow(() =>
            {
                var gen = new Generator
                {
                    UseXElementForAny = true,
                    CollectionType = typeof(IList<>),
                    CollectionImplementationType = typeof(List<>),
                    GenerateDesignerCategoryAttribute = false,
                    GenerateNullables = true,
                    GenerateSerializableAttribute = false,
                    OutputFolder = OutputPath,
                    NamingScheme = NamingScheme.Direct,
                    DataAnnotationMode = DataAnnotationMode.None,
                    NamespaceProvider = new NamespaceProvider()
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

        string InputPath
        {
            get { return Path.Combine(Directory.GetCurrentDirectory(), "xsd", "elster-xml-datenabholung5"); }
        }

        string OutputPath
        {
            get
            {
                var result = Path.Combine(Directory.GetCurrentDirectory(), "output", "elster-xml-datenabholung5");
                Directory.CreateDirectory(result);
                return result;
            }
        }
    }
}
