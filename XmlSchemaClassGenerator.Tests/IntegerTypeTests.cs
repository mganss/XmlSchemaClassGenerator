using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml.Schema;
using Xunit;

namespace XmlSchemaClassGenerator.Tests;
public sealed class IntegerTypeTests
{
    private static IEnumerable<string> ConvertXml(string xsd, Generator generatorPrototype)
    {
        var writer = new MemoryOutputWriter();

        var gen = new Generator
        {
            OutputWriter = writer,
            Version = new("Tests", "1.0.0.1"),
            NamespaceProvider = generatorPrototype.NamespaceProvider,
            GenerateNullables = generatorPrototype.GenerateNullables,
            IntegerDataType = generatorPrototype.IntegerDataType,
            UseIntegerDataTypeAsFallback = generatorPrototype.UseIntegerDataTypeAsFallback,
            DataAnnotationMode = generatorPrototype.DataAnnotationMode,
            GenerateDesignerCategoryAttribute = generatorPrototype.GenerateDesignerCategoryAttribute,
            GenerateComplexTypesForCollections = generatorPrototype.GenerateComplexTypesForCollections,
            EntityFramework = generatorPrototype.EntityFramework,
            AssemblyVisible = generatorPrototype.AssemblyVisible,
            GenerateInterfaces = generatorPrototype.GenerateInterfaces,
            MemberVisitor = generatorPrototype.MemberVisitor,
            CodeTypeReferenceOptions = generatorPrototype.CodeTypeReferenceOptions
        };

        var set = new XmlSchemaSet();

        using (var stringReader = new StringReader(xsd))
        {
            var schema = XmlSchema.Read(stringReader, (_, e) => throw new InvalidOperationException($"{e.Severity}: {e.Message}",e.Exception));
				ArgumentNullException.ThrowIfNull(schema);
            set.Add(schema);
        }

        gen.Generate(set);

        return writer.Content;
    }

    [Theory]
    [InlineData(2, "sbyte")]
    [InlineData(4, "short")]
    [InlineData(9, "int")]
    [InlineData(18, "long")]
    [InlineData(28, "decimal")]
    [InlineData(29, "string")]
    public void TestTotalDigits(int totalDigits, string expectedType)
    {
        var xsd = @$"<?xml version=""1.0"" encoding=""UTF-8""?>
<xs:schema elementFormDefault=""qualified"" xmlns:xs=""http://www.w3.org/2001/XMLSchema"">
	<xs:complexType name=""document"">
		<xs:sequence>
			<xs:element name=""someValue"">
				<xs:simpleType>
					<xs:restriction base=""xs:integer"">
						<xs:totalDigits value=""{totalDigits}""/>
					</xs:restriction>
			</xs:simpleType>
			</xs:element>
		</xs:sequence>
	</xs:complexType>
</xs:schema>";

        var generatedType = ConvertXml(
	            xsd, new()
        {
            NamespaceProvider = new()
            {
                GenerateNamespace = _ => "Test"
            }
        });

        var expectedProperty = $"public {expectedType} SomeValue";
        var generatedProperty = generatedType.First();

			Assert.Contains(expectedProperty, generatedProperty);
    }

    [Theory]
    [InlineData(4, false, "long")]
    [InlineData(30, false, "long")]
    [InlineData(4, true, "short")]
    [InlineData(30, true, "long")]
    public void TestFallbackType(int totalDigits, bool useTypeAsFallback, string expectedType)
    {
        var xsd = @$"<?xml version=""1.0"" encoding=""UTF-8""?>
<xs:schema elementFormDefault=""qualified"" xmlns:xs=""http://www.w3.org/2001/XMLSchema"">
	<xs:complexType name=""document"">
		<xs:sequence>
			<xs:element name=""someValue"">
				<xs:simpleType>
					<xs:restriction base=""xs:integer"">
						<xs:totalDigits value=""{totalDigits}""/>
					</xs:restriction>
				</xs:simpleType>
			</xs:element>
		</xs:sequence>
	</xs:complexType>
</xs:schema>";

      var generatedType = ConvertXml(
	          xsd, new()
      {
          NamespaceProvider = new()
          {
              GenerateNamespace = _ => "Test"
          },
          IntegerDataType = typeof(long),
          UseIntegerDataTypeAsFallback = useTypeAsFallback
      });

      var expectedProperty = $"public {expectedType} SomeValue";
      var generatedProperty = generatedType.First();

      Assert.Contains(expectedProperty, generatedProperty);
    }

