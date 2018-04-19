using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using SqlBinder.DemoApp.GuiExtensions;
using SqlBinder.DemoNorthwindDal;
using SqlBinder.DemoNorthwindDal.Entities;

namespace SqlBinder.DemoApp.ViewModels
{
	public class CategorySalesViewModel : DemoViewModel
	{
		public override string DemoTitle => "Category Sales";

		private bool _initialized;

		public ObservableCollection<CategorySale> CategorySales
		{
			get => GetValue<ObservableCollection<CategorySale>>();
			set => SetValue(value);
		}

		public DateTime DefaultFromDate { get; } = DateTime.ParseExact("01/01/94", "MM/dd/yy", CultureInfo.InvariantCulture);
		public DateTime DefaultToDate { get; } = DateTime.ParseExact("01/01/96", "MM/dd/yy", CultureInfo.InvariantCulture);

		public DateTime? FromDate
		{
			get => GetValue<DateTime?>();
			set
			{
				if (SetValue(value))
					DeferredRefreshData();
			}
		}

		public DateTime? ToDate
		{
			get => GetValue<DateTime?>();
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

		protected override void OnRefreshData()
		{
			if (!_initialized)
			{
				LoadCategories();
				_initialized = true;
			}

			CategorySales = new ObservableCollection<CategorySale>(Dal.GetCategorySales(
				categoryIds: ToUnboxedArray<int>(SelectedCategoryIds),
				fromDate: FromDate,
				toDate: ToDate
				));
		}
	}
}
