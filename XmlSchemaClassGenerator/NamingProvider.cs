namespace XmlSchemaClassGenerator
{
    using System.Xml;
    using System.Xml.Linq;
    using System.Xml.Schema;

    /// <summary>
    /// Provides options to customize member names
    /// </summary>
    public class NamingProvider
        : INamingProvider
    {
        protected readonly NamingScheme _namingScheme;

        /// <summary>
        /// Initializes a new instance of the <see cref="NamingProvider"/> class.
        /// </summary>
        /// <param name="namingScheme">The naming scheme.</param>
        public NamingProvider(NamingScheme namingScheme)
        {
            _namingScheme = namingScheme;
        }

        /// <summary>
        /// Creates a name for a property from an attribute name
        /// </summary>
        /// <param name="typeModelName">Name of the typeModel</param>
        /// <param name="attributeName">Attribute name</param>
        /// <param name="attribute">Original XSD attribute</param>
        /// <returns>Name of the property</returns>
        public virtual string PropertyNameFromAttribute(string typeModelName, string attributeName, XmlSchemaAttribute attribute)
        {
            return typeModelName.ToTitleCase(_namingScheme) + attributeName.ToTitleCase(_namingScheme);
        }

        /// <summary>
        /// Creates a name for a property from an element name
        /// </summary>
        /// <param name="typeModelName">Name of the typeModel</param>
        /// <param name="elementName">Element name</param>
        /// <param name="element">Original XSD element</param>
        /// <returns>Name of the property</returns>
        public virtual string PropertyNameFromElement(string typeModelName, string elementName, XmlSchemaElement element)
        {
            return typeModelName.ToTitleCase(_namingScheme) + elementName.ToTitleCase(_namingScheme);
        }

        /// <inheritdoc/>
        public virtual string TypeNameFromAttribute(string typeModelName, string attributeName, XmlSchemaAttribute attribute)
            => PropertyNameFromAttribute(typeModelName, attributeName, attribute);

        /// <inheritdoc/>
        public virtual string TypeNameFromElement(string typeModelName, string elementName, XmlSchemaElement element)
            => PropertyNameFromElement(typeModelName, elementName, element);

        /// <summary>
        /// Creates a name for an enum member based on a value
        /// </summary>
        /// <param name="enumName">Name of the enum</param>
        /// <param name="value">Value name</param>
        /// <param name="xmlFacet">Original XSD enumeration facet</param>
        /// <returns>Name of the enum member</returns>
        public virtual string EnumMemberNameFromValue(string enumName, string value, XmlSchemaEnumerationFacet xmlFacet)
        {
            return value.ToTitleCase(_namingScheme).ToNormalizedEnumName();
        }

        /// <summary>
        /// Define the name to be used when a ComplexType is found in the XSD.
        /// </summary>
        /// <param name="qualifiedName">The name as defined in the XSD if present.</param>
        /// <param name="complexType">Original XSD ComplexType</param>
        /// <returns>A string with a valid C# identifier name.</returns>
        public virtual string ComplexTypeNameFromQualifiedName(XmlQualifiedName qualifiedName, XmlSchemaComplexType complexType)
        {
            return QualifiedNameToTitleCase(qualifiedName);
        }

        /// <summary>
        /// Define the name to be used when a AttributeGroup is found in the XSD.
        /// </summary>
        /// <param name="qualifiedName">The name as defined in the XSD if present.</param>
        /// <param name="attributeGroup">Original XSD AttributeGroup</param>
        /// <returns>A string with a valid C# identifier name.</returns>
        public virtual string AttributeGroupTypeNameFromQualifiedName(XmlQualifiedName qualifiedName, XmlSchemaAttributeGroup attributeGroup)
        {
            return QualifiedNameToTitleCase(qualifiedName);
        }

        /// <summary>
        /// Define the name to be used when a GroupType is found in the XSD.
        /// </summary>
        /// <param name="qualifiedName">The name as defined in the XSD if present.</param>
        /// <param name="group">Original XSD group</param>
        /// <returns>A string with a valid C# identifier name.</returns>
        public virtual string GroupTypeNameFromQualifiedName(XmlQualifiedName qualifiedName, XmlSchemaGroup group)
        {
            return QualifiedNameToTitleCase(qualifiedName);
        }

        /// <summary>
        /// Define the name to be used when a SimpleType is found in the XSD.
        /// </summary>
        /// <param name="qualifiedName">The name as defined in the XSD if present.</param>
        /// <param name="simpleType">Original XSD SimpleType</param>
        /// <returns>A string with a valid C# identifier name.</returns>
        public virtual string SimpleTypeNameFromQualifiedName(XmlQualifiedName qualifiedName, XmlSchemaSimpleType simpleType)
        {
            return QualifiedNameToTitleCase(qualifiedName);
        }

        /// <summary>
        /// Define the name to be used for the root class.
        /// </summary>
        /// <param name="qualifiedName">The name as defined in the XSD if present.</param>
        /// <param name="xmlElement">Original XSD element</param>
        /// <returns>A string with a valid C# identifier name.</returns>
        public virtual string RootClassNameFromQualifiedName(XmlQualifiedName qualifiedName, XmlSchemaElement xmlElement)
        {
            return QualifiedNameToTitleCase(qualifiedName);
        }

        /// <summary>
        /// Define the name to be used when an enum type is found in the XSD.
        /// </summary>
        /// <param name="qualifiedName">The name as defined in the XSD if present.</param>
        /// <param name="xmlSimpleType">Original XSD SimpleType</param>
        /// <returns>A string with a valid C# identifier name.</returns>
        public virtual string EnumTypeNameFromQualifiedName(XmlQualifiedName qualifiedName, XmlSchemaSimpleType xmlSimpleType)
        {
            return QualifiedNameToTitleCase(qualifiedName);
        }

        /// <summary>
        /// Define the name to be used when an attribute is found in the XSD.
        /// </summary>
        /// <param name="qualifiedName">The name as defined in the XSD if present.</param>
        /// <param name="xmlAttribute">Original XSD attribute</param>
        /// <returns>A string with a valid C# identifier name.</returns>
        public virtual string AttributeNameFromQualifiedName(XmlQualifiedName qualifiedName, XmlSchemaAttribute xmlAttribute)
        {
            return QualifiedNameToTitleCase(qualifiedName);
        }

        /// <summary>
        /// Define the name of the C# class property from the element name in XSD.
        /// </summary>
        /// <param name="qualifiedName">The name as defined in the XSD if present.</param>
        /// <param name="xmlElement">Original XSD element</param>
        /// <returns>A string with a valid C# identifier name.</returns>
        public virtual string ElementNameFromQualifiedName(XmlQualifiedName qualifiedName, XmlSchemaElement xmlElement)
        {
            return QualifiedNameToTitleCase(qualifiedName);
        }

        /// <summary>
        /// Used internally to make the QualifiedName have the desired naming schema.
        /// </summary>
        /// <param name="qualifiedName">Not null element.</param>
        /// <returns>A string formatted as desired.</returns>
        protected virtual string QualifiedNameToTitleCase(XmlQualifiedName qualifiedName)
        {
            return qualifiedName.Name.ToTitleCase(_namingScheme);
        }
    }
}