		[Theory]
		[InlineData(1, 100, "byte")]
		[InlineData(byte.MinValue, byte.MaxValue, "byte")]
		[InlineData(-100, 100, "sbyte")]
		[InlineData(sbyte.MinValue, sbyte.MaxValue, "sbyte")]
		[InlineData(1, 1000, "ushort")]
		[InlineData(ushort.MinValue, ushort.MaxValue, "ushort")]
		[InlineData(-1000, 1000, "short")]
		[InlineData(short.MinValue, short.MaxValue, "short")]
		[InlineData(1, 100000, "uint")]
		[InlineData(uint.MinValue, uint.MaxValue, "uint")]
		[InlineData(-100000, 100000, "int")]
		[InlineData(int.MinValue, int.MaxValue, "int")]
		[InlineData(1, 10000000000, "ulong")]
		[InlineData(ulong.MinValue, ulong.MaxValue, "ulong")]
		[InlineData(-10000000000, 10000000000, "long")]
		[InlineData(long.MinValue, long.MaxValue, "long")]
		public void TestInclusiveRange(long minInclusive, ulong maxInclusive, string expectedType)
    {
        var xsd = @$"<?xml version=""1.0"" encoding=""UTF-8""?>
<xs:schema elementFormDefault=""qualified"" xmlns:xs=""http://www.w3.org/2001/XMLSchema"">
	<xs:complexType name=""document"">
		<xs:sequence>
			<xs:element name=""someValue"">
				<xs:simpleType>
					<xs:restriction base=""xs:integer"">
						<xs:minInclusive value=""{minInclusive}""/>
						<xs:maxInclusive value=""{maxInclusive}""/>
					</xs:restriction>
				</xs:simpleType>
			</xs:element>
		</xs:sequence>
	</xs:complexType>
</xs:schema>";

      var generatedType = ConvertXml(
	          xsd, new()
      {
          NamespaceProvider = new()
          {
              GenerateNamespace = _ => "Test"
          }
      });

      var expectedProperty = $"public {expectedType} SomeValue";
      var generatedProperty = generatedType.First();

      Assert.Contains(expectedProperty, generatedProperty);
    }

		[Theory]
		[InlineData(2, "sbyte")]
		[InlineData(4, "short")]
		[InlineData(9, "int")]
		[InlineData(18, "long")]
		[InlineData(28, "decimal")]
		[InlineData(29, "string")]
		public void TestDecimalFractionDigitsZeroTotalDigits(int totalDigits, string expectedType)
		{
			var xsd = @$"<?xml version=""1.0"" encoding=""UTF-8""?>
<xs:schema elementFormDefault=""qualified"" xmlns:xs=""http://www.w3.org/2001/XMLSchema"">
	<xs:complexType name=""document"">
		<xs:sequence>
			<xs:element name=""someValue"">
				<xs:simpleType>
					<xs:restriction base=""xs:decimal"">
						<xs:totalDigits value=""{totalDigits}""/>
						<xs:fractionDigits value=""0""/>
					</xs:restriction>
			</xs:simpleType>
			</xs:element>
		</xs:sequence>
	</xs:complexType>
</xs:schema>";

			var generatedType = ConvertXml(xsd, new()
			{
				NamespaceProvider = new()
				{
					GenerateNamespace = _ => "Test"
				}
			});

			var expectedProperty = $"public {expectedType} SomeValue";
			var generatedProperty = generatedType.First();

			Assert.Contains(expectedProperty, generatedProperty);
		}


