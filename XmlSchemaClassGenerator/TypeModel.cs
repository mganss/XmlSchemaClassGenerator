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
using static System.Net.Mime.MediaTypeNames;

namespace XmlSchemaClassGenerator;

public class NamespaceModel(NamespaceKey key, GeneratorConfiguration configuration) : GeneratorModel(configuration)
{
    public string Name { get; set; }
    public NamespaceKey Key { get; } = key;
    public Dictionary<string, TypeModel> Types { get; set; } = [];
    /// <summary>
    /// Does the namespace of this type clashes with a class in the same or upper namespace?
    /// </summary>
    public bool IsAmbiguous { get; set; }

    public static CodeNamespace Generate(string namespaceName, IEnumerable<NamespaceModel> parts, GeneratorConfiguration conf)
    {
        var codeNamespace = new CodeNamespace(namespaceName);

        foreach (var (Namespace, _) in CodeUtilities.UsingNamespaces.Where(n => n.Condition(conf)).OrderBy(n => n.Namespace))
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
public abstract class TypeModel(GeneratorConfiguration configuration) : GeneratorModel(configuration)
{
    protected static readonly CodeDomProvider CSharpProvider = CodeDomProvider.CreateProvider("CSharp");

    public NamespaceModel Namespace { get; set; }
    public XmlSchemaElement RootElement { get; set; }
    public XmlQualifiedName RootElementName { get; set; }
    public bool IsAbstractRoot { get; set; }
    public string Name { get; set; }
    public XmlQualifiedName XmlSchemaName { get; set; }
    public XmlSchemaType XmlSchemaType { get; set; }
    public List<DocumentationModel> Documentation { get; } = [];
    public bool IsAnonymous { get; set; }
    public virtual bool IsSubtype => false;
    public virtual bool IsRedefined => false;

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
        var xmlSchemaName = XmlSchemaName;

        if (xmlSchemaName == null && RootElementName != null && typeDeclaration.Name != RootElementName.Name)
            xmlSchemaName = RootElementName;

        if (xmlSchemaName == null || IsRedefined) return;

        var typeAttribute = AttributeDecl<XmlTypeAttribute>(
            new(new CodePrimitiveExpression(xmlSchemaName.Name)),
            new(nameof(XmlRootAttribute.Namespace), new CodePrimitiveExpression(xmlSchemaName.Namespace)));

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

public class InterfaceModel(GeneratorConfiguration configuration) : ReferenceTypeModel(configuration)
{
    public List<ReferenceTypeModel> DerivedTypes { get; } = [];

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
        processedTypeModels ??= [];

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

public class ClassModel(GeneratorConfiguration configuration) : ReferenceTypeModel(configuration)
{
    public override bool IsRedefined => DerivedTypes.Exists(d => d.XmlSchemaType?.Parent is XmlSchemaRedefine);
    public bool IsAbstract { get; set; }
    public bool IsMixed { get; set; }
    public bool IsSubstitution { get; set; }
    public TypeModel BaseClass { get; set; }
    public List<ClassModel> DerivedTypes { get; set; } = [];
    public override bool IsSubtype => BaseClass != null;

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

            SimpleModel type = new(Configuration) { ValueType = typeof(PropertyChangedEventHandler) };
            var propertyChangedModel = new PropertyModel(Configuration, propertyChangedEvent.Name, type, this);

            Configuration.MemberVisitor(propertyChangedEvent, propertyChangedModel);

            var param = new CodeParameterDeclarationExpression(typeof(string), "propertyName = null");
            param.CustomAttributes.Add(new(TypeRef<System.Runtime.CompilerServices.CallerMemberNameAttribute>()));
            var threadSafeDelegateInvokeExpression = new CodeSnippetExpression($"{propertyChangedEvent.Name}?.Invoke(this, new System.ComponentModel.PropertyChangedEventArgs(propertyName))");
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
                var textName = Configuration.TextValuePropertyName;
                var enableDataBinding = Configuration.EnableDataBinding;
                var typeReference = BaseClass.GetReferenceFor(Namespace);

                CodeMemberField backingFieldMember = null;
                if (enableDataBinding)
                {
                    backingFieldMember = new CodeMemberField(typeReference, textName.ToBackingField(Configuration.PrivateMemberPrefix))
                    {
                        Attributes = MemberAttributes.Private
                    };
                    classDeclaration.Members.Add(backingFieldMember);
                }

                CodeMemberField text = new(typeReference, textName + PropertyModel.GetAccessors(backingFieldMember, enableDataBinding, BaseClass.GetPropertyValueTypeCode()))
                {
                    Attributes = MemberAttributes.Public,
                };

                var docs = new List<DocumentationModel> {
                    new() { Language = English, Text = "Gets or sets the text value." },
                    new() { Language = German, Text = "Ruft den Text ab oder legt diesen fest." }
                };

                var attribute = AttributeDecl<XmlTextAttribute>();

                if (BaseClass is SimpleModel simpleModel)
                {
                    docs.AddRange(simpleModel.Restrictions.Select(r => new DocumentationModel { Language = English, Text = r.Description }));
                    text.CustomAttributes.AddRange(simpleModel.GetRestrictionAttributes().ToArray());

                    if (BaseClass.GetQualifiedName() is { Namespace: XmlSchema.Namespace, Name: var name } && (simpleModel.XmlSchemaType.Datatype.IsDataTypeAttributeAllowed(Configuration) ?? simpleModel.UseDataTypeAttribute))
                        attribute.Arguments.Add(new CodeAttributeArgument(nameof(XmlTextAttribute.DataType), new CodePrimitiveExpression(name)));
                }

                text.Comments.AddRange(GetComments(docs).ToArray());

                text.CustomAttributes.Add(attribute);
                classDeclaration.Members.Add(text);

                var valuePropertyModel = new PropertyModel(Configuration, textName, BaseClass, this);

                Configuration.MemberVisitor(text, valuePropertyModel);
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
                keyProperty = new PropertyModel(Configuration, "Id", new SimpleModel(Configuration) { ValueType = typeof(long) }, this)
                {
                    Documentation = {
                        new() { Language = English, Text = "Gets or sets a value uniquely identifying this entity." },
                        new() { Language = German, Text = "Ruft einen Wert ab, der diese Entität eindeutig identifiziert, oder legt diesen fest." }
                    },
                    IsRequired = true
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
            var propertyIndex = 1;

            // To not collide with any existing members
            while (Properties.Exists(x => x.Name.Equals(propName, StringComparison.Ordinal)) || propName.Equals(classDeclaration.Name, StringComparison.Ordinal))
            {
                propName = $"Text_{propertyIndex}";
                propertyIndex++;
            }
            // hack to generate automatic property
            var text = new CodeMemberField(typeof(string[]), propName + PropertyModel.GetAccessors()) { Attributes = MemberAttributes.Public };
            text.CustomAttributes.Add(AttributeDecl<XmlTextAttribute>());
            classDeclaration.Members.Add(text);

            var textPropertyModel = new PropertyModel(Configuration, propName, new SimpleModel(Configuration) { ValueType = typeof(string) }, this);

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

public class ReferenceTypeModel(GeneratorConfiguration configuration) : TypeModel(configuration)
{
    public List<PropertyModel> Properties { get; } = [];
    public List<InterfaceModel> Interfaces { get; } = [];

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
public class PropertyModel(GeneratorConfiguration configuration, string name, TypeModel type, TypeModel owningType) : GeneratorModel(configuration)
{
    private const string Value = nameof(Value);
    private const string Specified = nameof(Specified);
    private const string Namespace = nameof(XmlRootAttribute.Namespace);

    // ctor
    public List<DocumentationModel> Documentation { get; } = [];
    public List<Substitute> Substitutes { get; } = [];
    public TypeModel OwningType { get; } = owningType;
    public TypeModel Type { get; } = type;
    public string Name { get; set; } = name;

    // private
    public string OriginalPropertyName { get; private set; }
    public string DefaultValue { get; private set; }
    public string FixedValue { get; private set; }
    public XmlSchemaForm Form { get; private set; }
    public string XmlNamespace { get; private set; }
    public XmlQualifiedName XmlSchemaName { get; private set; }
    public XmlSchemaParticle XmlParticle { get; private set; }
    public XmlSchemaObject XmlParent { get; private set; }
    public Particle Particle { get; private set; }

    // public
    public bool IsAttribute { get; set; }
    public bool IsRequired { get; set; }
    public bool IsNillable { get; set; }
    public bool IsCollection { get; set; }
    public bool IsDeprecated { get; set; }
    public bool IsAny { get; set; }
    public int? Order { get; set; }
    public bool IsKey { get; set; }

    public void SetFromNode(string originalName, bool useFixedIfNoDefault, IXmlSchemaNode xs)
    {
        OriginalPropertyName = originalName;

        DefaultValue = xs.DefaultValue ?? (useFixedIfNoDefault ? xs.FixedValue : null);
        FixedValue = xs.FixedValue;
        Form = xs.Form switch
        {
            XmlSchemaForm.None => xs.RefName?.IsEmpty == false ? XmlSchemaForm.Qualified : xs.FormDefault,
            _ => xs.Form,
        };
    }

    public void SetFromParticles(Particle particle, Particle item, bool isRequired)
    {
        Particle = item;
        XmlParticle = item.XmlParticle;
        XmlParent = item.XmlParent;

        IsRequired = isRequired;
        IsCollection = item.MaxOccurs > 1.0m || particle.MaxOccurs > 1.0m; // http://msdn.microsoft.com/en-us/library/vstudio/d3hx2s7e(v=vs.100).aspx
    }

    public void SetSchemaNameAndNamespace(TypeModel owningTypeModel, IXmlSchemaNode xs)
    {
        XmlSchemaName = xs.QualifiedName;
        XmlNamespace = string.IsNullOrEmpty(xs.QualifiedName.Namespace)
                       || xs.QualifiedName.Namespace == owningTypeModel.XmlSchemaName.Namespace ? null
                        : xs.QualifiedName.Namespace;
    }

    internal static string GetAccessors(CodeMemberField backingField = null, bool withDataBinding = false, PropertyValueTypeCode typeCode = PropertyValueTypeCode.Other, string setter = "set")
    {
        return backingField == null ? " { get; set; }" : CodeUtilities.NormalizeNewlines($@"
        {{
            get
            {{
                return {backingField.Name};
            }}
            {setter}
            {{{(typeCode, withDataBinding) switch
        {
            (PropertyValueTypeCode.ValueType, true) => $@"
                if ({checkEquality()}){assignAndNotify()}",
            (PropertyValueTypeCode.Other or PropertyValueTypeCode.Array, true) => $@"
                if ({backingField.Name} == value)
                    return;
                if ({backingField.Name} == null || value == null || {checkEquality()}){assignAndNotify()}",
            _ => assign(),
        }}
            }}
        }}");

        string assign() => $@"
                {backingField.Name} = value;";

        string assignAndNotify() => $@"
                {{{assign()}
                    {OnPropertyChanged}();
                }}";

        string checkEquality()
            => $"!{backingField.Name}.{(typeCode is PropertyValueTypeCode.Array ? nameof(Enumerable.SequenceEqual) : EqualsMethod)}(value)";
    }

    private ClassModel TypeClassModel => Type as ClassModel;

    /// <summary>
    /// A property is an array if it is a sequence containing a single element with maxOccurs > 1.
    /// </summary>
    public bool IsArray => Configuration.UseArrayItemAttribute
            && !IsCollection && !IsList && TypeClassModel != null
            && TypeClassModel.BaseClass == null
            && TypeClassModel.Properties.Count == 1
            && !TypeClassModel.Properties[0].IsAttribute && !TypeClassModel.Properties[0].IsAny
            && TypeClassModel.Properties[0].IsCollection;

    private bool IsEnumerable => IsCollection || IsArray || IsList;

    private TypeModel PropertyType => !IsArray ? Type : TypeClassModel.Properties[0].Type;

    private bool IsNullable => DefaultValue == null && !IsRequired;

    private bool IsValueType => PropertyType is EnumModel || (PropertyType is SimpleModel model && model.ValueType.IsValueType);

    private bool IsNullableValueType => IsNullable && !IsEnumerable && IsValueType;

    private bool IsNullableReferenceType => IsNullable && (!IsEnumerable || !IsPrivateSetter) && (PropertyType is ClassModel || (PropertyType is SimpleModel model && !model.ValueType.IsValueType));

    private bool IsNillableValueType => IsNillable && !IsEnumerable && IsValueType;

    private bool IsList => Type.XmlSchemaType?.Datatype?.Variety == XmlSchemaDatatypeVariety.List;

    private bool IsPrivateSetter => IsEnumerable && Configuration.CollectionSettersMode == CollectionSettersMode.Private;

    private CodeTypeReference TypeReference => PropertyType.GetReferenceFor(OwningType.Namespace, collection: IsEnumerable, attribute: IsAttribute);

    private void AddDocs(CodeTypeMember member)
    {
        var docs = new List<DocumentationModel>(Documentation);

        AddDescription(member.CustomAttributes, docs);

        if (PropertyType is SimpleModel simpleType && !IsEnumerable)
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
            ? [new(new CodeTypeOfExpression(typeof(decimal))), new(new CodePrimitiveExpression(DefaultValue))]
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

        if ((isNullableValueType || IsNillableValueType) && Configuration.GenerateNullables)
            typeReference = NullableTypeRef(typeReference);

        member = new CodeMemberProperty
        {
            Name = Name,
            Type = typeReference,
            HasGet = true,
            HasSet = !isPrivateSetter
        };

        if (DefaultValue != null && !IsRequired)
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

        var isArray = IsArray;
        var isEnumerable = IsEnumerable;
        var propertyType = PropertyType;
        var isNullableValueType = IsNullableValueType;
        var typeReference = TypeReference;

        CodeAttributeDeclaration ignoreAttribute = new(TypeRef<XmlIgnoreAttribute>());
        CodeAttributeDeclaration notMappedAttribute = new(CodeUtilities.CreateTypeReference(Attributes.NotMapped, Configuration));

        CodeMemberField backingField = null;
        if (withDataBinding || DefaultValue != null || isEnumerable)
        {
            backingField = IsNillableValueType
                ? new CodeMemberField(NullableTypeRef(typeReference), OwningType.GetUniqueFieldName(this))
                : new CodeMemberField(typeReference, OwningType.GetUniqueFieldName(this)) { Attributes = MemberAttributes.Private };
            backingField.CustomAttributes.Add(ignoreAttribute);
            typeDeclaration.Members.Add(backingField);
        }

        if (DefaultValue == null || isEnumerable)
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

            var propertyValueTypeCode = IsCollection || isArray ? PropertyValueTypeCode.Array : propertyType.GetPropertyValueTypeCode();
            var setter = Configuration.CollectionSettersMode switch
            {
                CollectionSettersMode.Private when IsEnumerable => "private set",
                CollectionSettersMode.Init or CollectionSettersMode.InitWithoutConstructorInitialization when IsEnumerable => "init",
                _ => "set"
            };
            member.Name += GetAccessors(backingField, withDataBinding, propertyValueTypeCode, setter);
        }
        else
        {
            var defaultValueExpression = propertyType.GetDefaultValueFor(DefaultValue, IsAttribute);

            if (backingField != null)
                backingField.InitExpression = defaultValueExpression;

            member.Type = IsNillableValueType ? NullableTypeRef(typeReference) : typeReference;

            member.Name += GetAccessors(backingField, withDataBinding, propertyType.GetPropertyValueTypeCode());

            if (!IsRequired && (defaultValueExpression is CodePrimitiveExpression or CodeFieldReferenceExpression) && !CodeUtilities.IsXmlLangOrSpace(XmlSchemaName))
                member.CustomAttributes.Add(CreateDefaultValueAttribute(typeReference, defaultValueExpression));
        }

        member.Attributes = MemberAttributes.Public;
        typeDeclaration.Members.Add(member);

        AddDocs(member);

        if (IsRequired && Configuration.DataAnnotationMode != DataAnnotationMode.None)
        {
            var requiredAttribute = new CodeAttributeDeclaration(CodeUtilities.CreateTypeReference(Attributes.Required, Configuration));
            var noEmptyStrings = propertyType is SimpleModel simpleModel 
                && simpleModel.Restrictions.Any(r => r is MinLengthRestrictionModel m && m.Value > 0);
            var allowEmptyStringsArgument = new CodeAttributeArgument("AllowEmptyStrings", new CodePrimitiveExpression(!noEmptyStrings));
            requiredAttribute.Arguments.Add(allowEmptyStringsArgument);
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
                specifiedMember = new CodeMemberField(typeof(bool), specifiedName + Specified + GetAccessors());
                specifiedMember.CustomAttributes.Add(ignoreAttribute);
                if (Configuration.EntityFramework && generateNullablesProperty) { specifiedMember.CustomAttributes.Add(notMappedAttribute); }
                specifiedMember.Attributes = MemberAttributes.Public;
                var specifiedDocs = new DocumentationModel[] {
                    new() { Language = English, Text = $"Gets or sets a value indicating whether the {Name} property is specified." },
                    new() { Language = German, Text = $"Ruft einen Wert ab, der angibt, ob die {Name}-Eigenschaft spezifiziert ist, oder legt diesen fest." }
                };
                specifiedMember.Comments.AddRange(GetComments(specifiedDocs).ToArray());
                typeDeclaration.Members.Add(specifiedMember);

                var specifiedMemberPropertyModel = new PropertyModel(Configuration, specifiedName + Specified, null, null);

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
                    [new CodeMethodReturnStatement(valueExpression)],
                    [new CodeMethodReturnStatement(new CodePrimitiveExpression(null))]);
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
        else if (isEnumerable && !IsRequired)
        {
            var canBeNull = Configuration.CollectionSettersMode is CollectionSettersMode.PublicWithoutConstructorInitialization or CollectionSettersMode.Public or CollectionSettersMode.Init or CollectionSettersMode.InitWithoutConstructorInitialization;

            if (canBeNull || !Configuration.SerializeEmptyCollections)
            {
                var listReference = new CodePropertyReferenceExpression(new CodeThisReferenceExpression(), Name);
                var collectionType = Configuration.CollectionImplementationType ?? Configuration.CollectionType;
                var countProperty = collectionType == typeof(Array) ? nameof(Array.Length) : nameof(List<int>.Count);
                var countReference = new CodePropertyReferenceExpression(listReference, countProperty);
                var notZeroExpression = new CodeBinaryOperatorExpression(countReference, CodeBinaryOperatorType.IdentityInequality, new CodePrimitiveExpression(0));
                var returnExpression = notZeroExpression;

                if (canBeNull)
                {
                    var notNullExpression = new CodeBinaryOperatorExpression(listReference, CodeBinaryOperatorType.IdentityInequality, new CodePrimitiveExpression(null));
                    var notNullOrEmptyExpression = new CodeBinaryOperatorExpression(notNullExpression, CodeBinaryOperatorType.BooleanAnd, notZeroExpression);
                    returnExpression = Configuration.SerializeEmptyCollections ? notNullExpression : notNullOrEmptyExpression;
                }

                var returnStatement = new CodeMethodReturnStatement(returnExpression);

                if (Configuration.UseShouldSerializePattern)
                {
                    var shouldSerializeMethod = new CodeMemberMethod
                    {
                        Attributes = MemberAttributes.Public,
                        Name = "ShouldSerialize" + Name,
                        ReturnType = new CodeTypeReference(typeof(bool)),
                        Statements = { returnStatement }
                    };

                    Configuration.MemberVisitor(shouldSerializeMethod, this);

                    typeDeclaration.Members.Add(shouldSerializeMethod);
                }
                else
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

                    specifiedProperty.GetStatements.Add(returnStatement);

                    var specifiedDocs = new DocumentationModel[] {
                    new() { Language = English, Text = $"Gets a value indicating whether the {Name} collection is empty." },
                    new() { Language = German, Text = $"Ruft einen Wert ab, der angibt, ob die {Name}-Collection leer ist." }
                };
                    specifiedProperty.Comments.AddRange(GetComments(specifiedDocs).ToArray());

                    Configuration.MemberVisitor(specifiedProperty, this);

                    typeDeclaration.Members.Add(specifiedProperty);
                }
            }
        }

        if (IsNullableReferenceType && Configuration.EnableNullableReferenceAttributes)
        {
            member.CustomAttributes.Add(new CodeAttributeDeclaration(CodeUtilities.CreateTypeReference(Attributes.AllowNull, Configuration)));
            member.CustomAttributes.Add(new CodeAttributeDeclaration(CodeUtilities.CreateTypeReference(Attributes.MaybeNull, Configuration)));
        }

        var attributes = GetAttributes(isArray).ToArray();
        member.CustomAttributes.AddRange(attributes);

        // initialize List<>
        if (isEnumerable && (Configuration.CollectionSettersMode != CollectionSettersMode.PublicWithoutConstructorInitialization)
            && (Configuration.CollectionSettersMode != CollectionSettersMode.InitWithoutConstructorInitialization))
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
            var arrayItemProperty = TypeClassModel.Properties[0];

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

            if (IsNillable && !(IsCollection && Type is SimpleModel m && m.ValueType.IsValueType) && (IsRequired || !Configuration.DoNotForceIsNullable))
                args.Add(new("IsNullable", new CodePrimitiveExpression(true)));

            if (Type is SimpleModel simpleModel && simpleModel.UseDataTypeAttribute)
            {
                // walk up the inheritance chain to find DataType if the simple type is derived (see #18)
                var xmlSchemaType = Type.XmlSchemaType;
                while (xmlSchemaType != null)
                {
                    var qualifiedName = xmlSchemaType.GetQualifiedName();

                    if (qualifiedName.Namespace == XmlSchema.Namespace && qualifiedName.Name != "anySimpleType")
                    {
                        args.Add(new("DataType", new CodePrimitiveExpression(qualifiedName.Name)));
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
    public List<DocumentationModel> Documentation { get; } = [];
}

public class EnumModel(GeneratorConfiguration configuration) : TypeModel(configuration)
{
    public List<EnumValueModel> Values { get; set; } = [];

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

public class SimpleModel(GeneratorConfiguration configuration) : TypeModel(configuration)
{
    public Type ValueType { get; set; }
    public List<RestrictionModel> Restrictions { get; } = [];
    public bool UseDataTypeAttribute { get; set; } = true;

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
            type = XmlSchemaType.Datatype.GetEffectiveType(Configuration, Restrictions, XmlSchemaType, attribute);
            UseDataTypeAttribute = XmlSchemaType.Datatype.IsDataTypeAttributeAllowed(Configuration) ?? UseDataTypeAttribute;
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
            type = XmlSchemaType.Datatype.GetEffectiveType(Configuration, Restrictions, XmlSchemaType, attribute);
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
                return new CodePrimitiveExpression(double.PositiveInfinity);
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
                new(new CodeTypeOfExpression(GetReferenceFor(Namespace))),
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

    protected IEnumerable<CodeCommentStatement> GetComments(IReadOnlyList<DocumentationModel> docs)
    {
        if (DisableComments || docs.Count == 0)
            yield break;

        yield return new CodeCommentStatement("<summary>", true);

        foreach (var doc in docs.Where
	            (
		            d => !string.IsNullOrWhiteSpace(d.Text)
		                 && (string.IsNullOrEmpty(d.Language)
		                     || Configuration.CommentLanguages.Count is 0
		                     || Configuration.CommentLanguages.Contains(d.Language)
		                     || Configuration.CommentLanguages
			                     .Any(l => d.Language.StartsWith(l, StringComparison.OrdinalIgnoreCase)))
	            )
	            .OrderBy(d => d.Language))
        {
	            var text = doc.Text;
	            var comment = $"<para{(string.IsNullOrEmpty(doc.Language) ? "" : $@" xml:lang=""{doc.Language}""")}>{CodeUtilities.NormalizeNewlines(text).Trim()}</para>";

	            yield return new(comment, true);
        }

        yield return new CodeCommentStatement("</summary>", true);
    }

    protected void AddDescription(CodeAttributeDeclarationCollection attributes, IReadOnlyList<DocumentationModel> docs)
    {
        if (!Configuration.GenerateDescriptionAttribute || DisableComments || docs.Count is 0) return;

        var docText = GetSingleDoc(docs);

        if (!string.IsNullOrWhiteSpace(docText))
        {
            var descriptionAttribute = AttributeDecl<DescriptionAttribute>(new CodeAttributeArgument(new CodePrimitiveExpression(Regex.Replace(docText, @"\s+", " ").Trim())));
            attributes.Add(descriptionAttribute);
        }
    }

    private string GetSingleDoc(IReadOnlyList<DocumentationModel> docs) => string.Join
    (
	        " ",
	        docs.Where
		        (
			        d => !string.IsNullOrWhiteSpace(d.Text)
			             && (string.IsNullOrEmpty(d.Language)
			                 || Configuration.CommentLanguages.Count is 0
			                 || Configuration.CommentLanguages.Contains(d.Language)
			                 || Configuration.CommentLanguages
				                 .Any(l => d.Language.StartsWith(l, StringComparison.OrdinalIgnoreCase)))
		        )
		        .Select(x => x.Text)
    );
}
