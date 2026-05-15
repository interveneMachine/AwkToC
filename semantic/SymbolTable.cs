namespace AwkToC.Semantic;

public class SymbolTable
{
    private HashSet<string> UsedCNames = new();
    private readonly Dictionary<string, Symbol> symbols = new();
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
        int suffix = 0;
        while(UsedCNames.Contains(name + suffix))
            suffix++;
        UsedCNames.Add(name + suffix);
        return name + suffix;
    }

    public IEnumerable<Symbol> All()
    {
        return symbols.Values;
    }

    public IEnumerable<Symbol> AllInScope(string scope)
    {
        return (IEnumerable<Symbol>)symbols.Values.Where(sym => sym.Scope == scope).GetEnumerator();
    }
}
