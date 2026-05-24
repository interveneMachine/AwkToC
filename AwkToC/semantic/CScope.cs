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

    public string GetScope()
    {
        return string.Join(":", scopes);
    }
}