using System;
using System.CodeDom;
using System.Collections.Generic;
using System.Linq;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Schema;
using System.Xml.Serialization;

namespace XmlSchemaClassGenerator
{
    internal class ModelBuilder
    {
        private readonly GeneratorConfiguration _configuration;
        private readonly XmlSchemaSet _set;
        private readonly Dictionary<XmlQualifiedName, HashSet<XmlSchemaAttributeGroup>> AttributeGroups = new();
        private readonly Dictionary<XmlQualifiedName, HashSet<XmlSchemaGroup>> Groups = new();
        private readonly Dictionary<NamespaceKey, NamespaceModel> Namespaces = new();
        private readonly Dictionary<string, TypeModel> Types = new();
        private readonly Dictionary<XmlQualifiedName, HashSet<Substitute>> SubstitutionGroups = new();

        private static readonly XmlQualifiedName AnyType = new("anyType", XmlSchema.Namespace);

        private string BuildKey(XmlSchemaAnnotated annotated, XmlQualifiedName name)
            => $"{annotated.GetType()}:{annotated.SourceUri}:{annotated.LineNumber}:{annotated.LinePosition}:{name}";

        private void SetType(XmlSchemaAnnotated annotated, XmlQualifiedName name, TypeModel type)
            => Types[BuildKey(annotated, name)] = type;

        public ModelBuilder(GeneratorConfiguration configuration, XmlSchemaSet set)
        {
            _configuration = configuration;
            _set = set;

            GeneratorModel.DisableComments = _configuration.DisableComments;
            var objectModel = new SimpleModel(_configuration)
            {
                Name = "AnyType",
                Namespace = CreateNamespaceModel(new Uri(XmlSchema.Namespace), AnyType),
                XmlSchemaName = AnyType,
                XmlSchemaType = null,
                ValueType = typeof(object),
                UseDataTypeAttribute = false
            };

            SetType(new XmlSchemaComplexType(), AnyType, objectModel);

            var dependencyOrder = new List<XmlSchema>();
            var seenSchemas = new HashSet<XmlSchema>();
            foreach (var schema in set.Schemas().Cast<XmlSchema>())
                ResolveDependencies(schema, dependencyOrder, seenSchemas);

            foreach (var schema in dependencyOrder)
            {
                var currentAttributeGroups = schema.AttributeGroups.Values.Cast<XmlSchemaAttributeGroup>()
                    .DistinctBy(g => g.QualifiedName.ToString());

                foreach (var currentAttributeGroup in currentAttributeGroups)
                {
                    if (!AttributeGroups.ContainsKey(currentAttributeGroup.QualifiedName))
                    {
                        AttributeGroups.Add(currentAttributeGroup.QualifiedName, new HashSet<XmlSchemaAttributeGroup>());
                    }

                    AttributeGroups[currentAttributeGroup.QualifiedName].Add(currentAttributeGroup);
                }

                var currentSchemaGroups = schema.Groups.Values.Cast<XmlSchemaGroup>()
                    .DistinctBy(g => g.QualifiedName.ToString());

                foreach (var currentSchemaGroup in currentSchemaGroups)
                {
                    if (!Groups.ContainsKey(currentSchemaGroup.QualifiedName))
                    {
                        Groups.Add(currentSchemaGroup.QualifiedName, new HashSet<XmlSchemaGroup>());
                    }

                    Groups[currentSchemaGroup.QualifiedName].Add(currentSchemaGroup);
                }
            }

            foreach (var schema in dependencyOrder)
            {
                foreach (var globalType in set.GlobalTypes.Values.Cast<XmlSchemaType>().Where(s => s.GetSchema() == schema))
                    CreateTypeModel(globalType.QualifiedName, globalType);

                foreach (var rootElement in set.GlobalElements.Values.Cast<XmlSchemaElement>().Where(s => s.GetSchema() == schema))
                    CreateElement(rootElement);
            }

            CreateSubstitutes();

            if (configuration.GenerateInterfaces)
            {
                PromoteInterfacePropertiesToCollection();
                RenameInterfacePropertiesIfRenamedInDerivedClasses();
                RemoveDuplicateInterfaceProperties();
            }

            AddXmlRootAttributeToAmbiguousTypes();

            if (_configuration.UniqueTypeNameAcrossNamespaces)
                CreateUniqueTypeNames();
        }

        private void CreateUniqueTypeNames()
        {
            foreach (var types in Namespaces.Values.SelectMany(n => n.Types.Values).GroupBy(t => t.Name))
            {
                var i = 2;

                foreach (var t in types.Skip(1))
                {
                    t.Name += $"_{i}";
                    i++;
                }
            }
        }

        private void CreateSubstitutes()
        {
            var classesProps = Types.Values.OfType<ClassModel>().Select(c => c.Properties.ToList()).ToList();

            foreach (var classProps in classesProps)
            {
                var order = 0;

                foreach (var prop in classProps)
                {
                    if (_configuration.EmitOrder)
                    {
                        prop.Order = order;
                        order++;
                    }

                    if (prop.XmlSchemaName != null)
                    {
                        var substitutes = GetSubstitutedElements(prop.XmlSchemaName);

                        if (_configuration.SeparateSubstitutes)
                        {
                            foreach (var substitute in substitutes)
                            {
                                var cls = (ClassModel)prop.OwningType;
                                var schema = substitute.Element.GetSchema();
                                var source = CodeUtilities.CreateUri(schema.SourceUri);
                                var props = CreatePropertiesForElements(source, cls, prop.Particle, new[] { prop.Particle }, substitute, order);

                                cls.Properties.AddRange(props);

                                order += props.Count();
                            }
                        }
                        else
                        {
                            prop.Substitutes.AddRange(substitutes);
                        }
                    }
                }
            }
        }

