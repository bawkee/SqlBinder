using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace SqlBinder.DemoApp.ViewModels
{
	public class ToggleVisibilityViewModel : ViewModel
	{
		public ToggleVisibilityViewModel(bool isVisible = false)
		{
			IsVisible = IsVisible;
			ToggleCommand = CreateCommand(OnToggle);
		}

		public ICommand ToggleCommand { get; }

		protected virtual void OnToggle() => IsVisible = !IsVisible;

		public Visibility Visibility
		{
			get => GetValue<Visibility>();
			set
			{
				SetValue(value);
				if (Visibility == Visibility.Visible && !IsVisible)
					IsVisible = true;
				if (Visibility != Visibility.Visible && IsVisible)
					IsVisible = false;
			}
		}

		public bool IsVisible
		{
			get => GetValue<bool>();
			set
			{
				SetValue(value);
				if (IsVisible && Visibility != Visibility.Visible)
					Visibility = Visibility.Visible;
				if (!IsVisible && Visibility == Visibility.Visible)
					Visibility = Visibility.Collapsed;
			}
		}
	}
}
