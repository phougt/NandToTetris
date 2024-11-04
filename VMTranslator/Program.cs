using VMTranslator.Modules;
using VMTranslator.Enums;
using System.Text;
using System.Runtime.InteropServices;
using System.ComponentModel.Design;

namespace VMTranslator
{
    public class Program
    {
        private static bool s_canGenerateASM = true;
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
                HashSet<string> functionNames = new HashSet<string>();
                HashSet<string> labelNames = new HashSet<string>();

                s_ASMOutput.AppendLine(coder.Init());

                foreach (string file in vmFiles)
                {
                    string[] tempFunctionNames = GetFunctionNames(file);
                    string[] tempLabelNames = GetLabelNames(file);

                    foreach (string function in tempFunctionNames)
                    {
                        functionNames.Add(function);
                    }

                    foreach (string label in tempLabelNames)
                    {
                        labelNames.Add(label);
                    }
                }

                if (functionNames.Count == 0 || labelNames.Count == 0)
                    Environment.Exit(1);

                foreach (string file in vmFiles)
                {
                    coder.Filename = Path.GetFileNameWithoutExtension(file);
                    string temp = Translate(file, coder, functionNames, labelNames);

                    if (temp != string.Empty)
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

                HashSet<string> functionNames = GetFunctionNames(firstArg).ToHashSet();
                HashSet<string> labelNames = GetLabelNames(firstArg).ToHashSet();

                if (functionNames.Count == 0 || labelNames.Count == 0)
                    Environment.Exit(1);

                string temp = Translate(firstArg, coder, functionNames, labelNames);

                s_ASMOutput.AppendLine(coder.Init());

                if (temp != string.Empty)
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

        public static string[] GetLabelNames(string sourcePath)
        {
            Parser? parser = null;
            List<string> labelNames = new List<string>();

            string currentFunctionName = string.Empty;
            int lineNumber = 0;
            bool canGenerateASM = true;

            try
            {
                parser = new Parser(sourcePath);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine(ex.Message);
                Environment.Exit(1);
            }

            while (parser.HasMoreLines)
            {
                parser.Advance();

                if (!parser.IsValidCommand)
                {
                    canGenerateASM = false;
                }

                lineNumber++;

                CommandType currentCommandType = parser.Type;

                if (currentCommandType == CommandType.NONE)
                    continue;

                string arg1 = parser.Arg1;
                int arg2 = parser.Arg2;

                if (currentCommandType == CommandType.C_FUNCTION)
                {
                    currentFunctionName = arg1;
                }
                else if (currentCommandType == CommandType.C_LABEL)
                {
                    if (!labelNames.Contains($"{currentFunctionName}${arg1}"))
                    {
                        labelNames.Add($"{currentFunctionName}${arg1}");
                    }
                    else
                    {
                        Console.Error.WriteLine($"Label - {arg1} is already defined once. Line: {lineNumber}");
                        canGenerateASM = false;
                    }
                }
            }

            parser?.Dispose();

            return canGenerateASM ? labelNames.ToArray() : Array.Empty<string>();
        }

        public static string[] GetFunctionNames(string sourcePath)
        {
            Parser? parser = null;
            List<string> functionNames = new List<string>();

            string currentFunctionName = string.Empty;
            int lineNumber = 0;
            bool canGenerateASM = true;

            try
            {
                parser = new Parser(sourcePath);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine(ex.Message);
                Environment.Exit(1);
            }

            while (parser.HasMoreLines)
            {
                parser.Advance();

                if (!parser.IsValidCommand)
                {
                    canGenerateASM = false;
                }

                lineNumber++;

                CommandType currentCommandType = parser.Type;

                if (currentCommandType == CommandType.NONE)
                    continue;

                string arg1 = parser.Arg1;
                int arg2 = parser.Arg2;

                if (currentCommandType == CommandType.C_FUNCTION)
                {
                    if (!functionNames.Contains(arg1))
                    {
                        functionNames.Add(arg1);
                    }
                    else
                    {
                        Console.Error.WriteLine($"Function - {arg1} is already defined once. Line: {lineNumber}");
                        canGenerateASM = false;
                    }
                }
            }

            parser?.Dispose();

            return canGenerateASM ? functionNames.ToArray() : Array.Empty<string>();
        }

        public static string Translate(string sourcePath, Coder coder, HashSet<string> functionNames, HashSet<string> labelNames)
        {
            Parser? parser = null;
            StringBuilder tempASMoutput = new StringBuilder();

            string currentFunctionName = string.Empty;
            int lineNumber = 0;
            bool canGenerateASM = true;

            try
            {
                parser = new Parser(sourcePath);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine(ex.Message);
                Environment.Exit(1);
            }

            while (parser.HasMoreLines)
            {
                parser.Advance();

                if (!parser.IsValidCommand)
                {
                    canGenerateASM = false;
                }

                lineNumber++;

                CommandType currentCommandType = parser.Type;

                if (currentCommandType == CommandType.NONE)
                    continue;

                string arg1 = parser.Arg1;
                int arg2 = parser.Arg2;

                switch (currentCommandType)
                {
                    case CommandType.C_PUSH:
                        {
                            tempASMoutput.AppendLine(coder.PushPop(CommandType.C_PUSH, arg1, arg2));
                            break;
                        }
                    case CommandType.C_POP:
                        {
                            tempASMoutput.AppendLine(coder.PushPop(CommandType.C_POP, arg1, arg2));
                            break;
                        }
                    case CommandType.C_ARITHMETIC:
                        {
                            tempASMoutput.AppendLine(coder.Arithmethic(arg1));
                            break;
                        }
                    case CommandType.C_LABEL:
                        {
                            tempASMoutput.AppendLine(coder.Label($"{currentFunctionName}${arg1}"));
                            break;
                        }
                    case CommandType.C_GOTO:
                        {
                            if (!labelNames.Contains($"{currentFunctionName}${arg1}"))
                            {
                                Console.Error.WriteLine($"Unknown Label - {arg1}. Line: {lineNumber}");
                                canGenerateASM = false;
                                break;
                            }

                            tempASMoutput.AppendLine(coder.Goto($"{currentFunctionName}${arg1}"));
                            break;
                        }
                    case CommandType.C_IF:
                        {
                            if (!labelNames.Contains($"{currentFunctionName}${arg1}"))
                            {
                                Console.Error.WriteLine($"Unknown Label - {arg1}. Line: {lineNumber}");
                                canGenerateASM = false;
                                break;
                            }

                            tempASMoutput.AppendLine(coder.If($"{currentFunctionName}${arg1}"));
                            break;
                        }
                    case CommandType.C_CALL:
                        {
                            if (!functionNames.Contains(arg1))
                            {
                                Console.Error.WriteLine($"Unknown Function - {arg1}. Line: {lineNumber}");
                                canGenerateASM = false;
                                break;
                            }

                            tempASMoutput.AppendLine(coder.Call(arg1, arg2));
                            break;
                        }
                    case CommandType.C_RETURN:
                        {
                            tempASMoutput.AppendLine(coder.Return());
                            break;
                        }
                    case CommandType.C_FUNCTION:
                        {
                            currentFunctionName = arg1;
                            tempASMoutput.AppendLine(coder.Function(arg1, arg2));
                            break;
                        }
                };
            }

            parser?.Dispose();

            return canGenerateASM ? tempASMoutput.ToString() : string.Empty;
        }
    }
}
