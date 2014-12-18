namespace XmlSchemaClassGenerator
{
    public static class CodeUtilities
    {
        private const string Alpha = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ";
        private const string Num = "0123456789";

        public static string ToNormalizedEnumName(this string name)
        {
            name = name.Trim().Replace(' ', '_').Replace('\t', '_');
            if (string.IsNullOrEmpty(name))
                return "Item";
            if (Alpha.IndexOf(name[0]) == -1)
                return string.Format("Item{0}", name);
            return name;
        }
    }
}