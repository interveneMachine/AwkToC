using System.Diagnostics;

namespace AwkToC.Tests.CompilationTests;

public class PatternMatchingTests
{
    [Theory]
    [InlineData("TestBeginPattern_0")]
    [InlineData("TestBeginPattern_1")]
    [InlineData("TestEndPattern_0")]
    [InlineData("TestEndPattern_1")]
    [InlineData("TestExprPattern_0")]
    [InlineData("TestExprPattern_1")]
    [InlineData("TestRegexPattern_0")]
    [InlineData("TestRegexPattern_1")]
    [InlineData("TestRegexPattern_2")]
    void CompilationResultsInCorrectBehaviour(string testdir)
    {
        // tests are executed in /bin/Debug/net8.0 so we need to get back by with ..
        string dir = Path.Combine("..", "..", "..", "Tests", "PatternMatchingTests", testdir);
        string runtime = Path.Combine(dir, "..", "..", "..", "..", "runtime");
        string runtimeC = Path.Combine(runtime, "awk_runtime.c");
        string data = Path.Combine(dir, "data.txt");
        string awkFile = Path.Combine(dir, "main.awk");
        string cFile = Path.Combine(dir, "main.c");
        string compiled = Path.Combine(dir, "main");
        string correctResults = Path.Combine(dir, "results.txt");
        string generatedResults = Path.Combine(dir, "generatedResults.txt");
        
        AwkToC.Compiler.Compiler.Compile(
            new StreamReader(awkFile),
            new StreamWriter(cFile)
        );

        var compile = Process.Start(new ProcessStartInfo
        {
            FileName = "/bin/gcc",
            Arguments = $" {cFile} {runtimeC} -o {compiled} -I {runtime} -lm",
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        }) ?? throw new InvalidOperationException("Failed to start gcc process");
        compile.WaitForExit();
        if (compile.ExitCode != 0)
        {
            string stdOut = compile.StandardOutput.ReadToEnd();
            string stdErr = compile.StandardError.ReadToEnd();
            throw new InvalidOperationException(
                $"GCC compilation failed with exit code {compile.ExitCode}\n" +
                $"Command: /bin/gcc {cFile} -o {compiled}\n" +
                $"StdOut: {stdOut}\n" +
                $"StdErr: {stdErr}"
            );
        }

        var run = Process.Start(new ProcessStartInfo
        {
            FileName = "/bin/bash",
            Arguments = $"-c \"./{compiled} {data} > {generatedResults}\"",
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        }) ?? throw new InvalidOperationException("Failed to start bash process");
        run.WaitForExit();
        if (run.ExitCode != 0)
        {
            string stdOut = run.StandardOutput.ReadToEnd();
            string stdErr = run.StandardError.ReadToEnd();
            throw new InvalidOperationException(
                $"running compiled program failed with exit code {run.ExitCode}\n" +
                $"Command: /bin/bash -c \"./{compiled} {data} > {generatedResults}\"\n" +
                $"StdOut: {stdOut}\n" +
                $"StdErr: {stdErr}"
            );
        }
    
        Assert.Equal(
            File.ReadAllText(correctResults),
            File.ReadAllText(generatedResults)
        );

        File.Delete(cFile);
        File.Delete(compiled);
        File.Delete(generatedResults);
    }
}