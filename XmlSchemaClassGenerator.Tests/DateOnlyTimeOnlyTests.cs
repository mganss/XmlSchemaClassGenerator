using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Schema;
using Xunit;

namespace XmlSchemaClassGenerator.Tests;

public sealed class DateOnlyTimeOnlyTests
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
            UseDateOnly = generatorPrototype.UseDateOnly,
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
    public void WhenUseDateOnlyIsTrue_DateOnlyAndTimeOnlyAreGenerated()
    {
        var xsd = @$"<?xml version=""1.0"" encoding=""UTF-8""?>
<xs:schema elementFormDefault=""qualified"" xmlns:xs=""http://www.w3.org/2001/XMLSchema"">
	<xs:complexType name=""document"">
		<xs:sequence>
			<xs:element name=""someDate"" type=""xs:date"" />
            <xs:element name=""someTime"" type=""xs:time"" />
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
                UseDateOnly = true
            });

        var code = string.Join(Environment.NewLine, generatedType);

        Assert.Contains("public System.DateOnly SomeDate", code);
        Assert.Contains("public System.TimeOnly SomeTime", code);
        Assert.DoesNotContain("DataType=\"date\"", code);
        Assert.DoesNotContain("DataType=\"time\"", code);
    }

    [Fact]
    public void WhenUseDateOnlyIsFalse_DateTimeIsGenerated()
    {
        var xsd = @$"<?xml version=""1.0"" encoding=""UTF-8""?>
<xs:schema elementFormDefault=""qualified"" xmlns:xs=""http://www.w3.org/2001/XMLSchema"">
	<xs:complexType name=""document"">
		<xs:sequence>
			<xs:element name=""someDate"" type=""xs:date"" />
            <xs:element name=""someTime"" type=""xs:time"" />
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
                UseDateOnly = false
            });

        var code = string.Join(Environment.NewLine, generatedType);

        Assert.Contains("public System.DateTime SomeDate", code);
        Assert.Contains("public System.DateTime SomeTime", code);
        Assert.Contains("DataType=\"date\"", code);
        Assert.Contains("DataType=\"time\"", code);
    }
    [Fact]
    public void WhenDefaultValueIsPresent_CodeIsGenerated()
    {
        var xsd = @$"<?xml version=""1.0"" encoding=""UTF-8""?>
<xs:schema elementFormDefault=""qualified"" xmlns:xs=""http://www.w3.org/2001/XMLSchema"">
	<xs:complexType name=""document"">
		<xs:sequence>
			<xs:element name=""someDate"" type=""xs:date"" default=""2023-10-27"" />
            <xs:element name=""someTime"" type=""xs:time"" default=""12:34:56"" />
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
                UseDateOnly = true
            });

        var code = string.Join(Environment.NewLine, generatedType);

        Assert.Contains("System.DateOnly.Parse(\"2023-10-27\")", code);
        Assert.Contains("System.TimeOnly.Parse(\"12:34:56\")", code);
    }
    [Fact]
    public void WhenUseDateOnlyIsFalse_AndDateTimeWithTimeZoneIsTrue_DateTimeOffsetIsGeneratedForTime()
    {
        var xsd = @$"<?xml version=""1.0"" encoding=""UTF-8""?>
<xs:schema elementFormDefault=""qualified"" xmlns:xs=""http://www.w3.org/2001/XMLSchema"">
	<xs:complexType name=""document"">
		<xs:sequence>
            <xs:element name=""someTime"" type=""xs:time"" />
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
                UseDateOnly = false,
                DateTimeWithTimeZone = true
            });

        var code = string.Join(Environment.NewLine, generatedType);

        Assert.Contains("public System.DateTimeOffset SomeTime", code);
        Assert.DoesNotContain("DataType=\"time\"", code);
    }
    [Fact]
    public void WhenUseDateOnlyIsFalse_AndDateTimeWithTimeZoneIsTrue_DateTimeOffsetIsGeneratedForDate()
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
                UseDateOnly = false,
                DateTimeWithTimeZone = true
            });

        var code = string.Join(Environment.NewLine, generatedType);

        Assert.Contains("public System.DateTimeOffset SomeDate", code);
        Assert.DoesNotContain("DataType=\"date\"", code);
    }
    [Fact]
    public void WhenUseDateOnlyIsTrue_AndDateTimeWithTimeZoneIsTrue_DateOnlyAndTimeOnlyAreGenerated()
    {
        var xsd = @$"<?xml version=""1.0"" encoding=""UTF-8""?>
<xs:schema elementFormDefault=""qualified"" xmlns:xs=""http://www.w3.org/2001/XMLSchema"">
	<xs:complexType name=""document"">
		<xs:sequence>
			<xs:element name=""someDate"" type=""xs:date"" />
            <xs:element name=""someTime"" type=""xs:time"" />
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
                UseDateOnly = true,
                DateTimeWithTimeZone = true
            });

        var code = string.Join(Environment.NewLine, generatedType);

        Assert.Contains("public System.DateOnly SomeDate", code);
        Assert.Contains("public System.TimeOnly SomeTime", code);
        Assert.DoesNotContain("DataType=\"date\"", code);
        Assert.DoesNotContain("DataType=\"time\"", code);
    }
}
