using System;
using System.CodeDom;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
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

        public NamespaceModel(NamespaceKey key)
        {
            Key = key;
            Types = new Dictionary<string, TypeModel>();
        }

        public CodeNamespace Generate()
        {
            var codeNamespace = new CodeNamespace(Name);
            codeNamespace.Imports.Add(new CodeNamespaceImport("System.Collections.ObjectModel"));
            codeNamespace.Imports.Add(new CodeNamespaceImport("System.Xml.Serialization"));

            foreach (var typeModel in Types.Values)
            {
                var type = typeModel.Generate();
                if (type != null)
                    codeNamespace.Types.Add(type);
            }

            return codeNamespace;
        }

        public string GetUniqueName(string name)
        {
            var n = name;
            var i = 2;

            while (Types.ContainsKey(n) && !(Types[n] is SimpleModel))
            {
                n = name + i;
                i++;
            }

            return n;
        }
    }

    public class DocumentationModel
    {
        public string Language { get; set; }
        public string Text { get; set; }

        public static IEnumerable<CodeCommentStatement> GetComments(IEnumerable<DocumentationModel> docs)
        {
            yield return new CodeCommentStatement("<summary>", true);

            foreach (var doc in docs.OrderBy(d => d.Language))
            {
                var comment = string.Format(@"<para{0}>{1}</para>", 
                    string.IsNullOrEmpty(doc.Language) ? "" : string.Format(@" xml:lang=""{0}""", doc.Language), 
                    Regex.Replace(doc.Text, @"(^|[^\r])\n", "$1\r\n")); // normalize newlines
                yield return new CodeCommentStatement(comment, true);
            }

            yield return new CodeCommentStatement("</summary>", true);
        }
    }

    public abstract class TypeModel
    {
        public NamespaceModel Namespace { get; set; }
        public XmlQualifiedName RootElementName { get; set; }
        public string Name { get; set; }
        public XmlQualifiedName XmlSchemaName { get; set; }
        public XmlSchemaType XmlSchemaType { get; set; }
        public static Dictionary<XmlQualifiedName, TypeModel> Types = new Dictionary<XmlQualifiedName, TypeModel>();
        public List<DocumentationModel> Documentation { get; private set; }
        public bool IsAnonymous { get; set; }

        public TypeModel()
        {
            Documentation = new List<DocumentationModel>();
        }

        public virtual CodeTypeDeclaration Generate()
        {
            var typeDeclaration = new CodeTypeDeclaration { Name = Name };

            typeDeclaration.Comments.AddRange(DocumentationModel.GetComments(Documentation).ToArray());

            var title = ((AssemblyTitleAttribute)Attribute.GetCustomAttribute(Assembly.GetExecutingAssembly(),
                typeof(AssemblyTitleAttribute))).Title;
            var version = Assembly.GetExecutingAssembly().GetName().Version.ToString();

            var serializableAttribute = new CodeAttributeDeclaration(new CodeTypeReference(typeof(SerializableAttribute)));
            typeDeclaration.CustomAttributes.Add(serializableAttribute);

            var generatedAttribute = new CodeAttributeDeclaration(new CodeTypeReference(typeof(GeneratedCodeAttribute)),
                new CodeAttributeArgument(new CodePrimitiveExpression(title)),
                new CodeAttributeArgument(new CodePrimitiveExpression(version)));
            typeDeclaration.CustomAttributes.Add(generatedAttribute);

            if (XmlSchemaName != null)
            {
                var typeAttribute = new CodeAttributeDeclaration(new CodeTypeReference(typeof(XmlTypeAttribute)),
                    new CodeAttributeArgument(new CodePrimitiveExpression(XmlSchemaName.Name)),
                    new CodeAttributeArgument("Namespace", new CodePrimitiveExpression(XmlSchemaName.Namespace)));
                if (IsAnonymous)
                    typeAttribute.Arguments.Add(new CodeAttributeArgument("AnonymousType", new CodePrimitiveExpression(true)));
                typeDeclaration.CustomAttributes.Add(typeAttribute);
            }

            return typeDeclaration;
        }

        public virtual CodeTypeReference GetReferenceFor(NamespaceModel referencingNamespace, bool collection)
        {
            var name = referencingNamespace == Namespace ? Name : string.Format("{0}.{1}", Namespace.Name, Name);
            if (collection) name = string.Format("Collection<{0}>", name);
            return new CodeTypeReference(name);
        }

        public virtual CodeExpression GetDefaultValueFor(string defaultString)
        {
            throw new NotSupportedException(string.Format("Getting default value for {0} not supported.", defaultString));
        }
    }

    public class ClassModel : TypeModel
    {
        public bool IsAbstract { get; set; }
        public TypeModel BaseClass { get; set; }
        public List<PropertyModel> Properties { get; set; }
        public List<ClassModel> DerivedTypes { get; set; }

        public ClassModel()
        {
            Properties = new List<PropertyModel>();
            DerivedTypes = new List<ClassModel>();
        }

        public override CodeTypeDeclaration Generate()
        {
            var classDeclaration = base.Generate();
            classDeclaration.IsClass = true;
            classDeclaration.IsPartial = true;

            if (BaseClass != null)
            {
                if (BaseClass is ClassModel)
                    classDeclaration.BaseTypes.Add(BaseClass.GetReferenceFor(Namespace, false));
                else
                {
                    var typeReference = BaseClass.GetReferenceFor(Namespace, collection: false);
                    var member = new CodeMemberField(typeReference, "Value");
                    // hack to generate automatic property
                    member.Name += " { get; set; }";
                    member.Attributes = (member.Attributes & ~MemberAttributes.AccessMask) | MemberAttributes.Public;
                    var docs = new[] { new DocumentationModel { Language = "en", Text = "Gets or sets the text value." },
                        new DocumentationModel { Language = "de", Text = "Ruft den Text ab oder legt diesen fest." } };
                    member.Comments.AddRange(DocumentationModel.GetComments(docs).ToArray());
                    member.CustomAttributes.Add(new CodeAttributeDeclaration(new CodeTypeReference(typeof(XmlTextAttribute))));
                    classDeclaration.Members.Add(member);
                }
            }

            foreach (var property in Properties)
                property.AddMembers(classDeclaration);

            classDeclaration.CustomAttributes.Insert(classDeclaration.CustomAttributes.Count - 1,
                new CodeAttributeDeclaration(new CodeTypeReference(typeof(DebuggerStepThroughAttribute))));
            classDeclaration.CustomAttributes.Insert(classDeclaration.CustomAttributes.Count - 1,
                new CodeAttributeDeclaration(new CodeTypeReference(typeof(DesignerCategoryAttribute)),
                    new CodeAttributeArgument(new CodePrimitiveExpression("code"))));

            if (RootElementName != null)
            {
                var rootAttribute = new CodeAttributeDeclaration(new CodeTypeReference(typeof(XmlRootAttribute)),
                    new CodeAttributeArgument(new CodePrimitiveExpression(RootElementName.Name)),
                    new CodeAttributeArgument("Namespace", new CodePrimitiveExpression(RootElementName.Namespace)));
                classDeclaration.CustomAttributes.Add(rootAttribute);
            }

            var derivedTypes = GetAllDerivedTypes();
            foreach (var derivedType in derivedTypes.OrderBy(t => t.Name))
            {
                var includeAttribute = new CodeAttributeDeclaration(new CodeTypeReference(typeof(XmlIncludeAttribute)),
                    new CodeAttributeArgument(new CodeTypeOfExpression(derivedType.GetReferenceFor(Namespace, false))));
                classDeclaration.CustomAttributes.Add(includeAttribute);
            }

            return classDeclaration;
        }

        public List<ClassModel> GetAllDerivedTypes()
        {
            var allDerivedTypes = new List<ClassModel>(DerivedTypes);

            foreach (var derivedType in DerivedTypes)
                allDerivedTypes.AddRange(derivedType.GetAllDerivedTypes());

            return allDerivedTypes;
        }

        public int TotalProperties
        {
            get
            {
                var elems = 0;
                var clss = this;

                while (clss != null)
                {
                    elems += clss.Properties.Count();
                    clss = clss.BaseClass as ClassModel;
                }

                return elems;
            }
        }
    }

    public class PropertyModel
    {
        public static bool GenerateNullables { get; set; }

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

        public PropertyModel()
        {
            Documentation = new List<DocumentationModel>();
        }

        public string ToCamelCase(string s)
        {
            if (string.IsNullOrEmpty(s)) return s;
            return char.ToLowerInvariant(s[0]) + s.Substring(1);
        }

        public void AddMembers(CodeTypeDeclaration typeDeclaration)
        {
            CodeTypeMember member = null;

            var typeClassModel = Type as ClassModel;
            var isArray = !IsAttribute && typeClassModel != null && typeClassModel.TotalProperties == 1;
            var propertyType = !isArray ? Type : typeClassModel.Properties[0].Type;
            var isNullableValueType = DefaultValue == null 
                && IsNullable && !(IsCollection || isArray) 
                && ((propertyType is EnumModel) || (propertyType is SimpleModel && ((SimpleModel)propertyType).ValueType.IsValueType));

            var typeReference = propertyType.GetReferenceFor(OwningType.Namespace, IsCollection || isArray);
            var simpleType = propertyType as SimpleModel;

            if (DefaultValue == null)
            {
                var propertyName = isNullableValueType && GenerateNullables ? Name + "Value" : Name;
                member = new CodeMemberField(typeReference, propertyName);
                // hack to generate automatic property
                member.Name += IsCollection || isArray ? " { get; private set; }" : " { get; set; }";
            }
            else
            {
                var backingField = new CodeMemberField(typeReference, "_" + ToCamelCase(Name));
                backingField.Attributes = (backingField.Attributes & ~MemberAttributes.AccessMask) | MemberAttributes.Private;
                var ignoreAttribute = new CodeAttributeDeclaration(new CodeTypeReference(typeof(XmlIgnoreAttribute)));
                backingField.CustomAttributes.Add(ignoreAttribute);

                var defaultValueExpression = propertyType.GetDefaultValueFor(DefaultValue);
                backingField.InitExpression = defaultValueExpression;

                typeDeclaration.Members.Add(backingField);

                var prop = new CodeMemberProperty { HasGet = true, HasSet = true, Name = Name, Type = typeReference };

                prop.GetStatements.Add(new CodeMethodReturnStatement(
                    new CodeFieldReferenceExpression(new CodeThisReferenceExpression(), backingField.Name)));
                prop.SetStatements.Add(new CodeAssignStatement(
                    new CodeFieldReferenceExpression(new CodeThisReferenceExpression(), backingField.Name),
                    new CodePropertySetValueReferenceExpression()));

                if (IsNullable)
                {
                    if (!(defaultValueExpression is CodeObjectCreateExpression))
                    {
                        var defaultValueAttribute = new CodeAttributeDeclaration(new CodeTypeReference(typeof(DefaultValueAttribute)),
                            new CodeAttributeArgument(defaultValueExpression));
                        prop.CustomAttributes.Add(defaultValueAttribute);
                    }
                }
                
                member = prop;
            }

            member.Attributes = (member.Attributes & ~MemberAttributes.AccessMask) | MemberAttributes.Public;
            typeDeclaration.Members.Add(member);

            var docs = new List<DocumentationModel>(Documentation);

            if (simpleType != null)
            {
                docs.AddRange(simpleType.Documentation);
                docs.AddRange(simpleType.Restrictions.Select(r => new DocumentationModel { Language = "en", Text = r.Description }));
                member.CustomAttributes.AddRange(simpleType.GetRestrictionAttributes().ToArray());
            }

            if (IsDeprecated)
            {
                // From .NET 3.5 XmlSerializer doesn't serialize objects with [Obsolete] >(
                //var deprecatedAttribute = new CodeAttributeDeclaration(new CodeTypeReference(typeof(ObsoleteAttribute)));
                //member.CustomAttributes.Add(deprecatedAttribute);
            }

            member.Comments.AddRange(DocumentationModel.GetComments(docs).ToArray());

            if (isNullableValueType)
            {
                var specifiedName = GenerateNullables ? Name + "Value" : Name;
                var specifiedMember = new CodeMemberField(typeof(bool), specifiedName + "Specified { get; set; }");
                var ignoreAttribute = new CodeAttributeDeclaration(new CodeTypeReference(typeof(XmlIgnoreAttribute)));
                specifiedMember.CustomAttributes.Add(ignoreAttribute);
                specifiedMember.Attributes = (specifiedMember.Attributes & ~MemberAttributes.AccessMask) | MemberAttributes.Public;
                var specifiedDocs = new[] { new DocumentationModel { Language = "en", Text = string.Format("Gets or sets a value indicating whether the {0} property is specified.", Name) },
                    new DocumentationModel { Language = "de", Text = string.Format("Ruft einen Wert ab, der angibt, ob die {0}-Eigenschaft spezifiziert ist, oder legt diesen fest.", Name) } };
                specifiedMember.Comments.AddRange(DocumentationModel.GetComments(specifiedDocs).ToArray());
                typeDeclaration.Members.Add(specifiedMember);

                if (GenerateNullables)
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

                    var nullableType = new CodeTypeReference(typeof(Nullable<>));
                    nullableType.TypeArguments.Add(typeReference);
                    var nullableMember = new CodeMemberProperty
                    {
                        Type = nullableType,
                        Name = Name,
                        HasSet = true,
                        HasGet = true,
                    };
                    nullableMember.Attributes = (nullableMember.Attributes & ~MemberAttributes.AccessMask) | MemberAttributes.Public;
                    nullableMember.CustomAttributes.Add(ignoreAttribute);
                    nullableMember.Comments.AddRange(member.Comments);

                    var specifiedExpression = new CodePropertyReferenceExpression(new CodeThisReferenceExpression(), specifiedName + "Specified");
                    var valueExpression = new CodePropertyReferenceExpression(new CodeThisReferenceExpression(), Name + "Value");
                    var conditionStatement = new CodeConditionStatement(specifiedExpression,
                        new[] { new CodeMethodReturnStatement(valueExpression) },
                        new[] { new CodeMethodReturnStatement(new CodePrimitiveExpression(null)) });
                    nullableMember.GetStatements.Add(conditionStatement);

                    var getValueOrDefaultExpression = new CodeMethodInvokeExpression(new CodePropertySetValueReferenceExpression(), "GetValueOrDefault");
                    var setValueStatement = new CodeAssignStatement(valueExpression, getValueOrDefaultExpression);
                    var hasValueExpression = new CodePropertyReferenceExpression(new CodePropertySetValueReferenceExpression(), "HasValue");
                    var setSpecifiedStatement = new CodeAssignStatement(specifiedExpression, hasValueExpression);
                    nullableMember.SetStatements.AddRange(new[] { setValueStatement, setSpecifiedStatement });

                    typeDeclaration.Members.Add(nullableMember);

                    var editorBrowsableAttribute = new CodeAttributeDeclaration(new CodeTypeReference(typeof(EditorBrowsableAttribute)));
                    editorBrowsableAttribute.Arguments.Add(new CodeAttributeArgument(new CodeFieldReferenceExpression(new CodeTypeReferenceExpression(typeof(EditorBrowsableState)), "Never")));
                    specifiedMember.CustomAttributes.Add(editorBrowsableAttribute);
                    member.CustomAttributes.Add(editorBrowsableAttribute);
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
                var ignoreAttribute = new CodeAttributeDeclaration(new CodeTypeReference(typeof(XmlIgnoreAttribute)));
                specifiedProperty.CustomAttributes.Add(ignoreAttribute);
                specifiedProperty.Attributes = (specifiedProperty.Attributes & ~MemberAttributes.AccessMask) | MemberAttributes.Public;

                var listReference = new CodePropertyReferenceExpression(new CodeThisReferenceExpression(), Name);
                var countReference = new CodePropertyReferenceExpression(listReference, "Count");
                var notZeroExpression = new CodeBinaryOperatorExpression(countReference, CodeBinaryOperatorType.IdentityInequality, new CodePrimitiveExpression(0));
                var returnStatement = new CodeMethodReturnStatement(notZeroExpression);
                specifiedProperty.GetStatements.Add(returnStatement);

                var specifiedDocs = new[] { new DocumentationModel { Language = "en", Text = string.Format("Gets a value indicating whether the {0} collection is empty.", Name) },
                    new DocumentationModel { Language = "de", Text = string.Format("Ruft einen Wert ab, der angibt, ob die {0}-Collection leer ist.", Name) } };
                specifiedProperty.Comments.AddRange(DocumentationModel.GetComments(specifiedDocs).ToArray());

                typeDeclaration.Members.Add(specifiedProperty);
            }

            var attribute = GetAttribute(isArray);
            member.CustomAttributes.Add(attribute);

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
                var listReference = new CodePropertyReferenceExpression(new CodeThisReferenceExpression(), Name);
                var initExpression = new CodeObjectCreateExpression(typeReference, new CodeExpression[] { });
                constructor.Statements.Add(new CodeAssignStatement(listReference, initExpression));
            }

            if (isArray)
            {
                var arrayItemProperty = typeClassModel.Properties[0];
                var propertyAttribute = arrayItemProperty.GetAttribute(false);
                // HACK: repackage as ArrayItemAttribute
                var arrayItemAttribute = new CodeAttributeDeclaration(new CodeTypeReference(typeof(XmlArrayItemAttribute)),
                    propertyAttribute.Arguments.Cast<CodeAttributeArgument>().ToArray());
                member.CustomAttributes.Add(arrayItemAttribute);
            }
        }

        private CodeAttributeDeclaration GetAttribute(bool isArray)
        {
            CodeAttributeDeclaration attribute;
            if (IsAttribute)
            {
                if (IsAny)
                {
                    attribute = new CodeAttributeDeclaration(new CodeTypeReference(typeof(XmlAnyAttributeAttribute)));
                }
                else
                {
                    attribute = new CodeAttributeDeclaration(new CodeTypeReference(typeof(XmlAttributeAttribute)),
                        new CodeAttributeArgument(new CodePrimitiveExpression(XmlSchemaName.Name)));
                }
            }
            else if (!isArray)
            {
                if (IsAny)
                {
                    attribute = new CodeAttributeDeclaration(new CodeTypeReference(typeof(XmlAnyElementAttribute)));
                }
                else
                {
                    attribute = new CodeAttributeDeclaration(new CodeTypeReference(typeof(XmlElementAttribute)),
                        new CodeAttributeArgument(new CodePrimitiveExpression(XmlSchemaName.Name)));
                }
            }
            else
            {
                attribute = new CodeAttributeDeclaration(new CodeTypeReference(typeof(XmlArrayAttribute)),
                    new CodeAttributeArgument(new CodePrimitiveExpression(XmlSchemaName.Name)));
            }

            if (Form == XmlSchemaForm.Qualified)
            {
                attribute.Arguments.Add(new CodeAttributeArgument("Namespace", new CodePrimitiveExpression(OwningType.XmlSchemaName.Namespace)));
            }
            else if (XmlNamespace != null)
            {
                attribute.Arguments.Add(new CodeAttributeArgument("Namespace", new CodePrimitiveExpression(XmlNamespace)));
            }
            else if (!IsAny)
            {
                attribute.Arguments.Add(new CodeAttributeArgument("Form",
                    new CodeFieldReferenceExpression(new CodeTypeReferenceExpression(new CodeTypeReference(typeof(XmlSchemaForm))),
                        "Unqualified")));
            }

            if (IsNillable)
                attribute.Arguments.Add(new CodeAttributeArgument("IsNullable", new CodePrimitiveExpression(true)));

            var simpleModel = Type as SimpleModel;
            if (simpleModel != null && simpleModel.UseDataTypeAttribute)
            {
                var name = Type.XmlSchemaType.QualifiedName.IsEmpty ? Type.XmlSchemaType.BaseXmlSchemaType.QualifiedName : Type.XmlSchemaType.QualifiedName;
                if (name.Namespace == XmlSchema.Namespace)
                {
                    var dataType = new CodeAttributeArgument("DataType", new CodePrimitiveExpression(name.Name));
                    attribute.Arguments.Add(dataType);
                }
            }

            return attribute;
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

        public EnumModel()
        {
            Values = new List<EnumValueModel>();
        }

        public override CodeTypeDeclaration Generate()
        {
            var enumDeclaration = base.Generate();
            enumDeclaration.IsEnum = true;

            foreach (var val in Values)
            {
                var member = new CodeMemberField { Name = val.Name };
                var docs = new List<DocumentationModel>(val.Documentation);

                if (val.Name != val.Value) // illegal identifier chars in value
                {
                    var enumAttribute = new CodeAttributeDeclaration(new CodeTypeReference(typeof(XmlEnumAttribute)),
                        new CodeAttributeArgument(new CodePrimitiveExpression(val.Value)));
                    member.CustomAttributes.Add(enumAttribute);
                }

                if (val.IsDeprecated)
                {
                    // From .NET 3.5 XmlSerializer doesn't serialize objects with [Obsolete] >(
                    //var deprecatedAttribute = new CodeAttributeDeclaration(new CodeTypeReference(typeof(ObsoleteAttribute)));
                    //member.CustomAttributes.Add(deprecatedAttribute);
                
                    var obsolete = new DocumentationModel { Language = "en", Text = string.Format("[Obsolete]", Name) };
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
        public static Type IntegerDataType { get; set; }
        public Type ValueType { get; set; }
        public List<RestrictionModel> Restrictions { get; private set; }
        public bool UseDataTypeAttribute { get; set; }

        public SimpleModel()
        {
            Restrictions = new List<RestrictionModel>();
            UseDataTypeAttribute = true;
        }

        public override CodeTypeDeclaration Generate()
        {
            return null;
        }

        public override CodeTypeReference GetReferenceFor(NamespaceModel referencingNamespace, bool collection)
        {
            var type = ValueType;

            if (XmlSchemaType != null)
            {
                // some types are not mapped in the same way between XmlSerializer and XmlSchema >(
                // http://msdn.microsoft.com/en-us/library/aa719879(v=vs.71).aspx
                // http://msdn.microsoft.com/en-us/library/system.xml.serialization.xmlelementattribute.datatype(v=vs.110).aspx
                // XmlSerializer is inconsistent: maps xs:decimal to decimal but xs:integer to string,
                // even though xs:integer is a restriction of xs:decimal
                switch (XmlSchemaType.TypeCode)
                {
                    case XmlTypeCode.AnyAtomicType:
                        // union
                        type = typeof(string);
                        UseDataTypeAttribute = false;
                        break;
                    case XmlTypeCode.AnyUri:
                    case XmlTypeCode.Duration:
                    case XmlTypeCode.GDay:
                    case XmlTypeCode.GMonth:
                    case XmlTypeCode.GMonthDay:
                    case XmlTypeCode.GYear:
                    case XmlTypeCode.GYearMonth:
                    case XmlTypeCode.Time:
                        type = XmlSchemaType.Datatype.Variety == XmlSchemaDatatypeVariety.List ? typeof(string[]) : typeof(string);
                        break;
                    case XmlTypeCode.Integer:
                    case XmlTypeCode.NegativeInteger:
                    case XmlTypeCode.NonNegativeInteger:
                    case XmlTypeCode.NonPositiveInteger:
                    case XmlTypeCode.PositiveInteger:
                        if (IntegerDataType == null || IntegerDataType == typeof(string)) type = typeof(string);
                        else
                        {
                            type = IntegerDataType;
                            UseDataTypeAttribute = false;
                        }
                        break;
                }
            }

            if (collection) type = typeof(Collection<>).MakeGenericType(type);

            return new CodeTypeReference(type);
        }

        public override CodeExpression GetDefaultValueFor(string defaultString)
        {
            if (ValueType == typeof(XmlQualifiedName))
            {
                if (defaultString.StartsWith("xs:", StringComparison.OrdinalIgnoreCase))
                {
                    return new CodeObjectCreateExpression(typeof(XmlQualifiedName),
                        new CodeExpression[] { new CodePrimitiveExpression(defaultString.Substring(3)), 
                            new CodePrimitiveExpression(XmlSchema.Namespace) });
                }
                else throw new NotSupportedException(string.Format("Resolving default value {0} for QName not supported.", defaultString));
            }
            else return new CodePrimitiveExpression(Convert.ChangeType(defaultString, ValueType));
        }

        public IEnumerable<CodeAttributeDeclaration> GetRestrictionAttributes()
        {
            foreach (var attribute in Restrictions.Select(r => r.GetAttribute()).Where(a => a != null))
            {
                yield return attribute;
            }

            var minInclusive = Restrictions.OfType<MinInclusiveRestrictionModel>().FirstOrDefault();
            var maxInclusive = Restrictions.OfType<MaxInclusiveRestrictionModel>().FirstOrDefault();

            if (minInclusive != null && maxInclusive != null)
                yield return new CodeAttributeDeclaration(new CodeTypeReference(typeof(RangeAttribute)),
                    new CodeAttributeArgument(new CodeTypeOfExpression(minInclusive.Type)),
                    new CodeAttributeArgument(new CodePrimitiveExpression(minInclusive.Value)),
                    new CodeAttributeArgument(new CodePrimitiveExpression(maxInclusive.Value)));
        }
    }
}
