namespace XmlSchemaClassGenerator
{
    using System.Xml;

    public interface INamingProvider
    {
        /// <summary>
        /// Creates a name for a property from an attribute name
        /// </summary>
        /// <param name="typeModelName">Name of the typeModel</param>
        /// <param name="attributeName">Attribute name</param>
        /// <returns>Name of the property</returns>
        string PropertyNameFromAttribute(string typeModelName, string attributeName);

        /// <summary>
        /// Creates a name for a property from an element name
        /// </summary>
        /// <param name="typeModelName">Name of the typeModel</param>
        /// <param name="elementName">Element name</param>
        /// <returns>Name of the property</returns>
        string PropertyNameFromElement(string typeModelName, string elementName);

        /// <summary>
        /// Creates a name for an enum member based on a value
        /// </summary>
        /// <param name="enumName">Name of the enum</param>
        /// <param name="value">Value name</param>
        /// <returns>Name of the enum member</returns>
        string EnumMemberNameFromValue(string enumName, string value);

        string ComplexTypeNameFromQualifiedName(XmlQualifiedName qualifiedName);

        string AttributeGroupTypeNameFromQualifiedName(XmlQualifiedName qualifiedName);

        string GroupTypeNameFromQualifiedName(XmlQualifiedName qualifiedName);

        string SimpleTypeNameFromQualifiedName(XmlQualifiedName qualifiedName);

        string RootClassNameFromQualifiedName(XmlQualifiedName qualifiedName);

        string EnumTypeNameFromQualifiedName(XmlQualifiedName qualifiedName);

        string AttributeNameFromQualifiedName(XmlQualifiedName qualifiedName);

        string ElementNameFromQualifiedName(XmlQualifiedName qualifiedName);
    }
}