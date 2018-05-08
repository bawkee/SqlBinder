using System.Data;
using System.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SqlBinder.Parsing2;

namespace SqlBinder.UnitTesting
{
	public partial class SqlBinder_Tests
	{
		/// <summary>
		/// Tests with parsing functionalities in mind such as comments, literals, escape characters etc.
		/// </summary>
		[TestClass]
		public class Parser_Tests
		{			
			[TestInitialize]
			public void InitializeTest()
			{
				//
			}

			[TestMethod]
			public void ParserTest_Lexing()
			{
				var syntax = "SELECT * FROM TEST {WHERE SOMETHING LIKE 'Test' AND {SomethingElse [somethingElse]} {[[Something Third]] [something Third]}};";

				var root = new Lexer().Process(syntax);

				var nesting = 0;
				OutputLexerResults(root, ref nesting);

				Assert.IsNull(root.ClosingTag);
				Assert.IsNull(root.OpeningTag);
				Assert.IsNull(root.Parent);
				Assert.AreEqual(3, root.Children.Count);
				
				Assert.IsInstanceOfType(root.Children[0], typeof(Sql));
				Assert.IsTrue(((Sql) root.Children[0]).Parent == root);
				Assert.AreEqual(((Sql) root.Children[0]).Text, "SELECT * FROM TEST ");

				Assert.IsInstanceOfType(root.Children[1], typeof(Scope));
				Assert.IsTrue(((Scope) root.Children[1]).Parent == root);
				Assert.AreEqual(6, ((Scope) root.Children[1]).Children.Count);
				Assert.AreEqual("{", ((Scope) root.Children[1]).OpeningTag);
				Assert.AreEqual("}", ((Scope) root.Children[1]).ClosingTag);

				Assert.IsInstanceOfType(((Scope)root.Children[1]).Children[0], typeof(Sql));
				Assert.IsTrue(((Scope)root.Children[1]).Children[0].Parent == root.Children[1]);
				Assert.AreEqual("WHERE SOMETHING LIKE ", ((Sql)((Scope)root.Children[1]).Children[0]).Text);

				Assert.IsInstanceOfType( ((Scope)root.Children[1]).Children[1], typeof(SingleQuoteLiteral));
				Assert.IsTrue(((Scope)root.Children[1]).Children[1].Parent == root.Children[1]);
				Assert.IsInstanceOfType(((ContentElement) ((Scope) root.Children[1]).Children[1]).Content, typeof(ContentText));
				Assert.AreEqual("'", ((ScopedElement) ((Scope) root.Children[1]).Children[1]).OpeningTag);
				Assert.AreEqual("'", ((ScopedElement)((Scope)root.Children[1]).Children[1]).ClosingTag);
				Assert.AreEqual("Test", ((ContentText) ((ContentElement) ((Scope) root.Children[1]).Children[1]).Content).Text);

				Assert.IsInstanceOfType(((Scope)root.Children[1]).Children[2], typeof(Sql));
				Assert.IsTrue(((Scope)root.Children[1]).Children[2].Parent == root.Children[1]);
				Assert.AreEqual(" AND ", ((Sql)((Scope)root.Children[1]).Children[2]).Text);

				Assert.IsInstanceOfType(((Scope)root.Children[1]).Children[3], typeof(Scope));
				Assert.IsTrue(((Scope)root.Children[1]).Children[3].Parent == root.Children[1]);
				Assert.AreEqual(2, ((Scope)((Scope)root.Children[1]).Children[3]).Children.Count);

				Assert.IsInstanceOfType(((Scope) ((Scope) root.Children[1]).Children[3]).Children[0], typeof(Sql));
				Assert.IsTrue(((Scope) ((Scope) root.Children[1]).Children[3]).Children[0].Parent == (Scope) ((Scope) root.Children[1]).Children[3]);
				Assert.AreEqual("SomethingElse ", ((Sql) ((Scope) ((Scope) root.Children[1]).Children[3]).Children[0]).Text);

				Assert.IsInstanceOfType(((Scope)((Scope)root.Children[1]).Children[3]).Children[1], typeof(Parameter));
				Assert.AreEqual("[", ((ScopedElement) ((Scope) ((Scope) root.Children[1]).Children[3]).Children[1]).OpeningTag);
				Assert.AreEqual("]", ((ScopedElement) ((Scope) ((Scope) root.Children[1]).Children[3]).Children[1]).ClosingTag);
				Assert.AreEqual("somethingElse", ((ContentText) ((Parameter) ((Scope) ((Scope) root.Children[1]).Children[3]).Children[1]).Content).Text);

				Assert.IsInstanceOfType(((Scope)root.Children[1]).Children[4], typeof(ScopeSeparator));
				Assert.IsTrue(((Scope)root.Children[1]).Children[4].Parent == root.Children[1]);
				Assert.AreEqual(" ", ((ScopeSeparator)((Scope)root.Children[1]).Children[4]).Text);

				Assert.IsInstanceOfType(((Scope)root.Children[1]).Children[5], typeof(Scope));
				Assert.IsTrue(((Scope)root.Children[1]).Children[5].Parent == root.Children[1]);
				Assert.AreEqual(2, ((Scope)((Scope)root.Children[1]).Children[5]).Children.Count);

				Assert.IsInstanceOfType(((Scope)((Scope)root.Children[1]).Children[5]).Children[0], typeof(Sql));
				Assert.IsTrue(((Scope)((Scope)root.Children[1]).Children[5]).Children[0].Parent == (Scope)((Scope)root.Children[1]).Children[5]);
				Assert.AreEqual("[Something Third] ", ((Sql)((Scope)((Scope)root.Children[1]).Children[5]).Children[0]).Text);

				Assert.IsInstanceOfType(((Scope)((Scope)root.Children[1]).Children[5]).Children[1], typeof(Parameter));
				Assert.AreEqual("[", ((ScopedElement)((Scope)((Scope)root.Children[1]).Children[5]).Children[1]).OpeningTag);
				Assert.AreEqual("]", ((ScopedElement)((Scope)((Scope)root.Children[1]).Children[5]).Children[1]).ClosingTag);
				Assert.AreEqual("something Third", ((ContentText)((Parameter)((Scope)((Scope)root.Children[1]).Children[5]).Children[1]).Content).Text);

				Assert.IsInstanceOfType(root.Children[2], typeof(Sql));
				Assert.IsTrue(((Sql)root.Children[2]).Parent == root);
				Assert.AreEqual(";", ((Sql)root.Children[2]).Text);
			}

