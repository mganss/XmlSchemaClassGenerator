using System;
using System.CodeDom;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Xml;
using System.Xml.Schema;

namespace XmlSchemaClassGenerator
{
    public class Generator
    {
        private readonly GeneratorConfiguration _configuration = new GeneratorConfiguration();

        public GeneratorConfiguration Configuration => _configuration;

        public NamespaceProvider NamespaceProvider
        {
            get { return _configuration.NamespaceProvider; }
            set { _configuration.NamespaceProvider = value; }
        }

        public INamingProvider NamingProvider
        {
            get { return _configuration.NamingProvider; }
            set { _configuration.NamingProvider = value; }
        }

        public OutputWriter OutputWriter
        {
            get { return _configuration.OutputWriter; }
            set { _configuration.OutputWriter = value; }
        }

        public bool GenerateComplexTypesForCollections
        {
            get { return _configuration.GenerateComplexTypesForCollections; }
            set { _configuration.GenerateComplexTypesForCollections = value; }
        }

        public string NamespacePrefix
        {
            get { return _configuration.NamespacePrefix; }
            set { _configuration.NamespacePrefix = value; }
        }

        public string OutputFolder
        {
            get { return _configuration.OutputFolder; }
            set { _configuration.OutputFolder = value; }
        }

        public Action<string> Log
        {
            get { return _configuration.Log; }
            set { _configuration.Log = value; }
        }

        /// <summary>
        /// Enable data binding with INotifyPropertyChanged
        /// </summary>
        public bool EnableDataBinding
        {
            get { return _configuration.EnableDataBinding; }
            set { _configuration.EnableDataBinding = value; }
        }

        /// <summary>
        /// Use XElement instead of XmlElement for Any nodes?
        /// </summary>
        public bool UseXElementForAny
        {
            get { return _configuration.UseXElementForAny; }
            set { _configuration.UseXElementForAny = value; }
        }

        /// <summary>
        /// How are the names of the created properties changed?
        /// </summary>
        public NamingScheme NamingScheme
        {
            get { return _configuration.NamingScheme; }
            set { _configuration.NamingScheme = value; }
        }

        public bool AssemblyVisible
        {
            get { return _configuration.AssemblyVisible; }
            set { _configuration.AssemblyVisible = value; }
        }

        /// <summary>
        /// Emit the "Order" attribute value for XmlElementAttribute to ensure the correct order
        /// of the serialized XML elements.
        /// </summary>
        public bool EmitOrder
        {
            get { return _configuration.EmitOrder; }
            set { _configuration.EmitOrder = value; }
        }

        /// <summary>
        /// Determines the kind of annotations to emit
        /// </summary>
        public DataAnnotationMode DataAnnotationMode
        {
            get { return _configuration.DataAnnotationMode; }
            set { _configuration.DataAnnotationMode = value; }
        }

        public bool GenerateNullables
        {
            get { return _configuration.GenerateNullables; }
            set { _configuration.GenerateNullables = value; }
        }

        public bool UseShouldSerializePattern
        {
            get { return _configuration.UseShouldSerializePattern; }
            set { _configuration.UseShouldSerializePattern = value; }
        }

        public bool GenerateSerializableAttribute
        {
            get { return _configuration.GenerateSerializableAttribute; }
            set { _configuration.GenerateSerializableAttribute = value; }
        }

        public bool GenerateDebuggerStepThroughAttribute
        {
            get { return _configuration.GenerateDebuggerStepThroughAttribute; }
            set { _configuration.GenerateDebuggerStepThroughAttribute = value; }
        }

        public bool GenerateDesignerCategoryAttribute
        {
            get { return _configuration.GenerateDesignerCategoryAttribute; }
            set { _configuration.GenerateDesignerCategoryAttribute = value; }
        }

        public Type CollectionType
        {
            get { return _configuration.CollectionType; }
            set { _configuration.CollectionType = value; }
        }

        public Type CollectionImplementationType
        {
            get { return _configuration.CollectionImplementationType; }
            set { _configuration.CollectionImplementationType = value; }
        }

        public Type IntegerDataType
        {
            get { return _configuration.IntegerDataType; }
            set { _configuration.IntegerDataType = value; }
        }

