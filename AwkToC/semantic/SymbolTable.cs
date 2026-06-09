using System.ComponentModel;

namespace AwkToC.Semantic;

public class SymbolTable
{
    private HashSet<string> UsedCNames;
    private readonly Dictionary<string, Symbol> symbols = new();
    private readonly Dictionary<string, List<CSymbol>> TmpSymbols = new();
    private int temporaryCounter = 0;
    private int patternCounter = 0;
    private int itemCounter = 0;
    private int continueTargetCounter = 0;

    public SymbolTable()
    {
        UsedCNames = new HashSet<string>([
            "isBegin", "buffer", "file", "argc", "argv", "main", "stderr", "stdout",
            "AwkValueType", "AwkValue", "AWK_UNDEFINED", "AWK_NUMBER", "AWK_STRING", "Fields", "ArrayEntry", "Array", "ArrayIterator",
            "array_init", "array_get_value", "array_set_value", "array_delete_value", "array_delete", "array_free", "arrayiterator_init", "arrayiterator_next",
            "remove_newline", "fields_string", "fields_get", "fields_assign", "fields_free",
            "awk_undefined", "awk_number", "awk_string", "awk_copy", "awk_match", "awk_free", "awk_to_number", "awk_to_string", "awk_is_truthy", "awk_to_int",
            "awk_add", "awk_sub", "awk_mul", "awk_div", "awk_mod", "awk_pow", "awk_unary_plus", "awk_unary_minus", "awk_eq", "awn_ne", "awk_lt", "awk_le", "awk_gt",
            "awk_ge", "awk_not", "awk_concat", "awk_concat_array_arg", "awk_set_default_predefined", "awk_print_value", "awk_print_values", "awk_strdup",
            "awk_output_redirection_write", "awk_output_redirection_append", "awk_output_redirection_pipe",
            "NULL", "int", "long", "float", "double", "FILE", "char", "size_t", "const", "void", "struct", "extern", "static", "typedef", "return", "regmatch_t", "regex_t",
            "malloc", "free", "strcat", "strlen", "strcpy", "strncpy", "fprintf", "exit", "regfree", "regexec", "regcomp", "fopen", "fclose",
        ]);
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

        // builtin functions
        AddWithCName(new Symbol
        {
            Name = "atan2",
            Scope = "global",
            Type = SymbolType.Function,
            IsPredifined = true,
            TypeInC = CType.AwkValue,
            NameInC = "awk_atan2"
        });
        AddWithCName(new Symbol
        {
            Name = "sin",
            Scope = "global",
            Type = SymbolType.Function,
            IsPredifined = true,
            TypeInC = CType.AwkValue,
            NameInC = "awk_sin"
        });
        AddWithCName(new Symbol
        {
            Name = "cos",
            Scope = "global",
            Type = SymbolType.Function,
            IsPredifined = true,
            TypeInC = CType.AwkValue,
            NameInC = "awk_cos"
        });
        AddWithCName(new Symbol
        {
            Name = "exp",
            Scope = "global",
            Type = SymbolType.Function,
            IsPredifined = true,
            TypeInC = CType.AwkValue,
            NameInC = "awk_exp"
        });
        AddWithCName(new Symbol
        {
            Name = "log",
            Scope = "global",
            Type = SymbolType.Function,
            IsPredifined = true,
            TypeInC = CType.AwkValue,
            NameInC = "awk_log"
        });
        AddWithCName(new Symbol
        {
            Name = "sqrt",
            Scope = "global",
            Type = SymbolType.Function,
            IsPredifined = true,
            TypeInC = CType.AwkValue,
            NameInC = "awk_sqrt"
        });
        AddWithCName(new Symbol
        {
            Name = "int",
            Scope = "global",
            Type = SymbolType.Function,
            IsPredifined = true,
            TypeInC = CType.AwkValue,
            NameInC = "awk_int"
        });
    }

    private void AddWithCName(Symbol symbol)
    {
        string key = $"{symbol.Scope}:{symbol.Name}";
        symbols[key] = symbol;
        UsedCNames.Add(symbol.NameInC ?? throw new Exception("this method requiries nameInC to be provided"));
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

    public string NewTemporaryCName(CScope cScope, bool isMemoryAllocated, bool isIterator = false)
    {
        string currentScope = cScope.GetScope();
        string name = AddCName($"tmp{temporaryCounter++}");
        if (!TmpSymbols.ContainsKey(currentScope))
            TmpSymbols[currentScope] = [];
        TmpSymbols[currentScope].Add(new CSymbol
        {
            Name = name,
            Scope = currentScope,
            IsMemoryAllocated = isMemoryAllocated,
            IsIterator = isIterator,
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
