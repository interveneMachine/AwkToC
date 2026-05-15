using System.ComponentModel.DataAnnotations.Schema;
using Antlr4.Runtime.Misc;
using Antlr4.Runtime.Tree;

using AwkToC.Semantic;

namespace AwkToC.CodeGeneration;

class CodeGenerator : AwkBaseVisitor<NodeCompilationResult>
{
    SymbolTable symbolTable;
    CWriter stream;
    private string currentScope = "global";

    public CodeGenerator(SymbolTable symbolTable, string filename)
    {
        this.symbolTable = symbolTable;
        stream = new CWriter(filename);
    }

    public override NodeCompilationResult VisitProgram(AwkParser.ProgramContext context)
    {
        stream.WriteLine("#include<stdio.h>");
        stream.HSpace(2);
        stream.WriteLine("int main()");
        stream.EnterBlock();
        stream.WriteLine("printf(\"test\\n\");");
        stream.WriteLine("return 0;");
        stream.ExitBlock();
        return VisitChildren(context);
    }

    public override NodeCompilationResult VisitItem(AwkParser.ItemContext context)
    {
        if (context.FUNCTION() != null)
        {
            string functionName = context.NAME().GetText();
            string previousScope = currentScope;
            currentScope = $"function:{functionName}";

            // correct scope for function

            currentScope = previousScope;
            return new NodeCompilationResult();
        }
        return new NodeCompilationResult();
    }

    public void Close() { stream.Close(); } //TODO potential errors if closed more than one time
}