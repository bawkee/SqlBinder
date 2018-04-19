using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using System.Data.Common;
using System.Data.OleDb;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SqlBinder.DemoNorthwindDal.Entities;

namespace SqlBinder.DemoNorthwindDal.OleDb
{
    public class OleDbNorthwindDal : INorthwindDal, IDisposable
	{
		private readonly OleDbConnection _connection = new OleDbConnection();

		public OleDbNorthwindDal(string northwindMdb)
		{
			_connection.ConnectionString = $"Provider=Microsoft.Jet.OLEDB.4.0;Data Source={northwindMdb};";
			_connection.Open();			
		}

		public IEnumerable<Category> GetCategories()
		{
			using (var r = CreateTextCommand("SELECT * FROM Categories").ExecuteReader())
			{
				while (r?.Read() ?? false)
					yield return new Category
					{
						CategoryId = (int) r["CategoryID"],
						Name = (string) r["CategoryName"],
						Description = (string) r["Description"]
					};
			}			
		}

		public IEnumerable<Supplier> GetSuppliers()
		{
			using (var r = CreateTextCommand("SELECT * FROM Suppliers").ExecuteReader())
			{
				while (r?.Read() ?? false)
					yield return new Supplier
					{
						SupplierId = (int)r["SupplierID"],
						CompanyName = (string)r["CompanyName"],
						ContactName = r["ContactName"] as string,
						ContactTitle = r["ContactTitle"] as string,
						Address = r["Address"] as string,
						City = r["City"] as string,
						Region = r["Region"] as string,
						PostalCode = r["PostalCode"] as string,
						Country = r["Country"] as string,
						Phone = r["Phone"] as string,
						Fax = r["Fax"] as string,
						HomePage = r["HomePage"] as string,
					};
			}
		}

		public IEnumerable<Customer> GetCustomers()
		{
			using (var r = CreateTextCommand("SELECT * FROM Customers").ExecuteReader())
			{
				while (r?.Read() ?? false)
					yield return new Customer
					{
						CustomerId = r["CustomerId"] as string,
						CompanyName = r["CompanyName"] as string,
						ContactTitle = r["ContactTitle"] as string,
						ContactName = r["ContactName"] as string,
						Address = r["Address"] as string,
						City = r["City"] as string,
						Region = r["Region"] as string,
						PostalCode = r["PostalCode"] as string,
						Country = r["Country"] as string						
					};
			}
		}

		public IEnumerable<Shipper> GetShippers()
		{
			using (var r = CreateTextCommand("SELECT * FROM Shippers").ExecuteReader())
			{
				while (r?.Read() ?? false)
					yield return new Shipper
					{
						ShipperId = (int)r["ShipperId"],
						CompanyName = r["CompanyName"] as string,
						Phone = r["Phone"] as string,
					};
			}
		}

		public IEnumerable<Employee> GetEmployees()
		{
			using (var r = CreateTextCommand("SELECT * FROM Employees").ExecuteReader())
			{
				while (r?.Read() ?? false)
					yield return new Employee
					{
						EmployeeId = (int)r["EmployeeId"],
						FirstName = r["FirstName"] as string,
						LastName = r["LastName"] as string,
						Title = r["Title"] as string,
						HireDate = r["HireDate"] as DateTime?,
					};
			}
		}

		public IEnumerable<CategorySale> GetCategorySales(int[] categoryIds = null, DateTime? fromDate = null, DateTime? toDate = null)
		{
			var query = new OleDbQuery(_connection, File.ReadAllText("OleDbSql\\CategorySales.sql"));

			if (categoryIds?.Any() ?? false)
				query.SetCondition("categoryIds", categoryIds);

			if (fromDate.HasValue || toDate.HasValue)
				query.SetCondition("shippingDates", fromDate, toDate);

			using (var r = query.CreateCommand().ExecuteReader())
			{
				while (r.Read())
				{
					yield return new CategorySale
					{
						CategoryId = (int)r["CategoryID"],
						CategoryName = (string)r["CategoryName"],
						TotalSales = r["TotalSales"] as decimal? ?? 0
					};
				}
			}

			TraceQuery("Category Sales", query);
		}

		public IEnumerable<Product> GetProducts(decimal? productId = null,
			string productName = null,
			int[] supplierIds = null,
			int[] categoryIds = null,
			decimal? unitPriceFrom = null,
			decimal? unitPriceTo = null,
			bool? isDiscontinued = null,
			bool priceGreaterThanAvg = false)
		{
			var query = new OleDbQuery(_connection, File.ReadAllText("OleDbSql\\Products.sql"));
			
			if (productId != null)
				query.SetCondition("productId", productId);
			else
			{
				if (!string.IsNullOrEmpty(productName))
					query.SetCondition("productName", $"%{productName}%", StringOperator.IsLike);

				if (supplierIds?.Any() ?? false)
					query.SetCondition("supplierIds", supplierIds);

				if (categoryIds?.Any() ?? false)
					query.SetCondition("categoryIds", categoryIds);

				if (unitPriceFrom.HasValue || unitPriceTo.HasValue)
					query.SetCondition("unitPrice", unitPriceFrom, unitPriceTo);

				if (isDiscontinued.HasValue)
					query.SetCondition("isDiscontinued", isDiscontinued.Value);

				if (priceGreaterThanAvg)
					query.DefineVariable("priceGreaterThanAvg", "> (SELECT AVG(UnitPrice) From Products)");
			}

			using (var r = query.CreateCommand().ExecuteReader())
			{
				while (r.Read())
				{
					yield return new Product
					{
						ProductId = (int)r["ProductID"],
						ProductName = (string)r["ProductName"],
						CategoryName = (string)r["CategoryName"],
						SupplierCompany = (string)r["SupplierCompany"],
						SupplierId = (int)r["SupplierID"],
						CategoryId = (int)r["CategoryID"],
						QuantityPerUnit = (string)r["QuantityPerUnit"],
						UnitPrice = (decimal)r["UnitPrice"],
						UnitsInStock = Convert.ToInt32(r["UnitsInStock"] as Int16?),
						UnitsOnOrder = Convert.ToInt32(r["UnitsOnOrder"] as Int16?),
						Discontinued = (bool)r["Discontinued"],
					};
				}
			}

			TraceQuery("Products", query);
		}

