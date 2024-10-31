using VMTranslator.Enums;

namespace VMTranslator.Modules
{
    public class Coder
    {

        public string Filename
        {
            get { return _filename.Trim().Replace(' ', '_'); }
            set { _filename = value; }
        }

        private string _filename = string.Empty;
        private const int TEMP = 5;
        private const int STATIC = 16;
        private int _eqCount = 0;
        private int _ltCount = 0;
        private int _gtCount = 0;

        public string Arithmethic(string command)
        {
            switch (command.ToLower())
            {
                case "neg":
                    return $"""
                            @SP
                            A=M-1
                            D=-M
                            @SP
                            A=M-1
                            M=D
                            """;
                case "not":
                    return $"""
                             @SP
                             A=M-1
                             D=!M
                             @SP
                             A=M-1
                             M=D
                             """;
                case "add":
                    return $"""
                            @SP
                            AM=M-1
                            D=M
                            A=A-1
                            M=M+D
                            """;
                case "sub":
                    return $"""
                            @SP
                            AM=M-1
                            D=M
                            A=A-1
                            M=M-D
                            """;
                case "and":
                    return $"""
                            @SP
                            AM=M-1
                            D=M
                            A=A-1
                            M=D&M
                            """;
                case "or":
                    return $"""
                            @SP
                            AM=M-1
                            D=M
                            A=A-1
                            M=D|M
                            """;
                case "eq":
                    _eqCount++;
                    return $"""
                            @RETURN_ADDRESS_EQ_{_eqCount}
                            D=A
                            @R14
                            M=D
                            @START_EQ
                            0;JMP
                            (RETURN_ADDRESS_EQ_{_eqCount})
                            """;
                case "lt":
                    _ltCount++;
                    return $"""
                            @RETURN_ADDRESS_LT_{_ltCount}
                            D=A
                            @R14
                            M=D
                            @START_LT
                            0;JMP
                            (RETURN_ADDRESS_LT_{_ltCount})
                            """;
                case "gt":
                    _gtCount++;
                    return $"""
                            @RETURN_ADDRESS_GT_{_gtCount}
                            D=A
                            @R14
                            M=D
                            @START_GT
                            0;JMP
                            (RETURN_ADDRESS_GT_{_gtCount})
                            """;
                default:
                    return string.Empty;
            }
        }

        public string PushPop(CommandType commandType, string segment, int index)
        {
            if (commandType == CommandType.C_PUSH)
            {
                switch (segment.ToLower())
                {
                    case "constant":
                        return $"""
                                @{index}
                                D=A
                                @SP
                                A=M
                                M=D
                                @SP
                                M=M+1
                                """;
                    case "this":
                        return $""" 
                                @{index}
                                D=A
                                @THIS
                                A=M+D
                                D=M
                                @SP
                                A=M
                                M=D
                                @SP
                                M=M+1
                                """;
                    case "that":
                        return $"""
                                @{index}
                                D=A
                                @THAT
                                A=M+D
                                D=M
                                @SP
                                A=M
                                M=D
                                @SP
                                M=M+1
                                """;
                    case "local":
                        return $"""
                                @{index}
                                D=A
                                @LCL
                                A=M+D
                                D=M
                                @SP
                                A=M
                                M=D
                                @SP
                                M=M+1
                                """;
                    case "argument":
                        return $"""
                                @{index}
                                D=A
                                @ARG
                                A=M+D
                                D=M
                                @SP
                                A=M
                                M=D
                                @SP
                                M=M+1
                                """;
                    case "static":
                        return $"""
                                @{Filename}.{index}
                                D=M
                                @SP
                                A=M
                                M=D
                                @SP
                                M=M+1
                                """;
                    case "temp":
                        return $"""
                                @{index}
                                D=A
                                @{TEMP}
                                A=A+D
                                D=M
                                @SP
                                A=M
                                M=D
                                @SP
                                M=M+1
                                """;
                    case "pointer":
                        return $"""
                                @{index}
                                D=A
                                @THIS
                                A=A+D
                                D=M
                                @SP
                                A=M
                                M=D
                                @SP
                                M=M+1
                                """;
                    default:
                        return string.Empty;
                };
            }
            else
            {
                switch (segment.ToLower())
                {
                    case "this":
                        return $"""
                                @{index}
                                D=A
                                @THIS
                                D=M+D
                                @R13
                                M=D
                                @SP
                                AM=M-1
                                D=M
                                @R13
                                A=M
                                M=D
                                """;
                    case "that":
                        return $"""
                                @{index}
                                D=A
                                @THAT
                                D=M+D
                                @R13
                                M=D
                                @SP
                                AM=M-1
                                D=M
                                @R13
                                A=M
                                M=D
                                """;
                    case "local":
                        return $"""
                                @{index}
                                D=A
                                @LCL
                                D=M+D
                                @R13
                                M=D
                                @SP
                                AM=M-1
                                D=M
                                @R13
                                A=M
                                M=D
                                """;
                    case "argument":
                        return $"""
                                @{index}
                                D=A
                                @ARG
                                D=M+D
                                @R13
                                M=D
                                @SP
                                AM=M-1
                                D=M
                                @R13
                                A=M
                                M=D
                                """;
                    case "static":
                        return $"""
                                @SP
                                AM=M-1
                                D=M
                                @{Filename}.{index}
                                M=D
                                """;
                    case "temp":
                        return $"""
                                @{index}
                                D=A
                                @{TEMP}
                                D=A+D
                                @R13
                                M=D
                                @SP
                                AM=M-1
                                D=M
                                @R13
                                A=M
                                M=D
                                """;
                    case "pointer":
                        return $"""
                                @{index}
                                D=A
                                @THIS
                                D=A+D
                                @R13
                                M=D
                                @SP
                                AM=M-1
                                D=M
                                @R13
                                A=M
                                M=D
                                """;
                    default:
                        return string.Empty;
                };
            }
        }
    }
}
