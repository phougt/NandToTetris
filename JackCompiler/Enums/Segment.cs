using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JackCompiler.Enums
{
    public enum Segment
    {
        CONST,
        ARG,
        LOCAL,
        STATIC,
        THIS,
        THAT,
        POINTER,
        TEMP
    }
}
