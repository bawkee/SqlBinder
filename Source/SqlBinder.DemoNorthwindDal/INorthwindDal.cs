using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SqlBinder.DemoNorthwindDal.Entities;

namespace SqlBinder.DemoNorthwindDal
{
	public interface INorthwindDal
	{
		IEnumerable<Category> GetCategories();

		IEnumerable<Supplier> GetSuppliers();

		IEnumerable<Customer> GetCustomers();

		IEnumerable<Shipper> GetShippers();

		IEnumerable<Employee> GetEmployees();

		IEnumerable<string> GetShippingCountries();

		IEnumerable<string> GetShippingCities(string shippingCountry = null);

		IEnumerable<CategorySale> GetCategorySales(int[] categoryIds = null, DateTime? fromDate = null, DateTime? toDate = null);

		IEnumerable<Product> GetProducts(
			decimal? productId = null,
			string productName = null, 
			int[] supplierIds = null, 
			int[] categoryIds = null, 
			decimal? unitPriceFrom = null, decimal? unitPriceTo = null, 
			bool? isDiscontinued = null,
			bool priceGreaterThanAvg = false);

		IEnumerable<Order> GetOrders(
			int? orderId = null, 
			int[] productIds = null, 
			string[] customerIds = null, 
			int[] employeeIds = null, 
			int[] shipperIds = null,
			DateTime? orderDateFrom = null, DateTime? orderDateTo = null,
			DateTime? reqDateFrom = null, DateTime? reqDateTo = null,
			DateTime? shipDateFrom = null, DateTime? shipDateTo = null,
			decimal? freightFrom = null, decimal? freightTo = null,
			string shipCity = null,
			string shipCountry = null);

		ObservableCollection<string> TraceLog { get; }
	}
}