		public IEnumerable<string> GetShippingCountries()
		{
			using (var r = CreateTextCommand("SELECT ShipCountry FROM Orders GROUP BY ShipCountry").ExecuteReader())
			{
				while (r?.Read() ?? false)
					yield return r[0] as string;
			}
		}

		public IEnumerable<string> GetShippingCities(string shippingCountry = null)
		{
			var query = new OleDbQuery(_connection, "SELECT ShipCity FROM Orders {WHERE {ShipCountry [shippingCountry]}} GROUP BY ShipCity");

			if (shippingCountry != null)
				query.SetCondition("shippingCountry", shippingCountry);

			using (var r = query.CreateCommand().ExecuteReader())
			{
				while (r.Read())
					yield return r[0] as string;					
			}
		}

		public IEnumerable<Order> GetOrders(int? orderId = null,
			int[] productIds = null,
			string[] customerIds = null,
			int[] employeeIds = null,
			int[] shipperIds = null,
			DateTime? orderDateFrom = null, DateTime? orderDateTo = null,
			DateTime? reqDateFrom = null, DateTime? reqDateTo = null,
			DateTime? shipDateFrom = null, DateTime? shipDateTo = null,
			decimal? freightFrom = null, decimal? freightTo = null,
			string shipCity = null,
			string shipCountry = null)
		{
			var query = new OleDbQuery(_connection, File.ReadAllText("OleDbSql\\Orders.sql"));

			if (orderId.HasValue)
				query.SetCondition("orderId", orderId);
			else
			{
				if (productIds?.Any() ?? false)
					query.SetCondition("productIds", productIds);

				if (customerIds?.Any() ?? false)
					query.SetCondition("customerIds", customerIds);

				if (employeeIds?.Any() ?? false)
					query.SetCondition("employeeIds", employeeIds);

				if (shipperIds?.Any() ?? false)
					query.SetCondition("shipperIds", shipperIds);

				if (freightFrom.HasValue || freightTo.HasValue)
					query.SetCondition("freight", freightFrom, freightTo);

				if (orderDateFrom.HasValue || orderDateTo.HasValue)
					query.SetCondition("orderDate", orderDateFrom, orderDateTo);

				if (reqDateFrom.HasValue || reqDateTo.HasValue)
					query.SetCondition("reqDate", reqDateFrom, reqDateTo);

				if (shipDateFrom.HasValue || shipDateTo.HasValue)
					query.SetCondition("shipDate", shipDateFrom, shipDateTo);

				if (shipCity != null)
					query.SetCondition("shipCity", shipCity);

				if (shipCountry!= null)
					query.SetCondition("shipCountry", shipCountry);
			}

			using (var r = query.CreateCommand().ExecuteReader())
			{
				while (r.Read())
				{
					yield return new Order
					{
						OrderId = (int)r["OrderId"],
						CustomerId = (string)r["CustomerId"],
						EmployeeId = (int)r["EmployeeId"],
						OrderDate = r["OrderDate"] as DateTime?,
						RequiredDate = r["RequiredDate"] as DateTime?,
						ShippedDate = r["ShippedDate"] as DateTime?,
						ShipperId = (int)r["ShipVia"],
						Freight = (decimal)r["Freight"],
						CustomerName = (string)r["CustomerName"],
						EmployeeName = (string)r["EmployeeName"],
						ShippedVia = r["ShippedVia"] as string,
						ShipName = r["ShipName"] as string,
						ShipAddress = r["ShipAddress"] as string,
						ShipCity = r["ShipCity"] as string,
						ShipRegion = r["ShipRegion"] as string,
						ShipCountry = r["ShipCountry"] as string,
						ShipPostalCode = r["ShipPostalCode"] as string,
					};
				}
			}

			TraceQuery("Orders", query);

		}

		private OleDbCommand CreateTextCommand(string text)
		{
			var cmd = _connection.CreateCommand();
			cmd.CommandText = text;
			cmd.CommandType = CommandType.Text;
			return cmd;
		}

		public ObservableCollection<string> TraceLog { get; } = new ObservableCollection<string>();

		private void Trace(string message = "") => TraceLog.Add(message);

		private void TraceQuery(string name, OleDbQuery sqlBinderQuery)
		{
			Trace($"-- {name} SqlBinder Script ".PadRight(40, '-') + '>');
			Trace(sqlBinderQuery.SqlBinderScript);
			Trace();
			Trace($"-- {name} Output Sql ".PadRight(40, '-') + '>');
			Trace(sqlBinderQuery.DbCommand.CommandText);
			Trace();
		}

		public void Dispose()
		{
			_connection?.Dispose();
		}
	}
}
