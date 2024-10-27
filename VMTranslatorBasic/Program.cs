using System;
using VMTranslatorBasic.Modules;
using VMTranslatorBasic.Enums;
using System.Text;
using System.Reflection.Metadata.Ecma335;
using System.Net.Http.Headers;

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

            string firstArg = string.Empty;
            string secondArg = string.Empty;

            bool isMultiFilesTranslating = false;

            if (args.Length == 1)
            {
                isMultiFilesTranslating = args[0].Equals("-dir");
                firstArg = args[0];
            }
            else if (args.Length == 2)
            {
                firstArg = args[0];
                secondArg = args[1];
            }
            else
            {
                Console.Error.WriteLine("Arguments Format is expected to be (SourcePath, OutputPath) or (-dir).");
                Environment.Exit(1);
            }

            if (isMultiFilesTranslating)
            {
                string[] files = Directory.GetFiles(Environment.CurrentDirectory);

                if (files.Length == 0)
                {
                    Console.Error.WriteLine("Empty Directory.");
                    Environment.Exit(1);
                }

                string[] vmFiles = files.Where(filename => Path.GetExtension(filename).Equals(".vm")).ToArray();

                if (vmFiles.Length == 0)
                {
                    Console.Error.WriteLine("No .vm files are found in the specified directory.");
                    Environment.Exit(1);
                }

                foreach (string file in vmFiles)
                {
                    Translate(file);
                }
            }
            else
            {
                if (!Path.GetExtension(firstArg).Equals(".vm"))
                {
                    Console.Error.WriteLine("SourcePath is not a .vm file.");
                    Environment.Exit(1);
                }

                Translate(firstArg);
            }

            if (s_canGenerateASM)
            {
                if (isMultiFilesTranslating)
                {
                    string currentDirectory = Environment.CurrentDirectory;
                    string currentDirectoryName = new DirectoryInfo(currentDirectory).Name;

                    secondArg = $"{currentDirectory}{Path.DirectorySeparatorChar}{currentDirectoryName}.asm";
                }

                // Remove the extension from the outputPath
                secondArg = Path.ChangeExtension(secondArg, null);

                // Add a new extension .asm
                secondArg = Path.ChangeExtension(secondArg, ".asm");

                using (StreamWriter writer = new StreamWriter(secondArg))
                {
                    writer.Write(s_ASMOutput);
                }
            }
            else
            {
                Environment.Exit(1);
            }
        }

        public static void Translate(string sourcePath)
        {
            try
            {
                s_parser = new Parser(sourcePath);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine(ex.Message);
                Environment.Exit(1);
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
                    s_ASMOutput.AppendLine(Code.PushPop(CommandType.C_PUSH, segment, index));
                }
                else if (currentCommandType == CommandType.C_POP)
                {
                    s_ASMOutput.AppendLine(Code.PushPop(CommandType.C_POP, segment, index));
                }
                else if (currentCommandType == CommandType.C_ARITHMETIC)
                {
                    s_ASMOutput.AppendLine(Code.Arithmethic(segment));
                }
            }
        }
    }
}
