using System.Diagnostics;

namespace AwkToC.Tests.CompilationTests;

public class FunctionCompilationTests
{
    [Theory]
    [InlineData("TestEmptyFunction_0")]
    [InlineData("TestExprFunction_0")]
    [InlineData("TestReturnFunction_0")]
    [InlineData("TestParamFunction_0")]
    [InlineData("TestParamFunction_1")]
    [InlineData("TestParamFunction_2")]
    [InlineData("TestParamFunction_3")]
    [InlineData("TestReturnFunction_1")]
    [InlineData("TestGlobalVariablesFunction_0")]
    [InlineData("TestGlobalVariablesFunction_1")]
    void CompilationResultsInCorrectBehaviour(string testdir)
    {
        // tests are executed in /bin/Debug/net8.0 so we need to get back by with ..
        string dir = Path.Combine("..", "..", "..", "Tests", "FunctionCompilationTests", testdir);
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
            Arguments = $" {cFile} -o {compiled}",
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