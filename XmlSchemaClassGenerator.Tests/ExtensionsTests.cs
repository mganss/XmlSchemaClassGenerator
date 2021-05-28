using Xunit;

namespace XmlSchemaClassGenerator.Tests
{
    public class ExtensionsTests
    {
        [Theory]
        [InlineData(null, null)]
        [InlineData("", "")]
        [InlineData("MyText", "MyText")]
        [InlineData("My Text", "\"My Text\"")]
        public void QuoteEmptyOrNull(string input, string expected)
        {
            Assert.Equal(expected, input.QuoteIfNeeded());
        }
    }
}