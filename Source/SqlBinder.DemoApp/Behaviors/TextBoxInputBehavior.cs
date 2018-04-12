using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interactivity;

namespace SqlBinder.DemoApp.Behaviors
{
	public class TextBoxInputBehavior : Behavior<TextBox>
	{
		// This class was made as a result of reading through many answers in this thread, with my own twist:
		// https://stackoverflow.com/q/1268552/346577

		#region DependencyProperties

		public static readonly DependencyProperty RegularExpressionProperty = DependencyProperty.Register(
			"RegularExpression",
			typeof(string),
			typeof(TextBoxInputBehavior),
			new FrameworkPropertyMetadata(".*"));

		public string RegularExpression
		{
			get
			{
				if (IsInteger)
					return @"^[0-9\-]+$";
				if (IsNumeric)
					return @"^[0-9.\-]+$";
				return (string)GetValue(RegularExpressionProperty);
			}
			set { SetValue(RegularExpressionProperty, value); }
		}

		public static readonly DependencyProperty MaxLengthProperty = DependencyProperty.Register(
			"MaxLength",
			typeof(int),
			typeof(TextBoxInputBehavior),
			new FrameworkPropertyMetadata(int.MinValue));

		public int MaxLength
		{
			get { return (int)GetValue(MaxLengthProperty); }
			set { SetValue(MaxLengthProperty, value); }
		}

		public static readonly DependencyProperty EmptyValueProperty = DependencyProperty.Register(
			"EmptyValue",
			typeof(string),
			typeof(TextBoxInputBehavior));

		public string EmptyValue
		{
			get { return (string)GetValue(EmptyValueProperty); }
			set { SetValue(EmptyValueProperty, value); }
		}

		public static readonly DependencyProperty IsNumericProperty = DependencyProperty.Register(
			"IsNumeric",
			typeof(bool),
			typeof(TextBoxInputBehavior));

		public bool IsNumeric
		{
			get { return (bool)GetValue(IsNumericProperty); }
			set { SetValue(IsNumericProperty, value); }
		}

		public static readonly DependencyProperty IsIntegerProperty = DependencyProperty.Register(
			"IsInteger",
			typeof(bool),
			typeof(TextBoxInputBehavior));

		public bool IsInteger
		{
			get { return (bool)GetValue(IsIntegerProperty); }
			set
			{
				if (value)
					SetValue(IsNumericProperty, true);
				SetValue(IsIntegerProperty, value);
			}
		}

		public static readonly DependencyProperty AllowSpaceProperty = DependencyProperty.Register(
			"AllowSpace",
			typeof(bool),
			typeof(TextBoxInputBehavior));

		public bool AllowSpace
		{
			get { return (bool)GetValue(AllowSpaceProperty); }
			set { SetValue(AllowSpaceProperty, value); }
		}

		#endregion

		/// <summary>
		/// Attach our behaviour. Add event handlers
		/// </summary>
		protected override void OnAttached()
		{
			base.OnAttached();

			AssociatedObject.PreviewTextInput += PreviewTextInputHandler;
			AssociatedObject.PreviewKeyDown += PreviewKeyDownHandler;
			DataObject.AddPastingHandler(AssociatedObject, PastingHandler);
		}

		/// <summary>
		/// Deattach our behaviour. remove event handlers
		/// </summary>
		protected override void OnDetaching()
		{
			base.OnDetaching();

			AssociatedObject.PreviewTextInput -= PreviewTextInputHandler;
			AssociatedObject.PreviewKeyDown -= PreviewKeyDownHandler;
			DataObject.RemovePastingHandler(AssociatedObject, PastingHandler);
		}

		void PreviewTextInputHandler(object sender, TextCompositionEventArgs e)
		{
			string text;
			if (AssociatedObject.Text.Length < AssociatedObject.CaretIndex)
				text = AssociatedObject.Text;
			else
			{
				//  Remaining text after removing selected text.

				text = TreatSelectedText(out var remainingTextAfterRemoveSelection)
					? remainingTextAfterRemoveSelection.Insert(AssociatedObject.SelectionStart, e.Text)
					: AssociatedObject.Text.Insert(AssociatedObject.CaretIndex, e.Text);
			}

			e.Handled = !ValidateText(text);
		}

		/// <summary>
		/// PreviewKeyDown event handler
		/// </summary>
		void PreviewKeyDownHandler(object sender, KeyEventArgs e)
		{
			if (e.Key == Key.Space)
				e.Handled = !AllowSpace;

			if (string.IsNullOrEmpty(EmptyValue))
				return;

			string text = null;

			// Handle the Backspace key
			if (e.Key == Key.Back)
			{
				if (!TreatSelectedText(out text))
				{
					if (AssociatedObject.SelectionStart > 0)
						text = AssociatedObject.Text.Remove(AssociatedObject.SelectionStart - 1, 1);
				}
			}
			// Handle the Delete key
			else if (e.Key == Key.Delete)
			{
				// If text was selected, delete it
				if (!TreatSelectedText(out text) && AssociatedObject.Text.Length > AssociatedObject.SelectionStart)
				{
					// Otherwise delete next symbol
					text = AssociatedObject.Text.Remove(AssociatedObject.SelectionStart, 1);
				}
			}

			if (text == string.Empty)
			{
				AssociatedObject.Text = EmptyValue;
				if (e.Key == Key.Back)
					AssociatedObject.SelectionStart++;
				e.Handled = true;
			}
		}

		private void PastingHandler(object sender, DataObjectPastingEventArgs e)
		{
			if (e.DataObject.GetDataPresent(DataFormats.Text))
			{
				string text = Convert.ToString(e.DataObject.GetData(DataFormats.Text));

				if (!ValidateText(text))
					e.CancelCommand();
			}
			else
				e.CancelCommand();
		}

		/// <summary>
		/// Validate certain text by our regular expression and text length conditions
		/// </summary>
		/// <param name="text">Text for validation</param>
		/// <returns>True - valid, False - invalid</returns>
		private bool ValidateText(string text)
		{
			return (new Regex(RegularExpression, RegexOptions.IgnoreCase)).IsMatch(text) && (MaxLength == int.MinValue || text.Length <= MaxLength);
		}

		/// <summary>
		/// Handle text selection
		/// </summary>
		/// <returns>true if the character was successfully removed; otherwise, false.</returns>
		private bool TreatSelectedText(out string text)
		{
			text = null;
			if (AssociatedObject.SelectionLength <= 0)
				return false;

			var length = AssociatedObject.Text.Length;
			if (AssociatedObject.SelectionStart >= length)
				return true;

			if (AssociatedObject.SelectionStart + AssociatedObject.SelectionLength >= length)
				AssociatedObject.SelectionLength = length - AssociatedObject.SelectionStart;

			text = AssociatedObject.Text.Remove(AssociatedObject.SelectionStart, AssociatedObject.SelectionLength);
			return true;
		}
	}
}
