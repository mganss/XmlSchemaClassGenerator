using Microsoft.CSharp;

using System;
using System.CodeDom;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Schema;

namespace XmlSchemaClassGenerator
{
    public static class CodeUtilities
    {
        // Match non-letter followed by letter
        private static readonly Regex PascalCaseRegex = new(@"[^\p{L}]\p{L}", RegexOptions.Compiled);

        // Uppercases first letter and all letters following non-letters.
        // Examples: testcase -> Testcase, html5element -> Html5Element, test_case -> Test_Case
        public static string ToPascalCase(this string s) => string.IsNullOrEmpty(s) ? s
            : char.ToUpperInvariant(s[0]) + PascalCaseRegex.Replace(s.Substring(1), m => m.Value[0] + char.ToUpperInvariant(m.Value[1]).ToString());

        public static string ToCamelCase(this string s) => string.IsNullOrEmpty(s) ? s
            : char.ToLowerInvariant(s[0]) + s.Substring(1);

        public static string ToBackingField(this string propertyName, string privateFieldPrefix)
            => string.Concat(privateFieldPrefix, propertyName.ToCamelCase());

        public static bool? IsDataTypeAttributeAllowed(this XmlSchemaDatatype type) => type.TypeCode switch
        {
            XmlTypeCode.AnyAtomicType => false,// union
            XmlTypeCode.DateTime or XmlTypeCode.Time or XmlTypeCode.Date or XmlTypeCode.Base64Binary or XmlTypeCode.HexBinary => true,
            _ => false,
        };

        private static Type GetIntegerDerivedType(XmlSchemaDatatype xml, GeneratorConfiguration configuration, IEnumerable<RestrictionModel> restrictions)
        {
            if (configuration.IntegerDataType != null && !configuration.UseIntegerDataTypeAsFallback) return configuration.IntegerDataType;

            decimal? maxInclusive = (restrictions.OfType<MaxInclusiveRestrictionModel>().SingleOrDefault(), xml.TypeCode) switch
            {
                (null, XmlTypeCode.NegativeInteger) => -1,
                (null, XmlTypeCode.NonPositiveInteger) => 0,
                ({ Value: var str }, _) when decimal.TryParse(str, out decimal value) => value,
                _ => null,
            };

            decimal? minInclusive = (restrictions.OfType<MinInclusiveRestrictionModel>().SingleOrDefault(), xml.TypeCode) switch
            {
                (null, XmlTypeCode.PositiveInteger) => 1,
                (null, XmlTypeCode.NonNegativeInteger) => 0,
                ({ Value: var str }, _) when decimal.TryParse(str, out decimal value) => value,
                _ => null,
            };

            // If either value is null, then that value is either unbounded or too large to fit in any numeric type.
            return FromMinMax() ?? FromDigitRestriction(restrictions.OfType<TotalDigitsRestrictionModel>().SingleOrDefault()) ?? FromFallback();

            Type FromMinMax() => (minInclusive, maxInclusive) switch
            {
                (null, _) => null,
                (_, null) => null,
                ( >= byte.MinValue, <= byte.MaxValue) => typeof(byte),
                ( >= sbyte.MinValue, <= sbyte.MaxValue) => typeof(sbyte),
                ( >= ushort.MinValue, <= ushort.MaxValue) => typeof(ushort),
                ( >= short.MinValue, <= short.MaxValue) => typeof(short),
                ( >= uint.MinValue, <= uint.MaxValue) => typeof(uint),
                ( >= int.MinValue, <= int.MaxValue) => typeof(int),
                ( >= ulong.MinValue, <= ulong.MaxValue) => typeof(ulong),
                ( >= long.MinValue, <= long.MaxValue) => typeof(long),
                _ => typeof(decimal),
            };

            Type FromDigitRestriction(TotalDigitsRestrictionModel totalDigits) => xml.TypeCode switch
            {
                XmlTypeCode.PositiveInteger or XmlTypeCode.NonNegativeInteger => totalDigits?.Value switch
                {
                    < 3 => typeof(byte),
                    < 5 => typeof(ushort),
                    < 10 => typeof(uint),
                    < 20 => typeof(ulong),
                    < 30 => typeof(decimal),
                    _ => null
                },
                XmlTypeCode.Integer or XmlTypeCode.NegativeInteger or XmlTypeCode.NonPositiveInteger => totalDigits?.Value switch
                {
                    < 3 => typeof(sbyte),
                    < 5 => typeof(short),
                    < 10 => typeof(int),
                    < 19 => typeof(long),
                    < 29 => typeof(decimal),
                    _ => null
                },
                _ => null,
            };

            Type FromFallback() => configuration.UseIntegerDataTypeAsFallback && configuration.IntegerDataType != null ? configuration.IntegerDataType : typeof(string);
        }

