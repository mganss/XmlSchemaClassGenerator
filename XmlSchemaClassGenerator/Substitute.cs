using System;
using System.Collections.Generic;
using System.Text;
using System.Xml.Schema;

namespace XmlSchemaClassGenerator
{
    public sealed class Substitute: IEquatable<Substitute>
    {
        public XmlSchemaElement Element { get; set; }
        public TypeModel Type { get; set; }

        public bool Equals(Substitute other)
        {
            return Element.QualifiedName.Equals(other.Element.QualifiedName)
                && ($"{Type.Namespace}.{Type.Name}").Equals($"{other.Type.Namespace}.{other.Type.Name}");
        }
    }
}
