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
using System.Reflection.Metadata.Ecma335;

namespace JackAnalyzer.Modules
{
    public class CompilationEngine : IDisposable
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

        public Result CompileClass()
        {
            _currentResult = _tokenizer.Advance();
            if (_currentResult.IsFailed) return Result.Fail(_currentResult.Errors);
            if (!_currentResult.Expect(Keyword.CLASS)) return Result.Fail(_currentResult.CreateExpectedError(Keyword.CLASS));

            _output.AppendLine("<class>");
            AppendKeyword(_currentResult);

            _currentResult = _tokenizer.Advance();
            if (_currentResult.IsFailed) return Result.Fail(_currentResult.Errors);
            if (!_currentResult.Expect(TokenType.IDENTIFIER)) return Result.Fail(_currentResult.CreateExpectedError(TokenType.IDENTIFIER));

            AppendIdentifier(_currentResult);

            _currentResult = _tokenizer.Advance();
            if (_currentResult.IsFailed) return Result.Fail(_currentResult.Errors);
            if (!_currentResult.Expect(Symbol.LBRACE)) return Result.Fail(_currentResult.CreateExpectedError(Symbol.LBRACE));

            AppendSymbol(_currentResult);

            _currentResult = _tokenizer.Advance();
            if (_currentResult.IsFailed) return Result.Fail(_currentResult.Errors);

            while (_currentResult.Expect(Keyword.STATIC)
                    || _currentResult.Expect(Keyword.FIELD)
                    || _currentResult.Expect(Keyword.CONSTRUCTOR)
                    || _currentResult.Expect(Keyword.FUNCTION)
                    || _currentResult.Expect(Keyword.METHOD))
            {
                if (_currentResult.Expect(Keyword.STATIC) || _currentResult.Expect(Keyword.FIELD))
                {
                    var result = CompileClassVar();
                    if (result.IsFailed) return result;
                }
                else
                {
                    var result = CompileSubroutine();
                    if (result.IsFailed) return result;
                }
            }

            if (_currentResult.IsFailed) return Result.Fail(_currentResult.Errors);
            if (!_currentResult.Expect(Symbol.RBRACE)) return Result.Fail(_currentResult.CreateExpectedError(Symbol.RBRACE));

            AppendSymbol(_currentResult);

            _output.AppendLine("</class>");

            return Result.Ok();
        }

        private Result CompileClassVar()
        {
            _output.AppendLine("<classVarDec>");

            if (_currentResult.Expect(Keyword.STATIC) || _currentResult.Expect(Keyword.FIELD))
            {
                AppendKeyword(_currentResult);

                _currentResult = _tokenizer.Advance();
                if (_currentResult.IsFailed) return Result.Fail(_currentResult.Errors);
                if (!_currentResult.ExpectType()) return Result.Fail(_currentResult.CreateTypeExpectedError());

                if (_currentResult.ExpectBuiltInType())
                {
                    AppendKeyword(_currentResult);
                }
                else
                {
                    AppendIdentifier(_currentResult);
                }

                _currentResult = _tokenizer.Advance();
                if (_currentResult.IsFailed) return Result.Fail(_currentResult.Errors);
                if (!_currentResult.Expect(TokenType.IDENTIFIER)) return Result.Fail(_currentResult.CreateExpectedError(TokenType.IDENTIFIER));

                AppendIdentifier(_currentResult);

                _currentResult = _tokenizer.Advance();
                if (_currentResult.IsFailed) return Result.Fail(_currentResult.Errors);

                while (_currentResult.Expect(Symbol.COMMA))
                {
                    AppendSymbol(_currentResult);

                    _currentResult = _tokenizer.Advance();
                    if (_currentResult.IsFailed) return Result.Fail(_currentResult.Errors);
                    if (!_currentResult.Expect(TokenType.IDENTIFIER)) return Result.Fail(_currentResult.CreateExpectedError(TokenType.IDENTIFIER));

                    AppendIdentifier(_currentResult);

                    _currentResult = _tokenizer.Advance();
                    if (_currentResult.IsFailed) return Result.Fail(_currentResult.Errors);
                }

                if (_currentResult.IsFailed) return Result.Fail(_currentResult.Errors);
                if (!_currentResult.Expect(Symbol.SEMICOLON)) return Result.Fail(_currentResult.CreateExpectedError(Symbol.SEMICOLON));

                AppendSymbol(_currentResult);

                _currentResult = _tokenizer.Advance();
                if (_currentResult.IsFailed) return Result.Fail(_currentResult.Errors);
            }

            _output.AppendLine("</classVarDec>");
            return Result.Ok();
        }

