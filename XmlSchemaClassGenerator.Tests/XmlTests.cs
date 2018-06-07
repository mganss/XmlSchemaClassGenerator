using Microsoft.Xml.XMLGen;
using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;
using Xunit;

namespace XmlSchemaClassGenerator.Tests
{
    [TestCaseOrderer("XmlSchemaClassGenerator.Tests.PriorityOrderer", "XmlSchemaClassGenerator.Tests")]
    public class XmlTests
    {
        private static Dictionary<string, Assembly> Assemblies = new Dictionary<string, Assembly>();

        private Assembly Compile(string name, string pattern, Generator generatorPrototype = null)
        {
            var files = Glob.Glob.ExpandNames(pattern);

            return CompileFiles(name, files, generatorPrototype);
        }

        private IEnumerable<string> ConvertXml(string name, string xsd, Generator generatorPrototype = null)
        {
            var writer = new MemoryOutputWriter();

            var gen = new Generator
            {
                OutputWriter = writer,
                NamespaceProvider = generatorPrototype.NamespaceProvider,
                GenerateNullables = generatorPrototype.GenerateNullables,
                IntegerDataType = generatorPrototype.IntegerDataType,
                DataAnnotationMode = generatorPrototype.DataAnnotationMode,
                GenerateDesignerCategoryAttribute = generatorPrototype.GenerateDesignerCategoryAttribute,
                EntityFramework = generatorPrototype.EntityFramework,
                GenerateInterfaces = generatorPrototype.GenerateInterfaces,
                MemberVisitor = generatorPrototype.MemberVisitor,
                TimeDataType = generatorPrototype.TimeDataType
            };

            var set = new XmlSchemaSet();

            using (var stringReader = new StringReader(xsd))
            {
                var schema = XmlSchema.Read(stringReader, (s, e) =>
                {
                    throw new InvalidOperationException();
                });

                set.Add(schema);
            }

            gen.Generate(set);

            return writer.Content;
        }

        private Assembly CompileFiles(string name, IEnumerable<string> files, Generator generatorPrototype = null)
        {
            if (Assemblies.ContainsKey(name)) { return Assemblies[name]; }

            generatorPrototype = generatorPrototype ?? new Generator
            {
                GenerateNullables = true,
                IntegerDataType = typeof(int),
                DataAnnotationMode = DataAnnotationMode.All,
                GenerateDesignerCategoryAttribute = false,
                EntityFramework = false,
                GenerateInterfaces = true,
                NamespacePrefix = name
            };

            var output = new FileWatcherOutputWriter(Path.Combine("output", name));

            var gen = new Generator
            {
                OutputWriter = output,
                NamespaceProvider = generatorPrototype.NamespaceProvider,
                GenerateNullables = generatorPrototype.GenerateNullables,
                IntegerDataType = generatorPrototype.IntegerDataType,
                DataAnnotationMode = generatorPrototype.DataAnnotationMode,
                GenerateDesignerCategoryAttribute = generatorPrototype.GenerateDesignerCategoryAttribute,
                EntityFramework = generatorPrototype.EntityFramework,
                GenerateInterfaces = generatorPrototype.GenerateInterfaces,
                MemberVisitor = generatorPrototype.MemberVisitor,
                TimeDataType = generatorPrototype.TimeDataType
            };

            gen.Generate(files);

            var provider = CodeDomProvider.CreateProvider("CSharp");
            var assemblies = new[]
            {
                "System.dll",
                "System.Core.dll",
                "System.Xml.dll",
                "System.Xml.Linq.dll",
                "System.Xml.Serialization.dll",
                "System.ServiceModel.dll",
                "System.ComponentModel.DataAnnotations.dll",
            };

            var binFolder = Path.Combine(output.OutputDirectory, "bin");
            Directory.CreateDirectory(binFolder);
            var results = provider.CompileAssemblyFromFile(new CompilerParameters(assemblies, Path.Combine(binFolder, name + ".dll")), output.Files.ToArray());

            Assert.False(results.Errors.HasErrors, string.Join("\n", results.Output.Cast<string>()));
            Assert.False(results.Errors.HasWarnings, string.Join("\n", results.Output.Cast<string>()));
            Assert.NotNull(results.CompiledAssembly);

            var assembly = Assembly.Load(results.CompiledAssembly.GetName());

            Assemblies[name] = assembly;

            return assembly;
        }

        const string IS24Pattern = @"xsd\is24\*\*.xsd";
        const string IS24ImmoTransferPattern = @"xsd\is24immotransfer\is24immotransfer.xsd";
        const string WadlPattern = @"xsd\wadl\wadl.xsd";
        const string ClientPattern = @"xsd\client\client.xsd";
        const string IataPattern = @"xsd\iata\????[^_][^_]?[^-]*.xsd";
        const string TimePattern = @"xsd\time\time.xsd";

