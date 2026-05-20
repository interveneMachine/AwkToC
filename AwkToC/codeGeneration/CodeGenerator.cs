using System.Linq.Expressions;
using Antlr4.Runtime.Misc;
using AwkToC.Semantic;

namespace AwkToC.CodeGeneration;

class CodeGenerator : AwkBaseVisitor<NodeCompilationResult>
{
    private readonly SymbolTable symbolTable;
    private readonly CWriter stream;

    private string currentScope = "global";
    private string currentCScope = "global";

    public CodeGenerator(SymbolTable symbolTable, StreamWriter streamWriter)
    {
        this.symbolTable = symbolTable;
        stream = new CWriter(streamWriter);
    }

    private NodeCompilationResult EmitTemporary(string cExpression, bool isMemoryAllocated)
    {
        string temporaryName = symbolTable.NewTemporaryCName(currentCScope, isMemoryAllocated);

        stream.WriteLine($"AwkValue {temporaryName} = {cExpression};");

        return new NodeCompilationResult(
            temporaryName,
            CType.General
        );
    }

    private static string RequireReturnName(
        NodeCompilationResult result,
        string operationName
    )
    {
        if (result.ReturnName == null)
        {
            throw new InvalidOperationException(
                $"Compilation of {operationName} did not produce a C value."
            );
        }

        return result.ReturnName;
    }

    private void FreeTmpVariables()
    {
        foreach(CSymbol tmpSymbol in symbolTable.AllTmpVariablesInScope(currentCScope))
        {
            if (tmpSymbol.IsMemoryAllocated)
                stream.WriteLine($"awk_free(&{tmpSymbol.Name});");
        }
    }

    public override NodeCompilationResult VisitProgram(
        AwkParser.ProgramContext context
    )
    {
        stream.WriteLine("#include <stdio.h>");
        stream.WriteLine("#include \"awk_runtime.h\"");
        stream.HSpace(1);

        stream.WriteLine("Fields fields;");

        symbolTable.All()
                   .Where(s => s.Type == SymbolType.Variable)
                   .Select(s => s.NameInC
                        ?? throw new Exception($"variable {s.Name} in global has NameInC=null"))
                   .ToList()
                   .ForEach(name => stream.WriteLine($"AwkValue {name};"));

        string? begin = null, end = null;
        List<string> itemsResults = new();
        foreach(var i in context.item())
        {
            NodeCompilationResult result = Visit(i);
            if(i.pattern() is not null && i.pattern().BEGIN() is not null)
                begin = RequireReturnName(result, "begin item");
            else if(i.pattern() is not null && i.pattern().END() is not null)
                end = RequireReturnName(result, "end item");
            else if(result.Type == CType.ItemFunction)
                itemsResults.Add(RequireReturnName(result, "item"));
        }
        
        stream.WriteLine("int main(int argc, char* argv[])");
        stream.EnterBlock();
        if (begin is not null) stream.WriteLine($"{begin}();");
        stream.WriteLine("FILE* file = fopen(argv[1], \"r\");");
        stream.WriteLine("char buffer[1024];");
        
        stream.WriteLine("while(fgets(buffer, sizeof(buffer), file))");
        stream.EnterBlock();
        stream.WriteLine("remove_newline(buffer);");
        stream.WriteLine("fields = fields_string(buffer);");
        itemsResults.Select(function => $"{function}();")
                    .ToList().ForEach(stream.WriteLine);
        stream.WriteLine("fields_free(&fields);");
        stream.ExitBlock();

        if (end is not null) stream.WriteLine($"{end}();");

        stream.WriteLine("fclose(file);");
        stream.WriteLine("return 0;");
        stream.ExitBlock();

        return new NodeCompilationResult();
    }

