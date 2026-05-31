namespace AwkToC.Semantic;

public class CScope
{
    private Stack<string> scopes = new();

    public void EnterFunction(string name)
    {
        scopes.Push("function[" + name + "]");
    }

    public void ExitFunction()
    {
        scopes.Pop();
    }

    public void EnterIf(int line, int column)
    {
        scopes.Push($"if[{line},{column}]");
    }

    public void ExitIf()
    {
        scopes.Pop(); // TODO improve error handling
    }

    public void EnterElse(int line, int column)
    {
        scopes.Push($"else[{line},{column}]");
    }

    public void ExitElse()
    {
        scopes.Pop();
    }

    public void EnterWhile(int line, int column)
    {
        scopes.Push($"while[{line},{column}]");
    }

    public void ExitWhile()
    {
        scopes.Pop();
    }

    public void EnterCondition(int line, int column)
    {
        scopes.Push($"condition[{line},{column}]");
    }

    public void ExitCondition()
    {
        scopes.Pop();
    }

    public string GetScope()
    {
        return string.Join(":", scopes);
    }

    public List<string> GetScopesIn(char scopeName)
    {
        List<string> result = new();
        Stack<string> dropped = new();
        string topScope;
        do
        {
            if (scopes.Count == 0)
                throw new Exception("tried to use break/continoue outside of loop");
            topScope = scopes.Peek();
            string currentScope = GetScope();
            result.Add(currentScope);
            dropped.Push(scopes.Pop());
        } while (topScope[0] != scopeName);
        while (dropped.Count != 0)
            scopes.Push(dropped.Pop());
        return result;
    }
}