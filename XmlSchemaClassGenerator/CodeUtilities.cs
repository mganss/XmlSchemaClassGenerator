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
        public static string ToLegacyPascalCase(this string s) => string.IsNullOrEmpty(s) ? s
            : char.ToUpperInvariant(s[0]) + PascalCaseRegex.Replace(s.Substring(1), m => m.Value[0] + char.ToUpperInvariant(m.Value[1]).ToString());

        private static readonly Regex invalidCharsRgx = new(@"[^_\p{L}\p{N}]");
        private static readonly Regex whiteSpace = new(@"(?<=\s)");
        private static readonly Regex startsWithLowerCaseChar = new(@"^\p{Ll}");
        private static readonly Regex firstCharFollowedByUpperCasesOnly = new(@"(?<=\p{Lu})[\p{Lu}\p{N}]+$", RegexOptions.None, TimeSpan.FromSeconds(1));
        private static readonly Regex lowerCaseNextToNumber = new(@"(?<=\p{N})\p{Ll}");
        private static readonly Regex upperCaseInside = new(@"(?<=\p{Lu})\p{Lu}+?((?=\p{Lu}\p{Ll})|(?=\p{N}))", RegexOptions.None, TimeSpan.FromSeconds(1));

        // Credits: chviLadislav
        // https://stackoverflow.com/questions/18627112/how-can-i-convert-text-to-pascal-case
        // Example output:
        //   "WARD_VS_VITAL_SIGNS"          "WardVsVitalSigns"
        //   "Who am I?"                    "WhoAmI"
        //   "I ate before you got here"    "IAteBeforeYouGotHere"
        //   "Hello|Who|Am|I?"              "HelloWhoAmI"
        //   "Live long and prosper"        "LiveLongAndProsper"
        //   "Lorem ipsum dolor..."         "LoremIpsumDolor"
        //   "CoolSP"                       "CoolSp"
        //   "AB9CD"                        "Ab9Cd"
        //   "CCCTrigger"                   "CccTrigger"
        //   "CIRC"                         "Circ"
        //   "ID_SOME"                      "IdSome"
        //   "ID_SomeOther"                 "IdSomeOther"
        //   "ID_SOMEOther"                 "IdSomeOther"
        //   "CCC_SOME_2Phases"             "CccSome2Phases"
        //   "AlreadyGoodPascalCase"        "AlreadyGoodPascalCase"
        //   "999 999 99 9 "                "999999999"
        //   "1 2 3 "                       "123"
        //   "1 AB cd EFDDD 8"              "1AbCdEfddd8"
        //   "INVALID VALUE AND _2THINGS"   "InvalidValueAnd2Things"
        public static string ToPascalCase(this string original)
        {
            // replace white spaces with undescore, then replace all invalid chars with undescore
            var pascalCase = invalidCharsRgx.Replace(whiteSpace.Replace(original, "_"), "_")
                // split by underscores
                .Split(['_'], StringSplitOptions.RemoveEmptyEntries)
                // set first letter to uppercase
                .Select(w => startsWithLowerCaseChar.Replace(w, m => m.Value.ToUpper()))
                // replace second and all following upper case letters to lower if there is no next lower (ABC -> Abc)
                .Select(w => firstCharFollowedByUpperCasesOnly.Replace(w, m => m.Value.ToLower()))
                // set upper case the first lower case following a number (Ab9cd -> Ab9Cd)
                .Select(w => lowerCaseNextToNumber.Replace(w, m => m.Value.ToUpper()))
                // lower second and next upper case letters except the last if it follows by any lower (ABcDEf -> AbcDef)
                .Select(w => upperCaseInside.Replace(w, m => m.Value.ToLower()));

            return string.Concat(pascalCase);
        }

        public static string ToCamelCase(this string s) => string.IsNullOrEmpty(s) ? s
                : char.ToLowerInvariant(s[0]) + s.Substring(1);

        public static string ToBackingField(this string propertyName, string privateFieldPrefix)
            => string.Concat(privateFieldPrefix, propertyName.ToCamelCase());

        public static bool? IsDataTypeAttributeAllowed(this XmlSchemaDatatype type, GeneratorConfiguration configuration) => type.TypeCode switch
        {
            XmlTypeCode.AnyAtomicType => false,// union
            XmlTypeCode.DateTime or XmlTypeCode.Time => !configuration.DateTimeWithTimeZone,
            XmlTypeCode.Date or XmlTypeCode.Base64Binary or XmlTypeCode.HexBinary => true,
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

            Type FromDigitRestriction(TotalDigitsRestrictionModel totalDigits
            ) => xml.TypeCode switch
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
                XmlTypeCode.Integer or XmlTypeCode.NegativeInteger or XmlTypeCode.NonPositiveInteger => totalDigits
              ?.Value switch
                {
                    < 3 => typeof(sbyte),
                    < 5 => typeof(short),
                    < 10 => typeof(int),
                    < 19 => typeof(long),
                    < 29 => typeof(decimal),
                    _ => null
                },
                XmlTypeCode.Decimal
            when restrictions.OfType<FractionDigitsRestrictionModel>().SingleOrDefault() is { IsSupported: true, Value: 0 } => totalDigits
              ?.Value switch
            {
                < 3 => typeof(sbyte),
                < 5 => typeof(short),
                < 10 => typeof(int),
                < 19 => typeof(long),
                < 29 => typeof(decimal),
                _ => null
            },
                _ => null
            };

            Type FromFallback() => configuration.UseIntegerDataTypeAsFallback && configuration.IntegerDataType != null ? configuration.IntegerDataType : typeof(string);
        }

        private static readonly XmlQualifiedName GuidQualifiedName = new("guid", "http://microsoft.com/wsdl/types/");

        public static Type GetEffectiveType(this XmlSchemaDatatype type, GeneratorConfiguration configuration, IEnumerable<RestrictionModel> restrictions, XmlSchemaType schemaType, bool attribute = false)
        {
            var resultType = type.TypeCode switch
            {
                XmlTypeCode.AnyAtomicType => configuration.MapUnionToWidestCommonType ? GetUnionType(configuration, schemaType, attribute) : typeof(string), // union
                XmlTypeCode.AnyUri or XmlTypeCode.GDay or XmlTypeCode.GMonth or XmlTypeCode.GMonthDay or XmlTypeCode.GYear or XmlTypeCode.GYearMonth => typeof(string),
                XmlTypeCode.Duration => configuration.NetCoreSpecificCode ? type.ValueType : typeof(string),
                XmlTypeCode.Time or XmlTypeCode.DateTime => configuration.DateTimeWithTimeZone ? typeof(DateTimeOffset) : typeof(DateTime),
                XmlTypeCode.Idref => typeof(string),
                XmlTypeCode.Integer or XmlTypeCode.NegativeInteger or XmlTypeCode.NonNegativeInteger or XmlTypeCode.NonPositiveInteger or XmlTypeCode.PositiveInteger => GetIntegerDerivedType(type, configuration, restrictions),
                XmlTypeCode.Decimal when restrictions.OfType<FractionDigitsRestrictionModel>().SingleOrDefault() is { IsSupported: true, Value: 0 } => GetIntegerDerivedType(type, configuration, restrictions),
                _ => type.ValueType,
            };

            if (schemaType.IsDerivedFrom(GuidQualifiedName))
            {
                resultType = typeof(Guid);
            }

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

        static readonly Type[] intTypes = [typeof(byte), typeof(sbyte), typeof(ushort), typeof(short), typeof(uint), typeof(int), typeof(ulong), typeof(long), typeof(decimal)];
        static readonly Type[] decimalTypes = [typeof(float), typeof(double), typeof(decimal)];

        private static Type GetUnionType(GeneratorConfiguration configuration, XmlSchemaType schemaType, bool attribute)
        {
            if (schemaType is not XmlSchemaSimpleType simpleType || simpleType.Content is not XmlSchemaSimpleTypeUnion unionType) return typeof(string);

            var baseMemberEffectiveTypes = unionType.BaseMemberTypes.Select(t =>
            {
                var restriction = t.Content as XmlSchemaSimpleTypeRestriction;
                var facets = restriction?.Facets.OfType<XmlSchemaFacet>().ToList();
                var restrictions = GetRestrictions(facets, t, configuration).Where(r => r != null).Sanitize().ToList();
                return GetEffectiveType(t.Datatype, configuration, restrictions, t, attribute);
            }).ToList();

            // all member types are the same
            if (baseMemberEffectiveTypes.Distinct().Count() == 1) return baseMemberEffectiveTypes[0];

            // all member types are integer types
            if (baseMemberEffectiveTypes.TrueForAll(t => intTypes.Contains(t)))
            {
                var maxTypeIndex = baseMemberEffectiveTypes.Max(t => Array.IndexOf(intTypes, t));
                var maxType = intTypes[maxTypeIndex];
                // if the max type is signed and the corresponding unsigned type is also in the set we have to use the next higher type
                if (maxTypeIndex % 2 == 1 && baseMemberEffectiveTypes.Exists(t => Array.IndexOf(intTypes, t) == maxTypeIndex - 1))
                    return intTypes[maxTypeIndex + 1];
                return maxType;
            }

            // all member types are float/double/decimal
            if (baseMemberEffectiveTypes.TrueForAll(t => decimalTypes.Contains(t)))
            {
                var maxTypeIndex = baseMemberEffectiveTypes.Max(t => Array.IndexOf(decimalTypes, t));
                var maxType = decimalTypes[maxTypeIndex];
                return maxType;
            }

            return typeof(string);
        }

        public static IEnumerable<RestrictionModel> GetRestrictions(IEnumerable<XmlSchemaFacet> facets, XmlSchemaSimpleType type, GeneratorConfiguration _configuration)
        {
            var len = facets.OfType<XmlSchemaLengthFacet>().Select(f => int.Parse(f.Value)).ToList();
            var min = facets.OfType<XmlSchemaMinLengthFacet>().Select(f => int.Parse(f.Value))
                .Union(len)
                .DefaultIfEmpty()
                .Max();
            var max = facets.OfType<XmlSchemaMaxLengthFacet>().Select(f => int.Parse(f.Value))
                .Union(len)
                .DefaultIfEmpty()
                .Min();

            if (_configuration.DataAnnotationMode == DataAnnotationMode.All)
            {
                if (min > 0) yield return new MinLengthRestrictionModel(_configuration) { Value = min };
                if (max > 0) yield return new MaxLengthRestrictionModel(_configuration) { Value = max };
            }
            else if (min > 0 || max > 0)
            {
                yield return new MinMaxLengthRestrictionModel(_configuration) { Min = min, Max = max };
            }

            foreach (var facet in facets)
            {
                var valueType = type.Datatype.ValueType;
                switch (facet)
                {
                    case XmlSchemaTotalDigitsFacet:
                        yield return new TotalDigitsRestrictionModel(_configuration) { Value = int.Parse(facet.Value) }; break;
                    case XmlSchemaFractionDigitsFacet:
                        yield return new FractionDigitsRestrictionModel(_configuration) { Value = int.Parse(facet.Value) }; break;
                    case XmlSchemaPatternFacet:
                        yield return new PatternRestrictionModel(_configuration) { Value = facet.Value }; break;
                    case XmlSchemaMinInclusiveFacet:
                        yield return new MinInclusiveRestrictionModel(_configuration) { Value = facet.Value, Type = valueType }; break;
                    case XmlSchemaMinExclusiveFacet:
                        yield return new MinExclusiveRestrictionModel(_configuration) { Value = facet.Value, Type = valueType }; break;
                    case XmlSchemaMaxInclusiveFacet:
                        yield return new MaxInclusiveRestrictionModel(_configuration) { Value = facet.Value, Type = valueType }; break;
                    case XmlSchemaMaxExclusiveFacet:
                        yield return new MaxExclusiveRestrictionModel(_configuration) { Value = facet.Value, Type = valueType }; break;
                }
            }
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
                if (simpleTypeModel.XmlSchemaType == null) return simpleTypeModel.XmlSchemaName;
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
            var i = 2;

            while (model.Types.ContainsKey(n) && model.Types[n] is not SimpleModel)
            {
                n = name + i;
                i++;
            }

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

        public static string GetUniquePropertyName(this TypeModel tm, string name, IList<PropertyModel> properties)
        {
            if (tm is not ClassModel cls) return name;

            var i = 0;
            var n = name;
            var baseProps = cls.AllBaseClasses.SelectMany(b => b.Properties).ToList();
            var props = cls.Properties.ToList();

            while (baseProps.Concat(props).Concat(properties).Any(p => p.Name == n))
                n = name + (++i);

            return n;
        }

        private static readonly Regex NormalizeNewlinesRegex = new(@"(^|[^\r])\n", RegexOptions.Compiled);

        internal static string NormalizeNewlines(string text) => NormalizeNewlinesRegex.Replace(text, "$1\r\n");

        private static readonly CSharpCodeProvider CSharp = new();

        internal static Uri CreateUri(string uri) => string.IsNullOrEmpty(uri) ? null : new Uri(uri);

        public static KeyValuePair<NamespaceKey, string> ParseNamespace(string nsArg, string namespacePrefix)
        {
            var parts = nsArg.Split(['='], 2);

            if (parts.Length == 1)
                parts = [string.Empty, parts[0]];

            if (parts.Length != 2)
                throw new ArgumentException("XML and C# namespaces should be separated by '='. You entered: " + nsArg);

            var xmlNs = parts[0];
            var netNs = parts[1];
            var parts2 = xmlNs.Split(['|'], 2);
            var source = parts2.Length == 2 ? new Uri(parts2[1], UriKind.RelativeOrAbsolute) : null;
            xmlNs = parts2[0];
            if (!string.IsNullOrEmpty(namespacePrefix))
                netNs = namespacePrefix + "." + netNs;

            return new KeyValuePair<NamespaceKey, string>(new NamespaceKey(source, xmlNs), netNs);
        }

        public static readonly ImmutableList<(string Namespace, Func<GeneratorConfiguration, bool> Condition)> UsingNamespaces =
        [
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
,
        ];

        public static bool IsUsingNamespace(string namespaceName, GeneratorConfiguration conf)
            => UsingNamespaces.Exists(n => n.Namespace == namespaceName && n.Condition(conf));

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
