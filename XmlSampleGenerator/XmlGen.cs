using System;
using System.IO;
using System.Xml;
using System.Xml.Schema;
using System.Globalization;
using System.Collections;
using Microsoft.Xml.XMLGen;

namespace XmlTools {

    public class XmlGen {

        public static void Main(string[] args) {
            XmlSchemaSet schemas = new XmlSchemaSet();
            XmlQualifiedName qname = XmlQualifiedName.Empty;    
            string localName = string.Empty;
            string ns = string.Empty;
            int max = 0;
            int listLength = 0;

            for (int i = 0; i < args.Length; i++) {
                string arg = args[i];
                string value = string.Empty;
                bool argument = false;
                
                if (arg.StartsWith("/") || arg.StartsWith("-")) {
                    argument = true;
                    int colonPos = arg.IndexOf(":");
                    if (colonPos != -1) {
                        value = arg.Substring(colonPos + 1);
                        arg = arg.Substring(0, colonPos);
                    }
                }
                arg = arg.ToLower(CultureInfo.InvariantCulture);
                if (!argument && arg.EndsWith(".xsd")) {
                    schemas.Add(null, args[i]);
                }
                else if (ArgumentMatch(arg, "?") || ArgumentMatch(arg, "help")) {
                    WriteHelp();
                    return;
                }
                else if (ArgumentMatch(arg, "localname")) {
                    localName = value;
                }
                else if (ArgumentMatch(arg, "namespace")) {
                    ns = value;
                }
                else if (ArgumentMatch(arg, "maxthreshold")) {
                    max = Int16.Parse(value);
                }
                else if (ArgumentMatch(arg, "listlength")) {
                    listLength = Int16.Parse(value);
                }
                else {
                    throw new ArgumentException(args[i]);
                }
            }
            
            if (args.Length == 0) {
                WriteHelp();
                return;
            }
            XmlTextWriter textWriter = new XmlTextWriter("Sample.xml", null);
            textWriter.Formatting = Formatting.Indented;
            XmlSampleGenerator genr = new XmlSampleGenerator(schemas, qname);
            if (max > 0) genr.MaxThreshold = max;
            if (listLength > 0) genr.ListLength = listLength;
            genr.WriteXml(textWriter);
        }

        private static void WriteHelp() {
            Console.WriteLine("XmlGen - Utility to generate XML instance given an XML Schema");
            Console.WriteLine("Usage - XmlGen <schema>.xsd [/localName:] [/namespace:] [/maxThreshold:] [/listLength:]");
            Console.WriteLine("/localName:      LocalName of the root element to begin generating the XML");
            Console.WriteLine("/namespace:      Namespace of the root element to begin generating the XML");
            Console.WriteLine("/maxThreshold:   Number of elements to be generated if maxOccurs='unbounded' or a really high number");
            Console.WriteLine("/listLength:     Number of items while generating a list type");
        }

        // assumes all same case.        
        private static bool ArgumentMatch(string arg, string toMatch) {
            if (arg[0] != '/' && arg[0] != '-') {
                return false;
            }
            arg = arg.Substring(1);
            return arg == toMatch;
        }
    }
}

