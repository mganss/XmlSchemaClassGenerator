using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;

namespace XmlSchemaClassGenerator;

public class CommandLineArgumentsProvider(string commandLineArguments)
{
    public string CommandLineArguments { get; } = commandLineArguments;

    public static CommandLineArgumentsProvider CreateFromEnvironment()
    {
        var args = Environment.GetCommandLineArgs();
        var commandLineArguments = string.Join(" ", args.Take(1).Select(Path.GetFileNameWithoutExtension).Concat(args.Skip(1)).Select(Extensions.QuoteIfNeeded));
        return new CommandLineArgumentsProvider(commandLineArguments);
    }
}