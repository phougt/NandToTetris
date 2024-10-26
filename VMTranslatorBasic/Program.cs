using System;
using VMTranslatorBasic.Modules;
using VMTranslatorBasic.Enums;
using System.Text;

namespace VMTranslatorBasic
{
    public class Program
    {
        private static Parser s_parser;
        private static bool s_canGenerateASM = true;
        private static StreamWriter s_writer;
        private static StringBuilder s_ASMOutput;

        public static void InitializeData()
        {
            s_ASMOutput = new StringBuilder();

            s_ASMOutput.AppendLine("""
                                   @START_PROGRAM
                                   0;JMP
                                   """);

            s_ASMOutput.AppendLine("""
                                   (START_EQ)
                                   @SP
                                   AM=M-1
                                   D=M
                                   A=A-1
                                   D=M-D
                                   M=-1
                                   @END_EQ
                                   D;JEQ
                                   @SP
                                   A=M-1
                                   M=0
                                   (END_EQ)
                                   @R14
                                   A=M
                                   0;JMP
                                   """);

            s_ASMOutput.AppendLine("""
                                   (START_LT)
                                   @SP
                                   AM=M-1
                                   D=M
                                   A=A-1
                                   D=M-D
                                   M=-1
                                   @END_LT
                                   D;JLT
                                   @SP
                                   A=M-1
                                   M=0
                                   (END_LT)
                                   @R14
                                   A=M
                                   0;JMP
                                   """);

            s_ASMOutput.AppendLine("""
                                   (START_GT)
                                   @SP
                                   AM=M-1
                                   D=M
                                   A=A-1
                                   D=M-D
                                   M=-1
                                   @END_GT
                                   D;JGT
                                   @SP
                                   A=M-1
                                   M=0
                                   (END_GT)
                                   @R14
                                   A=M
                                   0;JMP
                                   """);

            s_ASMOutput.AppendLine("""
                                   (START_PROGRAM)
                                   """);
        }

        public static void Main(string[] args)
        {
            InitializeData();

            string sourcePath = string.Empty;
            string outputPath = string.Empty;

            if (args.Length == 2)
            {
                sourcePath = args[0];
                outputPath = args[1];
            }
            else
            {
                Console.Error.WriteLine("Expected 2 arguments (SourcePath, OutputPath).");
                Environment.Exit(1);
            }

            if (!(Path.GetExtension(sourcePath) == ".vm"))
            {
                Console.Error.WriteLine("Souce file must end with .vm extension");
                Environment.Exit(1);
            }

            try
            {
                s_parser = new Parser(sourcePath);
            }
            catch (FileNotFoundException)
            {
                Console.Error.WriteLine("Source File is not found");
            }

            while (s_parser.HasMoreLines)
            {
                s_parser.Advance();

                if (!s_parser.IsValidCommand)
                {
                    s_canGenerateASM = false;
                }

                CommandType currentCommandType = s_parser.Type;

                if (currentCommandType == CommandType.NONE)
                    continue;

                string segment = s_parser.Arg1;
                int index = s_parser.Arg2;

                if (currentCommandType == CommandType.C_PUSH)
                {
                    if (Code.PushPop(CommandType.C_PUSH, segment, index) == string.Empty)
                    {
                        Console.WriteLine($"segment C_POP: {segment}, {index} ");
                    }

                    s_ASMOutput.AppendLine(Code.PushPop(CommandType.C_PUSH, segment, index));
                }
                else if (currentCommandType == CommandType.C_POP)
                {
                    if (Code.PushPop(CommandType.C_POP, segment, index) == string.Empty)
                    {
                        Console.WriteLine($"segment C_POP: {segment}, {index} ");
                    }

                    s_ASMOutput.AppendLine(Code.PushPop(CommandType.C_POP, segment, index));
                }
                else if (currentCommandType == CommandType.C_ARITHMETIC)
                {
                    if (Code.Arithmethic(segment) == string.Empty)
                    {
                        Console.WriteLine($"segment arithmetic: {segment} ");
                    }

                    s_ASMOutput.AppendLine(Code.Arithmethic(segment));
                }
            }

            if (s_canGenerateASM)
            {
                // Remove the extension from the outputPath
                outputPath = Path.ChangeExtension(outputPath, null);

                // Add a new extension .asm
                outputPath = Path.ChangeExtension(outputPath, ".asm");

                using (StreamWriter writer = new StreamWriter(outputPath))
                {
                    writer.Write(s_ASMOutput);
                }
            }
            else
            {
                Environment.Exit(1);
            }
        }
    }
}