        public static Type GetEffectiveType(this XmlSchemaDatatype type, GeneratorConfiguration configuration, IEnumerable<RestrictionModel> restrictions, bool attribute = false)
        {
            var resultType = type.TypeCode switch
            {
                XmlTypeCode.AnyAtomicType => typeof(string),// union
                XmlTypeCode.AnyUri or XmlTypeCode.GDay or XmlTypeCode.GMonth or XmlTypeCode.GMonthDay or XmlTypeCode.GYear or XmlTypeCode.GYearMonth => typeof(string),
                XmlTypeCode.Duration => configuration.NetCoreSpecificCode ? type.ValueType : typeof(string),
                XmlTypeCode.Time => typeof(DateTime),
                XmlTypeCode.Idref => typeof(string),
                XmlTypeCode.Integer or XmlTypeCode.NegativeInteger or XmlTypeCode.NonNegativeInteger or XmlTypeCode.NonPositiveInteger or XmlTypeCode.PositiveInteger => GetIntegerDerivedType(type, configuration, restrictions),
                _ => type.ValueType,
            };

            if (type.Variety == XmlSchemaDatatypeVariety.List)
            {
                if (resultType.IsArray)
                    resultType = resultType.GetElementType();

                // XmlSerializer doesn't support xsd:list for elements, only for attributes:
                // https://docs.microsoft.com/en-us/previous-versions/dotnet/netframework-4.0/t84dzyst(v%3dvs.100)

                // Also, de/serialization fails when the XML schema type is ambiguous
                // DateTime -> date, datetime, or time
                // byte[] -> hexBinary or base64Binary

                if (!attribute || resultType == typeof(DateTime) || resultType == typeof(byte[]))
                    resultType = typeof(string);
            }

            return resultType;
        }

        public static XmlQualifiedName GetQualifiedName(this XmlSchemaType schemaType)
        {
            return schemaType.QualifiedName.IsEmpty
                ? schemaType.BaseXmlSchemaType.QualifiedName
                : schemaType.QualifiedName;
        }

        public static XmlQualifiedName GetQualifiedName(this TypeModel typeModel)
        {
            XmlQualifiedName qualifiedName;
            if (typeModel is not SimpleModel simpleTypeModel)
            {
                qualifiedName = typeModel.IsAnonymous ? typeModel.XmlSchemaName
                    : typeModel.XmlSchemaType.GetQualifiedName();
            }
            else
            {
                qualifiedName = simpleTypeModel.XmlSchemaType.GetQualifiedName();
                var xmlSchemaType = simpleTypeModel.XmlSchemaType;
                while (qualifiedName.Namespace != XmlSchema.Namespace && xmlSchemaType.BaseXmlSchemaType != null)
                {
                    xmlSchemaType = xmlSchemaType.BaseXmlSchemaType;
                    qualifiedName = xmlSchemaType.GetQualifiedName();
                }
            }
            return qualifiedName;
        }

        public static string GetUniqueTypeName(this NamespaceModel model, string name)
        {
            var n = name;

            for (var i = 2; model.Types.ContainsKey(n) && model.Types[n] is not SimpleModel; i++)
                n = name + i;

            return n;
        }

