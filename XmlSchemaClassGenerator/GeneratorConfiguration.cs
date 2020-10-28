using System;
using System.CodeDom;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text.RegularExpressions;

namespace XmlSchemaClassGenerator
{
    public class GeneratorConfiguration
    {
        public static Regex IdentifierRegex { get; } = new Regex(@"^@?[_\p{L}\p{Nl}][\p{L}\p{Nl}\p{Mn}\p{Mc}\p{Nd}\p{Pc}\p{Cf}]*$", RegexOptions.Compiled);

        public GeneratorConfiguration()
        {
            NamespaceProvider = new NamespaceProvider()
            {
                GenerateNamespace = key =>
                {
                    var xn = key.XmlSchemaNamespace;
                    var name = string.Join(".",
                        xn.Split('/').Where(p => p != "schema" && IdentifierRegex.IsMatch(p))
                            .Select(n => n.ToTitleCase(NamingScheme.PascalCase)));
                    if (!string.IsNullOrEmpty(NamespacePrefix))
                    {
                        name = NamespacePrefix + (string.IsNullOrEmpty(name) ? "" : ("." + name));
                    }
                    return name;
                },
            };

            NamingScheme = NamingScheme.PascalCase;
            DataAnnotationMode = DataAnnotationMode.All;
            GenerateSerializableAttribute = GenerateDesignerCategoryAttribute = true;
            CollectionType = typeof(Collection<>);
            MemberVisitor = (member, model) => { };
            TypeVisitor = (type, model) => { };
            NamingProvider = new NamingProvider(NamingScheme);
            Version = VersionProvider.CreateFromAssembly();
            EnableUpaCheck = true;
        }

        /// <summary>
        /// The writer to be used to generate the code files
        /// </summary>
        public OutputWriter OutputWriter { get; set; }

        /// <summary>
        /// A provider to obtain the name and version of the tool
        /// </summary>
        public VersionProvider Version { get; set; }

        /// <summary>
        /// The prefix which gets added to all automatically generated namespaces
        /// </summary>
        public string NamespacePrefix { get; set; }

        /// <summary>
        /// The caching namespace provider
        /// </summary>
        public NamespaceProvider NamespaceProvider { get; set; }
        /// <summary>
        /// The folder where the output files get stored
        /// </summary>
        public string OutputFolder { get; set; }
        /// <summary>
        /// Provides a way to redirect the log output
        /// </summary>
        public Action<string> Log { get; set; }
        /// <summary>
        /// Enable data binding with INotifyPropertyChanged
        /// </summary>
        public bool EnableDataBinding { get; set; }
        /// <summary>
        /// Use XElement instead of XmlElement for Any nodes?
        /// </summary>
        public bool UseXElementForAny { get; set; }

        private NamingScheme namingScheme;

        /// <summary>
        /// How are the names of the created properties changed?
        /// </summary>
        public NamingScheme NamingScheme
        {
            get => namingScheme;

            set
            {
                namingScheme = value;
                NamingProvider = new NamingProvider(namingScheme);
            }
        }

        /// <summary>
        /// Emit the "Order" attribute value for XmlElementAttribute to ensure the correct order
        /// of the serialized XML elements.
        /// </summary>
        public bool EmitOrder { get; set; }
        /// <summary>
        /// Determines the kind of annotations to emit
        /// </summary>
        public DataAnnotationMode DataAnnotationMode { get; set; }
        /// <summary>
        /// Generate Nullable members for optional elements?
        /// </summary>
        public bool GenerateNullables { get; set; }
        /// <summary>
        /// Use ShouldSerialize pattern in where possible to support nullables?
        /// </summary>
        public bool UseShouldSerializePattern { get; set; }
        /// <summary>
        /// Generate the Serializable attribute?
        /// </summary>
        public bool GenerateSerializableAttribute { get; set; }
        /// <summary>
        /// Generate the DebuggerStepThroughAttribute?
        /// </summary>
        public bool GenerateDebuggerStepThroughAttribute { get; set; }
        /// <summary>
        /// Generate the DesignerCategoryAttribute?
        /// </summary>
        public bool GenerateDesignerCategoryAttribute { get; set; }
        /// <summary>
        /// The default collection type to use
        /// </summary>
        public Type CollectionType { get; set; }
        /// <summary>
        /// The default collection type implementation to use
        /// </summary>
        /// <remarks>
        /// This is only useful when CollectionType is an interface type.
        /// </remarks>
        public Type CollectionImplementationType { get; set; }
        /// <summary>
        /// Default data type for numeric fields
        /// </summary>
        public Type IntegerDataType { get; set; }
        /// <summary>
        /// Use <see cref="IntegerDataType"/> only if no better type can be inferred
        /// </summary>
        public bool UseIntegerDataTypeAsFallback { get; set; }
        /// <summary>
        /// Generate Entity Framework Code First compatible classes
        /// </summary>
        public bool EntityFramework { get; set; }
        /// <summary>
        /// Generate interfaces for groups and attribute groups
        /// </summary>
        public bool GenerateInterfaces { get; set; }
        /// <summary>
        /// Generate <see cref="System.ComponentModel.DescriptionAttribute"/> from XML comments.
        /// </summary>
        public bool GenerateDescriptionAttribute { get; set; }
        /// <summary>
        /// Generate types as <c>internal</c> if <c>true</c>. <c>public</c> otherwise.
        /// </summary>
        public bool AssemblyVisible { get; set; }
        /// <summary>
        /// Generator Code reference options
        /// </summary>
        public CodeTypeReferenceOptions CodeTypeReferenceOptions { get; set; }
        /// <summary>
        /// Determines the kind of collection accessor modifiers to emit and controls baking collection fields initialization
        /// </summary>
        public CollectionSettersMode CollectionSettersMode { get; set; }