        [Fact, TestPriority(1)]
        [UseCulture("en-US")]
        public void CanDeserializeSampleXml()
        {
            Compile("Client", ClientPattern);
            TestSamples("Client", ClientPattern);
            Compile("IS24RestApi", IS24Pattern);
            TestSamples("IS24RestApi", IS24Pattern);
            Compile("Wadl", WadlPattern, new Generator
            {
                EntityFramework = true,
                DataAnnotationMode = DataAnnotationMode.All,
                NamespaceProvider = new Dictionary<NamespaceKey, string> { { new NamespaceKey("http://wadl.dev.java.net/2009/02"), "Wadl" } }.ToNamespaceProvider(new GeneratorConfiguration { NamespacePrefix = "Wadl" }.NamespaceProvider.GenerateNamespace),
                MemberVisitor = (member, model) => { }
            });
            TestSamples("Wadl", WadlPattern);
            Compile("IS24ImmoTransfer", IS24ImmoTransferPattern);
            TestSamples("IS24ImmoTransfer", IS24ImmoTransferPattern);
            Compile("Iata", IataPattern, new Generator
            {
                EntityFramework = true,
                DataAnnotationMode = DataAnnotationMode.All,
                NamespaceProvider = new Dictionary<NamespaceKey, string> { { new NamespaceKey(""), "XmlSchema" }, { new NamespaceKey("http://www.iata.org/IATA/EDIST/2017.2"), "Iata" } }
                    .ToNamespaceProvider(new GeneratorConfiguration { NamespacePrefix = "Wadl" }.NamespaceProvider.GenerateNamespace),
                MemberVisitor = (member, model) => { },
                GenerateInterfaces = true
            });
            TestSamples("Iata", IataPattern);
        }

        private void TestSamples(string name, string pattern)
        {
            Assemblies.TryGetValue(name, out Assembly assembly);
            Assert.NotNull(assembly);
            DeserializeSampleXml(pattern, assembly);
        }

        private void DeserializeSampleXml(string pattern, Assembly assembly)
        {
            var files = Glob.Glob.ExpandNames(pattern);

            var set = new XmlSchemaSet();

            var schemas = files.Select(f => XmlSchema.Read(XmlReader.Create(f), (s, e) =>
            {
                Assert.True(false, e.Message);
            }));

            foreach (var s in schemas)
            {
                set.Add(s);
            }

            set.Compile();

            foreach (var rootElement in set.GlobalElements.Values.Cast<XmlSchemaElement>().Where(e => !e.IsAbstract))
            {
                var type = FindType(assembly, rootElement.QualifiedName);
                var serializer = new XmlSerializer(type);
                var generator = new XmlSampleGenerator(set, rootElement.QualifiedName);
                var sb = new StringBuilder();
                using (var xw = XmlWriter.Create(sb, new XmlWriterSettings { Indent = true }))
                {
                    // generate sample xml
                    generator.WriteXml(xw);
                    var xml = sb.ToString();

                    File.WriteAllText("xml.xml", xml);

                    // deserialize from sample
                    var sr = new StringReader(xml);
                    var o = serializer.Deserialize(sr);

                    // serialize back to xml
                    var xml2 = Serialize(serializer, o);

                    File.WriteAllText("xml2.xml", xml2);

                    // validate serialized xml
                    XmlReaderSettings settings = new XmlReaderSettings
                    {
                        ValidationType = ValidationType.Schema,
                        Schemas = set
                    };

                    settings.ValidationEventHandler += (s, e) =>
                    {
                        // generator doesn't generate valid values where pattern restrictions exist, e.g. email
                        if (!e.Message.Contains("The Pattern constraint failed"))
                        {
                            Assert.True(false, e.Message);
                        }
                    };

                    XmlReader reader = XmlReader.Create(new StringReader(xml2), settings);
                    while (reader.Read()) ;

                    // deserialize again
                    sr = new StringReader(xml2);
                    var o2 = serializer.Deserialize(sr);

                    AssertEx.Equal(o, o2);
                }
            }
        }

        private Type FindType(Assembly assembly, XmlQualifiedName xmlQualifiedName)
        {
            return assembly.GetTypes()
                .Single(t => t.CustomAttributes.Any(a => a.AttributeType == typeof(XmlRootAttribute)
                    && a.ConstructorArguments.Any(n => (string)n.Value == xmlQualifiedName.Name)
                    && a.NamedArguments.Any(n => n.MemberName == "Namespace" && (string)n.TypedValue.Value == xmlQualifiedName.Namespace)));
        }

