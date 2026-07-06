using System.Collections.Generic;

public abstract class TableBaseGroupBase
{
    public abstract void Load(string[] csvLines);
}

public class TableBaseGroup<T> : TableBaseGroupBase where T : TableBaseRow, new()
{
    private readonly Dictionary<int, T> mRows = new Dictionary<int, T>();

    public T Get(int tid) => mRows.TryGetValue(tid, out T row) ? row : null;

    public bool TryGet(int tid, out T row) => mRows.TryGetValue(tid, out row);

    public IReadOnlyDictionary<int, T> All => mRows;

    protected void AddRow(int tid, T row) => mRows[tid] = row;

    public override void Load(string[] csvLines)
    {
        foreach (var line in csvLines)
        {
            var row = new T();
            row.Parse(line);
            AddRow(row.key, row);
        }
    }
}
