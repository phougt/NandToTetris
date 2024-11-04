using System.Collections.Immutable;
using VMTranslator.Enums;

namespace VMTranslator.Modules
{
    public class Parser : IDisposable
    {
        public CommandType Type { get; private set; }
        public bool HasMoreLines { get; private set; } = false;
        public bool IsValidCommand { get; private set; } = false;
        public string Arg1 { get; private set; } = string.Empty;
        public int Arg2 { get; private set; } = -1;

        private StreamReader _reader;
        private ImmutableArray<string> _segments;
        private ImmutableArray<char> _allowedCharForName;
        private string _currentCommand = string.Empty;
        private uint _lineNumber = 1;

        public Parser(string filename)
        {
            _reader = new StreamReader(filename);
            _segments = ["argument", "local", "static", "constant", "this", "that", "pointer", "temp"];
            _allowedCharForName = ['_', '.', ':'];
            HasMoreLines = !_reader.EndOfStream;
        }

        public void Advance()
        {
            string? temp = _reader.ReadLine() ?? throw new EndOfStreamException();
            _currentCommand = temp;
            HasMoreLines = !_reader.EndOfStream;

            RemoveComment();
            TrimWhiteSpaces();

            if (_currentCommand == string.Empty)
            {
                Type = CommandType.NONE;
                IsValidCommand = true;
                _lineNumber++;
                return;
            }

            if (IsPushCommand())
            {
                Type = CommandType.C_PUSH;

                if (!ValidatePushCommand())
                {
                    IsValidCommand = false;
                    _lineNumber++;
                    return;
                }

                string[] parts = _currentCommand.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                Arg1 = parts[1];
                Arg2 = int.Parse(parts[2]);
                IsValidCommand = true;
                _lineNumber++;
                return;
            }

            if (IsPopCommand())
            {
                Type = CommandType.C_POP;

                if (!ValidatePopCommand())
                {
                    IsValidCommand = false;
                    _lineNumber++;
                    return;
                }

                string[] parts = _currentCommand.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                Arg1 = parts[1];
                Arg2 = int.Parse(parts[2]);
                IsValidCommand = true;
                _lineNumber++;
                return;
            }

            if (IsArithmeticCommand())
            {
                Type = CommandType.C_ARITHMETIC;
                IsValidCommand = true;
                Arg1 = _currentCommand;
                _lineNumber++;
                return;
            }


            if (IsLabelCommand())
            {
                Type = CommandType.C_LABEL;

                if (!ValidateLabelCommand())
                {
                    IsValidCommand = false;
                    _lineNumber++;
                    return;
                }

                string[] parts = _currentCommand.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                Arg1 = parts[1];
                IsValidCommand = true;
                _lineNumber++;
                return;
            }

            if (IsGotoCommand())
            {
                Type = CommandType.C_GOTO;

                if (!ValidateGotoCommand())
                {
                    IsValidCommand = false;
                    _lineNumber++;
                    return;
                }

                string[] parts = _currentCommand.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                Arg1 = parts[1];
                IsValidCommand = true;
                _lineNumber++;
                return;
            }

            if (IsIfCommand())
            {
                Type = CommandType.C_IF;

                if (!ValidateIfCommand())
                {
                    IsValidCommand = false;
                    _lineNumber++;
                    return;
                }

                string[] parts = _currentCommand.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                Arg1 = parts[1];
                IsValidCommand = true;
                _lineNumber++;
                return;
            }

            if (IsCallCommand())
            {
                Type = CommandType.C_CALL;

                if (!ValidateCallCommand())
                {
                    IsValidCommand = false;
                    _lineNumber++;
                    return;
                }

                string[] parts = _currentCommand.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                Arg1 = parts[1];
                Arg2 = int.Parse(parts[2]);
                IsValidCommand = true;
                _lineNumber++;
                return;
            }

            if (IsReturnCommand())
            {
                Type = CommandType.C_RETURN;
                IsValidCommand = true;
                _lineNumber++;
                return;
            }

            if (IsFunctionCommand())
            {
                Type = CommandType.C_FUNCTION;

                if (!ValidateFunctionCommand())
                {
                    IsValidCommand = false;
                    _lineNumber++;
                    return;
                }

                string[] parts = _currentCommand.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                Arg1 = parts[1];
                Arg2 = int.Parse(parts[2]);
                IsValidCommand = true;
                _lineNumber++;
                return;
            }

            IsValidCommand = false;
            Type = CommandType.NONE;
            Console.Error.WriteLine($"{_currentCommand.Split()[0]} command does not exist. Line: {_lineNumber}");
        }

