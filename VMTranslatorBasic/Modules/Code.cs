using VMTranslatorBasic.Enums;

namespace VMTranslatorBasic.Modules
{
    public static class Code
    {
        private const int TEMP = 5;
        private const int STATIC = 16;
        private static int _eqCount = 0;
        private static int _ltCount = 0;
        private static int _gtCount = 0;

        public static string Arithmethic(string command)
        {
            string assemblyInstructions = string.Empty;

            if (command.Equals("neg", StringComparison.OrdinalIgnoreCase))
            {
                assemblyInstructions = $"""
                                        @SP
                                        A=M-1
                                        D=-M
                                        @SP
                                        A=M-1
                                        M=D
                                        """;
                return assemblyInstructions;
            }
            else if (command.Equals("not", StringComparison.OrdinalIgnoreCase))
            {
                assemblyInstructions = $"""
                                        @SP
                                        A=M-1
                                        D=!M
                                        @SP
                                        A=M-1
                                        M=D
                                        """;
                return assemblyInstructions;
            }
            else if (command.Equals("add", StringComparison.OrdinalIgnoreCase))
            {
                assemblyInstructions = $"""
                                        @SP
                                        AM=M-1
                                        D=M
                                        A=A-1
                                        M=M+D
                                        """;
                return assemblyInstructions;
            }
            else if (command.Equals("sub", StringComparison.OrdinalIgnoreCase))
            {
                assemblyInstructions = $"""
                                        @SP
                                        AM=M-1
                                        D=M
                                        A=A-1
                                        M=M-D
                                        """;
                return assemblyInstructions;
            }
            else if (command.Equals("and", StringComparison.OrdinalIgnoreCase))
            {
                assemblyInstructions = $"""
                                        @SP
                                        AM=M-1
                                        D=M
                                        A=A-1
                                        M=D&M
                                        """;
                return assemblyInstructions;
            }
            else if (command.Equals("or", StringComparison.OrdinalIgnoreCase))
            {
                assemblyInstructions = $"""
                                        @SP
                                        AM=M-1
                                        D=M
                                        A=A-1
                                        M=D|M
                                        """;
                return assemblyInstructions;
            }
            else if (command.Equals("eq", StringComparison.OrdinalIgnoreCase))
            {
                assemblyInstructions = $"""
                                        @RETURN_ADDRESS_EQ_{_eqCount}
                                        D=A
                                        @R14
                                        M=D
                                        @START_EQ
                                        0;JMP
                                        (RETURN_ADDRESS_EQ_{_eqCount})
                                        """;
                _eqCount++;
                return assemblyInstructions;
            }
            else if (command.Equals("lt", StringComparison.OrdinalIgnoreCase))
            {
                assemblyInstructions = $"""
                                        @RETURN_ADDRESS_LT_{_ltCount}
                                        D=A
                                        @R14
                                        M=D
                                        @START_LT
                                        0;JMP
                                        (RETURN_ADDRESS_LT_{_ltCount})
                                        """;
                _ltCount++;
                return assemblyInstructions;
            }
            else if (command.Equals("gt", StringComparison.OrdinalIgnoreCase))
            {
                assemblyInstructions = $"""
                                        @RETURN_ADDRESS_GT_{_gtCount}
                                        D=A
                                        @R14
                                        M=D
                                        @START_GT
                                        0;JMP
                                        (RETURN_ADDRESS_GT_{_gtCount})
                                        """;
                _gtCount++;
                return assemblyInstructions;
            }

            return string.Empty;
        }

        public static string PushPop(CommandType commandType, string segment, int index)
        {
            string assemblyInstructions = string.Empty;

            if (commandType == CommandType.C_PUSH)
            {
                if (segment.Equals("constant", StringComparison.OrdinalIgnoreCase))
                {
                    assemblyInstructions = $"""
                                            @{index}
                                            D=A
                                            @SP
                                            A=M
                                            M=D
                                            @SP
                                            M=M+1
                                            """;
                    return assemblyInstructions;
                }
                else if (segment.Equals("this", StringComparison.OrdinalIgnoreCase))
                {
                    assemblyInstructions = $"""
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
                    return assemblyInstructions;
                }
                else if (segment.Equals("that", StringComparison.OrdinalIgnoreCase))
                {
                    assemblyInstructions = $"""
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
                    return assemblyInstructions;
                }
                else if (segment.Equals("local", StringComparison.OrdinalIgnoreCase))
                {
                    assemblyInstructions = $"""
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
                    return assemblyInstructions;
                }
                else if (segment.Equals("argument", StringComparison.OrdinalIgnoreCase))
                {
                    assemblyInstructions = $"""
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
                    return assemblyInstructions;
                }
                else if (segment.Equals("static", StringComparison.OrdinalIgnoreCase))
                {
                    assemblyInstructions = $"""
                                            @{index}
                                            D=A
                                            @{STATIC}
                                            A=M+D
                                            D=M
                                            @SP
                                            A=M
                                            M=D
                                            @SP
                                            M=M+1
                                            """;
                    return assemblyInstructions;
                }
                else if (segment.Equals("temp", StringComparison.OrdinalIgnoreCase))
                {
                    assemblyInstructions = $"""
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

                    return assemblyInstructions;
                }
                else if (segment.Equals("pointer", StringComparison.OrdinalIgnoreCase))
                {
                    assemblyInstructions = $"""
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
                    return assemblyInstructions;
                }
            }

            if (commandType == CommandType.C_POP)
            {
                if (segment.Equals("this", StringComparison.OrdinalIgnoreCase))
                {
                    assemblyInstructions = $"""
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
                    return assemblyInstructions;
                }
                else if (segment.Equals("that", StringComparison.OrdinalIgnoreCase))
                {
                    assemblyInstructions = $"""
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
                    return assemblyInstructions;
                }
                else if (segment.Equals("local", StringComparison.OrdinalIgnoreCase))
                {
                    assemblyInstructions = $"""
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
                    return assemblyInstructions;
                }
                else if (segment.Equals("argument", StringComparison.OrdinalIgnoreCase))
                {
                    assemblyInstructions = $"""
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
                    return assemblyInstructions;
                }
                else if (segment.Equals("static", StringComparison.OrdinalIgnoreCase))
                {
                    assemblyInstructions = $"""
                                            @{index}
                                            D=A
                                            @{STATIC}
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
                    return assemblyInstructions;
                }
                else if (segment.Equals("temp", StringComparison.OrdinalIgnoreCase))
                {
                    assemblyInstructions = $"""
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
                    return assemblyInstructions;
                }
                else if (segment.Equals("pointer", StringComparison.OrdinalIgnoreCase))
                {
                    assemblyInstructions = $"""
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
                    return assemblyInstructions;
                }
            }

            return string.Empty;
        }
    }
}
