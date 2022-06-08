using System;
using System.CodeDom;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;

namespace XmlSchemaClassGenerator
{
    public class NamespaceModel : GeneratorModel
    {
        public string Name { get; set; }
        public NamespaceKey Key { get; }
        public Dictionary<string, TypeModel> Types { get; set; }
        /// <summary>
        /// Does the namespace of this type clashes with a class in the same or upper namespace?
        /// </summary>
        public bool IsAmbiguous { get; set; }

        public NamespaceModel(NamespaceKey key, GeneratorConfiguration configuration) : base(configuration)
        {
            Key = key;
            Types = new Dictionary<string, TypeModel>();
        }

        public static CodeNamespace Generate(string namespaceName, IEnumerable<NamespaceModel> parts, GeneratorConfiguration conf)
        {
            var codeNamespace = new CodeNamespace(namespaceName);

            foreach (var (Namespace, Condition) in CodeUtilities.UsingNamespaces.Where(n => n.Condition(conf)).OrderBy(n => n.Namespace))
                codeNamespace.Imports.Add(new CodeNamespaceImport(Namespace));

            foreach (var typeModel in parts.SelectMany(x => x.Types.Values).ToList())
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
    }

    [DebuggerDisplay("{Name}")]
    public abstract class TypeModel : GeneratorModel
    {
        protected static readonly CodeDomProvider CSharpProvider = CodeDomProvider.CreateProvider("CSharp");

        public NamespaceModel Namespace { get; set; }
        public XmlSchemaElement RootElement { get; set; }
        public XmlQualifiedName RootElementName { get; set; }
        public bool IsAbstractRoot { get; set; }
        public string Name { get; set; }
        public XmlQualifiedName XmlSchemaName { get; set; }
        public XmlSchemaType XmlSchemaType { get; set; }
        public List<DocumentationModel> Documentation { get; } = new();
        public bool IsAnonymous { get; set; }
        public virtual bool IsSubtype => false;

        protected TypeModel(GeneratorConfiguration configuration) : base(configuration) { }

        public virtual CodeTypeDeclaration Generate()
        {
            var typeDeclaration = new CodeTypeDeclaration { Name = Name };

            typeDeclaration.Comments.AddRange(GetComments(Documentation).ToArray());

            AddDescription(typeDeclaration.CustomAttributes, Documentation);

            var generatedAttribute = AttributeDecl<GeneratedCodeAttribute>(
                new(new CodePrimitiveExpression(Configuration.Version.Title)),
                new(new CodePrimitiveExpression(Configuration.CreateGeneratedCodeAttributeVersion ? Configuration.Version.Version : "")));
            typeDeclaration.CustomAttributes.Add(generatedAttribute);

            return typeDeclaration;
        }

        protected void GenerateTypeAttribute(CodeTypeDeclaration typeDeclaration)
        {
            if (XmlSchemaName == null) return;

            var typeAttribute = AttributeDecl<XmlTypeAttribute>(
                new(new CodePrimitiveExpression(XmlSchemaName.Name)),
                new(nameof(XmlRootAttribute.Namespace), new CodePrimitiveExpression(XmlSchemaName.Namespace)));

            // don't generate AnonymousType if it's derived class, otherwise XmlSerializer will
            // complain with "InvalidOperationException: Cannot include anonymous type '...'"
            if (IsAnonymous && !IsSubtype)
                typeAttribute.Arguments.Add(new("AnonymousType", new CodePrimitiveExpression(true)));

            typeDeclaration.CustomAttributes.Add(typeAttribute);
        }

        protected void GenerateSerializableAttribute(CodeTypeDeclaration typeDeclaration)
        {
            if (Configuration.GenerateSerializableAttribute)
                typeDeclaration.CustomAttributes.Add(AttributeDecl<SerializableAttribute>());
        }

