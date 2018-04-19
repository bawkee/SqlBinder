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
		public class Simplest_Sql
		{
			private static string _expectedSql = "SELECT * FROM TABLE1";
			private static string _expectedSqlComment = "/* Test comment */";

			[TestInitialize]
			public void InitializeTest()
			{
				//
			}

			[TestMethod]
			public void Baby_Sql_1()
			{
				AssertCommand(new MockQuery(_connection, "SELECT * FROM TABLE1").CreateCommand());
			}

			[TestMethod]
			public void Baby_Sql_2()
			{
				foreach (var junkSql in new[]
				{
					"SELECT * FROM TABLE1 {JUNK ON THE RIGHT}",
					"SELECT * FROM TABLE1 {JUNK ON THE RIGHT {NESTED JUNK}}",
					"{JUNK ON THE LEFT} SELECT * FROM TABLE1",
					"{JUNK ON THE LEFT {NESTED JUNK}} SELECT * FROM TABLE1 {JUNK ON THE RIGHT {NESTED JUNK}}",
					"{JUNK ON THE LEFT} SELECT * {JUNK ON THE MIDDLE}FROM TABLE1 {JUNK ON THE RIGHT {NESTED JUNK}}",
					"{JUNK ON THE LEFT} SELECT * {MIDDLE JUNK 1} {MIDDLE JUNK 2}   {MIDDLE JUNK 3}FROM TABLE1 {JUNK ON THE RIGHT {NESTED JUNK}}",
					"{JUNK: ~!@#$%^&*()_+~<>?,./;':}SELECT * FROM TABLE1 {JUNK ON THE RIGHT}",
				})
					AssertCommand(new MockQuery(_connection, junkSql).CreateCommand());
			}

			[TestMethod]
			public void Baby_Sql_3()
			{
				// Sql comments remain but should not hinder processing in any way
				foreach (var junkSql in new[]
				{
					"/* Test {this will get removed}comment */SELECT * FROM TABLE1 {JUNK ON THE RIGHT}",
					"SELECT * FROM TABLE1 {/* Test comment */JUNK ON THE RIGHT {NESTED/* Test comment */ JUNK}}",
					"{JUNK ON THE LEFT} SELECT * FROM TABLE1",
					"{JUNK ON THE LEFT {NESTED JUNK/* Test comment */}} SELECT * FROM TABLE1 {JUNK ON THE RIGHT {NESTED JUNK}}",
					"{JUNK ON THE LEFT} SELECT * {/* Test comment */JUNK ON THE MIDDLE/* Test comment */}FROM TABLE1 {JUNK ON THE RIGHT {NESTED JUNK}}",
				})
					AssertCommand(new MockQuery(_connection, junkSql).CreateCommand());
			}

			[TestMethod]
			public void Baby_Sql_4()
			{
				// SqlBinder comments should be removed. They can be nested and contain any characters.
				foreach (var junkSql in new[]
				{
					"{* Test comment 1 *}SELECT * FROM TABLE1 {{* Test comment 2 *}JUNK ON THE RIGHT}",
					"SELECT * FROM TABLE1 {JUNK ON THE RIGHT {NESTED JUNK{* Test comment 3 *}}{* Test comment 4 *}}",
					"{JUNK ON {* Test comment *}THE LEFT} SELECT * FROM TABLE1 {* Test comment *}",
					"{* Test comment {* Nested comment *} {Curly braces junk} *}{JUNK ON THE LEFT {NESTED JUNK}} SELECT * FROM TABLE1 {JUNK ON THE RIGHT {NESTED JUNK}}",
					"{JUNK ON THE LEFT} {* Special characters:[asdf][[[]}}}}~@#$%^&*()_+:'<>,./? *}SELECT * {JUNK ON THE MIDDLE}FROM TABLE1 {JUNK ON THE RIGHT {NESTED JUNK}}",
				})
					AssertCommand(new MockQuery(_connection, junkSql).CreateCommand());
			}

			[TestMethod]
			public void Baby_Sql_5()
			{
				foreach (var junkSql in new[]
				{
					"SELECT * FROM TABLE1 {JUNK ON THE RIGHT}",
					"SELECT * FROM TABLE1 {JUNK ON THE RIGHT {NESTED JUNK [criteria1]}}",
					"{JUNK ON THE LEFT} SELECT * FROM TABLE1",
					"{JUNK ON THE LEFT {NESTED JUNK [criteria1]}} SELECT * FROM TABLE1 {JUNK ON THE RIGHT {NESTED JUNK [criteria2]}}",
					"{JUNK ON THE LEFT [criteria1]} SELECT * {JUNK ON THE MIDDLE}FROM TABLE1 {JUNK ON THE RIGHT {NESTED JUNK [criteria2]}}",
				})
					AssertCommand(new MockQuery(_connection, junkSql).CreateCommand());
			}

			[TestMethod]
			[ExpectedException(typeof(UnmatchedConditionsException))]
			public void Invalid_Script_1()
			{
				var query = new MockQuery(_connection, "SELECT * FROM TABLE1");
				query.SetCondition("condition1", 0);
				AssertCommand(query.CreateCommand());
			}

			private void AssertCommand(IDbCommand cmd)
			{
				Assert.IsNotNull(cmd);
				Assert.IsTrue(cmd.CommandType == CommandType.Text);
				Assert.IsTrue(cmd.Parameters.Count == 0);
				Assert.AreEqual(_expectedSql, cmd.CommandText.Replace(_expectedSqlComment, ""));
			}
		}
	}

}
