using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SqlBinder.DemoApp.ViewModels
{
	public class MainViewModel : ViewModel
	{
		public MainViewModel()
		{
			if (Dal == null)
				return;

			CollectionChangedEventManager.AddHandler(Dal.TraceLog, TraceLog_CollectionChanged);
			TraceLogString = new StringBuilder();
		}

		private void TraceLog_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
		{
			if (e.Action == NotifyCollectionChangedAction.Add)
			{
				TraceLogString.Append(Dal.TraceLog[e.NewStartingIndex]);
				TraceLogString.Append("\n");
				OnPropertyChanged(nameof(TraceLogString));
			}
		}

		public ObservableCollection<DemoViewModel> DemoItems => new ObservableCollection<DemoViewModel>
		{
			new InfoViewModel(),
			new ProductsViewModel(),
			new OrdersViewModel(),
			new CategorySalesViewModel()
		};

		public StringBuilder TraceLogString
		{
			get => GetValue<StringBuilder>();
			set => SetValue(value);
		}

		public ToggleVisibilityViewModel TraceLogToggle { get; } = new ToggleVisibilityViewModel();
	}
}
