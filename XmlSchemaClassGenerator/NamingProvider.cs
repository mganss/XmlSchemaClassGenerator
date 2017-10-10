namespace XmlSchemaClassGenerator
{
    /// <summary>
    /// Provides options to customize member names
    /// </summary>
    public class NamingProvider
    {
        private readonly NamingScheme _namingScheme;

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
        /// <returns>Name of the property</returns>
        public virtual string PropertyNameFromAttribute(string typeModelName, string attributeName)
        {
            return PropertyNameFromElement(typeModelName, attributeName);
        }

        /// <summary>
        /// Creates a name for a property from an element name
        /// </summary>
        /// <param name="typeModelName">Name of the typeModel</param>
        /// <param name="elementName">Element name</param>
        /// <returns>Name of the property</returns>
        public virtual string PropertyNameFromElement(string typeModelName, string elementName)
        {
            return typeModelName.ToTitleCase(_namingScheme) + elementName.ToTitleCase(_namingScheme);
        }

        /// <summary>
        /// Creates a name for an enum member based on a value
        /// </summary>
        /// <param name="enumName">Name of the enum</param>
        /// <param name="value">Value name</param>
        /// <returns>Name of the enum member</returns>
        public virtual string EnumMemberNameFromValue(string enumName, string value)
        {
            return value.ToTitleCase(_namingScheme).ToNormalizedEnumName();
        }
    }
}