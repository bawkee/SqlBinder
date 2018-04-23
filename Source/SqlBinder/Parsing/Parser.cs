using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using SqlBinder.Properties;

namespace SqlBinder.Parsing
{
	public class RequestParameterArgs : EventArgs
	{
		public Parameter Parameter { get; internal set; }
		public object[] Values { get; set; }
		public bool Processed { get; set; }
	}

	public class ParserBuffer
	{
		public string Output { get; internal set; }
		public Exception CompileException { get; internal set; }
		public bool IsValid { get; internal set; } = true;
		public string Errors { get; internal set; }
		public string Warnings { get; internal set; }
		public List<Scope> Scopes { get; set; }

		internal string Buffer;
		internal StringBuilder SbOutput = new StringBuilder();
		internal StringBuilder SbErrors = new StringBuilder();
		internal StringBuilder SbWarnings = new StringBuilder();		

		internal void LogError(string format, params object[] parameters)
		{
			SbErrors.Append(string.Format(format, parameters));
			SbErrors.Append("\r\n");
		}

		internal void LogWarning(string format, params object[] parameters)
		{
			SbWarnings.Append(string.Format(format, parameters));
			SbWarnings.Append("\r\n");
		}

		internal void Write(string buffer) => SbOutput.Append(buffer);
		internal void Remove(int startIndex, int length) => SbOutput.Remove(startIndex, length);
		internal char GetLast() => SbOutput[SbOutput.Length - 1];
		internal void RemoveOne() => SbOutput.Remove(SbOutput.Length - 1, 1);
		internal void ReduceLeadingWhiteSpace() => ReduceWhiteSpace(1, 0);
		internal void ReduceTrailingWhiteSpace() => ReduceWhiteSpace(SbOutput.Length - 1, -1);

		private void ReduceWhiteSpace(int startIndex, int direction)
		{
			var whiteSpace = new[] {' ', '\t', '\r', '\n'};

			var counter = 0;
			var hasNewline = false;
			var hasSpace = false;

			while (true)
			{
				var index = startIndex + counter * direction;

				if (index < 0 || index >= SbOutput.Length)
					break;

				var c = SbOutput[index];

				if (!whiteSpace.Contains(c))
					break;

				if (c == '\n')
					hasNewline = true;
				if (c == ' ')
					hasSpace = true;

				SbOutput.Remove(index, 1);

				counter++;
			}

			if (hasNewline)
				SbOutput.Append('\n');
			else if (hasSpace)
				SbOutput.Append(' ');
		}

		public override string ToString() => Output;
	}

	public static class ParserSqlKeywords
	{
		public const string AND = "AND";
		public const string OR = "OR";
		public const string NULL = "NULL";		
	}

	public delegate void RequestParameterValueEventHandler(object sender, RequestParameterArgs e);

	public class Parser
	{		
		public event RequestParameterValueEventHandler RequestParameterValue;
		public ParserBuffer Buffer { get; private set; }
		public Dictionary<string, string> GlobalVariables { get; set; } = new Dictionary<string, string> { { "ParserVersion", "1.0" } };

		public void RegisterGlobal(string key, string value) => GlobalVariables.Add(key, value);

		protected virtual void OnRequestParameterValue(RequestParameterArgs e) => RequestParameterValue?.Invoke(this, e);

		public ParserBuffer Parse(string script)
		{
			Buffer = new ParserBuffer { Buffer = script };

			//
			// Remove all the comments, escape special characters, validate against some basic errors
			//
			Buffer.Buffer = ClearComments(Buffer.Buffer);			
			Buffer.Buffer = AutoEscapeStringLiterals(Buffer.Buffer, false);
			Buffer.Buffer = AutoEscapeSqlBrackets(Buffer.Buffer, false);
			ValidateBuffer(Buffer.Buffer);

			//
			// Call the recursive scope processing method against the script
			//
			try
			{
				Buffer.Scopes = new List<Scope>(ProcessScopes(Buffer.Buffer));
			}
			catch (Exception compileEx)
			{
				//
				// Write compilation exception for a faulty script
				//
				Buffer.Scopes = new List<Scope>();
				Buffer.CompileException = compileEx;
			}

			Buffer.Output = Buffer.SbOutput.ToString();
			Buffer.Output = TrimEmptyLinesAndJunk(Buffer.Output);			
			Buffer.Output = AutoEscapeStringLiterals(Buffer.Output, true);
			Buffer.Output = AutoEscapeSqlBrackets(Buffer.Output, true);

			Buffer.Errors = Buffer.SbErrors.ToString();
			Buffer.Warnings = Buffer.SbWarnings.ToString();

			if (Buffer.CompileException != null || !string.IsNullOrEmpty(Buffer.Errors))
				Buffer.IsValid = false;

			return Buffer;
		}

