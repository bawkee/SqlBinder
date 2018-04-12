using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;

namespace SqlBinder.DemoApp.Controls
{
	/// <summary>
	/// This class is not a custom control, it doesn't derive from ItemsControl, MultiSelector, ListBox or anything. It's just a
	/// user control with a DataGrid and a TextBox slapped on it. It doesn't support templates, item templates or any template selectors. For a full scale control
	/// which offers all the flexibility of WPF you'd need a commercial grade stuff - this control is not it, rather it's a helper control for internal purposes.
	/// The ItemsSource property is bindable, but, the control will simply copy source items to its internal collection and bind to that instead. If you don't specify
	/// <see cref="IdMemberPath"/>, the item itself will be used as ID and thus provided in the <see cref="SelectedIds"/> property. If you don't specify the
	/// <see cref="DisplayMemberPath"/> property, items texts will be the <c>item.ToString()</c>. You can add/remove objects from the <see cref="SelectedIds"/>
	/// property, the grid will reflect changes instantly, just make sure you do this after specifying the <see cref="ItemsSource"/> property and not before.
	/// </summary>
	public partial class CheckList
	{
		public class CheckListItem : INotifyPropertyChanged
		{
			private bool _checked;

			public event PropertyChangedEventHandler PropertyChanged;

			public bool Checked
			{
				get => _checked;
				set
				{
					if (_checked == value)
						return;
					_checked = value;
					PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Checked"));
				}
			}

			public object Id { get; set; }
			public string Name { get; set; }
			public string Description { get; set; }
			public object ItemData { get; set; }
		}

		public CheckList()
		{
			InitializeComponent();
			WatermarkText = "Search...";
			IsEditable = true;
			HideSelection = true;
			AllowSelectAll = true;
			AllowInvert = true;
			gridRoot.DataContext = this;
		}

		public string WatermarkText
		{
			get => (string)GetValue(WatermarkTextProperty);
			set => SetValue(WatermarkTextProperty, value);
		}

		public static readonly DependencyProperty WatermarkTextProperty = DependencyProperty.Register(
			"WatermarkText",
			typeof(string),
			typeof(CheckList));

		public object SelectedItem
		{
			get => GetValue(SelectedItemProperty);
			set => SetValue(SelectedItemProperty, value);
		}

		public static readonly DependencyProperty SelectedItemProperty = DependencyProperty.Register(
			"SelectedItem",
			typeof(object),
			typeof(CheckList));

		public ObservableCollection<object> SelectedIds
		{
			get { return (ObservableCollection<object>)GetValue(SelectedIdsProperty); }
			set { SetValue(SelectedIdsProperty, value); }
		}

		public static readonly DependencyProperty SelectedIdsProperty = DependencyProperty.Register(
			"SelectedIds",
			typeof(ObservableCollection<object>),
			typeof(CheckList),
			new FrameworkPropertyMetadata(OnSelectedIdsChanged));

		private static void OnSelectedIdsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
		{
			var selectedIds = (ObservableCollection<object>)e.NewValue;

			var filterItems = d.GetValue(CheckListItemsProperty) as ObservableCollection<CheckListItem>;
			if (selectedIds == null)
				return;

			if (filterItems != null)
			{
				foreach (var filterItem in filterItems)
				{
					filterItem.Checked = selectedIds.Contains(filterItem.Id);
				}
			}

			selectedIds.CollectionChanged += (s, cce) =>
			{
				filterItems = d.GetValue(CheckListItemsProperty) as ObservableCollection<CheckListItem>;

				if (filterItems == null)
					return;

				if (cce.Action == NotifyCollectionChangedAction.Add)
				{
					var selectedItem = cce.NewItems[0];
					var item = filterItems.FirstOrDefault(i => Equals(i.Id, selectedItem));
					if (item != null)
						item.Checked = true;
				}
				else if (cce.Action == NotifyCollectionChangedAction.Remove)
				{
					var selectedItem = cce.OldItems[0];
					var item = filterItems.FirstOrDefault(i => Equals(i.Id, selectedItem));
					if (item != null)
						item.Checked = false;
				}
			};
		}

