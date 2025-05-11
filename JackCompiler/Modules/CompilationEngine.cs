using FluentResults;
using JackCompiler.Enums;
using JackCompiler.Extensions;
using JackCompiler.Errors;
using JackCompiler.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reflection.Metadata.Ecma335;
using JackCompiler.DataStructures;

namespace JackCompiler.Modules
{
    public class CompilationEngine : IDisposable
    {
        private readonly JackTokenizer _tokenizer;
        private readonly VMWriter _vmWriter;
        private readonly SymbolTable _classScopeTable;
        private readonly SymbolTable _subroutineScopeTable;
        private readonly StringBuilder _output;
        private string _currentClassname = string.Empty;
        private Result<Token> _currentResult;
        private int _generalPurposeCounter = 0;

        public CompilationEngine(JackTokenizer tokenizer, VMWriter writer)
        {
            _tokenizer = tokenizer;
            _vmWriter = writer;
            _classScopeTable = new SymbolTable();
            _subroutineScopeTable = new SymbolTable();
            _output = new StringBuilder();
        }

        public bool TryWriteOutputToFile()
        {
            return _vmWriter.TryWriteToFile();
        }

        public Result CompileClass()
        {
            _currentResult = _tokenizer.Advance();
            if (_currentResult.IsFailed) return Result.Fail(_currentResult.Errors);
            if (!_currentResult.Expect(Keyword.CLASS)) return Result.Fail(_currentResult.CreateExpectedError(Keyword.CLASS));

            _currentResult = _tokenizer.Advance();
            if (_currentResult.IsFailed) return Result.Fail(_currentResult.Errors);
            if (!_currentResult.Expect(TokenType.IDENTIFIER)) return Result.Fail(_currentResult.CreateExpectedError(TokenType.IDENTIFIER));

            _currentClassname = (string)_currentResult.Value.Value;

            _currentResult = _tokenizer.Advance();
            if (_currentResult.IsFailed) return Result.Fail(_currentResult.Errors);
            if (!_currentResult.Expect(Symbol.LBRACE)) return Result.Fail(_currentResult.CreateExpectedError(Symbol.LBRACE));

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

            _classScopeTable.Reset();
            return Result.Ok();
        }

        private Result CompileClassVar()
        {
            if (_currentResult.Expect(Keyword.STATIC) || _currentResult.Expect(Keyword.FIELD))
            {
                Kind kind = _currentResult.Expect(Keyword.STATIC) ? Kind.STATIC : Kind.FIELD;

                _currentResult = _tokenizer.Advance();
                if (_currentResult.IsFailed) return Result.Fail(_currentResult.Errors);
                if (!_currentResult.ExpectType()) return Result.Fail(_currentResult.CreateTypeExpectedError());

                string type = _currentResult.ExpectBuiltInType() ? ((Keyword)_currentResult.Value.Value).ToString() : (string)_currentResult.Value.Value;

                _currentResult = _tokenizer.Advance();
                if (_currentResult.IsFailed) return Result.Fail(_currentResult.Errors);
                if (!_currentResult.Expect(TokenType.IDENTIFIER)) return Result.Fail(_currentResult.CreateExpectedError(TokenType.IDENTIFIER));

                _classScopeTable.Define(((Keyword)_currentResult.Value.Value).ToString()
                                        , type
                                        , kind);

                _currentResult = _tokenizer.Advance();
                if (_currentResult.IsFailed) return Result.Fail(_currentResult.Errors);

                while (_currentResult.Expect(Symbol.COMMA))
                {
                    _currentResult = _tokenizer.Advance();
                    if (_currentResult.IsFailed) return Result.Fail(_currentResult.Errors);
                    if (!_currentResult.Expect(TokenType.IDENTIFIER)) return Result.Fail(_currentResult.CreateExpectedError(TokenType.IDENTIFIER));

                    _classScopeTable.Define(((Keyword)_currentResult.Value.Value).ToString()
                                            , type
                                            , kind);

                    _currentResult = _tokenizer.Advance();
                    if (_currentResult.IsFailed) return Result.Fail(_currentResult.Errors);
                }

                if (_currentResult.IsFailed) return Result.Fail(_currentResult.Errors);
                if (!_currentResult.Expect(Symbol.SEMICOLON)) return Result.Fail(_currentResult.CreateExpectedError(Symbol.SEMICOLON));

                _currentResult = _tokenizer.Advance();
                if (_currentResult.IsFailed) return Result.Fail(_currentResult.Errors);
            }

            return Result.Ok();
        }

