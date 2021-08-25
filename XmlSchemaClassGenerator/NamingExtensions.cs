using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace XmlSchemaClassGenerator
{
    public static class NamingExtensions
    {
        private static readonly CodeDomProvider Provider = new Microsoft.CSharp.CSharpCodeProvider();

        private static readonly Dictionary<char, string> InvalidChars = new()
            {
                ['\x00'] = "Null",
                ['\x01'] = "StartOfHeading",
                ['\x02'] = "StartOfText",
                ['\x03'] = "EndOfText",
                ['\x04'] = "EndOfTransmission",
                ['\x05'] = "Enquiry",
                ['\x06'] = "Acknowledge",
                ['\x07'] = "Bell",
                ['\x08'] = "Backspace",
                ['\x09'] = "HorizontalTab",
                ['\x0A'] = "LineFeed",
                ['\x0B'] = "VerticalTab",
                ['\x0C'] = "FormFeed",
                ['\x0D'] = "CarriageReturn",
                ['\x0E'] = "ShiftOut",
                ['\x0F'] = "ShiftIn",
                ['\x10'] = "DataLinkEscape",
                ['\x11'] = "DeviceControl1",
                ['\x12'] = "DeviceControl2",
                ['\x13'] = "DeviceControl3",
                ['\x14'] = "DeviceControl4",
                ['\x15'] = "NegativeAcknowledge",
                ['\x16'] = "SynchronousIdle",
                ['\x17'] = "EndOfTransmissionBlock",
                ['\x18'] = "Cancel",
                ['\x19'] = "EndOfMedium",
                ['\x1A'] = "Substitute",
                ['\x1B'] = "Escape",
                ['\x1C'] = "FileSeparator",
                ['\x1D'] = "GroupSeparator",
                ['\x1E'] = "RecordSeparator",
                ['\x1F'] = "UnitSeparator",
                ['\x21'] = "ExclamationMark",
                ['\x22'] = "Quote",
                ['\x23'] = "Hash",
                ['\x24'] = "Dollar",
                ['\x25'] = "Percent",
                ['\x26'] = "Ampersand",
                ['\x27'] = "SingleQuote",
                ['\x28'] = "LeftParenthesis",
                ['\x29'] = "RightParenthesis",
                ['\x2A'] = "Asterisk",
                ['\x2B'] = "Plus",
                ['\x2C'] = "Comma",
                ['\x2E'] = "Period",
                ['\x2F'] = "Slash",
                ['\x3A'] = "Colon",
                ['\x3B'] = "Semicolon",
                ['\x3C'] = "LessThan",
                ['\x3D'] = "Equal",
                ['\x3E'] = "GreaterThan",
                ['\x3F'] = "QuestionMark",
                ['\x40'] = "At",
                ['\x5B'] = "LeftSquareBracket",
                ['\x5C'] = "Backslash",
                ['\x5D'] = "RightSquareBracket",
                ['\x5E'] = "Caret",
                ['\x60'] = "Backquote",
                ['\x7B'] = "LeftCurlyBrace",
                ['\x7C'] = "Pipe",
                ['\x7D'] = "RightCurlyBrace",
                ['\x7E'] = "Tilde",
                ['\x7F'] = "Delete"
            };

        private static readonly Regex InvalidCharsRegex = CreateInvalidCharsRegex();

        private static Regex CreateInvalidCharsRegex()
        {
            var r = string.Join("", InvalidChars.Keys.Select(c => string.Format(@"\x{0:x2}", (int)c)).ToArray());
            return new Regex("[" + r + "]", RegexOptions.Compiled);
        }

        public static string ToNormalizedEnumName(this string name)
        {
            name = name.Trim().Replace(' ', '_').Replace('\t', '_');
            if (string.IsNullOrEmpty(name))
            {
                return "Empty";
            }
            if (!char.IsLetter(name[0]))
            {
                return string.Format("Item{0}", name);
            }
            return name;
        }

        public static string ToTitleCase(this string s, NamingScheme namingScheme)
        {
            if (string.IsNullOrEmpty(s)) { return s; }
            switch (namingScheme)
            {
                case NamingScheme.PascalCase:
                    s = s.ToPascalCase();
                    break;
            }
            return s.MakeValidIdentifier();
        }

        private static string MakeValidIdentifier(this string s)
        {
            var id = InvalidCharsRegex.Replace(s, m => InvalidChars[m.Value[0]]);
            return Provider.CreateValidIdentifier(Regex.Replace(id, @"\W+", "_"));
        }
    }
}