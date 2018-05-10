using System;
using System.Data;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SqlBinder.ConditionValues;

namespace SqlBinder.UnitTesting
{
	public partial class SqlBinder_Tests
	{
		/// <summary>
		/// Tests that cover more complex Sql. A flexible script is defined which can be used for many different queries with different conditions.
		/// </summary>
		[TestClass]
		public class Complex_Sql
		{
			private MockQuery _query;

			[TestInitialize]
			public void InitializeTest()
			{
				_query = new MockQuery(_connection,
					"SELECT Orders.OrderID, Customers.ContactName, Orders.OrderDate, Orders.ShippedDate\n" +
				    "FROM Orders, Customers\n" +
				    "WHERE Customers.CustomerID = Orders.CustomerID\n" +
				    "{AND @{({Customers.ContactName [ContactNameFirst]} {Customers.ContactName [ContactNameMiddle]} {Customers.ContactName [ContactNameLast]})}\n" +
					"{Orders.OrderID [OrderID]}" +
				    "@{({Orders.OrderDate [OrderDate]} {Orders.ShippedDate [ShippedDate]} {Orders.RequiredDate [RequiredDate]})}\n" +
				    "{Orders.EmployeeID IN (SELECT EmployeeID FROM Employees WHERE {FirstName [EmployeeFirstName]} {LastName [EmployeeLastName]})}\n" +
					"{Orders.OrderID IN (SELECT OrderID FROM [[Order Details]] WHERE ProductID IN (SELECT ProductID FROM Products WHERE {ProductName [ProductName]} {SupplierID IN (SELECT SupplierID FROM Suppliers WHERE {CompanyName [SupplierCompanyName]})}))}\n" +
				    "{Orders.Freight [Freight]}}");
			}

			/// <summary>
			/// Simple test where the above heavy-duty query is reduced to a simple ID query
			/// </summary>
			[TestMethod]
			public void Complex_Sql_1()
			{
				_query.SetCondition("OrderID", Operator.Is, new NumberValue(10));

				var cmd = _query.CreateCommand();

				AssertCommand(cmd);

				Assert.AreEqual(
					"SELECT Orders.OrderID, Customers.ContactName, Orders.OrderDate, Orders.ShippedDate\n" +
					"FROM Orders, Customers\n" +
					"WHERE Customers.CustomerID = Orders.CustomerID\n" +
					"AND Orders.OrderID = :pOrderID_1",
					cmd.CommandText);

				Assert.AreEqual(1, cmd.Parameters.Count);

				Assert.AreEqual(10, cmd.Parameters[0].Value);
			}

			/// <summary>
			/// Simple test where we just want orders with specific freight cost
			/// </summary>
			[TestMethod]
			public void Complex_Sql_2()
			{
				_query.SetCondition("Freight", Operator.IsNotBetween, new NumberValue(50, 100));

				var cmd = _query.CreateCommand();

				AssertCommand(cmd);

				Assert.AreEqual(
					"SELECT Orders.OrderID, Customers.ContactName, Orders.OrderDate, Orders.ShippedDate\n" +
					"FROM Orders, Customers\n" +
					"WHERE Customers.CustomerID = Orders.CustomerID\n" +
					"AND Orders.Freight NOT BETWEEN :pFreight_1 AND :pFreight_2",
					cmd.CommandText);

				Assert.AreEqual(2, cmd.Parameters.Count);

				Assert.AreEqual(50, cmd.Parameters[0].Value);
				Assert.AreEqual(100, cmd.Parameters[1].Value);
			}

