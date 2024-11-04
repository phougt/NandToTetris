using Microsoft.VisualBasic.FileIO;
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
        private int _eqCount = 0;
        private int _ltCount = 0;
        private int _gtCount = 0;
        private int _callCount = 0;
        private int _ifFalseCount = 0;

        public string Arithmethic(string command)
        {
            switch (command)
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
                switch (segment)
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
                                @5
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
                switch (segment)
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
                                @5
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

        public string Init()
        {
            _callCount++;
            return $"""
                    (START_PROGRAM)
                    @256
                    D=A
                    @SP
                    M=D
                    @RETURN_ADDRESS_CALL_{_callCount}
                    D=A
                    @SP
                    A=M
                    M=D
                    @SP
                    M=M+1
                    @LCL
                    D=M
                    @SP
                    A=M
                    M=D
                    @SP
                    M=M+1
                    @ARG
                    D=M
                    @SP
                    A=M
                    M=D
                    @SP
                    M=M+1
                    @THIS
                    D=M
                    @SP
                    A=M
                    M=D
                    @SP
                    M=M+1
                    @THAT
                    D=M
                    @SP
                    A=M
                    M=D
                    @SP
                    MD=M+1
                    @5
                    D=D-A
                    @ARG
                    M=D
                    @SP
                    D=M
                    @LCL
                    M=D
                    @Sys.init
                    0;JMP
                    (RETURN_ADDRESS_CALL_{_callCount})
                    """;
        }

        public string Label(string label)
        {
            return $"({label})";
        }

        public string Goto(string label)
        {
            return $"""
                    @{label}
                    0;JMP
                    """;
        }

        public string If(string label)
        {
            _ifFalseCount++;
            return $"""
                    @SP
                    AM=M-1
                    D=M
                    @IF_FALSE_{_ifFalseCount}
                    D;JEQ
                    @{label}
                    0;JMP
                    (IF_FALSE_{_ifFalseCount})
                    """;
        }

        public string Call(string functionName, int argsLength)
        {
            _callCount++;
            return $"""
                    @RETURN_ADDRESS_CALL_{_callCount}
                    D=A
                    @SP
                    A=M
                    M=D
                    @SP
                    M=M+1
                    @LCL
                    D=M
                    @SP
                    A=M
                    M=D
                    @SP
                    M=M+1
                    @ARG
                    D=M
                    @SP
                    A=M
                    M=D
                    @SP
                    M=M+1
                    @THIS
                    D=M
                    @SP
                    A=M
                    M=D
                    @SP
                    M=M+1
                    @THAT
                    D=M
                    @SP
                    A=M
                    M=D
                    @SP
                    MD=M+1
                    @{argsLength}
                    D=D-A
                    @5
                    D=D-A
                    @ARG
                    M=D
                    @SP
                    D=M
                    @LCL
                    M=D
                    @{functionName}
                    0;JMP
                    (RETURN_ADDRESS_CALL_{_callCount})
                    """;
        }

        public string Return()
        {
            return $"""
                    @LCL
                    D=M
                    @R14 // LCL copies to R14
                    M=D
                    @5
                    A=D-A
                    D=M
                    @R15 //return address store in R15
                    M=D
                    @ARG // pop return value back to arg0
                    D=M
                    @R13
                    M=D
                    @SP
                    AM=M-1
                    D=M
                    @R13
                    A=M
                    M=D
                    @ARG
                    D=M
                    @SP
                    M=D+1
                    @R14
                    AM=M-1
                    D=M
                    @THAT
                    M=D
                    @R14
                    AM=M-1
                    D=M
                    @THIS
                    M=D
                    @R14
                    AM=M-1
                    D=M
                    @ARG
                    M=D
                    @R14
                    AM=M-1
                    D=M
                    @LCL
                    M=D
                    @R15
                    A=M
                    0;JMP
                    """;
        }

        public string Function(string functionName, int localLength)
        {
            string final = $"""
                           ({functionName})
                           """;

            for (int i = 0; i < localLength; i++)
            {
                final += Environment.NewLine;
                final += $"""
                         @0
                         D=A
                         @SP
                         A=M
                         M=D
                         @SP
                         M=M+1
                         """;
            }

            return final;
        }
    }
}
