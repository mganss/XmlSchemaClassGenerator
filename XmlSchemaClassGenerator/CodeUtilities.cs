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
        static readonly Regex PascalCaseRegex = new Regex(@"[^\p{L}]\p{L}", RegexOptions.Compiled);

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

        public static string ToBackingField(this string propertyName, string privateFieldPrefix)
        {
            return string.Concat(privateFieldPrefix, propertyName.ToCamelCase());
        }

        public static bool? IsDataTypeAttributeAllowed(this XmlSchemaDatatype type)
        {
            bool? result;
            switch (type.TypeCode)
            {
                case XmlTypeCode.AnyAtomicType:
                    // union
                    result = false;
                    break;
                case XmlTypeCode.DateTime:
                case XmlTypeCode.Time:
                case XmlTypeCode.Date:
                case XmlTypeCode.Base64Binary:
                case XmlTypeCode.HexBinary:
                    result = true;
                    break;
                default:
                    result = false;
                    break;
            }
            return result;
        }

        private static Type GetIntegerDerivedType(XmlSchemaDatatype type, GeneratorConfiguration configuration, IEnumerable<RestrictionModel> restrictions)
        {
            if (configuration.IntegerDataType != null && !configuration.UseIntegerDataTypeAsFallback) return configuration.IntegerDataType;

            var xmlTypeCode = type.TypeCode;

            Type result = null;

            var maxInclusive = restrictions.OfType<MaxInclusiveRestrictionModel>().SingleOrDefault();
            var minInclusive = restrictions.OfType<MinInclusiveRestrictionModel>().SingleOrDefault();

            decimal? maxInclusiveValue = null;
            if (maxInclusive is null && xmlTypeCode == XmlTypeCode.NegativeInteger)
            {
                maxInclusiveValue = -1;
            }
            else if (maxInclusive is null && xmlTypeCode == XmlTypeCode.NonPositiveInteger)
            {
                maxInclusiveValue = 0;
            }
            else if (maxInclusive != null && decimal.TryParse(maxInclusive.Value, out decimal value))
            {
                maxInclusiveValue = value;
            }

            decimal? minInclusiveValue = null;
            if (minInclusive is null && xmlTypeCode == XmlTypeCode.PositiveInteger)
            {
                minInclusiveValue = 1;
            }
            else if (minInclusive is null && xmlTypeCode == XmlTypeCode.NonNegativeInteger)
            {
                minInclusiveValue = 0;
            }
            else if (minInclusive != null && decimal.TryParse(minInclusive.Value, out decimal value))
            {
                minInclusiveValue = value;
            }

            // If either value is null, then that value is either unbounded or too large to fit in any numeric type.
            if (minInclusiveValue != null && maxInclusiveValue != null) {
                if (minInclusiveValue >= byte.MinValue && maxInclusiveValue <= byte.MaxValue)
                    result = typeof(byte);
                else if (minInclusiveValue >= sbyte.MinValue && maxInclusiveValue <= sbyte.MaxValue)
                    result = typeof(sbyte);
                else if (minInclusiveValue >= ushort.MinValue && maxInclusiveValue <= ushort.MaxValue)
                    result = typeof(ushort);
                else if (minInclusiveValue >= short.MinValue && maxInclusiveValue <= short.MaxValue)
                    result = typeof(short);
                else if (minInclusiveValue >= uint.MinValue && maxInclusiveValue <= uint.MaxValue)
                    result = typeof(uint);
                else if (minInclusiveValue >= int.MinValue && maxInclusiveValue <= int.MaxValue)
                    result = typeof(int);
                else if (minInclusiveValue >= ulong.MinValue && maxInclusiveValue <= ulong.MaxValue)
                    result = typeof(ulong);
                else if (minInclusiveValue >= long.MinValue && maxInclusiveValue <= long.MaxValue)
                    result = typeof(long);
                else // If it didn't fit in a decimal, we could not have gotten here.
                    result = typeof(decimal);

                return result;
            }

            if (!(restrictions.SingleOrDefault(r => r is TotalDigitsRestrictionModel) is TotalDigitsRestrictionModel totalDigits)
                || ((xmlTypeCode == XmlTypeCode.PositiveInteger
                     || xmlTypeCode == XmlTypeCode.NonNegativeInteger) && totalDigits.Value >= 30)
                || ((xmlTypeCode == XmlTypeCode.Integer
                     || xmlTypeCode == XmlTypeCode.NegativeInteger
                     || xmlTypeCode == XmlTypeCode.NonPositiveInteger) && totalDigits.Value >= 29))
            {
                if (configuration.UseIntegerDataTypeAsFallback && configuration.IntegerDataType != null)
                  return configuration.IntegerDataType;
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

        public static Type GetEffectiveType(this XmlSchemaDatatype type, GeneratorConfiguration configuration, IEnumerable<RestrictionModel> restrictions, bool attribute = false)
        {
            Type resultType;

            switch (type.TypeCode)
            {
                case XmlTypeCode.AnyAtomicType:
                    // union
                    resultType = typeof(string);
                    break;
                case XmlTypeCode.AnyUri:
                case XmlTypeCode.Duration:
                case XmlTypeCode.GDay:
                case XmlTypeCode.GMonth:
                case XmlTypeCode.GMonthDay:
                case XmlTypeCode.GYear:
                case XmlTypeCode.GYearMonth:
                    resultType = typeof(string);
                    break;
                case XmlTypeCode.Time:
                    resultType = typeof(DateTime);
                    break;
                case XmlTypeCode.Idref:
                    resultType = typeof(string);
                    break;
                case XmlTypeCode.Integer:
                case XmlTypeCode.NegativeInteger:
                case XmlTypeCode.NonNegativeInteger:
                case XmlTypeCode.NonPositiveInteger:
                case XmlTypeCode.PositiveInteger:
                    resultType = GetIntegerDerivedType(type, configuration, restrictions);
                    break;
                default:
                    resultType = type.ValueType;
                    break;
            }

            if (type.Variety == XmlSchemaDatatypeVariety.List)
            {
                if (resultType.IsArray)
                    resultType = resultType.GetElementType();

                // XmlSerializer doesn't support xsd:list for elements, only for attributes:
                // https://docs.microsoft.com/en-us/previous-versions/dotnet/netframework-4.0/t84dzyst(v%3dvs.100)

                // Also, de/serialization fails when the XML schema type is ambiguous
                // DateTime -> date, datetime, or time
                // byte[] -> hexBinary or base64Binary

                if (!attribute || resultType == typeof(DateTime) || resultType == typeof(byte[]))
                    resultType = typeof(string);
            }

            return resultType;
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
                if (typeModel.IsAnonymous)
                {
                    qualifiedName = typeModel.XmlSchemaName;
                }
                else
                {
                    qualifiedName = typeModel.XmlSchemaType.GetQualifiedName();
                }
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
            var propBackingFieldName = propertyModel.Name.ToBackingField(classModel?.Configuration.PrivateMemberPrefix);

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

                var backingFieldName = prop.Name.ToBackingField(classModel.Configuration.PrivateMemberPrefix);
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

        public static string GetUniquePropertyName(this TypeModel tm, string name)
        {
            if (tm is ClassModel cls)
            {
                var i = 0;
                var n = name;
                var baseClasses = cls.AllBaseClasses.ToList();

                while (baseClasses.SelectMany(b => b.Properties).Any(p => p.Name == n))
                    n = name + (++i);

                return n;
            }

            return name;
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

        internal static Uri CreateUri(string uri) => string.IsNullOrEmpty(uri) ? null : new Uri(uri);

        public static KeyValuePair<NamespaceKey, string> ParseNamespace(string nsArg, string namespacePrefix)
        {
            var parts = nsArg.Split(new[] { '=' }, 2);
            if (parts.Length != 2)
            {
                throw new ArgumentException("XML and C# namespaces should be separated by '='.");
            }
            
            var xmlNs = parts[0];
            var netNs = parts[1];
            var parts2 = xmlNs.Split(new[] { '|' }, 2);
            var source = parts2.Length == 2 ? new Uri(parts2[1], UriKind.RelativeOrAbsolute) : null;
            xmlNs = parts2[0];
            if (!string.IsNullOrEmpty(namespacePrefix))
            {
                netNs = namespacePrefix + "." + netNs;
            }
            return new KeyValuePair<NamespaceKey, string>(new NamespaceKey(source, xmlNs), netNs);
        }
    }
}