        private void RemoveComment()
        {
            int commentStartIndex = _currentCommand.IndexOf(@"//");
            if (commentStartIndex != -1)
            {
                _currentCommand = _currentCommand.Remove(commentStartIndex);
            }
        }

        private void TrimWhiteSpaces()
        {
            _currentCommand = _currentCommand.Trim();
        }

        private bool IsPushCommand()
        {
            return _currentCommand.Split(' ', StringSplitOptions.RemoveEmptyEntries)[0].Equals("push");
        }

        private bool ValidatePushCommand()
        {
            string[] parts = _currentCommand.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            bool hasThreeParts = parts.Length == 3;

            if (!hasThreeParts)
            {
                Console.Error.WriteLine($"Expect 'push SEGMENT UINT' Format.  \nLine: {_lineNumber}");
                return false;
            }

            bool isValidSegment = _segments.Contains(parts[1]);
            bool isNumberArg2 = int.TryParse(parts[2], out int numberArg2);
            bool hasConstantSegment = string.Equals(parts[1], "constant");
            bool hasPointerSegment = string.Equals(parts[1], "pointer");

            if (!isNumberArg2)
                Console.Error.WriteLine($"Expect Arg2 to be a positive number.  \nLine: {_lineNumber}");

            if (isNumberArg2 && hasConstantSegment)
            {
                if (!(numberArg2 >= 0 && numberArg2 <= 32767))
                    Console.Error.WriteLine($"Expect Arg2 to be a positive number in range 0..32767.  \nLine: {_lineNumber}");
            }

            if (isNumberArg2 && hasPointerSegment)
            {
                if (!(numberArg2 == 0 || numberArg2 == 1))
                    Console.Error.WriteLine($"Expect Arg2 to be a positive number in range 0..1.  \nLine: {_lineNumber}");
            }

            if (!isValidSegment)
                Console.Error.WriteLine($"Segment is not valid.  \nLine: {_lineNumber}");

            return (isNumberArg2 && hasConstantSegment && numberArg2 >= 0 && numberArg2 <= 32767)
                || (isNumberArg2 && hasPointerSegment && (numberArg2 == 0 || numberArg2 == 1))
                || (isNumberArg2 && isValidSegment);
        }

        private bool IsPopCommand()
        {
            return _currentCommand.Split(' ', StringSplitOptions.RemoveEmptyEntries)[0].Equals("pop");
        }

        private bool ValidatePopCommand()
        {
            string[] parts = _currentCommand.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            bool hasThreeParts = parts.Length == 3;

            if (!hasThreeParts)
            {
                Console.Error.WriteLine($"Expect 'pop SEGMENT UINT' Format.  \nLine: {_lineNumber}");
                return false;
            }

            bool hasConstantSegment = string.Equals(parts[1], "constant");
            bool hasPointerSegment = string.Equals(parts[1], "pointer");
            bool isValidSegment = _segments.Contains(parts[1]) && !hasConstantSegment;
            bool isNumberArg2 = int.TryParse(parts[2], out int numberArg2);

            if (!isNumberArg2)
                Console.Error.WriteLine($"Expect Arg2 to be a positive number.  \nLine: {_lineNumber}");

            if (!isValidSegment)
                Console.Error.WriteLine($"POP command doesn't support 'constant' segment.  \nLine: {_lineNumber}");

            if (isNumberArg2 && hasPointerSegment)
            {
                if (!(numberArg2 == 0 || numberArg2 == 1))
                    Console.Error.WriteLine($"Expect Arg2 to be a positive number in range 0..1.  \nLine: {_lineNumber}");
            }

            return isNumberArg2 && isValidSegment
                || isNumberArg2 && hasPointerSegment && (numberArg2 == 0 || numberArg2 == 1);
        }

