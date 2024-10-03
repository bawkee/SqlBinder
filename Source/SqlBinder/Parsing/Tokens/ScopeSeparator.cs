using System.Linq;

namespace SqlBinder.Parsing.Tokens
{
    /// <summary>
    /// Any white-space text between SqlBinder scopes is considered a Separator. Note that comments are not considered white-space.
    /// </summary>
    public class ScopeSeparator : TextToken
    {
        // This is a dubious token. Should be removed as parser or template processor should be in charge of determining what can 
        // stand between two scopes.
        internal ScopeSeparator(Token parent) : base(parent)
        {
        }

        internal static bool Evaluate(Reader reader)
        {
            if (reader.Token is not NestedToken nestedParent)
                return false;
            if (!char.IsWhiteSpace(reader.Char))
                return false;
            if (nestedParent.Children.LastOrDefault() is not Scope)
                return false;

            reader.StartSnapshot();
            try
            {
                while (reader.TryConsume())
                {
                    if (char.IsWhiteSpace(reader.Char))
                        continue;
                    return Scope.Evaluate(reader);
                }
            }
            finally
            {
                reader.FinishSnapshot();
            }

            return false;
        }

        public override string ToString() => $"{GetType().Name}";
    }
}