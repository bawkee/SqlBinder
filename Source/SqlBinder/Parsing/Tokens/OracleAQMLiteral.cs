using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SqlBinder.Parsing.Tokens
{
	/// <summary>
	/// Oracle Alternative Quoting Mechanism (AQM) literal, i.e. q'" ... "' or q'{test}'. For more information check out 
	/// this guide: 
	/// https://livesql.oracle.com/apex/livesql/file/content_CIREYU9EA54EOKQ7LAMZKRF6P.html
	/// Or this reference:
	/// https://docs.oracle.com/cd/B19306_01/server.102/b14200/sql_elements003.htm
	/// 
	/// I wasn't aware of the existence of this until I started reading up on all the types of literals I might encounter on my
	/// SQL parsing endavour. I was unpleasantly surprised by this tag because if it weren't for it, this parser would be a lot
	/// simpler and context-free. I later found that this mechanism is so unknown that not even Oracle is respecting it everywhere,
	/// their own drivers (both managed and native) can easily be broken by it. Even their syntax highlighters get messy if you 
	/// use it (try Oracle LiveSQL). Microsoft's driver works with it though as it doesn't attempt its own parsing. Native driver works
	/// in a lot of cases but they do attempt parsing when you use bind variables (probably LL) and can be broken in certain cases.
	/// Either way, once these bugs get fixed by Oracle, SqlBinder will be ready as it is.
	/// </summary>
	public class OracleAQMLiteral : ContentToken 
	{
		public const string OPENING_TAG = "q'";
		public const string CLOSING_TAG = "'";
		public const string STANDARD_PAIRS = "[]{}()<>";
		public const string ILLEGAL_CHARACTERS = "\r\n\t '";		

		public override string OpeningTag { get; }
		public override string ClosingTag { get; }

		internal OracleAQMLiteral(Token parent, Reader reader) 
			: base(parent)
		{
			var alternativeTag = reader.Peek(2);

			OpeningTag = OPENING_TAG + alternativeTag;

			var standardPair = STANDARD_PAIRS.IndexOf(alternativeTag);
			if (standardPair > -1)
				ClosingTag = STANDARD_PAIRS[standardPair + 1] + CLOSING_TAG;
			else
				ClosingTag = alternativeTag + CLOSING_TAG;
		}

		internal OracleAQMLiteral(Token parent) : base(parent) { }

		internal static bool Evaluate(Reader reader)
		{
			if (!Evaluate(reader, OPENING_TAG))
				return false;
			if (ILLEGAL_CHARACTERS.Contains(reader.Peek(2)))
				return false;
			var standardPair = STANDARD_PAIRS.IndexOf(reader.Peek(2));
			return standardPair < 0 || standardPair % 2 == 0;
		}

		internal override bool EvaluateClosingTag(Reader reader) 
			=> Content != null && base.EvaluateClosingTag(reader);
	}
}
