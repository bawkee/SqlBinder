using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SqlBinder.Parsing.Tokens
{
	public class Scope : NestedToken
	{
		public const string OPENING_TAG = "{";
		public const string CLOSING_TAG = "}";
		public const string VALID_FLAGS = "@";

		public override string OpeningTag { get; }
		public override string ClosingTag => CLOSING_TAG;

		public HashSet<char> Flags { get; } = new HashSet<char>();

		internal Scope(Token parent, Reader reader) : base(parent)
		{
			var c = reader.Peek();
			if (VALID_FLAGS.Contains(c))
			{
				OpeningTag = new string(new[] { c, OPENING_TAG[0] });
				Flags.Add(c);
			}
			else
				OpeningTag = OPENING_TAG;
		}

		internal static bool Evaluate(Reader reader) =>
			VALID_FLAGS.Contains(reader.Peek()) && reader.Peek(1) == OPENING_TAG[0] 
			|| Evaluate(reader, OPENING_TAG[0]);
	}
}