        private void AddXmlRootAttributeToAmbiguousTypes()
        {
            var ambiguousTypes = Types.Values.Where(t => t.RootElementName == null && !t.IsAbstractRoot && t is not InterfaceModel).GroupBy(t => t.Name);
            foreach (var ambiguousTypeGroup in ambiguousTypes)
            {
                var types = ambiguousTypeGroup.ToList();
                if (types.Count == 1)
                {
                    continue;
                }
                foreach (var typeModel in types)
                    typeModel.RootElementName = typeModel.GetQualifiedName();
            }
        }

        private void RemoveDuplicateInterfaceProperties()
        {
            foreach (var interfaceModel in Types.Values.OfType<InterfaceModel>())
            {
                var parentProperties = interfaceModel.Properties.ToList();
                foreach (var baseInterfaceTypeProperties in interfaceModel.AllDerivedReferenceTypes().OfType<InterfaceModel>().Select(i => i.Properties))
                {
                    foreach (var parentProperty in parentProperties)
                    {
                        var baseProperties = baseInterfaceTypeProperties.ToList();
                        foreach (var baseProperty in baseProperties.Where(baseProperty => parentProperty.Name == baseProperty.Name && parentProperty.Type.Name == baseProperty.Type.Name))
                            baseInterfaceTypeProperties.Remove(baseProperty);
                    }
                }
            }
        }

        private void RenameInterfacePropertiesIfRenamedInDerivedClasses()
        {
            foreach (var interfaceModel in Types.Values.OfType<InterfaceModel>())
            {
                foreach (var interfaceProperty in interfaceModel.Properties)
                {
                    foreach (var implementationClass in interfaceModel.AllDerivedReferenceTypes())
                    {
                        foreach (var implementationClassProperty in implementationClass.Properties)
                        {
                            if (implementationClassProperty.Name != implementationClassProperty.OriginalPropertyName
                                && implementationClassProperty.OriginalPropertyName == interfaceProperty.Name
                                && implementationClassProperty.XmlSchemaName == interfaceProperty.XmlSchemaName
                                && implementationClassProperty.IsAttribute == interfaceProperty.IsAttribute)
                            {
                                RenameInterfacePropertyInBaseClasses(interfaceModel, implementationClass, interfaceProperty, implementationClassProperty.Name);
                                interfaceProperty.Name = implementationClassProperty.Name;
                            }
                        }
                    }
                }
            }
        }

        private void PromoteInterfacePropertiesToCollection()
        {
            foreach (var interfaceModel in Types.Values.OfType<InterfaceModel>())
            {
                foreach (var interfaceProperty in interfaceModel.Properties)
                {
                    var derivedProperties = interfaceModel.AllDerivedReferenceTypes().SelectMany(t => t.Properties)
                        .Where(p => p.Name == interfaceProperty.Name || p.OriginalPropertyName == interfaceProperty.Name).ToList();

                    if (derivedProperties.Exists(p => p.IsCollection))
                    {
                        foreach (var derivedProperty in derivedProperties.Where(p => !p.IsCollection))
                            derivedProperty.IsCollection = true;

                        interfaceProperty.IsCollection = true;
                    }
                }
            }
        }

        private static void RenameInterfacePropertyInBaseClasses(InterfaceModel interfaceModel, ReferenceTypeModel implementationClass,
            PropertyModel interfaceProperty, string newName)
        {
            foreach (var derivedClass in interfaceModel.AllDerivedReferenceTypes().Where(c => c != implementationClass))
            {
                foreach (var propertyModel in derivedClass.Properties.Where(p => p.Name == interfaceProperty.Name))
                    propertyModel.Name = newName;
            }
        }

        private void ResolveDependencies(XmlSchema schema, List<XmlSchema> dependencyOrder, HashSet<XmlSchema> seenSchemas)
        {
            if (seenSchemas.Contains(schema))
                return;

            seenSchemas.Add(schema);

            var imports = schema.Includes.OfType<XmlSchemaExternal>();

            if (imports.Any())
            {
                foreach (var importSchema in imports.Select(i => i.Schema))
                {
                    if (importSchema != null)
                        ResolveDependencies(importSchema, dependencyOrder, seenSchemas);
                }
            }

            dependencyOrder.Add(schema);
        }

        private void CreateElement(XmlSchemaElement rootElement)
        {
            var qualifiedName = rootElement.ElementSchemaType.QualifiedName;
            if (qualifiedName.IsEmpty)
                qualifiedName = rootElement.QualifiedName;
            var type = CreateTypeModel(qualifiedName, rootElement.ElementSchemaType);
            ClassModel derivedClassModel = null;

            if (type.RootElementName != null || type.IsAbstractRoot)
            {
                if (type is ClassModel classModel)
                    derivedClassModel = CreateDerivedRootClass(rootElement, type, classModel);
                else
                    SetType(rootElement, rootElement.QualifiedName, type);
            }
            else
            {
                if (type is ClassModel classModel)
                    classModel.Documentation.AddRange(GetDocumentation(rootElement));

                type.RootElement = rootElement;
                type.RootElementName = rootElement.QualifiedName;
            }

            if (!rootElement.SubstitutionGroup.IsEmpty)
            {
                if (!SubstitutionGroups.TryGetValue(rootElement.SubstitutionGroup, out var substitutes))
                {
                    substitutes = new HashSet<Substitute>();
                    SubstitutionGroups.Add(rootElement.SubstitutionGroup, substitutes);
                }

                substitutes.Add(new Substitute { Element = rootElement, Type = derivedClassModel ?? type });
            }
        }

