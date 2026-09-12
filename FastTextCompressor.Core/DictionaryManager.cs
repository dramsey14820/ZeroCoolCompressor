namespace FastTextCompressor.Core;

internal sealed class DictionaryManager
{
    private readonly Dictionary<string, int> _ids = new(StringComparer.Ordinal);
    private readonly List<string> _tokens = new();
    private readonly List<int> _frequencies = new();

    public int Count => _tokens.Count;

    public IReadOnlyList<string> Tokens => _tokens;

    public int GetOrAdd(ReadOnlySpan<char> token)
    {
        var value = token.ToString();
        return GetOrAdd(value);
    }

    public int GetOrAdd(string value)
    {
        if (_ids.TryGetValue(value, out var id))
        {
            _frequencies[id]++;
            return id;
        }

        id = _tokens.Count;
        _ids.Add(value, id);
        _tokens.Add(value);
        _frequencies.Add(1);
        return id;
    }

    public int[] OrderByFrequency()
    {
        var order = Enumerable.Range(0, _tokens.Count)
            .OrderByDescending(index => _frequencies[index])
            .ThenBy(index => index)
            .ToArray();
        var remap = new int[order.Length];

        for (var newId = 0; newId < order.Length; newId++)
        {
            var oldId = order[newId];
            remap[oldId] = newId;
        }

        var tokens = order.Select(index => _tokens[index]).ToArray();
        _tokens.Clear();
        _tokens.AddRange(tokens);
        _ids.Clear();
        for (var index = 0; index < _tokens.Count; index++)
        {
            _ids.Add(_tokens[index], index);
        }

        return remap;
    }
}
