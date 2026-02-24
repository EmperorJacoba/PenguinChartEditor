using System.Collections.Generic;

public class FiniteStack<T>
{
    private LinkedList<T> list;
    private int maximumEntries;

    public FiniteStack(int maximumEntries)
    {
        list = new LinkedList<T>();
        this.maximumEntries = maximumEntries;
    }

    public void ChangeMaximumEntries(int newMax)
    {
        maximumEntries = newMax;
    }

    public void Push(T item)
    {
        list.AddLast(item);
        if (list.Count > maximumEntries)
        {
            list.RemoveFirst();
        }
    }

    public T Pop()
    {
        var item = list.Last.Value;
        list.RemoveLast();
        return item;
    }

    public T Peek()
    {
        return list.Last.Value;
    }

    public void Clear()
    {
        if (list.Count > 0)
        {
            list = new LinkedList<T>();
        }
    }

    public bool IsEmpty() => list.Count == 0;
    public int Count => list.Count;
}