        private ClassModel CreateDerivedRootClass(XmlSchemaElement rootElement, TypeModel type, ClassModel classModel)
        {
            ClassModel derivedClassModel;
            // There is already another global element with this type.
            // Need to create an empty derived class.

            var elementSource = CodeUtilities.CreateUri(rootElement.SourceUri);

            derivedClassModel = new ClassModel(_configuration)
            {
                Name = _configuration.NamingProvider.RootClassNameFromQualifiedName(rootElement.QualifiedName, rootElement),
                Namespace = CreateNamespaceModel(elementSource, rootElement.QualifiedName)
            };

            derivedClassModel.Documentation.AddRange(GetDocumentation(rootElement));

            if (derivedClassModel.Namespace != null)
            {
                derivedClassModel.Name = derivedClassModel.Namespace.GetUniqueTypeName(derivedClassModel.Name);
                derivedClassModel.Namespace.Types[derivedClassModel.Name] = derivedClassModel;
            }

            SetType(rootElement, rootElement.QualifiedName, derivedClassModel);

            derivedClassModel.BaseClass = classModel;
            ((ClassModel)derivedClassModel.BaseClass).DerivedTypes.Add(derivedClassModel);

            derivedClassModel.RootElementName = rootElement.QualifiedName;

            if (!type.IsAbstractRoot)
                CreateOriginalRootClass(rootElement, type, classModel);

            return derivedClassModel;
        }

        private void CreateOriginalRootClass(XmlSchemaElement rootElement, TypeModel type, ClassModel classModel)
        {
            // Also create an empty derived class for the original root element

            var originalClassModel = new ClassModel(_configuration)
            {
                Name = _configuration.NamingProvider.RootClassNameFromQualifiedName(type.RootElementName, rootElement),
                Namespace = classModel.Namespace
            };

            originalClassModel.Documentation.AddRange(classModel.Documentation);
            classModel.Documentation.Clear();

            if (originalClassModel.Namespace != null)
            {
                originalClassModel.Name = originalClassModel.Namespace.GetUniqueTypeName(originalClassModel.Name);
                originalClassModel.Namespace.Types[originalClassModel.Name] = originalClassModel;
            }

            if (classModel.XmlSchemaName?.IsEmpty == false)
                SetType(classModel.RootElement, classModel.XmlSchemaName, originalClassModel);

            originalClassModel.BaseClass = classModel;
            ((ClassModel)originalClassModel.BaseClass).DerivedTypes.Add(originalClassModel);

            originalClassModel.RootElementName = type.RootElementName;

            if (classModel.RootElement.SubstitutionGroup != null
                && SubstitutionGroups.TryGetValue(classModel.RootElement.SubstitutionGroup, out var substitutes))
            {
                foreach (var substitute in substitutes.Where(s => s.Element == classModel.RootElement))
                    substitute.Type = originalClassModel;
            }

            classModel.RootElementName = null;
            classModel.IsAbstractRoot = true;
        }

        private IEnumerable<Substitute> GetSubstitutedElements(XmlQualifiedName name)
        {
            if (SubstitutionGroups.TryGetValue(name, out var substitutes))
            {
                foreach (var substitute in substitutes.Where(s => s.Element.QualifiedName != name))
                {
                    yield return substitute;
                    foreach (var recursiveSubstitute in GetSubstitutedElements(substitute.Element.QualifiedName))
                        yield return recursiveSubstitute;
                }
            }
        }

        private TypeModel CreateTypeModel(XmlQualifiedName qualifiedName, XmlSchemaAnnotated type)
        {
            var key = BuildKey(type, qualifiedName);
            if (!qualifiedName.IsEmpty && Types.TryGetValue(key, out TypeModel typeModel)) return typeModel;

            var source = CodeUtilities.CreateUri(type.SourceUri);
            var namespaceModel = CreateNamespaceModel(source, qualifiedName);
            var docs = GetDocumentation(type);

            var typeModelBuilder = new TypeModelBuilder(this, _configuration, qualifiedName, namespaceModel, docs, source);

            return typeModelBuilder.Create(type);
        }

        private sealed class TypeModelBuilder
        {
            private readonly ModelBuilder builder;
            private readonly GeneratorConfiguration _configuration;
            private readonly XmlQualifiedName qualifiedName;
            private readonly NamespaceModel namespaceModel;
            private readonly List<DocumentationModel> docs;
            private readonly Uri source;

            public TypeModelBuilder(ModelBuilder builder, GeneratorConfiguration configuration, XmlQualifiedName qualifiedName, NamespaceModel namespaceModel, List<DocumentationModel> docs, Uri source)
            {
                this.builder = builder;
                _configuration = configuration;
                this.qualifiedName = qualifiedName;
                this.namespaceModel = namespaceModel;
                this.docs = docs;
                this.source = source;
            }

            internal TypeModel Create(XmlSchemaAnnotated type) => type switch
            {
                XmlSchemaGroup group => CreateTypeModel(group),
                XmlSchemaAttributeGroup attributeGroup => CreateTypeModel(attributeGroup),
                XmlSchemaComplexType complexType => CreateTypeModel(complexType),
                XmlSchemaSimpleType simpleType => CreateTypeModel(simpleType),
                _ => throw new NotSupportedException($"Cannot build declaration for {qualifiedName}"),
            };

