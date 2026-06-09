namespace AwkToC.Semantic;

public class CSymbol
{
    public string Name { get; set; } = string.Empty;
    public string Scope { get; set; } = "global";
    public bool IsMemoryAllocated { get; set; }
    public bool IsIterator { get; set; } = false;
}