namespace AwkToC.Semantic;

public class Symbol
{
    public string Name { get; set; } = string.Empty;
    public SymbolType Type { get; set; }
    public string Scope { get; set; } = "global";
    public int Line { get; set; }
    public int Column { get; set; }
    public bool IsAssigned { get; set; }
    public bool IsUsed { get; set; }
    public bool IsArray { get; set; }
    public List<string> Parameters { get; set; } = new();
}
