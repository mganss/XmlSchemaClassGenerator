using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace XmlSchemaClassGenerator.Tests
{
    public class NamespaceProviderTests
    {
        [Fact]
        public void ContainsKeyTest()
        {
            var ns = new NamespaceProvider { { new NamespaceKey("x"), "c" } };
            ns.GenerateNamespace = k => k.XmlSchemaNamespace != "z" ? k.XmlSchemaNamespace : null;
            AssertEx.CollectionEqual(ns.Values.ToArray(), new[] { "c" });
            Assert.True(ns.ContainsKey(new NamespaceKey("x")));
            Assert.True(ns.ContainsKey(new NamespaceKey("y")));
            Assert.True(ns.ContainsKey(new NamespaceKey("y")));
            Assert.False(ns.ContainsKey(new NamespaceKey("z")));
            ns.Clear();
            Assert.Empty(ns);
        }

        [Fact]
        public void KeysTest()
        {
            var ns = new NamespaceProvider { { new NamespaceKey("x"), "c" }, { new NamespaceKey("y"), "d" } };
            ns.Remove(new NamespaceKey("y"));
            AssertEx.CollectionEqual(ns.Keys.ToArray(), new[] { new NamespaceKey("x") });
        }

        [Fact]
        public void IndexTest()
        {
            var ns = new NamespaceProvider
            {
                [new NamespaceKey("x")] = "c",
                GenerateNamespace = k => k.XmlSchemaNamespace != "z" ? k.XmlSchemaNamespace : null
            };

            Assert.Equal("c", ns[new NamespaceKey("x")]);
            Assert.Equal("y", ns[new NamespaceKey("y")]);
            Assert.Equal("y", ns[new NamespaceKey("y")]);
            Assert.Throws<KeyNotFoundException>(() => ns[new NamespaceKey("z")]);
        }

        [Fact]
        public void NamespaceKeyComparableTest()
        {
            Assert.Equal(-1, new NamespaceKey((Uri)null).CompareTo(new NamespaceKey(new Uri("http://test"))));
            Assert.Equal(1, new NamespaceKey(new Uri("http://test")).CompareTo(new NamespaceKey((Uri)null)));
            Assert.NotEqual(0, new NamespaceKey(new Uri("http://test")).CompareTo(new NamespaceKey(new Uri("http://test2"))));
            Assert.True(new NamespaceKey("http://test").Equals((object)new NamespaceKey("http://test")));
            Assert.False(new NamespaceKey("http://test").Equals((object)null));
            Assert.NotEqual(0, ((IComparable)new NamespaceKey("http://test")).CompareTo(null));
            Assert.True(new NamespaceKey("http://test") == new NamespaceKey("http://test"));
            Assert.True(((NamespaceKey)null) == ((NamespaceKey)null));
            Assert.True(new NamespaceKey("http://test") > null);
            Assert.False(new NamespaceKey("http://test") < null);
            Assert.True(new NamespaceKey("http://test") >= null);
            Assert.False(new NamespaceKey("http://test") <= null);
            Assert.True(new NamespaceKey("http://test") != null);
        }

        [Theory]
        [InlineData("http://www.w3.org/2001/XMLSchema", "test.xsd", "MyNamespace", "Test")]
        [InlineData("http://www.w3.org/2001/XMLSchema", "test.xsd", "MyNamespace", null)]
        [InlineData("http://www.w3.org/2001/XMLSchema", "test.xsd", "MyNamespace", "")]
        [InlineData("", "test.xsd", "MyNamespace", "Test")]
        [InlineData("", "test.xsd", "MyNamespace", null)]
        [InlineData("", "test.xsd", "MyNamespace", "")]
        [InlineData(null, "test.xsd", "MyNamespace", "Test")]
        [InlineData(null, "test.xsd", "MyNamespace", null)]
        [InlineData(null, "test.xsd", "MyNamespace", "")]
        public void TestParseNamespaceUtilityMethod1(string xmlNs, string xmlSchema, string netNs, string netPrefix)
        {
            string customNsPattern = "{0}|{1}={2}";

            var uri = new Uri(xmlSchema, UriKind.RelativeOrAbsolute);
            var fullNetNs = (string.IsNullOrEmpty(netPrefix)) ? netNs : string.Join(".", netPrefix, netNs);

            var expected = new KeyValuePair<NamespaceKey, string>(new NamespaceKey(uri, xmlNs), fullNetNs);
            var actual = CodeUtilities.ParseNamespace(string.Format(customNsPattern, xmlNs, xmlSchema, netNs), netPrefix);

            Assert.Equal(expected, actual);
        }

        [Theory]
        [InlineData("test.xsd", "MyNamespace", "Test")]
        [InlineData("test.xsd", "MyNamespace", null)]
        [InlineData("test.xsd", "MyNamespace", "")]
        public void TestParseNamespaceUtilityMethod2(string xmlSchema, string netNs, string netPrefix)
        {
            string customNsPattern = "{0}={1}";

            var fullNetNs = (string.IsNullOrEmpty(netPrefix)) ? netNs : string.Join(".", netPrefix, netNs);
            var expected = new KeyValuePair<NamespaceKey, string>(new NamespaceKey(null, xmlSchema), fullNetNs);
            var actual = CodeUtilities.ParseNamespace(string.Format(customNsPattern, xmlSchema, netNs), netPrefix);

            Assert.Equal(expected, actual);
        }
    }
}
