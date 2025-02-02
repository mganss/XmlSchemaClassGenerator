using System.Linq;
using Xunit;

namespace XmlSchemaClassGenerator.Tests;

public class NullableGenerationTests
{
    const string NullablePattern = "xsd/nullable/*.xsd";

    [Fact]
    public void No_nullable_support()
    {
        var assembly = Compiler.Generate(nameof(No_nullable_support), NullablePattern, new Generator
        {
            GenerateNullables = false,
            NamespaceProvider = new NamespaceProvider
            {
                GenerateNamespace = key => "Test"
            }
        });

        var optionalPropertyTypeProperties = assembly.GetType("Test.HasOptionalProperty")
            .GetProperties()
            .Select(p => (p.Name, p.PropertyType))
            .OrderBy(p => p.Name)
            .ToArray();

        Assert.Equal(new[]
        {
            ("Id", typeof(int)),
            ("IdSpecified", typeof(bool))
        }, optionalPropertyTypeProperties);

        var optionalAttributeTypeProperties = assembly.GetType("Test.HasOptionalAttribute")
        .GetProperties()
        .Select(p => (p.Name, p.PropertyType))
        .OrderBy(p => p.Name)
        .ToArray();

        Assert.Equal(new[]
        {
            ("Id", typeof(int)),
            ("IdSpecified", typeof(bool))
        }, optionalAttributeTypeProperties);
    }

    [Fact]
    public void Nullable_support()
    {
        var assembly = Compiler.Generate(nameof(Nullable_support), NullablePattern, new Generator
        {
            GenerateNullables = true,
            NamespaceProvider = new NamespaceProvider
            {
                GenerateNamespace = key => "Test"
            }
        });

        var optionalPropertyTypeProperties = assembly.GetType("Test.HasOptionalProperty")
            .GetProperties()
            .Select(p => (p.Name, p.PropertyType))
            .OrderBy(p => p.Name)
            .ToArray();

        Assert.Equal(new[]
        {
            ("Id", typeof(int?)),
            ("IdValue", typeof(int)),
            ("IdValueSpecified", typeof(bool))
        }, optionalPropertyTypeProperties);


        var optionalAttributeTypeProperties = assembly.GetType("Test.HasOptionalAttribute")
          .GetProperties()
          .Select(p => (p.Name, p.PropertyType))
          .OrderBy(p => p.Name)
          .ToArray();

        Assert.Equal(new[]
        {
            ("Id", typeof(int?)),
            ("IdValue", typeof(int)),
            ("IdValueSpecified", typeof(bool))
        }, optionalAttributeTypeProperties);
    }

    [Fact]
    public void ShouldSerialize_nullable_support()
    {
        var assembly = Compiler.Generate(nameof(ShouldSerialize_nullable_support), NullablePattern, new Generator
        {
            GenerateNullables = true,
            UseShouldSerializePattern = true,
            NamespaceProvider = new NamespaceProvider
            {
                GenerateNamespace = key => "Test"
            }
        });

        var optionalPropertyType = assembly.GetType("Test.HasOptionalProperty");

        var optionalPropertyTypeProperties = optionalPropertyType
            .GetProperties()
            .Select(p => (p.Name, p.PropertyType))
            .OrderBy(p => p.Name)
            .ToArray();

        Assert.Equal(new[]
        {
            ("Id", typeof(int?)),
        }, optionalPropertyTypeProperties);

        Assert.Equal(
        [
            "ShouldSerializeId"
        ],
        optionalPropertyType.GetMethods().Where(m => m.Name.StartsWith("ShouldSerialize") && m.ReturnType == typeof(bool)).Select(m => m.Name));


        var optionalAttributeTypeProperties = assembly.GetType("Test.HasOptionalAttribute")
          .GetProperties()
          .Select(p => (p.Name, p.PropertyType))
          .OrderBy(p => p.Name)
          .ToArray();

        // ShouldSerialize pattern does not work on XML Attributes
        Assert.Equal(new[]
        {
            ("Id", typeof(int?)),
            ("IdValue", typeof(int)),
            ("IdValueSpecified", typeof(bool))
        }, optionalAttributeTypeProperties);
    }
}
