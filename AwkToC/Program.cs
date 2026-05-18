using AwkToC.Compiler;

class Program
{
    static void Main(String[] args)
    {
        var sw = new StreamWriter("main.c");
        try 
        {
            Compiler.Compile(
                new StreamReader("examples/example_function.awk"),
                sw
            );
        }
        catch (Exception e)
        {
            sw.Close();
            throw;
        }
        
    }
}
