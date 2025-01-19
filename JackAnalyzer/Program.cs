using JackAnalyzer.Modules;
using JackAnalyzer.Models;
using FluentResults;
using JackAnalyzer.Enums;

namespace JackAnalyzer
{
    class Program
    {
        static void Main(string[] args)
        {
            string filePath = "C:\\Users\\phoug\\Desktop\\test.txt";
            JackTokenizer tokenizer = new JackTokenizer(filePath);
            while (tokenizer.HasMoreTokens)
            {
                Result<Token> result = tokenizer.Advance();
                if (result.IsSuccess)
                {
                    Token token = result.Value;

                    if (token.Type == TokenType.INT_CONST)
                    {
                        Console.WriteLine($"INT_CONST: {(int)token.Value}");
                    }
                    else if (token.Type == TokenType.STRING_CONST)
                    {
                        Console.WriteLine($"STRING_CONST: {(string)token.Value}");
                    }
                    else if (token.Type == TokenType.KEYWORD)
                    {
                        Console.WriteLine($"KEYWORD: {(string)token.Value}");
                    }
                    else if (token.Type == TokenType.SYMBOL)
                    {
                        Console.WriteLine($"SYMBOL: {(char)token.Value}");
                    }
                    else if (token.Type == TokenType.IDENTIFIER)
                    {
                        Console.WriteLine($"IDENTIFIER: {(string)token.Value}");
                    }
                    else
                    {
                        foreach (var error in result.Errors)
                        {
                            Console.WriteLine(error.Message);
                        }
                    }
                }
            }
        }
    }
}