		[Theory]
		[InlineData(4, false, "long")]
		[InlineData(30, false, "long")]
		[InlineData(4, true, "short")]
		[InlineData(30, true, "long")]
		public void TestDecimalFractionDigitsFallbackType(int totalDigits, bool useTypeAsFallback, string expectedType)
		{
			var xsd = @$"<?xml version=""1.0"" encoding=""UTF-8""?>
<xs:schema elementFormDefault=""qualified"" xmlns:xs=""http://www.w3.org/2001/XMLSchema"">
	<xs:complexType name=""document"">
		<xs:sequence>
			<xs:element name=""someValue"">
				<xs:simpleType>
					<xs:restriction base=""xs:decimal"">
						<xs:totalDigits value=""{totalDigits}""/>
						<xs:fractionDigits value=""0""/>
					</xs:restriction>
				</xs:simpleType>
			</xs:element>
		</xs:sequence>
	</xs:complexType>
</xs:schema>";

			var generatedType = ConvertXml(
				xsd, new()
			{
				NamespaceProvider = new()
				{
					GenerateNamespace = _ => "Test"
				},
				IntegerDataType = typeof(long),
				UseIntegerDataTypeAsFallback = useTypeAsFallback
			});

			var expectedProperty = $"public {expectedType} SomeValue";
			var generatedProperty = generatedType.First();

			Assert.Contains(expectedProperty, generatedProperty);
		}

		[Theory]
		[InlineData(1, 100, "byte")]
		[InlineData(byte.MinValue, byte.MaxValue, "byte")]
		[InlineData(-100, 100, "sbyte")]
		[InlineData(sbyte.MinValue, sbyte.MaxValue, "sbyte")]
		[InlineData(1, 1000, "ushort")]
		[InlineData(ushort.MinValue, ushort.MaxValue, "ushort")]
		[InlineData(-1000, 1000, "short")]
		[InlineData(short.MinValue, short.MaxValue, "short")]
		[InlineData(1, 100000, "uint")]
		[InlineData(uint.MinValue, uint.MaxValue, "uint")]
		[InlineData(-100000, 100000, "int")]
		[InlineData(int.MinValue, int.MaxValue, "int")]
		[InlineData(1, 10000000000, "ulong")]
		[InlineData(ulong.MinValue, ulong.MaxValue, "ulong")]
		[InlineData(-10000000000, 10000000000, "long")]
		[InlineData(long.MinValue, long.MaxValue, "long")]
		public void TestDecimalFractionDigitsZeroInclusiveRange(long minInclusive, ulong maxInclusive, string expectedType)
		{
			var xsd = @$"<?xml version=""1.0"" encoding=""UTF-8""?>
<xs:schema elementFormDefault=""qualified"" xmlns:xs=""http://www.w3.org/2001/XMLSchema"">
	<xs:complexType name=""document"">
		<xs:sequence>
			<xs:element name=""someValue"">
				<xs:simpleType>
					<xs:restriction base=""xs:decimal"">
						<xs:minInclusive value=""{minInclusive}""/>
						<xs:maxInclusive value=""{maxInclusive}""/>
						<xs:fractionDigits value=""0""/>
					</xs:restriction>
				</xs:simpleType>
			</xs:element>
		</xs:sequence>
	</xs:complexType>
</xs:schema>";

			var generatedType = ConvertXml(
				xsd, new()
			{
				NamespaceProvider = new()
				{
					GenerateNamespace = _ => "Test"
				}
			});

			var expectedProperty = $"public {expectedType} SomeValue";
			var generatedProperty = generatedType.First();

			Assert.Contains(expectedProperty, generatedProperty);
		}

