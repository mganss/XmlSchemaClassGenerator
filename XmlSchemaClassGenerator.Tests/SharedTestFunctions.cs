namespace XmlSchemaClassGenerator.Tests
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using System.Text;
    using System.Xml;
    using System.Xml.Schema;
    using System.Xml.Serialization;
    using System.Xml.XPath;
    using Ganss.IO;
    using Microsoft.CodeAnalysis;
    using Microsoft.Xml.XMLGen;
    using Xunit;
    using Xunit.Abstractions;

    internal static class SharedTestFunctions
    {
        private static readonly XmlQualifiedName AnyType = new("anyType", XmlSchema.Namespace);

        internal static void TestSamples(ITestOutputHelper output, string name, string pattern)
        {
            var assembly = Compiler.GetAssembly(name);
            Assert.NotNull(assembly);
            DeserializeSampleXml(output, pattern, assembly);
        }

        internal static string Serialize(XmlSerializer serializer, object o, IDictionary<string, string> prefixToNsMap = null)
        {
            using var sw = new StringWriter();
            using var xw = XmlWriter.Create(sw, new XmlWriterSettings { Indent = true });
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

            serializer.Serialize(xw, o, ns);
            var serializedXml = sw.ToString();
            return serializedXml;
        }

        private static void DeserializeSampleXml(ITestOutputHelper output, string pattern, Assembly assembly)
        {
            var files = Glob.ExpandNames(pattern);

            var set = new XmlSchemaSet();
            var xmlSchemaReaderSettings = new XmlReaderSettings { DtdProcessing = DtdProcessing.Ignore };

            set.XmlResolver = new XmlUrlResolver();

            var readers = files.Select(f => XmlReader.Create(f, xmlSchemaReaderSettings));

            foreach (var reader in readers)
                set.Add(null, reader);

            set.Compile();

            var anyValidXml = false;
            var sb = new StringBuilder();

            foreach (var rootElement in set.GlobalElements.Values.Cast<XmlSchemaElement>().Where(e =>
                !e.IsAbstract
                && e.ElementSchemaType is not XmlSchemaSimpleType
                && e.ElementSchemaType.QualifiedName != AnyType))
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
                    if (HandleValidationError(output, xmlLines, e))
                        invalid = true;
                }

                settings.ValidationEventHandler += validate;

                var reader = XmlReader.Create(new StringReader(xml), settings);
                while (reader.Read())
                    ;

                settings.ValidationEventHandler -= validate;

                // generated xml is not schema valid -> skip
                if (invalid)
                    continue;
                anyValidXml = true;

                // deserialize from sample
                var sr = new StringReader(xml);
                var o = serializer.Deserialize(sr);

                // serialize back to xml
                var xml2 = Serialize(serializer, o, GetNamespacesFromSource(xml));

                File.WriteAllText("xml2.xml", xml2);
                xmlLines = xml2.Split('\n');
                void validate2(object s, ValidationEventArgs e)
                {
                    if (HandleValidationError(output, xmlLines, e))
                        throw e.Exception;
                };

                settings.ValidationEventHandler += validate2;

                reader = XmlReader.Create(new StringReader(xml2), settings);
                while (reader.Read())
                    ;

                settings.ValidationEventHandler -= validate2;

                // deserialize again
                sr = new StringReader(xml2);
                var o2 = serializer.Deserialize(sr);

                AssertEx.Equal(o, o2);
            }

            Assert.True(anyValidXml, "No valid generated XML for this test");
        }

        public static IDictionary<string, string> GetNamespacesFromSource(string source)
        {
            XPathDocument doc = new(new StringReader(source));
            XPathNavigator namespaceNavigator = doc.CreateNavigator();
            var namespaces = new Dictionary<string, string>();
            var namespaceNames = new HashSet<string>();
            var nodeIterator = namespaceNavigator.Select("//*");
            var i = 1;

            while (nodeIterator.MoveNext())
            {
                foreach (var (k, v) in nodeIterator.Current.GetNamespacesInScope(XmlNamespaceScope.All))
                {
                    if (!namespaceNames.Contains(v))
                    {
                        namespaceNames.Add(v);
                        namespaces[$"ns{i}"] = v;
                        i++;
                    }
                }
            }

            return namespaces;
        }

        private static Type FindType(Assembly assembly, XmlQualifiedName xmlQualifiedName)
        {
            return assembly.GetTypes()
                .Single(t => t.CustomAttributes.Any(a => a.AttributeType == typeof(XmlRootAttribute)
                    && a.ConstructorArguments.Any(n => (string)n.Value == xmlQualifiedName.Name)
                    && a.NamedArguments.Any(n => n.MemberName == "Namespace" && (string)n.TypedValue.Value == xmlQualifiedName.Namespace)));
        }

        private static bool HandleValidationError(ITestOutputHelper output, string[] xmlLines, ValidationEventArgs e)
        {
            var line = xmlLines[e.Exception.LineNumber - 1][(e.Exception.LinePosition - 1)..];
            var severity = e.Severity == XmlSeverityType.Error ? "Error" : "Warning";
            output.WriteLine($"{severity} at line {e.Exception.LineNumber}, column {e.Exception.LinePosition}: {e.Message}");
            output.WriteLine(line);
            return (e.Severity == XmlSeverityType.Error
                && !e.Message.Contains("The Pattern constraint failed"));  // generator doesn't generate valid values where pattern restrictions exist, e.g. email
        }
    }
}