        public bool UseIntegerDataTypeAsFallback
        {
            get { return _configuration.UseIntegerDataTypeAsFallback; }
            set { _configuration.UseIntegerDataTypeAsFallback = value; }
        }

        public bool EntityFramework
        {
            get { return _configuration.EntityFramework; }
            set { _configuration.EntityFramework = value; }
        }

        public bool GenerateInterfaces
        {
            get { return _configuration.GenerateInterfaces; }
            set { _configuration.GenerateInterfaces = value; }
        }

        public bool GenerateDescriptionAttribute
        {
            get { return _configuration.GenerateDescriptionAttribute; }
            set { _configuration.GenerateDescriptionAttribute = value; }
        }

        public CodeTypeReferenceOptions CodeTypeReferenceOptions
        {
            get { return _configuration.CodeTypeReferenceOptions; }
            set { _configuration.CodeTypeReferenceOptions = value; }
        }

        public CollectionSettersMode CollectionSettersMode
        {
            get { return _configuration.CollectionSettersMode; }
            set { _configuration.CollectionSettersMode = value; }
        }

        public string TextValuePropertyName
        {
            get { return _configuration.TextValuePropertyName; }
            set { _configuration.TextValuePropertyName = value; }
        }

        /// <summary>
        /// Optional delegate that is called for each generated type member
        /// </summary>
        public Action<CodeTypeMember, PropertyModel> MemberVisitor
        {
            get { return _configuration.MemberVisitor; }
            set { _configuration.MemberVisitor = value; }
        }

        /// <summary>
        /// Optional delegate that is called for each generated type (class, interface, enum)
        /// </summary>
        public Action<CodeTypeDeclaration, TypeModel> TypeVisitor
        {
            get { return _configuration.TypeVisitor; }
            set { _configuration.TypeVisitor = value; }
        }

        public VersionProvider Version
        {
            get { return _configuration.Version; }
            set { _configuration.Version = value; }
        }

        public bool DisableComments
        {
            get { return _configuration.DisableComments; }
            set { _configuration.DisableComments = value; }
        }

        public bool DoNotForceIsNullable
        {
            get { return _configuration.DoNotForceIsNullable; }
            set { _configuration.DoNotForceIsNullable = value; }
        }

        public string PrivateMemberPrefix
        {
            get { return _configuration.PrivateMemberPrefix; }
            set { _configuration.PrivateMemberPrefix = value; }
        }

        public bool EnableUpaCheck
        {
            get { return _configuration.EnableUpaCheck; }
            set { _configuration.EnableUpaCheck = value; }
        }

        public bool SeparateClasses
        {
            get { return _configuration.SeparateClasses; }
            set { _configuration.SeparateClasses = value; }
        }

        public bool SeparateSubstitutes
        {
            get { return _configuration.SeparateSubstitutes; }
            set { _configuration.SeparateSubstitutes = value; }
        }

        static Generator()
        {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        }

        public void Generate(IEnumerable<string> files)
        {
            var set = new XmlSchemaSet();
            var settings = new XmlReaderSettings { DtdProcessing = DtdProcessing.Ignore };
            var readers = files.Select(f => XmlReader.Create(f, settings));

            set.XmlResolver = new XmlUrlResolver();
            set.ValidationEventHandler += (s, e) =>
            {
                var ex = e.Exception as Exception;
                while (ex != null)
                {
                    Log?.Invoke(ex.Message);
                    ex = ex.InnerException;
                }
            };

            foreach (var reader in readers)
                set.Add(null, reader);

            Generate(set);
        }

        public void Generate(XmlSchemaSet set)
        {
            set.CompilationSettings.EnableUpaCheck = EnableUpaCheck;
            set.Compile();

            var m = new ModelBuilder(_configuration, set);
            var namespaces = m.GenerateCode();
            var writer = _configuration.OutputWriter ?? new FileOutputWriter(OutputFolder ?? ".") { Configuration = _configuration };

            foreach (var ns in namespaces)
            {
                if (Version != null)
                {
                    ns.Comments.Add(new CodeCommentStatement($"This code was generated by {Version.Title} version {Version.Version}."));
                }

                writer.Write(ns);
            }
        }
    }
}