			[TestMethod]
			public void ParserTest_LexingLiterals()
			{
				var syntax = "SELECT * FROM TEST {WHERE " +
				             "'{Test}' OR " +
				             "\"Some {More} tests\" OR " +
				             "\"'This'\" OR " +
				             "\"Something 'like this' or \\\"like this\\\"\" OR " +
				             "'\"This\"' OR " +
				             "'Something \"like this\" or ''like this\\'' OR " +
							 "q'\"This is 'quoted' text\"' OR " +
							 "q'\"This has one ' quote\"' OR " +
							 "q'\"This has one \\' quote\"' OR " +
							 "q'{This is 'quoted' text}' OR " +
							 "q'(This is 'quoted' text)' OR " +
							 "q'<This is 'quoted' text>' OR " +
							 "q'[This is 'quoted' text]' OR " +
							 "q'`This is 'quoted' text`' OR " +
							 "q'{This has alternative {} quotes}' OR " +
							 "q'{This has alternative \\} quotes}' OR " +
							 "q'\"This has alternative \"\" quotes\"' OR " +
				             "q'\"This has alternative \\\" quotes\"' OR " +				             
							 "'''This''' OR " +
				             "\"\"\"This\"\"\" OR " +
				             "testq'[This]'}";

				/* 
				 '{Test}'
				 "Some {More} tests"
				 "'This'"
				 "Something 'like this' or \"like this\""
				 '"This"'
				 'Something "like this" or ''like this\''
				 q'"This is 'quoted' text"'
				 q'"This has one ' quote"'
				 q'"This has one \' quote"'
				 q'{This is 'quoted' text}'
				 q'(This is 'quoted' text)'
				 q'<This is 'quoted' text>'
				 q'[This is 'quoted' text]'
				 q'`This is 'quoted' text`'
				 q'{This has alternative {} quotes}'
				 q'{This has alternative \} quotes}'
				 q'"This has alternative "" quotes"'
				 q'"This has alternative \" quotes"'
				 */

				var root = new Lexer().Process(syntax);

				var nesting = 0;
				OutputLexerResults(root, ref nesting);

				Assert.IsNull(root.ClosingTag);
				Assert.IsNull(root.OpeningTag);
				Assert.IsNull(root.Parent);
				Assert.IsTrue(root.Children.Count == 2);

				Assert.IsInstanceOfType(root.Children[0], typeof(Sql));
				Assert.IsTrue(((Sql)root.Children[0]).Parent == root);
				Assert.AreEqual(((Sql)root.Children[0]).Text, "SELECT * FROM TEST ");

				Assert.IsInstanceOfType(root.Children[1], typeof(Scope));
				Assert.IsTrue(((Scope)root.Children[1]).Parent == root);
				Assert.AreEqual("{", ((Scope)root.Children[1]).OpeningTag);
				Assert.AreEqual("}", ((Scope)root.Children[1]).ClosingTag);

				Assert.IsInstanceOfType(((Scope)root.Children[1]).Children[0], typeof(Sql));
				Assert.IsTrue(((Scope)root.Children[1]).Children[0].Parent == root.Children[1]);
				Assert.AreEqual("WHERE ", ((Sql)((Scope)root.Children[1]).Children[0]).Text);

				Assert.IsInstanceOfType(((Scope)root.Children[1]).Children[1], typeof(SingleQuoteLiteral));
				Assert.IsTrue(((Scope)root.Children[1]).Children[1].Parent == root.Children[1]);
				Assert.IsInstanceOfType(((ContentElement)((Scope)root.Children[1]).Children[1]).Content, typeof(ContentText));
				Assert.AreEqual("'", ((ScopedElement)((Scope)root.Children[1]).Children[1]).OpeningTag);
				Assert.AreEqual("'", ((ScopedElement)((Scope)root.Children[1]).Children[1]).ClosingTag);
				Assert.AreEqual("{Test}", ((ContentText)((ContentElement)((Scope)root.Children[1]).Children[1]).Content).Text);

				Assert.IsInstanceOfType(((Scope)root.Children[1]).Children[2], typeof(Sql));
				Assert.IsTrue(((Scope)root.Children[1]).Children[2].Parent == root.Children[1]);
				Assert.AreEqual(" OR ", ((Sql)((Scope)root.Children[1]).Children[2]).Text);

				Assert.IsInstanceOfType(((Scope)root.Children[1]).Children[3], typeof(DoubleQuoteLiteral));
				Assert.IsTrue(((Scope)root.Children[1]).Children[3].Parent == root.Children[1]);
				Assert.IsInstanceOfType(((ContentElement)((Scope)root.Children[1]).Children[3]).Content, typeof(ContentText));
				Assert.AreEqual("\"", ((ScopedElement)((Scope)root.Children[1]).Children[3]).OpeningTag);
				Assert.AreEqual("\"", ((ScopedElement)((Scope)root.Children[1]).Children[3]).ClosingTag);
				Assert.AreEqual("Some {More} tests", ((ContentText)((ContentElement)((Scope)root.Children[1]).Children[3]).Content).Text);

				Assert.IsInstanceOfType(((Scope)root.Children[1]).Children[4], typeof(Sql));
				Assert.IsTrue(((Scope)root.Children[1]).Children[4].Parent == root.Children[1]);
				Assert.AreEqual(((Sql)((Scope)root.Children[1]).Children[4]).Text, " OR ");

				Assert.IsInstanceOfType(((Scope)root.Children[1]).Children[5], typeof(DoubleQuoteLiteral));
				Assert.IsTrue(((Scope)root.Children[1]).Children[5].Parent == root.Children[1]);
				Assert.IsInstanceOfType(((ContentElement)((Scope)root.Children[1]).Children[5]).Content, typeof(ContentText));
				Assert.AreEqual("\"", ((ScopedElement)((Scope)root.Children[1]).Children[5]).OpeningTag);
				Assert.AreEqual("\"", ((ScopedElement)((Scope)root.Children[1]).Children[5]).ClosingTag);
				Assert.AreEqual("'This'", ((ContentText)((ContentElement)((Scope)root.Children[1]).Children[5]).Content).Text);

				Assert.IsInstanceOfType(((Scope)root.Children[1]).Children[6], typeof(Sql));
				Assert.IsTrue(((Scope)root.Children[1]).Children[6].Parent == root.Children[1]);
				Assert.AreEqual(" OR ", ((Sql)((Scope)root.Children[1]).Children[6]).Text);

				Assert.IsInstanceOfType(((Scope)root.Children[1]).Children[7], typeof(DoubleQuoteLiteral));
				Assert.IsTrue(((Scope)root.Children[1]).Children[7].Parent == root.Children[1]);
				Assert.IsInstanceOfType(((ContentElement)((Scope)root.Children[1]).Children[7]).Content, typeof(ContentText));
				Assert.AreEqual("\"", ((ScopedElement)((Scope)root.Children[1]).Children[7]).OpeningTag);
				Assert.AreEqual("\"", ((ScopedElement)((Scope)root.Children[1]).Children[7]).ClosingTag);
				Assert.AreEqual("Something 'like this' or \\\"like this\\\"", ((ContentText)((ContentElement)((Scope)root.Children[1]).Children[7]).Content).Text);

				Assert.IsInstanceOfType(((Scope)root.Children[1]).Children[8], typeof(Sql));
				Assert.IsTrue(((Scope)root.Children[1]).Children[8].Parent == root.Children[1]);
				Assert.AreEqual(" OR ", ((Sql)((Scope)root.Children[1]).Children[8]).Text);

				Assert.IsInstanceOfType(((Scope)root.Children[1]).Children[9], typeof(SingleQuoteLiteral));
				Assert.IsTrue(((Scope)root.Children[1]).Children[9].Parent == root.Children[1]);
				Assert.IsInstanceOfType(((ContentElement)((Scope)root.Children[1]).Children[9]).Content, typeof(ContentText));
				Assert.AreEqual("'", ((ScopedElement)((Scope)root.Children[1]).Children[9]).OpeningTag);
				Assert.AreEqual("'", ((ScopedElement)((Scope)root.Children[1]).Children[9]).ClosingTag);
				Assert.AreEqual("\"This\"", ((ContentText)((ContentElement)((Scope)root.Children[1]).Children[9]).Content).Text);

				Assert.IsInstanceOfType(((Scope)root.Children[1]).Children[10], typeof(Sql));
				Assert.IsTrue(((Scope)root.Children[1]).Children[10].Parent == root.Children[1]);
				Assert.AreEqual(" OR ", ((Sql)((Scope)root.Children[1]).Children[10]).Text);

				Assert.IsInstanceOfType(((Scope)root.Children[1]).Children[11], typeof(SingleQuoteLiteral));
				Assert.IsTrue(((Scope)root.Children[1]).Children[11].Parent == root.Children[1]);
				Assert.IsInstanceOfType(((ContentElement)((Scope)root.Children[1]).Children[11]).Content, typeof(ContentText));
				Assert.AreEqual("'", ((ScopedElement)((Scope)root.Children[1]).Children[11]).OpeningTag);
				Assert.AreEqual("'", ((ScopedElement)((Scope)root.Children[1]).Children[11]).ClosingTag);
				Assert.AreEqual("Something \"like this\" or ''like this\\'", ((ContentText)((ContentElement)((Scope)root.Children[1]).Children[11]).Content).Text);
								
				Assert.IsInstanceOfType(((Scope)root.Children[1]).Children[13], typeof(OracleAQMLiteral));
				Assert.IsTrue(((Scope)root.Children[1]).Children[13].Parent == root.Children[1]);
				Assert.IsInstanceOfType(((ContentElement)((Scope)root.Children[1]).Children[13]).Content, typeof(ContentText));
				Assert.AreEqual("q'\"", ((ScopedElement)((Scope)root.Children[1]).Children[13]).OpeningTag);
				Assert.AreEqual("\"'", ((ScopedElement)((Scope)root.Children[1]).Children[13]).ClosingTag);
				Assert.AreEqual("This is 'quoted' text", ((ContentText)((ContentElement)((Scope)root.Children[1]).Children[13]).Content).Text);

				Assert.IsInstanceOfType(((Scope)root.Children[1]).Children[15], typeof(OracleAQMLiteral));
				Assert.IsTrue(((Scope)root.Children[1]).Children[15].Parent == root.Children[1]);
				Assert.IsInstanceOfType(((ContentElement)((Scope)root.Children[1]).Children[15]).Content, typeof(ContentText));
				Assert.AreEqual("q'\"", ((ScopedElement)((Scope)root.Children[1]).Children[15]).OpeningTag);
				Assert.AreEqual("\"'", ((ScopedElement)((Scope)root.Children[1]).Children[15]).ClosingTag);
				Assert.AreEqual("This has one ' quote", ((ContentText)((ContentElement)((Scope)root.Children[1]).Children[15]).Content).Text);

				Assert.IsInstanceOfType(((Scope)root.Children[1]).Children[17], typeof(OracleAQMLiteral));
				Assert.IsTrue(((Scope)root.Children[1]).Children[17].Parent == root.Children[1]);
				Assert.IsInstanceOfType(((ContentElement)((Scope)root.Children[1]).Children[17]).Content, typeof(ContentText));
				Assert.AreEqual("q'\"", ((ScopedElement)((Scope)root.Children[1]).Children[17]).OpeningTag);
				Assert.AreEqual("\"'", ((ScopedElement)((Scope)root.Children[1]).Children[17]).ClosingTag);
				Assert.AreEqual("This has one \\' quote", ((ContentText)((ContentElement)((Scope)root.Children[1]).Children[17]).Content).Text);

				Assert.IsInstanceOfType(((Scope)root.Children[1]).Children[19], typeof(OracleAQMLiteral));
				Assert.IsTrue(((Scope)root.Children[1]).Children[19].Parent == root.Children[1]);
				Assert.IsInstanceOfType(((ContentElement)((Scope)root.Children[1]).Children[19]).Content, typeof(ContentText));
				Assert.AreEqual("q'{", ((ScopedElement)((Scope)root.Children[1]).Children[19]).OpeningTag);
				Assert.AreEqual("}'", ((ScopedElement)((Scope)root.Children[1]).Children[19]).ClosingTag);
				Assert.AreEqual("This is 'quoted' text", ((ContentText)((ContentElement)((Scope)root.Children[1]).Children[19]).Content).Text);

				Assert.IsInstanceOfType(((Scope)root.Children[1]).Children[21], typeof(OracleAQMLiteral));
				Assert.IsTrue(((Scope)root.Children[1]).Children[21].Parent == root.Children[1]);
				Assert.IsInstanceOfType(((ContentElement)((Scope)root.Children[1]).Children[21]).Content, typeof(ContentText));
				Assert.AreEqual("q'(", ((ScopedElement)((Scope)root.Children[1]).Children[21]).OpeningTag);
				Assert.AreEqual(")'", ((ScopedElement)((Scope)root.Children[1]).Children[21]).ClosingTag);
				Assert.AreEqual("This is 'quoted' text", ((ContentText)((ContentElement)((Scope)root.Children[1]).Children[21]).Content).Text);

				Assert.IsInstanceOfType(((Scope)root.Children[1]).Children[23], typeof(OracleAQMLiteral));
				Assert.IsTrue(((Scope)root.Children[1]).Children[23].Parent == root.Children[1]);
				Assert.IsInstanceOfType(((ContentElement)((Scope)root.Children[1]).Children[23]).Content, typeof(ContentText));
				Assert.AreEqual("q'<", ((ScopedElement)((Scope)root.Children[1]).Children[23]).OpeningTag);
				Assert.AreEqual(">'", ((ScopedElement)((Scope)root.Children[1]).Children[23]).ClosingTag);
				Assert.AreEqual("This is 'quoted' text", ((ContentText)((ContentElement)((Scope)root.Children[1]).Children[23]).Content).Text);

				Assert.IsInstanceOfType(((Scope)root.Children[1]).Children[25], typeof(OracleAQMLiteral));
				Assert.IsTrue(((Scope)root.Children[1]).Children[25].Parent == root.Children[1]);
				Assert.IsInstanceOfType(((ContentElement)((Scope)root.Children[1]).Children[25]).Content, typeof(ContentText));
				Assert.AreEqual("q'[", ((ScopedElement)((Scope)root.Children[1]).Children[25]).OpeningTag);
				Assert.AreEqual("]'", ((ScopedElement)((Scope)root.Children[1]).Children[25]).ClosingTag);
				Assert.AreEqual("This is 'quoted' text", ((ContentText)((ContentElement)((Scope)root.Children[1]).Children[25]).Content).Text);

				Assert.IsInstanceOfType(((Scope)root.Children[1]).Children[27], typeof(OracleAQMLiteral));
				Assert.IsTrue(((Scope)root.Children[1]).Children[27].Parent == root.Children[1]);
				Assert.IsInstanceOfType(((ContentElement)((Scope)root.Children[1]).Children[27]).Content, typeof(ContentText));
				Assert.AreEqual("q'`", ((ScopedElement)((Scope)root.Children[1]).Children[27]).OpeningTag);
				Assert.AreEqual("`'", ((ScopedElement)((Scope)root.Children[1]).Children[27]).ClosingTag);
				Assert.AreEqual("This is 'quoted' text", ((ContentText)((ContentElement)((Scope)root.Children[1]).Children[27]).Content).Text);

				Assert.IsInstanceOfType(((Scope)root.Children[1]).Children[29], typeof(OracleAQMLiteral));
				Assert.IsTrue(((Scope)root.Children[1]).Children[29].Parent == root.Children[1]);
				Assert.IsInstanceOfType(((ContentElement)((Scope)root.Children[1]).Children[29]).Content, typeof(ContentText));
				Assert.AreEqual("q'{", ((ScopedElement)((Scope)root.Children[1]).Children[29]).OpeningTag);
				Assert.AreEqual("}'", ((ScopedElement)((Scope)root.Children[1]).Children[29]).ClosingTag);
				Assert.AreEqual("This has alternative {} quotes", ((ContentText)((ContentElement)((Scope)root.Children[1]).Children[29]).Content).Text);

				Assert.IsInstanceOfType(((Scope)root.Children[1]).Children[31], typeof(OracleAQMLiteral));
				Assert.IsTrue(((Scope)root.Children[1]).Children[31].Parent == root.Children[1]);
				Assert.IsInstanceOfType(((ContentElement)((Scope)root.Children[1]).Children[31]).Content, typeof(ContentText));
				Assert.AreEqual("q'{", ((ScopedElement)((Scope)root.Children[1]).Children[31]).OpeningTag);
				Assert.AreEqual("}'", ((ScopedElement)((Scope)root.Children[1]).Children[31]).ClosingTag);
				Assert.AreEqual("This has alternative \\} quotes", ((ContentText)((ContentElement)((Scope)root.Children[1]).Children[31]).Content).Text);

				Assert.IsInstanceOfType(((Scope)root.Children[1]).Children[33], typeof(OracleAQMLiteral));
				Assert.IsTrue(((Scope)root.Children[1]).Children[33].Parent == root.Children[1]);
				Assert.IsInstanceOfType(((ContentElement)((Scope)root.Children[1]).Children[33]).Content, typeof(ContentText));
				Assert.AreEqual("q'\"", ((ScopedElement)((Scope)root.Children[1]).Children[33]).OpeningTag);
				Assert.AreEqual("\"'", ((ScopedElement)((Scope)root.Children[1]).Children[33]).ClosingTag);
				Assert.AreEqual("This has alternative \"\" quotes", ((ContentText)((ContentElement)((Scope)root.Children[1]).Children[33]).Content).Text);

				Assert.IsInstanceOfType(((Scope)root.Children[1]).Children[35], typeof(OracleAQMLiteral));
				Assert.IsTrue(((Scope)root.Children[1]).Children[35].Parent == root.Children[1]);
				Assert.IsInstanceOfType(((ContentElement)((Scope)root.Children[1]).Children[35]).Content, typeof(ContentText));
				Assert.AreEqual("q'\"", ((ScopedElement)((Scope)root.Children[1]).Children[35]).OpeningTag);
				Assert.AreEqual("\"'", ((ScopedElement)((Scope)root.Children[1]).Children[35]).ClosingTag);
				Assert.AreEqual("This has alternative \\\" quotes", ((ContentText)((ContentElement)((Scope)root.Children[1]).Children[35]).Content).Text);

				Assert.IsInstanceOfType(((Scope)root.Children[1]).Children[37], typeof(SingleQuoteLiteral));
				Assert.IsTrue(((Scope)root.Children[1]).Children[37].Parent == root.Children[1]);
				Assert.IsInstanceOfType(((ContentElement)((Scope)root.Children[1]).Children[37]).Content, typeof(ContentText));
				Assert.AreEqual("'", ((ScopedElement)((Scope)root.Children[1]).Children[37]).OpeningTag);
				Assert.AreEqual("'", ((ScopedElement)((Scope)root.Children[1]).Children[37]).ClosingTag);
				Assert.AreEqual("''This''", ((ContentText)((ContentElement)((Scope)root.Children[1]).Children[37]).Content).Text);

				Assert.IsInstanceOfType(((Scope)root.Children[1]).Children[39], typeof(DoubleQuoteLiteral));
				Assert.IsTrue(((Scope)root.Children[1]).Children[39].Parent == root.Children[1]);
				Assert.IsInstanceOfType(((ContentElement)((Scope)root.Children[1]).Children[39]).Content, typeof(ContentText));
				Assert.AreEqual("\"", ((ScopedElement)((Scope)root.Children[1]).Children[39]).OpeningTag);
				Assert.AreEqual("\"", ((ScopedElement)((Scope)root.Children[1]).Children[39]).ClosingTag);
				Assert.AreEqual("\"\"This\"\"", ((ContentText)((ContentElement)((Scope)root.Children[1]).Children[39]).Content).Text);

				Assert.IsInstanceOfType(((Scope)root.Children[1]).Children[41], typeof(SingleQuoteLiteral));
				Assert.IsTrue(((Scope)root.Children[1]).Children[41].Parent == root.Children[1]);
				Assert.IsInstanceOfType(((ContentElement)((Scope)root.Children[1]).Children[41]).Content, typeof(ContentText));
				Assert.AreEqual("'", ((ScopedElement)((Scope)root.Children[1]).Children[41]).OpeningTag);
				Assert.AreEqual("'", ((ScopedElement)((Scope)root.Children[1]).Children[41]).ClosingTag);
				Assert.AreEqual("[This]", ((ContentText)((ContentElement)((Scope)root.Children[1]).Children[41]).Content).Text);
			}

