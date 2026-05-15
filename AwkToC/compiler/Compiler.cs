using Antlr4.Runtime;

namespace AwkToC.Compiler;

class Compiler
{
    public static void Compile(StreamReader streamIn, StreamWriter streamOut)
    {
        ICharStream charStream = CharStreams.fromStream(streamIn.BaseStream);
        AwkLexer lexer = new AwkLexer(charStream);
        CommonTokenStream tokenStream = new CommonTokenStream(lexer);
        AwkParser parser = new AwkParser(tokenStream);

        AwkParser.ProgramContext context = parser.program();

        var table = new Semantic.SymbolTableBuilder().Build(context);

        var generator = new CodeGeneration.CodeGenerator(table, streamOut);
        generator.Visit(context);
        generator.Close();
    }
}