		public static readonly DependencyProperty ItemsSourceProperty = ItemsControl.ItemsSourceProperty.AddOwner(
			typeof(CheckList),
			new FrameworkPropertyMetadata(OnItemsSourceChanged));

		public IEnumerable ItemsSource
		{
			get { return (IEnumerable)GetValue(ItemsSourceProperty); }
			set { SetValue(ItemsSourceProperty, value); }
		}

		private static void OnItemsSourceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
		{
			var sourceItems = (IEnumerable)e.NewValue;
			var dstItems = new ObservableCollection<CheckListItem>();

			PropertyInfo idProp = null;
			PropertyInfo displayProp = null;
			PropertyInfo descProp = null;

			bool hasDescription = false;

			var selectedIds = d.GetValue(SelectedIdsProperty) as ObservableCollection<object> ??
							  new ObservableCollection<object>();

			d.SetCurrentValue(SelectedIdsProperty, selectedIds);

			if (sourceItems != null)
			{
				foreach (var item in sourceItems)
				{
					var idPropName = (string)d.GetValue(IdMemberPathProperty);
					var displayPropName = (string)d.GetValue(DisplayMemberPathProperty);
					var descPropName = (string)d.GetValue(DescriptionMemberPathProperty);

					object idValue;
					object displayValue;
					object descValue = null;

					var propNotFoundStr = "Could not find property {0} on type {1}.";

					if (!string.IsNullOrEmpty(idPropName))
					{
						if (idProp == null)
							idProp = item.GetType().GetProperty(idPropName);

						if (idProp == null)
							throw new ArgumentException(string.Format(propNotFoundStr, idPropName, item.GetType().Name));

						idValue = idProp.GetValue(item);
					}
					else
						idValue = item;

					if (!string.IsNullOrEmpty(displayPropName))
					{
						if (displayProp == null)
							displayProp = item.GetType().GetProperty(displayPropName);

						if (displayProp == null)
							throw new ArgumentException(string.Format(propNotFoundStr, displayPropName, item.GetType().Name));

						displayValue = displayProp.GetValue(item);

						if (!string.IsNullOrEmpty(descPropName))
						{
							if (descProp == null)
								descProp = item.GetType().GetProperty(descPropName);

							if (descProp == null)
								throw new ArgumentException(string.Format(propNotFoundStr, descPropName, item.GetType().Name));

							descValue = descProp.GetValue(item);
						}
					}
					else
						displayValue = item.ToString();

					if (descValue != null)
						hasDescription = true;

					var dstItem = new CheckListItem
					{
						Id = idValue,
						Name = displayValue.ToString(),
						Description = descValue as string,
						Checked = selectedIds.Contains(idValue),
						ItemData = item
					};

					dstItem.PropertyChanged += (s, le) =>
					{
						var changedItem = (CheckListItem)s;

						selectedIds = d.GetValue(SelectedIdsProperty) as ObservableCollection<object>;

						if (selectedIds == null)
							return;

						if (changedItem.Checked && !selectedIds.Contains(changedItem.Id))
							selectedIds.Add(changedItem.Id);
						else if (!changedItem.Checked && selectedIds.Contains(changedItem.Id))
							selectedIds.Remove(changedItem.Id);
					};

					dstItems.Add(dstItem);
				}
			}

			d.SetCurrentValue(HasDescriptionProperty, hasDescription);

			d.SetValue(CheckListItemsProperty, dstItems);
		}

		internal static readonly DependencyProperty CheckListItemsProperty = DependencyProperty.Register(
			"CheckListItems",
			typeof(ObservableCollection<CheckListItem>),
			typeof(CheckList));

