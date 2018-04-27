using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime;
using System.Runtime.CompilerServices;
using System.Text;

namespace SqlBinder.Parsing2
{
	public class Parser
	{
		public Root Parse(string script)
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

				if (TryToCreateScope(reader, nestedElement))
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

		private static bool TryToCreateScope(Reader reader, NestedElement nestedElement)
		{
			if (nestedElement == null)
				return false;

			ScopedElement newElem;
			
			if (SingleQuoteLiteral.Evaluate(reader)) newElem = new SingleQuoteLiteral(nestedElement);
			else if (DoubleQuoteLiteral.Evaluate(reader)) newElem = new DoubleQuoteLiteral(nestedElement);
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

	public class SingleQuoteLiteral : ContentElement
	{
		private const string TAG = "'";

		public override string OpeningTag => TAG;
		public override string ClosingTag => TAG;

		public SingleQuoteLiteral(Element parent) : base(parent) { }

		public static bool Evaluate(Reader reader) => Evaluate(reader, TAG);
	}

	public class DoubleQuoteLiteral : ContentElement
	{
		private const string TAG = "\"";

		public override string OpeningTag => TAG;
		public override string ClosingTag => TAG;

		public DoubleQuoteLiteral(Element parent) : base(parent) { }

		public static bool Evaluate(Reader reader) => Evaluate(reader, TAG);
	}

	public class OracleAQMLiteral : ContentElement
	{
		private const string TAG = "'";

		public override string OpeningTag => TAG;
		public override string ClosingTag => TAG;

		public OracleAQMLiteral(Element parent) : base(parent) { }
	}

	public class ScopeSeparator : TextElement
	{
		public ScopeSeparator(Element parent) : base(parent) { }

		public static bool Evaluate(Reader reader)
		{
			if (!(reader.Element.Parent is NestedElement nestedParent)) return false;
			if (!char.IsWhiteSpace(reader.Char)) return false;
			if (nestedParent.Children.LastOrDefault(e => e is Scope) == null) return false;

			using (new ReaderSnapshot(reader))
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
