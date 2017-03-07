using System.IO;

namespace at.jku.ssw.Coco
{
  public class Mapping
  {
    private class MapEntry
    {
      public int line;
      public int column;
      public int tline;
      public int tcolumn;
      public int length;
      public MapEntry next;
    }

    private MapEntry root;
    private MapEntry cur;

    private string grammar;

    public string Grammar
    {
      get { return grammar; }
    }

    public Mapping()
    {
      cur = null;
      root = null;
    }

    public bool Get(int line, int column, out int tline, out int tcolumn)
    {
      MapEntry entry = root;

      while (entry != null)
      {
        if (entry.line == line && entry.column <= column
            && entry.column + entry.length > column)
        {
          tline = entry.tline;
          tcolumn = entry.tcolumn + (column - entry.column);
          return true;
        }
        entry = entry.next;
      }
      tline = 0;
      tcolumn = 0;
      return false;
    }

    public void Set(int line, int column, int tline, int tcolumn,
        int length)
    {
      if (cur != null && cur.line == line
          && cur.column + cur.length == column && cur.tline == tline
              && cur.tcolumn + cur.length == tcolumn)
      {
        cur.length += length;
        return;
      }

      if (cur == null)
      {
        cur = root = new MapEntry();
      }
      else
      {
        cur = cur.next = new MapEntry();
      }

      cur.line = line;
      cur.column = column;
      cur.tline = tline;
      cur.tcolumn = tcolumn;
      cur.length = length;
    }

    public void Write(string fn, string grammar)
    {
      StreamWriter sw =
          new StreamWriter(new FileStream(fn, FileMode.Create));
      MapEntry entry = root;

      sw.WriteLine(grammar);

      while (entry != null)
      {
        sw.Write(entry.line);
        sw.Write(",");
        sw.Write(entry.column);
        sw.Write(",");
        sw.Write(entry.tline);
        sw.Write(",");
        sw.Write(entry.tcolumn);
        sw.Write(",");
        sw.Write(entry.length);
        sw.WriteLine();
        entry = entry.next;
      }
      sw.Close();
    }

    public void Read(string fn)
    {
      StreamReader sr = new StreamReader(new FileStream(fn, FileMode.Open));
      grammar = sr.ReadLine();
      string line;
      while ((line = sr.ReadLine()) != null)
      {
        string[] ar = line.Split(new char[] {','});
        Set(int.Parse(ar[0]), int.Parse(ar[1]), int.Parse(ar[2]),
            int.Parse(ar[3]), int.Parse(ar[4]));
      }
      sr.Close();
    }
  }
}