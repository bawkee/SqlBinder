using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SqlBinder.Parsing.Tokens
{
	public class PostgreDoubleDollarLiteral : ContentToken // Can conflict with Informix EXEC SQL keyword
	{
		public const char SYMBOL = '$';
		public const int MAX_TAG = 256;

		public override string OpeningTag { get; }
		public override string ClosingTag { get; }

		public const string WHITE_SPACE = "\r\n\t \0";

		internal PostgreDoubleDollarLiteral(Token parent, Reader reader) 
			: base(parent)
		{
			OpeningTag = ClosingTag = DetermineTag(reader);
		}

		internal static bool Evaluate(Reader reader)
		{
			if (reader.Char != SYMBOL)
				return false;
			if (!WHITE_SPACE.Contains(reader.Peek(-1)))
				return false;
			return DetermineTag(reader) != null;
		}

		private static unsafe string DetermineTag(Reader reader)
		{
			char* buf = stackalloc char[MAX_TAG];
			var done = false;

			for (var i = 0; i < MAX_TAG - 1; i++)
			{
				var c = reader.Peek(i);
				if (i > 0)
				{
					if (c == SYMBOL)
						done = true;
					else if (!char.IsLetter(c))
						break; // Invalid character
				}
				buf[i] = c;
				if (done)
				{
					buf[i + 1] = '\0';
					return new string(buf);
				}
			}
			return null;
		}
	}
}
