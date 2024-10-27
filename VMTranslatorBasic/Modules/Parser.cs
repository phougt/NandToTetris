using VMTranslatorBasic.Enums;

namespace VMTranslatorBasic.Modules
{
    public class Parser : IDisposable
    {
        public CommandType Type { get; private set; }
        public bool HasMoreLines { get; private set; } = false;
        public bool IsValidCommand { get; private set; } = false;
        public string Arg1 { get; private set; } = string.Empty;
        public int Arg2 { get; private set; } = 0;

        private StreamReader _reader;
        private Dictionary<string, CommandType> command;
        private HashSet<string> _segments;
        private string _currentCommand = string.Empty;
        private uint _lineNumber = 1;

        public Parser(string filename)
        {
            _reader = new StreamReader(filename);
            HasMoreLines = !_reader.EndOfStream;
            command = new Dictionary<string, CommandType>()
            {
                { "push", CommandType.C_PUSH },
                { "pop", CommandType.C_POP},
                { "sub", CommandType.C_ARITHMETIC },
                { "add", CommandType.C_ARITHMETIC },
                { "neg", CommandType.C_ARITHMETIC },
                { "eq", CommandType.C_ARITHMETIC },
                { "gt", CommandType.C_ARITHMETIC },
                { "lt", CommandType.C_ARITHMETIC },
                { "and", CommandType.C_ARITHMETIC },
                { "or", CommandType.C_ARITHMETIC },
                { "not", CommandType.C_ARITHMETIC }
            };

            _segments = new HashSet<string>()
            {
                "argument",
                "local",
                "static",
                "constant",
                "this",
                "that",
                "pointer",
                "temp"
            };
        }

        public void Advance()
        {
            string? temp = _reader.ReadLine() ?? throw new EndOfStreamException();
            _currentCommand = temp;
            HasMoreLines = !_reader.EndOfStream;

            TrimWhiteSpaces();
            RemoveComment();

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

                IsValidCommand = true;
                _lineNumber++;
                return;
            }

            if (IsArithmeticCommand())
            {
                Type = CommandType.C_ARITHMETIC;
                IsValidCommand = true;
                _lineNumber++;
                return;
            }

            IsValidCommand = false;
            Type = CommandType.NONE;
            Console.Error.WriteLine($"{_currentCommand.Split()[0]}Command does not exist. Line: {_lineNumber}");
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
            return _currentCommand.Split(' ', StringSplitOptions.RemoveEmptyEntries)[0].Equals("push", StringComparison.OrdinalIgnoreCase);
        }

        private bool ValidatePushCommand()
        {
            string[] parts = _currentCommand.Split(' ');
            bool hasThreeParts = parts.Length == 3;

            if (!hasThreeParts)
            {
                Console.Error.WriteLine($"Expect 'COMMAND SEGMENT UINT' Format.  \nLine: {_lineNumber}");
            }
            else
            {
                Arg1 = parts[1];
            }

            bool isValidSegment = _segments.Contains(Arg1);
            bool isNumberArg2 = int.TryParse(parts[2], out int numberArg2);
            Arg2 = numberArg2;
            bool hasConstantSegment = string.Equals(Arg1, "constant", StringComparison.OrdinalIgnoreCase);
            bool hasPointerSegment = string.Equals(Arg1, "pointer", StringComparison.OrdinalIgnoreCase);

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
            return _currentCommand.Split(' ', StringSplitOptions.RemoveEmptyEntries)[0].Equals("pop", StringComparison.OrdinalIgnoreCase);
        }

        private bool ValidatePopCommand()
        {
            string[] parts = _currentCommand.Split(' ');
            bool hasThreeParts = parts.Length == 3;

            if (!hasThreeParts)
            {
                Console.Error.WriteLine($"Expect 'COMMAND SEGMENT UINT' Format.  \nLine: {_lineNumber}");
            }
            else
            {
                Arg1 = parts[1];
            }

            bool hasConstantSegment = string.Equals(Arg1, "constant", StringComparison.OrdinalIgnoreCase);
            bool hasPointerSegment = string.Equals(Arg1, "pointer", StringComparison.OrdinalIgnoreCase);
            bool isValidSegment = _segments.Contains(Arg1) && !hasConstantSegment;
            bool isNumberArg2 = int.TryParse(parts[2], out int numberArg2);
            Arg2 = numberArg2; 

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
            Arg1 = _currentCommand;

            return Arg1.Equals("add", StringComparison.OrdinalIgnoreCase)
                || Arg1.Equals("neg", StringComparison.OrdinalIgnoreCase)
                || Arg1.Equals("sub", StringComparison.OrdinalIgnoreCase)
                || Arg1.Equals("eq", StringComparison.OrdinalIgnoreCase)
                || Arg1.Equals("gt", StringComparison.OrdinalIgnoreCase)
                || Arg1.Equals("lt", StringComparison.OrdinalIgnoreCase)
                || Arg1.Equals("and", StringComparison.OrdinalIgnoreCase)
                || Arg1.Equals("or", StringComparison.OrdinalIgnoreCase)
                || Arg1.Equals("not", StringComparison.OrdinalIgnoreCase);
        }

        public void Dispose()
        {
            _reader?.Dispose();
        }
    }
}
