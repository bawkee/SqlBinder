using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SqlBinder.Properties;

namespace SqlBinder.Parsing2
{
	[Serializable]
	public class LexerException : Exception
	{
		public LexerException(Exception innerException) : base(Exceptions.ParserFailure, innerException) { }
		public LexerException(string errorMessage) : base(string.Format(Exceptions.ScriptNotValid, errorMessage)) { }
	}

	[Flags]
	public enum LexerHints
	{
		None = 0,
		/// <summary>
		/// Improves performance slightly if you never plan on using the parser for PostgreSQL.
		/// </summary>
		DisablePostgreSqlFlavors = 1,
		/// <summary>
		/// Improves performance slightly if you never plan on using the parser for Oracle.
		/// </summary>
		DisableOracleFlavors = 2,
		/// <summary>
		/// If you enable this hint, Lexer will use standard bind variable syntax (:param, @param or ?param) to recognize SqlBinder parameters 
		/// (which is NOT the same thing as bind variables). This also lets you use square brackets [param] syntax in your SQL without having 
		/// to escape them (i.e. [[My Table]].[[My Column]]).
		/// </summary>
		UseBindVarSyntaxForParams = 4,
	}

	public class Lexer
	{
		public LexerHints Hints { get; set; }

		public Root Process(string script)
		{
			var reader = new Reader(script);
			var root = reader.Element;
			var openScopes = 0;

			while (reader.Read())
			{
				var scopedElement = reader.Element as ScopedElement ?? reader.Element.Parent as ScopedElement;
				var nestedElement = reader.Element as NestedElement ?? reader.Element.Parent as NestedElement;
				var contentElement = reader.Element as ContentElement ?? reader.Element.Parent as ContentElement;

				if (TryToCloseScope(reader, scopedElement))
				{
					openScopes--;
					continue;
				}

				if (TryToCreateScope(reader, nestedElement, contentElement))
				{
					openScopes++;
					continue;
				}

				if (reader.Element is TextElement element) // Keep writing on text elem
					element.Append(reader.Char);
				else
					CreateTextElement(reader, contentElement, nestedElement);
			}

			if (openScopes > 0)
				throw new LexerException(Exceptions.UnclosedScope);

			return root as Root;
		}

		private bool TryToCloseScope(Reader reader, ScopedElement scopedElement)
		{
			if (scopedElement == null || !scopedElement.EvaluateClosingTag(reader))
				return false;
			reader.Advance(scopedElement.ClosingTag.Length - 1);
			reader.Element = scopedElement.Parent;
			return true;
		}

		private bool TryToCreateScope(Reader reader, NestedElement nestedElement, ContentElement contentElement)
		{
			if (nestedElement == null || contentElement != null)
				return false;

			ScopedElement newElem;
			
			if (SqlBinderComment.Evaluate(reader)) newElem = new SqlBinderComment(nestedElement);
			else if (SqlComment.Evaluate(reader)) newElem = new SqlComment(nestedElement);
			else if (SingleQuoteLiteral.Evaluate(reader)) newElem = new SingleQuoteLiteral(nestedElement);
			else if (DoubleQuoteLiteral.Evaluate(reader)) newElem = new DoubleQuoteLiteral(nestedElement);
			else if (!Hints.HasFlag(LexerHints.DisableOracleFlavors) && OracleAQMLiteral.Evaluate(reader))
				newElem = new OracleAQMLiteral(nestedElement, reader);
			else if (!Hints.HasFlag(LexerHints.DisablePostgreSqlFlavors) && PostgreDoubleDollarLiteral.Evaluate(reader))
				newElem = new PostgreDoubleDollarLiteral(nestedElement, reader);
			else if (Scope.Evaluate(reader)) newElem = new Scope(nestedElement);
			else if (Hints.HasFlag(LexerHints.UseBindVarSyntaxForParams) ? BindVariableParameter.Evaluate(reader) : SqlBinderParameter.Evaluate(reader))
				newElem = Hints.HasFlag(LexerHints.UseBindVarSyntaxForParams) ? new BindVariableParameter(nestedElement, reader) : new SqlBinderParameter(nestedElement) as Parameter;
			else return false;

			nestedElement.Children.Add(newElem);
			reader.Advance(newElem.OpeningTag.Length - 1);
			reader.Element = newElem;

			return true;
		}

