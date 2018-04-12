using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Windows;
using System.Windows.Input;
using SqlBinder.DemoApp.GuiExtensions;
using SqlBinder.DemoNorthwindDal;

namespace SqlBinder.DemoApp.ViewModels
{
	public abstract class ViewModel : IViewModel
	{
		public INorthwindDal Dal => (Application.Current as App)?.NorthwindDal;

		public event PropertyChangedEventHandler PropertyChanged;

		protected SynchronizationContext SyncContext { get; set; }

		protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
			=> PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

		protected ViewModel()
		{
			Values = Hashtable.Synchronized(new Hashtable());
			SyncContext = SynchronizationContext.Current;
		}

		protected virtual bool SetProperty<T>(ref T memberVariable, T value, [CallerMemberName] string propertyName = null)
		{
			if (EqualityComparer<T>.Default.Equals(memberVariable, value))
				return false;
			memberVariable = value;
			OnPropertyChanged(propertyName);
			return true;
		}

		protected bool SetValue(object value, [CallerMemberName] string propertyName = "")
		{
			var changed = !Equals(value, Values[propertyName]);
			Values[propertyName] = value;
			OnSetValue(propertyName, changed);
			if (changed)
				OnPropertyChanged(propertyName);
			return changed;
		}

		protected T GetValue<T>([CallerMemberName] string propertyName = "") => (T)(Values[propertyName] ?? default(T));

		protected virtual void OnSetValue(string propertyName, bool changed) { }

		public void BeginInvoke(Action action)
		{
			if (SyncContext != null)
				SyncContext.Post(o => action(), null);
			else
				action();
		}

		protected Hashtable Values { get; }

		public static ICommand CreateCommand(Action<object> executeDelegate, Predicate<object> canExecutePredicate)
			=> new Command(executeDelegate, canExecutePredicate);

		public static ICommand CreateCommand(Action executeDelegate, Predicate canExecutePredicate)
			=> new Command(executeDelegate, canExecutePredicate);

		public static ICommand CreateCommand(Action<object> executeDelegate)
			=> new Command(executeDelegate);

		public static ICommand CreateCommand(Action executeDelegate)
			=> new Command(executeDelegate);

		// Disposable pattern *can* be used in view models so one should count on it. ---->
		protected virtual void Dispose(bool disposing)
		{
			//
		}

		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}
	}

	public abstract class DemoViewModel : ViewModel
	{
		public abstract string DemoTitle { get; }

		private readonly DelayedExecution _dataLoader = new DelayedExecution(300);

		public void DeferredRefreshData() => _dataLoader.Execute(RefreshData);

		public void RefreshData()
		{
			if (Dal == null)
				return;

			OnRefreshData();
		}

		protected virtual void OnRefreshData()
		{
			//
		}

		// This thing converts and unboxes your object enumerable into an array of specified type (if possible)
		public static T[] ToUnboxedArray<T>(IEnumerable<object> source) => source?.Select(o => Convert.ChangeType(o, typeof(T))).Cast<T>().ToArray();
	}

	public interface IViewModel : INotifyPropertyChanged, IDisposable
	{
		void BeginInvoke(Action action);
	}

	public class Command : ICommand
	{
		private readonly Action<object> _execute;
		private readonly Predicate<object> _canExecute;

		public string CommandName { get; set; }
		public string Description { get; set; }

		public Command(Action<object> executeDelegate, Predicate<object> canExecutePredicate)
		{
			_execute = executeDelegate ?? throw new ArgumentNullException(nameof(executeDelegate));
			_canExecute = canExecutePredicate;
		}

		public Command(Action executeDelegate, Predicate canExecutePredicate)
			: this(p => executeDelegate(), p => canExecutePredicate()) { }

		public Command(Action<object> executeDelegate)
			: this(executeDelegate, null) { }

		public Command(Action executeDelegate)
			: this(p => executeDelegate(), null) { }

		public bool CanExecute(object parameter) => _canExecute == null || _canExecute(parameter);

		public event EventHandler CanExecuteChanged
		{
			add => CommandManager.RequerySuggested += value;
			remove => CommandManager.RequerySuggested -= value;
		}

		public void Execute(object parameter) => _execute(parameter);
	}

	/// <summary>
	/// Parameterless predicate ¯\_(ツ)_/¯
	/// </summary>
	public delegate bool Predicate();
}
