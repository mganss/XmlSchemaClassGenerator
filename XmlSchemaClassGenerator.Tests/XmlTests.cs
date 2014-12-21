using IS24RestApi.Common;
using IS24RestApi.Offer.Listelement;
using IS24RestApi.Offer.Realestates;
using IS24RestApi.Search.Searcher;
using Microsoft.Xml.XMLGen;
using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Schema;
using System.Xml.Serialization;
using Xunit;

namespace XmlSchemaClassGenerator.Tests
{
    public class XmlTests
    {
        public XmlTests()
        {
            RestrictionModel.DataAnnotationMode = DataAnnotationMode.None;
            TypeModel.GenerateSerializableAttribute = true;
            ClassModel.GenerateDesignerCategoryAttribute = false;
            ClassModel.GenerateSerializableAttribute = true;
            PropertyModel.GenerateNullables = true;
            SimpleModel.CollectionType = typeof(Collection<>);
            SimpleModel.CollectionImplementationType = typeof(Collection<>);
            SimpleModel.GenerateSerializableAttribute = true;
            SimpleModel.IntegerDataType = typeof(int);
        }

        private Assembly Compile(string name, string pattern)
        {
            var cs = new List<string>();
            var outputFolder = Path.Combine("output", name);
            Directory.CreateDirectory(outputFolder);
            var gen = new Generator
            {
                OutputFolder = outputFolder,
                NamespaceProvider = new NamespaceProvider
                {
                    GenerateNamespace = key =>
                    {
                        var xn = key.XmlSchemaNamespace;
                        var nm = string.Join(".", xn.Split('/').Where(p => Regex.IsMatch(p, @"^[A-Za-z]+$") && p != "schema")
                            .Select(n => Generator.ToTitleCase(n, NamingScheme.PascalCase)));
                        return name + (string.IsNullOrEmpty(nm) ? "" : ("." + nm));
                    }
                },
                Log = f => cs.Add(f)
            };

            var files = Glob.Glob.ExpandNames(pattern);

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
            };

            var binFolder = Path.Combine(outputFolder, "bin");
            Directory.CreateDirectory(binFolder);
            var results = provider.CompileAssemblyFromFile(new CompilerParameters(assemblies, Path.Combine(binFolder, name + ".dll")), cs.ToArray());

            Assert.False(results.Errors.HasErrors);
            Assert.False(results.Errors.HasWarnings);
            Assert.NotNull(results.CompiledAssembly);

            var assembly = Assembly.Load(results.CompiledAssembly.GetName());

            return assembly;
        }

        [Fact]
        [UseCulture("en-US")]
        public void CanDeserializeSampleXml()
        {
            CompileXsdAndTestSamples("IS24RestApi", @"xsd\is24\*\*.xsd");
            CompileXsdAndTestSamples("Wadl", @"xsd\wadl\wadl.xsd");
        }

        private void CompileXsdAndTestSamples(string name, string pattern)
        {
            var assembly = Compile(name, pattern);
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

            foreach (var rootElement in set.GlobalElements.Values.Cast<XmlSchemaElement>())
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
                    XmlReaderSettings settings = new XmlReaderSettings();
                    settings.ValidationType = ValidationType.Schema;
                    settings.Schemas = set;
                    settings.ValidationEventHandler += (s, e) =>
                    {
                        // generator doesn't generate valid values where pattern restrictions exist, e.g. email
                        if (!e.Message.Contains("The Pattern constraint failed"))
                            Assert.True(false, e.Message);
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

        [Fact]
        public void ProducesSameXmlAsXsd()
        {
            TestCompareToXsd<ApartmentBuy, IS24RestApi.Xsd.ApartmentBuy>("apartmentBuy");
            TestCompareToXsd<ApartmentRent, IS24RestApi.Xsd.ApartmentRent>("apartmentRent");
            TestCompareToXsd<AssistedLiving, IS24RestApi.Xsd.AssistedLiving>("assistedLiving");
            TestCompareToXsd<CompulsoryAuction, IS24RestApi.Xsd.CompulsoryAuction>("compulsoryAuction");
            TestCompareToXsd<GarageBuy, IS24RestApi.Xsd.GarageBuy>("garageBuy");
            TestCompareToXsd<GarageRent, IS24RestApi.Xsd.GarageRent>("garageRent");
            TestCompareToXsd<Gastronomy, IS24RestApi.Xsd.Gastronomy>("gastronomy");
            TestCompareToXsd<HouseBuy, IS24RestApi.Xsd.HouseBuy>("houseBuy");
            TestCompareToXsd<HouseRent, IS24RestApi.Xsd.HouseRent>("houseRent");
            TestCompareToXsd<HouseType, IS24RestApi.Xsd.HouseType>("houseType");
            TestCompareToXsd<Industry, IS24RestApi.Xsd.Industry>("industry");
            TestCompareToXsd<Investment, IS24RestApi.Xsd.Investment>("investment");
            TestCompareToXsd<LivingBuySite, IS24RestApi.Xsd.LivingBuySite>("livingBuySite");
            TestCompareToXsd<LivingRentSite, IS24RestApi.Xsd.LivingRentSite>("livingRentSite");
            TestCompareToXsd<Office, IS24RestApi.Xsd.Office>("office");
            TestCompareToXsd<SeniorCare, IS24RestApi.Xsd.SeniorCare>("seniorCare");
            TestCompareToXsd<ShortTermAccommodation, IS24RestApi.Xsd.ShortTermAccommodation>("shortTermAccommodation");
            TestCompareToXsd<SpecialPurpose, IS24RestApi.Xsd.SpecialPurpose>("specialPurpose");
            TestCompareToXsd<Store, IS24RestApi.Xsd.Store>("store");
            TestCompareToXsd<TradeSite, IS24RestApi.Xsd.TradeSite>("tradeSite");
        }

        void TestCompareToXsd<T1, T2>(string file)
        {
            foreach (var suffix in new[] { "max", "min" })
            {
                var serializer1 = new XmlSerializer(typeof(T1));
                var serializer2 = new XmlSerializer(typeof(T2));
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

        [Fact]
        public void CanSerializeAndDeserializeAllExampleXmlFiles()
        {
            TestRoundtrip<ApartmentBuy>("apartmentBuy");
            TestRoundtrip<ApartmentRent>("apartmentRent");
            TestRoundtrip<AssistedLiving>("assistedLiving");
            TestRoundtrip<CompulsoryAuction>("compulsoryAuction");
            TestRoundtrip<GarageBuy>("garageBuy");
            TestRoundtrip<GarageRent>("garageRent");
            TestRoundtrip<Gastronomy>("gastronomy");
            TestRoundtrip<HouseBuy>("houseBuy");
            TestRoundtrip<HouseRent>("houseRent");
            TestRoundtrip<HouseType>("houseType");
            TestRoundtrip<Industry>("industry");
            TestRoundtrip<Investment>("investment");
            TestRoundtrip<LivingBuySite>("livingBuySite");
            TestRoundtrip<LivingRentSite>("livingRentSite");
            TestRoundtrip<Office>("office");
            TestRoundtrip<SeniorCare>("seniorCare");
            TestRoundtrip<ShortTermAccommodation>("shortTermAccommodation");
            TestRoundtrip<SpecialPurpose>("specialPurpose");
            TestRoundtrip<Store>("store");
            TestRoundtrip<TradeSite>("tradeSite");
        }

        void TestRoundtrip<T>(string file)
        {
            var serializer = new XmlSerializer(typeof(T));

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
    }
}
