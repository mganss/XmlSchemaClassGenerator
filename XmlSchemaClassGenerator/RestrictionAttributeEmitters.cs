using System.CodeDom;
using System.Collections.Generic;
using XmlSchemaClassGenerator.Metadata;

namespace XmlSchemaClassGenerator;

internal interface IRestrictionAttributeEmitter
{
    bool TryEmit(RestrictionModel restriction, GeneratorConfiguration configuration, out CodeAttributeDeclaration attribute, out string requiredMetadataHelper);
}

internal sealed class FractionDigitsRestrictionAttributeEmitter : IRestrictionAttributeEmitter
{
    public bool TryEmit(RestrictionModel restriction, GeneratorConfiguration configuration, out CodeAttributeDeclaration attribute, out string requiredMetadataHelper)
    {
        if (restriction is not FractionDigitsRestrictionModel fractionDigits)
        {
            attribute = null;
            requiredMetadataHelper = null;
            return false;
        }

        attribute = new CodeAttributeDeclaration(
            CodeUtilities.CreateTypeReference(Attributes.FractionDigits(configuration.MetadataNamespace), configuration),
            new CodeAttributeArgument(new CodePrimitiveExpression(fractionDigits.Value)));
        requiredMetadataHelper = MetadataHelperNames.FractionDigits;
        return true;
    }
}

internal sealed class RestrictionAttributeEmitterRegistry
{
    private readonly IReadOnlyList<IRestrictionAttributeEmitter> _emitters;

    private RestrictionAttributeEmitterRegistry(IReadOnlyList<IRestrictionAttributeEmitter> emitters)
    {
        _emitters = emitters;
    }

    public static RestrictionAttributeEmitterRegistry Default { get; } = new RestrictionAttributeEmitterRegistry(
    [
        new FractionDigitsRestrictionAttributeEmitter(),
    ]);

    public bool TryEmit(RestrictionModel restriction, GeneratorConfiguration configuration, out CodeAttributeDeclaration attribute, out string requiredMetadataHelper)
    {
        if (configuration.MetadataEmissionMode == MetadataEmissionMode.None)
        {
            attribute = null;
            requiredMetadataHelper = null;
            return false;
        }

        foreach (var emitter in _emitters)
        {
            if (emitter.TryEmit(restriction, configuration, out attribute, out requiredMetadataHelper))
                return true;
        }

        attribute = null;
        requiredMetadataHelper = null;
        return false;
    }
}