		private void CreateTextElement(Reader reader, ContentElement contentElement, NestedElement nestedElement)
		{
			TextElement newElem;

			if (ContentText.Evaluate(reader)) newElem = new ContentText(reader.Element);
			else if (ScopeSeparator.Evaluate(reader)) newElem = new ScopeSeparator(reader.Element);
			else if (Sql.Evaluate(reader))
			{
				if (Hints.HasFlag(LexerHints.UseBindVarSyntaxForParams))
					newElem = new Sql(reader.Element);
				else
				{
					const string ESCAPABLE_SQL_SYMBOLS = "[]";
					newElem = new Sql(reader.Element, ESCAPABLE_SQL_SYMBOLS);
				}
			}
			else throw new NotSupportedException();

			if (contentElement != null)
				contentElement.Content = newElem;
			else if (nestedElement != null)
				nestedElement.Children.Add(newElem);
			else
				throw new InvalidOperationException();

			newElem.Append(reader.Char);
			reader.Element = newElem;
		}
	}

	public class Reader
	{		
		public Reader(string buffer) => Buffer = buffer;
		public string Buffer { get; set; }
		public int Index { get; set; } = -1;
		public void Advance(int n) => Index += n;
		public char Char => Buffer[Index];
		public Element Element { get; set; } = new Root();
		public bool Read() => ++Index < Buffer.Length;
		public char Peek(int n = 0) => Index + n < Buffer.Length ? Buffer[Index + n] : (char)0;

		public bool TestString(string str)
		{			
			if (string.IsNullOrEmpty(str))
				return false;
			for (var i = 0; i < str.Length; i++)
				if (Peek(i) != str[i])
					return false;
			return true;
		}
	}

	///* ----------------------- */
	/// Element types:
	/// 
	/// Element
	///		TextElement
	///		ScopedElement
	///			ContentElement
	///			NestedElement
	///* ----------------------- */

	public abstract class Element
	{
		public Element Parent { get; set; }
	}

	public abstract class TextElement : Element
	{
		public const int BUFFER_SIZE = 512;		
		protected TextElement(Element parent) => Parent = parent;
		protected StringBuilder Buffer { get; set; } = new StringBuilder(BUFFER_SIZE);		
		private string _text;
		public string Text => _text ?? (_text = Buffer.ToString());
		public virtual void Append(char c)
		{
			Buffer.Append(c);
			_text = null;
		}
		public override string ToString() => $"{GetType().Name} ({Text})";
	}

	public abstract class ScopedElement : Element
	{
		protected ScopedElement(Element parent) => Parent = parent;

		public abstract string OpeningTag { get; }
		public abstract string ClosingTag { get; }

		protected static bool Evaluate(Reader reader, string tag) => reader.TestString(tag);
		protected static bool Evaluate(Reader reader, char tag) => reader.Peek() == tag;
		public virtual bool EvaluateClosingTag(Reader reader) => Evaluate(reader, ClosingTag);
	}

	public abstract class ContentElement : ScopedElement
	{
		public Element Content { get; set; }
		protected ContentElement(Element parent) : base(parent) { }
		public override string ToString() => $"{GetType().Name} ({Content})";
	}

	public abstract class NestedElement : ScopedElement
	{		
		public List<Element> Children { get; set; } = new List<Element>();
		protected NestedElement(Element parent) : base(parent) { }
		public override string ToString() => $"{GetType().Name} ({Children.Count} children)";
	}

	/* -------------------- */

	public class Root : NestedElement
	{
		public Root() : base(null) { }
		public override string OpeningTag => null;
		public override string ClosingTag => null;
	}

	public class SqlBinderComment : ContentElement
	{
		private const string OPENING_TAG = "/*{";
		private const string CLOSING_TAG = "}*/";

