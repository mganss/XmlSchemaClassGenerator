using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Xunit;

namespace XmlSchemaClassGenerator.Tests;

public class FractionDigitsDataAnnotationTests
{
    private static IEnumerable<string> ConvertXml(IEnumerable<string> xsds, Generator generatorPrototype)
    {
        var writer = new MemoryOutputWriter();

        var gen = new Generator
        {
            OutputWriter = writer,
            Version = new("Tests", "1.0.0.1"),
            NamespaceProvider = generatorPrototype.NamespaceProvider,
            DataAnnotationMode = generatorPrototype.DataAnnotationMode,
            CompactTypeNames = generatorPrototype.CompactTypeNames,
            EmitMetadataAttributes = generatorPrototype.EmitMetadataAttributes,
            MetadataNamespace = generatorPrototype.MetadataNamespace,
        };

        gen.Generate(xsds.Select(i => new StringReader(i)));

        return writer.Content;
    }

    [Fact]
    public void FractionDigitsAttribute_IsGeneratedUnderConfiguredNamespace_AndEmittedOnce()
    {
        const string xsd = @"<?xml version=""1.0"" encoding=""UTF-8""?>
<xs:schema xmlns:xs=""http://www.w3.org/2001/XMLSchema"" xmlns:t=""urn:test"" targetNamespace=""urn:test"" elementFormDefault=""qualified"">
  <xs:element name=""document"" type=""t:documentType"" />
  <xs:complexType name=""documentType"">
    <xs:sequence>
      <xs:element name=""price"">
        <xs:simpleType>
          <xs:restriction base=""xs:decimal"">
            <xs:fractionDigits value=""2"" />
          </xs:restriction>
        </xs:simpleType>
      </xs:element>
      <xs:element name=""tax"">
        <xs:simpleType>
          <xs:restriction base=""xs:decimal"">
            <xs:fractionDigits value=""3"" />
          </xs:restriction>
        </xs:simpleType>
      </xs:element>
    </xs:sequence>
  </xs:complexType>
</xs:schema>";

        var contents = ConvertXml([xsd], new Generator
        {
            NamespaceProvider = new NamespaceProvider
            {
                GenerateNamespace = _ => "My.Schema.Generated.Model",
            },
            DataAnnotationMode = DataAnnotationMode.All,
            EmitMetadataAttributes = true,
            MetadataNamespace = "Shared.Metadata",
        }).ToArray();

        var allContent = string.Join(Environment.NewLine, contents);

        Assert.Contains("namespace Shared.Metadata", allContent);
        Assert.Equal(1, contents.Sum(c => CountOf(c, "class FractionDigitsAttribute")));
        Assert.Contains("Shared.Metadata.FractionDigitsAttribute(2)", allContent);
        Assert.Contains("Shared.Metadata.FractionDigitsAttribute(3)", allContent);

        var assembly = Compiler.Compile(nameof(FractionDigitsAttribute_IsGeneratedUnderConfiguredNamespace_AndEmittedOnce), contents);
        var type = assembly.GetType("My.Schema.Generated.Model.DocumentType");
        var price = type.GetProperty("Price");
        var attr = price.CustomAttributes.Single(a => a.AttributeType.FullName == "Shared.Metadata.FractionDigitsAttribute");
        Assert.Equal(2, (int)attr.ConstructorArguments.Single().Value);
    }

