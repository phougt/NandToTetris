using JackAnalyzer.Enums;
namespace JackAnalyzer.Extensions
{
    public static class SymbolExtensions
    {
        public static string ToString(this Symbol symbol)
        {
            return symbol switch
            {
                Symbol.LBRACE => "{",
                Symbol.RBRACE => "}",
                Symbol.LPAR => "(",
                Symbol.RPAR => ")",
                Symbol.LBRACK => "[",
                Symbol.RBRACK => "]",
                Symbol.DOT => ".",
                Symbol.COMMA => ",",
                Symbol.SEMICOLON => ";",
                Symbol.PLUS => "+",
                Symbol.MINUS => "-",
                Symbol.STAR => "*",
                Symbol.SLASH => "/",
                Symbol.AMP => "&",
                Symbol.PIPE => "|",
                Symbol.LT => "<",
                Symbol.GT => ">",
                Symbol.EQUAL => "=",
                Symbol.TILDE => "~",
                _ => string.Empty
            };
        }
    }
}