		internal ObservableCollection<CheckListItem> CheckListItems
		{
			get { return (ObservableCollection<CheckListItem>)GetValue(CheckListItemsProperty); }
			set { SetValue(CheckListItemsProperty, value); }
		}

		public static readonly DependencyProperty DisplayMemberPathProperty = DependencyProperty.Register(
			"DisplayMemberPath",
			typeof(string),
			typeof(CheckList));

		public string DisplayMemberPath
		{
			get { return (string)GetValue(DisplayMemberPathProperty); }
			set { SetValue(DisplayMemberPathProperty, value); }
		}

		public static readonly DependencyProperty DescriptionMemberPathProperty = DependencyProperty.Register(
			"DescriptionMemberPath",
			typeof(string),
			typeof(CheckList));

		public string DescriptionMemberPath
		{
			get { return (string)GetValue(DescriptionMemberPathProperty); }
			set { SetValue(DescriptionMemberPathProperty, value); }
		}

		private static readonly DependencyProperty HasDescriptionProperty = DependencyProperty.Register(
			"HasDescription",
			typeof(bool),
			typeof(CheckList));

		public bool HasDescription
		{
			get { return (bool)GetValue(HasDescriptionProperty); }
			set { SetValue(HasDescriptionProperty, value); }
		}

		public static readonly DependencyProperty IdMemberPathProperty = DependencyProperty.Register(
			"IdMemberPath",
			typeof(string),
			typeof(CheckList),
			new PropertyMetadata(default(string)));

		public string IdMemberPath
		{
			get { return (string)GetValue(IdMemberPathProperty); }
			set { SetValue(IdMemberPathProperty, value); }
		}

		public static readonly DependencyProperty IsEditableProperty = DependencyProperty.Register(
			"IsEditable",
			typeof(bool),
			typeof(CheckList));

		public bool IsEditable
		{
			get { return (bool)GetValue(IsEditableProperty); }
			set { SetValue(IsEditableProperty, value); }
		}

		public static readonly DependencyProperty HideSelectionProperty = DependencyProperty.Register(
			"HideSelection",
			typeof(bool),
			typeof(CheckList),
			new FrameworkPropertyMetadata(OnHideSelectionChanged));

		public bool HideSelection
		{
			get { return (bool)GetValue(HideSelectionProperty); }
			set { SetValue(HideSelectionProperty, value); }
		}

		private static void OnHideSelectionChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
		{
			if (!(d is FrameworkElement element))
				return;

			if (((bool)e.NewValue) == false)
			{
				element.Resources.Remove(SystemColors.InactiveSelectionHighlightBrushKey);
			}
			else
			{
				element.Resources.Add(SystemColors.InactiveSelectionHighlightBrushKey, Colors.Transparent);
			}
		}

		public static readonly DependencyProperty AllowSelectAllProperty = DependencyProperty.Register(
			"AllowSelectAll",
			typeof(bool),
			typeof(CheckList));

		public bool AllowSelectAll
		{
			get { return (bool)GetValue(AllowSelectAllProperty); }
			set { SetValue(AllowSelectAllProperty, value); }
		}

		public static readonly DependencyProperty AllowInvertProperty = DependencyProperty.Register(
			"AllowInvert",
			typeof(bool),
			typeof(CheckList));

		public bool AllowInvert
		{
			get { return (bool)GetValue(AllowInvertProperty); }
			set { SetValue(AllowInvertProperty, value); }
		}

		private void CollectionViewSource_Filter(object sender, FilterEventArgs e)
		{
			e.Accepted = true;

			if (!string.IsNullOrEmpty(textboxSearch.Text))
			{
				var item = e.Item as CheckListItem;
				var searchText = textboxSearch.Text.ToUpper();

				if (!(item.Name ?? "").ToUpper().Contains(searchText) &&
					!(item.Description ?? "").ToUpper().Contains(searchText))
					e.Accepted = false;
			}
		}

