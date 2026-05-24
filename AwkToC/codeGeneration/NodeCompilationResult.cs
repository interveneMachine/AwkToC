namespace AwkToC.CodeGeneration;

class NodeCompilationResult
{
    public string? ReturnName { get; set; }

    public ResultType? Type { get; set; }

    public NodeCompilationResult()
    {
    }

    public NodeCompilationResult(string returnName, ResultType type)
    {
        ReturnName = returnName;
        Type = type;
    }
}