        private bool IsArithmeticCommand()
        {
            return _currentCommand.Equals("add")
                || _currentCommand.Equals("neg")
                || _currentCommand.Equals("sub")
                || _currentCommand.Equals("eq")
                || _currentCommand.Equals("gt")
                || _currentCommand.Equals("lt")
                || _currentCommand.Equals("and")
                || _currentCommand.Equals("or")
                || _currentCommand.Equals("not");
        }

        private bool IsLabelCommand()
        {
            return _currentCommand.Split(' ', StringSplitOptions.RemoveEmptyEntries)[0].Equals("label");
        }

        private bool ValidateLabelCommand()
        {
            string[] parts = _currentCommand.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            bool hasTwoParts = parts.Length == 2;

            if (!hasTwoParts)
            {
                Console.Error.WriteLine($"Expect 'label NAME' Format.  \nLine: {_lineNumber}");
                return false;
            }

            bool hasAllowedChar = parts[1].All((c) => char.IsAsciiLetter(c)
                                       || char.IsAsciiDigit(c)
                                       || _allowedCharForName.Contains(c));
            bool isLabelNameStartWithNumber = char.IsNumber(parts[1][0]);

            if (!hasAllowedChar)
                Console.Error.WriteLine($"Label's Name is not valid.  \nLine: {_lineNumber}");

            if (isLabelNameStartWithNumber)
                Console.Error.WriteLine($"Label's Name is not valid. Label Name can contain number, but can not start with a number.  \nLine: {_lineNumber}");

            return hasAllowedChar
                && !isLabelNameStartWithNumber;

        }

        private bool IsGotoCommand()
        {
            return _currentCommand.Split(' ', StringSplitOptions.RemoveEmptyEntries)[0].Equals("goto");
        }

        private bool ValidateGotoCommand()
        {
            string[] parts = _currentCommand.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            bool hasTwoParts = parts.Length == 2;

            if (!hasTwoParts)
            {
                Console.Error.WriteLine($"Expect 'goto LABEL_NAME' Format.  \nLine: {_lineNumber}");
                return false;
            }

            bool hasAllowedChar = parts[1].All((c) => char.IsAsciiLetter(c)
                                       || char.IsAsciiDigit(c)
                                       || _allowedCharForName.Contains(c));
            bool isLabelNameStartWithNumber = char.IsNumber(parts[1][0]);

            if (!hasAllowedChar)
                Console.Error.WriteLine($"Label's Name is not valid.  \nLine: {_lineNumber}");

            if (isLabelNameStartWithNumber)
                Console.Error.WriteLine($"Label's Name is not valid. Label Name can contain number, but can not start with a number.  \nLine: {_lineNumber}");

            return hasAllowedChar
                && !isLabelNameStartWithNumber;

        }

        private bool IsIfCommand()
        {
            return _currentCommand.Split(' ', StringSplitOptions.RemoveEmptyEntries)[0].Equals("if-goto");
        }

