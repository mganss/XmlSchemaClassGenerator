namespace XmlSchemaClassGenerator
{
    using System.Xml;
    using System.Xml.Schema;

    public interface INamingProvider
    {
        /// <summary>
        /// Creates a name for a property from an attribute name
        /// </summary>
        /// <param name="typeModelName">Name of the typeModel</param>
        /// <param name="attributeName">Attribute name</param>
        /// <param name="attribute">Original XSD attribute</param>
        /// <returns>Name of the property</returns>
        string PropertyNameFromAttribute(string typeModelName, string attributeName, XmlSchemaAttribute attribute);

        /// <summary>
        /// Creates a name for a property from an element name
        /// </summary>
        /// <param name="typeModelName">Name of the typeModel</param>
        /// <param name="elementName">Element name</param>
        /// <param name="element">Original XSD element</param>
        /// <returns>Name of the property</returns>
        string PropertyNameFromElement(string typeModelName, string elementName, XmlSchemaElement element);

        /// <summary>
        /// Creates a name for an inner type from an attribute name
        /// </summary>
        /// <param name="typeModelName">Name of the typeModel</param>
        /// <param name="attributeName">Attribute name</param>
        /// <param name="attribute">Original XSD attribute</param>
        /// <returns>Name of the inner type</returns>
        string TypeNameFromAttribute(string typeModelName, string attributeName, XmlSchemaAttribute attribute);

        /// <summary>
        /// Creates a name for an inner type from an element name
        /// </summary>
        /// <param name="typeModelName">Name of the typeModel</param>
        /// <param name="elementName">Element name</param>
        /// <param name="element">Original XSD element</param>
        /// <returns>Name of the inner type</returns>
        string TypeNameFromElement(string typeModelName, string elementName, XmlSchemaElement element);

        /// <summary>
        /// Creates a name for an enum member based on a value
        /// </summary>
        /// <param name="enumName">Name of the enum</param>
        /// <param name="value">Value name</param>
        /// <param name="xmlFacet">Original XSD enumeration facet</param>
        /// <returns>Name of the enum member</returns>
        string EnumMemberNameFromValue(string enumName, string value, XmlSchemaEnumerationFacet xmlFacet);

        /// <summary>
        /// Define the name to be used when a ComplexType is found in the XSD.
        /// </summary>
        /// <param name="qualifiedName">The name as defined in the XSD if present.</param>
        /// <param name="complexType">Original XSD ComplexType</param>
        /// <returns>A string with a valid C# identifier name.</returns>
        string ComplexTypeNameFromQualifiedName(XmlQualifiedName qualifiedName, XmlSchemaComplexType complexType);

        /// <summary>
        /// Define the name to be used when a AttributeGroup is found in the XSD.
        /// </summary>
        /// <param name="qualifiedName">The name as defined in the XSD if present.</param>
        /// <param name="attributeGroup">Original XSD AttributeGroup</param>
        /// <returns>A string with a valid C# identifier name.</returns>
        string AttributeGroupTypeNameFromQualifiedName(XmlQualifiedName qualifiedName, XmlSchemaAttributeGroup attributeGroup);

        /// <summary>
        /// Define the name to be used when a GroupType is found in the XSD.
        /// </summary>
        /// <param name="qualifiedName">The name as defined in the XSD if present.</param>
        /// <param name="group">Original XSD group</param>
        /// <returns>A string with a valid C# identifier name.</returns>
        string GroupTypeNameFromQualifiedName(XmlQualifiedName qualifiedName, XmlSchemaGroup group);

        /// <summary>
        /// Define the name to be used when a SimpleType is found in the XSD.
        /// </summary>
        /// <param name="qualifiedName">The name as defined in the XSD if present.</param>
        /// <param name="simpleType">Original XSD SimpleType</param>
        /// <returns>A string with a valid C# identifier name.</returns>
        string SimpleTypeNameFromQualifiedName(XmlQualifiedName qualifiedName, XmlSchemaSimpleType simpleType);

        /// <summary>
        /// Define the name to be used for the root class.
        /// </summary>
        /// <param name="qualifiedName">The name as defined in the XSD if present.</param>
        /// <param name="xmlElement">Original XSD element</param>
        /// <returns>A string with a valid C# identifier name.</returns>
        string RootClassNameFromQualifiedName(XmlQualifiedName qualifiedName, XmlSchemaElement xmlElement);

        /// <summary>
        /// Define the name to be used when an enum type is found in the XSD.
        /// </summary>
        /// <param name="qualifiedName">The name as defined in the XSD if present.</param>
        /// <param name="xmlSimpleType">Original XSD SimpleType</param>
        /// <returns>A string with a valid C# identifier name.</returns>
        string EnumTypeNameFromQualifiedName(XmlQualifiedName qualifiedName, XmlSchemaSimpleType xmlSimpleType);

        /// <summary>
        /// Define the name to be used when an attribute is found in the XSD.
        /// </summary>
        /// <param name="qualifiedName">The name as defined in the XSD if present.</param>
        /// <param name="xmlAttribute">Original XSD attribute</param>
        /// <returns>A string with a valid C# identifier name.</returns>
        string AttributeNameFromQualifiedName(XmlQualifiedName qualifiedName, XmlSchemaAttribute xmlAttribute);

        /// <summary>
        /// Define the name of the C# class property from the element name in XSD.
        /// </summary>
        /// <param name="qualifiedName">The name as defined in the XSD if present.</param>
        /// <param name="xmlElement">Original XSD element</param>
        /// <returns>A string with a valid C# identifier name.</returns>
        string ElementNameFromQualifiedName(XmlQualifiedName qualifiedName, XmlSchemaElement xmlElement);
    }
}