using System.Diagnostics;

namespace AwkToC.Tests.CompilationTests;

public class PatternMatchingTests
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