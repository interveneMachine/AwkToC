namespace AwkToC.CodeGeneration;

class NodeCompilationResult
{
    public string? ReturnName { get; set; }

    public CType? Type { get; set; }

    public NodeCompilationResult()
    {
    }

    public NodeCompilationResult(string returnName, CType type)
    {
        ReturnName = returnName;
        Type = type;
    }
}
