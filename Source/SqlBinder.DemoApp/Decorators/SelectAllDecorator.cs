using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace SqlBinder.DemoApp.Decorators
{
	/// <summary>
	/// Intuitive auto-select-all decorator for TextBoxes.
	/// Credit goes to: https://stackoverflow.com/a/2674291/346577 (slightly modified)
	/// </summary>
	public static class SelectAllDecorator
	{
		public static readonly DependencyProperty AutoSelectAllProperty = DependencyProperty.RegisterAttached(
			"AutoSelectAll",
			typeof(bool),
			typeof(SelectAllDecorator),
			new PropertyMetadata(false, AutoSelectAllPropertyChanged));

		private static void AutoSelectAllPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
		{
			if (!(d is TextBox textBox))
				return;

			if ((e.NewValue as bool?).GetValueOrDefault(false))
			{
				textBox.GotKeyboardFocus += OnKeyboardFocusSelectText;
				textBox.PreviewMouseLeftButtonDown += OnMouseLeftButtonDown;
			}
			else
			{
				textBox.GotKeyboardFocus -= OnKeyboardFocusSelectText;
				textBox.PreviewMouseLeftButtonDown -= OnMouseLeftButtonDown;
			}
		}

		private static void OnMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
		{
			var dependencyObject = GetParentFromVisualTree(e.OriginalSource);

			if (dependencyObject == null)
				return;

			var textBox = (TextBox)dependencyObject;

			if (!textBox.IsKeyboardFocusWithin)
			{
				textBox.Focus();
				e.Handled = true;
			}
		}

		private static DependencyObject GetParentFromVisualTree(object source)
		{
			DependencyObject parent = source as UIElement;

			while (parent != null && !(parent is TextBox))
				parent = VisualTreeHelper.GetParent(parent);

			return parent;
		}

		private static void OnKeyboardFocusSelectText(object sender, KeyboardFocusChangedEventArgs e)
		{
			if (e.OriginalSource is TextBox textBox)
				textBox.SelectAll();
		}

		[AttachedPropertyBrowsableForChildren(IncludeDescendants = false)]
		[AttachedPropertyBrowsableForType(typeof(TextBox))]
		public static bool GetAutoSelectAll(DependencyObject @object) => (bool)@object.GetValue(AutoSelectAllProperty);

		public static void SetAutoSelectAll(DependencyObject @object, bool value) => @object.SetValue(AutoSelectAllProperty, value);
	}
}