        /// <summary>
        /// The name of the property that will contain the text value of an XML element
        /// </summary>
        public string TextValuePropertyName { get; set; } = "Value";

        /// <summary>
        /// Provides a fast and safe way to write to the Log
        /// </summary>
        /// <param name="messageCreator"></param>
        /// <remarks>
        /// Does nothing when the Log isn't set.
        /// </remarks>
        public void WriteLog(Func<string> messageCreator)
        {
            Log?.Invoke(messageCreator());
        }
        /// <summary>
        /// Write the message to the log.
        /// </summary>
        /// <param name="message"></param>
        public void WriteLog(string message)
        {
            Log?.Invoke(message);
        }

        /// <summary>
        /// Optional delegate that is called for each generated type member
        /// </summary>
        public Action<CodeTypeMember, PropertyModel> MemberVisitor { get; set; }

        /// <summary>
        /// Optional delegate that is called for each generated type (class, interface, enum)
        /// </summary>
        public Action<CodeTypeDeclaration, TypeModel> TypeVisitor { get; set; }

        /// <summary>
        /// Provides options to customize Elementnamens with own logik
        /// </summary>
        public INamingProvider NamingProvider { get; set; }

        public bool DisableComments { get; set; }

        /// <summary>
        /// If True then do not force generator to emit IsNullable=true in XmlElement annotation
        /// for nillable elements when element is nullable (minOccurs &lt; 1 or parent element is choice)
        /// </summary>
        public bool DoNotForceIsNullable { get; set; }

        public string PrivateMemberPrefix { get; set; } = "_";

        /// <summary>
        /// Check for Unique Particle Attribution (UPA) violations
        /// </summary>
        public bool EnableUpaCheck { get; set; }

        /// <summary>
        /// When a ComplexType has a member that is used as a "collection" around another ComplexType
        /// the serializer will output the intermediate ComplexType.
        ///
        /// <code>
        /// &lt;xs:element name="books"&gt;
        ///   &lt;xs:complexType&gt;
        ///     &lt;xs:sequence&gt;
        ///       &lt;xs:element name="components"&gt;
        ///         &lt;xs:complexType&gt;
        ///           &lt;xs:sequence&gt;
        ///             &lt;xs:element name="component" type="componentType" maxOccurs="unbounded"/&gt;
        ///           &lt;/xs:sequence&gt;
        ///         &lt;/xs:complexType&gt;
        ///       &lt;/xs:element&gt;
        ///     &lt;/xs:sequence&gt;
        ///   &lt;xs:complexType&gt;
        /// &lt;/xs:element&gt;
        /// </code>
        ///
        /// With <code>false</code> it generates the classes:
        /// 
        /// <code>
        /// public class books {
        ///     public Container&lt;componentType&gt; components {get; set;}
        /// }
        ///
        /// public class componentType {} 
        /// </code>
        ///
        /// With <code>true</code> it generates the classes:
        /// 
        /// <code>
        /// public class books {
        ///     public Container&lt;componentType&gt; components {get; set;}
        /// }
        ///
        /// public class bookscomponents {
        ///     public Container&lt;componentType&gt; components {get; set;}
        /// }
        ///
        /// public class componentType {} 
        /// </code>
        /// </summary>
        public bool GenerateComplexTypesForCollections { get; set; } = true;

        /// <summary>
        /// Separates each class into an individual file
        /// </summary>
        public bool SeparateClasses { get; set; } = false;

        /// <summary>
        /// Generates a separate property for each element of a substitution group
        /// </summary>
        public bool SeparateSubstitutes { get; set; } = false;
    }
}