        private Result CompileSubroutine()
        {
            _output.AppendLine("<subroutineDec>");

            if (_currentResult.Expect(Keyword.CONSTRUCTOR) || _currentResult.Expect(Keyword.FUNCTION) || _currentResult.Expect(Keyword.METHOD))
            {
                AppendKeyword(_currentResult);

                _currentResult = _tokenizer.Advance();
                if (_currentResult.IsFailed) return Result.Fail(_currentResult.Errors);

                if (!(_currentResult.Expect(Keyword.VOID) || _currentResult.ExpectType()))
                {
                    return Result.Fail(_currentResult.CreateTypeExpectedError());
                }

                if (_currentResult.Expect(Keyword.VOID) || _currentResult.ExpectBuiltInType())
                {
                    AppendKeyword(_currentResult);
                }
                else
                {
                    AppendIdentifier(_currentResult);
                }

                _currentResult = _tokenizer.Advance();
                if (_currentResult.IsFailed) return Result.Fail(_currentResult.Errors);
                if (!_currentResult.Expect(TokenType.IDENTIFIER)) return Result.Fail(_currentResult.CreateExpectedError(TokenType.IDENTIFIER));

                AppendIdentifier(_currentResult);

                _currentResult = _tokenizer.Advance();
                if (_currentResult.IsFailed) return Result.Fail(_currentResult.Errors);
                if (!_currentResult.Expect(Symbol.LPAR)) return Result.Fail(_currentResult.CreateExpectedError(Symbol.LPAR));

                AppendSymbol(_currentResult);

                _currentResult = _tokenizer.Advance();
                if (_currentResult.IsFailed) return Result.Fail(_currentResult.Errors);

                var result = CompileParameterList();
                if (result.IsFailed) return result;

                if (_currentResult.IsFailed) return Result.Fail(_currentResult.Errors);
                if (!_currentResult.Expect(Symbol.RPAR)) return Result.Fail(_currentResult.CreateExpectedError(Symbol.RPAR));

                AppendSymbol(_currentResult);

                _output.AppendLine("<subroutineBody>");

                _currentResult = _tokenizer.Advance();
                if (_currentResult.IsFailed) return Result.Fail(_currentResult.Errors);
                if (!_currentResult.Expect(Symbol.LBRACE)) return Result.Fail(_currentResult.CreateExpectedError(Symbol.LBRACE));

                AppendSymbol(_currentResult);

                _currentResult = _tokenizer.Advance();
                if (_currentResult.IsFailed) return Result.Fail(_currentResult.Errors);

                while (_currentResult.Expect(Keyword.VAR) || _currentResult.IsStatements())
                {
                    if (_currentResult.Expect(Keyword.VAR))
                    {
                        var temp = CompileVarDec();
                        if (temp.IsFailed) return temp;
                    }
                    else
                    {
                        var temp = CompileStatements();
                        if (temp.IsFailed) return temp;
                    }
                }

                if (_currentResult.IsFailed) return Result.Fail(_currentResult.Errors);
                if (!_currentResult.Expect(Symbol.RBRACE)) return Result.Fail(_currentResult.CreateExpectedError(Symbol.RBRACE));

                AppendSymbol(_currentResult);
                _output.AppendLine("</subroutineBody>");

                _currentResult = _tokenizer.Advance();
                if (_currentResult.IsFailed) return Result.Fail(_currentResult.Errors);
            }

            _output.AppendLine("</subroutineDec>");
            return Result.Ok();
        }

        private Result CompileParameterList()
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

                _currentResult = _tokenizer.Advance();
                if (_currentResult.IsFailed) return Result.Fail(_currentResult.Errors);
                if (!_currentResult.Expect(TokenType.IDENTIFIER)) return Result.Fail(_currentResult.CreateExpectedError(TokenType.IDENTIFIER));

                AppendIdentifier(_currentResult);

                _currentResult = _tokenizer.Advance();
                if (_currentResult.IsFailed) return Result.Fail(_currentResult.Errors);

