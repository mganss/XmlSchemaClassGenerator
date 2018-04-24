using System;
using System.CodeDom;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;

namespace XmlSchemaClassGenerator
{
    public class NamespaceModel
    {
        public string Name { get; set; }
        public NamespaceKey Key { get; private set; }
        public Dictionary<string, TypeModel> Types { get; set; }
        /// <summary>
        /// Does the namespace of this type clashes with a class in the same or upper namespace?
        /// </summary>
        public bool IsAmbiguous { get; set; }
        public GeneratorConfiguration Configuration { get; private set; }

        public NamespaceModel(NamespaceKey key, GeneratorConfiguration configuration)
        {
            Configuration = configuration;
            Key = key;
            Types = new Dictionary<string, TypeModel>();
        }

        public static CodeNamespace Generate(string namespaceName, IEnumerable<NamespaceModel> parts)
        {
            var codeNamespace = new CodeNamespace(namespaceName);
            codeNamespace.Imports.Add(new CodeNamespaceImport("System.Collections"));
            codeNamespace.Imports.Add(new CodeNamespaceImport("System.Collections.Generic"));
            codeNamespace.Imports.Add(new CodeNamespaceImport("System.Collections.ObjectModel"));
            codeNamespace.Imports.Add(new CodeNamespaceImport("System.Xml.Serialization"));

            var typeModels = parts.SelectMany(x => x.Types.Values).ToList();
            if (typeModels.OfType<ClassModel>().Any(x => x.Configuration.EnableDataBinding))
            {
                codeNamespace.Imports.Add(new CodeNamespaceImport("System.Linq"));
                codeNamespace.Imports.Add(new CodeNamespaceImport("System.ComponentModel"));
            }

            foreach (var typeModel in typeModels)
            {
                var type = typeModel.Generate();
                if (type != null)
                {
                    codeNamespace.Types.Add(type);
                }
            }

            return codeNamespace;
        }
    }

    public class DocumentationModel
    {
        public string Language { get; set; }
        public string Text { get; set; }
        public static bool DisableComments { get; set; }

        public static IEnumerable<CodeCommentStatement> GetComments(IEnumerable<DocumentationModel> docs)
        {
            if (DisableComments)
                yield break;

            yield return new CodeCommentStatement("<summary>", true);

            foreach (var doc in docs.OrderBy(d => d.Language))
            {
                var text = doc.Text;
                var comment = string.Format(@"<para{0}>{1}</para>",
                    string.IsNullOrEmpty(doc.Language) ? "" : string.Format(@" xml:lang=""{0}""", doc.Language), CodeUtilities.NormalizeNewlines(text));
                yield return new CodeCommentStatement(comment, true);
            }

            yield return new CodeCommentStatement("</summary>", true);
        }
    }

    public abstract class TypeModel
    {
        protected static readonly CodeDomProvider CSharpProvider = CodeDomProvider.CreateProvider("CSharp");

        public NamespaceModel Namespace { get; set; }
        public XmlQualifiedName RootElementName { get; set; }
        public string Name { get; set; }
        public XmlQualifiedName XmlSchemaName { get; set; }
        public XmlSchemaType XmlSchemaType { get; set; }
        public List<DocumentationModel> Documentation { get; private set; }
        public bool IsAnonymous { get; set; }
        public GeneratorConfiguration Configuration { get; private set; }

        protected TypeModel(GeneratorConfiguration configuration)
        {
            Configuration = configuration;
            Documentation = new List<DocumentationModel>();
        }

        public virtual CodeTypeDeclaration Generate()
        {
            var typeDeclaration = new CodeTypeDeclaration { Name = Name };

            typeDeclaration.Comments.AddRange(DocumentationModel.GetComments(Documentation).ToArray());

            var title = ((AssemblyTitleAttribute)Attribute.GetCustomAttribute(Assembly.GetExecutingAssembly(),
                typeof(AssemblyTitleAttribute))).Title;
            var version = Assembly.GetExecutingAssembly().GetName().Version.ToString();

            var generatedAttribute = new CodeAttributeDeclaration(new CodeTypeReference(typeof(GeneratedCodeAttribute), Configuration.CodeTypeReferenceOptions),
                new CodeAttributeArgument(new CodePrimitiveExpression(title)),
                new CodeAttributeArgument(new CodePrimitiveExpression(version)));
            typeDeclaration.CustomAttributes.Add(generatedAttribute);

            return typeDeclaration;
        }

        protected void GenerateTypeAttribute(CodeTypeDeclaration typeDeclaration)
        {
            if (XmlSchemaName != null)
            {
                var typeAttribute = new CodeAttributeDeclaration(new CodeTypeReference(typeof(XmlTypeAttribute), Configuration.CodeTypeReferenceOptions),
                    new CodeAttributeArgument(new CodePrimitiveExpression(XmlSchemaName.Name)),
                    new CodeAttributeArgument("Namespace", new CodePrimitiveExpression(XmlSchemaName.Namespace)));
                if (IsAnonymous)
                {
                    // don't generate AnonymousType if it's derived class, otherwise XmlSerializer will
                    // complain with "InvalidOperationException: Cannot include anonymous type '...'"
                    var classModel = this as ClassModel;
                    if (classModel == null || classModel.BaseClass == null)
                    {
                        typeAttribute.Arguments.Add(new CodeAttributeArgument("AnonymousType", new CodePrimitiveExpression(true)));
                    }
                }
                typeDeclaration.CustomAttributes.Add(typeAttribute);
            }
        }

        protected void GenerateSerializableAttribute(CodeTypeDeclaration typeDeclaration)
        {
            if (Configuration.GenerateSerializableAttribute)
            {
                var serializableAttribute =
                    new CodeAttributeDeclaration(new CodeTypeReference(typeof(SerializableAttribute), Configuration.CodeTypeReferenceOptions));
                typeDeclaration.CustomAttributes.Add(serializableAttribute);
            }
        }

