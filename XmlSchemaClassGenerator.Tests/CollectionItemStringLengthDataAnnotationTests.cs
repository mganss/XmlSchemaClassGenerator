using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Xunit;

namespace XmlSchemaClassGenerator.Tests;

public class CollectionItemStringLengthDataAnnotationTests
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

    private const string RepeatingStringXsd = @"<?xml version=""1.0"" encoding=""UTF-8""?>
<xs:schema xmlns:xs=""http://www.w3.org/2001/XMLSchema"" xmlns:t=""urn:test"" targetNamespace=""urn:test"" elementFormDefault=""qualified"">
  <xs:element name=""document"" type=""t:documentType"" />
  <xs:complexType name=""documentType"">
    <xs:sequence>
      <xs:element name=""tag"" type=""t:tagType"" minOccurs=""0"" maxOccurs=""9"" />
    </xs:sequence>
  </xs:complexType>
  <xs:simpleType name=""tagType"">
    <xs:restriction base=""xs:string"">
      <xs:maxLength value=""35"" />
    </xs:restriction>
  </xs:simpleType>
</xs:schema>";

    private const string RepeatingStringWithMinAndMaxXsd = @"<?xml version=""1.0"" encoding=""UTF-8""?>
<xs:schema xmlns:xs=""http://www.w3.org/2001/XMLSchema"" xmlns:t=""urn:test"" targetNamespace=""urn:test"" elementFormDefault=""qualified"">
  <xs:element name=""document"" type=""t:documentType"" />
  <xs:complexType name=""documentType"">
    <xs:sequence>
      <xs:element name=""tag"" type=""t:tagType"" minOccurs=""0"" maxOccurs=""9"" />
    </xs:sequence>
  </xs:complexType>
  <xs:simpleType name=""tagType"">
    <xs:restriction base=""xs:string"">
      <xs:minLength value=""2"" />
      <xs:maxLength value=""35"" />
    </xs:restriction>
  </xs:simpleType>
</xs:schema>";

    private const string SingleStringXsd = @"<?xml version=""1.0"" encoding=""UTF-8""?>
<xs:schema xmlns:xs=""http://www.w3.org/2001/XMLSchema"" xmlns:t=""urn:test"" targetNamespace=""urn:test"" elementFormDefault=""qualified"">
  <xs:element name=""document"" type=""t:documentType"" />
  <xs:complexType name=""documentType"">
    <xs:sequence>
      <xs:element name=""tag"" type=""t:tagType"" />
    </xs:sequence>
  </xs:complexType>
  <xs:simpleType name=""tagType"">
    <xs:restriction base=""xs:string"">
      <xs:maxLength value=""35"" />
    </xs:restriction>
  </xs:simpleType>
