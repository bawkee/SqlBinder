using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using SqlBinder.DemoNorthwindDal;
using SqlBinder.DemoNorthwindDal.OleDb;

namespace SqlBinder.DemoApp
{
	/// <summary>
	/// Interaction logic for App.xaml
	/// </summary>
	public partial class App : Application
	{
		public INorthwindDal NorthwindDal { get; } = new OleDbNorthwindDal("Northwind Traders.mdb");

		private void App_OnExit(object sender, ExitEventArgs e)
		{
			(NorthwindDal as IDisposable)?.Dispose();
		}

		private void App_OnStartup(object sender, StartupEventArgs e)
		{
			System.Diagnostics.PresentationTraceSources.DataBindingSource.Switch.Level = System.Diagnostics.SourceLevels.Error;
		}
	}
}
