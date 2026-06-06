using System.Diagnostics;

namespace AwkToC.Tests.CompilationTests;

public class ExprCompilationTests
{
    [Theory]
    [InlineData("TestDecrExpr_0")]
    [InlineData("TestDecrExpr_1")]
    [InlineData("TestDecrExpr_2")]
    [InlineData("TestIncrExpr_0")]
    [InlineData("TestIncrExpr_1")]
    [InlineData("TestIncrExpr_2")]
    [InlineData("TestFieldExpr_0")]
    [InlineData("TestFieldExpr_1")]
    [InlineData("TestDivExpr_0")]
    [InlineData("TestMatchExpr_0")]
    [InlineData("TestMinusExpr_0")]
    [InlineData("TestModExpr_0")]
    [InlineData("TestMulExpr_0")]
    [InlineData("TestNotExpr_0")]
    [InlineData("TestPlusExpr_0")]
    [InlineData("TestPowExpr_0")]
    [InlineData("TestPowExpr_1")]
    [InlineData("TestComparisonExpr_0")]
    [InlineData("TestConcatenationExpr_0")]
    [InlineData("TestAssignExpr_0")]
    [InlineData("TestAndExpr_0")]
    [InlineData("TestOrExpr_0")]
    [InlineData("TestArrayExpr_0")]
    void CompilationResultsInCorrectBehaviour(string testdir)
    {
        // tests are executed in /bin/Debug/net8.0 so we need to get back by with ..
        string dir = Path.Combine("..", "..", "..", "Tests", "ExprCompilationTests", testdir);
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