        private Result CompileSubroutine()
        {
            if (_currentResult.Expect(Keyword.CONSTRUCTOR) || _currentResult.Expect(Keyword.FUNCTION) || _currentResult.Expect(Keyword.METHOD))
            {
                if (_currentResult.Expect(Keyword.METHOD))
                {
                    _subroutineScopeTable.Define("this", _currentClassname, Kind.ARGUMENT);
                    _vmWriter.WritePush(Segment.ARGUMENT, _subroutineScopeTable.IndexOf("this"));
                    _vmWriter.WritePop(Segment.POINTER, 0);
                }

                _currentResult = _tokenizer.Advance();
                if (_currentResult.IsFailed) return Result.Fail(_currentResult.Errors);

                if (!(_currentResult.Expect(Keyword.VOID) || _currentResult.ExpectType()))
                {
                    return Result.Fail(_currentResult.CreateTypeExpectedError());
                }

                _currentResult = _tokenizer.Advance();
                if (_currentResult.IsFailed) return Result.Fail(_currentResult.Errors);
                if (!_currentResult.Expect(TokenType.IDENTIFIER)) return Result.Fail(_currentResult.CreateExpectedError(TokenType.IDENTIFIER));

                string subroutineName = (string)_currentResult.Value.Value;

                _currentResult = _tokenizer.Advance();
                if (_currentResult.IsFailed) return Result.Fail(_currentResult.Errors);
                if (!_currentResult.Expect(Symbol.LPAR)) return Result.Fail(_currentResult.CreateExpectedError(Symbol.LPAR));

                _currentResult = _tokenizer.Advance();
                if (_currentResult.IsFailed) return Result.Fail(_currentResult.Errors);

                var result = CompileParameterList();
                if (result.IsFailed) return result;

                if (_currentResult.IsFailed) return Result.Fail(_currentResult.Errors);
                if (!_currentResult.Expect(Symbol.RPAR)) return Result.Fail(_currentResult.CreateExpectedError(Symbol.RPAR));

                _currentResult = _tokenizer.Advance();
                if (_currentResult.IsFailed) return Result.Fail(_currentResult.Errors);
                if (!_currentResult.Expect(Symbol.LBRACE)) return Result.Fail(_currentResult.CreateExpectedError(Symbol.LBRACE));

                _currentResult = _tokenizer.Advance();
                if (_currentResult.IsFailed) return Result.Fail(_currentResult.Errors);

                int localVariableCount = 0;

                while (_currentResult.Expect(Keyword.VAR))
                {
                    Result<int> resultVarDec = CompileVarDec();
                    if (resultVarDec.IsFailed) return resultVarDec.ToResult();
                    localVariableCount += resultVarDec.Value;
                }

                _vmWriter.WriteFunction($"{_currentClassname}.{subroutineName}", localVariableCount);

                while (_currentResult.IsStatement())
                {
                    var resultVarDec = CompileStatements();
                    if (resultVarDec.IsFailed) return resultVarDec;
                }

                if (_currentResult.IsFailed) return Result.Fail(_currentResult.Errors);
                if (!_currentResult.Expect(Symbol.RBRACE)) return Result.Fail(_currentResult.CreateExpectedError(Symbol.RBRACE));

                _currentResult = _tokenizer.Advance();
                if (_currentResult.IsFailed) return Result.Fail(_currentResult.Errors);
            }

            _subroutineScopeTable.Reset();
            return Result.Ok();
        }

        private Result CompileParameterList()
        {
            if (_currentResult.ExpectType())
            {
                string type = _currentResult.ExpectBuiltInType() ? ((Keyword)_currentResult.Value.Value).ToString() : (string)_currentResult.Value.Value;

                _currentResult = _tokenizer.Advance();
                if (_currentResult.IsFailed) return Result.Fail(_currentResult.Errors);
                if (!_currentResult.Expect(TokenType.IDENTIFIER)) return Result.Fail(_currentResult.CreateExpectedError(TokenType.IDENTIFIER));

                _subroutineScopeTable.Define(((Keyword)_currentResult.Value.Value).ToString()
                                            , type
                                            , Kind.ARGUMENT);

                _currentResult = _tokenizer.Advance();
                if (_currentResult.IsFailed) return Result.Fail(_currentResult.Errors);

                while (_currentResult.Expect(Symbol.COMMA))
                {
                    _currentResult = _tokenizer.Advance();
                    if (_currentResult.IsFailed) return Result.Fail(_currentResult.Errors);
                    if (!_currentResult.ExpectType()) return Result.Fail(_currentResult.CreateTypeExpectedError());

                    string tempType = _currentResult.ExpectBuiltInType() ? ((Keyword)_currentResult.Value.Value).ToString() : (string)_currentResult.Value.Value;

                    _currentResult = _tokenizer.Advance();
                    if (_currentResult.IsFailed) return Result.Fail(_currentResult.Errors);
                    if (!_currentResult.Expect(TokenType.IDENTIFIER)) return Result.Fail(_currentResult.CreateExpectedError(TokenType.IDENTIFIER));

                    _subroutineScopeTable.Define(((Keyword)_currentResult.Value.Value).ToString()
                                            , type
                                            , Kind.ARGUMENT);

                    _currentResult = _tokenizer.Advance();
                    if (_currentResult.IsFailed) return Result.Fail(_currentResult.Errors);
                }
            }

            return Result.Ok();
        }