			[TestMethod]
			public void ParserTest_Lexing_PostgreSQLDollarLiterals()
			{
				var syntax = "SELECT * FROM TEST {WHERE " +
				             "$q${This is 'quoted' text}$q$ OR " +
				             "$${This is \"quoted\" text}$$ OR " +
				             "$tag$This is q'{quoted}' text$tag$ OR " +
				             "$tag$This $is $'quoted' $$ text$tag$ OR " +
				             "$tag$This is $'$quoted$'$ text$tag$}";

				var root = new Lexer().Process(syntax);

				var nesting = 0;
				OutputLexerResults(root, ref nesting);

				Assert.IsNull(root.ClosingTag);
				Assert.IsNull(root.OpeningTag);
				Assert.IsNull(root.Parent);
				Assert.IsTrue(root.Children.Count == 2);

				Assert.IsInstanceOfType(root.Children[0], typeof(Sql));
				Assert.IsTrue(((Sql)root.Children[0]).Parent == root);
				Assert.AreEqual(((Sql)root.Children[0]).Text, "SELECT * FROM TEST ");

				Assert.IsInstanceOfType(root.Children[1], typeof(Scope));
				Assert.IsTrue(((Scope)root.Children[1]).Parent == root);
				Assert.AreEqual("{", ((Scope)root.Children[1]).OpeningTag);
				Assert.AreEqual("}", ((Scope)root.Children[1]).ClosingTag);

				Assert.IsInstanceOfType(((Scope)root.Children[1]).Children[0], typeof(Sql));
				Assert.IsTrue(((Scope)root.Children[1]).Children[0].Parent == root.Children[1]);
				Assert.AreEqual("WHERE ", ((Sql)((Scope)root.Children[1]).Children[0]).Text);

				Assert.IsInstanceOfType(((Scope)root.Children[1]).Children[1], typeof(PostgreDoubleDollarLiteral));
				Assert.IsTrue(((Scope)root.Children[1]).Children[1].Parent == root.Children[1]);
				Assert.IsInstanceOfType(((ContentElement)((Scope)root.Children[1]).Children[1]).Content, typeof(ContentText));
				Assert.AreEqual("$q$", ((ScopedElement)((Scope)root.Children[1]).Children[1]).OpeningTag);
				Assert.AreEqual("$q$", ((ScopedElement)((Scope)root.Children[1]).Children[1]).ClosingTag);
				Assert.AreEqual("{This is 'quoted' text}", ((ContentText)((ContentElement)((Scope)root.Children[1]).Children[1]).Content).Text);

				Assert.IsInstanceOfType(((Scope)root.Children[1]).Children[3], typeof(PostgreDoubleDollarLiteral));
				Assert.IsTrue(((Scope)root.Children[1]).Children[3].Parent == root.Children[1]);
				Assert.IsInstanceOfType(((ContentElement)((Scope)root.Children[1]).Children[3]).Content, typeof(ContentText));
				Assert.AreEqual("$$", ((ScopedElement)((Scope)root.Children[1]).Children[3]).OpeningTag);
				Assert.AreEqual("$$", ((ScopedElement)((Scope)root.Children[1]).Children[3]).ClosingTag);
				Assert.AreEqual("{This is \"quoted\" text}", ((ContentText)((ContentElement)((Scope)root.Children[1]).Children[3]).Content).Text);

				Assert.IsInstanceOfType(((Scope)root.Children[1]).Children[5], typeof(PostgreDoubleDollarLiteral));
				Assert.IsTrue(((Scope)root.Children[1]).Children[5].Parent == root.Children[1]);
				Assert.IsInstanceOfType(((ContentElement)((Scope)root.Children[1]).Children[5]).Content, typeof(ContentText));
				Assert.AreEqual("$tag$", ((ScopedElement)((Scope)root.Children[1]).Children[5]).OpeningTag);
				Assert.AreEqual("$tag$", ((ScopedElement)((Scope)root.Children[1]).Children[5]).ClosingTag);
				Assert.AreEqual("This is q'{quoted}' text", ((ContentText)((ContentElement)((Scope)root.Children[1]).Children[5]).Content).Text);

				Assert.IsInstanceOfType(((Scope)root.Children[1]).Children[7], typeof(PostgreDoubleDollarLiteral));
				Assert.IsTrue(((Scope)root.Children[1]).Children[7].Parent == root.Children[1]);
				Assert.IsInstanceOfType(((ContentElement)((Scope)root.Children[1]).Children[7]).Content, typeof(ContentText));
				Assert.AreEqual("$tag$", ((ScopedElement)((Scope)root.Children[1]).Children[7]).OpeningTag);
				Assert.AreEqual("$tag$", ((ScopedElement)((Scope)root.Children[1]).Children[7]).ClosingTag);
				Assert.AreEqual("This $is $'quoted' $$ text", ((ContentText)((ContentElement)((Scope)root.Children[1]).Children[7]).Content).Text);

				Assert.IsInstanceOfType(((Scope)root.Children[1]).Children[9], typeof(PostgreDoubleDollarLiteral));
				Assert.IsTrue(((Scope)root.Children[1]).Children[9].Parent == root.Children[1]);
				Assert.IsInstanceOfType(((ContentElement)((Scope)root.Children[1]).Children[9]).Content, typeof(ContentText));
				Assert.AreEqual("$tag$", ((ScopedElement)((Scope)root.Children[1]).Children[9]).OpeningTag);
				Assert.AreEqual("$tag$", ((ScopedElement)((Scope)root.Children[1]).Children[9]).ClosingTag);
				Assert.AreEqual("This is $'$quoted$'$ text", ((ContentText)((ContentElement)((Scope)root.Children[1]).Children[9]).Content).Text);
			}

