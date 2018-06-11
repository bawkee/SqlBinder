using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SqlBinder.DemoNorthwindDal.Entities;

namespace SqlBinder.DemoNorthwindDal.OleDb
{
	/// <summary>
	/// A very primitive, miniature ORM, separated purely for readability. One could easily just plug in any other micro ORM such as
	/// Dapper or PetaPoco and remove this. I just didn't want any extra dependencies.
	/// </summary>
	public class OledbOrm
	{
		public static Category CreateCategory(IDataReader r) => new Category
		{
			CategoryId = (int) r["CategoryID"],
			Name = (string) r["CategoryName"],
			Description = (string) r["Description"]
		};

		public static Supplier CreateSupplier(IDataReader r) => new Supplier
		{
			SupplierId = (int) r["SupplierID"],
			CompanyName = (string) r["CompanyName"],
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

		public static Customer CreateCustomer(IDataReader r) => new Customer
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

		public static CategorySale CreateCategorySale(IDataReader r) => new CategorySale
		{
			CategoryId = (int) r["CategoryID"],
			CategoryName = (string) r["CategoryName"],
			TotalSales = r["TotalSales"] as decimal? ?? 0
		};

		public static Product CreateProduct(IDataReader r) => new Product
		{
			ProductId = (int) r["ProductID"],
			ProductName = (string) r["ProductName"],
			CategoryName = (string) r["CategoryName"],
			SupplierCompany = (string) r["SupplierCompany"],
			SupplierId = (int) r["SupplierID"],
			CategoryId = (int) r["CategoryID"],
			QuantityPerUnit = (string) r["QuantityPerUnit"],
			UnitPrice = (decimal) r["UnitPrice"],
			UnitsInStock = Convert.ToInt32(r["UnitsInStock"] as Int16?),
			UnitsOnOrder = Convert.ToInt32(r["UnitsOnOrder"] as Int16?),
			Discontinued = (bool) r["Discontinued"],
		};

		public static Employee CreateEmployee(IDataReader r) => new Employee
		{
			EmployeeId = (int) r["EmployeeId"],
			FirstName = r["FirstName"] as string,
			LastName = r["LastName"] as string,
			Title = r["Title"] as string,
			HireDate = r["HireDate"] as DateTime?,
		};

		public static Order CreateOrder(IDataReader r) => new Order
		{
			OrderId = (int) r["OrderId"],
			CustomerId = (string) r["CustomerId"],
			EmployeeId = (int) r["EmployeeId"],
			OrderDate = r["OrderDate"] as DateTime?,
			RequiredDate = r["RequiredDate"] as DateTime?,
			ShippedDate = r["ShippedDate"] as DateTime?,
			ShipperId = (int) r["ShipVia"],
			Freight = (decimal) r["Freight"],
			CustomerName = (string) r["CustomerName"],
			EmployeeName = (string) r["EmployeeName"],
			ShippedVia = r["ShippedVia"] as string,
			ShipName = r["ShipName"] as string,
			ShipAddress = r["ShipAddress"] as string,
			ShipCity = r["ShipCity"] as string,
			ShipRegion = r["ShipRegion"] as string,
			ShipCountry = r["ShipCountry"] as string,
			ShipPostalCode = r["ShipPostalCode"] as string,
		};

		public static Shipper CreateShipper(IDataReader r) => new Shipper
		{
			ShipperId = (int) r["ShipperId"],
			CompanyName = r["CompanyName"] as string,
			Phone = r["Phone"] as string,
		};
	}
}
