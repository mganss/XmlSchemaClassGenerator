using System;
using System.CodeDom;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XmlSchemaClassGenerator
{
    public abstract class RestrictionModel
    {
        public abstract string Description { get; }
        public abstract CodeAttributeDeclaration GetAttribute();
    }

    public abstract class ValueRestrictionModel<T> : RestrictionModel
    {
        public T Value { get; set; }

        public override CodeAttributeDeclaration GetAttribute()
        {
            return null;
        }
    }

    public abstract class ValueTypeRestrictionModel: ValueRestrictionModel<string>
    {
        public Type Type { get; set; }
    }

    public class MaxLengthRestrictionModel : ValueRestrictionModel<int>
    {
        public override string Description
        {
            get
            {
                return string.Format("Maximum length: {0}.", Value);
            }
        }

        public override CodeAttributeDeclaration GetAttribute()
        {
            return new CodeAttributeDeclaration(new CodeTypeReference(typeof(MaxLengthAttribute)), new CodeAttributeArgument(new CodePrimitiveExpression(Value)));
        }
    }

    public class MinLengthRestrictionModel : ValueRestrictionModel<int>
    {
        public override string Description
        {
            get
            {
                return string.Format("Minimum length: {0}.", Value);
            }
        }

        public override CodeAttributeDeclaration GetAttribute()
        {
            return new CodeAttributeDeclaration(new CodeTypeReference(typeof(MinLengthAttribute)), new CodeAttributeArgument(new CodePrimitiveExpression(Value)));
        }
    }

    public class TotalDigitsRestrictionModel : ValueRestrictionModel<int>
    {
        public override string Description
        {
            get
            {
                return string.Format("Total number of digits: {0}.", Value);
            }
        }
    }

    public class FractionDigitsRestrictionModel : ValueRestrictionModel<int>
    {
        public override string Description
        {
            get
            {
                return string.Format("Total number of digits in fraction: {0}.", Value);
            }
        }
    }

    public class PatternRestrictionModel : ValueRestrictionModel<string>
    {
        public override string Description
        {
            get
            {
                return string.Format("Pattern: {0}.", Value);
            }
        }

        public override CodeAttributeDeclaration GetAttribute()
        {
            return new CodeAttributeDeclaration(new CodeTypeReference(typeof(RegularExpressionAttribute)), new CodeAttributeArgument(new CodePrimitiveExpression(Value)));
        }
    }

    public class MinInclusiveRestrictionModel: ValueTypeRestrictionModel
    {
        public override string Description
        {
            get
            {
                return string.Format("Minimum inclusive value: {0}.", Value);
            }
        }
    }

    public class MinExclusiveRestrictionModel: ValueTypeRestrictionModel
    {
        public override string Description
        {
            get
            {
                return string.Format("Minimum exclusive value: {0}.", Value);
            }
        }
    }

    public class MaxInclusiveRestrictionModel: ValueTypeRestrictionModel
    {
        public override string Description
        {
            get
            {
                return string.Format("Maximum inclusive value: {0}.", Value);
            }
        }
    }

    public class MaxExclusiveRestrictionModel: ValueTypeRestrictionModel
    {
        public override string Description
        {
            get
            {
                return string.Format("Maximum exclusive value: {0}.", Value);
            }
        }
    }
}
