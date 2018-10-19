using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Schema;

namespace XmlSchemaClassGenerator
{
    public static class CodeUtilities
    {
        // Match non-letter followed by letter
        static Regex PascalCaseRegex = new Regex(@"[^\p{L}]\p{L}", RegexOptions.Compiled);

        private static readonly XmlTypeCode[] IntegerDerivedTypeCodes =
        {
            XmlTypeCode.Integer, XmlTypeCode.NegativeInteger, XmlTypeCode.NonNegativeInteger,
            XmlTypeCode.NonPositiveInteger, XmlTypeCode.PositiveInteger
        };

        // Uppercases first letter and all letters following non-letters.
        // Examples: testcase -> Testcase, html5element -> Html5Element, test_case -> Test_Case
        public static string ToPascalCase(this string s)
        {
            if (string.IsNullOrEmpty(s)) { return s; }
            return char.ToUpperInvariant(s[0])
                + PascalCaseRegex.Replace(s.Substring(1), m => m.Value[0] + char.ToUpperInvariant(m.Value[1]).ToString());
        }

        public static string ToCamelCase(this string s)
        {
            if (string.IsNullOrEmpty(s)) { return s; }
            return char.ToLowerInvariant(s[0]) + s.Substring(1);
        }

        public static string ToBackingField(this string propertyName, bool doNotUseUnderscoreInPrivateMemberNames)
        {
            return doNotUseUnderscoreInPrivateMemberNames ? propertyName.ToCamelCase() : string.Concat("_", propertyName.ToCamelCase());
        }

        private static bool? IsDataTypeAttributeAllowed(XmlTypeCode typeCode, GeneratorConfiguration configuration)
        {
            bool? result;
            switch (typeCode)
            {
                case XmlTypeCode.AnyAtomicType:
                    // union
                    result = false;
                    break;
                case XmlTypeCode.Integer:
                case XmlTypeCode.NegativeInteger:
                case XmlTypeCode.NonNegativeInteger:
                case XmlTypeCode.NonPositiveInteger:
                case XmlTypeCode.PositiveInteger:
                    if (configuration.IntegerDataType != null && configuration.IntegerDataType != typeof(string))
                    {
                        result = false;
                    }
                    else
                    {
                        result = null;
                    }
                    break;
                case XmlTypeCode.Base64Binary:
                case XmlTypeCode.HexBinary:
                    result = true;
                    break;
                default:
                    result = null;
                    break;
            }
            return result;
        }

        public static bool? IsDataTypeAttributeAllowed(this XmlSchemaDatatype type, GeneratorConfiguration configuration)
        {
            return IsDataTypeAttributeAllowed(type.TypeCode, configuration);
        }

        public static bool? IsDataTypeAttributeAllowed(this XmlSchemaType type, GeneratorConfiguration configuration)
        {
            return IsDataTypeAttributeAllowed(type.TypeCode, configuration);
        }

        private static Type GetIntegerDerivedType(XmlSchemaDatatype type, GeneratorConfiguration configuration,
            IEnumerable<RestrictionModel> restrictions)
        {
            if (configuration.IntegerDataType != null) return configuration.IntegerDataType;

            var xmlTypeCode = type.TypeCode;

            Type result = null;

            if (!(restrictions.SingleOrDefault(r => r is TotalDigitsRestrictionModel) is TotalDigitsRestrictionModel totalDigits)
                || ((xmlTypeCode == XmlTypeCode.PositiveInteger
                     || xmlTypeCode == XmlTypeCode.NonNegativeInteger) && totalDigits.Value >= 30)
                || ((xmlTypeCode == XmlTypeCode.Integer
                     || xmlTypeCode == XmlTypeCode.NegativeInteger
                     || xmlTypeCode == XmlTypeCode.NonPositiveInteger) && totalDigits.Value >= 29))
            {
                return typeof(string);
            }

            switch (xmlTypeCode)
            {
                case XmlTypeCode.PositiveInteger:
                case XmlTypeCode.NonNegativeInteger:
                    switch (totalDigits.Value)
                    {
                        case int n when (n < 3):
                            result = typeof(byte);
                            break;
                        case int n when (n < 5):
                            result = typeof(ushort);
                            break;
                        case int n when (n < 10):
                            result = typeof(uint);
                            break;
                        case int n when (n < 20):
                            result = typeof(ulong);
                            break;
                        case int n when (n < 30):
                            result = typeof(decimal);
                            break;
                    }

                    break;

                case XmlTypeCode.Integer:
                case XmlTypeCode.NegativeInteger:
                case XmlTypeCode.NonPositiveInteger:
                    switch (totalDigits.Value)
                    {
                        case int n when (n < 3):
                            result = typeof(sbyte);
                            break;
                        case int n when (n < 5):
                            result = typeof(short);
                            break;
                        case int n when (n < 10):
                            result = typeof(int);
                            break;
                        case int n when (n < 19):
                            result = typeof(long);
                            break;
                        case int n when (n < 29):
                            result = typeof(decimal);
                            break;
                    }
                    break;
            }

            return result;
        }

