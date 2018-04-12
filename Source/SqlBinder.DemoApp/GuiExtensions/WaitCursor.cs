using System;
using System.Windows.Input;
using System.Windows.Threading;

namespace SqlBinder.DemoApp.GuiExtensions
{
	/// <summary>
	/// Makes sure cursor is always restored. Usage:
	/// <c>
	///	using(new WaitCursor())
	///	{
	///		// very long task
	///     ...
	///     AnotherMethod(); // No problem if this method uses this class as well
	///	}
	/// </c>
	/// </summary>
	public class WaitCursor : IDisposable
	{
		private static int _stack;

		public WaitCursor()
		{
			Dispatcher.CurrentDispatcher.Invoke(() =>
			{
				_stack++;
				if (Mouse.OverrideCursor != Cursors.Wait)
					Mouse.OverrideCursor = Cursors.Wait;
			}, DispatcherPriority.Background);
		}

		public void Dispose()
		{
			Dispatcher.CurrentDispatcher.Invoke(() =>
			{
				_stack--;
				if (_stack == 0)
					Mouse.OverrideCursor = null;
			}, DispatcherPriority.Background);
		}
	}
}
