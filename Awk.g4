grammar Awk;

// --- Parser Rules ---

program
    : item_list
    | item_list item
    ;

item_list
    : /* empty */
    | item_list item terminator
    ;

item
    : action
    | pattern action
    | FUNCTION NAME '(' param_list_opt ')' newline_opt action
    ;

param_list_opt
    : /* empty */
    | param_list
    ;

param_list
    : NAME
    | param_list ',' NAME
    ;

pattern
    : BEGIN
    | END
    | expr
    | expr ',' newline_opt expr
    ;

action
    : LBRACE newline_opt RBRACE
    | LBRACE newline_opt terminated_statement_list RBRACE
    | LBRACE newline_opt unterminated_statement_list RBRACE
    ;

terminator
    : terminator NEWLINE
    | ';'
    | NEWLINE
    ;

terminated_statement_list
    : terminated_statement
    | terminated_statement_list terminated_statement
    ;

unterminated_statement_list
    : unterminated_statement
    | terminated_statement_list unterminated_statement
    ;

terminated_statement
    : action newline_opt
    | IF LPAREN expr RPAREN newline_opt terminated_statement
    | IF LPAREN expr RPAREN newline_opt terminated_statement ELSE newline_opt terminated_statement
    | WHILE LPAREN expr RPAREN newline_opt terminated_statement
    | FOR LPAREN simple_statement_opt SEMICOLON expr_opt SEMICOLON simple_statement_opt RPAREN newline_opt terminated_statement
    | FOR LPAREN NAME IN NAME RPAREN newline_opt terminated_statement
    | SEMICOLON newline_opt
    | terminatable_statement NEWLINE newline_opt
    | terminatable_statement SEMICOLON newline_opt
    ;

unterminated_statement
    : terminatable_statement
    | IF LPAREN expr RPAREN newline_opt unterminated_statement
    | IF LPAREN expr RPAREN newline_opt terminated_statement ELSE newline_opt unterminated_statement
    | WHILE LPAREN expr RPAREN newline_opt unterminated_statement
    | FOR LPAREN simple_statement_opt SEMICOLON expr_opt SEMICOLON simple_statement_opt RPAREN newline_opt unterminated_statement
    | FOR LPAREN NAME IN NAME RPAREN newline_opt unterminated_statement
    ;

terminatable_statement
    : simple_statement
    | BREAK
    | CONTINUE
    | NEXT
    | NEXTFILE
    | EXIT expr_opt
    | RETURN expr_opt
    | DO newline_opt terminated_statement WHILE LPAREN expr RPAREN
    ;

simple_statement_opt
    : /* empty */
    | simple_statement
    ;

simple_statement
    : DELETE NAME LBRACKET expr_list RBRACKET
    | DELETE NAME
    | expr
    | print_statement
    ;

print_statement
    : simple_print_statement
    | simple_print_statement output_redirection
    ;

simple_print_statement
    : PRINT print_expr_list_opt
    | PRINT LPAREN multiple_expr_list RPAREN
    | PRINTF print_expr_list
    | PRINTF LPAREN multiple_expr_list RPAREN
    ;

output_redirection
    : GT expr
    | APPEND expr
    | PIPE expr
    ;

expr_list_opt
    : /* empty */
    | expr_list
    ;

expr_list
    : expr
    | multiple_expr_list
    ;

multiple_expr_list
    : expr COMMA newline_opt expr
    | multiple_expr_list COMMA newline_opt expr
    ;

expr_opt
    : /* empty */
    | expr
    ;

// Expression hierarchy with proper precedence (lowest to highest)
// Using ANTLR4 left-recursive rules with precedence
expr
    : expr POW expr                          # powExpr
    | INCR expr                               # preIncrExpr
    | DECR expr                               # preDecrExpr
    | NOT expr                                # notExpr
    | PLUS expr                               # unaryPlusExpr
    | MINUS expr                              # unaryMinusExpr
    | expr INCR                               # postIncrExpr
    | expr DECR                               # postDecrExpr
    | expr MUL expr                           # mulExpr
    | expr DIV expr                           # divExpr
    | expr MOD expr                           # modExpr
    | expr PLUS expr                          # addExpr
    | expr MINUS expr                         # subExpr
    | expr expr                               # concatExpr
    | expr LT expr                            # ltExpr
    | expr LE expr                            # leExpr
    | expr GT expr                            # gtExpr
    | expr GE expr                            # geExpr
    | expr EQ expr                            # eqExpr
    | expr NE expr                            # neExpr
    | expr MATCH expr                         # matchExpr
    | expr NO_MATCH expr                      # noMatchExpr
    | expr IN NAME                            # inExpr
    | LPAREN multiple_expr_list RPAREN IN NAME      # inMultiExpr
    | expr AND newline_opt expr               # andExpr
    | expr OR newline_opt expr                # orExpr
    | expr QUESTION newline_opt expr COLON newline_opt expr  # ternaryExpr
    | lvalue ASSIGN expr                      # assignExpr
    | lvalue ADD_ASSIGN expr                  # addAssignExpr
    | lvalue SUB_ASSIGN expr                  # subAssignExpr
    | lvalue MUL_ASSIGN expr                  # mulAssignExpr
    | lvalue DIV_ASSIGN expr                  # divAssignExpr
    | lvalue MOD_ASSIGN expr                  # modAssignExpr
    | lvalue POW_ASSIGN expr                  # powAssignExpr
    | NOT expr                                # notExpr2
    | LPAREN expr RPAREN                      # groupExpr
    | getline_expr                            # getlineExpr
    | NAME LPAREN expr_list_opt RPAREN        # funcCallExpr
    | BUILTIN_FUNC_NAME LPAREN expr_list_opt RPAREN # builtinFuncExpr
    | BUILTIN_FUNC_NAME                       # builtinExpr
    | lvalue                                  # lvalueExpr
    | NUMBER                                  # numberExpr
    | STRING                                  # stringExpr
    | ERE                                     # ereExpr
    | DOLLAR expr                             # fieldExpr
    ;

