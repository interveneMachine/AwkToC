using Antlr4.Runtime;
using AwkToC.Semantic;

namespace AwkToC.Exceptions;

class WrongTypeException : CompilationException
{
    public WrongTypeException(Symbol symbol, string expected,  ParserRuleContext context)
        : base($"{symbol.Name} is {symbol.Type}, expected {expected}", context) {}
}