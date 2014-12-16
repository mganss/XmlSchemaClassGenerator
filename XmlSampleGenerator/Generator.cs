namespace Microsoft.Xml.XMLGen {
       using System;
       using System.Collections;
       using System.Diagnostics;
       using System.IO;
       using System.Text;
       using System.Xml;
       using System.Xml.Schema;
       using System.Xml.XPath;
       using System.Globalization;
       using System.Reflection;
    
    internal enum AddSubtractState {
        StartPlusInc = 0,
        StartMinusInc = 1,
        MaxMinusInc = 2,
        MinPlusInc = 3,
    }

    internal abstract class XmlValueGenerator {
        internal static ArrayList ids = new ArrayList();
        internal static Generator_ID g_ID = new Generator_ID();
        internal static Generator_IDREF g_IDREF = new Generator_IDREF();
        internal static int IDCnt = 0;
        internal static bool IDRef = false;
        internal static Type TypeOfString = typeof(System.String);

        protected ArrayList AllowedValues = null;
        protected int occurNum = 0;
        
        string prefix = null;
        XmlSchemaDatatype datatype;

        internal static XmlValueGenerator AnyGenerator = new Generator_anyType();
        internal static XmlValueGenerator AnySimpleTypeGenerator = new Generator_anySimpleType();

        internal static AddSubtractState[] states = { AddSubtractState.StartPlusInc, AddSubtractState.MinPlusInc, AddSubtractState.MaxMinusInc, AddSubtractState.StartMinusInc};

        internal static ArrayList IDList {
            get {
                return ids;
            }
        }
               
        public virtual string Prefix {
            get {
               return prefix;
            }
            set {
              prefix = value;
            }
        }

        public virtual void AddGenerator(XmlValueGenerator genr) {
            return;
        }

        public abstract string GenerateValue();
        
        public XmlSchemaDatatype Datatype {
            get {
                return datatype;
            }
        }

        internal static XmlValueGenerator CreateGenerator(XmlSchemaDatatype datatype, int listLength) {
            XmlTypeCode typeCode = datatype.TypeCode;

            object restriction = datatype.GetType().InvokeMember("Restriction", BindingFlags.GetProperty | BindingFlags.NonPublic | BindingFlags.Instance, null, datatype, null);
            CompiledFacets rFacets = new CompiledFacets(restriction);
            XmlValueGenerator generator;

            if (datatype.Variety == XmlSchemaDatatypeVariety.Union) {
                generator = CreateUnionGenerator(datatype, rFacets, listLength);
            }
            else if (datatype.Variety == XmlSchemaDatatypeVariety.List) {
                generator = CreateListGenerator(datatype, rFacets, listLength);
            }
            else {
                switch (typeCode) {
                    case XmlTypeCode.None:
                        generator = AnyGenerator;
                        break;
                    case XmlTypeCode.Item:
                        generator = AnyGenerator;
                        break;
                    case XmlTypeCode.AnyAtomicType:
                        generator = AnySimpleTypeGenerator;
                        break;
                    case XmlTypeCode.String:
                        generator = new Generator_string(rFacets);
                        break;
                    case XmlTypeCode.Boolean:
                        generator = new Generator_boolean();
                        break;
                    case XmlTypeCode.Float:
                        generator = new Generator_float(rFacets);
                        break;
                    case XmlTypeCode.Double:
                        generator = new Generator_double(rFacets);
                        break;
                    case XmlTypeCode.AnyUri:
                        generator = new Generator_anyURI(rFacets);
                        break;
                    case XmlTypeCode.Integer:
                        generator = new Generator_integer(rFacets);
                        break;
                    case XmlTypeCode.Decimal:
                        generator = new Generator_decimal(rFacets);
                        break;
                    case XmlTypeCode.NonPositiveInteger:
                        generator = new Generator_nonPositiveInteger(rFacets);
                        break;
                    case XmlTypeCode.NegativeInteger:
                        generator = new Generator_negativeInteger(rFacets);
                        break;
                    case XmlTypeCode.Long:
                        generator = new Generator_long(rFacets);
                        break;
                    case XmlTypeCode.Int:
                        generator = new Generator_int(rFacets);
                        break;
                    case XmlTypeCode.Short:
                        generator = new Generator_short(rFacets);
                        break;
                    case XmlTypeCode.Byte:
                        generator = new Generator_byte(rFacets);
                        break;
                    case XmlTypeCode.NonNegativeInteger:
                        generator = new Generator_nonNegativeInteger(rFacets);
                        break;
                    case XmlTypeCode.UnsignedLong:
                        generator = new Generator_unsignedLong(rFacets);
                        break;
                    case XmlTypeCode.UnsignedInt:
                        generator = new Generator_unsignedInt(rFacets);
                        break;
                    case XmlTypeCode.UnsignedShort:
                        generator = new Generator_unsignedShort(rFacets);
                        break;
                    case XmlTypeCode.UnsignedByte:
                        generator = new Generator_unsignedByte(rFacets);
                        break;
                    case XmlTypeCode.PositiveInteger:
                        generator = new Generator_positiveInteger(rFacets);
                        break;
                    case XmlTypeCode.Duration:
                        generator = new Generator_duration(rFacets);
                        break;
                    case XmlTypeCode.DateTime:
                        generator = new Generator_dateTime(rFacets);
                        break;
                    case XmlTypeCode.Date:
                        generator = new Generator_date(rFacets);
                        break;
                    case XmlTypeCode.GYearMonth:
                        generator = new Generator_gYearMonth(rFacets);
                        break;
                    case XmlTypeCode.GYear:
                        generator = new Generator_gYear(rFacets);
                        break;
                    case XmlTypeCode.GMonthDay:
                        generator = new Generator_gMonthDay(rFacets);
                        break;
                    case XmlTypeCode.GDay:
                        generator = new Generator_gDay(rFacets);
                        break;
                    case XmlTypeCode.GMonth:
                        generator = new Generator_gMonth(rFacets);
                        break;
                    case XmlTypeCode.Time:
                        generator = new Generator_time(rFacets);
                        break;
                    case XmlTypeCode.HexBinary:
                        generator = new Generator_hexBinary(rFacets);
                        break;
                    case XmlTypeCode.Base64Binary:
                        generator = new Generator_base64Binary(rFacets);
                        break;
                    case XmlTypeCode.QName:
                        generator = new Generator_QName(rFacets);
                        break;
                    case XmlTypeCode.Notation:
                        generator = new Generator_Notation(rFacets);
                        break;
                    case XmlTypeCode.NormalizedString:
                        generator = new Generator_normalizedString(rFacets);
                        break;
                    case XmlTypeCode.Token:
                        generator = new Generator_token(rFacets);
                        break;
                    case XmlTypeCode.Language:
                        generator = new Generator_language(rFacets);
                        break;
                    case XmlTypeCode.NmToken:
                        generator = new Generator_NMTOKEN(rFacets);
                        break;
                    case XmlTypeCode.Name:
                        generator = new Generator_Name(rFacets);
                        break;
                    case XmlTypeCode.NCName:
                        generator = new Generator_NCName(rFacets);
                        break;
                    case XmlTypeCode.Id:
                        g_ID.CheckFacets(rFacets);
                        generator = g_ID;
                        break;
                    case XmlTypeCode.Idref:
                        g_IDREF.CheckFacets(rFacets);
                        generator = g_IDREF;
                        break;
                    default:
                        generator = AnyGenerator;
                        break;
                }
            }
            generator.SetDatatype(datatype);
            return generator;
        }
        
        private static XmlValueGenerator CreateUnionGenerator(XmlSchemaDatatype dtype, CompiledFacets facets, int listLength) {
            XmlSchemaSimpleType[] memberTypes = (XmlSchemaSimpleType[])dtype.GetType().InvokeMember("types", BindingFlags.GetField | BindingFlags.NonPublic | BindingFlags.Instance, null, dtype, null);
            Generator_Union union_genr = new Generator_Union(facets);
            foreach(XmlSchemaSimpleType st1 in memberTypes) {
                union_genr.AddGenerator(XmlValueGenerator.CreateGenerator(st1.Datatype, listLength));
            }
            return union_genr;
        }
        
        private static XmlValueGenerator CreateListGenerator(XmlSchemaDatatype dtype, CompiledFacets facets, int listLength) {
            XmlSchemaDatatype itemType = (XmlSchemaDatatype)dtype.GetType().InvokeMember("itemType", BindingFlags.GetField | BindingFlags.NonPublic | BindingFlags.Instance, null, dtype, null);
            Generator_List list_genr = new Generator_List(facets);
            list_genr.ListLength = listLength;
            list_genr.AddGenerator(XmlValueGenerator.CreateGenerator(itemType, listLength));
            return list_genr;
        }

        private void SetDatatype(XmlSchemaDatatype datatype) {
            this.datatype = datatype;
        }
    }


        internal class Generator_anyType : XmlValueGenerator {
            string value = "anyType";
            public override string GenerateValue() {
                return value;
            }
        }

        internal class Generator_anySimpleType : XmlValueGenerator {
            string value = "anySimpleType";
            public override string GenerateValue() {
                return value;
            }
        }
        
        internal class Generator_facetBase : XmlValueGenerator {

            protected int minLength = -1;
            protected int maxLength = -1;
            protected int length = -1;

            internal void CheckFacets(CompiledFacets genFacets) {
                  if(genFacets != null) {
                    RestrictionFlags flags = genFacets.Flags;
                    if ((flags & RestrictionFlags.Length) != 0) {
                        length = genFacets.Length;    
                    }
                    if ((flags & RestrictionFlags.MinLength) != 0) {
                        minLength = genFacets.MinLength;
                    }
                    if ((flags & RestrictionFlags.MaxLength) != 0) {
                        maxLength = genFacets.MaxLength;
                    }
                    if ((flags & RestrictionFlags.Enumeration) != 0) {
                        AllowedValues = genFacets.Enumeration;
                    }
                  }
            }
            
            public override string GenerateValue() {
                return string.Empty;
            }

            public object GetEnumerationValue() {
                Debug.Assert(AllowedValues != null);
                try {
                    return AllowedValues[occurNum++ % AllowedValues.Count];
                }
                catch (OverflowException) {
                    occurNum = 0;
                    return AllowedValues[occurNum++];
                }
            }
            public void ProcessLengthFacet(ref StringBuilder genString, int index) {
                int pLength = genString.Length;
                int indexLen = index.ToString().Length;
                int correctLen = length - indexLen;
                if(pLength > correctLen) {
                    genString.Remove(correctLen,pLength-correctLen);
                }
                else if(pLength < correctLen) {
                    int addCount = correctLen - pLength;
                    for(int i=0; i < addCount; i++) {
                        genString.Append("_");
                    }
                }
            }

            public void ProcessMinLengthFacet(ref StringBuilder genString, int index) {
                int pLength = genString.Length;
                int indexLen = index.ToString().Length;
                int correctLen = minLength - indexLen;
                if(pLength < correctLen) {
                    int addCount = correctLen - pLength;
                    for(int i=0; i < addCount; i++) {
                        genString.Append("_");
                    }
                }
            }

            public void ProcessMaxLengthFacet(ref StringBuilder genString, int index) {
                int pLength = genString.Length;
                int indexLen = index.ToString().Length;
                int correctLen = maxLength - indexLen;
                if(pLength > correctLen) {
                    genString.Remove(correctLen,pLength-correctLen);
                }
            }

        } //End of class stringBase

        internal class Generator_string : Generator_facetBase {
            int step = 1;
            int endValue = 0;
            StringBuilder genString;

            public Generator_string() {
            }

            public Generator_string(CompiledFacets rFacets) {
                if(rFacets != null) {
                    CheckFacets(rFacets);
                }
            }

            
            public override string GenerateValue() {
                if(AllowedValues != null) {
                    object enumValue = GetEnumerationValue();
                    return enumValue.ToString();
                }
                else {
                    if (genString == null) {
                        genString = new StringBuilder(Prefix);
                    }
                    else {
                        genString.Append(Prefix);
                    }
                    try {
                        endValue = endValue + step;
                    }
                    catch (OverflowException) { //reset
                        endValue = 1;
                    }
                    if(length == -1 && minLength == -1 && maxLength == -1) {
                        genString.Append(endValue);
                    }
                    else {
                        if(length != -1) { // The length facet is set
                            ProcessLengthFacet(ref genString, endValue);
                            genString.Append(endValue);
                        }
                        else { 
                            if(minLength != -1) {
                                ProcessMinLengthFacet(ref genString, endValue);
                            }
                            if(maxLength != -1) {
                                ProcessMaxLengthFacet(ref genString, endValue);
                            }
                            genString.Append(endValue);
                        }
                    }
                    string result = genString.ToString();
                    genString.Length = 0; //Clear the stringBuilder
                    return result;
                }
            } // End of genValue
        }
        
        internal class Generator_decimal : XmlValueGenerator {
            protected decimal increment = 0;
            protected decimal startValue = 1;
            protected decimal step = 0.1m;
            protected decimal maxBound = decimal.MaxValue;
            protected decimal minBound = decimal.MinValue;
           
            int stateStep = 1;

            public Generator_decimal(CompiledFacets rFacets) {
                CheckFacets(rFacets);
            }
            
            public Generator_decimal() {
            }

            public decimal StartValue {
                set {
                    startValue = value;
                }
            }
            
            public void CheckFacets(CompiledFacets genFacets) {
              if(genFacets != null) {
                RestrictionFlags flags = genFacets.Flags;
                if ((flags & RestrictionFlags.MaxInclusive) != 0) {
                    maxBound = (decimal)Convert.ChangeType(genFacets.MaxInclusive, typeof(decimal));
                }
                if ((flags & RestrictionFlags.MaxExclusive) != 0) {
                    maxBound = (decimal)Convert.ChangeType(genFacets.MaxExclusive, typeof(decimal)) - 1;
                }
                if ((flags & RestrictionFlags.MinInclusive) != 0) {
                    startValue = (decimal)Convert.ChangeType(genFacets.MinInclusive, typeof(decimal));
                    minBound = startValue;
                }
                if ((flags & RestrictionFlags.MinExclusive) != 0) {
                    startValue = (decimal)Convert.ChangeType(genFacets.MinExclusive, typeof(decimal)) + 1;
                    minBound = startValue;
                }
                if ((flags & RestrictionFlags.Enumeration) != 0) {
                    AllowedValues = genFacets.Enumeration;
                }
                if ((flags & RestrictionFlags.TotalDigits) != 0) {
                    decimal totalDigitsValue = (decimal)Math.Pow(10,genFacets.TotalDigits) - 1;
                    if (totalDigitsValue <= maxBound) { //Use the lower of totalDigits value and maxInc/maxEx
                        maxBound = Math.Min(maxBound, totalDigitsValue);
                        minBound = Math.Max(minBound, Math.Min(maxBound,  0 - maxBound));
                    }
                    
                }
                if ((flags & RestrictionFlags.FractionDigits) != 0 && genFacets.FractionDigits > 0) {
                    if ((flags & RestrictionFlags.TotalDigits) != 0) {
                        //if (T,F) is (6,3) the max value is not 999.999 but 99999.9d but we are going with the smaller range on the integral part to generate more varied fractional part.
                        int range = genFacets.TotalDigits - genFacets.FractionDigits;
                        double integralPart = Math.Pow(10,range) - 1;
                        double fractionPart = 1.0 - 1.0/Math.Pow(10,genFacets.FractionDigits);
                        maxBound = Math.Min(maxBound, (decimal)(integralPart + fractionPart));
                        minBound = Math.Max(minBound, Math.Min(maxBound, 0 - maxBound));
                        step = (decimal)(1/Math.Pow(10,genFacets.FractionDigits));
                    }
                    //If there is no TotalDigits facet, we use the step for decimal as 0.1 anyway which will satisfy fractionDigits >= 1
                }
                if(maxBound <= 0) {
                    startValue = maxBound;
                    occurNum++;
                    stateStep = 2;
                }
                if (startValue == minBound) {
                    stateStep = 2;
                }
              }
            }
            
           public override string GenerateValue() { 
                decimal result = 0;
                if(AllowedValues != null) {
                    try {
                        result = (decimal)AllowedValues[occurNum++ % AllowedValues.Count];
                    }
                    catch(OverflowException) {
                        occurNum = 0;
                        result = (decimal)AllowedValues[occurNum++];
                    }
                }
                else {
                    try {
                        AddSubtractState state = states[occurNum % states.Length];
                        switch (state) {
                            case AddSubtractState.StartPlusInc:
                                result = startValue + increment;
                                break;

                            case AddSubtractState.MinPlusInc:
                                result = minBound + increment;
                                break;

                            case AddSubtractState.MaxMinusInc:
                                result = maxBound - increment;
                                if (stateStep == 2) {
                                    increment = increment + step;
                                }
                                break;

                            case AddSubtractState.StartMinusInc:
                                increment = increment + step; //stateStep is 1 or 2, we need to increment now
                                result = startValue - increment;
                                break;
                            
                            default:
                                Debug.Assert(false);
                                break;
                        }
                        occurNum = occurNum + stateStep; 
                    }
                    catch (OverflowException) { //reset
                        result = ResetState();
                    }
                    if (result >= maxBound) {
                        result = maxBound;
                    }
                    else if (result <= minBound) {
                        result = minBound;
                    }
                }
                return result.ToString(null, NumberFormatInfo.InvariantInfo);
            } 

           private decimal ResetState() {
                increment = 0;
                if (startValue == maxBound) {
                    occurNum = 1;
                    stateStep = 2;
                }
                if (startValue == minBound) {
                    occurNum = 0;
                    stateStep = 2;
                }
                return startValue;
           }                
        }


         internal class Generator_integer : Generator_decimal {

            public Generator_integer(CompiledFacets rFacets) : base(rFacets) {
                maxBound = int.MaxValue;
                minBound = int.MinValue;
                step = 1;
                CheckFacets(rFacets);
            }

            public Generator_integer() {
                maxBound = int.MaxValue;
                minBound = int.MinValue;
                step = 1;
            }
         }
        
        internal class Generator_nonPositiveInteger : Generator_integer {
            
            public Generator_nonPositiveInteger() {
            }

            public Generator_nonPositiveInteger(CompiledFacets rFacets) {
                startValue = 0;
                increment = 1;
                maxBound = 0;
                CheckFacets(rFacets);
            }
        }
        
        internal class Generator_negativeInteger : Generator_nonPositiveInteger {
            public Generator_negativeInteger(CompiledFacets rFacets) {
                startValue = -1;
                maxBound = -1;
                CheckFacets(rFacets);
            }
        }
        
        internal class Generator_long : Generator_integer {
            
            public Generator_long() {
                maxBound = 9223372036854775807;
                minBound = -9223372036854775807;
            }

            public Generator_long(CompiledFacets rFacets) {
                maxBound = 9223372036854775807;
                minBound = -9223372036854775807;
                CheckFacets(rFacets);
            }
        }
        
        internal class Generator_int : Generator_long {
            
            public Generator_int() {
                maxBound = 2147483647;
                minBound = -2147483647;
            }

            public Generator_int(CompiledFacets rFacets) {
                maxBound = 2147483647;
                minBound = -2147483647;
                CheckFacets(rFacets);
            }
        }

        internal class Generator_short : Generator_int {
            
            public Generator_short() {
                maxBound = 32767;
                minBound = -32768;
            }

            public Generator_short(CompiledFacets rFacets) {
                maxBound = 32767;
                minBound = -32768;
                CheckFacets(rFacets);
            }
            
        }
        
        internal class Generator_byte : Generator_short {
            
            public Generator_byte(CompiledFacets rFacets) {
                maxBound = 127;
                minBound = -128;
                CheckFacets(rFacets);
            }
        }

        internal class Generator_nonNegativeInteger : Generator_integer {
            
            public Generator_nonNegativeInteger() {
                startValue = 0;
                minBound = 0;
            }

            public Generator_nonNegativeInteger(CompiledFacets rFacets) {
                startValue = 0;
                minBound = 0;
                CheckFacets(rFacets);
            }
        }
        
        internal class Generator_unsignedLong : Generator_nonNegativeInteger {
            
            public Generator_unsignedLong() {
            }

            public Generator_unsignedLong(CompiledFacets rFacets) {
                maxBound = 18446744073709551615;
                CheckFacets(rFacets);
            }
        }
        
        internal class Generator_unsignedInt : Generator_unsignedLong {
            
            public Generator_unsignedInt() {
            }

            public Generator_unsignedInt(CompiledFacets rFacets) {
                maxBound = 4294967295;
                CheckFacets(rFacets);
            }
        }
        
        internal class Generator_unsignedShort : Generator_unsignedInt {
            
            public Generator_unsignedShort() {
            }

            public Generator_unsignedShort(CompiledFacets rFacets) {
                maxBound = 65535;
                CheckFacets(rFacets);
            }
        }
        
        internal class Generator_unsignedByte : Generator_unsignedShort {
            
            public Generator_unsignedByte() {
            }

            public Generator_unsignedByte(CompiledFacets rFacets) {
                maxBound = 255;
                CheckFacets(rFacets);
            }
        }

        internal class Generator_positiveInteger : Generator_nonNegativeInteger {
            
            public Generator_positiveInteger(CompiledFacets rFacets) {
                startValue = 1;
                minBound = 1;
                CheckFacets(rFacets);
            }
        }
        

        internal class Generator_boolean : XmlValueGenerator {
            public override string GenerateValue() { 
                if ((occurNum & 0x1) == 0) {
                    occurNum = 1;
                    return "true";
                }
                else {
                    occurNum = 0;
                    return "false";
                }
            }
        }
        
        internal class Generator_double : XmlValueGenerator {
            double increment = 0F;
            protected double startValue = 1;
            protected double step = 1.1F;
            protected double maxBound = double.MaxValue;
            protected double minBound = double.MinValue;

            int stateStep = 1;

            public Generator_double() {
            }

            public Generator_double(CompiledFacets rFacets) {
                 CheckFacets(rFacets);
            }

            public void CheckFacets(CompiledFacets genFacets) {
                if(genFacets != null) {
                    RestrictionFlags flags = genFacets.Flags;
                    if ((flags & RestrictionFlags.MaxInclusive) != 0) {
                        maxBound = (double)genFacets.MaxInclusive;
                    }
                    if ((flags & RestrictionFlags.MaxExclusive) != 0) {
                        maxBound = (double)genFacets.MaxExclusive - 1;
                    }
                    if ((flags & RestrictionFlags.MinInclusive) != 0) {
                        startValue = (double)genFacets.MinInclusive;
                        minBound = startValue;
                    }
                    if ((flags & RestrictionFlags.MinExclusive) != 0) {
                        startValue = (double)genFacets.MinExclusive + 1;
                        minBound = startValue;
                    }
                    if ((flags & RestrictionFlags.Enumeration) != 0) {
                        AllowedValues = genFacets.Enumeration;
                    }
                    if(maxBound <= 0) {
                        startValue = maxBound;
                        occurNum++;
                        stateStep = 2;
                    }
                    if (startValue == minBound) {
                        stateStep = 2;
                    }
                }
            }

            public override string GenerateValue() { 
                double result = 0;
                if(AllowedValues != null) {
                    try {
                        result = (double)AllowedValues[occurNum++ % AllowedValues.Count];
                    }
                    catch(OverflowException) {
                        occurNum = 0;
                        result = (double)AllowedValues[occurNum++];
                    }
                }
                else {
                    try {
                        AddSubtractState state = states[occurNum % states.Length];
                        switch (state) {
                            case AddSubtractState.StartPlusInc:
                                result = startValue + increment;
                                break;

                            case AddSubtractState.MinPlusInc:
                                result = minBound + increment;
                                break;

                            case AddSubtractState.MaxMinusInc:
                                result = maxBound - increment;
                                if (stateStep == 2) {
                                    increment = increment + step;
                                }
                                break;

                            case AddSubtractState.StartMinusInc:
                                increment = increment + step; //stateStep is 1 or 2, we need to increment now
                                result = startValue - increment;
                                break;
                            
                            default:
                                Debug.Assert(false);
                                break;
                        }
                        occurNum = occurNum + stateStep; 
                    }
                    catch (OverflowException) { //reset
                        result = ResetState();
                    }
                    if (result > maxBound) {
                        result = maxBound;
                    }
                    else if (result < minBound) {
                        result = minBound;
                    }
                }
                return XmlConvert.ToString(result);
            } 

           private double ResetState() {
                increment = 0F;
                if (startValue == maxBound) {
                    occurNum = 1;
                    stateStep = 2;
                }
                if (startValue == minBound) {
                    occurNum = 0;
                    stateStep = 2;
                }
                return startValue;
           }
        }
        
        internal class Generator_float : XmlValueGenerator {
            float increment = 0F;
            float startValue = 1;
            float step = 1.1F;
            float maxBound = float.MaxValue;
            float minBound = float.MinValue;

            int stateStep = 1;

            public Generator_float(CompiledFacets rFacets) {
                CheckFacets(rFacets);
            }
            public void CheckFacets(CompiledFacets genFacets) {
                if(genFacets != null) {
                    RestrictionFlags flags = genFacets.Flags;
                    if ((flags & RestrictionFlags.MaxInclusive) != 0) {
                        maxBound = (float)genFacets.MaxInclusive;
                    }
                    if ((flags & RestrictionFlags.MaxExclusive) != 0) {
                        maxBound = (float)genFacets.MaxExclusive - 1;
                    }
                    if ((flags & RestrictionFlags.MinInclusive) != 0) {
                        startValue = (float)genFacets.MinInclusive;
                        minBound = startValue;
                    }
                    if ((flags & RestrictionFlags.MinExclusive) != 0) {
                        startValue = (float)genFacets.MinExclusive + 1;
                        minBound = startValue;
                    }
                    if ((flags & RestrictionFlags.Enumeration) != 0) {
                        AllowedValues = genFacets.Enumeration;
                    }
                    if(maxBound <= 0) {
                        startValue = maxBound;
                        occurNum++;
                        stateStep = 2;
                    }
                    if (startValue == minBound) {
                        stateStep = 2;
                    }
                }
            }
            
            public override string GenerateValue() {
                float result = 0; 
                if(AllowedValues != null) {
                    try {
                        result = (float)AllowedValues[occurNum++ % AllowedValues.Count];
                    }
                    catch(OverflowException) {
                        occurNum = 0;
                        result = (float)AllowedValues[occurNum++];
                    }
                }
                else {
                    try {
                        AddSubtractState state = states[occurNum % states.Length];
                        switch (state) {
                            case AddSubtractState.StartPlusInc:
                                result = startValue + increment;
                                break;

                            case AddSubtractState.MinPlusInc:
                                result = minBound + increment;
                                break;

                            case AddSubtractState.MaxMinusInc:
                                result = maxBound - increment;
                                if (stateStep == 2) {
                                    increment = increment + step;
                                }
                                break;

                            case AddSubtractState.StartMinusInc:
                                increment = increment + step; //stateStep is 1 or 2, we need to increment now
                                result = startValue - increment;
                                break;
                            
                            default:
                                Debug.Assert(false);
                                break;
                        }
                        occurNum = occurNum + stateStep; 
                    }
                    catch (OverflowException) { //reset
                        result = ResetState();
                    }
                    if (result > maxBound) {
                        result = maxBound;
                    }
                    else if (result < minBound) {
                        result = minBound;
                    }
                }
                return XmlConvert.ToString(result);
            } 

           private float ResetState() {
                increment = 0F;
                if (startValue == maxBound) {
                    occurNum = 1;
                    stateStep = 2;
                }
                if (startValue == minBound) {
                    occurNum = 0;
                    stateStep = 2;
                }
                return startValue;
           }
        }
        
        internal class Generator_duration : XmlValueGenerator {
            TimeSpan startValue = XmlConvert.ToTimeSpan("P1Y1M1DT1H1M1S");
            TimeSpan step       = XmlConvert.ToTimeSpan("P1Y");
            TimeSpan increment  = new TimeSpan(0,0,0,0);
            TimeSpan endValue   = new TimeSpan(1,0,0,0);
            TimeSpan minBound   = new TimeSpan(TimeSpan.MinValue.Days,TimeSpan.MinValue.Hours,TimeSpan.MinValue.Minutes,TimeSpan.MinValue.Seconds,TimeSpan.MinValue.Milliseconds);
            TimeSpan maxBound   = new TimeSpan(TimeSpan.MaxValue.Days,TimeSpan.MaxValue.Hours,TimeSpan.MaxValue.Minutes,TimeSpan.MaxValue.Seconds,TimeSpan.MaxValue.Milliseconds);

            int stateStep = 1;
            public Generator_duration(CompiledFacets rFacets) {
                CheckFacets(rFacets);
            }
            
            public void CheckFacets(CompiledFacets genFacets) {
                 if(genFacets != null) {
                    RestrictionFlags flags = genFacets.Flags;
                    if ((flags & RestrictionFlags.MaxInclusive) != 0) {
                        maxBound = (TimeSpan)genFacets.MaxInclusive;
                    }
                    if ((flags & RestrictionFlags.MaxExclusive) != 0) {
                        maxBound = (TimeSpan)genFacets.MaxExclusive - endValue;
                    }
                    if ((flags & RestrictionFlags.MinInclusive) != 0) {
                        startValue = (TimeSpan)genFacets.MinInclusive;
                        minBound = startValue;
                    }
                    if ((flags & RestrictionFlags.MinExclusive) != 0) {
                        startValue = (TimeSpan)genFacets.MinExclusive + endValue;
                        minBound = startValue;
                    }
                    if ((flags & RestrictionFlags.Enumeration) != 0) {
                        AllowedValues = genFacets.Enumeration;
                    }
                    if(TimeSpan.Compare(maxBound, TimeSpan.Zero) == -1) {
                        startValue = maxBound;
                        occurNum++;
                        stateStep = 2;
                    }
                    if (TimeSpan.Compare(minBound, startValue) == 0) {
                        stateStep = 2;
                    }
                }
            }

            public override string GenerateValue() {
                TimeSpan result = TimeSpan.Zero; 
                if(AllowedValues != null) {
                    try {
                        result = (TimeSpan)AllowedValues[occurNum++ % AllowedValues.Count];
                    }
                    catch (OverflowException) {
                        occurNum = 0;
                        result = (TimeSpan)AllowedValues[occurNum++];
                    }
                }
                else {
                    try {
                        AddSubtractState state = states[occurNum % states.Length];
                        switch (state) {
                            case AddSubtractState.StartPlusInc:
                                result = startValue + increment;
                                break;

                            case AddSubtractState.MinPlusInc:
                                result = minBound + increment;
                                break;

                            case AddSubtractState.MaxMinusInc:
                                result = maxBound - increment;
                                if (stateStep == 2) {
                                    increment = increment + step;
                                }
                                break;

                            case AddSubtractState.StartMinusInc:
                                increment = increment + step; //stateStep is 1 or 2, we need to increment now
                                result = startValue - increment;
                                break;
                            
                            default:
                                Debug.Assert(false);
                                break;
                        }
                        occurNum = occurNum + stateStep; 
                    }
                    catch (OverflowException) { //Reset
                        result = ResetState();
                    }
                    if (result > maxBound) {
                        result = maxBound;
                    }
                    else if (result < minBound) {
                        result = minBound;
                    }
                }
                return XmlConvert.ToString(result);
            } 

           private TimeSpan ResetState() {
                increment = TimeSpan.Zero;
                occurNum = 0;
                if(TimeSpan.Compare(maxBound, startValue) == 0) {
                    occurNum++;
                    stateStep = 2;
                }
                if (TimeSpan.Compare(minBound, startValue) == 0) {
                    stateStep = 2;
                }
                return startValue;
           }
      } // End of class Generator_duration


      internal class Generator_dateTime : XmlValueGenerator {
            TimeSpan  increment;
            protected DateTime startValue = new DateTime(1900,1,1,1,1,1);
            protected TimeSpan step       = XmlConvert.ToTimeSpan("P32D");
            protected DateTime minBound   = DateTime.MinValue;
            protected DateTime maxBound   = DateTime.MaxValue;
            
            int stateStep = 1;
            public Generator_dateTime() {
            }

            public Generator_dateTime (CompiledFacets rFacets) {
                CheckFacets(rFacets);
            }
            
            public void CheckFacets(CompiledFacets genFacets) {
                 if(genFacets != null) {
                    RestrictionFlags flags = genFacets.Flags;
                    if ((flags & RestrictionFlags.MaxInclusive) != 0) {
                        maxBound = (DateTime)genFacets.MaxInclusive;
                    }
                    if ((flags & RestrictionFlags.MaxExclusive) != 0) {
                        maxBound = ((DateTime)genFacets.MaxExclusive).Subtract(step);
                    }
                    if ((flags & RestrictionFlags.MinInclusive) != 0) {
                        startValue = (DateTime)genFacets.MinInclusive;
                        minBound = startValue;
                    }
                    if ((flags & RestrictionFlags.MinExclusive) != 0) {
                        startValue = ((DateTime)genFacets.MinExclusive).Add(step);
                        minBound = startValue;
                    }
                    if ((flags & RestrictionFlags.Enumeration) != 0) {
                        AllowedValues = genFacets.Enumeration;
                    }
                    if (DateTime.Compare(startValue, maxBound) == 0) {
                        occurNum++;
                        stateStep = 2;
                    }
                    if (DateTime.Compare(startValue, minBound) == 0) {
                        stateStep = 2;
                    }
                }
            }
            public override string GenerateValue() { 
                DateTime result = GenerateDate();
                return XmlConvert.ToString(result, XmlDateTimeSerializationMode.RoundtripKind);
           } 

           protected DateTime GenerateDate() {
                if (AllowedValues != null) {
                    try {
                        return (DateTime)AllowedValues[occurNum++ % AllowedValues.Count];
                    }
                    catch(OverflowException) {
                        occurNum = 0;
                        return (DateTime)AllowedValues[occurNum++];
                    }
                }
                else {
                    DateTime result = DateTime.UtcNow;
                    try {
                        AddSubtractState state = states[occurNum % states.Length];
                        switch (state) {
                            case AddSubtractState.StartPlusInc:
                                result = startValue.Add(increment);
                                break;

                            case AddSubtractState.MinPlusInc:
                                result = minBound.Add(increment);
                                break;

                            case AddSubtractState.MaxMinusInc:
                                result = maxBound.Subtract(increment);
                                if (stateStep == 2) {
                                    increment = increment + step;
                                }
                                break;

                            case AddSubtractState.StartMinusInc:
                                increment = increment + step; //stateStep is 1 or 2, we need to increment now
                                result = startValue.Subtract(increment);
                                break;
                            
                            default:
                                Debug.Assert(false);
                                break;                            
                        }
                        occurNum = occurNum + stateStep;
                    }
                    catch (ArgumentOutOfRangeException) { //reset
                        result = ResetState();
                    }
                    if (result >= maxBound) {
                        result = maxBound;
                    }
                    else if (result <= minBound) {
                        result = minBound;
                    }
                    return result; 
                }
           }

            private DateTime ResetState() {
                increment = TimeSpan.Zero;
                occurNum = 0;
                if (DateTime.Compare(startValue, maxBound) == 0) {
                    occurNum++;
                    stateStep = 2;
                }
                if (DateTime.Compare(startValue, minBound) == 0) {
                    stateStep = 2;
                }
                return startValue;
            }
      }// End of class dateTime
    
      internal class Generator_date : Generator_dateTime {
            public Generator_date(CompiledFacets rFacets) : base (rFacets){
            }

            public override string GenerateValue() {
                DateTime result = GenerateDate();
                return XmlConvert.ToString(result.Date, "yyyy-MM-dd");
            }
      }
      
      internal class Generator_gYearMonth : Generator_dateTime {
            public Generator_gYearMonth(CompiledFacets rFacets) {
                step = XmlConvert.ToTimeSpan("P32D");
                CheckFacets(rFacets);
            }

            public override string GenerateValue() {
                DateTime result = GenerateDate();
                return XmlConvert.ToString(result.Date, "yyyy-MM");
            }
      }
    
      internal class Generator_gYear : Generator_dateTime {
            public Generator_gYear(CompiledFacets rFacets) {
                step = XmlConvert.ToTimeSpan("P380D");
                CheckFacets(rFacets);
            }

            public override string GenerateValue() {
                DateTime result = GenerateDate();
                return XmlConvert.ToString(result.Date, "yyyy");
            }
      }

      internal class Generator_gMonthDay : Generator_dateTime {
            public Generator_gMonthDay(CompiledFacets rFacets) {
                step = XmlConvert.ToTimeSpan("P1M5D");
                CheckFacets(rFacets);
            }

            public override string GenerateValue() {
                DateTime result = GenerateDate();
                return XmlConvert.ToString(result.Date, "--MM-dd");
            }
      }

      internal class Generator_gDay : Generator_dateTime {
            public Generator_gDay(CompiledFacets rFacets) : base (rFacets){
            }

            public override string GenerateValue() {
                DateTime result = GenerateDate();
                return XmlConvert.ToString(result.Date, "---dd");
            }
      }
       
      internal class Generator_gMonth : Generator_dateTime {
            public Generator_gMonth(CompiledFacets rFacets) {
                step = XmlConvert.ToTimeSpan("P32D");
                CheckFacets(rFacets);
            }

            public override string GenerateValue() {
                DateTime result = GenerateDate();
                return XmlConvert.ToString(GenerateDate().Date, "--MM--");
            }
      }
      
      internal class Generator_time : Generator_dateTime {
            public Generator_time(CompiledFacets rFacets) {
                step = XmlConvert.ToTimeSpan("PT1M30S");
                CheckFacets(rFacets);
            }

            public override string GenerateValue() {
                return XmlConvert.ToString(GenerateDate(), "HH:mm:ss");
            }
      }

      

      internal class Generator_hexBinary : Generator_facetBase {
          Generator_integer binGen = new Generator_int();
           
          public Generator_hexBinary(CompiledFacets rFacets) {
             binGen.StartValue = 4023;
             base.CheckFacets(rFacets);
          }

          public override string GenerateValue() {
                if(AllowedValues != null) {
                    object enumValue = GetEnumerationValue();
                    return (string)this.Datatype.ChangeType(enumValue, TypeOfString);
                }
                else {
                    int binNo = (int)Convert.ChangeType(binGen.GenerateValue(), typeof(int));
                    StringBuilder str = new StringBuilder(binNo.ToString("X4"));
                    if(length == -1 && minLength == -1 && maxLength == -1) {
                        return str.ToString();
                    }
                    else if (length != -1){
                        ProcessLengthFacet(ref str);
                    }
                    else {
                        ProcessMinMaxLengthFacet(ref str);
                    }
                    return str.ToString();
                }
          } // End of GenValue     
       
          private void ProcessLengthFacet(ref StringBuilder str) {
                int pLength = str.Length;
                if (pLength % 2 != 0) {
                    throw new Exception("Total length of binary data should be even");
                }
                int correctLen = pLength / 2;

                if(correctLen > length) { //Need to remove (correctLen - length) * 2 chars 
                    str.Remove(length,(correctLen - length) * 2 );
                }
                else if(correctLen < length) { //Need to add (length - correctLen) * 2 chars
                    int addCount = length - correctLen;
                    for(int i=0; i < addCount; i++) {
                        str.Append("0A");
                    }
                }
          }

          private void ProcessMinMaxLengthFacet(ref StringBuilder str) {
                int pLength = str.Length;
                if (pLength % 2 != 0) {
                    throw new Exception("Total length of binary data should be even");
                }
                int correctLen = pLength / 2;
                if(minLength != -1) {
                    if(correctLen < minLength) {
                        int addCount = minLength - correctLen;
                        for(int i=0; i < addCount; i++) {
                            str.Append("0A");
                        }
                    }
                }
                else { //if maxLength != -1
                    if(correctLen > maxLength) { //Need to remove (correctLen - maxlength) * 2 chars 
                        str.Remove(maxLength,(correctLen - maxLength) * 2 );
                    }
                }
          }
      }

      internal class Generator_base64Binary : Generator_facetBase {
            
          public Generator_base64Binary(CompiledFacets rFacets) {
                CheckFacets(rFacets);
          }

          public override string GenerateValue() {
                if(AllowedValues != null) {
                    object enumValue = GetEnumerationValue();
                    return Convert.ToBase64String(enumValue as byte[]);
                }
                else {
                    return "base64Binary Content";
                }
          }            
      }
    
      
      internal class Generator_QName : Generator_string {
          public Generator_QName() {
          }

          public Generator_QName(CompiledFacets rFacets) {
                Prefix = "qname";
                CheckFacets(rFacets);
          }

          public override string GenerateValue() {
                string result = base.GenerateValue();
                if (result.Length == 1) { //If it is a qname of length 1, then return a char to be sure
                    return new string(Prefix[0], 1);
                }
                return result;
          }
      }
      
      internal class Generator_Notation : Generator_QName {
                      
          public Generator_Notation(CompiledFacets rFacets) {
             CheckFacets(rFacets);
          }
      }
    
      internal class Generator_normalizedString : Generator_string {
          public Generator_normalizedString() {
          }
          public Generator_normalizedString(CompiledFacets rFacets) : base(rFacets){
          }
      }

      internal class Generator_token : Generator_normalizedString {
          public Generator_token() {
          }
          public Generator_token(CompiledFacets rFacets) {
              Prefix = "Token";
              CheckFacets(rFacets);
          }
      }

      internal class Generator_language : Generator_string { //A derived type of token
          static string[] languageList = new string[] {"en", "fr", "de", "da", "el", "it", "en-US"};
          public Generator_language(CompiledFacets rFacets) : base(rFacets){
          }

          public override string GenerateValue() {
                if(AllowedValues != null) {
                    return base.GenerateValue();
                }
                else {
                    return languageList[occurNum++ % languageList.Length];
                }
          }
      }

      internal class Generator_NMTOKEN : Generator_token {

          public Generator_NMTOKEN() {
          }
            
          public Generator_NMTOKEN(CompiledFacets rFacets) : base(rFacets){
          }
      }

      internal class Generator_Name : Generator_string {

          public Generator_Name(CompiledFacets rFacets){
              Prefix = "Name";
              CheckFacets(rFacets);
          }
      }

      internal class Generator_NCName : Generator_string {

          public Generator_NCName(CompiledFacets rFacets) {
              Prefix = "NcName";
              CheckFacets(rFacets);
          }
      }

      internal class Generator_ID : Generator_string {
          
          public Generator_ID() {
              Prefix="ID";
          }
        
          public override string GenerateValue() {
                if (IDRef) {
                    IDRef = false;
                    return (string)IDList[IDCnt-1];
                }
                else {
                    string id = base.GenerateValue();
                    IDList.Add(id); //Add to arraylist so that you can retreive it from there for IDREF
                    return (string)IDList[IDCnt++];
                }
          }
      }


      internal class Generator_IDREF : Generator_string {
          int refCnt = 0;
          public Generator_IDREF() {
          }
          public override string GenerateValue() {
                if (IDList.Count == 0) {
                    string id = g_ID.GenerateValue();
                    IDRef = true;
                }
                if (refCnt >= IDList.Count) {
                    refCnt = refCnt - IDList.Count;
                }
                return (string)IDList[refCnt++];
          }
      }
      
        internal class Generator_anyURI : Generator_string {
            public Generator_anyURI(CompiledFacets rFacets) {
                Prefix = "http://uri";
                CheckFacets(rFacets);
            }
        }
        
        internal class Generator_Union : Generator_facetBase {
           ArrayList unionGens = new ArrayList();
            
           public Generator_Union() {
           }

           public Generator_Union(CompiledFacets rFacets) {
               CheckFacets(rFacets);
           }

           public override void AddGenerator(XmlValueGenerator genr) {
                unionGens.Add(genr);
           }
           
           internal ArrayList Generators {
               get {
                   return unionGens;
               }
           }

           public override string GenerateValue() {
                if(AllowedValues != null) {
                    object enumValue = GetEnumerationValue();
                    //Unpack the union enumeration value into memberType and typedValue
                    object typedValue = CompiledFacets.XsdSimpleValueType.InvokeMember("typedValue", BindingFlags.GetField | BindingFlags.NonPublic | BindingFlags.Instance, null, enumValue, null);
                    return (string)this.Datatype.ChangeType(typedValue, TypeOfString);
                } 
                else if (unionGens.Count > 0){
                    XmlValueGenerator genr = (XmlValueGenerator)(unionGens[occurNum % unionGens.Count]);
                    genr.Prefix = this.Prefix;
                    occurNum = occurNum + 1;
                    return genr.GenerateValue();
                }
                return string.Empty;
            }
        }

        internal class Generator_List : Generator_facetBase {
           XmlValueGenerator genr;
           int listLength;
           StringBuilder resultBuilder; 
           
           public Generator_List() {
           }

           public Generator_List(CompiledFacets rFacets) {
               CheckFacets(rFacets);
           }

           public int ListLength {
               get {
                   return listLength;
               }
               set {
                   listLength = value;
               }
           }

           public override void AddGenerator(XmlValueGenerator valueGenr) {
                genr = valueGenr;
           }

           public override string GenerateValue() {
                if (resultBuilder == null) {
                    resultBuilder = new StringBuilder();
                }
                else { //Clear old value
                    resultBuilder.Length = 0;
                }
                if (AllowedValues != null) {
                    object enumValue = GetEnumerationValue();
                    return (string)this.Datatype.ChangeType(enumValue, TypeOfString);
                }
                else {
                    genr.Prefix = this.Prefix;
                    int NoItems = listLength;
                    if (length != -1) {
                        NoItems = length;
                    }
                    else if (minLength != -1) {
                        NoItems = minLength;
                    }
                    else if (maxLength != -1) {
                        NoItems = maxLength;
                    }
                    for(int i=0; i < NoItems; i++) {
                        resultBuilder.Append(genr.GenerateValue());
                        resultBuilder.Append(" ");
                    }             
                }
                return resultBuilder.ToString().Trim();
            }
        }
}
