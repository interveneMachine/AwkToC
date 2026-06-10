# Kompilator języka AWK do języka C - TKiK 2026

## 1. Autorzy

Piotr Bibrzycki: [pbibrzycki@student.agh.edu.pl](mailto:pbibrzycki@student.agh.edu.pl)

Bartłomiej Cieśla: [bartekciesla@student.agh.edu.pl](mailto:bartekciesla@student.agh.edu.pl)

## 2. Założenia programu

### Ogólne cele programu

Celem projektu jest stworzenie kompilatora programu napisanego w języku AWK do kodu w języku C.
Wygenerowany kod C może następnie zostać skompilowany przy użyciu zewnętrznego kompilatora, na przykład `gcc`, w celu utworzenia pliku wykonywalnego.

AWK to język programowania przeznaczony głównie do przetwarzania tekstu oraz analizy danych zapisanych w plikach. Bardzo dobrze sprawdza się przy pracy na rekordach i kolumnach, dlatego jest często używany do filtrowania, zliczania oraz raportowania danych tekstowych.

Programy w AWK zwykle działają według modelu:

* wczytaj kolejną linię wejścia,
* podziel ją na pola,
* sprawdź warunek,
* wykonaj odpowiednią akcję.

Przykład typowego programu AWK:

```awk
$3 == "IT" {
    print $1, $2
}
```

Powyższy program dla każdej linii sprawdza, czy trzecie pole ma wartość `IT`, a jeśli tak, wypisuje pierwsze i drugie pole.

### Rodzaj translatora

Kompilator

### Język implementacji

C#

### Planowany wynik działania programu

Wynikiem działania programu jest plik `.c` zawierający kod w języku C.
Kod ten korzysta z przygotowanej biblioteki runtime, która obsługuje charakterystyczne elementy języka AWK, takie jak dynamiczne wartości, pola rekordów, tablice asocjacyjne oraz operacje na danych tekstowych i liczbowych.

Następnie wygenerowany plik C można skompilować przy pomocy kompilatora `gcc`.



### Sposób realizacji skanera/parsera

Do analizy leksykalnej i syntaktycznej wykorzystano generator ANTLR4.

ANTLR4 generuje lexer oraz parser, które są następnie używane w projekcie do budowy drzewa składniowego programu.

## 3. Opis działania kompilatora

Projekt został zbudowany jako kompilator dwuprzebiegowy.

### Pierwsze przejście

W pierwszym przejściu analizowane jest drzewo składniowe i budowana jest tabela symboli.
Tabela symboli przechowuje informacje o nazwach występujących w programie, takich jak:

* zmienne
* funkcje
* parametry funkcji
* pola rekordów
* tablice asocjacyjne
* symbole tymczasowe używane podczas generowania kodu C

### Drugie przejście

W drugim przejściu generowany jest kod w języku C.
Generator korzysta z informacji zebranych w tabeli symboli oraz z drzewa składniowego utworzonego przez parser.

Wygenerowany kod C korzysta z biblioteki runtime znajdującej się w katalogu:

```text
runtime/
```

Biblioteka ta zawiera między innymi:

* definicję ogólnego typu `AwkValue`
* funkcje do konwersji wartości
* operacje arytmetyczne i logiczne
* obsługę pól rekordu
* obsługę tablic asocjacyjnych
* funkcje wypisywania wartości

## 4. Aktualnie obsługiwane elementy języka

### Obsługiwane elementy

* kompilacja programu AWK do kodu C
* generowanie kodu korzystającego z biblioteki runtime
* dzielenie linii wejściowej na pola przy pomocy separatora `FS`
* dostęp do pól rekordu, na przykład `$0`, `$1`, `$2`
* dostęp do pola o dynamicznym indeksie
* własne rozszerzenie: obsługa ujemnych indeksów pól, na przykład `$-1` jako ostatnie pole
* wyrażenia arytmetyczne
* wyrażenia logiczne
* porównania
* konkatenacja napisów
* przypisania
* inkrementacja i dekrementacja
* instrukcja `print`
* przekierowanie wyjścia `print` (`>` tworzy nowy plik za każdym razem)
* funkcje użytkownika
* parametry funkcji
* instrukcja `return`
* patterny `BEGIN` i `END`
* patterny oparte na wyrażeniach
* patterny oparte na wyrażeniach regularnych
* instrukcje warunkowe `if` oraz `if ... else`
* pętle `while`, `for` oraz `do while`
* instrukcje `break` i `continue`
* tablice asocjacyjne
* iteracja po tablicach asocjacyjnych przy pomocy `for (key in array)`
* usuwanie wybranych lub wszystkich elementów tablic asocjacyjnych


## 5. Przykładowy program

Poniższy program zlicza wystąpienia osób z działu `IT` oraz sumuje ich wartości z drugiej kolumny.

### Dane wejściowe

```text
Jan 9000 IT
Anna 12000 HR
Piotr 15000 IT
Jan 11000 IT
```

### Program AWK

