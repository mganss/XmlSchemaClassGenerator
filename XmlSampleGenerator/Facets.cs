namespace Microsoft.Xml.XMLGen {
       using System;
       using System.Collections;
       using System.IO;
       using System.Text;
       using System.Xml.Schema;
       using System.Reflection;

        internal enum XmlSchemaWhiteSpace {
            Preserve,
            Replace,
            Collapse,
        }

        internal enum RestrictionFlags {
            Length              = 0x0001,
            MinLength           = 0x0002,
            MaxLength           = 0x0004,
            Pattern             = 0x0008,
            Enumeration         = 0x0010,
            WhiteSpace          = 0x0020,
            MaxInclusive        = 0x0040,
            MaxExclusive        = 0x0080,
            MinInclusive        = 0x0100,
            MinExclusive        = 0x0200,
            TotalDigits         = 0x0400,
            FractionDigits      = 0x0800,
        }
    
        internal class CompiledFacets {

            static FieldInfo lengthInfo;
            static FieldInfo minLengthInfo;
            static FieldInfo maxLengthInfo;
            static FieldInfo patternsInfo;
            static FieldInfo enumerationInfo;
            static FieldInfo whitespaceInfo;

            static FieldInfo maxInclusiveInfo;
            static FieldInfo maxExclusiveInfo;
            static FieldInfo minInclusiveInfo;
            static FieldInfo minExclusiveInfo;
            static FieldInfo totalDigitsInfo;
            static FieldInfo fractionDigitsInfo;

            static FieldInfo restrictionFlagsInfo;
            static FieldInfo restrictionFixedFlagsInfo;
            
            public static Type XsdSimpleValueType;
            public static Type XmlSchemaDatatypeType;

            object compiledRestriction;

            static CompiledFacets() {
                Assembly systemXmlAsm = typeof(XmlSchema).Assembly;

                Type RestrictionFacetsType = systemXmlAsm.GetType("System.Xml.Schema.RestrictionFacets", true);
                XsdSimpleValueType = systemXmlAsm.GetType("System.Xml.Schema.XsdSimpleValue", true);
                XmlSchemaDatatypeType = typeof(XmlSchemaDatatype);

                lengthInfo = RestrictionFacetsType.GetField("Length", BindingFlags.Instance | BindingFlags.NonPublic);
                minLengthInfo = RestrictionFacetsType.GetField("MinLength", BindingFlags.Instance | BindingFlags.NonPublic);
                maxLengthInfo = RestrictionFacetsType.GetField("MaxLength", BindingFlags.Instance | BindingFlags.NonPublic);
                patternsInfo = RestrictionFacetsType.GetField("Patterns", BindingFlags.Instance | BindingFlags.NonPublic);
                enumerationInfo = RestrictionFacetsType.GetField("Enumeration", BindingFlags.Instance | BindingFlags.NonPublic);
                whitespaceInfo = RestrictionFacetsType.GetField("WhiteSpace", BindingFlags.Instance | BindingFlags.NonPublic);

                maxInclusiveInfo = RestrictionFacetsType.GetField("MaxInclusive", BindingFlags.Instance | BindingFlags.NonPublic);
                maxExclusiveInfo = RestrictionFacetsType.GetField("MaxExclusive", BindingFlags.Instance | BindingFlags.NonPublic);
                minInclusiveInfo = RestrictionFacetsType.GetField("MinInclusive", BindingFlags.Instance | BindingFlags.NonPublic);
                minExclusiveInfo = RestrictionFacetsType.GetField("MinExclusive", BindingFlags.Instance | BindingFlags.NonPublic);

                totalDigitsInfo = RestrictionFacetsType.GetField("TotalDigits", BindingFlags.Instance | BindingFlags.NonPublic);
                fractionDigitsInfo = RestrictionFacetsType.GetField("FractionDigits", BindingFlags.Instance | BindingFlags.NonPublic);

                restrictionFlagsInfo = RestrictionFacetsType.GetField("Flags", BindingFlags.Instance | BindingFlags.NonPublic);
                restrictionFixedFlagsInfo = RestrictionFacetsType.GetField("FixedFlags", BindingFlags.Instance | BindingFlags.NonPublic);
            }

            public CompiledFacets(object restriction) {
                this.compiledRestriction = restriction;
            }
    
            public int Length {
                get {
                    if (compiledRestriction == null) {
                        return 0;
                    }
                    return (int)lengthInfo.GetValue(compiledRestriction);
                }
            }

            public int MinLength {
                get {
                    if (compiledRestriction == null) {
                        return 0;
                    }
                    return (int)minLengthInfo.GetValue(compiledRestriction);
                }
            }

            public int MaxLength {
                get {
                    if (compiledRestriction == null) {
                        return 0;
                    }
                    return (int)maxLengthInfo.GetValue(compiledRestriction);
                }
            }

            public ArrayList Patterns {
                get {
                    if (compiledRestriction == null) {
                        return null;
                    }
                    return (ArrayList)patternsInfo.GetValue(compiledRestriction);
                }
            }

            public ArrayList Enumeration {
                get {
                    if (compiledRestriction == null) {
                        return null;
                    }
                    return (ArrayList)enumerationInfo.GetValue(compiledRestriction);
                }
            }
            public XmlSchemaWhiteSpace WhiteSpace {
                get {
                    if (compiledRestriction == null) {
                        return XmlSchemaWhiteSpace.Preserve;
                    }
                    return (XmlSchemaWhiteSpace)whitespaceInfo.GetValue(compiledRestriction);
                }
            }   

            public object MaxInclusive {
                get {
                    if (compiledRestriction == null) {
                        return null;
                    }
                    return maxInclusiveInfo.GetValue(compiledRestriction);
                }
            }  
            public object MaxExclusive {
                get {
                    if (compiledRestriction == null) {
                        return null;
                    }
                    return maxExclusiveInfo.GetValue(compiledRestriction);
                }
            }
            public object MinInclusive {
                get {
                    if (compiledRestriction == null) {
                        return null;
                    }
                    return minInclusiveInfo.GetValue(compiledRestriction);
                }
            }
            public object MinExclusive {
                get {
                    if (compiledRestriction == null) {
                        return null;
                    }
                    return minExclusiveInfo.GetValue(compiledRestriction);
                }
            }

            public int TotalDigits {
                get {
                    if (compiledRestriction == null) {
                        return 0;
                    }
                    return (int)totalDigitsInfo.GetValue(compiledRestriction);
                }
            }
            public int FractionDigits {
                get {
                    if (compiledRestriction == null) {
                        return 0;
                    }
                    return (int)fractionDigitsInfo.GetValue(compiledRestriction);
                }
            }
            public RestrictionFlags Flags {
                get {
                    if (compiledRestriction == null) {
                        return 0;
                    }
                    return (RestrictionFlags)restrictionFlagsInfo.GetValue(compiledRestriction);
                }
            }
            public RestrictionFlags FixedFlags {
                get {
                    if (compiledRestriction == null) {
                        return 0;
                    }
                    return (RestrictionFlags)restrictionFixedFlagsInfo.GetValue(compiledRestriction);
                }
            }
        }
}
