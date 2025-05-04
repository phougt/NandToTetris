using JackCompiler.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JackCompiler.DataStructures
{
    public class SymbolTableRow
    {
        public string Name = string.Empty;
        public string Type = string.Empty;
        public Kind Kind = Kind.NONE;
        public int Index = 0;
    }
}
