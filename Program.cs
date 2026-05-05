using Antlr4.Runtime;

class Program
{   
    static void Main(String[] args)
    {
        string Path = "examples/example_function.awk";
        ICharStream inputStream = CharStreams.fromPath(Path);
        AwkLexer lexer = new AwkLexer(inputStream);
        CommonTokenStream stream = new CommonTokenStream(lexer);
        AwkParser parser = new AwkParser(stream);

        AwkParser.ProgramContext context = parser.program();

        var table = new AwkToC.Semantic.SymbolTableBuilder().Build(context);
        
        table.All().ToList().ForEach(v => Console.WriteLine(v.Name + "\t" + v.Scope + "\t" + v.Type));
        Console.WriteLine("Parse tree: " + context.ToStringTree(parser));
    }
}
