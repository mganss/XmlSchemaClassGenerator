using System;
using System.CodeDom;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace XmlSchemaClassGenerator
{
    public class GeneratorConfiguration
    {
        public GeneratorConfiguration()
        {
            NamespaceProvider = new NamespaceProvider()
            {
                GenerateNamespace = key =>
                {
                    var xn = key.XmlSchemaNamespace;
                    var name = string.Join(".",
                        xn.Split('/').Where(p => Regex.IsMatch(p, @"^[A-Za-z]+$") && p != "schema")
                            .Select(n => Generator.ToTitleCase(n, NamingScheme.PascalCase)));
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
        }

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
        /// <summary>
        /// How are the names of the created properties changed?
        /// </summary>
        public NamingScheme NamingScheme { get; set; }
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
        /// Generate the Serializable attribute?
        /// </summary>
        public bool GenerateSerializableAttribute { get; set; }
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
        /// Generate Entity Framework Code First compatible classes
        /// </summary>
        public bool EntityFramework { get; set; }
        /// <summary>
        /// Generate interfaces for groups and attribute groups
        /// </summary>
        public bool GenerateInterfaces { get; set; }

        /// <summary>
        /// Generator Code reference options
        /// </summary>
        public CodeTypeReferenceOptions CodeTypeReferenceOptions { get; set; }

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
            if (Log != null)
            {
                Log(messageCreator());
            }
        }
        /// <summary>
        /// Write the message to the log.
        /// </summary>
        /// <param name="message"></param>
        public void WriteLog(string message)
        {
            if (Log != null)
            {
                Log(message);
            }
        }

        /// <summary>
        /// Optional delegate that is called for each generated type member
        /// </summary>
        public Action<CodeTypeMember, PropertyModel> MemberVisitor { get; set; }
    }
}