            private InterfaceModel CreateInterfaceModel(XmlSchemaAnnotated group, string name)
            {
                if (namespaceModel != null)
                    name = namespaceModel.GetUniqueTypeName(name);

                var interfaceModel = new InterfaceModel(_configuration)
                {
                    Name = name,
                    Namespace = namespaceModel,
                    XmlSchemaName = qualifiedName
                };

                interfaceModel.Documentation.AddRange(docs);

                if (namespaceModel != null)
                    namespaceModel.Types[name] = interfaceModel;

                if (!qualifiedName.IsEmpty)
                    builder.SetType(group, qualifiedName, interfaceModel);
                return interfaceModel;
            }

            private TypeModel CreateTypeModel(XmlSchemaGroup group)
            {
                var name = "I" + _configuration.NamingProvider.GroupTypeNameFromQualifiedName(qualifiedName, group);

                InterfaceModel interfaceModel = CreateInterfaceModel(group, name);

                var xmlParticle = group.Particle;
                var particle = new Particle(xmlParticle, group.Parent);
                var items = builder.GetElements(xmlParticle);
                var properties = builder.CreatePropertiesForElements(source, interfaceModel, particle, items.Where(i => i.XmlParticle is not XmlSchemaGroupRef));
                interfaceModel.Properties.AddRange(properties);
                AddInterfaces(interfaceModel, items);

                return interfaceModel;
            }

            private TypeModel CreateTypeModel(XmlSchemaAttributeGroup group)
            {
                var name = "I" + _configuration.NamingProvider.AttributeGroupTypeNameFromQualifiedName(qualifiedName, group);

                InterfaceModel interfaceModel = CreateInterfaceModel(group, name);

                var attributes = group.Attributes;
                var properties = builder.CreatePropertiesForAttributes(source, interfaceModel, attributes.OfType<XmlSchemaAttribute>());
                interfaceModel.Properties.AddRange(properties);
                AddInterfaces(interfaceModel, attributes);

                return interfaceModel;
            }

            private TypeModel CreateTypeModel(XmlSchemaComplexType complexType)
            {
                var name = _configuration.NamingProvider.ComplexTypeNameFromQualifiedName(qualifiedName, complexType);
                if (namespaceModel != null)
                    name = namespaceModel.GetUniqueTypeName(name);

                var classModel = new ClassModel(_configuration)
                {
                    Name = name,
                    Namespace = namespaceModel,
                    XmlSchemaName = qualifiedName,
                    XmlSchemaType = complexType,
                    IsAbstract = complexType.IsAbstract,
                    IsAnonymous = string.IsNullOrEmpty(complexType.QualifiedName.Name),
                    IsMixed = complexType.IsMixed,
                    IsSubstitution = complexType.Parent is XmlSchemaElement parent && !parent.SubstitutionGroup.IsEmpty
                };

                classModel.Documentation.AddRange(docs);

                if (namespaceModel != null)
                    namespaceModel.Types[classModel.Name] = classModel;

                if (!qualifiedName.IsEmpty)
                    builder.SetType(complexType, qualifiedName, classModel);

                if (complexType.BaseXmlSchemaType != null && complexType.BaseXmlSchemaType.QualifiedName != AnyType)
                {
                    var baseModel = builder.CreateTypeModel(complexType.BaseXmlSchemaType.QualifiedName, complexType.BaseXmlSchemaType);
                    classModel.BaseClass = baseModel;
                    if (baseModel is ClassModel baseClassModel)
                        baseClassModel.DerivedTypes.Add(classModel);
                }

                XmlSchemaParticle xmlParticle = null;
                if (classModel.BaseClass != null)
                {
                    if (complexType.ContentModel.Content is XmlSchemaComplexContentExtension complexContent)
                        xmlParticle = complexContent.Particle;

                    // If it's a restriction, do not duplicate elements on the derived class, they're already in the base class.
                    // See https://msdn.microsoft.com/en-us/library/f3z3wh0y.aspx
                }
                else
                {
                    xmlParticle = complexType.Particle ?? complexType.ContentTypeParticle;
                }

                var items = builder.GetElements(xmlParticle, complexType).ToList();

                if (_configuration.GenerateInterfaces)
                    AddInterfaces(classModel, items);

                var particle = new Particle(xmlParticle, xmlParticle?.Parent);
                var properties = builder.CreatePropertiesForElements(source, classModel, particle, items);
                classModel.Properties.AddRange(properties);

                XmlSchemaObjectCollection attributes = null;
                if (classModel.BaseClass != null)
                {
                    if (complexType.ContentModel.Content is XmlSchemaComplexContentExtension complexContent)
                        attributes = complexContent.Attributes;
                    else if (complexType.ContentModel.Content is XmlSchemaSimpleContentExtension simpleContent)
                        attributes = simpleContent.Attributes;

                    // If it's a restriction, do not duplicate attributes on the derived class, they're already in the base class.
                    // See https://msdn.microsoft.com/en-us/library/f3z3wh0y.aspx
                }
                else
                {
                    attributes = complexType.Attributes;

                    if (attributes.Count == 0 && complexType.ContentModel != null)
                    {
                        var content = complexType.ContentModel.Content;

                        if (content is XmlSchemaComplexContentExtension extension)
                            attributes = extension.Attributes;
                        else if (content is XmlSchemaComplexContentRestriction restriction)
                            attributes = restriction.Attributes;
                    }
                }

                if (attributes != null)
                {
                    var attributeProperties = builder.CreatePropertiesForAttributes(source, classModel, attributes.Cast<XmlSchemaObject>());
                    classModel.Properties.AddRange(attributeProperties);

                    if (_configuration.GenerateInterfaces)
                        AddInterfaces(classModel, attributes);
                }

                XmlSchemaAnyAttribute anyAttribute = null;
                if (complexType.AnyAttribute != null)
                {
                    anyAttribute = complexType.AnyAttribute;
                }
                else if (complexType.AttributeWildcard != null)
                {
                    var hasAnyAttribute = true;
                    for (var baseType = complexType.BaseXmlSchemaType; baseType != null; baseType = baseType.BaseXmlSchemaType)
                    {
                        if (baseType is not XmlSchemaComplexType baseComplexType)
                            continue;

                        if (baseComplexType.AttributeWildcard != null)
                        {
                            hasAnyAttribute = false;
                            break;
                        }
                    }

                    if (hasAnyAttribute)
                        anyAttribute = complexType.AttributeWildcard;
                }

                if (anyAttribute != null)
                {
                    SimpleModel type = new(_configuration) { ValueType = typeof(XmlAttribute), UseDataTypeAttribute = false };
                    var property = new PropertyModel(_configuration, "AnyAttribute", type, classModel)
                    {
                        IsAttribute = true,
                        IsCollection = true,
                        IsAny = true
                    };

                    var attributeDocs = GetDocumentation(anyAttribute);
                    property.Documentation.AddRange(attributeDocs);

                    classModel.Properties.Add(property);
                }

                return classModel;
            }


