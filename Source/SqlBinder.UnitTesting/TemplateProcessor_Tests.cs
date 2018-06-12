using System.Data;
using System.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace SqlBinder.UnitTesting
{
	public partial class SqlBinder_Tests
	{
		private static MockDbConnection _connection = new MockDbConnection();

		/// <summary>
		/// Tests that should all produce an exact same, simple, parameterless command with a simplest possible sql. The aim of this test is to confirm
		/// the most low level requirements work.
		/// </summary>
		[TestClass]
		public class TemplateProcessor_Tests
		{
			private static string _expectedSql = "SELECT * FROM TABLE1";
			private static string _expectedSqlComment = "/* Test comment */";

			public TestContext TestContext { get; set; }

			[TestInitialize]
			public void InitializeTest()
			{
				//
			}

			[TestMethod]
			public void Basic_Sql()
			{
				AssertCommand(new MockQuery(_connection, "SELECT * FROM TABLE1").CreateCommand());
			}

			[TestMethod]
			public void Junk_Scopes()
			{
				AssertCommand(new MockQuery(_connection, "SELECT * FROM TABLE1 {JUNK ON THE RIGHT}").CreateCommand());
				AssertCommand(new MockQuery(_connection, "SELECT * FROM TABLE1 {JUNK ON THE RIGHT {NESTED JUNK}}").CreateCommand());
				AssertCommand(new MockQuery(_connection, "{JUNK ON THE LEFT} SELECT * FROM TABLE1").CreateCommand());
				AssertCommand(new MockQuery(_connection, "{} SELECT {}* FROM TABLE1 {}").CreateCommand());
				AssertCommand(new MockQuery(_connection, "SELECT{} * FROM TABLE1").CreateCommand());
				AssertCommand(new MockQuery(_connection, "{JUNK ON THE LEFT {NESTED JUNK}} SELECT * FROM TABLE1 {JUNK ON THE RIGHT {NESTED JUNK}}").CreateCommand());
				AssertCommand(new MockQuery(_connection, "{JUNK ON THE LEFT} SELECT * {JUNK ON THE MIDDLE}FROM TABLE1 {JUNK ON THE RIGHT {NESTED JUNK}}").CreateCommand());
				AssertCommand(new MockQuery(_connection, "{JUNK ON THE LEFT} SELECT * {MIDDLE JUNK 1} {MIDDLE JUNK 2}   {MIDDLE JUNK 3}FROM TABLE1 {JUNK ON THE RIGHT {NESTED JUNK}}").CreateCommand());
				AssertCommand(new MockQuery(_connection, "{JUNK: ~!@#$%^&*()_+~<>?,./;:}SELECT * FROM TABLE1 {JUNK ON THE RIGHT}").CreateCommand());
			}

			[TestMethod]
			public void Junk_Scopes_With_Sql_Comments()
			{
				// Sql comments should remain but not hinder anything
				AssertCommand(new MockQuery(_connection, "/* Test comment */SELECT * FROM TABLE1 {JUNK ON THE RIGHT}").CreateCommand());
				AssertCommand(new MockQuery(_connection, "SELECT * FROM TABLE1 {/* Test comment */JUNK ON THE RIGHT {NESTED/* Test comment */ JUNK}}").CreateCommand());
				AssertCommand(new MockQuery(_connection, "{JUNK ON THE LEFT {NESTED JUNK/* Test comment */}} SELECT * FROM TABLE1 {JUNK ON THE RIGHT {NESTED JUNK}}").CreateCommand());
				AssertCommand(new MockQuery(_connection, "{JUNK ON THE LEFT} /* Test comment */SELECT * {/* Test comment */JUNK ON THE MIDDLE/* Test comment */}FROM TABLE1 {JUNK ON THE RIGHT {NESTED JUNK}}").CreateCommand());
			}

			[TestMethod]
			public void Junk_Scopes_With_SqlBinder_Comments()
			{
				// SqlBinder comments should be removed. They cannot be nested. They contain any characters.
				AssertCommand(new MockQuery(_connection, "SELECT * FROM TABLE1 {JUNK ON THE RIGHT {NESTED JUNK/*{ Test comment 3 }*/}/*{ Test comment 4 }*/}").CreateCommand());
				AssertCommand(new MockQuery(_connection, "{JUNK ON /*{ Test comment }*/THE LEFT} SELECT * FROM TABLE1 /*{ Test comment }*/").CreateCommand());
				AssertCommand(new MockQuery(_connection, "/*{ Test comment {Curly braces junk} }*/{JUNK ON THE LEFT {NESTED JUNK}} SELECT * FROM TABLE1 {JUNK ON THE RIGHT {NESTED JUNK}}").CreateCommand());				
				AssertCommand(new MockQuery(_connection, "{JUNK ON THE LEFT} /*{ Special characters:[asdf][[[]}}}}~@#$%^&*()_+:'<>,./? }*/SELECT * {JUNK ON THE MIDDLE}FROM TABLE1 {JUNK ON THE RIGHT {NESTED JUNK}}").CreateCommand());				
			}

			[TestMethod]
			public void Junk_Scopes_With_Parameters()
			{				
				AssertCommand(new MockQuery(_connection, "SELECT * FROM TABLE1 {JUNK ON THE RIGHT}").CreateCommand());
				AssertCommand(new MockQuery(_connection, "SELECT * FROM TABLE1 {JUNK ON THE RIGHT {NESTED JUNK [criteria1]}}").CreateCommand());
				AssertCommand(new MockQuery(_connection, "{JUNK ON THE LEFT} SELECT * FROM TABLE1").CreateCommand());
				AssertCommand(new MockQuery(_connection, "{JUNK ON THE LEFT {NESTED JUNK [criteria1]}} SELECT * FROM TABLE1 {JUNK ON THE RIGHT {NESTED JUNK [criteria2]}}").CreateCommand());
				AssertCommand(new MockQuery(_connection, "{JUNK ON THE LEFT [criteria1]} SELECT * {JUNK ON THE MIDDLE}FROM TABLE1 {JUNK ON THE RIGHT {NESTED JUNK [criteria2]}}").CreateCommand());			
			}

			[TestMethod]
			public void Basic_Literals()
			{
				var sql = "SELECT * FROM TABLE1 WHERE COLUMN1 = 'This is some {literal text} that includes @{special characters} like [this] or [[this]] or \"{ this maybe }\".'";				
				AssertCommand(new MockQuery(_connection, sql).CreateCommand(), sql);

				sql = "SELECT * FROM TABLE1 WHERE COLUMN1 = ''";
				AssertCommand(new MockQuery(_connection, sql).CreateCommand(), sql);

				sql = "SELECT * FROM TABLE1 WHERE COLUMN1 = '' OR COLUMN2 = ''";
				AssertCommand(new MockQuery(_connection, sql).CreateCommand(), sql);

				sql = "SELECT * FROM TABLE1 WHERE COLUMN1 = \"\" OR COLUMN2 = \"\"";
				AssertCommand(new MockQuery(_connection, sql).CreateCommand(), sql);

				sql = "SELECT * FROM TABLE1 WHERE COLUMN1 = $$$$ OR COLUMN2 = $$ $$ OR COLUMN3 = $${test}$$";
				AssertCommand(new MockQuery(_connection, sql).CreateCommand(), sql);
			}

			[TestMethod]
			public void Basic_Comments()
			{
				// Escaping curly braces with dollar sign
				var sql = "SELECT * FROM TABLE1 WHERE COLUMN1 = 123 /* This \"comment\" 'should' include {this scope} or [this]. */";
				AssertCommand(new MockQuery(_connection, sql).CreateCommand(), sql);
			}

			[TestMethod]
			public void Escape_Strings()
			{
				// Escaping square brackets with dollar sign, a potentially common scenario in OLEDB queries
				var sql = "SELECT * FROM TABLE1 {WHERE {[[Some Column 1]] [SomeCriteria1]} {[[Some Column 2]] [SomeCriteria2]} {[[Some Column 3]] [SomeCriteria3]}}";
				var expected = "SELECT * FROM TABLE1 WHERE [Some Column 1] = :pSomeCriteria1_1 AND [Some Column 3] = :pSomeCriteria3_1";

				var query = new MockQuery(_connection, sql);

				query.SetCondition("SomeCriteria1", 123);
				query.SetCondition("SomeCriteria3", 456);

				AssertCommand(query.CreateCommand(), expected);
			}

			[TestMethod]
			[ExpectedException(typeof(UnmatchedConditionsException))]
			public void Invalid_Script()
			{
				var query = new MockQuery(_connection, "SELECT * FROM TABLE1");
				query.SetCondition("condition1", 0);
				AssertCommand(query.CreateCommand());
			}

			[TestMethod]
			public void Performance_Tests()
			{
				var query = new Query("SELECT * FROM TABLE1 {WHERE {COLUMN1 :Criteria1} {COLUMN2 :Criteria2} {COLUMN3 :Criteria3} " + 
				                      "{COLUMN4 :Criteria4} {COLUMN5 :Criteria5} {COLUMN6 :Criteria6}}");
				query.ParserHints = Parsing.ParserHints.None;

				var sw = new Stopwatch();

				var c = 1000;

				sw.Start();
				for (var i = 0; i < c; i++)
				{
					query.SqlBinderScript = query.SqlBinderScript; // Reset the cache
					query.GetSql();
				}
				sw.Stop();
				TestContext.WriteLine($"Cold start: {sw.Elapsed.TotalMilliseconds}");

				sw.Restart();
				for (var i = 0; i < c; i++)
				{
					query.GetSql();
				}
				sw.Stop();
				TestContext.WriteLine($"Warm parsing, no condition setters: {sw.Elapsed.TotalMilliseconds}");

				query.Conditions.Clear();
				sw.Restart();
				for (var i = 0; i < c; i++)
				{
					query.SetCondition("Criteria1", new ConditionValues.BoolValue(true));
					query.GetSql();
				}
				sw.Stop();
				TestContext.WriteLine($"Warm parsing, 1 condition: {sw.Elapsed.TotalMilliseconds}");

				query.Conditions.Clear();
				sw.Restart();
				for (var i = 0; i < c; i++)
				{
					query.SetCondition("Criteria1", new ConditionValues.BoolValue(true));
					query.SetCondition("Criteria2", new ConditionValues.BoolValue(true));
					query.GetSql();
				}
				sw.Stop();
				TestContext.WriteLine($"Warm parsing, 2 conditions: {sw.Elapsed.TotalMilliseconds}");

				query.Conditions.Clear();
				sw.Restart();
				for (var i = 0; i < c; i++)
				{
					query.SetCondition("Criteria1", new ConditionValues.BoolValue(true));
					query.SetCondition("Criteria2", new ConditionValues.BoolValue(true));
					query.SetCondition("Criteria3", new ConditionValues.BoolValue(true));
					query.GetSql();
				}
				sw.Stop();
				TestContext.WriteLine($"Warm parsing, 3 conditions: {sw.Elapsed.TotalMilliseconds}");

				query.Conditions.Clear();
				sw.Restart();
				for (var i = 0; i < c; i++)
				{
					query.SetCondition("Criteria1", new ConditionValues.BoolValue(true));
					query.SetCondition("Criteria2", new ConditionValues.BoolValue(true));
					query.SetCondition("Criteria3", new ConditionValues.BoolValue(true));
					query.SetCondition("Criteria4", new ConditionValues.BoolValue(true));
					query.GetSql();
				}
				sw.Stop();
				TestContext.WriteLine($"Warm parsing, 4 conditions: {sw.Elapsed.TotalMilliseconds}");

				query.Conditions.Clear();
				sw.Restart();
				for (var i = 0; i < c; i++)
				{
					query.SetCondition("Criteria1", new ConditionValues.BoolValue(true));
					query.SetCondition("Criteria2", new ConditionValues.BoolValue(true));
					query.SetCondition("Criteria3", new ConditionValues.BoolValue(true));
					query.SetCondition("Criteria4", new ConditionValues.BoolValue(true));
					query.SetCondition("Criteria5", new ConditionValues.BoolValue(true));
					query.GetSql();
				}
				sw.Stop();
				TestContext.WriteLine($"Warm parsing, 5 conditions: {sw.Elapsed.TotalMilliseconds}");

				query.Conditions.Clear();
				sw.Restart();
				for (var i = 0; i < c; i++)
				{
					query.SetCondition("Criteria1", new ConditionValues.BoolValue(true));
					query.SetCondition("Criteria2", new ConditionValues.BoolValue(true));
					query.SetCondition("Criteria3", new ConditionValues.BoolValue(true));
					query.SetCondition("Criteria4", new ConditionValues.BoolValue(true));
					query.SetCondition("Criteria5", new ConditionValues.BoolValue(true));
					query.SetCondition("Criteria6", new ConditionValues.BoolValue(true));
					query.GetSql();
				}
				sw.Stop();
				TestContext.WriteLine($"Warm parsing, 6 conditions: {sw.Elapsed.TotalMilliseconds}");
			}

			[TestMethod]
			public void Separators_Handling()
			{
				var query = new Query("SELECT * FROM TABLE1 {WHERE {COLUMN1 :Criteria1} {COLUMN2 :Criteria2} {COLUMN3 :Criteria3}}");

				query.ParserHints = Parsing.ParserHints.None;

				Assert.AreEqual("SELECT * FROM TABLE1", query.GetSql());

				query.SetCondition("Criteria1", true);
				Assert.AreEqual("SELECT * FROM TABLE1 WHERE COLUMN1 = :pCriteria1_1", query.GetSql());

				query.Conditions.Clear();
				query.SetCondition("Criteria2", true);
				Assert.AreEqual("SELECT * FROM TABLE1 WHERE COLUMN2 = :pCriteria2_1", query.GetSql());

				query.Conditions.Clear();
				query.SetCondition("Criteria3", true);
				Assert.AreEqual("SELECT * FROM TABLE1 WHERE COLUMN3 = :pCriteria3_1", query.GetSql());

				query.Conditions.Clear();
				query.SetCondition("Criteria1", true);
				query.SetCondition("Criteria2", true);
				Assert.AreEqual("SELECT * FROM TABLE1 WHERE COLUMN1 = :pCriteria1_1 AND COLUMN2 = :pCriteria2_1", query.GetSql());

				query.Conditions.Clear();
				query.SetCondition("Criteria2", true);
				query.SetCondition("Criteria3", true);
				Assert.AreEqual("SELECT * FROM TABLE1 WHERE COLUMN2 = :pCriteria2_1 AND COLUMN3 = :pCriteria3_1", query.GetSql());

				query.Conditions.Clear();
				query.SetCondition("Criteria1", true);
				query.SetCondition("Criteria2", true);
				query.SetCondition("Criteria3", true);
				Assert.AreEqual("SELECT * FROM TABLE1 WHERE COLUMN1 = :pCriteria1_1 AND COLUMN2 = :pCriteria2_1 AND COLUMN3 = :pCriteria3_1", query.GetSql());

				query.Conditions.Clear();
				query.SetCondition("Criteria1", true);
				query.SetCondition("Criteria3", true);
				Assert.AreEqual("SELECT * FROM TABLE1 WHERE COLUMN1 = :pCriteria1_1 AND COLUMN3 = :pCriteria3_1", query.GetSql());
			}

			[TestMethod]
			public void Separators_Blanks()
			{
				var query = new Query("SELECT * FROM TABLE1 {WHERE {COLUMN1 :Criteria1} +{COLUMN2 :Criteria2} {COLUMN3 :Criteria3}}");

				query.ParserHints = Parsing.ParserHints.None;

				Assert.AreEqual("SELECT * FROM TABLE1", query.GetSql());

				query.SetCondition("Criteria1", true);
				Assert.AreEqual("SELECT * FROM TABLE1 WHERE COLUMN1 = :pCriteria1_1", query.GetSql());

				query.Conditions.Clear();
				query.SetCondition("Criteria2", true);
				Assert.AreEqual("SELECT * FROM TABLE1 WHERE COLUMN2 = :pCriteria2_1", query.GetSql());

				query.Conditions.Clear();
				query.SetCondition("Criteria3", true);
				Assert.AreEqual("SELECT * FROM TABLE1 WHERE COLUMN3 = :pCriteria3_1", query.GetSql());

				query.Conditions.Clear();
				query.SetCondition("Criteria1", true);
				query.SetCondition("Criteria2", true);
				Assert.AreEqual("SELECT * FROM TABLE1 WHERE COLUMN1 = :pCriteria1_1 COLUMN2 = :pCriteria2_1", query.GetSql());

				query.Conditions.Clear();
				query.SetCondition("Criteria2", true);
				query.SetCondition("Criteria3", true);
				Assert.AreEqual("SELECT * FROM TABLE1 WHERE COLUMN2 = :pCriteria2_1 AND COLUMN3 = :pCriteria3_1", query.GetSql());

				query.Conditions.Clear();
				query.SetCondition("Criteria1", true);
				query.SetCondition("Criteria2", true);
				query.SetCondition("Criteria3", true);
				Assert.AreEqual("SELECT * FROM TABLE1 WHERE COLUMN1 = :pCriteria1_1 COLUMN2 = :pCriteria2_1 AND COLUMN3 = :pCriteria3_1", query.GetSql());

				query.Conditions.Clear();
				query.SetCondition("Criteria1", true);
				query.SetCondition("Criteria3", true);
				Assert.AreEqual("SELECT * FROM TABLE1 WHERE COLUMN1 = :pCriteria1_1 AND COLUMN3 = :pCriteria3_1", query.GetSql());
			}

			[TestMethod]
			public void Separators_HandlingCustomFlags()
			{
				var query = new Query("SELECT * FROM TABLE1 {WHERE {COLUMN1 :Criteria1} @{{COLUMN2 :Criteria2} {COLUMN3 :Criteria3}} {COLUMN4 :Criteria4}}");

				query.ParserHints = Parsing.ParserHints.None;

				Assert.AreEqual("SELECT * FROM TABLE1", query.GetSql());

				query.SetCondition("Criteria1", true);
				Assert.AreEqual("SELECT * FROM TABLE1 WHERE COLUMN1 = :pCriteria1_1", query.GetSql());

				query.Conditions.Clear();
				query.SetCondition("Criteria2", true);
				Assert.AreEqual("SELECT * FROM TABLE1 WHERE COLUMN2 = :pCriteria2_1", query.GetSql());

				query.Conditions.Clear();
				query.SetCondition("Criteria3", true);
				Assert.AreEqual("SELECT * FROM TABLE1 WHERE COLUMN3 = :pCriteria3_1", query.GetSql());

				query.Conditions.Clear();
				query.SetCondition("Criteria4", true);
				Assert.AreEqual("SELECT * FROM TABLE1 WHERE COLUMN4 = :pCriteria4_1", query.GetSql());

				query.Conditions.Clear();
				query.SetCondition("Criteria1", true);
				query.SetCondition("Criteria2", true);
				Assert.AreEqual("SELECT * FROM TABLE1 WHERE COLUMN1 = :pCriteria1_1 AND COLUMN2 = :pCriteria2_1", query.GetSql());

				query.Conditions.Clear();
				query.SetCondition("Criteria1", true);
				query.SetCondition("Criteria3", true);
				Assert.AreEqual("SELECT * FROM TABLE1 WHERE COLUMN1 = :pCriteria1_1 AND COLUMN3 = :pCriteria3_1", query.GetSql());

				query.Conditions.Clear();
				query.SetCondition("Criteria1", true);
				query.SetCondition("Criteria4", true);
				Assert.AreEqual("SELECT * FROM TABLE1 WHERE COLUMN1 = :pCriteria1_1 AND COLUMN4 = :pCriteria4_1", query.GetSql());

				query.Conditions.Clear();
				query.SetCondition("Criteria2", true);
				query.SetCondition("Criteria3", true);
				Assert.AreEqual("SELECT * FROM TABLE1 WHERE COLUMN2 = :pCriteria2_1 OR COLUMN3 = :pCriteria3_1", query.GetSql());

				query.Conditions.Clear();
				query.SetCondition("Criteria2", true);
				query.SetCondition("Criteria4", true);
				Assert.AreEqual("SELECT * FROM TABLE1 WHERE COLUMN2 = :pCriteria2_1 AND COLUMN4 = :pCriteria4_1", query.GetSql());

				query.Conditions.Clear();
				query.SetCondition("Criteria3", true);
				query.SetCondition("Criteria4", true);
				Assert.AreEqual("SELECT * FROM TABLE1 WHERE COLUMN3 = :pCriteria3_1 AND COLUMN4 = :pCriteria4_1", query.GetSql());

				query.Conditions.Clear();
				query.SetCondition("Criteria1", true);
				query.SetCondition("Criteria2", true);
				query.SetCondition("Criteria3", true);
				Assert.AreEqual("SELECT * FROM TABLE1 WHERE COLUMN1 = :pCriteria1_1 AND COLUMN2 = :pCriteria2_1 OR COLUMN3 = :pCriteria3_1", query.GetSql());

				query.Conditions.Clear();
				query.SetCondition("Criteria2", true);
				query.SetCondition("Criteria3", true);
				query.SetCondition("Criteria4", true);
				Assert.AreEqual("SELECT * FROM TABLE1 WHERE COLUMN2 = :pCriteria2_1 OR COLUMN3 = :pCriteria3_1 AND COLUMN4 = :pCriteria4_1", query.GetSql());

				query.Conditions.Clear();
				query.SetCondition("Criteria1", true);
				query.SetCondition("Criteria2", true);
				query.SetCondition("Criteria3", true);
				query.SetCondition("Criteria4", true);
				Assert.AreEqual("SELECT * FROM TABLE1 WHERE COLUMN1 = :pCriteria1_1 AND COLUMN2 = :pCriteria2_1 OR COLUMN3 = :pCriteria3_1 AND COLUMN4 = :pCriteria4_1", query.GetSql());
			}

			[TestMethod]
			public void Separators_BlankAndVariables()
			{
				var query = new Query("SELECT * FROM TABLE1 {WHERE {COLUMN1 :Variable1} +{COLUMN2 :Variable2} +{COLUMN3 :Variable3} {COLUMN4 :Criteria4}}");

				query.ParserHints = Parsing.ParserHints.None;

				Assert.AreEqual("SELECT * FROM TABLE1", query.GetSql());

				query.DefineVariable("Variable1", "= test1");
				Assert.AreEqual("SELECT * FROM TABLE1 WHERE COLUMN1 = test1", query.GetSql());

				query.Variables.Clear();
				query.DefineVariable("Variable2", "= test2");
				Assert.AreEqual("SELECT * FROM TABLE1 WHERE COLUMN2 = test2", query.GetSql());

				query.Variables.Clear();
				query.DefineVariable("Variable3", "= test3");
				Assert.AreEqual("SELECT * FROM TABLE1 WHERE COLUMN3 = test3", query.GetSql());

				query.Variables.Clear();
				query.DefineVariable("Variable1", "= test1");
				query.DefineVariable("Variable2", "= test2");
				Assert.AreEqual("SELECT * FROM TABLE1 WHERE COLUMN1 = test1 COLUMN2 = test2", query.GetSql());

				query.Variables.Clear();
				query.DefineVariable("Variable1", "= test1");
				query.DefineVariable("Variable3", "= test3");
				Assert.AreEqual("SELECT * FROM TABLE1 WHERE COLUMN1 = test1 COLUMN3 = test3", query.GetSql());

				query.Variables.Clear();
				query.DefineVariable("Variable1", "= test1");
				query.DefineVariable("Variable2", "= test2");
				query.DefineVariable("Variable3", "= test3");
				Assert.AreEqual("SELECT * FROM TABLE1 WHERE COLUMN1 = test1 COLUMN2 = test2 COLUMN3 = test3", query.GetSql());

				query.Variables.Clear();
				query.Conditions.Clear();
				query.DefineVariable("Variable1", "= test1");
				query.SetCondition("Criteria4", true);
				Assert.AreEqual("SELECT * FROM TABLE1 WHERE COLUMN1 = test1 AND COLUMN4 = :pCriteria4_1", query.GetSql());

				query.Variables.Clear();
				query.Conditions.Clear();
				query.DefineVariable("Variable2", "= test2");
				query.SetCondition("Criteria4", true);
				Assert.AreEqual("SELECT * FROM TABLE1 WHERE COLUMN2 = test2 AND COLUMN4 = :pCriteria4_1", query.GetSql());

				// Test a typical ORDER BY scenario

				query = new Query("SELECT * FROM TABLE1 {WHERE {COLUMN1 :Criteria1} {COLUMN2 :Criteria2}} +{ORDER BY :ordering}");

				query.ParserHints = Parsing.ParserHints.None;

				Assert.AreEqual("SELECT * FROM TABLE1", query.GetSql());
				
				query.DefineVariable("ordering", "COLUMN1 DESC");
				Assert.AreEqual("SELECT * FROM TABLE1 ORDER BY COLUMN1 DESC", query.GetSql());

				query.Variables.Clear();
				query.Conditions.Clear();
				query.SetCondition("Criteria1", true);
				Assert.AreEqual("SELECT * FROM TABLE1 WHERE COLUMN1 = :pCriteria1_1", query.GetSql());

				query.Conditions.Clear();
				query.SetCondition("Criteria1", true);
				query.SetCondition("Criteria2", true);
				Assert.AreEqual("SELECT * FROM TABLE1 WHERE COLUMN1 = :pCriteria1_1 AND COLUMN2 = :pCriteria2_1", query.GetSql());

				query.Conditions.Clear();
				query.SetCondition("Criteria2", true);
				Assert.AreEqual("SELECT * FROM TABLE1 WHERE COLUMN2 = :pCriteria2_1", query.GetSql());

				query.Conditions.Clear();
				query.SetCondition("Criteria2", true);
				query.DefineVariable("ordering", "COLUMN1 DESC");
				Assert.AreEqual("SELECT * FROM TABLE1 WHERE COLUMN2 = :pCriteria2_1 ORDER BY COLUMN1 DESC", query.GetSql());

				query.Variables.Clear();
				query.Conditions.Clear();
				query.SetCondition("Criteria1", true);
				query.SetCondition("Criteria2", true);
				query.DefineVariable("ordering", "COLUMN2 DESC");
				Assert.AreEqual("SELECT * FROM TABLE1 WHERE COLUMN1 = :pCriteria1_1 AND COLUMN2 = :pCriteria2_1 ORDER BY COLUMN2 DESC", query.GetSql());
			}

			private void AssertCommand(IDbCommand cmd)
			{
				Assert.IsNotNull(cmd);
				Assert.AreEqual(CommandType.Text, cmd.CommandType);
				Assert.AreEqual(0, cmd.Parameters.Count);
				Assert.AreEqual(_expectedSql, cmd.CommandText.Replace(_expectedSqlComment, ""));
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
