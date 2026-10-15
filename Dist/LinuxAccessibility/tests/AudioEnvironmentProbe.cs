using System;
using System.Reflection;
using System.Runtime.InteropServices;
class AudioEnvironmentProbe
{
 [DllImport("libc", CallingConvention=CallingConvention.Cdecl)]
 static extern IntPtr getenv(string name);
 static void Check(bool result, string label)
 {
  if (!result) throw new Exception(label);
  Console.WriteLine("PASS " + label);
 }
 static void Main(string[] args)
 {
  var assembly = Assembly.LoadFrom(args[0]);
  var method = assembly.GetType("BizHawk.Client.EmuHawk.Sound").GetMethod("ConfigureLinuxAudioEnvironment", BindingFlags.Static | BindingFlags.NonPublic);
  var constructor = assembly.GetType("BizHawk.Client.EmuHawk.Sound").GetConstructors()[0];
  var il = constructor.GetMethodBody().GetILAsByteArray();
  var call = BitConverter.GetBytes(method.MetadataToken);
  bool called = false;
  for (int i = 0; i + 4 < il.Length; i++)
   if (il[i] == 0x28 && il[i+1] == call[0] && il[i+2] == call[1] && il[i+3] == call[2] && il[i+4] == call[3]) called = true;
  Check(called, "sound initialization calls audio environment setup");
  Environment.SetEnvironmentVariable("ALSOFT_DRIVERS", null);
  Environment.SetEnvironmentVariable("PULSE_PROP", null);
  method.Invoke(null, new object[] { "" });
  Check(Environment.GetEnvironmentVariable("ALSOFT_DRIVERS") == "pulse,", "server routing with fallback");
  Check(Environment.GetEnvironmentVariable("PULSE_PROP") == "application.name=BizHawk application.id=BizHawk", "stable application identity");
  Check(Marshal.PtrToStringAnsi(getenv("ALSOFT_DRIVERS")) == "pulse,", "native OpenAL sees backend preference");
  Check(Marshal.PtrToStringAnsi(getenv("PULSE_PROP")) == "application.name=BizHawk application.id=BizHawk", "native Pulse sees application identity");
  Environment.SetEnvironmentVariable("ALSOFT_DRIVERS", "pipewire");
  Environment.SetEnvironmentVariable("PULSE_PROP", "application.name=Custom");
  method.Invoke(null, new object[] { "" });
  Check(Environment.GetEnvironmentVariable("ALSOFT_DRIVERS") == "pipewire", "explicit backend preserved");
  Check(Environment.GetEnvironmentVariable("PULSE_PROP") == "application.name=Custom", "explicit stream properties preserved");
  Environment.SetEnvironmentVariable("ALSOFT_DRIVERS", null);
  method.Invoke(null, new object[] { "Selected output" });
  Check(Environment.GetEnvironmentVariable("ALSOFT_DRIVERS") == null, "explicit device retains backend");
 }
}
