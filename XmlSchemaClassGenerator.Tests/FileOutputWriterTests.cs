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
            var directory = Path.Combine("output", "FileOutputWriterTests", "DefaultProviderGeneratorPrefix");
            var outputWriter = new FileWatcherOutputWriter(directory);

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
            Assert.Equal(Path.Combine(directory, "Generator.Prefix.cs"), outputWriter.Files.First());
        }

        [Fact]
        [TestPriority(1)]
        [UseCulture("en-US")]
        public void TestEmptyKeyProvider()
        {
            var directory = Path.Combine("output", "FileOutputWriterTests", "EmptyKeyProvider");
            var outputWriter = new FileWatcherOutputWriter(directory);

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
            Assert.Equal(Path.Combine(directory, "NamedNamespace.cs"), outputWriter.Files.First());
        }

        [Fact]
        [TestPriority(1)]
        [UseCulture("en-US")]
        public void TestEmptyKeyProviderGeneratorPrefix()
        {
            var directory = Path.Combine("output", "FileOutputWriterTests", "EmptyKeyProviderGeneratorPrefix");
            var outputWriter = new FileWatcherOutputWriter(directory);

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
            Assert.Equal(Path.Combine(directory, "NamedNamespace.cs"), outputWriter.Files.First());
        }

        [Fact]
        [TestPriority(1)]
        [UseCulture("en-US")]
        public void TestEmptyKeyProviderGeneratorConfigurationPrefix()
        {
            var directory = Path.Combine("output", "FileOutputWriterTests", "EmptyKeyProviderGeneratorConfigurationPrefix");
            var outputWriter = new FileWatcherOutputWriter(directory);

            Compiler.Generate(
                "EmptyKeyProviderGeneratorConfigurationPrefix",
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

            SharedTestFunctions.TestSamples(_output, "EmptyKeyProviderGeneratorConfigurationPrefix", PrefixPattern);
            Assert.Single(outputWriter.Files);
            Assert.Equal(Path.Combine(directory, "NamedNamespace.cs"), outputWriter.Files.First());
        }

        [Fact]
        [TestPriority(1)]
        [UseCulture("en-US")]
        public void TestEmptyKeyProviderBothPrefixes()
        {
            var directory = Path.Combine("output", "FileOutputWriterTests", "EmptyKeyProviderBothPrefixes");
            var outputWriter = new FileWatcherOutputWriter(directory);

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
            Assert.Equal(Path.Combine(directory, "NamedNamespace.cs"), outputWriter.Files.First());
        }

        [Fact]
        [TestPriority(1)]
        [UseCulture("en-US")]
        public void TestFullKeyProvider()
        {
            var directory = Path.Combine("output", "FileOutputWriterTests", "FullKeyProvider");
            var outputWriter = new FileWatcherOutputWriter(directory);

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
            Assert.Equal(Path.Combine(directory, "NamedNamespace.cs"), outputWriter.Files.First());
        }

        [Fact]
        [TestPriority(1)]
        [UseCulture("en-US")]
        public void TestFullKeyProviderGeneratorPrefix()
        {
            var directory = Path.Combine("output", "FileOutputWriterTests", "FullKeyProviderGeneratorPrefix");
            var outputWriter = new FileWatcherOutputWriter(directory);

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
            Assert.Equal(Path.Combine(directory, "NamedNamespace.cs"), outputWriter.Files.First());
        }

        [Fact]
        [TestPriority(1)]
        [UseCulture("en-US")]
        public void TestFullKeyProviderGeneratorConfigurationPrefix()
        {
            var directory = Path.Combine("output", "FileOutputWriterTests", "FullKeyProviderGeneratorConfigurationPrefix");
            var outputWriter = new FileWatcherOutputWriter(directory);

            Compiler.Generate(
                "FullKeyProviderGeneratorConfigurationPrefix",
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

            SharedTestFunctions.TestSamples(_output, "FullKeyProviderGeneratorConfigurationPrefix", PrefixPattern);
            Assert.Single(outputWriter.Files);
            Assert.Equal(Path.Combine(directory, "NamedNamespace.cs"), outputWriter.Files.First());
        }

        [Fact]
        [TestPriority(1)]
        [UseCulture("en-US")]
        public void TestFullKeyProviderBothPrefixes()
        {
            var directory = Path.Combine("output", "FileOutputWriterTests", "FullKeyProviderBothPrefixes");
            var outputWriter = new FileWatcherOutputWriter(directory);

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
            Assert.Equal(Path.Combine(directory, "NamedNamespace.cs"), outputWriter.Files.First());
        }

        [Fact]
        [TestPriority(1)]
        [UseCulture("en-US")]
        public void TestSeparateDefaultProvider_ThrowsArgumentException()
        {
            var directory = Path.Combine("output", "FileOutputWriterTests", "SeparateDefaultProvider");
            var outputWriter = new FileWatcherOutputWriter(directory);

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
            var directory = Path.Combine("output", "FileOutputWriterTests", "SeparateDefaultProviderGeneratorPrefix");
            var outputWriter = new FileWatcherOutputWriter(directory);

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
            Assert.Equal(Path.Combine(directory, "Generator.Prefix", "PurchaseOrderType.cs"), outputWriter.Files.First());
        }

        [Fact]
        [TestPriority(1)]
        [UseCulture("en-US")]
        public void TestSeparateEmptyKeyProvider()
        {
            var directory = Path.Combine("output", "FileOutputWriterTests", "SeparateEmptyKeyProvider");
            var outputWriter = new FileWatcherOutputWriter(directory);

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
            Assert.Equal(Path.Combine(directory, "NamedNamespace", "PurchaseOrderType.cs"), outputWriter.Files.First());
        }

        [Fact]
        [TestPriority(1)]
        [UseCulture("en-US")]
        public void TestSeparateEmptyKeyProviderGeneratorPrefix()
        {
            var directory = Path.Combine("output", "FileOutputWriterTests", "SeparateEmptyKeyProviderGeneratorPrefix");
            var outputWriter = new FileWatcherOutputWriter(directory);

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
            Assert.Equal(Path.Combine(directory, "NamedNamespace", "PurchaseOrderType.cs"), outputWriter.Files.First());
        }

        [Fact]
        [TestPriority(1)]
        [UseCulture("en-US")]
        public void TestSeparateEmptyKeyProviderGeneratorConfigurationPrefix()
        {
            var directory = Path.Combine("output", "FileOutputWriterTests", "SeparateEmptyKeyProviderGeneratorConfigurationPrefix");
            var outputWriter = new FileWatcherOutputWriter(directory);

            Compiler.Generate(
                "SeparateEmptyKeyProviderGeneratorConfigurationPrefix",
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

            SharedTestFunctions.TestSamples(_output, "SeparateEmptyKeyProviderGeneratorConfigurationPrefix", PrefixPattern);
            Assert.Equal(2, outputWriter.Files.Count());
            Assert.Equal(Path.Combine(directory, "NamedNamespace", "PurchaseOrderType.cs"), outputWriter.Files.First());
        }

        [Fact]
        [TestPriority(1)]
        [UseCulture("en-US")]
        public void TestSeparateEmptyKeyProviderBothPrefixes()
        {
            var directory = Path.Combine("output", "FileOutputWriterTests", "SeparateEmptyKeyProviderBothPrefixes");
            var outputWriter = new FileWatcherOutputWriter(directory);

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
            Assert.Equal(Path.Combine(directory, "NamedNamespace", "PurchaseOrderType.cs"), outputWriter.Files.First());
        }

        [Fact]
        [TestPriority(1)]
        [UseCulture("en-US")]
        public void TestSeparateFullKeyProvider()
        {
            var directory = Path.Combine("output", "FileOutputWriterTests", "SeparateFullKeyProvider");
            var outputWriter = new FileWatcherOutputWriter(directory);

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
            Assert.Equal(Path.Combine(directory, "NamedNamespace", "PurchaseOrderType.cs"), outputWriter.Files.First());
        }

        [Fact]
        [TestPriority(1)]
        [UseCulture("en-US")]
        public void TestSeparateFullKeyProviderGeneratorPrefix()
        {
            var directory = Path.Combine("output", "FileOutputWriterTests", "SeparateFullKeyProviderGeneratorPrefix");
            var outputWriter = new FileWatcherOutputWriter(directory);

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
            Assert.Equal(Path.Combine(directory, "NamedNamespace", "PurchaseOrderType.cs"), outputWriter.Files.First());
        }

        [Fact]
        [TestPriority(1)]
        [UseCulture("en-US")]
        public void TestSeparateFullKeyProviderGeneratorConfigurationPrefix()
        {
            var directory = Path.Combine("output", "FileOutputWriterTests", "SeparateFullKeyProviderGeneratorConfigurationPrefix");
            var outputWriter = new FileWatcherOutputWriter(directory);

            Compiler.Generate(
                "SeparateFullKeyProviderGeneratorConfigurationPrefix",
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

            SharedTestFunctions.TestSamples(_output, "SeparateFullKeyProviderGeneratorConfigurationPrefix", PrefixPattern);
            Assert.Equal(2, outputWriter.Files.Count());
            Assert.Equal(Path.Combine(directory, "NamedNamespace", "PurchaseOrderType.cs"), outputWriter.Files.First());
        }

        [Fact]
        [TestPriority(1)]
        [UseCulture("en-US")]
        public void TestSeparateFullKeyProviderBothPrefixes()
        {
            var directory = Path.Combine("output", "FileOutputWriterTests", "SeparateFullKeyProviderBothPrefixes");
            var outputWriter = new FileWatcherOutputWriter(directory);

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
            Assert.Equal(Path.Combine(directory, "NamedNamespace", "PurchaseOrderType.cs"), outputWriter.Files.First());
        }
    }
}