    public override NodeCompilationResult VisitItem(
        AwkParser.ItemContext context
    )
    {
        // pattern action
        if (context.pattern() is not null)
        {
            string itemName = symbolTable.NewItemCName();
            string patternName = "";
            bool isSpecial = context.pattern().BEGIN() is not null
                          || context.pattern().END() is not null;
            
            if (!isSpecial)
            {
                NodeCompilationResult patternResult = Visit(context.pattern());
                patternName = RequireReturnName(patternResult, "item -> pattern action");
            }
            
            stream.WriteLine($"void {itemName}()");
            stream.EnterBlock();

            string previousCScope = currentCScope;
            currentCScope = $"item:{itemName}";

            if (!isSpecial)
            {
                stream.WriteLine($"if({patternName}())");
                stream.EnterBlock();
            }

            Visit(context.action());

            FreeTmpVariables();
            if (!isSpecial) stream.ExitBlock();
            currentCScope = previousCScope;
            stream.ExitBlock();

            return new NodeCompilationResult(
                itemName,
                CType.ItemFunction
            );
        }

        // FUNCTION NAME ( param_list_opt ) newline_opt action
        if (context.FUNCTION() is not null)
        {
            string name = context.NAME().GetText();
            Symbol function = symbolTable.Lookup(name, currentScope)
                ?? throw new ArgumentException($"function {name} in {currentScope} is missing from symbolTable");
            string functionName = function.NameInC
                ?? throw new Exception($"function {name} in {currentScope} has NameInC=null");
            string previousScope = currentScope;
            currentScope = $"function:{name}";
            string previousCScope = currentCScope;
            currentCScope = $"function:{functionName}";

            List<string> parameters = new();
            if (context.param_list_opt() != null &&
                context.param_list_opt().param_list() != null)
                foreach(var parameter in context.param_list_opt().param_list().NAME())
                {
                    Symbol parameterSymbol = symbolTable.Lookup(parameter.GetText(), currentScope)
                        ?? throw new Exception($"symbolTable is missing symbol for {parameter.GetText()} in {currentScope}");
                    if(parameterSymbol.NameInC == null)
                        throw new Exception($"symbol with name: {parameterSymbol.Name} scope: {currentScope} is missing NameInC");
                    parameters.Add($"AwkValue {parameterSymbol.NameInC}");
                }
            string parameterNames = string.Join(", ", parameters);
            stream.WriteLine($"AwkValue {functionName}({parameterNames})");
            stream.EnterBlock();

            Visit(context.action());
            FreeTmpVariables();
            stream.WriteLine("return awk_undefined();");
            currentCScope = previousCScope;
            stream.ExitBlock();
            currentScope = previousScope;
            
            return new NodeCompilationResult();
        }

        // action
        if (context.action() is not null)
        {
            string itemNameAction = symbolTable.NewItemCName();
            stream.WriteLine($"void {itemNameAction}()");
            stream.EnterBlock();
            string previousCScope = currentCScope;
            currentCScope = $"item:{itemNameAction}";

            Visit(context.action());

            FreeTmpVariables();
            currentCScope = previousCScope; 
            stream.ExitBlock();
            return new NodeCompilationResult(
                itemNameAction,
                CType.ItemFunction
            );
        }
        
        // simple_pattern
        if(context.simple_pattern() is not null)
        {
            string itemNameSimplePattern = symbolTable.NewItemCName();
            NodeCompilationResult simplePatternResult = Visit(context.simple_pattern());
            string simplePatternName = RequireReturnName(simplePatternResult, "item -> simple_pattern");

            stream.WriteLine($"void {itemNameSimplePattern}()");
            stream.EnterBlock();
            string previousCScope = currentCScope;
            currentCScope = $"item:{itemNameSimplePattern}";

            stream.WriteLine($"if({simplePatternName}())");
            stream.EnterBlock();

            stream.WriteLine("awk_print_value(fields_get(fields, 0));");

            stream.ExitBlock();
            FreeTmpVariables();
            currentCScope = previousCScope;
            stream.ExitBlock();
                return new NodeCompilationResult(
                itemNameSimplePattern,
                CType.ItemFunction
            );
        }
        throw new Exception("unrecognised item rule");
    }

    public override NodeCompilationResult VisitPattern([NotNull] AwkParser.PatternContext context)
    {
        string patternName = symbolTable.NewPatternCName();
        stream.WriteLine($"int {patternName}()");
        stream.EnterBlock();
        string previousCScope = currentCScope;
        currentCScope = $"pattern:{patternName}";
        
        // BEGIN
        if (context.BEGIN() is not null)
        {
            stream.WriteLine("return isBegin;");
        }

        // END
        else if (context.END() is not null)
        {
            stream.WriteLine("return isEnd;");
        }

        // expr ',' newline_opt expr
        else if (context.COMMA() is not null)
        {
            throw new NotSupportedException(
                "expr ',' newline_opt expr nie jest obsługiwane"
            );
        }

        // expr
        else
        {
            NodeCompilationResult exprResult = Visit(context.expr()[0]);
            string exprName = RequireReturnName(exprResult, "pattern -> expr");
            stream.WriteLine($"return awk_is_truthy({exprName});");
        }

        FreeTmpVariables();
        currentCScope = previousCScope;
        stream.ExitBlock();
        return new NodeCompilationResult(
            patternName,
            CType.Function
        );
    }