			/// <summary>
			/// A much more complex query now, we're looking for specific freight, specific dates and product name now.
			/// </summary>
			[TestMethod]
			public void Complex_Sql_3()
			{			
				_query.SetCondition("ShippedDate", Operator.IsLessThanOrEqualTo,
					new DateValue(DateTime.Parse("12/30/1995", System.Globalization.CultureInfo.InvariantCulture)));
				_query.SetCondition("RequiredDate", Operator.IsBetween, new DateValue(
					DateTime.Parse("9/1/1995", System.Globalization.CultureInfo.InvariantCulture),
					DateTime.Parse("11/30/1995", System.Globalization.CultureInfo.InvariantCulture)));

				_query.SetCondition("ProductName", Operator.Is, new StringValue("Tofu"));

				_query.SetCondition("Freight", Operator.IsLessThan, new NumberValue(200));

				var cmd = _query.CreateCommand();

				AssertCommand(cmd);

				Assert.AreEqual(
					"SELECT Orders.OrderID, Customers.ContactName, Orders.OrderDate, Orders.ShippedDate\n" +
					"FROM Orders, Customers\n" +
					"WHERE Customers.CustomerID = Orders.CustomerID\n" +
					"AND (Orders.ShippedDate <= :pShippedDate_1 OR Orders.RequiredDate BETWEEN :pRequiredDate_1 AND :pRequiredDate_2)\n" +
					"AND Orders.OrderID IN (SELECT OrderID FROM [Order Details] WHERE ProductID IN (SELECT ProductID FROM Products WHERE ProductName = :pProductName_1))\n" +
					"AND Orders.Freight < :pFreight_1",
					cmd.CommandText);

				Assert.AreEqual(5, cmd.Parameters.Count);

				Assert.AreEqual(DateTime.Parse("12/30/1995", System.Globalization.CultureInfo.InvariantCulture), cmd.Parameters[0].Value);
				Assert.AreEqual(DateTime.Parse("9/1/1995", System.Globalization.CultureInfo.InvariantCulture), cmd.Parameters[1].Value);
				Assert.AreEqual(DateTime.Parse("11/30/1995", System.Globalization.CultureInfo.InvariantCulture), cmd.Parameters[2].Value);
				Assert.AreEqual("Tofu", cmd.Parameters[3].Value);
				Assert.AreEqual(200, cmd.Parameters[4].Value);
			}