		public override string OpeningTag => OPENING_TAG;
		public override string ClosingTag => CLOSING_TAG;		

		public SqlBinderComment(Element parent) : base(parent) { }
		
		public static bool Evaluate(Reader reader) => Evaluate(reader, OPENING_TAG);		
	}

	public class SqlComment : ContentElement
	{
		private const string OPENING_TAG = "/*";
		private const string CLOSING_TAG = "*/";

		public override string OpeningTag => OPENING_TAG;
		public override string ClosingTag => CLOSING_TAG;

		public SqlComment(Element parent) : base(parent) { }

		public static bool Evaluate(Reader reader) => Evaluate(reader, OPENING_TAG);
	}

	public class Sql : TextElement
	{
		public string EscapableSymbols { get; }

		public Sql(Element parent) : base(parent) { }

		public Sql(Element parent, string escapableSymbols) : base(parent)
		{
			EscapableSymbols = escapableSymbols;
		}

		public static bool Evaluate(Reader reader) => reader.Element is NestedElement;

		public override void Append(char c)
		{
			if (EscapableSymbols != null && EscapableSymbols.Contains(c))
			{
				if (Buffer.Length > 0 && Buffer[Buffer.Length - 1] == c)
					return;
			}

			base.Append(c);
		}
	}

	public abstract class EscapableStringLiteral : ContentElement
	{
		protected EscapableStringLiteral(Element parent) : base(parent) { }

		private bool _skip;

		public override bool EvaluateClosingTag(Reader reader)
		{
			if (_skip)
				return _skip = false;
			if (!base.EvaluateClosingTag(reader))
				return false;
			if (Content != null && reader.Peek(-1) == '\\')
				return false;
			if (reader.Peek(1) != LiteralTag)
				return true;
			_skip = true;
			return false;
		}

		public abstract char LiteralTag { get; }
	}

	public class SingleQuoteLiteral : EscapableStringLiteral
	{
		public const string SYMBOL = "'";
		public override string OpeningTag => SYMBOL;
		public override string ClosingTag => SYMBOL;
		public override char LiteralTag => SYMBOL[0];
		public SingleQuoteLiteral(Element parent) : base(parent) { }
		public static bool Evaluate(Reader reader) => Evaluate(reader, SYMBOL[0]);
	}

	public class DoubleQuoteLiteral : EscapableStringLiteral
	{
		public const string SYMBOL = "\"";
		public override string OpeningTag => SYMBOL;
		public override string ClosingTag => SYMBOL;
		public override char LiteralTag => SYMBOL[0];
		public DoubleQuoteLiteral(Element parent) : base(parent) { }
		public static bool Evaluate(Reader reader) => Evaluate(reader, SYMBOL[0]);
	}

	public class OracleAQMLiteral : ContentElement // Oracle alternative quoting mechanism
	{
		public const string OPENING_TAG = "q'";
		public const string CLOSING_TAG = "'";
		public const string STANDARD_PAIRS = "[]{}()<>";
		public const string ILLEGAL_CHARACTERS = "\r\n\t '";
		public const string WHITE_SPACE = "\r\n\t \0";

		public override string OpeningTag { get; }
		public override string ClosingTag { get; }

		public OracleAQMLiteral(Element parent, Reader reader) : base(parent)
		{
			var alternativeTag = reader.Peek(2);

			OpeningTag = OPENING_TAG + alternativeTag;

			var standardPair = STANDARD_PAIRS.IndexOf(alternativeTag);
			if (standardPair > -1)
				ClosingTag = STANDARD_PAIRS[standardPair + 1] + CLOSING_TAG;
			else
				ClosingTag = alternativeTag + CLOSING_TAG;

		}

		public OracleAQMLiteral(Element parent) : base(parent) { }

		public static bool Evaluate(Reader reader)
		{
			if (!Evaluate(reader, OPENING_TAG))
				return false;
			if (ILLEGAL_CHARACTERS.Contains(reader.Peek(2)))
				return false;
			if (!WHITE_SPACE.Contains(reader.Peek(-1)))
				return false;
			var standardPair = STANDARD_PAIRS.IndexOf(reader.Peek(2));
			return standardPair < 0 || standardPair % 2 == 0;
		}