    public override NodeCompilationResult VisitSimple_pattern([NotNull] AwkParser.Simple_patternContext context)
    {
        return base.VisitSimple_pattern(context);
    }

    public override NodeCompilationResult VisitSimple_print_statement(
        AwkParser.Simple_print_statementContext context
    )
    {
        if (context.PRINT() == null)
        {
            throw new NotSupportedException(
                "obsługiwane jest tylko 'print', bez 'printf'."
            );
        }

        var expressionList =
            context.print_expr_list_opt()?.print_expr_list();

        if (expressionList == null)
        {
            throw new NotSupportedException(
                "'print' bez argumentów nie jest jeszcze obsługiwany."
            );
        }

        List<AwkParser.ExprContext> expressions =
            CollectPrintExpressions(expressionList);

        List<string> compiledExpressionNames = new();

        foreach (var expression in expressions)
        {
            NodeCompilationResult expressionResult = Visit(expression);

            compiledExpressionNames.Add(
                RequireReturnName(expressionResult, "print expression")
            );
        }

    
        if (compiledExpressionNames.Count == 1)
        {
            stream.WriteLine(
                $"awk_print_value({compiledExpressionNames[0]});"
            );

            return new NodeCompilationResult();
        }


        string valuesArrayName = symbolTable.NewTemporaryCName(currentCScope, true);

        stream.WriteLine(
            $"AwkValue {valuesArrayName}[] = {{ {string.Join(", ", compiledExpressionNames)} }};"
        );

        stream.WriteLine(
            $"awk_print_values({compiledExpressionNames.Count}, {valuesArrayName});"
        );

        return new NodeCompilationResult();
    }

    private List<AwkParser.ExprContext> CollectPrintExpressions(
        AwkParser.Print_expr_listContext context
    )
    {
        List<AwkParser.ExprContext> expressions = new();

        if (context.print_expr_list() != null)
        {
            expressions.AddRange(
                CollectPrintExpressions(context.print_expr_list())
            );
        }

        expressions.Add(context.expr());

        return expressions;
    }

    public override NodeCompilationResult VisitTerminatable_statement(
        AwkParser.Terminatable_statementContext context
    )
    {
        // return 
        // return expr 
        if (context.RETURN() != null)
        {
            if (!currentScope.StartsWith("function:"))
            {
                throw new ArgumentException(
                    "'return' can be used only inside a function."
                );
            }

            AwkParser.ExprContext? returnExpression =
                context.expr_opt()?.expr();
            
            FreeTmpVariables();

            // return 
            if (returnExpression == null)
            {
                stream.WriteLine("return awk_undefined();");
                return new NodeCompilationResult();
            }

            // return expr
            NodeCompilationResult expressionResult =
                Visit(returnExpression);

            string expressionName =
                RequireReturnName(
                    expressionResult,
                    "return expression"
                );

            stream.WriteLine($"return {expressionName};");

            return new NodeCompilationResult();
        }

        return VisitChildren(context);
    }

    private List<AwkParser.ExprContext> CollectFunctionArguments(
        AwkParser.Expr_list_optContext? context
    )
    {
        List<AwkParser.ExprContext> arguments = new();

        if (context == null || context.expr_list() == null)
        {
            return arguments;
        }

        CollectExprList(context.expr_list(), arguments);
        return arguments;
    }

    private void CollectExprList(
        AwkParser.Expr_listContext context,
        List<AwkParser.ExprContext> arguments
    )
    {
        if (context.expr() != null)
        {
            arguments.Add(context.expr());
            return;
        }

        if (context.multiple_expr_list() != null)
        {
            CollectMultipleExprList(
                context.multiple_expr_list(),
                arguments
            );
        }
    }

