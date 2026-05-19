namespace AwkToC.Cli;

public class CliOptions
{
    public bool ShowHelp { get; init; }

    public string InputPath { get; init; } = string.Empty;

    public string OutputPath { get; init; } = "main.c";
}