namespace AwkToC.Semantic;

public class AwkScope
{
    private string currentScope = "global";
    private bool inFunction = false;

    public void EnterFunction(string name)
    {
        currentScope = "function:" + name;
        inFunction = true;
    }

    public void ExitFunction()
    {
        currentScope = "global";
        inFunction = false;
    }

    public string GetScope()
    {
        return currentScope;
    }

    public string GetGlobal()
    {
        return "global";
    }

    public bool InFunction() { return inFunction; }
}