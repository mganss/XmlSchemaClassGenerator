using System;
using System.CodeDom;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Xml;
using System.Xml.Schema;

namespace XmlSchemaClassGenerator;

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
        var fullTypeName = GenerateCSharpCodeFromExpression(typeOfExpr);
        Debug.Assert(fullTypeName.StartsWith("typeof(") && fullTypeName.EndsWith(")"), $"Expected typeof expression, got: {fullTypeName}");
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
        else if (type == typeof(DateOnly))
        {
            return new CodeMethodInvokeExpression(new CodeTypeReferenceExpression("System.DateOnly"), "Parse", new CodePrimitiveExpression(defaultString));
        }
        else if (type == typeof(TimeOnly))
        {
            return new CodeMethodInvokeExpression(new CodeTypeReferenceExpression("System.TimeOnly"), "Parse", new CodePrimitiveExpression(defaultString));
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
