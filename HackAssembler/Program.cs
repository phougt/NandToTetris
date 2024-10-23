using HackAssembler.Enums;
using HackAssembler.Modules;

namespace HackAssembler
{
    public class Program
    {
        private static SymbolTable s_symbolTable;
        private static Parser s_parser;
        private static int s_programCounter = 0;
        private static int s_availableRamAddress = 16;
        private static List<string> s_binaryOutput;
        private static bool s_canGenerateBinary = true;

        private static void InitializeData()
        {
            Dictionary<string, int> builtInKeyword = new Dictionary<string, int>()
            {
                {"SP", 0},
                {"LCL", 1},
                {"ARG", 2},
                {"THIS", 3},
                {"THAT", 4},
                {"R0", 0},
                {"R1", 1},
                {"R2", 2},
                {"R3", 3},
                {"R4", 4},
                {"R5", 5},
                {"R6", 6},
                {"R7", 7},
                {"R8", 8},
                {"R9", 9},
                {"R10", 10},
                {"R11", 11},
                {"R12", 12},
                {"R13", 13},
                {"R14", 14},
                {"R15", 15},
                {"SCREEN", 16384},
                {"KBD", 24576},
            };

            s_symbolTable = new SymbolTable(builtInKeyword);
            s_binaryOutput = new List<string>();
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

            if (!(Path.GetExtension(sourcePath) == ".asm"))
            {
                Console.Error.WriteLine("Souce file must end with .asm extension");
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

            s_parser.LabelPass = true;

            while (s_parser.HasMoreLines)
            {
                s_parser.Advance();

                if (!s_parser.IsValidCommand)
                {
                    s_canGenerateBinary = false;
                }

                CommandType currenCommandType = s_parser.Type;

                if (currenCommandType == CommandType.L_COMMAND && s_canGenerateBinary)
                {
                    string symbol = s_parser.Symbol;

                    if (!s_symbolTable.Contains(symbol))
                    {
                        s_symbolTable.AddEntry(symbol, s_programCounter);
                    }
                    else
                    {
                        Console.Error.WriteLine($"'{symbol}' Label can only be declared once. Line: {s_symbolTable.GetAddress(symbol)} and {s_programCounter}");
                        s_canGenerateBinary = false;
                    }
                }
                else if (currenCommandType == CommandType.A_COMMAND || currenCommandType == CommandType.C_COMMAND)
                {
                    s_programCounter++;
                }
            }

            s_parser.Dispose();
            s_programCounter = 0;
            s_parser = new Parser(sourcePath);
            s_parser.LabelPass = false;

            while (s_parser.HasMoreLines)
            {
                s_parser.Advance();

                if (!s_parser.IsValidCommand)
                {
                    s_canGenerateBinary = false;
                    continue;
                }

                CommandType currentCommandType = s_parser.Type;

                if (currentCommandType == CommandType.NONE || currentCommandType == CommandType.L_COMMAND)
                    continue;

                if (currentCommandType == CommandType.A_COMMAND)
                {
                    string symbol = s_parser.Symbol;
                    bool isNumber = symbol.All((c) => char.IsNumber(c));

                    if (!isNumber)
                    {
                        if (s_symbolTable.Contains(symbol))
                        {
                            int address = s_symbolTable.GetAddress(symbol);
                            s_binaryOutput.Add(Code.Address(address));
                        }
                        else
                        {
                            s_symbolTable.AddEntry(symbol, s_availableRamAddress);
                            s_binaryOutput.Add(Code.Address(s_availableRamAddress));
                            s_availableRamAddress++;
                        }
                    }
                    else
                    {
                        int address = Convert.ToInt32(symbol);
                        s_binaryOutput.Add(Code.Address(address));
                    }
                }

                if (currentCommandType == CommandType.C_COMMAND)
                {
                    string dest = Code.Dest(s_parser.Dest);
                    string comp = Code.Comp(s_parser.Comp);
                    string jump = Code.Jump(s_parser.Jump);

                    string[] parts = ["111", comp, dest, jump];
                    string final = string.Concat(parts);
                    s_binaryOutput.Add(final);
                }
            }

            if (!s_canGenerateBinary)
            {
                Environment.Exit(1);
            }
            else
            {
                // Remove the extension from the outputPath
                outputPath = Path.ChangeExtension(outputPath, null);

                // Add a new extension .hack
                outputPath = Path.ChangeExtension(outputPath, ".hack");

                using (StreamWriter streamWriter = new StreamWriter(outputPath))
                {
                    foreach (string output in s_binaryOutput)
                    {
                        streamWriter.WriteLine(output);
                    }
                }
            }
        }
    }
}