    [Fact]
    public void FractionDigitsAttribute_IsGeneratedOnce_ForMultipleModelNamespaces()
    {
        const string xsdA = @"<?xml version=""1.0"" encoding=""UTF-8""?>
<xs:schema xmlns:xs=""http://www.w3.org/2001/XMLSchema"" xmlns:a=""urn:a"" targetNamespace=""urn:a"" elementFormDefault=""qualified"">
  <xs:element name=""aDocument"" type=""a:documentType"" />
  <xs:complexType name=""documentType"">
    <xs:sequence>
      <xs:element name=""amount"">
        <xs:simpleType>
          <xs:restriction base=""xs:decimal"">
            <xs:fractionDigits value=""2"" />
          </xs:restriction>
        </xs:simpleType>
      </xs:element>
    </xs:sequence>
  </xs:complexType>
</xs:schema>";

        const string xsdB = @"<?xml version=""1.0"" encoding=""UTF-8""?>
<xs:schema xmlns:xs=""http://www.w3.org/2001/XMLSchema"" xmlns:b=""urn:b"" targetNamespace=""urn:b"" elementFormDefault=""qualified"">
  <xs:element name=""bDocument"" type=""b:documentType"" />
  <xs:complexType name=""documentType"">
    <xs:sequence>
      <xs:element name=""value"">
        <xs:simpleType>
          <xs:restriction base=""xs:decimal"">
            <xs:fractionDigits value=""4"" />
          </xs:restriction>
        </xs:simpleType>
      </xs:element>
    </xs:sequence>
  </xs:complexType>
</xs:schema>";

        var contents = ConvertXml([xsdA, xsdB], new Generator
        {
            NamespaceProvider = new NamespaceProvider
            {
                GenerateNamespace = key => key.XmlSchemaNamespace switch
                {
                    "urn:a" => "Ns.A",
                    "urn:b" => "Ns.B",
                    _ => "Ns.Other",
                },
            },
            DataAnnotationMode = DataAnnotationMode.All,
            EmitMetadataAttributes = true,
            MetadataNamespace = "Common.Metadata",
        }).ToArray();

        var allContent = string.Join(Environment.NewLine, contents);

        Assert.Contains("namespace Common.Metadata", allContent);
        Assert.Equal(1, contents.Sum(c => CountOf(c, "class FractionDigitsAttribute")));
        Assert.Contains("Common.Metadata.FractionDigitsAttribute(2)", allContent);
        Assert.Contains("Common.Metadata.FractionDigitsAttribute(4)", allContent);

        Compiler.Compile(nameof(FractionDigitsAttribute_IsGeneratedOnce_ForMultipleModelNamespaces), contents);
    }

    [Theory]
    [InlineData(DataAnnotationMode.None)]
    [InlineData(DataAnnotationMode.Partial)]
    public void FractionDigitsAttribute_IsNotEmitted_WhenNotSupportedByMode(DataAnnotationMode mode)
    {
        const string xsd = @"<?xml version=""1.0"" encoding=""UTF-8""?>
<xs:schema xmlns:xs=""http://www.w3.org/2001/XMLSchema"" xmlns:t=""urn:test"" targetNamespace=""urn:test"" elementFormDefault=""qualified"">
  <xs:element name=""document"" type=""t:documentType"" />
  <xs:complexType name=""documentType"">
    <xs:sequence>
      <xs:element name=""price"">
        <xs:simpleType>
          <xs:restriction base=""xs:decimal"">
            <xs:fractionDigits value=""2"" />
          </xs:restriction>
        </xs:simpleType>
      </xs:element>
    </xs:sequence>
  </xs:complexType>
</xs:schema>";

        var contents = ConvertXml([xsd], new Generator
        {
            NamespaceProvider = new NamespaceProvider
            {
                GenerateNamespace = _ => "My.Schema.Generated.Model",
            },
            DataAnnotationMode = mode,
            EmitMetadataAttributes = true,
            MetadataNamespace = "Shared.Metadata",
        }).ToArray();

        var allContent = string.Join(Environment.NewLine, contents);

        Assert.DoesNotContain("FractionDigitsAttribute(", allContent);
        Assert.DoesNotContain("class FractionDigitsAttribute", allContent);
        Compiler.Compile($"{nameof(FractionDigitsAttribute_IsNotEmitted_WhenNotSupportedByMode)}_{mode}", contents);
    }

    [Fact]
    public void FractionDigitsAttribute_IsNotEmitted_WhenEmitMetadataAttributesIsFalse()
    {
        const string xsd = @"<?xml version=""1.0"" encoding=""UTF-8""?>
<xs:schema xmlns:xs=""http://www.w3.org/2001/XMLSchema"" xmlns:t=""urn:test"" targetNamespace=""urn:test"" elementFormDefault=""qualified"">
  <xs:element name=""document"" type=""t:documentType"" />
  <xs:complexType name=""documentType"">
    <xs:sequence>
      <xs:element name=""price"">
        <xs:simpleType>
          <xs:restriction base=""xs:decimal"">
            <xs:fractionDigits value=""2"" />
          </xs:restriction>
        </xs:simpleType>
      </xs:element>
    </xs:sequence>
  </xs:complexType>
</xs:schema>";

        var contents = ConvertXml([xsd], new Generator
        {
            NamespaceProvider = new NamespaceProvider
            {
                GenerateNamespace = _ => "My.Schema.Generated.Model",
            },
            DataAnnotationMode = DataAnnotationMode.All,
            // Default mode: metadata emission disabled.
            MetadataNamespace = "Shared.Metadata",
        }).ToArray();

        var allContent = string.Join(Environment.NewLine, contents);

        Assert.DoesNotContain("FractionDigitsAttribute(", allContent);
        Assert.DoesNotContain("class FractionDigitsAttribute", allContent);
        Compiler.Compile(nameof(FractionDigitsAttribute_IsNotEmitted_WhenEmitMetadataAttributesIsFalse), contents);
    }

