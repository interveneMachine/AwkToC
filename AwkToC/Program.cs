using AwkToC.Compiler;

class Program
{
    static void Main(String[] args)
    {
        Compiler.Compile(
            new StreamReader("examples/example_function.awk"),
            new StreamWriter("main.c")
        );
    }
}
