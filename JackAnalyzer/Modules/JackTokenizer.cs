using JackAnalyzer.Enums;
using JackAnalyzer.Models;
using FluentResults;
using System.Collections.Immutable;

namespace JackAnalyzer.Modules
{
    public class JackTokenizer : IDisposable
    {
        public bool HasMoreTokens { get; private set; } = true;
        public Token Token { get; private set; }
        private StreamReader _reader = StreamReader.Null;
        private ImmutableArray<char> _validSymbols;
        private ImmutableArray<string> _validKeywords;
        public JackTokenizer(string filePath)
        {
            _reader = new StreamReader(filePath);
            HasMoreTokens = !_reader.EndOfStream;
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
            if (_reader.EndOfStream)
            {
                return Result.Fail("End of Stream");
            }

            string tempWord = string.Empty;

            while (HasMoreTokens)
            {
                char tempChar = (char)_reader.Peek();
                HasMoreTokens = !_reader.EndOfStream;

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
                bool startWithSymbol = _validSymbols.Any(c => c.Equals(tempWord[0]));

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
                        return Result.Ok(new Token(TokenType.STRING_CONST, tempWord.Remove(0, 1)));
                        // .Remove(0, 1) is to remove the first double quote
                    }
                    else if (!tempChar.Equals(Environment.NewLine))
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
                        if (_validKeywords.Any(keyword => keyword.Equals(tempWord)))
                        {
                            return Result.Ok(new Token(TokenType.KEYWORD, tempWord));
                        }
                        else
                        {
                            return Result.Ok(new Token(TokenType.IDENTIFIER, tempWord));
                        }
                    }
                }
            }

            return Result.Fail("End of Logic");
        }

        public void Dispose()
        {
            _reader.Dispose();
        }
    }
}