            private TypeModel CreateTypeModel(XmlSchemaSimpleType simpleType)
            {
                List<RestrictionModel> restrictions = null;
                List<IEnumerable<XmlSchemaFacet>> baseFacets = null;

                var facets = simpleType.Content switch
                {
                    XmlSchemaSimpleTypeUnion typeUnion when AllMembersHaveFacets(typeUnion, out baseFacets) => baseFacets.SelectMany(f => f).ToList(),
                    _ => MergeRestrictions(simpleType)
                };

                if (facets.Count > 0)
                {
                    var enumFacets = facets.OfType<XmlSchemaEnumerationFacet>().ToList();

                    // If a union has enum restrictions, there must be an enum restriction in all parts of the union
                    // If there are other restrictions mixed into the enumeration values, we'll generate a string to play it safe.
                    if (enumFacets.Count > 0 && (baseFacets is null || baseFacets.TrueForAll(fs => fs.OfType<XmlSchemaEnumerationFacet>().Any())) && !_configuration.EnumAsString)
                        return CreateEnumModel(simpleType, enumFacets);

                    restrictions = CodeUtilities.GetRestrictions(facets, simpleType, _configuration).Where(r => r != null).Sanitize().ToList();
                }

                return CreateSimpleModel(simpleType, restrictions ?? new());

                static bool AllMembersHaveFacets(XmlSchemaSimpleTypeUnion typeUnion, out List<IEnumerable<XmlSchemaFacet>> baseFacets)
                {
                    var members = typeUnion.BaseMemberTypes.Select(b => b.Content as XmlSchemaSimpleTypeRestriction);
                    var retval = members.All(r => r?.Facets.Count > 0);
                    baseFacets = !retval ? null : members.Select(r => r.Facets.Cast<XmlSchemaFacet>()).ToList();
                    return retval;
                }

                static List<XmlSchemaFacet> MergeRestrictions(XmlSchemaSimpleType type)
                {
                    if (type == null) return new();
                    var baseFacets = MergeRestrictions(type.BaseXmlSchemaType as XmlSchemaSimpleType);
                    if (type.Content is XmlSchemaSimpleTypeRestriction typeRestriction)
                    {
                        var facets = typeRestriction.Facets.Cast<XmlSchemaFacet>().ToList();
                        foreach (var facet in facets)
                        {
                            var baseFacet = baseFacets
                                .SingleOrDefault(f => f is not XmlSchemaEnumerationFacet
                                    && f.GetType() == facet.GetType());
                            if (baseFacet != null)
                                baseFacets.Remove(baseFacet);
                            baseFacets.Add(facet);
                        }
                    }
                    return baseFacets;
                }
            }

            private static List<EnumValueModel> EnsureEnumValuesUnique(List<EnumValueModel> enumModelValues)
            {
                var enumValueGroups = from enumValue in enumModelValues
                                      group enumValue by enumValue.Name;

                foreach (var g in enumValueGroups)
                {
                    var i = 1;
                    foreach (var t in g.Skip(1))
                        t.Name = $"{t.Name}{i++}";
                }

                return enumModelValues;
            }