			[TestMethod]
			public void ParserTest_LexingComments()
			{
				var syntax = "THIS IS SQL {THIS SQL IN A SCOPE " +
							 "WHICH HAS /*{SQL BINDER COMMENT}*/ AND /*AN SQL COMMENT*/ " +
							 "/*OR A\r\nMULTILINE\r\nSQL COMMENT*/}";

				var root = new Lexer().Process(syntax);

				var nesting = 0;
				OutputLexerResults(root, ref nesting);

				Assert.IsInstanceOfType(root.Children[0], typeof(Sql));
				Assert.IsTrue(((Sql)root.Children[0]).Parent == root);
				Assert.AreEqual(((Sql)root.Children[0]).Text, "THIS IS SQL ");

				Assert.IsInstanceOfType(root.Children[1], typeof(Scope));
				Assert.IsTrue(((Scope)root.Children[1]).Parent == root);
				Assert.AreEqual("{", ((Scope)root.Children[1]).OpeningTag);
				Assert.AreEqual("}", ((Scope)root.Children[1]).ClosingTag);

				Assert.IsInstanceOfType(((Scope)root.Children[1]).Children[0], typeof(Sql));
				Assert.IsTrue(((Scope)root.Children[1]).Children[0].Parent == root.Children[1]);
				Assert.AreEqual("THIS SQL IN A SCOPE WHICH HAS ", ((Sql)((Scope)root.Children[1]).Children[0]).Text);

				Assert.IsInstanceOfType(((Scope)root.Children[1]).Children[1], typeof(SqlBinderComment));
				Assert.IsTrue(((Scope)root.Children[1]).Children[1].Parent == root.Children[1]);
				Assert.IsInstanceOfType(((ContentElement)((Scope)root.Children[1]).Children[1]).Content, typeof(ContentText));
				Assert.AreEqual("SQL BINDER COMMENT", ((ContentText)((ContentElement)((Scope)root.Children[1]).Children[1]).Content).Text);

				Assert.IsInstanceOfType(((Scope)root.Children[1]).Children[2], typeof(Sql));
				Assert.IsTrue(((Scope)root.Children[1]).Children[2].Parent == root.Children[1]);
				Assert.AreEqual(" AND ", ((Sql)((Scope)root.Children[1]).Children[2]).Text);

				Assert.IsInstanceOfType(((Scope)root.Children[1]).Children[3], typeof(SqlComment));
				Assert.IsTrue(((Scope)root.Children[1]).Children[3].Parent == root.Children[1]);
				Assert.IsInstanceOfType(((ContentElement)((Scope)root.Children[1]).Children[3]).Content, typeof(ContentText));
				Assert.AreEqual("AN SQL COMMENT", ((ContentText)((ContentElement)((Scope)root.Children[1]).Children[3]).Content).Text);

				Assert.IsInstanceOfType(((Scope)root.Children[1]).Children[5], typeof(SqlComment));
				Assert.IsTrue(((Scope)root.Children[1]).Children[5].Parent == root.Children[1]);
				Assert.IsInstanceOfType(((ContentElement)((Scope)root.Children[1]).Children[5]).Content, typeof(ContentText));
				Assert.AreEqual("OR A\r\nMULTILINE\r\nSQL COMMENT", ((ContentText)((ContentElement)((Scope)root.Children[1]).Children[5]).Content).Text);

			}

