using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SqlBinder.Parsing.Tokens
{
	/// <summary>
	/// Any white-space text between SqlBinder scopes is considered a Separator. Note that comments are not considered white-space.
	/// </summary>
	public class ScopeSeparator : TextToken
	{
		internal ScopeSeparator(Token parent) : base(parent) { }

		internal static bool Evaluate(Reader reader)
		{
			if (!(reader.Token is NestedToken nestedParent))
				return false;
			if (!char.IsWhiteSpace(reader.Char))
				return false;
			if (!(nestedParent.Children.LastOrDefault() is Scope))
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
