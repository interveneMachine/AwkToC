namespace AwkToC.Semantic;

public class SymbolTable
{
    private HashSet<string> UsedCNames = new();
    private readonly Dictionary<string, Symbol> symbols = new();
    private readonly Dictionary<string, List<CSymbol>> TmpSymbols = new();
    private int temporaryCounter = 0;
    private int patternCounter = 0;
    private int itemCounter = 0;
    public void Add(Symbol symbol)
    {
        string key = $"{symbol.Scope}:{symbol.Name}";
        if (!symbols.ContainsKey(key))
        {
            symbols[key] = symbol;
            symbols[key].NameInC = AddCName(symbol.Name);
        }
    }

    public Symbol? Lookup(string name, string scope)
    {
        string localKey = $"{scope}:{name}";
        string globalKey = $"global:{name}";
        if (symbols.TryGetValue(localKey, out var local))
        {
            return local;
        }

        if (symbols.TryGetValue(globalKey, out var global))
        {
            return global;
        }
        return null;
    }

    public string AddCName(string name)
    {
        if (UsedCNames.Contains(name))
        {
            int suffix = 0;
            while (UsedCNames.Contains(name + suffix))
                suffix++;
            UsedCNames.Add(name + suffix);
            return name + suffix;
        }
        UsedCNames.Add(name);
        return name;
    }

    public string NewTemporaryCName(string scope, bool isMemoryAllocated)
    {
        string name = AddCName($"tmp{temporaryCounter++}");
        if (!TmpSymbols.ContainsKey(scope))
            TmpSymbols[scope] = new List<CSymbol>();
        TmpSymbols[scope].Add(new CSymbol
        {
            Name = name,
            Scope = scope,
            IsMemoryAllocated = isMemoryAllocated
        });
        return name;
    }

    public string NewPatternCName()
    {
        return AddCName($"pattern{patternCounter++}");
    }

    public string NewItemCName()
    {
        return AddCName($"item{itemCounter++}");
    }

    public IEnumerable<Symbol> All()
    {
        return symbols.Values;
    }

    public IEnumerable<Symbol> AllInScope(string scope)
    {
        return (IEnumerable<Symbol>)symbols.Values.Where(sym => sym.Scope == scope).GetEnumerator();
    }

    public List<CSymbol> AllTmpVariablesInScope(string scope)
    {
        if (TmpSymbols.TryGetValue(scope, out List<CSymbol>? value))
            return value;
        return [];
    }
}
