using Antlr4.Runtime;
using AwkToC.Semantic;

namespace AwkToC.Exceptions;

class MissingNameInCException : CompilationException
{
    public MissingNameInCException(Symbol symbol, ParserRuleContext context)
        : base($"{symbol.Type} {symbol.Name} is missing NameInC", context) {}
}