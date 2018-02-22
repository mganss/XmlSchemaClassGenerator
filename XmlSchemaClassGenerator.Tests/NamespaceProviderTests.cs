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
            var ns = new NamespaceProvider();
            ns[new NamespaceKey("x")] = "c";
            ns.GenerateNamespace = k => k.XmlSchemaNamespace != "z" ? k.XmlSchemaNamespace : null;
            Assert.Equal("c", ns[new NamespaceKey("x")]);
            Assert.Equal("y", ns[new NamespaceKey("y")]);
            Assert.Equal("y", ns[new NamespaceKey("y")]);
            Assert.Throws<KeyNotFoundException>(() => ns[new NamespaceKey("z")]);
        }
    }
}