			[TestMethod]
			public void ParserTest_Lexing_BindVariables()
			{
				var syntax = "SELECT * FROM [My Table] {WHERE {Column1 :someParameter} {Column2 @some_Parameter2} {Column3 ?some_Parameter3 AND [My Table].Column2 > 1}}";			

				var root = new Lexer { Hints = LexerHints.UseBindVarSyntaxForParams }.Process(syntax);

				var nesting = 0;
				OutputLexerResults(root, ref nesting);

				Assert.IsNull(root.ClosingTag);
				Assert.IsNull(root.OpeningTag);
				Assert.IsNull(root.Parent);

				Assert.IsInstanceOfType(root.Children[0], typeof(Sql));
				Assert.IsTrue(((Sql)root.Children[0]).Parent == root);
				Assert.AreEqual(((Sql)root.Children[0]).Text, "SELECT * FROM [My Table] ");

				Assert.IsInstanceOfType(root.Children[1], typeof(Scope));
				Assert.IsTrue(((Scope)root.Children[1]).Parent == root);
				Assert.AreEqual(6, ((Scope)root.Children[1]).Children.Count);
				Assert.AreEqual("{", ((Scope)root.Children[1]).OpeningTag);
				Assert.AreEqual("}", ((Scope)root.Children[1]).ClosingTag);

				Assert.IsInstanceOfType(((Scope)root.Children[1]).Children[0], typeof(Sql));
				Assert.IsTrue(((Scope)root.Children[1]).Children[0].Parent == root.Children[1]);
				Assert.AreEqual("WHERE ", ((Sql)((Scope)root.Children[1]).Children[0]).Text);

				Assert.IsInstanceOfType(((Scope)root.Children[1]).Children[1], typeof(Scope));
				Assert.IsTrue(((Scope)root.Children[1]).Children[1].Parent == root.Children[1]);
				Assert.AreEqual(2, ((Scope)((Scope)root.Children[1]).Children[1]).Children.Count);

				Assert.IsInstanceOfType(((Scope)((Scope)root.Children[1]).Children[1]).Children[0], typeof(Sql));
				Assert.IsTrue(((Scope)((Scope)root.Children[1]).Children[1]).Children[0].Parent == (Scope)((Scope)root.Children[1]).Children[1]);
				Assert.AreEqual("Column1 ", ((Sql)((Scope)((Scope)root.Children[1]).Children[1]).Children[0]).Text);

				Assert.IsInstanceOfType(((Scope)((Scope)root.Children[1]).Children[1]).Children[1], typeof(BindVariableParameter));
				Assert.AreEqual(":", ((ScopedElement)((Scope)((Scope)root.Children[1]).Children[1]).Children[1]).OpeningTag);
				Assert.AreEqual("someParameter", ((ContentText)((Parameter)((Scope)((Scope)root.Children[1]).Children[1]).Children[1]).Content).Text);

				Assert.IsInstanceOfType(((Scope)root.Children[1]).Children[3], typeof(Scope));
				Assert.IsTrue(((Scope)root.Children[1]).Children[3].Parent == root.Children[1]);
				Assert.AreEqual(2, ((Scope)((Scope)root.Children[1]).Children[3]).Children.Count);

				Assert.IsInstanceOfType(((Scope)((Scope)root.Children[1]).Children[3]).Children[0], typeof(Sql));
				Assert.IsTrue(((Scope)((Scope)root.Children[1]).Children[3]).Children[0].Parent == (Scope)((Scope)root.Children[1]).Children[3]);
				Assert.AreEqual("Column2 ", ((Sql)((Scope)((Scope)root.Children[1]).Children[3]).Children[0]).Text);

				Assert.IsInstanceOfType(((Scope)((Scope)root.Children[1]).Children[3]).Children[1], typeof(BindVariableParameter));
				Assert.AreEqual("@", ((ScopedElement)((Scope)((Scope)root.Children[1]).Children[3]).Children[1]).OpeningTag);
				Assert.AreEqual("some_Parameter2", ((ContentText)((Parameter)((Scope)((Scope)root.Children[1]).Children[3]).Children[1]).Content).Text);

				Assert.IsInstanceOfType(((Scope)root.Children[1]).Children[5], typeof(Scope));
				Assert.IsTrue(((Scope)root.Children[1]).Children[5].Parent == root.Children[1]);
				Assert.AreEqual(3, ((Scope)((Scope)root.Children[1]).Children[5]).Children.Count);

				Assert.IsInstanceOfType(((Scope)((Scope)root.Children[1]).Children[5]).Children[0], typeof(Sql));
				Assert.IsTrue(((Scope)((Scope)root.Children[1]).Children[5]).Children[0].Parent == (Scope)((Scope)root.Children[1]).Children[5]);
				Assert.AreEqual("Column3 ", ((Sql)((Scope)((Scope)root.Children[1]).Children[5]).Children[0]).Text);

				Assert.IsInstanceOfType(((Scope)((Scope)root.Children[1]).Children[5]).Children[1], typeof(BindVariableParameter));
				Assert.AreEqual("?", ((ScopedElement)((Scope)((Scope)root.Children[1]).Children[5]).Children[1]).OpeningTag);
				Assert.AreEqual("some_Parameter3", ((ContentText)((Parameter)((Scope)((Scope)root.Children[1]).Children[5]).Children[1]).Content).Text);

				Assert.IsInstanceOfType(((Scope)((Scope)root.Children[1]).Children[5]).Children[2], typeof(Sql));
				Assert.IsTrue(((Scope)((Scope)root.Children[1]).Children[5]).Children[2].Parent == (Scope)((Scope)root.Children[1]).Children[5]);
				Assert.AreEqual(" AND [My Table].Column2 > 1", ((Sql)((Scope)((Scope)root.Children[1]).Children[5]).Children[2]).Text);
			}