</xs:schema>";

    [Fact]
    public void CollectionItemStringLengthAttribute_IsEmitted_ForRepeatingStringElement_WithMaxLength()
    {
        var contents = ConvertXml([RepeatingStringXsd], new Generator
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
        Assert.Contains("class CollectionItemStringLengthAttribute", allContent);
        Assert.Contains("Shared.Metadata.CollectionItemStringLengthAttribute(35)", allContent);
        Assert.Contains("Maximum length: 35.", allContent);

        var assembly = Compiler.Compile(nameof(CollectionItemStringLengthAttribute_IsEmitted_ForRepeatingStringElement_WithMaxLength), contents);
        var type = assembly.GetType("My.Schema.Generated.Model.DocumentType");
        var tag = type.GetProperty("Tag");

        Assert.Equal("System.Collections.ObjectModel.Collection`1[System.String]", tag.PropertyType.ToString());

        var attr = tag.CustomAttributes.Single(a => a.AttributeType.FullName == "Shared.Metadata.CollectionItemStringLengthAttribute");
        Assert.Equal(35, (int)attr.ConstructorArguments.Single().Value);
        Assert.Empty(attr.NamedArguments);
    }

    [Fact]
    public void CollectionItemStringLengthAttribute_IncludesMinimumLength_WhenBothFacetsPresent()
    {
        var contents = ConvertXml([RepeatingStringWithMinAndMaxXsd], new Generator
        {
            NamespaceProvider = new NamespaceProvider
            {
                GenerateNamespace = _ => "My.Schema.Generated.Model",
            },
            DataAnnotationMode = DataAnnotationMode.All,
            EmitMetadataAttributes = true,
            MetadataNamespace = "Shared.Metadata",
        }).ToArray();

        var assembly = Compiler.Compile(nameof(CollectionItemStringLengthAttribute_IncludesMinimumLength_WhenBothFacetsPresent), contents);
        var type = assembly.GetType("My.Schema.Generated.Model.DocumentType");
        var tag = type.GetProperty("Tag");

        var attr = tag.CustomAttributes.Single(a => a.AttributeType.FullName == "Shared.Metadata.CollectionItemStringLengthAttribute");
        Assert.Equal(35, (int)attr.ConstructorArguments.Single().Value);

        var namedArg = attr.NamedArguments.Single(na => na.MemberName == "MinimumLength");
        Assert.Equal(2, (int)namedArg.TypedValue.Value);
    }

    [Fact]
    public void CollectionItemStringLengthAttribute_IsNotEmitted_WhenEmitMetadataAttributesIsFalse()
    {
        var contents = ConvertXml([RepeatingStringXsd], new Generator
        {
            NamespaceProvider = new NamespaceProvider
            {
                GenerateNamespace = _ => "My.Schema.Generated.Model",
            },
            DataAnnotationMode = DataAnnotationMode.All,
            // Default: metadata emission disabled.
            MetadataNamespace = "Shared.Metadata",
        }).ToArray();

        var allContent = string.Join(Environment.NewLine, contents);

        Assert.DoesNotContain("CollectionItemStringLengthAttribute(", allContent);
        Assert.DoesNotContain("class CollectionItemStringLengthAttribute", allContent);
        Assert.DoesNotContain("Maximum length: 35.", allContent);

        var assembly = Compiler.Compile(nameof(CollectionItemStringLengthAttribute_IsNotEmitted_WhenEmitMetadataAttributesIsFalse), contents);
        var type = assembly.GetType("My.Schema.Generated.Model.DocumentType");
        var tag = type.GetProperty("Tag");

        Assert.DoesNotContain(tag.CustomAttributes, a => a.AttributeType.Name.Contains("Length"));
    }

    [Fact]
    public void ScalarStringLengthAttribute_IsUnaffected_ByCollectionItemStringLengthFeature()
    {
        var contents = ConvertXml([SingleStringXsd], new Generator
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

        // Scalar properties keep using the stock DataAnnotations attribute, not the new metadata one.
        Assert.DoesNotContain("CollectionItemStringLengthAttribute", allContent);

        var assembly = Compiler.Compile(nameof(ScalarStringLengthAttribute_IsUnaffected_ByCollectionItemStringLengthFeature), contents);
        var type = assembly.GetType("My.Schema.Generated.Model.DocumentType");
        var tag = type.GetProperty("Tag");

        Assert.Equal("System.String", tag.PropertyType.ToString());

        var attr = tag.CustomAttributes.Single(a => a.AttributeType.FullName == "System.ComponentModel.DataAnnotations.MaxLengthAttribute");
        Assert.Equal(35, (int)attr.ConstructorArguments.Single().Value);
    }

    [Fact]
    public void CollectionItemStringLengthAttribute_IsGeneratedOnce_ForMultipleModelNamespaces()
    {
        const string xsdA = @"<?xml version=""1.0"" encoding=""UTF-8""?>
<xs:schema xmlns:xs=""http://www.w3.org/2001/XMLSchema"" xmlns:a=""urn:a"" targetNamespace=""urn:a"" elementFormDefault=""qualified"">
  <xs:element name=""aDocument"" type=""a:documentType"" />
  <xs:complexType name=""documentType"">
    <xs:sequence>
      <xs:element name=""tag"" minOccurs=""0"" maxOccurs=""9"">
        <xs:simpleType>
          <xs:restriction base=""xs:string"">
            <xs:maxLength value=""20"" />
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
      <xs:element name=""tag"" minOccurs=""0"" maxOccurs=""9"">
        <xs:simpleType>
          <xs:restriction base=""xs:string"">
            <xs:maxLength value=""40"" />
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
        Assert.Equal(1, contents.Sum(c => CountOf(c, "class CollectionItemStringLengthAttribute")));
        Assert.Contains("Common.Metadata.CollectionItemStringLengthAttribute(20)", allContent);
        Assert.Contains("Common.Metadata.CollectionItemStringLengthAttribute(40)", allContent);

        Compiler.Compile(nameof(CollectionItemStringLengthAttribute_IsGeneratedOnce_ForMultipleModelNamespaces), contents);
    }

    [Fact]
    public void CollectionItemStringLengthAttribute_UsesDefaultMetadataNamespace_WhenMetadataNamespaceIsNull()
    {
        var contents = ConvertXml([RepeatingStringXsd], new Generator
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
        Assert.Contains("XmlSchemaClassGenerator.Metadata.CollectionItemStringLengthAttribute(35)", allContent);
        Assert.Equal(1, contents.Sum(c => CountOf(c, "class CollectionItemStringLengthAttribute")));

        Compiler.Compile(nameof(CollectionItemStringLengthAttribute_UsesDefaultMetadataNamespace_WhenMetadataNamespaceIsNull), contents);
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
