using System;
using Xunit;

namespace XmlSchemaClassGenerator.Tests;

public class NormalizingXmlResolverTests
{
    [Theory]
    [InlineData("none", "http://foo.bar.com/baz.xsd", "http://foo.bar.com", "baz.xsd")]
    [InlineData("none", "http://x.y.z", "https://foo.bar.com/baz.xsd", "http://x.y.z")]
    [InlineData("same", "http://foo.bar.com/baz.xsd", "http://foo.bar.com", "baz.xsd")]
    [InlineData("same", "https://x.y.z", "https://foo.bar.com/baz.xsd", "http://x.y.z")]
    [InlineData("http", "http://foo.bar.com/baz.xsd", "https://foo.bar.com", "baz.xsd")]
    [InlineData("http", "http://x.y.z", "http://foo.bar.com/baz.xsd", "https://x.y.z")]
    [InlineData("https", "https://foo.bar.com/baz.xsd", "http://foo.bar.com", "baz.xsd")]
    [InlineData("https", "https://x.y.z", "http://foo.bar.com/baz.xsd", "http://x.y.z")]
    [InlineData("file", "file://foo.bar.com/baz.xsd", "http://foo.bar.com", "baz.xsd")]
    [InlineData("file", "file://x.y.z/a.xsd", "http://foo.bar.com/baz.xsd", "https://x.y.z/a.xsd")]
    public void TestOverrides(string forceScheme, string expect, string baseUri, string relUri)
    {
        var res = new NormalizingXmlResolver(forceScheme);
        Assert.Equal(new Uri(expect), res.ResolveUri(new Uri(baseUri), relUri));
    }
}