        public virtual CodeTypeReference GetReferenceFor(NamespaceModel referencingNamespace, bool collection, bool forInit = false)
        {
            var name = referencingNamespace == Namespace ? Name : string.Format("{2}{0}.{1}", Namespace.Name, Name, ((referencingNamespace ?? Namespace).IsAmbiguous ? "global::" : string.Empty));
            if (collection)
            {
                name = forInit ? SimpleModel.GetCollectionImplementationName(name, Configuration) : SimpleModel.GetCollectionDefinitionName(name, Configuration);
            }
            return new CodeTypeReference(name);
        }

        public virtual CodeExpression GetDefaultValueFor(string defaultString)
        {
            throw new NotSupportedException(string.Format("Getting default value for {0} not supported.", defaultString));
        }
    }

    public class InterfaceModel : TypeModel
    {
        public InterfaceModel(GeneratorConfiguration configuration)
            : base(configuration)
        {
            Properties = new List<PropertyModel>();
            Interfaces = new List<InterfaceModel>();
        }

        public List<PropertyModel> Properties { get; set; }
        public List<InterfaceModel> Interfaces { get; set; }

        public override CodeTypeDeclaration Generate()
        {
            var interfaceDeclaration = base.Generate();

            interfaceDeclaration.IsInterface = true;
            interfaceDeclaration.IsPartial = true;

            foreach (var property in Properties)
                property.AddInterfaceMembersTo(interfaceDeclaration);

            interfaceDeclaration.BaseTypes.AddRange(Interfaces.Select(i => i.GetReferenceFor(Namespace, false)).ToArray());

            return interfaceDeclaration;
        }
    }

    public class ClassModel : TypeModel
    {
        public bool IsAbstract { get; set; }
        public bool IsMixed { get; set; }
        public bool IsSubstitution { get; set; }
        public TypeModel BaseClass { get; set; }
        public List<PropertyModel> Properties { get; set; }
        public List<InterfaceModel> Interfaces { get; set; }
        public List<ClassModel> DerivedTypes { get; set; }
        public ClassModel(GeneratorConfiguration configuration)
            : base(configuration)
        {
            Properties = new List<PropertyModel>();
            Interfaces = new List<InterfaceModel>();
            DerivedTypes = new List<ClassModel>();
        }

        public IEnumerable<ClassModel> AllBaseClasses
        {
            get
            {
                var baseClass = BaseClass as ClassModel;
                while (baseClass != null)
                {
                    yield return baseClass;
                    baseClass = baseClass.BaseClass as ClassModel;
                }
            }
        }

        public override CodeTypeDeclaration Generate()
        {
            var classDeclaration = base.Generate();

            GenerateSerializableAttribute(classDeclaration);
            GenerateTypeAttribute(classDeclaration);

            classDeclaration.IsClass = true;
            classDeclaration.IsPartial = true;

            if (Configuration.EnableDataBinding)
            {
                classDeclaration.Members.Add(new CodeMemberEvent()
                {
                    Name = "PropertyChanged",
                    Type = new CodeTypeReference(typeof(PropertyChangedEventHandler), Configuration.CodeTypeReferenceOptions),
                    Attributes = MemberAttributes.Public,
                });

                var onPropChangedMethod = new CodeMemberMethod
                {
                    Name = "OnPropertyChanged",
                    Attributes = MemberAttributes.Family,
                };
                var param = new CodeParameterDeclarationExpression(typeof(string), "propertyName");
                onPropChangedMethod.Parameters.Add(param);
                onPropChangedMethod.Statements.Add(
                    new CodeConditionStatement(
                        new CodeBinaryOperatorExpression(
                            new CodeEventReferenceExpression(null, "PropertyChanged"),
                            CodeBinaryOperatorType.IdentityInequality,
                            new CodePrimitiveExpression(null)),
                        new CodeExpressionStatement(new CodeDelegateInvokeExpression(
                            new CodeEventReferenceExpression(null, "PropertyChanged"),
                            new CodeThisReferenceExpression(),
                            new CodeObjectCreateExpression(
                                "PropertyChangedEventArgs",
                                new CodeArgumentReferenceExpression("propertyName"))
                            ))));
                classDeclaration.Members.Add(onPropChangedMethod);
            }

            if (BaseClass != null)
            {
                if (BaseClass is ClassModel)
                {
                    classDeclaration.BaseTypes.Add(BaseClass.GetReferenceFor(Namespace, false));
                }
                else if (!string.IsNullOrEmpty(Configuration.TextValuePropertyName))
                {
                    var typeReference = BaseClass.GetReferenceFor(Namespace, false);

                    var member = new CodeMemberField(typeReference, Configuration.TextValuePropertyName)
                    {
                        Attributes = MemberAttributes.Public,
                    };

                    if (Configuration.EnableDataBinding)
                    {
                        var backingFieldMember = new CodeMemberField(typeReference, member.Name.ToBackingField(Configuration.DoNotUseUnderscoreInPrivateMemberNames))
                        {
                            Attributes = MemberAttributes.Private
                        };
                        member.Name += PropertyModel.GetAccessors(member.Name, backingFieldMember.Name, BaseClass.GetPropertyValueTypeCode(), false);
                        classDeclaration.Members.Add(backingFieldMember);
                    }
                    else
                    {
                        // hack to generate automatic property
                        member.Name += " { get; set; }";
                    }

                    var docs = new[] { new DocumentationModel { Language = "en", Text = "Gets or sets the text value." },
                        new DocumentationModel { Language = "de", Text = "Ruft den Text ab oder legt diesen fest." } };
                    member.Comments.AddRange(DocumentationModel.GetComments(docs).ToArray());

                    var attribute = new CodeAttributeDeclaration(new CodeTypeReference(typeof(XmlTextAttribute), Configuration.CodeTypeReferenceOptions));
                    if (BaseClass is SimpleModel simpleModel && (simpleModel.XmlSchemaType.IsDataTypeAttributeAllowed(Configuration) ?? simpleModel.UseDataTypeAttribute))
                    {
                        var name = BaseClass.GetQualifiedName();
                        if (name.Namespace == XmlSchema.Namespace)
                        {
                            var dataType = new CodeAttributeArgument("DataType", new CodePrimitiveExpression(name.Name));
                            attribute.Arguments.Add(dataType);
                        }
                    }

                    member.CustomAttributes.Add(attribute);
                    classDeclaration.Members.Add(member);
                }
            }

            if (Configuration.EnableDataBinding)
            {
                classDeclaration.BaseTypes.Add(new CodeTypeReference(typeof(INotifyPropertyChanged), Configuration.CodeTypeReferenceOptions));
            }

            if (Configuration.EntityFramework && (BaseClass == null || !(BaseClass is ClassModel)))
            {
                // generate key
                var keyProperty = Properties.FirstOrDefault(p => p.Name.ToLowerInvariant() == "id")
                    ?? Properties.FirstOrDefault(p => p.Name.ToLowerInvariant() == (Name.ToLowerInvariant() + "id"));

                if (keyProperty == null)
                {
                    keyProperty = new PropertyModel(Configuration)
                    {
                        Name = "Id",
                        Type = new SimpleModel(Configuration) { ValueType = typeof(long) },
                        OwningType = this,
                        Documentation = { new DocumentationModel {  Language = "en", Text = "Gets or sets a value uniquely identifying this entity." },
                            new DocumentationModel { Language = "de", Text = "Ruft einen Wert ab, der diese Entität eindeutig identifiziert, oder legt diesen fest." } }
                    };
                    Properties.Insert(0, keyProperty);
                }

                keyProperty.IsKey = true;
            }

            foreach (var property in Properties)
                property.AddMembersTo(classDeclaration, Configuration.EnableDataBinding);

            if (IsMixed && (BaseClass == null || (BaseClass is ClassModel && !AllBaseClasses.Any(b => b.IsMixed))))
            {
                var text = new CodeMemberField(typeof(string), "Text");
                // hack to generate automatic property
                text.Name += " { get; set; }";
                text.Attributes = MemberAttributes.Public;
                var xmlTextAttribute = new CodeAttributeDeclaration(new CodeTypeReference(typeof(XmlTextAttribute), Configuration.CodeTypeReferenceOptions));
                text.CustomAttributes.Add(xmlTextAttribute);
                classDeclaration.Members.Add(text);
            }

            if (Configuration.GenerateDebuggerStepThroughAttribute)
                classDeclaration.CustomAttributes.Add(
                    new CodeAttributeDeclaration(new CodeTypeReference(typeof(DebuggerStepThroughAttribute), Configuration.CodeTypeReferenceOptions)));

            if (Configuration.GenerateDesignerCategoryAttribute)
            {
                classDeclaration.CustomAttributes.Add(
                    new CodeAttributeDeclaration(new CodeTypeReference(typeof(DesignerCategoryAttribute), Configuration.CodeTypeReferenceOptions),
                        new CodeAttributeArgument(new CodePrimitiveExpression("code"))));
            }

            if (RootElementName != null)
            {
                var rootAttribute = new CodeAttributeDeclaration(new CodeTypeReference(typeof(XmlRootAttribute), Configuration.CodeTypeReferenceOptions),
                    new CodeAttributeArgument(new CodePrimitiveExpression(RootElementName.Name)),
                    new CodeAttributeArgument("Namespace", new CodePrimitiveExpression(RootElementName.Namespace)));
                classDeclaration.CustomAttributes.Add(rootAttribute);
            }

            var derivedTypes = GetAllDerivedTypes();
            foreach (var derivedType in derivedTypes.OrderBy(t => t.Name))
            {
                var includeAttribute = new CodeAttributeDeclaration(new CodeTypeReference(typeof(XmlIncludeAttribute), Configuration.CodeTypeReferenceOptions),
                    new CodeAttributeArgument(new CodeTypeOfExpression(derivedType.GetReferenceFor(Namespace, false))));
                classDeclaration.CustomAttributes.Add(includeAttribute);
            }

            classDeclaration.BaseTypes.AddRange(Interfaces.Select(i => i.GetReferenceFor(Namespace, false)).ToArray());

            return classDeclaration;
        }