        public virtual CodeTypeReference GetReferenceFor(NamespaceModel referencingNamespace, bool collection = false, bool forInit = false, bool attribute = false)
        {
            string name;
            var referencingOptions = Configuration.CodeTypeReferenceOptions;
            if (referencingNamespace == Namespace)
            {
                name = Name;
                referencingOptions = CodeTypeReferenceOptions.GenericTypeParameter;
            }
            else if ((referencingNamespace ?? Namespace).IsAmbiguous)
            {
                name = $"global::{Namespace.Name}.{Name}";
                referencingOptions = CodeTypeReferenceOptions.GenericTypeParameter;
            }
            else
            {
                name = $"{Namespace.Name}.{Name}";
            }

            if (collection)
            {
                name = forInit ? SimpleModel.GetCollectionImplementationName(name, Configuration) : SimpleModel.GetCollectionDefinitionName(name, Configuration);
                referencingOptions = Configuration.CollectionType == typeof(Array)
                    ? CodeTypeReferenceOptions.GenericTypeParameter
                    : Configuration.CodeTypeReferenceOptions;
            }

            return new CodeTypeReference(name, referencingOptions);
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

        public IEnumerable<ReferenceTypeModel> AllDerivedReferenceTypes(List<ReferenceTypeModel> processedTypeModels = null)
        {
            processedTypeModels ??= new();

            foreach (var interfaceModelDerivedType in DerivedTypes.Except(processedTypeModels))
            {
                yield return interfaceModelDerivedType;

                processedTypeModels.Add(interfaceModelDerivedType);

                switch (interfaceModelDerivedType)
                {
                    case InterfaceModel derivedInterfaceModel:
                        {
                            foreach (var referenceTypeModel in derivedInterfaceModel.AllDerivedReferenceTypes(processedTypeModels))
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
        public override bool IsSubtype => BaseClass != null;

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

            if (Configuration.EnableDataBinding && BaseClass is not ClassModel)
            {
                var propertyChangedEvent = new CodeMemberEvent()
                {
                    Name = nameof(INotifyPropertyChanged.PropertyChanged),
                    Type = TypeRef<PropertyChangedEventHandler>(),
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

                var param = new CodeParameterDeclarationExpression(typeof(string), "propertyName");
                var threadSafeDelegateInvokeExpression = new CodeSnippetExpression($"{propertyChangedEvent.Name}?.Invoke(this, new System.ComponentModel.PropertyChangedEventArgs({param.Name}))");
                var onPropChangedMethod = new CodeMemberMethod
                {
                    Name = OnPropertyChanged,
                    Attributes = MemberAttributes.Family,
                    Parameters = { param },
                    Statements = { threadSafeDelegateInvokeExpression }
                };

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
                        member.Name += GetSet;
                    }

                    var docs = new List<DocumentationModel> {
                        new() { Language = English, Text = "Gets or sets the text value." },
                        new() { Language = German, Text = "Ruft den Text ab oder legt diesen fest." }
                    };

                    var attribute = AttributeDecl<XmlTextAttribute>();

                    if (BaseClass is SimpleModel simpleModel)
                    {
                        docs.AddRange(simpleModel.Restrictions.Select(r => new DocumentationModel { Language = English, Text = r.Description }));
                        member.CustomAttributes.AddRange(simpleModel.GetRestrictionAttributes().ToArray());

                        if (BaseClass.GetQualifiedName() is { Namespace: XmlSchema.Namespace, Name: var name } && (simpleModel.XmlSchemaType.Datatype.IsDataTypeAttributeAllowed() ?? simpleModel.UseDataTypeAttribute))
                            attribute.Arguments.Add(new CodeAttributeArgument(nameof(XmlTextAttribute.DataType), new CodePrimitiveExpression(name)));
                    }

                    member.Comments.AddRange(GetComments(docs).ToArray());

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
                classDeclaration.BaseTypes.Add(TypeRef<INotifyPropertyChanged>());
            }

            if (Configuration.EntityFramework && BaseClass is not ClassModel)
            {
                // generate key
                var keyProperty = Properties.Find(p => string.Equals(p.Name, "id", StringComparison.InvariantCultureIgnoreCase))
                    ?? Properties.Find(p => p.Name.ToLowerInvariant() == Name.ToLowerInvariant() + "id");

                if (keyProperty == null)
                {
                    keyProperty = new PropertyModel(Configuration)
                    {
                        Name = "Id",
                        Type = new SimpleModel(Configuration) { ValueType = typeof(long) },
                        OwningType = this,
                        Documentation = {
                            new() { Language = English, Text = "Gets or sets a value uniquely identifying this entity." },
                            new() { Language = German, Text = "Ruft einen Wert ab, der diese Entität eindeutig identifiziert, oder legt diesen fest." }
                        }
                    };
                    Properties.Insert(0, keyProperty);
                }

                keyProperty.IsKey = true;
            }

            var properties = Properties.GroupBy(x => x.Name).SelectMany(g => g.Select((p, i) => (Property: p, Index: i)).ToList());
            foreach (var (Property, Index) in properties)
            {
                if (Index > 0)
                {
                    Property.Name += $"_{Index + 1}";

                    if (properties.Any(q => Property.XmlSchemaName == q.Property.XmlSchemaName && q.Index < Index))
                        continue;
                }

                Property.AddMembersTo(classDeclaration, Configuration.EnableDataBinding);
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
                text.Name += GetSet;
                text.Attributes = MemberAttributes.Public;
                var xmlTextAttribute = AttributeDecl<XmlTextAttribute>();
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

            var customAttributes = classDeclaration.CustomAttributes;

            if (Configuration.GenerateDebuggerStepThroughAttribute)
                customAttributes.Add(AttributeDecl<DebuggerStepThroughAttribute>());

            if (Configuration.GenerateDesignerCategoryAttribute)
                customAttributes.Add(AttributeDecl<DesignerCategoryAttribute>(new CodeAttributeArgument(new CodePrimitiveExpression("code"))));

            if (RootElementName != null)
            {
                var rootAttribute = AttributeDecl<XmlRootAttribute>(
                    new(new CodePrimitiveExpression(RootElementName.Name)),
                    new(nameof(XmlRootAttribute.Namespace), new CodePrimitiveExpression(RootElementName.Namespace)));
                customAttributes.Add(rootAttribute);
            }

            var derivedTypes = GetAllDerivedTypes();
            foreach (var derivedType in derivedTypes.OrderBy(t => t.Name))
                customAttributes.Add(AttributeDecl<XmlIncludeAttribute>(new CodeAttributeArgument(new CodeTypeOfExpression(derivedType.GetReferenceFor(Namespace)))));

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

                return new CodeSnippetExpression($"new {reference} {{ {Configuration.TextValuePropertyName} = {val} }};");
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
                if (!Interfaces.Contains(interfaceModel) && interfaceModel != this)
                {
                    Interfaces.Add(interfaceModel);
                    interfaceModel.DerivedTypes.Add(this);
                }
            }
        }
    }

    [DebuggerDisplay("{Name}")]
    public class PropertyModel : GeneratorModel
    {
        private const string Value = nameof(Value);
        private const string Specified = nameof(Specified);
        private const string Namespace = nameof(XmlRootAttribute.Namespace);

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
        public List<DocumentationModel> Documentation { get; }
        public bool IsDeprecated { get; set; }
        public XmlQualifiedName XmlSchemaName { get; set; }
        public bool IsAny { get; set; }
        public int? Order { get; set; }
        public bool IsKey { get; set; }
        public XmlSchemaParticle XmlParticle { get; set; }
        public XmlSchemaObject XmlParent { get; set; }
        public Particle Particle { get; set; }
        public List<Substitute> Substitutes { get; set; }

        public PropertyModel(GeneratorConfiguration configuration) : base(configuration)
        {
            Documentation = new List<DocumentationModel>();
            Substitutes = new List<Substitute>();
        }

        internal static string GetAccessors(string memberName, string backingFieldName, PropertyValueTypeCode typeCode, bool privateSetter, bool withDataBinding = true)
        {
            string assign = $@"
                {backingFieldName} = value;";

            return CodeUtilities.NormalizeNewlines($@"
        {{
            get
            {{
                return {backingFieldName};
            }}
            {(privateSetter ? "private " : string.Empty)}set
            {{{(typeCode, withDataBinding) switch
            {
                (PropertyValueTypeCode.ValueType, true) => $@"
                if (!{backingFieldName}.Equals(value))
                {{{assign}
                    OnPropertyChanged(nameof({memberName}));
                }}",
                (PropertyValueTypeCode.Other or PropertyValueTypeCode.Array, true) => $@"
                if ({backingFieldName} == value)
                    return;
                if ({backingFieldName} == null || value == null || !{backingFieldName}.{(typeCode is PropertyValueTypeCode.Other ? EqualsMethod : nameof(Enumerable.SequenceEqual))}(value))
                {{{assign}
                    OnPropertyChanged(nameof({memberName}));
                }}",
                _ => assign,
            }}
            }}
        }}");
        }

        private ClassModel TypeClassModel => Type as ClassModel;

        /// <summary>
        /// A property is an array if it is a sequence containing a single element with maxOccurs > 1.
        /// </summary>
        public bool IsArray => Configuration.UseArrayItemAttribute
                && !IsCollection && !IsAttribute && !IsList && TypeClassModel != null
                && TypeClassModel.BaseClass == null
                && TypeClassModel.Properties.Count == 1
                && !TypeClassModel.Properties[0].IsAttribute && !TypeClassModel.Properties[0].IsAny
                && TypeClassModel.Properties[0].IsCollection;

        private TypeModel PropertyType => !IsArray ? Type : TypeClassModel.Properties[0].Type;

        private bool IsNullableValueType => DefaultValue == null
                    && IsNullable && !(IsCollection || IsArray) && !IsList
                    && ((PropertyType is EnumModel) || (PropertyType is SimpleModel model && model.ValueType.IsValueType));

        private bool IsNullableReferenceType => DefaultValue == null
                    && IsNullable && (IsCollection || IsArray || IsList || PropertyType is ClassModel || (PropertyType is SimpleModel model && !model.ValueType.IsValueType));

        private bool IsNillableValueType => IsNillable
                    && !(IsCollection || IsArray)
                    && ((PropertyType is EnumModel) || (PropertyType is SimpleModel model && model.ValueType.IsValueType));

        private bool IsList => Type.XmlSchemaType?.Datatype?.Variety == XmlSchemaDatatypeVariety.List;

        private bool IsPrivateSetter => Configuration.CollectionSettersMode == CollectionSettersMode.Private
                    && (IsCollection || IsArray || (IsList && IsAttribute));

        private CodeTypeReference TypeReference => PropertyType.GetReferenceFor(OwningType.Namespace,
                    collection: IsCollection || IsArray || (IsList && IsAttribute),
                    attribute: IsAttribute);

        private void AddDocs(CodeTypeMember member)
        {
            var docs = new List<DocumentationModel>(Documentation);

            AddDescription(member.CustomAttributes, docs);

            if (PropertyType is SimpleModel simpleType)
            {
                docs.AddRange(simpleType.Documentation);
                docs.AddRange(simpleType.Restrictions.Select(r => new DocumentationModel { Language = English, Text = r.Description }));
                member.CustomAttributes.AddRange(simpleType.GetRestrictionAttributes().ToArray());
            }

            member.Comments.AddRange(GetComments(docs).ToArray());
        }

        private CodeAttributeDeclaration CreateDefaultValueAttribute(CodeTypeReference typeReference, CodeExpression defaultValueExpression)
        {
            var defaultValueAttribute = AttributeDecl<DefaultValueAttribute>();

            defaultValueAttribute.Arguments.AddRange(typeReference.BaseType == typeof(decimal).FullName
                ? new CodeAttributeArgument[] { new(new CodeTypeOfExpression(typeof(decimal))), new(new CodePrimitiveExpression(DefaultValue)) }
                : new CodeAttributeArgument[] { new(defaultValueExpression) });

            return defaultValueAttribute;
        }

        public void AddInterfaceMembersTo(CodeTypeDeclaration typeDeclaration)
        {
            CodeTypeMember member;

            var propertyType = PropertyType;
            var isNullableValueType = IsNullableValueType;
            var isPrivateSetter = IsPrivateSetter;
            var typeReference = TypeReference;

            if (isNullableValueType && Configuration.GenerateNullables)
                typeReference = NullableTypeRef(typeReference);

            member = new CodeMemberProperty
            {
                Name = Name,
                Type = typeReference,
                HasGet = true,
                HasSet = !isPrivateSetter
            };

            if (DefaultValue != null && IsNullable)
            {
                var defaultValueExpression = propertyType.GetDefaultValueFor(DefaultValue, IsAttribute);

                if ((defaultValueExpression is CodePrimitiveExpression or CodeFieldReferenceExpression) && !CodeUtilities.IsXmlLangOrSpace(XmlSchemaName))
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
            // Note: We use CodeMemberField because CodeMemberProperty doesn't allow for private set
            var member = new CodeMemberField() { Name = Name };

            var typeClassModel = TypeClassModel;
            var isArray = IsArray;
            var propertyType = PropertyType;
            var isNullableValueType = IsNullableValueType;
            var isNullableReferenceType = IsNullableReferenceType;
            var isPrivateSetter = IsPrivateSetter;
            var typeReference = TypeReference;

            CodeAttributeDeclaration ignoreAttribute = new(TypeRef<XmlIgnoreAttribute>());
            CodeAttributeDeclaration notMappedAttribute = new(CodeUtilities.CreateTypeReference(Attributes.NotMapped, Configuration));

            CodeMemberField backingField = null;
            if (withDataBinding || DefaultValue != null || IsCollection || isArray)
            {
                backingField = IsNillableValueType
                    ? new CodeMemberField(NullableTypeRef(typeReference), OwningType.GetUniqueFieldName(this))
                    : new CodeMemberField(typeReference, OwningType.GetUniqueFieldName(this)) { Attributes = MemberAttributes.Private };
                backingField.CustomAttributes.Add(ignoreAttribute);
                typeDeclaration.Members.Add(backingField);
            }

            if (DefaultValue == null || ((IsCollection || isArray || (IsList && IsAttribute)) && IsNullable))
            {
                if (isNullableValueType && Configuration.GenerateNullables && !(Configuration.UseShouldSerializePattern && !IsAttribute))
                    member.Name += Value;

                if (IsNillableValueType)
                {
                    member.Type = NullableTypeRef(typeReference);
                }
                else if (isNullableValueType && !IsAttribute && Configuration.UseShouldSerializePattern)
                {
                    member.Type = NullableTypeRef(typeReference);

                    typeDeclaration.Members.Add(new CodeMemberMethod
                    {
                        Attributes = MemberAttributes.Public,
                        Name = "ShouldSerialize" + member.Name,
                        ReturnType = new CodeTypeReference(typeof(bool)),
                        Statements = { new CodeSnippetExpression($"return {member.Name}.{HasValue}") }
                    });
                }
                else
                {
                    member.Type = typeReference;
                }

                if (backingField != null)
                {
                    var propertyValueTypeCode = IsCollection || isArray ? PropertyValueTypeCode.Array : propertyType.GetPropertyValueTypeCode();
                    member.Name += GetAccessors(member.Name, backingField.Name, propertyValueTypeCode, isPrivateSetter, withDataBinding);
                }
                else
                {
                    var privateSetter = isPrivateSetter ? "private " : string.Empty;
                    member.Name += $" {{ get; {privateSetter}set; }}"; // hack to generate automatic property
                }
            }
            else
            {
                var defaultValueExpression = propertyType.GetDefaultValueFor(DefaultValue, IsAttribute);
                backingField.InitExpression = defaultValueExpression;

                member.Type = IsNillableValueType ? NullableTypeRef(typeReference) : typeReference;

                member.Name += GetAccessors(member.Name, backingField.Name, propertyType.GetPropertyValueTypeCode(), false, withDataBinding);

                if (IsNullable && (defaultValueExpression is CodePrimitiveExpression or CodeFieldReferenceExpression) && !CodeUtilities.IsXmlLangOrSpace(XmlSchemaName))
                    member.CustomAttributes.Add(CreateDefaultValueAttribute(typeReference, defaultValueExpression));
            }

            member.Attributes = MemberAttributes.Public;
            typeDeclaration.Members.Add(member);

            AddDocs(member);

            if (!IsNullable && Configuration.DataAnnotationMode != DataAnnotationMode.None)
            {
                var requiredAttribute = new CodeAttributeDeclaration(CodeUtilities.CreateTypeReference(Attributes.Required, Configuration));
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

                var specifiedName = generateNullablesProperty ? Name + Value : Name;
                CodeMemberField specifiedMember = null;
                if (generateSpecifiedProperty)
                {
                    specifiedMember = new CodeMemberField(typeof(bool), specifiedName + Specified + GetSet);
                    specifiedMember.CustomAttributes.Add(ignoreAttribute);
                    if (Configuration.EntityFramework && generateNullablesProperty) { specifiedMember.CustomAttributes.Add(notMappedAttribute); }
                    specifiedMember.Attributes = MemberAttributes.Public;
                    var specifiedDocs = new DocumentationModel[] {
                        new() { Language = English, Text = $"Gets or sets a value indicating whether the {Name} property is specified." },
                        new() { Language = German, Text = $"Ruft einen Wert ab, der angibt, ob die {Name}-Eigenschaft spezifiziert ist, oder legt diesen fest." }
                    };
                    specifiedMember.Comments.AddRange(GetComments(specifiedDocs).ToArray());
                    typeDeclaration.Members.Add(specifiedMember);

                    var specifiedMemberPropertyModel = new PropertyModel(Configuration) { Name = specifiedName + Specified };

                    Configuration.MemberVisitor(specifiedMember, specifiedMemberPropertyModel);
                }

                if (generateNullablesProperty)
                {
                    var nullableMember = new CodeMemberProperty
                    {
                        Type = NullableTypeRef(typeReference),
                        Name = Name,
                        HasSet = true,
                        HasGet = true,
                        Attributes = MemberAttributes.Public | MemberAttributes.Final,
                    };
                    nullableMember.CustomAttributes.Add(ignoreAttribute);
                    nullableMember.Comments.AddRange(member.Comments);

                    var specifiedExpression = new CodePropertyReferenceExpression(new CodeThisReferenceExpression(), specifiedName + Specified);
                    var valueExpression = new CodePropertyReferenceExpression(new CodeThisReferenceExpression(), Name + Value);
                    var conditionStatement = new CodeConditionStatement(specifiedExpression,
                        new CodeStatement[] { new CodeMethodReturnStatement(valueExpression) },
                        new CodeStatement[] { new CodeMethodReturnStatement(new CodePrimitiveExpression(null)) });
                    nullableMember.GetStatements.Add(conditionStatement);

                    var getValueOrDefaultExpression = new CodeMethodInvokeExpression(new CodePropertySetValueReferenceExpression(), nameof(Nullable<int>.GetValueOrDefault));
                    var setValueStatement = new CodeAssignStatement(valueExpression, getValueOrDefaultExpression);
                    var hasValueExpression = new CodePropertyReferenceExpression(new CodePropertySetValueReferenceExpression(), HasValue);
                    var setSpecifiedStatement = new CodeAssignStatement(specifiedExpression, hasValueExpression);

                    var statements = new List<CodeStatement>();
                    if (withDataBinding)
                    {
                        var ifNotEquals = new CodeConditionStatement(
                            new CodeBinaryOperatorExpression(
                                new CodeBinaryOperatorExpression(
                                    new CodeMethodInvokeExpression(valueExpression, EqualsMethod, getValueOrDefaultExpression),
                                    CodeBinaryOperatorType.ValueEquality,
                                    new CodePrimitiveExpression(false)
                                    ),
                                CodeBinaryOperatorType.BooleanOr,
                                new CodeBinaryOperatorExpression(
                                    new CodeMethodInvokeExpression(specifiedExpression, EqualsMethod, hasValueExpression),
                                    CodeBinaryOperatorType.ValueEquality,
                                    new CodePrimitiveExpression(false)
                                    )
                            ),
                            setValueStatement,
                            setSpecifiedStatement,
                            new CodeExpressionStatement(new CodeMethodInvokeExpression(null, OnPropertyChanged,
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

                    var editorBrowsableAttribute = AttributeDecl<EditorBrowsableAttribute>();
                    editorBrowsableAttribute.Arguments.Add(new(new CodeFieldReferenceExpression(TypeRefExpr<EditorBrowsableState>(), nameof(EditorBrowsableState.Never))));
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
                    Type = TypeRef<bool>(),
                    Name = Name + Specified,
                    HasSet = false,
                    HasGet = true,
                };
                specifiedProperty.CustomAttributes.Add(ignoreAttribute);
                if (Configuration.EntityFramework) { specifiedProperty.CustomAttributes.Add(notMappedAttribute); }
                specifiedProperty.Attributes = MemberAttributes.Public | MemberAttributes.Final;

                var listReference = new CodePropertyReferenceExpression(new CodeThisReferenceExpression(), Name);
                var collectionType = Configuration.CollectionImplementationType ?? Configuration.CollectionType;
                var countProperty = collectionType == typeof(Array) ? nameof(Array.Length) : nameof(List<int>.Count);
                var countReference = new CodePropertyReferenceExpression(listReference, countProperty);
                var notZeroExpression = new CodeBinaryOperatorExpression(countReference, CodeBinaryOperatorType.IdentityInequality, new CodePrimitiveExpression(0));
                if (Configuration.CollectionSettersMode is CollectionSettersMode.PublicWithoutConstructorInitialization or CollectionSettersMode.Public)
                {
                    var notNullExpression = new CodeBinaryOperatorExpression(listReference, CodeBinaryOperatorType.IdentityInequality, new CodePrimitiveExpression(null));
                    notZeroExpression = new CodeBinaryOperatorExpression(notNullExpression, CodeBinaryOperatorType.BooleanAnd, notZeroExpression);
                }
                var returnStatement = new CodeMethodReturnStatement(notZeroExpression);
                specifiedProperty.GetStatements.Add(returnStatement);

                var specifiedDocs = new DocumentationModel[] {
                    new() { Language = English, Text = $"Gets a value indicating whether the {Name} collection is empty." },
                    new() { Language = German, Text = $"Ruft einen Wert ab, der angibt, ob die {Name}-Collection leer ist." }
                };
                specifiedProperty.Comments.AddRange(GetComments(specifiedDocs).ToArray());

                Configuration.MemberVisitor(specifiedProperty, this);

                typeDeclaration.Members.Add(specifiedProperty);
            }

            if (!IsCollection && isNullableReferenceType && Configuration.EnableNullableReferenceAttributes)
            {
                member.CustomAttributes.Add(new CodeAttributeDeclaration(CodeUtilities.CreateTypeReference(Attributes.AllowNull, Configuration)));
                member.CustomAttributes.Add(new CodeAttributeDeclaration(CodeUtilities.CreateTypeReference(Attributes.MaybeNull, Configuration)));
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
                    var constructorDocs = new DocumentationModel[] {
                        new() { Language = English, Text = $@"Initializes a new instance of the <see cref=""{typeDeclaration.Name}"" /> class." },
                        new() { Language = German, Text = $@"Initialisiert eine neue Instanz der <see cref=""{typeDeclaration.Name}"" /> Klasse." }
                    };
                    constructor.Comments.AddRange(GetComments(constructorDocs).ToArray());
                    typeDeclaration.Members.Add(constructor);
                }

                CodeExpression listReference = backingField != null
                    ? new CodeFieldReferenceExpression(new CodeThisReferenceExpression(), backingField.Name)
                    : new CodePropertyReferenceExpression(new CodeThisReferenceExpression(), Name);
                var collectionType = Configuration.CollectionImplementationType ?? Configuration.CollectionType;

                CodeExpression initExpression;

                if (collectionType == typeof(Array))
                {
                    var initTypeReference = propertyType.GetReferenceFor(OwningType.Namespace, collection: false, forInit: true, attribute: IsAttribute);
                    initExpression = new CodeMethodInvokeExpression(new(TypeRefExpr<Array>(), nameof(Array.Empty), initTypeReference));
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

                // HACK: repackage as ArrayItemAttribute
                foreach (var propertyAttribute in arrayItemProperty.GetAttributes(false, OwningType).ToList())
                {
                    var arrayItemAttribute = AttributeDecl<XmlArrayItemAttribute>(
                        propertyAttribute.Arguments.Cast<CodeAttributeArgument>().Where(x => !string.Equals(x.Name, nameof(Order), StringComparison.Ordinal)).ToArray());
                    var namespacePresent = arrayItemAttribute.Arguments.OfType<CodeAttributeArgument>().Any(a => a.Name == Namespace);
                    if (!namespacePresent && !arrayItemProperty.XmlSchemaName.IsEmpty && !string.IsNullOrEmpty(arrayItemProperty.XmlSchemaName.Namespace))
                        arrayItemAttribute.Arguments.Add(new(Namespace, new CodePrimitiveExpression(arrayItemProperty.XmlSchemaName.Namespace)));
                    member.CustomAttributes.Add(arrayItemAttribute);
                }
            }

            if (IsKey)
                member.CustomAttributes.Add(new(CodeUtilities.CreateTypeReference(Attributes.Key, Configuration)));

            if (IsAny && Configuration.EntityFramework)
                member.CustomAttributes.Add(notMappedAttribute);

            Configuration.MemberVisitor(member, this);
        }

        private IEnumerable<CodeAttributeDeclaration> GetAttributes(bool isArray, TypeModel owningType = null)
        {
            var attributes = new List<CodeAttributeDeclaration>();

            if (IsKey && XmlSchemaName == null)
            {
                attributes.Add(AttributeDecl<XmlIgnoreAttribute>());
                return attributes;
            }

            if (IsAttribute)
            {
                if (IsAny)
                {
                    var anyAttribute = AttributeDecl<XmlAnyAttributeAttribute>();
                    if (Order != null)
                        anyAttribute.Arguments.Add(new(nameof(Order), new CodePrimitiveExpression(Order.Value)));
                    attributes.Add(anyAttribute);
                }
                else
                {
                    attributes.Add(AttributeDecl<XmlAttributeAttribute>(new CodeAttributeArgument(new CodePrimitiveExpression(XmlSchemaName.Name))));
                }
            }
            else if (!isArray)
            {
                if (IsAny)
                {
                    var anyAttribute = AttributeDecl<XmlAnyElementAttribute>();
                    if (Order != null)
                        anyAttribute.Arguments.Add(new(nameof(Order), new CodePrimitiveExpression(Order.Value)));
                    attributes.Add(anyAttribute);
                }
                else
                {
                    if (!Configuration.SeparateSubstitutes && Substitutes.Count > 0)
                    {
                        owningType ??= OwningType;

                        foreach (var substitute in Substitutes)
                        {
                            var substitutedAttribute = AttributeDecl<XmlElementAttribute>(
                                new(new CodePrimitiveExpression(substitute.Element.QualifiedName.Name)),
                                new(nameof(XmlElementAttribute.Type), new CodeTypeOfExpression(substitute.Type.GetReferenceFor(owningType.Namespace))),
                                new(nameof(XmlElementAttribute.Namespace), new CodePrimitiveExpression(substitute.Element.QualifiedName.Namespace)));

                            if (Order != null)
                                substitutedAttribute.Arguments.Add(new(nameof(Order), new CodePrimitiveExpression(Order.Value)));

                            attributes.Add(substitutedAttribute);
                        }
                    }

                    var attribute = AttributeDecl<XmlElementAttribute>(new CodeAttributeArgument(new CodePrimitiveExpression(XmlSchemaName.Name)));
                    if (Order != null)
                        attribute.Arguments.Add(new(nameof(Order), new CodePrimitiveExpression(Order.Value)));
                    attributes.Add(attribute);
                }
            }
            else
            {
                var arrayAttribute = AttributeDecl<XmlArrayAttribute>(new CodeAttributeArgument(new CodePrimitiveExpression(XmlSchemaName.Name)));
                if (Order != null)
                    arrayAttribute.Arguments.Add(new(nameof(Order), new CodePrimitiveExpression(Order.Value)));
                attributes.Add(arrayAttribute);
            }

            foreach (var args in attributes.Select(a => a.Arguments))
            {
                bool namespacePrecalculated = args.OfType<CodeAttributeArgument>().Any(a => a.Name == Namespace);
                if (!namespacePrecalculated)
                {
                    if (XmlNamespace != null)
                        args.Add(new(Namespace, new CodePrimitiveExpression(XmlNamespace)));

                    if (Form == XmlSchemaForm.Qualified && IsAttribute)
                    {
                        if (XmlNamespace == null)
                            args.Add(new(Namespace, new CodePrimitiveExpression(OwningType.XmlSchemaName.Namespace)));

                        args.Add(new(nameof(Form), new CodeFieldReferenceExpression(TypeRefExpr<XmlSchemaForm>(), nameof(XmlSchemaForm.Qualified))));
                    }
                    else if ((Form == XmlSchemaForm.Unqualified || Form == XmlSchemaForm.None) && !IsAttribute && !IsAny && XmlNamespace == null)
                    {
                        args.Add(new(nameof(Form), new CodeFieldReferenceExpression(TypeRefExpr<XmlSchemaForm>(), nameof(XmlSchemaForm.Unqualified))));
                    }
                }

                if (IsNillable && !(IsCollection && Type is SimpleModel m && m.ValueType.IsValueType) && !(IsNullable && Configuration.DoNotForceIsNullable))
                    args.Add(new("IsNullable", new CodePrimitiveExpression(true)));

                if (Type is SimpleModel simpleModel && simpleModel.UseDataTypeAttribute)
                {
                    // walk up the inheritance chain to find DataType if the simple type is derived (see #18)
                    var xmlSchemaType = Type.XmlSchemaType;
                    while (xmlSchemaType != null)
                    {
                        var name = xmlSchemaType.GetQualifiedName();
                        if (name.Namespace == XmlSchema.Namespace && name.Name != "anySimpleType")
                        {
                            args.Add(new("DataType", new CodePrimitiveExpression(name.Name)));
                            break;
                        }
                        else
                        {
                            xmlSchemaType = xmlSchemaType.BaseXmlSchemaType;
                        }
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
        public List<DocumentationModel> Documentation { get; } = new();
    }

    public class EnumModel : TypeModel
    {
        public List<EnumValueModel> Values { get; set; } = new();

        public EnumModel(GeneratorConfiguration configuration) : base(configuration) { }
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

                AddDescription(member.CustomAttributes, docs);

                if (val.Name != val.Value) // illegal identifier chars in value
                {
                    var enumAttribute = AttributeDecl<XmlEnumAttribute>(new CodeAttributeArgument(new CodePrimitiveExpression(val.Value)));
                    member.CustomAttributes.Add(enumAttribute);
                }

                if (val.IsDeprecated)
                {
                    // From .NET 3.5 XmlSerializer doesn't serialize objects with [Obsolete] >(

                    DocumentationModel obsolete = new() { Language = English, Text = "[Obsolete]" };
                    docs.Add(obsolete);
                }

                member.Comments.AddRange(GetComments(docs).ToArray());

                enumDeclaration.Members.Add(member);
            }

            if (RootElementName != null)
            {
                var rootAttribute = AttributeDecl<XmlRootAttribute>(
                    new(new CodePrimitiveExpression(RootElementName.Name)),
                    new(nameof(XmlRootAttribute.Namespace), new CodePrimitiveExpression(RootElementName.Namespace)));
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
        public List<RestrictionModel> Restrictions { get; } = new();
        public bool UseDataTypeAttribute { get; set; } = true;

        public SimpleModel(GeneratorConfiguration configuration) : base(configuration) { }

        public static string GetCollectionDefinitionName(string typeName, GeneratorConfiguration configuration)
        {
            var type = configuration.CollectionType;
            var typeRef = CodeUtilities.CreateTypeReference(type, configuration);
            return GetFullTypeName(typeName, typeRef, type);
        }

        public static string GetCollectionImplementationName(string typeName, GeneratorConfiguration configuration)
        {
            var type = configuration.CollectionImplementationType ?? configuration.CollectionType;
            var typeRef = CodeUtilities.CreateTypeReference(type, configuration);
            return GetFullTypeName(typeName, typeRef, type);
        }

        private static string GetFullTypeName(string typeName, CodeTypeReference typeRef, Type type)
        {
            if (type.IsGenericTypeDefinition)
            {
                typeRef.TypeArguments.Add(typeName);
            }
            else if (type == typeof(Array))
            {
                typeRef.ArrayElementType = new CodeTypeReference(typeName);
                typeRef.ArrayRank = 1;
            }
            var typeOfExpr = new CodeTypeOfExpression(typeRef)
            {
                Type = { Options = CodeTypeReferenceOptions.GenericTypeParameter }
            };
            var writer = new System.IO.StringWriter();
            CSharpProvider.GenerateCodeFromExpression(typeOfExpr, writer, new CodeGeneratorOptions());
            var fullTypeName = writer.ToString();
            Debug.Assert(fullTypeName.StartsWith("typeof(") && fullTypeName.EndsWith(")"));
            return fullTypeName.Substring(7, fullTypeName.Length - 8);
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
                {
                    type = collectionType.MakeGenericType(type);
                }
                else
                {
                    if (collectionType == typeof(Array))
                    {
                        type = type.MakeArrayType();
                    }
                    else
                    {
                        type = collectionType;
                    }
                }
            }

            return CodeUtilities.CreateTypeReference(type, Configuration);
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
                return new CodeMethodInvokeExpression(TypeRefExpr<DateTime>(), nameof(DateTime.Parse), new CodePrimitiveExpression(defaultString));
            }
            else if (type == typeof(TimeSpan))
            {
                return new CodeMethodInvokeExpression(TypeRefExpr<XmlConvert>(), nameof(XmlConvert.ToTimeSpan), new CodePrimitiveExpression(defaultString));
            }
            else if (type == typeof(bool) && !string.IsNullOrWhiteSpace(defaultString))
            {
                var val = defaultString switch
                {
                    "0" => false,
                    "1" => true,
                    _ => Convert.ChangeType(defaultString, ValueType)
                };
                return new CodePrimitiveExpression(val);
            }
            else if (type == typeof(byte[]) && defaultString != null)
            {
                int numberChars = defaultString.Length;
                var byteValues = new CodePrimitiveExpression[numberChars / 2];
                for (int i = 0; i < numberChars; i += 2)
                    byteValues[i / 2] = new CodePrimitiveExpression(Convert.ToByte(defaultString.Substring(i, 2), 16));

                // For whatever reason, CodeDom will not generate a semicolon for the assignment statement if CodeArrayCreateExpression
                //  is used alone. Casting the value to the same type to work around this issue.
                return new CodeCastExpression(typeof(byte[]), new CodeArrayCreateExpression(typeof(byte), byteValues));
            }
            else if (type == typeof(double) && !string.IsNullOrWhiteSpace(defaultString))
            {
                if (defaultString.Equals("inf", StringComparison.OrdinalIgnoreCase))
                    return new CodePrimitiveExpression(double.NegativeInfinity);
                else if (defaultString.Equals("-inf", StringComparison.OrdinalIgnoreCase))
                    return new CodePrimitiveExpression(double.NegativeInfinity);
            }

            return new CodePrimitiveExpression(Convert.ChangeType(defaultString, ValueType, CultureInfo.InvariantCulture));
        }

        public IEnumerable<CodeAttributeDeclaration> GetRestrictionAttributes()
        {
            foreach (var attribute in Restrictions.Where(x => x.IsSupported).Select(r => r.GetAttribute()).Where(a => a != null))
                yield return attribute;

            var minInclusive = Restrictions.OfType<MinInclusiveRestrictionModel>().FirstOrDefault(x => x.IsSupported);
            var maxInclusive = Restrictions.OfType<MaxInclusiveRestrictionModel>().FirstOrDefault(x => x.IsSupported);

            if (minInclusive != null && maxInclusive != null)
            {
                var rangeAttribute = new CodeAttributeDeclaration(
                    CodeUtilities.CreateTypeReference(Attributes.Range, Configuration),
                    new(new CodeTypeOfExpression(minInclusive.Type)),
                    new(new CodePrimitiveExpression(minInclusive.Value)),
                    new(new CodePrimitiveExpression(maxInclusive.Value)));

                // see https://github.com/mganss/XmlSchemaClassGenerator/issues/268
                if (Configuration.NetCoreSpecificCode)
                {
                    if (minInclusive.Value.Contains(".") || maxInclusive.Value.Contains("."))
                        rangeAttribute.Arguments.Add(new("ParseLimitsInInvariantCulture", new CodePrimitiveExpression(true)));

                    if (minInclusive.Type != typeof(int) && minInclusive.Type != typeof(double))
                        rangeAttribute.Arguments.Add(new("ConvertValueInInvariantCulture", new CodePrimitiveExpression(true)));
                }

                yield return rangeAttribute;
            }
        }
    }

    public class GeneratorModel
    {
        protected const string OnPropertyChanged = nameof(OnPropertyChanged);
        protected const string EqualsMethod = nameof(object.Equals);
        protected const string HasValue = nameof(Nullable<int>.HasValue);

        protected const string GetSet = " { get; set; }";

        protected const string English = "en";
        protected const string German = "de";

        protected GeneratorModel(GeneratorConfiguration configuration) => Configuration = configuration;

        public GeneratorConfiguration Configuration { get; }

        protected CodeTypeReferenceExpression TypeRefExpr<T>() => new(TypeRef<T>());

        protected CodeAttributeDeclaration AttributeDecl<T>(params CodeAttributeArgument[] args) => new(TypeRef<T>(), args);

        private protected CodeAttributeDeclaration AttributeDecl(TypeInfo attribute, CodeAttributeArgument arg)
            => new(CodeUtilities.CreateTypeReference(attribute, Configuration), arg);

        protected CodeTypeReference TypeRef<T>() => CodeUtilities.CreateTypeReference(typeof(T), Configuration);

        protected CodeTypeReference NullableTypeRef(CodeTypeReference typeReference)
        {
            var nullableType = CodeUtilities.CreateTypeReference(typeof(Nullable<>), Configuration);
            nullableType.TypeArguments.Add(typeReference);
            return nullableType;
        }
        public static bool DisableComments { get; set; }

        protected IEnumerable<CodeCommentStatement> GetComments(IList<DocumentationModel> docs)
        {
            if (DisableComments || docs.Count == 0)
                yield break;

            yield return new CodeCommentStatement("<summary>", true);

            foreach (var doc in docs
                .Where(d => string.IsNullOrEmpty(d.Language) || Configuration.CommentLanguages.Any(l => d.Language.StartsWith(l, StringComparison.OrdinalIgnoreCase)))
                .OrderBy(d => d.Language))
            {
                var text = doc.Text;
                var comment = $"<para{(string.IsNullOrEmpty(doc.Language) ? "" : $@" xml:lang=""{doc.Language}""")}>{CodeUtilities.NormalizeNewlines(text).Trim()}</para>";
                yield return new CodeCommentStatement(comment, true);
            }

            yield return new CodeCommentStatement("</summary>", true);
        }

        protected void AddDescription(CodeAttributeDeclarationCollection attributes, IEnumerable<DocumentationModel> docs)
        {
            if (!Configuration.GenerateDescriptionAttribute || DisableComments || !docs.Any()) return;

            var doc = GetSingleDoc(docs.Where(d => string.IsNullOrEmpty(d.Language) || Configuration.CommentLanguages.Any(l => d.Language.StartsWith(l, StringComparison.OrdinalIgnoreCase))));

            if (doc != null)
            {
                var descriptionAttribute = AttributeDecl<DescriptionAttribute>(new CodeAttributeArgument(new CodePrimitiveExpression(Regex.Replace(doc.Text, @"\s+", " ").Trim())));
                attributes.Add(descriptionAttribute);
            }
        }

        private static DocumentationModel GetSingleDoc(IEnumerable<DocumentationModel> docs)
        {
            return docs.Count() == 1 ? docs.Single()
                 : docs.FirstOrDefault(d => string.IsNullOrEmpty(d.Language) || d.Language.StartsWith(English, StringComparison.OrdinalIgnoreCase))
                 ?? docs.FirstOrDefault();
        }
    }
}
