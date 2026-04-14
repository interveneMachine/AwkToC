grammar Awk;

// --- Parser Rules ---

program
    : terminator? item_list
    | terminator? item_list item
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

expr
    : expr POW expr
    | INCR expr
    | DECR expr
    | NOT expr
    | PLUS expr
    | MINUS expr
    | expr INCR
    | expr DECR
    | expr MUL expr
    | expr DIV expr
    | expr MOD expr
    | expr PLUS expr
    | expr MINUS expr
    | expr expr /* concatenate strings */
    | expr LT expr
    | expr LE expr
    | expr GT expr
    | expr GE expr
    | expr EQ expr
    | expr NE expr
    | expr MATCH expr
    | expr NO_MATCH expr
    | expr IN NAME
    | LPAREN multiple_expr_list RPAREN IN NAME
    | expr AND newline_opt expr
    | expr OR newline_opt expr
    | expr QUESTION newline_opt expr COLON newline_opt expr
    | lvalue ASSIGN expr
    | lvalue ADD_ASSIGN expr
    | lvalue SUB_ASSIGN expr
    | lvalue MUL_ASSIGN expr
    | lvalue DIV_ASSIGN expr
    | lvalue MOD_ASSIGN expr
    | lvalue POW_ASSIGN expr
    | NOT expr
    | LPAREN expr RPAREN
    | getline_expr
    | NAME LPAREN expr_list_opt RPAREN
    | BUILTIN_FUNC_NAME LPAREN expr_list_opt RPAREN
    | BUILTIN_FUNC_NAME
    | lvalue
    | NUMBER
    | STRING
    | ERE
    | DOLLAR expr
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

BUILTIN_FUNC_NAME
    : 'atan2' | 'cos' | 'sin' | 'exp' | 'log' | 'sqrt' | 'int' | 'rand' | 'srand'
    | 'gsub' | 'index' | 'length' | 'match' | 'split' | 'sprintf' | 'sub'
    | 'substr' | 'tolower' | 'toupper' | 'close' | 'fflush' | 'system'
    ;

NAME      : [a-zA-Z_][a-zA-Z0-9_]* ;

NUMBER    : [0-9]+ ('.' [0-9]*)? ([eE] [+-]? [0-9]+)?
          | '.' [0-9]+ ([eE] [+-]? [0-9]+)? ;
STRING    : '"' ( '\\'. | ~["\\] )* '"' ;
ERE       : '/' ( '\\'. | ~[/\\\r\n] )* '/' ;

NEWLINE   : '\r'? '\n' ;

COMMENT   : '#' ~[\r\n]* -> skip ;

// Ignore remaining whitespace
WS        : [ \t]+ -> skip ;