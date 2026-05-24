using Microsoft.VisualBasic;

namespace AwkToC.Semantic;

public class Symbol
{
    public string Name { get; set; } = string.Empty;
    public SymbolType Type { get; set; }
    public string Scope { get; set; } = "global";
    public int Line { get; set; }
    public int Column { get; set; }
    public bool IsArray { get; set; }
    public bool Returns { get;  set; }
    public bool IsPredifined { get; set; } = false;
    public CType TypeInC { get; set; }
    public string? NameInC { get; set; } = null;
}