        private bool ValidateIfCommand()
        {
            string[] parts = _currentCommand.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            bool hasTwoParts = parts.Length == 2;

            if (!hasTwoParts)
            {
                Console.Error.WriteLine($"Expect 'if-goto LABEL_NAME' Format.  \nLine: {_lineNumber}");
                return false;
            }

            bool hasAllowedChar = parts[1].All((c) => char.IsAsciiLetter(c)
                                       || char.IsAsciiDigit(c)
                                       || _allowedCharForName.Contains(c));
            bool isLabelNameStartWithNumber = char.IsNumber(parts[1][0]);

            if (!hasAllowedChar)
                Console.Error.WriteLine($"Label's Name is not valid.  \nLine: {_lineNumber}");

            if (isLabelNameStartWithNumber)
                Console.Error.WriteLine($"Label's Name is not valid. Label Name can contain number, but can not start with a number.  \nLine: {_lineNumber}");

            return hasAllowedChar
                && !isLabelNameStartWithNumber;
        }

        private bool IsCallCommand()
        {
            return _currentCommand.Split(' ', StringSplitOptions.RemoveEmptyEntries)[0].Equals("call");
        }

        private bool ValidateCallCommand()
        {
            string[] parts = _currentCommand.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            bool hasThreeParts = parts.Length == 3;

            if (!hasThreeParts)
            {
                Console.Error.WriteLine($"Expect 'call FUNCTION_NAME NUM_ARG' Format.  \nLine: {_lineNumber}");
                return false;
            }

            bool hasAllowedChar = parts[1].All((c) => char.IsAsciiLetter(c)
                                       || char.IsAsciiDigit(c)
                                       || _allowedCharForName.Contains(c));
            bool isFuncNameStartWithNumber = char.IsNumber(parts[1][0]);
            bool isNumberArg2 = int.TryParse(parts[2], out int numberArg2);

            if (!hasAllowedChar)
                Console.Error.WriteLine($"Function's Name is not valid.  \nLine: {_lineNumber}");

            if (isFuncNameStartWithNumber)
                Console.Error.WriteLine($"Function's Name is not valid. Label Name can contain number, but can not start with a number.  \nLine: {_lineNumber}");

            if (isNumberArg2)
            {
                if (!(numberArg2 >= 0))
                    Console.Error.WriteLine($"Expect number of arguments to be 0 or larger.  \nLine: {_lineNumber}");
            }

            return hasAllowedChar
                && !isFuncNameStartWithNumber
                && (isNumberArg2 && numberArg2 >= 0);
        }

        private bool IsFunctionCommand()
        {
            return _currentCommand.Split(' ', StringSplitOptions.RemoveEmptyEntries)[0].Equals("function");
        }

        private bool ValidateFunctionCommand()
        {
            string[] parts = _currentCommand.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            bool hasThreeParts = parts.Length == 3;

            if (!hasThreeParts)
            {
                Console.Error.WriteLine($"Expect 'function FUNCTION_NAME NUM_LOCAL' Format.  \nLine: {_lineNumber}");
                return false;
            }

            bool hasAllowedChar = parts[1].All((c) => char.IsAsciiLetter(c)
                                       || char.IsAsciiDigit(c)
                                       || _allowedCharForName.Contains(c));
            bool isFuncNameStartWithNumber = char.IsNumber(parts[1][0]);
            bool isNumberArg2 = int.TryParse(parts[2], out int numberArg2);

            if (!hasAllowedChar)
                Console.Error.WriteLine($"Function's Name is not valid.  \nLine: {_lineNumber}");

            if (isFuncNameStartWithNumber)
                Console.Error.WriteLine($"Function's Name is not valid. Label Name can contain number, but can not start with a number.  \nLine: {_lineNumber}");

            if (isNumberArg2)
            {
                if (!(numberArg2 >= 0))
                    Console.Error.WriteLine($"Expect number of local variable to be 0 or larger.  \nLine: {_lineNumber}");
            }

            return hasAllowedChar
                && !isFuncNameStartWithNumber
                && (isNumberArg2 && numberArg2 >= 0);
        }

        private bool IsReturnCommand()
        {
            return _currentCommand.Split(' ', StringSplitOptions.RemoveEmptyEntries)[0].Equals("return");
        }

        public void Dispose()
        {
            _reader?.Dispose();
        }
    }
}
