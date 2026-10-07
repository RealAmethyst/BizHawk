// SPDX-License-Identifier: MIT
// Integrate GLib's prepare/query/check/dispatch cycle with the WinForms UI thread.
// Only the blocking file-descriptor wait runs in the background. Providers and
// ATK objects are consequently never read or mutated from competing threads.
using System;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;

namespace UiaAtkBridge
{
	internal sealed class WinFormsMainLoop : IDisposable, IMessageFilter
	{
		[DllImport("libbridge-glue.so")] private static extern IntPtr bridge_loop_create();
		[DllImport("libbridge-glue.so")] private static extern void bridge_loop_prepare(IntPtr state);
		[DllImport("libbridge-glue.so")] private static extern void bridge_loop_poll(IntPtr state);
		[DllImport("libbridge-glue.so")] private static extern void bridge_loop_dispatch(IntPtr state);
		[DllImport("libbridge-glue.so")] private static extern void bridge_loop_wakeup(IntPtr state);
		[DllImport("libbridge-glue.so")] private static extern void bridge_loop_free(IntPtr state);

		private readonly IntPtr state;
		private readonly SynchronizationContext context;
		private readonly AutoResetEvent nextPoll = new AutoResetEvent(false);
		private readonly ManualResetEvent ready = new ManualResetEvent(false);
		private readonly Thread pollThread;
		private volatile bool stopped;
		private volatile bool responsiveWaiting;
		private bool dispatching;
		private static WinFormsMainLoop current;
		private Form altForm;
		private static readonly MethodInfo ProcessMenuKey = typeof(ToolStripManager).GetMethod(
			"ProcessMenuKey", BindingFlags.Static | BindingFlags.NonPublic, null,
			new[] { typeof(Message).MakeByRefType() }, null);
		private static readonly PropertyInfo KeyboardCapture = typeof(Application).GetProperty(
			"KeyboardCapture", BindingFlags.Static | BindingFlags.NonPublic);
		internal static int UiThreadId { get; private set; }

		public WinFormsMainLoop()
		{
			UiThreadId = Thread.CurrentThread.ManagedThreadId;
			if (ProcessMenuKey == null || KeyboardCapture == null)
				throw new NotSupportedException("This Mono WinForms version lacks the menu keyboard interfaces required by the accessibility bridge.");
			context = SynchronizationContext.Current ?? new WindowsFormsSynchronizationContext();
			state = bridge_loop_create();
			if (state == IntPtr.Zero) throw new InvalidOperationException("The UI thread could not acquire GLib's main context.");
			Application.AddMessageFilter(this);
			current = this;
			bridge_loop_prepare(state);
			pollThread = new Thread(WaitForEvents) { IsBackground = true, Name = "AT-SPI event wait" };
			pollThread.Start();
		}

		// Called by the emulator's throttle. Service only accessibility work, never
		// WinForms input, menu invocations, or another emulator frame.
		public static void ResponsiveWait(int milliseconds)
		{
			var loop = current;
			if (loop == null || loop.stopped || Thread.CurrentThread.ManagedThreadId != UiThreadId || loop.dispatching)
			{
				Thread.Sleep(milliseconds);
				return;
			}
			long deadline = Stopwatch.GetTimestamp() + (long)milliseconds * Stopwatch.Frequency / 1000;
			loop.responsiveWaiting = true;
			try
			{
				while (!loop.stopped)
				{
					long remaining = deadline - Stopwatch.GetTimestamp();
					if (remaining <= 0) break;
					int timeout = (int)Math.Ceiling(remaining * 1000.0 / Stopwatch.Frequency);
					if (!loop.ready.WaitOne(timeout)) break;
					loop.Dispatch(null);
				}
			}
			finally
			{
				loop.responsiveWaiting = false;
				if (!loop.stopped && loop.ready.WaitOne(0)) loop.context.Post(loop.Dispatch, null);
			}
		}

		public bool PreFilterMessage(ref Message message)
		{
			const int KeyDown = 0x100, KeyUp = 0x101, SysKeyDown = 0x104, SysKeyUp = 0x105;
			var key = (Keys)message.WParam.ToInt32() & Keys.KeyCode;
			if (message.Msg == KeyDown || message.Msg == SysKeyDown)
				altForm = key == Keys.Menu && (Control.ModifierKeys & Keys.Control) == 0
					&& KeyboardCapture.GetValue(null, null) == null ? Form.ActiveForm : null;
			else if ((message.Msg == KeyUp || message.Msg == SysKeyUp) && key == Keys.Menu)
			{
				// Mono X11Keyboard.PreFilter clears Alt before SendKeyboardInput reads it.
				// Restore native ToolStrip activation for a bare Alt tap.
				// Alt shortcuts, AltGr, and dismissing an active menu stay untouched.
				bool activate = altForm != null && altForm == Form.ActiveForm && message.Msg == KeyUp;
				altForm = null;
				// Mono's RunLoop discards changes to the filtered Message on dispatch.
				// Call the same native menu consumer used by Control.WmSysKeyUp.
				if (activate)
					return (bool)ProcessMenuKey.Invoke(null, new object[] { message });
			}
			else if (message.Msg == 0x8) altForm = null; // WM_KILLFOCUS
			return false;
		}

		private void WaitForEvents()
		{
			while (!stopped)
			{
				bridge_loop_poll(state);
				if (stopped) return;
				ready.Set();
				if (!responsiveWaiting) context.Post(Dispatch, null);
				nextPoll.WaitOne();
			}
		}

		private void Dispatch(object unused)
		{
			if (stopped || !ready.WaitOne(0)) return;
			ready.Reset();
			dispatching = true;
			try { bridge_loop_dispatch(state); }
			finally
			{
				dispatching = false;
				if (stopped) Free();
			}
			if (stopped) return;
			bridge_loop_prepare(state);
			nextPoll.Set();
		}

		public void Dispose()
		{
			if (stopped) return;
			if (Thread.CurrentThread.ManagedThreadId != UiThreadId)
				throw new InvalidOperationException("Dispose the AT-SPI loop on the UI thread.");
			stopped = true;
			current = null;
			Application.RemoveMessageFilter(this);
			bridge_loop_wakeup(state);
			nextPoll.Set();
			pollThread.Join();
			if (!dispatching) Free();
		}

		private void Free()
		{
			bridge_loop_free(state);
			nextPoll.Dispose();
			ready.Dispose();
		}
	}
}
