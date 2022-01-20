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
        private readonly Dictionary<XmlQualifiedName, XmlSchemaAttributeGroup> AttributeGroups;
        private readonly Dictionary<XmlQualifiedName, XmlSchemaGroup> Groups;
        private readonly Dictionary<NamespaceKey, NamespaceModel> Namespaces = new();
        private readonly Dictionary<string, TypeModel> Types = new();
        private readonly Dictionary<XmlQualifiedName, HashSet<Substitute>> SubstitutionGroups = new();

        private static readonly XmlQualifiedName AnyType = new("anyType", XmlSchema.Namespace);

        private string BuildKey(XmlSchemaAnnotated annotated, XmlQualifiedName name)
            => $"{annotated.GetType()}:{annotated.SourceUri}:{annotated.LineNumber}:{annotated.LinePosition}:{name}";

        public ModelBuilder(GeneratorConfiguration configuration, XmlSchemaSet set)
        {
            _configuration = configuration;
            _set = set;

            DocumentationModel.DisableComments = _configuration.DisableComments;
            var objectModel = new SimpleModel(_configuration)
            {
                Name = "AnyType",
                Namespace = CreateNamespaceModel(new Uri(XmlSchema.Namespace), AnyType),
                XmlSchemaName = AnyType,
                XmlSchemaType = null,
                ValueType = typeof(object),
                UseDataTypeAttribute = false
            };

            var key = BuildKey(new XmlSchemaComplexType(), AnyType);
            Types[key] = objectModel;

            AttributeGroups = set.Schemas().Cast<XmlSchema>().SelectMany(s => s.AttributeGroups.Values.Cast<XmlSchemaAttributeGroup>())
                .DistinctBy(g => g.QualifiedName.ToString())
                .ToDictionary(g => g.QualifiedName);
            Groups = set.Schemas().Cast<XmlSchema>().SelectMany(s => s.Groups.Values.Cast<XmlSchemaGroup>())
                .DistinctBy(g => g.QualifiedName.ToString())
                .ToDictionary(g => g.QualifiedName);

            var dependencyOrder = new List<XmlSchema>();
            var seenSchemas = new HashSet<XmlSchema>();
            foreach (var schema in set.Schemas().Cast<XmlSchema>())
            {
                ResolveDependencies(schema, dependencyOrder, seenSchemas);
            }

            foreach (var schema in dependencyOrder)
            {
                var types = set.GlobalTypes.Values.Cast<XmlSchemaType>().Where(s => s.GetSchema() == schema);
                CreateTypes(types);
                var elements = set.GlobalElements.Values.Cast<XmlSchemaElement>().Where(s => s.GetSchema() == schema);
                CreateElements(elements);
            }

            CreateSubstitutes();

            if (configuration.GenerateInterfaces)
            {
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
                        var substitutes = GetSubstitutedElements(prop.XmlSchemaName).ToList();

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
                            prop.Substitutes = substitutes;
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
                {
                    typeModel.RootElementName = typeModel.GetQualifiedName();
                }
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
                        {
                            baseInterfaceTypeProperties.Remove(baseProperty);
                        }
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
                                && implementationClassProperty.XmlSchemaName == interfaceProperty.XmlSchemaName)
                            {
                                RenameInterfacePropertyInBaseClasses(interfaceModel, implementationClass, interfaceProperty, implementationClassProperty.Name);
                                interfaceProperty.Name = implementationClassProperty.Name;
                            }
                        }
                    }
                }
            }
        }

        private static void RenameInterfacePropertyInBaseClasses(InterfaceModel interfaceModel, ReferenceTypeModel implementationClass,
            PropertyModel interfaceProperty, string newName)
        {
            foreach (var interfaceModelImplementationClass in interfaceModel.AllDerivedReferenceTypes().Where(c =>
                c != implementationClass))
            {
                foreach (var propertyModel in interfaceModelImplementationClass.Properties.Where(p =>
                    p.Name == interfaceProperty.Name))
                {
                    propertyModel.Name = newName;
                }
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


        private void CreateTypes(IEnumerable<XmlSchemaType> types)
        {
            foreach (var globalType in types)
            {
                CreateTypeModel(globalType, globalType.QualifiedName);
            }
        }

        private void CreateElements(IEnumerable<XmlSchemaElement> elements)
        {
            foreach (var rootElement in elements)
            {
                var qualifiedName = rootElement.ElementSchemaType.QualifiedName;
                if (qualifiedName.IsEmpty) { qualifiedName = rootElement.QualifiedName; }
                var type = CreateTypeModel(rootElement.ElementSchemaType, qualifiedName);
                ClassModel derivedClassModel = null;

                if (type.RootElementName != null || type.IsAbstractRoot)
                {
                    if (type is ClassModel classModel)
                    {
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

                        var key = BuildKey(rootElement, rootElement.QualifiedName);
                        Types[key] = derivedClassModel;

                        derivedClassModel.BaseClass = classModel;
                        ((ClassModel)derivedClassModel.BaseClass).DerivedTypes.Add(derivedClassModel);

                        derivedClassModel.RootElementName = rootElement.QualifiedName;

                        if (!type.IsAbstractRoot)
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

                            if (classModel.XmlSchemaName != null && !classModel.XmlSchemaName.IsEmpty)
                            {
                                key = BuildKey(classModel.RootElement, classModel.XmlSchemaName);
                                Types[key] = originalClassModel;
                            }

                            originalClassModel.BaseClass = classModel;
                            ((ClassModel)originalClassModel.BaseClass).DerivedTypes.Add(originalClassModel);

                            originalClassModel.RootElementName = type.RootElementName;

                            if (classModel.RootElement.SubstitutionGroup != null
                                && SubstitutionGroups.TryGetValue(classModel.RootElement.SubstitutionGroup, out var substitutes))
                            {
                                foreach (var substitute in substitutes.Where(s => s.Element == classModel.RootElement))
                                {
                                    substitute.Type = originalClassModel;
                                }
                            }

                            classModel.RootElementName = null;
                            classModel.IsAbstractRoot = true;
                        }
                    }
                    else
                    {
                        var key = BuildKey(rootElement, rootElement.QualifiedName);
                        Types[key] = type;
                    }
                }
                else
                {
                    if (type is ClassModel classModel)
                    {
                        classModel.Documentation.AddRange(GetDocumentation(rootElement));
                    }

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
        }

        private IEnumerable<Substitute> GetSubstitutedElements(XmlQualifiedName name)
        {
            if (SubstitutionGroups.TryGetValue(name, out var substitutes))
            {
                foreach (var substitute in substitutes)
                {
                    yield return substitute;
                    foreach (var recursiveSubstitute in GetSubstitutedElements(substitute.Element.QualifiedName))
                        yield return recursiveSubstitute;
                }
            }
        }

        private TypeModel CreateTypeModel(XmlSchemaAnnotated type, XmlQualifiedName qualifiedName)
        {
            var key = BuildKey(type, qualifiedName);
            if (!qualifiedName.IsEmpty && Types.TryGetValue(key, out TypeModel typeModel))
            {
                return typeModel;
            }

            var source = CodeUtilities.CreateUri(type.SourceUri);
            var namespaceModel = CreateNamespaceModel(source, qualifiedName);
            var docs = GetDocumentation(type);

            if (type is XmlSchemaGroup group)
            {
                return CreateTypeModel(source, group, namespaceModel, qualifiedName, docs);
            }
            else if (type is XmlSchemaAttributeGroup attributeGroup)
            {
                return CreateTypeModel(source, attributeGroup, namespaceModel, qualifiedName, docs);
            }
            else if (type is XmlSchemaComplexType complexType)
            {
                return CreateTypeModel(source, complexType, namespaceModel, qualifiedName, docs);
            }
            else if (type is XmlSchemaSimpleType simpleType)
            {
                return CreateTypeModel(simpleType, namespaceModel, qualifiedName, docs);
            }

            throw new NotSupportedException($"Cannot build declaration for {qualifiedName}");
        }

        private TypeModel CreateTypeModel(Uri source, XmlSchemaGroup group, NamespaceModel namespaceModel, XmlQualifiedName qualifiedName, List<DocumentationModel> docs)
        {
            var name = "I" + _configuration.NamingProvider.GroupTypeNameFromQualifiedName(qualifiedName, group);
            if (namespaceModel != null) { name = namespaceModel.GetUniqueTypeName(name); }

            var interfaceModel = new InterfaceModel(_configuration)
            {
                Name = name,
                Namespace = namespaceModel,
                XmlSchemaName = qualifiedName
            };

            interfaceModel.Documentation.AddRange(docs);

            if (namespaceModel != null) { namespaceModel.Types[name] = interfaceModel; }

            if (!qualifiedName.IsEmpty)
            {
                var key = BuildKey(group, qualifiedName);
                Types[key] = interfaceModel;
            }

            var xmlParticle = group.Particle;
            var particle = new Particle(xmlParticle, group.Parent);
            var items = GetElements(xmlParticle);
            var properties = CreatePropertiesForElements(source, interfaceModel, particle, items.Where(i => i.XmlParticle is not XmlSchemaGroupRef));
            interfaceModel.Properties.AddRange(properties);
            var interfaces = items.Select(i => i.XmlParticle).OfType<XmlSchemaGroupRef>()
                .Select(i => (InterfaceModel)CreateTypeModel(Groups[i.RefName], i.RefName));
            interfaceModel.AddInterfaces(interfaces);

            return interfaceModel;
        }

        private TypeModel CreateTypeModel(Uri source, XmlSchemaAttributeGroup attributeGroup, NamespaceModel namespaceModel, XmlQualifiedName qualifiedName, List<DocumentationModel> docs)
        {
            var name = "I" + _configuration.NamingProvider.AttributeGroupTypeNameFromQualifiedName(qualifiedName, attributeGroup);
            if (namespaceModel != null) { name = namespaceModel.GetUniqueTypeName(name); }

            var interfaceModel = new InterfaceModel(_configuration)
            {
                Name = name,
                Namespace = namespaceModel,
                XmlSchemaName = qualifiedName
            };

            interfaceModel.Documentation.AddRange(docs);

            if (namespaceModel != null) { namespaceModel.Types[name] = interfaceModel; }

            if (!qualifiedName.IsEmpty)
            {
                var key = BuildKey(attributeGroup, qualifiedName);
                Types[key] = interfaceModel;
            }

            var items = attributeGroup.Attributes;
            var properties = CreatePropertiesForAttributes(source, interfaceModel, items.OfType<XmlSchemaAttribute>());
            interfaceModel.Properties.AddRange(properties);
            var interfaces = items.OfType<XmlSchemaAttributeGroupRef>()
                .Select(a => (InterfaceModel)CreateTypeModel(AttributeGroups[a.RefName], a.RefName));
            interfaceModel.AddInterfaces(interfaces);

            return interfaceModel;
        }

        private TypeModel CreateTypeModel(Uri source, XmlSchemaComplexType complexType, NamespaceModel namespaceModel, XmlQualifiedName qualifiedName, List<DocumentationModel> docs)
        {
            var name = _configuration.NamingProvider.ComplexTypeNameFromQualifiedName(qualifiedName, complexType);
            if (namespaceModel != null)
            {
                name = namespaceModel.GetUniqueTypeName(name);
            }

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
            {
                namespaceModel.Types[classModel.Name] = classModel;
            }

            if (!qualifiedName.IsEmpty)
            {
                var key = BuildKey(complexType, qualifiedName);
                Types[key] = classModel;
            }

            if (complexType.BaseXmlSchemaType != null && complexType.BaseXmlSchemaType.QualifiedName != AnyType)
            {
                var baseModel = CreateTypeModel(complexType.BaseXmlSchemaType, complexType.BaseXmlSchemaType.QualifiedName);
                classModel.BaseClass = baseModel;
                if (baseModel is ClassModel baseClassModel) { baseClassModel.DerivedTypes.Add(classModel); }
            }

            XmlSchemaParticle xmlParticle = null;
            if (classModel.BaseClass != null)
            {
                if (complexType.ContentModel.Content is XmlSchemaComplexContentExtension complexContent)
                {
                    xmlParticle = complexContent.Particle;
                }

                // If it's a restriction, do not duplicate elements on the derived class, they're already in the base class.
                // See https://msdn.microsoft.com/en-us/library/f3z3wh0y.aspx
            }
            else xmlParticle = complexType.Particle ?? complexType.ContentTypeParticle;

            var items = GetElements(xmlParticle, complexType).ToList();

            if (_configuration.GenerateInterfaces)
            {
                var interfaces = items.Select(i => i.XmlParticle).OfType<XmlSchemaGroupRef>()
                    .Select(i => (InterfaceModel)CreateTypeModel(Groups[i.RefName], i.RefName)).ToList();

                classModel.AddInterfaces(interfaces);
            }

            var particle = new Particle(xmlParticle, xmlParticle?.Parent);
            var properties = CreatePropertiesForElements(source, classModel, particle, items);
            classModel.Properties.AddRange(properties);

            XmlSchemaObjectCollection attributes = null;
            if (classModel.BaseClass != null)
            {
                if (complexType.ContentModel.Content is XmlSchemaComplexContentExtension complexContent)
                {
                    attributes = complexContent.Attributes;
                }
                else if (complexType.ContentModel.Content is XmlSchemaSimpleContentExtension simpleContent)
                {
                    attributes = simpleContent.Attributes;
                }

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
                var attributeProperties = CreatePropertiesForAttributes(source, classModel, attributes.Cast<XmlSchemaObject>());
                classModel.Properties.AddRange(attributeProperties);

                if (_configuration.GenerateInterfaces)
                {
                    var attributeInterfaces = attributes.OfType<XmlSchemaAttributeGroupRef>()
                        .Select(i => (InterfaceModel)CreateTypeModel(AttributeGroups[i.RefName], i.RefName));
                    classModel.AddInterfaces(attributeInterfaces);
                }
            }

            XmlSchemaAnyAttribute anyAttribute = null;
            if (complexType.AnyAttribute != null)
                anyAttribute = complexType.AnyAttribute;
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
                var property = new PropertyModel(_configuration)
                {
                    OwningType = classModel,
                    Name = "AnyAttribute",
                    Type = new SimpleModel(_configuration) { ValueType = typeof(XmlAttribute), UseDataTypeAttribute = false },
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

        private TypeModel CreateTypeModel(XmlSchemaSimpleType simpleType, NamespaceModel namespaceModel, XmlQualifiedName qualifiedName, List<DocumentationModel> docs)
        {
            var restrictions = new List<RestrictionModel>();
            var allBasesHaveEnums = true;
            List<XmlSchemaFacet> facets = new();

            if (simpleType.Content is XmlSchemaSimpleTypeRestriction typeRestriction)
            {
                facets = typeRestriction.Facets.Cast<XmlSchemaFacet>().ToList();
            }
            else if (simpleType.Content is XmlSchemaSimpleTypeUnion typeUnion
                && typeUnion.BaseMemberTypes.All(b => b.Content is XmlSchemaSimpleTypeRestriction r && r.Facets.Count > 0))
            {
                var baseFacets = typeUnion.BaseMemberTypes.Select(b => ((XmlSchemaSimpleTypeRestriction)b.Content).Facets.Cast<XmlSchemaFacet>()).ToList();
                // if a union has enum restrictions, there must be an enum restriction in all parts of the union
                allBasesHaveEnums = baseFacets.All(fs => fs.OfType<XmlSchemaEnumerationFacet>().Any());
                facets = baseFacets.SelectMany(f => f).ToList();
            }

            if (facets.Any())
            {
                var enumFacets = facets.OfType<XmlSchemaEnumerationFacet>().ToList();
                // If there are other restrictions mixed into the enumeration values, we'll generate a string to play it safe.
                var isEnum = enumFacets.Any() && allBasesHaveEnums;

                if (isEnum)
                {
                    // we got an enum
                    var name = _configuration.NamingProvider.EnumTypeNameFromQualifiedName(qualifiedName, simpleType);
                    if (namespaceModel != null) { name = namespaceModel.GetUniqueTypeName(name); }

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

                        var deprecated = facet.Annotation != null && facet.Annotation.Items.OfType<XmlSchemaAppInfo>()
                            .Any(a => a.Markup.Any(m => m.Name == "annox:annotate" && m.HasChildNodes && m.FirstChild.Name == "jl:Deprecated"));
                        value.IsDeprecated = deprecated;

                        enumModel.Values.Add(value);
                    }

                    enumModel.Values = EnsureEnumValuesUnique(enumModel.Values);
                    if (namespaceModel != null)
                    {
                        namespaceModel.Types[enumModel.Name] = enumModel;
                    }

                    if (!qualifiedName.IsEmpty)
                    {
                        var key = BuildKey(simpleType, qualifiedName);
                        Types[key] = enumModel;
                    }

                    return enumModel;
                }

                restrictions = GetRestrictions(facets, simpleType).Where(r => r != null).Sanitize().ToList();
            }

            var simpleModelName = _configuration.NamingProvider.SimpleTypeNameFromQualifiedName(qualifiedName, simpleType);
            if (namespaceModel != null) { simpleModelName = namespaceModel.GetUniqueTypeName(simpleModelName); }

            var simpleModel = new SimpleModel(_configuration)
            {
                Name = simpleModelName,
                Namespace = namespaceModel,
                XmlSchemaName = qualifiedName,
                XmlSchemaType = simpleType,
                ValueType = simpleType.Datatype.GetEffectiveType(_configuration, restrictions),
            };

            simpleModel.Documentation.AddRange(docs);
            simpleModel.Restrictions.AddRange(restrictions);

            if (namespaceModel != null)
            {
                namespaceModel.Types[simpleModel.Name] = simpleModel;
            }

            if (!qualifiedName.IsEmpty)
            {
                var key = BuildKey(simpleType, qualifiedName);
                Types[key] = simpleModel;
            }

            return simpleModel;
        }

        private static List<EnumValueModel> EnsureEnumValuesUnique(List<EnumValueModel> enumModelValues)
        {
            var enumValueGroups = from enumValue in enumModelValues
                group enumValue by enumValue.Name;

            foreach (var g in enumValueGroups)
            {
                var i = 1;
                foreach (var t in g.Skip(1))
                {
                    t.Name = $"{t.Name}{i++}";
                }
            }

            return enumModelValues;
        }

        private IEnumerable<PropertyModel> CreatePropertiesForAttributes(Uri source, TypeModel typeModel, IEnumerable<XmlSchemaObject> items,
            List<XmlSchemaObject> processedItems = null)
        {
            processedItems ??= new();
            processedItems.AddRange(items);

            var properties = new List<PropertyModel>();

            foreach (var item in items)
            {
                if (item is XmlSchemaAttribute attribute)
                {
                    if (attribute.Use != XmlSchemaUse.Prohibited)
                    {
                        var attributeQualifiedName = attribute.AttributeSchemaType.QualifiedName;
                        var attributeName = _configuration.NamingProvider.AttributeNameFromQualifiedName(attribute.QualifiedName, attribute);

                        if (attribute.Parent is XmlSchemaAttributeGroup attributeGroup
                            && attributeGroup.QualifiedName != typeModel.XmlSchemaName
                            && Types.TryGetValue(BuildKey(attributeGroup, attributeGroup.QualifiedName), out var typeModelValue)
                            && typeModelValue is InterfaceModel interfaceTypeModel)
                        {
                            var interfaceProperty = interfaceTypeModel.Properties.Single(p => p.XmlSchemaName == attribute.QualifiedName);
                            attributeQualifiedName = interfaceProperty.Type.XmlSchemaName;
                            attributeName = interfaceProperty.Name;
                        }
                        else
                        {
                            if (attributeQualifiedName.IsEmpty)
                            {
                                attributeQualifiedName = attribute.QualifiedName;

                                if (attributeQualifiedName.IsEmpty || string.IsNullOrEmpty(attributeQualifiedName.Namespace))
                                {
                                    // inner type, have to generate a type name
                                    var typeName = _configuration.NamingProvider.PropertyNameFromAttribute(typeModel.Name, attribute.QualifiedName.Name, attribute);
                                    attributeQualifiedName = new XmlQualifiedName(typeName, typeModel.XmlSchemaName.Namespace);
                                    // try to avoid name clashes
                                    if (NameExists(attributeQualifiedName))
                                    {
                                        attributeQualifiedName = new[] { "Item", "Property", "Element" }
                                            .Select(s => new XmlQualifiedName(attributeQualifiedName.Name + s, attributeQualifiedName.Namespace))
                                            .First(n => !NameExists(n));
                                    }
                                }
                            }

                            if (attributeName == typeModel.Name)
                            {
                                attributeName += "Property"; // member names cannot be the same as their enclosing type
                            }
                        }

                        attributeName = typeModel.GetUniquePropertyName(attributeName);

                        var property = new PropertyModel(_configuration)
                        {
                            OwningType = typeModel,
                            Name = attributeName,
                            XmlSchemaName = attribute.QualifiedName,
                            Type = CreateTypeModel(attribute.AttributeSchemaType, attributeQualifiedName),
                            IsAttribute = true,
                            IsNullable = attribute.Use != XmlSchemaUse.Required,
                            DefaultValue = attribute.DefaultValue ?? (attribute.Use != XmlSchemaUse.Optional ? attribute.FixedValue : null),
                            FixedValue = attribute.FixedValue,
                            XmlNamespace = !string.IsNullOrEmpty(attribute.QualifiedName.Namespace) && attribute.QualifiedName.Namespace != typeModel.XmlSchemaName.Namespace ? attribute.QualifiedName.Namespace : null,
                        };

                        if (attribute.Form == XmlSchemaForm.None)
                        {
                            if (attribute.RefName != null && !attribute.RefName.IsEmpty)
                                property.Form = XmlSchemaForm.Qualified;
                            else
                                property.Form = attribute.GetSchema().AttributeFormDefault;
                        }
                        else
                            property.Form = attribute.Form;

                        var attributeDocs = GetDocumentation(attribute);
                        property.Documentation.AddRange(attributeDocs);

                        properties.Add(property);
                    }
                }
                else if (item is XmlSchemaAttributeGroupRef attributeGroupRef)
                {
                    if (_configuration.GenerateInterfaces)
                    {
                        CreateTypeModel(AttributeGroups[attributeGroupRef.RefName], attributeGroupRef.RefName);
                    }

                    var groupItems = AttributeGroups[attributeGroupRef.RefName].Attributes.Cast<XmlSchemaObject>().Except(processedItems).ToList();
                    var groupProperties = CreatePropertiesForAttributes(source, typeModel, groupItems, processedItems);
                    properties.AddRange(groupProperties);
                }
            }

            return properties;
        }

        private IEnumerable<PropertyModel> CreatePropertiesForElements(Uri source, TypeModel typeModel, Particle particle, IEnumerable<Particle> items,
            Substitute substitute = null, int order = 0, List<Particle> processedItems = null)
        {
            var properties = new List<PropertyModel>();
            var xmlParticle = particle.XmlParticle;

            processedItems ??= new();
            processedItems.AddRange(items);

            foreach (var item in items)
            {
                PropertyModel property = null;

                // ElementSchemaType must be non-null. This is not the case when maxOccurs="0".
                if (item.XmlParticle is XmlSchemaElement element && element.ElementSchemaType != null)
                {
                    var elementQualifiedName = element.ElementSchemaType.QualifiedName;

                    if (elementQualifiedName.IsEmpty)
                    {
                        elementQualifiedName = element.RefName;

                        if (elementQualifiedName.IsEmpty)
                        {
                            // inner type, have to generate a type name
                            var typeModelName = xmlParticle is XmlSchemaGroupRef groupRef ? groupRef.RefName : typeModel.XmlSchemaName;
                            var typeName = _configuration.NamingProvider.PropertyNameFromElement(typeModelName.Name, element.QualifiedName.Name, element);
                            elementQualifiedName = new XmlQualifiedName(typeName, typeModel.XmlSchemaName.Namespace);
                            // try to avoid name clashes
                            if (NameExists(elementQualifiedName))
                            {
                                elementQualifiedName = new[] { "Item", "Property", "Element" }
                                    .Select(s => new XmlQualifiedName(elementQualifiedName.Name + s, elementQualifiedName.Namespace))
                                    .First(n => !NameExists(n));
                            }
                        }
                    }

                    var effectiveElement = substitute?.Element ?? element;
                    var propertyName = _configuration.NamingProvider.ElementNameFromQualifiedName(effectiveElement.QualifiedName, effectiveElement);
                    var originalPropertyName = propertyName;
                    if (propertyName == typeModel.Name)
                    {
                        propertyName += "Property"; // member names cannot be the same as their enclosing type
                    }

                    propertyName = typeModel.GetUniquePropertyName(propertyName);

                    property = new PropertyModel(_configuration)
                    {
                        OwningType = typeModel,
                        XmlSchemaName = effectiveElement.QualifiedName,
                        Name = propertyName,
                        OriginalPropertyName = originalPropertyName,
                        Type = substitute?.Type ?? CreateTypeModel(element.ElementSchemaType, elementQualifiedName),
                        IsNillable = element.IsNillable,
                        IsNullable = item.MinOccurs < 1.0m || (item.XmlParent is XmlSchemaChoice),
                        IsCollection = item.MaxOccurs > 1.0m || particle.MaxOccurs > 1.0m, // http://msdn.microsoft.com/en-us/library/vstudio/d3hx2s7e(v=vs.100).aspx
                        DefaultValue = element.DefaultValue ?? ((item.MinOccurs >= 1.0m && item.XmlParent is not XmlSchemaChoice) ? element.FixedValue : null),
                        FixedValue = element.FixedValue,
                        Form = element.Form == XmlSchemaForm.None ? element.GetSchema().ElementFormDefault : element.Form,
                        XmlNamespace = !string.IsNullOrEmpty(effectiveElement.QualifiedName.Namespace) && effectiveElement.QualifiedName.Namespace != typeModel.XmlSchemaName.Namespace
                            ? effectiveElement.QualifiedName.Namespace : null,
                        XmlParticle = item.XmlParticle,
                        XmlParent = item.XmlParent,
                        Particle = item
                    };

                    if (property.IsArray && !_configuration.GenerateComplexTypesForCollections)
                    {
                        property.Type.Namespace.Types.Remove(property.Type.Name);
                    }
                }
                else
                {
                    if (item.XmlParticle is XmlSchemaAny any)
                    {
                        property = new PropertyModel(_configuration)
                        {
                            OwningType = typeModel,
                            Name = "Any",
                            Type = new SimpleModel(_configuration) { ValueType = (_configuration.UseXElementForAny ? typeof(XElement) : typeof(XmlElement)), UseDataTypeAttribute = false },
                            IsNullable = item.MinOccurs < 1.0m || (item.XmlParent is XmlSchemaChoice),
                            IsCollection = item.MaxOccurs > 1.0m || particle.MaxOccurs > 1.0m, // http://msdn.microsoft.com/en-us/library/vstudio/d3hx2s7e(v=vs.100).aspx
                            IsAny = true,
                            XmlParticle = item.XmlParticle,
                            XmlParent = item.XmlParent,
                            Particle = item
                        };
                    }
                    else
                    {
                        if (item.XmlParticle is XmlSchemaGroupRef groupRef)
                        {
                            var group = Groups[groupRef.RefName];

                            if (_configuration.GenerateInterfaces)
                            {
                                CreateTypeModel(group, groupRef.RefName);
                            }

                            var groupItems = GetElements(groupRef.Particle).Where(p => !processedItems.Any(q => p.XmlParticle == q.XmlParticle)).ToList();
                            var groupProperties = CreatePropertiesForElements(source, typeModel, item, groupItems, order: order, processedItems: processedItems).ToList();
                            if (_configuration.EmitOrder)
                            {
                                order += groupProperties.Count;
                            }
                            properties.AddRange(groupProperties);
                        }
                    }
                }

                // Discard duplicate property names. This is most likely due to:
                // - Choice or
                // - Element and attribute with the same name
                if (property != null && !properties.Any(p => p.Name == property.Name))
                {
                    var itemDocs = GetDocumentation(item.XmlParticle);
                    property.Documentation.AddRange(itemDocs);

                    if (_configuration.EmitOrder)
                    {
                        property.Order = order++;
                    }
                    property.IsDeprecated = itemDocs.Any(d => d.Text.StartsWith("DEPRECATED"));

                    properties.Add(property);
                }
            }

            return properties;
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

        private IEnumerable<RestrictionModel> GetRestrictions(IEnumerable<XmlSchemaFacet> facets, XmlSchemaSimpleType type)
        {
            var min = facets.OfType<XmlSchemaMinLengthFacet>().Select(f => int.Parse(f.Value)).DefaultIfEmpty().Max();
            var max = facets.OfType<XmlSchemaMaxLengthFacet>().Select(f => int.Parse(f.Value)).DefaultIfEmpty().Min();

            if (_configuration.DataAnnotationMode == DataAnnotationMode.All)
            {
                if (min > 0) { yield return new MinLengthRestrictionModel(_configuration) { Value = min }; }
                if (max > 0) { yield return new MaxLengthRestrictionModel(_configuration) { Value = max }; }
            }
            else if (min > 0 || max > 0)
            {
                yield return new MinMaxLengthRestrictionModel(_configuration) { Min = min, Max = max };
            }

            foreach (var facet in facets)
            {
                if (facet is XmlSchemaLengthFacet)
                {
                    var value = int.Parse(facet.Value);
                    if (_configuration.DataAnnotationMode == DataAnnotationMode.All)
                    {
                        yield return new MinLengthRestrictionModel(_configuration) { Value = value };
                        yield return new MaxLengthRestrictionModel(_configuration) { Value = value };
                    }
                    else
                    {
                        yield return new MinMaxLengthRestrictionModel(_configuration) { Min = value, Max = value };
                    }
                }

                if (facet is XmlSchemaTotalDigitsFacet)
                {
                    yield return new TotalDigitsRestrictionModel(_configuration) { Value = int.Parse(facet.Value) };
                }
                if (facet is XmlSchemaFractionDigitsFacet)
                {
                    yield return new FractionDigitsRestrictionModel(_configuration) { Value = int.Parse(facet.Value) };
                }

                if (facet is XmlSchemaPatternFacet)
                {
                    yield return new PatternRestrictionModel(_configuration) { Value = facet.Value };
                }

                var valueType = type.Datatype.ValueType;

                if (facet is XmlSchemaMinInclusiveFacet)
                {
                    yield return new MinInclusiveRestrictionModel(_configuration) { Value = facet.Value, Type = valueType };
                }
                if (facet is XmlSchemaMinExclusiveFacet)
                {
                    yield return new MinExclusiveRestrictionModel(_configuration) { Value = facet.Value, Type = valueType };
                }
                if (facet is XmlSchemaMaxInclusiveFacet)
                {
                    yield return new MaxInclusiveRestrictionModel(_configuration) { Value = facet.Value, Type = valueType };
                }
                if (facet is XmlSchemaMaxExclusiveFacet)
                {
                    yield return new MaxExclusiveRestrictionModel(_configuration) { Value = facet.Value, Type = valueType };
                }
            }
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
            if (item == null) { yield break; }

            if (item is XmlSchemaElement element) { yield return new Particle(element, parent); }

            if (item is XmlSchemaAny any) { yield return new Particle(any, parent); }

            if (item is XmlSchemaGroupRef groupRef) { yield return new Particle(groupRef, parent); }

            if (item is XmlSchemaGroupBase itemGroupBase)
            {
                foreach (var groupBaseElement in GetElements(itemGroupBase))
                    yield return groupBaseElement;
            }
        }

        public static List<DocumentationModel> GetDocumentation(XmlSchemaAnnotated annotated)
        {
            if (annotated.Annotation == null) { return new List<DocumentationModel>(); }

            return annotated.Annotation.Items.OfType<XmlSchemaDocumentation>()
                .Where(d => d.Markup != null && d.Markup.Any())
                .Select(d => new DocumentationModel { Language = d.Language, Text = new XText(d.Markup.First().InnerText).ToString() })
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
            if (!string.IsNullOrEmpty(result))
            {
                return result;
            }

            throw new ArgumentException(string.Format("Namespace {0} not provided through map or generator.", xmlNamespace));
        }
    }
}