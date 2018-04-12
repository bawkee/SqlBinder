using System;
using System.Collections.Generic;
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
	/// Interaction logic for ProductsView.xaml
	/// </summary>
	public partial class ProductsView : UserControl
	{
		public ProductsView()
		{
			InitializeComponent();
		}

		private void ProductsView_OnLoaded(object sender, RoutedEventArgs e)
		{
			((DemoViewModel) DataContext).RefreshData();
		}
	}
}
