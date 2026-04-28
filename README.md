# Cel projektu

Celem projektu jest kompilacja programu z języka AWK do C, tak by można było użyć innych kompilatorów np. gcc do stworzenia plików wykonywalnych.
AWK to język programowania przeznaczony głównie do przetwarzania tekstu i analizy danych zapisanych w plikach.  Dobrze sprawdza się przy pracy na rekordach i kolumnach, dlatego jest często używany do filtrowania, zliczania i raportowania danych tekstowych.

Programy w AWK zwykle działają w modelu:
- wczytaj kolejną linię wejścia
- sprawdź warunek
- wykonaj akcję

Projekt skupia się na:
- rozpoznawaniu składni AWK
- analizie instrukcji, wyrażeń i operacji na danych
- obsłudze charakterystycznych elementów języka, takich jak pola `$1`, `$2`, instrukcje `print`, `getline` oraz tablice asocjacyjne

# Implementcja

Język implementacji: C# \
Generator parserów: ANTLR4


# Spis tokenów 

| Token | Znaczenie |
|---|---|
| `BEGIN`, `END` | specjalne bloki wykonywane przed i po przetwarzaniu danych |
| `BEGINFILE`, `ENDFILE` | bloki wykonywane na początku i końcu każdego pliku |
| `IF`, `ELSE`, `WHILE`, `FOR`, `DO` | instrukcje sterujące |
| `BREAK`, `CONTINUE` | sterowanie przebiegiem pętli |
| `RETURN`, `EXIT` | zakończenie funkcji lub programu |
| `FUNCTION` | definicja funkcji |
| `NEXT`, `NEXTFILE` | przejście do kolejnego rekordu lub kolejnego pliku |
| `PRINT`, `PRINTF` | wypisywanie danych |
| `GETLINE` | pobieranie danych wejściowych |
| `DELETE` | usuwanie elementów tablic asocjacyjnych |
| `IN` | sprawdzanie istnienia klucza w tablicy lub iteracja po tablicy |
| `NAME`, `NUMBER`, `STRING`, `ERE` | identyfikatory i literały |
| `PLUS`, `MINUS`, `MUL`, `DIV`, `MOD`, `POW` | operatory arytmetyczne |
| `EQ`, `NE`, `LT`, `LE`, `GT`, `GE` | operatory porównania |
| `AND`, `OR`, `NOT` | operatory logiczne |
| `ASSIGN`, `ADD_ASSIGN`, `SUB_ASSIGN`, `MUL_ASSIGN`, `DIV_ASSIGN`, `MOD_ASSIGN`, `POW_ASSIGN` | operatory przypisania |
| `INCR`, `DECR` | inkrementacja i dekrementacja |
| `MATCH`, `NO_MATCH` | dopasowanie i brak dopasowania do wyrażenia regularnego |
| `QUESTION`, `COLON` | operator warunkowy |
| `DOLLAR` | odwołanie do pola, np. `$1` |
| `PIPE`, `APPEND` | przekierowanie wyjścia |
| `LPAREN`, `RPAREN`, `LBRACKET`, `RBRACKET`, `LBRACE`, `RBRACE` | nawiasy |
| `COMMA`, `SEMICOLON` | separatory |
| `BUILTIN_FUNC_NAME` | nazwy funkcji wbudowanych, np. `length`, `split`, `substr` |
| `NEWLINE` | znak nowej linii |
| `COMMENT` | komentarz zaczynający się od `#` |
| `WS` | białe znaki pomijane przez lexer |

# Autorzy

Piotr Bibrzycki pbibrzycki@student.agh.edu.pl

Bartłomiej Cieśla bartekciesla@student.agh.edu.pl




