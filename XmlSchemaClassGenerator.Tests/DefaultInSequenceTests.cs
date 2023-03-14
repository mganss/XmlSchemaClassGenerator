using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace XmlSchemaClassGenerator.Tests
{
	public class DefaultInSequenceTests
	{
		/// <summary>
		/// In case a element which is defined as a sequence, has a default value,
		/// the default value shall be ignored (it was wrong to initialize a collection with int value, for example).
		/// bakcing field was like: private Collection<int> _someElementWithDefaultValue = 0;
		/// In such case, the syntax breaks compliation.
		/// We would like to verify the default is ignored.
		/// </summary>
		[Fact]
		public void SequenceWithDefaultValue_CompilationPass()
		{
			var assembly = Compiler.Generate(nameof(SequenceWithDefaultValue_CompilationPass),
				"xsd/SequenceWithDefault/*.xsd", new Generator
				{
					GenerateNullables = true,
					UseShouldSerializePattern = true,
					NamespaceProvider = new NamespaceProvider
					{
						GenerateNamespace = key => "Test"
					}
				});

			var type = assembly.GetType("Test.SomeComplexType");

			var collectionProperties = type
				.GetProperties()
				.Select(p => (p.Name, p.PropertyType))
				.OrderBy(p => p.Name)
				.ToArray();

			Assert.Equal(new[]
			{
				("SomeElementWithDefault", typeof(Collection<int>)),
			}, collectionProperties);
		}
	}
}