    [Fact]
    public void FractionDigitsAttribute_CoexistsWithPattern()
    {
        const string xsd = @"<?xml version=""1.0"" encoding=""UTF-8""?>
<xs:schema xmlns:xs=""http://www.w3.org/2001/XMLSchema"" xmlns:t=""urn:test"" targetNamespace=""urn:test"" elementFormDefault=""qualified"">
  <xs:element name=""document"" type=""t:documentType"" />
  <xs:complexType name=""documentType"">
    <xs:sequence>
      <xs:element name=""value"">
        <xs:simpleType>
          <xs:restriction base=""xs:decimal"">
            <xs:fractionDigits value=""2"" />
            <xs:pattern value=""\d+(\.\d{1,2})?"" />
          </xs:restriction>
        </xs:simpleType>
      </xs:element>
    </xs:sequence>
  </xs:complexType>
</xs:schema>";

        var contents = ConvertXml([xsd], new Generator
        {
            NamespaceProvider = new NamespaceProvider
            {
                GenerateNamespace = _ => "My.Schema.Generated.Model",
            },
            DataAnnotationMode = DataAnnotationMode.All,
            EmitMetadataAttributes = true,
            MetadataNamespace = "Shared.Metadata",
        }).ToArray();

        var allContent = string.Join(Environment.NewLine, contents);
        Assert.Contains("FractionDigitsAttribute(2)", allContent);
        Assert.Contains("RegularExpressionAttribute", allContent);

        Compiler.Compile(nameof(FractionDigitsAttribute_CoexistsWithPattern), contents);
    }

    [Fact]
    public void FractionDigitsAttribute_UsesDefaultMetadataNamespace_WhenMetadataNamespaceIsNull()
    {
        const string xsd = @"<?xml version=""1.0"" encoding=""UTF-8""?>
<xs:schema xmlns:xs=""http://www.w3.org/2001/XMLSchema"" xmlns:t=""urn:test"" targetNamespace=""urn:test"" elementFormDefault=""qualified"">
  <xs:element name=""document"" type=""t:documentType"" />
  <xs:complexType name=""documentType"">
    <xs:sequence>
      <xs:element name=""price"">
        <xs:simpleType>
          <xs:restriction base=""xs:decimal"">
            <xs:fractionDigits value=""2"" />
          </xs:restriction>
        </xs:simpleType>
      </xs:element>
    </xs:sequence>
  </xs:complexType>
</xs:schema>";

        var contents = ConvertXml([xsd], new Generator
        {
            NamespaceProvider = new NamespaceProvider
            {
                GenerateNamespace = _ => "My.Schema.Generated.Model",
            },
            DataAnnotationMode = DataAnnotationMode.All,
            EmitMetadataAttributes = true,
            MetadataNamespace = null,
        }).ToArray();

        var allContent = string.Join(Environment.NewLine, contents);

        Assert.Contains("namespace XmlSchemaClassGenerator.Metadata", allContent);
        Assert.Contains("XmlSchemaClassGenerator.Metadata.FractionDigitsAttribute(2)", allContent);
        Assert.Equal(1, contents.Sum(c => CountOf(c, "class FractionDigitsAttribute")));

        Compiler.Compile(nameof(FractionDigitsAttribute_UsesDefaultMetadataNamespace_WhenMetadataNamespaceIsNull), contents);
    }

    private static int CountOf(string input, string value)
    {
        if (string.IsNullOrEmpty(input) || string.IsNullOrEmpty(value))
            return 0;

        var count = 0;
        var index = 0;

        while ((index = input.IndexOf(value, index, StringComparison.Ordinal)) >= 0)
        {
            count++;
            index += value.Length;
        }

        return count;
    }
}
