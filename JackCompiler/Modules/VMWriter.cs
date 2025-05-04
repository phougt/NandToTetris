using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using JackCompiler.Enums;

namespace JackCompiler.Modules
{
    public class VMWriter : IDisposable
    {
        public string FilePath { get; set; } = string.Empty;
        private readonly StringBuilder _vmOutput;
        private StreamWriter _streamWriter;

        public VMWriter()
        {
            _vmOutput = new StringBuilder();
        }

        public VMWriter(string filePath)
        {
            FilePath = filePath;
            _vmOutput = new StringBuilder();
        }

        public bool TryWriteToFile()
        {
            _streamWriter = new StreamWriter(FilePath, false);

            try
            {
                _streamWriter.Write(_vmOutput.ToString());
                return true;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Error writing to file: {ex.Message}");
                return false;
            }
            finally
            {
                _streamWriter?.Close();
            }
        }

        public void Dispose()
        {
            _streamWriter?.Dispose();
        }

        public void WritePush(Segment segment, int index)
        {
            _vmOutput.AppendLine($"push {segment} {index}");
        }

        public void WritePop(Segment segment, int index)
        {
            _vmOutput.AppendLine($"pop {segment} {index}");
        }

        public void WriteArithmetic(Command command)
        {
            _vmOutput.AppendLine(command.ToString().ToLower());
        }

        public void WriteLabel(string label)
        {
            _vmOutput.AppendLine($"label {label}");
        }

        public void WriteGoto(string label)
        {
            _vmOutput.AppendLine($"goto {label}");
        }

        public void WriteIf(string label)
        {
            _vmOutput.AppendLine($"if-goto {label}");
        }

        public void WriteCall(string name, int nArgs)
        {
            _vmOutput.AppendLine($"call {name} {nArgs}");
        }

        public void WriteFunction(string name, int nLocals)
        {
            _vmOutput.AppendLine($"function {name} {nLocals}");
        }

        public void WriteReturn()
        {
            _vmOutput.AppendLine("return");
        }
    }
}