        public static string GetUniqueFieldName(this TypeModel typeModel, PropertyModel propertyModel)
        {
            var classModel = typeModel as ClassModel;
            var propBackingFieldName = propertyModel.Name.ToBackingField(classModel?.Configuration.PrivateMemberPrefix);

            propBackingFieldName = CSharp.CreateEscapedIdentifier(propBackingFieldName);

            if (classModel == null)
                return propBackingFieldName;

            var i = 0;
            foreach (var prop in classModel.Properties)
            {
                if (propertyModel == prop)
                {
                    i++;
                    break;
                }

                var backingFieldName = prop.Name.ToBackingField(classModel.Configuration.PrivateMemberPrefix);
                if (backingFieldName == propBackingFieldName)
                    i++;
            }

            return i <= 1 ? propBackingFieldName : $"{propBackingFieldName}{i}";
        }

        public static string GetUniquePropertyName(this TypeModel tm, string name)
        {
            if (tm is not ClassModel cls) return name;

            var i = 0;
            var n = name;
            var baseProps = cls.AllBaseClasses.SelectMany(b => b.Properties).ToList();
            var props = cls.Properties.ToList();

            while (baseProps.Concat(props).Any(p => p.Name == n))
                n = name + (++i);

            return n;
        }

        private static readonly Regex NormalizeNewlinesRegex = new(@"(^|[^\r])\n", RegexOptions.Compiled);

        internal static string NormalizeNewlines(string text) => NormalizeNewlinesRegex.Replace(text, "$1\r\n");

        private static readonly CSharpCodeProvider CSharp = new();

        internal static Uri CreateUri(string uri) => string.IsNullOrEmpty(uri) ? null : new Uri(uri);

        public static KeyValuePair<NamespaceKey, string> ParseNamespace(string nsArg, string namespacePrefix)
        {
            var parts = nsArg.Split(new[] { '=' }, 2);
            if (parts.Length != 2)
                throw new ArgumentException("XML and C# namespaces should be separated by '='. You entered: " + nsArg);

            var xmlNs = parts[0];
            var netNs = parts[1];
            var parts2 = xmlNs.Split(new[] { '|' }, 2);
            var source = parts2.Length == 2 ? new Uri(parts2[1], UriKind.RelativeOrAbsolute) : null;
            xmlNs = parts2[0];
            if (!string.IsNullOrEmpty(namespacePrefix))
                netNs = namespacePrefix + "." + netNs;

            return new KeyValuePair<NamespaceKey, string>(new NamespaceKey(source, xmlNs), netNs);
        }

        public static readonly ImmutableList<(string Namespace, Func<GeneratorConfiguration, bool> Condition)> UsingNamespaces = ImmutableList.Create<(string, Func<GeneratorConfiguration, bool>)>(
            ("System", c => c.CompactTypeNames),
            ("System.CodeDom.Compiler", c => c.CompactTypeNames),
            ("System.Collections.Generic", c => c.CompactTypeNames),
            ("System.Collections.ObjectModel", c => c.CompactTypeNames),
            ("System.ComponentModel", c => c.CompactTypeNames),
            ("System.ComponentModel.DataAnnotations", c => c.CompactTypeNames && (c.DataAnnotationMode != DataAnnotationMode.None || c.EntityFramework)),
            ("System.Diagnostics", c => c.CompactTypeNames && c.GenerateDebuggerStepThroughAttribute),
            ("System.Diagnostics.CodeAnalysis", c => c.CompactTypeNames && c.EnableNullableReferenceAttributes),
            ("System.Linq", c => c.EnableDataBinding),
            ("System.Xml", c => c.CompactTypeNames),
            ("System.Xml.Schema", c => c.CompactTypeNames),
            ("System.Xml.Serialization", c => c.CompactTypeNames)
        );

