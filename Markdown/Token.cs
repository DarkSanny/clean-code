namespace Markdown
{
    public class Token {

        public Token(string value, int startIndex, int length) {
            StartIndex = startIndex;
            Length = length;
            Value = value;
        }

        public readonly int StartIndex;
        public readonly int Length;
        public readonly string Value;

        public int GetIndexNextToToken() {
            return StartIndex + Length;
        }

    }
}