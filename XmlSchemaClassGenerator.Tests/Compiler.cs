using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Emit;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using System.Linq;
using Xunit;
using Ganss.IO;
using System.Collections.Concurrent;

namespace XmlSchemaClassGenerator.Tests
{
    class CompilationResult
    {
        public Assembly Assembly { get; set; }
        public EmitResult Result { get; set; }
    }

    class Compiler
    {
        public static CompilationResult GenerateAssembly(Compilation compilation)
        {
            using (var stream = new MemoryStream())
            {
                var emitResult = compilation.Emit(stream);

                return new CompilationResult
                {
                    Assembly = emitResult.Success ? Assembly.Load(stream.ToArray()) : null,
                    Result = emitResult
                };
            }
        }

        private static ConcurrentDictionary<string, Assembly> Assemblies = new ConcurrentDictionary<string, Assembly>();

        private static readonly string[] DependencyAssemblies = new[]
        {
            "netstandard",
            "System.ComponentModel.Annotations",
            "System.ComponentModel.Primitives",
            "System.Diagnostics.Tools",
            "System.Linq",
            "System.ObjectModel",
            "System.Private.CoreLib",
            "System.Private.Xml",
            "System.Private.Xml.Linq",
            "System.Runtime",
            "System.Xml.XDocument",
            "System.Xml.XmlSerializer",
        };

        public static Assembly GetAssembly(string name)
        {
            Assemblies.TryGetValue(name, out var assembly);
            return assembly;
        }

        public static Assembly Generate(string name, string pattern, Generator generatorPrototype = null)
        {
            if (Assemblies.ContainsKey(name)) { return Assemblies[name]; }

            var files = Glob.ExpandNames(pattern);

            return GenerateFiles(name, files, generatorPrototype);
        }

        public static Assembly GenerateFiles(string name, IEnumerable<string> files, Generator generatorPrototype = null)
        {
            if (Assemblies.ContainsKey(name)) { return Assemblies[name]; }

            generatorPrototype = generatorPrototype ?? new Generator
            {
                GenerateNullables = true,
                IntegerDataType = typeof(int),
                DataAnnotationMode = DataAnnotationMode.All,
                GenerateDesignerCategoryAttribute = false,
                GenerateComplexTypesForCollections = true,
                EntityFramework = false,
                GenerateInterfaces = true,
                NamespacePrefix = name,
                GenerateDescriptionAttribute = true
            };

            var output = new FileWatcherOutputWriter(Path.Combine("output", name));

            var gen = new Generator
            {
                OutputWriter = output,
                NamespaceProvider = generatorPrototype.NamespaceProvider,
                GenerateNullables = generatorPrototype.GenerateNullables,
                IntegerDataType = generatorPrototype.IntegerDataType,
                DataAnnotationMode = generatorPrototype.DataAnnotationMode,
                GenerateDesignerCategoryAttribute = generatorPrototype.GenerateDesignerCategoryAttribute,
                GenerateComplexTypesForCollections = generatorPrototype.GenerateComplexTypesForCollections,
                EntityFramework = generatorPrototype.EntityFramework,
                GenerateInterfaces = generatorPrototype.GenerateInterfaces,
                MemberVisitor = generatorPrototype.MemberVisitor,
                GenerateDescriptionAttribute = generatorPrototype.GenerateDescriptionAttribute
            };

            gen.Generate(files);

            return CompileFiles(name, output.Files);
        }

        public static Assembly CompileFiles(string name, IEnumerable<string> files)
        {
            var trustedAssembliesPaths = ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")).Split(Path.PathSeparator);
            var references = trustedAssembliesPaths
                .Where(p => DependencyAssemblies.Contains(Path.GetFileNameWithoutExtension(p)))
                .Select(p => MetadataReference.CreateFromFile(p))
                .ToList();
            var syntaxTrees = files.Select(f => CSharpSyntaxTree.ParseText(File.ReadAllText(f))).ToList();
            var compilation = CSharpCompilation.Create(name, syntaxTrees)
                .AddReferences(references)
                .WithOptions(new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
            var result = Compiler.GenerateAssembly(compilation);

            Assert.True(result.Result.Success);
            var errors = result.Result.Diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error);
            Assert.False(errors.Any(), string.Join("\n", errors.Select(e => e.GetMessage())));
            var warnings = result.Result.Diagnostics.Where(d => d.Severity == DiagnosticSeverity.Warning);
            Assert.False(warnings.Any(), string.Join("\n", errors.Select(w => w.GetMessage())));
            Assert.NotNull(result.Assembly);

            Assemblies[name] = result.Assembly;

            return result.Assembly;
        }

        private static readonly LanguageVersion MaxLanguageVersion = Enum
            .GetValues(typeof(LanguageVersion))
            .Cast<LanguageVersion>()
            .Max();

        public static Assembly Compile(string name, string contents, Generator generator)
        {
            var trustedAssembliesPaths = ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")).Split(Path.PathSeparator);
            var references = trustedAssembliesPaths
                .Where(p => DependencyAssemblies.Contains(Path.GetFileNameWithoutExtension(p)))
                .Select(p => MetadataReference.CreateFromFile(p))
                .ToList();

            var options = new CSharpParseOptions(kind: SourceCodeKind.Regular, languageVersion: MaxLanguageVersion);

            // Return a syntax tree of our source code
            var syntaxTree = CSharpSyntaxTree.ParseText(contents, options);

            var compilation = CSharpCompilation.Create(name, new[] { syntaxTree })
                .AddReferences(references)
                .WithOptions(new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
            var result = Compiler.GenerateAssembly(compilation);


            Assert.True(result.Result.Success);
            var errors = result.Result.Diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error).ToList();
            Assert.False(errors.Any(), string.Join("\n", errors.Select(e => e.GetMessage())));
            var warnings = result.Result.Diagnostics.Where(d => d.Severity == DiagnosticSeverity.Warning).ToList();
            Assert.False(warnings.Any(), string.Join("\n", errors.Select(w => w.GetMessage())));
            Assert.NotNull(result.Assembly);

            return result.Assembly;
        }
    }
}
