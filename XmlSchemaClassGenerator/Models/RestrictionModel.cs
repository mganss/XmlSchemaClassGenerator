using System;
using System.CodeDom;

namespace XmlSchemaClassGenerator;

public abstract class RestrictionModel(GeneratorConfiguration configuration) : GeneratorModel(configuration)
{
    public bool IsSupported => MinimumDataAnnotationMode >= Configuration.DataAnnotationMode;

    /// <summary>
    /// The DataAnnotationMode required to be able to emit this restriction
    /// </summary>
    public abstract DataAnnotationMode MinimumDataAnnotationMode { get; }
    public abstract string Description { get; }
    public abstract CodeAttributeDeclaration GetAttribute();
}

public abstract class ValueRestrictionModel<T>(GeneratorConfiguration configuration) : RestrictionModel(configuration)
{
    public T Value { get; set; }

    public override CodeAttributeDeclaration GetAttribute() => null;
}

public abstract class ValueTypeRestrictionModel(GeneratorConfiguration configuration) : ValueRestrictionModel<string>(configuration)
{
    public Type Type { get; set; }
}

public class MinMaxLengthRestrictionModel(GeneratorConfiguration configuration) : RestrictionModel(configuration)
{
    public int Min { get; set; }
    public int Max { get; set; }

    public override DataAnnotationMode MinimumDataAnnotationMode => DataAnnotationMode.Partial;

    public override string Description => ((Min > 0 ? $"Minimum length: {Min}. " : string.Empty) + (Max > 0 ? $"Maximum length: {Max}." : string.Empty)).Trim();

    public override CodeAttributeDeclaration GetAttribute()
    {
        var a = AttributeDecl(Attributes.StringLength, new(Max > 0 ? new CodePrimitiveExpression(Max) : new CodeSnippetExpression("int.MaxValue")));
        if (Min > 0) { a.Arguments.Add(new("MinimumLength", new CodePrimitiveExpression(Min))); }
        return a;
    }
}

public class MaxLengthRestrictionModel(GeneratorConfiguration configuration) : ValueRestrictionModel<int>(configuration)
{
    public override DataAnnotationMode MinimumDataAnnotationMode => DataAnnotationMode.All;

    public override string Description => string.Format("Maximum length: {0}.", Value);

    public override CodeAttributeDeclaration GetAttribute()
        => AttributeDecl(Attributes.MaxLength, new(new CodePrimitiveExpression(Value)));
}

public class MinLengthRestrictionModel(GeneratorConfiguration configuration) : ValueRestrictionModel<int>(configuration)
{
    public override DataAnnotationMode MinimumDataAnnotationMode => DataAnnotationMode.All;

    public override string Description => string.Format("Minimum length: {0}.", Value);

    public override CodeAttributeDeclaration GetAttribute()
        => AttributeDecl(Attributes.MinLength, new(new CodePrimitiveExpression(Value)));
}

public class TotalDigitsRestrictionModel(GeneratorConfiguration configuration) : ValueRestrictionModel<int>(configuration)
{
    public override DataAnnotationMode MinimumDataAnnotationMode => DataAnnotationMode.All;

    public override string Description => string.Format("Total number of digits: {0}.", Value);
}

public class FractionDigitsRestrictionModel(GeneratorConfiguration configuration) : ValueRestrictionModel<int>(configuration)
{
    public override DataAnnotationMode MinimumDataAnnotationMode => DataAnnotationMode.All;

    public override string Description => $"Total number of digits in fraction: {Value}.";

    public override CodeAttributeDeclaration GetAttribute()
    {
        if (!Configuration.EmitMetadataAttributes)
            return null;

        return AttributeDecl(
            Attributes.FractionDigits(Configuration.MetadataNamespace),
            new(new CodePrimitiveExpression(Value)));
    }
}

public class PatternRestrictionModel(GeneratorConfiguration configuration) : ValueRestrictionModel<string>(configuration)
{
    public override DataAnnotationMode MinimumDataAnnotationMode => DataAnnotationMode.Partial;

    public override string Description => $"Pattern: {Value}.";

    public override CodeAttributeDeclaration GetAttribute()
        => AttributeDecl(Attributes.RegularExpression, new(new CodePrimitiveExpression(Value)));
}

public class MinInclusiveRestrictionModel(GeneratorConfiguration configuration) : ValueTypeRestrictionModel(configuration)
{
    public override DataAnnotationMode MinimumDataAnnotationMode => DataAnnotationMode.All;

    public override string Description => $"Minimum inclusive value: {Value}.";
}

public class MinExclusiveRestrictionModel(GeneratorConfiguration configuration) : ValueTypeRestrictionModel(configuration)
{
    public override DataAnnotationMode MinimumDataAnnotationMode => DataAnnotationMode.All;

    public override string Description => $"Minimum exclusive value: {Value}.";
}

public class MaxInclusiveRestrictionModel(GeneratorConfiguration configuration) : ValueTypeRestrictionModel(configuration)
{
    public override DataAnnotationMode MinimumDataAnnotationMode => DataAnnotationMode.All;

    public override string Description => $"Maximum inclusive value: {Value}.";
}

public class MaxExclusiveRestrictionModel(GeneratorConfiguration configuration) : ValueTypeRestrictionModel(configuration)
{
    public override DataAnnotationMode MinimumDataAnnotationMode => DataAnnotationMode.All;

    public override string Description => $"Maximum exclusive value: {Value}.";
}