		/// <summary>
		/// Removes any potential unwanted white space, tidying up the sql.
		/// </summary>
		private static string TrimEmptyLinesAndJunk(string buffer) => buffer.Replace("\n\r\n", "\n").Replace("\r\n", "\n").Trim();

		/// <summary>
		/// Removes SqlBinder block comments ({* ... *}). Nested comments are supported as well. Inline comments are not supported.
		/// </summary>
		private static string ClearComments(string buffer)
		{
			var ret = buffer;
			ret = Regex.Replace(ret, @"{\*(?>{\*(?<depth>)|\*}(?<-depth>)|.?)*(?(depth)(?!))\*}", "",
				RegexOptions.IgnorePatternWhitespace | RegexOptions.Singleline);
			return ret;
		}

		/// <summary>
		/// Finds all quoted strings and escapes/unescapes special SqlBinder symbols inside. This is very important for Regex to be able
		/// to parse the script later on - this is the common problem in simple markup scripts, Regex can't deal with that.
		/// </summary>
		private static string AutoEscapeStringLiterals(string buffer, bool unescape)
		{
			var symbols = new[]
			{
				new[] {"[", "&sqlbinderlb;"}, new[] {"]", "&sqlbinderrb;"},
				new[] {"{", "&sqlbinderlcb;"}, new[] {"}", "&sqlbinderrcb;"},
				new[] {"$[", "&sqlbinderlbl;"}, new[] {"]$", "&sqlbinderrbl;"},
				new[] {"${", "&sqlbinderlcbl;"}, new[] {"}$", "&sqlbinderrcbl;"},
			};

			return Regex.Replace(buffer, @"'(?>'(?<depth>)|.?)*(?(depth)(?!))'",
				match => AutoEscapeSpecialCharacters(match.Value, unescape, symbols));
		}

		/// <summary>
		/// Replaces all special characters that collide with SqlBinder symbols as to not have regex pick it up. It's ugly but 
		/// still easier and faster than having regex worry about it.
		/// </summary>
		private static string AutoEscapeSqlBrackets(string buffer, bool unescape)
		{			
			return AutoEscapeSpecialCharacters(buffer, unescape, unescape ?
				new[]
				{
					new[] {"[", "$SBLB$"}, new[] {"]", "$SBRB$"},
					new[] {"{", "$SBLCB$"}, new[] {"}", "$SBRCB$"}
				} :
				new[]
				{
					new[] {"$[", "$SBLB$"}, new[] {"]$", "$SBRB$"},
					new[] {"${", "$SBLCB$"}, new[] {"}$", "$SBRCB$"},
				});
		}

		/// <summary>
		/// Escapes/unescapes given special characters. 
		/// </summary>
		private static string AutoEscapeSpecialCharacters(string buffer, bool unescape, string[][] symbols) 
			=> symbols.Aggregate(buffer, (current, substitute) 
			=> unescape ? current.Replace(substitute[1], substitute[0]) : current.Replace(substitute[0], substitute[1]));

		/// <summary>
		/// Basic syntax validation, checks if there are extra or missing braces.
		/// </summary>
		private void ValidateBuffer(string buffer)
		{
			var opening = Regex.Matches(buffer, @"\{");
			var closing = Regex.Matches(buffer, @"\}");

			if (opening.Count > closing.Count)
				Buffer.LogError(Exceptions.MissingRightCurlyBraces);
			if (closing.Count > opening.Count)
				Buffer.LogError(Exceptions.MissingLeftCurlyBraces);

			opening = Regex.Matches(buffer, @"\[");
			closing = Regex.Matches(buffer, @"\]");

			if (opening.Count > closing.Count)
				Buffer.LogError(Exceptions.MissingRightSquareBrackets);
			if (closing.Count > opening.Count)
				Buffer.LogError(Exceptions.MissingLeftSquareBrackets);
		}

