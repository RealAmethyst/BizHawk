using System.Collections.Generic;
using System.Linq;

#if DEBUG
using System.Diagnostics;
using BizHawk.Common.CollectionExtensions;
#endif

namespace BizHawk.Client.Common
{
	public class HotkeyInfo
	{
		public static readonly IReadOnlyDictionary<string, HotkeyInfo> AllHotkeys;

		public static readonly IReadOnlyList<string> Groupings;

		static HotkeyInfo()
		{
			var dict = new Dictionary<string, HotkeyInfo>();
			var i = 0;
#if true
			void Bind(string tabGroup, string displayName, string defaultBinding = "", string toolTip = "")
				=> dict.Add(displayName, new(tabGroup: tabGroup, i++, displayName: displayName, toolTip: toolTip, defaultBinding: defaultBinding));
#else //TODO switch to a sort key more resilient than the DisplayName, like with this example (need to update `Config.HotkeyBindings["A Hotkey"]` usages across codebase; please switch it to a `Config.GetHotkeyBindings` method so it can return "<not bound>")
			void Bind(string tabGroup, string displayName, string defaultBinding = "", string toolTip = "")
				=> dict.Add($"{tabGroup}__{displayName}".Replace(" ", ""), new(tabGroup: tabGroup, i++, displayName: displayName, toolTip: toolTip, defaultBinding: defaultBinding));
#endif

			Bind("General", "Frame Advance");
			Bind("General", "Rewind");
			Bind("General", "Pause");
			Bind("General", "Fast Forward", "Tab");
			Bind("General", "Turbo", "Shift+Tab");
			Bind("General", "Toggle Throttle");
			Bind("General", "Soft Reset");
			Bind("General", "Hard Reset");
			Bind("General", "Autofire");
			Bind("General", "Autohold");
			Bind("General", "Clear Autohold");
			Bind("General", "Screenshot");
			Bind("General", "Full Screen", "Alt+Enter");
			Bind("General", "Open ROM", "Ctrl+O");
			Bind("General", "Close ROM", "Ctrl+W");
			Bind("General", "Load Last ROM");
			Bind("General", "Flush SaveRAM");
			Bind("General", "Display FPS");
			Bind("General", "Frame Counter");
			Bind("General", "Lag Counter");
			Bind("General", "Input Display");
			Bind("General", "Toggle BG Input");
			Bind("General", "Toggle Menu");
			Bind("General", "Volume Up");
			Bind("General", "Volume Down");
			Bind("General", "Record A/V");
			Bind("General", "Stop A/V");
			Bind("General", "Larger Window", "Alt+Up");
			Bind("General", "Smaller Window", "Alt+Down");
			Bind("General", "Increase Speed");
			Bind("General", "Decrease Speed");
			Bind("General", "Reset Speed");
			Bind("General", "Reboot Core", "Ctrl+R");
			Bind("General", "Toggle Sound");
			Bind("General", "Exit Program");
			Bind("General", "Screen Raw to Clipboard");
			Bind("General", "Screen Client to Clipboard");
			Bind("General", "Toggle Skip Lag Frame");
			Bind("General", "Toggle Key Priority");
			Bind("General", "Frame Inch");
			Bind("General", "Toggle Messages");
			Bind("General", "Toggle Display Nothing");
			Bind("General", "Accept Background Input");
			Bind("General", "Capture Mouse");
			Bind("General", "Toggle Stay on Top");

			Bind("Save States", "Save State 1", "Shift+F1");
			Bind("Save States", "Save State 2", "Shift+F2");
			Bind("Save States", "Save State 3", "Shift+F3");
			Bind("Save States", "Save State 4", "Shift+F4");
			Bind("Save States", "Save State 5", "Shift+F5");
			Bind("Save States", "Save State 6", "Shift+F6");
			Bind("Save States", "Save State 7", "Shift+F7");
			Bind("Save States", "Save State 8", "Shift+F8");
			Bind("Save States", "Save State 9", "Shift+F9");
			Bind("Save States", "Save State 10", "Shift+F10");
			Bind("Save States", "Load State 1", "F1");
			Bind("Save States", "Load State 2", "F2");
			Bind("Save States", "Load State 3", "F3");
			Bind("Save States", "Load State 4", "F4");
			Bind("Save States", "Load State 5", "F5");
			Bind("Save States", "Load State 6", "F6");
			Bind("Save States", "Load State 7", "F7");
			Bind("Save States", "Load State 8", "F8");
			Bind("Save States", "Load State 9", "F9");
			Bind("Save States", "Load State 10", "F10");
			Bind("Save States", "Select State 1");
			Bind("Save States", "Select State 2");
			Bind("Save States", "Select State 3");
			Bind("Save States", "Select State 4");
			Bind("Save States", "Select State 5");
			Bind("Save States", "Select State 6");
			Bind("Save States", "Select State 7");
			Bind("Save States", "Select State 8");
			Bind("Save States", "Select State 9");
			Bind("Save States", "Select State 10");
			Bind("Save States", "Quick Load");
			Bind("Save States", "Quick Save");
			Bind("Save States", "Save Named State");
			Bind("Save States", "Load Named State");
			Bind("Save States", "Previous Slot");
			Bind("Save States", "Next Slot");

			Bind("Movie", "Toggle read-only");
			Bind("Movie", "Play Movie");
			Bind("Movie", "Record Movie");
			Bind("Movie", "Stop Movie");
			Bind("Movie", "Play from beginning");
			Bind("Movie", "Save Movie");

			Bind("Tools", "RAM Watch");
			Bind("Tools", "RAM Search");
			Bind("Tools", "Hex Editor");
			Bind("Tools", "Trace Logger");
			Bind("Tools", "Lua Console");
			Bind("Tools", "Cheats");
			Bind("Tools", "TAStudio");
			Bind("Tools", "ToolBox");
			Bind("Tools", "Virtual Pad");

			Bind("RAM Search", "New Search");
			Bind("RAM Search", "Do Search");
			Bind("RAM Search", "Previous Compare To");
			Bind("RAM Search", "Next Compare To");
			Bind("RAM Search", "Previous Operator");
			Bind("RAM Search", "Next Operator");

			Bind("TAStudio", "Add Branch");
			Bind("TAStudio", "Delete Branch");
			Bind("TAStudio", "Show Cursor");
			Bind("TAStudio", "Select Current Frame");
			Bind("TAStudio", "Toggle Follow Cursor");
			Bind("TAStudio", "Toggle Auto-Restore");
			Bind("TAStudio", "Seek To Green Arrow");
			Bind("TAStudio", "Toggle Turbo Seek");
			Bind("TAStudio", "Undo", "Ctrl+Z"); // TODO: these are getting not unique enough
			Bind("TAStudio", "Redo", "Ctrl+Y");
			Bind("TAStudio", "Seek To Prev Marker");
			Bind("TAStudio", "Seek To Next Marker");
			Bind("TAStudio", "Cancel Seek");
			Bind("TAStudio", "Set Marker");
			Bind("TAStudio", "Delete Marker");
			Bind("TAStudio", "Sel. bet. Markers");
			Bind("TAStudio", "Select All");
			Bind("TAStudio", "Reselect Clip.");
			Bind("TAStudio", "Clear Frames");
			Bind("TAStudio", "Delete Frames");
			Bind("TAStudio", "Insert Frame");
			Bind("TAStudio", "Insert # Frames");
			Bind("TAStudio", "Clone Frames");
			Bind("TAStudio", "Clone # Times");
			Bind("TAStudio", "Analog Increment");
			Bind("TAStudio", "Analog Decrement");
			Bind("TAStudio", "Analog Incr. by 10");
			Bind("TAStudio", "Analog Decr. by 10");
			Bind("TAStudio", "Analog Maximum");
			Bind("TAStudio", "Analog Minimum");

			Bind("SNES", "Toggle BG 1");
			Bind("SNES", "Toggle BG 2");
			Bind("SNES", "Toggle BG 3");
			Bind("SNES", "Toggle BG 4");
			Bind("SNES", "Toggle OBJ 1");
			Bind("SNES", "Toggle OBJ 2");
			Bind("SNES", "Toggle OBJ 3");
			Bind("SNES", "Toggle OBJ 4");

			Bind("GB", "GB Toggle BG");
			Bind("GB", "GB Toggle Obj");
			Bind("GB", "GB Toggle Window");

			Bind("Analog", "Y Up Small", toolTip: "For Virtual Pad");
			Bind("Analog", "Y Up Large", toolTip: "For Virtual Pad");
			Bind("Analog", "Y Down Small", toolTip: "For Virtual Pad");
			Bind("Analog", "Y Down Large", toolTip: "For Virtual Pad");
			Bind("Analog", "X Up Small", toolTip: "For Virtual Pad");
			Bind("Analog", "X Up Large", toolTip: "For Virtual Pad");
			Bind("Analog", "X Down Small", toolTip: "For Virtual Pad");
			Bind("Analog", "X Down Large", toolTip: "For Virtual Pad");

			Bind("Tools", "Toggle All Cheats");
			Bind("Tools", "Toggle Last Lua Script");

			Bind("NDS", "Next Screen Layout");
			Bind("NDS", "Previous Screen Layout");
			Bind("NDS", "Screen Rotate");
			Bind("NDS", "Swap Screens");

			Bind("RAIntegration", "Open RA Overlay");
			Bind("RAIntegration", "RA Up");
			Bind("RAIntegration", "RA Down");
			Bind("RAIntegration", "RA Left");
			Bind("RAIntegration", "RA Right");
			Bind("RAIntegration", "RA Confirm");
			Bind("RAIntegration", "RA Cancel");
			Bind("RAIntegration", "RA Quit");

			AllHotkeys = dict;
			Groupings = dict.Values.Select(static info => info.TabGroup).Distinct().ToList();

#if DEBUG
			var bindings = dict.Values
				// We skip TAStudio analog hotkeys because they have special handling (no other hotkey can trigger when in analog editing mode)
				.Where(static info => !(info.DisplayName.StartsWith("Analog ") && info.TabGroup == "TAStudio") && !string.IsNullOrEmpty(info.DefaultBinding))
				.Select(static info => info.DefaultBinding)
				.ToArray();
			Debug.Assert(bindings.Distinct().CountIsExactly(bindings.Length), "Do not default bind multiple hotkeys to the same button combination.");
#endif
		}

		public static void ResolveWithDefaults(IDictionary<string, string> dict)
		{
			foreach (var k in dict.Keys.Where(static k => !AllHotkeys.ContainsKey(k)).ToArray()) dict.Remove(k); // remove extraneous
			foreach (var (k, v) in AllHotkeys) if (!dict.ContainsKey(k)) dict[k] = v.DefaultBinding; // add missing
		}

		public readonly string DefaultBinding;

		public readonly string DisplayName;

		public readonly int Ordinal;

		public readonly string TabGroup;

		public readonly string ToolTip;

		private HotkeyInfo(string tabGroup, int ordinal, string displayName, string toolTip, string defaultBinding)
		{
			DefaultBinding = defaultBinding;
			DisplayName = displayName;
			Ordinal = ordinal;
			TabGroup = tabGroup;
			ToolTip = toolTip;
		}
	}
}
