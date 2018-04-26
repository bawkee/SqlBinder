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
		//public SyntaxConfig Config { get; set; } = new SyntaxConfig();

		public static HashSet<Type> ElementTypes = new HashSet<Type>
		{
			typeof(SqlBinderComment),
			typeof(SingleQuoteLiteral),
			typeof(DoubleQuoteLiteral),
			typeof(OracleAQMLiteral),
			typeof(Scope),
			typeof(Parameter),
			typeof(ContentText),
			typeof(ScopeSeparator),
			typeof(Sql)
		};

		public Root Parse(string script)
		{
			var reader = new Reader(script);
			var root = reader.Element;

			while(reader.Read())
			{
				var scopedElement = reader.Element as ScopedElement ?? reader.Element.Parent as ScopedElement;
				var nestedElement = reader.Element as NestedElement ?? reader.Element.Parent as NestedElement;
				var contentElement = reader.Element as ContentElement ?? reader.Element.Parent as ContentElement;

				if (scopedElement != null) // Try to close the current scope
				{
					if (scopedElement.ValidateClosingTag())
					{
						reader.Advance(scopedElement.ClosingTag.Length - 1);
						reader.Element = scopedElement.Parent;
						goto continue_while;
					}
				}

				if (nestedElement != null) // Try to create new scope
				{
					var scopedElementTypes = ElementTypes.Where(t => t.IsSubclassOf(typeof(ScopedElement))).ToArray();

					foreach (var type in scopedElementTypes)
					{
						if (!(Activator.CreateInstance(type, nestedElement) is ScopedElement newScopedElement) 
						    || !newScopedElement.Process(reader))
							continue;
						nestedElement.Children.Add(newScopedElement);
						reader.Advance(newScopedElement.OpeningTag.Length - 1);
						reader.Element = newScopedElement;
						goto continue_while;
					}
				}

				if (reader.Element is TextElement element) // Keep writing on text elem
				{
					element.Text.Append(reader.Char);
				}
				else // Try to create new text elem
				{
					var textElementTypes = ElementTypes.Where(t => t.IsSubclassOf(typeof(TextElement))).ToArray();

					foreach (var type in textElementTypes)
					{
						if (!(Activator.CreateInstance(type, reader.Element) is TextElement newTextElement)
						    || !newTextElement.Process(reader))
							continue;
						if (contentElement != null)
							contentElement.Content = newTextElement;
						else if (nestedElement != null)
							nestedElement.Children.Add(newTextElement);
						else
							throw new InvalidOperationException();
						reader.Element = newTextElement;
						goto continue_while;
					}					
				}

				continue_while: ;
			}

			return root as Root;
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
		public char Peek(int n = 1) => Index + n < Buffer.Length ? Buffer[Index + n] : (char)0;
		public string PeekTwo() => new string(new[] {Char, Peek()});
	}

	//public class LockedReader
	//{

	//}

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
		protected Reader _reader;

		public Element Parent { get; set; }

		public bool Process(Reader reader)
		{
			_reader = reader;
			return OnProcess();
		}

		protected virtual bool OnProcess() => false;
	}

	public abstract class TextElement : Element
	{
		protected TextElement(Element parent) => Parent = parent;

		public StringBuilder Text { get; set; } = new StringBuilder(512);

		protected override bool OnProcess()
		{
			Text.Append(_reader.Char);
			return true;
		}
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

		protected override bool OnProcess()
		{
			return (bool)(_openingTag = ValidateOpeningTag());
		}

		public virtual bool ValidateOpeningTag() => _openingTag ?? ValidateTag(_reader, OpeningTag);
		public virtual bool ValidateClosingTag() => ValidateTag(_reader, ClosingTag);
	}

	public abstract class ContentElement : ScopedElement
	{
		public Element Content { get; set; }

		protected ContentElement(Element parent) : base(parent) { }
	}

	public abstract class NestedElement : ScopedElement
	{		
		public HashSet<Element> Children { get; set; } = new HashSet<Element>();

		protected NestedElement(Element parent) : base(parent) { }
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

		protected override bool OnProcess()
		{
			return _reader.Element is NestedElement && base.OnProcess();
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
		public ScopeSeparator(Element parent) : base(parent)
		{
		}

		protected override bool OnProcess()
		{
			return false && base.OnProcess();
		}
	}

	public class ContentText : TextElement
	{
		public ContentText(Element parent) : base(parent)
		{
		}

		protected override bool OnProcess()
		{
			return _reader.Element is ContentElement && base.OnProcess();
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
