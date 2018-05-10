using System.Data;
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
		public class Parser_Tests
		{
			private static string _expectedSql = "SELECT * FROM TABLE1";
			private static string _expectedSqlComment = "/* Test comment */";

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