        public List<ClassModel> GetAllDerivedTypes()
        {
            var allDerivedTypes = new List<ClassModel>(DerivedTypes);

            foreach (var derivedType in DerivedTypes)
                allDerivedTypes.AddRange(derivedType.GetAllDerivedTypes());

            return allDerivedTypes;
        }

        public override CodeExpression GetDefaultValueFor(string defaultString)
        {
            if (BaseClass is SimpleModel)
            {
                string reference, val;

                using (var writer = new System.IO.StringWriter())
                {
                    CSharpProvider.GenerateCodeFromExpression(BaseClass.GetDefaultValueFor(defaultString), writer, new CodeGeneratorOptions());
                    val = writer.ToString();
                }

                using (var writer = new System.IO.StringWriter())
                {
                    CSharpProvider.GenerateCodeFromExpression(new CodeTypeReferenceExpression(GetReferenceFor(null, false)), writer, new CodeGeneratorOptions());
                    reference = writer.ToString();
                }

                var dv = new CodeSnippetExpression($"new { reference } {{ { Configuration.TextValuePropertyName } = { val } }};");
                return dv;
            }

            return base.GetDefaultValueFor(defaultString);
        }
    }

    public class PropertyModel
    {
        public TypeModel OwningType { get; set; }
        public string Name { get; set; }
        public bool IsAttribute { get; set; }
        public TypeModel Type { get; set; }
        public bool IsNullable { get; set; }
        public bool IsNillable { get; set; }
        public bool IsCollection { get; set; }
        public string DefaultValue { get; set; }
        public XmlSchemaForm Form { get; set; }
        public string XmlNamespace { get; set; }
        public List<DocumentationModel> Documentation { get; private set; }
        public bool IsDeprecated { get; set; }
        public XmlQualifiedName XmlSchemaName { get; set; }
        public bool IsAny { get; set; }
        public int? Order { get; set; }
        public bool IsKey { get; set; }
        public GeneratorConfiguration Configuration { get; private set; }

        public PropertyModel(GeneratorConfiguration configuration)
        {
            Configuration = configuration;
            Documentation = new List<DocumentationModel>();
        }