// Getline expression variants (no recursion with expr to avoid left-recursion)
getline_expr
    : GETLINE lvalue?
    | GETLINE lvalue? LT expr
    ;

print_expr_list_opt
    : /* empty */
    | print_expr_list
    ;

print_expr_list
    : expr
    | print_expr_list COMMA newline_opt expr
    ;

lvalue
    : NAME
    | NAME LBRACKET expr_list RBRACKET
    | DOLLAR expr
    ;

newline_opt
    : /* empty */
    | newline_opt NEWLINE
    ;

// --- Lexer Rules ---

// Keywords (UPPERCASE for consistency)
BEGIN    : 'BEGIN' ;
END      : 'END' ;
BREAK    : 'break' ;
CONTINUE : 'continue' ;
DELETE   : 'delete' ;
DO       : 'do' ;
ELSE     : 'else' ;
EXIT     : 'exit' ;
FOR      : 'for' ;
FUNCTION : 'function' ;
IF       : 'if' ;
IN       : 'in' ;
NEXT     : 'next' ;
NEXTFILE : 'nextfile' ;
PRINT    : 'print' ;
PRINTF   : 'printf' ;
RETURN   : 'return' ;
WHILE    : 'while' ;

GETLINE  : 'getline' ;

// Compound Operators
ADD_ASSIGN : '+=' ;
SUB_ASSIGN : '-=' ;
MUL_ASSIGN : '*=' ;
DIV_ASSIGN : '/=' ;
MOD_ASSIGN : '%=' ;
POW_ASSIGN : '^=' ;

OR         : '||' ;
AND        : '&&' ;
NO_MATCH   : '!~' ;
EQ         : '==' ;
LE         : '<=' ;
GE         : '>=' ;
NE         : '!=' ;
INCR       : '++' ;
DECR       : '--' ;
APPEND     : '>>' ;

// Single-character operators needed for expr rule
ASSIGN     : '=' ;
MATCH      : '~' ;
LT         : '<' ;
GT         : '>' ;
PLUS       : '+' ;
MINUS      : '-' ;
MUL        : '*' ;
DIV        : '/' ;
MOD        : '%' ;
POW        : '^' ;
NOT        : '!' ;
QUESTION   : '?' ;
COLON      : ':' ;
DOLLAR     : '$' ;
PIPE       : '|' ;

// Delimiters
LPAREN     : '(' ;
RPAREN     : ')' ;
LBRACKET   : '[' ;
RBRACKET   : ']' ;
LBRACE     : '{' ;
RBRACE     : '}' ;
SEMICOLON  : ';' ;
COMMA      : ',' ;

// Built-in Functions (must come before NAME to have priority)
BUILTIN_FUNC_NAME
    : 'atan2' | 'cos' | 'sin' | 'exp' | 'log' | 'sqrt' | 'int' | 'rand' | 'srand'
    | 'gsub' | 'index' | 'length' | 'match' | 'split' | 'sprintf' | 'sub'
    | 'substr' | 'tolower' | 'toupper' | 'close' | 'fflush' | 'system'
    ;

// Identifiers (must come after keywords and built-in functions)
NAME      : [a-zA-Z_][a-zA-Z0-9_]* ;

NUMBER    : [0-9]+ ('.' [0-9]*)? ([eE] [+-]? [0-9]+)? 
          | '.' [0-9]+ ([eE] [+-]? [0-9]+)? ;
STRING    : '"' ( '\\'. | ~["\\] )* '"' ;
ERE       : '/' ( '\\'. | ~[/\\\r\n] )* '/' ;

NEWLINE   : '\r'? '\n' ;

// Comments
COMMENT   : '#' ~[\r\n]* -> skip ;

// Ignore remaining whitespace
WS        : [ \t]+ -> skip ;