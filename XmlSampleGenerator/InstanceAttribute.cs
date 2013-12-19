namespace Microsoft.Xml.XMLGen {
    using System.Xml;
    using System.Xml.Schema;

    internal class InstanceAttribute : InstanceObject{
        XmlSchemaUse attrUse;
        InstanceAttribute nextAttribute;

        internal InstanceAttribute(XmlQualifiedName name) {
            this.QualifiedName = new XmlQualifiedName(name.Name, name.Namespace);
        }
        
        internal XmlSchemaUse AttrUse {
            get {
                return attrUse;
            }
            set {
                attrUse = value;
            }
        }
        
        internal InstanceAttribute NextAttribute {
            get {
                return nextAttribute;
            }
            set {
                nextAttribute = value;
            }
        }

      }
}
