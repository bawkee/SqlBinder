using System;
using System.Data;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SqlBinder.ConditionValues;

namespace SqlBinder.UnitTesting
{
	[TestClass]
	public partial class SqlBinder_Tests
	{
		/// <summary>
		/// Tests that cover the most common cases, moderately complex Sql.
		/// </summary>
		[TestClass]
		public class Moderate_Sql
		{
			[TestInitialize]
			public void InitializeTest()
			{
				//
			}

			[TestMethod]
			public void Common_Sql_1()
			{
				var query = new MockQuery(_connection,"SELECT * FROM TABLE1 {WHERE {COLUMN1 [Criteria1]}}");
				

				query.SetCondition("Criteria1", new BoolValue(true));

				var cmd = query.CreateCommand();

				AssertCommand(cmd);
				Assert.AreEqual("SELECT * FROM TABLE1 WHERE COLUMN1 = :pCriteria1_1", cmd.CommandText);
				Assert.AreEqual(1, cmd.Parameters.Count);
				Assert.AreEqual("pCriteria1_1", cmd.Parameters[0].ParameterName);
				Assert.AreEqual(true, cmd.Parameters[0].Value);
			}

			[TestMethod]
			public void Common_Sql_2()
			{
				var query = new MockQuery(_connection, "SELECT * FROM TABLE1 {WHERE {JUNK 1} {COLUMN1 [Criteria1]} {JUNK 2}}");

				query.SetCondition("Criteria1", new BoolValue(true));

				var cmd = query.CreateCommand();

				AssertCommand(cmd);
				
				Assert.AreEqual("SELECT * FROM TABLE1 WHERE COLUMN1 = :pCriteria1_1", cmd.CommandText);
				Assert.AreEqual(1, cmd.Parameters.Count);
				Assert.AreEqual("pCriteria1_1", cmd.Parameters[0].ParameterName);
				Assert.AreEqual(true, cmd.Parameters[0].Value);
			}

			[TestMethod]
			public void Common_Sql_3()
			{
				var query = new MockQuery(_connection, "SELECT * FROM TABLE1 {WHERE {COLUMN1 :Criteria1} {COLUMN2 :Criteria2}}");
				query.ParserHints = Parsing.ParserHints.None;				

				// No columns
				var cmd = query.CreateCommand();

				Assert.IsNotNull(cmd);
				Assert.AreEqual(CommandType.Text, cmd.CommandType);
				Assert.AreEqual("SELECT * FROM TABLE1", cmd.CommandText);
				Assert.AreEqual(0, cmd.Parameters.Count);

				// Both columns
				query.SetCondition("Criteria1", new BoolValue(true));
				query.SetCondition("Criteria2", new NumberValue(13));

				cmd = query.CreateCommand();

				Assert.IsNotNull(cmd);
				Assert.AreEqual(CommandType.Text, cmd.CommandType);
				Assert.AreEqual("SELECT * FROM TABLE1 WHERE COLUMN1 = :pCriteria1_1 AND COLUMN2 = :pCriteria2_1", cmd.CommandText);
				Assert.AreEqual(2, cmd.Parameters.Count);
				Assert.AreEqual("pCriteria1_1", cmd.Parameters[0].ParameterName);
				Assert.AreEqual("pCriteria2_1", cmd.Parameters[1].ParameterName);
				Assert.AreEqual(true, cmd.Parameters[0].Value);
				Assert.AreEqual(13, cmd.Parameters[1].Value);

				// First column
				query.Conditions.Clear();
				query.SetCondition("Criteria1", new BoolValue(true));

				cmd = query.CreateCommand();

				Assert.IsNotNull(cmd);
				Assert.AreEqual(CommandType.Text, cmd.CommandType);
				Assert.AreEqual("SELECT * FROM TABLE1 WHERE COLUMN1 = :pCriteria1_1", cmd.CommandText);
				Assert.AreEqual(1, cmd.Parameters.Count);
				Assert.AreEqual("pCriteria1_1", cmd.Parameters[0].ParameterName);
				Assert.AreEqual(true, cmd.Parameters[0].Value);

				// Second column				
				query.Conditions.Clear();
				query.SetCondition("Criteria2", new NumberValue(13));

				cmd = query.CreateCommand();

				Assert.IsNotNull(cmd);
				Assert.AreEqual(CommandType.Text, cmd.CommandType);
				Assert.AreEqual("SELECT * FROM TABLE1 WHERE COLUMN2 = :pCriteria2_1", cmd.CommandText);
				Assert.AreEqual(1, cmd.Parameters.Count);
				Assert.AreEqual("pCriteria2_1", cmd.Parameters[0].ParameterName);
				Assert.AreEqual(13, cmd.Parameters[0].Value);
			}