        internal static string GetAccessors(string memberName, string backingFieldName, PropertyValueTypeCode typeCode, bool privateSetter, bool withDataBinding = true)
        {
            if (withDataBinding)
            {
                switch (typeCode)
                {
                    case PropertyValueTypeCode.ValueType:
                        return CodeUtilities.NormalizeNewlines(string.Format(@"
        {{
            get
            {{
                return {0};
            }}
            {2}set
            {{
                if (!{0}.Equals(value))
                {{
                    {0} = value;
                    OnPropertyChanged(""{1}"");
                }}
            }}
        }}", backingFieldName, memberName, (privateSetter ? "private " : string.Empty)));
                    case PropertyValueTypeCode.Other:
                        return CodeUtilities.NormalizeNewlines(string.Format(@"
        {{
            get
            {{
                return {0};
            }}
            {2}set
            {{
                if ({0} == value)
                    return;
                if ({0} == null || value == null || !{0}.Equals(value))
                {{
                    {0} = value;
                    OnPropertyChanged(""{1}"");
                }}
            }}
        }}", backingFieldName, memberName, (privateSetter ? "private " : string.Empty)));
                    case PropertyValueTypeCode.Array:
                        return CodeUtilities.NormalizeNewlines(string.Format(@"
        {{
            get
            {{
                return {0};
            }}
            {2}set
            {{
                if ({0} == value)
                    return;
                if ({0} == null || value == null || !{0}.SequenceEqual(value))
                {{
                    {0} = value;
                    OnPropertyChanged(""{1}"");
                }}
            }}
        }}", backingFieldName, memberName, (privateSetter ? "private " : string.Empty)));
                }
            }

            if (privateSetter)
            {
                return CodeUtilities.NormalizeNewlines(string.Format(@"
        {{
            get
            {{
                return this.{0};
            }}
            private set
            {{
                this.{0} = value;
            }}
        }}", backingFieldName));
            }
            else
            {
                return CodeUtilities.NormalizeNewlines(string.Format(@"
        {{
            get
            {{
                return this.{0};
            }}
            set
            {{
                this.{0} = value;
            }}
        }}", backingFieldName));
            }
        }

        private ClassModel TypeClassModel
        {
            get { return Type as ClassModel; }
        }

        private bool IsArray
        {
            get
            {
                return !IsCollection && !IsAttribute && TypeClassModel != null
                && TypeClassModel.BaseClass == null
                && TypeClassModel.Properties.Count == 1
                && !TypeClassModel.Properties[0].IsAttribute && !TypeClassModel.Properties[0].IsAny
                && TypeClassModel.Properties[0].IsCollection;
            }
        }

        private TypeModel PropertyType
        {
            get { return !IsArray ? Type : TypeClassModel.Properties[0].Type; }
        }

        private bool IsNullableValueType
        {
            get
            {
                return DefaultValue == null
                    && IsNullable && !(IsCollection || IsArray)
                    && ((PropertyType is EnumModel) || (PropertyType is SimpleModel && ((SimpleModel)PropertyType).ValueType.IsValueType));
            }
        }

        private bool IsNillableValueType
        {
            get
            {
                return IsNillable
                    && !IsNullableValueType
                    && !(IsCollection || IsArray)
                    && ((PropertyType is EnumModel) || (PropertyType is SimpleModel && ((SimpleModel)PropertyType).ValueType.IsValueType));
            }
        }

        private CodeTypeReference TypeReference
        {
            get { return PropertyType.GetReferenceFor(OwningType.Namespace, IsCollection || IsArray); }
        }

        private void AddDocs(CodeTypeMember member)
        {
            var docs = new List<DocumentationModel>(Documentation);

            if (PropertyType is SimpleModel simpleType)
            {
                docs.AddRange(simpleType.Documentation);
                docs.AddRange(simpleType.Restrictions.Select(r => new DocumentationModel { Language = "en", Text = r.Description }));
                member.CustomAttributes.AddRange(simpleType.GetRestrictionAttributes().ToArray());
            }

            member.Comments.AddRange(DocumentationModel.GetComments(docs).ToArray());
        }

        public void AddInterfaceMembersTo(CodeTypeDeclaration typeDeclaration)
        {
            CodeTypeMember member;

            var typeClassModel = TypeClassModel;
            var isArray = IsArray;
            var propertyType = PropertyType;
            var isNullableValueType = IsNullableValueType;
            var typeReference = TypeReference;
            var simpleType = propertyType as SimpleModel;

            if (isNullableValueType && Configuration.GenerateNullables)
            {
                var nullableType = new CodeTypeReference(typeof(Nullable<>), Configuration.CodeTypeReferenceOptions);
                nullableType.TypeArguments.Add(typeReference);
                typeReference = nullableType;
            }

            member = new CodeMemberProperty
            {
                Name = Name,
                Type = typeReference,
                HasGet = true,
                HasSet = !IsCollection && !isArray
            };

            if (DefaultValue != null && IsNullable)
            {
                var defaultValueExpression = propertyType.GetDefaultValueFor(DefaultValue);

                if ((defaultValueExpression is CodePrimitiveExpression) || (defaultValueExpression is CodeFieldReferenceExpression))
                {
                    var defaultValueAttribute = new CodeAttributeDeclaration(new CodeTypeReference(typeof(DefaultValueAttribute), Configuration.CodeTypeReferenceOptions),
                        new CodeAttributeArgument(defaultValueExpression));
                    member.CustomAttributes.Add(defaultValueAttribute);
                }
            }

            typeDeclaration.Members.Add(member);

            AddDocs(member);
        }

        // ReSharper disable once FunctionComplexityOverflow
        public void AddMembersTo(CodeTypeDeclaration typeDeclaration, bool withDataBinding)
        {
            CodeTypeMember member;

            var typeClassModel = TypeClassModel;
            var isArray = IsArray;
            var propertyType = PropertyType;
            var isNullableValueType = IsNullableValueType;
            var typeReference = TypeReference;
            var simpleType = propertyType as SimpleModel;

            var requiresBackingField = withDataBinding || DefaultValue != null || IsCollection || isArray;
            var backingField = new CodeMemberField(typeReference, OwningType.GetUniqueFieldName(this))
            {
                Attributes = MemberAttributes.Private
            };
            var ignoreAttribute = new CodeAttributeDeclaration(new CodeTypeReference(typeof(XmlIgnoreAttribute), Configuration.CodeTypeReferenceOptions));
            var notMappedAttribute = new CodeAttributeDeclaration(new CodeTypeReference(typeof(NotMappedAttribute), Configuration.CodeTypeReferenceOptions));
            backingField.CustomAttributes.Add(ignoreAttribute);

            if (requiresBackingField)
            {
                typeDeclaration.Members.Add(backingField);
            }

            if (DefaultValue == null)
            {
                var propertyName = isNullableValueType && Configuration.GenerateNullables ? Name + "Value" : Name;

                if (IsNillableValueType)
                {
                    var nullableType = new CodeTypeReference(typeof(Nullable<>), Configuration.CodeTypeReferenceOptions);
                    nullableType.TypeArguments.Add(typeReference);
                    member = new CodeMemberField(nullableType, propertyName);
                }
                else
                    member = new CodeMemberField(typeReference, propertyName);

                var isPrivateSetter = IsCollection || isArray;

                if (requiresBackingField)
                {
                    member.Name += GetAccessors(member.Name, backingField.Name,
                        IsCollection || isArray ? PropertyValueTypeCode.Array : propertyType.GetPropertyValueTypeCode(),
                        isPrivateSetter, withDataBinding);
                }
                else
                {
                    // hack to generate automatic property
                    member.Name += isPrivateSetter ? " { get; private set; }" : " { get; set; }";
                }
            }
            else
            {
                var defaultValueExpression = propertyType.GetDefaultValueFor(DefaultValue);
                backingField.InitExpression = defaultValueExpression;

                if (IsNillableValueType)
                {
                    var nullableType = new CodeTypeReference(typeof(Nullable<>), Configuration.CodeTypeReferenceOptions);
                    nullableType.TypeArguments.Add(typeReference);
                    member = new CodeMemberField(nullableType, Name);
                }
                else
                    member = new CodeMemberField(typeReference, Name);

                member.Name += GetAccessors(member.Name, backingField.Name, propertyType.GetPropertyValueTypeCode(), false, withDataBinding);

                if (IsNullable)
                {
                    if ((defaultValueExpression is CodePrimitiveExpression) || (defaultValueExpression is CodeFieldReferenceExpression))
                    {
                        var defaultValueAttribute = new CodeAttributeDeclaration(new CodeTypeReference(typeof(DefaultValueAttribute), Configuration.CodeTypeReferenceOptions),
                            new CodeAttributeArgument(defaultValueExpression));
                        member.CustomAttributes.Add(defaultValueAttribute);
                    }
                }
            }

            member.Attributes = MemberAttributes.Public;
            typeDeclaration.Members.Add(member);

            AddDocs(member);

            if (IsDeprecated)
            {
                // From .NET 3.5 XmlSerializer doesn't serialize objects with [Obsolete] >(
                //var deprecatedAttribute = new CodeAttributeDeclaration(new CodeTypeReference(typeof(ObsoleteAttribute)));
                //member.CustomAttributes.Add(deprecatedAttribute);
            }

            if (isNullableValueType)
            {
                var specifiedName = Configuration.GenerateNullables ? Name + "Value" : Name;
                var specifiedMember = new CodeMemberField(typeof(bool), specifiedName + "Specified { get; set; }");
                specifiedMember.CustomAttributes.Add(ignoreAttribute);
                if (Configuration.EntityFramework && Configuration.GenerateNullables) { specifiedMember.CustomAttributes.Add(notMappedAttribute); }
                specifiedMember.Attributes = MemberAttributes.Public;
                var specifiedDocs = new[] { new DocumentationModel { Language = "en", Text = string.Format("Gets or sets a value indicating whether the {0} property is specified.", Name) },
                    new DocumentationModel { Language = "de", Text = string.Format("Ruft einen Wert ab, der angibt, ob die {0}-Eigenschaft spezifiziert ist, oder legt diesen fest.", Name) } };
                specifiedMember.Comments.AddRange(DocumentationModel.GetComments(specifiedDocs).ToArray());
                typeDeclaration.Members.Add(specifiedMember);

                var specifiedMemberPropertyModel = new PropertyModel(Configuration)
                {
                    Name = specifiedName + "Specified"
                };

                Configuration.MemberVisitor(specifiedMember, specifiedMemberPropertyModel);

                if (Configuration.GenerateNullables)
                {
                    // public X? Name
                    // {
                    //      get { return NameSpecified ? NameValue : null; }
                    //      set
                    //      {
                    //          NameValue = value.GetValueOrDefault();
                    //          NameSpecified = value.HasValue;
                    //      }
                    // }

                    var nullableType = new CodeTypeReference(typeof(Nullable<>), Configuration.CodeTypeReferenceOptions);
                    nullableType.TypeArguments.Add(typeReference);
                    var nullableMember = new CodeMemberProperty
                    {
                        Type = nullableType,
                        Name = Name,
                        HasSet = true,
                        HasGet = true,
                        Attributes = MemberAttributes.Public | MemberAttributes.Final,
                    };
                    nullableMember.CustomAttributes.Add(ignoreAttribute);
                    nullableMember.Comments.AddRange(member.Comments);

                    var specifiedExpression = new CodePropertyReferenceExpression(new CodeThisReferenceExpression(), specifiedName + "Specified");
                    var valueExpression = new CodePropertyReferenceExpression(new CodeThisReferenceExpression(), Name + "Value");
                    var conditionStatement = new CodeConditionStatement(specifiedExpression,
                        new CodeStatement[] { new CodeMethodReturnStatement(valueExpression) },
                        new CodeStatement[] { new CodeMethodReturnStatement(new CodePrimitiveExpression(null)) });
                    nullableMember.GetStatements.Add(conditionStatement);

                    var getValueOrDefaultExpression = new CodeMethodInvokeExpression(new CodePropertySetValueReferenceExpression(), "GetValueOrDefault");
                    var setValueStatement = new CodeAssignStatement(valueExpression, getValueOrDefaultExpression);
                    var hasValueExpression = new CodePropertyReferenceExpression(new CodePropertySetValueReferenceExpression(), "HasValue");
                    var setSpecifiedStatement = new CodeAssignStatement(specifiedExpression, hasValueExpression);

                    var statements = new List<CodeStatement>();
                    if (withDataBinding)
                    {
                        var ifNotEquals = new CodeConditionStatement(
                            new CodeBinaryOperatorExpression(
                                new CodeMethodInvokeExpression(valueExpression, "Equals", getValueOrDefaultExpression),
                                CodeBinaryOperatorType.ValueEquality,
                                new CodePrimitiveExpression(false)
                                ),
                            setValueStatement,
                            setSpecifiedStatement,
                            new CodeExpressionStatement(new CodeMethodInvokeExpression(null, "OnPropertyChanged",
                                new CodePrimitiveExpression(Name)))
                            );
                        statements.Add(ifNotEquals);
                    }
                    else
                    {
                        statements.Add(setValueStatement);
                        statements.Add(setSpecifiedStatement);
                    }

                    nullableMember.SetStatements.AddRange(statements.ToArray());

                    typeDeclaration.Members.Add(nullableMember);

                    var editorBrowsableAttribute = new CodeAttributeDeclaration(new CodeTypeReference(typeof(EditorBrowsableAttribute), Configuration.CodeTypeReferenceOptions));
                    editorBrowsableAttribute.Arguments.Add(new CodeAttributeArgument(new CodeFieldReferenceExpression(new CodeTypeReferenceExpression(typeof(EditorBrowsableState)), "Never")));
                    specifiedMember.CustomAttributes.Add(editorBrowsableAttribute);
                    member.CustomAttributes.Add(editorBrowsableAttribute);
                    if (Configuration.EntityFramework) { member.CustomAttributes.Add(notMappedAttribute); }
                }
            }
            else if ((IsCollection || isArray) && IsNullable && !IsAttribute)
            {
                var specifiedProperty = new CodeMemberProperty
                {
                    Type = new CodeTypeReference(typeof(bool)),
                    Name = Name + "Specified",
                    HasSet = false,
                    HasGet = true,
                };
                specifiedProperty.CustomAttributes.Add(ignoreAttribute);
                if (Configuration.EntityFramework) { specifiedProperty.CustomAttributes.Add(notMappedAttribute); }
                specifiedProperty.Attributes = MemberAttributes.Public | MemberAttributes.Final;

                var listReference = new CodePropertyReferenceExpression(new CodeThisReferenceExpression(), Name);
                var countReference = new CodePropertyReferenceExpression(listReference, "Count");
                var notZeroExpression = new CodeBinaryOperatorExpression(countReference, CodeBinaryOperatorType.IdentityInequality, new CodePrimitiveExpression(0));
                var returnStatement = new CodeMethodReturnStatement(notZeroExpression);
                specifiedProperty.GetStatements.Add(returnStatement);

                var specifiedDocs = new[] { new DocumentationModel { Language = "en", Text = string.Format("Gets a value indicating whether the {0} collection is empty.", Name) },
                    new DocumentationModel { Language = "de", Text = string.Format("Ruft einen Wert ab, der angibt, ob die {0}-Collection leer ist.", Name) } };
                specifiedProperty.Comments.AddRange(DocumentationModel.GetComments(specifiedDocs).ToArray());

                Configuration.MemberVisitor(specifiedProperty, this);

                typeDeclaration.Members.Add(specifiedProperty);
            }

            var attributes = GetAttributes(isArray).ToArray();
            member.CustomAttributes.AddRange(attributes);

            // initialize List<>
            if (IsCollection || isArray)
            {
                var constructor = typeDeclaration.Members.OfType<CodeConstructor>().FirstOrDefault();
                if (constructor == null)
                {
                    constructor = new CodeConstructor { Attributes = MemberAttributes.Public | MemberAttributes.Final };
                    var constructorDocs = new[] { new DocumentationModel { Language = "en", Text = string.Format(@"Initializes a new instance of the <see cref=""{0}"" /> class.", typeDeclaration.Name) },
                        new DocumentationModel { Language = "de", Text = string.Format(@"Initialisiert eine neue Instanz der <see cref=""{0}"" /> Klasse.", typeDeclaration.Name) } };
                    constructor.Comments.AddRange(DocumentationModel.GetComments(constructorDocs).ToArray());
                    typeDeclaration.Members.Add(constructor);
                }
                var listReference = requiresBackingField ? (CodeExpression)new CodeFieldReferenceExpression(new CodeThisReferenceExpression(), backingField.Name) :
                    new CodePropertyReferenceExpression(new CodeThisReferenceExpression(), Name);
                var initTypeReference = propertyType.GetReferenceFor(OwningType.Namespace, true, true);
                var initExpression = new CodeObjectCreateExpression(initTypeReference);
                constructor.Statements.Add(new CodeAssignStatement(listReference, initExpression));
            }

            if (isArray)
            {
                var arrayItemProperty = typeClassModel.Properties[0];
                var propertyAttributes = arrayItemProperty.GetAttributes(false).ToList();
                // HACK: repackage as ArrayItemAttribute
                foreach (var propertyAttribute in propertyAttributes)
                {
                    var arrayItemAttribute = new CodeAttributeDeclaration(new CodeTypeReference(typeof(XmlArrayItemAttribute), Configuration.CodeTypeReferenceOptions),
                        propertyAttribute.Arguments.Cast<CodeAttributeArgument>().Where(x => !string.Equals(x.Name, "Order", StringComparison.Ordinal)).ToArray());
                    member.CustomAttributes.Add(arrayItemAttribute);
                }
            }

            if (IsKey)
            {
                var keyAttribute = new CodeAttributeDeclaration(new CodeTypeReference(typeof(KeyAttribute), Configuration.CodeTypeReferenceOptions));
                member.CustomAttributes.Add(keyAttribute);
            }

            if (IsAny && Configuration.EntityFramework)
            {
                member.CustomAttributes.Add(notMappedAttribute);
            }

            Configuration.MemberVisitor(member, this);
        }

        private IEnumerable<CodeAttributeDeclaration> GetAttributes(bool isArray)
        {
            var attributes = new List<CodeAttributeDeclaration>();

            if (IsKey && XmlSchemaName == null)
            {
                attributes.Add(new CodeAttributeDeclaration(new CodeTypeReference(typeof(XmlIgnoreAttribute), Configuration.CodeTypeReferenceOptions)));
                return attributes;
            }

            if (IsAttribute)
            {
                if (IsAny)
                {
                    var anyAttribute = new CodeAttributeDeclaration(new CodeTypeReference(typeof(XmlAnyAttributeAttribute), Configuration.CodeTypeReferenceOptions));
                    if (Order != null)
                        anyAttribute.Arguments.Add(new CodeAttributeArgument("Order", new CodePrimitiveExpression(Order.Value)));
                    attributes.Add(anyAttribute);
                }
                else
                {
                    attributes.Add(new CodeAttributeDeclaration(new CodeTypeReference(typeof(XmlAttributeAttribute), Configuration.CodeTypeReferenceOptions),
                        new CodeAttributeArgument(new CodePrimitiveExpression(XmlSchemaName.Name))));
                }
            }
            else if (!isArray)
            {
                if (IsAny)
                {
                    var anyAttribute = new CodeAttributeDeclaration(new CodeTypeReference(typeof(XmlAnyElementAttribute), Configuration.CodeTypeReferenceOptions));
                    if (Order != null)
                        anyAttribute.Arguments.Add(new CodeAttributeArgument("Order", new CodePrimitiveExpression(Order.Value)));
                    attributes.Add(anyAttribute);
                }
                else
                {
                    if (PropertyType is ClassModel classType && classType.IsAbstract && classType.DerivedTypes.Any())
                    {
                        var derivedTypes = classType.GetAllDerivedTypes().Where(t => t.IsSubstitution);
                        foreach (var derivedType in derivedTypes)
                        {
                            var derivedAttribute = new CodeAttributeDeclaration(new CodeTypeReference(typeof(XmlElementAttribute), Configuration.CodeTypeReferenceOptions),
                                new CodeAttributeArgument(new CodePrimitiveExpression(derivedType.XmlSchemaName.Name)),
                                new CodeAttributeArgument("Type", new CodeTypeOfExpression(derivedType.GetReferenceFor(OwningType.Namespace, false))));
                            if (Order != null)
                            {
                                derivedAttribute.Arguments.Add(new CodeAttributeArgument("Order",
                                    new CodePrimitiveExpression(Order.Value)));
                            }
                            attributes.Add(derivedAttribute);
                        }
                    }

                    var attribute = new CodeAttributeDeclaration(new CodeTypeReference(typeof(XmlElementAttribute), Configuration.CodeTypeReferenceOptions),
                            new CodeAttributeArgument(new CodePrimitiveExpression(XmlSchemaName.Name)));
                    if (Order != null)
                    {
                        attribute.Arguments.Add(new CodeAttributeArgument("Order",
                            new CodePrimitiveExpression(Order.Value)));
                    }
                    attributes.Add(attribute);
                }
            }
            else
            {
                attributes.Add(new CodeAttributeDeclaration(new CodeTypeReference(typeof(XmlArrayAttribute), Configuration.CodeTypeReferenceOptions),
                    new CodeAttributeArgument(new CodePrimitiveExpression(XmlSchemaName.Name))));
            }

            foreach (var attribute in attributes)
            {
                if (XmlNamespace != null)
                {
                    attribute.Arguments.Add(new CodeAttributeArgument("Namespace", new CodePrimitiveExpression(XmlNamespace)));
                }
                else if (Form == XmlSchemaForm.Qualified)
                {
                    attribute.Arguments.Add(new CodeAttributeArgument("Namespace", new CodePrimitiveExpression(OwningType.XmlSchemaName.Namespace)));
                }
                else if (!IsAny)
                {
                    attribute.Arguments.Add(new CodeAttributeArgument("Form",
                        new CodeFieldReferenceExpression(new CodeTypeReferenceExpression(new CodeTypeReference(typeof(XmlSchemaForm), Configuration.CodeTypeReferenceOptions)),
                            "Unqualified")));
                }

                if (IsNillable)
                {
                    attribute.Arguments.Add(new CodeAttributeArgument("IsNullable", new CodePrimitiveExpression(true)));
                }

                if (Type is SimpleModel simpleModel && simpleModel.UseDataTypeAttribute)
                {
                    // walk up the inheritance chain to find DataType if the simple type is derived (see #18)
                    var xmlSchemaType = Type.XmlSchemaType;
                    while (xmlSchemaType != null)
                    {
                        var name = xmlSchemaType.GetQualifiedName();
                        if (name.Namespace == XmlSchema.Namespace && name.Name != "anySimpleType")
                        {
                            var dataType = new CodeAttributeArgument("DataType", new CodePrimitiveExpression(name.Name));
                            attribute.Arguments.Add(dataType);
                            break;
                        }
                        else
                            xmlSchemaType = xmlSchemaType.BaseXmlSchemaType;
                    }
                }
            }

            return attributes;
        }
    }

    public class EnumValueModel
    {
        public string Name { get; set; }
        public string Value { get; set; }
        public bool IsDeprecated { get; set; }
        public List<DocumentationModel> Documentation { get; private set; }

        public EnumValueModel()
        {
            Documentation = new List<DocumentationModel>();
        }
    }

    public class EnumModel : TypeModel
    {
        public List<EnumValueModel> Values { get; set; }

        public EnumModel(GeneratorConfiguration configuration)
            : base(configuration)
        {
            Values = new List<EnumValueModel>();
        }

        public override CodeTypeDeclaration Generate()
        {
            var enumDeclaration = base.Generate();

            GenerateSerializableAttribute(enumDeclaration);
            GenerateTypeAttribute(enumDeclaration);

            enumDeclaration.IsEnum = true;

            foreach (var val in Values)
            {
                var member = new CodeMemberField { Name = val.Name };
                var docs = new List<DocumentationModel>(val.Documentation);

                if (val.Name != val.Value) // illegal identifier chars in value
                {
                    var enumAttribute = new CodeAttributeDeclaration(new CodeTypeReference(typeof(XmlEnumAttribute), Configuration.CodeTypeReferenceOptions),
                        new CodeAttributeArgument(new CodePrimitiveExpression(val.Value)));
                    member.CustomAttributes.Add(enumAttribute);
                }

                if (val.IsDeprecated)
                {
                    // From .NET 3.5 XmlSerializer doesn't serialize objects with [Obsolete] >(
                    //var deprecatedAttribute = new CodeAttributeDeclaration(new CodeTypeReference(typeof(ObsoleteAttribute)));
                    //member.CustomAttributes.Add(deprecatedAttribute);

                    var obsolete = new DocumentationModel { Language = "en", Text = "[Obsolete]" };
                    docs.Add(obsolete);
                }

                member.Comments.AddRange(DocumentationModel.GetComments(docs).ToArray());

                enumDeclaration.Members.Add(member);
            }

            return enumDeclaration;
        }

        public override CodeExpression GetDefaultValueFor(string defaultString)
        {
            return new CodeFieldReferenceExpression(new CodeTypeReferenceExpression(GetReferenceFor(null, false)),
                Values.First(v => v.Value == defaultString).Name);
        }
    }

    public class SimpleModel : TypeModel
    {
        public Type ValueType { get; set; }
        public List<RestrictionModel> Restrictions { get; private set; }
        public bool UseDataTypeAttribute { get; set; }

        public SimpleModel(GeneratorConfiguration configuration)
            : base(configuration)
        {
            Restrictions = new List<RestrictionModel>();
            UseDataTypeAttribute = true;
        }

        public static string GetCollectionDefinitionName(string typeName, GeneratorConfiguration configuration)
        {
            var typeRef = new CodeTypeReference(configuration.CollectionType, configuration.CodeTypeReferenceOptions);
            if (configuration.CollectionType.IsGenericTypeDefinition)
            {
                typeRef.TypeArguments.Add(typeName);
            }
            var typeOfExpr = new CodeTypeOfExpression(typeRef);
            var writer = new System.IO.StringWriter();
            CSharpProvider.GenerateCodeFromExpression(typeOfExpr, writer, new CodeGeneratorOptions());
            var fullTypeName = writer.ToString();
            Debug.Assert(fullTypeName.StartsWith("typeof(") && fullTypeName.EndsWith(")"));
            fullTypeName = fullTypeName.Substring(7, fullTypeName.Length - 8);
            return fullTypeName;
        }

        public static string GetCollectionImplementationName(string typeName, GeneratorConfiguration configuration)
        {
            var typeRef = new CodeTypeReference(configuration.CollectionImplementationType ?? configuration.CollectionType, configuration.CodeTypeReferenceOptions);
            if (configuration.CollectionType.IsGenericTypeDefinition)
            {
                typeRef.TypeArguments.Add(typeName);
            }
            var typeOfExpr = new CodeTypeOfExpression(typeRef);
            var writer = new System.IO.StringWriter();
            CSharpProvider.GenerateCodeFromExpression(typeOfExpr, writer, new CodeGeneratorOptions());
            var fullTypeName = writer.ToString();
            Debug.Assert(fullTypeName.StartsWith("typeof(") && fullTypeName.EndsWith(")"));
            fullTypeName = fullTypeName.Substring(7, fullTypeName.Length - 8);
            return fullTypeName;
        }

        public override CodeTypeDeclaration Generate()
        {
            return null;
        }

        public override CodeTypeReference GetReferenceFor(NamespaceModel referencingNamespace, bool collection, bool forInit = false)
        {
            var type = ValueType;

            if (XmlSchemaType != null)
            {
                // some types are not mapped in the same way between XmlSerializer and XmlSchema >(
                // http://msdn.microsoft.com/en-us/library/aa719879(v=vs.71).aspx
                // http://msdn.microsoft.com/en-us/library/system.xml.serialization.xmlelementattribute.datatype(v=vs.110).aspx
                // XmlSerializer is inconsistent: maps xs:decimal to decimal but xs:integer to string,
                // even though xs:integer is a restriction of xs:decimal
                type = XmlSchemaType.GetEffectiveType(Configuration);
                UseDataTypeAttribute = XmlSchemaType.IsDataTypeAttributeAllowed(Configuration) ?? UseDataTypeAttribute;
            }

            if (collection)
            {
                if (forInit)
                {
                    type = (Configuration.CollectionImplementationType ?? Configuration.CollectionType).MakeGenericType(type);
                }
                else
                {
                    type = Configuration.CollectionType.MakeGenericType(type);
                }
            }

            return new CodeTypeReference(type, Configuration.CodeTypeReferenceOptions);
        }

        public override CodeExpression GetDefaultValueFor(string defaultString)
        {
            if (ValueType == typeof(XmlQualifiedName))
            {
                if (defaultString.StartsWith("xs:", StringComparison.OrdinalIgnoreCase))
                {
                    var rv = new CodeObjectCreateExpression(typeof(XmlQualifiedName),
                        new CodePrimitiveExpression(defaultString.Substring(3)),
                        new CodePrimitiveExpression(XmlSchema.Namespace));
                    rv.CreateType.Options = Configuration.CodeTypeReferenceOptions;
                    return rv;
                }
                throw new NotSupportedException(string.Format("Resolving default value {0} for QName not supported.", defaultString));
            }
            return new CodePrimitiveExpression(Convert.ChangeType(defaultString, ValueType));
        }

        public IEnumerable<CodeAttributeDeclaration> GetRestrictionAttributes()
        {
            foreach (var attribute in Restrictions.Where(x => x.IsSupported).Select(r => r.GetAttribute()).Where(a => a != null))
            {
                yield return attribute;
            }

            var minInclusive = Restrictions.OfType<MinInclusiveRestrictionModel>().FirstOrDefault(x => x.IsSupported);
            var maxInclusive = Restrictions.OfType<MaxInclusiveRestrictionModel>().FirstOrDefault(x => x.IsSupported);

            if (minInclusive != null && maxInclusive != null)
            {
                yield return new CodeAttributeDeclaration(
                        new CodeTypeReference(typeof(RangeAttribute), Configuration.CodeTypeReferenceOptions),
                        new CodeAttributeArgument(new CodeTypeOfExpression(minInclusive.Type)),
                        new CodeAttributeArgument(new CodePrimitiveExpression(minInclusive.Value)),
                        new CodeAttributeArgument(new CodePrimitiveExpression(maxInclusive.Value)));
            }
        }
    }
}
