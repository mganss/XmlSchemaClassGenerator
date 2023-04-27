using System.Collections.Generic;
using System.Xml;
using System.Xml.Schema;

namespace XmlSchemaClassGenerator.NamingProviders
{
    /// <summary>
    /// Provides options to customize member names, and automatically substitute names for defined types/members.
    /// </summary>
    public class SubstituteNamingProvider
        : NamingProvider, INamingProvider
    {
        private readonly Dictionary<string, string> _nameSubstitutes;

        /// <inheritdoc cref="SubstituteNamingProvider(NamingScheme, Dictionary{string, string})"/>
        public SubstituteNamingProvider(NamingScheme namingScheme)
            : this(namingScheme, new())
        {
        }

        /// <inheritdoc cref="SubstituteNamingProvider(NamingScheme, Dictionary{string, string})"/>
        public SubstituteNamingProvider(Dictionary<string, string> nameSubstitutes)
            : this(NamingScheme.PascalCase, nameSubstitutes)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SubstituteNamingProvider"/> class.
        /// </summary>
        /// <param name="namingScheme">The naming scheme.</param>
        /// <param name="nameSubstitutes">
        /// A dictionary containing name substitute pairs.
        /// <para>
        /// Keys need to be prefixed with an appropriate kind ID as documented at:
        /// <see href="https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/language-specification/documentation-comments#d42-id-string-format">https://t.ly/HHEI</see>.
        /// </para>
        /// <para>Prefix with <c>A:</c> to substitute any type/member.</para>
        /// </param>
        public SubstituteNamingProvider(NamingScheme namingScheme, Dictionary<string, string> nameSubstitutes)
            : base(namingScheme)
        {
            _nameSubstitutes = nameSubstitutes;
        }

        /// <inheritdoc/>
        public override string PropertyNameFromAttribute(string typeModelName, string attributeName, XmlSchemaAttribute attribute)
            => SubstituteName("P", base.PropertyNameFromAttribute(typeModelName, attributeName, attribute));

        /// <inheritdoc/>
        public override string PropertyNameFromElement(string typeModelName, string elementName, XmlSchemaElement element)
            => SubstituteName("P", base.PropertyNameFromElement(typeModelName, elementName, element));

        /// <inheritdoc/>
        public override string TypeNameFromAttribute(string typeModelName, string attributeName, XmlSchemaAttribute attribute)
            => SubstituteName("T", base.PropertyNameFromAttribute(typeModelName, attributeName, attribute));

        /// <inheritdoc/>
        public override string TypeNameFromElement(string typeModelName, string elementName, XmlSchemaElement element)
            => SubstituteName("T", base.PropertyNameFromElement(typeModelName, elementName, element));

        /// <inheritdoc/>
        public override string EnumMemberNameFromValue(string enumName, string value, XmlSchemaEnumerationFacet xmlFacet)
            => SubstituteName($"T:{enumName}", base.EnumMemberNameFromValue(enumName, value, xmlFacet));

        /// <inheritdoc/>
        public override string ComplexTypeNameFromQualifiedName(XmlQualifiedName qualifiedName, XmlSchemaComplexType complexType)
            => SubstituteName("T", base.ComplexTypeNameFromQualifiedName(qualifiedName, complexType));

        /// <inheritdoc/>
        public override string AttributeGroupTypeNameFromQualifiedName(XmlQualifiedName qualifiedName, XmlSchemaAttributeGroup attributeGroup)
            => SubstituteName("T", base.AttributeGroupTypeNameFromQualifiedName(qualifiedName, attributeGroup));

        /// <inheritdoc/>
        public override string GroupTypeNameFromQualifiedName(XmlQualifiedName qualifiedName, XmlSchemaGroup group)
            => SubstituteName("T", base.GroupTypeNameFromQualifiedName(qualifiedName, group));

        /// <inheritdoc/>
        public override string SimpleTypeNameFromQualifiedName(XmlQualifiedName qualifiedName, XmlSchemaSimpleType simpleType)
            => SubstituteName("T", base.SimpleTypeNameFromQualifiedName(qualifiedName, simpleType));

        /// <inheritdoc/>
        public override string RootClassNameFromQualifiedName(XmlQualifiedName qualifiedName, XmlSchemaElement xmlElement)
            => SubstituteName("T", base.RootClassNameFromQualifiedName(qualifiedName, xmlElement));

        /// <inheritdoc/>
        public override string EnumTypeNameFromQualifiedName(XmlQualifiedName qualifiedName, XmlSchemaSimpleType xmlSimpleType)
            => SubstituteName("T", base.EnumTypeNameFromQualifiedName(qualifiedName, xmlSimpleType));

        /// <inheritdoc/>
        public override string AttributeNameFromQualifiedName(XmlQualifiedName qualifiedName, XmlSchemaAttribute xmlAttribute)
            => SubstituteName("P", base.AttributeNameFromQualifiedName(qualifiedName, xmlAttribute));

        /// <inheritdoc/>
        public override string ElementNameFromQualifiedName(XmlQualifiedName qualifiedName, XmlSchemaElement xmlElement)
            => SubstituteName("P", base.ElementNameFromQualifiedName(qualifiedName, xmlElement));

        private string SubstituteName(string typeIdPrefix, string name)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                return name;
            }

            string substituteName;
            if (_nameSubstitutes.TryGetValue($"{typeIdPrefix}:{name}", out substituteName) || _nameSubstitutes.TryGetValue($"A:{name}", out substituteName))
            {
                return substituteName;
            }

            return name;
        }
    }
}
