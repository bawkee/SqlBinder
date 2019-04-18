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
		static void Main()
		{
			var whichExample = 4.2;

			// Set the example you want to play with. You can browse contents of the database from within Visual Studio 
			// (double click on the Northwind Traders.mdb item in the project) and experiment.						

			switch (whichExample)
			{
				case 0.0: PerfTestSqlServer(); break;
				case 0.1: PerfTestAccess(); break;
				case 0.2: PerfCompareAccess(); break;
				case 1: Example1(); break;
				case 2: Example2(); break;
				case 3: Example3(); break;
				case 4.0: Example4_SlqBuilder(); break;
				case 4.1: Example4_SqlBinder(); break;
				case 4.2:
					Example4_SlqBuilder();
					Example4_SqlBinder();
					Example4_SqlBinderNice();
					break;
			}

			Console.ReadKey();
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

				var query = new Query(GetEmbeddedSql("CategorySales.sql"));

				Console.WriteLine("-- Category Sales --");
				var sql = query.GetSql();
				var categorySales = connection.Query<CategorySale>(sql);
				PrintSales(categorySales);

				Console.WriteLine("-- Category Sales After July 1996 --");
				query.SetCondition("shippingDates", new DateTime(1995, 7, 1), NumericOperator.IsGreaterThanOrEqualTo);
				sql = query.GetSql();
				categorySales = connection.Query<CategorySale>(sql, query.SqlParameters);
				PrintSales(categorySales);

				Console.WriteLine("-- Category Sales for Beverages and Seafood After July 1996 --");
				query.SetCondition("categoryIds", new [] { 1, 8 });
				sql = query.GetSql();
				categorySales = connection.Query<CategorySale>(sql, query.SqlParameters);
				PrintSales(categorySales);
			}
		}

		static void Example4_SlqBuilder()
		{
			Console.WriteLine("### Example 4.1, Just Dapper (SqlBuilder)");
			using (var connection = OpenOleDbConnection())
			{
				Console.WriteLine("Case 1");
				PrintSales(GetCategorySales_SqlBuilder(connection,
					categoryIds: new[] {1},
					fromShippingDate: new DateTime(1995, 1, 1),
					shippingCountries: new[] {"France"}));

				Console.WriteLine("Case 2");
				PrintSales(GetCategorySales_SqlBuilder(connection,
					categoryIds: new[] { 1, 2 },
					toShippingDate: new DateTime(1996, 1, 1),
					shippingCountries: new[] { "Austria" }));

				Console.WriteLine("Case 3");
				PrintSales(GetCategorySales_SqlBuilder(connection,
					categoryIds: new[] { 3, 4 },
					fromShippingDate: new DateTime(1995, 1, 1), toShippingDate: new DateTime(1996, 1, 1),
					shippingCountries: new[] { "Italy" }));

				Console.WriteLine("Case 4");
				PrintSales(GetCategorySales_SqlBuilder(connection,
					categoryIds: new[] { 5, 6, 7, 8 },
					toShippingDate: new DateTime(1995, 1, 1),
					shippingCountries: new[] { "Spain" }));

				Console.WriteLine("Case 5");
				PrintSales(GetCategorySales_SqlBuilder(connection,
					categoryIds: new[] { 1, 2, 3 }));
			}
		}

		static void Example4_SqlBinder()
		{
			Console.WriteLine("### Example 4.2, SqlBinder + Dapper");
			using (var connection = OpenOleDbConnection())
			{
				Console.WriteLine("Case 1");
				PrintSales(GetCategorySales_SqlBinder(connection,
					categoryIds: new[] { 1 },
					fromShippingDate: new DateTime(1995, 1, 1),
					shippingCountries: new[] { "France" }));

				Console.WriteLine("Case 2");
				PrintSales(GetCategorySales_SqlBinder(connection,
					categoryIds: new[] { 1, 2 },
					toShippingDate: new DateTime(1996, 1, 1),
					shippingCountries: new[] { "Austria" }));

				Console.WriteLine("Case 3");
				PrintSales(GetCategorySales_SqlBinder(connection,
					categoryIds: new[] { 3, 4 },
					fromShippingDate: new DateTime(1995, 1, 1), toShippingDate: new DateTime(1996, 1, 1),
					shippingCountries: new[] { "Italy" }));
				
				Console.WriteLine("Case 4");
				PrintSales(GetCategorySales_SqlBinder(connection,
					categoryIds: new[] { 5, 6, 7, 8 },
					toShippingDate: new DateTime(1995, 1, 1),
					shippingCountries: new[] { "Spain" }));

				Console.WriteLine("Case 5");
				PrintSales(GetCategorySales_SqlBinder(connection,
					categoryIds: new[] { 1, 2, 3 }));
			}
		}

		static void Example4_SqlBinderNice()
		{
			Console.WriteLine("### Example 4.2, SqlBinder + Dapper");
			using (var connection = OpenOleDbConnection())
			{
				Console.WriteLine("Case 1");
				PrintSales(GetCategorySales_TheNiceWay(connection,
					categoryIds: new[] { 1 },
					fromShippingDate: new DateTime(1995, 1, 1),
					shippingCountries: new[] { "France" }));

				Console.WriteLine("Case 2");
				PrintSales(GetCategorySales_TheNiceWay(connection,
					categoryIds: new[] { 1, 2 },
					toShippingDate: new DateTime(1996, 1, 1),
					shippingCountries: new[] { "Austria" }));

				Console.WriteLine("Case 3");
				PrintSales(GetCategorySales_TheNiceWay(connection,
					categoryIds: new[] { 3, 4 },
					fromShippingDate: new DateTime(1995, 1, 1), toShippingDate: new DateTime(1996, 1, 1),
					shippingCountries: new[] { "Italy" }));

				Console.WriteLine("Case 4");
				PrintSales(GetCategorySales_TheNiceWay(connection,
					categoryIds: new[] { 5, 6, 7, 8 },
					toShippingDate: new DateTime(1995, 1, 1),
					shippingCountries: new[] { "Spain" }));

				Console.WriteLine("Case 5");
				PrintSales(GetCategorySales_TheNiceWay(connection,
					categoryIds: new[] { 1, 2, 3 }));
			}
		}

		/// <summary>
		/// CategorySales query done by Dapper.Contib (SqlBuilder)
		/// </summary>
		private static IEnumerable<CategorySale> GetCategorySales_SqlBuilder(
			IDbConnection connection,
			int[] categoryIds = null,
			DateTime? fromShippingDate = null, DateTime? toShippingDate = null,
			DateTime? fromOrderDate = null, DateTime? toOrderDate = null,
			string[] shippingCountries = null)
		{
			var builder = new SqlBuilder();

			var sql = @"SELECT
				Categories.CategoryID, 
				Categories.CategoryName, 
				SUM(CCUR(OrderDetails.UnitPrice * OrderDetails.Quantity * 
				(1 - OrderDetails.Discount) / 100) * 100) AS TotalSales
			FROM (((Categories		
				INNER JOIN Products ON Products.CategoryID = Categories.CategoryID)
				INNER JOIN OrderDetails ON OrderDetails.ProductID = Products.ProductID)
				INNER JOIN Orders ON Orders.OrderID = OrderDetails.OrderID) /**where**/
			GROUP BY 
				Categories.CategoryID, Categories.CategoryName";
			
			var template = builder.AddTemplate(sql);

			if (fromShippingDate.HasValue && toShippingDate.HasValue)
				builder.Where("Orders.ShippedDate BETWEEN :fromShippingDate AND :toShippingDate", 
					new { fromShippingDate, toShippingDate });
			else if (fromShippingDate.HasValue)
				builder.Where("Orders.ShippedDate >= :fromShippingDate", new { fromShippingDate });
			else if (toShippingDate.HasValue)
				builder.Where("Orders.ShippedDate <= :toShippingDate", new { toShippingDate });

			if (fromOrderDate.HasValue && toOrderDate.HasValue)
				builder.Where("Orders.ShippedDate BETWEEN :fromOrderDate AND :toOrderDate", 
					new { fromOrderDate, toOrderDate });
			else if (fromOrderDate.HasValue)
				builder.Where("Orders.ShippedDate >= :fromOrderDate", new { fromOrderDate });
			else if (toOrderDate.HasValue)
				builder.Where("Orders.ShippedDate <= :toOrderDate", new { toOrderDate });

			if (categoryIds?.Any() ?? false)
				builder.Where("Categories.CategoryID IN :categoryIds", new { categoryIds });

			if (shippingCountries?.Any() ?? false)
				builder.Where("Orders.ShipCountry IN :shippingCountries", new { shippingCountries });

			var sw = new Stopwatch();
			sw.Start();

			var ret = connection.Query<CategorySale>(template.RawSql, template.Parameters);

			sw.Stop();
			Debug.WriteLine("ELAP1:" + sw.Elapsed.TotalMilliseconds);


			return ret;
		}

		/// <summary>
		/// CategorySales query done by SqlBinder + Dapper. It uses a slightly different query to emphasis the possibility of 
		/// optional subqueries. 
		/// </summary>
		private static IEnumerable<CategorySale> GetCategorySales_SqlBinder(
			IDbConnection connection, 
			IEnumerable<int> categoryIds = null, 
			DateTime? fromShippingDate = null, DateTime? toShippingDate = null,
			DateTime? fromOrderDate = null, DateTime? toOrderDate = null,
			IEnumerable<string> shippingCountries = null)
		{
			var query = new Query(@"SELECT
	Categories.CategoryID, 
	Categories.CategoryName, 
	SUM(CCUR(OrderDetails.UnitPrice * OrderDetails.Quantity * (1 - OrderDetails.Discount) / 100) * 100) AS TotalSales
FROM ((Categories		
	INNER JOIN Products ON Products.CategoryID = Categories.CategoryID)
	INNER JOIN OrderDetails ON OrderDetails.ProductID = Products.ProductID)
{WHERE 	
	{OrderDetails.OrderID IN (SELECT OrderID FROM Orders WHERE 
			{Orders.ShippedDate :shippingDates} 
			{Orders.OrderDate :orderDates}
			{Orders.ShipCountry :shippingCountries})} 
	{Categories.CategoryID :categoryIds}}
GROUP BY 
	Categories.CategoryID, Categories.CategoryName");

			query.SetCondition("categoryIds", categoryIds);			
			query.SetConditionRange("shippingDates", fromShippingDate, toShippingDate);
			query.SetConditionRange("orderDates", fromOrderDate, toOrderDate);			
			query.SetCondition("shippingCountries", shippingCountries);

			var sw = new Stopwatch();
			sw.Start();

			var ret = connection.Query<CategorySale>(query.GetSql(), query.SqlParameters);

			sw.Stop();
			Debug.WriteLine("ELAP2:" + sw.Elapsed.TotalMilliseconds);

			return ret;
		}


		private static IEnumerable<CategorySale> GetCategorySales_TheNiceWay(
			IDbConnection connection,
			IEnumerable<int> categoryIds = null,
			DateTime? fromShippingDate = null, DateTime? toShippingDate = null,
			DateTime? fromOrderDate = null, DateTime? toOrderDate = null,
			IEnumerable<string> shippingCountries = null)
		{
			var query = new Query(GetEmbeddedSql("CategorySales.sql"));

			query.SetCondition("categoryIds", categoryIds);
			query.SetConditionRange("shippingDates", fromShippingDate, toShippingDate);
			query.SetConditionRange("orderDates", fromOrderDate, toOrderDate);
			query.SetCondition("shippingCountries", shippingCountries);

			return connection.Query<CategorySale>(query.GetSql(), query.SqlParameters);
		}

		static void PrintSales(IEnumerable<CategorySale> categorySales)
		{
			foreach (var sale in categorySales)
				Console.WriteLine("\t" +
				                  $"{sale.CategoryId.ToString().PadRight(3)} " +
				                  $"{sale.CategoryName.PadRight(20)} " +
				                  $"{sale.TotalSales.ToString("C").PadRight(20)}");
		}

		static void PrintEmployees(IEnumerable<Employee> emps)
		{
			Console.WriteLine("Employees:");
			foreach (var emp in emps)
				Console.WriteLine($"\t{emp.FirstName} {emp.LastName}");
			Console.WriteLine();
		}

		/// <summary>
		/// Purpose of this test is to see how much overhead would be adding SqlBinder on top of Dapper. There's very little. You could probably save
		/// that difference by forking or extending Dapper to not attempt any parsing and just do the ORM part.
		/// </summary>
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
					var query = new Query("SELECT * FROM Employees {WHERE {EmployeeID @id}}");
					for (var i = 0; i < 500; i++)
					{
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
			Console.WriteLine(" * First iteration is not accounted for in AVG.");
		}

		/// <summary>
		/// Same as before but with LocalDB which is much faster. Both tests are polluted by the DB implementation but this at least gives 
		/// us another perspective.
		/// </summary>
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
							new Dictionary<string, object> { ["id"] = new[] { i, 1 + i, 2 + i } });
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
						query.SetCondition("id", new[] { i, 1 + i, 2 + i });
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
			Console.WriteLine(" * First iteration is not accounted for in AVG.");
		}

		/// <summary>
		/// Compares parsing performance between Dapper and SqlBinder. There's not much point to it since the two are doing entirely different
		/// things but I just wanted to observe how much 'slower' would using SqlBinder be. It turns out it's so fast the difference is insignificant.
		/// I've used a relatively complex query for this test.
		/// </summary>
		static void PerfCompareAccess()
		{
			using (var connection = OpenOleDbConnection())
			{
				var script = GetEmbeddedSql("CategorySales.sql");

				var dapperQuery = new Query(script);

				dapperQuery.SetCondition("shippingDates", new DateTime(1995, 7, 1), NumericOperator.IsGreaterThanOrEqualTo);
				dapperQuery.SetCondition("categoryIds", new[] { 1, 8 });

				var sql = dapperQuery.GetSql();
				var bindParams = dapperQuery.SqlParameters;

				const int CNT = 100;

				var sqlBinderTimes = new List<double>(CNT);
				var dapperTimes = new List<double>(CNT);

				var sw = new Stopwatch();

				var query = new DbQuery(connection);
				query.DisableParserCache = true;

				for (var i = 0; i < CNT; i++)
				{
					var poke = $" OR {i} = {i}"; // Add junk to the sql so it 100% doesn't get cached somewhere by something
					var queryPerf = sql + poke;
					var scriptPerf = script + poke;

					sw.Restart();

					query.SqlBinderScript = scriptPerf;
					query.SetCondition("shippingDates", new DateTime(1995, 7, 1), NumericOperator.IsGreaterThanOrEqualTo);
					query.SetCondition("categoryIds", new[] { 1, 8 });
					query.CreateCommand().ExecuteReader().Close();

					sw.Stop();
					sqlBinderTimes.Add(sw.Elapsed.TotalMilliseconds);

					sw.Restart();

					connection.ExecuteReader(queryPerf, bindParams).Close();

					sw.Stop();
					dapperTimes.Add(sw.Elapsed.TotalMilliseconds);
				}

				Console.WriteLine("SqlBinder:".PadRight(15) + sqlBinderTimes.Average());
				Console.WriteLine("Dapper:".PadRight(15) + dapperTimes.Average());
			}
		}

		static IDbConnection OpenOleDbConnection()
		{
			var connection = new OleDbConnection("Provider=Microsoft.Jet.OLEDB.4.0;Data Source=Northwind Traders.mdb;");
			connection.Open();
			return connection;
		}

		static IDbConnection OpenSqlServerConnection()
		{
			var dbName = "(LocalDb)\\v11.0";
			var connection = new SqlConnection($"Data Source={dbName};Initial Catalog=tempdb;Integrated Security=True");
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
		static string GetEmbeddedSql(string fileName)
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
