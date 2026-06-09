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
                Column = context.Start.Column,
                Returns = false
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
    
    public override object? VisitTerminated_statement(
        [NotNull] AwkParser.Terminated_statementContext context
    )
    {
        if (context.IN() != null)
        {
            string name = context.NAME()[0].GetText();

            var symbol = table.Lookup(name, awkScope);
            bool isArray = false;

            if (symbol != null)
            {
                ValidateArrayUsage(symbol, isArray, context.Start.Line, context.Start.Column);
                return VisitChildren(context);
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

        return VisitChildren(context);
    }

    public override object? VisitUnterminated_statement(
        [NotNull] AwkParser.Unterminated_statementContext context
    )
    {
        if (context.IN() != null)
        {
            string name = context.NAME()[0].GetText();

            var symbol = table.Lookup(name, awkScope);
            bool isArray = false;

            if (symbol != null)
            {
                ValidateArrayUsage(symbol, isArray, context.Start.Line, context.Start.Column);
                return VisitChildren(context);
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

        return VisitChildren(context);
    }
   
    public override object? VisitTerminatable_statement(
        [NotNull] AwkParser.Terminatable_statementContext context
    )
    {
        if (context.RETURN() != null)
        {
            if (awkScope.GetScope() == "global")
            {
                throw new Exception(
                    $"[{context.Start.Line}:{context.Start.Column}] Return statement outside of function"
                );
            }

            string name = awkScope.GetScope().Split(":")[1];

            Symbol function = table.Lookup(name, awkScope)
                              ?? throw new Exception($"missing {name} from table");

            if (function.Type != SymbolType.Function)
            {
                throw new Exception($"symbol {name} is not a function");
            }

            function.Returns = true;
        }

        return base.VisitTerminatable_statement(context);
    }
    public override object? VisitSimple_statement(
        [NotNull] AwkParser.Simple_statementContext context
    )
    {
        if (context.DELETE() != null)
        {
            string name = context.NAME().GetText();
            bool isArray = true;

            var symbol = table.Lookup(name, awkScope);

            if (symbol != null)
            {
                ValidateArrayUsage(symbol, isArray, context.Start.Line, context.Start.Column);
            }
            else
            {
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
            
            if (context.expr_list() != null)
            {
                Visit(context.expr_list());
            }

            return null;
        }

        return base.VisitSimple_statement(context);
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
                        ? $"[{context.Start.Line}:{context.Start.Column}] attempt to use array `{name}` in a scalar context"
                        : $"[{context.Start.Line}:{context.Start.Column}] attempt to use scalar `{name}` in an array context");
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
    
    private static void ValidateArrayUsage(
        Symbol symbol,
        bool expectedArrayUsage,
        int line,
        int column
    )
    {
        if (symbol.IsArray == expectedArrayUsage)
        {
            return;
        }

        string name = symbol.Name;

        throw new Exception(symbol.IsArray
            ? $"[{line}:{column}] attempt to use array `{name}` in a scalar context"
            : $"[{line}:{column}] attempt to use scalar `{name}` in an array context");
    }
}

