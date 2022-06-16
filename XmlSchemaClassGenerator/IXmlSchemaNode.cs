using System.Xml;
using System.Xml.Schema;

namespace XmlSchemaClassGenerator
{
    public interface IXmlSchemaNode
    {
        string DefaultValue { get; }
        string FixedValue { get; }
        XmlSchemaForm Form { get; }
        XmlQualifiedName QualifiedName { get; }
        XmlQualifiedName RefName { get; }

        XmlSchemaAnnotated Base { get; }
        XmlSchemaForm FormDefault { get; }
    }

    public sealed class XmlSchemaAttributeEx : IXmlSchemaNode
    {
        private XmlSchemaAttributeEx(XmlSchemaAttribute xs) => Real = xs;

        public XmlSchemaAttribute Real { get; }

        public string DefaultValue => Real.DefaultValue;
        public string FixedValue => Real.FixedValue;
        public XmlSchemaForm Form => Real.Form;
        public XmlQualifiedName QualifiedName => Real.QualifiedName;
        public XmlQualifiedName RefName => Real.RefName;
        public XmlSchemaSimpleType AttributeSchemaType => Real.AttributeSchemaType;
        public XmlSchemaAnnotated Base => Real;
        public XmlSchemaForm FormDefault => Base.GetSchema().AttributeFormDefault;
        public XmlSchemaUse Use => Real.Use;

        public static implicit operator XmlSchemaAttributeEx(XmlSchemaAttribute xs) => new(xs);
        public static implicit operator XmlSchemaAttribute(XmlSchemaAttributeEx ex) => ex.Real;
    }

    public sealed class XmlSchemaElementEx : IXmlSchemaNode
    {
        private XmlSchemaElementEx(XmlSchemaElement xs) => Real = xs;

        public XmlSchemaElement Real { get; }

        public string DefaultValue => Real.DefaultValue;
        public string FixedValue => Real.FixedValue;
        public XmlSchemaForm Form => Real.Form;
        public XmlQualifiedName QualifiedName => Real.QualifiedName;
        public XmlQualifiedName RefName => Real.RefName;
        public XmlSchemaType ElementSchemaType => Real.ElementSchemaType;
        public XmlSchemaAnnotated Base => Real;
        public XmlSchemaForm FormDefault => Base.GetSchema().ElementFormDefault;
        public bool IsNillable => Real.IsNillable;

        public static implicit operator XmlSchemaElementEx(XmlSchemaElement xs) => new(xs);
        public static implicit operator XmlSchemaElement(XmlSchemaElementEx ex) => ex.Real;
    }
}