        private static Type GetEffectiveType(XmlTypeCode typeCode, XmlSchemaDatatypeVariety variety)
        {
            Type result;
            switch (typeCode)
            {
                case XmlTypeCode.AnyAtomicType:
                    // union
                    result = typeof(string);
                    break;
                case XmlTypeCode.AnyUri:
                case XmlTypeCode.Duration:
                case XmlTypeCode.GDay:
                case XmlTypeCode.GMonth:
                case XmlTypeCode.GMonthDay:
                case XmlTypeCode.GYear:
                case XmlTypeCode.GYearMonth:
                    result = variety == XmlSchemaDatatypeVariety.List ? typeof(string[]) : typeof(string);
                    break;
                case XmlTypeCode.Time:
                    result = typeof(DateTime);
                    break;
                case XmlTypeCode.Idref:
                    result = typeof(string);
                    break;
                default:
                    result = null;
                    break;
            }
            return result;
        }

        public static Type GetEffectiveType(this XmlSchemaDatatype type, GeneratorConfiguration configuration,
            IEnumerable<RestrictionModel> restrictions)
        {
            var xmlTypeCode = type.TypeCode;

            return IntegerDerivedTypeCodes.Contains(xmlTypeCode)
                ? GetIntegerDerivedType(type, configuration, restrictions) ?? type.ValueType
                : GetEffectiveType(xmlTypeCode, type.Variety) ?? type.ValueType;
        }

        public static Type GetEffectiveType(this XmlSchemaType type, GeneratorConfiguration configuration,
            IEnumerable<RestrictionModel> restrictions)
        {
            return GetEffectiveType(type.Datatype, configuration, restrictions);
        }

        public static XmlQualifiedName GetQualifiedName(this XmlSchemaType schemaType)
        {
            return schemaType.QualifiedName.IsEmpty
                ? schemaType.BaseXmlSchemaType.QualifiedName
                : schemaType.QualifiedName;
        }

        public static XmlQualifiedName GetQualifiedName(this TypeModel typeModel)
        {
            XmlQualifiedName qualifiedName;
            if (!(typeModel is SimpleModel simpleTypeModel))
            {
                qualifiedName = typeModel.XmlSchemaType.GetQualifiedName();
            }
            else
            {
                qualifiedName = simpleTypeModel.XmlSchemaType.GetQualifiedName();
                var xmlSchemaType = simpleTypeModel.XmlSchemaType;
                while (qualifiedName.Namespace != XmlSchema.Namespace &&
                       xmlSchemaType.BaseXmlSchemaType != null)
                {
                    xmlSchemaType = xmlSchemaType.BaseXmlSchemaType;
                    qualifiedName = xmlSchemaType.GetQualifiedName();
                }
            }
            return qualifiedName;
        }

        public static string GetUniqueTypeName(this NamespaceModel model, string name)
        {
            var n = name;
            var i = 2;

            while (model.Types.ContainsKey(n) && !(model.Types[n] is SimpleModel))
            {
                n = name + i;
                i++;
            }

            return n;
        }

        public static string GetUniqueFieldName(this TypeModel typeModel, PropertyModel propertyModel)
        {
            var classModel = typeModel as ClassModel;
            var propBackingFieldName = propertyModel.Name.ToBackingField(classModel?.Configuration.DoNotUseUnderscoreInPrivateMemberNames == true);

            if (CSharpKeywords.Contains(propBackingFieldName.ToLower()))
                propBackingFieldName = "@" + propBackingFieldName;

            if (classModel == null)
            {
                return propBackingFieldName;
            }

            var i = 0;
            foreach (var prop in classModel.Properties)
            {
                if (!classModel.Configuration.EnableDataBinding && !(prop.Type is SimpleModel))
                {
                    continue;
                }

                if (propertyModel == prop)
                {
                    i += 1;
                    break;
                }

                var backingFieldName = prop.Name.ToBackingField(classModel.Configuration.DoNotUseUnderscoreInPrivateMemberNames);
                if (backingFieldName == propBackingFieldName)
                {
                    i += 1;
                }
            }

            if (i <= 1)
            {
                return propBackingFieldName;
            }

            return string.Format("{0}{1}", propBackingFieldName, i);
        }

        static readonly Regex NormalizeNewlinesRegex = new Regex(@"(^|[^\r])\n", RegexOptions.Compiled);

        internal static string NormalizeNewlines(string text)
        {
            return NormalizeNewlinesRegex.Replace(text, "$1\r\n");
        }

        static readonly List<string> CSharpKeywords = new List<string>
        {
            "abstract", "as", "base", "bool",
            "break", "byte", "case", "catch",
            "char", "checked", "class", "const",
            "continue", "decimal", "default", "delegate",
            "do", "double", "else", "enum",
            "event", " explicit", "extern", "false",
            "finally", "fixed", "float", "for",
            "foreach", "goto", "if", "implicit",
            "in", "int", "interface", "internal",
            "is", "lock", "long", "namespace",
            "new", "null", "object", "operator",
            "out", "override", "params", "private",
            "protected", "public", "readonly", "ref",
            "return", "sbyte", "sealed", "short",
            "sizeof", "stackalloc", "static", "string",
            "struct", "switch", "this", "throw",
            "true", "try", "typeof", "uint",
            "ulong", "unchecked", "unsafe", "ushort",
            "using", "using static", "virtual", "void",
            "volatile", "while"
        };
    }
}