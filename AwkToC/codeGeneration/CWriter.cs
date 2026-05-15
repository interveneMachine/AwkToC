using System.CodeDom.Compiler;
using System.Transactions;

namespace AwkToC.CodeGeneration;

/// <summary>
/// This class makes writing C code to a file easier
/// by handing indentation, entering/exiting blocks and writing comments
/// </summary>
class CWriter
{
    private readonly string basicIndent = "    ";
    private readonly string commentBegining = "\\\\ ";
    private StreamWriter stream;
    private string indent = "";
    private int indentLevel = 0;

    public CWriter(StreamWriter streamWriter)
    {
        stream = streamWriter;
    }

    public void WriteLine(string line)
    {
        stream.WriteLine(indent + line);
    }

    public void WriteComment(string comment)
    {
        stream.WriteLine(commentBegining + comment);
    }

    public void HSpace(int lines)
    {
        stream.Write(new String('\n', lines));
    }

    public void EnterBlock()
    {
        WriteLine("{");
        indentLevel++;
        indent += basicIndent;
    }

    public void ExitBlock()
    {
        if(indentLevel == 0)
        {
            stream.Close();
            throw new InvalidOperationException("ExitBlock called without matching EnterBlock being called before");
        }
        indentLevel--;
        indent = string.Concat(Enumerable.Repeat(basicIndent, indentLevel));
        WriteLine("}");
    }

    public void Close()
    {
        stream.Close();
        if(indentLevel != 0)
            throw new InvalidOperationException($"Close called before exiting all blocks with ExitBlock, currnetly entered: {indentLevel} blocks");
    }
}