		public override bool EvaluateClosingTag(Reader reader) => Content != null && base.EvaluateClosingTag(reader);
	}

	public class PostgreDoubleDollarLiteral : ContentElement // This conflicts with Informix EXEC SQL keyword
	{
		public const char SYMBOL = '$';
		public const ushort MAX_TAG = 256;

		public override string OpeningTag { get; }
		public override string ClosingTag { get; }

		public const string WHITE_SPACE = "\r\n\t \0";

		public PostgreDoubleDollarLiteral(Element parent, Reader reader) : base(parent)
		{
			OpeningTag = ClosingTag = DetermineTag(reader);
		}

		public static bool Evaluate(Reader reader)
		{
			if (reader.Char != SYMBOL)
				return false;
			if (!WHITE_SPACE.Contains(reader.Peek(-1)))
				return false;
			return DetermineTag(reader) != null;
		}

		private static unsafe string  DetermineTag(Reader reader)
		{
			char* buf = stackalloc char[MAX_TAG];
			var done = false;			

			for (ushort i = 0; i < MAX_TAG - 1; i++)
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

	public class ScopeSeparator : TextElement
	{
		public ScopeSeparator(Element parent) : base(parent) { }

		public static bool Evaluate(Reader reader)
		{
			if (!(reader.Element is NestedElement nestedParent)) return false;
			if (!char.IsWhiteSpace(reader.Char)) return false;
			if (!(nestedParent.Children.LastOrDefault() is Scope)) return false;

			var startingIdx = reader.Index;
			try
			{
				while (reader.Read())
				{
					if (char.IsWhiteSpace(reader.Char))
						continue;
					return Scope.Evaluate(reader);
				}
			}
			finally { reader.Index = startingIdx; }

			return false;
		}

		public override string ToString() => $"{GetType().Name}";
	}

	public class ContentText : TextElement
	{
		public ContentText(Element parent) : base(parent) { }
		public static bool Evaluate(Reader reader) => reader.Element is ContentElement;
	}

	public abstract class Parameter : ContentElement
	{
		protected Parameter(Element parent) : base(parent) { }

		public abstract string Name { get; }
	}

	public class SqlBinderParameter : Parameter
	{
		public const string OPENING_SYMBOL = "[";
		public const string CLOSING_SYMBOL = "]";

		public override string OpeningTag => OPENING_SYMBOL;
		public override string ClosingTag => CLOSING_SYMBOL;

		public SqlBinderParameter(Element parent) : base(parent) { }

		public static bool Evaluate(Reader reader) => Evaluate(reader, OPENING_SYMBOL[0]) 
		                                              && reader.Peek(1) != OPENING_SYMBOL[0] && reader.Peek(-1) != OPENING_SYMBOL[0];

		public override string Name => ((ContentText) Content).Text;
	}

	public class BindVariableParameter : Parameter
	{
		public const string SYMBOLS = ":@?";

		public override string OpeningTag { get; }
		public override string ClosingTag { get => string.Empty; }

		public BindVariableParameter(Element parent, Reader reader) : base(parent) => OpeningTag = reader.Char.ToString();

		public static bool Evaluate(Reader reader) => SYMBOLS.Contains(reader.Char) && char.IsLetterOrDigit(reader.Peek(1));

		public override bool EvaluateClosingTag(Reader reader) => !(char.IsLetterOrDigit(reader.Char) || reader.Char == '_');

		public override string Name => ((ContentText)Content).Text;
	}

	public class Scope : NestedElement
	{
		public const string OPENING_TAG = "{";
		public const string CLOSING_TAG = "}";

		public override string OpeningTag => OPENING_TAG;
		public override string ClosingTag => CLOSING_TAG;

		public Scope(Element parent) : base(parent) { }

		public static bool Evaluate(Reader reader) => Evaluate(reader, OPENING_TAG);		
	}
}
