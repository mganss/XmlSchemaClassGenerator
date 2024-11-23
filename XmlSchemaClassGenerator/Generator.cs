using System;
using System.CodeDom;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;
using System.Xml.Schema;

namespace XmlSchemaClassGenerator
{
    class NormalizingXmlResolver : XmlUrlResolver
    {
        // the Uri scheme to force on the resolved Uris
        // "none" - do not change Uri scheme
        // "same" - force the same Uri scheme as base Uri
        // any other string becomes the new Uri scheme of the baseUri
        private string forceUriScheme; 
        public NormalizingXmlResolver(string forceUriScheme) : base()
        {
            this.forceUriScheme=forceUriScheme;
        }
        public override Uri ResolveUri(Uri baseUri, string relativeUri)
        {
            Uri resolvedUri = base.ResolveUri(baseUri, relativeUri);
            var r=NormalizeUri(baseUri, resolvedUri);
            Console.WriteLine($"-- ResolveUri:from {baseUri} to {relativeUri}: {r}");
            return r; 
        }
        private Uri NormalizeUri(Uri baseUri, Uri resolvedUri )
        {
            var newScheme=forceUriScheme;
            switch (forceUriScheme)
            {
                case "none": return resolvedUri;
                case "same":
                {
                    newScheme=baseUri.Scheme;
                    break; 
                }
            }
            UriBuilder builder = new UriBuilder(resolvedUri) { Scheme = newScheme, Port=-1};
            return builder.Uri;
        }
    }
    
    public class Generator
    {
        private readonly GeneratorConfiguration _configuration = new();

        public GeneratorConfiguration Configuration => _configuration;

        public string ForceUriScheme
        {
            get { return _configuration.ForceUriScheme; }
            set { _configuration.ForceUriScheme=value;  }
        }
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

        public bool EnumAsString
        {
            get { return _configuration.EnumAsString; }
            set { _configuration.EnumAsString = value; }
        }

        public bool MergeRestrictionsWithBase
        {
            get { return _configuration.MergeRestrictionsWithBase; }
            set { _configuration.MergeRestrictionsWithBase = value; }
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

        public bool EnableNullableReferenceAttributes
        {
            get { return _configuration.EnableNullableReferenceAttributes; }
            set { _configuration.EnableNullableReferenceAttributes = value; }
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

        public bool DateTimeWithTimeZone
        {
            get { return _configuration.DateTimeWithTimeZone; }
            set { _configuration.DateTimeWithTimeZone = value; }
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

        public bool CompactTypeNames
        {
            get { return _configuration.CompactTypeNames; }
            set { _configuration.CompactTypeNames = value; }
        }

        public HashSet<string> CommentLanguages
        {
            get { return _configuration.CommentLanguages; }
        }

        public bool UniqueTypeNamesAcrossNamespaces
        {
            get { return _configuration.UniqueTypeNameAcrossNamespaces; }
            set { _configuration.UniqueTypeNameAcrossNamespaces = value; }
        }

        public bool CreateGeneratedCodeAttributeVersion
        {
            get { return _configuration.CreateGeneratedCodeAttributeVersion; }
            set { _configuration.CreateGeneratedCodeAttributeVersion = value; }
        }

        public bool NetCoreSpecificCode
        {
            get { return _configuration.NetCoreSpecificCode; }
            set { _configuration.NetCoreSpecificCode = value; }
        }

        public bool UseArrayItemAttribute
        {
            get { return _configuration.UseArrayItemAttribute; }
            set {  _configuration.UseArrayItemAttribute = value;}
        }

        public bool GenerateCommandLineArgumentsComment
        {
            get { return _configuration.GenerateCommandLineArgumentsComment; }
            set { _configuration.GenerateCommandLineArgumentsComment = value; }
        }

        public CommandLineArgumentsProvider CommandLineArgumentsProvider
        {
            get { return _configuration.CommandLineArgumentsProvider; }
            set { _configuration.CommandLineArgumentsProvider = value; }
        }

        public bool MapUnionToWidestCommonType
        {
            get { return _configuration.MapUnionToWidestCommonType; }
            set { _configuration.MapUnionToWidestCommonType = value; }
        }

        public bool SeparateNamespaceHierarchy
        {
            get { return _configuration.SeparateNamespaceHierarchy; }
            set { _configuration.SeparateNamespaceHierarchy = value; }
        }

        public bool SerializeEmptyCollections
        {
            get { return _configuration.SerializeEmptyCollections; }
            set { _configuration.SerializeEmptyCollections = value; }
        }

        public bool AllowDtdParse
        {
            get { return _configuration.AllowDtdParse; }
            set { _configuration.AllowDtdParse = value; }
        }

        public bool ValidationError { get; private set; }

        static Generator()
        {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        }

        public void Generate(IEnumerable<string> files)
        {
            var settings = CreateSettings();
            Generate(files.Select(f => XmlReader.Create(f, settings)));
        }

        public void Generate(IEnumerable<TextReader> streams)
        {
            var settings = CreateSettings();
            Generate(streams.Select(f => XmlReader.Create(f, settings)));
        }

        public void Generate(IEnumerable<XmlReader> readers)
        {
            var set = new XmlSchemaSet(); 
            ValidationError = false;

            set.XmlResolver = new NormalizingXmlResolver(ForceUriScheme); // XmlUrlResolver();
            set.ValidationEventHandler += (s, e) =>
            {
                var ex = e.Exception as Exception;
                while (ex != null)
                {
                    ValidationError = true;
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
                    var comment = new StringBuilder($"This code was generated by {Version.Title}");
                    if (CreateGeneratedCodeAttributeVersion)
                    {
                        comment.Append($" version {Version.Version}");
                    }
                    if (GenerateCommandLineArgumentsComment)
                    {
                        comment.Append(" using the following command:");
                    }
                    ns.Comments.Add(new CodeCommentStatement(comment.ToString()));
                    if (GenerateCommandLineArgumentsComment)
                    {
                        ns.Comments.Add(new CodeCommentStatement(CommandLineArgumentsProvider?.CommandLineArguments ?? "N/A"));
                    }
                }

                writer.Write(ns);
            }
        }

        private XmlReaderSettings CreateSettings()
        {
            return new XmlReaderSettings { DtdProcessing = AllowDtdParse ? DtdProcessing.Parse : DtdProcessing.Ignore };
        }
    }
}