    private void CollectMultipleExprList(
        AwkParser.Multiple_expr_listContext context,
        List<AwkParser.ExprContext> arguments
    )
    {
        if (context.multiple_expr_list() != null)
        {
            CollectMultipleExprList(
                context.multiple_expr_list(),
                arguments
            );

            var expressions = context.expr();

            if (expressions.Length != 1)
            {
                throw new InvalidOperationException(
                    "Unexpected function argument list structure."
                );
            }

            arguments.Add(expressions[0]);
            return;
        }

        var baseExpressions = context.expr();

        foreach (var expression in baseExpressions)
        {
            arguments.Add(expression);
        }
    }

    public override NodeCompilationResult VisitExpr(
        AwkParser.ExprContext context
    )
    {
     
        if (context.NUMBER() != null)
        {
            return EmitTemporary(
                $"awk_number({context.NUMBER().GetText()})",
                false
            );
        }

    
        if (context.STRING() != null)
        {
            return EmitTemporary(
                $"awk_string({context.STRING().GetText()})",
                false
            );
        }
        if (
            context.NAME() != null &&
            context.LPAREN() != null &&
            context.RPAREN() != null
        )
        {
            string functionName = context.NAME().GetText();

            Symbol functionSymbol =
                symbolTable.Lookup(functionName, "global")
                ?? throw new ArgumentException(
                    $"Function '{functionName}' is not defined."
                );

            if (functionSymbol.Type != SymbolType.Function)
            {
                throw new ArgumentException(
                    $"Symbol '{functionName}' is not a function."
                );
            }

            string functionNameInC =
                functionSymbol.NameInC
                ?? throw new Exception(
                    $"Function '{functionName}' has NameInC=null."
                );

            List<AwkParser.ExprContext> arguments =
                CollectFunctionArguments(context.expr_list_opt());

            List<string> compiledArgumentNames = new();

            foreach (var argument in arguments)
            {
                NodeCompilationResult argumentResult =
                    Visit(argument);

                compiledArgumentNames.Add(
                    RequireReturnName(argumentResult, "function argument")
                );
            }

            string joinedArguments =
                string.Join(", ", compiledArgumentNames);

            return EmitTemporary(
                $"{functionNameInC}({joinedArguments})",
                false
            );
        }

        AwkParser.ExprContext[] nestedExpressions = context.expr();

        // ERE
        if (
            context.ERE() != null &&
            nestedExpressions.Length == 0
        )
        {
            string ere = context.ERE().GetText();
            ere = ere.Trim('/').Replace("\\/", "/");

            return EmitTemporary(
                $"awk_match(fields_get(fields, 0), \"{ere}\")",
                false
            );
        }

        if (
            context.lvalue() != null &&
            nestedExpressions.Length == 0
        )
        {
            NodeCompilationResult lvalue = 
                Visit(context.lvalue());
            string lvalueName =
                RequireReturnName(lvalue, "lvalue");
            
            // lvalue
            if (context.ChildCount == 1)
                return EmitTemporary(
                    $"{lvalueName}",
                    false
                );

            // INCR lvalue
            if (context.GetChild(0).GetText() == "++")
            {
                return EmitTemporary(
                    $"({lvalueName} = awk_add({lvalueName}, awk_number(1)))",
                    false
                );
            }

            // DECR lvalue
            else if (context.GetChild(0).GetText() == "--")
            {
                return EmitTemporary(
                    $"({lvalueName} = awk_sub({lvalueName}, awk_number(1)))",
                    false
                );
            }

            // lvalue INCR
            else if (context.GetChild(1).GetText() == "++")
            {
                NodeCompilationResult result = EmitTemporary(
                    $"awk_copy({lvalueName})",
                    true
                );
                stream.WriteLine(
                    $"{lvalueName} = awk_add({lvalueName}, awk_number(1));"
                );
                return result;
            }

            // lvalue DECR
            else if (context.GetChild(1).GetText() == "--")
            {
                NodeCompilationResult result = EmitTemporary(
                    $"awk_copy({lvalueName})",
                    true
                );
                stream.WriteLine(
                    $"{lvalueName} = awk_sub({lvalueName}, awk_number(1));"
                );
                return result;
            }

            throw new Exception("unknown rule");
        }
        if (
            context.LPAREN() != null &&
            context.RPAREN() != null &&
            nestedExpressions.Length == 1
        )
        {
            return Visit(nestedExpressions[0]);
        }

        if (
            context.DOLLAR() != null &&
            nestedExpressions.Length == 1
        )
        {
            NodeCompilationResult value =
                Visit(nestedExpressions[0]);
            
            string valueName =
                RequireReturnName(value, "field operator");
            
            return EmitTemporary(
                $"fields_get(fields, awk_to_int({valueName}))",
                true
            );
        }

        if (
            context.NOT() != null &&
            nestedExpressions.Length == 1
        )
        {
            NodeCompilationResult value =
                Visit(nestedExpressions[0]);

            string valueName =
                RequireReturnName(value, "logical NOT");

            return EmitTemporary(
                $"awk_not({valueName})",
                false
            );
        }
        
        if (
            context.PLUS() != null &&
            nestedExpressions.Length == 1
        )
        {
            NodeCompilationResult value =
                Visit(nestedExpressions[0]);

            string valueName =
                RequireReturnName(value, "unary plus");

            return EmitTemporary(
                $"awk_unary_plus({valueName})",
                false
            );
        }

        if (
            context.MINUS() != null &&
            nestedExpressions.Length == 1
        )
        {
            NodeCompilationResult value =
                Visit(nestedExpressions[0]);

            string valueName =
                RequireReturnName(value, "unary minus");

            return EmitTemporary(
                $"awk_unary_minus({valueName})",
                false
            );
        }
        if (nestedExpressions.Length == 1)
        {
            // INCR expr / expr INCR
            if (context.INCR() != null)
            {
                NodeCompilationResult value = 
                    Visit(nestedExpressions[0]);
                
                string valueName =
                    RequireReturnName(value, "expr");
                
                return EmitTemporary(
                    $"awk_add({valueName}, awk_number(1))",
                    false
                );
            }
            // DECR expr / expr DECR
            if (context.DECR() != null)
            {
                NodeCompilationResult value = 
                    Visit(nestedExpressions[0]);
                
                string valueName =
                    RequireReturnName(value, "expr");
                
                return EmitTemporary(
                    $"awk_sub({valueName}, awk_number(1))",
                    false
                );
            }
            if (context.ASSIGN() != null)
            {
                NodeCompilationResult lvalue =
                    Visit(context.lvalue());
                
                string lvalueName =
                    RequireReturnName(lvalue, "assign(lvalue)");
                
                NodeCompilationResult value =
                    Visit(nestedExpressions[0]);

                string valueName =
                    RequireReturnName(value, "assign(expr)");

                return EmitTemporary(
                    $"({lvalueName} = {valueName})",
                    false
                );
            }

            if (context.lvalue() != null)
            {
                 NodeCompilationResult lvalue =
                    Visit(context.lvalue());
                
                string lvalueName =
                    RequireReturnName(lvalue, "assign(lvalue)");
                
                NodeCompilationResult value =
                    Visit(nestedExpressions[0]);

                string valueName =
                    RequireReturnName(value, "assign(expr)");
                
                string operationSymbol;
                if      (context.ADD_ASSIGN() != null) operationSymbol = "awk_add";
                else if (context.SUB_ASSIGN() != null) operationSymbol = "awk_sub";
                else if (context.MUL_ASSIGN() != null) operationSymbol = "awk_mul";
                else if (context.DIV_ASSIGN() != null) operationSymbol = "awk_div";
                else if (context.MOD_ASSIGN() != null) operationSymbol = "awk_mod";
                else if (context.POW_ASSIGN() != null) operationSymbol = "awk_pow";
                else throw new Exception("unable to match expr rule");

                return EmitTemporary(
                    $"({lvalueName} = {operationSymbol}({lvalueName}, {valueName}))",
                    false
                );
            }

            if (context.MATCH() != null)
            {
                NodeCompilationResult value = 
                    Visit(nestedExpressions[0]);
                
                string valueName = 
                    RequireReturnName(value, "MATCH");
                
                string ere = context.ERE().GetText();
                ere = ere.Trim('/').Replace("\\/", "/");
                return EmitTemporary(
                    $"awk_match({valueName}, \"{ere}\")",
                    false
                );
            }

            if (context.NO_MATCH() != null)
            {
                NodeCompilationResult value = 
                    Visit(nestedExpressions[0]);
                
                string valueName = 
                    RequireReturnName(value, "NO_MATCH");
                
                string ere = context.ERE().GetText();
                ere = ere.Trim('/').Replace("\\/", "/");
                return EmitTemporary(
                    $"awk_not(awk_match({valueName}, \"{ere}\"))",
                    false
                );
            }
        }

        if (nestedExpressions.Length == 2)
        {
            NodeCompilationResult left =
                Visit(nestedExpressions[0]);

            NodeCompilationResult right =
                Visit(nestedExpressions[1]);

            string leftName =
                RequireReturnName(left, "left expression");

            string rightName =
                RequireReturnName(right, "right expression");
                
            if (context.PLUS() != null)
                return EmitTemporary(
                    $"awk_add({leftName}, {rightName})",
                    false
                );

            if (context.MINUS() != null)
                return EmitTemporary(
                    $"awk_sub({leftName}, {rightName})",
                    false
                );

            if (context.MUL() != null)
                return EmitTemporary(
                    $"awk_mul({leftName}, {rightName})",
                    false
                );

            if (context.DIV() != null)
                return EmitTemporary(
                    $"awk_div({leftName}, {rightName})",
                    false
                );

            if (context.MOD() != null)
                return EmitTemporary(
                    $"awk_mod({leftName}, {rightName})",
                    false
                );

            if (context.POW() != null)
                return EmitTemporary(
                    $"awk_pow({leftName}, {rightName})",
                    false
                );

            if (context.EQ() != null)
                return EmitTemporary(
                    $"awk_eq({leftName}, {rightName})",
                    false
                );

            if (context.NE() != null)
                return EmitTemporary(
                    $"awk_ne({leftName}, {rightName})",
                    false
                );

            if (context.LT() != null)
                return EmitTemporary(
                    $"awk_lt({leftName}, {rightName})",
                    false
                );

            if (context.LE() != null)
                return EmitTemporary(
                    $"awk_le({leftName}, {rightName})",
                    false
                );

            if (context.GT() != null)
                return EmitTemporary(
                    $"awk_gt({leftName}, {rightName})",
                    false
                );

            if (context.GE() != null)
                return EmitTemporary(
                    $"awk_ge({leftName}, {rightName})",
                    false
                );

    
            if (context.AND() != null)
                return EmitTemporary(
                    $"awk_number(awk_is_truthy({leftName}) && awk_is_truthy({rightName}))",
                    false
                );

            if (context.OR() != null)
                return EmitTemporary(
                    $"awk_number(awk_is_truthy({leftName}) || awk_is_truthy({rightName}))",
                    false
                );

  
            return EmitTemporary(
                $"awk_concat({leftName}, {rightName})",
                true
            );
        }

        throw new NotSupportedException(
            $"To wyrażenie nie jest jeszcze obsługiwane: {context.GetText()}"
        );
    }

