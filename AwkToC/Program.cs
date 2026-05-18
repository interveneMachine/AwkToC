using AwkToC.Cli;
using AwkToC.Compiler;

class Program
{
    static int Main(string[] args)
    {
        if (!CliParser.TryParse(
                args,
                out CliOptions options,
                out string? errorMessage
            ))
        {
            Console.Error.WriteLine($"Error: {errorMessage}");
            Console.Error.WriteLine("Use --help to see available options.");
            return 1;
        }

        if (options.ShowHelp)
        {
            Console.WriteLine(CliParser.GetHelpText());
            return 0;
        }

        if (!File.Exists(options.InputPath))
        {
            Console.Error.WriteLine(
                $"Error: input file '{options.InputPath}' does not exist."
            );

            return 1;
        }

        try
        {
            using var input = new StreamReader(options.InputPath);
            using var output = new StreamWriter(options.OutputPath);

            Compiler.Compile(input, output);

            Console.WriteLine(
                $"Generated C code: {options.OutputPath}"
            );

            return 0;
        }
        catch (Exception exception)
        {
            Console.Error.WriteLine(
                $"Compilation failed: {exception.Message}"
            );

            return 1;
        }
    }
}