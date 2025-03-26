using JackAnalyzer.Enums;
using JackAnalyzer.Models;
using FluentResults;
using System.Collections.Immutable;
using JackAnalyzer.Errors;
using JackAnalyzer.Extensions;
using System.Security.Cryptography;

namespace JackAnalyzer.Modules
{
    public class JackTokenizer : IDisposable
    {
        private readonly StreamReader _reader;
        private readonly HashSet<char> _validSymbols;
        private readonly HashSet<string> _validKeywords;
        private uint _lineNumber = 1;
        private string _filepath = string.Empty;
        public JackTokenizer(string filePath)
        {
            _reader = new StreamReader(filePath);
            _filepath = filePath;
            _validSymbols = [
                '{',
                '}',
                '(',
                ')',
                '[',
                ']',
                '.',
                ',',
                ';',
                '+',
                '-',
                '*',
                '/',
                '&',
                '|',
                '<',
                '>',
                '=',
                '~'
            ];
            _validKeywords =
            [
                "class",
                "method",
                "function",
                "constructor",
                "int",
                "boolean",
                "char",
                "void",
                "var",
                "static",
                "field",
                "let",
                "do",
                "if",
                "else",
                "while",
                "return",
                "true",
                "false",
                "null",
                "this"
            ];
        }

        public Result<Token> Advance()
        {
            bool hasMoreChars = !_reader.EndOfStream;

            if (!hasMoreChars)
            {
                return Result.Fail(new EndOfStreamError { Message = "End of Stream!" });
            }

            string tempWord = string.Empty;

            while (hasMoreChars)
            {
                char tempChar = (char)_reader.Peek();
                hasMoreChars = !_reader.EndOfStream;

                bool startWithWhiteSpace = char.IsWhiteSpace(tempChar);

                if (tempWord.Equals(string.Empty))
                {
                    if (!startWithWhiteSpace)
                        tempWord += tempChar;

                    if (tempChar.Equals('\n'))
                        _lineNumber++;

                    _reader.Read();
                    continue;
                }

                bool startWithSlashAsterisk = tempWord.StartsWith("/*");
                bool endWithSlashAsterisk = tempWord.EndsWith("*/");
                bool startWithDigit = char.IsAsciiDigit(tempWord[0]);
                bool startWithDoubleQuote = tempWord[0].Equals('\"');
                bool startWithLetterOrUnderscore = char.IsAsciiLetter(tempWord[0]) || tempWord[0].Equals('_');
                bool startWithSymbol = _validSymbols.Contains(tempWord[0]);

                if (startWithSymbol)
                {
                    if (startWithSlashAsterisk)
                    {
                        if (tempChar == '*' || tempChar == '/')
                        {
                            tempWord += tempChar;
                            _reader.Read();
                            continue;
                        }

                        if (endWithSlashAsterisk)
                        {
                            tempWord = string.Empty;
                            continue;
                        }

                        _reader.Read();
                        continue;
                    }
                    else if (tempWord[0] == '/' && _reader.Peek() == '*' && !startWithSlashAsterisk)
                    {
                        tempWord += tempChar;
                        continue;
                    }
                    else if (tempWord[0] == '/' && _reader.Peek() == '/')
                    {
                        _reader.ReadLine();
                        tempWord = string.Empty;
                        _lineNumber++;
                        continue;
                    }

                    return Result.Ok(new Token(TokenType.SYMBOL, tempWord.ToSymbol(), row: _lineNumber, filename: _filepath));
                }
                else if (startWithDigit)
                {
                    if (!char.IsAsciiDigit(tempChar))
                    {
                        return Result.Ok(new Token(TokenType.INT_CONST, int.Parse(tempWord), row: _lineNumber, filename: _filepath));
                    }
                    else
                    {
                        tempWord += tempChar;
                        _reader.Read();
                        continue;
                    }
                }
                else if (startWithDoubleQuote)
                {
                    if (tempChar.Equals('\"'))
                    {
                        _reader.Read();
                        return Result.Ok(new Token(TokenType.STRING_CONST, tempWord.Remove(0, 1), row: _lineNumber, filename: _filepath));
                        // .Remove(0, 1) is to remove the first double quote
                    }
                    else if (tempChar.Equals('\n') || tempChar.Equals('\r'))
                    {
                        return Result.Fail(new InvalidCharError { Message = "Illegal newline char in string literal." });
                    }
                    else
                    {
                        tempWord += tempChar;
                        _reader.Read();
                        continue;
                    }
                }
                else if (startWithLetterOrUnderscore)
                {
                    bool isValidChar = char.IsAsciiLetterOrDigit(tempChar) || tempChar.Equals('_');

                    if (isValidChar)
                    {
                        tempWord += tempChar;
                        _reader.Read();
                        continue;
                    }
                    else
                    {
                        if (_validKeywords.Contains(tempWord))
                        {
                            return Result.Ok(new Token(TokenType.KEYWORD, Enum.Parse<Keyword>(tempWord.ToUpper()), row: _lineNumber, filename: _filepath));
                        }
                        else
                        {
                            return Result.Ok(new Token(TokenType.IDENTIFIER, tempWord, row: _lineNumber, filename: _filepath));
                        }
                    }
                }
            }

            return Result.Fail(new EndOfStreamError { Message = "End of stream!" });
        }

        public void Dispose()
        {
            _reader.Dispose();
        }
    }
}
