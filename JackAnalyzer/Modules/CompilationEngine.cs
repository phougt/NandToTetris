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
        private string _outputFilePath;
        public string OutputFilePath
        {
            get { return _outputFilePath; }
            set { _outputFilePath = value; }
        }
        private Result<Token> _currentResult;

        public CompilationEngine(JackTokenizer tokenizer)
        {
            _tokenizer = tokenizer;
            _output = new StringBuilder();
            _outputFilePath = string.Empty;
        }

        public CompilationEngine(JackTokenizer tokenizer, string outputFilePath)
        {
            _tokenizer = tokenizer;
            _output = new StringBuilder();
            _outputFilePath = outputFilePath;
        }

        public bool TryWriteOutputToFile()
        {
            try
            {
                StreamWriter writer = new(_outputFilePath);
                writer.Write(_output.ToString());
                writer.Close();
            }
            catch (Exception)
            {
                return false;
            }

            return true;
        }

        public bool WriteOutputToFile()
        {
            StreamWriter writer = new(_outputFilePath);
            writer.Write(_output.ToString());
            writer.Close();
            return false;
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
        }

        private void CompileClassVar()
        {
            _output.AppendLine("<classVarDec>");

            if (_currentResult.Expect(Keyword.STATIC) || _currentResult.Expect(Keyword.FIELD))
            {
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

            }

            _output.AppendLine("</classVarDec>");
        }

        private void CompileSubroutine()
        {
            _output.AppendLine("<subroutineDec>");

            if (_currentResult.Expect(Keyword.CONSTRUCTOR) || _currentResult.Expect(Keyword.FUNCTION) || _currentResult.Expect(Keyword.METHOD))
            {
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

                _output.AppendLine("<subroutineBody>");

                _currentResult = _tokenizer.Advance()
                                .PrintErrorAndExitIfFailed()
                                .ExpectOrExit(Symbol.LBRACE);

                AppendSymbol(_currentResult);

                _currentResult = _tokenizer.Advance()
                                .PrintErrorAndExitIfFailed();

                while (_currentResult.Expect(Keyword.VAR) || _currentResult.IsStatements())
                {
                    if (_currentResult.Expect(Keyword.VAR))
                    {
                        CompileVarDec();
                    }
                    else
                    {
                        CompileStatements();
                    }
                }

                _currentResult.PrintErrorAndExitIfFailed()
                            .ExpectOrExit(Symbol.RBRACE);

                AppendSymbol(_currentResult);
                _output.AppendLine("</subroutineBody>");

                _currentResult = _tokenizer.Advance()
                                .PrintErrorAndExitIfFailed();

            }

            _output.AppendLine("</subroutineDec>");
        }

        private void CompileParameterList()
        {
            _output.AppendLine("<parameterList>");

            if (_currentResult.ExpectType())
            {

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

            }

            _output.AppendLine("</parameterList>");
        }

        private void CompileVarDec()
        {
            _output.AppendLine("<varDec>");
            if (_currentResult.Expect(Keyword.VAR))
            {
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

            }

            _output.AppendLine("</varDec>");
        }

        private void CompileStatements()
        {
            _output.AppendLine("<statements>");

            if (_currentResult.Expect(Keyword.LET)
                || _currentResult.Expect(Keyword.DO)
                || _currentResult.Expect(Keyword.IF)
                || _currentResult.Expect(Keyword.WHILE)
                || _currentResult.Expect(Keyword.RETURN))
            {

                while (_currentResult.Expect(Keyword.LET)
                        || _currentResult.Expect(Keyword.DO)
                        || _currentResult.Expect(Keyword.IF)
                        || _currentResult.Expect(Keyword.WHILE)
                        || _currentResult.Expect(Keyword.RETURN))
                {
                    if (_currentResult.Expect(Keyword.LET))
                    {
                        CompileLet();
                    }
                    else if (_currentResult.Expect(Keyword.DO))
                    {
                        CompileDo();
                    }
                    else if (_currentResult.Expect(Keyword.IF))
                    {
                        CompileIf();
                    }
                    else if (_currentResult.Expect(Keyword.WHILE))
                    {
                        CompileWhile();
                    }
                    else
                    {
                        CompileReturn();
                    }
                }

            }

            _output.AppendLine("</statements>");
        }

        private void CompileDo()
        {
            _output.AppendLine("<doStatement>");
            if (_currentResult.Expect(Keyword.DO))
            {
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

                    CompileExpressionList();

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

                    CompileExpressionList();

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
            }

            _output.AppendLine("</doStatement>");
        }

        private void CompileLet()
        {
            _output.AppendLine("<letStatement>");

            if (_currentResult.Expect(Keyword.LET))
            {
                AppendKeyword(_currentResult);

                _currentResult = _tokenizer.Advance()
                                .PrintErrorAndExitIfFailed()
                                .ExpectOrExit(TokenType.IDENTIFIER);

                AppendIdentifier(_currentResult);

                _currentResult = _tokenizer.Advance()
                                .PrintErrorAndExitIfFailed();

                if (_currentResult.Expect(Symbol.LBRACK))
                {
                    AppendSymbol(_currentResult);

                    _currentResult = _tokenizer.Advance()
                                    .PrintErrorAndExitIfFailed();

                    CompileExpression();

                    _currentResult.PrintErrorAndExitIfFailed()
                                .ExpectOrExit(Symbol.RBRACK);

                    AppendSymbol(_currentResult);

                    _currentResult = _tokenizer.Advance()
                                    .PrintErrorAndExitIfFailed();
                }

                _currentResult.PrintErrorAndExitIfFailed()
                            .ExpectOrExit(Symbol.EQUAL);

                AppendSymbol(_currentResult);

                _currentResult = _tokenizer.Advance()
                                .PrintErrorAndExitIfFailed();

                CompileExpression();

                _currentResult.PrintErrorAndExitIfFailed()
                            .ExpectOrExit(Symbol.SEMICOLON);

                AppendSymbol(_currentResult);

                _currentResult = _tokenizer.Advance()
                                .PrintErrorAndExitIfFailed();
            }

            _output.AppendLine("</letStatement>");
        }

        private void CompileWhile()
        {
            _output.AppendLine("<whileStatement>");

            if (_currentResult.Expect(Keyword.WHILE))
            {
                AppendKeyword(_currentResult);

                _currentResult = _tokenizer.Advance()
                                .PrintErrorAndExitIfFailed()
                                .ExpectOrExit(Symbol.LPAR);

                AppendSymbol(_currentResult);

                _currentResult = _tokenizer.Advance()
                                .PrintErrorAndExitIfFailed();

                CompileExpression();

                _currentResult.PrintErrorAndExitIfFailed()
                            .ExpectOrExit(Symbol.RPAR);

                AppendSymbol(_currentResult);

                _currentResult = _tokenizer.Advance()
                                .PrintErrorAndExitIfFailed()
                                .ExpectOrExit(Symbol.LBRACE);

                AppendSymbol(_currentResult);

                _currentResult = _tokenizer.Advance()
                                .PrintErrorAndExitIfFailed();

                CompileStatements();

                _currentResult.PrintErrorAndExitIfFailed()
                            .ExpectOrExit(Symbol.RBRACE);

                AppendSymbol(_currentResult);

                _currentResult = _tokenizer.Advance()
                                .PrintErrorAndExitIfFailed();
            }

            _output.AppendLine("</whileStatement>");
        }

        private void CompileReturn()
        {
            _output.AppendLine("<returnStatement>");

            if (_currentResult.Expect(Keyword.RETURN))
            {
                AppendKeyword(_currentResult);

                _currentResult = _tokenizer.Advance()
                                .PrintErrorAndExitIfFailed();

                if (!_currentResult.Expect(Symbol.SEMICOLON))
                {
                    CompileExpression();
                }

                _currentResult.PrintErrorAndExitIfFailed()
                            .ExpectOrExit(Symbol.SEMICOLON);

                AppendSymbol(_currentResult);

                _currentResult = _tokenizer.Advance()
                                .PrintErrorAndExitIfFailed();
            }

            _output.AppendLine("</returnStatement>");
        }

        private void CompileIf()
        {
            _output.AppendLine("<ifStatement>");

            if (_currentResult.Expect(Keyword.IF))
            {
                AppendKeyword(_currentResult);

                _currentResult = _tokenizer.Advance()
                                .PrintErrorAndExitIfFailed()
                                .ExpectOrExit(Symbol.LPAR);

                AppendSymbol(_currentResult);

                _currentResult = _tokenizer.Advance()
                                .PrintErrorAndExitIfFailed();

                if (!_currentResult.IsTerm())
                {
                    Console.Error.WriteLine("Expected expression");
                    Environment.Exit(1);
                }

                CompileExpression();

                _currentResult.PrintErrorAndExitIfFailed()
                            .ExpectOrExit(Symbol.RPAR);

                AppendSymbol(_currentResult);

                _currentResult = _tokenizer.Advance()
                                .PrintErrorAndExitIfFailed()
                                .ExpectOrExit(Symbol.LBRACE);

                AppendSymbol(_currentResult);

                _currentResult = _tokenizer.Advance()
                                .PrintErrorAndExitIfFailed();

                _output.AppendLine("<statements>");

                while (_currentResult.IsStatements())
                {

                    if (_currentResult.Expect(Keyword.LET))
                    {
                        CompileLet();
                    }
                    else if (_currentResult.Expect(Keyword.DO))
                    {
                        CompileDo();
                    }
                    else if (_currentResult.Expect(Keyword.IF))
                    {
                        CompileIf();
                    }
                    else if (_currentResult.Expect(Keyword.WHILE))
                    {
                        CompileWhile();
                    }
                    else if (_currentResult.Expect(Keyword.RETURN))
                    {
                        CompileReturn();
                    }
                }

                _output.AppendLine("</statements>");

                _currentResult.PrintErrorAndExitIfFailed()
                            .ExpectOrExit(Symbol.RBRACE);

                AppendSymbol(_currentResult);

                _currentResult = _tokenizer.Advance()
                                .PrintErrorAndExitIfFailed();

                if (_currentResult.Expect(Keyword.ELSE))
                {
                    AppendKeyword(_currentResult);

                    _currentResult = _tokenizer.Advance()
                                    .PrintErrorAndExitIfFailed()
                                    .ExpectOrExit(Symbol.LBRACE);

                    AppendSymbol(_currentResult);

                    _currentResult = _tokenizer.Advance()
                                    .PrintErrorAndExitIfFailed();

                    CompileStatements();

                    _currentResult.PrintErrorAndExitIfFailed()
                                .ExpectOrExit(Symbol.RBRACE);

                    AppendSymbol(_currentResult);

                    _currentResult = _tokenizer.Advance()
                                    .PrintErrorAndExitIfFailed();
                }
            }

            _output.AppendLine("</ifStatement>");
        }

        private void CompileExpression()
        {
            _output.AppendLine("<expression>");
            if (_currentResult.IsTerm())
            {
                CompileTerm();

                while (_currentResult.IsUnaryOperator())
                {
                    AppendSymbol(_currentResult);
                    _currentResult = _tokenizer.Advance()
                                    .PrintErrorAndExitIfFailed();

                    if (!_currentResult.IsTerm())
                    {
                        Console.Error.WriteLine("Expected term");
                        Environment.Exit(1);
                    }

                    CompileTerm();
                }
            }

            _output.AppendLine("</expression>");
        }

        private void CompileTerm()
        {
            _output.AppendLine("<term>");

            if (!_currentResult.IsTerm())
            {
                return;
            }

            if (_currentResult.IsKeywordConstant())
            {
                AppendKeyword(_currentResult);
                _currentResult = _tokenizer.Advance()
                                .PrintErrorAndExitIfFailed();
            }
            else if (_currentResult.IsUnaryOperator())
            {
                AppendSymbol(_currentResult);

                if (!_currentResult.IsTerm())
                {
                    Console.Error.WriteLine("Expected term");
                    Environment.Exit(1);
                }

                CompileTerm();
            }
            else if (_currentResult.Expect(TokenType.INT_CONST))
            {
                AppendIntConstant(_currentResult);
                _currentResult = _tokenizer.Advance()
                                .PrintErrorAndExitIfFailed();
            }
            else if (_currentResult.Expect(TokenType.STRING_CONST))
            {
                AppendStringConstant(_currentResult);
                _currentResult = _tokenizer.Advance()
                                .PrintErrorAndExitIfFailed();
            }
            else if (_currentResult.Expect(TokenType.IDENTIFIER))
            {
                AppendIdentifier(_currentResult);
                _currentResult = _tokenizer.Advance()
                                .PrintErrorAndExitIfFailed();

                if (_currentResult.Expect(Symbol.LBRACK))
                {
                    AppendSymbol(_currentResult);
                    _currentResult = _tokenizer.Advance()
                                    .PrintErrorAndExitIfFailed();
                    CompileExpression();
                    _currentResult.PrintErrorAndExitIfFailed()
                                .ExpectOrExit(Symbol.RBRACK);
                    AppendSymbol(_currentResult);
                }
            }
            else if (_currentResult.Expect(Keyword.DO))
            {
                _currentResult = _tokenizer.Advance()
                                .PrintErrorAndExitIfFailed();
                CompileDo();
            }
            else if (_currentResult.Expect(Symbol.LPAR))
            {
                AppendSymbol(_currentResult);
                _currentResult = _tokenizer.Advance()
                                .PrintErrorAndExitIfFailed();

                CompileExpression();

                _currentResult.PrintErrorAndExitIfFailed()
                            .ExpectOrExit(Symbol.RPAR);

                AppendSymbol(_currentResult);
                _currentResult = _tokenizer.Advance()
                                .PrintErrorAndExitIfFailed();
            }

            _output.AppendLine("</term>");
        }

        private void CompileExpressionList()
        {
            _output.AppendLine("<expressionList>");
            if (_currentResult.IsTerm())
            {
                CompileExpression();

                while (_currentResult.Expect(Symbol.COMMA))
                {
                    AppendSymbol(_currentResult);

                    _currentResult = _tokenizer.Advance()
                                    .PrintErrorAndExitIfFailed();

                    CompileExpression();
                }
            }

            _output.AppendLine("</expressionList>");
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

        private void AppendStringConstant(Result<Token> token)
        {
            _output.AppendLine($"<stringConstant> {_currentResult.Value.Value} </stringConstant>");
        }

        private void AppendIntConstant(Result<Token> token)
        {
            _output.AppendLine($"<integerConstant> {_currentResult.Value.Value} </integerConstant>");
        }
    }
}
