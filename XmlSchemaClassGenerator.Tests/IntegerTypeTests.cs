using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml.Schema;
using Xunit;

namespace XmlSchemaClassGenerator.Tests {
    public class IntegerTypeTests
    {
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
                UseIntegerDataTypeAsFallback = generatorPrototype.UseIntegerDataTypeAsFallback,
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

        [Theory]
        [InlineData(2, "sbyte")]
        [InlineData(4, "short")]
        [InlineData(9, "int")]
        [InlineData(18, "long")]
        [InlineData(28, "decimal")]
        [InlineData(29, "string")]
        public void TestTotalDigits(int totalDigits, string expectedType)
        {
            var xsd = @$"<?xml version=""1.0"" encoding=""UTF-8""?>
<xs:schema elementFormDefault=""qualified"" xmlns:xs=""http://www.w3.org/2001/XMLSchema"">
	<xs:complexType name=""document"">
    <xs:sequence>
		  <xs:element name=""someValue"">
			  <xs:simpleType>
				  <xs:restriction base=""xs:integer"">
            <xs:totalDigits value=""{totalDigits}""/>
				  </xs:restriction>
			  </xs:simpleType>
		  </xs:element>
    </xs:sequence>
	</xs:complexType>
</xs:schema>";

            var generatedType = ConvertXml(nameof(TestTotalDigits), xsd, new Generator
            {
                NamespaceProvider = new NamespaceProvider
                {
                    GenerateNamespace = key => "Test"
                }
            });

            var expectedProperty = $"public {expectedType} SomeValue";
            Assert.Contains(expectedProperty, generatedType.First());
        }

        [Theory]
        [InlineData(4, false, "long")]
        [InlineData(30, false, "long")]
        [InlineData(4, true, "short")]
        [InlineData(30, true, "long")]
        public void TestFallbackType(int totalDigits, bool useTypeAsFallback, string expectedType)
        {
            var xsd = @$"<?xml version=""1.0"" encoding=""UTF-8""?>
<xs:schema elementFormDefault=""qualified"" xmlns:xs=""http://www.w3.org/2001/XMLSchema"">
	<xs:complexType name=""document"">
    <xs:sequence>
		  <xs:element name=""someValue"">
			  <xs:simpleType>
				  <xs:restriction base=""xs:integer"">
            <xs:totalDigits value=""{totalDigits}""/>
				  </xs:restriction>
			  </xs:simpleType>
		  </xs:element>
    </xs:sequence>
	</xs:complexType>
</xs:schema>";

          var generatedType = ConvertXml(nameof(TestTotalDigits), xsd, new Generator
          {
              NamespaceProvider = new NamespaceProvider
              {
                  GenerateNamespace = key => "Test"
              },
              IntegerDataType = typeof(long),
              UseIntegerDataTypeAsFallback = useTypeAsFallback
          });

          var expectedProperty = $"public {expectedType} SomeValue";
          Assert.Contains(expectedProperty, generatedType.First());
        }

        [Theory]
        [InlineData(1, 100, "byte")]
        [InlineData(-100, 100, "sbyte")]
        [InlineData(1, 1000, "ushort")]
        [InlineData(-1000, 1000, "short")]
        [InlineData(1, 100000, "uint")]
        [InlineData(-100000, 100000, "int")]
        [InlineData(1, 10000000000, "ulong")]
        [InlineData(-10000000000, 10000000000, "long")]
        public void TestInclusiveRange(long minInclusive, long maxInclusive, string expectedType)
        {
            var xsd = @$"<?xml version=""1.0"" encoding=""UTF-8""?>
<xs:schema elementFormDefault=""qualified"" xmlns:xs=""http://www.w3.org/2001/XMLSchema"">
	<xs:complexType name=""document"">
    <xs:sequence>
		  <xs:element name=""someValue"">
			  <xs:simpleType>
				  <xs:restriction base=""xs:integer"">
            <xs:minInclusive value=""{minInclusive}""/>
            <xs:maxInclusive value=""{maxInclusive}""/>
				  </xs:restriction>
			  </xs:simpleType>
		  </xs:element>
    </xs:sequence>
	</xs:complexType>
</xs:schema>";

          var generatedType = ConvertXml(nameof(TestTotalDigits), xsd, new Generator
          {
              NamespaceProvider = new NamespaceProvider
              {
                  GenerateNamespace = key => "Test"
              }
          });

          var expectedProperty = $"public {expectedType} SomeValue";
          Assert.Contains(expectedProperty, generatedType.First());
        }
    }
}
