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
			public void LexerTest_1()
			{
				var syntax = "SELECT * FROM TEST {WHERE SOMETHING LIKE 'Test' AND {SomethingElse [somethingElse]} {SomethingThird [somethingThird]}};";

				var root = new Lexer().Process(syntax);

				var nesting = 0;
				OutputLexerResults(root, ref nesting);

				Assert.IsNull(root.ClosingTag);
				Assert.IsNull(root.OpeningTag);
				Assert.IsNull(root.Parent);
				Assert.AreEqual(3, root.Children.Count);
				
				Assert.IsInstanceOfType(root.Children[0], typeof(Sql));
				Assert.IsTrue(((Sql) root.Children[0]).Parent == root);
				Assert.AreEqual(((Sql) root.Children[0]).Text.ToString(), "SELECT * FROM TEST ");

				Assert.IsInstanceOfType(root.Children[1], typeof(Scope));
				Assert.IsTrue(((Scope) root.Children[1]).Parent == root);
				Assert.IsTrue(((Scope) root.Children[1]).Children.Count == 6);
				Assert.IsTrue(((Scope) root.Children[1]).OpeningTag == "{");
				Assert.IsTrue(((Scope) root.Children[1]).ClosingTag == "}");

				Assert.IsInstanceOfType(((Scope)root.Children[1]).Children[0], typeof(Sql));
				Assert.IsTrue(((Scope)root.Children[1]).Children[0].Parent == root.Children[1]);
				Assert.AreEqual(((Sql)((Scope)root.Children[1]).Children[0]).Text.ToString(), "WHERE SOMETHING LIKE ");

				Assert.IsInstanceOfType( ((Scope)root.Children[1]).Children[1], typeof(SingleQuoteLiteral));
				Assert.IsTrue(((Scope)root.Children[1]).Children[1].Parent == root.Children[1]);
				Assert.IsInstanceOfType(((ContentElement) ((Scope) root.Children[1]).Children[1]).Content, typeof(ContentText));
				Assert.AreEqual(((ScopedElement) ((Scope) root.Children[1]).Children[1]).OpeningTag, "'");
				Assert.AreEqual(((ScopedElement)((Scope)root.Children[1]).Children[1]).ClosingTag, "'");
				Assert.AreEqual(((ContentText) ((ContentElement) ((Scope) root.Children[1]).Children[1]).Content).Text.ToString(), "Test");

				Assert.IsInstanceOfType(((Scope)root.Children[1]).Children[2], typeof(Sql));
				Assert.IsTrue(((Scope)root.Children[1]).Children[2].Parent == root.Children[1]);
				Assert.AreEqual(((Sql)((Scope)root.Children[1]).Children[2]).Text.ToString(), " AND ");

				Assert.IsInstanceOfType(((Scope)root.Children[1]).Children[3], typeof(Scope));
				Assert.IsTrue(((Scope)root.Children[1]).Children[3].Parent == root.Children[1]);
				Assert.IsTrue(((Scope)((Scope)root.Children[1]).Children[3]).Children.Count == 2);

				Assert.IsInstanceOfType(((Scope) ((Scope) root.Children[1]).Children[3]).Children[0], typeof(Sql));
				Assert.IsTrue(((Scope) ((Scope) root.Children[1]).Children[3]).Children[0].Parent == (Scope) ((Scope) root.Children[1]).Children[3]);
				Assert.AreEqual(((Sql) ((Scope) ((Scope) root.Children[1]).Children[3]).Children[0]).Text.ToString(), "SomethingElse ");

				Assert.IsInstanceOfType(((Scope)((Scope)root.Children[1]).Children[3]).Children[1], typeof(Parameter));
				Assert.AreEqual(((ScopedElement) ((Scope) ((Scope) root.Children[1]).Children[3]).Children[1]).OpeningTag, "[");
				Assert.AreEqual(((ScopedElement) ((Scope) ((Scope) root.Children[1]).Children[3]).Children[1]).ClosingTag, "]");
				Assert.AreEqual(((ContentText) ((Parameter) ((Scope) ((Scope) root.Children[1]).Children[3]).Children[1]).Content).Text.ToString(), "somethingElse");

				Assert.IsInstanceOfType(((Scope)root.Children[1]).Children[4], typeof(ScopeSeparator));
				Assert.IsTrue(((Scope)root.Children[1]).Children[4].Parent == root.Children[1]);
				Assert.AreEqual(((ScopeSeparator)((Scope)root.Children[1]).Children[4]).Text.ToString(), " ");

				Assert.IsInstanceOfType(((Scope)root.Children[1]).Children[5], typeof(Scope));
				Assert.IsTrue(((Scope)root.Children[1]).Children[5].Parent == root.Children[1]);
				Assert.IsTrue(((Scope)((Scope)root.Children[1]).Children[5]).Children.Count == 2);

				Assert.IsInstanceOfType(((Scope)((Scope)root.Children[1]).Children[5]).Children[0], typeof(Sql));
				Assert.IsTrue(((Scope)((Scope)root.Children[1]).Children[5]).Children[0].Parent == (Scope)((Scope)root.Children[1]).Children[5]);
				Assert.AreEqual(((Sql)((Scope)((Scope)root.Children[1]).Children[5]).Children[0]).Text.ToString(), "SomethingThird ");

				Assert.IsInstanceOfType(((Scope)((Scope)root.Children[1]).Children[5]).Children[1], typeof(Parameter));
				Assert.AreEqual(((ScopedElement)((Scope)((Scope)root.Children[1]).Children[5]).Children[1]).OpeningTag, "[");
				Assert.AreEqual(((ScopedElement)((Scope)((Scope)root.Children[1]).Children[5]).Children[1]).ClosingTag, "]");
				Assert.AreEqual(((ContentText)((Parameter)((Scope)((Scope)root.Children[1]).Children[5]).Children[1]).Content).Text.ToString(), "somethingThird");

				Assert.IsInstanceOfType(root.Children[2], typeof(Sql));
				Assert.IsTrue(((Sql)root.Children[2]).Parent == root);
				Assert.AreEqual(((Sql)root.Children[2]).Text.ToString(), ";");
			}

			[TestMethod]
			public void LexerTest_2()
			{
				var syntax = "SELECT * FROM TEST {WHERE SOMETHING LIKE '{Test}' OR SOMETHING LIKE \"Some {More} tests\" OR \"'This'\" OR \"Something 'like' this\" OR '\"This\"' OR 'Something \"like\" this'}";

				var root = new Lexer().Process(syntax);

				var nesting = 0;
				OutputLexerResults(root, ref nesting);

				Assert.IsNull(root.ClosingTag);
				Assert.IsNull(root.OpeningTag);
				Assert.IsNull(root.Parent);
				Assert.IsTrue(root.Children.Count == 2);

				Assert.IsInstanceOfType(root.Children[0], typeof(Sql));
				Assert.IsTrue(((Sql)root.Children[0]).Parent == root);
				Assert.AreEqual(((Sql)root.Children[0]).Text.ToString(), "SELECT * FROM TEST ");

				Assert.IsInstanceOfType(root.Children[1], typeof(Scope));
				Assert.IsTrue(((Scope)root.Children[1]).Parent == root);
				//Assert.IsTrue(((Scope)root.Children[1]).Children.Count == 4);
				Assert.IsTrue(((Scope)root.Children[1]).OpeningTag == "{");
				Assert.IsTrue(((Scope)root.Children[1]).ClosingTag == "}");

				Assert.IsInstanceOfType(((Scope)root.Children[1]).Children[0], typeof(Sql));
				Assert.IsTrue(((Scope)root.Children[1]).Children[0].Parent == root.Children[1]);
				Assert.AreEqual(((Sql)((Scope)root.Children[1]).Children[0]).Text.ToString(), "WHERE SOMETHING LIKE ");

				Assert.IsInstanceOfType(((Scope)root.Children[1]).Children[1], typeof(SingleQuoteLiteral));
				Assert.IsTrue(((Scope)root.Children[1]).Children[1].Parent == root.Children[1]);
				Assert.IsInstanceOfType(((ContentElement)((Scope)root.Children[1]).Children[1]).Content, typeof(ContentText));
				Assert.AreEqual(((ScopedElement)((Scope)root.Children[1]).Children[1]).OpeningTag, "'");
				Assert.AreEqual(((ScopedElement)((Scope)root.Children[1]).Children[1]).ClosingTag, "'");
				Assert.AreEqual(((ContentText)((ContentElement)((Scope)root.Children[1]).Children[1]).Content).Text.ToString(), "{Test}");

				Assert.IsInstanceOfType(((Scope)root.Children[1]).Children[2], typeof(Sql));
				Assert.IsTrue(((Scope)root.Children[1]).Children[2].Parent == root.Children[1]);
				Assert.AreEqual(((Sql)((Scope)root.Children[1]).Children[2]).Text.ToString(), " OR SOMETHING LIKE ");

				Assert.IsInstanceOfType(((Scope)root.Children[1]).Children[3], typeof(DoubleQuoteLiteral));
				Assert.IsTrue(((Scope)root.Children[1]).Children[3].Parent == root.Children[1]);
				Assert.IsInstanceOfType(((ContentElement)((Scope)root.Children[1]).Children[3]).Content, typeof(ContentText));
				Assert.AreEqual(((ScopedElement)((Scope)root.Children[1]).Children[3]).OpeningTag, "\"");
				Assert.AreEqual(((ScopedElement)((Scope)root.Children[1]).Children[3]).ClosingTag, "\"");
				Assert.AreEqual(((ContentText)((ContentElement)((Scope)root.Children[1]).Children[3]).Content).Text.ToString(), "Some {More} tests");

				Assert.IsInstanceOfType(((Scope)root.Children[1]).Children[4], typeof(Sql));
				Assert.IsTrue(((Scope)root.Children[1]).Children[4].Parent == root.Children[1]);
				Assert.AreEqual(((Sql)((Scope)root.Children[1]).Children[4]).Text.ToString(), " OR ");

				Assert.IsInstanceOfType(((Scope)root.Children[1]).Children[5], typeof(DoubleQuoteLiteral));
				Assert.IsTrue(((Scope)root.Children[1]).Children[5].Parent == root.Children[1]);
				Assert.IsInstanceOfType(((ContentElement)((Scope)root.Children[1]).Children[5]).Content, typeof(ContentText));
				Assert.AreEqual(((ScopedElement)((Scope)root.Children[1]).Children[5]).OpeningTag, "\"");
				Assert.AreEqual(((ScopedElement)((Scope)root.Children[1]).Children[5]).ClosingTag, "\"");
				Assert.AreEqual(((ContentText)((ContentElement)((Scope)root.Children[1]).Children[5]).Content).Text.ToString(), "'This'");

				Assert.IsInstanceOfType(((Scope)root.Children[1]).Children[6], typeof(Sql));
				Assert.IsTrue(((Scope)root.Children[1]).Children[6].Parent == root.Children[1]);
				Assert.AreEqual(((Sql)((Scope)root.Children[1]).Children[6]).Text.ToString(), " OR ");

				Assert.IsInstanceOfType(((Scope)root.Children[1]).Children[7], typeof(DoubleQuoteLiteral));
				Assert.IsTrue(((Scope)root.Children[1]).Children[7].Parent == root.Children[1]);
				Assert.IsInstanceOfType(((ContentElement)((Scope)root.Children[1]).Children[7]).Content, typeof(ContentText));
				Assert.AreEqual(((ScopedElement)((Scope)root.Children[1]).Children[7]).OpeningTag, "\"");
				Assert.AreEqual(((ScopedElement)((Scope)root.Children[1]).Children[7]).ClosingTag, "\"");
				Assert.AreEqual(((ContentText)((ContentElement)((Scope)root.Children[1]).Children[7]).Content).Text.ToString(), "Something 'like' this");

				Assert.IsInstanceOfType(((Scope)root.Children[1]).Children[8], typeof(Sql));
				Assert.IsTrue(((Scope)root.Children[1]).Children[8].Parent == root.Children[1]);
				Assert.AreEqual(((Sql)((Scope)root.Children[1]).Children[8]).Text.ToString(), " OR ");

				Assert.IsInstanceOfType(((Scope)root.Children[1]).Children[9], typeof(SingleQuoteLiteral));
				Assert.IsTrue(((Scope)root.Children[1]).Children[9].Parent == root.Children[1]);
				Assert.IsInstanceOfType(((ContentElement)((Scope)root.Children[1]).Children[9]).Content, typeof(ContentText));
				Assert.AreEqual(((ScopedElement)((Scope)root.Children[1]).Children[9]).OpeningTag, "'");
				Assert.AreEqual(((ScopedElement)((Scope)root.Children[1]).Children[9]).ClosingTag, "'");
				Assert.AreEqual(((ContentText)((ContentElement)((Scope)root.Children[1]).Children[9]).Content).Text.ToString(), "\"This\"");

				Assert.IsInstanceOfType(((Scope)root.Children[1]).Children[10], typeof(Sql));
				Assert.IsTrue(((Scope)root.Children[1]).Children[10].Parent == root.Children[1]);
				Assert.AreEqual(((Sql)((Scope)root.Children[1]).Children[10]).Text.ToString(), " OR ");

				Assert.IsInstanceOfType(((Scope)root.Children[1]).Children[11], typeof(SingleQuoteLiteral));
				Assert.IsTrue(((Scope)root.Children[1]).Children[11].Parent == root.Children[1]);
				Assert.IsInstanceOfType(((ContentElement)((Scope)root.Children[1]).Children[11]).Content, typeof(ContentText));
				Assert.AreEqual(((ScopedElement)((Scope)root.Children[1]).Children[11]).OpeningTag, "'");
				Assert.AreEqual(((ScopedElement)((Scope)root.Children[1]).Children[11]).ClosingTag, "'");
				Assert.AreEqual(((ContentText)((ContentElement)((Scope)root.Children[1]).Children[11]).Content).Text.ToString(), "Something \"like\" this");
			}

			private void OutputLexerResults(Element element, ref int nesting)
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

				Debug.WriteLine("Elap1: " + sw.Elapsed.TotalMilliseconds);

				SqlBinder.Parsing.Parser parser = new Parsing.Parser();
				parser.Parse(syntax);				
				sw.Restart();
				for (var i = 0; i < 1000; i++)
					parser.Parse(syntax);
				sw.Stop();

				Debug.WriteLine("Elap2: " + sw.Elapsed.TotalMilliseconds);				
			}

			[TestMethod]
			public void PerformanceTest_2()
			{
				var syntax = "SELECT * FROM TEST {WHERE SOMETHING LIKE 'Test' AND {SomethingElse [somethingElse]} {SomethingThird [somethingThird]}}";


			}


			[TestMethod]
			public void Comments_1()
			{
				var withoutComments = "SELECT * FROM TABLE1 WHERE TABLE1.COLUMN1 = 123";
				var withComments = "SELECT * FROM TABLE1{*, TABLE2*} WHERE TABLE1.COLUMN1 = 123{* AND TABLE2.COLUMN1 = TABLE1.COLUMN1 {AND {TABLE1.COLUMN1 [SomeCriteria]}}*}";

				AssertCommand(new MockQuery(_connection, withComments).CreateCommand(), withoutComments);
			}

			[TestMethod]
			public void Comments_2()
			{
				var withoutComments = "SELECT * FROM TABLE1";
				var withComments = "{* We're testing multiline \n" +
				                   " * comments here. \n" +
				                   " * They should work fine *} \n" +
				                   "SELECT * FROM TABLE1";

				AssertCommand(new MockQuery(_connection, withComments).CreateCommand(), withoutComments);
			}

			[TestMethod]
			public void Escape_Strings_1()
			{
				// Literals should be ignored entirely
				var sql = "SELECT * FROM TABLE1 WHERE COLUMN1 = 'This is some {literal text} that includes @{special characters} like [this] or $[this]$ or ${ this maybe }$.'";

				AssertCommand(new MockQuery(_connection, sql).CreateCommand(), sql);
			}

			[TestMethod]
			public void Escape_Strings_2()
			{
				// Escaping curly braces with dollar sign
				var sql = "SELECT * FROM TABLE1 WHERE COLUMN1 = 123 /* This comment should include ${this scope}$ because it was escaped. */";
				var expected = "SELECT * FROM TABLE1 WHERE COLUMN1 = 123 /* This comment should include {this scope} because it was escaped. */";

				AssertCommand(new MockQuery(_connection, sql).CreateCommand(), expected);
			}

			[TestMethod]
			public void Escape_Strings_3()
			{
				// Escaping square brackets with dollar sign, a potentially common scenario in OLEDB queries
				var sql = "SELECT * FROM TABLE1 {WHERE {$[Some Column 1]$ [SomeCriteria1]} {$[Some Column 2]$ [SomeCriteria2]} {$[Some Column 3]$ [SomeCriteria3]}}";
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
