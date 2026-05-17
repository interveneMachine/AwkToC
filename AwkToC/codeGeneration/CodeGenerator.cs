using AwkToC.Semantic;

namespace AwkToC.CodeGeneration;

class CodeGenerator : AwkBaseVisitor<NodeCompilationResult>
{
    private readonly SymbolTable symbolTable;
    private readonly CWriter stream;

    private string currentScope = "global";
    private int temporaryCounter = 0;

    public CodeGenerator(SymbolTable symbolTable, StreamWriter streamWriter)
    {
        this.symbolTable = symbolTable;
        stream = new CWriter(streamWriter);
    }

    private string NewTemporaryName()
    {
        return $"tmp{temporaryCounter++}";
    }

    private NodeCompilationResult EmitTemporary(string cExpression)
    {
        string temporaryName = NewTemporaryName();

        stream.WriteLine($"AwkValue {temporaryName} = {cExpression};");

        return new NodeCompilationResult(
            temporaryName,
            CType.General
        );
    }

    private string RequireReturnName(
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

    public override NodeCompilationResult VisitProgram(
        AwkParser.ProgramContext context
    )
    {
        stream.WriteLine("#include <stdio.h>");
        stream.WriteLine("#include \"awk_runtime.h\"");
        stream.HSpace(1);

        stream.WriteLine("int main(void)");
        stream.EnterBlock();

        VisitChildren(context);

        stream.WriteLine("return 0;");
        stream.ExitBlock();

        return new NodeCompilationResult();
    }

    public override NodeCompilationResult VisitItem(
        AwkParser.ItemContext context
    )
    {
        if (
            context.pattern() != null &&
            context.pattern().BEGIN() != null &&
            context.action() != null
        )
        {
            Visit(context.action());
            return new NodeCompilationResult();
        }

        return new NodeCompilationResult();
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


        string valuesArrayName = NewTemporaryName();

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

    public override NodeCompilationResult VisitExpr(
        AwkParser.ExprContext context
    )
    {
     
        if (context.NUMBER() != null)
        {
            return EmitTemporary(
                $"awk_number({context.NUMBER().GetText()})"
            );
        }

    
        if (context.STRING() != null)
        {
            return EmitTemporary(
                $"awk_string({context.STRING().GetText()})"
            );
        }

        AwkParser.ExprContext[] nestedExpressions = context.expr();

        if (
            context.LPAREN() != null &&
            context.RPAREN() != null &&
            nestedExpressions.Length == 1
        )
        {
            return Visit(nestedExpressions[0]);
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
                $"awk_not({valueName})"
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
                $"awk_unary_plus({valueName})"
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
                $"awk_unary_minus({valueName})"
            );
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
                    $"awk_add({leftName}, {rightName})"
                );

            if (context.MINUS() != null)
                return EmitTemporary(
                    $"awk_sub({leftName}, {rightName})"
                );

            if (context.MUL() != null)
                return EmitTemporary(
                    $"awk_mul({leftName}, {rightName})"
                );

            if (context.DIV() != null)
                return EmitTemporary(
                    $"awk_div({leftName}, {rightName})"
                );

            if (context.MOD() != null)
                return EmitTemporary(
                    $"awk_mod({leftName}, {rightName})"
                );

            if (context.POW() != null)
                return EmitTemporary(
                    $"awk_pow({leftName}, {rightName})"
                );

            if (context.EQ() != null)
                return EmitTemporary(
                    $"awk_eq({leftName}, {rightName})"
                );

            if (context.NE() != null)
                return EmitTemporary(
                    $"awk_ne({leftName}, {rightName})"
                );

            if (context.LT() != null)
                return EmitTemporary(
                    $"awk_lt({leftName}, {rightName})"
                );

            if (context.LE() != null)
                return EmitTemporary(
                    $"awk_le({leftName}, {rightName})"
                );

            if (context.GT() != null)
                return EmitTemporary(
                    $"awk_gt({leftName}, {rightName})"
                );

            if (context.GE() != null)
                return EmitTemporary(
                    $"awk_ge({leftName}, {rightName})"
                );

    
            if (context.AND() != null)
                return EmitTemporary(
                    $"awk_number(awk_is_truthy({leftName}) && awk_is_truthy({rightName}))"
                );

            if (context.OR() != null)
                return EmitTemporary(
                    $"awk_number(awk_is_truthy({leftName}) || awk_is_truthy({rightName}))"
                );

  
            return EmitTemporary(
                $"awk_concat({leftName}, {rightName})"
            );
        }

        throw new NotSupportedException(
            $"To wyrażenie nie jest jeszcze obsługiwane: {context.GetText()}"
        );
    }

    public void Close()
    {
        stream.Close();
    }
}
