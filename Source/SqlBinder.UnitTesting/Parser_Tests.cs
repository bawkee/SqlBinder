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
			public void SimpleTest_1()
			{
				var syntax = "SELECT * FROM TEST {WHERE SOMETHING LIKE 'Test' AND {SomethingElse [somethingElse]} {SomethingThird [somethingThird]}}";
				var parser = new Parser();
				var root = parser.Parse(syntax);
				var nesting = 0;
				OutputLexerResults(root, ref nesting);
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
				var syntax = "SELECT * FROM TEST {WHERE SOMETHING LIKE 'Test' AND {SomethingElse [somethingElse]} {SomethingThird [somethingThird]}}";
				var parser2 = new Parser();

				parser2.Parse(syntax);
				var sw = new Stopwatch();
				sw.Start();
				for (var i = 0; i < 1000; i++)
					parser2.Parse(syntax);
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
