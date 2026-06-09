using Antlr4.Runtime;

namespace AwkToC.Exceptions;

class InvalidRuleException : CompilationException
{
    public InvalidRuleException(string ruleName, ParserRuleContext context)
        : base($"invalid rule for {ruleName}", context) {}
    
    public InvalidRuleException(string ruleName, int line, int column)
        : base($"invalid rule for {ruleName}", line, column) {}
}