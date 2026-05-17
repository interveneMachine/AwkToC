using System.Diagnostics;

namespace AwkToC.Tests.CompilationTests;

public class ExprCompilationTests
{
    public static bool CompareFiles(string firstFilePath, string secondFilePath)
    {
        const int bufferSize = 1024;
        using var stream1 = new FileStream(firstFilePath, FileMode.Open, FileAccess.Read);
        using var stream2 = new FileStream(secondFilePath, FileMode.Open, FileAccess.Read);
        if (stream1.Length != stream2.Length)
        {
            return false;
        }
        Span<byte> buffer1 = new byte[bufferSize];
        Span<byte> buffer2 = new byte[bufferSize];
        while (true)
        {
            var bytesRead1 = stream1.Read(buffer1);
            var bytesRead2 = stream2.Read(buffer2);
            if (bytesRead1 != bytesRead2)
            {
                return false;
            }
            if (bytesRead1 == 0)
            {
                return true;
            }
            if (!buffer1.SequenceEqual(buffer2))
            {
                return false;
            }
        }
    }

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
    void CompilationResultsInCorrectBehaviour(string testdir)
    {
        // tests are executed in /bin/Debug/net8.0 so we need to get back by with ..
        string dir = Path.Combine("..", "..", "..", "Tests", "ExprCompilationTests", testdir);
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

        var compile = Process.Start("/bin/gcc", $" {cFile} -o {compiled}");
        compile.WaitForExit();
        var run = Process.Start("/bin/bash", $"-c \"./{compiled} {data} > {generatedResults}\"");
        run.WaitForExit();
        
        Assert.True(
            CompareFiles(generatedResults, correctResults),
            $"results of running compiled program are different than expected in test \"{testdir}\"" +
            "\n\nexpected:\n" + File.ReadAllText(correctResults) +
            "\n\ngenerated:\n" + File.ReadAllText(generatedResults)
        );

        File.Delete(cFile);
        File.Delete(compiled);
        File.Delete(generatedResults);
    }
}