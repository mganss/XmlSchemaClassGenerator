using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Linq;
using Xunit;

namespace XmlSchemaClassGenerator.Tests;

public class PatternAttributeTypeTests
{
    private const string RegexAttribute = "RegularExpressionAttribute";

    private static string Convert(string baseType, string restriction, Action<Generator> configure = null, string typeName = "valueType")
    {
        var xsd = $@"<?xml version=""1.0"" encoding=""UTF-8""?>
<xs:schema xmlns:xs=""http://www.w3.org/2001/XMLSchema"" xmlns:t=""urn:test"" targetNamespace=""urn:test"" elementFormDefault=""qualified"">
  <xs:element name=""doc"" type=""t:docType"" />
  <xs:simpleType name=""baseType"">
    <xs:restriction base=""{baseType}"">{restriction}</xs:restriction>
  </xs:simpleType>
  <xs:simpleType name=""valueType"">
    <xs:restriction base=""t:baseType"" />
  </xs:simpleType>
  <xs:complexType name=""docType"">
    <xs:sequence>
      <xs:element name=""value"" type=""t:{typeName}"" />
    </xs:sequence>
  </xs:complexType>
</xs:schema>";

        var writer = new MemoryOutputWriter();
        var gen = new Generator
        {
            OutputWriter = writer,
            Version = new("Tests", "1.0.0.1"),
            NamespaceProvider = new NamespaceProvider { GenerateNamespace = _ => "Test.Model" },
        };
        configure?.Invoke(gen);
        gen.Generate([new StringReader(xsd)]);
        return string.Join(Environment.NewLine, writer.Content);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void Boolean_WithPatterns_HasNoRegularExpression_ButKeepsPatternDoc(bool merge)
    {
        var content = Convert("xs:boolean", @"<xs:pattern value=""false"" /><xs:pattern value=""true"" />",
            g => g.MergeRestrictionsWithBase = merge, typeName: merge ? "valueType" : "baseType");

        Assert.Contains("public bool Value", content);
        Assert.DoesNotContain(RegexAttribute, content);
        Assert.Contains("Pattern: (false)|(true).", content);
    }

    [Theory]
    [InlineData("xs:date", false, false)]
    [InlineData("xs:date", true, false)]
    [InlineData("xs:dateTime", false, false)]
    [InlineData("xs:dateTime", false, true)]
    [InlineData("xs:time", false, false)]
    [InlineData("xs:time", true, false)]
    public void DateAndTime_WithPattern_HasNoRegularExpression(string baseType, bool useDateOnly, bool withTimeZone)
    {
        var content = Convert(baseType, @"<xs:pattern value=""\d.*"" />", g =>
        {
            g.UseDateOnly = useDateOnly;
            g.DateTimeWithTimeZone = withTimeZone;
        });

        Assert.DoesNotContain(RegexAttribute, content);
        Assert.Contains("Pattern:", content);
    }

    [Theory]
    [InlineData("xs:base64Binary")]
    [InlineData("xs:hexBinary")]
    public void Binary_WithPattern_HasNoRegularExpression(string baseType)
    {
        var content = Convert(baseType, @"<xs:pattern value=""[0-9A-F]+"" />");

        Assert.Contains("byte[]", content);
        Assert.DoesNotContain(RegexAttribute, content);
    }

    [Theory]
    [InlineData("xs:string")]
    [InlineData("xs:int")]
    [InlineData("xs:decimal")]
    public void StringAndNumeric_WithPattern_KeepRegularExpression(string baseType)
    {
        var content = Convert(baseType, @"<xs:pattern value=""\d+"" />");

        Assert.Contains($@"{RegexAttribute}(""\\d+"")", content);
    }

    [Fact]
    public void Boolean_Generated_PassesDataAnnotationsValidation()
    {
        var content = Convert("xs:boolean", @"<xs:pattern value=""false"" /><xs:pattern value=""true"" />");
        var assembly = Compiler.Compile(nameof(Boolean_Generated_PassesDataAnnotationsValidation), content);
        var docType = assembly.GetType("Test.Model.DocType");
        var property = docType.GetProperty("Value");

        foreach (var value in new[] { true, false })
        {
            var instance = Activator.CreateInstance(docType);
            property.SetValue(instance, value);

            var results = new List<ValidationResult>();
            Assert.True(Validator.TryValidateObject(instance, new ValidationContext(instance), results, validateAllProperties: true),
                string.Join("; ", results.Select(r => r.ErrorMessage)));
        }
    }

    [Fact]
    public void Date_Generated_PassesDataAnnotationsValidation()
    {
        var content = Convert("xs:date", @"<xs:pattern value=""\d{4}-\d{2}-\d{2}"" />");
        var assembly = Compiler.Compile(nameof(Date_Generated_PassesDataAnnotationsValidation), content);
        var docType = assembly.GetType("Test.Model.DocType");

        var instance = Activator.CreateInstance(docType);
        docType.GetProperty("Value").SetValue(instance, new DateTime(2026, 10, 21));

        var results = new List<ValidationResult>();
        Assert.True(Validator.TryValidateObject(instance, new ValidationContext(instance), results, validateAllProperties: true),
            string.Join("; ", results.Select(r => r.ErrorMessage)));
    }
}
