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
			typeof(ScopeSeparator),
			typeof(Sql)
		};

		public void Parse(string script)
		{
			var scopedElementTypes = ElementTypes.Where(t => t == typeof(ScopedElement)).ToArray();

			

			var reader = new Reader(script);

			while(reader.Read())
			{
				if (reader.Element is Root || reader.Element.Parent is NestedElement)
				{
					foreach (var type in scopedElementTypes)
					{
						if (Activator.CreateInstance(type) is ScopedElement scopedObj)
						{
							if (scopedObj.Process(reader))
							{
								if (reader.Element is NestedElement nestedElement)
									nestedElement.Children.Add(scopedObj);
								else
									((NestedElement) reader.Element.Parent).Children.Add(scopedObj);
								reader.Element = scopedObj;
								goto continue_while;
							}
						}
					}
				}

				if (reader.Element is TextElement element)
				{
					element.Text.Append(reader.Char);
				}

				continue_while: ;
			}
		}
	}

	public class Reader
	{
		public Reader(string buffer) => Buffer = buffer;
		public string Buffer { get; set; }
		public int Index { get; set; }
		public char Char { get; set; }
		public Element Element { get; set; } = new Root();
		public bool Read() => (Char = Index < Buffer.Length ? Buffer[Index++] : (char)0) > 0;
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
		public StringBuilder Text { get; set; }
	}

	public abstract class ScopedElement : Element
	{
		public string OpeningTag { get; set; }
		public string ClosingTag { get; set; }

		protected static bool ValidateTag(Reader r, string tag)
		{
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
	}

	public abstract class NestedElement : ScopedElement
	{		
		public HashSet<Element> Children { get; set; } = new HashSet<Element>();
	}

	/* -------------------- */

	public class Root : NestedElement
	{
		//
	}

	public class SqlBinderComment : ContentElement
	{
		public SqlBinderComment()
		{
			OpeningTag = "{*";
			ClosingTag = "*}";
		}
	}

	public class Sql : TextElement
	{
		
	}

	public class SingleQuoteLiteral : ContentElement
	{
		public SingleQuoteLiteral()
		{
			OpeningTag = ClosingTag = "'";
		}
	}

	public class DoubleQuoteLiteral : ContentElement
	{
		public DoubleQuoteLiteral()
		{
			OpeningTag = ClosingTag = "\"";
		}
	}

	public class OracleAQMLiteral : ContentElement
	{

	}

	public class ScopeSeparator : TextElement
	{

	}

	public class Parameter : ContentElement
	{
		public string Name => ((TextElement) Content).Text.ToString();
	}

	public class Scope : NestedElement
	{
		public Scope()
		{
			OpeningTag = "{";
			ClosingTag = "}";
		}
	}
}