            private EnumModel CreateEnumModel(XmlSchemaSimpleType simpleType, List<XmlSchemaEnumerationFacet> enumFacets)
            {
                // we got an enum
                var name = _configuration.NamingProvider.EnumTypeNameFromQualifiedName(qualifiedName, simpleType);
                if (namespaceModel != null)
                    name = namespaceModel.GetUniqueTypeName(name);

                var enumModel = new EnumModel(_configuration)
                {
                    Name = name,
                    Namespace = namespaceModel,
                    XmlSchemaName = qualifiedName,
                    XmlSchemaType = simpleType,
                    IsAnonymous = string.IsNullOrEmpty(simpleType.QualifiedName.Name),
                };

                enumModel.Documentation.AddRange(docs);

                foreach (var facet in enumFacets.DistinctBy(f => f.Value))
                {
                    var value = new EnumValueModel
                    {
                        Name = _configuration.NamingProvider.EnumMemberNameFromValue(enumModel.Name, facet.Value, facet),
                        Value = facet.Value
                    };

                    var valueDocs = GetDocumentation(facet);
                    value.Documentation.AddRange(valueDocs);

                    value.IsDeprecated = facet.Annotation?.Items.OfType<XmlSchemaAppInfo>()
                        .Any(a => Array.Exists(a.Markup, m => m.Name == "annox:annotate" && m.HasChildNodes && m.FirstChild.Name == "jl:Deprecated")) == true;

                    enumModel.Values.Add(value);
                }

                enumModel.Values = EnsureEnumValuesUnique(enumModel.Values);
                if (namespaceModel != null)
                    namespaceModel.Types[enumModel.Name] = enumModel;

                if (!qualifiedName.IsEmpty)
                    builder.SetType(simpleType, qualifiedName, enumModel);

                return enumModel;
            }

            private SimpleModel CreateSimpleModel(XmlSchemaSimpleType simpleType, List<RestrictionModel> restrictions)
            {
                var simpleModelName = _configuration.NamingProvider.SimpleTypeNameFromQualifiedName(qualifiedName, simpleType);
                if (namespaceModel != null)
                    simpleModelName = namespaceModel.GetUniqueTypeName(simpleModelName);

                var simpleModel = new SimpleModel(_configuration)
                {
                    Name = simpleModelName,
                    Namespace = namespaceModel,
                    XmlSchemaName = qualifiedName,
                    XmlSchemaType = simpleType,
                    ValueType = simpleType.Datatype.GetEffectiveType(_configuration, restrictions, simpleType),
                };

                simpleModel.Documentation.AddRange(docs);
                simpleModel.Restrictions.AddRange(restrictions);

                if (namespaceModel != null)
                    namespaceModel.Types[simpleModel.Name] = simpleModel;

                if (!qualifiedName.IsEmpty)
                    builder.SetType(simpleType, qualifiedName, simpleModel);
                return simpleModel;
            }

            private void AddInterfaces(ReferenceTypeModel refTypeModel, IEnumerable<Particle> items)
            {
                var interfaces = items.Select(i => i.XmlParticle).OfType<XmlSchemaGroupRef>()
                    .Select(i => (InterfaceModel)builder.CreateTypeModel(i.RefName, builder.Groups[i.RefName].First()));
                refTypeModel.AddInterfaces(interfaces);
            }

            private void AddInterfaces(ReferenceTypeModel refTypeModel, XmlSchemaObjectCollection attributes)
            {
                var interfaces = attributes.OfType<XmlSchemaAttributeGroupRef>()
                    .Select(a => (InterfaceModel)builder.CreateTypeModel(a.RefName, builder.AttributeGroups[a.RefName].First()));
                refTypeModel.AddInterfaces(interfaces);
            }
        }

        private IEnumerable<PropertyModel> CreatePropertiesForAttributes(Uri source, TypeModel owningTypeModel, IEnumerable<XmlSchemaObject> items)
        {
            var properties = new List<PropertyModel>();

            foreach (var item in items)
            {
                switch (item)
                {
                    case XmlSchemaAttribute attribute when attribute.Use != XmlSchemaUse.Prohibited:

                        properties.Add(PropertyFromAttribute(owningTypeModel, attribute, properties));
                        break;

                    case XmlSchemaAttributeGroupRef attributeGroupRef:

                        foreach (var attributeGroup in AttributeGroups[attributeGroupRef.RefName])
                        {
                            if (_configuration.GenerateInterfaces)
                                CreateTypeModel(attributeGroupRef.RefName, attributeGroup);

                            var attributes = attributeGroup.Attributes.Cast<XmlSchemaObject>()
                                .Where(a => !(a is XmlSchemaAttributeGroupRef agr && agr.RefName == attributeGroupRef.RefName))
                                .ToList();

                            if (attributeGroup.RedefinedAttributeGroup != null)
                            {
                                var attrs = attributeGroup.RedefinedAttributeGroup.Attributes.Cast<XmlSchemaObject>()
                                    .Where(a => !(a is XmlSchemaAttributeGroupRef agr && agr.RefName == attributeGroupRef.RefName)).ToList();

                                foreach (var attr in attrs)
                                {
                                    var n = attr.GetQualifiedName();

                                    if (n != null)
                                        attributes.RemoveAll(a => a.GetQualifiedName() == n);

                                    attributes.Add(attr);
                                }
                            }

                            var newProperties = CreatePropertiesForAttributes(source, owningTypeModel, attributes);
                            properties.AddRange(newProperties);
                        }

                        break;
                }
            }
            return properties;
        }

