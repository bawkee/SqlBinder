using Dapper;
using System;
using System.Data;
using System.Data.OleDb;
using System.Data.SqlClient;
using System.Linq;
using System.Collections.Generic;

public class Program
{
	public class OrderDetail
	{
		public int OrderID { get; set; }
		public int ProductID { get; set; }
	}

	public class Order
	{
		public int OrderID { get; set; }
		public List<OrderDetail> OrderDetails { get; set; } = new List<OrderDetail>();
	}

	public static void Main()
	{
		string sql = "SELECT 'Dummy' AS Dummy, Orders.OrderID AS OrderID, OrderDetails.ProductID FROM Orders INNER JOIN OrderDetails ON Orders.OrderID = OrderDetails.OrderID WHERE Orders.OrderID = 10248";

		using (var connection = new OleDbConnection("Provider=Microsoft.Jet.OLEDB.4.0;Data Source=Northwind Traders.mdb;"))
		{
			connection.Open();

			var cmd = connection.CreateCommand();
			cmd.CommandText = sql;
			cmd.CommandType = CommandType.Text;

			var reader = cmd.ExecuteReader();

			for (var i = 0; i < reader.FieldCount - 1; i++)
			{				
				System.Diagnostics.Debug.WriteLine(reader.GetName(i));
			}

			var orderDictionary = new Dictionary<int, Order>();


			var list = connection.Query<Order, OrderDetail, Order>(
					sql,
					(order, orderDetail) =>
					{
						order.OrderDetails.Add(orderDetail);
						return order;
					},
					splitOn: "OrderID")
				.ToList();

			Console.WriteLine(list.Count);

			Console.ReadKey();
		}
	}
}