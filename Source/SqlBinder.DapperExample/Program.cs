using System;
using System.Collections.Generic;
using System.Data;
using System.Data.OleDb;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

using SqlBinder.DapperExample.Entities;
using Dapper;

namespace SqlBinder.DapperExample
{
	class Program
	{
		static void Main(string[] args)
		{
			var whichExample = 0;

			// Set the tutorial you want to play with. You can browse contents of the database from within Visual Studio 
			// (double click on the Northwind Traders.mdb item in the project) and experiment.
			
			switch (whichExample)
			{
				case 0: PerfTest2(); break;
				case 1: Example1(); break;
				case 2: Example2(); break;
				case 3: Example3(); break;
			}

			Console.ReadKey();
		}

		static void PerfTest()
		{
			var sw = new Stopwatch();

			using (var connection = OpenConnection())
			{
				// Dapper
				sw.Start();
				for (var i = 0; i < 1000; i++)
				{
					connection.Query<Employee>(
						"SELECT * FROM Employees WHERE City IN :city",
						new Dictionary<string, object> {["city"] = new[] {"London", "Seattle"}}, buffered: true);
				}
				sw.Stop();
				Console.WriteLine("Elap1: " + sw.Elapsed.TotalMilliseconds);

				// SqlBinder + Dapper
				sw.Restart();
				var query = new Query("SELECT * FROM Employees {WHERE {City :city}}");
				for (var i = 0; i < 1000; i++)
				{
					query.SetCondition("city", new[] { "London", "Seattle" });
					query.GetSql();
					connection.Query<Employee>(query.OutputSql, query.SqlParameters, buffered: true);
				}
				sw.Stop();
				Console.WriteLine("Elap2: " + sw.Elapsed.TotalMilliseconds);
				
				// Dapper
				sw.Restart();
				for (var i = 0; i < 1000; i++)
				{
					connection.Query<Employee>(
						"SELECT * FROM Employees WHERE City IN :city",
						new Dictionary<string, object> { ["city"] = new[] { "London", "Seattle" } }, buffered: true);
				}
				sw.Stop();
				Console.WriteLine("Elap3: " + sw.Elapsed.TotalMilliseconds);

				// SqlBinder + Dapper
				sw.Restart();
				query = new Query("SELECT * FROM Employees {WHERE {City :city}}");
				for (var i = 0; i < 1000; i++)
				{
					query.SetCondition("city", new[] { "London", "Seattle" });
					query.GetSql();
					var cmdDef = new CommandDefinition(query.OutputSql, query.SqlParameters, commandType: CommandType.Text);
					connection.Query<Employee>(cmdDef);
				}
				sw.Stop();
				Console.WriteLine("Elap4: " + sw.Elapsed.TotalMilliseconds);
			}
		}

		static void PerfTest2()
		{
			var sw = new Stopwatch();

			using (var connection = OpenConnection())
			{
				// Dapper
				sw.Start();
				for (var i = 0; i < 1000; i++)
				{
					connection.Query<Employee>(
						"SELECT E.*, 'Test' AS Test FROM Employees E WHERE City IN :city",
						new Dictionary<string, object> { ["city"] = new[] { "London", "Seattle" } }, buffered: true);
				}
				sw.Stop();
				Console.WriteLine("Elap1: " + sw.Elapsed.TotalMilliseconds);

				// SqlBinder + Dapper
				sw.Restart();
				var query = new Query("SELECT * FROM Employees {WHERE {City :city}}");
				for (var i = 0; i < 1000; i++)
				{
					query.SetCondition("city", new[] { "London", "Seattle" });
					query.GetSql();
					connection.Query<Employee>(query.OutputSql, query.SqlParameters, buffered: true);
				}
				sw.Stop();
				Console.WriteLine("Elap2: " + sw.Elapsed.TotalMilliseconds);

				// Dapper
				sw.Restart();
				for (var i = 0; i < 1000; i++)
				{
					connection.Query<Employee>(
						"SELECT * FROM Employees WHERE City IN :city",
						new Dictionary<string, object> { ["city"] = new[] { "London", "Seattle" } }, buffered: true);
				}
				sw.Stop();
				Console.WriteLine("Elap3: " + sw.Elapsed.TotalMilliseconds);

				// SqlBinder + Dapper
				sw.Restart();
				query = new Query("SELECT * FROM Employees {WHERE {City :city}}");
				for (var i = 0; i < 1000; i++)
				{
					query.SetCondition("city", new[] { "London", "Seattle" });
					query.GetSql();
					connection.Query<Employee>(query.OutputSql, query.SqlParameters, buffered: true);
				}
				sw.Stop();
				Console.WriteLine("Elap4: " + sw.Elapsed.TotalMilliseconds);
			}
		}

