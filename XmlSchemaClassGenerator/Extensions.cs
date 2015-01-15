using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Schema;

namespace XmlSchemaClassGenerator
{
    public static class Extensions
    {
        public static XmlSchema GetSchema(this XmlSchemaObject xmlSchemaObject)
        {
            while (xmlSchemaObject != null && !(xmlSchemaObject is XmlSchema)) 
                xmlSchemaObject = xmlSchemaObject.Parent;
            return (XmlSchema)xmlSchemaObject;
        }

        public static PropertyValueTypeCode GetPropertyValueTypeCode(this TypeModel model)
        {
            var simpleType = model as SimpleModel;
            if (simpleType == null)
            {
                var enumModel = model as EnumModel;
                if (enumModel == null)
                    return PropertyValueTypeCode.Other;
                return PropertyValueTypeCode.ValueType;
            }
            if (simpleType.ValueType.IsArray)
                return PropertyValueTypeCode.Array;
            if (simpleType.ValueType.IsValueType)
                return PropertyValueTypeCode.ValueType;
            return PropertyValueTypeCode.Other;
        }

        public static IEnumerable<TSource> DistinctBy<TSource, TKey>(this IEnumerable<TSource> source, Func<TSource, TKey> propertySelector)
        {
            return source.GroupBy(propertySelector).Select(x => x.First());
        }
    }
}
