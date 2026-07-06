
using System;

public class TableBaseRow
{
    public virtual int key { get { return 0; } }

    internal void Parse(string line)
    {
        throw new NotImplementedException();
    }
}