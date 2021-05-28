using System;
using System.IO;
using System.Linq;

namespace XmlSchemaClassGenerator
{
    public class CommandLineArgumentsProvider
    {
        public virtual string CommandLineArguments
        {
            get
            {
                var args = Environment.GetCommandLineArgs();
                return string.Join(" ", args.Take(1).Select(Path.GetFileNameWithoutExtension).Concat(args.Skip(1)).Select(Extensions.QuoteIfNeeded));
            }
        }
    }
}