		private static readonly Regex _scopesRegex = new Regex(
			@"(?<tag>[\@])*(?<symbol>[\[{])(?<content>(?>[^{}\[\]]+|[{\[](?<depth>)|[}\]](?<-depth>))*(?(depth)(?!)))[}\]]",
			RegexOptions.Compiled | RegexOptions.IgnorePatternWhitespace);

		/// <summary>
		/// The recursive method that does most of the stuff
		/// </summary>
		private List<Scope> ProcessScopes(string buf, Scope parentScope = null)
		{
			var scopes = new List<Scope>();
		
			Match previousMatch = null;			
		
			var matches = _scopesRegex.Matches(buf);

			if (matches.Count == 0)
			{
				//
				// This is a plain buffer, no scopes in it
				//

				Buffer.Write(buf);
				return scopes;
			}

			//
			// Used for invalid / nonOK scopes in order to avoid joining (And's) scopes with junk inbetween
			//			
			var doJoin = false;

			//
			// Compound parameters are those with ".Item" on them. They allow operations resembling a foreach loop.
			//
			var isCompoundParameter = false;

			//
			// Iterate resulting scopes
			//
			foreach (Match scopeMatch in matches)
			{
				var content = scopeMatch.Groups["content"];
				var symbol = scopeMatch.Groups["symbol"];
				var tag = scopeMatch.Groups["tag"];

				var scope = new Scope
				{
					Content = content.Value,
					Tag = tag.Value
				};

				//
				// Scope with a compound parameter can only have one child scope which is the parameter itself
				//
				if (previousMatch != null && isCompoundParameter)
					Buffer.LogError(Exceptions.MultipleChildScopesInCompound);

				if (symbol.Value == "[")
				{
					//
					// This is a parameter
					//
					scope.IsParameter = true;

					var paramBuf = scope.Content.Split('|');
					var paramFlags = new string[paramBuf.Length - 1];

					//
					// Parameter flags, strings separated by the pipe character
					//
					for (var p = 1; p < paramBuf.Length; p++)
						paramFlags[p - 1] = paramBuf[p].Trim();

					scope.Parameter = new Parameter
					{
						Name = paramBuf[0].Trim(),
						Flags = paramFlags
					};

					//
					// Process parameter members/properties
					//
					var paramMembers = scope.Parameter.Name.Split('.');

					if (paramMembers.Length > 1)
					{
						scope.Parameter.Name = paramMembers[0];
						scope.Parameter.Member = paramMembers[1];

						if (paramMembers[1].ToUpper() == "ITEM")
							isCompoundParameter = true;

						scope.Parameter.IsCompound = isCompoundParameter;
					}
				}

				//
				// Insert text before the scope match
				//
				string textBeforeMatch;

				if (previousMatch == null)
					textBeforeMatch = buf.Substring(0, scopeMatch.Index);
				else
					textBeforeMatch = buf.Substring(previousMatch.Index + previousMatch.Length,
												 scopeMatch.Index - (previousMatch.Index + previousMatch.Length));
				
				Buffer.Write(textBeforeMatch);

				Buffer.ReduceTrailingWhiteSpace();

				//
				// This is where actual contents of this scope begin
				//
				var scopeBegin = Buffer.SbOutput.Length;

				if (doJoin && string.IsNullOrEmpty(textBeforeMatch.Trim()))
				{
					// Add space before the separator (i.e. AND/OR) if there isn't one
					if (!new[] { ' ', '\t', '\r', '\n' }.Contains(Buffer.SbOutput[Buffer.SbOutput.Length - 1]))
						Buffer.Write(" ");
					
					// It used to support any operator but after more than 8 years besides just 'AND' I've only used 'OR' a couple of times so I 
					// didn't see a point polluting the syntax. It might be a good idea to support more but have to be careful not to interfere 
					// with the SQL.
					Buffer.Write(string.Format("{0} ", parentScope?.Tag == "@" ? ParserSqlKeywords.OR : ParserSqlKeywords.AND));
				}

				var children = new List<Scope>();

				object[] paramValues = null;

				if (!scope.IsParameter)
				{
					
					//
					// Process schildren scopes, sub-scopes and parameters
					//
					children = ProcessScopes(content.Value, scope);

					//
					// If scope has children and none are valid then parent scope itself is not valid
					//
					if (children.Count > 0)
						scope.IsValid = children.Any(child => child.IsValid);
				}
				else
				{
					//
					// Process this parameter
					//						
					if (scope.Parameter.Name.ToUpper() == "GLOBAL")
					{
						if (GlobalVariables.ContainsKey(scope.Parameter.Member))
							Buffer.Write(GlobalVariables[scope.Parameter.Member]);
						else
							Buffer.LogWarning(Exceptions.CouldNotFindGlobalVariable, scope.Parameter.Member);
					}
					else
					{
						var paramArgs = new RequestParameterArgs
						{
							Parameter = scope.Parameter
						};

						OnRequestParameterValue(paramArgs);

						if (paramArgs.Processed)
						{
							paramValues = paramArgs.Values;

							if (paramValues != null && paramValues.Length > 0)
							{
								Buffer.Write(Convert.ToString(paramValues[0] ?? ParserSqlKeywords.NULL));
								scope.IsValid = true;
							}
						}
						else
						{
							Buffer.LogWarning(Exceptions.ParameterNotProcessed, scope.Parameter);
						}
					}
				}

				if (!scope.IsValid)
					doJoin = doJoin && string.IsNullOrEmpty(textBeforeMatch.Trim());
				else
					doJoin = true;

				//
				// Scope internal contents end here
				//
				var scopeEnd = Buffer.SbOutput.Length;

				//
				// Insert after-text if this is the last match
				//
				string textAfterMatch = null;

				if (!scopeMatch.NextMatch().Success)
					textAfterMatch = buf.Substring(scopeMatch.Index + scopeMatch.Length);

				Buffer.Write(textAfterMatch);

				if (!scope.IsValid)
					Buffer.Remove(scopeBegin, scopeEnd - scopeBegin);

				//
				// Process a compound parameter, its before/after text and its contents will be repeated
				// in a foreach manner. Basically, its parent scope will be appended N times.
				//

				if (isCompoundParameter && paramValues != null && paramValues.Length > 1)
				{
					for (var p = 1; p < paramValues.Length; p++)
					{
						var paramSeparator = string.Empty;

						if (scope.Parameter.Flags.Length == 0)
							Buffer.LogError(string.Format(Exceptions.CompoundParameterWithNoSeparator, scope.Parameter.Name));
						else
							paramSeparator = scope.Parameter.Flags[0];

						Buffer.Write(string.Format(" {0} ", paramSeparator));//res						
						Buffer.Write(textBeforeMatch);//res

						if (paramValues[p] != null)
						{
							Buffer.Write(paramValues[p].ToString());//res
						}

						Buffer.Write(textAfterMatch);//res
					}

				}

				scope.ChildScopes = new List<Scope>(children);

				scopes.Add(scope);

				previousMatch = scopeMatch;
			}

			return scopes;
		}
	}

	public class Parameter
	{
		internal Parameter()
		{
			Name = string.Empty;
			Member = string.Empty;
		}

		public string Name { get; internal set; }
		public string Member { get; internal set; }
		public string[] Flags { get; internal set; }
		public bool IsCompound { get; internal set; }

		public override string ToString()
		{
			if (string.IsNullOrEmpty(Member))
				return Name;
			return !string.IsNullOrEmpty(Name) ? string.Format("{0}.{1}", Name, Member) : "";
		}
	}

	public class Scope
	{
		public string Content { get; internal set; }
		public Parameter Parameter { get; internal set; }
		public bool IsParameter { get; internal set; }
		public Scope ParentScope { get; internal set; }
		public List<Scope> ChildScopes { get; internal set; }				
		public bool IsValid { get; internal set; }
		public string Tag { get; internal set; }
		public override string ToString() => Content;
	}
}
