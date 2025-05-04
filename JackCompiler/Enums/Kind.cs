using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JackCompiler.Enums
{
    public enum Kind
    {
        NONE = -1,
        STATIC = 0,
        FIELD = 1,
        ARGUMENT = 2,
        VAR = 3
    }
}
