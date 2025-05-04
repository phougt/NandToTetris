using JackCompiler.Enums;
namespace JackCompiler.Extensions 
{
    public static class StringExtension
    {
        public static Symbol ToSymbol(this string value)
        {
            return value switch
            {
                "{" => Symbol.LBRACE,
                "}" => Symbol.RBRACE,
                "(" => Symbol.LPAR,
                ")" => Symbol.RPAR,
                "[" => Symbol.LBRACK,
                "]" => Symbol.RBRACK,
                "." => Symbol.DOT,
                "," => Symbol.COMMA,
                ";" => Symbol.SEMICOLON,
                "+" => Symbol.PLUS,
                "-" => Symbol.MINUS,
                "*" => Symbol.STAR,
                "/" => Symbol.SLASH,
                "&" => Symbol.AMP,
                "|" => Symbol.PIPE,
                "<" => Symbol.LT,
                ">" => Symbol.GT,
                "=" => Symbol.EQUAL,
                "~" => Symbol.TILDE,
                _ => throw new ArgumentException("Invalid Argument")
            };
        }
    }
}