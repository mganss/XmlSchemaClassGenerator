using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml.Schema;
using Xunit;

namespace XmlSchemaClassGenerator.Tests {
    public class VisitorTests
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
                DataAnnotationMode = generatorPrototype.DataAnnotationMode,
                GenerateDesignerCategoryAttribute = generatorPrototype.GenerateDesignerCategoryAttribute,
                GenerateComplexTypesForCollections = generatorPrototype.GenerateComplexTypesForCollections,
                EntityFramework = generatorPrototype.EntityFramework,
                AssemblyVisible = generatorPrototype.AssemblyVisible,
                GenerateInterfaces = generatorPrototype.GenerateInterfaces,
                MemberVisitor = generatorPrototype.MemberVisitor,
                TypeVisitor = generatorPrototype.TypeVisitor,
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

        [Fact]
        public void MemberVisitorIsCalledForProperty()
        {
            const string xsd = @"<?xml version=""1.0"" encoding = ""UTF-8""?>
<xs:schema elementFormDefault=""qualified"" xmlns:xs=""http://www.w3.org/2001/XMLSchema"" targetNamespace=""http://local.none"">
  <xs:complexType name=""MyType"">
    <xs:sequence>
      <xs:element maxOccurs=""1"" minOccurs=""0"" name=""output"" type=""xs:string""/>
    </xs:sequence>
  </xs:complexType>
</xs:schema>
";

            var memberVisitorWasCalled = false;
            var generatedType = ConvertXml(nameof(MemberVisitorIsCalledForProperty), xsd, new Generator
            {
                NamespaceProvider = new NamespaceProvider
                {
                    GenerateNamespace = key => "Test"
                },
                MemberVisitor = (member, model) => memberVisitorWasCalled = true
            });

            Assert.True(memberVisitorWasCalled);
        }

        [Fact]
        public void MemberVisitorIsCalledForAllNullableProperties()
        {
            const string xsd = @"<?xml version=""1.0"" encoding = ""UTF-8""?>
<xs:schema elementFormDefault=""qualified"" xmlns:xs=""http://www.w3.org/2001/XMLSchema"" targetNamespace=""http://local.none"">
  <xs:complexType name=""MyType"">
    <xs:sequence>
      <xs:element maxOccurs=""1"" minOccurs=""0"" name=""output"" type=""xs:int""/>
    </xs:sequence>
  </xs:complexType>
</xs:schema>
";

            var visitorCount = 0;
            var generatedType = ConvertXml(nameof(MemberVisitorIsCalledForProperty), xsd, new Generator
            {
                NamespaceProvider = new NamespaceProvider
                {
                    GenerateNamespace = key => "Test"
                },
                GenerateNullables = true,
                MemberVisitor = (member, model) => visitorCount++
            });

            // Visitor should be called 3 times: Value, ValueSpecified, and Nullable
            Assert.Equal(3, visitorCount);
        }

        [Fact]
        public void MemberVisitorIsCalledForTextContent()
        {
            const string xsd = @"<?xml version=""1.0"" encoding = ""UTF-8""?>
<xs:schema elementFormDefault=""qualified"" xmlns:xs=""http://www.w3.org/2001/XMLSchema"" targetNamespace=""http://local.none"">
  <xs:complexType name=""MyType"">
    <xs:simpleContent>
      <xs:extension base=""xs:string"">
        <xs:attribute name=""myAttribute"" type=""xs:string""/>
      </xs:extension>
    </xs:simpleContent>
  </xs:complexType>
</xs:schema>
";

            var visitorCount = 0;
            var generatedType = ConvertXml(nameof(MemberVisitorIsCalledForProperty), xsd, new Generator
            {
                NamespaceProvider = new NamespaceProvider
                {
                    GenerateNamespace = key => "Test"
                },
                GenerateNullables = true,
                MemberVisitor = (member, model) => visitorCount++
            });

            // Visitor should be called 2 times: Value and MyAttribute
            Assert.Equal(2, visitorCount);
        }

        [Fact]
        public void TypeVisitorIsCalledForClass()
        {
            const string xsd = @"<?xml version=""1.0"" encoding = ""UTF-8""?>
<xs:schema elementFormDefault=""qualified"" xmlns:xs=""http://www.w3.org/2001/XMLSchema"" targetNamespace=""http://local.none"">
  <xs:complexType name=""MyType"">
    <xs:sequence>
      <xs:element maxOccurs=""1"" minOccurs=""0"" name=""output"" type=""xs:string""/>
    </xs:sequence>
  </xs:complexType>
</xs:schema>
";

            var typeVisitorWasCalled = false;
            var generatedType = ConvertXml(nameof(MemberVisitorIsCalledForProperty), xsd, new Generator
            {
                NamespaceProvider = new NamespaceProvider
                {
                    GenerateNamespace = key => "Test"
                },
                TypeVisitor = (type, model) => typeVisitorWasCalled = true
            });

            Assert.True(typeVisitorWasCalled);
        }

        [Fact]
        public void TypeVisitorIsCalledForEnum()
        {
            const string xsd = @"<?xml version=""1.0"" encoding = ""UTF-8""?>
<xs:schema elementFormDefault=""qualified"" xmlns:xs=""http://www.w3.org/2001/XMLSchema"" targetNamespace=""http://local.none"">
  <xs:simpleType name=""MyType"">
    <xs:restriction base=""xs:string"">
      <xs:enumeration value=""One""/>
      <xs:enumeration value=""Two""/>
      <xs:enumeration value=""Three""/>
    </xs:restriction>
  </xs:simpleType>
</xs:schema>
";

            var typeVisitorWasCalled = false;
            var generatedType = ConvertXml(nameof(MemberVisitorIsCalledForProperty), xsd, new Generator
            {
                NamespaceProvider = new NamespaceProvider
                {
                    GenerateNamespace = key => "Test"
                },
                TypeVisitor = (type, model) => typeVisitorWasCalled = true
            });

            Assert.True(typeVisitorWasCalled);
        }
    }
}
