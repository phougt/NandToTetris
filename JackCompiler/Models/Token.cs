using JackCompiler.Enums;

namespace JackCompiler.Models
{
    public class Token(TokenType type, object value, string filename, uint row = 0, uint column = 0)
    {
        public TokenType Type { get; init; } = type;
        public object Value { get; init; } = value;
        public uint Row { get; init; } = row;
        public uint Column { get; init; } = column;
        public string Filename { get; init; } = filename;
    }
}