        private Result<int> CompileVarDec()
        {
            int localVariableCount = 0;

            if (_currentResult.Expect(Keyword.VAR))
            {
                _currentResult = _tokenizer.Advance();
                if (_currentResult.IsFailed) return Result.Fail(_currentResult.Errors);
                if (!_currentResult.ExpectType()) return Result.Fail(_currentResult.CreateTypeExpectedError());

                string tempType = _currentResult.ExpectBuiltInType() ? ((Keyword)_currentResult.Value.Value).ToString() : (string)_currentResult.Value.Value;

                _currentResult = _tokenizer.Advance();
                if (_currentResult.IsFailed) return Result.Fail(_currentResult.Errors);
                if (!_currentResult.Expect(TokenType.IDENTIFIER)) return Result.Fail(_currentResult.CreateExpectedError(TokenType.IDENTIFIER));

                _subroutineScopeTable.Define((string)_currentResult.Value.Value
                                        , tempType
                                        , Kind.VAR);
                localVariableCount++;

                _currentResult = _tokenizer.Advance();
                if (_currentResult.IsFailed) return Result.Fail(_currentResult.Errors);

                while (_currentResult.Expect(Symbol.COMMA))
                {
                    _currentResult = _tokenizer.Advance();
                    if (_currentResult.IsFailed) return Result.Fail(_currentResult.Errors);
                    if (!_currentResult.Expect(TokenType.IDENTIFIER)) return Result.Fail(_currentResult.CreateExpectedError(TokenType.IDENTIFIER));

                    _subroutineScopeTable.Define((string)_currentResult.Value.Value
                                            , tempType
                                            , Kind.VAR);
                    localVariableCount++;

                    _currentResult = _tokenizer.Advance();
                    if (_currentResult.IsFailed) return Result.Fail(_currentResult.Errors);
                }

                if (_currentResult.IsFailed) return Result.Fail(_currentResult.Errors);
                if (!_currentResult.Expect(Symbol.SEMICOLON)) return Result.Fail(_currentResult.CreateExpectedError(Symbol.SEMICOLON));

                _currentResult = _tokenizer.Advance();
                if (_currentResult.IsFailed) return Result.Fail(_currentResult.Errors);
            }

            return Result.Ok(localVariableCount);
        }

        private Result CompileStatements()
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

            return Result.Ok();
        }