			[TestMethod]
			public void Common_Sql_4()
			{
				var query = new MockQuery(_connection, "SELECT * FROM TABLE1 {WHERE {COLUMN1 [Criteria1]} {JUNK 1} {COLUMN2 [Criteria2]}} {JUNK 2}");

				query.SetCondition("Criteria1", new BoolValue(true));
				query.SetCondition("Criteria2", new NumberValue(13));

				var cmd = query.CreateCommand();

				AssertCommand(cmd);

				// Note that now there is expected extra space before the AND resulting from the JUNK 1.
				Assert.AreEqual("SELECT * FROM TABLE1 WHERE COLUMN1 = :pCriteria1_1 AND COLUMN2 = :pCriteria2_1", cmd.CommandText);
				Assert.AreEqual(2, cmd.Parameters.Count);
				Assert.AreEqual("pCriteria1_1", cmd.Parameters[0].ParameterName);
				Assert.AreEqual("pCriteria2_1", cmd.Parameters[1].ParameterName);
				Assert.AreEqual(true, cmd.Parameters[0].Value);
				Assert.AreEqual(13, cmd.Parameters[1].Value);
			}

			[TestMethod]
			public void Common_Sql_5()
			{
				var query = new MockQuery(_connection, "SELECT * FROM TABLE1 {WHERE {COLUMN1 [Criteria1]}};");

				query.SetCondition("Criteria1", new CustomParameterlessConditionValue("test"));

				var cmd = query.CreateCommand();

				Assert.IsNotNull(cmd);
				Assert.AreEqual(0, cmd.Parameters.Count);
				Assert.AreEqual("SELECT * FROM TABLE1 WHERE COLUMN1 = 'test' /*hint*/;", cmd.CommandText);

				query.SetCondition("Criteria1", Operator.IsNot, new CustomParameterlessConditionValue("test"));

				cmd = query.CreateCommand();

				Assert.IsNotNull(cmd);
				Assert.AreEqual(0, cmd.Parameters.Count);
				Assert.AreEqual("SELECT * FROM TABLE1 WHERE COLUMN1 <> 'test';", cmd.CommandText);
			}

			[TestMethod]
			public void Convert_In_To_Equality_1()
			{
				// Tests the scenario where IN('A') or IN(123) should be automatically converted by the = 'A' and = 123 etc. This happens 
				// in the ConditionValue classes

				var query = new MockQuery(_connection, "SELECT * FROM TABLE1 {WHERE {COLUMN1 [Criteria1]}}");

				// Set conditions, string short overload
				query.SetCondition("Criteria1", new [] { "A" });

				var cmd = query.CreateCommand();

				AssertCommand(cmd);
				Assert.AreEqual("SELECT * FROM TABLE1 WHERE COLUMN1 = :pCriteria1_1", cmd.CommandText);
				Assert.AreEqual(1, cmd.Parameters.Count);
				Assert.AreEqual("pCriteria1_1", cmd.Parameters[0].ParameterName);
				Assert.AreEqual("A", cmd.Parameters[0].Value);

				// Set conditions, string short overload, negative
				query.SetCondition("Criteria1", new[] { "A" }, true);

				cmd = query.CreateCommand();

				AssertCommand(cmd);
				Assert.AreEqual("SELECT * FROM TABLE1 WHERE COLUMN1 != :pCriteria1_1", cmd.CommandText);
				Assert.AreEqual(1, cmd.Parameters.Count);
				Assert.AreEqual("pCriteria1_1", cmd.Parameters[0].ParameterName);
				Assert.AreEqual("A", cmd.Parameters[0].Value);

				// Set conditions, string
				query.SetCondition("Criteria1", Operator.IsAnyOf, new StringValue(new[] { "A" }));

				cmd = query.CreateCommand();

				AssertCommand(cmd);
				Assert.AreEqual("SELECT * FROM TABLE1 WHERE COLUMN1 = :pCriteria1_1", cmd.CommandText);
				Assert.AreEqual(1, cmd.Parameters.Count);
				Assert.AreEqual("pCriteria1_1", cmd.Parameters[0].ParameterName);
				Assert.AreEqual("A", cmd.Parameters[0].Value);

				// Set conditions, number short overload
				query.SetCondition("Criteria1", new[] { 123 });

				cmd = query.CreateCommand();

				AssertCommand(cmd);
				Assert.AreEqual("SELECT * FROM TABLE1 WHERE COLUMN1 = :pCriteria1_1", cmd.CommandText);
				Assert.AreEqual(1, cmd.Parameters.Count);
				Assert.AreEqual("pCriteria1_1", cmd.Parameters[0].ParameterName);
				Assert.AreEqual(123, cmd.Parameters[0].Value);

				// Set conditions, number short overload, negative
				query.SetCondition("Criteria1", new[] { 123 }, true);

				cmd = query.CreateCommand();

				AssertCommand(cmd);
				Assert.AreEqual("SELECT * FROM TABLE1 WHERE COLUMN1 != :pCriteria1_1", cmd.CommandText);
				Assert.AreEqual(1, cmd.Parameters.Count);
				Assert.AreEqual("pCriteria1_1", cmd.Parameters[0].ParameterName);
				Assert.AreEqual(123, cmd.Parameters[0].Value);

				// Set conditions, number
				query.SetCondition("Criteria1", Operator.IsAnyOf, new NumberValue(new[] { 123 }));

				cmd = query.CreateCommand();

				AssertCommand(cmd);
				Assert.AreEqual("SELECT * FROM TABLE1 WHERE COLUMN1 = :pCriteria1_1", cmd.CommandText);
				Assert.AreEqual(1, cmd.Parameters.Count);
				Assert.AreEqual("pCriteria1_1", cmd.Parameters[0].ParameterName);
				Assert.AreEqual(123, cmd.Parameters[0].Value);
			}

