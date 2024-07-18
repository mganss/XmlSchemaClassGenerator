using XmlSchemaClassGenerator;
using XmlSchemaClassGenerator.NamingProviders;
using System;
using System.CodeDom;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Mono.Options;
using Ganss.IO;

namespace XmlSchemaClassGenerator.Console
{
    static class Program
    {
        static int Main(string[] args)
        {
            var showHelp = args.Length == 0;
            var namespaces = new List<string>();
            var nameSubstitutes = new List<string>();
            var outputFolder = (string)null;
            bool dateTimeWithTimeZone = false;
            Type integerType = null;
            var useIntegerTypeAsFallback = false;
            var namespacePrefix = "";
            var verbose = false;
            var nullables = false;
            var pclCompatible = false;
            var enableDataBinding = false;
            var emitOrder = false;
            var entityFramework = false;
            var interfaces = true;
            var pascal = true;
            var assembly = false;
            var collectionType = typeof(Collection<>);
            Type collectionImplementationType = null;
            var codeTypeReferenceOptions = default(CodeTypeReferenceOptions);
            string textValuePropertyName = "Value";
            var generateDebuggerStepThroughAttribute = true;
            var disableComments = false;
            var doNotUseUnderscoreInPrivateMemberNames = false;
            var generateDescriptionAttribute = true;
            var enableUpaCheck = true;
            var generateComplexTypesForCollections = true;
            var useShouldSerialize = false;
            var separateClasses = false;
            var separateSubstitutes = false;
            var collectionSettersMode = CollectionSettersMode.Private;
            var doNotForceIsNullable = false;
            var compactTypeNames = false;
            var commentLanguages = new[] { "en" };
            var supportedCommentLanguages = new[] { "en", "de" };
            var uniqueTypeNamesAcrossNamespaces = false;
            var createGeneratedCodeAttributeVersion = true;
            var netCoreSpecificCode = false;
            var nullableReferenceAttributes = false;
            var generateCommandLineArgs = true;
            var useArrayItemAttribute = true;
            var enumAsString = false;
            var namespaceFiles = new List<string>();
            var nameSubstituteFiles = new List<string>();
            var unionCommonType = false;
            var separateNamespaceHierarchy = false;
            var serializeEmptyCollections = false;
            var allowDtdParse = false;
            NamingScheme? namingScheme = null;

            var options = new OptionSet {
                { "h|help", "show this message and exit", v => showHelp = v != null },
                { "n|namespace=", @"map an XML namespace to a C# namespace
Separate XML namespace and C# namespace by '='.
A single value (no '=') is taken as the C# namespace the empty XML namespace is mapped to.
One option must be given for each namespace to be mapped.
A file name may be given by appending a pipe sign (|) followed by a file name (like schema.xsd) to the XML namespace.
If no mapping is found for an XML namespace, a name is generated automatically (may fail).", v => namespaces.Add(v) },
                { "nf|namespaceFile=", @"file containing mappings from XML namespaces to C# namespaces
The line format is one mapping per line: XML namespace = C# namespace [optional file name].
Lines starting with # and empty lines are ignored.", v => namespaceFiles.Add(v) },
                { "tns|typeNameSubstitute=", @"substitute a generated type/member name
Separate type/member name and substitute name by '='.
Prefix type/member name with an appropriate kind ID as documented at: https://t.ly/HHEI.
Prefix with 'A:' to substitute any type/member.", v => nameSubstitutes.Add(v) },
                { "tnsf|typeNameSubstituteFile=", @"file containing generated type/member name substitute mappings
The line format is one mapping per line: prefixed type/member name = substitute name.
Lines starting with # and empty lines are ignored.", v => nameSubstituteFiles.Add(v) },
                { "o|output=", "the {FOLDER} to write the resulting .cs files to", v => outputFolder = v },
                { "d|datetime-offset", "map xs:datetime and derived types to System.DateTimeOffset instead of System.DateTime", v => dateTimeWithTimeZone = v != null },
                { "i|integer=", @"map xs:integer and derived types to {TYPE} instead of automatic approximation
{TYPE} can be i[nt], l[ong], or d[ecimal]", v => {
                                         switch (v)
                                         {
                                             case "i":
                                             case "int":
                                                 integerType = typeof(int);
                                                 break;
                                             case "l":
                                             case "long":
                                                 integerType = typeof(long);
                                                 break;
                                             case "d":
                                             case "decimal":
                                                 integerType = typeof(decimal);
                                                 break;
                                         }
                                     } },
                { "fb|fallback|use-integer-type-as-fallback", "use integer type specified via -i only if no type can be deduced", v => useIntegerTypeAsFallback = v != null },
                { "e|edb|enable-data-binding", "enable INotifyPropertyChanged data binding", v => enableDataBinding = v != null },
                { "r|order", "emit order for all class members stored as XML element", v => emitOrder = v != null },
                { "c|pcl", "PCL compatible output", v => pclCompatible = v != null },
                { "p|prefix=", "the {PREFIX} to prepend to auto-generated namespace names", v => namespacePrefix = v },
                { "v|verbose", "print generated file names on stdout", v => verbose = v != null },
                { "0|nullable", "generate nullable adapter properties for optional elements/attributes w/o default values", v => nullables = v != null },
                { "f|ef", "generate Entity Framework Code First compatible classes", v => entityFramework = v != null },
                { "t|interface", "generate interfaces for groups and attribute groups (default is enabled)", v => interfaces = v != null },
                { "a|pascal", "use Pascal case for class and property names (default is enabled)", v => pascal = v != null },
                { "av|assemblyVisible", "use the internal visibility modifier (default is false)", v => assembly = v != null },
                { "u|enableUpaCheck", "should XmlSchemaSet check for Unique Particle Attribution (UPA) (default is enabled)", v => enableUpaCheck = v != null },
                { "ct|collectionType=", "collection type to use (default is " + typeof(Collection<>).FullName + ")", v => collectionType = v == null ? typeof(Collection<>) : Type.GetType(v, true) },
                { "cit|collectionImplementationType=", "the default collection type implementation to use (default is null)", v => collectionImplementationType = v == null ? null : Type.GetType(v, true) },
                { "csm|collectionSettersMode=", @"generate a private, public, or init-only setter
with or without backing field initialization for collections
(default is Private; can be: {Private, Public, PublicWithoutConstructorInitialization, Init, InitWithoutConstructorInitialization})",
                    v =>
                    {
                        collectionSettersMode = v switch
                        {
                            "pr" or "Private" => CollectionSettersMode.Private,
                            "pu" or "Public" => CollectionSettersMode.Public,
                            "puwci" or "PublicWithoutConstructorInitialization" =>
                                CollectionSettersMode.PublicWithoutConstructorInitialization,
                            "in" or "Init" => CollectionSettersMode.Init,
                            "inwci" or "InitWithoutConstructorInitialization" =>
                                CollectionSettersMode.InitWithoutConstructorInitialization,
                            _ => CollectionSettersMode.Private
                        };
                    }},
                { "ctro|codeTypeReferenceOptions=", "the default CodeTypeReferenceOptions Flags to use (default is unset; can be: {GlobalReference, GenericTypeParameter})", v => codeTypeReferenceOptions = v == null ? default : (CodeTypeReferenceOptions)Enum.Parse(typeof(CodeTypeReferenceOptions), v, false) },
                { "tvpn|textValuePropertyName=", "the name of the property that holds the text value of an element (default is Value)", v => textValuePropertyName = v },
                { "dst|debuggerStepThrough", "generate DebuggerStepThroughAttribute (default is enabled)", v => generateDebuggerStepThroughAttribute = v != null },
                { "dc|disableComments", "do not include comments from xsd", v => disableComments = v != null },
                { "nu|noUnderscore", "do not generate underscore in private member name (default is false)", v => doNotUseUnderscoreInPrivateMemberNames = v != null },
                { "da|description", "generate DescriptionAttribute (default is true)", v => generateDescriptionAttribute = v != null },
                { "cc|complexTypesForCollections", "generate complex types for collections (default is true)", v => generateComplexTypesForCollections = v != null },
                { "s|useShouldSerialize", "use ShouldSerialize pattern instead of Specified pattern (default is false)", v => useShouldSerialize = v != null },
                { "sf|separateFiles", "generate a separate file for each class (default is false)", v => separateClasses = v != null },
                { "nh|namespaceHierarchy", "generate a separate folder for namespace hierarchy. Implies \"separateFiles\" if true (default is false)", v=> separateNamespaceHierarchy = v != null },
                { "sg|separateSubstitutes", "generate a separate property for each element of a substitution group (default is false)", v => separateSubstitutes = v != null },
                { "dnfin|doNotForceIsNullable", "do not force generator to emit IsNullable = true in XmlElement annotation for nillable elements when element is nullable (minOccurs < 1 or parent element is choice) (default is false)", v => doNotForceIsNullable = v != null },
                { "cn|compactTypeNames", "use type names without namespace qualifier for types in the using list (default is false)", v => compactTypeNames = v != null },
                { "cl|commentLanguages=", $"comment languages to use (default is {string.Join(", ", commentLanguages)}; supported are {string.Join(", ", supportedCommentLanguages)})",
                    v => commentLanguages = v.Split(',').Select(l => l.Trim()).ToArray() },
                { "un|uniqueTypeNames", "generate type names that are unique across namespaces (default is false)", v => uniqueTypeNamesAcrossNamespaces = v != null },
                { "gc|generatedCodeAttribute", "add version information to GeneratedCodeAttribute (default is true)", v => createGeneratedCodeAttributeVersion = v != null },
                { "nc|netCore", "generate .NET Core specific code that might not work with .NET Framework (default is false)", v => netCoreSpecificCode = v != null },
                { "nr|nullableReferenceAttributes", "generate attributes for nullable reference types (default is false)", v => nullableReferenceAttributes = v != null },
                { "ar|useArrayItemAttribute", "use ArrayItemAttribute for sequences with single elements (default is true)", v => useArrayItemAttribute = v != null },
                { "es|enumAsString", "Use string instead of enum for enumeration", v => enumAsString = v != null },
                { "ca|commandArgs", "generate a comment with the exact command line arguments that were used to generate the source code (default is true)", v => generateCommandLineArgs = v != null },
                { "uc|unionCommonType", "generate a common type for unions if possible (default is false)", v => unionCommonType = v != null },
                { "ec|serializeEmptyCollections", "serialize empty collections (default is false)", v => serializeEmptyCollections = v != null },
                { "dtd|allowDtdParse", "allows dtd parse (default is false)", v => allowDtdParse = v != null },
                { "ns|namingScheme=", @"use the specified naming scheme for class and property names (default is Pascal; can be: Direct, Pascal, Legacy)",
                    v =>
                    {
                        namingScheme = v?.ToLowerInvariant() switch
                        {
                            "direct" => NamingScheme.Direct,
                            "pascal" => NamingScheme.PascalCase,
                            "legacy" => NamingScheme.LegacyPascalCase,
                            _ => NamingScheme.PascalCase,
                        };
                    }
                }
            };

            var globsAndUris = options.Parse(args);

            if (showHelp)
            {
                ShowHelp(options);
                return 0;
            }

            var uris = new List<string>();
            foreach (var globOrUri in globsAndUris)
            {
                if (Uri.IsWellFormedUriString(globOrUri, UriKind.Absolute))
                {
                    uris.Add(globOrUri);
                    continue;
                }

                var expandedGlob = Glob.ExpandNames(globOrUri).ToList();
                if (expandedGlob.Count == 0)
                {
                    System.Console.WriteLine($"No files found for '{globOrUri}'");
                    Environment.Exit((int)ExitCodes.FileNotFound);
                }

                uris.AddRange(expandedGlob);
            }

            ParseNamespaceFiles(namespaces, namespaceFiles);

            var namespaceMap = namespaces.Select(n => CodeUtilities.ParseNamespace(n, namespacePrefix)).ToNamespaceProvider(key =>
            {
                var xn = key.XmlSchemaNamespace;
                var name = string.Join(".", xn.Split('/').Where(p => p != "schema" && GeneratorConfiguration.IdentifierRegex.IsMatch(p))
                    .Select(n => n.ToTitleCase(NamingScheme.PascalCase)));
                if (!string.IsNullOrEmpty(namespacePrefix)) { name = namespacePrefix + (string.IsNullOrEmpty(name) ? "" : ("." + name)); }
                return name;
            });

            ParseNameSubstituteFiles(nameSubstitutes, nameSubstituteFiles);
            var nameSubstituteMap = nameSubstitutes.ToDictionary();

            if (!string.IsNullOrEmpty(outputFolder))
            {
                outputFolder = Path.GetFullPath(outputFolder);
            }

            var generator = new Generator
            {
                NamespaceProvider = namespaceMap,
                OutputFolder = outputFolder,
                GenerateNullables = nullables,
                EnableDataBinding = enableDataBinding,
                EmitOrder = emitOrder,
                IntegerDataType = integerType,
                UseIntegerDataTypeAsFallback = useIntegerTypeAsFallback,
                DateTimeWithTimeZone = dateTimeWithTimeZone,
                EntityFramework = entityFramework,
                GenerateInterfaces = interfaces,
                AssemblyVisible = assembly,
                CollectionType = collectionType,
                CollectionImplementationType = collectionImplementationType,
                CodeTypeReferenceOptions = codeTypeReferenceOptions,
                TextValuePropertyName = textValuePropertyName,
                GenerateDebuggerStepThroughAttribute = generateDebuggerStepThroughAttribute,
                DisableComments = disableComments,
                GenerateDescriptionAttribute = generateDescriptionAttribute,
                PrivateMemberPrefix = doNotUseUnderscoreInPrivateMemberNames ? "" : "_",
                EnableUpaCheck = enableUpaCheck,
                GenerateComplexTypesForCollections = generateComplexTypesForCollections,
                UseShouldSerializePattern = useShouldSerialize,
                SeparateClasses = separateClasses,
                CollectionSettersMode = collectionSettersMode,
                DoNotForceIsNullable = doNotForceIsNullable,
                SeparateSubstitutes = separateSubstitutes,
                CompactTypeNames = compactTypeNames,
                UniqueTypeNamesAcrossNamespaces = uniqueTypeNamesAcrossNamespaces,
                CreateGeneratedCodeAttributeVersion = createGeneratedCodeAttributeVersion,
                NetCoreSpecificCode = netCoreSpecificCode,
                EnableNullableReferenceAttributes = nullableReferenceAttributes,
                GenerateCommandLineArgumentsComment = generateCommandLineArgs,
                UseArrayItemAttribute = useArrayItemAttribute,
                EnumAsString = enumAsString,
                MapUnionToWidestCommonType = unionCommonType,
                SeparateNamespaceHierarchy = separateNamespaceHierarchy,
                SerializeEmptyCollections = serializeEmptyCollections,
                AllowDtdParse = allowDtdParse,
            };

            if (namingScheme != null)
            {
                generator.NamingScheme = namingScheme.Value;
            }
            else
            {
                generator.NamingScheme = pascal ? NamingScheme.PascalCase : NamingScheme.Direct;
            }

            if (nameSubstituteMap.Any())
            {
                generator.NamingProvider = new SubstituteNamingProvider(nameSubstituteMap);
            }

            generator.CommentLanguages.UnionWith(commentLanguages);

            if (pclCompatible)
            {
                generator.UseXElementForAny = true;
                generator.GenerateDesignerCategoryAttribute = false;
                generator.GenerateSerializableAttribute = false;
                generator.GenerateDebuggerStepThroughAttribute = false;
                generator.DataAnnotationMode = DataAnnotationMode.None;
                generator.GenerateDescriptionAttribute = false;
            }

            if (verbose) { generator.Log = s => System.Console.Out.WriteLine(s); }

            generator.Generate(uris);

            return !generator.ValidationError ? (int)ExitCodes.Ok : (int)ExitCodes.ValidationError;
        }

        enum ExitCodes
        {
            Ok = 0,
            FileNotFound = 1,
            InvalidNameSubstitutionFile = 2,
            InvalidNamespaceFile = 3,
            ValidationError = 4
        }

        private static void ParseNamespaceFiles(List<string> namespaces, List<string> namespaceFiles)
        {
            foreach (var namespaceFile in namespaceFiles)
            {
                foreach (var (line, number) in File.ReadAllLines(namespaceFile)
                    .Select((l, i) => (Line: l.Trim(), Number: i + 1))
                    .Where(l => !string.IsNullOrWhiteSpace(l.Line) && !l.Line.StartsWith("#")))
                {
                    var parts = line.Split('=');

                    if (parts.Length == 1)
                        parts = [string.Empty, parts[0]];

                    if (parts.Length != 2)
                    {
                        System.Console.WriteLine($"{namespaceFile}:{number}: Line format is XML namespace = C# namespace [file name]");
                        Environment.Exit((int)ExitCodes.InvalidNamespaceFile);
                    }

                    var xmlns = parts[0].Trim();
                    var parts2 = parts[1].Trim().Split();
                    var csns = parts2[0];
                    var source = parts2.Length > 1 ? $"|{parts2[1].Trim()}" : string.Empty;
                    var ns = $"{xmlns}{source}={csns}";

                    namespaces.Add(ns);
                }
            }
        }

        private static void ParseNameSubstituteFiles(List<string> nameSubstitutes, List<string> nameSubstituteFiles)
        {
            foreach (var nameSubstituteFile in nameSubstituteFiles)
            {
                foreach (var (line, number) in File.ReadAllLines(nameSubstituteFile)
                    .Select((l, i) => (Line: l.Trim(), Number: i + 1))
                    .Where(l => !string.IsNullOrWhiteSpace(l.Line) && !l.Line.StartsWith("#")))
                {
                    var parts = line.Split('=');

                    if (parts.Length != 2)
                    {
                        System.Console.WriteLine($"{nameSubstituteFile}:{number}: Line format is prefixed type/member name = substitute name");
                        Environment.Exit((int)ExitCodes.InvalidNameSubstitutionFile);
                    }

                    var generatedName = parts[0].Trim();
                    var substituteName = parts[1].Trim();
                    var nameSubstitute = $"{generatedName}={substituteName}";

                    nameSubstitutes.Add(nameSubstitute);
                }
            }
        }

        static void ShowHelp(OptionSet p)
        {
            System.Console.WriteLine("Usage: xscgen [OPTIONS]+ xsdFile...");
            System.Console.WriteLine("Generate C# classes from XML Schema files.");
            System.Console.WriteLine("Version " + typeof(Generator).Assembly.GetCustomAttribute<AssemblyFileVersionAttribute>().Version);
            System.Console.WriteLine(@"xsdFiles may contain globs, e.g. ""content\{schema,xsd}\**\*.xsd"", and URLs.");
            System.Console.WriteLine(@"Append - to option to disable it, e.g. --interface-.");
            System.Console.WriteLine();
            System.Console.WriteLine("Options:");
            p.WriteOptionDescriptions(System.Console.Out);
        }
    }
}
