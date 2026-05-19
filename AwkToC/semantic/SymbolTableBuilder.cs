using Antlr4.Runtime.Misc;
using Antlr4.Runtime.Tree;

namespace AwkToC.Semantic;

public class SymbolTableBuilder : AwkBaseVisitor<object?>
{
    private readonly SymbolTable table = new();
    private string currentScope = "global";
    public SymbolTable Build(AwkParser.ProgramContext context)
    {
        Visit(context);
        return table;
    }

    public override object? VisitItem(AwkParser.ItemContext context)
    {
        if (context.FUNCTION() != null)
        {
            string functionName = context.NAME().GetText();
            table.Add(new Symbol
            {
                Name = functionName,
                Type = SymbolType.Function,
                Scope = "global",
                Line = context.Start.Line,
                Column = context.Start.Column
            });

            string previousScope = currentScope;
            currentScope = $"function:{functionName}";
            
            var paramList = context.param_list_opt()?.param_list();
            if (paramList != null)
            {
                Visit(paramList);
            }

            Visit(context.action());
            currentScope = previousScope;
            return null;
        }

        return base.VisitItem(context);
    }
    public override object? VisitParam_list([NotNull] AwkParser.Param_listContext context)
    {
        foreach(var nameToken in context.NAME())
        {
            table.Add(new Symbol
            {
                Name = nameToken.GetText(),
                Type = SymbolType.Parameter,
                Scope = currentScope,
                Line = nameToken.Symbol.Line,
                Column = nameToken.Symbol.Column,
                IsAssigned = true
            });
        }
        return null;
    }

    public override object? VisitLvalue(AwkParser.LvalueContext context)
    {
        if (context.NAME() != null)
        {
            bool isArray = context.LBRACKET() != null;
            table.Add(new Symbol
            {
                Name = context.NAME().GetText(),
                Type = isArray ? SymbolType.Array : SymbolType.Variable,
                Scope = "global",
                Line = context.Start.Line,
                Column = context.Start.Column,
                IsArray = isArray,
                IsUsed = true
            });
        }

        return base.VisitLvalue(context);
    }
}
