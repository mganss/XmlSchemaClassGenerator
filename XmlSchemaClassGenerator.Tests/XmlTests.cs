using System;
using System.CodeDom;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;
using System.Xml.XPath;
using Ganss.IO;
using Microsoft.CodeAnalysis;
using Microsoft.Xml.XMLGen;
using Xunit;
using Xunit.Abstractions;

namespace XmlSchemaClassGenerator.Tests
{
    [TestCaseOrderer("XmlSchemaClassGenerator.Tests.PriorityOrderer", "XmlSchemaClassGenerator.Tests")]
    public class XmlTests
    {
        private readonly ITestOutputHelper Output;

        public XmlTests(ITestOutputHelper output)
        {
            Output = output;
        }

        private IEnumerable<string> ConvertXml(string name, string xsd, Generator generatorPrototype = null)
        {
            if (name is null)
            {
                throw new ArgumentNullException(nameof(name));
            }

            var writer = new MemoryOutputWriter();

            var gen = new Generator
            {
                OutputWriter = writer,
                Version = new VersionProvider("Tests", "1.0.0.1"),
                NamespaceProvider = generatorPrototype.NamespaceProvider,
                GenerateNullables = generatorPrototype.GenerateNullables,
                IntegerDataType = generatorPrototype.IntegerDataType,
                DataAnnotationMode = generatorPrototype.DataAnnotationMode,
                GenerateDesignerCategoryAttribute = generatorPrototype.GenerateDesignerCategoryAttribute,
                GenerateComplexTypesForCollections = generatorPrototype.GenerateComplexTypesForCollections,
                EntityFramework = generatorPrototype.EntityFramework,
                AssemblyVisible = generatorPrototype.AssemblyVisible,
                GenerateInterfaces = generatorPrototype.GenerateInterfaces,
                MemberVisitor = generatorPrototype.MemberVisitor,
                CodeTypeReferenceOptions = generatorPrototype.CodeTypeReferenceOptions
            };

            var set = new XmlSchemaSet();

            using (var stringReader = new StringReader(xsd))
            {
                var schema = XmlSchema.Read(stringReader, (s, e) =>
                {
                    throw new InvalidOperationException($"{e.Severity}: {e.Message}",e.Exception);
                });

                set.Add(schema);
            }

            gen.Generate(set);

            return writer.Content;
        }

        const string IS24Pattern = @"xsd\is24\*\*.xsd";
        const string IS24ImmoTransferPattern = @"xsd\is24immotransfer\is24immotransfer.xsd";
        const string WadlPattern = @"xsd\wadl\*.xsd";
        const string ListPattern = @"xsd\list\list.xsd";
        const string SimplePattern = @"xsd\simple\simple.xsd";
        const string ArrayOrderPattern = @"xsd\array-order\array-order.xsd";
        const string ClientPattern = @"xsd\client\client.xsd";
        const string IataPattern = @"xsd\iata\*.xsd";
        const string TimePattern = @"xsd\time\time.xsd";
        const string TableauPattern = @"xsd\ts-api\*.xsd";
        const string VSTstPattern = @"xsd\vstst\vstst.xsd";
        const string BpmnPattern = @"xsd\bpmn\*.xsd";
        const string DtsxPattern = @"xsd\dtsx\dtsx2.xsd";

        // IATA test takes too long to perform every time

        //[Fact, TestPriority(1)]
        //[UseCulture("en-US")]
        //public void TestIata()
        //{
        //    Compiler.Generate("Iata", IataPattern, new Generator
        //    {
        //        EntityFramework = true,
        //        DataAnnotationMode = DataAnnotationMode.All,
        //        NamespaceProvider = new Dictionary<NamespaceKey, string> { { new NamespaceKey(""), "XmlSchema" }, { new NamespaceKey("http://www.iata.org/IATA/EDIST/2017.2"), "Iata" } }
        //            .ToNamespaceProvider(new GeneratorConfiguration { NamespacePrefix = "Iata" }.NamespaceProvider.GenerateNamespace),
        //        MemberVisitor = (member, model) => { },
        //        GenerateInterfaces = true
        //    });
        //    TestSamples("Iata", IataPattern);
        //}

        [Fact, TestPriority(1)]
        [UseCulture("en-US")]
        public void TestClient()
        {
            Compiler.Generate("Client", ClientPattern);
            TestSamples("Client", ClientPattern);
        }

        [Fact, TestPriority(1)]
        [UseCulture("en-US")]
        public void TestList()
        {
            Compiler.Generate("List", ListPattern);
            TestSamples("List", ListPattern);
        }

        [Fact, TestPriority(1)]
        [UseCulture("en-US")]
        public void TestSimple()
        {
            Compiler.Generate("Simple", SimplePattern, new Generator
            {
                GenerateNullables = true,
                IntegerDataType = typeof(int),
                DataAnnotationMode = DataAnnotationMode.All,
                GenerateDesignerCategoryAttribute = false,
                GenerateComplexTypesForCollections = true,
                EntityFramework = false,
                GenerateInterfaces = true,
                NamespacePrefix = "Simple",
                GenerateDescriptionAttribute = true,
                CodeTypeReferenceOptions = CodeTypeReferenceOptions.GlobalReference
            });
            TestSamples("Simple", SimplePattern);
        }

        [Fact, TestPriority(1)]
        [UseCulture("en-US")]
        public void TestArrayOrder()
        {
            Compiler.Generate("ArrayOrder", ArrayOrderPattern, new Generator
            {
                NamespacePrefix = "ArrayOrder",
                EmitOrder = true
            });
            TestSamples("ArrayOrder", ArrayOrderPattern);
        }

        [Fact, TestPriority(1)]
        [UseCulture("en-US")]
        public void TestIS24RestApi()
        {
            Compiler.Generate("IS24RestApi", IS24Pattern);
            TestSamples("IS24RestApi", IS24Pattern);
        }

