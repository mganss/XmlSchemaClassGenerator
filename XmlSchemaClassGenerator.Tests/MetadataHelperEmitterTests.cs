using System;
using System.CodeDom;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Xunit;

namespace XmlSchemaClassGenerator.Tests;

public class MetadataHelperEmitterTests
{
    [Fact]
    public void EnsureFractionDigitsAttributeEmitted_AddsType_WhenNamespaceAlreadyExistsAndTypeIsMissing()
    {
        var configuration = new GeneratorConfiguration
        {
            EmitMetadataAttributes = true,
            MetadataNamespace = "Shared.Metadata",
        };

        var metadataNamespace = new CodeNamespace("Shared.Metadata");
        var codeNamespaces = new List<CodeNamespace> { metadataNamespace };

        EnsureFractionDigitsAttributeEmitted(configuration, codeNamespaces);

        Assert.Single(metadataNamespace.Types.OfType<CodeTypeDeclaration>(), t => t.Name == "FractionDigitsAttribute");
    }

    [Fact]
    public void EnsureFractionDigitsAttributeEmitted_DoesNotDuplicateType_WhenTypeAlreadyExists()
    {
        var configuration = new GeneratorConfiguration
        {
            EmitMetadataAttributes = true,
            MetadataNamespace = "Shared.Metadata",
        };

        var metadataNamespace = new CodeNamespace("Shared.Metadata");
        metadataNamespace.Types.Add(new CodeTypeDeclaration("FractionDigitsAttribute"));
        var codeNamespaces = new List<CodeNamespace> { metadataNamespace };

        EnsureFractionDigitsAttributeEmitted(configuration, codeNamespaces);

        Assert.Single(metadataNamespace.Types.OfType<CodeTypeDeclaration>(), t => t.Name == "FractionDigitsAttribute");
    }

    [Fact]
    public void EnsureFractionDigitsAttributeEmitted_AddsConfiguredImports_WhenCreatingMetadataNamespace()
    {
        var configuration = new GeneratorConfiguration
        {
            EmitMetadataAttributes = true,
            MetadataNamespace = "Shared.Metadata",
            CompactTypeNames = true,
            DataAnnotationMode = DataAnnotationMode.All,
        };

        var codeNamespaces = new List<CodeNamespace>();

        EnsureFractionDigitsAttributeEmitted(configuration, codeNamespaces);

        var metadataNamespace = Assert.Single(codeNamespaces);
        var imports = metadataNamespace.Imports.Cast<CodeNamespaceImport>().Select(i => i.Namespace).ToList();

        Assert.Contains("System", imports);
        Assert.Contains("System.ComponentModel.DataAnnotations", imports);
    }

    private static void EnsureFractionDigitsAttributeEmitted(GeneratorConfiguration configuration, ICollection<CodeNamespace> codeNamespaces)
    {
        var emitterType = typeof(Generator).Assembly.GetType("XmlSchemaClassGenerator.Metadata.MetadataHelperEmitter", throwOnError: true);
        var emitter = Activator.CreateInstance(emitterType, configuration);
        Assert.NotNull(emitter);
        
        var method = emitterType.GetMethod("EnsureFractionDigitsAttributeEmitted", BindingFlags.Instance | BindingFlags.Public);
        Assert.NotNull(method);

        method.Invoke(emitter, [codeNamespaces]);
    }
}