    public override NodeCompilationResult VisitLvalue(
        [NotNull] AwkParser.LvalueContext context
    )
    {
        // NAME
        if (context.NAME() != null && context.expr_list() == null)
        {
            string name = context.NAME().GetText();
            Symbol symbol = symbolTable.Lookup(name, currentScope)
                ?? throw new Exception($"missing symbol with name: {name} and scope: {currentScope}");
            if (symbol.NameInC is null)
                throw new Exception($"missing NameInC for symbol with name: {name} and scope: {currentScope}");
            if (symbol.Type != SymbolType.Variable && symbol.Type != SymbolType.Parameter)
                throw new Exception($"lvalue can't be of type: {symbol.Type}. Symbol with name: {name} and scope: {currentScope}");
            return new NodeCompilationResult(
                symbol.NameInC,
                CType.General
            );
        }

        // DOLLAR expr
        if (context.DOLLAR() != null)
        {
            NodeCompilationResult result = Visit(context.expr());
            string name = RequireReturnName(result, "field expr");
            return new NodeCompilationResult(
                $"fields_get(fields, awk_to_int({name}))",
                CType.General
            );
        }

        // NAME LBRACKET expr_list RBRACKET
        throw new NotSupportedException("list as an lvalue is not supported");
    }

    public void Close()
    {
        stream.Close();
    }
}
