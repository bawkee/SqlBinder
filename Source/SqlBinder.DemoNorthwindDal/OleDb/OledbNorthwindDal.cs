using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using System.Data.OleDb;
using System.IO;
using System.Linq;
using System.Reflection;
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
				while (r?.Read() ?? false)
					yield return OledbOrm.CreateCategory(r);
		}

		public IEnumerable<Supplier> GetSuppliers()
		{
			using (var r = CreateTextCommand("SELECT * FROM Suppliers").ExecuteReader())
				while (r?.Read() ?? false)
					yield return OledbOrm.CreateSupplier(r);
		}

		public IEnumerable<Customer> GetCustomers()
		{
			using (var r = CreateTextCommand("SELECT * FROM Customers").ExecuteReader())
				while (r?.Read() ?? false)
					yield return OledbOrm.CreateCustomer(r);
		}

		public IEnumerable<Shipper> GetShippers()
		{
			using (var r = CreateTextCommand("SELECT * FROM Shippers").ExecuteReader())
				while (r?.Read() ?? false)
					yield return OledbOrm.CreateShipper(r);
		}

		public IEnumerable<Employee> GetEmployees()
		{
			using (var r = CreateTextCommand("SELECT * FROM Employees").ExecuteReader())
				while (r?.Read() ?? false)
					yield return OledbOrm.CreateEmployee(r);
		}

		/// <summary>
		/// Get Category Sales by building a dynamic SQL via SqlBinder. The *real* meat of this method is in the .Sql file.
		/// </summary>
		public IEnumerable<CategorySale> GetCategorySales(int[] categoryIds = null, DateTime? fromDate = null, DateTime? toDate = null)
		{
			var query = new DbQuery(_connection, GetSqlBinderScript("CategorySales.sql"));

			query.SetCondition("categoryIds", categoryIds);
			query.SetConditionRange("shippingDates", fromDate, toDate);

			using (var r = query.CreateCommand().ExecuteReader())
				while (r.Read())
					yield return OledbOrm.CreateCategorySale(r);

			TraceQuery("Category Sales", query);
		}

		/// <summary>
		/// Get Products by building a dynamic SQL via SqlBinder. The *real* meat of this method is in the .Sql file.
		/// </summary>
		public IEnumerable<Product> GetProducts(decimal? productId = null,
			string productName = null,
			int[] supplierIds = null,
			int[] categoryIds = null,
			decimal? unitPriceFrom = null,
			decimal? unitPriceTo = null,
			bool? isDiscontinued = null,
			bool priceGreaterThanAvg = false)
		{
			var query = new DbQuery(_connection, GetSqlBinderScript("Products.sql"));
			
			if (productId != null)
				query.SetCondition("productId", productId);
			else
			{
				query.SetCondition("productName", productName, StringOperator.Contains);
				query.SetCondition("supplierIds", supplierIds);
				query.SetCondition("categoryIds", categoryIds);
				query.SetConditionRange("unitPrice", unitPriceFrom, unitPriceTo);
				query.SetCondition("isDiscontinued", isDiscontinued, ignoreIfNull: true);
				if (priceGreaterThanAvg)
					query.DefineVariable("priceGreaterThanAvg", "> (SELECT AVG(UnitPrice) From Products)");
			}

			using (var r = query.CreateCommand().ExecuteReader())
				while (r.Read())
					yield return OledbOrm.CreateProduct(r);

			TraceQuery("Products", query);
		}

		public IEnumerable<string> GetShippingCountries()
		{
			using (var r = CreateTextCommand("SELECT ShipCountry FROM Orders GROUP BY ShipCountry").ExecuteReader())
				while (r?.Read() ?? false)
					yield return r[0] as string;
		}

		public IEnumerable<string> GetShippingCities(string shippingCountry = null)
		{
			var query = new DbQuery(_connection, "SELECT ShipCity FROM Orders {WHERE {ShipCountry :shippingCountry}} GROUP BY ShipCity");

			if (shippingCountry != null)
				query.SetCondition("shippingCountry", shippingCountry);

			using (var r = query.CreateCommand().ExecuteReader())
				while (r.Read())
					yield return r[0] as string;					
		}

		/// <summary>
		/// Get Orders by building a dynamic SQL via SqlBinder. The *real* meat of this method is in the .Sql file.
		/// </summary>
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
			var query = new DbQuery(_connection, GetSqlBinderScript("Orders.sql"));

			if (orderId.HasValue)
				query.SetCondition("orderId", orderId);
			else
			{
				query.SetCondition("productIds", productIds);
				query.SetCondition("customerIds", customerIds);
				query.SetCondition("employeeIds", employeeIds);
				query.SetCondition("shipperIds", shipperIds);
				query.SetConditionRange("freight", freightFrom, freightTo);
				query.SetConditionRange("orderDate", orderDateFrom, orderDateTo);
				query.SetConditionRange("reqDate", reqDateFrom, reqDateTo);
				query.SetConditionRange("shipDate", shipDateFrom, shipDateTo);
				query.SetCondition("shipCity", shipCity, ignoreIfNull: true);
				query.SetCondition("shipCountry", shipCountry, ignoreIfNull: true);
			}

			using (var r = query.CreateCommand().ExecuteReader())
				while (r.Read())
					yield return OledbOrm.CreateOrder(r);

			TraceQuery("Orders", query);
		}

		private OleDbCommand CreateTextCommand(string text)
		{
			var cmd = _connection.CreateCommand();
			cmd.CommandText = text;
			cmd.CommandType = CommandType.Text;
			return cmd;
		}

		/// <summary>
		/// Reads the embedded sql file from the assembly's manifest. This is a pretty safe way to store your sql queries.
		/// </summary>
		public static string GetSqlBinderScript(string fileName)
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

		public ObservableCollection<string> TraceLog { get; } = new ObservableCollection<string>();

		private void Trace(string message = "") => TraceLog.Add(message);

		private void TraceQuery(string name, Query sqlBinderQuery)
		{
			Trace($"-- {name} SqlBinder Script ".PadRight(40, '-') + '>');
			Trace(sqlBinderQuery.SqlBinderScript);
			Trace();
			Trace($"-- {name} Output Sql ".PadRight(40, '-') + '>');
			Trace(sqlBinderQuery.OutputSql);
			Trace();
		}

		public void Dispose() => _connection?.Dispose();
	}
}
