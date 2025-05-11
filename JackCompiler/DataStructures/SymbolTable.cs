using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using JackCompiler.Enums;

namespace JackCompiler.DataStructures
{
    public class SymbolTable
    {
        private readonly Dictionary<string, SymbolTableRow> _symbolTableRows;
        private readonly int[] _kindsCount;

        public SymbolTable()
        {
            _symbolTableRows = [];
            _kindsCount = new int[Enum.GetNames(typeof(Kind)).Length];
        }

        public void Reset()
        {
            _symbolTableRows.Clear();
            Array.Clear(_kindsCount);
        }

        public void Define(string name, string type, Kind kind)
        {
            SymbolTableRow symbolTableRow = new SymbolTableRow
            {
                Name = name,
                Type = type,
                Kind = kind,
                Index = _kindsCount[(int)kind]
            };

            _kindsCount[(int)kind]++;
            _symbolTableRows.Add(name, symbolTableRow);
        }

        public int VarCount(Kind kind)
        {
            return _symbolTableRows.Count(pair => pair.Value.Kind == kind);
        }

        public Kind KindOf(string name)
        {
            return _symbolTableRows.TryGetValue(name, out var symbolTableRow) ? symbolTableRow.Kind : Kind.NONE;
        }

        public string TypeOf(string name)
        {
            return _symbolTableRows.TryGetValue(name, out var symbolTableRow) ? symbolTableRow.Type : string.Empty;
        }

        public int IndexOf(string name)
        {
            return _symbolTableRows.TryGetValue(name, out var symbolTableRow) ? symbolTableRow.Index : -1;
        }

        public bool IsDefinedSymbol(string name)
        {
            return _symbolTableRows.ContainsKey(name);
        }
    }
}
