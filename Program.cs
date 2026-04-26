using Antlr4.Runtime;

class Program
{   
    static void Main(String[] args)
    {
        string Path = "examples/example01.awk";
        ICharStream inputStream = CharStreams.fromPath(Path);
        AwkLexer lexer = new AwkLexer(inputStream);
        CommonTokenStream stream = new CommonTokenStream(lexer);
        AwkParser parser = new AwkParser(stream);

        AwkParser.ProgramContext context = parser.program();
        Console.WriteLine("Parse tree: " + context.ToStringTree(parser));
    }
}
