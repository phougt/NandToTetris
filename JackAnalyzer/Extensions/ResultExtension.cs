using FluentResults;
using JackAnalyzer.Enums;
using JackAnalyzer.Errors;
using JackAnalyzer.Models;
namespace JackAnalyzer.Extensions 
{
    public static class ResultExtension
    {
        public static SyntaxError CreateTypeExpectedError(this Result<Token> token)
        {
            return new SyntaxError { Message = $"[Error] File: {token.Value.Filename}. Expected type. Line: {token.Value.Row}" };
        }

        public static SyntaxError CreateExpectedError(this Result<Token> token, Keyword keyword)
        {
            return new SyntaxError { Message = $"[Error] File: {token.Value.Filename}. Expected '{keyword}' keyword. Line: {token.Value.Row}" };
        }

        public static SyntaxError CreateExpectedError(this Result<Token> token, TokenType type)
        {
            return new SyntaxError { Message = $"[Error] File: { token.Value.Filename}. Expected '{type}' token type. Line: {token.Value.Row}" };
        }

        public static SyntaxError CreateExpectedError(this Result<Token> token, Symbol symbol)
        {
            return new SyntaxError { Message = $"[Error] File: {token.Value.Filename}. Expected '{symbol.ToSymbolString()}' symbol. Line: {token.Value.Row}" };
        }

        public static void PrintErrorAndExitIfFailed (this Result token)
        {
            if (token.IsFailed)
            {
                foreach (var error in token.Errors)
                {
                    Console.Error.WriteLine(error.ToString());
                    Console.Error.WriteLine(error.Message);
                }

                Environment.Exit(1);
            }
        }

        public static Result<Token> ExpectOrExit(this Result<Token> token, Keyword keyword)
        {
            if (token.Value.Type != TokenType.KEYWORD || (Keyword)token.Value.Value != keyword)
            {
                Console.Error.WriteLine($"[Error] File: {token.Value.Filename}. Expected '{keyword}' keyword. Line: {token.Value.Row}");
                Environment.Exit(1);
            }

            return token;
        }

        public static Result<Token> ExpectOrExit(this Result<Token> token, TokenType tokenType)
        {
            if (token.Value.Type != tokenType)
            {
                Console.Error.WriteLine($"[Error] File: { token.Value.Filename}. Expected '{tokenType}' token type. Line: {token.Value.Row}");
                Environment.Exit(1);
            }

            return token;
        }

        public static Result<Token> ExpectOrExit(this Result<Token> token, Symbol symbol)
        {
            if (token.Value.Type != TokenType.SYMBOL || (Symbol)token.Value.Value != symbol)
            {
                Console.Error.WriteLine($"[Error] File: {token.Value.Filename}. Expected '{symbol}' symbol. Line: {token.Value.Row}");
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

        public static Result<Token> ExpectTypeOrExit(this Result<Token> token)
        {
            bool isType = token.Expect(Keyword.INT) || token.Expect(Keyword.CHAR) || token.Expect(Keyword.BOOLEAN) || token.Expect(TokenType.IDENTIFIER);

            if (!isType)
            {
                Console.Error.WriteLine($"[Error] File: {token.Value.Filename}. Expected type.  Line: {token.Value.Row}");
                Environment.Exit(1);
            }

            return token;
        }

        public static bool ExpectType(this Result<Token> token)
        {
            return token.Expect(Keyword.INT) || token.Expect(Keyword.CHAR) || token.Expect(Keyword.BOOLEAN) || token.Expect(TokenType.IDENTIFIER);
        }

        public static bool ExpectBuiltInType(this Result<Token> token)
        {
            return token.Expect(Keyword.INT) || token.Expect(Keyword.CHAR) || token.Expect(Keyword.BOOLEAN);
        }

        public static bool IsKeywordConstant(this Result<Token> token)
        {
            return token.Expect(Keyword.TRUE)
                || token.Expect(Keyword.FALSE)
                || token.Expect(Keyword.NULL)
                || token.Expect(Keyword.THIS);
        }

        public static bool IsUnaryOperator(this Result<Token> token)
        {
            return token.Expect(Symbol.MINUS)
                || token.Expect(Symbol.PLUS)
                || token.Expect(Symbol.STAR)
                || token.Expect(Symbol.SLASH)
                || token.Expect(Symbol.AMP)
                || token.Expect(Symbol.PIPE)
                || token.Expect(Symbol.LT)
                || token.Expect(Symbol.GT)
                || token.Expect(Symbol.EQUAL)
                || token.Expect(Symbol.TILDE);
        }


        public static bool IsTerm(this Result<Token> token)
        {

            return token.Expect(TokenType.INT_CONST)
                || token.Expect(TokenType.STRING_CONST)
                || token.Expect(TokenType.IDENTIFIER)
                || token.Expect(Keyword.DO)
                || token.Expect(Symbol.LPAR)
                || token.IsKeywordConstant()
                || token.IsUnaryOperator();
        }

        public static bool IsStatements(this Result<Token> token)
        {
            return token.Expect(Keyword.LET)
                || token.Expect(Keyword.IF)
                || token.Expect(Keyword.WHILE)
                || token.Expect(Keyword.DO)
                || token.Expect(Keyword.RETURN);
        }
    }
}