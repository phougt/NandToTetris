using JackAnalyzer.Enums;
using JackAnalyzer.Models;
using FluentResults;
using System.Collections.Immutable;
using JackAnalyzer.Errors;

namespace JackAnalyzer.Modules
{
    public class JackTokenizer : IDisposable
    {
        private readonly StreamReader _reader;
        private readonly HashSet<char> _validSymbols;
        private readonly HashSet<string> _validKeywords;
        public JackTokenizer(string filePath)
        {
            _reader = new StreamReader(filePath);
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

                    _reader.Read();
                    continue;
                }

                bool startWithDigit = char.IsAsciiDigit(tempWord[0]);
                bool startWithDoubleQuote = tempWord[0].Equals('\"');
                bool startWithLetter = char.IsAsciiLetter(tempWord[0]);
                bool startWithSymbol = _validSymbols.Contains(tempWord[0]);

                if (startWithSymbol)
                {
                    return Result.Ok(new Token(TokenType.SYMBOL, tempWord[0]));
                }
                else if (startWithDigit)
                {
                    if (!char.IsAsciiDigit(tempChar))
                    {
                        return Result.Ok(new Token(TokenType.INT_CONST, int.Parse(tempWord)));
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
                        return Result.Ok(new Token(TokenType.STRING_CONST, tempWord.Remove(0, 1)));
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
                else if (startWithLetter)
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
                            return Result.Ok(new Token(TokenType.KEYWORD, Enum.Parse<KeywordType>(tempWord.ToUpper())));
                        }
                        else
                        {
                            return Result.Ok(new Token(TokenType.IDENTIFIER, tempWord));
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