        private Result CompileDo()
        {
            if (_currentResult.Expect(Keyword.DO))
            {
                _currentResult = _tokenizer.Advance();
                if (_currentResult.IsFailed) return Result.Fail(_currentResult.Errors);
                if (!_currentResult.Expect(TokenType.IDENTIFIER)) return Result.Fail(_currentResult.CreateExpectedError(TokenType.IDENTIFIER));

                string tempIdentifier = (string)_currentResult.Value.Value;

                _currentResult = _tokenizer.Advance();
                if (_currentResult.IsFailed) return Result.Fail(_currentResult.Errors);

                if (_currentResult.Expect(Symbol.LPAR))
                {
                    _currentResult = _tokenizer.Advance();
                    if (_currentResult.IsFailed) return Result.Fail(_currentResult.Errors);

                    var temp = CompileExpressionList();
                    if (temp.IsFailed) return temp.ToResult();

                    int argumentCount = temp.Value;
                    _vmWriter.WriteCall($"{_currentClassname}.{tempIdentifier}", argumentCount);

                    if (_currentResult.IsFailed) return Result.Fail(_currentResult.Errors);
                    if (!_currentResult.Expect(Symbol.RPAR)) return Result.Fail(_currentResult.CreateExpectedError(Symbol.RPAR));
                }
                else if (_currentResult.Expect(Symbol.DOT))
                {
                    int subroutineScopeIndex = _subroutineScopeTable.IndexOf(tempIdentifier);
                    int classScopeIndex = _classScopeTable.IndexOf(tempIdentifier);

                    _currentResult = _tokenizer.Advance();
                    if (_currentResult.IsFailed) return Result.Fail(_currentResult.Errors);
                    if (!_currentResult.Expect(TokenType.IDENTIFIER)) return Result.Fail(_currentResult.CreateExpectedError(TokenType.IDENTIFIER));

                    string subroutineName = (string)_currentResult.Value.Value;

                    _currentResult = _tokenizer.Advance();
                    if (_currentResult.IsFailed) return Result.Fail(_currentResult.Errors);
                    if (!_currentResult.Expect(Symbol.LPAR)) return Result.Fail(_currentResult.CreateExpectedError(Symbol.LPAR));

                    _currentResult = _tokenizer.Advance();
                    if (_currentResult.IsFailed) return Result.Fail(_currentResult.Errors);

                    Result<int> expressionListResult = CompileExpressionList();
                    if (expressionListResult.IsFailed) return expressionListResult.ToResult();
                    int argumentCount = expressionListResult.Value;

                    if (!_currentResult.Expect(Symbol.RPAR)) return Result.Fail(_currentResult.CreateExpectedError(Symbol.RPAR));

                    if (subroutineScopeIndex == -1 && classScopeIndex == -1)
                    {
                        _vmWriter.WriteCall($"{tempIdentifier}.{subroutineName}", argumentCount);
                    }
                    else if (subroutineScopeIndex != -1)
                    {
                        _vmWriter.WriteCall($"{_subroutineScopeTable.TypeOf(tempIdentifier)}.{subroutineName}", argumentCount);
                    }
                    else if (classScopeIndex != -1)
                    {
                        _vmWriter.WriteCall($"{_classScopeTable.TypeOf(tempIdentifier)}.{subroutineName}", argumentCount);
                    }
                    else
                    {
                        return Result.Fail(new SemanticError
                        {
                            Message = $"[Error] File: {_currentResult.Value.Filename}. '{tempIdentifier}' is undefined in this context.  Line: {_currentResult.Value.Row}"
                        });
                    }
                }

                _currentResult = _tokenizer.Advance();
                if (_currentResult.IsFailed) return Result.Fail(_currentResult.Errors);
                if (!_currentResult.Expect(Symbol.SEMICOLON)) return Result.Fail(_currentResult.CreateExpectedError(Symbol.SEMICOLON));

                _vmWriter.WritePop(Segment.TEMP, 1);

                _currentResult = _tokenizer.Advance();
                if (_currentResult.IsFailed) return Result.Fail(_currentResult.Errors);
            }

            return Result.Ok();
        }

        private Result CompileLet()
        {
            if (_currentResult.Expect(Keyword.LET))
            {
                _currentResult = _tokenizer.Advance();
                if (_currentResult.IsFailed) return Result.Fail(_currentResult.Errors);
                if (!_currentResult.Expect(TokenType.IDENTIFIER)) return Result.Fail(_currentResult.CreateExpectedError(TokenType.IDENTIFIER));

                string tempIdentifier = (string)_currentResult.Value.Value;
                int subroutineScopeIndex = _subroutineScopeTable.IndexOf(tempIdentifier);
                int classScopeIndex = _classScopeTable.IndexOf(tempIdentifier);

                if (subroutineScopeIndex == -1 && classScopeIndex == -1)
                {
                    return Result.Fail(new SemanticError
                    {
                        Message = $"[Error] File: {_currentResult.Value.Filename}. '{tempIdentifier}' is undefined in this context.  Line: {_currentResult.Value.Row}"
                    });
                }

                _currentResult = _tokenizer.Advance();
                if (_currentResult.IsFailed) return Result.Fail(_currentResult.Errors);

                if (_currentResult.Expect(Symbol.LBRACK))
                {
                    _currentResult = _tokenizer.Advance();
                    if (_currentResult.IsFailed) return Result.Fail(_currentResult.Errors);

                    if (subroutineScopeIndex != -1)
                        _vmWriter.WritePush(_subroutineScopeTable.KindOf(tempIdentifier).ToSegment(), subroutineScopeIndex);
                    else if (classScopeIndex != -1)
                        _vmWriter.WritePush(_classScopeTable.KindOf(tempIdentifier).ToSegment(), classScopeIndex);

                    var tmp = CompileExpression();
                    if (tmp.IsFailed) return tmp;

                    _vmWriter.WriteArithmetic(Command.ADD);
                    _vmWriter.WritePop(Segment.TEMP, 0);

                    if (_currentResult.IsFailed) return Result.Fail(_currentResult.Errors);
                    if (!_currentResult.Expect(Symbol.RBRACK)) return Result.Fail(_currentResult.CreateExpectedError(Symbol.RBRACK));

                    _currentResult = _tokenizer.Advance();
                    if (_currentResult.IsFailed) return Result.Fail(_currentResult.Errors);

                    if (_currentResult.IsFailed) return Result.Fail(_currentResult.Errors);
                    if (!_currentResult.Expect(Symbol.EQUAL)) return Result.Fail(_currentResult.CreateExpectedError(Symbol.EQUAL));

                    _currentResult = _tokenizer.Advance();
                    if (_currentResult.IsFailed) return Result.Fail(_currentResult.Errors);

                    var temp = CompileExpression();
                    if (temp.IsFailed) return temp;

                    _vmWriter.WritePush(Segment.TEMP, 0);
                    _vmWriter.WritePop(Segment.POINTER, 1);
                    _vmWriter.WritePop(Segment.THAT, 0);

                }
                else
                {
                    if (_currentResult.IsFailed) return Result.Fail(_currentResult.Errors);
                    if (!_currentResult.Expect(Symbol.EQUAL)) return Result.Fail(_currentResult.CreateExpectedError(Symbol.EQUAL));

                    _currentResult = _tokenizer.Advance();
                    if (_currentResult.IsFailed) return Result.Fail(_currentResult.Errors);

                    var temp = CompileExpression();
                    if (temp.IsFailed) return temp;

                    if (subroutineScopeIndex != -1)
                        _vmWriter.WritePop(_subroutineScopeTable.KindOf(tempIdentifier).ToSegment(), subroutineScopeIndex);
                    else if (classScopeIndex != -1)
                        _vmWriter.WritePop(_classScopeTable.KindOf(tempIdentifier).ToSegment(), classScopeIndex);
                }

                if (_currentResult.IsFailed) return Result.Fail(_currentResult.Errors);
                if (!_currentResult.Expect(Symbol.SEMICOLON)) return Result.Fail(_currentResult.CreateExpectedError(Symbol.SEMICOLON));

                _currentResult = _tokenizer.Advance();
                if (_currentResult.IsFailed) return Result.Fail(_currentResult.Errors);
            }

            return Result.Ok();
        }