			[TestMethod]
			public void Join_Sql_1()
			{
				// Slightly more complex query. We're getting Orders by Customer's name and OrderDate but we also have a possibility to filter by ShippedDate 
				// which we will omit in this test. We'll looking for orders shipped in November '95 from a customer whose name begins with Thomas.

				var query = new MockQuery(_connection, "SELECT Orders.OrderID, Customers.ContactName, Orders.OrderDate, Orders.ShippedDate " +
				                                "FROM Orders, Customers " +
				                                "WHERE Customers.CustomerID = Orders.CustomerID " +
				                                "{AND {Customers.ContactName [ContactName]} " +
				                                "{Orders.OrderDate [OrderDate]} " +
												"{Orders.ShippedDate [ShippedDate]}}");

				query.SetCondition("ContactName", Operator.Contains, 
					new StringValue("Thomas", StringValue.MatchOption.BeginsWith));
				query.SetCondition("OrderDate", Operator.IsBetween, new DateValue(
					DateTime.Parse("11/1/1995", System.Globalization.CultureInfo.InvariantCulture), 
					DateTime.Parse("11/30/1995", System.Globalization.CultureInfo.InvariantCulture)));
				
				var cmd = query.CreateCommand();

				AssertCommand(cmd);
				
				Assert.AreEqual(
					"SELECT Orders.OrderID, Customers.ContactName, Orders.OrderDate, Orders.ShippedDate " +
					"FROM Orders, Customers " +
					"WHERE Customers.CustomerID = Orders.CustomerID " +
					"AND Customers.ContactName LIKE :pContactName_1 " +
					"AND Orders.OrderDate BETWEEN :pOrderDate_1 AND :pOrderDate_2"
					, cmd.CommandText);

				Assert.AreEqual(3, cmd.Parameters.Count);
				Assert.AreEqual("pContactName_1", cmd.Parameters[0].ParameterName);
				Assert.AreEqual("pOrderDate_1", cmd.Parameters[1].ParameterName);
				Assert.AreEqual("pOrderDate_2", cmd.Parameters[2].ParameterName);

				Assert.AreEqual("Thomas%", cmd.Parameters[0].Value);
				Assert.AreEqual(DateTime.Parse("11/1/1995", System.Globalization.CultureInfo.InvariantCulture), cmd.Parameters[1].Value);
				Assert.AreEqual(DateTime.Parse("11/30/1995", System.Globalization.CultureInfo.InvariantCulture), cmd.Parameters[2].Value);
			}

