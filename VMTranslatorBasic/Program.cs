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

            //Jump to the beginning of the actual program when started
            s_ASMOutput.AppendLine("""
                                   @START_PROGRAM
                                   0;JMP
                                   """);

            //Handling "eq" VM instruction
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

            //Handling "lt" VM instruction
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

            //Handling "gt" VM instruction
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

            //Mark the start of the actual program
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

                Coder coder = new Coder();

                foreach (string file in vmFiles)
                {
                    coder.Filename = Path.GetFileNameWithoutExtension(file);
                    StringBuilder? temp = Translate(file, coder);

                    if (temp != null)
                        s_ASMOutput.Append(temp);
                    else
                        s_canGenerateASM = false;
                }
            }
            else
            {
                if (!Path.GetExtension(firstArg).Equals(".vm"))
                {
                    Console.Error.WriteLine("SourcePath is not a .vm file.");
                    Environment.Exit(1);
                }

                Coder coder = new Coder();
                coder.Filename = Path.GetFileNameWithoutExtension(firstArg);
                StringBuilder? temp = Translate(firstArg, coder);

                if (temp != null)
                    s_ASMOutput.Append(temp);
                else
                    s_canGenerateASM = false;
            }

            if (!s_canGenerateASM)
            {
                Environment.Exit(1);
            }
            else
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
        }

        public static StringBuilder? Translate(string sourcePath, Coder coder)
        {
            StringBuilder tempASMoutput = new StringBuilder();
            bool canGenerateASM = true;

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
                    canGenerateASM = false;
                }

                CommandType currentCommandType = s_parser.Type;

                if (currentCommandType == CommandType.NONE)
                    continue;

                string segment = s_parser.Arg1;
                int index = s_parser.Arg2;

                if (currentCommandType == CommandType.C_PUSH)
                {
                    tempASMoutput.AppendLine(coder.PushPop(CommandType.C_PUSH, segment, index));
                }
                else if (currentCommandType == CommandType.C_POP)
                {
                    tempASMoutput.AppendLine(coder.PushPop(CommandType.C_POP, segment, index));
                }
                else if (currentCommandType == CommandType.C_ARITHMETIC)
                {
                    tempASMoutput.AppendLine(coder.Arithmethic(segment));
                }
            }

            s_parser?.Dispose();

            return (canGenerateASM) ? tempASMoutput : null;
        }
    }
}
