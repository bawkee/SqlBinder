using System;
using System.Collections.Generic;
using System.Data;
using System.Data.OleDb;
using System.Data.SqlClient;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using SqlBinder.DapperExample.Entities;
using Dapper;

namespace SqlBinder.DapperExample
{
	class Program
	{
		static void Main(string[] args)
		{
			var whichExample = 0.0;

			// Set the tutorial you want to play with. You can browse contents of the database from within Visual Studio 
			// (double click on the Northwind Traders.mdb item in the project) and experiment.
			
			switch (whichExample)
			{
				case 0.0: PerfTestSqlServer(); break;
				case 0.1: PerfTestAccess(); break;
				case 1: Example1(); break;
				case 2: Example2(); break;
				case 3: Example3(); break;
			}

			Console.ReadKey();
		}

		static void PerfTestAccess()
		{
			var sw = new Stopwatch();

			Console.WriteLine("Dapper".PadLeft(10) + " " + "+SqlBinder".PadLeft(10));
			Console.WriteLine(new string('-', 21));

			var dapper = new List<double>();
			var plusSqlbinder = new List<double>();

			for (var n = 0; n < 10; n++)
			{
				using (var connection = OpenOleDbConnection())
				{
					// Dapper
					sw.Restart();
					for (var i = 0; i < 500; i++)
					{
						connection.Query<Employee>(
							"SELECT * FROM Employees WHERE EmployeeID IN @id",
							new Dictionary<string, object> { ["id"] = new[] { 1, 2, 3, 4 } });
					}
					sw.Stop();
					Console.Write(sw.Elapsed.TotalMilliseconds.ToString("N2").PadLeft(10));
					if (n > 0)
						dapper.Add(sw.Elapsed.TotalMilliseconds);

					Console.Write(' ');

					// SqlBinder + Dapper
					sw.Restart();					
					for (var i = 0; i < 500; i++)
					{
						var query = new Query("SELECT * FROM Employees {WHERE {EmployeeID @id}}");
						query.SetCondition("id", new[] { 1, 2, 3, 4 });
						query.GetSql();
						connection.Query<Employee>(query.OutputSql, query.SqlParameters);
					}
					sw.Stop();
					if (n > 0)
						plusSqlbinder.Add(sw.Elapsed.TotalMilliseconds);
					Console.Write(sw.Elapsed.TotalMilliseconds.ToString("N2").PadLeft(10));
					Console.WriteLine();
				}
			}

			Console.Write($"AVG {dapper.Average():N0}".PadLeft(10));
			Console.Write(' ');
			Console.Write($"AVG {plusSqlbinder.Average():N0}".PadLeft(10));

			Console.WriteLine();
			Console.WriteLine(" ^ Dapper = Just Dapper.");
			Console.WriteLine(" ^ +SqlBinder = Dapper with SqlBinder.");
		}

		static void PerfTestSqlServer()
		{
			var sw = new Stopwatch();

			Console.WriteLine("Dapper".PadLeft(10) + " " + "+SqlBinder".PadLeft(10));
			Console.WriteLine(new string('-', 21));

			var dapper = new List<double>();
			var plusSqlbinder = new List<double>();

			for (var n = 0; n < 10; n++)
			{
				using (var connection = OpenSqlServerConnection())
				{
					// Dapper
					sw.Restart();
					for (var i = 0; i < 500; i++)
					{
						connection.Query<Employee>(
							"SELECT * FROM POSTS WHERE ID IN @id",
							new Dictionary<string, object> {["id"] = new[] {i, 1 + i, 2 + i}});
					}
					sw.Stop();
					Console.Write(sw.Elapsed.TotalMilliseconds.ToString("N2").PadLeft(10));
					if (n > 0)
						dapper.Add(sw.Elapsed.TotalMilliseconds);

					Console.Write(' ');

					// SqlBinder + Dapper (cached query)
					sw.Restart();
					var query = new Query("SELECT * FROM POSTS {WHERE {ID @id}}");
					for (var i = 0; i < 500; i++)
					{
						query.SetCondition("id", new[] {i, 1 + i, 2 + i});
						query.GetSql();
						connection.Query<Employee>(query.OutputSql, query.SqlParameters);
					}
					sw.Stop();
					if (n > 0)
						plusSqlbinder.Add(sw.Elapsed.TotalMilliseconds);
					Console.Write(sw.Elapsed.TotalMilliseconds.ToString("N2").PadLeft(10));
					Console.WriteLine();
				}
			}

			Console.Write($"AVG {dapper.Average():N0}".PadLeft(10));
			Console.Write(' ');
			Console.Write($"AVG {plusSqlbinder.Average():N0}".PadLeft(10));

			Console.WriteLine();
			Console.WriteLine(" ^ Dapper = Just Dapper.");
			Console.WriteLine(" ^ +SqlBinder = Dapper with SqlBinder.");
		}

		static void Example1()
		{
			using (var connection = OpenOleDbConnection())
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
			// With SqlBinder, you don't have to recreate the SQL every time. You manipulate the list of conditions you
			// need for every new query, SqlBinder will adapt the SQL for you. 

			using (var connection = OpenOleDbConnection())
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
			using (var connection = OpenOleDbConnection())
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

		static IDbConnection OpenOleDbConnection()
		{
			var connection = new OleDbConnection("Provider=Microsoft.Jet.OLEDB.4.0;Data Source=Northwind Traders.mdb;");
			connection.Open();
			return connection;
		}

		static IDbConnection OpenSqlServerConnection()
		{
			var connection = new SqlConnection(@"Data Source=(LocalDb)\v11.0;Initial Catalog=tempdb;Integrated Security=True");
			connection.Open();

			// Taken from Dapper's benchmark project
			var cmd = connection.CreateCommand();
			cmd.CommandText = @"
				If (Object_Id('Posts') Is Null)
				Begin
					Create Table Posts
					(
						Id int identity primary key, 
						[Text] varchar(max) not null, 
						CreationDate datetime not null, 
						LastChangeDate datetime not null,
						Counter1 int,
						Counter2 int,
						Counter3 int,
						Counter4 int,
						Counter5 int,
						Counter6 int,
						Counter7 int,
						Counter8 int,
						Counter9 int
					);
	   
					Set NoCount On;
					Declare @i int = 0;

					While @i <= 5001
					Begin
						Insert Posts ([Text],CreationDate, LastChangeDate) values (replicate('x', 2000), GETDATE(), GETDATE());
						Set @i = @i + 1;
					End
				End";
			cmd.ExecuteNonQuery();

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