```awk
BEGIN {
    total = 0
}

$3 == "IT" {
    count[$1]++
    salary[$1] += $2
    total += $2
}

END {
    for (name in count)
        print name, count[name], salary[name]

    print "Total:", total
}
```

### Wynik działania

```text
Jan 2 20000
Piotr 1 15000
Total: 35000
```


## 6. Opis tokenów

Poniższa tabela przedstawia najważniejsze tokeny zdefiniowane w gramatyce.

| Kategoria                 | Token                      | Znaczenie                                                  |
| ------------------------- | -------------------------- | ---------------------------------------------------------- |
| Bloki specjalne           | `BEGIN`, `END`             | Bloki wykonywane przed i po przetwarzaniu danych           |
| Instrukcje sterujące      | `IF`, `ELSE`               | Instrukcja warunkowa                                       |
| Instrukcje sterujące      | `WHILE`, `FOR`, `DO`       | Pętle                                                      |
| Instrukcje sterujące      | `BREAK`, `CONTINUE`        | Sterowanie przebiegiem pętli                               |
| Instrukcje sterujące      | `RETURN`, `EXIT`           | Zakończenie funkcji lub programu                           |
| Funkcje                   | `FUNCTION`                 | Definicja funkcji użytkownika                              |
| Wejście/wyjście           | `PRINT`         | Wypisywanie danych                                         |
| Tablice                   | `DELETE`                   | Usuwanie elementów tablic asocjacyjnych                    |
| Tablice                   | `IN`                       | Sprawdzanie istnienia klucza lub iteracja po tablicy       |
| Literały i identyfikatory | `NAME`                     | Nazwa zmiennej lub funkcji                                 |
| Literały i identyfikatory | `NUMBER`                   | Liczba                                                     |
| Literały i identyfikatory | `STRING`                   | Napis tekstowy                                             |
| Literały i identyfikatory | `ERE`                      | Wyrażenie regularne                                        |
| Operatory arytmetyczne    | `PLUS`, `MINUS`            | Dodawanie i odejmowanie                                    |
| Operatory arytmetyczne    | `MUL`, `DIV`, `MOD`, `POW` | Mnożenie, dzielenie, modulo i potęgowanie                  |
| Operatory porównania      | `EQ`, `NE`                 | Równość i różność                                          |
| Operatory porównania      | `LT`, `LE`, `GT`, `GE`     | Porównania relacyjne                                       |
| Operatory logiczne        | `AND`, `OR`, `NOT`         | Operacje logiczne                                          |
| Przypisania               | `ASSIGN`                   | Przypisanie wartości                                       |
| Przypisania               | `ADD_ASSIGN`, `SUB_ASSIGN` | Przypisanie z dodaniem lub odjęciem                        |
| Przypisania               | `MUL_ASSIGN`, `DIV_ASSIGN` | Przypisanie z mnożeniem lub dzieleniem                     |
| Przypisania               | `MOD_ASSIGN`, `POW_ASSIGN` | Przypisanie z modulo lub potęgowaniem                      |
| Operatory specjalne       | `INCR`, `DECR`             | Inkrementacja i dekrementacja                              |
| Operatory specjalne       | `MATCH`, `NO_MATCH`        | Dopasowanie lub brak dopasowania do wyrażenia regularnego  |                                    |
| Operatory specjalne       | `DOLLAR`                   | Odwołanie do pola rekordu, np. `$1`                        |
| Operatory specjalne       | `PIPE`, `APPEND`           | Przekierowanie wyjścia                                     |
| Separatory                | `LPAREN`, `RPAREN`         | Nawiasy okrągłe                                            |
| Separatory                | `LBRACKET`, `RBRACKET`     | Nawiasy kwadratowe                                         |
| Separatory                | `LBRACE`, `RBRACE`         | Nawiasy klamrowe                                           |
| Separatory                | `COMMA`, `SEMICOLON`       | Przecinek i średnik                                        |
| Funkcje wbudowane         | `BUILTIN_FUNC_NAME`        | Nazwy funkcji wbudowanych, np. `sin`, `log` |
| Inne                      | `NEWLINE`                  | Znak nowej linii                                           |
| Inne                      | `COMMENT`                  | Komentarz zaczynający się od `#`                           |
| Inne                      | `WS`                       | Białe znaki pomijane przez lexer                           |

## 7. Gramatyka

Gramatyka została zapisana w notacji generatora ANTLR4.


