using JackAnalyzer.Enums;

namespace JackAnalyzer.Models
{
    public class Token(TokenType type, object value, int row = 0, int column = 0)
    {
        public TokenType Type { get; init; } = type;
        public object Value { get; init; } = value;
        public int Row { get; init; } = row;
        public int Column { get; init; } = column;
    }
}
