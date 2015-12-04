using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XmlSchemaClassGenerator
{
    public class NamespaceProvider : IDictionary<NamespaceKey, string>
    {
        private readonly Dictionary<NamespaceKey, string> InternalDictionary = new Dictionary<NamespaceKey, string>();

        public GenerateNamespaceDelegate GenerateNamespace;

        protected virtual string OnGenerateNamespace(NamespaceKey key)
        {
            if (GenerateNamespace != null)
                return GenerateNamespace(key);
            return null;
        }

        protected virtual bool TryGenerateNamespace(NamespaceKey key, out string ns)
        {
            ns = OnGenerateNamespace(key);
            return !string.IsNullOrEmpty(ns);
        }

        public void Add(NamespaceKey key, string value)
        {
            InternalDictionary.Add(key, value);
        }

        public bool ContainsKey(NamespaceKey key)
        {
            if (InternalDictionary.ContainsKey(key))
                return true;
            string ns;
            if (!TryGenerateNamespace(key, out ns))
                return false;
            InternalDictionary.Add(key, ns);
            return true;
        }

        public ICollection<NamespaceKey> Keys
        {
            get { return InternalDictionary.Keys; }
        }

        public bool Remove(NamespaceKey key)
        {
            return InternalDictionary.Remove(key);
        }

        public bool TryGetValue(NamespaceKey key, out string value)
        {
            if (InternalDictionary.TryGetValue(key, out value))
                return true;
            if (!TryGenerateNamespace(key, out value))
                return false;
            InternalDictionary.Add(key, value);
            return true;
        }

        public ICollection<string> Values
        {
            get { return InternalDictionary.Values; }
        }

        public string this[NamespaceKey key]
        {
            get
            {
                string result;
                if (TryGetValue(key, out result))
                    return result;
                throw new KeyNotFoundException();
            }
            set { InternalDictionary[key] = value; }
        }

        void ICollection<KeyValuePair<NamespaceKey, string>>.Add(KeyValuePair<NamespaceKey, string> item)
        {
            InternalDictionary.Add(item.Key, item.Value);
        }

        public void Clear()
        {
            InternalDictionary.Clear();
        }

        bool ICollection<KeyValuePair<NamespaceKey, string>>.Contains(KeyValuePair<NamespaceKey, string> item)
        {
            return InternalDictionary.Contains(item);
        }

        void ICollection<KeyValuePair<NamespaceKey, string>>.CopyTo(KeyValuePair<NamespaceKey, string>[] array, int arrayIndex)
        {
            ((IDictionary<NamespaceKey, string>)InternalDictionary).CopyTo(array, arrayIndex);
        }

        public int Count
        {
            get { return InternalDictionary.Count; }
        }

        bool ICollection<KeyValuePair<NamespaceKey, string>>.IsReadOnly
        {
            get { return ((IDictionary<NamespaceKey, string>)InternalDictionary).IsReadOnly; }
        }

        bool ICollection<KeyValuePair<NamespaceKey, string>>.Remove(KeyValuePair<NamespaceKey, string> item)
        {
            return ((IDictionary<NamespaceKey, string>)InternalDictionary).Remove(item);
        }

        public IEnumerator<KeyValuePair<NamespaceKey, string>> GetEnumerator()
        {
            return ((IDictionary<NamespaceKey, string>)InternalDictionary).GetEnumerator();
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return ((System.Collections.IEnumerable)InternalDictionary).GetEnumerator();
        }

        public string FindNamespace(NamespaceKey key, string defaultNamespace = null)
        {
            var keyValues = new List<NamespaceKey>
	        {
                // First, search for key as-is
	            key,
	        };
            if (key.Source == null)
            {
                if (key.XmlSchemaNamespace != null)
                    // Search for empty key
                    keyValues.Add(new NamespaceKey());
            }
            else if (key.XmlSchemaNamespace != null)
            {
                // Search for URI only
                keyValues.Add(new NamespaceKey(key.Source));

                // Search for file name only
                var path = key.Source.IsAbsoluteUri ? key.Source.LocalPath : key.Source.OriginalString;
                keyValues.Add(new NamespaceKey(new Uri(Path.GetFileName(path), UriKind.Relative)));

                // Search for XmlSchemaNamespace only
                keyValues.Add(new NamespaceKey(key.XmlSchemaNamespace));

                // Search for empty key
                keyValues.Add(new NamespaceKey());
            }
            else
            {
                // Search for file name only
                var path = key.Source.IsAbsoluteUri ? key.Source.LocalPath : key.Source.OriginalString;
                keyValues.Add(new NamespaceKey(new Uri(Path.GetFileName(path), UriKind.Relative)));

                // Search for empty key
                keyValues.Add(new NamespaceKey());
            }

            foreach (var keyValue in keyValues)
            {
                string result;
                if (InternalDictionary.TryGetValue(keyValue, out result))
                    return result;
            }

            string ns;
            if (TryGetValue(key, out ns))
                return ns;

            return defaultNamespace;
        }
    }
}
