using System.ComponentModel.DataAnnotations.Schema;
using Antlr4.Runtime.Misc;
using Antlr4.Runtime.Tree;

using AwkToC.Semantic;

namespace AwkToC.CodeGeneration;

class CodeGenerator : AwkBaseVisitor<string?>
{
    SymbolTable symbolTable;
    StreamWriter stream;
    private string currentScope = "global";

    public CodeGenerator(SymbolTable symbolTable, StreamWriter stream)
    {
        this.symbolTable = symbolTable;
        this.stream = stream;
    }

    public override string? VisitProgram(AwkParser.ProgramContext context)
    {
        stream.Write("#include<stdio.h>\n" +
                     "int main()\n" + 
                     "{\n" +
                     "  printf(\"test\\n\");\n" +
                     "  return 0;\n" +
                     "}\n");
        return VisitChildren(context);
    }

    public override string? VisitItem(AwkParser.ItemContext context)
    {
        if (context.FUNCTION() != null)
        {
            string functionName = context.NAME().GetText();
            string previousScope = currentScope;
            currentScope = $"function:{functionName}";

            // correct scope for function

            currentScope = previousScope;
            return null;
        }
        return null;
    }
}