namespace Microsoft.Xml.XMLGen {
    using System.Xml;
    using System.Xml.Schema;
    using System.Collections;
    using System.Xml.Serialization;
    using System.Diagnostics;

    internal abstract class InstanceObject {

        XmlQualifiedName qualifiedName;
        XmlValueGenerator valueGenerator;

        XmlSchemaForm form;
        string defaultValue;
        string fixedValue;
        bool firstGen = true;

        internal XmlQualifiedName QualifiedName {
            get {
                return qualifiedName;
            }
            set {
                qualifiedName = value;
            }
        }

        internal XmlValueGenerator ValueGenerator {
            get {
                return valueGenerator;
            }

            set {
                valueGenerator = value;
            }
        }

        internal XmlSchemaForm Form {
            get {
                return form;
            }
            set {
                form = value;
            }
        }

        internal string DefaultValue {
            get {
                return defaultValue;
            }
            set {
                defaultValue = value;
            }
        }

        internal string FixedValue {
            get {
                return fixedValue;
            }
            set {
                fixedValue = value;
            }
        }

        internal bool IsFixed {
            get {
                if (fixedValue != null) {
                    return true;
                }
                return false;
            }
        }
        
        internal bool HasDefault {
            get {
                if (defaultValue != null && firstGen) {
                    firstGen = false;
                    return true;
                }
                return false;
            }
        }
    }
}

