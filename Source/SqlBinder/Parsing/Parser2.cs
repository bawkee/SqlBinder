using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SqlBinder.Parsing2
{
	//public class SyntaxConfig
	//{
	//	public virtual string DefaultSeparator { get; set; } = "AND";
	//	public virtual string ScopeSymbols { get; set; } = "{}";
	//	public virtual string ParameterSymbols { get; set; } = "[]";
	//	public virtual string NestedCommentSymbols { get; set; } = "{**}";
	//	public virtual string[] Literals { get; set; } = { "''", "\"\"" };
	//}

	public class Parser
	{
		public Root Parse(string script)
		{
			var reader = new Reader(script);
			var root = reader.Element;

			while(reader.Read())
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
					TryToCreateTextElement(reader, contentElement, nestedElement);
			}

			return root as Root;
		}

		private bool TryToCloseScope(Reader reader, ScopedElement scopedElement)
		{
			if (scopedElement == null || !scopedElement.ValidateClosingTag(reader))
				return false;
			reader.Advance(scopedElement.ClosingTag.Length - 1);
			reader.Element = scopedElement.Parent;
			return true;
		}

		private static bool TryToCreateScope(Reader reader, NestedElement nestedElement)
		{
			if (nestedElement == null)
				return false;

			//ScopedElement newElem = new SingleQuoteLiteral(nestedElement);
			//if (!newElem.Evaluate(reader))
			//{
			//	newElem = new DoubleQuoteLiteral(nestedElement);
			//	if (!newElem.Evaluate(reader))
			//	{
			//		newElem = new OracleAQMLiteral(nestedElement);
			//		if (!newElem.Evaluate(reader))
			//		{
			//			newElem = new Scope(nestedElement);
			//			if (!newElem.Evaluate(reader))
			//			{
			//				newElem = new Parameter(nestedElement);
			//				if (!newElem.Evaluate(reader))
			//					return false;
			//			}
			//		}
			//	}
			//}

			ScopedElement newElem = new SingleQuoteLiteral(nestedElement);
			if (!newElem.Evaluate(reader))
			{
				//newElem = new DoubleQuoteLiteral(nestedElement);
				//if (!newElem.Evaluate(reader))
				//{
				//	newElem = new OracleAQMLiteral(nestedElement);
				//	if (!newElem.Evaluate(reader))
				//	{
						newElem = new Scope(nestedElement);
						if (!newElem.Evaluate(reader))
						{
							newElem = new Parameter(nestedElement);
							if (!newElem.Evaluate(reader))
								return false;
						}
				//	}
				//}
			}

			nestedElement.Children.Add(newElem);
			reader.Advance(newElem.OpeningTag.Length - 1);
			reader.Element = newElem;
			return true;
		}

		private static bool TryToCreateTextElement(Reader reader, ContentElement contentElement, NestedElement nestedElement)
		{
			TextElement newElem = new ContentText(reader.Element);
			if (!newElem.Evaluate(reader))
			{
				newElem = new ScopeSeparator(reader.Element);
				if (!newElem.Evaluate(reader))
				{
					newElem = new Sql(reader.Element);
					if (!newElem.Evaluate(reader))
						return false;
				}
			}

			if (contentElement != null)
				contentElement.Content = newElem;
			else if (nestedElement != null)
				nestedElement.Children.Add(newElem);
			else
				throw new InvalidOperationException();
			reader.Element = newElem;
			return true;			
		}
	}

	public class ElementPool
	{
		private Dictionary<Type, Element> _elementPool = new Dictionary<Type, Element>();

		public Element Resolve(Type elementType, Element parent)
		{
			return Element.CreateElement(elementType, parent);

			if (!_elementPool.ContainsKey(elementType))
				return _elementPool[elementType] = Element.CreateElement(elementType, parent);
			return _elementPool[elementType];
		}

		public void Release(Type elementType)
		{
			_elementPool.Remove(elementType);
		}
	}

	public class Reader
	{
		public Reader(string buffer) => Buffer = buffer;
		public string Buffer { get; set; }
		public int Index { get; set; } = -1;
		public void Advance(int n) => Index += n;
		public char Char { get; set; } 
		public Element Element { get; set; } = new Root();
		public bool Read() => (Char = ++Index < Buffer.Length ? Buffer[Index] : (char)0) > 0;
		public char Peek(int n = 0) => Index + n < Buffer.Length ? Buffer[Index + n] : (char)0;
		public ElementPool ElementPool { get; set; } = new ElementPool();
	}

	public class ReaderRestorer : IDisposable
	{
		public int StartingIndex { get; }
		public Reader Reader { get; set; }

		public ReaderRestorer(Reader reader)
		{
			Reader = reader;
			StartingIndex = reader.Index;
		}

		public void Dispose()
		{
			Reader.Index = StartingIndex;
			Reader.Char = Reader.Peek();
		}

	}

	///* -------------------- */
	/// Element types:
	/// 
	/// Element
	///		TextElement
	///		ScopedElement
	///			ContentElement
	///			NestedElement
	///* -------------------- */

	public abstract class Element
	{
		public Element Parent { get; set; }

		public bool Evaluate(Reader reader) => OnEvaulate(reader);

		protected virtual bool OnEvaulate(Reader reader) => false;

		public static Element CreateElement(Type elementType, Element parent)
		{
			if (elementType == typeof(Sql))
				return new Sql(parent);
			if (elementType == typeof(Scope))
				return new Scope(parent);
			if (elementType == typeof(ScopeSeparator))
				return new ScopeSeparator(parent);
			if (elementType == typeof(Parameter))
				return new Parameter(parent);
			if (elementType == typeof(ContentText))
				return new ContentText(parent);
			if (elementType == typeof(SingleQuoteLiteral))
				return new SingleQuoteLiteral(parent);
			if (elementType == typeof(DoubleQuoteLiteral))
				return new DoubleQuoteLiteral(parent);
			if (elementType == typeof(OracleAQMLiteral))
				return new OracleAQMLiteral(parent);
			if (elementType == typeof(SqlBinderComment))
				return new SqlBinderComment(parent);
			throw new NotSupportedException();
		}
	}

	public abstract class TextElement : Element
	{
		protected TextElement(Element parent) => Parent = parent;

		public StringBuilder Text { get; set; } = new StringBuilder(512);

		protected override bool OnEvaulate(Reader reader)
		{
			Text.Append(reader.Char);
			return true;
		}

		public override string ToString() => $"{GetType().Name} ({Text})";
	}

	public abstract class ScopedElement : Element
	{
		protected ScopedElement(Element parent) => Parent = parent;

		public string OpeningTag { get; set; }
		public string ClosingTag { get; set; }

		protected static bool ValidateTag(Reader r, string tag)
		{
			if (string.IsNullOrEmpty(tag))
				return false;
			for (var i = 0; i < tag.Length; i++)
				if (r.Peek(i) != tag[i])
					return false;
			return true;
		}

		private bool? _openingTag; // Cache since there is no point to re-validate

		protected override bool OnEvaulate(Reader reader)
		{			
			return (bool)(_openingTag = ValidateOpeningTag(reader));
		}
		
		public virtual bool ValidateOpeningTag(Reader reader) => _openingTag ?? ValidateTag(reader, OpeningTag);
		public virtual bool ValidateClosingTag(Reader reader) => ValidateTag(reader, ClosingTag);
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
	}

	public class SqlBinderComment : ContentElement
	{
		public SqlBinderComment(Element parent) : base(parent)
		{
			OpeningTag = "{*";
			ClosingTag = "*}";
		}
	}

	public class Sql : TextElement
	{
		public Sql(Element parent) : base(parent)
		{
		}

		protected override bool OnEvaulate(Reader reader)
		{
			return reader.Element is NestedElement && base.OnEvaulate(reader);
		}
	}

	public class SingleQuoteLiteral : ContentElement
	{
		public SingleQuoteLiteral(Element parent) : base(parent)
		{
			OpeningTag = ClosingTag = "'";
		}
	}

	public class DoubleQuoteLiteral : ContentElement
	{
		public DoubleQuoteLiteral(Element parent) : base(parent)
		{
			OpeningTag = ClosingTag = "\"";
		}
	}

	public class OracleAQMLiteral : ContentElement
	{
		public OracleAQMLiteral(Element parent) : base(parent)
		{
		}
	}

	public class ScopeSeparator : TextElement
	{
		public StringBuilder Separator { get; set; } = new StringBuilder(256);

		public ScopeSeparator(Element parent) : base(parent)
		{
		}

		protected override bool OnEvaulate(Reader reader)
		{
			if (!(Parent is NestedElement nestedParent))
				return false;

			if (!char.IsWhiteSpace(reader.Char))
				return false;
			
			if (nestedParent.Children.LastOrDefault(e => e is Scope) == null)
				return false;

			using (new ReaderRestorer(reader))
			{
				while (reader.Read())
				{
					if (char.IsWhiteSpace(reader.Char))
					{
						Separator.Append(reader.Char);
						continue;
					}
					return new Scope(reader.Element).Evaluate(reader);						
				}
			}

			return false;
		}

		public override string ToString() => $"{GetType().Name}";
	}

	public class ContentText : TextElement
	{
		public ContentText(Element parent) : base(parent)
		{
		}

		protected override bool OnEvaulate(Reader reader)
		{
			return reader.Element is ContentElement && base.OnEvaulate(reader);
		}
	}

	public class Parameter : ContentElement
	{
		public Parameter(Element parent) : base(parent)
		{
			OpeningTag = "[";
			ClosingTag = "]";
		}

		public string Name => ((ContentText) Content).Text.ToString();
	}

	public class Scope : NestedElement
	{
		public Scope(Element parent) : base(parent)
		{
			OpeningTag = "{";
			ClosingTag = "}";
		}
	}
}