			[TestMethod]
			public void Join_Sql_2()
			{
				// More complex query. Similar to previous one except now we're looking for customer whose name either: begins with Thomas, ends with Hardy or has " John " in the middle.
				// This means we have 3 different conditions clustered inside a single group that generates an OR query (condition1 OR condition2). Also, now, we're looking at
				// the shipment date rather than order date.

				var query = new MockQuery(_connection, "SELECT Orders.OrderID, Customers.ContactName, Orders.OrderDate, Orders.ShippedDate \n" +
												"FROM Orders, Customers \n" +
												"WHERE Customers.CustomerID = Orders.CustomerID \n" +
												"{AND @{({Customers.ContactName [ContactNameFirst]} {Customers.ContactName [ContactNameMiddle]} {Customers.ContactName [ContactNameLast]})} \n" +
												"{Orders.OrderDate [OrderDate]} \n" +
												"{Orders.ShippedDate [ShippedDate]}}");

				query.SetCondition("ContactNameFirst", Operator.Contains,
					new StringValue("Thomas", StringValue.MatchOption.BeginsWith));
				query.SetCondition("ContactNameMiddle", Operator.Contains,
					new StringValue(" John ", StringValue.MatchOption.OccursAnywhere));
				query.SetCondition("ContactNameLast", Operator.Contains,
					new StringValue("Hardy", StringValue.MatchOption.EndsWith));

				query.SetCondition("ShippedDate", Operator.IsGreaterThanOrEqualTo, 
					new DateValue(DateTime.Parse("11/20/1995", System.Globalization.CultureInfo.InvariantCulture)));

				var cmd = query.CreateCommand();
				
				AssertCommand(cmd);

				// Note that linefeeds differ between the original and the output. Some are removed. When outputting scopes, text before the scope is written but with reduced whitespace
				// as to clean up any junk that was potentially left by any scopes that were previously removed. Generator maintains the original formatting but some lines might end up
				// trimmed. This is expected behavior and this test covers it.
				
				Assert.AreEqual(
					"SELECT Orders.OrderID, Customers.ContactName, Orders.OrderDate, Orders.ShippedDate \n" +
					"FROM Orders, Customers \n" +
					"WHERE Customers.CustomerID = Orders.CustomerID \n" +
					"AND (Customers.ContactName LIKE :pContactNameFirst_1 OR Customers.ContactName LIKE :pContactNameMiddle_1 OR Customers.ContactName LIKE :pContactNameLast_1) \n" +
					"AND Orders.ShippedDate >= :pShippedDate_1"
					, cmd.CommandText);

				Assert.AreEqual(4, cmd.Parameters.Count);
				Assert.AreEqual("pContactNameFirst_1", cmd.Parameters[0].ParameterName);
				Assert.AreEqual("pContactNameMiddle_1", cmd.Parameters[1].ParameterName);
				Assert.AreEqual("pContactNameLast_1", cmd.Parameters[2].ParameterName);
				Assert.AreEqual("pShippedDate_1", cmd.Parameters[3].ParameterName);

				Assert.AreEqual("Thomas%", cmd.Parameters[0].Value);
				Assert.AreEqual("% John %", cmd.Parameters[1].Value);
				Assert.AreEqual("%Hardy", cmd.Parameters[2].Value);
				Assert.AreEqual(DateTime.Parse("11/20/1995", System.Globalization.CultureInfo.InvariantCulture), cmd.Parameters[3].Value);
			}

			[TestMethod]
			public void Null_Sql_1()
			{
				var query = new MockQuery(_connection, "SELECT * FROM TABLE1 {WHERE {COLUMN1 [Criteria1]}}");

				// Set the condition
				query.SetCondition("Criteria1", new StringValue((string)null));

				var cmd = query.CreateCommand();

				Assert.IsNotNull(cmd);
				Assert.AreEqual("SELECT * FROM TABLE1 WHERE COLUMN1 IS NULL", cmd.CommandText);
				Assert.AreEqual(0, cmd.Parameters.Count);

				// Now try the overload
				query.SetCondition("Criteria1", (string)null);

				cmd = query.CreateCommand();

				Assert.IsNotNull(cmd);
				Assert.AreEqual("SELECT * FROM TABLE1 WHERE COLUMN1 IS NULL", cmd.CommandText);
				Assert.AreEqual(0, cmd.Parameters.Count);

				// Now try IS NOT
				query.SetCondition("Criteria1", null, StringOperator.IsNot);

				cmd = query.CreateCommand();

				Assert.IsNotNull(cmd);
				Assert.AreEqual("SELECT * FROM TABLE1 WHERE COLUMN1 IS NOT NULL", cmd.CommandText);
				Assert.AreEqual(0, cmd.Parameters.Count);
			}

