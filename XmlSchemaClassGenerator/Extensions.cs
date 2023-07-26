using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml;
using System.Xml.Schema;

namespace XmlSchemaClassGenerator
{
    public static class Extensions
    {
        public static XmlSchema GetSchema(this XmlSchemaObject xmlSchemaObject)
        {
            while (xmlSchemaObject is not null and not XmlSchema)
                xmlSchemaObject = xmlSchemaObject.Parent;
            return (XmlSchema)xmlSchemaObject;
        }

        public static PropertyValueTypeCode GetPropertyValueTypeCode(this TypeModel model) => model switch
        {
            SimpleModel { ValueType.IsArray: true } => PropertyValueTypeCode.Array,
            SimpleModel { ValueType.IsValueType: true } => PropertyValueTypeCode.ValueType,
            SimpleModel or not EnumModel => PropertyValueTypeCode.Other,
            _ => PropertyValueTypeCode.ValueType
        };

        public static IEnumerable<TSource> DistinctBy<TSource, TKey>(this IEnumerable<TSource> source, Func<TSource, TKey> propertySelector)
        {
            return source.GroupBy(propertySelector).Select(x => x.First());
        }

        public static string QuoteIfNeeded(this string text) => !string.IsNullOrEmpty(text) && text.Contains(" ") ? "\"" + text + "\"" : text;

        public static bool IsDerivedFrom(this XmlSchemaType type, XmlQualifiedName qualifiedName)
        {
            while (type != null)
            {
                if (type.QualifiedName == qualifiedName) return true;
                type = type.BaseXmlSchemaType;
            }

            return false;
        }
    }
}
