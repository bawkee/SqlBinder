using System;
using System.Collections.Generic;
using System.Data;
using System.Data.OleDb;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SqlBinder;
using SqlBinder.ConditionValues;

namespace SqlBinder.ConsoleTutorial
{
	class Program
	{
		static void Main(string[] args)
		{
			using (var connection = new OleDbConnection("Provider=Microsoft.Jet.OLEDB.4.0;Data Source=Northwind Traders.mdb"))
			{
				connection.Open();

				// Comment/uncomment the tutorial you want to play with. You can browse contents of the database from within Visual Studio 
				// (double click on the Northwind Traders.mdb item in the project) and experiment.
				
				Tutorial1(connection);
				//Tutorial2(connection);
				//Tutorial3(connection);
			}

			Console.ReadKey();
		}

		private static void Tutorial1(OleDbConnection connection)
		{
			// Define a simple query here for the table Employees which we can later filter by the EmployeeID column.
			var query = new OleDbQuery(connection, @"SELECT * FROM Employees {WHERE EmployeeID [employeeId]}");
			
			// ---- * Example 1 * ---- //
			Console.WriteLine("All the employees, no filter is applied:");

			using (var r = query.CreateCommand().ExecuteReader())
			{
				while (r.Read())
					Console.WriteLine(r["FirstName"]);
			}
			Console.WriteLine("-- SQL: " + query.DbCommand.CommandText);
			Console.WriteLine();

			// ---- * Example 2 * ---- //
			Console.WriteLine("Employee with the ID 1:");

			query.SetCondition("employeeId", 1);

			using (var r = query.CreateCommand().ExecuteReader())
			{
				while (r.Read())
					Console.WriteLine(r["FirstName"]);
			}
			Console.WriteLine("-- SQL: " + query.DbCommand.CommandText);
			Console.WriteLine();

			// ---- * Example 3 * ---- //
			Console.WriteLine("Employees with the IDs 1 and 2:");

			query.SetCondition("employeeId", new[] { 1, 2 });

			using (var r = query.CreateCommand().ExecuteReader())
			{
				while (r.Read())
					Console.WriteLine(r["FirstName"]);
			}
			Console.WriteLine("-- SQL: " + query.DbCommand.CommandText);
			Console.WriteLine();
		}

		private static void Tutorial2(OleDbConnection connection)
		{
			// Define a query for the table Employees which we can later filter by City and/or HireDate.
			var query = new OleDbQuery(connection, 
				@"SELECT * FROM Employees {WHERE {City [city]} {HireDate [hireDate]} {YEAR(HireDate) [hireDateYear]}}");

			// ---- * Example 1 * ---- //
			Console.WriteLine("All the employees, no filter is applied:");

			using (var r = query.CreateCommand().ExecuteReader())
			{
				while (r.Read())
					Console.WriteLine(r["FirstName"]);
			}

			Console.WriteLine("-- SQL: " + query.DbCommand.CommandText);
			Console.WriteLine();

			// ---- * Example 2 * ---- //
			Console.WriteLine("Employees hired in year 1993:");

			query.SetCondition("hireDateYear", 1993);

			using (var r = query.CreateCommand().ExecuteReader())
			{
				while (r.Read())
					Console.WriteLine(r["FirstName"]);
			}

			Console.WriteLine("-- SQL: " + query.DbCommand.CommandText);
			Console.WriteLine();

			// ---- * Example 2 * ---- //
			Console.WriteLine("Employees hired after July 1993:");

			query.Conditions.Clear();
			query.SetCondition("hireDate", from: new DateTime(1993, 6, 1));

			using (var r = query.CreateCommand().ExecuteReader())
			{
				while (r.Read())
					Console.WriteLine(r["FirstName"]);
			}

			Console.WriteLine("-- SQL: " + query.DbCommand.CommandText);
			Console.WriteLine();

			// ---- * Example 2 * ---- //
			Console.WriteLine("Employees from London that were hired between 1993 and 1994:");

			query.Conditions.Clear();
			query.SetCondition("hireDateYear", 1993, 1994);
			query.SetCondition("city", "London");

			using (var r = query.CreateCommand().ExecuteReader())
			{
				while (r.Read())
					Console.WriteLine(r["FirstName"]);
			}

			Console.WriteLine("-- SQL: " + query.DbCommand.CommandText);
			Console.WriteLine();
		}

		private static void Tutorial3(OleDbConnection connection)
		{
			// A little bit more complex example. Here we get a summary shipping freight costs summed by each year and month so we can see
			// how much money was spent on freight. We can add filtering (and other customization) to any subquery.

			var sql = @"SELECT (OrderMonth & ' / ' & OrderYear) AS OrderYearMonth, FreightSum FROM (
							SELECT OrderYear, OrderMonth, SUM(Freight) AS FreightSum FROM (
								SELECT YEAR(OrderDate) AS OrderYear, MONTH(OrderDate) AS OrderMonth, Freight FROM Orders {WHERE {ShipCountry [shipCountry]}})
							GROUP BY OrderYear, OrderMonth) 
						{WHERE {OrderYear [orderYear]}} ORDER BY OrderYear ASC, OrderMonth ASC";

			var query = new OleDbQuery(connection, sql);

			// ---- * Example 1 * ---- //
			Console.WriteLine("Shipments from the USA:");

			query.SetCondition("shipCountry", "USA");

			using (var r = query.CreateCommand().ExecuteReader())
			{
				while (r.Read())
					Console.WriteLine($"{r["OrderYearMonth"]} - {r["FreightSum"]}");
			}
			Console.WriteLine();

			// ---- * Example 1 * ---- //
			Console.WriteLine("Shipments from the USA and UK for 1995:");

			query.SetCondition("shipCountry", new [] { "USA", "UK" });
			query.SetCondition("orderYear", 1995);

			using (var r = query.CreateCommand().ExecuteReader())
			{
				while (r.Read())
					Console.WriteLine($"{r["OrderYearMonth"]} - {r["FreightSum"]}");
			}
			Console.WriteLine();
		}
	}
}
