using System;
using System.CodeDom;

namespace XmlSchemaClassGenerator
{
    public abstract class RestrictionModel : GeneratorModel
    {
        protected RestrictionModel(GeneratorConfiguration configuration) : base(configuration) { }

        public bool IsSupported => MinimumDataAnnotationMode >= Configuration.DataAnnotationMode;

        /// <summary>
        /// The DataAnnotationMode required to be able to emit this restriction
        /// </summary>
        public abstract DataAnnotationMode MinimumDataAnnotationMode { get; }
        public abstract string Description { get; }
        public abstract CodeAttributeDeclaration GetAttribute();
    }

    public abstract class ValueRestrictionModel<T> : RestrictionModel
    {
        protected ValueRestrictionModel(GeneratorConfiguration configuration) : base(configuration) { }

        public T Value { get; set; }

        public override CodeAttributeDeclaration GetAttribute() => null;
    }

    public abstract class ValueTypeRestrictionModel : ValueRestrictionModel<string>
    {
        protected ValueTypeRestrictionModel(GeneratorConfiguration configuration) : base(configuration) { }

        public Type Type { get; set; }
    }

    public class MinMaxLengthRestrictionModel : RestrictionModel
    {
        public MinMaxLengthRestrictionModel(GeneratorConfiguration configuration) : base(configuration) { }

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

    public class MaxLengthRestrictionModel : ValueRestrictionModel<int>
    {
        public MaxLengthRestrictionModel(GeneratorConfiguration configuration) : base(configuration) { }

        public override DataAnnotationMode MinimumDataAnnotationMode => DataAnnotationMode.All;

        public override string Description => string.Format("Maximum length: {0}.", Value);

        public override CodeAttributeDeclaration GetAttribute()
            => AttributeDecl(Attributes.MaxLength, new(new CodePrimitiveExpression(Value)));
    }

    public class MinLengthRestrictionModel : ValueRestrictionModel<int>
    {
        public MinLengthRestrictionModel(GeneratorConfiguration configuration) : base(configuration) { }

        public override DataAnnotationMode MinimumDataAnnotationMode => DataAnnotationMode.All;

        public override string Description => string.Format("Minimum length: {0}.", Value);

        public override CodeAttributeDeclaration GetAttribute()
            => AttributeDecl(Attributes.MinLength, new(new CodePrimitiveExpression(Value)));
    }

    public class TotalDigitsRestrictionModel : ValueRestrictionModel<int>
    {
        public TotalDigitsRestrictionModel(GeneratorConfiguration configuration) : base(configuration) { }

        public override DataAnnotationMode MinimumDataAnnotationMode => DataAnnotationMode.All;

        public override string Description => string.Format("Total number of digits: {0}.", Value);
    }

    public class FractionDigitsRestrictionModel : ValueRestrictionModel<int>
    {
        public FractionDigitsRestrictionModel(GeneratorConfiguration configuration) : base(configuration) { }

        public override DataAnnotationMode MinimumDataAnnotationMode => DataAnnotationMode.All;

        public override string Description => $"Total number of digits in fraction: {Value}.";
    }

    public class PatternRestrictionModel : ValueRestrictionModel<string>
    {
        public PatternRestrictionModel(GeneratorConfiguration configuration) : base(configuration) { }

        public override DataAnnotationMode MinimumDataAnnotationMode => DataAnnotationMode.Partial;

        public override string Description => $"Pattern: {Value}.";

        public override CodeAttributeDeclaration GetAttribute()
            => AttributeDecl(Attributes.RegularExpression, new(new CodePrimitiveExpression(Value)));
    }

    public class MinInclusiveRestrictionModel : ValueTypeRestrictionModel
    {
        public MinInclusiveRestrictionModel(GeneratorConfiguration configuration) : base(configuration) { }

        public override DataAnnotationMode MinimumDataAnnotationMode => DataAnnotationMode.All;

        public override string Description => $"Minimum inclusive value: {Value}.";
    }

    public class MinExclusiveRestrictionModel : ValueTypeRestrictionModel
    {
        public MinExclusiveRestrictionModel(GeneratorConfiguration configuration) : base(configuration) { }

        public override DataAnnotationMode MinimumDataAnnotationMode => DataAnnotationMode.All;

        public override string Description => $"Minimum exclusive value: {Value}.";
    }

    public class MaxInclusiveRestrictionModel : ValueTypeRestrictionModel
    {
        public MaxInclusiveRestrictionModel(GeneratorConfiguration configuration) : base(configuration) { }

        public override DataAnnotationMode MinimumDataAnnotationMode => DataAnnotationMode.All;

        public override string Description => $"Maximum inclusive value: {Value}.";
    }

    public class MaxExclusiveRestrictionModel : ValueTypeRestrictionModel
    {
        public MaxExclusiveRestrictionModel(GeneratorConfiguration configuration) : base(configuration) { }

        public override DataAnnotationMode MinimumDataAnnotationMode => DataAnnotationMode.All;

        public override string Description => $"Maximum exclusive value: {Value}.";
    }
}
