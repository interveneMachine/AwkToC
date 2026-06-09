using Antlr4.Runtime;
using AwkToC.Semantic;

namespace AwkToC.Exceptions;

class MissingFromSymbolTableException : CompilationException
{
    public MissingFromSymbolTableException(string name, ParserRuleContext context)
        : base($"{name} is missing from symbol table", context) {}
}