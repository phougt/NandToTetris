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
            _output = new StringBuilder();
        }

        public void CompileClass()
        {
            _currentResult = _tokenizer.Advance()
                            .PrintErrorAndExitIfFailed()
                            .ExpectOrExit(Keyword.CLASS);

            _output.AppendLine("<class>");
            AppendKeyword(_currentResult);

            _currentResult = _tokenizer.Advance()
                    .PrintErrorAndExitIfFailed()
                    .ExpectOrExit(TokenType.IDENTIFIER);

            AppendIdentifier(_currentResult);

            _currentResult = _tokenizer.Advance()
                    .PrintErrorAndExitIfFailed()
                    .ExpectOrExit(Symbol.LBRACE);

            AppendSymbol(_currentResult);

            _currentResult = _tokenizer.Advance()
                            .PrintErrorAndExitIfFailed();

            while (_currentResult.Expect(Keyword.STATIC)
                    || _currentResult.Expect(Keyword.FIELD)
                    || _currentResult.Expect(Keyword.CONSTRUCTOR)
                    || _currentResult.Expect(Keyword.FUNCTION)
                    || _currentResult.Expect(Keyword.METHOD))
            {
                if (_currentResult.Expect(Keyword.STATIC) || _currentResult.Expect(Keyword.FIELD))
                {
                    CompileClassVar();
                }
                else
                {
                    CompileSubroutine();
                }
            }

            _currentResult.PrintErrorAndExitIfFailed()
                        .ExpectOrExit(Symbol.RBRACE);

            AppendSymbol(_currentResult);

            _output.AppendLine("</class>");
            StreamWriter writer = new StreamWriter("C:\\Users\\phoug\\Desktop\\output.xml");
            writer.Write(_output.ToString());
            writer.Close();
        }

        private void CompileClassVar()
        {
            if (_currentResult.Expect(Keyword.STATIC) || _currentResult.Expect(Keyword.FIELD))
            {
                _output.AppendLine("<classVarDec>");
                AppendKeyword(_currentResult);

                _currentResult = _tokenizer.Advance()
                                .PrintErrorAndExitIfFailed()
                                .ExpectTypeOrExit();

                if (_currentResult.ExpectBuiltInType())
                {
                    AppendKeyword(_currentResult);
                }
                else
                {
                    AppendIdentifier(_currentResult);
                }

                _currentResult = _tokenizer.Advance()
                                .PrintErrorAndExitIfFailed()
                                .ExpectOrExit(TokenType.IDENTIFIER);

                AppendIdentifier(_currentResult);

                _currentResult = _tokenizer.Advance()
                                .PrintErrorAndExitIfFailed();

                while (_currentResult.Expect(Symbol.COMMA))
                {
                    AppendSymbol(_currentResult);

                    _currentResult = _tokenizer.Advance()
                                    .PrintErrorAndExitIfFailed()
                                    .ExpectOrExit(TokenType.IDENTIFIER);

                    AppendIdentifier(_currentResult);

                    _currentResult = _tokenizer.Advance()
                                    .PrintErrorAndExitIfFailed();
                }

                _currentResult.PrintErrorAndExitIfFailed()
                            .ExpectOrExit(Symbol.SEMICOLON);

                AppendSymbol(_currentResult);

                _currentResult = _tokenizer.Advance()
                                .PrintErrorAndExitIfFailed();

                _output.AppendLine("</classVarDec>");
            }
        }

        private void CompileSubroutine()
        {
            if (_currentResult.Expect(Keyword.CONSTRUCTOR) || _currentResult.Expect(Keyword.FUNCTION) || _currentResult.Expect(Keyword.METHOD))
            {
                _output.AppendLine("<subroutineDec>");
                AppendKeyword(_currentResult);

                _currentResult = _tokenizer.Advance()
                                .PrintErrorAndExitIfFailed();

                if (!(_currentResult.Expect(Keyword.VOID) || _currentResult.ExpectType()))
                {
                    Console.Error.WriteLine("Expected 'void' or type");
                    Environment.Exit(1);
                }

                if (_currentResult.Expect(Keyword.VOID) || _currentResult.ExpectBuiltInType())
                {
                    AppendKeyword(_currentResult);
                }
                else
                {
                    AppendIdentifier(_currentResult);
                }

                _currentResult = _tokenizer.Advance()
                                .PrintErrorAndExitIfFailed()
                                .ExpectOrExit(TokenType.IDENTIFIER);

                AppendIdentifier(_currentResult);

                _currentResult = _tokenizer.Advance()
                                .PrintErrorAndExitIfFailed()
                                .ExpectOrExit(Symbol.LPAR);

                AppendSymbol(_currentResult);

                _currentResult = _tokenizer.Advance()
                                .PrintErrorAndExitIfFailed();

                CompileParameterList();

                _currentResult.PrintErrorAndExitIfFailed()
                            .ExpectOrExit(Symbol.RPAR);

                AppendSymbol(_currentResult);

                _currentResult = _tokenizer.Advance()
                                .PrintErrorAndExitIfFailed()
                                .ExpectOrExit(Symbol.LBRACE);

                AppendSymbol(_currentResult);

                _currentResult = _tokenizer.Advance()
                                .PrintErrorAndExitIfFailed();

                while (_currentResult.Expect(Keyword.VAR)
                        || _currentResult.Expect(Keyword.DO))
                {
                    if (_currentResult.Expect(Keyword.VAR))
                    {
                        CompileVarDec();
                    }
                    else if (_currentResult.Expect(Keyword.DO))
                    {
                        CompileDo();
                    }
                    else
                    {
                        _currentResult = _tokenizer.Advance()
                                        .PrintErrorAndExitIfFailed();
                    }
                }

                _currentResult.PrintErrorAndExitIfFailed()
                            .ExpectOrExit(Symbol.RBRACE);

                AppendSymbol(_currentResult);

                _currentResult = _tokenizer.Advance()
                                .PrintErrorAndExitIfFailed();

                _output.AppendLine("</subroutineDec>");
            }
        }

        private void CompileParameterList()
        {
            if (_currentResult.ExpectType())
            {
                _output.AppendLine("<parameterList>");

                if (_currentResult.ExpectBuiltInType())
                {
                    AppendKeyword(_currentResult);
                }
                else
                {
                    AppendIdentifier(_currentResult);
                }

                _currentResult = _tokenizer.Advance()
                                .PrintErrorAndExitIfFailed()
                                .ExpectOrExit(TokenType.IDENTIFIER);

                AppendIdentifier(_currentResult);

                _currentResult = _tokenizer.Advance()
                                .PrintErrorAndExitIfFailed();

                while (_currentResult.Expect(Symbol.COMMA))
                {
                    AppendSymbol(_currentResult);

                    _currentResult = _tokenizer.Advance()
                                    .PrintErrorAndExitIfFailed()
                                    .ExpectTypeOrExit();

                    if (_currentResult.ExpectBuiltInType())
                    {
                        AppendKeyword(_currentResult);
                    }
                    else
                    {
                        AppendIdentifier(_currentResult);
                    }

                    _currentResult = _tokenizer.Advance()
                                    .PrintErrorAndExitIfFailed()
                                    .ExpectOrExit(TokenType.IDENTIFIER);

                    AppendIdentifier(_currentResult);

                    _currentResult = _tokenizer.Advance()
                                    .PrintErrorAndExitIfFailed();
                }

                _output.AppendLine("</parameterList>");
            }
        }

        private void CompileVarDec()
        {
            if (_currentResult.Expect(Keyword.VAR))
            {
                _output.AppendLine("<varDec>");
                AppendKeyword(_currentResult);

                _currentResult = _tokenizer.Advance()
                            .PrintErrorAndExitIfFailed()
                            .ExpectTypeOrExit();

                if (_currentResult.ExpectBuiltInType())
                {
                    AppendKeyword(_currentResult);
                }
                else
                {
                    AppendIdentifier(_currentResult);
                }

                _currentResult = _tokenizer.Advance()
                            .PrintErrorAndExitIfFailed()
                            .ExpectOrExit(TokenType.IDENTIFIER);

                AppendIdentifier(_currentResult);

                _currentResult = _tokenizer.Advance()
                                .PrintErrorAndExitIfFailed();

                while (_currentResult.Expect(Symbol.COMMA))
                {
                    AppendSymbol(_currentResult);

                    _currentResult = _tokenizer.Advance()
                                    .PrintErrorAndExitIfFailed()
                                    .ExpectOrExit(TokenType.IDENTIFIER);

                    AppendIdentifier(_currentResult);

                    _currentResult = _tokenizer.Advance()
                                    .PrintErrorAndExitIfFailed();
                }

                _currentResult.PrintErrorAndExitIfFailed()
                            .ExpectOrExit(Symbol.SEMICOLON);

                AppendSymbol(_currentResult);

                _currentResult = _tokenizer.Advance()
                                .PrintErrorAndExitIfFailed();

                _output.AppendLine("</varDec>");
            }
        }

        private void CompileStatements()
        {
        }

        private void CompileDo()
        {
            if (_currentResult.Expect(Keyword.DO))
            {
                _output.AppendLine("<doStatement>");
                AppendKeyword(_currentResult);

                _currentResult = _tokenizer.Advance()
                                .PrintErrorAndExitIfFailed()
                                .ExpectOrExit(TokenType.IDENTIFIER);

                AppendIdentifier(_currentResult);

                _currentResult = _tokenizer.Advance()
                                .PrintErrorAndExitIfFailed();

                if (_currentResult.Expect(Symbol.LPAR))
                {
                    AppendSymbol(_currentResult);

                    _currentResult = _tokenizer.Advance()
                                    .PrintErrorAndExitIfFailed();
                    // CompileExpressionList();

                    _currentResult.PrintErrorAndExitIfFailed()
                                .ExpectOrExit(Symbol.RPAR);

                    AppendSymbol(_currentResult);
                }
                else if (_currentResult.Expect(Symbol.DOT))
                {
                    AppendSymbol(_currentResult);

                    _currentResult = _tokenizer.Advance()
                                    .PrintErrorAndExitIfFailed()
                                    .ExpectOrExit(TokenType.IDENTIFIER);

                    AppendIdentifier(_currentResult);

                    _currentResult = _tokenizer.Advance()
                                    .PrintErrorAndExitIfFailed()
                                    .ExpectOrExit(Symbol.LPAR);

                    AppendSymbol(_currentResult);

                    _currentResult = _tokenizer.Advance()
                                    .PrintErrorAndExitIfFailed();

                    // CompileExpressionList();

                    _currentResult.PrintErrorAndExitIfFailed()
                                .ExpectOrExit(Symbol.RPAR);
                    
                    AppendSymbol(_currentResult);
                }

                _currentResult = _tokenizer.Advance()
                                .PrintErrorAndExitIfFailed()
                                .ExpectOrExit(Symbol.SEMICOLON);

                AppendSymbol(_currentResult);

                _currentResult = _tokenizer.Advance()
                                .PrintErrorAndExitIfFailed();

                _output.AppendLine("</doStatement>");
            }
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
            _currentResult = _tokenizer.Advance()
                            .PrintErrorAndExitIfFailed();
        }

        private void AppendKeyword(Result<Token> token)
        {
            _output.AppendLine($"<keyword> {((Keyword)token.Value.Value).ToString().ToLower()} </keyword>");
        }

        private void AppendIdentifier(Result<Token> token)
        {
            _output.AppendLine($"<identifier> {(string)_currentResult.Value.Value} </identifier>");
        }

        private void AppendSymbol(Result<Token> token)
        {
            _output.AppendLine($"<symbol> {((Symbol)_currentResult.Value.Value).ToSymbolString()} </symbol>");
        }
    }
}