			private static void OutputLexerResults(Element element, ref int nesting)
			{
				Debug.WriteLine(new string('\t', nesting) + element);
				if (element is NestedElement nestedElement)
				{
					nesting++;
					foreach (var childElement in nestedElement.Children)
						OutputLexerResults(childElement, ref nesting);
					nesting--;
				}
			}

			public TestContext TestContext { get; set; }

			[TestMethod]
			public void PerformanceTest_1()
			{
				var syntax = "SELECT * FROM TEST {WHERE SOMETHING LIKE 'Test' AND {SomethingElse [somethingElse]} {SomethingThird [somethingThird]}};";
				var parser2 = new Lexer();

				parser2.Process(syntax);
				var sw = new Stopwatch();
				sw.Start();
				for (var i = 0; i < 1000; i++)
					parser2.Process(syntax);
				sw.Stop();
				TestContext.WriteLine("Elap1: " + sw.Elapsed.TotalMilliseconds);

				Parsing.Parser parser = new Parsing.Parser();
				parser.Parse(syntax);				
				sw.Restart();
				for (var i = 0; i < 1000; i++)
					parser.Parse(syntax);
				sw.Stop();
				TestContext.WriteLine("Elap2: " + sw.Elapsed.TotalMilliseconds);				
			}


			[TestMethod]
			public void Comments_1()
			{
				var withoutComments = "SELECT * FROM TABLE1 WHERE TABLE1.COLUMN1 = 123";
				var withComments = "SELECT * FROM TABLE1/*{, TABLE2}*/ WHERE TABLE1.COLUMN1 = 123/*{ AND TABLE2.COLUMN1 = TABLE1.COLUMN1 {AND {TABLE1.COLUMN1 [SomeCriteria]}}}*/";

				AssertCommand(new MockQuery(_connection, withComments).CreateCommand(), withoutComments);
			}

