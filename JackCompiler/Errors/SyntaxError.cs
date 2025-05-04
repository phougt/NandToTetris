using FluentResults;

namespace JackCompiler.Errors
{
    public class SyntaxError : IError
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
