using System;
using System.CodeDom;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices.ComTypes;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;
using System.Xml.XPath;
using Ganss.IO;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.Xml.XMLGen;
using Xunit;
using Xunit.Abstractions;

namespace XmlSchemaClassGenerator.Tests;

[TestCaseOrderer("XmlSchemaClassGenerator.Tests.PriorityOrderer", "XmlSchemaClassGenerator.Tests")]
public class XmlTests(ITestOutputHelper output)
{
    private readonly ITestOutputHelper Output = output;

    private static IEnumerable<string> ConvertXml(string name, IEnumerable<string> xsds, Generator generatorPrototype = null)
    {
        ArgumentNullException.ThrowIfNull(name);

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
            CodeTypeReferenceOptions = generatorPrototype.CodeTypeReferenceOptions,
            DoNotForceIsNullable = generatorPrototype.DoNotForceIsNullable,
            CreateGeneratedCodeAttributeVersion = generatorPrototype.CreateGeneratedCodeAttributeVersion,
            NetCoreSpecificCode = generatorPrototype.NetCoreSpecificCode,
            GenerateCommandLineArgumentsComment = generatorPrototype.GenerateCommandLineArgumentsComment,
            CommandLineArgumentsProvider = generatorPrototype.CommandLineArgumentsProvider,
            CollectionType = generatorPrototype.CollectionType,
            CollectionImplementationType = generatorPrototype.CollectionImplementationType,
            CollectionSettersMode = generatorPrototype.CollectionSettersMode,
            UseArrayItemAttribute = generatorPrototype.UseArrayItemAttribute,
            EnumAsString = generatorPrototype.EnumAsString,
            AllowDtdParse = generatorPrototype.AllowDtdParse
        };

        gen.CommentLanguages.Clear();
        gen.CommentLanguages.UnionWith(generatorPrototype.CommentLanguages);

        gen.Generate(xsds.Select(i => new StringReader(i)));

