using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using SqlBinder.DemoApp.ViewModels;

namespace SqlBinder.DemoApp.Views
{
	/// <summary>
	/// Interaction logic for CategorySalesView.xaml
	/// </summary>
	public partial class CategorySalesView : UserControl
	{
		public CategorySalesView()
		{
			InitializeComponent();
		}

		private void CategorySalesView_OnLoaded(object sender, RoutedEventArgs e)
		{
			//_fromDate.DisplayDate = DateTime.ParseExact("01/01/94", "MM/dd/yy", CultureInfo.InvariantCulture);


			((DemoViewModel) DataContext).RefreshData();
		}
	}
}