        private Result CompileWhile()
        {
            if (_currentResult.Expect(Keyword.WHILE))
            {
                _currentResult = _tokenizer.Advance();
                if (_currentResult.IsFailed) return Result.Fail(_currentResult.Errors);
                if (!_currentResult.Expect(Symbol.LPAR)) return Result.Fail(_currentResult.CreateExpectedError(Symbol.LPAR));

                _currentResult = _tokenizer.Advance();
                if (_currentResult.IsFailed) return Result.Fail(_currentResult.Errors);

                int whileCounter = _generalPurposeCounter;
                _generalPurposeCounter++;

                _vmWriter.WriteLabel($"WHILE_CONDITION_{_generalPurposeCounter}");
                var expressionResult = CompileExpression();
                if (expressionResult.IsFailed) return expressionResult;

                _vmWriter.WriteArithmetic(Command.NEG);
                _vmWriter.WriteIf($"WHILE_END_{whileCounter}");

                if (!_currentResult.Expect(Symbol.RPAR)) return Result.Fail(_currentResult.CreateExpectedError(Symbol.RPAR));

                _currentResult = _tokenizer.Advance();
                if (_currentResult.IsFailed) return Result.Fail(_currentResult.Errors);
                if (!_currentResult.Expect(Symbol.LBRACE)) return Result.Fail(_currentResult.CreateExpectedError(Symbol.LBRACE));

                _currentResult = _tokenizer.Advance();
                if (_currentResult.IsFailed) return Result.Fail(_currentResult.Errors);

                var statementsResult = CompileStatements();
                if (statementsResult.IsFailed) return statementsResult;
                _vmWriter.WriteGoto($"WHILE_CONDITION_{_generalPurposeCounter}");

                if (!_currentResult.Expect(Symbol.RBRACE)) return Result.Fail(_currentResult.CreateExpectedError(Symbol.RBRACE));
                _vmWriter.WriteLabel($"WHILE_END_{whileCounter}");

                _currentResult = _tokenizer.Advance();
                if (_currentResult.IsFailed) return Result.Fail(_currentResult.Errors);
            }

            return Result.Ok();
        }

        private Result CompileReturn()
        {
            if (_currentResult.Expect(Keyword.RETURN))
            {
                _currentResult = _tokenizer.Advance();
                if (_currentResult.IsFailed) return Result.Fail(_currentResult.Errors);

                if (!_currentResult.Expect(Symbol.SEMICOLON))
                {
                    var expressionResult = CompileExpression();
                    if (expressionResult.IsFailed) return expressionResult;
                }
                else
                {
                    _vmWriter.WritePush(Segment.CONSTANT, 0);
                }

                if (!_currentResult.Expect(Symbol.SEMICOLON)) return Result.Fail(_currentResult.CreateExpectedError(Symbol.SEMICOLON));
                _vmWriter.WriteReturn();

                _currentResult = _tokenizer.Advance();
                if (_currentResult.IsFailed) return Result.Fail(_currentResult.Errors);
            }

            return Result.Ok();
        }