		static void Example1()
		{
			using (var connection = OpenConnection())
			{
				Console.WriteLine("### Example 1, Just Dapper");

				Console.WriteLine("-- All Employees --");
				PrintEmployees(connection.Query<Employee>("SELECT * FROM Employees"));

				Console.WriteLine("-- Employees From London --");
				PrintEmployees(connection.Query<Employee>(
					"SELECT * FROM Employees WHERE City = :city", 
					new Dictionary<string, object> { ["city"] = "London" }));

				Console.WriteLine("-- Employees From London or Seattle --");
				PrintEmployees(connection.Query<Employee>(
					"SELECT * FROM Employees WHERE City IN :city",
					new Dictionary<string, object> { ["city"] = new [] { "London", "Seattle" } }));
			}
		}

		static void Example2()
		{
			// With SqlBinder, you don't have to recreate the SQL. You just manipulate the list of conditions you
			// need for every new query, SqlBinder will adapt the SQL for you. 

			using (var connection = OpenConnection())
			{
				var query = new Query("SELECT * FROM Employees {WHERE {City :city}}");

				Console.WriteLine("### Example 2, Dapper with SqlBinder");
				Console.WriteLine(query.SqlBinderScript);
				Console.WriteLine();

				Console.WriteLine("-- All Employees --");
				PrintEmployees(connection.Query<Employee>(query.GetSql(), query.SqlParameters));

				Console.WriteLine("-- Employees From London --");
				query.SetCondition("city", "London");
				PrintEmployees(connection.Query<Employee>(query.GetSql(), query.SqlParameters));

				Console.WriteLine("-- Employees From London or Seattle --");
				query.SetCondition("city", new [] { "London", "Seattle" });
				PrintEmployees(connection.Query<Employee>(query.GetSql(), query.SqlParameters));
			}
		}

		static void Example3()
		{
			using (var connection = OpenConnection())
			{
				Console.WriteLine("### Example 3, Dapper with SqlBinder");

				var query = new Query(GetSqlBinderScript("CategorySales.sql"));

				Console.WriteLine("-- Category Sales --");
				var sql = query.GetSql();
				var categorySales = connection.Query<CategorySale>(sql);				
				PrintSales();

				Console.WriteLine("-- Category Sales After July 1996 --");
				query.SetCondition("shippingDates", new DateTime(1995, 7, 1), NumericOperator.IsGreaterThanOrEqualTo);
				sql = query.GetSql();
				categorySales = connection.Query<CategorySale>(sql, query.SqlParameters);
				PrintSales();

				Console.WriteLine("-- Category Sales for Beverages and Seafood After July 1996 --");
				query.SetCondition("categoryIds", new [] { 1, 8 });
				sql = query.GetSql();
				categorySales = connection.Query<CategorySale>(sql, query.SqlParameters);
				PrintSales();

				void PrintSales()
				{
					foreach (var sale in categorySales)
						Console.WriteLine("\t" +
						                  $"{sale.CategoryId.ToString().PadRight(3)} " +
						                  $"{sale.CategoryName.PadRight(20)} " +
						                  $"{sale.TotalSales.ToString("C").PadRight(20)}");
				}
			}
		}


		static void PrintEmployees(IEnumerable<Employee> emps)
		{
			Console.WriteLine("Employees:");
			foreach (var emp in emps)
				Console.WriteLine($"\t{emp.FirstName} {emp.LastName}");
			Console.WriteLine();
		}

		static IDbConnection OpenConnection()
		{
			var connection = new OleDbConnection("Provider=Microsoft.Jet.OLEDB.4.0;Data Source=Northwind Traders.mdb;");
			connection.Open();
			return connection;
		}

		/// <summary>
		/// Reads the embedded sql file from the assembly's manifest. This is a pretty safe way to store your sql queries.
		/// </summary>
		static string GetSqlBinderScript(string fileName)
		{
			var asm = Assembly.GetExecutingAssembly();
			var resPath = $"{asm.GetName().Name}.OleDbSql.{fileName}";
			using (var stream = asm.GetManifestResourceStream(resPath))
			{
				if (stream == null)
					throw new FileNotFoundException("Could not find SqlBinder script in the manifest!", resPath);
				return new StreamReader(stream).ReadToEnd();
			}
		}
	}
}
