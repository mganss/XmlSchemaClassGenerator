using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XmlSchemaClassGenerator
{
    public static class EnumerableExtensions
    {
        public static NamespaceProvider ToNamespaceProvider(this IEnumerable<KeyValuePair<NamespaceKey, string>> items,
            GenerateNamespaceDelegate generate = null)
        {
            var result = new NamespaceProvider()
            {
                GenerateNamespace = generate,
            };
            foreach (var item in items)
                result.Add(item.Key, item.Value);
            return result;
        }
    }
}
