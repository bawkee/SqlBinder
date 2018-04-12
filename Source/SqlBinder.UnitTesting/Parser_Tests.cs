using System.Data;
using Microsoft.VisualStudio.TestTools.UnitTesting;

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
			private MockSqlBinder _binder;

			[TestInitialize]
			public void InitializeTest()
			{
				_binder = new MockSqlBinder(_connection);
			}

			[TestMethod]
			public void Comments_1()
			{
				var withoutComments = "SELECT * FROM TABLE1 WHERE TABLE1.COLUMN1 = 123";
				var withComments = "SELECT * FROM TABLE1{*, TABLE2*} WHERE TABLE1.COLUMN1 = 123{* AND TABLE2.COLUMN1 = TABLE1.COLUMN1 {AND {TABLE1.COLUMN1 [SomeCriteria]}}*}";

				AssertCommand(_binder.CreateQuery(withComments).CreateCommand(), withoutComments);
			}

			[TestMethod]
			public void Comments_2()
			{
				var withoutComments = "SELECT * FROM TABLE1";
				var withComments = "{* We're testing multiline \n" +
				                   " * comments here. \n" +
				                   " * They should work fine *} \n" +
				                   "SELECT * FROM TABLE1";

				AssertCommand(_binder.CreateQuery(withComments).CreateCommand(), withoutComments);
			}

			[TestMethod]
			public void Escape_Strings_1()
			{
				// Literals should be ignored entirely
				var sql = "SELECT * FROM TABLE1 WHERE COLUMN1 = 'This is some {literal text} that includes @{special characters} like [this] or $[this]$ or ${ this maybe }$.'";

				AssertCommand(_binder.CreateQuery(sql).CreateCommand(), sql);
			}

			[TestMethod]
			public void Escape_Strings_2()
			{
				// Escaping curly braces with dollar sign
				var sql = "SELECT * FROM TABLE1 WHERE COLUMN1 = 123 /* This comment should include ${this scope}$ because it was escaped. */";
				var expected = "SELECT * FROM TABLE1 WHERE COLUMN1 = 123 /* This comment should include {this scope} because it was escaped. */";

				AssertCommand(_binder.CreateQuery(sql).CreateCommand(), expected);
			}

			[TestMethod]
			public void Escape_Strings_3()
			{
				// Escaping square brackets with dollar sign, a potentially common scenario in OLEDB queries
				var sql = "SELECT * FROM TABLE1 {WHERE {$[Some Column 1]$ [SomeCriteria1]} {$[Some Column 2]$ [SomeCriteria2]} {$[Some Column 3]$ [SomeCriteria3]}}";
				var expected = "SELECT * FROM TABLE1 WHERE [Some Column 1] = :pSomeCriteria1_1 AND [Some Column 3] = :pSomeCriteria3_1";

				_binder.ThrowScriptErrorException = true;
				var query = _binder.CreateQuery(sql);

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
