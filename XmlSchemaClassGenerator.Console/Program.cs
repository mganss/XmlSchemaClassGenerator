using XmlSchemaClassGenerator;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Mono.Options;

namespace XmlSchemaClassGenerator.Console
{
    class Program
    {
        static void Main(string[] args)
        {
            var showHelp = false;
            var namespaces = new List<string>();
            var outputFolder = "";
            var integerType = typeof(string);
            var namespacePrefix = "";
            var verbose = false;
            var nullables = false;

            var options = new OptionSet {
                { "h|help", "show this message and exit", v => showHelp = v != null },
                { "n|namespace=", @"map an XML namespace to a C# namespace
Separate XML namespace and C# namespace by '='.
One option must be given for each namespace to be mapped.
If no mapping is found for an XML namespace, a name is generated automatically (may fail).", v => namespaces.Add(v) },
                { "o|output=", "the {FOLDER} to write the resulting .cs files to", v => outputFolder = v },
                { "i|integer=", @"map xs:integer and derived types to {TYPE} instead of string
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
                { "p|prefix=", "the {PREFIX} to prepend to auto-generated namespace names", v => namespacePrefix = v },
                { "v|verbose", "print generated file names on stdout", v => verbose = v != null },
                { "0|nullable", "generate nullable adapter properties for optional elements/attributes w/o default values", v => nullables = v != null },
            };

            var files = options.Parse(args);

            if (showHelp)
            {
                ShowHelp(options);
                return;
            }

            files = files.SelectMany(f => Glob.Glob.ExpandNames(f)).ToList();

            SimpleModel.IntegerDataType = integerType;

            var generator = new Generator
            {
                GenerateNamespaceName = xn =>
                {
                    var name = string.Join(".", xn.Split('/').Where(p => Regex.IsMatch(p, @"^[a-z]+$") && p != "schema")
                        .Select(n => Generator.ToTitleCase(n)));
                    if (!string.IsNullOrEmpty(namespacePrefix)) name = namespacePrefix + (string.IsNullOrEmpty(name) ? "" : ("." + name));
                    return name;
                },
                NamespaceMapping = namespaces.Select(n => n.Split('=')).ToDictionary(n => n[0], n => n[1]),
                OutputFolder = outputFolder,
                GenerateNullables = nullables,
            };

            if (verbose) generator.Log = s => System.Console.Out.WriteLine(s);

            generator.Generate(files);
        }

        static void ShowHelp(OptionSet p)
        {
            System.Console.WriteLine("Usage: XmlSchemaClassGenerator.Console [OPTIONS]+ xsdFile...");
            System.Console.WriteLine("Generate C# classes from XML Schema files.");
            System.Console.WriteLine(@"xsdFiles may contain globs, e.g. ""content\{schema,xsd}\**\*.xsd"".");
            System.Console.WriteLine();
            System.Console.WriteLine("Options:");
            p.WriteOptionDescriptions(System.Console.Out);
        }
    }
}
