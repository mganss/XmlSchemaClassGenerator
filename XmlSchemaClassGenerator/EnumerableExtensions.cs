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

        public static IEnumerable<RestrictionModel> Sanitize(this IEnumerable<RestrictionModel> input)
        {
            var patterns = new List<PatternRestrictionModel>();
            foreach (var item in input)
            {
                var pattern = item as PatternRestrictionModel;
                if (pattern != null)
                    patterns.Add(pattern);
                else
                    yield return item;
            }
            if (patterns.Count == 1)
            {
                yield return patterns[0];
            }
            else if (patterns.Count > 1)
            {
                var pattern = string.Join("|", patterns.Select(x => string.Format("({0})", x.Value)));
                yield return new PatternRestrictionModel()
                {
                    Value = pattern,
                };
            }
        }
    }
}
