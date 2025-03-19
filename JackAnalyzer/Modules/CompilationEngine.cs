using FluentResults;
using JackAnalyzer.Enums;
using JackAnalyzer.Extensions;
using JackAnalyzer.Errors;
using JackAnalyzer.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JackAnalyzer.Modules
{
    public class CompilationEngine
    {
        private readonly JackTokenizer _tokenizer;
        private readonly StringBuilder _output;

        public CompilationEngine(JackTokenizer tokenizer)
        {
            _tokenizer = tokenizer;
        }

        private void Eat(Symbol symbol)
        {
            Result<Token> result = _tokenizer.Advance();

            if (result.IsFailed)
            {
                foreach (IError error in result.Errors)
                {
                    Console.Error.WriteLine(error.Message);
                }

                Environment.Exit(1);
            }

            Token tempToken = result.Value;
            TokenType tokenType = tempToken.Type;

            if (tokenType != TokenType.SYMBOL || (Symbol)tempToken.Value != symbol)
            {
                Console.Error.WriteLine($"Expected '{SymbolExtensions.ToString(symbol)}', but got {tempToken.Value} instead.");
                Environment.Exit(1);
            }
        }

        private void Eat(Keyword keyword)
        {
            Result<Token> result = _tokenizer.Advance();

            if (result.IsFailed)
            {
                foreach (IError error in result.Errors)
                {
                    Console.Error.WriteLine(error.Message);
                }

                Environment.Exit(1);
            }

            Token tempToken = result.Value;
            TokenType tokenType = tempToken.Type;

            if (tokenType != TokenType.KEYWORD || (Keyword)tempToken.Value != keyword)
            {
                Console.Error.WriteLine($"Expected '{keyword.ToString().ToLower()}', but got {tempToken.Value} instead.");
                Environment.Exit(1);
            }
        }

        private void Eat(TokenType tokenType)
        {
            Result<Token> result = _tokenizer.Advance();

            if (result.IsFailed)
            {
                foreach (IError error in result.Errors)
                {
                    Console.Error.WriteLine(error.Message);
                }

                Environment.Exit(1);
            }

            Token tempToken = result.Value;

            if (tempToken.Type != tokenType)
            {
                Console.Error.WriteLine($"Expected '{tokenType.ToString().ToLower()}', but got {tempToken.Value} instead.");
                Environment.Exit(1);
            }
        }

        public void CompileClass()
        {
            Eat(Keyword.CLASS);
            Eat(TokenType.IDENTIFIER);
            Eat(Symbol.LBRACE);
            Eat(Symbol.RBRACE);
        }

        private void CompileClassVar()
        {
        }

        private void CompileSubroutine()
        {
        }

        private void CompileParameterList()
        {
        }

        private void CompileVarDec()
        {
        }

        private void CompileStatements()
        {
        }

        private void CompileDo()
        {
        }

        private void CompileLet()
        {
        }

        private void CompileWhile()
        {
        }

        private void CompileReturn()
        {
        }

        private void CompileIf()
        {
        }

        private void CompileExpression()
        {
        }

        private void CompileTerm()
        {
        }

        private void CompileExpressionList()
        {
        }
    }
}