        private PropertyModel PropertyFromAttribute(TypeModel owningTypeModel, XmlSchemaAttributeEx attribute, IList<PropertyModel> properties)
        {
            var attributeQualifiedName = attribute.AttributeSchemaType.QualifiedName;
            var name = _configuration.NamingProvider.AttributeNameFromQualifiedName(attribute.QualifiedName, attribute);
            var originalName = name;

            if (attribute.Base.Parent is XmlSchemaAttributeGroup attributeGroup
                && attributeGroup.QualifiedName != owningTypeModel.XmlSchemaName
                && Types.TryGetValue(BuildKey(attributeGroup, attributeGroup.QualifiedName), out var typeModelValue)
                && typeModelValue is InterfaceModel interfaceTypeModel)
            {
                var interfaceProperty = interfaceTypeModel.Properties.Single(p => p.XmlSchemaName == attribute.QualifiedName);
                attributeQualifiedName = interfaceProperty.Type.XmlSchemaName;
                name = interfaceProperty.Name;
            }
            else
            {
                if (attributeQualifiedName.IsEmpty)
                {
                    attributeQualifiedName = attribute.QualifiedName;

                    if (attributeQualifiedName.IsEmpty || string.IsNullOrEmpty(attributeQualifiedName.Namespace))
                    {
                        // inner type, have to generate a type name
                        var typeName = _configuration.NamingProvider.TypeNameFromAttribute(owningTypeModel.Name, attribute.QualifiedName.Name, attribute);
                        attributeQualifiedName = new XmlQualifiedName(typeName, owningTypeModel.XmlSchemaName.Namespace);
                        // try to avoid name clashes
                        if (NameExists(attributeQualifiedName))
                            attributeQualifiedName = new[] { "Item", "Property", "Element" }.Select(s => new XmlQualifiedName(attributeQualifiedName.Name + s, attributeQualifiedName.Namespace)).First(n => !NameExists(n));
                    }
                }

                if (name == owningTypeModel.Name)
                    name += "Property";
            }

            name = owningTypeModel.GetUniquePropertyName(name, properties);

            var typeModel = CreateTypeModel(attributeQualifiedName, attribute.AttributeSchemaType);
            var property = new PropertyModel(_configuration, name, typeModel, owningTypeModel)
            {
                IsAttribute = true,
                IsRequired = attribute.Use == XmlSchemaUse.Required
            };

            property.SetFromNode(originalName, attribute.Use != XmlSchemaUse.Optional, attribute);
            property.SetSchemaNameAndNamespace(owningTypeModel, attribute);
            property.Documentation.AddRange(GetDocumentation(attribute));

            return property;
        }

        private IEnumerable<PropertyModel> CreatePropertiesForElements(Uri source, TypeModel owningTypeModel, Particle particle, IEnumerable<Particle> items,
            Substitute substitute = null, int order = 0, bool passProperties = true)
        {
            var properties = new List<PropertyModel>();

            foreach (var item in items)
            {
                PropertyModel property = null;

                switch (item.XmlParticle)
                {
                    // ElementSchemaType must be non-null. This is not the case when maxOccurs="0".
                    case XmlSchemaElement element when element.ElementSchemaType != null:
                        property = PropertyFromElement(owningTypeModel, element, particle, item, substitute, passProperties ? properties : new List<PropertyModel>());
                        break;
                    case XmlSchemaAny:
                        SimpleModel typeModel = new(_configuration)
                        {
                            ValueType = _configuration.UseXElementForAny ? typeof(XElement) : typeof(XmlElement),
                            UseDataTypeAttribute = false
                        };
                        property = new PropertyModel(_configuration, "Any", typeModel, owningTypeModel) { IsAny = true };
                        property.SetFromParticles(particle, item, item.MinOccurs >= 1.0m && !IsNullableByChoice(item.XmlParent));
                        break;
                    case XmlSchemaGroupRef groupRef:
                        var group = Groups[groupRef.RefName];

                        if (_configuration.GenerateInterfaces)
                            CreateTypeModel(groupRef.RefName, group.First());

                        var groupItems = GetElements(groupRef.Particle).ToList();
                        var groupProperties = CreatePropertiesForElements(source, owningTypeModel, item, groupItems, order: order, passProperties: false).ToList();
                        if (_configuration.EmitOrder)
                            order += groupProperties.Count;

                        properties.AddRange(groupProperties);
                        break;
                }

                // Discard duplicate property names. This is most likely due to:
                // - Choice or
                // - Element and attribute with the same name
                if (property != null && !properties.Exists(p => p.Name == property.Name))
                {
                    var itemDocs = GetDocumentation(item.XmlParticle);
                    property.Documentation.AddRange(itemDocs);

                    if (_configuration.EmitOrder)
                        property.Order = order++;

                    property.IsDeprecated = itemDocs.Exists(d => d.Text.StartsWith("DEPRECATED"));

                    properties.Add(property);
                }
            }

            return properties;
        }

        private static bool IsNullableByChoice(XmlSchemaObject parent)
        {
            while (parent != null)
            {
                switch (parent)
                {
                    case XmlSchemaChoice:
                        return true;
                    // Any ancestor element between the current item and the
                    // choice would already have been forced to nullable.
                    case XmlSchemaElement:
                    case XmlSchemaParticle p when p.MinOccurs < 1.0m:
                        return false;
                    default:
                        break;
                }
                parent = parent.Parent;
            }
            return false;
        }

        private PropertyModel PropertyFromElement(TypeModel owningTypeModel, XmlSchemaElementEx element, Particle particle, Particle item, Substitute substitute,
            IList<PropertyModel> properties)
        {
            PropertyModel property;
            XmlSchemaElementEx effectiveElement = substitute?.Element ?? element;

            property = properties.FirstOrDefault(p => element.QualifiedName == p.XmlSchemaName && p.Type.XmlSchemaType == element.ElementSchemaType);

            if (property != null)
            {
                property.IsCollection = true;
                return property;
            }

            var name = _configuration.NamingProvider.ElementNameFromQualifiedName(effectiveElement.QualifiedName, effectiveElement);
            var originalName = name;
            if (name == owningTypeModel.Name)
                name += "Property"; // member names cannot be the same as their enclosing type

            name = owningTypeModel.GetUniquePropertyName(name, properties);

            var typeModel = substitute?.Type ?? CreateTypeModel(GetQualifiedName(owningTypeModel, particle.XmlParticle, element), element.ElementSchemaType);

            property = new PropertyModel(_configuration, name, typeModel, owningTypeModel) { IsNillable = element.IsNillable };
            var isRequired = item.MinOccurs >= 1.0m && !IsNullableByChoice(item.XmlParent);
            property.SetFromParticles(particle, item, isRequired);
            property.SetFromNode(originalName, isRequired, element);
            property.SetSchemaNameAndNamespace(owningTypeModel, effectiveElement);

            if (property.IsArray && !_configuration.GenerateComplexTypesForCollections)
                property.Type.Namespace.Types.Remove(property.Type.Name);

            return property;
        }

