using Antlr4.Runtime;

namespace AwkToC.Exceptions;

class CompilationException : Exception
{
    public CompilationException(string message, ParserRuleContext context)
        : base($"[{context.Start.Line}:{context.Start.Column}] {message}") {}
    
    public CompilationException(string message, int line, int column)
        : base($"[{line}:{column}] {message}") {}
}