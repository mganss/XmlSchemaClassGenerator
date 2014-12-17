using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XmlSchemaClassGenerator
{
    public class NamespaceKey : IComparable<NamespaceKey>, IEquatable<NamespaceKey>, IComparable
    {
        public string XmlSchemaNamespace { get; private set; }
        public string FileName { get; private set; }

        public NamespaceKey(string fileName, string xmlSchemaNamespace)
        {
            FileName = fileName;
            XmlSchemaNamespace = xmlSchemaNamespace;
        }

        public bool Equals(NamespaceKey other)
        {
            return CompareTo(other) == 0;
        }

        public int CompareTo(NamespaceKey other)
        {
            var result = String.Compare(FileName, other.FileName, StringComparison.OrdinalIgnoreCase);
            if (result != 0)
                return result;
            result = string.Compare(XmlSchemaNamespace, other.XmlSchemaNamespace, StringComparison.Ordinal);
            return result;
        }

        public override bool Equals(object obj)
        {
            return Equals((NamespaceKey)obj);
        }

        public override int GetHashCode()
        {
            return FileName.ToLower().GetHashCode()
                   ^ XmlSchemaNamespace.GetHashCode();
        }

        int IComparable.CompareTo(object obj)
        {
            return CompareTo((NamespaceKey)obj);
        }
    }
}
