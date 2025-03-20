using FluentResults;
using JackAnalyzer.Enums;
using JackAnalyzer.Models;
namespace JackAnalyzer.Extensions 
{
    public static class ResultExtension
    {
        public static Result<Token> PrintErrorAndExitIfFailed (this Result<Token> token)
        {
            if (token.IsFailed)
            {
                foreach (var error in token.Errors)
                {
                    Console.Error.WriteLine(error.Message);
                }

                Environment.Exit(1);
            }

            return token;
        }

        public static Result<Token> ExpectOrExit(this Result<Token> token, Keyword keyword)
        {
            if (token.Value.Type != TokenType.KEYWORD || (Keyword)token.Value.Value != keyword)
            {
                Console.Error.WriteLine($"Expected '{keyword}' keyword");
                Environment.Exit(1);
            }

            return token;
        }

        public static Result<Token> ExpectOrExit(this Result<Token> token, TokenType tokenType)
        {
            if (token.Value.Type != tokenType)
            {
                Console.Error.WriteLine($"Expected '{tokenType}' token type");
                Environment.Exit(1);
            }

            return token;
        }

        public static Result<Token> ExpectOrExit(this Result<Token> token, Symbol symbol)
        {
            if (token.Value.Type != TokenType.SYMBOL || (Symbol)token.Value.Value != symbol)
            {
                Console.Error.WriteLine($"Expected '{symbol}' symbol");
                Environment.Exit(1);
            }

            return token;
        }

        public static bool Expect(this Result<Token> token, Symbol symbol)
        {
            return token.Value.Type == TokenType.SYMBOL && (Symbol)token.Value.Value == symbol;
        }

        public static bool Expect(this Result<Token> token, Keyword keyword)
        {
            return token.Value.Type == TokenType.KEYWORD && (Keyword)token.Value.Value == keyword;
        }

        public static bool Expect(this Result<Token> token, TokenType tokenType)
        {
            return token.Value.Type == tokenType;
        }
    }
}