        private Result CompileIf()
        {
            if (_currentResult.Expect(Keyword.IF))
            {
                _currentResult = _tokenizer.Advance();
                if (_currentResult.IsFailed) return Result.Fail(_currentResult.Errors);
                if (!_currentResult.Expect(Symbol.LPAR)) return Result.Fail(_currentResult.CreateExpectedError(Symbol.LPAR));

                _currentResult = _tokenizer.Advance();
                if (_currentResult.IsFailed) return Result.Fail(_currentResult.Errors);

                var expressionResult = CompileExpression();
                if (expressionResult.IsFailed) return expressionResult;

                if (!_currentResult.Expect(Symbol.RPAR)) return Result.Fail(_currentResult.CreateExpectedError(Symbol.RPAR));

                _currentResult = _tokenizer.Advance();
                if (_currentResult.IsFailed) return Result.Fail(_currentResult.Errors);
                if (!_currentResult.Expect(Symbol.LBRACE)) return Result.Fail(_currentResult.CreateExpectedError(Symbol.LBRACE));

                _currentResult = _tokenizer.Advance();
                if (_currentResult.IsFailed) return Result.Fail(_currentResult.Errors);

                int ifElseCounter = _generalPurposeCounter;

                _vmWriter.WriteArithmetic(Command.NEG);
                _vmWriter.WriteIf($"ELSE_{ifElseCounter}");
                _generalPurposeCounter++;
                var statementsResult = CompileStatements();
                if (statementsResult.IsFailed) return statementsResult;

                if (!_currentResult.Expect(Symbol.RBRACE)) return Result.Fail(_currentResult.CreateExpectedError(Symbol.RBRACE));
                _vmWriter.WriteGoto($"END_IF_ELSE_{ifElseCounter}");

                _currentResult = _tokenizer.Advance();
                if (_currentResult.IsFailed) return Result.Fail(_currentResult.Errors);

                if (_currentResult.Expect(Keyword.ELSE))
                {
                    _currentResult = _tokenizer.Advance();
                    if (_currentResult.IsFailed) return Result.Fail(_currentResult.Errors);
                    if (!_currentResult.Expect(Symbol.LBRACE)) return Result.Fail(_currentResult.CreateExpectedError(Symbol.LBRACE));

                    _currentResult = _tokenizer.Advance();
                    if (_currentResult.IsFailed) return Result.Fail(_currentResult.Errors);

                    _vmWriter.WriteGoto($"ELSE_{ifElseCounter}");
                    var elseStatementsResult = CompileStatements();
                    if (elseStatementsResult.IsFailed) return elseStatementsResult;

                    if (!_currentResult.Expect(Symbol.RBRACE)) return Result.Fail(_currentResult.CreateExpectedError(Symbol.RBRACE));

                    _currentResult = _tokenizer.Advance();
                    if (_currentResult.IsFailed) return Result.Fail(_currentResult.Errors);
                }

                _vmWriter.WriteLabel($"END_IF_ELSE_{ifElseCounter}");
            }

            return Result.Ok();
        }

        private Result CompileExpression()
        {
            if (_currentResult.IsTerm())
            {
                var termResult = CompileTerm();
                if (termResult.IsFailed) return termResult;

                while (_currentResult.IsUnaryOperator())
                {
                    _currentResult = _tokenizer.Advance();
                    if (_currentResult.IsFailed) return Result.Fail(_currentResult.Errors);

                    if (!_currentResult.IsTerm()) return Result.Fail(new SyntaxError { Message = "Expected term." });

                    var nextTermResult = CompileTerm();
                    if (nextTermResult.IsFailed) return nextTermResult;
                }
            }

            return Result.Ok();
        }

