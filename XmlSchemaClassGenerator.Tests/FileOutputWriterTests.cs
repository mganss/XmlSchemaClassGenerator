namespace XmlSchemaClassGenerator.Tests
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using Xunit;
    using Xunit.Abstractions;

    [TestCaseOrderer("XmlSchemaClassGenerator.Tests.PriorityOrderer", "XmlSchemaClassGenerator.Tests")]
    public class FileOutputWriterTests
    {
        private const string PrefixPattern = "xsd/prefix/prefix.xsd";
        private readonly ITestOutputHelper _output;

        public FileOutputWriterTests(ITestOutputHelper output)
        {
            _output = output;
        }

        [Fact]
        [TestPriority(1)]
        [UseCulture("en-US")]
        public void TestDefaultProvider_ThrowsArgumentException()
        {
            var outputWriter = new FileWatcherOutputWriter(Path.Combine("output", "FileOutputWriterTests", "DefaultProvider"));

            void Action() => Compiler.Generate(
                "DefaultProvider",
                PrefixPattern,
                new Generator
                {
                    OutputWriter = outputWriter,
                    EnableDataBinding = true,
                });

            ArgumentException ex = Assert.Throws<ArgumentException>(Action);
            Assert.Equal("Namespace http://tempuri.org/PurchaseOrderSchema.xsd not provided through map or generator.", ex.Message);
        }

        [Fact]
        [TestPriority(1)]
        [UseCulture("en-US")]
        public void TestDefaultProviderGeneratorPrefix()
        {
            var outputWriter = new FileWatcherOutputWriter(Path.Combine("output", "FileOutputWriterTests", "DefaultProviderGeneratorPrefix"));

            Compiler.Generate(
                "DefaultProviderGeneratorPrefix",
                PrefixPattern,
                new Generator
                {
                    OutputWriter = outputWriter,
                    EnableDataBinding = true,
                    NamespacePrefix = "Generator.Prefix",
                });

            SharedTestFunctions.TestSamples(_output, "DefaultProviderGeneratorPrefix", PrefixPattern);
            Assert.Single(outputWriter.Files);
            Assert.Equal(@"output\FileOutputWriterTests\DefaultProviderGeneratorPrefix\Generator.Prefix.cs", outputWriter.Files.First());
        }

        [Fact]
        [TestPriority(1)]
        [UseCulture("en-US")]
        public void TestEmptyKeyProvider()
        {
            var outputWriter = new FileWatcherOutputWriter(Path.Combine("output", "FileOutputWriterTests", "EmptyKeyProvider"));

            Compiler.Generate(
                "EmptyKeyProvider",
                PrefixPattern,
                new Generator
                {
                    OutputWriter = outputWriter,
                    EnableDataBinding = true,
                    NamespaceProvider = new Dictionary<NamespaceKey, string>
                    {
                        { new NamespaceKey(String.Empty), "NamedNamespace" },
                    }.ToNamespaceProvider(),
                });

            SharedTestFunctions.TestSamples(_output, "EmptyKeyProvider", PrefixPattern);
            Assert.Single(outputWriter.Files);
            Assert.Equal(@"output\FileOutputWriterTests\EmptyKeyProvider\NamedNamespace.cs", outputWriter.Files.First());
        }

        [Fact]
        [TestPriority(1)]
        [UseCulture("en-US")]
        public void TestEmptyKeyProviderGeneratorPrefix()
        {
            var outputWriter = new FileWatcherOutputWriter(Path.Combine("output", "FileOutputWriterTests", "EmptyKeyProviderGeneratorPrefix"));

            Compiler.Generate(
                "EmptyKeyProviderGeneratorPrefix",
                PrefixPattern,
                new Generator
                {
                    OutputWriter = outputWriter,
                    EnableDataBinding = true,
                    NamespacePrefix = "Generator.Prefix",
                    NamespaceProvider = new Dictionary<NamespaceKey, string>
                    {
                        { new NamespaceKey(String.Empty), "NamedNamespace" },
                    }.ToNamespaceProvider(),
                });

            SharedTestFunctions.TestSamples(_output, "EmptyKeyProviderGeneratorPrefix", PrefixPattern);
            Assert.Single(outputWriter.Files);
            Assert.Equal(@"output\FileOutputWriterTests\EmptyKeyProviderGeneratorPrefix\NamedNamespace.cs", outputWriter.Files.First());
        }

        [Fact]
        [TestPriority(1)]
        [UseCulture("en-US")]
        public void TestEmptyKeyProviderGeneratorConfiruationPrefix()
        {
            var outputWriter = new FileWatcherOutputWriter(Path.Combine("output", "FileOutputWriterTests", "EmptyKeyProviderGeneratorConfiruationPrefix"));

            Compiler.Generate(
                "EmptyKeyProviderGeneratorConfiruationPrefix",
                PrefixPattern,
                new Generator
                {
                    OutputWriter = outputWriter,
                    EnableDataBinding = true,
                    NamespaceProvider = new Dictionary<NamespaceKey, string>
                    {
                        { new NamespaceKey(String.Empty), "NamedNamespace" },
                    }.ToNamespaceProvider(new GeneratorConfiguration { NamespacePrefix = "GeneratorConfiguration.Prefix" }.NamespaceProvider.GenerateNamespace),
                });

            SharedTestFunctions.TestSamples(_output, "EmptyKeyProviderGeneratorConfiruationPrefix", PrefixPattern);
            Assert.Single(outputWriter.Files);
            Assert.Equal(@"output\FileOutputWriterTests\EmptyKeyProviderGeneratorConfiruationPrefix\NamedNamespace.cs", outputWriter.Files.First());
        }

        [Fact]
        [TestPriority(1)]
        [UseCulture("en-US")]
        public void TestEmptyKeyProviderBothPrefixes()
        {
            var outputWriter = new FileWatcherOutputWriter(Path.Combine("output", "FileOutputWriterTests", "EmptyKeyProviderBothPrefixes"));

            Compiler.Generate(
                "EmptyKeyProviderBothPrefixes",
                PrefixPattern,
                new Generator
                {
                    OutputWriter = outputWriter,
                    EnableDataBinding = true,
                    NamespacePrefix = "Generator.Prefix",
                    NamespaceProvider = new Dictionary<NamespaceKey, string>
                    {
                        { new NamespaceKey(String.Empty), "NamedNamespace" },
                    }.ToNamespaceProvider(new GeneratorConfiguration { NamespacePrefix = "GeneratorConfiguration.Prefix" }.NamespaceProvider.GenerateNamespace),
                });

            SharedTestFunctions.TestSamples(_output, "EmptyKeyProviderBothPrefixes", PrefixPattern);
            Assert.Single(outputWriter.Files);
            Assert.Equal(@"output\FileOutputWriterTests\EmptyKeyProviderBothPrefixes\NamedNamespace.cs", outputWriter.Files.First());
        }

        [Fact]
        [TestPriority(1)]
        [UseCulture("en-US")]
        public void TestFullKeyProvider()
        {
            var outputWriter = new FileWatcherOutputWriter(Path.Combine("output", "FileOutputWriterTests", "FullKeyProvider"));

            Compiler.Generate(
                "FullKeyProvider",
                PrefixPattern,
                new Generator
                {
                    OutputWriter = outputWriter,
                    EnableDataBinding = true,
                    NamespaceProvider = new Dictionary<NamespaceKey, string>
                    {
                        { new NamespaceKey("http://tempuri.org/PurchaseOrderSchema.xsd"), "NamedNamespace" },
                    }.ToNamespaceProvider(),
                });

            SharedTestFunctions.TestSamples(_output, "FullKeyProvider", PrefixPattern);
            Assert.Single(outputWriter.Files);
            Assert.Equal(@"output\FileOutputWriterTests\FullKeyProvider\NamedNamespace.cs", outputWriter.Files.First());
        }

        [Fact]
        [TestPriority(1)]
        [UseCulture("en-US")]
        public void TestFullKeyProviderGeneratorPrefix()
        {
            var outputWriter = new FileWatcherOutputWriter(Path.Combine("output", "FileOutputWriterTests", "FullKeyProviderGeneratorPrefix"));

            Compiler.Generate(
                "FullKeyProviderGeneratorPrefix",
                PrefixPattern,
                new Generator
                {
                    OutputWriter = outputWriter,
                    EnableDataBinding = true,
                    NamespacePrefix = "Generator.Prefix",
                    NamespaceProvider = new Dictionary<NamespaceKey, string>
                    {
                        { new NamespaceKey("http://tempuri.org/PurchaseOrderSchema.xsd"), "NamedNamespace" },
                    }.ToNamespaceProvider(),
                });

            SharedTestFunctions.TestSamples(_output, "FullKeyProviderGeneratorPrefix", PrefixPattern);
            Assert.Single(outputWriter.Files);
            Assert.Equal(@"output\FileOutputWriterTests\FullKeyProviderGeneratorPrefix\NamedNamespace.cs", outputWriter.Files.First());
        }

        [Fact]
        [TestPriority(1)]
        [UseCulture("en-US")]
        public void TestFullKeyProviderGeneratorConfiruationPrefix()
        {
            var outputWriter = new FileWatcherOutputWriter(Path.Combine("output", "FileOutputWriterTests", "FullKeyProviderGeneratorConfiruationPrefix"));

            Compiler.Generate(
                "FullKeyProviderGeneratorConfiruationPrefix",
                PrefixPattern,
                new Generator
                {
                    OutputWriter = outputWriter,
                    EnableDataBinding = true,
                    NamespaceProvider = new Dictionary<NamespaceKey, string>
                    {
                        { new NamespaceKey("http://tempuri.org/PurchaseOrderSchema.xsd"), "NamedNamespace" },
                    }.ToNamespaceProvider(new GeneratorConfiguration { NamespacePrefix = "GeneratorConfiguration.Prefix" }.NamespaceProvider.GenerateNamespace),
                });

            SharedTestFunctions.TestSamples(_output, "FullKeyProviderGeneratorConfiruationPrefix", PrefixPattern);
            Assert.Single(outputWriter.Files);
            Assert.Equal(@"output\FileOutputWriterTests\FullKeyProviderGeneratorConfiruationPrefix\NamedNamespace.cs", outputWriter.Files.First());
        }

        [Fact]
        [TestPriority(1)]
        [UseCulture("en-US")]
        public void TestFullKeyProviderBothPrefixes()
        {
            var outputWriter = new FileWatcherOutputWriter(Path.Combine("output", "FileOutputWriterTests", "FullKeyProviderBothPrefixes"));

            Compiler.Generate(
                "FullKeyProviderBothPrefixes",
                PrefixPattern,
                new Generator
                {
                    OutputWriter = outputWriter,
                    EnableDataBinding = true,
                    NamespacePrefix = "Generator.Prefix",
                    NamespaceProvider = new Dictionary<NamespaceKey, string>
                    {
                        { new NamespaceKey("http://tempuri.org/PurchaseOrderSchema.xsd"), "NamedNamespace" },
                    }.ToNamespaceProvider(new GeneratorConfiguration { NamespacePrefix = "GeneratorConfiguration.Prefix" }.NamespaceProvider.GenerateNamespace),
                });

            SharedTestFunctions.TestSamples(_output, "FullKeyProviderBothPrefixes", PrefixPattern);
            Assert.Single(outputWriter.Files);
            Assert.Equal(@"output\FileOutputWriterTests\FullKeyProviderBothPrefixes\NamedNamespace.cs", outputWriter.Files.First());
        }

        [Fact]
        [TestPriority(1)]
        [UseCulture("en-US")]
        public void TestSeparateDefaultProvider_ThrowsArgumentException()
        {
            var outputWriter = new FileWatcherOutputWriter(Path.Combine("output", "FileOutputWriterTests", "SeparateDefaultProvider"));

            void Action() => Compiler.Generate(
                "SeparateDefaultProvider",
                PrefixPattern,
                new Generator
                {
                    OutputWriter = outputWriter,
                    EnableDataBinding = true,
                    SeparateClasses = true,
                });

            ArgumentException ex = Assert.Throws<ArgumentException>(Action);
            Assert.Equal("Namespace http://tempuri.org/PurchaseOrderSchema.xsd not provided through map or generator.", ex.Message);
        }

        [Fact]
        [TestPriority(1)]
        [UseCulture("en-US")]
        public void TestSeparateDefaultProviderGeneratorPrefix()
        {
            var outputWriter = new FileWatcherOutputWriter(Path.Combine("output", "FileOutputWriterTests", "SeparateDefaultProviderGeneratorPrefix"));

            Compiler.Generate(
                "SeparateDefaultProviderGeneratorPrefix",
                PrefixPattern,
                new Generator
                {
                    OutputWriter = outputWriter,
                    EnableDataBinding = true,
                    SeparateClasses = true,
                    NamespacePrefix = "Generator.Prefix",
                });

            SharedTestFunctions.TestSamples(_output, "SeparateDefaultProviderGeneratorPrefix", PrefixPattern);
            Assert.Equal(2, outputWriter.Files.Count());
            Assert.Equal(@"output\FileOutputWriterTests\SeparateDefaultProviderGeneratorPrefix\Generator.Prefix\PurchaseOrderType.cs", outputWriter.Files.First());
        }

        [Fact]
        [TestPriority(1)]
        [UseCulture("en-US")]
        public void TestSeparateEmptyKeyProvider()
        {
            var outputWriter = new FileWatcherOutputWriter(Path.Combine("output", "FileOutputWriterTests", "SeparateEmptyKeyProvider"));

            Compiler.Generate(
                "SeparateEmptyKeyProvider",
                PrefixPattern,
                new Generator
                {
                    OutputWriter = outputWriter,
                    EnableDataBinding = true,
                    SeparateClasses = true,
                    NamespaceProvider = new Dictionary<NamespaceKey, string>
                    {
                        { new NamespaceKey(String.Empty), "NamedNamespace" },
                    }.ToNamespaceProvider(),
                });

            SharedTestFunctions.TestSamples(_output, "SeparateEmptyKeyProvider", PrefixPattern);
            Assert.Equal(2, outputWriter.Files.Count());
            Assert.Equal(@"output\FileOutputWriterTests\SeparateEmptyKeyProvider\NamedNamespace\PurchaseOrderType.cs", outputWriter.Files.First());
        }

        [Fact]
        [TestPriority(1)]
        [UseCulture("en-US")]
        public void TestSeparateEmptyKeyProviderGeneratorPrefix()
        {
            var outputWriter = new FileWatcherOutputWriter(Path.Combine("output", "FileOutputWriterTests", "SeparateEmptyKeyProviderGeneratorPrefix"));

            Compiler.Generate(
                "SeparateEmptyKeyProviderGeneratorPrefix",
                PrefixPattern,
                new Generator
                {
                    OutputWriter = outputWriter,
                    EnableDataBinding = true,
                    SeparateClasses = true,
                    NamespacePrefix = "Generator.Prefix",
                    NamespaceProvider = new Dictionary<NamespaceKey, string>
                    {
                        { new NamespaceKey(String.Empty), "NamedNamespace" },
                    }.ToNamespaceProvider(),
                });

            SharedTestFunctions.TestSamples(_output, "SeparateEmptyKeyProviderGeneratorPrefix", PrefixPattern);
            Assert.Equal(2, outputWriter.Files.Count());
            Assert.Equal(@"output\FileOutputWriterTests\SeparateEmptyKeyProviderGeneratorPrefix\NamedNamespace\PurchaseOrderType.cs", outputWriter.Files.First());
        }

        [Fact]
        [TestPriority(1)]
        [UseCulture("en-US")]
        public void TestSeparateEmptyKeyProviderGeneratorConfiruationPrefix()
        {
            var outputWriter = new FileWatcherOutputWriter(Path.Combine("output", "FileOutputWriterTests", "SeparateEmptyKeyProviderGeneratorConfiruationPrefix"));

            Compiler.Generate(
                "SeparateEmptyKeyProviderGeneratorConfiruationPrefix",
                PrefixPattern,
                new Generator
                {
                    OutputWriter = outputWriter,
                    EnableDataBinding = true,
                    SeparateClasses = true,
                    NamespaceProvider = new Dictionary<NamespaceKey, string>
                    {
                        { new NamespaceKey(String.Empty), "NamedNamespace" },
                    }.ToNamespaceProvider(new GeneratorConfiguration { NamespacePrefix = "GeneratorConfiguration.Prefix" }.NamespaceProvider.GenerateNamespace),
                });

            SharedTestFunctions.TestSamples(_output, "SeparateEmptyKeyProviderGeneratorConfiruationPrefix", PrefixPattern);
            Assert.Equal(2, outputWriter.Files.Count());
            Assert.Equal(@"output\FileOutputWriterTests\SeparateEmptyKeyProviderGeneratorConfiruationPrefix\NamedNamespace\PurchaseOrderType.cs", outputWriter.Files.First());
        }

        [Fact]
        [TestPriority(1)]
        [UseCulture("en-US")]
        public void TestSeparateEmptyKeyProviderBothPrefixes()
        {
            var outputWriter = new FileWatcherOutputWriter(Path.Combine("output", "FileOutputWriterTests", "SeparateEmptyKeyProviderBothPrefixes"));

            Compiler.Generate(
                "SeparateEmptyKeyProviderBothPrefixes",
                PrefixPattern,
                new Generator
                {
                    OutputWriter = outputWriter,
                    EnableDataBinding = true,
                    SeparateClasses = true,
                    NamespacePrefix = "Generator.Prefix",
                    NamespaceProvider = new Dictionary<NamespaceKey, string>
                    {
                        { new NamespaceKey(String.Empty), "NamedNamespace" },
                    }.ToNamespaceProvider(new GeneratorConfiguration { NamespacePrefix = "GeneratorConfiguration.Prefix" }.NamespaceProvider.GenerateNamespace),
                });

            SharedTestFunctions.TestSamples(_output, "SeparateEmptyKeyProviderBothPrefixes", PrefixPattern);
            Assert.Equal(2, outputWriter.Files.Count());
            Assert.Equal(@"output\FileOutputWriterTests\SeparateEmptyKeyProviderBothPrefixes\NamedNamespace\PurchaseOrderType.cs", outputWriter.Files.First());
        }

        [Fact]
        [TestPriority(1)]
        [UseCulture("en-US")]
        public void TestSeparateFullKeyProvider()
        {
            var outputWriter = new FileWatcherOutputWriter(Path.Combine("output", "FileOutputWriterTests", "SeparateFullKeyProvider"));

            Compiler.Generate(
                "SeparateFullKeyProvider",
                PrefixPattern,
                new Generator
                {
                    OutputWriter = outputWriter,
                    EnableDataBinding = true,
                    SeparateClasses = true,
                    NamespaceProvider = new Dictionary<NamespaceKey, string>
                    {
                        { new NamespaceKey("http://tempuri.org/PurchaseOrderSchema.xsd"), "NamedNamespace" },
                    }.ToNamespaceProvider(),
                });

            SharedTestFunctions.TestSamples(_output, "SeparateFullKeyProvider", PrefixPattern);
            Assert.Equal(2, outputWriter.Files.Count());
            Assert.Equal(@"output\FileOutputWriterTests\SeparateFullKeyProvider\NamedNamespace\PurchaseOrderType.cs", outputWriter.Files.First());
        }

        [Fact]
        [TestPriority(1)]
        [UseCulture("en-US")]
        public void TestSeparateFullKeyProviderGeneratorPrefix()
        {
            var outputWriter = new FileWatcherOutputWriter(Path.Combine("output", "FileOutputWriterTests", "SeparateFullKeyProviderGeneratorPrefix"));

            Compiler.Generate(
                "SeparateFullKeyProviderGeneratorPrefix",
                PrefixPattern,
                new Generator
                {
                    OutputWriter = outputWriter,
                    EnableDataBinding = true,
                    SeparateClasses = true,
                    NamespacePrefix = "Generator.Prefix",
                    NamespaceProvider = new Dictionary<NamespaceKey, string>
                    {
                        { new NamespaceKey("http://tempuri.org/PurchaseOrderSchema.xsd"), "NamedNamespace" },
                    }.ToNamespaceProvider(),
                });

            SharedTestFunctions.TestSamples(_output, "SeparateFullKeyProviderGeneratorPrefix", PrefixPattern);
            Assert.Equal(2, outputWriter.Files.Count());
            Assert.Equal(@"output\FileOutputWriterTests\SeparateFullKeyProviderGeneratorPrefix\NamedNamespace\PurchaseOrderType.cs", outputWriter.Files.First());
        }

        [Fact]
        [TestPriority(1)]
        [UseCulture("en-US")]
        public void TestSeparateFullKeyProviderGeneratorConfiruationPrefix()
        {
            var outputWriter = new FileWatcherOutputWriter(Path.Combine("output", "FileOutputWriterTests", "SeparateFullKeyProviderGeneratorConfiruationPrefix"));

            Compiler.Generate(
                "SeparateFullKeyProviderGeneratorConfiruationPrefix",
                PrefixPattern,
                new Generator
                {
                    OutputWriter = outputWriter,
                    EnableDataBinding = true,
                    SeparateClasses = true,
                    NamespaceProvider = new Dictionary<NamespaceKey, string>
                    {
                        { new NamespaceKey("http://tempuri.org/PurchaseOrderSchema.xsd"), "NamedNamespace" },
                    }.ToNamespaceProvider(new GeneratorConfiguration { NamespacePrefix = "GeneratorConfiguration.Prefix" }.NamespaceProvider.GenerateNamespace),
                });

            SharedTestFunctions.TestSamples(_output, "SeparateFullKeyProviderGeneratorConfiruationPrefix", PrefixPattern);
            Assert.Equal(2, outputWriter.Files.Count());
            Assert.Equal(@"output\FileOutputWriterTests\SeparateFullKeyProviderGeneratorConfiruationPrefix\NamedNamespace\PurchaseOrderType.cs", outputWriter.Files.First());
        }

        [Fact]
        [TestPriority(1)]
        [UseCulture("en-US")]
        public void TestSeparateFullKeyProviderBothPrefixes()
        {
            var outputWriter = new FileWatcherOutputWriter(Path.Combine("output", "FileOutputWriterTests", "SeparateFullKeyProviderBothPrefixes"));

            Compiler.Generate(
                "SeparateFullKeyProviderBothPrefixes",
                PrefixPattern,
                new Generator
                {
                    OutputWriter = outputWriter,
                    EnableDataBinding = true,
                    SeparateClasses = true,
                    NamespacePrefix = "Generator.Prefix",
                    NamespaceProvider = new Dictionary<NamespaceKey, string>
                    {
                        { new NamespaceKey("http://tempuri.org/PurchaseOrderSchema.xsd"), "NamedNamespace" },
                    }.ToNamespaceProvider(new GeneratorConfiguration { NamespacePrefix = "GeneratorConfiguration.Prefix" }.NamespaceProvider.GenerateNamespace),
                });

            SharedTestFunctions.TestSamples(_output, "SeparateFullKeyProviderBothPrefixes", PrefixPattern);
            Assert.Equal(2, outputWriter.Files.Count());
            Assert.Equal(@"output\FileOutputWriterTests\SeparateFullKeyProviderBothPrefixes\NamedNamespace\PurchaseOrderType.cs", outputWriter.Files.First());
        }
    }
}
