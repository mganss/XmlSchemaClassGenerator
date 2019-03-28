using System;
using System.CodeDom;
using System.Collections.Generic;
using System.Linq;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Schema;

namespace XmlSchemaClassGenerator
{
    internal class ModelBuilder
    {
        private readonly GeneratorConfiguration _configuration;
        private readonly XmlSchemaSet _set;
        private readonly Dictionary<XmlQualifiedName, XmlSchemaAttributeGroup> AttributeGroups;
        private readonly Dictionary<XmlQualifiedName, XmlSchemaGroup> Groups;
        private readonly Dictionary<NamespaceKey, NamespaceModel> Namespaces = new Dictionary<NamespaceKey, NamespaceModel>();
        private readonly Dictionary<XmlQualifiedName, TypeModel> Types = new Dictionary<XmlQualifiedName, TypeModel>();

        private static readonly XmlQualifiedName AnyType = new XmlQualifiedName("anyType", XmlSchema.Namespace);

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

            Types[AnyType] = objectModel;

            AttributeGroups = set.Schemas().Cast<XmlSchema>().SelectMany(s => s.AttributeGroups.Values.Cast<XmlSchemaAttributeGroup>())
                .DistinctBy(g => g.QualifiedName.ToString())
                .ToDictionary(g => g.QualifiedName);
            Groups = set.Schemas().Cast<XmlSchema>().SelectMany(s => s.Groups.Values.Cast<XmlSchemaGroup>())
                .DistinctBy(g => g.QualifiedName.ToString())
                .ToDictionary(g => g.QualifiedName);

            foreach (var globalType in set.GlobalTypes.Values.Cast<XmlSchemaType>())
            {
                var schema = globalType.GetSchema();
                var source = CodeUtilities.CreateUri(schema?.SourceUri);
                CreateTypeModel(source, globalType, globalType.QualifiedName);
            }

            foreach (var rootElement in set.GlobalElements.Values.Cast<XmlSchemaElement>())
            {
                var rootSchema = rootElement.GetSchema();
                var source = CodeUtilities.CreateUri(rootSchema.SourceUri);
                var qualifiedName = rootElement.ElementSchemaType.QualifiedName;
                if (qualifiedName.IsEmpty) { qualifiedName = rootElement.QualifiedName; }
                var type = CreateTypeModel(source, rootElement.ElementSchemaType, qualifiedName);

                if (type.RootElementName != null)
                {
                    if (type is ClassModel classModel)
                    {
                        // There is already another global element with this type.
                        // Need to create an empty derived class.

                        var derivedClassModel = new ClassModel(_configuration)
                        {
                            Name = _configuration.NamingProvider.RootClassNameFromQualifiedName(rootElement.QualifiedName),
                            Namespace = CreateNamespaceModel(source, rootElement.QualifiedName)
                        };

                        derivedClassModel.Documentation.AddRange(GetDocumentation(rootElement));

                        if (derivedClassModel.Namespace != null)
                        {
                            derivedClassModel.Name = derivedClassModel.Namespace.GetUniqueTypeName(derivedClassModel.Name);
                            derivedClassModel.Namespace.Types[derivedClassModel.Name] = derivedClassModel;
                        }

                        Types[rootElement.QualifiedName] = derivedClassModel;

                        derivedClassModel.BaseClass = classModel;
                        ((ClassModel)derivedClassModel.BaseClass).DerivedTypes.Add(derivedClassModel);

                        derivedClassModel.RootElementName = rootElement.QualifiedName;
                    }
                    else
                    {
                        Types[rootElement.QualifiedName] = type;
                    }
                }
                else
                {
                    if (type is ClassModel classModel)
                    {
                        classModel.Documentation.AddRange(GetDocumentation(rootElement));
                        if (!rootElement.SubstitutionGroup.IsEmpty)
                        {
                            classModel.IsSubstitution = true;
                            classModel.SubstitutionName = rootElement.QualifiedName;
                        }
                    }

                    type.RootElementName = rootElement.QualifiedName;
                }
            }
        }

        // see http://msdn.microsoft.com/en-us/library/z2w0sxhf.aspx
        private static readonly HashSet<string> EnumTypes = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            { "string", "normalizedString", "token", "Name", "NCName", "ID", "ENTITY", "NMTOKEN" };

