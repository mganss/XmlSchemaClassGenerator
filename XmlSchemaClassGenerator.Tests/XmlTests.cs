using System;
using System.CodeDom;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices.ComTypes;
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

        private IEnumerable<string> ConvertXml(string name, IEnumerable<string> xsds, Generator generatorPrototype = null)
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
                CodeTypeReferenceOptions = generatorPrototype.CodeTypeReferenceOptions,
                DoNotForceIsNullable = generatorPrototype.DoNotForceIsNullable
            };

            var set = new XmlSchemaSet();

            foreach (var xsd in xsds)
            {
                using var stringReader = new StringReader(xsd);
                var schema = XmlSchema.Read(stringReader, (s, e) =>
                {
                    throw new InvalidOperationException($"{e.Severity}: {e.Message}",e.Exception);
                });

                set.Add(schema);
            }

            gen.Generate(set);

            return writer.Content;
        }

        private IEnumerable<string> ConvertXml(string name, string xsd, Generator generatorPrototype = null)
        {
            return ConvertXml(name, new[] {xsd}, generatorPrototype);
        }

        const string IS24Pattern = @"xsd\is24\*\*.xsd";
        const string IS24ImmoTransferPattern = @"xsd\is24immotransfer\is24immotransfer.xsd";
        const string WadlPattern = @"xsd\wadl\*.xsd";
        const string ListPattern = @"xsd\list\list.xsd";
        const string SimplePattern = @"xsd\simple\*.xsd";
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

        [Fact]
        public void TestListWithPrivatePropertySetters()
        {
            var assembly = Compiler.Generate("List", ListPattern, new Generator() {
                GenerateNullables = true,
                IntegerDataType = typeof(int),
                DataAnnotationMode = DataAnnotationMode.All,
                GenerateDesignerCategoryAttribute = false,
                GenerateComplexTypesForCollections = true,
                EntityFramework = false,
                GenerateInterfaces = true,
                NamespacePrefix = "List",
                GenerateDescriptionAttribute = true,
                TextValuePropertyName = "Value",
                CollectionSettersMode = CollectionSettersMode.Private
            });
            Assert.NotNull(assembly);
            var myClassType = assembly.GetType("List.MyClass");
            Assert.NotNull(myClassType);
            var iListType = typeof(Collection<>);
            var collectionPropertyInfos = myClassType.GetProperties().Where(p => p.PropertyType.IsGenericType && iListType.IsAssignableFrom(p.PropertyType.GetGenericTypeDefinition())).OrderBy(p=>p.Name).ToList();
            var publicCollectionPropertyInfos = collectionPropertyInfos.Where(p => p.SetMethod.IsPrivate).OrderBy(p=>p.Name).ToList();
            Assert.True(collectionPropertyInfos.Count > 0);
            Assert.Equal(collectionPropertyInfos, publicCollectionPropertyInfos);

            var myClassInstance = Activator.CreateInstance(myClassType);
            foreach (var collectionPropertyInfo in publicCollectionPropertyInfos)
            {
                Assert.NotNull(collectionPropertyInfo.GetValue(myClassInstance));
            }
        }

        [Fact]
        public void TestListWithPublicPropertySetters()
        {
            var assembly = Compiler.Generate("ListPublic", ListPattern, new Generator {
                GenerateNullables = true,
                IntegerDataType = typeof(int),
                DataAnnotationMode = DataAnnotationMode.All,
                GenerateDesignerCategoryAttribute = false,
                GenerateComplexTypesForCollections = true,
                EntityFramework = false,
                GenerateInterfaces = true,
                NamespacePrefix = "List",
                GenerateDescriptionAttribute = true,
                TextValuePropertyName = "Value",
                CollectionSettersMode = CollectionSettersMode.Public
            });
            Assert.NotNull(assembly);
            var myClassType = assembly.GetType("List.MyClass");
            Assert.NotNull(myClassType);
            var iListType = typeof(Collection<>);
            var collectionPropertyInfos = myClassType.GetProperties().Where(p => p.PropertyType.IsGenericType && iListType.IsAssignableFrom(p.PropertyType.GetGenericTypeDefinition())).OrderBy(p=>p.Name).ToList();
            var publicCollectionPropertyInfos = collectionPropertyInfos.Where(p => p.SetMethod.IsPublic).OrderBy(p=>p.Name).ToList();
            Assert.True(collectionPropertyInfos.Count > 0);
            Assert.Equal(collectionPropertyInfos, publicCollectionPropertyInfos);

            var myClassInstance = Activator.CreateInstance(myClassType);
            foreach (var collectionPropertyInfo in publicCollectionPropertyInfos)
            {
                Assert.NotNull(collectionPropertyInfo.GetValue(myClassInstance));
            }
        }

        [Fact]
        public void TestListWithPublicPropertySettersWithoutConstructors()
        {
            var assembly = Compiler.Generate("ListPublicWithoutConstructorInitialization", ListPattern, new Generator {
                GenerateNullables = true,
                IntegerDataType = typeof(int),
                DataAnnotationMode = DataAnnotationMode.All,
                GenerateDesignerCategoryAttribute = false,
                GenerateComplexTypesForCollections = true,
                EntityFramework = false,
                GenerateInterfaces = true,
                NamespacePrefix = "List",
                GenerateDescriptionAttribute = true,
                TextValuePropertyName = "Value",
                CollectionSettersMode = CollectionSettersMode.PublicWithoutConstructorInitialization
            });
            Assert.NotNull(assembly);
            var myClassType = assembly.GetType("List.MyClass");
            Assert.NotNull(myClassType);
            var iListType = typeof(Collection<>);
            var collectionPropertyInfos = myClassType.GetProperties().Where(p => p.PropertyType.IsGenericType && iListType.IsAssignableFrom(p.PropertyType.GetGenericTypeDefinition())).OrderBy(p=>p.Name).ToList();
            var publicCollectionPropertyInfos = collectionPropertyInfos.Where(p => p.SetMethod.IsPublic).OrderBy(p=>p.Name).ToList();
            Assert.True(collectionPropertyInfos.Count > 0);
            Assert.Equal(collectionPropertyInfos, publicCollectionPropertyInfos);
            var myClassInstance = Activator.CreateInstance(myClassType);
            foreach (var collectionPropertyInfo in publicCollectionPropertyInfos)
            {
                Assert.Null(collectionPropertyInfo.GetValue(myClassInstance));
            }
        }

        [Fact]
        public void TestListWithPublicPropertySettersWithoutConstructorsSpecified()
        {
            var assembly = Compiler.Generate("ListPublicWithoutConstructorInitialization", ListPattern, new Generator
            {
                GenerateNullables = true,
                IntegerDataType = typeof(int),
                DataAnnotationMode = DataAnnotationMode.All,
                GenerateDesignerCategoryAttribute = false,
                GenerateComplexTypesForCollections = true,
                EntityFramework = false,
                GenerateInterfaces = true,
                NamespacePrefix = "List",
                GenerateDescriptionAttribute = true,
                TextValuePropertyName = "Value",
                CollectionSettersMode = CollectionSettersMode.PublicWithoutConstructorInitialization
            });
            Assert.NotNull(assembly);
            var myClassType = assembly.GetType("List.MyClass");
            Assert.NotNull(myClassType);
            var iListType = typeof(Collection<>);
            var collectionPropertyInfos = myClassType.GetProperties().Where(p => p.PropertyType.IsGenericType && iListType.IsAssignableFrom(p.PropertyType.GetGenericTypeDefinition())).OrderBy(p => p.Name).ToList();
            var publicCollectionPropertyInfos = collectionPropertyInfos.Where(p => p.SetMethod.IsPublic).OrderBy(p => p.Name).ToList();
            Assert.True(collectionPropertyInfos.Count > 0);
            Assert.Equal(collectionPropertyInfos, publicCollectionPropertyInfos);
            var myClassInstance = Activator.CreateInstance(myClassType);

            var propertyNamesWithSpecifiedPostfix = publicCollectionPropertyInfos.Select(p => p.Name + "Specified").ToHashSet();
            var propertiesWithSpecifiedPostfix =
                myClassType.GetProperties().Where(p => propertyNamesWithSpecifiedPostfix.Contains(p.Name)).ToList();

            //Null collection
            foreach (var propertyInfo in propertiesWithSpecifiedPostfix)
            {
                Assert.False((bool)propertyInfo.GetValue(myClassInstance));
            }

            foreach (var propertyInfo in publicCollectionPropertyInfos)
            {
                var collection = Activator.CreateInstance(propertyInfo.PropertyType);
                propertyInfo.SetValue(myClassInstance, collection);
            }

            //Not Null but empty collection
            foreach (var propertyInfo in propertiesWithSpecifiedPostfix)
            {
                Assert.False((bool)propertyInfo.GetValue(myClassInstance));
            }

            foreach (var propertyInfo in publicCollectionPropertyInfos)
            {

                var collection = Activator.CreateInstance(propertyInfo.PropertyType);
                propertyInfo.PropertyType.InvokeMember("Add", BindingFlags.Public | BindingFlags.InvokeMethod | BindingFlags.Instance, null, collection,
                    new object[] {null});
                propertyInfo.SetValue(myClassInstance, collection);
            }

            //Not Null and not empty collection
            foreach (var propertyInfo in propertiesWithSpecifiedPostfix)
            {
                Assert.True((bool)propertyInfo.GetValue(myClassInstance));
            }
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

            Compiler.Generate("IS24ImmoTransferSeparate", IS24ImmoTransferPattern, new Generator
            {
                SeparateSubstitutes = true
            });
            TestSamples("IS24ImmoTransferSeparate", IS24ImmoTransferPattern);
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
        public void TestSeparateClasses()
        {
            var output = new FileWatcherOutputWriter(Path.Combine("output", "Tableau.Separate"));
            Compiler.Generate("Tableau.Separate", TableauPattern,
                new Generator
                {
                    OutputWriter = output,
                    SeparateClasses = true,
                    EnableDataBinding = true
                });
            TestSamples("Tableau.Separate", TableauPattern);
        }

        [Fact, TestPriority(1)]
        [UseCulture("en-US")]
        public void TestArray()
        {
            var output = new FileWatcherOutputWriter(Path.Combine("output", "Tableau.Array"));
            Compiler.Generate("Tableau.Array", TableauPattern,
                new Generator
                {
                    OutputWriter = output,
                    EnableDataBinding = true,
                    CollectionType = typeof(System.Array),
                    CollectionSettersMode = CollectionSettersMode.Public
                });
            TestSamples("Tableau.Array", TableauPattern);
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

        private bool HandleValidationError(string[] xmlLines, ValidationEventArgs e)
        {
            var line = xmlLines[e.Exception.LineNumber - 1].Substring(e.Exception.LinePosition - 1);
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
            var sb = new StringBuilder();
            foreach (var rootElement in set.GlobalElements.Values.Cast<XmlSchemaElement>().Where(e => !e.IsAbstract && !(e.ElementSchemaType is XmlSchemaSimpleType)))
            {
                var type = FindType(assembly, rootElement.QualifiedName);
                var serializer = new XmlSerializer(type);
                var generator = new XmlSampleGenerator(set, rootElement.QualifiedName);

                using var xw = XmlWriter.Create(sb, new XmlWriterSettings { Indent = true });

                // generate sample xml
                generator.WriteXml(xw);
                var xml = sb.ToString();
                sb.Clear();
                File.WriteAllText("xml.xml", xml);

                // validate serialized xml
                var settings = new XmlReaderSettings
                {
                    ValidationType = ValidationType.Schema,
                    Schemas = set
                };

                var invalid = false;
                var xmlLines = xml.Split('\n');
                void validate(object s, ValidationEventArgs e)
                {
                    if (HandleValidationError(xmlLines, e)) invalid = true;
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
                xmlLines = xml2.Split('\n');
                void validate2(object s, ValidationEventArgs e)
                {
                    if (HandleValidationError(xmlLines, e)) throw e.Exception;
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
            PerformBpmnTest("Bpmn");
            PerformBpmnTest("BpmnSeparate", new Generator
            {
                SeparateSubstitutes = true
            });
        }

        private void PerformBpmnTest(string name, Generator generator = null)
        {
            var assembly = Compiler.Generate(name, BpmnPattern, generator);
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
        [InlineData((CodeTypeReferenceOptions)0, "[System.ComponentModel.EditorBrowsableAttribute(global::System.ComponentModel.EditorBrowsableState.Never)]")]
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
            Assert.Contains(@"public enum MyType2", generatedType);
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

        [Fact]
        public void EnumWithNonUniqueEntriesTest()
        {
            const string xsd = @"<?xml version=""1.0"" encoding=""UTF-8""?>
            <xs:schema xmlns:xs=""http://www.w3.org/2001/XMLSchema"" elementFormDefault=""qualified"" attributeFormDefault=""unqualified"">
				<xs:simpleType name=""TestEnum"">
					<xs:restriction base=""xs:string"">
					    <xs:enumeration value=""test_case""/>
					    <xs:enumeration value=""test_Case""/>
					    <xs:enumeration value=""Test_case""/>
					    <xs:enumeration value=""Test_Case""/>
					</xs:restriction>
				</xs:simpleType>
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
            var contents = ConvertXml(nameof(EnumWithNonUniqueEntriesTest), xsd, generator);
            var content = Assert.Single(contents);

            var assembly = Compiler.Compile(nameof(EnumWithNonUniqueEntriesTest), content);
            var durationEnumType = assembly.GetType("Test.TestEnum");
            Assert.NotNull(durationEnumType);

            var expectedEnumValues = new[] {"Test_Case", "Test_Case1", "Test_Case2", "Test_Case3"};
            var enumValues = durationEnumType.GetEnumNames().OrderBy(n => n).ToList();
            Assert.Equal(expectedEnumValues, enumValues);

            var mEnumValue = durationEnumType.GetMembers().First(mi => mi.Name == "Test_Case1");
            var xmlEnumAttribute = mEnumValue.GetCustomAttributes<XmlEnumAttribute>().FirstOrDefault();
            Assert.NotNull(xmlEnumAttribute);
            Assert.Equal("test_Case", xmlEnumAttribute.Name);
        }

        [Fact]
        public void RenameInterfacePropertyInDerivedClassTest()
        {
            const string xsd = @"<?xml version=""1.0"" encoding=""UTF-8""?>
            <xs:schema xmlns:xs=""http://www.w3.org/2001/XMLSchema""
                elementFormDefault=""qualified"" attributeFormDefault=""unqualified"">

                <xs:complexType name=""ClassItemBase"">
			        <xs:sequence>
                        <xs:group ref=""Level1""/>
                    </xs:sequence>
		        </xs:complexType>

	            <xs:element name=""ClassItem"">
		            <xs:complexType>
			            <xs:complexContent>
                            <xs:extension base=""ClassItemBase""/>
		                </xs:complexContent>
		            </xs:complexType>
                </xs:element>

                <xs:element name=""SomeType1"">
		            <xs:complexType>
			            <xs:group ref=""Level1""/>
		            </xs:complexType>
                </xs:element>

	            <xs:group name=""Level1"">
		            <xs:choice>
                        <xs:group ref=""Level2""/>
		            </xs:choice>
	            </xs:group>

	            <xs:group name=""Level2"">
		            <xs:choice>
			            <xs:group ref=""Level3""/>
		            </xs:choice>
	            </xs:group>

	            <xs:group name=""Level3"">
		            <xs:choice>
			            <xs:element name=""ClassItemBase"" type=""xs:string""/>
		            </xs:choice>
	            </xs:group>

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
            var contents = ConvertXml(nameof(RenameInterfacePropertyInDerivedClassTest), xsd, generator);
            var content = Assert.Single(contents);

            var assembly = Compiler.Compile(nameof(RenameInterfacePropertyInDerivedClassTest), content);
            var classType = assembly.GetType("Test.ClassItem");
            Assert.NotNull(classType);
            Assert.Single(classType.GetProperties());
            Assert.Equal("ClassItemBaseProperty", classType.GetProperties().First().Name);

            var level1Interface = assembly.GetType("Test.ILevel1");
            Assert.NotNull(level1Interface);
            Assert.Empty(level1Interface.GetProperties());

            var level2Interface = assembly.GetType("Test.ILevel1");
            Assert.NotNull(level2Interface);
            Assert.Empty(level2Interface.GetProperties());

            var level3Interface = assembly.GetType("Test.ILevel3");
            Assert.NotNull(level3Interface);
            Assert.Single(level3Interface.GetProperties());
            Assert.Equal("ClassItemBaseProperty", level3Interface.GetProperties().First().Name);
        }

        [Fact]
        public void DoNotGenerateSamePropertiesInDerivedInterfacesClassTest()
        {
            const string xsd = @"<?xml version=""1.0"" encoding=""UTF-8""?>
            <xs:schema xmlns:xs=""http://www.w3.org/2001/XMLSchema""
                elementFormDefault=""qualified"" attributeFormDefault=""unqualified"">

	            <xs:element name=""ParentClass"">
		            <xs:complexType>
			            <xs:group ref=""Level1""/>
		            </xs:complexType>
                </xs:element>

	            <xs:group name=""Level1"">
		            <xs:choice>
                    <xs:sequence>
                        <xs:element name=""InterfaceProperty"" type=""xs:string""/>
                        <xs:group ref=""Level2""/>
                    </xs:sequence>
		            </xs:choice>
	            </xs:group>

	            <xs:group name=""Level2"">
                    <xs:sequence>
                        <xs:element name=""InterfaceProperty"" type=""xs:string""/>
                        <xs:group ref=""Level3""/>
                    </xs:sequence>
	            </xs:group>

	            <xs:group name=""Level22"">
                    <xs:sequence>
                        <xs:element name=""InterfaceProperty"" type=""xs:string""/>
                        <xs:element name=""Level22OwnProperty"" type=""xs:string""/>
                        <xs:group ref=""Level3""/>
                    </xs:sequence>
	            </xs:group>

	            <xs:group name=""Level3"">
		            <xs:choice>
			            <xs:element name=""InterfaceProperty"" type=""xs:string""/>
		            </xs:choice>
	            </xs:group>

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
            var contents = ConvertXml(nameof(DoNotGenerateSamePropertiesInDerivedInterfacesClassTest), xsd, generator);
            var content = Assert.Single(contents);

            var assembly = Compiler.Compile(nameof(DoNotGenerateSamePropertiesInDerivedInterfacesClassTest), content);

            var listType = assembly.GetType("Test.ParentClass");
            Assert.NotNull(listType);

            var listTypePropertyInfo = listType.GetProperties().FirstOrDefault(p => p.Name == "InterfaceProperty");
            Assert.NotNull(listTypePropertyInfo);

            var level1Interface = assembly.GetType("Test.ILevel1");
            Assert.NotNull(level1Interface);
            Assert.Empty(level1Interface.GetProperties());

            var level2Interface = assembly.GetType("Test.ILevel2");
            Assert.NotNull(level2Interface);
            Assert.Empty(level2Interface.GetProperties());

            var level3Interface = assembly.GetType("Test.ILevel3");
            Assert.NotNull(level3Interface);
            var level3InterfacePropertyInfo = level3Interface.GetProperties().FirstOrDefault(p => p.Name == "InterfaceProperty");
            Assert.NotNull(level3InterfacePropertyInfo);

        }

        [Fact]
        public void NillableWithDefaultValueTest()
        {
            const string xsd = @"<?xml version=""1.0"" encoding=""UTF-8""?>
            <xs:schema xmlns:xs=""http://www.w3.org/2001/XMLSchema""
                elementFormDefault=""qualified"" attributeFormDefault=""unqualified"">

                <xs:complexType name=""TestType"">
                    <xs:sequence>
                        <xs:element name=""IntProperty"" type=""xs:int"" nillable=""true"" default=""9000"" />
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
            var contents = ConvertXml(nameof(NillableWithDefaultValueTest), xsd, generator);
            var content = Assert.Single(contents);

            var assembly = Compiler.Compile(nameof(NillableWithDefaultValueTest), content);

            var testType = assembly.GetType("Test.TestType");
            Assert.NotNull(testType);

            var propertyInfo = testType.GetProperties().FirstOrDefault(p => p.Name == "IntProperty");
            Assert.NotNull(propertyInfo);
            var testTypeInstance = Activator.CreateInstance(testType);
            var propertyDefaultValue = propertyInfo.GetValue(testTypeInstance);
            Assert.IsType<int>(propertyDefaultValue);
            Assert.Equal(9000, propertyDefaultValue);

            propertyInfo.SetValue(testTypeInstance, null);
            var serializer = new XmlSerializer(testType);

            var serializedXml = Serialize(serializer, testTypeInstance);
            Assert.Contains(
                @":nil=""true""",
                serializedXml);
        }

        [Fact]
        public void GenerateXmlRootAttributeForEnumTest()
        {
            const string xsd = @"<?xml version=""1.0"" encoding=""UTF-8""?>
            <xs:schema xmlns:xs=""http://www.w3.org/2001/XMLSchema"" targetNamespace=""http://test.namespace""
                elementFormDefault=""qualified"" attributeFormDefault=""unqualified"">

		        <xs:element name=""EnumTestType"">
		            <xs:simpleType>
					    <xs:restriction base=""xs:string"">
						    <xs:enumeration value=""EnumValue""/>
					    </xs:restriction>
				    </xs:simpleType>
	            </xs:element>

            </xs:schema>";

            var generator = new Generator
            {
                NamespaceProvider = new NamespaceProvider
                {
                    GenerateNamespace = key => "Test"
                }
            };
            var contents = ConvertXml(nameof(GenerateXmlRootAttributeForEnumTest), xsd, generator);
            var content = Assert.Single(contents);

            var assembly = Compiler.Compile(nameof(GenerateXmlRootAttributeForEnumTest), content);

            var testType = assembly.GetType("Test.EnumTestType");
            Assert.NotNull(testType);
            var xmlRootAttribute = testType.GetCustomAttributes<XmlRootAttribute>().FirstOrDefault();
            Assert.NotNull(xmlRootAttribute);
            Assert.Equal("EnumTestType", xmlRootAttribute.ElementName);
            Assert.Equal("http://test.namespace", xmlRootAttribute.Namespace);
        }

        [Fact]
        public void AmbiguousTypesTest()
        {
            const string xsd1 = @"<?xml version=""1.0"" encoding=""UTF-8""?>
            <xs:schema xmlns:xs=""http://www.w3.org/2001/XMLSchema"" targetNamespace=""Test_NS1""
                elementFormDefault=""qualified"" attributeFormDefault=""unqualified"">

		        <xs:element name=""EnumTestType"">
		            <xs:simpleType>
					    <xs:restriction base=""xs:string"">
						    <xs:enumeration value=""EnumValue""/>
					    </xs:restriction>
				    </xs:simpleType>
	            </xs:element>

            </xs:schema>";
            const string xsd2 = @"<?xml version=""1.0"" encoding=""UTF-8""?>
            <xs:schema xmlns:xs=""http://www.w3.org/2001/XMLSchema"" targetNamespace=""Test_NS2""
                elementFormDefault=""qualified"" attributeFormDefault=""unqualified"">

		        <xs:element name=""EnumTestType"">
		            <xs:simpleType>
					    <xs:restriction base=""xs:string"">
						    <xs:enumeration value=""EnumValue""/>
					    </xs:restriction>
				    </xs:simpleType>
	            </xs:element>

            </xs:schema>";
            var generator = new Generator
            {
                NamespaceProvider = new NamespaceProvider
                {
                    GenerateNamespace = key =>key.XmlSchemaNamespace
                }
            };
            var contents1 = ConvertXml(nameof(GenerateXmlRootAttributeForEnumTest), xsd1, generator);
            var contents2 = ConvertXml(nameof(GenerateXmlRootAttributeForEnumTest), xsd2, generator);
            var content1 = Assert.Single(contents1);
            var content2 = Assert.Single(contents2);

            var assembly = Compiler.Compile(nameof(GenerateXmlRootAttributeForEnumTest), content1, content2);

            var testType = assembly.GetType("Test_NS1.EnumTestType");
            Assert.NotNull(testType);
            var xmlRootAttribute = testType.GetCustomAttributes<XmlRootAttribute>().FirstOrDefault();
            Assert.NotNull(xmlRootAttribute);
            Assert.Equal("EnumTestType", xmlRootAttribute.ElementName);
            Assert.Equal("Test_NS1", xmlRootAttribute.Namespace);

            testType = assembly.GetType("Test_NS2.EnumTestType");
            Assert.NotNull(testType);
            xmlRootAttribute = testType.GetCustomAttributes<XmlRootAttribute>().FirstOrDefault();
            Assert.NotNull(xmlRootAttribute);
            Assert.Equal("EnumTestType", xmlRootAttribute.ElementName);
            Assert.Equal("Test_NS2", xmlRootAttribute.Namespace);
        }

        [Fact]
        public void AmbiguousAnonymousTypesTest()
        {
            const string xsd1 = @"<?xml version=""1.0"" encoding=""UTF-8""?>
            <xs:schema xmlns:xs=""http://www.w3.org/2001/XMLSchema"" targetNamespace=""Test_NS1""
                elementFormDefault=""qualified"" attributeFormDefault=""unqualified"">

	            <xs:complexType name=""TestType"">
		            <xs:sequence>
			            <xs:element name=""Property"">
				            <xs:simpleType>
					            <xs:restriction base=""xs:string"">
						            <xs:enumeration value=""EnumValue""/>
					            </xs:restriction>
				            </xs:simpleType>
			            </xs:element>
					</xs:sequence>
	            </xs:complexType>

	            <xs:complexType name=""TestType2"">
		            <xs:sequence>
			            <xs:element name=""Property"">
				            <xs:complexType>
					            <xs:sequence>
			                        <xs:element name=""Property"" type=""xs:string""/>
                                </xs:sequence>
				            </xs:complexType>
			            </xs:element>
					</xs:sequence>
	            </xs:complexType>

            </xs:schema>";
            const string xsd2 = @"<?xml version=""1.0"" encoding=""UTF-8""?>
            <xs:schema xmlns:xs=""http://www.w3.org/2001/XMLSchema"" targetNamespace=""Test_NS2""
                elementFormDefault=""qualified"" attributeFormDefault=""unqualified"">

	            <xs:complexType name=""TestType"">
		            <xs:sequence>
			            <xs:element name=""Property"">
				            <xs:simpleType>
					            <xs:restriction base=""xs:string"">
						            <xs:enumeration value=""EnumValue""/>
					            </xs:restriction>
				            </xs:simpleType>
			            </xs:element>
					</xs:sequence>
	            </xs:complexType>

	            <xs:complexType name=""TestType2"">
		            <xs:sequence>
			            <xs:element name=""Property"">
				            <xs:complexType>
					            <xs:sequence>
			                        <xs:element name=""Property"" type=""xs:string""/>
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
                    GenerateNamespace = key =>key.XmlSchemaNamespace
                }
            };
            var contents = ConvertXml(nameof(GenerateXmlRootAttributeForEnumTest), new[]{xsd1, xsd2}, generator).ToArray();
            Assert.Equal(2, contents.Length);

            var assembly = Compiler.Compile(nameof(GenerateXmlRootAttributeForEnumTest), contents);

            var testType = assembly.GetType("Test_NS1.TestType");
            Assert.NotNull(testType);
            var xmlRootAttribute = testType.GetCustomAttributes<XmlRootAttribute>().FirstOrDefault();
            Assert.NotNull(xmlRootAttribute);
            Assert.Equal("TestType", xmlRootAttribute.ElementName);
            Assert.Equal("Test_NS1", xmlRootAttribute.Namespace);
            testType = assembly.GetType("Test_NS1.TestTypeProperty");
            Assert.NotNull(testType);
            xmlRootAttribute = testType.GetCustomAttributes<XmlRootAttribute>().FirstOrDefault();
            Assert.NotNull(xmlRootAttribute);
            Assert.Equal("TestTypeProperty", xmlRootAttribute.ElementName);
            Assert.Equal("Test_NS1", xmlRootAttribute.Namespace);
            testType = assembly.GetType("Test_NS1.TestType2Property");
            Assert.NotNull(testType);
            xmlRootAttribute = testType.GetCustomAttributes<XmlRootAttribute>().FirstOrDefault();
            Assert.NotNull(xmlRootAttribute);
            Assert.Equal("TestType2Property", xmlRootAttribute.ElementName);
            Assert.Equal("Test_NS1", xmlRootAttribute.Namespace);


            testType = assembly.GetType("Test_NS2.TestType");
            Assert.NotNull(testType);
            xmlRootAttribute = testType.GetCustomAttributes<XmlRootAttribute>().FirstOrDefault();
            Assert.NotNull(xmlRootAttribute);
            Assert.Equal("TestType", xmlRootAttribute.ElementName);
            Assert.Equal("Test_NS2", xmlRootAttribute.Namespace);
            testType = assembly.GetType("Test_NS2.TestTypeProperty");
            Assert.NotNull(testType);
            xmlRootAttribute = testType.GetCustomAttributes<XmlRootAttribute>().FirstOrDefault();
            Assert.NotNull(xmlRootAttribute);
            Assert.Equal("TestTypeProperty", xmlRootAttribute.ElementName);
            Assert.Equal("Test_NS2", xmlRootAttribute.Namespace);
            testType = assembly.GetType("Test_NS2.TestType2Property");
            Assert.NotNull(testType);
            xmlRootAttribute = testType.GetCustomAttributes<XmlRootAttribute>().FirstOrDefault();
            Assert.NotNull(xmlRootAttribute);
            Assert.Equal("TestType2Property", xmlRootAttribute.ElementName);
            Assert.Equal("Test_NS2", xmlRootAttribute.Namespace);
        }

        [Fact]
        public void TestShouldPatternForCollections()
        {
            const string xsd = @"<?xml version=""1.0"" encoding=""UTF-8""?>
            <xs:schema xmlns:xs=""http://www.w3.org/2001/XMLSchema"" targetNamespace=""Test_NS1""
            elementFormDefault=""qualified"" attributeFormDefault=""unqualified"">
            <xs:element name=""TestType"">
				<xs:complexType>
					<xs:choice maxOccurs=""unbounded"">
						<xs:element name=""DateValue"" type=""xs:dateTime"" nillable=""true""/>
					</xs:choice>
				</xs:complexType>
            </xs:element>
            <xs:element name=""TestType2"">
				<xs:complexType>
					<xs:choice maxOccurs=""unbounded"">
						<xs:element name=""StringValue"" type=""xs:string"" nillable=""true""/>
					</xs:choice>
				</xs:complexType>
            </xs:element>
            </xs:schema>";

            var generator = new Generator
            {
                NamespaceProvider = new NamespaceProvider
                {
                    GenerateNamespace = key => key.XmlSchemaNamespace
                }
            };

            var contents = ConvertXml(nameof(TestShouldPatternForCollections), xsd, generator).ToArray();
            Assert.Single(contents);
            var assembly = Compiler.Compile(nameof(TestShouldPatternForCollections), contents);
            var testType = assembly.GetType("Test_NS1.TestType");
            var serializer = new XmlSerializer(testType);
            Assert.NotNull(serializer);
        }


        [Fact]
        public void TestDoNotForceIsNullableGeneration()
        {
            const string xsd = @"<?xml version=""1.0"" encoding=""UTF-8""?>
            <xs:schema xmlns:xs=""http://www.w3.org/2001/XMLSchema"" targetNamespace=""Test_NS1""
            elementFormDefault=""qualified"" attributeFormDefault=""unqualified"">
            <xs:element name=""TestType"">
				<xs:complexType>
					<xs:sequence>
						<xs:element name=""StringProperty"" type=""xs:string"" nillable=""true"" minOccurs=""0""/>
						<xs:element name=""StringNullableProperty"" type=""xs:string"" nillable=""true""/>
						<xs:element name=""StringNullableProperty2"" type=""xs:string"" nillable=""true"" minOccurs=""1""/>
					</xs:sequence>
				</xs:complexType>
            </xs:element>
            </xs:schema>";

            var generator = new Generator
            {
                NamespaceProvider = new NamespaceProvider
                {
                    GenerateNamespace = key => key.XmlSchemaNamespace
                },
                DoNotForceIsNullable = true
            };

            var contents = ConvertXml(nameof(TestDoNotForceIsNullableGeneration), xsd, generator).ToArray();
            Assert.Single(contents);
            var assembly = Compiler.Compile(nameof(TestDoNotForceIsNullableGeneration), contents);
            var testType = assembly.GetType("Test_NS1.TestType");
            var serializer = new XmlSerializer(testType);
            Assert.NotNull(serializer);

            var prop = testType.GetProperty("StringProperty");
            Assert.NotNull(prop);
            var xmlElementAttribute = prop.GetCustomAttribute<XmlElementAttribute>();
            Assert.False(xmlElementAttribute.IsNullable);

            prop = testType.GetProperty("StringNullableProperty");
            Assert.NotNull(prop);
            var xmlElementNullableAttribute = prop.GetCustomAttribute<XmlElementAttribute>();
            Assert.True(xmlElementNullableAttribute.IsNullable);

            prop = testType.GetProperty("StringNullableProperty2");
            Assert.NotNull(prop);
            var xmlElementNullableAttribute2 = prop.GetCustomAttribute<XmlElementAttribute>();
            Assert.True(xmlElementNullableAttribute2.IsNullable);
        }

        [Fact]
        public void TestForceIsNullableGeneration()
        {
            const string xsd = @"<?xml version=""1.0"" encoding=""UTF-8""?>
            <xs:schema xmlns:xs=""http://www.w3.org/2001/XMLSchema"" targetNamespace=""Test_NS1""
            elementFormDefault=""qualified"" attributeFormDefault=""unqualified"">
            <xs:element name=""TestType"">
				<xs:complexType>
					<xs:sequence>
						<xs:element name=""StringProperty"" type=""xs:string"" nillable=""true"" minOccurs=""0""/>
						<xs:element name=""StringNullableProperty"" type=""xs:string"" nillable=""true""/>
						<xs:element name=""StringNullableProperty2"" type=""xs:string"" nillable=""true"" minOccurs=""1""/>
					</xs:sequence>
				</xs:complexType>
            </xs:element>
            </xs:schema>";

            var generator = new Generator
            {
                NamespaceProvider = new NamespaceProvider
                {
                    GenerateNamespace = key => key.XmlSchemaNamespace
                },
                DoNotForceIsNullable = false
            };

            var contents = ConvertXml(nameof(TestForceIsNullableGeneration), xsd, generator).ToArray();
            Assert.Single(contents);
            var assembly = Compiler.Compile(nameof(TestForceIsNullableGeneration), contents);
            var testType = assembly.GetType("Test_NS1.TestType");
            var serializer = new XmlSerializer(testType);
            Assert.NotNull(serializer);

            var prop = testType.GetProperty("StringProperty");
            Assert.NotNull(prop);
            var xmlElementAttribute = prop.GetCustomAttribute<XmlElementAttribute>();
            Assert.True(xmlElementAttribute.IsNullable);

            prop = testType.GetProperty("StringNullableProperty");
            Assert.NotNull(prop);
            var xmlElementNullableAttribute = prop.GetCustomAttribute<XmlElementAttribute>();
            Assert.True(xmlElementNullableAttribute.IsNullable);

            prop = testType.GetProperty("StringNullableProperty2");
            Assert.NotNull(prop);
            var xmlElementNullableAttribute2 = prop.GetCustomAttribute<XmlElementAttribute>();
            Assert.True(xmlElementNullableAttribute2.IsNullable);
        }

        [Fact]
        public void TestArrayOfMsTypeGeneration()
        {
            // see https://github.com/mganss/XmlSchemaClassGenerator/issues/214

            var xsd0 =
                @"<xs:schema xmlns:xs=""http://www.w3.org/2001/XMLSchema"" xmlns:tns=""http://schemas.microsoft.com/2003/10/Serialization/Arrays"" targetNamespace=""http://schemas.microsoft.com/2003/10/Serialization/Arrays"" elementFormDefault=""qualified"">
            <xs:complexType name=""ArrayOfstring"">
                <xs:sequence>
	                <xs:element name=""string"" type=""xs:string"" nillable=""true"" minOccurs=""0"" maxOccurs=""unbounded""/>
                </xs:sequence>
            </xs:complexType>
            <xs:element name=""ArrayOfstring"" type=""tns:ArrayOfstring"" nillable=""true""/>
        </xs:schema>
        ";
            var xsd1 =
                @"<xs:schema xmlns:xs=""http://www.w3.org/2001/XMLSchema"" xmlns:q1=""http://schemas.microsoft.com/2003/10/Serialization/Arrays"" elementFormDefault=""qualified"">
            <xs:import namespace=""http://schemas.microsoft.com/2003/10/Serialization/Arrays""/>
            <xs:complexType name=""c_ai"">
                <xs:sequence>
	                <xs:element name=""d"" type=""q1:ArrayOfstring"" nillable=""true"" minOccurs=""0"">
		                <xs:annotation>
			                <xs:appinfo>
				                <DefaultValue EmitDefaultValue=""false"" xmlns=""http://schemas.microsoft.com/2003/10/Serialization/""/>
			                </xs:appinfo>
		                </xs:annotation>
	                </xs:element>
                </xs:sequence>
            </xs:complexType>
            <xs:element name=""c_ai"" type=""c_ai"" nillable=""true""/>
        </xs:schema>
        ";
            var validXml =
                @"<c_ai xmlns:tns=""http://schemas.microsoft.com/2003/10/Serialization/Arrays"">
            <d>
                <tns:string>String</tns:string>
                <tns:string>String</tns:string>
                <tns:string>String</tns:string>
            </d>
        </c_ai>
        ";
            var generator = new Generator
            {
                IntegerDataType = typeof(int),
                NamespacePrefix = "Test_NS1",
                GenerateNullables = true,
                CollectionType = typeof(System.Collections.Generic.List<>)
            };
            var contents = ConvertXml(nameof(TestArrayOfMsTypeGeneration), new[] { xsd0, xsd1 }, generator).ToArray();
            var assembly = Compiler.Compile(nameof(TestForceIsNullableGeneration), contents);
            var testType = assembly.GetType("Test_NS1.C_Ai");
            var serializer = new XmlSerializer(testType);
            Assert.NotNull(serializer);
            dynamic deserialized = serializer.Deserialize(new StringReader(validXml));
            //Assert.NotEmpty((System.Collections.IEnumerable)deserialized.D);  //<== oops
        }

        [Fact, TestPriority(1)]
        public void AirspaceServicesTest1()
        {
            var outputPath = Path.Combine("output", "aixm");

            string xlink = "http://www.w3.org/1999/xlink";
            string gml3 = "http://www.opengis.net/gml/3.2";
            string gts = "http://www.isotc211.org/2005/gts";
            string gss = "http://www.isotc211.org/2005/gss";
            string gsr = "http://www.isotc211.org/2005/gsr";
            string gmd = "http://www.isotc211.org/2005/gmd";
            string gco = "http://www.isotc211.org/2005/gco";

            string fixmBase = "http://www.fixm.aero/base/4.1";
            string fixmFlight = "http://www.fixm.aero/flight/4.1";
            string fixmNm = "http://www.fixm.aero/nm/1.2";
            string fixmMessaging = "http://www.fixm.aero/messaging/4.1";

            string adr = "http://www.aixm.aero/schema/5.1.1/extensions/EUR/ADR";
            string aixmV511 = "http://www.aixm.aero/schema/5.1.1";

            string adrmessage = "http://www.eurocontrol.int/cfmu/b2b/ADRMessage";

            var _xsdToCsharpNsMap = new Dictionary<NamespaceKey, string>
            {
                { new NamespaceKey(), "other" },
                { new NamespaceKey(xlink), "org.w3._1999.xlink" },
                { new NamespaceKey(gts), "org.isotc211._2005.gts" },
                { new NamespaceKey(gss), "org.isotc211._2005.gss" },
                { new NamespaceKey(gsr), "org.isotc211._2005.gsr" },
                { new NamespaceKey(gmd), "org.isotc211._2005.gmd" },
                { new NamespaceKey(gco), "org.isotc211._2005.gco" },
                { new NamespaceKey(gml3), "net.opengis.gml._3" },
                { new NamespaceKey(aixmV511), "aero.aixm.v5_1_1" },
                { new NamespaceKey(fixmNm), "aero.fixm.v4_1_0.nm.v1_2" },
                { new NamespaceKey(fixmMessaging), "aero.fixm.v4_1_0.messaging" },
                { new NamespaceKey(fixmFlight), "aero.fixm.v4_1_0.flight" },
                { new NamespaceKey(fixmBase), "aero.fixm.v4_1_0.base" },
                { new NamespaceKey(adr), "aero.aixm.schema._5_1_1.extensions.eur.adr" },
                { new NamespaceKey(adrmessage), "_int.eurocontrol.cfmu.b2b.adrmessage" }
            };

            var gen = new Generator
            {
                OutputFolder = outputPath,
                NamespaceProvider = _xsdToCsharpNsMap.ToNamespaceProvider(),
                CollectionSettersMode = CollectionSettersMode.Public
            };
            var xsdFiles = new[]
            {
                    "AIXM_AbstractGML_ObjectTypes.xsd",
                    "AIXM_DataTypes.xsd",
                    "AIXM_Features.xsd",
                    "extensions\\ADR-23.5.0\\ADR_DataTypes.xsd",
                    "extensions\\ADR-23.5.0\\ADR_Features.xsd",
                    "message\\ADR_Message.xsd",
                    "message\\AIXM_BasicMessage.xsd",
            }.Select(x => Path.Combine(Directory.GetCurrentDirectory(), "xsd", "aixm", "aixm-5.1.1", x)).ToList();

            var assembly = Compiler.GenerateFiles("Aixm", xsdFiles, gen);
            Assert.NotNull(assembly);

            /*
            var testFiles = new Dictionary<string, string>
            {
                { "airport1.xml", "AirportHeliportType" },
                { "airportHeliportTimeSlice.xml", "AirportHeliportTimeSliceType" },
                { "airspace1.xml", "AirspaceType" },
                { "navaid1.xml", "NavaidType" },
                { "navaidTimeSlice.xml", "NavaidTimeSliceType" },
                { "navaidWithAbstractTime.xml", "NavaidWithAbstractTime" },
                { "navaid.xml", "Navaid" },
                { "routesegment1.xml", "RouteSegment" },
                { "timePeriod.xml", "TimePeriod" },
            };

            foreach (var testFile in testFiles)
            {
                var type = assembly.GetTypes().SingleOrDefault(t => t.Name == testFile.Value);
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

                var xmlString = File.ReadAllText($"xml/aixm_tests/{testFile.Key}");
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
            */
        }
    }
}