			/// <summary>
			/// Here with such complex query we'll now test the shortcut SetCondition overloads. We'll also dynamically extend and reduce the query without rebuilding it.
			/// </summary>
			[TestMethod]
			public void Complex_Sql_4()
			{
				// Compile the query 

				_query.SetCondition("OrderDate", DateTime.Parse("12/30/1995", System.Globalization.CultureInfo.InvariantCulture), NumericOperator.IsLessThanOrEqualTo);
				_query.SetCondition("ShippedDate",
					new[]
					{
						DateTime.Parse("11/20/1995", System.Globalization.CultureInfo.InvariantCulture),
						DateTime.Parse("11/21/1995", System.Globalization.CultureInfo.InvariantCulture),
						DateTime.Parse("11/25/1995", System.Globalization.CultureInfo.InvariantCulture)
					});
				_query.SetCondition("ProductName", new[] { "Tofu", "Chai", "Chocolade" });
				_query.SetCondition("Freight", 200m, NumericOperator.IsGreaterThanOrEqualTo);

				var cmd = _query.CreateCommand();

				AssertCommand(cmd);

				Assert.AreEqual(
					"SELECT Orders.OrderID, Customers.ContactName, Orders.OrderDate, Orders.ShippedDate\n" +
					"FROM Orders, Customers\n" +
					"WHERE Customers.CustomerID = Orders.CustomerID\n" +
					"AND (Orders.OrderDate <= :pOrderDate_1 OR Orders.ShippedDate IN (:pShippedDate_1, :pShippedDate_2, :pShippedDate_3))\n" +
					"AND Orders.OrderID IN (SELECT OrderID FROM [Order Details] WHERE ProductID IN (SELECT ProductID FROM Products WHERE ProductName IN (:pProductName_1, :pProductName_2, :pProductName_3)))\n" +
					"AND Orders.Freight >= :pFreight_1",
					cmd.CommandText);

				Assert.AreEqual(8, cmd.Parameters.Count);

				Assert.AreEqual(DateTime.Parse("12/30/1995", System.Globalization.CultureInfo.InvariantCulture), cmd.Parameters[0].Value);
				Assert.AreEqual(DateTime.Parse("11/20/1995", System.Globalization.CultureInfo.InvariantCulture), cmd.Parameters[1].Value);
				Assert.AreEqual(DateTime.Parse("11/21/1995", System.Globalization.CultureInfo.InvariantCulture), cmd.Parameters[2].Value);
				Assert.AreEqual(DateTime.Parse("11/25/1995", System.Globalization.CultureInfo.InvariantCulture), cmd.Parameters[3].Value);
				Assert.AreEqual("Tofu", cmd.Parameters[4].Value);
				Assert.AreEqual("Chai", cmd.Parameters[5].Value);
				Assert.AreEqual("Chocolade", cmd.Parameters[6].Value);
				Assert.AreEqual(200m, cmd.Parameters[7].Value);

				// Add extra condition

				_query.SetCondition("SupplierCompanyName", "Tokyo Traders");

				cmd = _query.CreateCommand();

				AssertCommand(cmd);

				Assert.AreEqual(
					"SELECT Orders.OrderID, Customers.ContactName, Orders.OrderDate, Orders.ShippedDate\n" +
					"FROM Orders, Customers\n" +
					"WHERE Customers.CustomerID = Orders.CustomerID\n" +
					"AND (Orders.OrderDate <= :pOrderDate_1 OR Orders.ShippedDate IN (:pShippedDate_1, :pShippedDate_2, :pShippedDate_3))\n" +
					"AND Orders.OrderID IN (SELECT OrderID FROM [Order Details] WHERE ProductID IN (SELECT ProductID FROM Products WHERE ProductName IN (:pProductName_1, :pProductName_2, :pProductName_3) AND SupplierID IN (SELECT SupplierID FROM Suppliers WHERE CompanyName = :pSupplierCompanyName_1)))\n" +
					"AND Orders.Freight >= :pFreight_1",
					cmd.CommandText);

				Assert.AreEqual(9, cmd.Parameters.Count);

				Assert.AreEqual(DateTime.Parse("12/30/1995", System.Globalization.CultureInfo.InvariantCulture), cmd.Parameters[0].Value);
				Assert.AreEqual(DateTime.Parse("11/20/1995", System.Globalization.CultureInfo.InvariantCulture), cmd.Parameters[1].Value);
				Assert.AreEqual(DateTime.Parse("11/21/1995", System.Globalization.CultureInfo.InvariantCulture), cmd.Parameters[2].Value);
				Assert.AreEqual(DateTime.Parse("11/25/1995", System.Globalization.CultureInfo.InvariantCulture), cmd.Parameters[3].Value);
				Assert.AreEqual("Tofu", cmd.Parameters[4].Value);
				Assert.AreEqual("Chai", cmd.Parameters[5].Value);
				Assert.AreEqual("Chocolade", cmd.Parameters[6].Value);
				Assert.AreEqual("Tokyo Traders", cmd.Parameters[7].Value);
				Assert.AreEqual(200m, cmd.Parameters[8].Value);

				// Remove two conditions

				_query.RemoveCondition("ProductName");
				_query.RemoveCondition("OrderDate");

				cmd = _query.CreateCommand();

				AssertCommand(cmd);

				Assert.AreEqual(
					"SELECT Orders.OrderID, Customers.ContactName, Orders.OrderDate, Orders.ShippedDate\n" +
					"FROM Orders, Customers\n" +
					"WHERE Customers.CustomerID = Orders.CustomerID\n" +
					"AND (Orders.ShippedDate IN (:pShippedDate_1, :pShippedDate_2, :pShippedDate_3))\n" +
					"AND Orders.OrderID IN (SELECT OrderID FROM [Order Details] WHERE ProductID IN (SELECT ProductID FROM Products WHERE SupplierID IN (SELECT SupplierID FROM Suppliers WHERE CompanyName = :pSupplierCompanyName_1)))\n" +
					"AND Orders.Freight >= :pFreight_1",
					cmd.CommandText);

				Assert.AreEqual(5, cmd.Parameters.Count);

				Assert.AreEqual(DateTime.Parse("11/20/1995", System.Globalization.CultureInfo.InvariantCulture), cmd.Parameters[0].Value);
				Assert.AreEqual(DateTime.Parse("11/21/1995", System.Globalization.CultureInfo.InvariantCulture), cmd.Parameters[1].Value);
				Assert.AreEqual(DateTime.Parse("11/25/1995", System.Globalization.CultureInfo.InvariantCulture), cmd.Parameters[2].Value);
				Assert.AreEqual("Tokyo Traders", cmd.Parameters[3].Value);
				Assert.AreEqual(200m, cmd.Parameters[4].Value);
			}

			private void AssertCommand(IDbCommand cmd)
			{
				Assert.IsNotNull(cmd);
				Assert.AreEqual(CommandType.Text, cmd.CommandType);
				Assert.IsTrue(cmd.Parameters.Count != 0);
			}
		}
	}
}