        private TypeModel CreateTypeModel(Uri source, XmlSchemaAnnotated type, XmlQualifiedName qualifiedName)
        {
            if (!qualifiedName.IsEmpty && Types.TryGetValue(qualifiedName, out TypeModel typeModel))
            {
                return typeModel;
            }

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
            var name = "I" + _configuration.NamingProvider.GroupTypeNameFromQualifiedName(qualifiedName);
            if (namespaceModel != null) { name = namespaceModel.GetUniqueTypeName(name); }

            var interfaceModel = new InterfaceModel(_configuration)
            {
                Name = name,
                Namespace = namespaceModel,
                XmlSchemaName = qualifiedName
            };

            interfaceModel.Documentation.AddRange(docs);

            if (namespaceModel != null) { namespaceModel.Types[name] = interfaceModel; }
            if (!qualifiedName.IsEmpty) { Types[qualifiedName] = interfaceModel; }

            var particle = group.Particle;
            var items = GetElements(particle);
            var properties = CreatePropertiesForElements(source, interfaceModel, particle, items.Where(i => !(i.XmlParticle is XmlSchemaGroupRef)));
            interfaceModel.Properties.AddRange(properties);
            var interfaces = items.Select(i => i.XmlParticle).OfType<XmlSchemaGroupRef>()
                .Select(i => (InterfaceModel)CreateTypeModel(CodeUtilities.CreateUri(i.SourceUri), Groups[i.RefName], i.RefName));
            interfaceModel.Interfaces.AddRange(interfaces);

            return interfaceModel;
        }

        private TypeModel CreateTypeModel(Uri source, XmlSchemaAttributeGroup attributeGroup, NamespaceModel namespaceModel, XmlQualifiedName qualifiedName, List<DocumentationModel> docs)
        {
            var name = "I" + _configuration.NamingProvider.AttributeGroupTypeNameFromQualifiedName(qualifiedName);
            if (namespaceModel != null) { name = namespaceModel.GetUniqueTypeName(name); }

            var interfaceModel = new InterfaceModel(_configuration)
            {
                Name = name,
                Namespace = namespaceModel,
                XmlSchemaName = qualifiedName
            };

            interfaceModel.Documentation.AddRange(docs);

            if (namespaceModel != null) { namespaceModel.Types[name] = interfaceModel; }
            if (!qualifiedName.IsEmpty) { Types[qualifiedName] = interfaceModel; }

            var items = attributeGroup.Attributes;
            var properties = CreatePropertiesForAttributes(source, interfaceModel, items.OfType<XmlSchemaAttribute>());
            interfaceModel.Properties.AddRange(properties);
            var interfaces = items.OfType<XmlSchemaAttributeGroupRef>()
                .Select(a => (InterfaceModel)CreateTypeModel(CodeUtilities.CreateUri(a.SourceUri), AttributeGroups[a.RefName], a.RefName));
            interfaceModel.Interfaces.AddRange(interfaces);

            return interfaceModel;
        }

        private TypeModel CreateTypeModel(Uri source, XmlSchemaComplexType complexType, NamespaceModel namespaceModel, XmlQualifiedName qualifiedName, List<DocumentationModel> docs)
        {
            var name = _configuration.NamingProvider.ComplexTypeNameFromQualifiedName(qualifiedName);
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
                IsAnonymous = complexType.QualifiedName.Name == "",
                IsMixed = complexType.IsMixed,
                IsSubstitution = complexType.Parent is XmlSchemaElement && !((XmlSchemaElement)complexType.Parent).SubstitutionGroup.IsEmpty
            };

            classModel.Documentation.AddRange(docs);

            if (namespaceModel != null)
            {
                namespaceModel.Types[classModel.Name] = classModel;
            }

            if (!qualifiedName.IsEmpty) { Types[qualifiedName] = classModel; }

            if (complexType.BaseXmlSchemaType != null && complexType.BaseXmlSchemaType.QualifiedName != AnyType)
            {
                var baseModel = CreateTypeModel(source, complexType.BaseXmlSchemaType, complexType.BaseXmlSchemaType.QualifiedName);
                classModel.BaseClass = baseModel;
                if (baseModel is ClassModel baseClassModel) { baseClassModel.DerivedTypes.Add(classModel); }
            }

            XmlSchemaParticle particle = null;
            if (classModel.BaseClass != null)
            {
                if (complexType.ContentModel.Content is XmlSchemaComplexContentExtension complexContent)
                {
                    particle = complexContent.Particle;
                }

                // If it's a restriction, do not duplicate elements on the derived class, they're already in the base class.
                // See https://msdn.microsoft.com/en-us/library/f3z3wh0y.aspx
            }
            else particle = complexType.Particle ?? complexType.ContentTypeParticle;

            var items = GetElements(particle, complexType);
            var properties = CreatePropertiesForElements(source, classModel, particle, items);
            classModel.Properties.AddRange(properties);

            if (_configuration.GenerateInterfaces)
            {
                var interfaces = items.Select(i => i.XmlParticle).OfType<XmlSchemaGroupRef>()
                    .Select(i => (InterfaceModel)CreateTypeModel(CodeUtilities.CreateUri(i.SourceUri), Groups[i.RefName], i.RefName));
                classModel.Interfaces.AddRange(interfaces);
            }

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
            else { attributes = complexType.Attributes; }

