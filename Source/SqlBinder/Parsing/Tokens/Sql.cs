using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SqlBinder.Parsing.Tokens
{
	/// <summary>
	/// Sql is any text that was not translated into any other token. Sql can also support escape characters 
	/// which can be replaced with their associated character preventing the other token from being recognized,
	/// e.g. [[parameter]] may be converted into '[parameter]' sql due to '[[' and ']]'.
	/// </summary>
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
