using Antlr4.Runtime.Misc;
using AwkToC.Semantic;

namespace AwkToC.CodeGeneration;

class CodeGenerator : AwkBaseVisitor<NodeCompilationResult>
{
    private readonly SymbolTable symbolTable;
    private readonly CWriter stream;

    private AwkScope awkScope = new();
    private CScope cScope = new();

    /// <summary> 
    /// Stores names of labels, that are used while compiling `continue`, 
    /// null value is interpreted as missing label, 
    /// code that labels leeds to is an increment part of for loop
    /// </summary>
    private Stack<string?> continueTargets = new();

    private static ResultType Convert(CType type)
    {
        return type switch
        {
            CType.AwkValue => ResultType.General,
            CType.Int => ResultType.Int,
            CType.CString => ResultType.CString,
            _ => ResultType.General,
        };
    }

    public CodeGenerator(SymbolTable symbolTable, StreamWriter streamWriter)
    {
        this.symbolTable = symbolTable;
        stream = new CWriter(streamWriter);
    }

    private NodeCompilationResult EmitTemporary(string cExpression, bool isMemoryAllocated)
    {
        string temporaryName = symbolTable.NewTemporaryCName(cScope, isMemoryAllocated);

        stream.WriteLine($"AwkValue {temporaryName} = {cExpression};");

        return new NodeCompilationResult(
            temporaryName,
            ResultType.General
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

    private NodeCompilationResult EmitLvalue(string lvalueName, ResultType? type)
    {
        switch (type)
        {
            case ResultType.General:
            return EmitTemporary(
                $"awk_copy({lvalueName})",
                true
            );
            case ResultType.Int:
            return EmitTemporary(
                $"awk_number((double){lvalueName})",
                false
            );
            case ResultType.CString:
            return EmitTemporary(
                $"awk_string({lvalueName})",
                true
            );
            case ResultType.Array:
            return EmitTemporary(
                $"array_get_value({lvalueName})",
                true
            );
            case ResultType.Field:
            return EmitTemporary(
                $"fields_get(Fields, {lvalueName})",
                true
            );
        }
        throw new Exception("unhandled ResultType");
    }

    private void AssignToLValue(
        string lvalueName,
        ResultType? lvalueType,
        string valueName,
        ResultType? valueType
    )
    {
        switch(lvalueType, valueType)
        {
            case (ResultType.General, ResultType.General):
            stream.WriteLine($"awk_free(&{lvalueName});");
            stream.WriteLine($"{lvalueName} = awk_copy({valueName});");
            break;

            case (ResultType.General, ResultType.CString):
            stream.WriteLine($"awk_free(&{lvalueName});");
            stream.WriteLine($"{lvalueName} = awk_string({valueName});");
            break;

            case (ResultType.General, ResultType.Int):
            stream.WriteLine($"awk_free(&{lvalueName});");
            stream.WriteLine($"{lvalueName} = awk_number({valueName});");
            break;

            case (ResultType.General, ResultType.Field):
            stream.WriteLine($"awk_free(&{lvalueName});");
            stream.WriteLine($"{lvalueName} = fields_get(Fields, {valueName});");
            break;
        
            case (ResultType.Array, ResultType.General):
            stream.WriteLine($"array_set_value({lvalueName}, awk_copy({valueName}));");
            break;

            case (ResultType.Array, ResultType.CString):
            stream.WriteLine($"array_set_value({lvalueName}, awk_string({valueName}));");
            break;

            case (ResultType.Array, ResultType.Int):
            stream.WriteLine($"array_set_value({lvalueName}, awk_number({valueName}));");
            break;

            case (ResultType.Array, ResultType.Field):
            stream.WriteLine($"array_set_value({lvalueName}, fields_get(Fields, {valueName}));");
            break;

            case (ResultType.Int, ResultType.General):
            stream.WriteLine($"{lvalueName} = awk_to_int({valueName});");
            break;

            case (ResultType.Int, ResultType.CString):
            stream.WriteLine($"{lvalueName} = atoi({valueName});");
            break;

            case (ResultType.Int, ResultType.Int):
            stream.WriteLine($"{lvalueName} = {valueName};");
            break;

            case (ResultType.Int, ResultType.Field):
            var tmpResult = EmitLvalue(valueName, ResultType.Field);
            string tmpName = RequireReturnName(tmpResult, "field to general");
            stream.WriteLine($"{lvalueName} = awk_to_int({tmpName});");
            break;

            case (ResultType.CString, ResultType.General):
            stream.WriteLine($"free({lvalueName});");
            stream.WriteLine($"{lvalueName} = awk_to_string({valueName});");
            break;

            case (ResultType.CString, ResultType.CString):
            stream.WriteLine($"free({lvalueName});");
            stream.WriteLine($"{lvalueName} = awk_strdup({valueName});");
            break;

            case (ResultType.CString, ResultType.Int):
            stream.WriteLine($"free({lvalueName});");
            stream.WriteLine($"{lvalueName} = awk_to_string({{AWK_NUMBER, (double){valueName}, NULL}});");
            break;

            case (ResultType.CString, ResultType.Field):
            stream.WriteLine($"free({lvalueName});");
            stream.WriteLine($"{lvalueName} = fields_get(Fields, {valueName});");
            break;

            default:
                throw new Exception("unknown lvalue type");
        }
    }

    private void FreeTmpVariables()
    {
        foreach(CSymbol tmpSymbol in symbolTable.AllTmpVariablesInCurrentScope(cScope))
        {
            if (tmpSymbol.IsMemoryAllocated)
            {
                stream.WriteLine($"awk_free(&{tmpSymbol.Name});");
                tmpSymbol.IsMemoryAllocated = false;
            }
        }
    }

    private void FreeTmpVariablesIn(string scopeName)
    {
        foreach(CSymbol tmpSymbol in symbolTable.AllTmpVariablesIn(cScope, scopeName))
        {
            if (tmpSymbol.IsMemoryAllocated)
            {
                stream.WriteLine($"awk_free(&{tmpSymbol.Name});");
            }
        }
    }

    public override NodeCompilationResult VisitProgram(
        AwkParser.ProgramContext context
    )
    {
        stream.WriteLine("#include <stdio.h>");
        stream.WriteLine("#include <stdlib.h>");
        stream.WriteLine("#include \"awk_runtime.h\"");
        stream.HSpace(1);

        stream.WriteLine("Fields fields;");

        foreach (var variable in symbolTable.AllVariables())
        {
            string name = variable.NameInC
                ?? throw new Exception($"variable {variable.Name} in global has NameInC=null");
            if (variable.IsArray)
                stream.WriteLine($"Array* {name};");
            else
                stream.WriteLine($"AwkValue {name};");
        }

        string? begin = null, end = null;
        List<string> itemsResults = new();
        foreach(var i in context.item())
        {
            NodeCompilationResult result = Visit(i);
            if(i.pattern() is not null && i.pattern().BEGIN() is not null)
                begin = RequireReturnName(result, "begin item");
            else if(i.pattern() is not null && i.pattern().END() is not null)
                end = RequireReturnName(result, "end item");
            else if(result.Type == ResultType.ItemFunction)
                itemsResults.Add(RequireReturnName(result, "item"));
        }
        
        stream.WriteLine("int main(int argc, char* argv[])");
        stream.EnterBlock();
        symbolTable.AllVariables()
            .Where(s => s.IsArray)
            .Select(s => s.NameInC
                ?? throw new Exception($"variable {s.Name} in global has NameInC=null"))
            .ToList()
            .ForEach(name => stream.WriteLine($"{name} = array_init();"));
        stream.WriteLine("awk_set_default_predefined();");
        if (begin is not null) stream.WriteLine($"{begin}();");
        stream.WriteLine("FILE* file = fopen(argv[1], \"r\");");
        stream.WriteLine("char buffer[1024];");
        
        stream.WriteLine("while(fgets(buffer, sizeof(buffer), file))");
        stream.EnterBlock();
        stream.WriteLine("remove_newline(buffer);");
        stream.WriteLine("fields = fields_string(buffer);");
        stream.WriteLine("NR++;");
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

            cScope.EnterFunction(itemName);

            if (!isSpecial)
            {
                stream.WriteLine($"if({patternName}())");
                stream.EnterBlock();
            }

            Visit(context.action());

            FreeTmpVariables();
            if (!isSpecial) stream.ExitBlock();
            cScope.ExitFunction();
            stream.ExitBlock();

            return new NodeCompilationResult(
                itemName,
                ResultType.ItemFunction
            );
        }

        // FUNCTION NAME ( param_list_opt ) newline_opt action
        if (context.FUNCTION() is not null)
        {
            string name = context.NAME().GetText();
            Symbol function = symbolTable.Lookup(name, awkScope)
                ?? throw new ArgumentException($"function {name} is missing from symbolTable");
            string functionName = function.NameInC
                ?? throw new Exception($"function {name} has NameInC=null");
            awkScope.EnterFunction(name);
            cScope.EnterFunction(name);

            List<string> parameters = new();
            if (context.param_list_opt() != null &&
                context.param_list_opt().param_list() != null)
                foreach(var parameter in context.param_list_opt().param_list().NAME())
                {
                    Symbol parameterSymbol = symbolTable.Lookup(parameter.GetText(), awkScope)
                        ?? throw new Exception($"symbolTable is missing symbol for {parameter.GetText()} in {awkScope.GetScope()}");
                    if(parameterSymbol.NameInC == null)
                        throw new Exception($"symbol with name: {parameterSymbol.Name} scope: {awkScope.GetScope()} is missing NameInC");
                    parameters.Add($"AwkValue {parameterSymbol.NameInC}");
                }
            string parameterNames = string.Join(", ", parameters);
            stream.WriteLine($"AwkValue {functionName}({parameterNames})");
            stream.EnterBlock();

            Visit(context.action());
            FreeTmpVariables();
            if (!function.Returns) stream.WriteLine("return awk_undefined();");
            stream.ExitBlock();
            awkScope.ExitFunction();
            cScope.ExitFunction();
            
            return new NodeCompilationResult();
        }

        // action
        if (context.action() is not null)
        {
            string itemNameAction = symbolTable.NewItemCName();
            stream.WriteLine($"void {itemNameAction}()");
            stream.EnterBlock();
            cScope.EnterFunction(itemNameAction);

            Visit(context.action());

            FreeTmpVariables();
            cScope.ExitFunction();
            stream.ExitBlock();
            return new NodeCompilationResult(
                itemNameAction,
                ResultType.ItemFunction
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
            cScope.EnterFunction(itemNameSimplePattern);

            stream.WriteLine($"if({simplePatternName}())");
            stream.EnterBlock();

            stream.WriteLine("awk_print_value(fields_get(fields, 0));");

            stream.ExitBlock();
            FreeTmpVariables();
            cScope.ExitFunction();
            stream.ExitBlock();
            return new NodeCompilationResult(
                itemNameSimplePattern,
                ResultType.ItemFunction
            );
        }
        throw new Exception("unrecognised item rule");
    }

    public override NodeCompilationResult VisitPattern([NotNull] AwkParser.PatternContext context)
    {
        string patternName = symbolTable.NewPatternCName();
        stream.WriteLine($"int {patternName}()");
        stream.EnterBlock();
        cScope.EnterFunction(patternName);
        
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
            string returnValueCName = symbolTable.AddCName("returnValue");
            string exprName = RequireReturnName(exprResult, "expr");
            stream.WriteLine($"int {returnValueCName} = awk_is_truthy({exprName});");
            FreeTmpVariables();
            stream.WriteLine($"return {returnValueCName};");
        }

        cScope.ExitFunction();
        stream.ExitBlock();
        return new NodeCompilationResult(
            patternName,
            ResultType.Function
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


        string valuesArrayName = symbolTable.NewTemporaryCName(cScope, false);

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

    public override NodeCompilationResult VisitTerminated_statement([NotNull] AwkParser.Terminated_statementContext context)
    {
        // IF LPAREN expr RPAREN newline_opt terminated_statement
        // IF LPAREN expr RPAREN newline_opt terminated_statement ELSE newline_opt terminated_statement
        if (context.IF() != null)
        {
            NodeCompilationResult conditionResults = Visit(context.expr());
            string conditionName = RequireReturnName(conditionResults, "expr");
            stream.WriteLine($"if(awk_is_truthy({conditionName}))");
            stream.EnterBlock();
            cScope.EnterIf(context.Start.Line, context.Start.Column);

            Visit(context.terminated_statement()[0]);

            FreeTmpVariables();
            cScope.ExitIf();
            stream.ExitBlock();

            // ELSE newline_opt terminated_statement
            if (context.ELSE() != null)
            {
                stream.WriteLine("else");
                stream.EnterBlock();
                cScope.EnterElse(context.Start.Line, context.Start.Column);

                Visit(context.terminated_statement()[1]);

                FreeTmpVariables();
                cScope.ExitElse();
                stream.ExitBlock();
            }
            return new NodeCompilationResult();
        }

        // WHILE LPAREN expr RPAREN newline_opt terminated_statement
        if (context.WHILE() != null)
        {
            stream.WriteLine("while(1)");
            stream.EnterBlock();
            cScope.EnterWhile(context.Start.Line, context.Start.Column);

            cScope.EnterCondition(context.Start.Line, context.Start.Column);
            NodeCompilationResult exprResult = Visit(context.expr());
            string exprName = RequireReturnName(exprResult, "expr");
            stream.WriteLine($"if(!awk_is_truthy({exprName}))");
            stream.EnterBlock();
            FreeTmpVariablesIn("while");
            stream.WriteLine("break;");
            stream.ExitBlock();

            continueTargets.Push(null);
            Visit(context.terminated_statement()[0]);

            FreeTmpVariables();
            cScope.ExitWhile();
            stream.ExitBlock();
            continueTargets.Pop();
            return new NodeCompilationResult();
        }

        // FOR LPAREN NAME IN NAME RPAREN newline_opt terminated_statement
        if (context.IN() != null)
        {
            string text = context.NAME()[1].GetText();
            Symbol arraySymbol = symbolTable.Lookup(text, awkScope)
                ?? throw new Exception($"[{context.Start.Line}:{context.Start.Column}] Missing array {text} from symbol table");
            string arrayNameInC = arraySymbol.NameInC
                ?? throw new Exception($"[{context.Start.Line}:{context.Start.Column}] Array {text} is missing NameInC parameter");
            if (!arraySymbol.IsArray)
                throw new Exception($"[{context.Start.Line}:{context.Start.Column}] Symbol {text} is used in array context");
            
            text = context.NAME()[0].GetText();
            Symbol keySymbol = symbolTable.Lookup(text, awkScope)
                ?? throw new Exception($"[{context.Start.Line}:{context.Start.Column}] Missing variable {text} from symbol table");
            string keyNameInC = keySymbol.NameInC
                ?? throw new Exception($"[{context.Start.Line}:{context.Start.Column}] Variable {text} is missing NameInC parameter");
            if (keySymbol.Type != SymbolType.Variable)
                throw new Exception($"[{context.Start.Line}:{context.Start.Column}] Symbol {text} is {keySymbol.Type}, expected Variable");
            if (keySymbol.IsArray)
                throw new Exception($"[{context.Start.Line}:{context.Start.Column}] Array {text} is used in variable context");

            string iteratorName = symbolTable.NewTemporaryCName(cScope, false);
            stream.WriteLine($"ArrayIterator {iteratorName} = arrayiterator_init({arrayNameInC});");
            
            stream.WriteLine("while(1)");
            stream.EnterBlock(); cScope.EnterWhile(context.Start.Line, context.Start.Column);

            stream.WriteLine($"if(arrayiterator_is_end(&{iteratorName}))");
            stream.EnterBlock();
            FreeTmpVariablesIn("while");
            stream.WriteLine("break;");
            stream.ExitBlock();

            AssignToLValue(keyNameInC, Convert(keySymbol.TypeInC), $"{iteratorName}.entry->key", ResultType.CString);

            continueTargets.Push(symbolTable.NewContinueTarget());
            Visit(context.terminated_statement()[0]);

            FreeTmpVariables();

            // incr instruction in for loop, continue jumps to this label with goto
            stream.WriteLine($"{continueTargets.Peek()}:");
            stream.WriteLine($"arrayiterator_next(&{iteratorName});");

            stream.ExitBlock(); cScope.ExitWhile();
            continueTargets.Pop();
            return new NodeCompilationResult();
        }

        // FOR LPAREN simple_statement? SEMICOLON expr? SEMICOLON simple_statement? RPAREN newline_opt terminated_statement
        if (context.FOR() != null &&
            context.simple_statement() != null)
        {
            if (context.simple_statement()[0] != null)
                Visit(context.simple_statement()[0]);
            stream.WriteLine("while(1)");
            stream.EnterBlock();
            cScope.EnterWhile(context.Start.Line, context.Start.Column);

            if (context.expr() != null)
            {
                NodeCompilationResult exprResult = Visit(context.expr());
                string exprName = RequireReturnName(exprResult, "expr");
                stream.WriteLine($"if(!awk_is_truthy({exprName}))");
                stream.EnterBlock();
                FreeTmpVariablesIn("while");
                stream.WriteLine("break;");
                stream.ExitBlock();
            }
            continueTargets.Push(symbolTable.NewContinueTarget());
            Visit(context.terminated_statement()[0]);
            
            FreeTmpVariables();

            if (context.simple_statement()[1] != null)
            {
                // incr instruction in for loop, continue jumps to this label with goto
                stream.WriteLine($"{continueTargets.Peek()}:");
                Visit(context.simple_statement()[1]);
                FreeTmpVariables();
            }

            continueTargets.Pop();
            stream.ExitBlock();
            cScope.ExitWhile();
            return new NodeCompilationResult();
        }

        // action newline_opt
        if (context.action() != null)
        {
            Visit(context.action());
            return new NodeCompilationResult();
        }

        // terminatable_statement NEWLINE newline_opt
        // terminatable_statement SEMICOLON newline_opt
        if (context.terminatable_statement() != null)
        {
            Visit(context.terminatable_statement());
            return new NodeCompilationResult();
        }

        // SEMICOLON newline_opt
        if (context.SEMICOLON() != null)
            return new NodeCompilationResult();
        
        throw new Exception("unrecognised terminated_statement rule");
    }

    public override NodeCompilationResult VisitTerminatable_statement(
        AwkParser.Terminatable_statementContext context
    )
    {
        // BREAK
        if (context.BREAK() != null)
        {
            FreeTmpVariablesIn("while");
            stream.WriteLine("break;");
            return new NodeCompilationResult();
        }

        // CONTINUE
        if (context.CONTINUE() != null)
        {
            FreeTmpVariablesIn("while");
            if (continueTargets.Peek() != null)
                stream.WriteLine($"goto {continueTargets.Peek()};");
            else
                stream.WriteLine("continue;");
            return new NodeCompilationResult();
        }

        // return 
        // return expr 
        if (context.RETURN() != null)
        {
            if (!awkScope.InFunction())
            {
                throw new ArgumentException(
                    "'return' can be used only inside a function."
                );
            }

            AwkParser.ExprContext? returnExpression =
                context.expr();

            // return
            if (returnExpression == null)
            {
                FreeTmpVariablesIn("function");
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
            NodeCompilationResult returnResult =
                EmitTemporary(
                    $"awk_copy({expressionName})",
                    false
                );
            string returnName =
                RequireReturnName(
                    returnResult,
                    "return expression"
                );
            
            FreeTmpVariablesIn("function");

            stream.WriteLine($"return {returnName};");

            return new NodeCompilationResult();
        }

        if (context.simple_statement() != null)
        {
            return Visit(context.simple_statement());
        }

        if (context.DO() != null)
        {
            stream.WriteLine("while(1)");
            stream.EnterBlock();
            cScope.EnterWhile(context.Start.Line, context.Start.Column);
            continueTargets.Push(null);

            Visit(context.terminated_statement());
            FreeTmpVariables();

            cScope.EnterCondition(context.Start.Line, context.Start.Column);
            var exprResult = Visit(context.expr());
            string exprName = RequireReturnName(
                exprResult,
                "expr"
            );
            stream.WriteLine($"if(!awk_is_truthy({exprName}))");
            stream.EnterBlock();
            FreeTmpVariablesIn("while");
            stream.WriteLine("break;");
            stream.ExitBlock();
            cScope.ExitCondition();

            continueTargets.Pop();
            cScope.ExitWhile();
            stream.ExitBlock();
            return new NodeCompilationResult();
        }

        throw new Exception("unrecognised rule");
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

    private List<AwkParser.ExprContext> CollectListArguments(
        AwkParser.Expr_listContext context
    )
    {
        List<AwkParser.ExprContext> arguments = new();
        CollectExprList(context, arguments);
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
                true
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
                symbolTable.Lookup(functionName, awkScope)
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
                true
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

            NodeCompilationResult result =
                EmitTemporary(
                    $"fields_get(fields, 0)",
                    true
                );
            string resultName = 
                RequireReturnName(result, "ere");

            return EmitTemporary(
                $"awk_match({resultName}, \"{ere}\")",
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
            {
                return EmitLvalue(lvalueName, lvalue.Type);
            }

            switch (lvalue.Type)
            {
            case ResultType.Int:
                // lvalue INCR
                if (context.GetChild(1).GetText() == "++")
                    return EmitTemporary(
                        $"awk_number((double){lvalueName}++)",
                        false
                    );
                // INCR lvalue
                if (context.GetChild(0).GetText() == "++")
                    return EmitTemporary(
                        $"awk_number((double)++{lvalueName})",
                        false
                    );
                // lvalue DECR
                if (context.GetChild(1).GetText() == "--")
                    return EmitTemporary(
                        $"awk_number((double){lvalueName}--)",
                        false
                    );
                // DECR lvalue
                if (context.GetChild(0).GetText() == "--")
                    return EmitTemporary(
                        $"awk_number((double)--{lvalueName})",
                        false
                    );
                throw new Exception("unknown rule");
            
            case ResultType.CString:
                throw new Exception("unable to add to char*"); // TODO test how it works in other interpreters
            }
            
            NodeCompilationResult result = EmitLvalue(
                lvalueName,
                lvalue.Type
            );
            string resultName =
                RequireReturnName(result, "lvalue");
            
            if (lvalue.Type == ResultType.Array)
            {
                if (context.GetChild(1).GetText() == "++")
                {
                    stream.WriteLine($"awk_set_value({lvalueName}, awk_add({resultName}, awk_number(1)));");
                    return new NodeCompilationResult(
                        resultName,
                        ResultType.General
                    );
                }

                if (context.GetChild(1).GetText() == "--")
                {
                    stream.WriteLine($"awk_set_value({lvalueName}, awk_sub({resultName}, awk_number(1)));");
                    return new NodeCompilationResult(
                        resultName,
                        ResultType.General
                    );
                }

                if (context.GetChild(0).GetText() == "++")
                {
                    var valueResult = EmitTemporary(
                        $"awk_add({resultName}, awk_number(1))",
                        false
                    );
                    var valueName = RequireReturnName(
                        valueResult,
                        "incr lvalue"
                    );
                    stream.WriteLine($"awk_set_value({lvalueName}, {valueName});");
                    return new NodeCompilationResult(
                        valueName,
                        ResultType.General
                    );
                }

                if (context.GetChild(0).GetText() == "--")
                {
                    var valueResult = EmitTemporary(
                        $"awk_sub({resultName}, awk_number(1))",
                        false
                    );
                    var valueName = RequireReturnName(
                        valueResult,
                        "incr lvalue"
                    );
                    stream.WriteLine($"awk_set_value({lvalueName}, {valueName});");
                    return new NodeCompilationResult(
                        valueName,
                        ResultType.General
                    );
                }
            }
            
            stream.WriteLine($"awk_free(&{lvalueName});");
            
            // INCR lvalue
            if (context.GetChild(0).GetText() == "++")
            {
                return EmitTemporary(
                    $"({lvalueName} = awk_add({resultName}, awk_number(1)))",
                    false
                );
            }

            // DECR lvalue
            else if (context.GetChild(0).GetText() == "--")
            {
                return EmitTemporary(
                    $"({lvalueName} = awk_sub({resultName}, awk_number(1)))",
                    false
                );
            }

            // lvalue INCR
            else if (context.GetChild(1).GetText() == "++")
            {
                stream.WriteLine(
                    $"{lvalueName} = awk_add({resultName}, awk_number(1));"
                );
                return result;
            }

            // lvalue DECR
            else if (context.GetChild(1).GetText() == "--")
            {
                stream.WriteLine(
                    $"{lvalueName} = awk_sub({resultName}, awk_number(1));"
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
                
                if (lvalue.Type == ResultType.Array)
                {
                    stream.WriteLine($"array_set_value({lvalueName}, awk_copy({valueName}));");
                    return new NodeCompilationResult(
                        valueName,
                        ResultType.General
                    );
                }

                stream.WriteLine($"awk_free(&{lvalueName});");

                return EmitTemporary(
                    $"awk_copy({lvalueName} = awk_copy({valueName}))",
                    true
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
                    $"awk_copy({lvalueName} = {operationSymbol}({lvalueName}, {valueName}))",
                    true
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
            Symbol symbol = symbolTable.Lookup(name, awkScope)
                ?? throw new Exception($"missing symbol with name: {name}");
            if (symbol.NameInC is null)
                throw new Exception($"missing NameInC for symbol with name: {name}");
            if (symbol.Type != SymbolType.Variable && symbol.Type != SymbolType.Parameter)
                throw new Exception($"lvalue can't be of type: {symbol.Type}. Symbol with name: {name}"); // TODO improve error
            var type = symbol.TypeInC switch
            {
                CType.Int => ResultType.Int,
                CType.CString => ResultType.CString,
                _ => ResultType.General,
            };
            return new NodeCompilationResult(
                symbol.NameInC,
                type
            );
        }

        // DOLLAR expr
        if (context.DOLLAR() != null)
        {
            NodeCompilationResult result = Visit(context.expr());
            string name = RequireReturnName(result, "field expr");
            var idResult = EmitTemporary(
                $"awk_to_int({name})",
                false
            );
            string idName = RequireReturnName(idResult, "convert to int");
            return new NodeCompilationResult(
                idName,
                ResultType.Field
            );
        }

        // NAME LBRACKET expr_list RBRACKET
        if (context.LBRACKET() != null)
        {
            string text = context.NAME().GetText();
            Symbol array = symbolTable.Lookup(text, awkScope)
                ?? throw new Exception($"[{context.Start.Line}:{context.Start.Column}] Missing array {text} from symbol table");
            string nameInC = array.NameInC
                ?? throw new Exception($"[{context.Start.Line}:{context.Start.Column}] Array {text} is missing NameInC parameter");
            if (!array.IsArray)
                throw new Exception($"[{context.Start.Line}:{context.Start.Column}] Symbol {text} is used in array context");

            List<AwkParser.ExprContext> exprContexts = CollectListArguments(context.expr_list());
            List<string> exprNames = [];
            foreach (var exprContext in exprContexts)
            {
                var exprResult = Visit(exprContext);
                exprNames.Add(RequireReturnName(exprResult, "array argument expr"));
            }
            string argumentName = symbolTable.NewTemporaryCName(cScope, false);
            stream.WriteLine($"AwkValue {argumentName}[] = {{ {string.Join(", ", exprNames)} }};");
            var concatenatedArgumentResult = EmitTemporary(
                $"awk_concat_array_arg({exprNames.Count}, {argumentName})",
                true
            );
            string concatenatedArgumentName = RequireReturnName(
                concatenatedArgumentResult,
                "concatenation of array arguments"
            );
            return new NodeCompilationResult(
                $"{nameInC}, {concatenatedArgumentName}",
                ResultType.Array
            );
        }

        throw new Exception("unknown lvalue rule");
    }

    public void Close()
    {
        stream.Close();
    }
}
