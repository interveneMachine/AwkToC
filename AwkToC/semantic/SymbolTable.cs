using System.Transactions;

namespace AwkToC.Semantic;

public class SymbolTable
{
    private HashSet<string> UsedCNames = new();
    private readonly Dictionary<string, Symbol> symbols = new();
    private readonly Dictionary<string, List<CSymbol>> TmpSymbols = new();
    private int temporaryCounter = 0;
    private int patternCounter = 0;
    private int itemCounter = 0;
    private int continueTargetCounter = 0;

    public SymbolTable()
    {
        Add(new Symbol
        {
            Name = "NR",
            Scope = "global",
            Type = SymbolType.Variable,
            IsPredifined = true,
            TypeInC = CType.Int
        });
        Add(new Symbol
        {
            Name = "FS",
            Scope = "global",
            Type = SymbolType.Variable,
            IsPredifined = true,
            TypeInC = CType.CString
        });
        Add(new Symbol
        {
            Name = "CONVFMT",
            Scope = "global",
            Type = SymbolType.Variable,
            IsPredifined = true,
            TypeInC = CType.CString
        });
        Add(new Symbol
        {
            Name = "SUBSEP",
            Scope = "global",
            Type = SymbolType.Variable,
            IsPredifined = true,
            TypeInC = CType.CString
        });
    }

    public void Add(Symbol symbol)
    {
        string key = $"{symbol.Scope}:{symbol.Name}";
        if (!symbols.ContainsKey(key))
        {
            symbols[key] = symbol;
            symbols[key].NameInC = AddCName(symbol.Name);
        }
    }

    public Symbol? Lookup(string name, AwkScope awkScope)
    {
        string localKey = $"{awkScope.GetScope()}:{name}";
        string globalKey = $"{awkScope.GetGlobal()}:{name}";
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

    public string NewTemporaryCName(CScope cScope, bool isMemoryAllocated)
    {
        string currentScope = cScope.GetScope();
        string name = AddCName($"tmp{temporaryCounter++}");
        if (!TmpSymbols.ContainsKey(currentScope))
            TmpSymbols[currentScope] = [];
        TmpSymbols[currentScope].Add(new CSymbol
        {
            Name = name,
            Scope = currentScope,
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

    public string NewContinueTarget()
    {
        return AddCName($"_incr_{continueTargetCounter++}");
    }

    public IEnumerable<Symbol> AllVariables()
    {
        return symbols.Values.Where(s => s.Type == SymbolType.Variable && !s.IsPredifined);
    }

    public List<CSymbol> AllTmpVariablesInCurrentScope(CScope cScope)
    {
        if (TmpSymbols.TryGetValue(cScope.GetScope(), out List<CSymbol>? value))
            return value;
        return [];
    }

    public List<CSymbol> AllTmpVariablesIn(CScope cScope, string scopeName)
    {
        List<CSymbol> result = new();
        foreach(string scope in cScope.GetScopesIn(scopeName[0]))
        {
            if (TmpSymbols.TryGetValue(scope, out List<CSymbol>? value))
                result.AddRange(value);
        }
        return result;
    }
}
