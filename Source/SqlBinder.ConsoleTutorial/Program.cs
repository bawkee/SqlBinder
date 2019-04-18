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

				// Set the tutorial you want to play with. You can browse contents of the database from within Visual Studio 
				// (double click on the Northwind Traders.mdb item in the project) and experiment.

				var chooseTut = 1;

				switch (chooseTut)
				{
					case 1: Tutorial1(connection); break;
					case 2: Tutorial2(connection); break;
					case 3: Tutorial3(connection); break;
				}
			}

			Console.ReadKey();
		}

		private static void Tutorial1(IDbConnection connection)
		{
			// Define a simple query here for the table Employees which we can later filter by the EmployeeID column.
			var query = new DbQuery(connection, @"SELECT * FROM Employees {WHERE EmployeeID :employeeId}");

			Console.WriteLine("-- Tutorial 1 SqlBinder Script --");
			Console.WriteLine(query.SqlBinderScript);
			Console.WriteLine();

			// ---- * Example 1 * ---- //
			query.CreateCommand();

			Console.WriteLine("-- Example 1 --");
			Console.WriteLine("All the employees, no filter is applied.");

			Console.WriteLine("-- SQL --");
			Console.WriteLine(query.DbCommand.CommandText);

			Console.WriteLine("-- Results --");
			using (var r = query.DbCommand.ExecuteReader())
			{
				while (r.Read())
					Console.WriteLine(r["FirstName"]);
			}			
			Console.WriteLine();

			// ---- * Example 2 * ---- //
			query.SetCondition("employeeId", 1);
			query.CreateCommand();

			Console.WriteLine("-- Example 2 --");
			Console.WriteLine("Employee with the ID 1.");

			Console.WriteLine("-- SQL --");
			Console.WriteLine(query.DbCommand.CommandText);

			Console.WriteLine("-- Results --");		
			using (var r = query.DbCommand.ExecuteReader())
			{
				while (r.Read())
					Console.WriteLine(r["FirstName"]);
			}
			Console.WriteLine();

			// ---- * Example 3 * ---- //
			query.SetCondition("employeeId", new[] { 1, 2 });
			query.CreateCommand();

			Console.WriteLine("-- Example 3 --");
			Console.WriteLine("Employees with the IDs 1 and 2.");

			Console.WriteLine("-- SQL --");
			Console.WriteLine(query.DbCommand.CommandText);

			Console.WriteLine("-- Results --");
			using (var r = query.DbCommand.ExecuteReader())
			{
				while (r.Read())
					Console.WriteLine(r["FirstName"]);
			}
			Console.WriteLine();
		}

		private static void Tutorial2(IDbConnection connection)
		{
			// Define a query for the table Employees which we can later filter by City and/or HireDate.
			var query = new DbQuery(connection, 
				@"SELECT * FROM Employees {WHERE {City :city} {HireDate :hireDate} {YEAR(HireDate) :hireDateYear}}");

			Console.WriteLine("-- Tutorial 2 SqlBinder Script --");
			Console.WriteLine(query.SqlBinderScript);
			Console.WriteLine();

			// ---- * Example 1 * ---- //
			query.CreateCommand();

			Console.WriteLine("-- Example 1 --");
			Console.WriteLine("All the employees, no filter is applied.");

			Console.WriteLine("-- SQL --");
			Console.WriteLine(query.DbCommand.CommandText);

			Console.WriteLine("-- Results --");
			using (var r = query.DbCommand.ExecuteReader())
			{
				while (r.Read())
					Console.WriteLine(r["FirstName"]);
			}
			Console.WriteLine();

			// ---- * Example 2 * ---- //
			query.SetCondition("hireDateYear", 1993);
			query.CreateCommand();

			Console.WriteLine("-- Example 2 --");
			Console.WriteLine("Employees hired in year 1993.");

			Console.WriteLine("-- SQL --");
			Console.WriteLine(query.DbCommand.CommandText);

			Console.WriteLine("-- Results --");
			using (var r = query.DbCommand.ExecuteReader())
			{
				while (r.Read())
					Console.WriteLine(r["FirstName"]);
			}
			Console.WriteLine();

			// ---- * Example 3 * ---- //
			query.Conditions.Clear();
			query.SetConditionRange("hireDate", from: new DateTime(1993, 6, 1));
			query.CreateCommand();

			Console.WriteLine("-- Example 3 --");
			Console.WriteLine("Employees hired after July 1993.");

			Console.WriteLine("-- SQL --");
			Console.WriteLine(query.DbCommand.CommandText);

			Console.WriteLine("-- Results --");
			using (var r = query.DbCommand.ExecuteReader())
			{
				while (r.Read())
					Console.WriteLine(r["FirstName"]);
			}
			Console.WriteLine();

			// ---- * Example 4 * ---- //
			query.Conditions.Clear();
			query.SetConditionRange("hireDateYear", 1993, 1994);
			query.SetCondition("city", "London");
			query.CreateCommand();

			Console.WriteLine("-- Example 4 --");
			Console.WriteLine("Employees from London that were hired between 1993 and 1994.");

			Console.WriteLine("-- SQL --");
			Console.WriteLine(query.DbCommand.CommandText);

			Console.WriteLine("-- Results --");
			using (var r = query.DbCommand.ExecuteReader())
			{
				while (r.Read())
					Console.WriteLine(r["FirstName"]);
			}
			Console.WriteLine();
		}

		private static void Tutorial3(IDbConnection connection)
		{
			// A little bit more complex example. Here we get a summary shipping freight costs summed by each year and month so we can see
			// how much money was spent on freight. We can add filtering (and other customization) to any subquery.

			var sql = @"SELECT (OrderMonth & ' / ' & OrderYear) AS OrderYearMonth, FreightSum FROM (
							SELECT OrderYear, OrderMonth, SUM(Freight) AS FreightSum FROM (
								SELECT YEAR(OrderDate) AS OrderYear, MONTH(OrderDate) AS OrderMonth, Freight FROM Orders {WHERE {ShipCountry :shipCountry}})
							GROUP BY OrderYear, OrderMonth) 
						{WHERE {OrderYear :orderYear}} ORDER BY OrderYear ASC, OrderMonth ASC";

			var query = new DbQuery(connection, sql);

			Console.WriteLine("-- Tutorial 3 SqlBinder Script --");
			Console.WriteLine(query.SqlBinderScript);
			Console.WriteLine();

			// ---- * Example 1 * ---- //
			query.SetCondition("shipCountry", "USA");
			query.CreateCommand();

			Console.WriteLine("-- Example 1 --");
			Console.WriteLine("Shipments from the USA.");

			Console.WriteLine("-- SQL --");
			Console.WriteLine(query.DbCommand.CommandText);

			Console.WriteLine("-- Results --");
			using (var r = query.DbCommand.ExecuteReader())
			{
				while (r.Read())
					Console.WriteLine($"{r["OrderYearMonth"]} - {r["FreightSum"]}");
			}
			Console.WriteLine();

			// ---- * Example 2 * ---- //
			query.SetCondition("shipCountry", new[] { "USA", "UK" });
			query.SetCondition("orderYear", 1995);
			query.CreateCommand();

			Console.WriteLine("-- Example 2 --");
			Console.WriteLine("Shipments from the USA and UK for 1995.");

			Console.WriteLine("-- SQL --");
			Console.WriteLine(query.DbCommand.CommandText);

			Console.WriteLine("-- Results --");
			using (var r = query.DbCommand.ExecuteReader())
			{
				while (r.Read())
					Console.WriteLine($"{r["OrderYearMonth"]} - {r["FreightSum"]}");
			}
			Console.WriteLine();
		}
	}
}