        private XmlQualifiedName GetQualifiedName(TypeModel typeModel, XmlSchemaParticle xmlParticle, XmlSchemaElementEx element)
        {
            var elementQualifiedName = element.ElementSchemaType.QualifiedName;

            if (elementQualifiedName.IsEmpty)
            {
                elementQualifiedName = element.RefName;

                if (elementQualifiedName.IsEmpty)
                {
                    // inner type, have to generate a type name
                    var typeModelName = xmlParticle is XmlSchemaGroupRef groupRef ? groupRef.RefName : typeModel.XmlSchemaName;
                    var typeName = _configuration.NamingProvider.TypeNameFromElement(typeModelName.Name, element.QualifiedName.Name, element);
                    elementQualifiedName = new XmlQualifiedName(typeName, typeModel.XmlSchemaName.Namespace);
                    // try to avoid name clashes
                    if (NameExists(elementQualifiedName))
                        elementQualifiedName = new[] { "Item", "Property", "Element" }.Select(s => new XmlQualifiedName(elementQualifiedName.Name + s, elementQualifiedName.Namespace)).First(n => !NameExists(n));
                }
            }

            return elementQualifiedName;
        }

        private NamespaceModel CreateNamespaceModel(Uri source, XmlQualifiedName qualifiedName)
        {
            NamespaceModel namespaceModel = null;
            if (!qualifiedName.IsEmpty && qualifiedName.Namespace != XmlSchema.Namespace)
            {
                var key = new NamespaceKey(source, qualifiedName.Namespace);
                if (!Namespaces.TryGetValue(key, out namespaceModel))
                {
                    var namespaceName = BuildNamespace(source, qualifiedName.Namespace);
                    namespaceModel = new NamespaceModel(key, _configuration) { Name = namespaceName };
                    Namespaces.Add(key, namespaceModel);
                }
            }
            return namespaceModel;
        }

        private bool NameExists(XmlQualifiedName name)
        {
            var elements = _set.GlobalElements.Names.Cast<XmlQualifiedName>();
            var types = _set.GlobalTypes.Names.Cast<XmlQualifiedName>();
            return elements.Concat(types).Any(n => n.Namespace == name.Namespace && name.Name.Equals(n.Name, StringComparison.OrdinalIgnoreCase));
        }

        public IEnumerable<Particle> GetElements(XmlSchemaGroupBase groupBase)
        {
            if (groupBase?.Items != null)
            {
                foreach (var item in groupBase.Items)
                {
                    foreach (var element in GetElements(item, groupBase))
                    {
                        element.MaxOccurs = Math.Max(element.MaxOccurs, groupBase.MaxOccurs);
                        element.MinOccurs = Math.Min(element.MinOccurs, groupBase.MinOccurs);
                        yield return element;
                    }
                }
            }
        }

        public IEnumerable<Particle> GetElements(XmlSchemaObject item, XmlSchemaObject parent)
        {
            switch (item)
            {
                case null:
                    yield break;
                case XmlSchemaElement element:
                    yield return new Particle(element, parent); break;
                case XmlSchemaAny any:
                    yield return new Particle(any, parent); break;
                case XmlSchemaGroupRef groupRef:
                    yield return new Particle(groupRef, parent); break;
                case XmlSchemaGroupBase itemGroupBase:
                    foreach (var groupBaseElement in GetElements(itemGroupBase))
                        yield return groupBaseElement;
                    break;
            }
        }

        public static List<DocumentationModel> GetDocumentation(XmlSchemaAnnotated annotated)
        {
	        return annotated.Annotation == null ? new List<DocumentationModel>()
		        : annotated.Annotation.Items.OfType<XmlSchemaDocumentation>()
		        .Where(d => d.Markup?.Length > 0)
		        .Select(d => d.Markup.Select(m => new DocumentationModel { Language = d.Language, Text = m.OuterXml }))
		        .SelectMany(d => d)
		        .Where(d => !string.IsNullOrEmpty(d.Text))
		        .ToList();
        }

        public IEnumerable<CodeNamespace> GenerateCode()
        {
            var hierarchy = NamespaceHierarchyItem.Build(Namespaces.Values.GroupBy(x => x.Name).SelectMany(x => x))
                .MarkAmbiguousNamespaceTypes();
            return hierarchy.Flatten()
                .Select(nhi => NamespaceModel.Generate(nhi.FullName, nhi.Models, _configuration));
        }

        private string BuildNamespace(Uri source, string xmlNamespace)
        {
            var key = new NamespaceKey(source, xmlNamespace);
            var result = _configuration.NamespaceProvider.FindNamespace(key);
            return !string.IsNullOrEmpty(result) ? result
                : throw new ArgumentException(string.Format("Namespace {0} not provided through map or generator.", xmlNamespace));
        }
    }
}