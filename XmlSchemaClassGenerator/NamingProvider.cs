namespace XmlSchemaClassGenerator
{
    /// <summary>
    /// Provides options to customize elementnamens with own logik
    /// </summary>
    public class NamingProvider
    {
        /// <summary>
        /// Scheme for the naming
        /// </summary>
        private readonly NamingScheme _namingScheme;

        /// <summary>
        /// Creates the provider
        /// </summary>
        /// <param name="namingScheme">Scheme for the naming</param>
        public NamingProvider(NamingScheme namingScheme)
        {
            _namingScheme = namingScheme;
        }

        /// <summary>
        /// Creates a name for a property of a type
        /// </summary>
        /// <param name="typeModelName">Name of the typeModel</param>
        /// <param name="attributeName">Attributename in the orginal XML</param>
        /// <returns>Name of the Property</returns>
        public virtual string PropertyNameFromAttribute(string typeModelName, string attributeName)
        {
            return PropertyNameFromElement(typeModelName, attributeName);
        }

        /// <summary>
        /// Creates a name for a property of a type
        /// </summary>
        /// <param name="typeModelName">Name of the typeModel</param>
        /// <param name="elementName">Elementname in the orginal XML</param>
        /// <returns>Name of the Property</returns>
        public virtual string PropertyNameFromElement(string typeModelName, string elementName)
        {
            return typeModelName.ToTitleCase(_namingScheme) + elementName.ToTitleCase(_namingScheme);
        }

        /// <summary>
        /// Creates a name for an enummember based on a value
        /// </summary>
        /// <param name="enumName">Name of the enum</param>
        /// <param name="value">Value in the original XML</param>
        /// <returns>Name of the Enummember</returns>
        public virtual string EnumMemberNameFromValue(string enumName, string value)
        {
            return value.ToTitleCase(_namingScheme).ToNormalizedEnumName();
        }
    }
}