		private void textboxSearch_TextChanged(object sender, TextChangedEventArgs e)
		{
			CollectionViewSource.GetDefaultView(datagridItems.ItemsSource).Refresh();
		}

		private void OnClearFilter(object sender, ExecutedRoutedEventArgs e)
		{
			textboxSearch.Text = "";
			foreach (var item in CheckListItems)
				item.Checked = false;
		}

		private void OnSelectAll(object sender, ExecutedRoutedEventArgs e)
		{
			foreach (var item in CollectionViewSource.GetDefaultView(datagridItems.ItemsSource).Cast<CheckListItem>())
				item.Checked = true;
		}

		private void OnInvertSelection(object sender, ExecutedRoutedEventArgs e)
		{
			foreach (var item in CollectionViewSource.GetDefaultView(datagridItems.ItemsSource).Cast<CheckListItem>())
				item.Checked = !item.Checked;
		}

		private void datagridItems_KeyDown(object sender, KeyEventArgs e)
		{
			var grid = (DataGrid)sender;

			if (!IsEditable)
				return;

			var selection = new List<object>((IEnumerable<object>)grid.SelectedItems);

			if (e.Key == Key.Space && selection.Count > 1)
			{
				foreach (var item in selection.Cast<CheckListItem>())
					item.Checked = !item.Checked;

				e.Handled = true;
			}
		}

		private void datagridItems_OnMouseDoubleClick(object sender, MouseButtonEventArgs e)
		{
			var grid = (DataGrid)sender;
			var item = grid.SelectedItem as CheckListItem;

			if (!IsEditable)
				return;

			if (grid.SelectedItems.Count == 1)
				item.Checked = !item.Checked;
		}

		private void datagridItems_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			var grid = (DataGrid)sender;

			if (!(grid.SelectedItem is CheckListItem item))
				return;

			grid.ScrollIntoView(item);
		}
	}

	public class CheckListItemDataConverter : DependencyObject, IValueConverter
	{
		public static readonly DependencyProperty SourceProperty = DependencyProperty.Register(
			"Source",
			typeof(IEnumerable),
			typeof(CheckListItemDataConverter));

		[Bindable(true)]
		public IEnumerable Source
		{
			get { return (IEnumerable)GetValue(SourceProperty); }
			set { SetValue(SourceProperty, value); }
		}

		public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			if (Source == null)
				return null;
			return Source.Cast<CheckList.CheckListItem>().FirstOrDefault(i => i.ItemData == value);
		}

		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		{
			if (value == null)
				return null;
			return ((CheckList.CheckListItem)value).ItemData;
		}
	}

	public static class CheckListCommands
	{
		public static RoutedCommand ClearSelection = new RoutedCommand("ClearSelection", typeof(CheckList));
		public static RoutedCommand InvertSelection = new RoutedCommand("InvertSelection", typeof(CheckList));
		public static RoutedCommand SelectAll = new RoutedCommand("SelectAll", typeof(CheckList));
		public static RoutedCommand Print = new RoutedCommand("Print", typeof(CheckList));
	}

	/// <summary>
	/// Some non ortodox ways of doing things, saves up some time and effort.... The CheckList is will continuosly have to deal with many different 
	/// implementations of 'available items / chosen items' model so we'll just use this class to help us deal with different scenarios.
	/// </summary>
	public static class CheckListExtensions
	{
		public static T[] ToIdArray<T>(this ObservableCollection<object> source)
		{
			return source?.Select(o => Convert.ChangeType(o, typeof(T))).Cast<T>().ToArray();
		}

		public static ObservableCollection<object> ToIdCollection<T>(this T[] source, Type targetType = null)
		{
			if (targetType == null)
				targetType = typeof(T);

			if (source != null)
				return new ObservableCollection<object>(source.Select(o => Convert.ChangeType(o, targetType)));
			return new ObservableCollection<object>(Array.CreateInstance(targetType, 0).Cast<object>());
		}
	}
}