        private Result CompileTerm()
        {
            if (!_currentResult.IsTerm()) return Result.Fail(new SyntaxError { Message = "Expected term." });

            if (_currentResult.IsKeywordConstant())
            {
                switch ((Keyword)_currentResult.Value.Value)
                {
                    case Keyword.TRUE:
                        _vmWriter.WritePush(Segment.CONSTANT, 1);
                        _vmWriter.WriteArithmetic(Command.NEG);
                        break;
                    case Keyword.FALSE:
                        _vmWriter.WritePush(Segment.CONSTANT, 0);
                        break;
                    case Keyword.NULL:
                        _vmWriter.WritePush(Segment.CONSTANT, 0);
                        break;
                    case Keyword.THIS:
                        int thisIndex = _subroutineScopeTable.IndexOf("this");
                        if (thisIndex == -1)
                            _vmWriter.WritePush(Segment.ARGUMENT, thisIndex);
                        else
                            return Result.Fail(
                                    new SemanticError
                                    {
                                        Message = $"[Error] File: {_currentResult.Value.Filename}. 'this' is undefined in this context.  Line: {_currentResult.Value.Row}"
                                    });
                        break;
                }

                _currentResult = _tokenizer.Advance();
                if (_currentResult.IsFailed) return Result.Fail(_currentResult.Errors);
            }
            else if (_currentResult.IsUnaryOperator())
            {
                Result<Token> unaryOperator = _currentResult;
                _currentResult = _tokenizer.Advance();
                if (_currentResult.IsFailed) return Result.Fail(_currentResult.Errors);
                if (!_currentResult.IsTerm()) return Result.Fail(new SyntaxError { Message = "Expected term." });

                var unaryTermResult = CompileTerm();
                if (unaryTermResult.IsFailed) return unaryTermResult;

                switch ((Symbol)unaryOperator.Value.Value)
                {
                    case Symbol.MINUS:
                        _vmWriter.WriteArithmetic(Command.SUB);
                        break;
                    case Symbol.PLUS:
                        _vmWriter.WriteArithmetic(Command.ADD);
                        break;
                    case Symbol.AMP:
                        _vmWriter.WriteArithmetic(Command.AND);
                        break;
                    case Symbol.PIPE:
                        _vmWriter.WriteArithmetic(Command.OR);
                        break;
                    case Symbol.LT:
                        _vmWriter.WriteArithmetic(Command.LT);
                        break;
                    case Symbol.GT:
                        _vmWriter.WriteArithmetic(Command.GT);
                        break;
                    case Symbol.EQUAL:
                        _vmWriter.WriteArithmetic(Command.EQ);
                        break;
                    case Symbol.TILDE:
                        _vmWriter.WriteArithmetic(Command.NEG);
                        break;
                    case Symbol.STAR:
                        _vmWriter.WriteCall($"Math.multiply", 2);
                        break;
                    case Symbol.SLASH:
                        _vmWriter.WriteCall($"Math.divide", 2);
                        break;
                }
            }
            else if (_currentResult.Expect(TokenType.INT_CONST))
            {
                _vmWriter.WritePush(Segment.CONSTANT, (int)_currentResult.Value.Value);
                _currentResult = _tokenizer.Advance();
                if (_currentResult.IsFailed) return Result.Fail(_currentResult.Errors);
            }
            else if (_currentResult.Expect(TokenType.STRING_CONST))
            {
                _currentResult = _tokenizer.Advance();
                if (_currentResult.IsFailed) return Result.Fail(_currentResult.Errors);
            }
            else if (_currentResult.Expect(TokenType.IDENTIFIER))
            {
                string identifier = (string)_currentResult.Value.Value;
                int subroutineScopeIndex = _subroutineScopeTable.IndexOf(identifier);
                int classScopeIndex = _classScopeTable.IndexOf(identifier);

                _currentResult = _tokenizer.Advance();
                if (_currentResult.IsFailed) return Result.Fail(_currentResult.Errors);

                if (_currentResult.Expect(Symbol.LBRACK))
                {
                    _currentResult = _tokenizer.Advance();
                    if (_currentResult.IsFailed) return Result.Fail(_currentResult.Errors);

                    if (subroutineScopeIndex != -1)
                        _vmWriter.WritePush(_subroutineScopeTable.KindOf(identifier).ToSegment(), subroutineScopeIndex);
                    else if (classScopeIndex != -1)
                        _vmWriter.WritePush(_classScopeTable.KindOf(identifier).ToSegment(), classScopeIndex);

                    var expressionResult = CompileExpression();
                    if (expressionResult.IsFailed) return expressionResult;

                    _vmWriter.WriteArithmetic(Command.ADD);

                    if (!_currentResult.Expect(Symbol.RBRACK)) return Result.Fail(_currentResult.CreateExpectedError(Symbol.RBRACK));

                    _currentResult = _tokenizer.Advance();
                    if (_currentResult.IsFailed) return Result.Fail(_currentResult.Errors);
                }
                else if (_currentResult.Expect(Symbol.LPAR))
                {
                    _currentResult = _tokenizer.Advance();
                    if (_currentResult.IsFailed) return Result.Fail(_currentResult.Errors);

                    Result<int> expressionListResult = CompileExpressionList();
                    if (expressionListResult.IsFailed) return expressionListResult.ToResult();

                    int argumentCount = expressionListResult.Value;
                    _vmWriter.WriteCall($"{_currentClassname}.{identifier}", argumentCount);

                    if (!_currentResult.Expect(Symbol.RPAR)) return Result.Fail(_currentResult.CreateExpectedError(Symbol.RPAR));

                    _currentResult = _tokenizer.Advance();
                    if (_currentResult.IsFailed) return Result.Fail(_currentResult.Errors);
                }
                else if (_currentResult.Expect(Symbol.DOT))
                {
                    _currentResult = _tokenizer.Advance();
                    if (_currentResult.IsFailed) return Result.Fail(_currentResult.Errors);
                    if (!_currentResult.Expect(TokenType.IDENTIFIER)) return Result.Fail(_currentResult.CreateExpectedError(TokenType.IDENTIFIER));

                    string subroutineName = (string)_currentResult.Value.Value;

                    _currentResult = _tokenizer.Advance();
                    if (_currentResult.IsFailed) return Result.Fail(_currentResult.Errors);
                    if (!_currentResult.Expect(Symbol.LPAR)) return Result.Fail(_currentResult.CreateExpectedError(Symbol.LPAR));

                    _currentResult = _tokenizer.Advance();
                    if (_currentResult.IsFailed) return Result.Fail(_currentResult.Errors);

                    Result<int> expressionListResult = CompileExpressionList();
                    if (expressionListResult.IsFailed) return expressionListResult.ToResult();
                    int argumentCount = expressionListResult.Value;

                    if (!_currentResult.Expect(Symbol.RPAR)) return Result.Fail(_currentResult.CreateExpectedError(Symbol.RPAR));

                    if (subroutineScopeIndex == -1 && classScopeIndex == -1)
                    {
                        _vmWriter.WriteCall($"{identifier}.{subroutineName}", argumentCount);
                    }
                    else if (subroutineScopeIndex != -1)
                    {
                        _vmWriter.WriteCall($"{_subroutineScopeTable.TypeOf(identifier)}.{subroutineName}", argumentCount);
                    }
                    else if (classScopeIndex != -1)
                    {
                        _vmWriter.WriteCall($"{_classScopeTable.TypeOf(identifier)}.{subroutineName}", argumentCount);
                    }
                    else
                    {
                        return Result.Fail(new SemanticError
                        {
                            Message = $"[Error] File: {_currentResult.Value.Filename}. '{identifier}' is undefined in this context.  Line: {_currentResult.Value.Row}"
                        });
                    }

                    _currentResult = _tokenizer.Advance();
                    if (_currentResult.IsFailed) return Result.Fail(_currentResult.Errors);
                }
                else
                {
                    if (subroutineScopeIndex == -1 && classScopeIndex == -1)
                    {
                        return Result.Fail(new SemanticError
                        {
                            Message = $"[Error] File: {_currentResult.Value.Filename}. '{identifier}' is undefined in this context.  Line: {_currentResult.Value.Row}"
                        });
                    }

                    if (subroutineScopeIndex != -1)
                        _vmWriter.WritePush(_subroutineScopeTable.KindOf(identifier).ToSegment(), subroutineScopeIndex);
                    else
                        _vmWriter.WritePush(_classScopeTable.KindOf(identifier).ToSegment(), classScopeIndex);
                }
            }
            else if (_currentResult.Expect(Symbol.LPAR))
            {
                _currentResult = _tokenizer.Advance();
                if (_currentResult.IsFailed) return Result.Fail(_currentResult.Errors);

                var expressionResult = CompileExpression();
                if (expressionResult.IsFailed) return expressionResult;

                if (!_currentResult.Expect(Symbol.RPAR)) return Result.Fail(_currentResult.CreateExpectedError(Symbol.RPAR));

                _currentResult = _tokenizer.Advance();
                if (_currentResult.IsFailed) return Result.Fail(_currentResult.Errors);
            }

            return Result.Ok();
        }

        private Result<int> CompileExpressionList()
        {
            int argumentCount = 0;

            if (_currentResult.IsTerm())
            {
                var expressionResult = CompileExpression();
                if (expressionResult.IsFailed) return expressionResult;
                argumentCount++;

                while (_currentResult.Expect(Symbol.COMMA))
                {
                    _currentResult = _tokenizer.Advance();
                    if (_currentResult.IsFailed) return Result.Fail(_currentResult.Errors);
                    expressionResult = CompileExpression();
                    if (expressionResult.IsFailed) return expressionResult;

                    argumentCount++;
                }
            }

            return Result.Ok(argumentCount);
        }

        public void Dispose()
        {
            _tokenizer.Dispose();
        }
    }
}