                while (_currentResult.Expect(Symbol.COMMA))
                {
                    AppendSymbol(_currentResult);

                    _currentResult = _tokenizer.Advance();
                    if (_currentResult.IsFailed) return Result.Fail(_currentResult.Errors);
                    if (!_currentResult.ExpectType()) return Result.Fail(_currentResult.CreateTypeExpectedError());

                    if (_currentResult.ExpectBuiltInType())
                    {
                        AppendKeyword(_currentResult);
                    }
                    else
                    {
                        AppendIdentifier(_currentResult);
                    }

                    _currentResult = _tokenizer.Advance();
                    if (_currentResult.IsFailed) return Result.Fail(_currentResult.Errors);
                    if (!_currentResult.Expect(TokenType.IDENTIFIER)) return Result.Fail(_currentResult.CreateExpectedError(TokenType.IDENTIFIER));

                    AppendIdentifier(_currentResult);

                    _currentResult = _tokenizer.Advance();
                    if (_currentResult.IsFailed) return Result.Fail(_currentResult.Errors);
                }

            }

            _output.AppendLine("</parameterList>");
            return Result.Ok();
        }

        private Result CompileVarDec()
        {
            _output.AppendLine("<varDec>");
            if (_currentResult.Expect(Keyword.VAR))
            {
                AppendKeyword(_currentResult);

                _currentResult = _tokenizer.Advance();
                if (_currentResult.IsFailed) return Result.Fail(_currentResult.Errors);
                if (!_currentResult.ExpectType()) return Result.Fail(_currentResult.CreateTypeExpectedError());

                if (_currentResult.ExpectBuiltInType())
                {
                    AppendKeyword(_currentResult);
                }
                else
                {
                    AppendIdentifier(_currentResult);
                }

                _currentResult = _tokenizer.Advance();
                if (_currentResult.IsFailed) return Result.Fail(_currentResult.Errors);
                if (!_currentResult.Expect(TokenType.IDENTIFIER)) return Result.Fail(_currentResult.CreateExpectedError(TokenType.IDENTIFIER));

                AppendIdentifier(_currentResult);

                _currentResult = _tokenizer.Advance();
                if (_currentResult.IsFailed) return Result.Fail(_currentResult.Errors);

                while (_currentResult.Expect(Symbol.COMMA))
                {
                    AppendSymbol(_currentResult);

                    _currentResult = _tokenizer.Advance();
                    if (_currentResult.IsFailed) return Result.Fail(_currentResult.Errors);
                    if (!_currentResult.Expect(TokenType.IDENTIFIER)) return Result.Fail(_currentResult.CreateExpectedError(TokenType.IDENTIFIER));

                    AppendIdentifier(_currentResult);

                    _currentResult = _tokenizer.Advance();
                    if (_currentResult.IsFailed) return Result.Fail(_currentResult.Errors);
                }

                if (_currentResult.IsFailed) return Result.Fail(_currentResult.Errors);
                if (!_currentResult.Expect(Symbol.SEMICOLON)) return Result.Fail(_currentResult.CreateExpectedError(Symbol.SEMICOLON));

                AppendSymbol(_currentResult);

                _currentResult = _tokenizer.Advance();
                if (_currentResult.IsFailed) return Result.Fail(_currentResult.Errors);

            }

            _output.AppendLine("</varDec>");
            return Result.Ok();
        }

        private Result CompileStatements()
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
                        var temp = CompileLet();
                        if (temp.IsFailed) return temp;
                    }
                    else if (_currentResult.Expect(Keyword.DO))
                    {
                        var temp = CompileDo();
                        if (temp.IsFailed) return temp;
                    }
                    else if (_currentResult.Expect(Keyword.IF))
                    {
                        var temp = CompileIf();
                        if (temp.IsFailed) return temp;
                    }
                    else if (_currentResult.Expect(Keyword.WHILE))
                    {
                        var temp = CompileWhile();
                        if (temp.IsFailed) return temp;
                    }
                    else
                    {
                        var temp = CompileReturn();
                        if (temp.IsFailed) return temp;
                    }
                }
            }

            _output.AppendLine("</statements>");
            return Result.Ok();
        }

        private Result CompileDo()
        {
            _output.AppendLine("<doStatement>");
            if (_currentResult.Expect(Keyword.DO))
            {
                AppendKeyword(_currentResult);

                _currentResult = _tokenizer.Advance();
                if (_currentResult.IsFailed) return Result.Fail(_currentResult.Errors);
                if (!_currentResult.Expect(TokenType.IDENTIFIER)) return Result.Fail(_currentResult.CreateExpectedError(TokenType.IDENTIFIER));

                AppendIdentifier(_currentResult);

                _currentResult = _tokenizer.Advance();
                if (_currentResult.IsFailed) return Result.Fail(_currentResult.Errors);

                if (_currentResult.Expect(Symbol.LPAR))
                {
                    AppendSymbol(_currentResult);

                    _currentResult = _tokenizer.Advance();
                    if (_currentResult.IsFailed) return Result.Fail(_currentResult.Errors);

                    var temp = CompileExpressionList();
                    if (temp.IsFailed) return temp;

                    if (_currentResult.IsFailed) return Result.Fail(_currentResult.Errors);
                    if (!_currentResult.Expect(Symbol.RPAR)) return Result.Fail(_currentResult.CreateExpectedError(Symbol.RPAR));

                    AppendSymbol(_currentResult);
                }
                else if (_currentResult.Expect(Symbol.DOT))
                {
                    AppendSymbol(_currentResult);

                    _currentResult = _tokenizer.Advance();
                    if (_currentResult.IsFailed) return Result.Fail(_currentResult.Errors);
                    if (!_currentResult.Expect(TokenType.IDENTIFIER)) return Result.Fail(_currentResult.CreateExpectedError(TokenType.IDENTIFIER));

                    AppendIdentifier(_currentResult);

                    _currentResult = _tokenizer.Advance();
                    if (_currentResult.IsFailed) return Result.Fail(_currentResult.Errors);
                    if (!_currentResult.Expect(Symbol.LPAR)) return Result.Fail(_currentResult.CreateExpectedError(Symbol.LPAR));

                    AppendSymbol(_currentResult);

                    _currentResult = _tokenizer.Advance();
                    if (_currentResult.IsFailed) return Result.Fail(_currentResult.Errors);

                    var temp = CompileExpressionList();
                    if (temp.IsFailed) return temp;

                    if (_currentResult.IsFailed) return Result.Fail(_currentResult.Errors);
                    if (!_currentResult.Expect(Symbol.RPAR)) return Result.Fail(_currentResult.CreateExpectedError(Symbol.RPAR));

                    AppendSymbol(_currentResult);
                }

                _currentResult = _tokenizer.Advance();
                if (_currentResult.IsFailed) return Result.Fail(_currentResult.Errors);
                if (!_currentResult.Expect(Symbol.SEMICOLON)) return Result.Fail(_currentResult.CreateExpectedError(Symbol.SEMICOLON));

                AppendSymbol(_currentResult);

                _currentResult = _tokenizer.Advance();
                if (_currentResult.IsFailed) return Result.Fail(_currentResult.Errors);
            }

            _output.AppendLine("</doStatement>");
            return Result.Ok();
        }

        private Result CompileLet()
        {
            _output.AppendLine("<letStatement>");

            if (_currentResult.Expect(Keyword.LET))
            {
                AppendKeyword(_currentResult);

                _currentResult = _tokenizer.Advance();
                if (_currentResult.IsFailed) return Result.Fail(_currentResult.Errors);
                if (!_currentResult.Expect(TokenType.IDENTIFIER)) return Result.Fail(_currentResult.CreateExpectedError(TokenType.IDENTIFIER));

                AppendIdentifier(_currentResult);

                _currentResult = _tokenizer.Advance();
                if (_currentResult.IsFailed) return Result.Fail(_currentResult.Errors);

                if (_currentResult.Expect(Symbol.LBRACK))
                {
                    AppendSymbol(_currentResult);

                    _currentResult = _tokenizer.Advance();
                    if (_currentResult.IsFailed) return Result.Fail(_currentResult.Errors);

                    var tmp = CompileExpression();
                    if (tmp.IsFailed) return tmp;

                    if (_currentResult.IsFailed) return Result.Fail(_currentResult.Errors);
                    if (!_currentResult.Expect(Symbol.RBRACK)) return Result.Fail(_currentResult.CreateExpectedError(Symbol.RBRACK));

                    AppendSymbol(_currentResult);

                    _currentResult = _tokenizer.Advance();
                    if (_currentResult.IsFailed) return Result.Fail(_currentResult.Errors);
                }

                if (_currentResult.IsFailed) return Result.Fail(_currentResult.Errors);
                if (!_currentResult.Expect(Symbol.EQUAL)) return Result.Fail(_currentResult.CreateExpectedError(Symbol.EQUAL));

                AppendSymbol(_currentResult);

                _currentResult = _tokenizer.Advance();
                if (_currentResult.IsFailed) return Result.Fail(_currentResult.Errors);

                var temp = CompileExpression();
                if (temp.IsFailed) return temp;

                if (_currentResult.IsFailed) return Result.Fail(_currentResult.Errors);
                if (!_currentResult.Expect(Symbol.SEMICOLON)) return Result.Fail(_currentResult.CreateExpectedError(Symbol.SEMICOLON));

                AppendSymbol(_currentResult);

                _currentResult = _tokenizer.Advance();
                if (_currentResult.IsFailed) return Result.Fail(_currentResult.Errors);
            }

            _output.AppendLine("</letStatement>");
            return Result.Ok();
        }

        private Result CompileWhile()
        {
            _output.AppendLine("<whileStatement>");

            if (_currentResult.Expect(Keyword.WHILE))
            {
                AppendKeyword(_currentResult);

                _currentResult = _tokenizer.Advance();
                if (_currentResult.IsFailed) return Result.Fail(_currentResult.Errors);
                if (!_currentResult.Expect(Symbol.LPAR)) return Result.Fail(_currentResult.CreateExpectedError(Symbol.LPAR));

                AppendSymbol(_currentResult);

                _currentResult = _tokenizer.Advance();
                if (_currentResult.IsFailed) return Result.Fail(_currentResult.Errors);

                var expressionResult = CompileExpression();
                if (expressionResult.IsFailed) return expressionResult;

                if (!_currentResult.Expect(Symbol.RPAR)) return Result.Fail(_currentResult.CreateExpectedError(Symbol.RPAR));

                AppendSymbol(_currentResult);

                _currentResult = _tokenizer.Advance();
                if (_currentResult.IsFailed) return Result.Fail(_currentResult.Errors);
                if (!_currentResult.Expect(Symbol.LBRACE)) return Result.Fail(_currentResult.CreateExpectedError(Symbol.LBRACE));

                AppendSymbol(_currentResult);

                _currentResult = _tokenizer.Advance();
                if (_currentResult.IsFailed) return Result.Fail(_currentResult.Errors);

                var statementsResult = CompileStatements();
                if (statementsResult.IsFailed) return statementsResult;

                if (!_currentResult.Expect(Symbol.RBRACE)) return Result.Fail(_currentResult.CreateExpectedError(Symbol.RBRACE));

                AppendSymbol(_currentResult);

                _currentResult = _tokenizer.Advance();
                if (_currentResult.IsFailed) return Result.Fail(_currentResult.Errors);
            }

            _output.AppendLine("</whileStatement>");
            return Result.Ok();
        }

        private Result CompileReturn()
        {
            _output.AppendLine("<returnStatement>");

            if (_currentResult.Expect(Keyword.RETURN))
            {
                AppendKeyword(_currentResult);

                _currentResult = _tokenizer.Advance();
                if (_currentResult.IsFailed) return Result.Fail(_currentResult.Errors);

                if (!_currentResult.Expect(Symbol.SEMICOLON))
                {
                    var expressionResult = CompileExpression();
                    if (expressionResult.IsFailed) return expressionResult;
                }

                if (!_currentResult.Expect(Symbol.SEMICOLON)) return Result.Fail(_currentResult.CreateExpectedError(Symbol.SEMICOLON));

                AppendSymbol(_currentResult);

                _currentResult = _tokenizer.Advance();
                if (_currentResult.IsFailed) return Result.Fail(_currentResult.Errors);
            }

            _output.AppendLine("</returnStatement>");
            return Result.Ok();
        }

        private Result CompileIf()
        {
            _output.AppendLine("<ifStatement>");

            if (_currentResult.Expect(Keyword.IF))
            {
                AppendKeyword(_currentResult);

                _currentResult = _tokenizer.Advance();
                if (_currentResult.IsFailed) return Result.Fail(_currentResult.Errors);
                if (!_currentResult.Expect(Symbol.LPAR)) return Result.Fail(_currentResult.CreateExpectedError(Symbol.LPAR));

                AppendSymbol(_currentResult);

                _currentResult = _tokenizer.Advance();
                if (_currentResult.IsFailed) return Result.Fail(_currentResult.Errors);

                var expressionResult = CompileExpression();
                if (expressionResult.IsFailed) return expressionResult;

                if (!_currentResult.Expect(Symbol.RPAR)) return Result.Fail(_currentResult.CreateExpectedError(Symbol.RPAR));

                AppendSymbol(_currentResult);

                _currentResult = _tokenizer.Advance();
                if (_currentResult.IsFailed) return Result.Fail(_currentResult.Errors);
                if (!_currentResult.Expect(Symbol.LBRACE)) return Result.Fail(_currentResult.CreateExpectedError(Symbol.LBRACE));

                AppendSymbol(_currentResult);

                _currentResult = _tokenizer.Advance();
                if (_currentResult.IsFailed) return Result.Fail(_currentResult.Errors);

                var statementsResult = CompileStatements();
                if (statementsResult.IsFailed) return statementsResult;

                if (!_currentResult.Expect(Symbol.RBRACE)) return Result.Fail(_currentResult.CreateExpectedError(Symbol.RBRACE));

                AppendSymbol(_currentResult);

                _currentResult = _tokenizer.Advance();
                if (_currentResult.IsFailed) return Result.Fail(_currentResult.Errors);

                if (_currentResult.Expect(Keyword.ELSE))
                {
                    AppendKeyword(_currentResult);

                    _currentResult = _tokenizer.Advance();
                    if (_currentResult.IsFailed) return Result.Fail(_currentResult.Errors);
                    if (!_currentResult.Expect(Symbol.LBRACE)) return Result.Fail(_currentResult.CreateExpectedError(Symbol.LBRACE));

                    AppendSymbol(_currentResult);

                    _currentResult = _tokenizer.Advance();
                    if (_currentResult.IsFailed) return Result.Fail(_currentResult.Errors);

                    var elseStatementsResult = CompileStatements();
                    if (elseStatementsResult.IsFailed) return elseStatementsResult;

                    if (!_currentResult.Expect(Symbol.RBRACE)) return Result.Fail(_currentResult.CreateExpectedError(Symbol.RBRACE));

                    AppendSymbol(_currentResult);

                    _currentResult = _tokenizer.Advance();
                    if (_currentResult.IsFailed) return Result.Fail(_currentResult.Errors);
                }
            }

            _output.AppendLine("</ifStatement>");
            return Result.Ok();
        }

        private Result CompileExpression()
        {
            _output.AppendLine("<expression>");

            if (_currentResult.IsTerm())
            {
                var termResult = CompileTerm();
                if (termResult.IsFailed) return termResult;

                while (_currentResult.IsUnaryOperator())
                {
                    AppendSymbol(_currentResult);

                    _currentResult = _tokenizer.Advance();
                    if (_currentResult.IsFailed) return Result.Fail(_currentResult.Errors);

                    if (!_currentResult.IsTerm()) return Result.Fail(new SyntaxError { Message = "Expected term." });

                    var nextTermResult = CompileTerm();
                    if (nextTermResult.IsFailed) return nextTermResult;
                }
            }

            _output.AppendLine("</expression>");
            return Result.Ok();
        }

        private Result CompileTerm()
        {
            _output.AppendLine("<term>");

            if (!_currentResult.IsTerm()) return Result.Fail(new SyntaxError { Message = "Expected term." });

            if (_currentResult.IsKeywordConstant())
            {
                AppendKeyword(_currentResult);
                _currentResult = _tokenizer.Advance();
                if (_currentResult.IsFailed) return Result.Fail(_currentResult.Errors);
            }
            else if (_currentResult.IsUnaryOperator())
            {
                AppendSymbol(_currentResult);
                _currentResult = _tokenizer.Advance();
                if (_currentResult.IsFailed) return Result.Fail(_currentResult.Errors);

                if (!_currentResult.IsTerm()) return Result.Fail(new SyntaxError { Message = "Expected term." });

                var unaryTermResult = CompileTerm();
                if (unaryTermResult.IsFailed) return unaryTermResult;
            }
            else if (_currentResult.Expect(TokenType.INT_CONST))
            {
                AppendIntConstant(_currentResult);
                _currentResult = _tokenizer.Advance();
                if (_currentResult.IsFailed) return Result.Fail(_currentResult.Errors);
            }
            else if (_currentResult.Expect(TokenType.STRING_CONST))
            {
                AppendStringConstant(_currentResult);
                _currentResult = _tokenizer.Advance();
                if (_currentResult.IsFailed) return Result.Fail(_currentResult.Errors);
            }
            else if (_currentResult.Expect(TokenType.IDENTIFIER))
            {
                AppendIdentifier(_currentResult);
                _currentResult = _tokenizer.Advance();
                if (_currentResult.IsFailed) return Result.Fail(_currentResult.Errors);

                if (_currentResult.Expect(Symbol.LBRACK))
                {
                    AppendSymbol(_currentResult);
                    _currentResult = _tokenizer.Advance();
                    if (_currentResult.IsFailed) return Result.Fail(_currentResult.Errors);

                    var expressionResult = CompileExpression();
                    if (expressionResult.IsFailed) return expressionResult;

                    if (!_currentResult.Expect(Symbol.RBRACK)) return Result.Fail(_currentResult.CreateExpectedError(Symbol.RBRACK));

                    AppendSymbol(_currentResult);
                    _currentResult = _tokenizer.Advance();
                    if (_currentResult.IsFailed) return Result.Fail(_currentResult.Errors);
                }
                else if (_currentResult.Expect(Symbol.LPAR))
                {
                    AppendSymbol(_currentResult);

                    _currentResult = _tokenizer.Advance();
                    if (_currentResult.IsFailed) return Result.Fail(_currentResult.Errors);

                    var expressionListResult = CompileExpressionList();
                    if (expressionListResult.IsFailed) return expressionListResult;

                    if (!_currentResult.Expect(Symbol.RPAR)) return Result.Fail(_currentResult.CreateExpectedError(Symbol.RPAR));

                    AppendSymbol(_currentResult);
                    _currentResult = _tokenizer.Advance();
                    if (_currentResult.IsFailed) return Result.Fail(_currentResult.Errors);
                }
                else if (_currentResult.Expect(Symbol.DOT))
                {
                    AppendSymbol(_currentResult);

                    _currentResult = _tokenizer.Advance();
                    if (_currentResult.IsFailed) return Result.Fail(_currentResult.Errors);
                    if (!_currentResult.Expect(TokenType.IDENTIFIER)) return Result.Fail(_currentResult.CreateExpectedError(TokenType.IDENTIFIER));

                    AppendIdentifier(_currentResult);

                    _currentResult = _tokenizer.Advance();
                    if (_currentResult.IsFailed) return Result.Fail(_currentResult.Errors);
                    if (!_currentResult.Expect(Symbol.LPAR)) return Result.Fail(_currentResult.CreateExpectedError(Symbol.LPAR));

                    AppendSymbol(_currentResult);

                    _currentResult = _tokenizer.Advance();
                    if (_currentResult.IsFailed) return Result.Fail(_currentResult.Errors);

                    var expressionListResult = CompileExpressionList();
                    if (expressionListResult.IsFailed) return expressionListResult;

                    if (!_currentResult.Expect(Symbol.RPAR)) return Result.Fail(_currentResult.CreateExpectedError(Symbol.RPAR));

                    AppendSymbol(_currentResult);
                    _currentResult = _tokenizer.Advance();
                    if (_currentResult.IsFailed) return Result.Fail(_currentResult.Errors);
                }
            }
            else if (_currentResult.Expect(Symbol.LPAR))
            {
                AppendSymbol(_currentResult);
                _currentResult = _tokenizer.Advance();
                if (_currentResult.IsFailed) return Result.Fail(_currentResult.Errors);

                var expressionResult = CompileExpression();
                if (expressionResult.IsFailed) return expressionResult;

                if (!_currentResult.Expect(Symbol.RPAR)) return Result.Fail(_currentResult.CreateExpectedError(Symbol.RPAR));

                AppendSymbol(_currentResult);
                _currentResult = _tokenizer.Advance();
                if (_currentResult.IsFailed) return Result.Fail(_currentResult.Errors);
            }

            _output.AppendLine("</term>");
            return Result.Ok();
        }

        private Result CompileExpressionList()
        {
            _output.AppendLine("<expressionList>");

            if (_currentResult.IsTerm())
            {
                var expressionResult = CompileExpression();
                if (expressionResult.IsFailed) return expressionResult;

                while (_currentResult.Expect(Symbol.COMMA))
                {
                    AppendSymbol(_currentResult);

                    _currentResult = _tokenizer.Advance();
                    if (_currentResult.IsFailed) return Result.Fail(_currentResult.Errors);

                    expressionResult = CompileExpression();
                    if (expressionResult.IsFailed) return expressionResult;
                }
            }

            _output.AppendLine("</expressionList>");
            return Result.Ok();
        }

        public void Dispose() 
        {
            _tokenizer.Dispose();
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
