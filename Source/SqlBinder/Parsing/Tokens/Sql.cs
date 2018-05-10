using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SqlBinder.Parsing.Tokens
{
	public class Sql : TextToken
	{
		public string EscapableSymbols { get; }

		internal Sql(Token parent) : base(parent) { }

		internal Sql(Token parent, string escapableSymbols) : base(parent)
		{
			EscapableSymbols = escapableSymbols;
		}

		internal static bool Evaluate(Reader reader) => reader.Token is NestedToken;

		internal override void Append(char c)
		{
			if (EscapableSymbols != null && EscapableSymbols.Contains(c))
			{
				if (Buffer.Length > 0 && Buffer[Buffer.Length - 1] == c)
					return;
			}
			base.Append(c);
		}
	}
}