        return writer.Content;
    }

    private static IEnumerable<string> ConvertXml(string name, string xsd, Generator generatorPrototype = null)
    {
        return ConvertXml(name, [xsd], generatorPrototype);
    }

    const string IS24Pattern = "xsd/is24/*/*.xsd";
    const string IS24ImmoTransferPattern = "xsd/is24immotransfer/is24immotransfer.xsd";
    const string WadlPattern = "xsd/wadl/wadl.xsd";
    const string ListPattern = "xsd/list/list.xsd";
    const string SimplePattern = "xsd/simple/*.xsd";
    const string ArrayOrderPattern = "xsd/array-order/array-order.xsd";
    const string ClientPattern = "xsd/client/client.xsd";
    const string IataPattern = "xsd/iata/*.xsd";
    const string TimePattern = "xsd/time/time.xsd";
    const string TableauPattern = "xsd/ts-api/*.xsd";
    const string VSTstPattern = "xsd/vstst/vstst.xsd";
    const string BpmnPattern = "xsd/bpmn/*.xsd";
    const string DtsxPattern = "xsd/dtsx/dtsx2.xsd";
    const string WfsPattern = "xsd/wfs/schemas.opengis.net/wfs/2.0/wfs.xsd";
    const string EppPattern = "xsd/epp/*.xsd";
    const string GraphMLPattern = "xsd/graphml/ygraphml.xsd";
    const string UnionPattern = "xsd/union/union.xsd";
    const string GuidPattern = "xsd/guid/*.xsd";
    const string NullableReferenceAttributesPattern = "xsd/nullablereferenceattributes/nullablereference.xsd";
    const string X3DPattern = "xsd/x3d/*.xsd";

    [Fact, TestPriority(1)]
    [UseCulture("en-US")]
    public void TestIata()
    {
        Compiler.Generate("Iata", IataPattern, new Generator
        {
            EntityFramework = true,
            DataAnnotationMode = DataAnnotationMode.All,
            NamespaceProvider = new Dictionary<NamespaceKey, string> { { new NamespaceKey(""), "XmlSchema" }, { new NamespaceKey("http://www.iata.org/IATA/EDIST/2017.2"), "Iata" } }
                .ToNamespaceProvider(new GeneratorConfiguration { NamespacePrefix = "Iata" }.NamespaceProvider.GenerateNamespace),
            MemberVisitor = (member, model) => { },
            GenerateInterfaces = true
        });
        var typesToTest = new List<XmlQualifiedName> { new("AirShoppingRS", "http://www.iata.org/IATA/EDIST/2017.2") };
        SharedTestFunctions.TestSimple(Output, "Iata", IataPattern, typesToTest);
    }

    [Fact, TestPriority(1)]
    [UseCulture("en-US")]
    public void TestGraphML()
    {
        Compiler.Generate("GraphML", GraphMLPattern, new Generator
        {
            NamespaceProvider = new Dictionary<NamespaceKey, string> {
                { new NamespaceKey(new Uri("graphml.xsd", UriKind.RelativeOrAbsolute), "http://graphml.graphdrawing.org/xmlns"), "GraphML.Main" },
                { new NamespaceKey(new Uri("graphml-structure.xsd", UriKind.RelativeOrAbsolute), "http://graphml.graphdrawing.org/xmlns"), "GraphML.Structure" },
                { new NamespaceKey(new Uri("ygraphxml.xsd", UriKind.RelativeOrAbsolute), "http://graphml.graphdrawing.org/xmlns"), "GraphML.Y" },
                { new NamespaceKey("http://www.w3.org/1999/xlink"), "XLink" },
                { new NamespaceKey("http://www.yworks.com/xml/graphml"), "YEd" },
            }.ToNamespaceProvider(new GeneratorConfiguration { NamespacePrefix = "GraphML" }.NamespaceProvider.GenerateNamespace),
        });
        SharedTestFunctions.TestSamples(Output, "GraphML", GraphMLPattern);
    }

    [Fact, TestPriority(1)]
    [UseCulture("en-US")]
    public void TestClient()
    {
        Compiler.Generate("Client", ClientPattern);
        SharedTestFunctions.TestSamples(Output, "Client", ClientPattern);
    }

    [Fact, TestPriority(1)]
    [UseCulture("en-US")]
    public void TestX3D()
    {
        Compiler.Generate("X3D", X3DPattern);
        var typesToTest = new List<XmlQualifiedName> { new("GeoLocation") };
        SharedTestFunctions.TestSimple(Output, "X3D", X3DPattern, typesToTest);
    }

    [Fact, TestPriority(1)]
    [UseCulture("en-US")]
    public void TestGuid()
    {
        var assembly = Compiler.Generate("Guid", GuidPattern);
        var testType = assembly.GetType("Guid.Test");
        var idProperty = testType.GetProperty("Id");
        var elementIdProperty = testType.GetProperty("ElementId");
        var headerType = assembly.GetType("Guid.V1.Header");
        var referenceProperty = headerType.GetProperty("Reference");

        Assert.Equal(typeof(Nullable<>).MakeGenericType(typeof(Guid)), idProperty.PropertyType);
        Assert.Equal(typeof(Guid), elementIdProperty.PropertyType);
        Assert.Equal(typeof(Guid), referenceProperty.PropertyType);

        var serializer = new XmlSerializer(testType);

        var test = Activator.CreateInstance(testType);
        var idGuid = Guid.NewGuid();
        var elementGuid = Guid.NewGuid();

        idProperty.SetValue(test, idGuid);
        elementIdProperty.SetValue(test, elementGuid);

        var sw = new StringWriter();

        serializer.Serialize(sw, test);

        var xml = sw.ToString();
        var sr = new StringReader(xml);

        var o = serializer.Deserialize(sr);

        Assert.Equal(idGuid, idProperty.GetValue(o));
        Assert.Equal(elementGuid, elementIdProperty.GetValue(o));
    }

    [Fact, TestPriority(1)]
    [UseCulture("en-US")]
    public void TestUnion()
    {
        var assembly = Compiler.Generate("Union", UnionPattern, new Generator
        {
            NamespacePrefix = "Union",
            IntegerDataType = typeof(int),
            MapUnionToWidestCommonType = true
        });

        Assert.NotNull(assembly);

        SharedTestFunctions.TestSamples(Output, "Union", UnionPattern);

        var snapshotType = assembly.GetType("Union.Snapshot");
        Assert.NotNull(snapshotType);

        var date = snapshotType.GetProperty("Date");
        Assert.NotNull(date);
        Assert.Equal(typeof(DateTime), date.PropertyType);

        var count = snapshotType.GetProperty("Count");
        Assert.NotNull(count);
        Assert.Equal(typeof(int), count.PropertyType);

        var num = snapshotType.GetProperty("Num");
        Assert.NotNull(num);
        Assert.Equal(typeof(decimal), num.PropertyType);
    }

    [Fact, TestPriority(1)]
    [UseCulture("en-US")]
    public void TestList()
    {
        Compiler.Generate("List", ListPattern);
        SharedTestFunctions.TestSamples(Output, "List", ListPattern);
    }

    [Fact]
    public void TestListWithPrivatePropertySetters()
    {
        var assembly = Compiler.Generate("List", ListPattern, new Generator() {
            GenerateNullables = true,
            IntegerDataType = typeof(int),
            DataAnnotationMode = DataAnnotationMode.All,
            GenerateDesignerCategoryAttribute = false,
            GenerateComplexTypesForCollections = true,
            EntityFramework = false,
            GenerateInterfaces = true,
            NamespacePrefix = "List",
            GenerateDescriptionAttribute = true,
            TextValuePropertyName = "Value",
            CollectionSettersMode = CollectionSettersMode.Private
        });
        Assert.NotNull(assembly);
        var myClassType = assembly.GetType("List.MyClass");
        Assert.NotNull(myClassType);
        var iListType = typeof(Collection<>);
        var collectionPropertyInfos = myClassType.GetProperties().Where(p => p.PropertyType.IsGenericType && iListType.IsAssignableFrom(p.PropertyType.GetGenericTypeDefinition())).OrderBy(p=>p.Name).ToList();
        var publicCollectionPropertyInfos = collectionPropertyInfos.Where(p => p.SetMethod.IsPrivate).OrderBy(p=>p.Name).ToList();
        Assert.NotEmpty(collectionPropertyInfos);
        Assert.Equal(collectionPropertyInfos, publicCollectionPropertyInfos);

        var myClassInstance = Activator.CreateInstance(myClassType);
        foreach (var collectionPropertyInfo in publicCollectionPropertyInfos)
        {
            Assert.NotNull(collectionPropertyInfo.GetValue(myClassInstance));
        }
    }

    [Fact]
    public void TestListWithPublicPropertySetters()
    {
        var assembly = Compiler.Generate("ListPublic", ListPattern, new Generator {
            GenerateNullables = true,
            IntegerDataType = typeof(int),
            DataAnnotationMode = DataAnnotationMode.All,
            GenerateDesignerCategoryAttribute = false,
            GenerateComplexTypesForCollections = true,
            EntityFramework = false,
            GenerateInterfaces = true,
            NamespacePrefix = "List",
            GenerateDescriptionAttribute = true,
            TextValuePropertyName = "Value",
            CollectionSettersMode = CollectionSettersMode.Public
        });
        Assert.NotNull(assembly);
        var myClassType = assembly.GetType("List.MyClass");
        Assert.NotNull(myClassType);
        var iListType = typeof(Collection<>);
        var collectionPropertyInfos = myClassType.GetProperties().Where(p => p.PropertyType.IsGenericType && iListType.IsAssignableFrom(p.PropertyType.GetGenericTypeDefinition())).OrderBy(p=>p.Name).ToList();
        var publicCollectionPropertyInfos = collectionPropertyInfos.Where(p => p.SetMethod.IsPublic).OrderBy(p=>p.Name).ToList();
        Assert.NotEmpty(collectionPropertyInfos);
        Assert.Equal(collectionPropertyInfos, publicCollectionPropertyInfos);

        var myClassInstance = Activator.CreateInstance(myClassType);
        foreach (var collectionPropertyInfo in publicCollectionPropertyInfos)
        {
            Assert.NotNull(collectionPropertyInfo.GetValue(myClassInstance));
        }
    }

    [Fact]
    public void TestListWithPublicPropertySettersWithoutConstructors()
    {
        var assembly = Compiler.Generate("ListPublicWithoutConstructorInitialization", ListPattern, new Generator
        {
            GenerateNullables = true,
            IntegerDataType = typeof(int),
            DataAnnotationMode = DataAnnotationMode.All,
            GenerateDesignerCategoryAttribute = false,
            GenerateComplexTypesForCollections = true,
            EntityFramework = false,
            GenerateInterfaces = true,
            NamespacePrefix = "List",
            GenerateDescriptionAttribute = true,
            TextValuePropertyName = "Value",
            CollectionSettersMode = CollectionSettersMode.PublicWithoutConstructorInitialization
        });
        Assert.NotNull(assembly);
        var myClassType = assembly.GetType("List.MyClass");
        Assert.NotNull(myClassType);
        var iListType = typeof(Collection<>);
        var collectionPropertyInfos = myClassType.GetProperties().Where(p => p.PropertyType.IsGenericType && iListType.IsAssignableFrom(p.PropertyType.GetGenericTypeDefinition())).OrderBy(p => p.Name).ToList();
        var publicCollectionPropertyInfos = collectionPropertyInfos.Where(p => p.SetMethod.IsPublic).OrderBy(p => p.Name).ToList();
        Assert.NotEmpty(collectionPropertyInfos);
        Assert.Equal(collectionPropertyInfos, publicCollectionPropertyInfos);
        var myClassInstance = Activator.CreateInstance(myClassType);
        foreach (var collectionPropertyInfo in publicCollectionPropertyInfos)
        {
            Assert.Null(collectionPropertyInfo.GetValue(myClassInstance));
        }
    }

    [Fact]
    public void TestListWithInitPropertySetters()
    {
        var assembly = Compiler.Generate("ListPublic", ListPattern, new Generator
        {
            GenerateNullables = true,
            IntegerDataType = typeof(int),
            DataAnnotationMode = DataAnnotationMode.All,
            GenerateDesignerCategoryAttribute = false,
            GenerateComplexTypesForCollections = true,
            EntityFramework = false,
            GenerateInterfaces = true,
            NamespacePrefix = "List",
            GenerateDescriptionAttribute = true,
            TextValuePropertyName = "Value",
            CollectionSettersMode = CollectionSettersMode.Init
        });
        Assert.NotNull(assembly);
        var myClassType = assembly.GetType("List.MyClass");
        Assert.NotNull(myClassType);
        var iListType = typeof(Collection<>);
        var collectionPropertyInfos = myClassType.GetProperties().Where(p => p.PropertyType.IsGenericType && iListType.IsAssignableFrom(p.PropertyType.GetGenericTypeDefinition())).OrderBy(p => p.Name).ToList();
        var publicCollectionPropertyInfos = collectionPropertyInfos.Where(p => p.SetMethod.IsPublic).OrderBy(p => p.Name).ToList();
        Assert.NotEmpty(collectionPropertyInfos);
        Assert.Equal(collectionPropertyInfos, publicCollectionPropertyInfos);
        var requiredCustomModifiers = collectionPropertyInfos.Select(p => p.SetMethod.ReturnParameter.GetRequiredCustomModifiers()).ToList();
        Assert.Equal(collectionPropertyInfos.Count, requiredCustomModifiers.Count);
        Assert.All(requiredCustomModifiers, m => Assert.Contains(typeof(System.Runtime.CompilerServices.IsExternalInit), m));

        var myClassInstance = Activator.CreateInstance(myClassType);
        foreach (var collectionPropertyInfo in publicCollectionPropertyInfos)
        {
            Assert.NotNull(collectionPropertyInfo.GetValue(myClassInstance));
        }
    }

    [Fact]
    public void TestListWithInitPropertySettersWithoutConstructors()
    {
        var assembly = Compiler.Generate("ListInitWithoutConstructorInitialization", ListPattern, new Generator
        {
            GenerateNullables = true,
            IntegerDataType = typeof(int),
            DataAnnotationMode = DataAnnotationMode.All,
            GenerateDesignerCategoryAttribute = false,
            GenerateComplexTypesForCollections = true,
            EntityFramework = false,
            GenerateInterfaces = true,
            NamespacePrefix = "List",
            GenerateDescriptionAttribute = true,
            TextValuePropertyName = "Value",
            CollectionSettersMode = CollectionSettersMode.InitWithoutConstructorInitialization
        });
        Assert.NotNull(assembly);
        var myClassType = assembly.GetType("List.MyClass");
        Assert.NotNull(myClassType);
        var iListType = typeof(Collection<>);
        var collectionPropertyInfos = myClassType.GetProperties().Where(p => p.PropertyType.IsGenericType && iListType.IsAssignableFrom(p.PropertyType.GetGenericTypeDefinition())).OrderBy(p => p.Name).ToList();
        var publicCollectionPropertyInfos = collectionPropertyInfos.Where(p => p.SetMethod.IsPublic).OrderBy(p => p.Name).ToList();
        Assert.NotEmpty(collectionPropertyInfos);
        Assert.Equal(collectionPropertyInfos, publicCollectionPropertyInfos);
        var requiredCustomModifiers = collectionPropertyInfos.Select(p => p.SetMethod.ReturnParameter.GetRequiredCustomModifiers()).ToList();
        Assert.Equal(collectionPropertyInfos.Count, requiredCustomModifiers.Count);
        Assert.All(requiredCustomModifiers, m => Assert.Contains(typeof(System.Runtime.CompilerServices.IsExternalInit), m));
        var myClassInstance = Activator.CreateInstance(myClassType);
        foreach (var collectionPropertyInfo in publicCollectionPropertyInfos)
        {
            Assert.Null(collectionPropertyInfo.GetValue(myClassInstance));
        }
    }

    [Fact]
    public void TestListWithPublicPropertySettersWithoutConstructorsSpecified()
    {
        var assembly = Compiler.Generate("ListPublicWithoutConstructorInitialization", ListPattern, new Generator
        {
            GenerateNullables = true,
            IntegerDataType = typeof(int),
            DataAnnotationMode = DataAnnotationMode.All,
            GenerateDesignerCategoryAttribute = false,
            GenerateComplexTypesForCollections = true,
            EntityFramework = false,
            GenerateInterfaces = true,
            NamespacePrefix = "List",
            GenerateDescriptionAttribute = true,
            TextValuePropertyName = "Value",
            CollectionSettersMode = CollectionSettersMode.PublicWithoutConstructorInitialization
        });
        Assert.NotNull(assembly);
        var myClassType = assembly.GetType("List.MyClass");
        Assert.NotNull(myClassType);
        var iListType = typeof(Collection<>);
        var collectionPropertyInfos = myClassType.GetProperties().Where(p => p.PropertyType.IsGenericType && iListType.IsAssignableFrom(p.PropertyType.GetGenericTypeDefinition())).OrderBy(p => p.Name).ToList();
        var publicCollectionPropertyInfos = collectionPropertyInfos.Where(p => p.SetMethod.IsPublic).OrderBy(p => p.Name).ToList();
        Assert.NotEmpty(collectionPropertyInfos);
        Assert.Equal(collectionPropertyInfos, publicCollectionPropertyInfos);
        var myClassInstance = Activator.CreateInstance(myClassType);

        var propertyNamesWithSpecifiedPostfix = publicCollectionPropertyInfos.Select(p => p.Name + "Specified").ToHashSet();
        var propertiesWithSpecifiedPostfix =
            myClassType.GetProperties().Where(p => propertyNamesWithSpecifiedPostfix.Contains(p.Name)).ToList();

        //Null collection
        foreach (var propertyInfo in propertiesWithSpecifiedPostfix)
        {
            Assert.False((bool)propertyInfo.GetValue(myClassInstance));
        }

        foreach (var propertyInfo in publicCollectionPropertyInfos)
        {
            var collection = Activator.CreateInstance(propertyInfo.PropertyType);
            propertyInfo.SetValue(myClassInstance, collection);
        }

        //Not Null but empty collection
        foreach (var propertyInfo in propertiesWithSpecifiedPostfix)
        {
            Assert.False((bool)propertyInfo.GetValue(myClassInstance));
        }

        foreach (var propertyInfo in publicCollectionPropertyInfos)
        {

            var collection = Activator.CreateInstance(propertyInfo.PropertyType);
            propertyInfo.PropertyType.InvokeMember("Add", BindingFlags.Public | BindingFlags.InvokeMethod | BindingFlags.Instance, null, collection,
                [null]);
            propertyInfo.SetValue(myClassInstance, collection);
        }

        //Not Null and not empty collection
        foreach (var propertyInfo in propertiesWithSpecifiedPostfix)
        {
            Assert.True((bool)propertyInfo.GetValue(myClassInstance));
        }
    }

    public static TheoryData<CodeTypeReferenceOptions, NamingScheme, Type> TestSimpleData() {
        var theoryData = new TheoryData<CodeTypeReferenceOptions, NamingScheme, Type>();
        foreach (var referenceMode in new[]
            { CodeTypeReferenceOptions.GlobalReference, /*CodeTypeReferenceOptions.GenericTypeParameter*/ })
        foreach (var namingScheme in new[] { NamingScheme.Direct, NamingScheme.PascalCase })
        foreach (var collectionType in new[] { typeof(Collection<>), /*typeof(Array)*/ })
        {
            theoryData.Add(referenceMode, namingScheme, collectionType);
        }
        return theoryData;
    }


    [Theory, TestPriority(1)]
    [MemberData(nameof(TestSimpleData))]
    [UseCulture("en-US")]
    public void TestSimple(CodeTypeReferenceOptions referenceMode, NamingScheme namingScheme, Type collectionType)
    {
        var name = $"Simple_{referenceMode}_{namingScheme}_{collectionType}";
        Compiler.Generate(name, SimplePattern, new Generator
        {
            GenerateNullables = true,
            IntegerDataType = typeof(int),
            DataAnnotationMode = DataAnnotationMode.All,
            GenerateDesignerCategoryAttribute = false,
            GenerateComplexTypesForCollections = true,
            EntityFramework = false,
            GenerateInterfaces = true,
            NamespacePrefix = "Simple",
            GenerateDescriptionAttribute = true,
            CodeTypeReferenceOptions = referenceMode,
            NetCoreSpecificCode = true,
            NamingScheme = namingScheme,
            CollectionType = collectionType,
            CollectionSettersMode = CollectionSettersMode.Public
        });
        SharedTestFunctions.TestSamples(Output, name, SimplePattern);
    }

    [Fact, TestPriority(1)]
    [UseCulture("en-US")]
    public void TestArrayOrder()
    {
        Compiler.Generate("ArrayOrder", ArrayOrderPattern, new Generator
        {
            NamespacePrefix = "ArrayOrder",
            EmitOrder = true
        });
        SharedTestFunctions.TestSamples(Output, "ArrayOrder", ArrayOrderPattern);
    }

    [Fact, TestPriority(1)]
    [UseCulture("en-US")]
    public void TestIS24RestApi()
    {
        Compiler.Generate("IS24RestApi", IS24Pattern, new Generator
        {
            GenerateNullables = true,
            IntegerDataType = typeof(int),
            DataAnnotationMode = DataAnnotationMode.All,
            GenerateDesignerCategoryAttribute = false,
            GenerateComplexTypesForCollections = true,
            EntityFramework = false,
            GenerateInterfaces = true,
            NamespacePrefix = "IS24RestApi",
            GenerateDescriptionAttribute = true,
            TextValuePropertyName = "Value",
            CompactTypeNames = true
        });
        SharedTestFunctions.TestSamples(Output, "IS24RestApi", IS24Pattern);
    }

    [Fact, TestPriority(1)]
    [UseCulture("en-US")]
    public void TestIS24RestApiShouldSerialize()
    {
        Compiler.Generate("IS24RestApiShouldSerialize", IS24Pattern, new Generator
        {
            GenerateNullables = true,
            GenerateInterfaces = true,
            NamespacePrefix = "IS24RestApi",
            GenerateDescriptionAttribute = true,
            UseShouldSerializePattern = true
        });
        SharedTestFunctions.TestSamples(Output, "IS24RestApiShouldSerialize", IS24Pattern);
    }

    [Fact, TestPriority(1)]
    [UseCulture("en-US")]
    public void TestWadl()
    {
        Compiler.Generate("Wadl", WadlPattern, new Generator
        {
            EntityFramework = true,
            DataAnnotationMode = DataAnnotationMode.All,
            NamespaceProvider = new Dictionary<NamespaceKey, string> { { new NamespaceKey("http://wadl.dev.java.net/2009/02"), "Wadl" } }.ToNamespaceProvider(new GeneratorConfiguration { NamespacePrefix = "Wadl" }.NamespaceProvider.GenerateNamespace),
            MemberVisitor = (member, model) => { }
        });
        SharedTestFunctions.TestSamples(Output, "Wadl", WadlPattern);
    }

    [Fact, TestPriority(1)]
    [UseCulture("en-US")]
    public void TestIS24ImmoTransfer()
    {
        Compiler.Generate("IS24ImmoTransfer", IS24ImmoTransferPattern);
        SharedTestFunctions.TestSamples(Output, "IS24ImmoTransfer", IS24ImmoTransferPattern);

        Compiler.Generate("IS24ImmoTransferSeparate", IS24ImmoTransferPattern, new Generator
        {
            SeparateSubstitutes = true
        });
        SharedTestFunctions.TestSamples(Output, "IS24ImmoTransferSeparate", IS24ImmoTransferPattern);
    }

    [Fact, TestPriority(1)]
    [UseCulture("en-US")]
    public void TestTableau()
    {
        Compiler.Generate("Tableau", TableauPattern, new Generator { CompactTypeNames = true });
        SharedTestFunctions.TestSamples(Output, "Tableau", TableauPattern);
    }

    [Fact, TestPriority(1)]
    [UseCulture("en-US")]
    public void TestSeparateClasses()
    {
        var output = new FileWatcherOutputWriter(Path.Combine("output", "Tableau.Separate"));
        Compiler.Generate("Tableau.Separate", TableauPattern,
            new Generator
            {
                OutputWriter = output,
                SeparateClasses = true,
                EnableDataBinding = true
            });
        SharedTestFunctions.TestSamples(Output, "Tableau.Separate", TableauPattern);
    }

    [Fact, TestPriority(1)]
    [UseCulture("en-US")]
    public void TestArray()
    {
        var output = new FileWatcherOutputWriter(Path.Combine("output", "Tableau.Array"));
        Compiler.Generate("Tableau.Array", TableauPattern,
            new Generator
            {
                OutputWriter = output,
                EnableDataBinding = true,
                CollectionType = typeof(System.Array),
                CollectionSettersMode = CollectionSettersMode.Public
            });
        SharedTestFunctions.TestSamples(Output, "Tableau.Array", TableauPattern);
    }

    [Fact, TestPriority(1)]
    [UseCulture("en-US")]
    public void TestSerializeEmptyCollection()
    {
        var output = new FileWatcherOutputWriter(Path.Combine("output", "Tableau.EmptyCollection"));
        Compiler.Generate("Tableau.EmptyCollection", TableauPattern,
            new Generator
            {
                OutputWriter = output,
                EnableDataBinding = true,
                CollectionSettersMode = CollectionSettersMode.Private,
                SerializeEmptyCollections = true
            });
        SharedTestFunctions.TestSamples(Output, "Tableau.EmptyCollection", TableauPattern);
    }

    [Fact, TestPriority(1)]
    [UseCulture("en-US")]
    public void TestSerializeEmptyPublicCollection()
    {
        var output = new FileWatcherOutputWriter(Path.Combine("output", "Tableau.EmptyPublicCollection"));
        Compiler.Generate("Tableau.EmptyPublicCollection", TableauPattern,
            new Generator
            {
                OutputWriter = output,
                EnableDataBinding = true,
                CollectionSettersMode = CollectionSettersMode.Public,
                SerializeEmptyCollections = true
            });
        SharedTestFunctions.TestSamples(Output, "Tableau.EmptyPublicCollection", TableauPattern);
    }

    [Fact, TestPriority(1)]
    [UseCulture("en-US")]
    public void TestDtsx()
    {
        Compiler.Generate("Dtsx", DtsxPattern, new Generator());
        SharedTestFunctions.TestSamples(Output, "Dtsx", DtsxPattern);
    }

    [Fact, TestPriority(1)]
    [UseCulture("en-US")]
    public void TestVSTst()
    {
        Compiler.Generate("VSTst", VSTstPattern, new Generator
        {
            TextValuePropertyName = "TextValue",
            GenerateComplexTypesForCollections = true
        });
        SharedTestFunctions.TestSamples(Output, "VSTst", VSTstPattern);
    }

    [Fact, TestPriority(1)]
    [UseCulture("en-US")]
    public void TestWfs()
    {
        var output = new FileWatcherOutputWriter(Path.Combine("output", "wfs"));
        Compiler.Generate("wfs", WfsPattern,
            new Generator
            {
                OutputWriter = output,
                EmitOrder = true,
                GenerateInterfaces = true
            });
        SharedTestFunctions.TestSamples(Output, "wfs", WfsPattern);
    }

    [Fact, TestPriority(1)]
    [UseCulture("en-US")]
    public void TestEpp()
    {
        var output = new FileWatcherOutputWriter(Path.Combine("output", "epp"));
        Compiler.Generate("epp", EppPattern,
            new Generator
            {
                OutputWriter = output,
                GenerateInterfaces = false,
                UniqueTypeNamesAcrossNamespaces = true,
                NamespaceProvider = new Dictionary<NamespaceKey, string>
                {
                    { new NamespaceKey("urn:ietf:params:xml:ns:eppcom-1.0"), "FoxHillSolutions.Escrow.eppcom" },
                    { new NamespaceKey("urn:ietf:params:xml:ns:rdeDomain-1.0"), "FoxHillSolutions.Escrow.rdeDomain" },
                    { new NamespaceKey("urn:ietf:params:xml:ns:rdeHeader-1.0"), "FoxHillSolutions.Escrow.rdeHeader" },
                    { new NamespaceKey("urn:ietf:params:xml:ns:rdeHost-1.0"), "FoxHillSolutions.Escrow.rdeHost" },
                    { new NamespaceKey("urn:ietf:params:xml:ns:rdeIDN-1.0"), "FoxHillSolutions.Escrow.rdeIDN" },
                    { new NamespaceKey("urn:ietf:params:xml:ns:rdeRegistrar-1.0"), "FoxHillSolutions.Escrow.Registrar" },
                    { new NamespaceKey("urn:ietf:params:xml:ns:rdeDnrdCommon-1.0"), "FoxHillSolutions.Escrow.DnrdCommon" },
                    { new NamespaceKey("urn:ietf:params:xml:ns:secDNS-1.1"), "FoxHillSolutions.Escrow.secDNS" },
                    { new NamespaceKey("urn:ietf:params:xml:ns:domain-1.0"), "FoxHillSolutions.Escrow.domain" },
                    { new NamespaceKey("urn:ietf:params:xml:ns:contact-1.0"), "FoxHillSolutions.Escrow.contact" },
                    { new NamespaceKey("urn:ietf:params:xml:ns:host-1.0"), "FoxHillSolutions.Escrow.host" },
                    { new NamespaceKey("urn:ietf:params:xml:ns:rgp-1.0"), "FoxHillSolutions.Escrow.rgp" },
                    { new NamespaceKey("urn:ietf:params:xml:ns:epp-1.0"), "FoxHillSolutions.Escrow.epp" },
                    { new NamespaceKey("urn:ietf:params:xml:ns:rde-1.0"), "FoxHillSolutions.Escrow.rde" },
                    { new NamespaceKey("urn:ietf:params:xml:ns:rdeContact-1.0"), "FoxHillSolutions.Escrow.rdeContact" },
                    { new NamespaceKey("urn:ietf:params:xml:ns:rdeEppParams-1.0"), "FoxHillSolutions.Escrow.rdeEppParams" },
                    { new NamespaceKey("urn:ietf:params:xml:ns:rdeNNDN-1.0"), "FoxHillSolutions.Escrow.rdeNNDN" },
                    { new NamespaceKey("urn:ietf:params:xml:ns:rdePolicy-1.0"), "FoxHillSolutions.Escrow.rdePolicy" },
                }.ToNamespaceProvider(new GeneratorConfiguration { NamespacePrefix = "Epp" }.NamespaceProvider.GenerateNamespace),
            });
        SharedTestFunctions.TestSamples(Output, "epp", EppPattern);
    }

    [Fact, TestPriority(1)]
    [UseCulture("en-US")]
    public void TestXbrl()
    {
        var outputPath = Path.Combine("output", "xbrl");

        var gen = new Generator
        {
            OutputFolder = outputPath,
            GenerateInterfaces = false,
            UniqueTypeNamesAcrossNamespaces = true,
        };

        gen.NamespaceProvider.Add(new NamespaceKey("http://www.xbrl.org/2003/XLink"), "XbrlLink");

        var xsdFiles = new[] { Path.Combine(Directory.GetCurrentDirectory(), "xsd", "xbrl", "xhtml-inlinexbrl-1_1.xsd") };

        var assembly = Compiler.GenerateFiles("Xbrl", xsdFiles, gen);
        Assert.NotNull(assembly);

        var testFiles = new Dictionary<string, string>
        {
            { "Schaltbau.xhtml", "XhtmlHtmlType" },
            { "GLEIF Annual Accounts.html", "XhtmlHtmlType" },
        };

        foreach (var testFile in testFiles)
        {
            var type = assembly.GetTypes().SingleOrDefault(t => t.Name == testFile.Value);
            Assert.NotNull(type);

            var serializer = new XmlSerializer(type);
            serializer.UnknownNode += new XmlNodeEventHandler(UnknownNodeHandler);
            serializer.UnknownAttribute += new XmlAttributeEventHandler(UnknownAttributeHandler);
            var unknownNodeError = false;
            var unknownAttrError = false;

            void UnknownNodeHandler(object sender, XmlNodeEventArgs e)
            {
                unknownNodeError = true;
            }

            void UnknownAttributeHandler(object sender, XmlAttributeEventArgs e)
            {
                unknownAttrError = true;
            }

            var xmlString = File.ReadAllText($"xml/xbrl_tests/{testFile.Key}");
            xmlString = Regex.Replace(xmlString, "xsi:schemaLocation=\"[^\"]*\"", string.Empty);
            var reader = XmlReader.Create(new StringReader(xmlString), new XmlReaderSettings { IgnoreWhitespace = true });

            var isDeserializable = serializer.CanDeserialize(reader);
            Assert.True(isDeserializable);

            var deserializedObject = serializer.Deserialize(reader);
            Assert.False(unknownNodeError);
            Assert.False(unknownAttrError);

            var serializedXml = SharedTestFunctions.Serialize(serializer, deserializedObject, SharedTestFunctions.GetNamespacesFromSource(xmlString));

            var deserializedXml = serializer.Deserialize(new StringReader(serializedXml));
            AssertEx.Equal(deserializedObject, deserializedXml);
        }
    }

    private static readonly XmlQualifiedName AnyType = new("anyType", XmlSchema.Namespace);

    public static TheoryData<string> Classes =>
    [
        "ApartmentBuy",
        "ApartmentRent",
        "AssistedLiving",
        "CompulsoryAuction",
        "GarageBuy",
        "GarageRent",
        "Gastronomy",
        "HouseBuy",
        "HouseRent",
        "HouseType",
        "Industry",
        "Investment",
        "LivingBuySite",
        "LivingRentSite",
        "Office",
        "SeniorCare",
        "ShortTermAccommodation",
        "SpecialPurpose",
        "Store",
        "TradeSite",
    ];

    [Theory, TestPriority(2)]
    [MemberData(nameof(Classes))]
    public void ProducesSameXmlAsXsd(string c)
    {
        var assembly = Compiler.Generate("IS24RestApi", IS24Pattern);

        var t1 = assembly.GetTypes().SingleOrDefault(t => t.Name == c && t.Namespace.StartsWith("IS24RestApi.Offer.Realestates"));
        Assert.NotNull(t1);
        var t2 = Assembly.GetExecutingAssembly().GetTypes().SingleOrDefault(t => t.Name == c && t.Namespace == "IS24RestApi.Xsd");
        Assert.NotNull(t2);
        var f = char.ToLower(c[0]) + c[1..];
        TestCompareToXsd(t1, t2, f);
    }

    static void TestCompareToXsd(Type t1, Type t2, string file)
    {
        foreach (var suffix in new[] { "max", "min" })
        {
            var serializer1 = new XmlSerializer(t1);
            var serializer2 = new XmlSerializer(t2);
            var xml = ReadXml(string.Format("{0}_{1}", file, suffix));
            var o1 = serializer1.Deserialize(new StringReader(xml));
            var o2 = serializer2.Deserialize(new StringReader(xml));
            var x1 = SharedTestFunctions.Serialize(serializer1, o1);
            var x2 = SharedTestFunctions.Serialize(serializer2, o2);

            File.WriteAllText("x1.xml", x1);
            File.WriteAllText("x2.xml", x2);

            Assert.Equal(x2, x1);
        }
    }

    [Fact, TestPriority(1)]
    [UseCulture("en-US")]
    public void TestCustomNamespaces()
    {
        string customNsPattern = "|{0}={1}";
        string bpmnXsd = "BPMN20.xsd";
        string semantXsd = "Semantic.xsd";
        string bpmndiXsd = "BPMNDI.xsd";
        string dcXsd = "DC.xsd";
        string diXsd = "DI.xsd";

        Dictionary<string, string> xsdToCsharpNsMap = new()
        {
            { bpmnXsd, "Namespace1" },
            { semantXsd, "Namespace1" },
            { bpmndiXsd, "Namespace2" },
            { dcXsd, "Namespace3" },
            { diXsd, "Namespace4" }
        };

        Dictionary<string, string> xsdToCsharpTypeMap = new()
        {
            { bpmnXsd, "TDefinitions" },
            { semantXsd, "TActivity" },
            { bpmndiXsd, "BpmnDiagram" },
            { dcXsd, "Font" },
            { diXsd, "DiagramElement" }
        };

        List<string> customNamespaceConfig = [];

        foreach (var ns in xsdToCsharpNsMap)
            customNamespaceConfig.Add(string.Format(customNsPattern, ns.Key, ns.Value));

        var assembly = Compiler.Generate("Bpmn", BpmnPattern, new Generator
        {
            DataAnnotationMode = DataAnnotationMode.All,
            GenerateNullables = true,
            MemberVisitor = (member, model) => { },
            NamespaceProvider = customNamespaceConfig.Select(n => CodeUtilities.ParseNamespace(n, null)).ToNamespaceProvider()
        });
        Assert.NotNull(assembly);

        Type type = null;

        type = assembly.GetTypes().SingleOrDefault(t => t.Name == xsdToCsharpTypeMap[bpmnXsd]);
        Assert.NotNull(type);
        Assert.Equal(xsdToCsharpNsMap[bpmnXsd], type.Namespace);

        type = assembly.GetTypes().SingleOrDefault(t => t.Name == xsdToCsharpTypeMap[semantXsd]);
        Assert.NotNull(type);
        Assert.Equal(xsdToCsharpNsMap[semantXsd], type.Namespace);

        type = assembly.GetTypes().SingleOrDefault(t => t.Name == xsdToCsharpTypeMap[bpmndiXsd]);
        Assert.NotNull(type);
        Assert.Equal(xsdToCsharpNsMap[bpmndiXsd], type.Namespace);

        type = assembly.GetTypes().SingleOrDefault(t => t.Name == xsdToCsharpTypeMap[dcXsd]);
        Assert.NotNull(type);
        Assert.Equal(xsdToCsharpNsMap[dcXsd], type.Namespace);

        type = assembly.GetTypes().SingleOrDefault(t => t.Name == xsdToCsharpTypeMap[diXsd]);
        Assert.NotNull(type);
        Assert.Equal(xsdToCsharpNsMap[diXsd], type.Namespace);
    }

    [Fact, TestPriority(2)]
    [UseCulture("en-US")]
    public void TestBpmn()
    {
        PerformBpmnTest("Bpmn");
        PerformBpmnTest("BpmnSeparate", new Generator
        {
            SeparateSubstitutes = true
        });
    }

    private void PerformBpmnTest(string name, Generator generator = null)
    {
        var assembly = Compiler.Generate(name, BpmnPattern, generator);
        Assert.NotNull(assembly);

        var type = assembly.GetTypes().SingleOrDefault(t => t.Name == "TDefinitions");
        Assert.NotNull(type);

        var serializer = new XmlSerializer(type);
        serializer.UnknownNode += new XmlNodeEventHandler(UnknownNodeHandler);
        serializer.UnknownAttribute += new XmlAttributeEventHandler(UnknownAttributeHandler);
        var unknownNodeError = false;
        var unknownAttrError = false;

        void UnknownNodeHandler(object sender, XmlNodeEventArgs e)
        {
            unknownNodeError = true;
        }

        void UnknownAttributeHandler(object sender, XmlAttributeEventArgs e)
        {
            unknownAttrError = true;
        }

        var testFiles = Glob.ExpandNames(Path.Combine("xml", "bpmn_tests", "*.bpmn"));

        foreach (var testFile in testFiles)
        {
            var xmlString = File.ReadAllText(testFile);
            var reader = XmlReader.Create(new StringReader(xmlString), new XmlReaderSettings { IgnoreWhitespace = true });

            var isDeserializable = serializer.CanDeserialize(reader);
            Assert.True(isDeserializable);

            var deserializedObject = serializer.Deserialize(reader);
            Assert.False(unknownNodeError);
            Assert.False(unknownAttrError);

            var serializedXml = SharedTestFunctions.Serialize(serializer, deserializedObject, SharedTestFunctions.GetNamespacesFromSource(xmlString));

            var deserializedXml = serializer.Deserialize(new StringReader(serializedXml));
            AssertEx.Equal(deserializedObject, deserializedXml);
        }
    }

    [Theory, TestPriority(3)]
    [MemberData(nameof(Classes))]
    public void CanSerializeAndDeserializeAllExampleXmlFiles(string c)
    {
        var assembly = Compiler.Generate("IS24RestApi", IS24Pattern);

        var t1 = assembly.GetTypes().SingleOrDefault(t => t.Name == c && t.Namespace.StartsWith("IS24RestApi.Offer.Realestates"));
        Assert.NotNull(t1);
        var f = char.ToLower(c[0]) + c[1..];
        TestRoundtrip(t1, f);
    }

    static void TestRoundtrip(Type t, string file)
    {
        var serializer = new XmlSerializer(t);

        foreach (var suffix in new[] { "min", "max" })
        {
            var xml = ReadXml(string.Format("{0}_{1}", file, suffix));

            var deserializedObject = serializer.Deserialize(new StringReader(xml));

            var serializedXml = SharedTestFunctions.Serialize(serializer, deserializedObject);

            var deserializedXml = serializer.Deserialize(new StringReader(serializedXml));
            AssertEx.Equal(deserializedObject, deserializedXml);
        }
    }

    static string ReadXml(string name)
    {
        var xml = File.ReadAllText(Path.Combine("xml", name + ".xml"));
        return xml;
    }

    [Fact]
    public void DontGenerateElementForEmptyCollectionInChoice()
    {
        var assembly = Compiler.Generate("Tableau", TableauPattern, new Generator());
        Assert.NotNull(assembly);
        var requestType = assembly.GetType("Api.TsRequest");
        Assert.NotNull(requestType);
        var r = Activator.CreateInstance(requestType);
        var s = new XmlSerializer(requestType);
        var sw = new StringWriter();
        s.Serialize(sw, r);
        var xml = sw.ToString();
        Assert.DoesNotContain("tags", xml, StringComparison.OrdinalIgnoreCase);
    }


    [Theory]
    [InlineData(CodeTypeReferenceOptions.GlobalReference, "[global::System.ComponentModel.EditorBrowsableAttribute(global::System.ComponentModel.EditorBrowsableState.Never)]")]
    [InlineData((CodeTypeReferenceOptions)0, "[System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]")]
    public void EditorBrowsableAttributeRespectsCodeTypeReferenceOptions(CodeTypeReferenceOptions codeTypeReferenceOptions, string expectedLine)
    {
        const string xsd = @"<?xml version=""1.0"" encoding=""UTF-8""?>
<xs:schema elementFormDefault=""qualified"" xmlns:xs=""http://www.w3.org/2001/XMLSchema"">
		<xs:complexType name=""document"">
			<xs:attribute name=""some-value"">
				<xs:simpleType>
					<xs:restriction base=""xs:string"">
						<xs:enumeration value=""one""/>
						<xs:enumeration value=""two""/>
					</xs:restriction>
				</xs:simpleType>
			</xs:attribute>
			<xs:attribute name=""system"" type=""xs:string""/>
		</xs:complexType>
</xs:schema>";

        var generatedType = ConvertXml(nameof(EditorBrowsableAttributeRespectsCodeTypeReferenceOptions), xsd, new Generator
        {
            CodeTypeReferenceOptions = codeTypeReferenceOptions,
            GenerateNullables = true,
            GenerateInterfaces = true,
            NamespaceProvider = new NamespaceProvider
            {
                GenerateNamespace = key => "Test"
            }
        });

        Assert.Contains(
            expectedLine,
            generatedType.First());
    }

    [Fact]
    public void MixedTypeMustNotCollideWithExistingMembers()
    {
        const string xsd = @"<?xml version=""1.0"" encoding=""UTF-8""?>
<xs:schema elementFormDefault=""qualified"" xmlns:xs=""http://www.w3.org/2001/XMLSchema"" targetNamespace=""http://local.none"" xmlns:l=""http://local.none"">
	<xs:element name=""document"" type=""l:elem"">
	</xs:element>
	<xs:complexType name=""elem"" mixed=""true"">
		<xs:attribute name=""Text"" type=""xs:string""/>
	</xs:complexType>
</xs:schema>";

        var generatedType = ConvertXml(nameof(MixedTypeMustNotCollideWithExistingMembers), xsd, new Generator
        {
            NamespaceProvider = new NamespaceProvider
            {
                GenerateNamespace = key => "Test"
            }
        });

        Assert.Contains(
            @"public string[] Text_1 { get; set; }",
            generatedType.First());
    }

    [Fact]
    public void MixedTypeMustNotCollideWithContainingTypeName()
    {
        const string xsd = @"<?xml version=""1.0"" encoding=""UTF-8""?>
<xs:schema elementFormDefault=""qualified"" xmlns:xs=""http://www.w3.org/2001/XMLSchema"" targetNamespace=""http://local.none"" xmlns:l=""http://local.none"">
	<xs:element name=""document"" type=""l:Text"">
	</xs:element>
	<xs:complexType name=""Text"" mixed=""true"">
	</xs:complexType>
</xs:schema>";

        var generatedType = ConvertXml(nameof(MixedTypeMustNotCollideWithExistingMembers), xsd, new Generator
        {
            NamespaceProvider = new NamespaceProvider
            {
                GenerateNamespace = key => "Test"
            }
        });

        Assert.Contains(
            @"public string[] Text_1 { get; set; }",
            generatedType.First());
    }

    [Theory]
    [InlineData(@"xml/sameattributenames.xsd", @"xml/sameattributenames_import.xsd")]
    public void CollidingAttributeAndPropertyNamesCanBeResolved(params string[] files)
    {
        // Compilation would previously throw due to duplicate type name within type
        var assembly = Compiler.GenerateFiles("AttributesWithSameName", files);

        Assert.NotNull(assembly);
    }

    [Fact]
    public void CollidingElementAndComplexTypeNamesCanBeResolved()
    {
        const string xsd = @"<?xml version=""1.0"" encoding = ""UTF-8""?>
<xs:schema elementFormDefault=""qualified"" xmlns:xs=""http://www.w3.org/2001/XMLSchema"" targetNamespace=""http://local.none"">
  <xs:complexType name=""MyType"">
    <xs:sequence>
      <xs:element maxOccurs=""1"" minOccurs=""0"" name=""output"" type=""xs:string""/>
    </xs:sequence>
  </xs:complexType>
  <xs:element name=""MyType"">
    <xs:simpleType>
      <xs:restriction base=""xs:string"">
        <xs:enumeration value=""Choice1""/>
        <xs:enumeration value=""Choice2""/>
        <xs:enumeration value=""Choice3""/>
      </xs:restriction>
    </xs:simpleType>
  </xs:element>
</xs:schema>
";
        var generator = new Generator
        {
            NamespaceProvider = new NamespaceProvider
            {
                GenerateNamespace = key => "Test"
            }
        };

        var generatedType = ConvertXml(nameof(CollidingElementAndComplexTypeNamesCanBeResolved), xsd, generator).First();

        Assert.Contains(@"public partial class MyType", generatedType);
        Assert.Contains(@"public enum MyType2", generatedType);
    }

    [Fact]
    public void EnumAsStringOption()
    {
        const string xsd = @"<?xml version=""1.0"" encoding = ""UTF-8""?>
<xs:schema elementFormDefault=""qualified"" xmlns:xs=""http://www.w3.org/2001/XMLSchema"" targetNamespace=""http://local.none"">
	<xs:element name=""Authorisation"">
		<xs:complexType>
			<xs:sequence>
				<xs:element name=""type"">
					<xs:simpleType>
						<xs:restriction base=""xs:string"">
							<xs:enumeration value=""C019""/>
							<xs:enumeration value=""C512""/>
							<xs:enumeration value=""C513""/>
							<xs:enumeration value=""C514""/>
						</xs:restriction>
					</xs:simpleType>
				</xs:element>
			</xs:sequence>
		</xs:complexType>
	</xs:element>
</xs:schema>
";
        var generator = new Generator
        {
            NamespaceProvider = new NamespaceProvider
            {
                GenerateNamespace = key => "Test"
            },
            EnumAsString = true
        };

        var generatedType = ConvertXml(nameof(EnumAsStringOption), xsd, generator).First();

        Assert.Contains(@"public string Type", generatedType);
    }

    [Fact]
    public void ComplexTypeWithAttributeGroupExtension()
    {
        const string xsd = @"<?xml version=""1.0"" encoding=""UTF-8""?>
<xs:schema xmlns:xs=""http://www.w3.org/2001/XMLSchema"" xmlns:xlink=""http://www.w3.org/1999/xlink"" elementFormDefault=""qualified"" attributeFormDefault=""unqualified"">
  <xs:attributeGroup name=""justify"">
    <xs:attribute name=""justify"" type=""simpleType""/>
  </xs:attributeGroup>
  <xs:complexType name=""group-name"">
    <xs:simpleContent>
      <xs:extension base=""xs:string"">
        <xs:attributeGroup ref=""justify""/>
      </xs:extension>
    </xs:simpleContent>
  </xs:complexType>
  <xs:simpleType name=""simpleType"">
    <xs:restriction base=""xs:token"">
      <xs:enumeration value=""foo""/>
    </xs:restriction>
  </xs:simpleType>
</xs:schema>";

        var generator = new Generator
        {
            GenerateInterfaces = false,
            NamespaceProvider = new NamespaceProvider
            {
                GenerateNamespace = key => "Test"
            },
            CommentLanguages = { "de", "en" },
            CreateGeneratedCodeAttributeVersion = false
        };

        var contents = ConvertXml(nameof(ComplexTypeWithAttributeGroupExtension), xsd, generator);

        var csharp = Assert.Single(contents);

        CompareOutput(
            @"//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

// This code was generated by Tests
namespace Test
{


    [System.CodeDom.Compiler.GeneratedCodeAttribute(""Tests"", """")]
    [System.SerializableAttribute()]
    [System.Xml.Serialization.XmlTypeAttribute(""group-name"", Namespace="""")]
    [System.ComponentModel.DesignerCategoryAttribute(""code"")]
    public partial class GroupName
    {

        /// <summary>
        /// <para xml:lang=""de"">Ruft den Text ab oder legt diesen fest.</para>
        /// <para xml:lang=""en"">Gets or sets the text value.</para>
        /// </summary>
        [System.Xml.Serialization.XmlTextAttribute()]
        public string Value { get; set; }

        [System.Xml.Serialization.XmlAttributeAttribute(""justify"")]
        public SimpleType Justify { get; set; }

        /// <summary>
        /// <para xml:lang=""de"">Ruft einen Wert ab, der angibt, ob die Justify-Eigenschaft spezifiziert ist, oder legt diesen fest.</para>
        /// <para xml:lang=""en"">Gets or sets a value indicating whether the Justify property is specified.</para>
        /// </summary>
        [System.Xml.Serialization.XmlIgnoreAttribute()]
        public bool JustifySpecified { get; set; }
    }

    [System.CodeDom.Compiler.GeneratedCodeAttribute(""Tests"", """")]
    [System.SerializableAttribute()]
    [System.Xml.Serialization.XmlTypeAttribute(""simpleType"", Namespace="""")]
    public enum SimpleType
    {

        [System.Xml.Serialization.XmlEnumAttribute(""foo"")]
        Foo,
    }
}
", csharp);
    }

    [Fact]
    public void ChoiceMembersAreNullable()
    {
        // We test to see whether choices which are part of a larger ComplexType are marked as nullable.
        // Because nullability isn't directly exposed in the generated C#, we use "XXXSpecified" on a value type
        // as a proxy.

        const string xsd = @"<?xml version=""1.0"" encoding=""UTF-8""?>
<xs:schema xmlns:xs=""http://www.w3.org/2001/XMLSchema"" xmlns:xlink=""http://www.w3.org/1999/xlink"" elementFormDefault=""qualified"" attributeFormDefault=""unqualified"">
    <xs:complexType name=""Root"">
      <xs:sequence>
      <!-- Choice directly inside a complex type -->
      <xs:element name=""Sub"">
        <xs:complexType>
          <xs:choice>
              <xs:element name=""Opt1"" type=""xs:int""/>
              <xs:element name=""Opt2"" type=""xs:int""/>
          </xs:choice>
        </xs:complexType>
        </xs:element>
        <!-- Choice as part of a larger sequence -->
        <xs:choice>
          <xs:element name=""Opt3"" type=""xs:int""/>
          <xs:element name=""Opt4"" type=""xs:int""/>
        </xs:choice>
      </xs:sequence>
    </xs:complexType>
</xs:schema>";

        var generator = new Generator
        {
            NamespaceProvider = new NamespaceProvider
            {
                GenerateNamespace = key => "Test"
            }
        };
        var contents = ConvertXml(nameof(ComplexTypeWithAttributeGroupExtension), xsd, generator);
        var content = Assert.Single(contents);

        Assert.Contains("Opt1Specified", content);
        Assert.Contains("Opt2Specified", content);
        Assert.Contains("Opt3Specified", content);
        Assert.Contains("Opt4Specified", content);
    }

    [Fact]
    public void NestedElementInChoiceIsNullable()
    {
        // Because nullability isn't directly exposed in the generated C#, we use "XXXSpecified" on a value type
        // as a proxy.
        const string xsd = @"<?xml version=""1.0"" encoding=""UTF-8""?>
<xs:schema xmlns:xs=""http://www.w3.org/2001/XMLSchema"">
  <xs:element name=""Root"">
    <xs:complexType>
      <xs:choice>
        <xs:sequence>
          <xs:element name=""ElementA"" type=""xs:int""/>
        </xs:sequence>
        <xs:group ref=""Group""/>
      </xs:choice>
    </xs:complexType>
  </xs:element>

  <xs:group name=""Group"">
    <xs:sequence>
      <xs:element name=""ElementB"" type=""xs:int""/>
    </xs:sequence>
  </xs:group>
</xs:schema>";

        var generator = new Generator
        {
            NamespaceProvider = new NamespaceProvider
            {
                GenerateNamespace = key => "Test"
            }
        };
        var contents = ConvertXml(nameof(NestedElementInChoiceIsNullable), xsd, generator);
        var content = Assert.Single(contents);

        Assert.Contains("ElementASpecified", content);
        Assert.Contains("ElementBSpecified", content);
    }

    [Fact]
    public void OnlyFirstElementOfNestedElementsIsForcedToNullableInChoice()
    {
        // Because nullability isn't directly exposed in the generated C#, we use the "RequiredAttribute"
        // as a proxy.
        const string xsd = @"<?xml version=""1.0"" encoding=""UTF-8""?>
<xs:schema xmlns:xs=""http://www.w3.org/2001/XMLSchema"">
  <xs:element name=""Root"">
    <xs:complexType>
      <xs:choice>
        <xs:element name=""ElementWithChild"">
          <xs:complexType>
            <xs:sequence>
              <xs:element name=""NestedChild"" type=""xs:int""/>
            </xs:sequence>
          </xs:complexType>
        </xs:element>
      </xs:choice>
    </xs:complexType>
  </xs:element>
</xs:schema>";

        var generator = new Generator
        {
            NamespaceProvider = new NamespaceProvider
            {
                GenerateNamespace = key => "Test"
            }
        };
        var contents = ConvertXml(nameof(OnlyFirstElementOfNestedElementsIsForcedToNullableInChoice), xsd, generator).ToArray();
        var assembly = Compiler.Compile(nameof(OnlyFirstElementOfNestedElementsIsForcedToNullableInChoice), contents);

        var elementWithChildProperty = assembly.GetType("Test.Root")?.GetProperty("ElementWithChild");
        var nestedChildProperty = assembly.GetType("Test.RootElementWithChild")?.GetProperty("NestedChild");
        Assert.NotNull(elementWithChildProperty);
        Assert.NotNull(nestedChildProperty);

        Type requiredType = typeof(System.ComponentModel.DataAnnotations.RequiredAttribute);
        bool elementWithChildIsRequired = Attribute.GetCustomAttribute(elementWithChildProperty, requiredType) != null;
        bool nestedChildIsRequired = Attribute.GetCustomAttribute(nestedChildProperty, requiredType) != null;
        Assert.False(elementWithChildIsRequired);
        Assert.True(nestedChildIsRequired);
    }

    [Fact]
    public void AssemblyVisibleIsInternalClass()
    {
        // We test to see whether choices which are part of a larger ComplexType are marked as nullable.
        // Because nullability isn't directly exposed in the generated C#, we use "XXXSpecified" on a value type
        // as a proxy.

        const string xsd = @"<?xml version=""1.0"" encoding=""UTF-8""?>
<xs:schema xmlns:xs=""http://www.w3.org/2001/XMLSchema"" xmlns:xlink=""http://www.w3.org/1999/xlink"" elementFormDefault=""qualified"" attributeFormDefault=""unqualified"">
    <xs:complexType name=""Root"">
      <xs:sequence>
      <!-- Choice directly inside a complex type -->
      <xs:element name=""Sub"">
        <xs:complexType>
          <xs:choice>
              <xs:element name=""Opt1"" type=""xs:int""/>
              <xs:element name=""Opt2"" type=""xs:int""/>
          </xs:choice>
        </xs:complexType>
        </xs:element>
        <!-- Choice as part of a larger sequence -->
        <xs:choice>
          <xs:element name=""Opt3"" type=""xs:int""/>
          <xs:element name=""Opt4"" type=""xs:int""/>
        </xs:choice>
      </xs:sequence>
    </xs:complexType>
</xs:schema>";

        var generator = new Generator
        {
            NamespaceProvider = new NamespaceProvider
            {
                GenerateNamespace = key => "Test"
            },
            AssemblyVisible = true
        };
        var contents = ConvertXml(nameof(ComplexTypeWithAttributeGroupExtension), xsd, generator);
        var content = Assert.Single(contents);

        Assert.Contains("internal partial class RootSub", content);
        Assert.Contains("internal partial class Root", content);
    }

    [Fact]
    public void AssemblyVisibleIsInternalEnum()
    {
        // We test to see whether choices which are part of a larger ComplexType are marked as nullable.
        // Because nullability isn't directly exposed in the generated C#, we use "XXXSpecified" on a value type
        // as a proxy.

        const string xsd = @"<?xml version=""1.0"" encoding=""UTF-8""?>
<xs:schema xmlns:xs=""http://www.w3.org/2001/XMLSchema"" xmlns:xlink=""http://www.w3.org/1999/xlink"" elementFormDefault=""qualified"" attributeFormDefault=""unqualified"">
  <xs:simpleType name=""Answer"">
    <xs:restriction base=""xs:string"">
      <xs:enumeration value=""Yes""/>
      <xs:enumeration value=""No""/>
      <xs:enumeration value=""Probably""/>
    </xs:restriction>
  </xs:simpleType>
</xs:schema>";

        var generator = new Generator
        {
            NamespaceProvider = new NamespaceProvider
            {
                GenerateNamespace = key => "Test"
            },
            AssemblyVisible = true
        };
        var contents = ConvertXml(nameof(ComplexTypeWithAttributeGroupExtension), xsd, generator);
        var content = Assert.Single(contents);

        Assert.Contains("internal enum Answer", content);
    }

    [Fact]
    public void AssemblyVisibleIsInternalInterface()
    {
        // We test to see whether choices which are part of a larger ComplexType are marked as nullable.
        // Because nullability isn't directly exposed in the generated C#, we use "XXXSpecified" on a value type
        // as a proxy.

        const string xsd = @"<?xml version=""1.0"" encoding=""UTF-8""?>
<xs:schema xmlns:tns=""http://test.test/schema/AssemblyVisibleIsInternalInterface"" targetNamespace=""http://test.test/schema/AssemblyVisibleIsInternalInterface"" xmlns:xs=""http://www.w3.org/2001/XMLSchema"" xmlns:xlink=""http://www.w3.org/1999/xlink"" elementFormDefault=""qualified"" attributeFormDefault=""unqualified"">
  <xs:complexType name=""NamedType"">
    <xs:attributeGroup ref=""tns:NamedElement""/>
  </xs:complexType>
  <xs:attributeGroup name=""NamedElement"">
    <xs:attribute name=""Name"" use=""required"" type=""xs:string"" />
  </xs:attributeGroup>
</xs:schema>";

        var generator = new Generator
        {
            NamespaceProvider = new NamespaceProvider
            {
                GenerateNamespace = key => "Test"
            },
            GenerateInterfaces = true,
            AssemblyVisible = true
        };
        var contents = ConvertXml(nameof(ComplexTypeWithAttributeGroupExtension), xsd, generator);
        var content = Assert.Single(contents);

        Assert.Contains("internal partial interface INamedElement", content);
    }

    [Fact]
    public void DecimalSeparatorTest()
    {
        // see https://github.com/mganss/XmlSchemaClassGenerator/issues/101

        const string xsd = @"<?xml version=""1.0"" encoding=""UTF-8""?>
<xs:schema xmlns:xs=""http://www.w3.org/2001/XMLSchema"" elementFormDefault=""qualified"" attributeFormDefault=""unqualified"">
  <xs:complexType name=""NamedType"">
    <xs:attribute name=""SomeAttr"" type=""xs:decimal"" default=""1.5"" />
  </xs:complexType>
</xs:schema>";

        var generator = new Generator
        {
            NamespaceProvider = new NamespaceProvider
            {
                GenerateNamespace = key => "Test",
            },
            GenerateInterfaces = true,
            AssemblyVisible = true
        };
        var contents = ConvertXml(nameof(ComplexTypeWithAttributeGroupExtension), xsd, generator);
        var content = Assert.Single(contents);

        Assert.Contains("private decimal _someAttr = 1.5m;", content);
    }

    [Fact]
    public void DoNotGenerateIntermediaryClassForArrayElements()
    {
        // see https://github.com/mganss/XmlSchemaClassGenerator/issues/32

        const string xsd = @"<?xml version=""1.0"" encoding=""UTF-8""?>
<xs:schema version=""1.0"" targetNamespace=""test""
  elementFormDefault=""qualified""
  xmlns:test=""test""
  xmlns:xs=""http://www.w3.org/2001/XMLSchema"">
  <xs:element name=""foo"" type=""test:foo""/>
<xs:complexType name=""foo"">
       <xs:sequence>
        <xs:element name=""bar"" minOccurs=""0"">
          <xs:complexType>
            <xs:sequence>
              <xs:element name=""baz"" type=""xs:string"" minOccurs=""0"" maxOccurs=""unbounded"">
              </xs:element>
            </xs:sequence>
          </xs:complexType>
        </xs:element>
      </xs:sequence>
</xs:complexType>
</xs:schema>";

        var generator = new Generator
        {
            NamespaceProvider = new NamespaceProvider
            {
                GenerateNamespace = key => "Test",
            },
            GenerateComplexTypesForCollections = false,
            GenerateInterfaces = true,
            AssemblyVisible = true
        };
        var contents = ConvertXml(nameof(ComplexTypeWithAttributeGroupExtension), xsd, generator);
        var content = Assert.Single(contents);

        var assembly = Compiler.Compile(nameof(DoNotGenerateIntermediaryClassForArrayElements), content);

        var fooType = assembly.DefinedTypes.Single(t => t.FullName == "Test.Foo");
        Assert.NotNull(fooType);
        Assert.Equal("Test.Foo", fooType.FullName);
    }

    [Fact]
    public void GenerateIntermediaryClassForArrayElements()
    {
        // see https://github.com/mganss/XmlSchemaClassGenerator/issues/32

        const string xsd = @"<?xml version=""1.0"" encoding=""UTF-8""?>
<xs:schema version=""1.0"" targetNamespace=""test""
  elementFormDefault=""qualified""
  xmlns:test=""test""
  xmlns:xs=""http://www.w3.org/2001/XMLSchema"">
  <xs:element name=""foo"" type=""test:foo""/>
<xs:complexType name=""foo"">
       <xs:sequence>
        <xs:element name=""bar"" minOccurs=""0"">
          <xs:complexType>
            <xs:sequence>
              <xs:element name=""baz"" type=""xs:string"" minOccurs=""0"" maxOccurs=""unbounded"">
              </xs:element>
            </xs:sequence>
          </xs:complexType>
        </xs:element>
      </xs:sequence>
</xs:complexType>
</xs:schema>";

        var generator = new Generator
        {
            NamespaceProvider = new NamespaceProvider
            {
                GenerateNamespace = key => "Test",
            },
            GenerateComplexTypesForCollections = true,
            GenerateInterfaces = true,
            AssemblyVisible = true
        };
        var contents = ConvertXml(nameof(GenerateIntermediaryClassForArrayElements), xsd, generator);
        var content = Assert.Single(contents);

        var assembly = Compiler.Compile(nameof(GenerateIntermediaryClassForArrayElements), content);

        Assert.Single(assembly.DefinedTypes, x => x.FullName == "Test.Foo");
        Assert.Single(assembly.DefinedTypes, x => x.FullName == "Test.FooBar");
    }

    [Fact]
    public void BoolTest()
    {
        // see https://github.com/mganss/XmlSchemaClassGenerator/issues/103

        const string xsd = @"<?xml version=""1.0"" encoding=""UTF-8""?>
<xs:schema xmlns:xs=""http://www.w3.org/2001/XMLSchema"" elementFormDefault=""qualified"" attributeFormDefault=""unqualified"">
  <xs:complexType name=""NamedType"">
    <xs:attribute name=""b0"" type=""xs:boolean"" default=""0"" />
    <xs:attribute name=""b1"" type=""xs:boolean"" default=""1"" />
    <xs:attribute name=""bf"" type=""xs:boolean"" default=""false"" />
    <xs:attribute name=""bt"" type=""xs:boolean"" default=""true"" />
  </xs:complexType>
</xs:schema>";

        var generator = new Generator
        {
            NamespaceProvider = new NamespaceProvider
            {
                GenerateNamespace = key => "Test",
            },
            GenerateInterfaces = true,
            AssemblyVisible = true
        };
        var contents = ConvertXml(nameof(ComplexTypeWithAttributeGroupExtension), xsd, generator);
        var content = Assert.Single(contents);

        Assert.Contains("private bool _b0 = false;", content);
        Assert.Contains("private bool _b1 = true;", content);
        Assert.Contains("private bool _bf = false;", content);
        Assert.Contains("private bool _bt = true;", content);
    }

    private static void CompareOutput(string expected, string actual)
    {
        static string Normalize(string input) => Regex.Replace(input, @"[ \t]*\r?\n", "\n");
        Assert.Equal(Normalize(expected), Normalize(actual));
    }

    [Theory]
    [InlineData(typeof(decimal), "decimal")]
    [InlineData(typeof(long), "long")]
    [InlineData(null, "string")]
    public void UnmappedIntegerDerivedTypesAreMappedToExpectedCSharpType(Type integerDataType, string expectedTypeName)
    {
        const string xsd = @"<?xml version=""1.0"" encoding=""UTF-8""?>
<xs:schema xmlns:xs=""http://www.w3.org/2001/XMLSchema"" elementFormDefault=""qualified"" attributeFormDefault=""unqualified"">
    <xs:complexType name=""root"">
        <xs:sequence>
	        <xs:element name=""unboundedInteger01"" type=""xs:integer""/>
	        <xs:element name=""unboundedInteger02"" type=""xs:nonNegativeInteger""/>
	        <xs:element name=""unboundedInteger03"" type=""xs:positiveInteger""/>
	        <xs:element name=""unboundedInteger04"" type=""xs:nonPositiveInteger""/>
	        <xs:element name=""unboundedInteger05"" type=""xs:negativeInteger""/>

	        <xs:element name=""outOfBoundsInteger01"" type=""tooLongPositiveInteger""/>
	        <xs:element name=""outOfBoundsInteger02"" type=""tooLongNonNegativeInteger""/>
	        <xs:element name=""outOfBoundsInteger03"" type=""tooLongInteger""/>
	        <xs:element name=""outOfBoundsInteger04"" type=""tooLongNegativeInteger""/>
	        <xs:element name=""outOfBoundsInteger05"" type=""tooLongNonPositiveInteger""/>
        </xs:sequence>
	</xs:complexType>

    <xs:simpleType name=""tooLongPositiveInteger"">
	    <xs:restriction base=""xs:positiveInteger"">
		    <xs:totalDigits value=""30""/>
	    </xs:restriction>
    </xs:simpleType>
    <xs:simpleType name=""tooLongNonNegativeInteger"">
	    <xs:restriction base=""xs:nonNegativeInteger"">
		    <xs:totalDigits value=""30""/>
	    </xs:restriction>
    </xs:simpleType>
    <xs:simpleType name=""tooLongInteger"">
	    <xs:restriction base=""xs:integer"">
		    <xs:totalDigits value=""29""/>
	    </xs:restriction>
    </xs:simpleType>
    <xs:simpleType name=""tooLongNegativeInteger"">
	    <xs:restriction base=""xs:negativeInteger"">
		    <xs:totalDigits value=""29""/>
	    </xs:restriction>
    </xs:simpleType>
    <xs:simpleType name=""tooLongNonPositiveInteger"">
	    <xs:restriction base=""xs:nonPositiveInteger"">
		    <xs:totalDigits value=""29""/>
	    </xs:restriction>
    </xs:simpleType>
</xs:schema>";

        var generatedType = ConvertXml(nameof(UnmappedIntegerDerivedTypesAreMappedToExpectedCSharpType), xsd, new Generator
        {
            NamespaceProvider = new NamespaceProvider
            {
                GenerateNamespace = key => "Test",
            },
            GenerateNullables = true,
            NamingScheme = NamingScheme.PascalCase,
            IntegerDataType = integerDataType,
        }).First();

        Assert.Contains($"public {expectedTypeName} UnboundedInteger01", generatedType);
        Assert.Contains($"public {expectedTypeName} UnboundedInteger02", generatedType);
        Assert.Contains($"public {expectedTypeName} UnboundedInteger03", generatedType);
        Assert.Contains($"public {expectedTypeName} UnboundedInteger04", generatedType);
        Assert.Contains($"public {expectedTypeName} UnboundedInteger05", generatedType);
        Assert.Contains($"public {expectedTypeName} OutOfBoundsInteger01", generatedType);
        Assert.Contains($"public {expectedTypeName} OutOfBoundsInteger02", generatedType);
        Assert.Contains($"public {expectedTypeName} OutOfBoundsInteger03", generatedType);
        Assert.Contains($"public {expectedTypeName} OutOfBoundsInteger04", generatedType);
        Assert.Contains($"public {expectedTypeName} OutOfBoundsInteger05", generatedType);
    }

    [Theory]
    [InlineData("xs:positiveInteger", 1, 2, "byte")]
    [InlineData("xs:nonNegativeInteger", 1, 2, "byte")]
    [InlineData("xs:integer", 1, 2, "sbyte")]
    [InlineData("xs:negativeInteger", 1, 2, "sbyte")]
    [InlineData("xs:nonPositiveInteger", 1, 2, "sbyte")]
    [InlineData("xs:positiveInteger", 3, 4, "ushort")]
    [InlineData("xs:nonNegativeInteger", 3, 4, "ushort")]
    [InlineData("xs:integer", 3, 4, "short")]
    [InlineData("xs:negativeInteger", 3, 4, "short")]
    [InlineData("xs:nonPositiveInteger", 3, 4, "short")]
    [InlineData("xs:positiveInteger", 5, 9, "uint")]
    [InlineData("xs:nonNegativeInteger", 5, 9, "uint")]
    [InlineData("xs:integer", 5, 9, "int")]
    [InlineData("xs:negativeInteger", 5, 9, "int")]
    [InlineData("xs:nonPositiveInteger", 5, 9, "int")]
    [InlineData("xs:positiveInteger", 10, 19, "ulong")]
    [InlineData("xs:nonNegativeInteger", 10, 19, "ulong")]
    [InlineData("xs:integer", 10, 18, "long")]
    [InlineData("xs:negativeInteger", 10, 18, "long")]
    [InlineData("xs:nonPositiveInteger", 10, 18, "long")]
    [InlineData("xs:positiveInteger", 20, 29, "decimal")]
    [InlineData("xs:nonNegativeInteger", 20, 29, "decimal")]
    [InlineData("xs:integer", 20, 28, "decimal")]
    [InlineData("xs:negativeInteger", 20, 28, "decimal")]
    [InlineData("xs:nonPositiveInteger", 20, 28, "decimal")]
    public void RestrictedIntegerDerivedTypesAreMappedToExpectedCSharpTypes(string restrictionBase, int totalDigitsRangeFrom, int totalDigitsRangeTo, string expectedTypeName)
    {
        const string xsdTemplate = @"<?xml version=""1.0"" encoding=""UTF-8""?>
<xs:schema xmlns:xs=""http://www.w3.org/2001/XMLSchema"" elementFormDefault=""qualified"" attributeFormDefault=""unqualified"">
    <xs:complexType name=""root"">
        <xs:sequence>
	        {0}
        </xs:sequence>
	</xs:complexType>

    {1}
</xs:schema>";

        const string elementTemplate = @"<xs:element name=""restrictedInteger{0}"" type=""RestrictedInteger{0}""/>";

        const string simpleTypeTemplate = @"
<xs:simpleType name=""RestrictedInteger{1}"">
	<xs:restriction base=""{0}"">
		<xs:totalDigits value=""{1}""/>
	</xs:restriction>
</xs:simpleType>
";

        string elementDefinitions = "", simpleTypeDefinitions = "";
        for (var i = totalDigitsRangeFrom; i <= totalDigitsRangeTo; i++)
        {
            elementDefinitions += string.Format(elementTemplate, i);
            simpleTypeDefinitions += string.Format(simpleTypeTemplate, restrictionBase, i);
        }

        var xsd = string.Format(xsdTemplate, elementDefinitions, simpleTypeDefinitions);
        var generatedType = ConvertXml(nameof(RestrictedIntegerDerivedTypesAreMappedToExpectedCSharpTypes), xsd,
            new Generator
            {
                NamespaceProvider = new NamespaceProvider
                {
                    GenerateNamespace = key => "Test",
                },
                GenerateNullables = true,
                NamingScheme = NamingScheme.PascalCase,
            }).First();

        for (var i = totalDigitsRangeFrom; i <= totalDigitsRangeTo; i++)
        {
            Assert.Contains($"public {expectedTypeName} RestrictedInteger{i}", generatedType);
        }
    }

    [Fact]
    public void EnumWithNonUniqueEntriesTest()
    {
        const string xsd = @"<?xml version=""1.0"" encoding=""UTF-8""?>
            <xs:schema xmlns:xs=""http://www.w3.org/2001/XMLSchema"" elementFormDefault=""qualified"" attributeFormDefault=""unqualified"">
				<xs:simpleType name=""TestEnum"">
					<xs:restriction base=""xs:string"">
					    <xs:enumeration value=""test_case""/>
					    <xs:enumeration value=""test_Case""/>
					    <xs:enumeration value=""Test_case""/>
					    <xs:enumeration value=""Test_Case""/>
					</xs:restriction>
				</xs:simpleType>
            </xs:schema>";

        var generator = new Generator
        {
            NamespaceProvider = new NamespaceProvider
            {
                GenerateNamespace = key => "Test"
            },
            GenerateInterfaces = true,
            AssemblyVisible = true
        };
        var contents = ConvertXml(nameof(EnumWithNonUniqueEntriesTest), xsd, generator);
        var content = Assert.Single(contents);

        var assembly = Compiler.Compile(nameof(EnumWithNonUniqueEntriesTest), content);
        var durationEnumType = assembly.GetType("Test.TestEnum");
        Assert.NotNull(durationEnumType);

        var expectedEnumValues = new[] {"TestCase", "TestCase1", "TestCase2", "TestCase3"};
        var enumValues = durationEnumType.GetEnumNames().OrderBy(n => n).ToList();
        Assert.Equal(expectedEnumValues, enumValues);

        var mEnumValue = durationEnumType.GetMembers().First(mi => mi.Name == "TestCase1");
        var xmlEnumAttribute = mEnumValue.GetCustomAttributes<XmlEnumAttribute>().FirstOrDefault();
        Assert.NotNull(xmlEnumAttribute);
        Assert.Equal("test_Case", xmlEnumAttribute.Name);
    }

    [Fact]
    public void RenameInterfacePropertyInDerivedClassTest()
    {
        const string xsd = @"<?xml version=""1.0"" encoding=""UTF-8""?>
            <xs:schema xmlns:xs=""http://www.w3.org/2001/XMLSchema""
                elementFormDefault=""qualified"" attributeFormDefault=""unqualified"">

                <xs:complexType name=""ClassItemBase"">
			        <xs:sequence>
                        <xs:group ref=""Level1""/>
                    </xs:sequence>
		        </xs:complexType>

	            <xs:element name=""ClassItem"">
		            <xs:complexType>
			            <xs:complexContent>
                            <xs:extension base=""ClassItemBase""/>
		                </xs:complexContent>
		            </xs:complexType>
                </xs:element>

                <xs:element name=""SomeType1"">
		            <xs:complexType>
			            <xs:group ref=""Level1""/>
		            </xs:complexType>
                </xs:element>

	            <xs:group name=""Level1"">
		            <xs:choice>
                        <xs:group ref=""Level2""/>
		            </xs:choice>
	            </xs:group>

	            <xs:group name=""Level2"">
		            <xs:choice>
			            <xs:group ref=""Level3""/>
		            </xs:choice>
	            </xs:group>

	            <xs:group name=""Level3"">
		            <xs:choice>
			            <xs:element name=""ClassItemBase"" type=""xs:string""/>
		            </xs:choice>
	            </xs:group>

            </xs:schema>";

        var generator = new Generator
        {
            NamespaceProvider = new NamespaceProvider
            {
                GenerateNamespace = key => "Test"
            },
            GenerateInterfaces = true,
            AssemblyVisible = true
        };
        var contents = ConvertXml(nameof(RenameInterfacePropertyInDerivedClassTest), xsd, generator);
        var content = Assert.Single(contents);

        var assembly = Compiler.Compile(nameof(RenameInterfacePropertyInDerivedClassTest), content);
        var classType = assembly.GetType("Test.ClassItem");
        Assert.NotNull(classType);
        Assert.Single(classType.GetProperties());
        Assert.Equal("ClassItemBaseProperty", classType.GetProperties().First().Name);

        var level1Interface = assembly.GetType("Test.ILevel1");
        Assert.NotNull(level1Interface);
        Assert.Empty(level1Interface.GetProperties());

        var level2Interface = assembly.GetType("Test.ILevel1");
        Assert.NotNull(level2Interface);
        Assert.Empty(level2Interface.GetProperties());

        var level3Interface = assembly.GetType("Test.ILevel3");
        Assert.NotNull(level3Interface);
        Assert.Single(level3Interface.GetProperties());
        Assert.Equal("ClassItemBaseProperty", level3Interface.GetProperties().First().Name);
    }

    [Fact]
    public void RefTypesGetNoXmlElementAttributeTest()
    {
        const string xsd = @"<?xml version=""1.0"" encoding=""utf-16""?>
<xs:schema xmlns=""SampleNamespace"" targetNamespace=""SampleNamespace"" version=""1.0"" xmlns:xs=""http://www.w3.org/2001/XMLSchema"">
  <xs:element name=""SampleRoot"">
    <xs:complexType>
      <xs:sequence>
        <xs:element name=""Direct"">
          <xs:complexType>
            <xs:sequence>
              <xs:element name=""Direct1"" type=""xs:string"" />
            </xs:sequence>
          </xs:complexType>
        </xs:element>
        <xs:element ref=""ViaRef"" />
      </xs:sequence>
    </xs:complexType>
  </xs:element>
  <xs:element name=""ViaRef"">
    <xs:complexType>
      <xs:sequence>
        <xs:element name=""ViaRef1"" type=""xs:string"" />
      </xs:sequence>
    </xs:complexType>
  </xs:element>
</xs:schema>";

        var generator = new Generator
        {
            NamespaceProvider = new NamespaceProvider
            {
                GenerateNamespace = key => "Test"
            },
            GenerateInterfaces = true,
            AssemblyVisible = true
        };
        var contents = ConvertXml(nameof(RefTypesGetNoXmlElementAttributeTest), xsd, generator);
        var content = Assert.Single(contents);

        var assembly = Compiler.Compile(nameof(RefTypesGetNoXmlElementAttributeTest), content);
        var classType = assembly.GetType("Test.SampleRoot");
        Assert.NotNull(classType);

        var directProperty = Assert.Single(classType.GetProperties(), p => p.Name == "Direct");
        Assert.Equal(XmlSchemaForm.Unqualified, directProperty.GetCustomAttributes<XmlElementAttribute>().FirstOrDefault()?.Form);

        var viaRefProperty = Assert.Single(classType.GetProperties(), p => p.Name == "ViaRef");
        Assert.Equal(XmlSchemaForm.None, viaRefProperty.GetCustomAttributes<XmlElementAttribute>().FirstOrDefault()?.Form);
    }

    [Fact]
    public void DoNotGenerateSamePropertiesInDerivedInterfacesClassTest()
    {
        const string xsd = @"<?xml version=""1.0"" encoding=""UTF-8""?>
            <xs:schema xmlns:xs=""http://www.w3.org/2001/XMLSchema""
                elementFormDefault=""qualified"" attributeFormDefault=""unqualified"">

	            <xs:element name=""ParentClass"">
		            <xs:complexType>
			            <xs:group ref=""Level1""/>
		            </xs:complexType>
                </xs:element>

	            <xs:group name=""Level1"">
		            <xs:choice>
                    <xs:sequence>
                        <xs:element name=""InterfaceProperty"" type=""xs:string""/>
                        <xs:group ref=""Level2""/>
                    </xs:sequence>
		            </xs:choice>
	            </xs:group>

	            <xs:group name=""Level2"">
                    <xs:sequence>
                        <xs:element name=""InterfaceProperty"" type=""xs:string""/>
                        <xs:group ref=""Level3""/>
                    </xs:sequence>
	            </xs:group>

	            <xs:group name=""Level22"">
                    <xs:sequence>
                        <xs:element name=""InterfaceProperty"" type=""xs:string""/>
                        <xs:element name=""Level22OwnProperty"" type=""xs:string""/>
                        <xs:group ref=""Level3""/>
                    </xs:sequence>
	            </xs:group>

	            <xs:group name=""Level3"">
		            <xs:choice>
			            <xs:element name=""InterfaceProperty"" type=""xs:string""/>
		            </xs:choice>
	            </xs:group>

            </xs:schema>";

        var generator = new Generator
        {
            NamespaceProvider = new NamespaceProvider
            {
                GenerateNamespace = key => "Test"
            },
            GenerateInterfaces = true,
            AssemblyVisible = true
        };
        var contents = ConvertXml(nameof(DoNotGenerateSamePropertiesInDerivedInterfacesClassTest), xsd, generator);
        var content = Assert.Single(contents);

        var assembly = Compiler.Compile(nameof(DoNotGenerateSamePropertiesInDerivedInterfacesClassTest), content);

        var listType = assembly.GetType("Test.ParentClass");
        Assert.NotNull(listType);

        var listTypePropertyInfo = listType.GetProperties().FirstOrDefault(p => p.Name == "InterfaceProperty");
        Assert.NotNull(listTypePropertyInfo);

        var level1Interface = assembly.GetType("Test.ILevel1");
        Assert.NotNull(level1Interface);
        Assert.Empty(level1Interface.GetProperties());

        var level2Interface = assembly.GetType("Test.ILevel2");
        Assert.NotNull(level2Interface);
        Assert.Empty(level2Interface.GetProperties());

        var level3Interface = assembly.GetType("Test.ILevel3");
        Assert.NotNull(level3Interface);
        var level3InterfacePropertyInfo = level3Interface.GetProperties().FirstOrDefault(p => p.Name == "InterfaceProperty");
        Assert.NotNull(level3InterfacePropertyInfo);

    }

    [Fact]
    public void NillableWithDefaultValueTest()
    {
        const string xsd = @"<?xml version=""1.0"" encoding=""UTF-8""?>
            <xs:schema xmlns:xs=""http://www.w3.org/2001/XMLSchema""
                elementFormDefault=""qualified"" attributeFormDefault=""unqualified"">

                <xs:complexType name=""TestType"">
                    <xs:sequence>
                        <xs:element name=""IntProperty"" type=""xs:int"" nillable=""true"" default=""9000"" />
                    </xs:sequence>
                </xs:complexType>

            </xs:schema>";

        var generator = new Generator
        {
            NamespaceProvider = new NamespaceProvider
            {
                GenerateNamespace = key => "Test"
            }
        };
        var contents = ConvertXml(nameof(NillableWithDefaultValueTest), xsd, generator);
        var content = Assert.Single(contents);

        var assembly = Compiler.Compile(nameof(NillableWithDefaultValueTest), content);

        var testType = assembly.GetType("Test.TestType");
        Assert.NotNull(testType);

        var propertyInfo = testType.GetProperties().FirstOrDefault(p => p.Name == "IntProperty");
        Assert.NotNull(propertyInfo);
        var testTypeInstance = Activator.CreateInstance(testType);
        var propertyDefaultValue = propertyInfo.GetValue(testTypeInstance);
        Assert.IsType<int>(propertyDefaultValue);
        Assert.Equal(9000, propertyDefaultValue);

        propertyInfo.SetValue(testTypeInstance, null);
        var serializer = new XmlSerializer(testType);

        var serializedXml = SharedTestFunctions.Serialize(serializer, testTypeInstance);
        Assert.Contains(
            @":nil=""true""",
            serializedXml);
    }

    [Fact]
    public void GenerateXmlRootAttributeForEnumTest()
    {
        const string xsd = @"<?xml version=""1.0"" encoding=""UTF-8""?>
            <xs:schema xmlns:xs=""http://www.w3.org/2001/XMLSchema"" targetNamespace=""http://test.namespace""
                elementFormDefault=""qualified"" attributeFormDefault=""unqualified"">

		        <xs:element name=""EnumTestType"">
		            <xs:simpleType>
					    <xs:restriction base=""xs:string"">
						    <xs:enumeration value=""EnumValue""/>
					    </xs:restriction>
				    </xs:simpleType>
	            </xs:element>

            </xs:schema>";

        var generator = new Generator
        {
            NamespaceProvider = new NamespaceProvider
            {
                GenerateNamespace = key => "Test"
            }
        };
        var contents = ConvertXml(nameof(GenerateXmlRootAttributeForEnumTest), xsd, generator);
        var content = Assert.Single(contents);

        var assembly = Compiler.Compile(nameof(GenerateXmlRootAttributeForEnumTest), content);

        var testType = assembly.GetType("Test.EnumTestType");
        Assert.NotNull(testType);
        var xmlRootAttribute = testType.GetCustomAttributes<XmlRootAttribute>().FirstOrDefault();
        Assert.NotNull(xmlRootAttribute);
        Assert.Equal("EnumTestType", xmlRootAttribute.ElementName);
        Assert.Equal("http://test.namespace", xmlRootAttribute.Namespace);
    }

    [Fact]
    public void AmbiguousTypesTest()
    {
        const string xsd1 = @"<?xml version=""1.0"" encoding=""UTF-8""?>
            <xs:schema xmlns:xs=""http://www.w3.org/2001/XMLSchema"" targetNamespace=""Test_NS1""
                elementFormDefault=""qualified"" attributeFormDefault=""unqualified"">

		        <xs:element name=""EnumTestType"">
		            <xs:simpleType>
					    <xs:restriction base=""xs:string"">
						    <xs:enumeration value=""EnumValue""/>
					    </xs:restriction>
				    </xs:simpleType>
	            </xs:element>

            </xs:schema>";
        const string xsd2 = @"<?xml version=""1.0"" encoding=""UTF-8""?>
            <xs:schema xmlns:xs=""http://www.w3.org/2001/XMLSchema"" targetNamespace=""Test_NS2""
                elementFormDefault=""qualified"" attributeFormDefault=""unqualified"">

		        <xs:element name=""EnumTestType"">
		            <xs:simpleType>
					    <xs:restriction base=""xs:string"">
						    <xs:enumeration value=""EnumValue""/>
					    </xs:restriction>
				    </xs:simpleType>
	            </xs:element>

            </xs:schema>";
        var generator = new Generator
        {
            NamespaceProvider = new NamespaceProvider
            {
                GenerateNamespace = key =>key.XmlSchemaNamespace
            }
        };
        var contents1 = ConvertXml(nameof(GenerateXmlRootAttributeForEnumTest), xsd1, generator);
        var contents2 = ConvertXml(nameof(GenerateXmlRootAttributeForEnumTest), xsd2, generator);
        var content1 = Assert.Single(contents1);
        var content2 = Assert.Single(contents2);

        var assembly = Compiler.Compile(nameof(GenerateXmlRootAttributeForEnumTest), content1, content2);

        var testType = assembly.GetType("Test_NS1.EnumTestType");
        Assert.NotNull(testType);
        var xmlRootAttribute = testType.GetCustomAttributes<XmlRootAttribute>().FirstOrDefault();
        Assert.NotNull(xmlRootAttribute);
        Assert.Equal("EnumTestType", xmlRootAttribute.ElementName);
        Assert.Equal("Test_NS1", xmlRootAttribute.Namespace);

        testType = assembly.GetType("Test_NS2.EnumTestType");
        Assert.NotNull(testType);
        xmlRootAttribute = testType.GetCustomAttributes<XmlRootAttribute>().FirstOrDefault();
        Assert.NotNull(xmlRootAttribute);
        Assert.Equal("EnumTestType", xmlRootAttribute.ElementName);
        Assert.Equal("Test_NS2", xmlRootAttribute.Namespace);
    }

    [Fact]
    public void AmbiguousAnonymousTypesTest()
    {
        const string xsd1 = @"<?xml version=""1.0"" encoding=""UTF-8""?>
            <xs:schema xmlns:xs=""http://www.w3.org/2001/XMLSchema"" targetNamespace=""Test_NS1""
                elementFormDefault=""qualified"" attributeFormDefault=""unqualified"">

	            <xs:complexType name=""TestType"">
		            <xs:sequence>
			            <xs:element name=""Property"">
				            <xs:simpleType>
					            <xs:restriction base=""xs:string"">
						            <xs:enumeration value=""EnumValue""/>
					            </xs:restriction>
				            </xs:simpleType>
			            </xs:element>
					</xs:sequence>
	            </xs:complexType>

	            <xs:complexType name=""TestType2"">
		            <xs:sequence>
			            <xs:element name=""Property"">
				            <xs:complexType>
					            <xs:sequence>
			                        <xs:element name=""Property"" type=""xs:string""/>
                                </xs:sequence>
				            </xs:complexType>
			            </xs:element>
					</xs:sequence>
	            </xs:complexType>

            </xs:schema>";
        const string xsd2 = @"<?xml version=""1.0"" encoding=""UTF-8""?>
            <xs:schema xmlns:xs=""http://www.w3.org/2001/XMLSchema"" targetNamespace=""Test_NS2""
                elementFormDefault=""qualified"" attributeFormDefault=""unqualified"">

	            <xs:complexType name=""TestType"">
		            <xs:sequence>
			            <xs:element name=""Property"">
				            <xs:simpleType>
					            <xs:restriction base=""xs:string"">
						            <xs:enumeration value=""EnumValue""/>
					            </xs:restriction>
				            </xs:simpleType>
			            </xs:element>
					</xs:sequence>
	            </xs:complexType>

	            <xs:complexType name=""TestType2"">
		            <xs:sequence>
			            <xs:element name=""Property"">
				            <xs:complexType>
					            <xs:sequence>
			                        <xs:element name=""Property"" type=""xs:string""/>
                                </xs:sequence>
				            </xs:complexType>
			            </xs:element>
					</xs:sequence>
	            </xs:complexType>

            </xs:schema>";
        var generator = new Generator
        {
            NamespaceProvider = new NamespaceProvider
            {
                GenerateNamespace = key =>key.XmlSchemaNamespace
            }
        };
        var contents = ConvertXml(nameof(GenerateXmlRootAttributeForEnumTest), [xsd1, xsd2], generator).ToArray();
        Assert.Equal(2, contents.Length);

        var assembly = Compiler.Compile(nameof(GenerateXmlRootAttributeForEnumTest), contents);

        var testType = assembly.GetType("Test_NS1.TestType");
        Assert.NotNull(testType);
        var xmlRootAttribute = testType.GetCustomAttributes<XmlRootAttribute>().FirstOrDefault();
        Assert.NotNull(xmlRootAttribute);
        Assert.Equal("TestType", xmlRootAttribute.ElementName);
        Assert.Equal("Test_NS1", xmlRootAttribute.Namespace);
        testType = assembly.GetType("Test_NS1.TestTypeProperty");
        Assert.NotNull(testType);
        xmlRootAttribute = testType.GetCustomAttributes<XmlRootAttribute>().FirstOrDefault();
        Assert.NotNull(xmlRootAttribute);
        Assert.Equal("TestTypeProperty", xmlRootAttribute.ElementName);
        Assert.Equal("Test_NS1", xmlRootAttribute.Namespace);
        testType = assembly.GetType("Test_NS1.TestType2Property");
        Assert.NotNull(testType);
        xmlRootAttribute = testType.GetCustomAttributes<XmlRootAttribute>().FirstOrDefault();
        Assert.NotNull(xmlRootAttribute);
        Assert.Equal("TestType2Property", xmlRootAttribute.ElementName);
        Assert.Equal("Test_NS1", xmlRootAttribute.Namespace);


        testType = assembly.GetType("Test_NS2.TestType");
        Assert.NotNull(testType);
        xmlRootAttribute = testType.GetCustomAttributes<XmlRootAttribute>().FirstOrDefault();
        Assert.NotNull(xmlRootAttribute);
        Assert.Equal("TestType", xmlRootAttribute.ElementName);
        Assert.Equal("Test_NS2", xmlRootAttribute.Namespace);
        testType = assembly.GetType("Test_NS2.TestTypeProperty");
        Assert.NotNull(testType);
        xmlRootAttribute = testType.GetCustomAttributes<XmlRootAttribute>().FirstOrDefault();
        Assert.NotNull(xmlRootAttribute);
        Assert.Equal("TestTypeProperty", xmlRootAttribute.ElementName);
        Assert.Equal("Test_NS2", xmlRootAttribute.Namespace);
        testType = assembly.GetType("Test_NS2.TestType2Property");
        Assert.NotNull(testType);
        xmlRootAttribute = testType.GetCustomAttributes<XmlRootAttribute>().FirstOrDefault();
        Assert.NotNull(xmlRootAttribute);
        Assert.Equal("TestType2Property", xmlRootAttribute.ElementName);
        Assert.Equal("Test_NS2", xmlRootAttribute.Namespace);
    }

    [Fact]
    public void TestShouldPatternForCollections()
    {
        const string xsd = @"<?xml version=""1.0"" encoding=""UTF-8""?>
            <xs:schema xmlns:xs=""http://www.w3.org/2001/XMLSchema"" targetNamespace=""Test_NS1""
            elementFormDefault=""qualified"" attributeFormDefault=""unqualified"">
            <xs:element name=""TestType"">
				<xs:complexType>
					<xs:choice maxOccurs=""unbounded"">
						<xs:element name=""DateValue"" type=""xs:dateTime"" nillable=""true""/>
					</xs:choice>
				</xs:complexType>
            </xs:element>
            <xs:element name=""TestType2"">
				<xs:complexType>
					<xs:choice maxOccurs=""unbounded"">
						<xs:element name=""StringValue"" type=""xs:string"" nillable=""true""/>
					</xs:choice>
				</xs:complexType>
            </xs:element>
            </xs:schema>";

        var generator = new Generator
        {
            NamespaceProvider = new NamespaceProvider
            {
                GenerateNamespace = key => key.XmlSchemaNamespace
            }
        };

        var contents = ConvertXml(nameof(TestShouldPatternForCollections), xsd, generator).ToArray();
        Assert.Single(contents);
        var assembly = Compiler.Compile(nameof(TestShouldPatternForCollections), contents);
        var testType = assembly.GetType("Test_NS1.TestType");
        var serializer = new XmlSerializer(testType);
        Assert.NotNull(serializer);
    }


    [Fact]
    public void TestDoNotForceIsNullableGeneration()
    {
        const string xsd = @"<?xml version=""1.0"" encoding=""UTF-8""?>
            <xs:schema xmlns:xs=""http://www.w3.org/2001/XMLSchema"" targetNamespace=""Test_NS1""
            elementFormDefault=""qualified"" attributeFormDefault=""unqualified"">
            <xs:element name=""TestType"">
				<xs:complexType>
					<xs:sequence>
						<xs:element name=""StringProperty"" type=""xs:string"" nillable=""true"" minOccurs=""0""/>
						<xs:element name=""StringNullableProperty"" type=""xs:string"" nillable=""true""/>
						<xs:element name=""StringNullableProperty2"" type=""xs:string"" nillable=""true"" minOccurs=""1""/>
					</xs:sequence>
				</xs:complexType>
            </xs:element>
            </xs:schema>";

        var generator = new Generator
        {
            NamespaceProvider = new NamespaceProvider
            {
                GenerateNamespace = key => key.XmlSchemaNamespace
            },
            DoNotForceIsNullable = true
        };

        var contents = ConvertXml(nameof(TestDoNotForceIsNullableGeneration), xsd, generator).ToArray();
        Assert.Single(contents);
        var assembly = Compiler.Compile(nameof(TestDoNotForceIsNullableGeneration), contents);
        var testType = assembly.GetType("Test_NS1.TestType");
        var serializer = new XmlSerializer(testType);
        Assert.NotNull(serializer);

        var prop = testType.GetProperty("StringProperty");
        Assert.NotNull(prop);
        var xmlElementAttribute = prop.GetCustomAttribute<XmlElementAttribute>();
        Assert.False(xmlElementAttribute.IsNullable);

        prop = testType.GetProperty("StringNullableProperty");
        Assert.NotNull(prop);
        var xmlElementNullableAttribute = prop.GetCustomAttribute<XmlElementAttribute>();
        Assert.True(xmlElementNullableAttribute.IsNullable);

        prop = testType.GetProperty("StringNullableProperty2");
        Assert.NotNull(prop);
        var xmlElementNullableAttribute2 = prop.GetCustomAttribute<XmlElementAttribute>();
        Assert.True(xmlElementNullableAttribute2.IsNullable);
    }

    [Fact]
    public void TestForceIsNullableGeneration()
    {
        const string xsd = @"<?xml version=""1.0"" encoding=""UTF-8""?>
            <xs:schema xmlns:xs=""http://www.w3.org/2001/XMLSchema"" targetNamespace=""Test_NS1""
            elementFormDefault=""qualified"" attributeFormDefault=""unqualified"">
            <xs:element name=""TestType"">
				<xs:complexType>
					<xs:sequence>
						<xs:element name=""StringProperty"" type=""xs:string"" nillable=""true"" minOccurs=""0""/>
						<xs:element name=""StringNullableProperty"" type=""xs:string"" nillable=""true""/>
						<xs:element name=""StringNullableProperty2"" type=""xs:string"" nillable=""true"" minOccurs=""1""/>
					</xs:sequence>
				</xs:complexType>
            </xs:element>
            </xs:schema>";

        var generator = new Generator
        {
            NamespaceProvider = new NamespaceProvider
            {
                GenerateNamespace = key => key.XmlSchemaNamespace
            },
            DoNotForceIsNullable = false
        };

        var contents = ConvertXml(nameof(TestForceIsNullableGeneration), xsd, generator).ToArray();
        Assert.Single(contents);
        var assembly = Compiler.Compile(nameof(TestForceIsNullableGeneration), contents);
        var testType = assembly.GetType("Test_NS1.TestType");
        var serializer = new XmlSerializer(testType);
        Assert.NotNull(serializer);

        var prop = testType.GetProperty("StringProperty");
        Assert.NotNull(prop);
        var xmlElementAttribute = prop.GetCustomAttribute<XmlElementAttribute>();
        Assert.True(xmlElementAttribute.IsNullable);

        prop = testType.GetProperty("StringNullableProperty");
        Assert.NotNull(prop);
        var xmlElementNullableAttribute = prop.GetCustomAttribute<XmlElementAttribute>();
        Assert.True(xmlElementNullableAttribute.IsNullable);

        prop = testType.GetProperty("StringNullableProperty2");
        Assert.NotNull(prop);
        var xmlElementNullableAttribute2 = prop.GetCustomAttribute<XmlElementAttribute>();
        Assert.True(xmlElementNullableAttribute2.IsNullable);
    }

    [Fact]
    public void TestArrayOfMsTypeGeneration()
    {
        // see https://github.com/mganss/XmlSchemaClassGenerator/issues/214

        var xsd0 =
            @"<xs:schema xmlns:xs=""http://www.w3.org/2001/XMLSchema"" xmlns:tns=""http://schemas.microsoft.com/2003/10/Serialization/Arrays"" targetNamespace=""http://schemas.microsoft.com/2003/10/Serialization/Arrays"" elementFormDefault=""qualified"">
            <xs:complexType name=""ArrayOfstring"">
                <xs:sequence>
	                <xs:element name=""string"" type=""xs:string"" nillable=""true"" minOccurs=""0"" maxOccurs=""unbounded""/>
                </xs:sequence>
            </xs:complexType>
            <xs:element name=""ArrayOfstring"" type=""tns:ArrayOfstring"" nillable=""true""/>
        </xs:schema>
        ";
        var xsd1 =
            @"<xs:schema xmlns:xs=""http://www.w3.org/2001/XMLSchema"" xmlns:q1=""http://schemas.microsoft.com/2003/10/Serialization/Arrays"" elementFormDefault=""qualified"">
            <xs:import namespace=""http://schemas.microsoft.com/2003/10/Serialization/Arrays""/>
            <xs:complexType name=""c_ai"">
                <xs:sequence>
	                <xs:element name=""d"" type=""q1:ArrayOfstring"" nillable=""true"" minOccurs=""0"">
		                <xs:annotation>
			                <xs:appinfo>
				                <DefaultValue EmitDefaultValue=""false"" xmlns=""http://schemas.microsoft.com/2003/10/Serialization/""/>
			                </xs:appinfo>
		                </xs:annotation>
	                </xs:element>
                </xs:sequence>
            </xs:complexType>
            <xs:element name=""c_ai"" type=""c_ai"" nillable=""true""/>
        </xs:schema>
        ";
        var validXml =
            @"<c_ai xmlns:tns=""http://schemas.microsoft.com/2003/10/Serialization/Arrays"">
            <d>
                <tns:string>String</tns:string>
                <tns:string>String</tns:string>
                <tns:string>String</tns:string>
            </d>
        </c_ai>
        ";
        var generator = new Generator
        {
            IntegerDataType = typeof(int),
            NamespacePrefix = "TestNS1",
            GenerateNullables = true,
            CollectionType = typeof(System.Collections.Generic.List<>)
        };
        var contents = ConvertXml(nameof(TestArrayOfMsTypeGeneration), [xsd0, xsd1], generator).ToArray();
        var assembly = Compiler.Compile(nameof(TestForceIsNullableGeneration), contents);
        var testType = assembly.GetType("TestNS1.CAi");
        var serializer = new XmlSerializer(testType);
        Assert.NotNull(serializer);
        dynamic deserialized = serializer.Deserialize(new StringReader(validXml));
        //Assert.NotEmpty((System.Collections.IEnumerable)deserialized.D);  //<== oops
    }

    [Fact]
    public void TestArrayOfStringsWhenPublicAndNull()
    {
        // see https://github.com/mganss/XmlSchemaClassGenerator/issues/282

        // arrange
        var xsd =
            @"<xs:schema xmlns:xs=""http://www.w3.org/2001/XMLSchema"" xmlns:tns=""http://schemas.microsoft.com/2003/10/Serialization/Arrays"">
                    <xs:complexType name=""ArrayOfstring"">
                        <xs:sequence>
                             <xs:element name=""testString"" type=""xs:string"" minOccurs=""0"" maxOccurs=""unbounded""/>
                        </xs:sequence>
                    </xs:complexType>
                </xs:schema>
                ";
        var validXml =
            @"<ArrayOfstring xmlns:tns=""http://schemas.microsoft.com/2003/10/Serialization/Arrays"">
                </ArrayOfstring>
                ";
        var generator = new Generator
        {
            IntegerDataType = typeof(int),
            NamespacePrefix = "Test_NS1",
            GenerateNullables = true,
            CollectionType = typeof(System.Array),
            CollectionSettersMode = CollectionSettersMode.Public
        };
        var contents = ConvertXml(nameof(TestArrayOfStringsWhenPublicAndNull), [xsd], generator).ToArray();
        var assembly = Compiler.Compile(nameof(TestForceIsNullableGeneration), contents);
        var testType = assembly.GetType("Test_NS1.ArrayOfstring");
        Assert.NotNull(testType);
        var serializer = new XmlSerializer(testType);

        // act
        dynamic deserialized = serializer.Deserialize(new StringReader(validXml));
        var xml = SharedTestFunctions.Serialize(serializer, deserialized);

        // assert
        Assert.NotNull(xml);
    }

    [Fact, TestPriority(1)]
    public void AirspaceServicesTest1()
    {
        var outputPath = Path.Combine("output", "aixm");

        string xlink = "http://www.w3.org/1999/xlink";
        string gml3 = "http://www.opengis.net/gml/3.2";
        string gts = "http://www.isotc211.org/2005/gts";
        string gss = "http://www.isotc211.org/2005/gss";
        string gsr = "http://www.isotc211.org/2005/gsr";
        string gmd = "http://www.isotc211.org/2005/gmd";
        string gco = "http://www.isotc211.org/2005/gco";

        string fixmBase = "http://www.fixm.aero/base/4.1";
        string fixmFlight = "http://www.fixm.aero/flight/4.1";
        string fixmNm = "http://www.fixm.aero/nm/1.2";
        string fixmMessaging = "http://www.fixm.aero/messaging/4.1";

        string adr = "http://www.aixm.aero/schema/5.1.1/extensions/EUR/ADR";
        string aixmV511 = "http://www.aixm.aero/schema/5.1.1";

        string adrmessage = "http://www.eurocontrol.int/cfmu/b2b/ADRMessage";

        var _xsdToCsharpNsMap = new Dictionary<NamespaceKey, string>
        {
            { new NamespaceKey(), "other" },
            { new NamespaceKey(xlink), "org.w3._1999.xlink" },
            { new NamespaceKey(gts), "org.isotc211._2005.gts" },
            { new NamespaceKey(gss), "org.isotc211._2005.gss" },
            { new NamespaceKey(gsr), "org.isotc211._2005.gsr" },
            { new NamespaceKey(gmd), "org.isotc211._2005.gmd" },
            { new NamespaceKey(gco), "org.isotc211._2005.gco" },
            { new NamespaceKey(gml3), "net.opengis.gml._3" },
            { new NamespaceKey(aixmV511), "aero.aixm.v5_1_1" },
            { new NamespaceKey(fixmNm), "aero.fixm.v4_1_0.nm.v1_2" },
            { new NamespaceKey(fixmMessaging), "aero.fixm.v4_1_0.messaging" },
            { new NamespaceKey(fixmFlight), "aero.fixm.v4_1_0.flight" },
            { new NamespaceKey(fixmBase), "aero.fixm.v4_1_0.base" },
            { new NamespaceKey(adr), "aero.aixm.schema._5_1_1.extensions.eur.adr" },
            { new NamespaceKey(adrmessage), "_int.eurocontrol.cfmu.b2b.adrmessage" }
        };

        var gen = new Generator
        {
            OutputFolder = outputPath,
            NamespaceProvider = _xsdToCsharpNsMap.ToNamespaceProvider(),
            CollectionSettersMode = CollectionSettersMode.Public,
            SeparateSubstitutes = true,
            GenerateInterfaces = false,
            EmitOrder = true
        };
        var xsdFiles = new[]
        {
            "AIXM_AbstractGML_ObjectTypes.xsd",
            "AIXM_DataTypes.xsd",
            "AIXM_Features.xsd",
            Path.Combine("extensions", "ADR-23.5.0", "ADR_DataTypes.xsd"),
            Path.Combine("extensions", "ADR-23.5.0", "ADR_Features.xsd"),
            Path.Combine("message", "ADR_Message.xsd"),
            Path.Combine("message", "AIXM_BasicMessage.xsd"),
        }.Select(x => Path.Combine(Directory.GetCurrentDirectory(), "xsd", "aixm", "aixm-5.1.1", x)).ToList();

        var assembly = Compiler.GenerateFiles("Aixm", xsdFiles, gen);
        Assert.NotNull(assembly);

        /*
        var testFiles = new Dictionary<string, string>
        {
            { "airport1.xml", "AirportHeliportType" },
            { "airportHeliportTimeSlice.xml", "AirportHeliportTimeSliceType" },
            { "airspace1.xml", "AirspaceType" },
            { "navaid1.xml", "NavaidType" },
            { "navaidTimeSlice.xml", "NavaidTimeSliceType" },
            { "navaidWithAbstractTime.xml", "NavaidWithAbstractTime" },
            { "navaid.xml", "Navaid" },
            { "routesegment1.xml", "RouteSegment" },
            { "timePeriod.xml", "TimePeriod" },
        };

        foreach (var testFile in testFiles)
        {
            var type = assembly.GetTypes().SingleOrDefault(t => t.Name == testFile.Value);
            Assert.NotNull(type);

            var serializer = new XmlSerializer(type);
            serializer.UnknownNode += new XmlNodeEventHandler(UnknownNodeHandler);
            serializer.UnknownAttribute += new XmlAttributeEventHandler(UnknownAttributeHandler);
            var unknownNodeError = false;
            var unknownAttrError = false;

            void UnknownNodeHandler(object sender, XmlNodeEventArgs e)
            {
                unknownNodeError = true;
            }

            void UnknownAttributeHandler(object sender, XmlAttributeEventArgs e)
            {
                unknownAttrError = true;
            }

            var xmlString = File.ReadAllText($"xml/aixm_tests/{testFile.Key}");
            var reader = XmlReader.Create(new StringReader(xmlString), new XmlReaderSettings { IgnoreWhitespace = true });

            var isDeserializable = serializer.CanDeserialize(reader);
            Assert.True(isDeserializable);

            var deserializedObject = serializer.Deserialize(reader);
            Assert.False(unknownNodeError);
            Assert.False(unknownAttrError);

            var serializedXml = Serialize(serializer, deserializedObject, GetNamespacesFromSource(xmlString));

            var deserializedXml = serializer.Deserialize(new StringReader(serializedXml));
            AssertEx.Equal(deserializedObject, deserializedXml);
        }
        */
    }

    [Fact, TestPriority(1)]
    [UseCulture("en-US")]
    public void TestNullableReferenceAttributes()
    {
        var files = Glob.ExpandNames(NullableReferenceAttributesPattern).OrderByDescending(f => f);
        var generator = new Generator
        {
            EnableNullableReferenceAttributes = true,
            UseShouldSerializePattern = true,
            NamespaceProvider = new NamespaceProvider
            {
                GenerateNamespace = key => "Test"
            }
        };
        var assembly = Compiler.Generate(nameof(TestNullableReferenceAttributes), NullableReferenceAttributesPattern, generator);
        void assertNullable(string typename, bool nullable)
        {
            Type c = assembly.GetType(typename);
            var property = c.GetProperty("Text");
            var setParameter = property.SetMethod.GetParameters();
            var getReturnParameter = property.GetMethod.ReturnParameter;
            var allowNullableAttribute = setParameter.Single().CustomAttributes.SingleOrDefault(a => a.AttributeType == typeof(AllowNullAttribute));
            var maybeNullAttribute = getReturnParameter.CustomAttributes.SingleOrDefault(a => a.AttributeType == typeof(MaybeNullAttribute));
            var hasAllowNullAttribute = allowNullableAttribute != null;
            var hasMaybeNullAttribute = maybeNullAttribute != null;
            Assert.Equal(nullable, hasAllowNullAttribute);
            Assert.Equal(nullable, hasMaybeNullAttribute);
        }
        assertNullable("Test.ElementReferenceNullable", true);
        assertNullable("Test.ElementReferenceList", false);
        assertNullable("Test.ElementReferenceNonNullable", false);
        assertNullable("Test.AttributeReferenceNullable", true);
        assertNullable("Test.AttributeReferenceNonNullable", false);
        assertNullable("Test.AttributeValueNullableInt", false);
    }

    [Fact, TestPriority(1)]
    public void TestNetex()
    {
        var outputPath = Path.Combine("output", "netex");

        var gen = new Generator
        {
            OutputFolder = outputPath,
            CollectionSettersMode = CollectionSettersMode.Public,
            EmitOrder = true,
            SeparateSubstitutes = true,
            GenerateInterfaces = false,
            UniqueTypeNamesAcrossNamespaces = true,
        };

        var xsdFiles = new[] { Path.Combine(Directory.GetCurrentDirectory(), "xsd", "netex", "NeTEx_publication.xsd") };

        var assembly = Compiler.GenerateFiles("Netex", xsdFiles, gen);
        Assert.NotNull(assembly);

        var testFiles = new Dictionary<string, string>
        {
            { "functions/calendar/Netex_calendarCodeing_02.xml", "PublicationDeliveryStructure" },
        };

        foreach (var testFile in testFiles)
        {
            var type = assembly.GetTypes().SingleOrDefault(t => t.Name == testFile.Value);
            Assert.NotNull(type);

            var serializer = new XmlSerializer(type);
            serializer.UnknownNode += new XmlNodeEventHandler(UnknownNodeHandler);
            serializer.UnknownAttribute += new XmlAttributeEventHandler(UnknownAttributeHandler);
            var unknownNodeError = false;
            var unknownAttrError = false;

            void UnknownNodeHandler(object sender, XmlNodeEventArgs e)
            {
                unknownNodeError = true;
            }

            void UnknownAttributeHandler(object sender, XmlAttributeEventArgs e)
            {
                unknownAttrError = true;
            }

            var xmlString = File.ReadAllText($"xml/netex_tests/{testFile.Key}");
            xmlString = Regex.Replace(xmlString, "xsi:schemaLocation=\"[^\"]*\"", string.Empty);
            var reader = XmlReader.Create(new StringReader(xmlString), new XmlReaderSettings { IgnoreWhitespace = true });

            var isDeserializable = serializer.CanDeserialize(reader);
            Assert.True(isDeserializable);

            var deserializedObject = serializer.Deserialize(reader);
            Assert.False(unknownNodeError);
            Assert.False(unknownAttrError);

            var serializedXml = SharedTestFunctions.Serialize(serializer, deserializedObject, SharedTestFunctions.GetNamespacesFromSource(xmlString));

            var deserializedXml = serializer.Deserialize(new StringReader(serializedXml));
            AssertEx.Equal(deserializedObject, deserializedXml);
        }
    }

    [Theory]
    [InlineData("fake command line arguments", "fake command line arguments")]
    [InlineData(null, "N/A")]
    public void IncludeCommandLineArguments(string commandLineArguments, string expectedCommandLineArguments)
    {
        const string xsd = @"<?xml version=""1.0"" encoding=""UTF-8""?>
<xs:schema elementFormDefault=""qualified"" xmlns:xs=""http://www.w3.org/2001/XMLSchema"" targetNamespace=""http://local.none"" xmlns:l=""http://local.none"">
	<xs:element name=""document"" type=""l:elem"" />
	<xs:complexType name=""elem"">
		<xs:attribute name=""Text"" type=""xs:string""/>
	</xs:complexType>
</xs:schema>";

        var generator = new Generator
        {
            GenerateInterfaces = false,
            NamespaceProvider = new NamespaceProvider
            {
                GenerateNamespace = key => "Test"
            },
            GenerateCommandLineArgumentsComment = true,
            CommandLineArgumentsProvider = new CommandLineArgumentsProvider(commandLineArguments)
        };

        var contents = ConvertXml(nameof(IncludeCommandLineArguments), xsd, generator);

        var csharp = Assert.Single(contents);

        CompareOutput(
            $@"//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

// This code was generated by Tests version 1.0.0.1 using the following command:
// {expectedCommandLineArguments}
namespace Test
{{
    
    
    [System.CodeDom.Compiler.GeneratedCodeAttribute(""Tests"", ""1.0.0.1"")]
    [System.SerializableAttribute()]
    [System.Xml.Serialization.XmlTypeAttribute(""elem"", Namespace=""http://local.none"")]
    [System.ComponentModel.DesignerCategoryAttribute(""code"")]
    [System.Xml.Serialization.XmlRootAttribute(""document"", Namespace=""http://local.none"")]
    public partial class Elem
    {{

        [System.Xml.Serialization.XmlAttributeAttribute(""Text"")]
        public string Text {{ get; set; }}
    }}
}}
", csharp);
    }

    [Fact]
    public void TestArrayItemAttribute()
    {
        // see https://github.com/mganss/XmlSchemaClassGenerator/issues/313

        var xsd =
@"<?xml version=""1.0"" encoding=""UTF-8""?>

<xs:schema xmlns:xs=""http://www.w3.org/2001/XMLSchema""
	 xmlns=""test_generation_namespace/common.xsd""
	 xmlns:ct=""test_generation_namespace/commontypes.xsd""
	 targetNamespace=""test_generation_namespace/common.xsd""
	 version=""1.1""
	 elementFormDefault=""qualified""
	 attributeFormDefault=""unqualified"">
	<xs:import namespace=""test_generation_namespace/commontypes.xsd"" schemaLocation=""TheCommonTypes.xsd""/>
	<xs:complexType name=""T_NameValue"">
		<xs:sequence>
			<xs:element name=""Name"" type=""xs:string""/>
			<xs:element name=""Value"" type=""xs:string"" minOccurs=""0""/>
		</xs:sequence>
	</xs:complexType>
	<xs:complexType name=""T_OptionList"">
		<xs:sequence>
			<xs:element name=""Option"" type=""T_NameValue"" minOccurs=""0"" maxOccurs=""unbounded""/>
		</xs:sequence>
	</xs:complexType>
    <xs:complexType name=""T_Application"">
		<xs:sequence>
			<xs:element name=""OptionList"" type=""T_OptionList"" minOccurs=""0""/>
		</xs:sequence>
	</xs:complexType>
</xs:schema>";
        var generator = new Generator
        {
            IntegerDataType = typeof(int),
            GenerateNullables = true,
            CollectionType = typeof(System.Array),
            CollectionSettersMode = CollectionSettersMode.Public,
            UseArrayItemAttribute = false
        };
        var contents = ConvertXml(nameof(TestArrayItemAttribute), [xsd], generator).ToArray();
        var assembly = Compiler.Compile(nameof(TestArrayItemAttribute), contents);
        var applicationType = assembly.GetType("TestGenerationNamespace.TApplication");
        Assert.NotNull(applicationType);
        var optionList = applicationType.GetProperty("OptionList");
        Assert.Equal("TestGenerationNamespace.TOptionList", optionList.PropertyType.FullName);
    }

    [Fact]
    public void CollectionSetterInAttributeGroupInterfaceIsPrivateIfCollectionSetterModeIsPrivate()
    {
        const string xsd = @"<?xml version=""1.0"" encoding=""UTF-8""?>
<xs:schema xmlns:xs=""http://www.w3.org/2001/XMLSchema"">
  <xs:element name=""Element"">
    <xs:complexType>
      <xs:attributeGroup ref=""AttrGroup""/>
    </xs:complexType>
  </xs:element>

  <xs:attributeGroup name=""AttrGroup"">
    <xs:attribute name=""Attr"">
      <xs:simpleType>
        <xs:list itemType=""xs:int""/>
      </xs:simpleType>
    </xs:attribute>
  </xs:attributeGroup>
</xs:schema>";

        var generator = new Generator
        {
            NamespaceProvider = new NamespaceProvider
            {
                GenerateNamespace = key => "Test",
            },
            GenerateInterfaces = true,
            CollectionSettersMode = CollectionSettersMode.Private
        };
        var contents = ConvertXml(nameof(CollectionSetterInAttributeGroupInterfaceIsPrivateIfCollectionSetterModeIsPrivate), xsd, generator).ToArray();
        var assembly = Compiler.Compile(nameof(CollectionSetterInAttributeGroupInterfaceIsPrivateIfCollectionSetterModeIsPrivate), contents);

        var interfaceProperty = assembly.GetType("Test.IAttrGroup")?.GetProperty("Attr");
        var implementerProperty = assembly.GetType("Test.Element")?.GetProperty("Attr");
        Assert.NotNull(interfaceProperty);
        Assert.NotNull(implementerProperty);

        var interfaceHasPublicSetter = interfaceProperty.GetSetMethod() != null;
        var implementerHasPublicSetter = implementerProperty.GetSetMethod() != null;
        Assert.False(interfaceHasPublicSetter);
        Assert.False(implementerHasPublicSetter);
    }

    [Fact]
    public void CollectionSetterInAttributeGroupInterfaceIsPublicIfCollectionSetterModeIsPublic()
    {
        const string xsd = @"<?xml version=""1.0"" encoding=""UTF-8""?>
<xs:schema xmlns:xs=""http://www.w3.org/2001/XMLSchema"">
  <xs:element name=""Element"">
    <xs:complexType>
      <xs:attributeGroup ref=""AttrGroup""/>
    </xs:complexType>
  </xs:element>

  <xs:attributeGroup name=""AttrGroup"">
    <xs:attribute name=""Attr"">
      <xs:simpleType>
        <xs:list itemType=""xs:int""/>
      </xs:simpleType>
    </xs:attribute>
  </xs:attributeGroup>
</xs:schema>";

        var generator = new Generator
        {
            NamespaceProvider = new NamespaceProvider
            {
                GenerateNamespace = key => "Test",
            },
            GenerateInterfaces = true,
            CollectionSettersMode = CollectionSettersMode.Public
        };
        var contents = ConvertXml(nameof(CollectionSetterInAttributeGroupInterfaceIsPublicIfCollectionSetterModeIsPublic), xsd, generator).ToArray();
        var assembly = Compiler.Compile(nameof(CollectionSetterInAttributeGroupInterfaceIsPublicIfCollectionSetterModeIsPublic), contents);

        var interfaceProperty = assembly.GetType("Test.IAttrGroup")?.GetProperty("Attr");
        var implementerProperty = assembly.GetType("Test.Element")?.GetProperty("Attr");
        Assert.NotNull(interfaceProperty);
        Assert.NotNull(implementerProperty);

        var interfaceHasPublicSetter = interfaceProperty.GetSetMethod() != null;
        var implementerHasPublicSetter = implementerProperty.GetSetMethod() != null;
        Assert.True(interfaceHasPublicSetter);
        Assert.True(implementerHasPublicSetter);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void SimpleInterface(bool generateInterface)
    {
        const string xsd = @"<?xml version=""1.0"" encoding=""UTF-8""?>
<xs:schema xmlns:xs=""http://www.w3.org/2001/XMLSchema"">
  <xs:attributeGroup name=""Common"">
    <xs:attribute name=""name"" type=""xs:string""></xs:attribute>
  </xs:attributeGroup>

  <xs:complexType name=""A"">
    <xs:attributeGroup ref=""Common""/>
  </xs:complexType>

  <xs:complexType name=""B"">
    <xs:attributeGroup ref=""Common""/>
  </xs:complexType>
</xs:schema>";

        var generator = new Generator
        {
            NamespaceProvider = new NamespaceProvider
            {
                GenerateNamespace = key => "Test",
            },
            GenerateInterfaces = generateInterface,
        };
        var contents = ConvertXml(nameof(SimpleInterface) + $"({generateInterface})", xsd, generator).ToArray();
        var assembly = Compiler.Compile(nameof(CollectionSetterInAttributeGroupInterfaceIsPublicIfCollectionSetterModeIsPublic), contents);

        var interfaceCommon = assembly.GetType("Test.ICommon");
        var typeA = assembly.GetType("Test.A");
        var typeB = assembly.GetType("Test.B");
        if(generateInterface)
        {
            Assert.True(interfaceCommon.IsInterface);
            Assert.True(interfaceCommon.IsAssignableFrom(typeA));
            Assert.True(interfaceCommon.IsAssignableFrom(typeB));
        }
        else
        {
            Assert.Null(interfaceCommon);
        }
    }


    [Fact]
    public void TestAllowDtdParse()
    {
        const string xsd = @"<?xml version=""1.0"" encoding=""utf-8""?>
<!DOCTYPE schema [
	<!ENTITY lowalpha ""a-z"">
	<!ENTITY hialpha ""A-Z"">
	<!ENTITY digit ""0-9"">
]>
<xs:schema xmlns:xs=""http://www.w3.org/2001/XMLSchema"">
	<xs:simpleType name=""CodecsType"">
		<xs:annotation>
			<xs:documentation xml:lang=""en"">
				List of Profiles
			</xs:documentation>
		</xs:annotation>
		<xs:restriction base=""xs:string"">
			<xs:pattern value=""[&lowalpha;&hialpha;&digit;]+""/>
		</xs:restriction>
	</xs:simpleType>
    <xs:complexType name=""ComplexType"">
      <xs:attribute name=""codecs"" type=""CodecsType""/>
    </xs:complexType>
</xs:schema>

";
        var generator = new Generator
        {
            NamespaceProvider = new NamespaceProvider
            {
                GenerateNamespace = key => "Test"
            },
            AllowDtdParse = true
        };

        var generatedType = ConvertXml(nameof(TestAllowDtdParse), xsd, generator).First();

        Assert.Contains(@"public partial class ComplexType", generatedType);
        Assert.Contains(@"[a-zA-Z0-9]+", generatedType);
    }

    [Fact]
    public void TestNotAllowDtdParse()
    {
        const string xsd = @"<?xml version=""1.0"" encoding=""utf-8""?>
<!DOCTYPE schema [
	<!ENTITY lowalpha ""a-z"">
	<!ENTITY hialpha ""A-Z"">
	<!ENTITY digit ""0-9"">
]>
<xs:schema xmlns:xs=""http://www.w3.org/2001/XMLSchema"">
	<xs:simpleType name=""CodecsType"">
		<xs:annotation>
			<xs:documentation xml:lang=""en"">
				List of Profiles
			</xs:documentation>
		</xs:annotation>
		<xs:restriction base=""xs:string"">
			<xs:pattern value=""[&lowalpha;&hialpha;&digit;]+""/>
		</xs:restriction>
	</xs:simpleType>
    <xs:complexType name=""ComplexType"">
      <xs:attribute name=""codecs"" type=""CodecsType""/>
    </xs:complexType>
</xs:schema>

";
        var generator = new Generator
        {
            NamespaceProvider = new NamespaceProvider
            {
                GenerateNamespace = key => "Test"
            },
            AllowDtdParse = false
        };

        var exception = Assert.Throws<XmlException>(() => ConvertXml(nameof(TestNotAllowDtdParse), xsd, generator));
        Assert.Contains("Reference to undeclared entity 'lowalpha'", exception.Message);
    }
}
