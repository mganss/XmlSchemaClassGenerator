using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection.Metadata;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Schema;
using Xunit;

namespace XmlSchemaClassGenerator.Tests
{
    public sealed class DateTimeTypeTests
    {
        private static IEnumerable<string> ConvertXml(string xsd, Generator generatorPrototype)
        {
            var writer = new MemoryOutputWriter();

            var gen = new Generator
            {
                OutputWriter = writer,
                Version = new("Tests", "1.0.0.1"),
                NamespaceProvider = generatorPrototype.NamespaceProvider,
                GenerateNullables = generatorPrototype.GenerateNullables,
                DateTimeWithTimeZone = generatorPrototype.DateTimeWithTimeZone,
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
                var schema = XmlSchema.Read(stringReader, (_, e) => throw new InvalidOperationException($"{e.Severity}: {e.Message}", e.Exception));
                ArgumentNullException.ThrowIfNull(schema);
                set.Add(schema);
            }

            gen.Generate(set);

            return writer.Content;
        }

        [Fact]
        public void WhenDateTimeOffsetIsUsed_NoDataTypePropertyIsPresent()
        {
            var xsd = @$"<?xml version=""1.0"" encoding=""UTF-8""?>
<xs:schema elementFormDefault=""qualified"" xmlns:xs=""http://www.w3.org/2001/XMLSchema"">
	<xs:complexType name=""document"">
		<xs:sequence>
			<xs:element name=""someDate"" type=""xs:dateTime"" />
		</xs:sequence>
	</xs:complexType>
</xs:schema>";

            var generatedType = ConvertXml(
                xsd, new()
                {
                    NamespaceProvider = new()
                    {
                        GenerateNamespace = _ => "Test"
                    },
                    DateTimeWithTimeZone = true
                });

            var expectedXmlSerializationAttribute = "[System.Xml.Serialization.XmlElementAttribute(\"someDate\")]";
            var generatedProperty = generatedType.First();

            Assert.Contains(expectedXmlSerializationAttribute, generatedProperty);
        }

        [Fact]
        public void WhenDateTimeOffsetIsNotUsed_DataTypePropertyIsPresent()
        {
            var xsd = @$"<?xml version=""1.0"" encoding=""UTF-8""?>
<xs:schema elementFormDefault=""qualified"" xmlns:xs=""http://www.w3.org/2001/XMLSchema"">
	<xs:complexType name=""document"">
		<xs:sequence>
			<xs:element name=""someDate"" type=""xs:dateTime"" />
		</xs:sequence>
	</xs:complexType>
</xs:schema>";

            var generatedType = ConvertXml(
                xsd, new()
                {
                    NamespaceProvider = new()
                    {
                        GenerateNamespace = _ => "Test"
                    },
                    DateTimeWithTimeZone = false
                });

            var expectedXmlSerializationAttribute = "[System.Xml.Serialization.XmlElementAttribute(\"someDate\", DataType=\"dateTime\")]";
            var generatedProperty = generatedType.First();

            Assert.Contains(expectedXmlSerializationAttribute, generatedProperty);
        }

        [Fact]
        public void WhenDateTimeOffsetIsNotUsed_DataTypePropertyIsPresent2()
        {
            var xsd = @$"<?xml version=""1.0"" encoding=""UTF-8""?>
<xs:schema elementFormDefault=""qualified"" xmlns:xs=""http://www.w3.org/2001/XMLSchema"">
	<xs:complexType name=""document"">
		<xs:sequence>
			<xs:element name=""someDate"" type=""xs:date"" />
		</xs:sequence>
	</xs:complexType>
</xs:schema>";

            var generatedType = ConvertXml(
                xsd, new()
                {
                    NamespaceProvider = new()
                    {
                        GenerateNamespace = _ => "Test"
                    },
                    DateTimeWithTimeZone = true
                });

            var expectedXmlSerializationAttribute = "[System.Xml.Serialization.XmlElementAttribute(\"someDate\", DataType=\"date\")]";
            var generatedProperty = generatedType.First();

            Assert.Contains(expectedXmlSerializationAttribute, generatedProperty);
        }

        [Theory]
        [InlineData(false, "System.DateTime")]
        [InlineData(true, "System.DateTimeOffset")]
        public void TestCorrectDateTimeDataType(bool dateTimeWithTimeZone, string expectedType)
        {
            var xsd = @$"<?xml version=""1.0"" encoding=""UTF-8""?>
<xs:schema elementFormDefault=""qualified"" xmlns:xs=""http://www.w3.org/2001/XMLSchema"">
	<xs:complexType name=""document"">
		<xs:sequence>
			<xs:element name=""someDate"" type=""xs:dateTime"" />
		</xs:sequence>
	</xs:complexType>
</xs:schema>";

            var generatedType = ConvertXml(
                xsd, new()
                {
                    NamespaceProvider = new()
                    {
                        GenerateNamespace = _ => "Test"
                    },
                    DateTimeWithTimeZone = dateTimeWithTimeZone
                });

            var expectedProperty = $"public {expectedType} SomeDate";
            var generatedProperty = generatedType.First();

            Assert.Contains(expectedProperty, generatedProperty);
        }

    }
}