```antlr
grammar Awk;

//parser rules

program
    : terminator? (item terminator)* item?
    ;

item
    : action
    | pattern action
    | simple_pattern
    | FUNCTION NAME '(' param_list_opt ')' newline_opt action
    ;

param_list_opt
    : /* empty */
    | param_list
    ;

param_list
    : NAME (',' NAME)*
    ;

simple_pattern
    : expr
    | expr ',' newline_opt expr
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
    | FOR LPAREN simple_statement? SEMICOLON expr? SEMICOLON simple_statement? RPAREN newline_opt terminated_statement
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
    | FOR LPAREN simple_statement? SEMICOLON expr? SEMICOLON simple_statement? RPAREN newline_opt unterminated_statement
    | FOR LPAREN NAME IN NAME RPAREN newline_opt unterminated_statement
    ;

terminatable_statement
    : simple_statement
    | BREAK
    | CONTINUE
    | EXIT expr?
    | RETURN expr?
    | DO newline_opt terminated_statement WHILE LPAREN expr RPAREN
    ;

simple_statement
    : DELETE NAME LBRACKET expr_list RBRACKET
    | DELETE NAME
    | expr
    | print_statement
    ;

print_statement
    : simple_print_statement output_redirection
    | simple_print_statement
    ;

simple_print_statement
    : PRINT print_expr_list_opt
    | PRINT LPAREN multiple_expr_list RPAREN
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

expr
    : LPAREN expr RPAREN
    | DOLLAR expr
    | lvalue INCR
    | lvalue DECR
    | INCR lvalue
    | DECR lvalue
    | expr INCR
    | expr DECR
    | INCR expr
    | DECR expr
    | expr POW expr
    | NOT expr
    | PLUS expr
    | MINUS expr
    | expr MUL expr
    | expr DIV expr
    | expr MOD expr
    | expr PLUS expr
    | expr MINUS expr
    | expr expr /* concatenate strings */
    | expr LT expr
    | expr LE expr
    | expr NE expr
    | expr EQ expr
    | expr GT expr
    | expr GE expr
    | ERE MATCH expr // changed from expr MATCH expr
    | ERE NO_MATCH expr // changed from expr NO_MATCH expr
    | expr MATCH ERE
    | expr NO_MATCH ERE
    | expr IN NAME
    | LPAREN multiple_expr_list RPAREN IN NAME
    | expr AND newline_opt expr
    | expr OR newline_opt expr
    | lvalue ASSIGN expr
    | lvalue ADD_ASSIGN expr
    | lvalue SUB_ASSIGN expr
    | lvalue MUL_ASSIGN expr
    | lvalue DIV_ASSIGN expr
    | lvalue MOD_ASSIGN expr
    | lvalue POW_ASSIGN expr
    | NAME LPAREN expr_list_opt RPAREN
    | lvalue
    | NUMBER
    | STRING
    | ERE
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

// lexer rules

// keywords
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
PRINT    : 'print' ;
RETURN   : 'return' ;
WHILE    : 'while' ;

// compound operators
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

// single-character operators 
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

NAME      : [a-zA-Z_][a-zA-Z0-9_]* ;

NUMBER    : [0-9]+ ('.' [0-9]*)? ([eE] [+-]? [0-9]+)?
          | '.' [0-9]+ ([eE] [+-]? [0-9]+)? ;
STRING    : '"' ( '\\'. | ~["\\] )* '"' ;
ERE       : '/' ( '\\'. | ~[/\\\r\n] )* '/' ;

NEWLINE   : '\r'? '\n' ;

COMMENT   : '#' ~[\r\n]* -> skip ;

// ignore remaining whitespaces
WS        : [ \t]+ -> skip ;
```


## 8. Wymagania wstępne, instalacja i instrukcja obsługi

### Wymagane oprogramowanie

Do uruchomienia projektu wymagane są:

* .NET SDK
* Java / JRE / JDK, wymagana przez ANTLR4
* ANTLR4
* GCC
* biblioteka matematyczna linkowana przez `-lm`

Opcjonalnie do testowania wycieków pamięci:

* Valgrind.

### Uruchamianie kompilatora

Kompilator można uruchomić poleceniem:

```console
dotnet run --project AwkToC <input.awk> [-o <output.c>]
```

Dostępne opcje:

```console
Options:
  -o, --output <file>   Path to generated C file. Default: main.c
  -h, --help            Show this help message
```

### Przykład kompilacji programu AWK

Dla pliku:

```text
main.awk
```

można uruchomić:

```console
dotnet run --project AwkToC main.awk
```

Domyślnie utworzony zostanie plik:

```text
main.c
```

Można również wskazać własną nazwę pliku wynikowego:

```console
dotnet run --project AwkToC main.awk -o output.c
```

### Kompilacja wygenerowanego kodu C

Wygenerowany plik C można skompilować przy użyciu `gcc`:

```console
gcc main.c runtime/awk_runtime.c -o main.out -lm -I runtime/
```

### Uruchomienie skompilowanego programu

Aby uruchomić skompilowany program na pliku z danymi, należy wywołać:

```console
./main.out dane.txt
```

## 9. Testy

Projekt zawiera testy integracyjne sprawdzające działanie kompilatora.

Testy obejmują między innymi:

* kompilację wyrażeń
* instrukcję `print`
* patterny
* funkcje użytkownika
* instrukcje sterujące
* tablice asocjacyjne
* kompilację wygenerowanego kodu C przy pomocy GCC
* porównanie wyniku działania programu z oczekiwanym wyjściem

Część testów może dodatkowo uruchamiać wygenerowany program przez Valgrind w celu wykrywania wycieków pamięci.