			[TestMethod]
			public void Null_Sql_2()
			{
				var query = new MockQuery(_connection, "SELECT * FROM TABLE1 {WHERE {COLUMN1 [Criteria1]}}");

				// Set the DateTime condition
				query.SetCondition("Criteria1", new DateValue((DateTime?)null));

				var cmd = query.CreateCommand();

				Assert.IsNotNull(cmd);
				Assert.AreEqual("SELECT * FROM TABLE1 WHERE COLUMN1 IS NULL", cmd.CommandText);
				Assert.AreEqual(0, cmd.Parameters.Count);

				// Now try IS NOT
				query.SetCondition("Criteria1", Operator.IsNot, new DateValue((DateTime?)null));

				cmd = query.CreateCommand();

				Assert.IsNotNull(cmd);
				Assert.AreEqual("SELECT * FROM TABLE1 WHERE COLUMN1 IS NOT NULL", cmd.CommandText);
				Assert.AreEqual(0, cmd.Parameters.Count);

				// Set the Bool condition
				query.SetCondition("Criteria1", new BoolValue(null));

				cmd = query.CreateCommand();

				Assert.IsNotNull(cmd);
				Assert.AreEqual("SELECT * FROM TABLE1 WHERE COLUMN1 IS NULL", cmd.CommandText);
				Assert.AreEqual(0, cmd.Parameters.Count);

				// Now try IS NOT
				query.SetCondition("Criteria1", Operator.IsNot, new BoolValue(null));

				cmd = query.CreateCommand();

				Assert.IsNotNull(cmd);
				Assert.AreEqual("SELECT * FROM TABLE1 WHERE COLUMN1 IS NOT NULL", cmd.CommandText);
				Assert.AreEqual(0, cmd.Parameters.Count);
			}

			[TestMethod]
			public void Empty_String_Sql()
			{
				var query = new MockQuery(_connection, "SELECT * FROM TABLE1 {WHERE {COLUMN1 [Criteria1]}}");

				// Set the condition
				query.SetCondition("Criteria1", new StringValue(""));

				var cmd = query.CreateCommand();

				AssertCommand(cmd);
				Assert.AreEqual("SELECT * FROM TABLE1 WHERE COLUMN1 = :pCriteria1_1", cmd.CommandText);
				Assert.AreEqual(1, cmd.Parameters.Count);
				Assert.AreEqual("", cmd.Parameters[0].Value);

				// Now try IS NOT
				query.SetCondition("Criteria1", "", StringOperator.IsNot);

				cmd = query.CreateCommand();

				AssertCommand(cmd);
				Assert.AreEqual("SELECT * FROM TABLE1 WHERE COLUMN1 != :pCriteria1_1", cmd.CommandText);
				Assert.AreEqual(1, cmd.Parameters.Count);
				Assert.AreEqual("", cmd.Parameters[0].Value);

				// Set the condition
				query.SetCondition("Criteria1", Operator.IsAnyOf, new StringValue(new[] { null, "A", "B" }));

				cmd = query.CreateCommand();

				AssertCommand(cmd);
				Assert.AreEqual("SELECT * FROM TABLE1 WHERE COLUMN1 IN (:pCriteria1_1, :pCriteria1_2, :pCriteria1_3)", cmd.CommandText);
				Assert.AreEqual(3, cmd.Parameters.Count);
				Assert.AreEqual(DBNull.Value, cmd.Parameters[0].Value);
				Assert.AreEqual("A", cmd.Parameters[1].Value);
				Assert.AreEqual("B", cmd.Parameters[2].Value);
			}

			[TestMethod]
			public void Variables_1()
			{
				var query = new MockQuery(_connection, "SELECT * FROM TABLE1 {WHERE {COLUMN1 [Variable1]}}");
				var variable = "= 'Some Value'";

				query.DefineVariable("Variable1", variable);

				var cmd = query.CreateCommand();

				Assert.IsNotNull(cmd);
				Assert.AreEqual(CommandType.Text, cmd.CommandType);
				Assert.AreEqual($"SELECT * FROM TABLE1 WHERE COLUMN1 {variable}", cmd.CommandText);				
			}

			private void AssertCommand(IDbCommand cmd)
			{
				Assert.IsNotNull(cmd);
				Assert.AreEqual(CommandType.Text, cmd.CommandType);
				Assert.IsTrue(cmd.Parameters.Count > 0);
			}
		}


	}
}
