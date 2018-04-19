using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SqlBinder.DemoApp.GuiExtensions;
using SqlBinder.DemoNorthwindDal.Entities;

namespace SqlBinder.DemoApp.ViewModels
{
	public class OrdersViewModel : DemoViewModel
	{
		public override string DemoTitle => "Orders";

		private bool _initialized;

		public ObservableCollection<Order> Orders
		{
			get => GetValue<ObservableCollection<Order>>();
			set => SetValue(value);
		}

		#region Customer Selector

		private void LoadCustomers()
		{
			Customers = new ObservableCollection<Customer>(Dal.GetCustomers());
			SelectedCustomerIds = new ObservableCollection<object>();
		}

		public ObservableCollection<Customer> Customers
		{
			get => GetValue<ObservableCollection<Customer>>();
			set => SetValue(value);
		}

		public ObservableCollection<object> SelectedCustomerIds
		{
			get => GetValue<ObservableCollection<object>>();
			set
			{
				SetValue(value);
				if (SelectedCustomerIds != null)
					SelectedCustomerIds.CollectionChanged += (s, e) => { DeferredRefreshData(); };
			}
		}

		#endregion

		#region Employee Selector

		private void LoadEmployees()
		{
			Employees = new ObservableCollection<Employee>(Dal.GetEmployees());
			SelectedEmployeeIds = new ObservableCollection<object>();
		}

		public ObservableCollection<Employee> Employees
		{
			get => GetValue<ObservableCollection<Employee>>();
			set => SetValue(value);
		}

		public ObservableCollection<object> SelectedEmployeeIds
		{
			get => GetValue<ObservableCollection<object>>();
			set
			{
				SetValue(value);
				if (SelectedEmployeeIds != null)
					SelectedEmployeeIds.CollectionChanged += (s, e) => { DeferredRefreshData(); };
			}
		}

		#endregion

		#region Product Selector

		private void LoadProducts()
		{
			Products = new ObservableCollection<Product>(Dal.GetProducts());
			SelectedProductIds = new ObservableCollection<object>();
		}

		public ObservableCollection<Product> Products
		{
			get => GetValue<ObservableCollection<Product>>();
			set => SetValue(value);
		}

		public ObservableCollection<object> SelectedProductIds
		{
			get => GetValue<ObservableCollection<object>>();
			set
			{
				SetValue(value);
				if (SelectedProductIds != null)
					SelectedProductIds.CollectionChanged += (s, e) => { DeferredRefreshData(); };
			}
		}

		#endregion

		#region Shipper Selector

		private void LoadShippers()
		{
			Shippers = new ObservableCollection<Shipper>(Dal.GetShippers());
			SelectedShipperIds = new ObservableCollection<object>();
		}

		public ObservableCollection<Shipper> Shippers
		{
			get => GetValue<ObservableCollection<Shipper>>();
			set => SetValue(value);
		}

		public ObservableCollection<object> SelectedShipperIds
		{
			get => GetValue<ObservableCollection<object>>();
			set
			{
				SetValue(value);
				if (SelectedShipperIds != null)
					SelectedShipperIds.CollectionChanged += (s, e) => { DeferredRefreshData(); };
			}
		}

		#endregion

		public string[] ShippingCountries
		{
			get => GetValue<string[]>();
			set => SetValue(value);
		}

		public string SelectedShippingCountry
		{
			get => GetValue<string>();
			set
			{
				SetValue(value);
				LoadShippingCities();
				DeferredRefreshData();
			}
		}

		private void LoadShippingCountries()
		{
			ShippingCountries = Dal.GetShippingCountries().ToArray();
		}

		public string[] ShippingCities
		{
			get => GetValue<string[]>();
			set => SetValue(value);
		}

		public string SelectedShippingCity
		{
			get => GetValue<string>();
			set
			{
				SetValue(value);
				DeferredRefreshData();
			}
		}

		private void LoadShippingCities()
		{
			ShippingCities = Dal.GetShippingCities(SelectedShippingCountry).ToArray();
		}

		public DateTime? FromOrderDate
		{
			get => GetValue<DateTime?>();
			set
			{
				if (SetValue(value))
					DeferredRefreshData();
			}
		}

		public DateTime DefaultFromDate { get; } = DateTime.ParseExact("01/01/94", "MM/dd/yy", CultureInfo.InvariantCulture);
		public DateTime DefaultToDate { get; } = DateTime.ParseExact("01/01/96", "MM/dd/yy", CultureInfo.InvariantCulture);

		public DateTime? ToOrderDate
		{
			get => GetValue<DateTime?>();
			set
			{
				if (SetValue(value))
					DeferredRefreshData();
			}
		}

		public DateTime? FromShippedDate
		{
			get => GetValue<DateTime?>();
			set
			{
				if (SetValue(value))
					DeferredRefreshData();
			}
		}

		public DateTime? ToShippedDate
		{
			get => GetValue<DateTime?>();
			set
			{
				if (SetValue(value))
					DeferredRefreshData();
			}
		}

		public DateTime? FromRequiredDate
		{
			get => GetValue<DateTime?>();
			set
			{
				if (SetValue(value))
					DeferredRefreshData();
			}
		}

		public DateTime? ToRequiredDate
		{
			get => GetValue<DateTime?>();
			set
			{
				if (SetValue(value))
					DeferredRefreshData();
			}
		}

		public decimal? FromFreight
		{
			get => GetValue<decimal?>();
			set
			{
				if (SetValue(value))
					DeferredRefreshData();
			}
		}

		public decimal? ToFreight
		{
			get => GetValue<decimal?>();
			set
			{
				if (SetValue(value))
					DeferredRefreshData();
			}
		}

		protected override void OnRefreshData()
		{
			if (!_initialized)
			{
				LoadCustomers();
				LoadEmployees();
				LoadProducts();
				LoadShippers();
				LoadShippingCountries();
				LoadShippingCities();
				_initialized = true;
			}

			using (new WaitCursor())
			{
				Orders = new ObservableCollection<Order>(Dal.GetOrders(orderId: null,
					productIds: ToUnboxedArray<int>(SelectedProductIds),
					customerIds: ToUnboxedArray<string>(SelectedCustomerIds),
					employeeIds: ToUnboxedArray<int>(SelectedEmployeeIds),
					shipperIds: ToUnboxedArray<int>(SelectedShipperIds),
					orderDateFrom: FromOrderDate,
					orderDateTo: ToOrderDate,
					reqDateFrom: FromRequiredDate,
					reqDateTo: ToRequiredDate,
					shipDateFrom: FromShippedDate,
					shipDateTo: ToShippedDate,
					freightFrom: FromFreight,
					freightTo: ToFreight,
					shipCity: SelectedShippingCity,
					shipCountry: SelectedShippingCountry));
			}
		}
	}
}