		[Theory]
		[InlineData(typeof(sbyte), "sbyte")]
		[InlineData(typeof(short), "short")]
		[InlineData(typeof(int), "int")]
		[InlineData(typeof(long), "long")]
		[InlineData(typeof(nint), "System.IntPtr")]
		[InlineData(typeof(byte), "byte")]
		[InlineData(typeof(ushort), "ushort")]
		[InlineData(typeof(uint), "uint")]
		[InlineData(typeof(ulong), "ulong")]
		[InlineData(typeof(nuint), "System.UIntPtr")]
		[InlineData(typeof(decimal), "decimal")]
		public void TestExplicitIntegerType(Type integerType, string expectedType)
		{
			// totalDigits=9 would auto-detect as int; the explicit override must always win
			var xsd = @$"<?xml version=""1.0"" encoding=""UTF-8""?>
<xs:schema elementFormDefault=""qualified"" xmlns:xs=""http://www.w3.org/2001/XMLSchema"">
	<xs:complexType name=""document"">
		<xs:sequence>
			<xs:element name=""someValue"">
				<xs:simpleType>
					<xs:restriction base=""xs:integer"">
						<xs:totalDigits value=""9""/>
					</xs:restriction>
				</xs:simpleType>
			</xs:element>
		</xs:sequence>
	</xs:complexType>
</xs:schema>";

			var generatedType = ConvertXml(xsd, new()
			{
				NamespaceProvider = new()
				{
					GenerateNamespace = _ => "Test"
				},
				IntegerDataType = integerType,
				UseIntegerDataTypeAsFallback = false
			});

			var expectedProperty = $"public {expectedType} SomeValue";
			var generatedProperty = generatedType.First();

			Assert.Contains(expectedProperty, generatedProperty);
		}

		[Theory]
		[InlineData(typeof(sbyte), 2, "sbyte")]           // auto = sbyte  → fallback not used
		[InlineData(typeof(sbyte), 29, "sbyte")]          // auto = null   → fallback sbyte used
		[InlineData(typeof(byte), 2, "sbyte")]            // auto = sbyte  → fallback byte not used
		[InlineData(typeof(byte), 29, "byte")]            // auto = null   → fallback byte used
		[InlineData(typeof(ushort), 4, "short")]          // auto = short  → fallback ushort not used
		[InlineData(typeof(ushort), 29, "ushort")]        // auto = null   → fallback ushort used
		[InlineData(typeof(uint), 9, "int")]              // auto = int    → fallback uint not used
		[InlineData(typeof(uint), 29, "uint")]            // auto = null   → fallback uint used
		[InlineData(typeof(ulong), 18, "long")]           // auto = long   → fallback ulong not used
		[InlineData(typeof(ulong), 29, "ulong")]          // auto = null   → fallback ulong used
		[InlineData(typeof(nint), 9, "int")]              // auto = int    → fallback nint not used
		[InlineData(typeof(nint), 29, "System.IntPtr")]   // auto = null   → fallback nint used
		[InlineData(typeof(nuint), 9, "int")]             // auto = int    → fallback nuint not used
		[InlineData(typeof(nuint), 29, "System.UIntPtr")] // auto = null   → fallback nuint used
		public void TestFallbackTypeExtended(Type fallbackType, int totalDigits, string expectedType)
		{
			// When UseIntegerDataTypeAsFallback=true the fallback is only used when auto-detection yields no result
			var xsd = @$"<?xml version=""1.0"" encoding=""UTF-8""?>
<xs:schema elementFormDefault=""qualified"" xmlns:xs=""http://www.w3.org/2001/XMLSchema"">
	<xs:complexType name=""document"">
		<xs:sequence>
			<xs:element name=""someValue"">
				<xs:simpleType>
					<xs:restriction base=""xs:integer"">
						<xs:totalDigits value=""{totalDigits}""/>
					</xs:restriction>
				</xs:simpleType>
			</xs:element>
		</xs:sequence>
	</xs:complexType>
</xs:schema>";

			var generatedType = ConvertXml(xsd, new()
			{
				NamespaceProvider = new()
				{
					GenerateNamespace = _ => "Test"
				},
				IntegerDataType = fallbackType,
				UseIntegerDataTypeAsFallback = true
			});

			var expectedProperty = $"public {expectedType} SomeValue";
			var generatedProperty = generatedType.First();

			Assert.Contains(expectedProperty, generatedProperty);
		}
	}
