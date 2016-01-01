using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using Xunit;

namespace XmlSchemaClassGenerator.Tests
{
    class AssertEx
    {
        public static async Task ThrowsAsync<TException>(Func<Task> func)
        {
            var expected = typeof(TException);
            Type actual = null;
            try
            {
                await func();
            }
            catch (Exception e)
            {
                actual = e.GetType();
            }
            Assert.Equal(expected, actual);
        }

        public static void Equal(object o1, object o2)
        {
            if (o1 == null && o2 == null) return;
            Assert.NotNull(o1);
            Assert.NotNull(o2);

            var type1 = o1.GetType();
            var type2 = o2.GetType();
            Assert.Equal(type1, type2);

            if (type1.IsPrimitive || type1.IsEnum || type1 == typeof(string) || type1 == typeof(System.DateTime))
            {
                Assert.Equal(o1, o2);
            }
            else if (type1 == typeof(XmlAttribute))
            {
                var a1 = (XmlAttribute)o1;
                var a2 = (XmlAttribute)o2;

                Assert.Equal(a1.Name, a1.Name);
                Assert.Equal(a1.Value, a2.Value);
            }
            else if (type1 == typeof(XmlElement))
            {
                var e1 = (XmlElement)o1;
                var e2 = (XmlElement)o2;

                Assert.Equal(e1.Name, e1.Name);
                Assert.Equal(e1.InnerXml, e2.InnerXml);
            }
            else if (typeof(IList).IsAssignableFrom(type1))
            {
                var arr1 = (IList)o1;
                var arr2 = (IList)o2;

                Assert.Equal(arr1.Count, arr2.Count);

                for (int i = 0; i < arr1.Count; i++)
                {
                    Equal(arr1[i], arr2[i]);
                }
            }
            else
            {
                foreach (var prop in type1.GetProperties(BindingFlags.Public | BindingFlags.Instance).Where(p => p.CanRead && !p.GetIndexParameters().Any()))
                {
                    var val1 = prop.GetValue(o1);
                    var val2 = prop.GetValue(o2);

                    Equal(val1, val2);
                }
            }
        }

        public static void CollectionEqual<T>(IList<T> l1, IList<T> l2)
        {
            if (l1 == null && l2 == null) return;
            Assert.NotNull(l1);
            Assert.NotNull(l2);

            Assert.Equal(l1.Count, l2.Count);
            for (int i = 0; i < l1.Count; i++)
            {
                Assert.Equal(l1[i], l2[i]);
            }
        }
    }
}
