using System.IO;
using System.Xml.Serialization;
using Xunit;

namespace XmlSchemaClassGenerator.Tests;

public class ElementTextDefaultTests
{
    /// <summary>
    /// The default of an optional element with simple content applies to the text value of the element,
    /// so the element must stay null when absent and get the default only when present but empty.
    /// </summary>
    [Fact]
    public void OptionalElementDefaultIsAppliedToTextValue()
    {
        var assembly = Compiler.Generate(nameof(OptionalElementDefaultIsAppliedToTextValue), "xsd/ElementTextDefault/*.xsd", new Generator
        {
            NamespaceProvider = new NamespaceProvider { GenerateNamespace = _ => "Test" }
        });

        var geometryType = assembly.GetType("Test.Geometry");
        Assert.NotNull(geometryType);
        var serializer = new XmlSerializer(geometryType);

        var geometry = System.Activator.CreateInstance(geometryType);
        Assert.Null(geometryType.GetProperty("ThicknessReduction").GetValue(geometry));

        const string absent = "<geometry />";
        var absentObject = serializer.Deserialize(new StringReader(absent));
        Assert.Null(geometryType.GetProperty("ThicknessReduction").GetValue(absentObject));

        var writer = new StringWriter();
        serializer.Serialize(writer, absentObject);
        Assert.DoesNotContain("thicknessReduction", writer.ToString());

        const string empty = "<geometry><thicknessReduction reference=\"Bottom\" /></geometry>";
        var emptyObject = serializer.Deserialize(new StringReader(empty));
        var reduction = geometryType.GetProperty("ThicknessReduction").GetValue(emptyObject);
        Assert.NotNull(reduction);
        Assert.Equal(false, reduction.GetType().GetProperty("Value").GetValue(reduction));
        Assert.Equal("Bottom", reduction.GetType().GetProperty("Reference").GetValue(reduction).ToString());

        const string explicitValue = "<geometry><thicknessReduction reference=\"Top\">true</thicknessReduction></geometry>";
        var explicitObject = serializer.Deserialize(new StringReader(explicitValue));
        reduction = geometryType.GetProperty("ThicknessReduction").GetValue(explicitObject);
        Assert.Equal(true, reduction.GetType().GetProperty("Value").GetValue(reduction));
    }
}