        [Fact, TestPriority(1)]
        [UseCulture("en-US")]
        public void TestIS24RestApiShouldSerialize()
        {
            Compiler.Generate("IS24RestApiShouldSerialize", IS24Pattern, new Generator
            {
                GenerateNullables = true,
                GenerateInterfaces = true,
                NamespacePrefix = "IS24RestApi",
                GenerateDescriptionAttribute = true,
                UseShouldSerializePattern = true
            });
            TestSamples("IS24RestApiShouldSerialize", IS24Pattern);
        }

        [Fact, TestPriority(1)]
        [UseCulture("en-US")]
        public void TestWadl()
        {
            Compiler.Generate("Wadl", WadlPattern, new Generator
            {
                EntityFramework = true,
                DataAnnotationMode = DataAnnotationMode.All,
                NamespaceProvider = new Dictionary<NamespaceKey, string> { { new NamespaceKey("http://wadl.dev.java.net/2009/02"), "Wadl" } }.ToNamespaceProvider(new GeneratorConfiguration { NamespacePrefix = "Wadl" }.NamespaceProvider.GenerateNamespace),
                MemberVisitor = (member, model) => { }
            });
            TestSamples("Wadl", WadlPattern);
        }

        [Fact, TestPriority(1)]
        [UseCulture("en-US")]
        public void TestIS24ImmoTransfer()
        {
            Compiler.Generate("IS24ImmoTransfer", IS24ImmoTransferPattern);
            TestSamples("IS24ImmoTransfer", IS24ImmoTransferPattern);
        }

        [Fact, TestPriority(1)]
        [UseCulture("en-US")]
        public void TestTableau()
        {
            Compiler.Generate("Tableau", TableauPattern, new Generator());
            TestSamples("Tableau", TableauPattern);
        }

        [Fact, TestPriority(1)]
        [UseCulture("en-US")]
        public void TestDtsx()
        {
            Compiler.Generate("Dtsx", DtsxPattern, new Generator());
            TestSamples("Dtsx", DtsxPattern);
        }

        [Fact, TestPriority(1)]
        [UseCulture("en-US")]
        public void TestVSTst()
        {
            Compiler.Generate("VSTst", VSTstPattern, new Generator
            {
                TextValuePropertyName = "TextValue",
                GenerateComplexTypesForCollections = true
            });
            TestSamples("VSTst", VSTstPattern);
        }

        private void TestSamples(string name, string pattern)
        {
            var assembly = Compiler.GetAssembly(name);
            Assert.NotNull(assembly);
            DeserializeSampleXml(pattern, assembly);
        }

        private bool HandleValidationError(string xml, ValidationEventArgs e)
        {
            var line = xml.Split('\n')[e.Exception.LineNumber - 1].Substring(e.Exception.LinePosition - 1);
            var severity = e.Severity == XmlSeverityType.Error ? "Error" : "Warning";
            Output.WriteLine($"{severity} at line {e.Exception.LineNumber}, column {e.Exception.LinePosition}: {e.Message}");
            Output.WriteLine(line);
            return (e.Severity == XmlSeverityType.Error
                && !e.Message.Contains("The Pattern constraint failed"));  // generator doesn't generate valid values where pattern restrictions exist, e.g. email
        }

        private void DeserializeSampleXml(string pattern, Assembly assembly)
        {
            var files = Glob.ExpandNames(pattern);

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

            var anyValidXml = false;

            foreach (var rootElement in set.GlobalElements.Values.Cast<XmlSchemaElement>().Where(e => !e.IsAbstract && !(e.ElementSchemaType is XmlSchemaSimpleType)))
            {
                var type = FindType(assembly, rootElement.QualifiedName);
                var serializer = new XmlSerializer(type);
                var generator = new XmlSampleGenerator(set, rootElement.QualifiedName);
                var sb = new StringBuilder();
                using var xw = XmlWriter.Create(sb, new XmlWriterSettings { Indent = true });

                // generate sample xml
                generator.WriteXml(xw);
                var xml = sb.ToString();

                File.WriteAllText("xml.xml", xml);

                // validate serialized xml
                var settings = new XmlReaderSettings
                {
                    ValidationType = ValidationType.Schema,
                    Schemas = set
                };

                var invalid = false;

                void validate(object s, ValidationEventArgs e)
                {
                    if (HandleValidationError(xml, e)) invalid = true;
                }

                settings.ValidationEventHandler += validate;

                var reader = XmlReader.Create(new StringReader(xml), settings);
                while (reader.Read()) ;

                settings.ValidationEventHandler -= validate;

                // generated xml is not schema valid -> skip
                if (invalid) continue;
                anyValidXml = true;

                // deserialize from sample
                var sr = new StringReader(xml);
                var o = serializer.Deserialize(sr);

                // serialize back to xml
                var xml2 = Serialize(serializer, o);

                File.WriteAllText("xml2.xml", xml2);

                void validate2(object s, ValidationEventArgs e)
                {
                    if (HandleValidationError(xml2, e)) throw e.Exception;
                };

                settings.ValidationEventHandler += validate2;

                reader = XmlReader.Create(new StringReader(xml2), settings);
                while (reader.Read()) ;

                settings.ValidationEventHandler -= validate2;

                // deserialize again
                sr = new StringReader(xml2);
                var o2 = serializer.Deserialize(sr);

                AssertEx.Equal(o, o2);
            }

            Assert.True(anyValidXml, "No valid generated XML for this test");
        }

        private static Type FindType(Assembly assembly, XmlQualifiedName xmlQualifiedName)
        {
            return assembly.GetTypes()
                .Single(t => t.CustomAttributes.Any(a => a.AttributeType == typeof(XmlRootAttribute)
                    && a.ConstructorArguments.Any(n => (string)n.Value == xmlQualifiedName.Name)
                    && a.NamedArguments.Any(n => n.MemberName == "Namespace" && (string)n.TypedValue.Value == xmlQualifiedName.Namespace)));
        }

