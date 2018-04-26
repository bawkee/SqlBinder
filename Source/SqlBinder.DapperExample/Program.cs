using System;
using System.Collections.Generic;
using System.Data;
using System.Data.OleDb;
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
			Test4();

			Console.ReadKey();
		}

		static void TestSimplestDapperQuery()
		{
			using (var connection = OpenConnection())
			{
				PrintEmployees(connection.Query<Employee>("SELECT * FROM Employees"));
			}
		}

		static void Test2()
		{
			using (var connection = OpenConnection())
			{
				PrintEmployees(connection.Query<Employee>("SELECT * FROM Employees WHERE City = @city", new Dictionary<string, object> { ["city"] = "London" }));
			}
		}

		static void Test3()
		{
			using (var connection = OpenConnection())
			{
				var query = new Query("SELECT * FROM Employees {WHERE {City [city]}}");

				PrintEmployees(connection.Query<Employee>(query.GetSql(), query.SqlParameters));
			}
		}

		static void Test4()
		{
			using (var connection = OpenConnection())
			{
				var query = new Query(GetSqlBinderScript("Orders.sql"));
				var sql = query.GetSql();

				var orders = connection.Query<Order, OrderDetail, Order>(
					sql,
					(order, orderDetail) =>
					{
						order.OrderDetails.Add(orderDetail);
						return order;
					},
					query.SqlParameters, splitOn: "OrderID");

				Console.WriteLine("Orders:");
				foreach (var order in orders)
				{
					//Console.WriteLine($"\t{order.OrderID.ToString().PadRight(30)} {order.CustomerId.PadRight(30)}");
				//	foreach(var orderDetail in order.OrderDetails)
					//	Console.WriteLine($"\t\t{orderDetail.ProductId.ToString().PadRight(30)} {orderDetail.UnitPrice.ToString("C").PadRight(30)}");
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
			var connection = new OleDbConnection($"Provider=Microsoft.Jet.OLEDB.4.0;Data Source=Northwind Traders.mdb;");
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
