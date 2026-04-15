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


# Autorzy

Piotr Bibrzycki pbibrzycki@student.agh.edu.pl

Bartłomiej Cieśla bartekciesla@student.agh.edu.pl

# Implementcja

Język implementacji: C# \
Generator parserów: ANTLR4


Programy w AWK zwykle działają w modelu:
- wczytaj kolejną linię wejścia,
- sprawdź warunek,
- wykonaj akcję.


