using System;
using System.IO;

namespace at.jku.ssw.Coco
{
  public class Writer
  {
    private readonly StreamWriter sw;

    private int line;
    private int column;

    public int Line
    {
      get { return line; }
    }

    public int Coloumn
    {
      get { return column; }
    }

    public Writer(StreamWriter streamWriter)
    {
      sw = streamWriter;
      line = 0;
      column = 0;
    }

    public void Write(string str)
    {
      string[] lines = str.Split(new char[] {'\n', '\r'},
          StringSplitOptions.RemoveEmptyEntries);
      for (int i = 0; i < lines.Length; i++)
      {
        if (i != 0)
        {
          WriteLine();
        }
        sw.Write(lines[i]);
        column += lines[i].Length;
      }
      if (str.Length != 0 && str[str.Length - 1] == '\n')
      {
        WriteLine();
      }
    }

    public void Write<T>(T value)
    {
      Write(value.ToString());
    }

    public void Write(string format, params object[] args)
    {
      sw.Write(String.Format(format, args));
    }

    public void WriteLine()
    {
      sw.WriteLine();
      line++;
      column = 0;
    }

    public void WriteLine<T>(T value)
    {
      Write(value);
      WriteLine();
    }

    public void WriteLine(string value)
    {
      Write(value);
      WriteLine();
    }

    public void WriteLine(string format, params object[] args)
    {
      Write(format, args);
      WriteLine();
    }

    public void Close()
    {
      sw.Close();
    }
  }
}