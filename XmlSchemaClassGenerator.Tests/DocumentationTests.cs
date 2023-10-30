using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Schema;

using Xunit;
using Xunit.Abstractions;

namespace XmlSchemaClassGenerator.Tests;

public sealed class DocumentationTests
{
	private readonly ITestOutputHelper _testOutputHelper;

	public DocumentationTests(ITestOutputHelper testOutputHelper) => _testOutputHelper = testOutputHelper;

	private static IEnumerable<string> ConvertXml(string xsd, Generator generatorPrototype)
	{
		var writer = new MemoryOutputWriter();

		var gen = new Generator
		{
			OutputWriter = writer,
			Version = new("Tests", "1.0.0.1"),
			NamespaceProvider = generatorPrototype.NamespaceProvider,
			//DataAnnotationMode = generatorPrototype.DataAnnotationMode,
			GenerateDescriptionAttribute = generatorPrototype.GenerateDescriptionAttribute
		};

		var set = new XmlSchemaSet();

		using (var stringReader = new StringReader(xsd))
		{
			var schema = XmlSchema.Read
			(
				stringReader,
				(_, e) => throw new InvalidOperationException($"{e.Severity}: {e.Message}", e.Exception)
			);

			ArgumentNullException.ThrowIfNull(schema);
			set.Add(schema);
		}

		gen.Generate(set);

		return writer.Content;
	}

	[Fact]
	public void TestSummaryDoc()
	{
		const string xsd = """
		                   <?xml version="1.0" encoding="UTF-8"?>
		                   <xs:schema targetNamespace="urn:customs.ru:Envelope:ApplicationInf:1.0" elementFormDefault="qualified" version="1.0.0" xmlns:xs="http://www.w3.org/2001/XMLSchema" xmlns:app="urn:customs.ru:Envelope:ApplicationInf:1.0">
		                   	<xs:element name="ApplicationInf" type="app:ApplicationInfType"/>
		                   	<xs:complexType name="ApplicationInfType">
		                   		<xs:annotation>
		                   			<xs:documentation>Заголовок конверта.</xs:documentation>
		                   			<xs:documentation>Информация приложения</xs:documentation>
		                   		</xs:annotation>
		                   		<xs:sequence>
		                   			<xs:element name="SoftKind" type="xs:string" minOccurs="0">
		                   				<xs:annotation>
		                   					<xs:documentation>Тип программного обеспечения</xs:documentation>
		                   				</xs:annotation>
		                   			</xs:element>
		                   			<xs:element name="SoftVersion" type="xs:string" minOccurs="0">
		                   				<xs:annotation>
		                   					<xs:documentation>Версия программного</xs:documentation>
		                   					<xs:documentation>обеспечения</xs:documentation>
		                   				</xs:annotation>
		                   			</xs:element>
		                   			<xs:element name="MessageKind" type="xs:string" minOccurs="0">
		                   				<xs:annotation>
		                   					<xs:documentation>Тип сообщения</xs:documentation>
		                   				</xs:annotation>
		                   			</xs:element>
		                   		</xs:sequence>
		                   	</xs:complexType>
		                   </xs:schema>
		                   """;

		var code = ConvertXml(xsd, new() {NamespacePrefix = "Test"})
			.Single();
		_testOutputHelper.WriteLine(code);
		Assert.Contains
		(
			"""
			    /// <summary>
			    /// <para>Заголовок конверта.</para>
			    /// <para>Информация приложения</para>
			    /// </summary>
			""",
			code,
			StringComparison.Ordinal
		);

		Assert.Contains
		(
			"""
			        /// <summary>
			        /// <para>Тип программного обеспечения</para>
			        /// </summary>
			""",
			code,
			StringComparison.Ordinal
		);
		Assert.Contains
		(
			"""
			        /// <summary>
			        /// <para>Версия программного</para>
			        /// <para>обеспечения</para>
			        /// </summary>
			""",
			code,
			StringComparison.Ordinal
		);

		Assert.Contains
		(
			"""
			        /// <summary>
			        /// <para>Тип сообщения</para>
			        /// </summary>
			""",
			code,
			StringComparison.Ordinal
		);
	}

	[Fact]
	public void TestDescriptionAttributeValue()
	{
		const string xsd = """
		                   <?xml version="1.0" encoding="UTF-8"?>
		                   <xs:schema targetNamespace="urn:customs.ru:Envelope:ApplicationInf:1.0" elementFormDefault="qualified" version="1.0.0" xmlns:xs="http://www.w3.org/2001/XMLSchema" xmlns:app="urn:customs.ru:Envelope:ApplicationInf:1.0">
		                   	<xs:element name="ApplicationInf" type="app:ApplicationInfType"/>
		                   	<xs:complexType name="ApplicationInfType">
		                      		<xs:annotation>
		                         			<xs:documentation>Заголовок конверта.</xs:documentation>
		                         			<xs:documentation>Информация приложения</xs:documentation>
		                      		</xs:annotation>
		                      		<xs:sequence>
		                         			<xs:element name="SoftKind" type="xs:string" minOccurs="0">
		                            				<xs:annotation>
		                               					<xs:documentation>Тип программного обеспечения</xs:documentation>
		                            				</xs:annotation>
		                         			</xs:element>
		                         			<xs:element name="SoftVersion" type="xs:string" minOccurs="0">
		                            				<xs:annotation>
		                               					<xs:documentation>Версия программного</xs:documentation>
		                               					<xs:documentation>обеспечения</xs:documentation>
		                            				</xs:annotation>
		                         			</xs:element>
		                         			<xs:element name="MessageKind" type="xs:string" minOccurs="0">
		                            				<xs:annotation>
		                               					<xs:documentation>Тип сообщения</xs:documentation>
		                            				</xs:annotation>
		                         			</xs:element>
		                      		</xs:sequence>
		                   	</xs:complexType>
		                   </xs:schema>
		                   """;

		var code = ConvertXml(xsd, new() { NamespacePrefix = "Test", GenerateDescriptionAttribute = true })
			.Single();

		Assert.Contains
		(
			"""[System.ComponentModel.DescriptionAttribute("Заголовок конверта. Информация приложения")]""",
			code,
			StringComparison.Ordinal
		);

		Assert.Contains
		(
			"""[System.ComponentModel.DescriptionAttribute("Тип программного обеспечения")]""",
			code,
			StringComparison.Ordinal
		);
		Assert.Contains
		(
			"""[System.ComponentModel.DescriptionAttribute("Версия программного обеспечения")]""",
			code,
			StringComparison.Ordinal
		);

		Assert.Contains
		(
			"""[System.ComponentModel.DescriptionAttribute("Тип сообщения")]""",
			code,
			StringComparison.Ordinal
		);
	}
}