using System;
using System.IO;
using System.Linq;
using System.Xml.Schema;
using Xunit;

namespace XmlSchemaClassGenerator.Tests;

public sealed class BackingFieldNamingTests
{
    const string TypeWithValueCollectionXsd = """
            <?xml version="1.0"?>
            <xsd:schema xmlns="http://www.example.org/Values" targetNamespace="http://www.example.org/Values" xmlns:xsd="http://www.w3.org/2001/XMLSchema" elementFormDefault="qualified" attributeFormDefault="unqualified">
                <xsd:complexType name="TypeWithValueCollection">
                    <xsd:sequence>
                        <xsd:element name="Value" type="xsd:string" minOccurs="0" maxOccurs="unbounded" />
                    </xsd:sequence>
                </xsd:complexType>
            </xsd:schema>
            """;

    private static string GenerateCode(string xsd, Generator generatorPrototype)
    {
        var writer = new MemoryOutputWriter();

        var gen = new Generator
        {
            OutputWriter = writer,
            Version = new("Tests", "1.0.0.1"),
            NamespaceProvider = generatorPrototype.NamespaceProvider,
            EnableDataBinding = generatorPrototype.EnableDataBinding,
            PrivateMemberPrefix = generatorPrototype.PrivateMemberPrefix,
            CollectionSettersMode = generatorPrototype.CollectionSettersMode,
        };

        var set = new XmlSchemaSet();

        using (var stringReader = new StringReader(xsd))
        {
            var schema = XmlSchema.Read(stringReader, (_, e) => throw new InvalidOperationException($"{e.Severity}: {e.Message}", e.Exception));
            ArgumentNullException.ThrowIfNull(schema);
            set.Add(schema);
        }

        gen.Generate(set);

        return writer.Content.First();
    }

    [Fact]
    public void BackingFieldNamedValue_WithEmptyPrefix_GeneratesValidCode()
    {
        var generatedCode = GenerateCode(TypeWithValueCollectionXsd, new Generator
        {
            NamespaceProvider = new NamespaceProvider
            {
                GenerateNamespace = _ => "Test"
            },
            EnableDataBinding = true,
            PrivateMemberPrefix = "",
            CollectionSettersMode = CollectionSettersMode.Private,
        });

        Assert.Contains("return this.value;", generatedCode);
        Assert.Contains("this.value = value;", generatedCode);

        Compiler.Compile(nameof(BackingFieldNamedValue_WithEmptyPrefix_GeneratesValidCode), generatedCode);
    }

    [Fact]
    public void BackingFieldNamedValue_WithDefaultPrefix_GeneratesValidCode()
    {
        var generatedCode = GenerateCode(TypeWithValueCollectionXsd, new Generator
        {
            NamespaceProvider = new NamespaceProvider
            {
                GenerateNamespace = _ => "Test"
            },
            EnableDataBinding = true,
            PrivateMemberPrefix = "_",
            CollectionSettersMode = CollectionSettersMode.Private,
        });

        Assert.Contains("return _value;", generatedCode);
        Assert.Contains("_value = value;", generatedCode);
        Compiler.Compile(nameof(BackingFieldNamedValue_WithDefaultPrefix_GeneratesValidCode), generatedCode);
    }
}
