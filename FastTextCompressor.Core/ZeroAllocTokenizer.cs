using System.Text;

namespace FastTextCompressor.Core;

public delegate void TokenHandler(ReadOnlySpan<char> token);

public sealed class ZeroAllocTokenizer
{
    private readonly StringBuilder _current = new();
    private bool? _wordToken;

    public void Process(ReadOnlySpan<char> input, bool isFinal, TokenHandler handler)
    {
        foreach (var character in input)
        {
            var isWord = char.IsLetterOrDigit(character);
            if (_wordToken is not null && _wordToken != isWord)
            {
                Emit(handler);
            }

            _wordToken ??= isWord;
            _current.Append(character);
        }

        if (isFinal)
        {
            Emit(handler);
        }
    }

    private void Emit(TokenHandler handler)
    {
        if (_current.Length == 0)
        {
            return;
        }

        handler(_current.ToString().AsSpan());
        _current.Clear();
        _wordToken = null;
    }
}
