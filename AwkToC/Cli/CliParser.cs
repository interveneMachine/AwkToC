namespace AwkToC.Cli;

public static class CliParser
{
    public static bool TryParse(
        string[] args,
        out CliOptions options,
        out string? errorMessage
    )
    {
        options = new CliOptions();
        errorMessage = null;

        string? inputPath = null;
        string outputPath = "main.c";
        bool showHelp = false;

        for (int i = 0; i < args.Length; i++)
        {
            string arg = args[i];

            switch (arg)
            {
                case "-h":
                case "--help":
                    showHelp = true;
                    break;

                case "-o":
                case "--output":
                    if (i + 1 >= args.Length)
                    {
                        errorMessage =
                            "Option '-o' / '--output' requires an output file name.";
                        return false;
                    }

                    outputPath = args[i + 1];
                    i++;
                    break;

                default:
                    if (arg.StartsWith("-"))
                    {
                        errorMessage = $"Unknown option '{arg}'.";
                        return false;
                    }

                    if (inputPath != null)
                    {
                        errorMessage =
                            "Too many input files were provided.";
                        return false;
                    }

                    inputPath = arg;
                    break;
            }
        }

        if (showHelp)
        {
            options = new CliOptions
            {
                ShowHelp = true,
                OutputPath = outputPath,
                InputPath = inputPath ?? string.Empty
            };

            return true;
        }

        if (inputPath == null)
        {
            errorMessage = "Missing input AWK file.";
            return false;
        }

        options = new CliOptions
        {
            ShowHelp = false,
            InputPath = inputPath,
            OutputPath = outputPath
        };

        return true;
    }

    public static string GetHelpText()
    {
        return """
Usage:
  AwkToC <input.awk> [-o <output.c>]

Options:
  -o, --output <file>   Path to generated C file. Default: main.c
  -h, --help            Show this help message
""";
    }
}