using System.Text.Json;

namespace HomeGrid;

/// <summary>The 50 most recent meter readings, newest first.</summary>
public sealed class EventBuffer
{
    private const int Capacity = 50;

    private readonly LinkedList<JsonElement> _events = new();
    private readonly object _lock = new();

    public void Add(JsonElement reading)
    {
        lock (_lock)
        {
            _events.AddFirst(reading);
            if (_events.Count > Capacity)
            {
                _events.RemoveLast();
            }
        }
    }

    public List<JsonElement> Snapshot()
    {
        lock (_lock)
        {
            return [.. _events];
        }
    }

    public void Clear()
    {
        lock (_lock)
        {
            _events.Clear();
        }
    }
}
