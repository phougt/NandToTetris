using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FluentResults;

namespace JackCompiler.Errors
{
    internal class EndOfStreamError : IError
    {
        public List<IError> Reasons => throw new NotImplementedException();

        private string _message = string.Empty;
        public string Message
        {
            get => _message;
            set => _message = value;
        }

        public Dictionary<string, object> Metadata => throw new NotImplementedException();
    }
}
