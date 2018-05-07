using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime;
using System.Runtime.CompilerServices;
using System.Text;

namespace SqlBinder.Parsing2
{
	public class Lexer
	{
		public Root Process(string script)
		{
			var reader = new Reader(script);
			var root = reader.Element;

			while (reader.Read())
			{
				var scopedElement = reader.Element as ScopedElement ?? reader.Element.Parent as ScopedElement;
				var nestedElement = reader.Element as NestedElement ?? reader.Element.Parent as NestedElement;
				var contentElement = reader.Element as ContentElement ?? reader.Element.Parent as ContentElement;

				if (TryToCloseScope(reader, scopedElement))
					continue;

				if (TryToCreateScope(reader, nestedElement, contentElement))
					continue;

				if (reader.Element is TextElement element) // Keep writing on text elem
					element.Text.Append(reader.Char);
				else
					CreateTextElement(reader, contentElement, nestedElement);
			}

			return root as Root;
		}

		private static bool TryToCloseScope(Reader reader, ScopedElement scopedElement)
		{
			if (scopedElement == null || !scopedElement.EvaluateClosingTag(reader))
				return false;
			reader.Advance(scopedElement.ClosingTag.Length - 1);
			reader.Element = scopedElement.Parent;
			return true;
		}

		private static bool TryToCreateScope(Reader reader, NestedElement nestedElement, ContentElement contentElement)
		{
			if (nestedElement == null || contentElement != null)
				return false;

			ScopedElement newElem;
			
			if (SingleQuoteLiteral.Evaluate(reader)) newElem = new SingleQuoteLiteral(nestedElement);
			else if (DoubleQuoteLiteral.Evaluate(reader)) newElem = new DoubleQuoteLiteral(nestedElement);
			else if (OracleAQMLiteral.Evaluate(reader)) newElem = new OracleAQMLiteral(nestedElement, reader);
			else if (PostgreDoubleDollarLiteral.Evaluate(reader)) newElem = new PostgreDoubleDollarLiteral(nestedElement, reader);
			else if (Scope.Evaluate(reader)) newElem = new Scope(nestedElement);
			else if (Parameter.Evaluate(reader)) newElem = new Parameter(nestedElement);
			else return false;

			nestedElement.Children.Add(newElem);
			reader.Advance(newElem.OpeningTag.Length - 1);
			reader.Element = newElem;

			return true;
		}

		private static void CreateTextElement(Reader reader, ContentElement contentElement, NestedElement nestedElement)
		{
			TextElement newElem;

			if (ContentText.Evaluate(reader)) newElem = new ContentText(reader.Element);
			else if (ScopeSeparator.Evaluate(reader)) newElem = new ScopeSeparator(reader.Element);
			else if (Sql.Evaluate(reader)) newElem = new Sql(reader.Element);
			else throw new NotSupportedException();

			if (contentElement != null)
				contentElement.Content = newElem;
			else if (nestedElement != null)
				nestedElement.Children.Add(newElem);
			else
				throw new InvalidOperationException();

			newElem.Text.Append(reader.Char);
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

		public bool ValidateString(string str)
		{
			if (string.IsNullOrEmpty(str))
				return false;
			for (var i = 0; i < str.Length; i++)
				if (Peek(i) != str[i])
					return false;
			return true;
		}
	}

	public class ReaderSnapshot : IDisposable
	{
		public int StartingIndex { get; }
		public Reader Reader { get; set; }

		public ReaderSnapshot(Reader reader)
		{
			Reader = reader;
			StartingIndex = reader.Index;
		}

		public void Dispose() => Reader.Index = StartingIndex;
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
		public StringBuilder Text { get; set; } = new StringBuilder(BUFFER_SIZE);
		public override string ToString() => $"{GetType().Name} ({Text})";
	}

	public abstract class ScopedElement : Element
	{
		protected ScopedElement(Element parent) => Parent = parent;

		public abstract string OpeningTag { get; }
		public abstract string ClosingTag { get; }

		protected static bool Evaluate(Reader reader, string tag) => reader.ValidateString(tag);
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
		private const string OPENING_TAG = "{*";
		private const string CLOSING_TAG = "*}";

		public override string OpeningTag => OPENING_TAG;
		public override string ClosingTag => CLOSING_TAG;		

		public SqlBinderComment(Element parent) : base(parent) { }

		public static bool Evaluate(Reader reader) => Evaluate(reader, OPENING_TAG);		
	}

	public class Sql : TextElement
	{
		public Sql(Element parent) : base(parent) { }
		public static bool Evaluate(Reader reader) => reader.Element is NestedElement;
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
		public const string TAG = "'";
		public override string OpeningTag => TAG;
		public override string ClosingTag => TAG;
		public override char LiteralTag => TAG[0];
		public SingleQuoteLiteral(Element parent) : base(parent) { }
		public static bool Evaluate(Reader reader) => Evaluate(reader, TAG);
	}

	public class DoubleQuoteLiteral : EscapableStringLiteral
	{
		public const string TAG = "\"";
		public override string OpeningTag => TAG;
		public override string ClosingTag => TAG;
		public override char LiteralTag => TAG[0];
		public DoubleQuoteLiteral(Element parent) : base(parent) { }
		public static bool Evaluate(Reader reader) => Evaluate(reader, TAG);
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
			if (!(reader.Element.Parent is NestedElement nestedParent)) return false;
			if (!char.IsWhiteSpace(reader.Char)) return false;
			if (nestedParent.Children.LastOrDefault(e => e is Scope) == null) return false;

			using (new ReaderSnapshot(reader)) // <-- code smell, heap alloc and GC fuckery
			{
				while (reader.Read())
				{
					if (char.IsWhiteSpace(reader.Char))
						continue;
					return Scope.Evaluate(reader);
				}
			}

			return false;
		}

		public override string ToString() => $"{GetType().Name}";
	}

	public class ContentText : TextElement
	{
		public ContentText(Element parent) : base(parent) { }
		public static bool Evaluate(Reader reader) => reader.Element is ContentElement;
	}

	public class Parameter : ContentElement
	{
		private const string OPENING_TAG = "[";
		private const string CLOSING_TAG = "]";

		public override string OpeningTag => OPENING_TAG;
		public override string ClosingTag => CLOSING_TAG;

		public Parameter(Element parent) : base(parent) { }

		public static bool Evaluate(Reader reader) => Evaluate(reader, OPENING_TAG);		

		public string Name => ((ContentText) Content).Text.ToString();
	}

	public class Scope : NestedElement
	{
		private const string OPENING_TAG = "{";
		private const string CLOSING_TAG = "}";

		public override string OpeningTag => OPENING_TAG;
		public override string ClosingTag => CLOSING_TAG;

		public Scope(Element parent) : base(parent) { }

		public static bool Evaluate(Reader reader) => Evaluate(reader, OPENING_TAG);		
	}
}