			[TestMethod]
			public void Comments_2()
			{
				var withoutComments = "SELECT * FROM TABLE1";
				var withComments = "/*{ We're testing multiline \n" +
				                   " * comments here. \n" +
				                   " * They should work fine }*/ \n" +
				                   "SELECT * FROM TABLE1";

				AssertCommand(new MockQuery(_connection, withComments).CreateCommand(), withoutComments);
			}

			[TestMethod]
			public void Escape_Strings_1()
			{
				// Literals should be ignored entirely
				var sql = "SELECT * FROM TABLE1 WHERE COLUMN1 = 'This is some {literal text} that includes @{special characters} like [this] or [[this]] or \"{ this maybe }\".'";

				AssertCommand(new MockQuery(_connection, sql).CreateCommand(), sql);
			}

			[TestMethod]
			public void Escape_Strings_2()
			{
				// Escaping curly braces with dollar sign
				var sql = "SELECT * FROM TABLE1 WHERE COLUMN1 = 123 /* This comment should include {this scope}. */";
				var expected = "SELECT * FROM TABLE1 WHERE COLUMN1 = 123 /* This comment should include {this scope}. */";

				AssertCommand(new MockQuery(_connection, sql).CreateCommand(), expected);
			}

			[TestMethod]
			public void Escape_Strings_3()
			{
				// Escaping square brackets with dollar sign, a potentially common scenario in OLEDB queries
				var sql = "SELECT * FROM TABLE1 {WHERE {[[Some Column 1]] [SomeCriteria1]} {[[Some Column 2]] [SomeCriteria2]} {[[Some Column 3]] [SomeCriteria3]}}";
				var expected = "SELECT * FROM TABLE1 WHERE [Some Column 1] = :pSomeCriteria1_1 AND [Some Column 3] = :pSomeCriteria3_1";
				
				var query = new MockQuery(_connection, sql);
				query.ThrowScriptErrorException = true;

				query.SetCondition("SomeCriteria1", 123);
				query.SetCondition("SomeCriteria3", 456);

				AssertCommand(query.CreateCommand(), expected);
			}

			private void AssertCommand(IDbCommand cmd, string expectedSql)
			{
				Assert.IsNotNull(cmd);
				Assert.IsTrue(cmd.CommandType == CommandType.Text);
				Assert.AreEqual(expectedSql, cmd.CommandText);
			}
		}
	}
}