        public static bool IsUsingNamespace(string namespaceName, GeneratorConfiguration conf)
            => UsingNamespaces.Any(n => n.Namespace == namespaceName && n.Condition(conf));

        public static CodeTypeReference CreateTypeReference(Type type, GeneratorConfiguration conf)
        {
            // If the type is a keyword it will prefix it with an @ to make it a valid identifier.
            var isKeyword = CSharp.CreateEscapedIdentifier(CSharp.GetTypeOutput(new(type)))[0] == '@';
            if (!isKeyword && IsUsingNamespace(type.Namespace, conf))
            {
                var typeRef = new CodeTypeReference(type.Name, conf.CodeTypeReferenceOptions);

                if (type.IsConstructedGenericType)
                {
                    var typeArgs = type.GenericTypeArguments.Select(a => CreateTypeReference(a, conf)).ToArray();
                    typeRef.TypeArguments.AddRange(typeArgs);
                }

                return typeRef;
            }
            else
            {
                var typeRef = new CodeTypeReference(type, conf.CodeTypeReferenceOptions);

                foreach (var typeArgRef in typeRef.TypeArguments.OfType<CodeTypeReference>())
                    typeArgRef.Options = conf.CodeTypeReferenceOptions;

                return typeRef;
            }
        }

        public static CodeTypeReference CreateTypeReference(TypeInfo type, GeneratorConfiguration conf)
        {
            return IsUsingNamespace(type.Namespace, conf)
                ? new CodeTypeReference(type.Name, conf.CodeTypeReferenceOptions)
                : new CodeTypeReference($"{type.Namespace}.{type.Name}", conf.CodeTypeReferenceOptions);
        }

        /// <summary>
        /// See https://github.com/mganss/XmlSchemaClassGenerator/issues/245
        /// and https://docs.microsoft.com/en-us/dotnet/api/system.xml.serialization.xmlattributeattribute#remarks
        /// </summary>
        public static bool IsXmlLangOrSpace(XmlQualifiedName name)
            => name?.Namespace == "http://www.w3.org/XML/1998/namespace" && (name.Name == "lang" || name.Name == "space");

        internal static XmlQualifiedName GetQualifiedName(this XmlSchemaObject obj) => obj switch
        {
            XmlSchemaAttribute attr => attr.QualifiedName,
            XmlSchemaAttributeGroup attrGroup => attrGroup.QualifiedName,
            _ => null
        };
    }

    public readonly record struct TypeInfo(string Namespace, string Name);

    /// <summary>
    /// For attributes which can't be referenced by <c>typeof(<see cref="Type"/>)</c>
    /// </summary>
    internal static class Attributes
    {
        private const string DataAnnotations = "System.ComponentModel.DataAnnotations";
        private const string CodeAnalysis = "System.Diagnostics.CodeAnalysis";

        private static TypeInfo Make(string @namespace, [CallerMemberName] string name = null)
            => new(@namespace, name + "Attribute");

        public static TypeInfo Required { get; } = Make(DataAnnotations);
        public static TypeInfo Key { get; } = Make(DataAnnotations);
        public static TypeInfo Range { get; } = Make(DataAnnotations);
        public static TypeInfo MinLength { get; } = Make(DataAnnotations);
        public static TypeInfo MaxLength { get; } = Make(DataAnnotations);
        public static TypeInfo StringLength { get; } = Make(DataAnnotations);
        public static TypeInfo RegularExpression { get; } = Make(DataAnnotations);
        public static TypeInfo NotMapped { get; } = Make($"{DataAnnotations}.Schema");

        public static TypeInfo AllowNull { get; } = Make(CodeAnalysis);
        public static TypeInfo MaybeNull { get; } = Make(CodeAnalysis);
    }
}

//Fixes a bug with VS2019 (https://developercommunity.visualstudio.com/content/problem/1244809/error-cs0518-predefined-type-systemruntimecompiler.html)
namespace System.Runtime.CompilerServices { internal static class IsExternalInit { } }