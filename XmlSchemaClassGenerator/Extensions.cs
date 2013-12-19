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
            while (!(xmlSchemaObject is XmlSchema)) xmlSchemaObject = xmlSchemaObject.Parent;
            return (XmlSchema)xmlSchemaObject;
        }
    }
}
