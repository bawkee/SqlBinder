using System;
using System.Windows.Threading;

namespace SqlBinder.DemoApp.GuiExtensions
{
	/// <summary>
	/// Creates a system timer that fires ExecutionDelegate after the set period elapses. Timer
	/// will reset every time Execute method is called.
	/// </summary>
	public class DelayedExecution
	{
		private readonly int _interval;
		private DispatcherTimer _timer;

		public DelayedExecution(int interval)
		{
			_interval = interval;
		}

		public delegate void ExecutionDelegate();

		private ExecutionDelegate _execution;

		/// <summary>
		/// Starts (or restarts) the timer and executes the delegate after the set period has elapsed.
		/// </summary>
		/// <param name="e">Delegate that will be executed once the period elapses.</param>
		public void Execute(ExecutionDelegate e)
		{
			if (_timer != null)
				_timer.Stop();
			else
			{
				_timer = new DispatcherTimer { Interval = new TimeSpan(_interval * TimeSpan.TicksPerMillisecond) };
				_timer.Tick += _timer_Tick;
			}

			_execution = e;
			_timer.Start();
		}

		void _timer_Tick(object sender, EventArgs e)
		{
			_timer.Stop();
			_execution();
		}

		public void Terminate()
		{
			_timer.Stop();
		}
	}
}
