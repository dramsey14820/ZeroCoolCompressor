namespace FastTextCompressor.Core;

internal sealed class DictionaryManager
{
    private readonly Dictionary<string, int> _ids = new(StringComparer.Ordinal);
    private readonly List<string> _tokens = new();

    public int Count => _tokens.Count;

    public IReadOnlyList<string> Tokens => _tokens;

    public int GetOrAdd(ReadOnlySpan<char> token)
    {
        var value = token.ToString();
        if (_ids.TryGetValue(value, out var id))
        {
            return id;
        }

        id = _tokens.Count;
        _ids.Add(value, id);
        _tokens.Add(value);
        return id;
    }
}
