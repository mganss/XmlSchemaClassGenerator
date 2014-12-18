using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Schema;

namespace XmlSchemaClassGenerator
{
    public static class CodeUtilities
    {
        private const string Alpha = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ";
        //private const string Num = "0123456789";

        public static string ToNormalizedEnumName(this string name)
        {
            name = name.Trim().Replace(' ', '_').Replace('\t', '_');
            if (string.IsNullOrEmpty(name))
                return "Item";
            if (Alpha.IndexOf(name[0]) == -1)
                return string.Format("Item{0}", name);
            return name;
        }

        private static bool? IsDataTypeAttributeAllowed(XmlTypeCode typeCode)
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
                    if (SimpleModel.IntegerDataType != null && SimpleModel.IntegerDataType != typeof(string))
                        result = false;
                    else
                        result = null;
                    break;
                default:
                    result = null;
                    break;
            }
            return result;
        }

        public static bool? IsDataTypeAttributeAllowed(this XmlSchemaDatatype type)
        {
            return IsDataTypeAttributeAllowed(type.TypeCode);
        }

        public static bool? IsDataTypeAttributeAllowed(this XmlSchemaType type)
        {
            return IsDataTypeAttributeAllowed(type.TypeCode);
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
                case XmlTypeCode.Time:
                    result = variety == XmlSchemaDatatypeVariety.List ? typeof(string[]) : typeof(string);
                    break;
                case XmlTypeCode.Integer:
                case XmlTypeCode.NegativeInteger:
                case XmlTypeCode.NonNegativeInteger:
                case XmlTypeCode.NonPositiveInteger:
                case XmlTypeCode.PositiveInteger:
                    if (SimpleModel.IntegerDataType == null || SimpleModel.IntegerDataType == typeof(string))
                        result = typeof(string);
                    else
                    {
                        result = SimpleModel.IntegerDataType;
                    }
                    break;
                default:
                    result = null;
                    break;
            }
            return result;
        }

        public static Type GetEffectiveType(this XmlSchemaDatatype type)
        {
            return GetEffectiveType(type.TypeCode, type.Variety) ?? type.ValueType;
        }

        public static Type GetEffectiveType(this XmlSchemaType type)
        {
            return GetEffectiveType(type.TypeCode, type.Datatype.Variety) ?? type.Datatype.ValueType;
        }
    }
}