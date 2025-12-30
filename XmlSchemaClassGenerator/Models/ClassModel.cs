using System;
using System.CodeDom;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Xml.Schema;
using System.Xml.Serialization;

namespace XmlSchemaClassGenerator;

public class ClassModel(GeneratorConfiguration configuration) : ReferenceTypeModel(configuration)
{
    public override bool IsRedefined => DerivedTypes.Exists(d => d.XmlSchemaType?.Parent is XmlSchemaRedefine);
    public bool IsAbstract { get; set; }
    public bool IsMixed { get; set; }
    public bool IsSubstitution { get; set; }
    public TypeModel BaseClass { get; set; }
    public TypeModel TextValueType { get; set; }
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

                // When a derived class has a simpleContent restriction with enum facets (TextValueType != null),
                // we generate an adapter property that allows strongly-typed access to the enum values.
                // We cannot add a new XmlText property because the XmlSerializer doesn't allow it when
                // the base class already has one. Instead, we use an [XmlIgnore] adapter property.
                if (TextValueType != null && !string.IsNullOrEmpty(Configuration.TextValuePropertyName))
                {
                    var textName = Configuration.TextValuePropertyName;
                    var enumTypeReference = TextValueType.GetReferenceFor(Namespace);
                    var nullableEnumTypeReference = new CodeTypeReference(typeof(Nullable<>));
                    nullableEnumTypeReference.TypeArguments.Add(enumTypeReference);

                    // Create the EnumValue adapter property
                    var enumValueProperty = new CodeMemberProperty
                    {
                        Name = "EnumValue",
                        Type = nullableEnumTypeReference,
                        Attributes = MemberAttributes.Public,
                        HasGet = true,
                        HasSet = true
                    };

                    // Add [XmlIgnore] attribute
                    var ignoreAttribute = AttributeDecl<XmlIgnoreAttribute>();
                    enumValueProperty.CustomAttributes.Add(ignoreAttribute);

                    // Getter: Try to parse the Value property to enum
                    // if (Enum.TryParse(typeof(EnumType), Value, true, out var result))
                    //     return (EnumType)result;
                    // return null;
                    var resultVariable = new CodeVariableDeclarationStatement(typeof(object), "result");
                    var tryParseCondition = new CodeMethodInvokeExpression(
                        new CodeTypeReferenceExpression(typeof(Enum)),
                        "TryParse",
                        new CodeTypeOfExpression(enumTypeReference),
                        new CodePropertyReferenceExpression(
                            new CodeThisReferenceExpression(),
                            textName),
                        new CodePrimitiveExpression(true),
                        new CodeDirectionExpression(FieldDirection.Out, new CodeVariableReferenceExpression("result")));

                    var returnCastResult = new CodeMethodReturnStatement(
                        new CodeCastExpression(
                            nullableEnumTypeReference,
                            new CodeVariableReferenceExpression("result")));

                    var ifTryParse = new CodeConditionStatement(
                        tryParseCondition,
                        returnCastResult);

                    enumValueProperty.GetStatements.Add(resultVariable);
                    enumValueProperty.GetStatements.Add(ifTryParse);
                    enumValueProperty.GetStatements.Add(new CodeMethodReturnStatement(new CodePrimitiveExpression(null)));

                    // Setter: Value = value?.ToString();
                    // Since CodeDOM doesn't support null-conditional operator, we need to check and set
                    var valueNotNull = new CodeBinaryOperatorExpression(
                        new CodePropertySetValueReferenceExpression(),
                        CodeBinaryOperatorType.IdentityInequality,
                        new CodePrimitiveExpression(null));

                    var setToString = new CodeAssignStatement(
                        new CodePropertyReferenceExpression(
                            new CodeThisReferenceExpression(),
                            textName),
                        new CodeMethodInvokeExpression(
                            new CodePropertySetValueReferenceExpression(),
                            "ToString"));

                    var setToNull = new CodeAssignStatement(
                        new CodePropertyReferenceExpression(
                            new CodeThisReferenceExpression(),
                            textName),
                        new CodePrimitiveExpression(null));

                    enumValueProperty.SetStatements.Add(
                        new CodeConditionStatement(
                            valueNotNull,
                            new CodeStatement[] { setToString },
                            new CodeStatement[] { setToNull }));

                    var docs = new List<DocumentationModel> {
                        new() { Language = English, Text = "Gets or sets the typed value of the text content." },
                        new() { Language = German, Text = "Ruft den typisierten Wert des Textinhalts ab oder legt diesen fest." }
                    };

                    enumValueProperty.Comments.AddRange(GetComments(docs).ToArray());

                    classDeclaration.Members.Add(enumValueProperty);

                    var enumValuePropertyModel = new PropertyModel(Configuration, "EnumValue", TextValueType, this);
                    Configuration.MemberVisitor(enumValueProperty, enumValuePropertyModel);
                }
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

        if (!Configuration.OmitXmlIncludeAttribute)
        {
            var derivedTypes = GetAllDerivedTypes();
            foreach (var derivedType in derivedTypes.OrderBy(t => t.Name))
                customAttributes.Add(AttributeDecl<XmlIncludeAttribute>(new CodeAttributeArgument(new CodeTypeOfExpression(derivedType.GetReferenceFor(Namespace)))));
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

        if (rootClass is SimpleModel || rootClass is EnumModel)
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