        static string[] Classes = new[] { "ApartmentBuy",
                "ApartmentRent",
                "AssistedLiving",
                "CompulsoryAuction",
                "GarageBuy",
                "GarageRent",
                "Gastronomy",
                "HouseBuy",
                "HouseRent",
                "HouseType",
                "Industry",
                "Investment",
                "LivingBuySite",
                "LivingRentSite",
                "Office",
                "SeniorCare",
                "ShortTermAccommodation",
                "SpecialPurpose",
                "Store",
                "TradeSite" };

        [Fact, TestPriority(2)]
        public void ProducesSameXmlAsXsd()
        {
            var assembly = Compile("IS24RestApi", IS24Pattern);

            foreach (var c in Classes)
            {
                var t1 = assembly.GetTypes().SingleOrDefault(t => t.Name == c && t.Namespace.StartsWith("IS24RestApi.Offer.Realestates"));
                Assert.NotNull(t1);
                var t2 = Assembly.GetExecutingAssembly().GetTypes().SingleOrDefault(t => t.Name == c && t.Namespace == "IS24RestApi.Xsd");
                Assert.NotNull(t2);
                var f = char.ToLower(c[0]) + c.Substring(1);
                TestCompareToXsd(t1, t2, f);
            }
        }

        void TestCompareToXsd(Type t1, Type t2, string file)
        {
            foreach (var suffix in new[] { "max", "min" })
            {
                var serializer1 = new XmlSerializer(t1);
                var serializer2 = new XmlSerializer(t2);
                var xml = ReadXml(string.Format("{0}_{1}", file, suffix));
                var o1 = serializer1.Deserialize(new StringReader(xml));
                var o2 = serializer2.Deserialize(new StringReader(xml));
                var x1 = Serialize(serializer1, o1);
                var x2 = Serialize(serializer2, o2);

                File.WriteAllText("x1.xml", x1);
                File.WriteAllText("x2.xml", x2);

                Assert.Equal(x2, x1);
            }
        }

        [Fact, TestPriority(3)]
        public void CanSerializeAndDeserializeAllExampleXmlFiles()
        {
            var assembly = Compile("IS24RestApi", IS24Pattern);

            foreach (var c in Classes)
            {
                var t1 = assembly.GetTypes().SingleOrDefault(t => t.Name == c && t.Namespace.StartsWith("IS24RestApi.Offer.Realestates"));
                Assert.NotNull(t1);
                var f = char.ToLower(c[0]) + c.Substring(1);
                TestRoundtrip(t1, f);
            }
        }

        void TestRoundtrip(Type t, string file)
        {
            var serializer = new XmlSerializer(t);

            foreach (var suffix in new[] { "min", "max" })
            {
                var xml = ReadXml(string.Format("{0}_{1}", file, suffix));

                var deserializedObject = serializer.Deserialize(new StringReader(xml));

                var serializedXml = Serialize(serializer, deserializedObject);

                var deserializedXml = serializer.Deserialize(new StringReader(serializedXml));
                AssertEx.Equal(deserializedObject, deserializedXml);
            }
        }

        string Serialize(XmlSerializer serializer, object o)
        {
            var sw = new StringWriter();
            var ns = new XmlSerializerNamespaces();
            ns.Add("", null);
            serializer.Serialize(sw, o, ns);
            var serializedXml = sw.ToString();
            return serializedXml;
        }

        string ReadXml(string name)
        {
            var folder = Directory.GetCurrentDirectory();
            var xml = File.ReadAllText(string.Format(@"{0}\xml\{1}.xml", folder, name));
            return xml;
        }

        /// <summary>
        /// When the TimeDataType is set to use the DateTime, creating a serialiser against types
        /// that use xsd:time should no longer throw exceptions.
        /// </summary>
        [Fact]
        public void CreateDeserialiser_NoException_WhereTimeXsdPresent_AndTimeDataTypeSet()
        {
            Compile("Time1", TimePattern, new Generator
            {
                EntityFramework = true,
                DataAnnotationMode = DataAnnotationMode.All,
                NamespaceProvider = new Dictionary<NamespaceKey, string>
                    {
                        {new NamespaceKey("http://hic.gov.au/hiconline/medicare/version-4"), "hiconline"}
                    }
                    .ToNamespaceProvider(new GeneratorConfiguration { NamespacePrefix = "time" }.NamespaceProvider
                        .GenerateNamespace),
                MemberVisitor = (member, model) => { },
                GenerateInterfaces = true,
                TimeDataType = typeof(DateTime)
            });

            Assemblies.TryGetValue("Time1", out Assembly assembly);
            Assert.NotNull(assembly);

            var type = assembly.GetType("hiconline.Service");
            Assert.NotNull(type);

            var serializer = new XmlSerializer(type); // exception not thrown
            Assert.NotNull(serializer);
        }

