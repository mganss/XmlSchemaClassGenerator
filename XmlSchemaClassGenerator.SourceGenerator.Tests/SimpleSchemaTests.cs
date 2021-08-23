using Xunit;

namespace XmlSchemaClassGenerator.SourceGenerator.Tests
{
    public class SimpleSchemaTests
    {
        [Fact]
        public void Compiles()
        {
            new Sample.Generated.MyRootElement
            {
                Child1 = true,
                Child2 = "foo",
                ComplexChild1 = new Sample.Generated.MyRootElementComplexChild1
                {
                    Child11 = null
                }
            };
        }
    }
}
