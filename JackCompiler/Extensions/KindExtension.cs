using JackCompiler.Enums;
namespace JackCompiler.Extensions
{
    public static class KindExtension
    {
        public static Segment ToSegment(this Kind kind)
        {
            return kind switch
            {
                Kind.STATIC => Segment.STATIC,
                Kind.VAR => Segment.LOCAL,
                Kind.ARGUMENT => Segment.ARGUMENT,
                Kind.FIELD => Segment.THIS,
                _ => Segment.NONE
            };
        }
    }
}