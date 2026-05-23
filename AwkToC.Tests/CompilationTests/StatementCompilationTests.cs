using System.Diagnostics;

namespace AwkToC.Tests.CompilationTests;

public class StatementCompilationTests
{
    [Theory]
    [InlineData("TestIfStatement_0")]
    [InlineData("TestWhileStatement_0")]
    [InlineData("TestForStatement_0")]
    [InlineData("TestForStatement_1")]
    [InlineData("TestForStatement_2")]
    [InlineData("TestForStatement_3")]
    [InlineData("TestForInStatement_0")]
    [InlineData("TestForInStatement_1")]
    [InlineData("TestForInStatement_2")]
    [InlineData("TestDoWhileStatement_0")]
    [InlineData("TestDoWhileStatement_1")]
    [InlineData("TestDoWhileStatement_2")]
    [InlineData("TestDoWhileStatement_3")]
    [InlineData("TestIfElseStatement_0")]
    [InlineData("TestIfElseStatement_1")]
    [InlineData("TestIfElseChainedStatement_0")]
    [InlineData("TestIfElseChainedStatement_1")]
    [InlineData("TestIfElseChainedStatement_2")]
    [InlineData("TestNestedLoopsWithBreakContinue_0")]
    [InlineData("TestBreakContinueInFunctionStatement_0")]
    [InlineData("TestMultipleBreaksStatement_0")]
    [InlineData("TestContinueInWhileWithComplexCondition_0")]
    void CompilationResultsInCorrectBehaviour(string testdir)
    {
        // tests are executed in /bin/Debug/net8.0 so we need to get back by with ..
        string dir = Path.Combine("..", "..", "..", "Tests", "StatementCompilationTests", testdir);
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

        string stdOut, stdErr;
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
            stdOut = compile.StandardOutput.ReadToEnd();
            stdErr = compile.StandardError.ReadToEnd();
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
            stdOut = run.StandardOutput.ReadToEnd();
            stdErr = run.StandardError.ReadToEnd();
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

        var runWithValgrind = Process.Start(new ProcessStartInfo
        {
            FileName = "/bin/bash",
            Arguments = $"valgrind --leak-check=full --error-exitcode=1 ./{compiled} {data}",
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        }) ?? throw new InvalidOperationException("Failed to start valgrind process");
        runWithValgrind.WaitForExit();

        Assert.True(runWithValgrind.ExitCode == 0, 
            $"valgrind memcheck detected errors/leaks\n" +
            $"command: valgrind --leak-check=full --error-exitcode=1 ./main data.txt\n" +
            $"in {dir}"
        );

        File.Delete(cFile);
        File.Delete(compiled);
        File.Delete(generatedResults);
    }
}