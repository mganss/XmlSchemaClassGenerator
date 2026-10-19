using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Xunit;

namespace XmlSchemaClassGenerator.Tests;

public class RestrictionMergeTests
{
    private static string Convert(string baseRestriction, string derivedRestriction, bool merge = true)
    {
        var xsd = $@"<?xml version=""1.0"" encoding=""UTF-8""?>
<xs:schema xmlns:xs=""http://www.w3.org/2001/XMLSchema"" xmlns:t=""urn:test"" targetNamespace=""urn:test"" elementFormDefault=""qualified"">
  <xs:element name=""doc"" type=""t:docType"" />
  <xs:simpleType name=""baseType"">
    <xs:restriction base=""xs:string"">{baseRestriction}</xs:restriction>
  </xs:simpleType>
  <xs:simpleType name=""derivedType"">
    <xs:restriction base=""t:baseType"">{derivedRestriction}</xs:restriction>
  </xs:simpleType>
  <xs:complexType name=""docType"">
    <xs:sequence>
      <xs:element name=""value"" type=""t:derivedType"" />
    </xs:sequence>
  </xs:complexType>
</xs:schema>";

        var writer = new MemoryOutputWriter();
        var gen = new Generator
        {
            OutputWriter = writer,
            Version = new("Tests", "1.0.0.1"),
            NamespaceProvider = new NamespaceProvider { GenerateNamespace = _ => "Test.Model" },
            MergeRestrictionsWithBase = merge,
        };
        gen.Generate([new StringReader(xsd)]);
        return string.Join(Environment.NewLine, writer.Content);
    }

    [Fact]
    public void SiblingPatternsInOneRestriction_AreOrJoined()
    {
        var content = Convert(
            @"<xs:pattern value=""[A-Z]{2}[!-~]{1,15}"" /><xs:pattern value=""0{17}"" />",
            "");

        Assert.Contains(@"RegularExpressionAttribute(""([A-Z]{2}[!-~]{1,15})|(0{17})"")", content);
    }

    [Fact]
    public void BaseWithTwoPatterns_DerivedWithoutPattern_InheritsBoth()
    {
        var content = Convert(
            @"<xs:pattern value=""a+"" /><xs:pattern value=""b+"" />",
            @"<xs:maxLength value=""10"" />");

        Assert.Contains(@"RegularExpressionAttribute(""(a+)|(b+)"")", content);
    }

    [Fact]
    public void BaseWithTwoPatterns_DerivedWithOnePattern_DerivedReplacesBase()
    {
        var content = Convert(
            @"<xs:pattern value=""a+"" /><xs:pattern value=""b+"" />",
            @"<xs:pattern value=""c+"" />");

        Assert.Contains(@"RegularExpressionAttribute(""c+"")", content);
        Assert.DoesNotContain("a+", content);
    }

    [Fact]
    public void BaseWithOnePattern_DerivedWithTwoPatterns_DerivedReplacesBase()
    {
        var content = Convert(
            @"<xs:pattern value=""a+"" />",
            @"<xs:pattern value=""c+"" /><xs:pattern value=""d+"" />");

        Assert.Contains(@"RegularExpressionAttribute(""(c+)|(d+)"")", content);
        Assert.DoesNotContain("a+", content);
    }

    [Fact]
    public void NonPatternFacet_IsReplacedByDerivedValue()
    {
        var content = Convert(
            @"<xs:maxLength value=""20"" />",
            @"<xs:maxLength value=""17"" />");

        Assert.Contains("MaxLengthAttribute(17)", content);
        Assert.DoesNotContain("MaxLengthAttribute(20)", content);
    }

    [Fact]
    public void MergeDisabled_UsesOnlyOwnRestrictionPatterns()
    {
        var content = Convert(
            @"<xs:pattern value=""a+"" />",
            @"<xs:pattern value=""c+"" /><xs:pattern value=""d+"" />",
            merge: false);

        Assert.Contains(@"RegularExpressionAttribute(""(c+)|(d+)"")", content);
        Assert.DoesNotContain("a+", content);
    }
}
