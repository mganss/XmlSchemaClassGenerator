using XmlSchemaClassGenerator;
using System;
using System.CodeDom;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Mono.Options;
using Ganss.IO;

namespace XmlSchemaClassGenerator.Console
{
    static class Program
    {
        static void Main(string[] args)
        {
            var showHelp = args.Length == 0;
            var namespaces = new List<string>();
            var outputFolder = (string)null;
            var integerType = typeof(string);
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

            var options = new OptionSet {
                { "h|help", "show this message and exit", v => showHelp = v != null },
                { "n|namespace=", @"map an XML namespace to a C# namespace
Separate XML namespace and C# namespace by '='.
One option must be given for each namespace to be mapped.
A file name may be given by appending a pipe sign (|) followed by a file name (like schema.xsd) to the XML namespace.
If no mapping is found for an XML namespace, a name is generated automatically (may fail).", v => namespaces.Add(v) },
                { "o|output=", "the {FOLDER} to write the resulting .cs files to", v => outputFolder = v },
                { "i|integer=", @"map xs:integer and derived types to {TYPE} instead of automatic approximation
{TYPE} can be i[nt], l[ong], or d[ecimal].", v => {
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
                { "ctro|codeTypeReferenceOptions=", "the default CodeTypeReferenceOptions Flags to use (default is unset; can be: {GlobalReference, GenericTypeParameter})", v => codeTypeReferenceOptions = v == null ? default(CodeTypeReferenceOptions) : (CodeTypeReferenceOptions)Enum.Parse(typeof(CodeTypeReferenceOptions), v, false) },
                { "tvpn|textValuePropertyName=", "the name of the property that holds the text value of an element (default is Value)", v => textValuePropertyName = v },
                { "dst|debuggerStepThrough", "generate DebuggerStepThroughAttribute (default is enabled)", v => generateDebuggerStepThroughAttribute = v != null },
                { "dc|disableComments", "do not include comments from xsd", v => disableComments = v != null },
                { "nu|noUnderscore", "do not generate underscore in private member name (default is false)", v => doNotUseUnderscoreInPrivateMemberNames = v != null },
                { "da|description", "generate DescriptionAttribute (default is true)", v => generateDescriptionAttribute = v != null },
                { "cc|complexTypesForCollections", "generate complex types for collections (default is true)", v => generateComplexTypesForCollections = v != null },
            };

            var globsAndUris = options.Parse(args);

            if (showHelp)
            {
                ShowHelp(options);
                return;
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
                    Environment.Exit(1);
                }

                uris.AddRange(expandedGlob);
            }

            var namespaceMap = namespaces.Select(n => ParseNamespace(n, namespacePrefix)).ToNamespaceProvider(key =>
            {
                var xn = key.XmlSchemaNamespace;
                var name = string.Join(".", xn.Split('/').Where(p => p != "schema" && GeneratorConfiguration.IdentifierRegex.IsMatch(p))
                    .Select(n => n.ToTitleCase(NamingScheme.PascalCase)));
                if (!string.IsNullOrEmpty(namespacePrefix)) { name = namespacePrefix + (string.IsNullOrEmpty(name) ? "" : ("." + name)); }
                return name;
            });

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
                EntityFramework = entityFramework,
                GenerateInterfaces = interfaces,
                NamingScheme = pascal ? NamingScheme.PascalCase : NamingScheme.Direct,
                AssemblyVisible=assembly,
                CollectionType = collectionType,
                CollectionImplementationType = collectionImplementationType,
                CodeTypeReferenceOptions = codeTypeReferenceOptions,
                TextValuePropertyName = textValuePropertyName,
                GenerateDebuggerStepThroughAttribute = generateDebuggerStepThroughAttribute,
                DisableComments = disableComments,
                GenerateDescriptionAttribute = generateDescriptionAttribute,
                PrivateMemberPrefix = doNotUseUnderscoreInPrivateMemberNames ? "" : "_",
                EnableUpaCheck = enableUpaCheck,
                GenerateComplexTypesForCollections = generateComplexTypesForCollections
            };

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
        }

        static KeyValuePair<NamespaceKey, string> ParseNamespace(string nsArg, string namespacePrefix)
        {
            var parts = nsArg.Split(new[] { '=' }, 2);
            var xmlNs = parts[0];
            var netNs = parts[1];
            var parts2 = xmlNs.Split(new[] { '|' }, 2);
            var source = parts2.Length == 2 ? new Uri(parts2[1], UriKind.RelativeOrAbsolute) : null;
            xmlNs = parts2[0];
            if (!string.IsNullOrEmpty(namespacePrefix))
            {
                netNs = namespacePrefix + "." + netNs;
            }
            return new KeyValuePair<NamespaceKey, string>(new NamespaceKey(source, xmlNs), netNs);
        }

        static void ShowHelp(OptionSet p)
        {
            System.Console.WriteLine("Usage: dotnet xscgen [OPTIONS]+ xsdFile...");
            System.Console.WriteLine("Generate C# classes from XML Schema files.");
            System.Console.WriteLine("Version " + typeof(Generator).Assembly.GetName().Version);
            System.Console.WriteLine(@"xsdFiles may contain globs, e.g. ""content\{schema,xsd}\**\*.xsd"", and URLs.");
            System.Console.WriteLine(@"Append - to option to disable it, e.g. --interface-.");
            System.Console.WriteLine();
            System.Console.WriteLine("Options:");
            p.WriteOptionDescriptions(System.Console.Out);
        }
    }
}
