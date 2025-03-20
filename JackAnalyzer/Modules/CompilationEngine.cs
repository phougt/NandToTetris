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
        private Result<Token> _currentResult;

        public CompilationEngine(JackTokenizer tokenizer)
        {
            _tokenizer = tokenizer;
        }

        public void CompileClass()
        {
            _currentResult = _tokenizer.Advance()
                                    .PrintErrorAndExitIfFailed()
                                    .ExpectOrExit(Keyword.CLASS);

            _currentResult = _tokenizer.Advance()
                    .PrintErrorAndExitIfFailed()
                    .ExpectOrExit(TokenType.IDENTIFIER);

            _currentResult = _tokenizer.Advance()
                    .PrintErrorAndExitIfFailed()
                    .ExpectOrExit(Symbol.LBRACE);

            _currentResult = _tokenizer.Advance()
                            .PrintErrorAndExitIfFailed();

            CompileClassVar();
            
            _currentResult.PrintErrorAndExitIfFailed()
                        .ExpectOrExit(Symbol.RBRACE);
        }

        private void CompileClassVar()
        {
            if (_currentResult.Expect(Keyword.STATIC) || _currentResult.Expect(Keyword.FIELD))
            {
                _currentResult = _tokenizer.Advance()
                                .PrintErrorAndExitIfFailed()
                                .ExpectOrExit(TokenType.IDENTIFIER);

                _currentResult = _tokenizer.Advance()
                                .PrintErrorAndExitIfFailed();

                while (_currentResult.Expect(Symbol.COMMA))
                {
                    _currentResult = _tokenizer.Advance()
                                    .PrintErrorAndExitIfFailed()
                                    .ExpectOrExit(TokenType.IDENTIFIER);

                    _currentResult = _tokenizer.Advance()
                                    .PrintErrorAndExitIfFailed();
                }

                _currentResult.PrintErrorAndExitIfFailed()
                            .ExpectOrExit(Symbol.SEMICOLON);
                
                _currentResult = _tokenizer.Advance()
                                .PrintErrorAndExitIfFailed();

                CompileClassVar();
            }

            CompileSubroutine();

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
