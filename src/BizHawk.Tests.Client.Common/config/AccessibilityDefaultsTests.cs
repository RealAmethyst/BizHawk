using System.Collections.Generic;
using System.IO;

using BizHawk.Client.Common;
using BizHawk.Emulation.Cores.Consoles.Nintendo.NDS;

using Newtonsoft.Json;

namespace BizHawk.Tests.Client.Common.config
{
	[TestClass]
	public sealed class AccessibilityDefaultsTests
	{
		[TestMethod]
		public void HotkeysMatchApprovedWindowsRelease()
		{
			// Extracted from RealAmethyst/BizHawk v1.1, not generated from Binding.cs.
			using var stream = typeof(AccessibilityDefaultsTests).Assembly.GetManifestResourceStream(
				"BizHawk.Tests.Client.Common.data.accessibility-hotkeys-v1.1.json");
			using var reader = new StreamReader(stream!);
			var expected = JsonConvert.DeserializeObject<Dictionary<string, string>>(reader.ReadToEnd())!;
			var actual = new Dictionary<string, string>();
			HotkeyInfo.ResolveWithDefaults(actual);
			Assert.AreEqual(expected.Count, actual.Count, "Every hotkey must have an approved default.");
			foreach (var (name, binding) in expected)
			{
				Assert.AreEqual(binding, actual[name], $"Fresh config changed the approved binding for {name}.");
				Assert.AreEqual(binding, HotkeyInfo.AllHotkeys[name].DefaultBinding, $"Restore defaults changed {name}.");
			}
		}

		[TestMethod]
		public void ExistingUserBindingsArePreserved()
		{
			var bindings = new Dictionary<string, string> { ["Frame Advance"] = "Ctrl+F", ["Fast Forward"] = "" };
			HotkeyInfo.ResolveWithDefaults(bindings);
			Assert.AreEqual("Ctrl+F", bindings["Frame Advance"], "An explicit user binding must survive loading.");
			Assert.AreEqual("", bindings["Fast Forward"], "An explicitly cleared binding must stay cleared.");
		}

		[TestMethod]
		public void DsJitMatchesApprovedWindowsRelease()
			=> Assert.IsTrue(new NDS.NDSSyncSettings().EnableJIT, "v1.1 enables DS JIT by default.");
	}
}
