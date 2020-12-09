using System;
using System.CodeDom;
using System.CodeDom.Compiler;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
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

        public static IEnumerable<CodeCommentStatement> GetComments(IList<DocumentationModel> docs)
        {
            if (DisableComments || docs.Count == 0)
                yield break;

            yield return new CodeCommentStatement("<summary>", true);

            foreach (var doc in docs.OrderBy(d => d.Language))
            {
                var text = doc.Text;
                var comment = string.Format(@"<para{0}>{1}</para>",
                    string.IsNullOrEmpty(doc.Language) ? "" : string.Format(@" xml:lang=""{0}""", doc.Language), CodeUtilities.NormalizeNewlines(text).Trim());
                yield return new CodeCommentStatement(comment, true);
            }

            yield return new CodeCommentStatement("</summary>", true);
        }

        public static void AddDescription(CodeAttributeDeclarationCollection attributes, IEnumerable<DocumentationModel> docs, GeneratorConfiguration conf)
        {
            if (!conf.GenerateDescriptionAttribute || DisableComments || !docs.Any()) return;

            var doc = GetSingleDoc(docs);

            if (doc != null)
            {
                var descriptionAttribute = new CodeAttributeDeclaration(new CodeTypeReference(typeof(DescriptionAttribute), conf.CodeTypeReferenceOptions),
                    new CodeAttributeArgument(new CodePrimitiveExpression(Regex.Replace(doc.Text, @"\s+", " ").Trim())));
                attributes.Add(descriptionAttribute);
            }
        }

        private static DocumentationModel GetSingleDoc(IEnumerable<DocumentationModel> docs)
        {
            if (docs.Count() == 1) return docs.Single();
            var englishDoc = docs.FirstOrDefault(d => string.IsNullOrEmpty(d.Language) || d.Language.StartsWith("en", StringComparison.OrdinalIgnoreCase));
            if (englishDoc != null) return englishDoc;
            return docs.FirstOrDefault();
        }
    }

    [DebuggerDisplay("{Name}")]
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

            DocumentationModel.AddDescription(typeDeclaration.CustomAttributes, Documentation, Configuration);

            var generatedAttribute = new CodeAttributeDeclaration(new CodeTypeReference(typeof(GeneratedCodeAttribute), Configuration.CodeTypeReferenceOptions),
                new CodeAttributeArgument(new CodePrimitiveExpression(Configuration.Version.Title)),
                new CodeAttributeArgument(new CodePrimitiveExpression(Configuration.Version.Version)));
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
                if (IsAnonymous && (this is not ClassModel classModel || classModel.BaseClass == null))
                {
                    // don't generate AnonymousType if it's derived class, otherwise XmlSerializer will
                    // complain with "InvalidOperationException: Cannot include anonymous type '...'"
                    typeAttribute.Arguments.Add(new CodeAttributeArgument("AnonymousType", new CodePrimitiveExpression(true)));
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

        public virtual CodeTypeReference GetReferenceFor(NamespaceModel referencingNamespace, bool collection = false, bool forInit = false, bool attribute = false)
        {
            string name;
            if (referencingNamespace == Namespace)
                name = Name;
            else
                name = string.Format("{2}{0}.{1}", Namespace.Name, Name, ((referencingNamespace ?? Namespace).IsAmbiguous ? "global::" : string.Empty));

            if (collection)
            {
                name = forInit ? SimpleModel.GetCollectionImplementationName(name, Configuration) : SimpleModel.GetCollectionDefinitionName(name, Configuration);
            }

            return new CodeTypeReference(name, Configuration.CodeTypeReferenceOptions);
        }

        public virtual CodeExpression GetDefaultValueFor(string defaultString, bool attribute)
        {
            throw new NotSupportedException(string.Format("Getting default value for {0} not supported.", defaultString));
        }
    }

    public class InterfaceModel : ReferenceTypeModel
    {
        public InterfaceModel(GeneratorConfiguration configuration)
            : base(configuration)
        {
            Properties = new List<PropertyModel>();
            DerivedTypes = new List<ReferenceTypeModel>();
        }

        public List<ReferenceTypeModel> DerivedTypes { get; set; }

        public override CodeTypeDeclaration Generate()
        {
            var interfaceDeclaration = base.Generate();

            interfaceDeclaration.IsInterface = true;
            interfaceDeclaration.IsPartial = true;
            if (Configuration.AssemblyVisible)
            {
                interfaceDeclaration.TypeAttributes = (interfaceDeclaration.TypeAttributes & ~System.Reflection.TypeAttributes.VisibilityMask) | System.Reflection.TypeAttributes.NestedAssembly;
            }


            foreach (var property in Properties)
                property.AddInterfaceMembersTo(interfaceDeclaration);

            interfaceDeclaration.BaseTypes.AddRange(Interfaces.Select(i => i.GetReferenceFor(Namespace)).ToArray());

            Configuration.TypeVisitor(interfaceDeclaration, this);
            return interfaceDeclaration;
        }

        public IEnumerable<ReferenceTypeModel> AllDerivedReferenceTypes()
        {
            foreach (var interfaceModelDerivedType in DerivedTypes)
            {
                yield return interfaceModelDerivedType;
                switch (interfaceModelDerivedType)
                {
                    case InterfaceModel derivedInterfaceModel:
                    {
                        foreach (var referenceTypeModel in derivedInterfaceModel.AllDerivedReferenceTypes())
                        {
                            yield return referenceTypeModel;
                        }

                        break;
                    }
                    case ClassModel derivedClassModel:
                    {
                        foreach (var baseClass in derivedClassModel.GetAllDerivedTypes())
                        {
                            yield return baseClass;
                        }

                        break;
                    }
                }
            }
        }
    }

    public class ClassModel : ReferenceTypeModel
    {
        public bool IsAbstract { get; set; }
        public bool IsMixed { get; set; }
        public bool IsSubstitution { get; set; }
        public TypeModel BaseClass { get; set; }
        public List<ClassModel> DerivedTypes { get; set; }
        public ClassModel(GeneratorConfiguration configuration)
            : base(configuration)
        {
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

        public IEnumerable<TypeModel> AllBaseTypes
        {
            get
            {
                var baseType = BaseClass;
                while (baseType != null)
                {
                    yield return baseType;
                    baseType = (baseType as ClassModel)?.BaseClass;
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
            if (Configuration.AssemblyVisible)
                classDeclaration.TypeAttributes = (classDeclaration.TypeAttributes & ~System.Reflection.TypeAttributes.VisibilityMask) | System.Reflection.TypeAttributes.NestedAssembly;

            if (IsAbstract)
                classDeclaration.TypeAttributes |= System.Reflection.TypeAttributes.Abstract;

            if (Configuration.EnableDataBinding && !(BaseClass is ClassModel))
            {
                var propertyChangedEvent = new CodeMemberEvent()
                {
                    Name = "PropertyChanged",
                    Type = new CodeTypeReference(typeof(PropertyChangedEventHandler), Configuration.CodeTypeReferenceOptions),
                    Attributes = MemberAttributes.Public,
                };
                classDeclaration.Members.Add(propertyChangedEvent);

                var propertyChangedModel = new PropertyModel(Configuration)
                {
                    Name = propertyChangedEvent.Name,
                    OwningType = this,
                    Type = new SimpleModel(Configuration) { ValueType = typeof(PropertyChangedEventHandler) }
                };

                Configuration.MemberVisitor(propertyChangedEvent, propertyChangedModel);

                var onPropChangedMethod = new CodeMemberMethod
                {
                    Name = "OnPropertyChanged",
                    Attributes = MemberAttributes.Family,
                };
                var param = new CodeParameterDeclarationExpression(typeof(string), "propertyName");
                onPropChangedMethod.Parameters.Add(param);
                var threadSafeDelegateInvokeExpression = new CodeSnippetExpression($"{propertyChangedEvent.Name}?.Invoke(this, new System.ComponentModel.PropertyChangedEventArgs({param.Name}))");

                onPropChangedMethod.Statements.Add(threadSafeDelegateInvokeExpression);
                classDeclaration.Members.Add(onPropChangedMethod);
            }

            if (BaseClass != null)
            {
                if (BaseClass is ClassModel)
                {
                    classDeclaration.BaseTypes.Add(BaseClass.GetReferenceFor(Namespace));
                }
                else if (!string.IsNullOrEmpty(Configuration.TextValuePropertyName))
                {
                    var typeReference = BaseClass.GetReferenceFor(Namespace);

                    var member = new CodeMemberField(typeReference, Configuration.TextValuePropertyName)
                    {
                        Attributes = MemberAttributes.Public,
                    };

                    if (Configuration.EnableDataBinding)
                    {
                        var backingFieldMember = new CodeMemberField(typeReference, member.Name.ToBackingField(Configuration.PrivateMemberPrefix))
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

                    var docs = new List<DocumentationModel> { new DocumentationModel { Language = "en", Text = "Gets or sets the text value." },
                        new DocumentationModel { Language = "de", Text = "Ruft den Text ab oder legt diesen fest." } };

                    var attribute = new CodeAttributeDeclaration(new CodeTypeReference(typeof(XmlTextAttribute), Configuration.CodeTypeReferenceOptions));

                    if (BaseClass is SimpleModel simpleModel)
                    {
                        docs.AddRange(simpleModel.Restrictions.Select(r => new DocumentationModel { Language = "en", Text = r.Description }));
                        member.CustomAttributes.AddRange(simpleModel.GetRestrictionAttributes().ToArray());

                        if (simpleModel.XmlSchemaType.Datatype.IsDataTypeAttributeAllowed() ?? simpleModel.UseDataTypeAttribute)
                        {
                            var name = BaseClass.GetQualifiedName();
                            if (name.Namespace == XmlSchema.Namespace)
                            {
                                var dataType = new CodeAttributeArgument("DataType", new CodePrimitiveExpression(name.Name));
                                attribute.Arguments.Add(dataType);
                            }
                        }
                    }

                    member.Comments.AddRange(DocumentationModel.GetComments(docs).ToArray());

                    member.CustomAttributes.Add(attribute);
                    classDeclaration.Members.Add(member);

                    var valuePropertyModel = new PropertyModel(Configuration)
                    {
                        Name = Configuration.TextValuePropertyName,
                        OwningType = this,
                        Type = BaseClass
                    };

                    Configuration.MemberVisitor(member, valuePropertyModel);
                }
            }

            if (Configuration.EnableDataBinding)
            {
                classDeclaration.BaseTypes.Add(new CodeTypeReference(typeof(INotifyPropertyChanged), Configuration.CodeTypeReferenceOptions));
            }

            if (Configuration.EntityFramework && !(BaseClass is ClassModel))
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

            foreach (var property in Properties.GroupBy(x => x.Name))
            {
                var propertyIndex = 0;
                foreach (var p in property)
                {
                    if (propertyIndex > 0)
                    {
                        p.Name += $"_{propertyIndex}";
                    }
                    p.AddMembersTo(classDeclaration, Configuration.EnableDataBinding);
                    propertyIndex++;
                }
            }

            if (IsMixed && (BaseClass == null || (BaseClass is ClassModel && !AllBaseClasses.Any(b => b.IsMixed))))
            {
                var propName = "Text";

                // To not collide with any existing members
                for (var propertyIndex = 1; Properties.Any(x => x.Name.Equals(propName, StringComparison.Ordinal)) || propName.Equals(classDeclaration.Name, StringComparison.Ordinal); propertyIndex++)
                {
                    propName = $"Text_{propertyIndex}";
                }
                var text = new CodeMemberField(typeof(string[]), propName);
                // hack to generate automatic property
                text.Name += " { get; set; }";
                text.Attributes = MemberAttributes.Public;
                var xmlTextAttribute = new CodeAttributeDeclaration(new CodeTypeReference(typeof(XmlTextAttribute), Configuration.CodeTypeReferenceOptions));
                text.CustomAttributes.Add(xmlTextAttribute);
                classDeclaration.Members.Add(text);

                var textPropertyModel = new PropertyModel(Configuration)
                {
                    Name = propName,
                    OwningType = this,
                    Type = new SimpleModel(Configuration) { ValueType = typeof(string) }
                };

                Configuration.MemberVisitor(text, textPropertyModel);
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
                    new CodeAttributeArgument(new CodeTypeOfExpression(derivedType.GetReferenceFor(Namespace))));
                classDeclaration.CustomAttributes.Add(includeAttribute);
            }

            classDeclaration.BaseTypes.AddRange(Interfaces.Select(i => i.GetReferenceFor(Namespace)).ToArray());

            Configuration.TypeVisitor(classDeclaration, this);
            return classDeclaration;
        }

        public List<ClassModel> GetAllDerivedTypes()
        {
            var allDerivedTypes = new List<ClassModel>(DerivedTypes);

            foreach (var derivedType in DerivedTypes)
                allDerivedTypes.AddRange(derivedType.GetAllDerivedTypes());

            return allDerivedTypes;
        }

        public override CodeExpression GetDefaultValueFor(string defaultString, bool attribute)
        {
            var rootClass = AllBaseTypes.LastOrDefault();

            if (rootClass is SimpleModel)
            {
                string reference, val;

                using (var writer = new System.IO.StringWriter())
                {
                    CSharpProvider.GenerateCodeFromExpression(rootClass.GetDefaultValueFor(defaultString, attribute), writer, new CodeGeneratorOptions());
                    val = writer.ToString();
                }

                using (var writer = new System.IO.StringWriter())
                {
                    CSharpProvider.GenerateCodeFromExpression(new CodeTypeReferenceExpression(GetReferenceFor(referencingNamespace: null)), writer, new CodeGeneratorOptions());
                    reference = writer.ToString();
                }

                var dv = new CodeSnippetExpression($"new { reference } {{ { Configuration.TextValuePropertyName } = { val } }};");
                return dv;
            }

            return base.GetDefaultValueFor(defaultString, attribute);
        }
    }

    public class ReferenceTypeModel : TypeModel
    {
        public ReferenceTypeModel(GeneratorConfiguration configuration)
            : base(configuration)
        {
            Properties = new List<PropertyModel>();
            Interfaces = new List<InterfaceModel>();
        }

        public List<PropertyModel> Properties { get; set; }
        public List<InterfaceModel> Interfaces { get; }

        public void AddInterfaces(IEnumerable<InterfaceModel> interfaces)
        {
            foreach (var interfaceModel in interfaces)
            {
                Interfaces.Add(interfaceModel);
                interfaceModel.DerivedTypes.Add(this);
            }

        }
    }

    [DebuggerDisplay("{Name}")]
    public class PropertyModel
    {
        public TypeModel OwningType { get; set; }
        public string Name { get; set; }
        public string OriginalPropertyName { get; set; }
        public bool IsAttribute { get; set; }
        public TypeModel Type { get; set; }
        public bool IsNullable { get; set; }
        public bool IsNillable { get; set; }
        public bool IsCollection { get; set; }
        public string DefaultValue { get; set; }
        public string FixedValue { get; set; }
        public XmlSchemaForm Form { get; set; }
        public string XmlNamespace { get; set; }
        public List<DocumentationModel> Documentation { get; private set; }
        public bool IsDeprecated { get; set; }
        public XmlQualifiedName XmlSchemaName { get; set; }
        public bool IsAny { get; set; }
        public int? Order { get; set; }
        public bool IsKey { get; set; }
        public XmlSchemaParticle XmlParticle { get; set; }
        public XmlSchemaObject XmlParent { get; set; }
        public GeneratorConfiguration Configuration { get; private set; }
        public List<Substitute> Substitutes { get; set; }

        public PropertyModel(GeneratorConfiguration configuration)
        {
            Configuration = configuration;
            Documentation = new List<DocumentationModel>();
            Substitutes = new List<Substitute>();
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
                    OnPropertyChanged(nameof({1}));
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
                    OnPropertyChanged(nameof({1}));
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
                    OnPropertyChanged(nameof({1}));
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

        /// <summary>
        /// A property is an array if it is a sequence containing a single element with maxOccurs > 1.
        /// </summary>
        public bool IsArray
        {
            get
            {
                return !IsCollection && !IsAttribute && !IsList && TypeClassModel != null
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
                    && IsNullable && !(IsCollection || IsArray) && !IsList
                    && ((PropertyType is EnumModel) || (PropertyType is SimpleModel model && model.ValueType.IsValueType));
            }
        }

        private bool IsNillableValueType
        {
            get
            {
                return IsNillable
                    && !(IsCollection || IsArray)
                    && ((PropertyType is EnumModel) || (PropertyType is SimpleModel model && model.ValueType.IsValueType));
            }
        }

        private bool IsList
        {
            get
            {
                return Type.XmlSchemaType?.Datatype?.Variety == XmlSchemaDatatypeVariety.List;
            }
        }

        private CodeTypeReference TypeReference
        {
            get
            {
                return PropertyType.GetReferenceFor(OwningType.Namespace,
                    collection: IsCollection || IsArray || (IsList && IsAttribute),
                    attribute: IsAttribute);
            }
        }

        private void AddDocs(CodeTypeMember member)
        {
            var docs = new List<DocumentationModel>(Documentation);

            DocumentationModel.AddDescription(member.CustomAttributes, docs, Configuration);

            if (PropertyType is SimpleModel simpleType)
            {
                docs.AddRange(simpleType.Documentation);
                docs.AddRange(simpleType.Restrictions.Select(r => new DocumentationModel { Language = "en", Text = r.Description }));
                member.CustomAttributes.AddRange(simpleType.GetRestrictionAttributes().ToArray());
            }

            member.Comments.AddRange(DocumentationModel.GetComments(docs).ToArray());
        }

        private CodeAttributeDeclaration CreateDefaultValueAttribute(CodeTypeReference typeReference, CodeExpression defaultValueExpression)
        {
            var defaultValueAttribute = new CodeAttributeDeclaration(new CodeTypeReference(typeof(DefaultValueAttribute), Configuration.CodeTypeReferenceOptions));
            if (typeReference.BaseType == "System.Decimal")
            {
                defaultValueAttribute.Arguments.Add(new CodeAttributeArgument(new CodeTypeOfExpression(typeof(decimal))));
                defaultValueAttribute.Arguments.Add(new CodeAttributeArgument(new CodePrimitiveExpression(DefaultValue)));
            }
            else
                defaultValueAttribute.Arguments.Add(new CodeAttributeArgument(defaultValueExpression));

            return defaultValueAttribute;
        }

        public void AddInterfaceMembersTo(CodeTypeDeclaration typeDeclaration)
        {
            CodeTypeMember member;

            var isArray = IsArray;
            var propertyType = PropertyType;
            var isNullableValueType = IsNullableValueType;
            var typeReference = TypeReference;

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
                var defaultValueExpression = propertyType.GetDefaultValueFor(DefaultValue, IsAttribute);

                if ((defaultValueExpression is CodePrimitiveExpression) || (defaultValueExpression is CodeFieldReferenceExpression))
                {
                    var defaultValueAttribute = CreateDefaultValueAttribute(typeReference, defaultValueExpression);
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

            var requiresBackingField = withDataBinding || DefaultValue != null || IsCollection || isArray;
            CodeMemberField backingField;

            if (IsNillableValueType)
            {
                var nullableType = new CodeTypeReference(typeof(Nullable<>), Configuration.CodeTypeReferenceOptions);
                nullableType.TypeArguments.Add(typeReference);
                backingField = new CodeMemberField(nullableType, OwningType.GetUniqueFieldName(this));
            }
            else
            {
                backingField = new CodeMemberField(typeReference, OwningType.GetUniqueFieldName(this))
                {
                    Attributes = MemberAttributes.Private
                };
            }

            var ignoreAttribute = new CodeAttributeDeclaration(new CodeTypeReference(typeof(XmlIgnoreAttribute), Configuration.CodeTypeReferenceOptions));
            var notMappedAttribute = new CodeAttributeDeclaration(new CodeTypeReference(typeof(NotMappedAttribute), Configuration.CodeTypeReferenceOptions));
            backingField.CustomAttributes.Add(ignoreAttribute);

            if (requiresBackingField)
            {
                typeDeclaration.Members.Add(backingField);
            }

            if (DefaultValue == null || ((IsCollection || isArray || (IsList && IsAttribute)) && IsNullable))
            {
                var propertyName = Name;

                if (isNullableValueType && Configuration.GenerateNullables && !(Configuration.UseShouldSerializePattern && !IsAttribute))
                {
                    propertyName += "Value";
                }

                if (IsNillableValueType)
                {
                    var nullableType = new CodeTypeReference(typeof(Nullable<>), Configuration.CodeTypeReferenceOptions);
                    nullableType.TypeArguments.Add(typeReference);
                    member = new CodeMemberField(nullableType, propertyName);
                }
                else if (isNullableValueType && !IsAttribute && Configuration.UseShouldSerializePattern)
                {
                    var nullableType = new CodeTypeReference(typeof(Nullable<>), Configuration.CodeTypeReferenceOptions)
                    {
                        TypeArguments = { typeReference }
                    };
                    member = new CodeMemberField(nullableType, propertyName);

                    typeDeclaration.Members.Add(new CodeMemberMethod
                    {
                        Attributes = MemberAttributes.Public,
                        Name = "ShouldSerialize" + propertyName,
                        ReturnType = new CodeTypeReference(typeof(bool)),
                        Statements =
                        {
                            new CodeSnippetExpression($"return {propertyName}.HasValue")
                        }
                    });
                }
                else
                    member = new CodeMemberField(typeReference, propertyName);

                var isPrivateSetter = (IsCollection || isArray || (IsList && IsAttribute)) && Configuration.CollectionSettersMode == CollectionSettersMode.Private;

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
                var defaultValueExpression = propertyType.GetDefaultValueFor(DefaultValue, IsAttribute);
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

                if (IsNullable && ((defaultValueExpression is CodePrimitiveExpression) || (defaultValueExpression is CodeFieldReferenceExpression)))
                {
                    var defaultValueAttribute = CreateDefaultValueAttribute(typeReference, defaultValueExpression);
                    member.CustomAttributes.Add(defaultValueAttribute);
                }
            }

            member.Attributes = MemberAttributes.Public;
            typeDeclaration.Members.Add(member);

            AddDocs(member);

            if (!IsNullable && Configuration.DataAnnotationMode != DataAnnotationMode.None)
            {
                var requiredAttribute = new CodeAttributeDeclaration(new CodeTypeReference(typeof(RequiredAttribute), Configuration.CodeTypeReferenceOptions));
                member.CustomAttributes.Add(requiredAttribute);
            }

            if (IsDeprecated)
            {
                // From .NET 3.5 XmlSerializer doesn't serialize objects with [Obsolete] >(
            }

            if (isNullableValueType)
            {
                bool generateNullablesProperty = Configuration.GenerateNullables;
                bool generateSpecifiedProperty = true;

                if (generateNullablesProperty && Configuration.UseShouldSerializePattern && !IsAttribute)
                {
                    generateNullablesProperty = false;
                    generateSpecifiedProperty = false;
                }

                var specifiedName = generateNullablesProperty ? Name + "Value" : Name;
                CodeMemberField specifiedMember = null;
                if (generateSpecifiedProperty)
                {
                    specifiedMember = new CodeMemberField(typeof(bool), specifiedName + "Specified { get; set; }");
                    specifiedMember.CustomAttributes.Add(ignoreAttribute);
                    if (Configuration.EntityFramework && generateNullablesProperty) { specifiedMember.CustomAttributes.Add(notMappedAttribute); }
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
                }

                if (generateNullablesProperty)
                {
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
                                new CodeBinaryOperatorExpression(
                                    new CodeMethodInvokeExpression(valueExpression, "Equals", getValueOrDefaultExpression),
                                    CodeBinaryOperatorType.ValueEquality,
                                    new CodePrimitiveExpression(false)
                                    ),
                                CodeBinaryOperatorType.BooleanOr,
                                new CodeBinaryOperatorExpression(
                                    new CodeMethodInvokeExpression(specifiedExpression, "Equals", hasValueExpression),
                                    CodeBinaryOperatorType.ValueEquality,
                                    new CodePrimitiveExpression(false)
                                    )
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
                    editorBrowsableAttribute.Arguments.Add(new CodeAttributeArgument(new CodeFieldReferenceExpression(new CodeTypeReferenceExpression(new CodeTypeReference(typeof(EditorBrowsableState), CodeTypeReferenceOptions.GlobalReference)), "Never")));
                    specifiedMember?.CustomAttributes.Add(editorBrowsableAttribute);
                    member.CustomAttributes.Add(editorBrowsableAttribute);
                    if (Configuration.EntityFramework) { member.CustomAttributes.Add(notMappedAttribute); }

                    Configuration.MemberVisitor(nullableMember, this);
                }
            }
            else if ((IsCollection || isArray || (IsList && IsAttribute)) && IsNullable)
            {
                var specifiedProperty = new CodeMemberProperty
                {
                    Type = new CodeTypeReference(typeof(bool), Configuration.CodeTypeReferenceOptions),
                    Name = Name + "Specified",
                    HasSet = false,
                    HasGet = true,
                };
                specifiedProperty.CustomAttributes.Add(ignoreAttribute);
                if (Configuration.EntityFramework) { specifiedProperty.CustomAttributes.Add(notMappedAttribute); }
                specifiedProperty.Attributes = MemberAttributes.Public | MemberAttributes.Final;

                var listReference = new CodePropertyReferenceExpression(new CodeThisReferenceExpression(), Name);
                var collectionType = Configuration.CollectionImplementationType ?? Configuration.CollectionType;
                var countProperty = collectionType == typeof(System.Array) ? "Length" : "Count";
                var countReference = new CodePropertyReferenceExpression(listReference, countProperty);
                var notZeroExpression = new CodeBinaryOperatorExpression(countReference, CodeBinaryOperatorType.IdentityInequality, new CodePrimitiveExpression(0));
                if (Configuration.CollectionSettersMode == CollectionSettersMode.PublicWithoutConstructorInitialization)
                {
                    var notNullExpression = new CodeBinaryOperatorExpression(listReference, CodeBinaryOperatorType.IdentityInequality, new CodePrimitiveExpression(null));
                    notZeroExpression = new CodeBinaryOperatorExpression(notNullExpression, CodeBinaryOperatorType.BooleanAnd, notZeroExpression);
                }
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
            if ((IsCollection || isArray || (IsList && IsAttribute)) && Configuration.CollectionSettersMode != CollectionSettersMode.PublicWithoutConstructorInitialization)
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
                var collectionType = Configuration.CollectionImplementationType ?? Configuration.CollectionType;

                CodeExpression initExpression;

                if (collectionType == typeof(System.Array))
                {
                    var initTypeReference = propertyType.GetReferenceFor(OwningType.Namespace, collection: false, forInit: true, attribute: IsAttribute);
                    initExpression = new CodeArrayCreateExpression(initTypeReference);
                }
                else
                {
                    var initTypeReference = propertyType.GetReferenceFor(OwningType.Namespace, collection: true, forInit: true, attribute: IsAttribute);
                    initExpression = new CodeObjectCreateExpression(initTypeReference);
                }

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
                    var namespacePresent = arrayItemAttribute.Arguments.OfType<CodeAttributeArgument>().Any(a => a.Name == "Namespace");
                    if (!namespacePresent && !arrayItemProperty.XmlSchemaName.IsEmpty && !string.IsNullOrEmpty(arrayItemProperty.XmlSchemaName.Namespace))
                        arrayItemAttribute.Arguments.Add(new CodeAttributeArgument("Namespace", new CodePrimitiveExpression(arrayItemProperty.XmlSchemaName.Namespace)));
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
                    if (!Configuration.SeparateSubstitutes && Substitutes.Any())
                    {
                        foreach (var substitute in Substitutes)
                        {
                            var substitutedAttribute = new CodeAttributeDeclaration(new CodeTypeReference(typeof(XmlElementAttribute), Configuration.CodeTypeReferenceOptions),
                                new CodeAttributeArgument(new CodePrimitiveExpression(substitute.Element.QualifiedName.Name)),
                                new CodeAttributeArgument("Type", new CodeTypeOfExpression(substitute.Type.GetReferenceFor(OwningType.Namespace))),
                                new CodeAttributeArgument("Namespace", new CodePrimitiveExpression(substitute.Element.QualifiedName.Namespace)));

                            if (Order != null)
                            {
                                substitutedAttribute.Arguments.Add(new CodeAttributeArgument("Order",
                                    new CodePrimitiveExpression(Order.Value)));
                            }

                            attributes.Add(substitutedAttribute);
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
                var arrayAttribute = new CodeAttributeDeclaration(new CodeTypeReference(typeof(XmlArrayAttribute), Configuration.CodeTypeReferenceOptions),
                    new CodeAttributeArgument(new CodePrimitiveExpression(XmlSchemaName.Name)));
                if (Order != null)
                    arrayAttribute.Arguments.Add(new CodeAttributeArgument("Order", new CodePrimitiveExpression(Order.Value)));
                attributes.Add(arrayAttribute);
            }

            foreach (var attribute in attributes)
            {
                bool namespacePrecalculated = attribute.Arguments.OfType<CodeAttributeArgument>().Any(a => a.Name == "Namespace");
                if (!namespacePrecalculated)
                {
                    if (XmlNamespace != null)
                    {
                        attribute.Arguments.Add(new CodeAttributeArgument("Namespace", new CodePrimitiveExpression(XmlNamespace)));
                    }

                    if (Form == XmlSchemaForm.Qualified && IsAttribute)
                    {
                        if (XmlNamespace == null)
                        {
                            attribute.Arguments.Add(new CodeAttributeArgument("Namespace", new CodePrimitiveExpression(OwningType.XmlSchemaName.Namespace)));
                        }

                        attribute.Arguments.Add(new CodeAttributeArgument("Form",
                            new CodeFieldReferenceExpression(new CodeTypeReferenceExpression(new CodeTypeReference(typeof(XmlSchemaForm), Configuration.CodeTypeReferenceOptions)),
                                "Qualified")));
                    }
                    else if (Form == XmlSchemaForm.Unqualified && !IsAttribute && !IsAny && XmlNamespace == null)
                    {
                        attribute.Arguments.Add(new CodeAttributeArgument("Form",
                            new CodeFieldReferenceExpression(new CodeTypeReferenceExpression(new CodeTypeReference(typeof(XmlSchemaForm), Configuration.CodeTypeReferenceOptions)),
                                "Unqualified")));
                    }
                }

                if (IsNillable && !(IsCollection && Type is SimpleModel m && m.ValueType.IsValueType) && !(IsNullable && Configuration.DoNotForceIsNullable))
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
            if (Configuration.AssemblyVisible)
            {
                enumDeclaration.TypeAttributes = (enumDeclaration.TypeAttributes & ~System.Reflection.TypeAttributes.VisibilityMask) | System.Reflection.TypeAttributes.NestedAssembly;
            }

            foreach (var val in Values)
            {
                var member = new CodeMemberField { Name = val.Name };
                var docs = new List<DocumentationModel>(val.Documentation);

                DocumentationModel.AddDescription(member.CustomAttributes, docs, Configuration);

                if (val.Name != val.Value) // illegal identifier chars in value
                {
                    var enumAttribute = new CodeAttributeDeclaration(new CodeTypeReference(typeof(XmlEnumAttribute), Configuration.CodeTypeReferenceOptions),
                        new CodeAttributeArgument(new CodePrimitiveExpression(val.Value)));
                    member.CustomAttributes.Add(enumAttribute);
                }

                if (val.IsDeprecated)
                {
                    // From .NET 3.5 XmlSerializer doesn't serialize objects with [Obsolete] >(

                    var obsolete = new DocumentationModel { Language = "en", Text = "[Obsolete]" };
                    docs.Add(obsolete);
                }

                member.Comments.AddRange(DocumentationModel.GetComments(docs).ToArray());

                enumDeclaration.Members.Add(member);
            }

            if (RootElementName != null)
            {
                var rootAttribute = new CodeAttributeDeclaration(new CodeTypeReference(typeof(XmlRootAttribute), Configuration.CodeTypeReferenceOptions),
                    new CodeAttributeArgument(new CodePrimitiveExpression(RootElementName.Name)),
                    new CodeAttributeArgument("Namespace", new CodePrimitiveExpression(RootElementName.Namespace)));
                enumDeclaration.CustomAttributes.Add(rootAttribute);
            }
            Configuration.TypeVisitor(enumDeclaration, this);
            return enumDeclaration;
        }

        public override CodeExpression GetDefaultValueFor(string defaultString, bool attribute)
        {
            return new CodeFieldReferenceExpression(new CodeTypeReferenceExpression(GetReferenceFor(referencingNamespace: null)),
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
            var type = configuration.CollectionType;
            var typeRef = new CodeTypeReference(type, configuration.CodeTypeReferenceOptions);
            return GetFullTypeName(typeName, typeRef, type);
        }

        public static string GetCollectionImplementationName(string typeName, GeneratorConfiguration configuration)
        {
            var type = configuration.CollectionImplementationType ?? configuration.CollectionType;
            var typeRef = new CodeTypeReference(type, configuration.CodeTypeReferenceOptions);
            return GetFullTypeName(typeName, typeRef, type);
        }

        private static string GetFullTypeName(string typeName, CodeTypeReference typeRef, Type type)
        {
            if (type.IsGenericTypeDefinition)
                typeRef.TypeArguments.Add(typeName);
            else if (type == typeof(System.Array))
            {
                typeRef.ArrayElementType = new CodeTypeReference(typeName);
                typeRef.ArrayRank = 1;
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

        public override CodeTypeReference GetReferenceFor(NamespaceModel referencingNamespace, bool collection = false, bool forInit = false, bool attribute = false)
        {
            var type = ValueType;

            if (XmlSchemaType != null)
            {
                // some types are not mapped in the same way between XmlSerializer and XmlSchema >(
                // http://msdn.microsoft.com/en-us/library/aa719879(v=vs.71).aspx
                // http://msdn.microsoft.com/en-us/library/system.xml.serialization.xmlelementattribute.datatype(v=vs.110).aspx
                // XmlSerializer is inconsistent: maps xs:decimal to decimal but xs:integer to string,
                // even though xs:integer is a restriction of xs:decimal
                type = XmlSchemaType.Datatype.GetEffectiveType(Configuration, Restrictions, attribute);
                UseDataTypeAttribute = XmlSchemaType.Datatype.IsDataTypeAttributeAllowed() ?? UseDataTypeAttribute;
            }

            if (collection)
            {
                var collectionType = forInit ? (Configuration.CollectionImplementationType ?? Configuration.CollectionType) : Configuration.CollectionType;

                if (collectionType.IsGenericType)
                    type = collectionType.MakeGenericType(type);
                else if (collectionType == typeof(System.Array))
                    type = type.MakeArrayType();
                else
                    type = collectionType;
            }

            return new CodeTypeReference(type, Configuration.CodeTypeReferenceOptions);
        }

        public override CodeExpression GetDefaultValueFor(string defaultString, bool attribute)
        {
            var type = ValueType;

            if (XmlSchemaType != null)
            {
                type = XmlSchemaType.Datatype.GetEffectiveType(Configuration, Restrictions, attribute);
            }

            if (type == typeof(XmlQualifiedName))
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
            else if (type == typeof(DateTime))
            {
                var rv = new CodeMethodInvokeExpression(new CodeTypeReferenceExpression(new CodeTypeReference(typeof(DateTime), Configuration.CodeTypeReferenceOptions)),
                    "Parse", new CodePrimitiveExpression(defaultString));
                return rv;
            }
            else if (type == typeof(bool) && !string.IsNullOrWhiteSpace(defaultString))
            {
                if (defaultString == "0")
                    return new CodePrimitiveExpression(false);
                else if (defaultString == "1")
                    return new CodePrimitiveExpression(true);
                else
                    return new CodePrimitiveExpression(Convert.ChangeType(defaultString, ValueType));
            }
            else if (type == typeof(byte[]) && defaultString != null)
            {
                int numberChars = defaultString.Length;
                var byteValues = new CodePrimitiveExpression[numberChars / 2];
                for (int i = 0; i < numberChars; i += 2)
                    byteValues[i / 2] = new CodePrimitiveExpression(Convert.ToByte(defaultString.Substring(i, 2), 16));

                // For whatever reason, CodeDom will not generate a semicolon for the assignment statement if CodeArrayCreateExpression
                //  is used alone. Casting the value to the same type to work around this issue.
                var rv = new CodeCastExpression(typeof(byte[]), new CodeArrayCreateExpression(typeof(byte), byteValues));
                return rv;

            }

            return new CodePrimitiveExpression(Convert.ChangeType(defaultString, ValueType, CultureInfo.InvariantCulture));
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
