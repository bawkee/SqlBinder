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
using SqlBinder.DemoApp.GuiExtensions;

namespace SqlBinder.DemoApp
{
	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class MainWindow : Window
	{
		public MainWindow()
		{
			InitializeComponent();
		}

		private void txtLog_OnTextChanged(object sender, TextChangedEventArgs e)
		{
			((TextBox) sender).ScrollToEnd();
		}

		private void txtLog_OnIsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
		{
			var txtLog = sender as TextBox;

			if (!txtLog?.IsVisible ?? true)
				return;
		
			new DelayedExecution(50).Execute(() => 
				txtLog.ScrollToEnd());
		}
	}
}
