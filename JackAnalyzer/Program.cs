using JackAnalyzer.Modules;
using JackAnalyzer.Models;
using FluentResults;
using JackAnalyzer.Enums;
using System.ComponentModel.Design;
using JackAnalyzer.Errors;

namespace JackAnalyzer
{
    class Program
    {
        static void Main(string[] args)
        {
            string filePath = "C:\\Users\\phoug\\Desktop\\test.txt";
            JackTokenizer tokenizer = new JackTokenizer(filePath);

            for (int i = 1; i <= 33; i++)
            {
                Result<Token> result = tokenizer.Advance();
                if (result.IsSuccess)
                {
                    Token token = result.Value;

                    switch (token.Type)
                    {
                        case TokenType.SYMBOL:
                            Console.WriteLine($"SYMBOL: {(char)token.Value}");
                            break;
                        case TokenType.INT_CONST:
                            Console.WriteLine($"INT_CONST: {(int)token.Value}");
                            break;
                        case TokenType.STRING_CONST:
                            Console.WriteLine($"STRING_CONST: {(string)token.Value}");
                            break;
                        case TokenType.KEYWORD:
                            Console.WriteLine($"KEYWORD: {(KeywordType)token.Value}");
                            break;
                        case TokenType.IDENTIFIER:
                            Console.WriteLine($"IDENTIFIER: {(string)token.Value}");
                            break;
                    }
                }
                else if (result.HasError<InvalidCharError>())
                {
                    foreach (IError error in result.Errors)
                    {
                        Console.WriteLine(error.Message);
                    }

                    return;
                }
            }
        }
    }
}