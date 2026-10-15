using System;
using System.Reflection;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows.Forms;

class WakePipeProbe
{
    [DllImport("libc", SetLastError=true)] static extern int ioctl(int fd, uint request, out int value);
    [DllImport("libc", SetLastError=true)] static extern int fcntl(int fd, int command);
    [DllImport("libc", SetLastError=true)] static extern int pipe([Out] int[] fds);
    [DllImport("libc", SetLastError=true)] static extern int close(int fd);
    static int readFd;
    static Type driver;
    static int Pending() { int value; if (ioctl(readFd, 0x541b, out value) != 0) throw new Exception("FIONREAD failed"); return value; }
    static void CheckOtherError()
    {
        var wake = driver.GetField("wake", BindingFlags.Static | BindingFlags.NonPublic);
        var original = wake.GetValue(null);
        int[] fds = new int[2];
        if (pipe(fds) != 0) throw new Exception("test pipe creation failed");
        var broken = Activator.CreateInstance(original.GetType(), new object[] { fds[1], false });
        close(fds[1]);
        try
        {
            wake.SetValue(null, broken);
            var instance = driver.GetMethod("GetInstance",BindingFlags.Public | BindingFlags.Static).Invoke(null,null);
            try
            {
                driver.GetMethod("WakeupMain", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(instance,null);
                throw new Exception("Expected EBADF was swallowed");
            }
            catch (TargetInvocationException ex)
            {
                var error = ex.InnerException;
                if (error is ArgumentException && error.InnerException != null) error = error.InnerException;
                if (error.GetType().FullName != "Mono.Unix.UnixIOException" ||
                    error.GetType().GetProperty("NativeErrorCode").GetValue(error,null).ToString() != "9") throw;
                Console.WriteLine("PASS other errors propagate: EBADF");
            }
        }
        finally { wake.SetValue(null, original); close(fds[0]); ((IDisposable)broken).Dispose(); }
    }
    [STAThread] static void Main()
    {
        Console.WriteLine("WINFORMS="+typeof(Form).Assembly.Location);
        int posts = int.Parse(Environment.GetEnvironmentVariable("PROBE_POSTS") ?? "1");
        const int target = 150000;
        var form = new Form { Text = "Wake pipe probe" };
        var result = new TextBox { AccessibleName="Wake pipe result", ReadOnly=true, Dock=DockStyle.Fill };
        form.Controls.Add(result);
        int frame = 0, completed = 0;
        EventHandler idle = null;
        idle = (sender, args) => {
            if (frame == 0)
            {
                driver = typeof(Form).Assembly.GetType("System.Windows.Forms.XplatUIX11");
                var stream = driver.GetField("wake_receive", BindingFlags.Static | BindingFlags.NonPublic).GetValue(null);
                readFd = (int)stream.GetType().GetProperty("Handle").GetValue(stream,null);
                Console.WriteLine("CAPACITY=" + fcntl(readFd, 1032) + " INITIAL=" + Pending());
            }
            if (frame == target)
            {
                Application.Idle -= idle;
                if (completed != target*posts) throw new Exception("Missing callback");
                Console.WriteLine("PASS all "+completed+" callbacks in order; PIPE="+Pending());
                CheckOtherError();
                var drainWatch = Stopwatch.StartNew();
                var cpuBefore = Process.GetCurrentProcess().TotalProcessorTime;
                var timer = new Timer { Interval=20 };
                timer.Tick += (s,e) => {
                    timer.Stop();
                    Console.WriteLine("PASS timer after full pipe; PIPE="+Pending()+" ELAPSED_MS="+drainWatch.Elapsed.TotalMilliseconds);
                    var modalWatch = Stopwatch.StartNew();
                    using (var modal = new Form { Text="Wake pipe modal probe" })
                    using (var closeTimer = new Timer { Interval=20 })
                    {
                        closeTimer.Tick += (s2,e2) => { closeTimer.Stop(); modal.Close(); };
                        modal.Shown += (s2,e2) => closeTimer.Start();
                        modal.ShowDialog(form);
                    }
                    Console.WriteLine("PASS modal open and close ELAPSED_MS="+modalWatch.Elapsed.TotalMilliseconds);
                    result.Text = "150000 callbacks completed in order";
                    result.Focus();
                    var drainTimer = new Timer { Interval=20 };
                    drainTimer.Tick += (s3,e3) => {
                        if (Pending() != 0 && drainWatch.ElapsedMilliseconds < 5000) return;
                        drainTimer.Stop();
                        Console.WriteLine("DRAIN_FINAL pipe="+Pending()+" WALL_MS="+drainWatch.Elapsed.TotalMilliseconds+" CPU_MS="+(Process.GetCurrentProcess().TotalProcessorTime-cpuBefore).TotalMilliseconds);
                        if (Pending() != 0) throw new Exception("Wake pipe did not drain within five seconds");
                        Console.WriteLine("PROBE COMPLETE");
                    };
                    drainTimer.Start();
                };
                // This uses the same native SetTimer/WakeupMain path that failed
                // while constructing the emulator's exception dialog.
                timer.Start();
                return;
            }
            frame++;
            try
            {
                for (int i=0;i<posts;i++) {
                    int expected = (frame-1)*posts+i+1;
                    form.BeginInvoke((Action)(() => { completed++; if (completed != expected) throw new Exception("Callback order mismatch"); }));
                }
                if (frame % 10000 == 0) Console.WriteLine("FRAME="+frame+" COMPLETED="+completed+" PIPE="+Pending());
            }
            catch (Exception ex)
            {
                Application.Idle -= idle;
                Console.WriteLine("CAUGHT frame="+frame+" completed="+completed+" pipe="+Pending()+" "+ex);
                Console.WriteLine("PROBE COMPLETE");
            }
        };
        Application.Idle += idle;
        Application.Run(form);
    }
}
