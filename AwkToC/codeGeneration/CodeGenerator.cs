using Antlr4.Runtime.Misc;
using AwkToC.Semantic;
using AwkToC.Exceptions;

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
                $"fields_get(fields, {lvalueName})",
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

        stream.WriteLine("int isBegin;");
        stream.WriteLine("Fields fields;");

        foreach (var variable in symbolTable.AllVariables())
        {
            string name = variable.NameInC
                ?? throw new MissingNameInCException(variable, context);
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
        if (itemsResults.Count != 0)
        {
            stream.WriteLine("if (argc != 2)");
            stream.EnterBlock();
            stream.WriteLine("fprintf(stderr, \"Expected 1 argument <filename>, got %d arguments\\n\", argc-1);");
            stream.WriteLine("return 1;");
            stream.ExitBlock();
        }
        else
        {
            stream.WriteLine("if (argc > 2)");
            stream.EnterBlock();
            stream.WriteLine("fprintf(stderr, \"Expected 1 or 0 arguments, got %d arguments\\n\", argc-1);");
            stream.WriteLine("return 1;");
            stream.ExitBlock();
        }
        symbolTable.AllVariables()
            .Where(s => s.IsArray)
            .Select(s => s.NameInC
                ?? throw new MissingNameInCException(s, context))
            .ToList()
            .ForEach(name => stream.WriteLine($"{name} = array_init();"));
        stream.WriteLine("awk_set_default_predefined();");
        stream.WriteLine("isBegin = 1;");

        if (begin is not null) stream.WriteLine($"{begin}();");

        if (itemsResults.Count != 0)
        {
            stream.WriteLine("FILE* file = fopen(argv[1], \"r\");");
            stream.WriteLine("if (file == NULL)");
            stream.EnterBlock();
            stream.WriteLine("fprintf(stderr, \"Failed to open file %s\\n\", argv[1]);");
            stream.WriteLine("return 1;");
            stream.ExitBlock();
            stream.WriteLine("char buffer[1024];");

            stream.WriteLine("while(fgets(buffer, sizeof(buffer), file))");
            stream.EnterBlock();
            stream.WriteLine("remove_newline(buffer);");
            stream.WriteLine("fields = fields_string(buffer);");
            stream.WriteLine("NR++;");
            itemsResults.Select(function => $"{function}();")
                        .ToList().ForEach(stream.WriteLine);
            stream.WriteLine("fields_free(&fields);");
            stream.WriteLine("isBegin = 0;");
            stream.ExitBlock();
        }

        if (end is not null) stream.WriteLine($"{end}();");
        if (itemsResults.Count != 0) stream.WriteLine("fclose(file);");
        symbolTable.AllVariables()
            .Where(symbol => symbol.IsArray)
            .Select(symbol => symbol.NameInC
                              ?? throw new MissingNameInCException(symbol, context))
            .ToList()
            .ForEach(name => stream.WriteLine($"array_free({name});"));
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
                ?? throw new MissingFromSymbolTableException(name, context);
            string functionName = function.NameInC
                ?? throw new MissingNameInCException(function, context);
            awkScope.EnterFunction(name);
            cScope.EnterFunction(name);

            List<string> parameters = new();
            if (context.param_list_opt() != null &&
                context.param_list_opt().param_list() != null)
                foreach(var parameter in context.param_list_opt().param_list().NAME())
                {
                    Symbol parameterSymbol = symbolTable.Lookup(parameter.GetText(), awkScope)
                        ?? throw new MissingFromSymbolTableException(parameter.GetText(), context);
                    if(parameterSymbol.NameInC == null)
                        throw new MissingNameInCException(parameterSymbol, context);
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
            string simplePatternName = RequireReturnName(simplePatternResult, "simple_pattern");

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
        throw new InvalidRuleException("item", context);
    }

    public override NodeCompilationResult VisitPattern([NotNull] AwkParser.PatternContext context)
    {
        string patternName = symbolTable.NewPatternCName();
        stream.WriteLine($"int {patternName}()");
        stream.EnterBlock();
        cScope.EnterFunction(patternName);

        // expr ',' newline_opt expr
        if (context.COMMA() is not null)
        {
            string returnValueCName = symbolTable.AddCName("returnValue");
            stream.WriteLine($"static int {returnValueCName};");
            
            stream.WriteLine($"if (isBegin || !{returnValueCName})");
            stream.EnterBlock();
            NodeCompilationResult exprResult = Visit(context.expr()[0]);
            string exprName = RequireReturnName(exprResult, "expr");
            stream.WriteLine($"{returnValueCName} = awk_is_truthy({exprName});");
            FreeTmpVariables();
            stream.WriteLine($"return {returnValueCName};");
            stream.ExitBlock();

            stream.WriteLine($"if ({returnValueCName})");
            stream.EnterBlock();
            exprResult = Visit(context.expr()[1]);
            exprName = RequireReturnName(exprResult, "expr");
            stream.WriteLine($"{returnValueCName} = !awk_is_truthy({exprName});");
            FreeTmpVariables();
            stream.WriteLine($"return {returnValueCName};");
            stream.ExitBlock();
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

    public override NodeCompilationResult VisitOutput_redirection([NotNull] AwkParser.Output_redirectionContext context)
    {
        var exprResult = Visit(context.expr());
        string exprName = RequireReturnName(exprResult, "expr");

        if (context.GT() != null)
        {
            return new NodeCompilationResult(
                $"awk_output_redirection_write({exprName}), 1",
                ResultType.File
            );
        }
        if (context.APPEND() != null)
        {
            return new NodeCompilationResult(
                $"awk_output_redirection_append({exprName}), 1",
                ResultType.File
            );
        }
        if (context.PIPE() != null)
        {
            return new NodeCompilationResult(
                $"awk_output_redirection_pipe({exprName}), 2",
                ResultType.File
            );
        }
        throw new InvalidRuleException("output_redirection", context);
    }

    public override NodeCompilationResult VisitPrint_statement([NotNull] AwkParser.Print_statementContext context)
    {
        var simple_print_statement = context.simple_print_statement();
        string cstream;
        if (context.output_redirection() != null)
        {
            var redirectionResult = Visit(context.output_redirection());
            cstream = RequireReturnName(redirectionResult, "output_redirection");
        }
        else cstream = "stdout, 0";

        if (simple_print_statement.PRINT() == null)
        {
            // TODO
            throw new NotSupportedException(
                "obsługiwane jest tylko 'print', bez 'printf'."
            );
        }

        var expressionList =
            simple_print_statement.print_expr_list_opt()?.print_expr_list();

        if (expressionList == null)
        {
            var fieldResult = EmitTemporary(
                $"fields_get(fields, 0)",
                true
            );
            string fieldName = RequireReturnName(fieldResult, "fields_get");
            stream.WriteLine($"awk_print_value({fieldName}, {cstream});");
            return new NodeCompilationResult();
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
                $"awk_print_value({compiledExpressionNames[0]}, {cstream});"
            );

            return new NodeCompilationResult();
        }


        string valuesArrayName = symbolTable.NewTemporaryCName(cScope, false);

        stream.WriteLine(
            $"AwkValue {valuesArrayName}[] = {{ {string.Join(", ", compiledExpressionNames)} }};"
        );

        stream.WriteLine(
            $"awk_print_values({compiledExpressionNames.Count}, {valuesArrayName}, {cstream});"
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
                ?? throw new MissingFromSymbolTableException(text, context);
            string arrayNameInC = arraySymbol.NameInC
                ?? throw new MissingNameInCException(arraySymbol, context);
            if (!arraySymbol.IsArray)
                throw new WrongTypeException(arraySymbol, "array", context);
            
            text = context.NAME()[0].GetText();
            Symbol keySymbol = symbolTable.Lookup(text, awkScope)
                ?? throw new MissingFromSymbolTableException(text, context);
            string keyNameInC = keySymbol.NameInC
                ?? throw new MissingNameInCException(keySymbol, context);
            if (keySymbol.Type != SymbolType.Variable || keySymbol.IsArray)
                throw new WrongTypeException(keySymbol, "variable", context);

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
        
        throw new InvalidRuleException("terminated_statement", context);
    }
    public override NodeCompilationResult VisitSimple_statement(
    AwkParser.Simple_statementContext context
)
{
    /*
     * delete arr[key]
     * delete arr
     */
    if (context.DELETE() != null)
    {
        string arrayName = context.NAME().GetText();

        Symbol arraySymbol = symbolTable.Lookup(arrayName, awkScope)
            ?? throw new MissingFromSymbolTableException(arrayName, context);

        string arrayNameInC = arraySymbol.NameInC
            ?? throw new MissingNameInCException(arraySymbol, context);

        if (!arraySymbol.IsArray)
        {
            throw new WrongTypeException(arraySymbol, "array", context);
        }

        /*
         * delete arr[key]
         */
        if (context.LBRACKET() != null)
        {
            var exprList = context.expr_list()
                ?? throw new InvalidRuleException("simple_statement", context);

            List<AwkParser.ExprContext> keyExpressions =
                CollectListArguments(exprList);

            List<string> keyExpressionNames = new();

            foreach (var keyExpression in keyExpressions)
            {
                NodeCompilationResult keyResult = Visit(keyExpression);

                keyExpressionNames.Add(
                    RequireReturnName(keyResult, "array key expression")
                );
            }

            string keyArrayName =
                symbolTable.NewTemporaryCName(cScope, false);

            stream.WriteLine(
                $"AwkValue {keyArrayName}[] = {{ {string.Join(", ", keyExpressionNames)} }};"
            );

            NodeCompilationResult concatenatedKeyResult =
                EmitTemporary(
                    $"awk_concat_array_arg({keyExpressionNames.Count}, {keyArrayName})",
                    true
                );

            string concatenatedKeyName =
                RequireReturnName(
                    concatenatedKeyResult,
                    "concatenated array key"
                );

            stream.WriteLine(
                $"array_delete_value({arrayNameInC}, {concatenatedKeyName});"
            );

            return new NodeCompilationResult();
        }

        /*
         * delete arr
         */
        stream.WriteLine($"array_delete({arrayNameInC});");

        return new NodeCompilationResult();
    }

    return VisitChildren(context);
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
                throw new CompilationException("return used outside function context", context);

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

        throw new InvalidRuleException("terminatable_statement", context);
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
                throw new InvalidRuleException("multiple_expr_list", context);

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
                ?? throw new MissingFromSymbolTableException(functionName, context);

            if (functionSymbol.Type != SymbolType.Function)
                throw new WrongTypeException(functionSymbol, "function", context);

            string functionNameInC = functionSymbol.NameInC
                ?? throw new MissingNameInCException(functionSymbol, context);

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

            if (lvalue.Type == ResultType.Int)
            {
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
                throw new InvalidRuleException("expr", context);
            }
            
            NodeCompilationResult result = EmitLvalue(
                lvalueName,
                lvalue.Type
            );
            string resultName =
                RequireReturnName(result, "lvalue");
            
            if (context.GetChild(1).GetText() == "++")
            {
                AssignToLValue(
                    lvalueName,
                    lvalue.Type,
                    $"awk_add({resultName}, awk_number(1))",
                    ResultType.General
                );
                return new NodeCompilationResult(
                    resultName,
                    ResultType.General
                );
            }

            if (context.GetChild(1).GetText() == "--")
            {
                AssignToLValue(
                    lvalueName,
                    lvalue.Type,
                    $"awk_sub({resultName}, awk_number(1))",
                    ResultType.General
                );
                return new NodeCompilationResult(
                    resultName,
                    ResultType.General
                );
            }

            if (context.GetChild(0).GetText() == "++")
            {
                AssignToLValue(
                    lvalueName,
                    lvalue.Type,
                    $"awk_add({resultName}, awk_number(1))",
                    ResultType.General
                );
                return EmitLvalue(
                    lvalueName,
                    ResultType.General
                );
            }

            if (context.GetChild(0).GetText() == "--")
            {
                AssignToLValue(
                    lvalueName,
                    lvalue.Type,
                    $"awk_sub({resultName}, awk_number(1))",
                    ResultType.General
                );
                return EmitLvalue(
                    lvalueName,
                    ResultType.General
                );
            }

            throw new InvalidRuleException("expr", context);
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
            // lvalue ASSIGN expr
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
                
                AssignToLValue(lvalueName, lvalue.Type, valueName, ResultType.General);
                return new NodeCompilationResult(
                    valueName,
                    ResultType.General
                );
            }
            // lvalue ADD_ASSIGN expr
            // lvalue SUB_ASSIGN expr
            // lvalue MUL_ASSIGN expr
            // lvalue DIV_ASSIGN expr
            // lvalue MOD_ASSIGN expr
            // lvalue POW_ASSIGN expr
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
                else throw new InvalidRuleException("expr", context);

                var generalLvalueResult = EmitLvalue(lvalueName, lvalue.Type);
                string generalLvalueName = RequireReturnName(generalLvalueResult, "emit tmp lvalue");

                AssignToLValue(
                    lvalueName,
                    lvalue.Type,
                    $"{operationSymbol}({generalLvalueName}, {valueName})",
                    ResultType.General
                );

                return EmitLvalue(lvalueName, lvalue.Type);
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

        throw new InvalidRuleException("expr", context);
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
                ?? throw new MissingFromSymbolTableException(name, context);
            if (symbol.NameInC is null)
                throw new MissingNameInCException(symbol, context);
            if (symbol.Type != SymbolType.Variable && symbol.Type != SymbolType.Parameter)
                throw new WrongTypeException(symbol, "one of function, variable, parameter, array", context);
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
            string idName = symbolTable.NewTemporaryCName(cScope, false);
            stream.WriteLine($"int {idName} = awk_to_int({name});");
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
                ?? throw new MissingFromSymbolTableException(text, context);
            string nameInC = array.NameInC
                ?? throw new MissingNameInCException(array, context);
            if (!array.IsArray)
                throw new WrongTypeException(array, "array", context);

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

        throw new InvalidRuleException("lvalue", context);
    }

    public void Close()
    {
        stream.Close();
    }
}
