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
        public static string ToNormalizedEnumName(this string name)
        {
            name = name.Trim().Replace(' ', '_').Replace('\t', '_');
            if (string.IsNullOrEmpty(name))
                return "Item";
            if (!char.IsLetter(name[0]))
                return string.Format("Item{0}", name);
            return name;
        }

        // Match non-letter followed by letter
        static Regex PascalCaseRegex = new Regex(@"[^\p{L}]\p{L}", RegexOptions.Compiled);

        // Uppercases first letter and all letters following non-letters.
        // Examples: testcase -> Testcase, html5element -> Html5Element, test_case -> Test_Case
        public static string ToPascalCase(this string s)
        {
            if (string.IsNullOrEmpty(s)) return s;
            return char.ToUpperInvariant(s[0])
                + PascalCaseRegex.Replace(s.Substring(1), m => m.Value[0] + char.ToUpperInvariant(m.Value[1]).ToString());
        }

        public static string ToCamelCase(this string s)
        {
            if (string.IsNullOrEmpty(s)) return s;
            return char.ToLowerInvariant(s[0]) + s.Substring(1);
        }

        public static string ToBackingField(this string propertyName)
        {
            return string.Concat("_", propertyName.ToCamelCase());
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
                        result = false;
                    else
                        result = null;
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

        private static Type GetEffectiveType(XmlTypeCode typeCode, XmlSchemaDatatypeVariety variety, GeneratorConfiguration configuration)
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
                case XmlTypeCode.Time:
                    result = variety == XmlSchemaDatatypeVariety.List ? typeof(string[]) : typeof(string);
                    break;
                case XmlTypeCode.Integer:
                case XmlTypeCode.NegativeInteger:
                case XmlTypeCode.NonNegativeInteger:
                case XmlTypeCode.NonPositiveInteger:
                case XmlTypeCode.PositiveInteger:
                    if (configuration.IntegerDataType == null || configuration.IntegerDataType == typeof(string))
                        result = typeof(string);
                    else
                    {
                        result = configuration.IntegerDataType;
                    }
                    break;
                default:
                    result = null;
                    break;
            }
            return result;
        }

        public static Type GetEffectiveType(this XmlSchemaDatatype type, GeneratorConfiguration configuration)
        {
            return GetEffectiveType(type.TypeCode, type.Variety, configuration) ?? type.ValueType;
        }

        public static Type GetEffectiveType(this XmlSchemaType type, GeneratorConfiguration configuration)
        {
            return GetEffectiveType(type.TypeCode, type.Datatype.Variety, configuration) ?? type.Datatype.ValueType;
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
            var simpleTypeModel = typeModel as SimpleModel;
            if (simpleTypeModel == null)
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
            var propBackingFieldName = propertyModel.Name.ToBackingField();
            var classModel = typeModel as ClassModel;
            if (classModel == null)
                return propBackingFieldName;
            
            var i = 0;
            foreach (var prop in classModel.Properties)
            {
                if (!classModel.EnableDataBinding && !(prop.Type is SimpleModel))
                    continue;

                if (propertyModel == prop)
                {
                    i += 1;
                    break;
                }

                var backingFieldName = prop.Name.ToBackingField();
                if (backingFieldName == propBackingFieldName)
                    i += 1;
            }

            if (i <= 1)
                return propBackingFieldName;

            return string.Format("{0}{1}", propBackingFieldName, i);
        }
    }
}