        static readonly string[] Classes = new[] { "ApartmentBuy",
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
            var assembly = Compiler.Generate("IS24RestApi", IS24Pattern);

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

        [Fact, TestPriority(1)]
        [UseCulture("en-US")]
        public void TestCustomNamespaces()
        {
            string customNsPattern = "|{0}={1}";
            string bpmnXsd = "BPMN20.xsd";
            string semantXsd = "Semantic.xsd";
            string bpmndiXsd = "BPMNDI.xsd";
            string dcXsd = "DC.xsd";
            string diXsd = "DI.xsd";

            Dictionary<string, string> xsdToCsharpNsMap = new Dictionary<string, string>
            {
                { bpmnXsd, "Namespace1" },
                { semantXsd, "Namespace1" },
                { bpmndiXsd, "Namespace2" },
                { dcXsd, "Namespace3" },
                { diXsd, "Namespace4" }
            };

            Dictionary<string, string> xsdToCsharpTypeMap = new Dictionary<string, string>
            {
                { bpmnXsd, "TDefinitions" },
                { semantXsd, "TActivity" },
                { bpmndiXsd, "BPMNDiagram" },
                { dcXsd, "Font" },
                { diXsd, "DiagramElement" }
            };

            List<string> customNamespaceConfig = new List<string>();

            foreach (var ns in xsdToCsharpNsMap)
                customNamespaceConfig.Add(string.Format(customNsPattern, ns.Key, ns.Value));

            var assembly = Compiler.Generate("Bpmn", BpmnPattern, new Generator
            {
                DataAnnotationMode = DataAnnotationMode.All,
                GenerateNullables = true,
                MemberVisitor = (member, model) => { },
                NamespaceProvider = customNamespaceConfig.Select(n => CodeUtilities.ParseNamespace(n, null)).ToNamespaceProvider()
            });
            Assert.NotNull(assembly);

            Type type = null;

            type = assembly.GetTypes().SingleOrDefault(t => t.Name == xsdToCsharpTypeMap[bpmnXsd]);
            Assert.NotNull(type);
            Assert.Equal(xsdToCsharpNsMap[bpmnXsd], type.Namespace);

            type = assembly.GetTypes().SingleOrDefault(t => t.Name == xsdToCsharpTypeMap[semantXsd]);
            Assert.NotNull(type);
            Assert.Equal(xsdToCsharpNsMap[semantXsd], type.Namespace);

            type = assembly.GetTypes().SingleOrDefault(t => t.Name == xsdToCsharpTypeMap[bpmndiXsd]);
            Assert.NotNull(type);
            Assert.Equal(xsdToCsharpNsMap[bpmndiXsd], type.Namespace);

            type = assembly.GetTypes().SingleOrDefault(t => t.Name == xsdToCsharpTypeMap[dcXsd]);
            Assert.NotNull(type);
            Assert.Equal(xsdToCsharpNsMap[dcXsd], type.Namespace);

            type = assembly.GetTypes().SingleOrDefault(t => t.Name == xsdToCsharpTypeMap[diXsd]);
            Assert.NotNull(type);
            Assert.Equal(xsdToCsharpNsMap[diXsd], type.Namespace);
        }

        [Fact, TestPriority(2)]
        [UseCulture("en-US")]
        public void TestBpmn()
        {
            var assembly = Compiler.Generate("Bpmn", BpmnPattern);
            Assert.NotNull(assembly);

            var type = assembly.GetTypes().SingleOrDefault(t => t.Name == "TDefinitions");
            Assert.NotNull(type);

            var serializer = new XmlSerializer(type);
            serializer.UnknownNode += new XmlNodeEventHandler(UnknownNodeHandler);
            serializer.UnknownAttribute += new XmlAttributeEventHandler(UnknownAttributeHandler);
            var unknownNodeError = false;
            var unknownAttrError = false;

            void UnknownNodeHandler(object sender, XmlNodeEventArgs e)
            {
                unknownNodeError = true;
            }

            void UnknownAttributeHandler(object sender, XmlAttributeEventArgs e)
            {
                unknownAttrError = true;
            }

            var currDir = Directory.GetCurrentDirectory();
            var testDir = "bpmn_tests";
            var fileExt = "bpmn";
            var testFiles = Glob.ExpandNames(string.Format("{0}\\xml\\{1}\\*.{2}", currDir, testDir, fileExt));

            foreach (var testFile in testFiles)
            {
                var xmlString = File.ReadAllText(testFile);
                var reader = XmlReader.Create(new StringReader(xmlString), new XmlReaderSettings { IgnoreWhitespace = true });

                var isDeserializable = serializer.CanDeserialize(reader);
                Assert.True(isDeserializable);

                var deserializedObject = serializer.Deserialize(reader);
                Assert.False(unknownNodeError);
                Assert.False(unknownAttrError);

                var serializedXml = Serialize(serializer, deserializedObject, GetNamespacesFromSource(xmlString));

                var deserializedXml = serializer.Deserialize(new StringReader(serializedXml));
                AssertEx.Equal(deserializedObject, deserializedXml);
            }
        }

        private IDictionary<string, string> GetNamespacesFromSource(string source)
        {
            XPathDocument doc = new XPathDocument(new StringReader(source));
            XPathNavigator namespaceNavigator = doc.CreateNavigator();
            namespaceNavigator.MoveToFollowing(XPathNodeType.Element);
            return namespaceNavigator.GetNamespacesInScope(XmlNamespaceScope.All);
        }

        [Fact, TestPriority(3)]
        public void CanSerializeAndDeserializeAllExampleXmlFiles()
        {
            var assembly = Compiler.Generate("IS24RestApi", IS24Pattern);

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

        static string Serialize(XmlSerializer serializer, object o, IDictionary<string, string> prefixToNsMap = null)
        {
            var sw = new StringWriter();
            var ns = new XmlSerializerNamespaces();
            if (prefixToNsMap == null)
            {
                ns.Add("", null);
            }
            else
            {
                foreach (var ptns in prefixToNsMap)
                {
                    ns.Add(ptns.Key, ptns.Value);
                }
            }
            serializer.Serialize(sw, o, ns);
            var serializedXml = sw.ToString();
            return serializedXml;
        }

        static string ReadXml(string name)
        {
            var folder = Directory.GetCurrentDirectory();
            var xml = File.ReadAllText(string.Format(@"{0}\xml\{1}.xml", folder, name));
            return xml;
        }

        [Fact]
        public void DontGenerateElementForEmptyCollectionInChoice()
        {
            var assembly = Compiler.Generate("Tableau", TableauPattern, new Generator());
            Assert.NotNull(assembly);
            var requestType = assembly.GetType("Api.TsRequest");
            Assert.NotNull(requestType);
            var r = Activator.CreateInstance(requestType);
            var s = new XmlSerializer(requestType);
            var sw = new StringWriter();
            s.Serialize(sw, r);
            var xml = sw.ToString();
            Assert.DoesNotContain("tags", xml, StringComparison.OrdinalIgnoreCase);
        }


        [Theory]
        [InlineData(CodeTypeReferenceOptions.GlobalReference, "[global::System.ComponentModel.EditorBrowsableAttribute(global::System.ComponentModel.EditorBrowsableState.Never)]")]
        [InlineData((CodeTypeReferenceOptions)0, "[System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]")]
        public void EditorBrowsableAttributeRespectsCodeTypeReferenceOptions(CodeTypeReferenceOptions codeTypeReferenceOptions, string expectedLine)
        {
            const string xsd = @"<?xml version=""1.0"" encoding=""UTF-8""?>
<xs:schema elementFormDefault=""qualified"" xmlns:xs=""http://www.w3.org/2001/XMLSchema"">
		<xs:complexType name=""document"">
			<xs:attribute name=""some-value"">
				<xs:simpleType>
					<xs:restriction base=""xs:string"">
						<xs:enumeration value=""one""/>
						<xs:enumeration value=""two""/>
					</xs:restriction>
				</xs:simpleType>
			</xs:attribute>
			<xs:attribute name=""system"" type=""xs:string""/>
		</xs:complexType>
</xs:schema>";

            var generatedType = ConvertXml(nameof(EditorBrowsableAttributeRespectsCodeTypeReferenceOptions), xsd, new Generator
            {
                CodeTypeReferenceOptions = codeTypeReferenceOptions,
                GenerateNullables = true,
                GenerateInterfaces = false,
                NamespaceProvider = new NamespaceProvider
                {
                    GenerateNamespace = key => "Test"
                }
            });

            Assert.Contains(
                expectedLine,
                generatedType.First());
        }

        [Fact]
        public void MixedTypeMustNotCollideWithExistingMembers()
        {
            const string xsd = @"<?xml version=""1.0"" encoding=""UTF-8""?>
<xs:schema elementFormDefault=""qualified"" xmlns:xs=""http://www.w3.org/2001/XMLSchema"" targetNamespace=""http://local.none"" xmlns:l=""http://local.none"">
	<xs:element name=""document"" type=""l:elem"">
	</xs:element>
	<xs:complexType name=""elem"" mixed=""true"">
		<xs:attribute name=""Text"" type=""xs:string""/>
	</xs:complexType>
</xs:schema>";

            var generatedType = ConvertXml(nameof(MixedTypeMustNotCollideWithExistingMembers), xsd, new Generator
            {
                NamespaceProvider = new NamespaceProvider
                {
                    GenerateNamespace = key => "Test"
                }
            });

            Assert.Contains(
                @"public string[] Text_1 { get; set; }",
                generatedType.First());
        }

        [Fact]
        public void MixedTypeMustNotCollideWithContainingTypeName()
        {
            const string xsd = @"<?xml version=""1.0"" encoding=""UTF-8""?>
<xs:schema elementFormDefault=""qualified"" xmlns:xs=""http://www.w3.org/2001/XMLSchema"" targetNamespace=""http://local.none"" xmlns:l=""http://local.none"">
	<xs:element name=""document"" type=""l:Text"">
	</xs:element>
	<xs:complexType name=""Text"" mixed=""true"">
	</xs:complexType>
</xs:schema>";

            var generatedType = ConvertXml(nameof(MixedTypeMustNotCollideWithExistingMembers), xsd, new Generator
            {
                NamespaceProvider = new NamespaceProvider
                {
                    GenerateNamespace = key => "Test"
                }
            });

            Assert.Contains(
                @"public string[] Text_1 { get; set; }",
                generatedType.First());
        }

        [Theory]
        [InlineData(@"xml/sameattributenames.xsd", @"xml/sameattributenames_import.xsd")]
        public void CollidingAttributeAndPropertyNamesCanBeResolved(params string[] files)
        {
            // Compilation would previously throw due to duplicate type name within type
            var assembly = Compiler.GenerateFiles("AttributesWithSameName", files);

            Assert.NotNull(assembly);
        }

        [Fact]
        public void CollidingElementAndComplexTypeNamesCanBeResolved()
        {
            const string xsd = @"<?xml version=""1.0"" encoding = ""UTF-8""?>
<xs:schema elementFormDefault=""qualified"" xmlns:xs=""http://www.w3.org/2001/XMLSchema"" targetNamespace=""http://local.none"">
  <xs:complexType name=""MyType"">
    <xs:sequence>
      <xs:element maxOccurs=""1"" minOccurs=""0"" name=""output"" type=""xs:string""/>
    </xs:sequence>
  </xs:complexType>
  <xs:element name=""MyType"">
    <xs:simpleType>
      <xs:restriction base=""xs:string"">
        <xs:enumeration value=""Choice1""/>
        <xs:enumeration value=""Choice2""/>
        <xs:enumeration value=""Choice3""/>
      </xs:restriction>
    </xs:simpleType>
  </xs:element>
</xs:schema>
";
            var generator = new Generator
            {
                NamespaceProvider = new NamespaceProvider
                {
                    GenerateNamespace = key => "Test"
                }
            };

            var generatedType = ConvertXml(nameof(CollidingElementAndComplexTypeNamesCanBeResolved), xsd, generator).First();

            Assert.Contains(@"public partial class MyType", generatedType);
            Assert.Contains(@"public enum MyType", generatedType);
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
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

// This code was generated by Tests version 1.0.0.1.
namespace Test
{
    using System.Collections;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Xml.Serialization;


    [System.CodeDom.Compiler.GeneratedCodeAttribute(""Tests"", ""1.0.0.1"")]
    [System.SerializableAttribute()]
    [System.Xml.Serialization.XmlTypeAttribute(""group-name"", Namespace="""")]
    [System.ComponentModel.DesignerCategoryAttribute(""code"")]
    public partial class Group_Name
    {

        /// <summary>
        /// <para xml:lang=""de"">Ruft den Text ab oder legt diesen fest.</para>
        /// <para xml:lang=""en"">Gets or sets the text value.</para>
        /// </summary>
        [System.Xml.Serialization.XmlTextAttribute()]
        public string Value { get; set; }

        [System.Xml.Serialization.XmlAttributeAttribute(""justify"")]
        public SimpleType Justify { get; set; }

        /// <summary>
        /// <para xml:lang=""de"">Ruft einen Wert ab, der angibt, ob die Justify-Eigenschaft spezifiziert ist, oder legt diesen fest.</para>
        /// <para xml:lang=""en"">Gets or sets a value indicating whether the Justify property is specified.</para>
        /// </summary>
        [System.Xml.Serialization.XmlIgnoreAttribute()]
        public bool JustifySpecified { get; set; }
    }

    [System.CodeDom.Compiler.GeneratedCodeAttribute(""Tests"", ""1.0.0.1"")]
    [System.SerializableAttribute()]
    [System.Xml.Serialization.XmlTypeAttribute(""simpleType"", Namespace="""")]
    public enum SimpleType
    {

        [System.Xml.Serialization.XmlEnumAttribute(""foo"")]
        Foo,
    }
}
", csharp);
        }

        [Fact]
        public void ChoiceMembersAreNullable()
        {
            // We test to see whether choices which are part of a larger ComplexType are marked as nullable.
            // Because nullability isn't directly exposed in the generated C#, we use "XXXSpecified" on a value type
            // as a proxy.

            const string xsd = @"<?xml version=""1.0"" encoding=""UTF-8""?>
<xs:schema xmlns:xs=""http://www.w3.org/2001/XMLSchema"" xmlns:xlink=""http://www.w3.org/1999/xlink"" elementFormDefault=""qualified"" attributeFormDefault=""unqualified"">
    <xs:complexType name=""Root"">
      <xs:sequence>
      <!-- Choice directly inside a complex type -->
      <xs:element name=""Sub"">
        <xs:complexType>
          <xs:choice>
              <xs:element name=""Opt1"" type=""xs:int""/>
              <xs:element name=""Opt2"" type=""xs:int""/>
          </xs:choice>
        </xs:complexType>
        </xs:element>
        <!-- Choice as part of a larger sequence -->
        <xs:choice>
          <xs:element name=""Opt3"" type=""xs:int""/>
          <xs:element name=""Opt4"" type=""xs:int""/>
        </xs:choice>
      </xs:sequence>
    </xs:complexType>
</xs:schema>";

            var generator = new Generator
            {
                NamespaceProvider = new NamespaceProvider
                {
                    GenerateNamespace = key => "Test"
                }
            };
            var contents = ConvertXml(nameof(ComplexTypeWithAttributeGroupExtension), xsd, generator);
            var content = Assert.Single(contents);

            Assert.Contains("Opt1Specified", content);
            Assert.Contains("Opt2Specified", content);
            Assert.Contains("Opt3Specified", content);
            Assert.Contains("Opt4Specified", content);
        }

        [Fact]
        public void AssemblyVisibleIsInternalClass()
        {
            // We test to see whether choices which are part of a larger ComplexType are marked as nullable.
            // Because nullability isn't directly exposed in the generated C#, we use "XXXSpecified" on a value type
            // as a proxy.

            const string xsd = @"<?xml version=""1.0"" encoding=""UTF-8""?>
<xs:schema xmlns:xs=""http://www.w3.org/2001/XMLSchema"" xmlns:xlink=""http://www.w3.org/1999/xlink"" elementFormDefault=""qualified"" attributeFormDefault=""unqualified"">
    <xs:complexType name=""Root"">
      <xs:sequence>
      <!-- Choice directly inside a complex type -->
      <xs:element name=""Sub"">
        <xs:complexType>
          <xs:choice>
              <xs:element name=""Opt1"" type=""xs:int""/>
              <xs:element name=""Opt2"" type=""xs:int""/>
          </xs:choice>
        </xs:complexType>
        </xs:element>
        <!-- Choice as part of a larger sequence -->
        <xs:choice>
          <xs:element name=""Opt3"" type=""xs:int""/>
          <xs:element name=""Opt4"" type=""xs:int""/>
        </xs:choice>
      </xs:sequence>
    </xs:complexType>
</xs:schema>";

            var generator = new Generator
            {
                NamespaceProvider = new NamespaceProvider
                {
                    GenerateNamespace = key => "Test"
                },
                AssemblyVisible = true
            };
            var contents = ConvertXml(nameof(ComplexTypeWithAttributeGroupExtension), xsd, generator);
            var content = Assert.Single(contents);

            Assert.Contains("internal partial class RootSub", content);
            Assert.Contains("internal partial class Root", content);
        }

        [Fact]
        public void AssemblyVisibleIsInternalEnum()
        {
            // We test to see whether choices which are part of a larger ComplexType are marked as nullable.
            // Because nullability isn't directly exposed in the generated C#, we use "XXXSpecified" on a value type
            // as a proxy.

            const string xsd = @"<?xml version=""1.0"" encoding=""UTF-8""?>
<xs:schema xmlns:xs=""http://www.w3.org/2001/XMLSchema"" xmlns:xlink=""http://www.w3.org/1999/xlink"" elementFormDefault=""qualified"" attributeFormDefault=""unqualified"">
  <xs:simpleType name=""Answer"">
    <xs:restriction base=""xs:string"">
      <xs:enumeration value=""Yes""/>
      <xs:enumeration value=""No""/>
      <xs:enumeration value=""Probably""/>
    </xs:restriction>
  </xs:simpleType>
</xs:schema>";

            var generator = new Generator
            {
                NamespaceProvider = new NamespaceProvider
                {
                    GenerateNamespace = key => "Test"
                },
                AssemblyVisible = true
            };
            var contents = ConvertXml(nameof(ComplexTypeWithAttributeGroupExtension), xsd, generator);
            var content = Assert.Single(contents);

            Assert.Contains("internal enum Answer", content);
        }

        [Fact]
        public void AssemblyVisibleIsInternalInterface()
        {
            // We test to see whether choices which are part of a larger ComplexType are marked as nullable.
            // Because nullability isn't directly exposed in the generated C#, we use "XXXSpecified" on a value type
            // as a proxy.

            const string xsd = @"<?xml version=""1.0"" encoding=""UTF-8""?>
<xs:schema xmlns:tns=""http://test.test/schema/AssemblyVisibleIsInternalInterface"" targetNamespace=""http://test.test/schema/AssemblyVisibleIsInternalInterface"" xmlns:xs=""http://www.w3.org/2001/XMLSchema"" xmlns:xlink=""http://www.w3.org/1999/xlink"" elementFormDefault=""qualified"" attributeFormDefault=""unqualified"">
  <xs:complexType name=""NamedType"">
    <xs:attributeGroup ref=""tns:NamedElement""/>
  </xs:complexType>
  <xs:attributeGroup name=""NamedElement"">
    <xs:attribute name=""Name"" use=""required"" type=""xs:string"" />
  </xs:attributeGroup>
</xs:schema>";

            var generator = new Generator
            {
                NamespaceProvider = new NamespaceProvider
                {
                    GenerateNamespace = key => "Test"
                },
                GenerateInterfaces = true,
                AssemblyVisible = true
            };
            var contents = ConvertXml(nameof(ComplexTypeWithAttributeGroupExtension), xsd, generator);
            var content = Assert.Single(contents);

            Assert.Contains("internal partial interface INamedElement", content);
        }

        [Fact]
        public void DecimalSeparatorTest()
        {
            // see https://github.com/mganss/XmlSchemaClassGenerator/issues/101

            const string xsd = @"<?xml version=""1.0"" encoding=""UTF-8""?>
<xs:schema xmlns:xs=""http://www.w3.org/2001/XMLSchema"" elementFormDefault=""qualified"" attributeFormDefault=""unqualified"">
  <xs:complexType name=""NamedType"">
    <xs:attribute name=""SomeAttr"" type=""xs:decimal"" default=""1.5"" />
  </xs:complexType>
</xs:schema>";

            var generator = new Generator
            {
                NamespaceProvider = new NamespaceProvider
                {
                    GenerateNamespace = key => "Test",
                },
                GenerateInterfaces = true,
                AssemblyVisible = true
            };
            var contents = ConvertXml(nameof(ComplexTypeWithAttributeGroupExtension), xsd, generator);
            var content = Assert.Single(contents);

            Assert.Contains("private decimal _someAttr = 1.5m;", content);
        }

        [Fact]
        public void DoNotGenerateIntermediaryClassForArrayElements()
        {
            // see https://github.com/mganss/XmlSchemaClassGenerator/issues/32

            const string xsd = @"<?xml version=""1.0"" encoding=""UTF-8""?>
<xs:schema version=""1.0"" targetNamespace=""test""
  elementFormDefault=""qualified""
  xmlns:test=""test""
  xmlns:xs=""http://www.w3.org/2001/XMLSchema"">
  <xs:element name=""foo"" type=""test:foo""/>
<xs:complexType name=""foo"">
       <xs:sequence>
        <xs:element name=""bar"" minOccurs=""0"">
          <xs:complexType>
            <xs:sequence>
              <xs:element name=""baz"" type=""xs:string"" minOccurs=""0"" maxOccurs=""unbounded"">
              </xs:element>
            </xs:sequence>
          </xs:complexType>
        </xs:element>
      </xs:sequence>
</xs:complexType>
</xs:schema>";

            var generator = new Generator
            {
                NamespaceProvider = new NamespaceProvider
                {
                    GenerateNamespace = key => "Test",
                },
                GenerateComplexTypesForCollections = false,
                GenerateInterfaces = true,
                AssemblyVisible = true
            };
            var contents = ConvertXml(nameof(ComplexTypeWithAttributeGroupExtension), xsd, generator);
            var content = Assert.Single(contents);

            var assembly = Compiler.Compile(nameof(DoNotGenerateIntermediaryClassForArrayElements), content);

            var fooType = Assert.Single(assembly.DefinedTypes);
            Assert.NotNull(fooType);
            Assert.True(fooType.FullName == "Test.Foo");
        }

        [Fact]
        public void GenerateIntermediaryClassForArrayElements()
        {
            // see https://github.com/mganss/XmlSchemaClassGenerator/issues/32

            const string xsd = @"<?xml version=""1.0"" encoding=""UTF-8""?>
<xs:schema version=""1.0"" targetNamespace=""test""
  elementFormDefault=""qualified""
  xmlns:test=""test""
  xmlns:xs=""http://www.w3.org/2001/XMLSchema"">
  <xs:element name=""foo"" type=""test:foo""/>
<xs:complexType name=""foo"">
       <xs:sequence>
        <xs:element name=""bar"" minOccurs=""0"">
          <xs:complexType>
            <xs:sequence>
              <xs:element name=""baz"" type=""xs:string"" minOccurs=""0"" maxOccurs=""unbounded"">
              </xs:element>
            </xs:sequence>
          </xs:complexType>
        </xs:element>
      </xs:sequence>
</xs:complexType>
</xs:schema>";

            var generator = new Generator
            {
                NamespaceProvider = new NamespaceProvider
                {
                    GenerateNamespace = key => "Test",
                },
                GenerateComplexTypesForCollections = true,
                GenerateInterfaces = true,
                AssemblyVisible = true
            };
            var contents = ConvertXml(nameof(GenerateIntermediaryClassForArrayElements), xsd, generator);
            var content = Assert.Single(contents);

            var assembly = Compiler.Compile(nameof(GenerateIntermediaryClassForArrayElements), content);

            Assert.True(assembly.DefinedTypes.Count() == 2);
            Assert.Single(assembly.DefinedTypes, x => x.FullName == "Test.Foo");
            Assert.Single(assembly.DefinedTypes, x => x.FullName == "Test.FooBar");
        }

        [Fact]
        public void BoolTest()
        {
            // see https://github.com/mganss/XmlSchemaClassGenerator/issues/103

            const string xsd = @"<?xml version=""1.0"" encoding=""UTF-8""?>
<xs:schema xmlns:xs=""http://www.w3.org/2001/XMLSchema"" elementFormDefault=""qualified"" attributeFormDefault=""unqualified"">
  <xs:complexType name=""NamedType"">
    <xs:attribute name=""b0"" type=""xs:boolean"" default=""0"" />
    <xs:attribute name=""b1"" type=""xs:boolean"" default=""1"" />
    <xs:attribute name=""bf"" type=""xs:boolean"" default=""false"" />
    <xs:attribute name=""bt"" type=""xs:boolean"" default=""true"" />
  </xs:complexType>
</xs:schema>";

            var generator = new Generator
            {
                NamespaceProvider = new NamespaceProvider
                {
                    GenerateNamespace = key => "Test",
                },
                GenerateInterfaces = true,
                AssemblyVisible = true
            };
            var contents = ConvertXml(nameof(ComplexTypeWithAttributeGroupExtension), xsd, generator);
            var content = Assert.Single(contents);

            Assert.Contains("private bool _b0 = false;", content);
            Assert.Contains("private bool _b1 = true;", content);
            Assert.Contains("private bool _bf = false;", content);
            Assert.Contains("private bool _bt = true;", content);
        }

        private static void CompareOutput(string expected, string actual)
        {
            static string Normalize(string input) => Regex.Replace(input, @"[ \t]*\r\n", "\n");
            Assert.Equal(Normalize(expected), Normalize(actual));
        }

        [Theory]
        [InlineData(typeof(decimal), "decimal")]
        [InlineData(typeof(long), "long")]
        [InlineData(null, "string")]
        public void UnmappedIntegerDerivedTypesAreMappedToExpectedCSharpType(Type integerDataType, string expectedTypeName)
        {
            const string xsd = @"<?xml version=""1.0"" encoding=""UTF-8""?>
<xs:schema xmlns:xs=""http://www.w3.org/2001/XMLSchema"" elementFormDefault=""qualified"" attributeFormDefault=""unqualified"">
    <xs:complexType name=""root"">
        <xs:sequence>
	        <xs:element name=""unboundedInteger01"" type=""xs:integer""/>
	        <xs:element name=""unboundedInteger02"" type=""xs:nonNegativeInteger""/>
	        <xs:element name=""unboundedInteger03"" type=""xs:positiveInteger""/>
	        <xs:element name=""unboundedInteger04"" type=""xs:nonPositiveInteger""/>
	        <xs:element name=""unboundedInteger05"" type=""xs:negativeInteger""/>

	        <xs:element name=""outOfBoundsInteger01"" type=""tooLongPositiveInteger""/>
	        <xs:element name=""outOfBoundsInteger02"" type=""tooLongNonNegativeInteger""/>
	        <xs:element name=""outOfBoundsInteger03"" type=""tooLongInteger""/>
	        <xs:element name=""outOfBoundsInteger04"" type=""tooLongNegativeInteger""/>
	        <xs:element name=""outOfBoundsInteger05"" type=""tooLongNonPositiveInteger""/>
        </xs:sequence>
	</xs:complexType>

    <xs:simpleType name=""tooLongPositiveInteger"">
	    <xs:restriction base=""xs:positiveInteger"">
		    <xs:totalDigits value=""30""/>
	    </xs:restriction>
    </xs:simpleType>
    <xs:simpleType name=""tooLongNonNegativeInteger"">
	    <xs:restriction base=""xs:nonNegativeInteger"">
		    <xs:totalDigits value=""30""/>
	    </xs:restriction>
    </xs:simpleType>
    <xs:simpleType name=""tooLongInteger"">
	    <xs:restriction base=""xs:integer"">
		    <xs:totalDigits value=""29""/>
	    </xs:restriction>
    </xs:simpleType>
    <xs:simpleType name=""tooLongNegativeInteger"">
	    <xs:restriction base=""xs:negativeInteger"">
		    <xs:totalDigits value=""29""/>
	    </xs:restriction>
    </xs:simpleType>
    <xs:simpleType name=""tooLongNonPositiveInteger"">
	    <xs:restriction base=""xs:nonPositiveInteger"">
		    <xs:totalDigits value=""29""/>
	    </xs:restriction>
    </xs:simpleType>
</xs:schema>";

            var generatedType = ConvertXml(nameof(UnmappedIntegerDerivedTypesAreMappedToExpectedCSharpType), xsd, new Generator
            {
                NamespaceProvider = new NamespaceProvider
                {
                    GenerateNamespace = key => "Test",
                },
                GenerateNullables = true,
                NamingScheme = NamingScheme.PascalCase,
                IntegerDataType = integerDataType,
            }).First();

            Assert.Contains($"public {expectedTypeName} UnboundedInteger01", generatedType);
            Assert.Contains($"public {expectedTypeName} UnboundedInteger02", generatedType);
            Assert.Contains($"public {expectedTypeName} UnboundedInteger03", generatedType);
            Assert.Contains($"public {expectedTypeName} UnboundedInteger04", generatedType);
            Assert.Contains($"public {expectedTypeName} UnboundedInteger05", generatedType);
            Assert.Contains($"public {expectedTypeName} OutOfBoundsInteger01", generatedType);
            Assert.Contains($"public {expectedTypeName} OutOfBoundsInteger02", generatedType);
            Assert.Contains($"public {expectedTypeName} OutOfBoundsInteger03", generatedType);
            Assert.Contains($"public {expectedTypeName} OutOfBoundsInteger04", generatedType);
            Assert.Contains($"public {expectedTypeName} OutOfBoundsInteger05", generatedType);
        }

        [Theory]
        [InlineData("xs:positiveInteger", 1, 2, "byte")]
        [InlineData("xs:nonNegativeInteger", 1, 2, "byte")]
        [InlineData("xs:integer", 1, 2, "sbyte")]
        [InlineData("xs:negativeInteger", 1, 2, "sbyte")]
        [InlineData("xs:nonPositiveInteger", 1, 2, "sbyte")]
        [InlineData("xs:positiveInteger", 3, 4, "ushort")]
        [InlineData("xs:nonNegativeInteger", 3, 4, "ushort")]
        [InlineData("xs:integer", 3, 4, "short")]
        [InlineData("xs:negativeInteger", 3, 4, "short")]
        [InlineData("xs:nonPositiveInteger", 3, 4, "short")]
        [InlineData("xs:positiveInteger", 5, 9, "uint")]
        [InlineData("xs:nonNegativeInteger", 5, 9, "uint")]
        [InlineData("xs:integer", 5, 9, "int")]
        [InlineData("xs:negativeInteger", 5, 9, "int")]
        [InlineData("xs:nonPositiveInteger", 5, 9, "int")]
        [InlineData("xs:positiveInteger", 10, 19, "ulong")]
        [InlineData("xs:nonNegativeInteger", 10, 19, "ulong")]
        [InlineData("xs:integer", 10, 18, "long")]
        [InlineData("xs:negativeInteger", 10, 18, "long")]
        [InlineData("xs:nonPositiveInteger", 10, 18, "long")]
        [InlineData("xs:positiveInteger", 20, 29, "decimal")]
        [InlineData("xs:nonNegativeInteger", 20, 29, "decimal")]
        [InlineData("xs:integer", 20, 28, "decimal")]
        [InlineData("xs:negativeInteger", 20, 28, "decimal")]
        [InlineData("xs:nonPositiveInteger", 20, 28, "decimal")]
        public void RestrictedIntegerDerivedTypesAreMappedToExpectedCSharpTypes(string restrictionBase, int totalDigitsRangeFrom, int totalDigitsRangeTo, string expectedTypeName)
        {
            const string xsdTemplate = @"<?xml version=""1.0"" encoding=""UTF-8""?>
<xs:schema xmlns:xs=""http://www.w3.org/2001/XMLSchema"" elementFormDefault=""qualified"" attributeFormDefault=""unqualified"">
    <xs:complexType name=""root"">
        <xs:sequence>
	        {0}
        </xs:sequence>
	</xs:complexType>

    {1}
</xs:schema>";

            const string elementTemplate = @"<xs:element name=""restrictedInteger{0}"" type=""RestrictedInteger{0}""/>";

            const string simpleTypeTemplate = @"
<xs:simpleType name=""RestrictedInteger{1}"">
	<xs:restriction base=""{0}"">
		<xs:totalDigits value=""{1}""/>
	</xs:restriction>
</xs:simpleType>
";

            string elementDefinitions = "", simpleTypeDefinitions = "";
            for (var i = totalDigitsRangeFrom; i <= totalDigitsRangeTo; i++)
            {
                elementDefinitions += string.Format(elementTemplate, i);
                simpleTypeDefinitions += string.Format(simpleTypeTemplate, restrictionBase, i);
            }

            var xsd = string.Format(xsdTemplate, elementDefinitions, simpleTypeDefinitions);
            var generatedType = ConvertXml(nameof(RestrictedIntegerDerivedTypesAreMappedToExpectedCSharpTypes), xsd,
                new Generator
                {
                    NamespaceProvider = new NamespaceProvider
                    {
                        GenerateNamespace = key => "Test",
                    },
                    GenerateNullables = true,
                    NamingScheme = NamingScheme.PascalCase,
                }).First();

            for (var i = totalDigitsRangeFrom; i <= totalDigitsRangeTo; i++)
            {
                Assert.Contains($"public {expectedTypeName} RestrictedInteger{i}", generatedType);
            }
        }
    }
}