            if (attributes != null)
            {
                var attributeProperties = CreatePropertiesForAttributes(source, classModel, attributes.Cast<XmlSchemaObject>());
                classModel.Properties.AddRange(attributeProperties);

                if (_configuration.GenerateInterfaces)
                {
                    var attributeInterfaces = attributes.OfType<XmlSchemaAttributeGroupRef>()
                        .Select(i => (InterfaceModel)CreateTypeModel(CodeUtilities.CreateUri(i.SourceUri), AttributeGroups[i.RefName], i.RefName));
                    classModel.Interfaces.AddRange(attributeInterfaces);
                }
            }

            if (complexType.AnyAttribute != null)
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

                var attributeDocs = GetDocumentation(complexType.AnyAttribute);
                property.Documentation.AddRange(attributeDocs);

                classModel.Properties.Add(property);
            }

            return classModel;
        }

        private TypeModel CreateTypeModel(XmlSchemaSimpleType simpleType, NamespaceModel namespaceModel, XmlQualifiedName qualifiedName, List<DocumentationModel> docs)
        {
            var restrictions = new List<RestrictionModel>();

            if (simpleType.Content is XmlSchemaSimpleTypeRestriction typeRestriction)
            {
                var enumFacets = typeRestriction.Facets.OfType<XmlSchemaEnumerationFacet>().ToList();
                var isEnum = (enumFacets.Count == typeRestriction.Facets.Count && enumFacets.Count != 0)
                                || (EnumTypes.Contains(typeRestriction.BaseTypeName.Name) && enumFacets.Any());
                if (isEnum)
                {
                    // we got an enum
                    var name = _configuration.NamingProvider.EnumTypeNameFromQualifiedName(qualifiedName);
                    if (namespaceModel != null) { name = namespaceModel.GetUniqueTypeName(name); }

                    var enumModel = new EnumModel(_configuration)
                    {
                        Name = name,
                        Namespace = namespaceModel,
                        XmlSchemaName = qualifiedName,
                        XmlSchemaType = simpleType,
                    };

                    enumModel.Documentation.AddRange(docs);

                    foreach (var facet in enumFacets.DistinctBy(f => f.Value))
                    {
                        var value = new EnumValueModel
                        {
                            Name = _configuration.NamingProvider.EnumMemberNameFromValue(enumModel.Name, facet.Value),
                            Value = facet.Value
                        };

                        var valueDocs = GetDocumentation(facet);
                        value.Documentation.AddRange(valueDocs);

                        var deprecated = facet.Annotation != null && facet.Annotation.Items.OfType<XmlSchemaAppInfo>()
                            .Any(a => a.Markup.Any(m => m.Name == "annox:annotate" && m.HasChildNodes && m.FirstChild.Name == "jl:Deprecated"));
                        value.IsDeprecated = deprecated;

                        enumModel.Values.Add(value);
                    }

                    if (namespaceModel != null)
                    {
                        namespaceModel.Types[enumModel.Name] = enumModel;
                    }

                    if (!qualifiedName.IsEmpty)
                    {
                        Types[qualifiedName] = enumModel;
                    }

                    return enumModel;
                }

                restrictions = GetRestrictions(typeRestriction.Facets.Cast<XmlSchemaFacet>(), simpleType).Where(r => r != null).Sanitize().ToList();
            }

            var simpleModelName = _configuration.NamingProvider.SimpleTypeNameFromQualifiedName(qualifiedName);
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

            if (!qualifiedName.IsEmpty) { Types[qualifiedName] = simpleModel; }

            return simpleModel;
        }

        private IEnumerable<PropertyModel> CreatePropertiesForAttributes(Uri source, TypeModel typeModel, IEnumerable<XmlSchemaObject> items)
        {
            var properties = new List<PropertyModel>();

            foreach (var item in items)
            {
                if (item is XmlSchemaAttribute attribute)
                {
                    if (attribute.Use != XmlSchemaUse.Prohibited)
                    {
                        var attributeQualifiedName = attribute.AttributeSchemaType.QualifiedName;
                        var attributeName = _configuration.NamingProvider.AttributeNameFromQualifiedName(attribute.QualifiedName);

                        if (attribute.Parent is XmlSchemaAttributeGroup attributeGroup && attributeGroup.QualifiedName != typeModel.XmlSchemaName && Types.TryGetValue(attributeGroup.QualifiedName, out var typeModelValue) && typeModelValue is InterfaceModel interfaceTypeModel)
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

                                if (attributeQualifiedName.IsEmpty || attributeQualifiedName.Namespace == "")
                                {
                                    // inner type, have to generate a type name
                                    var typeName = _configuration.NamingProvider.PropertyNameFromAttribute(typeModel.Name, attribute.QualifiedName.Name);
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

                        var property = new PropertyModel(_configuration)
                        {
                            OwningType = typeModel,
                            Name = attributeName,
                            XmlSchemaName = attribute.QualifiedName,
                            Type = CreateTypeModel(source, attribute.AttributeSchemaType, attributeQualifiedName),
                            IsAttribute = true,
                            IsNullable = attribute.Use != XmlSchemaUse.Required,
                            DefaultValue = attribute.DefaultValue ?? (attribute.Use != XmlSchemaUse.Optional ? attribute.FixedValue : null),
                            FixedValue = attribute.FixedValue,
                            Form = attribute.Form == XmlSchemaForm.None ? attribute.GetSchema().AttributeFormDefault : attribute.Form,
                            XmlNamespace = attribute.QualifiedName.Namespace != "" && attribute.QualifiedName.Namespace != typeModel.XmlSchemaName.Namespace ? attribute.QualifiedName.Namespace : null,
                        };

                        var attributeDocs = GetDocumentation(attribute);
                        property.Documentation.AddRange(attributeDocs);

                        properties.Add(property);
                    }
                }
                else if (item is XmlSchemaAttributeGroupRef attributeGroupRef)
                {
                    if (_configuration.GenerateInterfaces)
                    {
                        CreateTypeModel(CodeUtilities.CreateUri(attributeGroupRef.SourceUri), AttributeGroups[attributeGroupRef.RefName], attributeGroupRef.RefName);
                    }

                    var groupItems = AttributeGroups[attributeGroupRef.RefName].Attributes;
                    var groupProperties = CreatePropertiesForAttributes(source, typeModel, groupItems.Cast<XmlSchemaObject>());
                    properties.AddRange(groupProperties);
                }
            }

            return properties;
        }

        private IEnumerable<PropertyModel> CreatePropertiesForElements(Uri source, TypeModel typeModel, XmlSchemaParticle particle, IEnumerable<Particle> items)
        {
            var properties = new List<PropertyModel>();
            var order = 0;

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
                            var typeModelName = particle is XmlSchemaGroupRef groupRef ? groupRef.RefName : typeModel.XmlSchemaName;
                            var typeName = _configuration.NamingProvider.PropertyNameFromElement(typeModelName.Name, element.QualifiedName.Name);
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

                    var propertyName = _configuration.NamingProvider.ElementNameFromQualifiedName(element.QualifiedName);
                    if (propertyName == typeModel.Name)
                    {
                        propertyName += "Property"; // member names cannot be the same as their enclosing type
                    }

                    property = new PropertyModel(_configuration)
                    {
                        OwningType = typeModel,
                        XmlSchemaName = element.QualifiedName,
                        Name = propertyName,
                        Type = CreateTypeModel(source, element.ElementSchemaType, elementQualifiedName),
                        IsNillable = element.IsNillable,
                        IsNullable = item.MinOccurs < 1.0m || (item.XmlParent is XmlSchemaChoice),
                        IsCollection = item.MaxOccurs > 1.0m || particle.MaxOccurs > 1.0m, // http://msdn.microsoft.com/en-us/library/vstudio/d3hx2s7e(v=vs.100).aspx
                        DefaultValue = element.DefaultValue ?? ((item.MinOccurs >= 1.0m && !(item.XmlParent is XmlSchemaChoice)) ? element.FixedValue : null),
                        FixedValue = element.FixedValue,
                        Form = element.Form == XmlSchemaForm.None ? element.GetSchema().ElementFormDefault : element.Form,
                        XmlNamespace = element.QualifiedName.Namespace != "" && element.QualifiedName.Namespace != typeModel.XmlSchemaName.Namespace ? element.QualifiedName.Namespace : null,
                        XmlParticle = item.XmlParticle,
                        XmlParent = item.XmlParent,
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
                        };
                    }
                    else
                    {
                        if (item.XmlParticle is XmlSchemaGroupRef groupRef)
                        {
                            if (_configuration.GenerateInterfaces)
                            {
                                CreateTypeModel(CodeUtilities.CreateUri(groupRef.SourceUri), Groups[groupRef.RefName], groupRef.RefName);
                            }

                            var groupItems = GetElements(groupRef.Particle);
                            var groupProperties = CreatePropertiesForElements(source, typeModel, item.XmlParticle, groupItems);
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

        public List<DocumentationModel> GetDocumentation(XmlSchemaAnnotated annotated)
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
                .Select(nhi => NamespaceModel.Generate(nhi.FullName, nhi.Models));
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