        /// <summary>
        /// Test to ensure existing behaviour not changed.
        /// </summary>
        [Fact]
        public void CreateDeserialiser_ThrowsException_WhereTimeXsdPresent_AndTimeDataTypeNotSet()
        {
            Compile("Time2", TimePattern, new Generator
            {
                EntityFramework = true,
                DataAnnotationMode = DataAnnotationMode.All,
                NamespaceProvider = new Dictionary<NamespaceKey, string>
                    {
                        {new NamespaceKey("http://hic.gov.au/hiconline/medicare/version-4"), "hiconline"}
                    }
                    .ToNamespaceProvider(new GeneratorConfiguration { NamespacePrefix = "time" }.NamespaceProvider
                        .GenerateNamespace),
                MemberVisitor = (member, model) => { },
                GenerateInterfaces = true
            });

            Assemblies.TryGetValue("Time2", out Assembly assembly);
            Assert.NotNull(assembly);

            var type = assembly.GetType("hiconline.Service");
            Assert.NotNull(type);

            var ex = Assert.Throws<InvalidOperationException>(() => new XmlSerializer(type));
            Assert.NotNull(ex);
            Assert.Equal("There was an error reflecting type 'hiconline.Service'.", ex.Message);
        }

        [Fact]
        public void ComplexTypeWithAttributeGroupExtension()
        {
            const string xsd = @"<?xml version=""1.0"" encoding=""UTF-8""?>
<xs:schema xmlns:xs=""http://www.w3.org/2001/XMLSchema"" xmlns:xlink=""http://www.w3.org/1999/xlink"" elementFormDefault=""qualified"" attributeFormDefault=""unqualified"">
  <xs:attributeGroup name=""justify"">
    <xs:attribute name=""justify"" type=""simpleType""/>
  </xs:attributeGroup>
  <xs:complexType name=""group-name"">
    <xs:simpleContent>
      <xs:extension base=""xs:string"">
        <xs:attributeGroup ref=""justify""/>
      </xs:extension>
    </xs:simpleContent>
  </xs:complexType>
  <xs:simpleType name=""simpleType"">
    <xs:restriction base=""xs:token"">
      <xs:enumeration value=""foo""/>
    </xs:restriction>
  </xs:simpleType>
</xs:schema>";

            var generator = new Generator
            {
                GenerateInterfaces = false,
                NamespaceProvider = new NamespaceProvider
                {
                    GenerateNamespace = key => "Test"
                }
            };

            var contents = ConvertXml(nameof(ComplexTypeWithAttributeGroupExtension), xsd, generator);

            var csharp = Assert.Single(contents);

            CompareOutput(
                @"//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Runtime Version:4.0.30319.42000
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

// This code was generated by XmlSchemaClassGenerator version 1.0.0.0.
namespace Test
{
    using System.Collections;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Xml.Serialization;
    
    
    /// <summary>
    /// </summary>
    [System.CodeDom.Compiler.GeneratedCodeAttribute(""XmlSchemaClassGenerator"", ""1.0.0.0"")]
    [System.SerializableAttribute()]
    [System.Xml.Serialization.XmlTypeAttribute(""group-name"", Namespace="""")]
    [System.ComponentModel.DesignerCategoryAttribute(""code"")]
    public partial class Group_Name
    {

        /// <summary>
        /// <para xml:lang=""de"">Ruft den Text ab oder legt diesen fest.</para>
        /// <para xml:lang=""en"">Gets or sets the text value.</para>
        /// </summary>
        [System.Xml.Serialization.XmlTextAttribute(DataType=""string"")]
        public string Value { get; set; }

        /// <summary>
        /// </summary>
        [System.Xml.Serialization.XmlAttributeAttribute(""justify"", Form=System.Xml.Schema.XmlSchemaForm.Unqualified)]
        public SimpleType Justify { get; set; }

        /// <summary>
        /// <para xml:lang=""de"">Ruft einen Wert ab, der angibt, ob die Justify-Eigenschaft spezifiziert ist, oder legt diesen fest.</para>
        /// <para xml:lang=""en"">Gets or sets a value indicating whether the Justify property is specified.</para>
        /// </summary>
        [System.Xml.Serialization.XmlIgnoreAttribute()]
        public bool JustifySpecified { get; set; }
    }

    /// <summary>
    /// </summary>
    [System.CodeDom.Compiler.GeneratedCodeAttribute(""XmlSchemaClassGenerator"", ""1.0.0.0"")]
    [System.SerializableAttribute()]
    [System.Xml.Serialization.XmlTypeAttribute(""simpleType"", Namespace="""")]
    public enum SimpleType
    {

        /// <summary>
        /// </summary>
        [System.Xml.Serialization.XmlEnumAttribute(""foo"")]
        Foo,
    }
}
", csharp);
        }

        private static void CompareOutput(string expected, string actual)
        {
            string Normalize(string input) => input.Replace("\r\n", "\n");
            Assert.Equal(Normalize(expected), Normalize(actual));
        }
    }
}
