using Antlr4.Runtime.Misc;

namespace AwkToC.Semantic;

public class SymbolTableBuilder : AwkBaseVisitor<object?>
{
    private readonly SymbolTable table = new();
    private readonly AwkScope awkScope = new();
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

            awkScope.EnterFunction(functionName);
            
            var paramList = context.param_list_opt()?.param_list();
            if (paramList != null)
            {
                Visit(paramList);
            }

            Visit(context.action());
            awkScope.ExitFunction();
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
                Scope = awkScope.GetScope(),
                Line = nameToken.Symbol.Line,
                Column = nameToken.Symbol.Column,
            });
        }
        return null;
    }

    public override object? VisitLvalue(AwkParser.LvalueContext context)
    {
        if (context.NAME() != null)
        {
            string name = context.NAME().GetText();
            bool isArray = context.LBRACKET() != null;
            var s = table.Lookup(name, awkScope);
            if (s != null)
            {
                if (s.IsArray != isArray)
                    throw new Exception(s.IsArray
                        ? $"[line {context.Start.Line}] attempt to use array `{name}` in a scalar context"
                        : $"[line {context.Start.Line}] attempt to use scalar `{name}` in an array context");
                return null;
            }
            table.Add(new Symbol
            {
                Name = name,
                Type = SymbolType.Variable,
                Scope = awkScope.GetGlobal(),
                Line = context.Start.Line,
                Column = context.Start.Column,
                IsArray = isArray
            });
        }

        return null;
    }
}
