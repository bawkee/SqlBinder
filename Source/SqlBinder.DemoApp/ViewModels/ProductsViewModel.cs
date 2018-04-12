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
	public class ProductsViewModel : DemoViewModel
	{
		public override string DemoTitle => "Products";

		private bool _initialized;

		public ObservableCollection<Product> Products
		{
			get => GetValue<ObservableCollection<Product>>();
			set => SetValue(value);
		}

		public string ProductName
		{
			get => GetValue<string>();
			set
			{
				if (SetValue(value))
					DeferredRefreshData();
			}
		}

		public decimal? FromUnitPrice
		{
			get => GetValue<decimal?>();
			set
			{
				if (SetValue(value))
					DeferredRefreshData();
			}
		}

		public decimal? ToUnitPrice
		{
			get => GetValue<decimal?>();
			set
			{
				if (SetValue(value))
					DeferredRefreshData();
			}
		}

		public bool? IsDiscontinued
		{
			get => GetValue<bool?>();
			set
			{
				if (SetValue(value))
					DeferredRefreshData();
			}
		}

		public bool PriceGreaterThanAvg
		{
			get => GetValue<bool>();
			set
			{
				if (SetValue(value))
					DeferredRefreshData();
			}
		}

		#region Category Selector

		private void LoadCategories()
		{
			Categories = new ObservableCollection<Category>(Dal.GetCategories());
			SelectedCategoryIds = new ObservableCollection<object>();
		}

		public ObservableCollection<Category> Categories
		{
			get => GetValue<ObservableCollection<Category>>();
			set => SetValue(value);
		}

		public ObservableCollection<object> SelectedCategoryIds
		{
			get => GetValue<ObservableCollection<object>>();
			set
			{
				SetValue(value);
				if (SelectedCategoryIds != null)
					SelectedCategoryIds.CollectionChanged += (s, e) => { DeferredRefreshData(); };
			}
		}

		#endregion

		#region Suppliers Selector

		private void LoadSuppliers()
		{
			Suppliers = new ObservableCollection<Supplier>(Dal.GetSuppliers());
			SelectedSupplierIds = new ObservableCollection<object>();
		}

		public ObservableCollection<Supplier> Suppliers
		{
			get => GetValue<ObservableCollection<Supplier>>();
			set => SetValue(value);
		}

		public ObservableCollection<object> SelectedSupplierIds
		{
			get => GetValue<ObservableCollection<object>>();
			set
			{
				SetValue(value);
				if (SelectedSupplierIds != null)
					SelectedSupplierIds.CollectionChanged += (s, e) => { DeferredRefreshData(); };
			}
		}

		#endregion

		protected override void OnRefreshData()
		{
			if (!_initialized)
			{
				LoadCategories();
				LoadSuppliers();
				_initialized = true;
			}

			using (new WaitCursor())
			{
				Products = new ObservableCollection<Product>(Dal.GetProducts(
					productId: null,
					productName: ProductName,
					supplierIds: ToUnboxedArray<int>(SelectedSupplierIds),
					categoryIds: ToUnboxedArray<int>(SelectedCategoryIds),
					unitPriceFrom: FromUnitPrice,
					unitPriceTo: ToUnitPrice,
					isDiscontinued: IsDiscontinued,
					priceGreaterThanAvg: PriceGreaterThanAvg));
			}
		}
	}
}
