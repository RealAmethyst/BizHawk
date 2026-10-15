using System;
using System.ComponentModel;
using System.IO;
using System.Reflection;
using System.Windows.Forms;

class PropertyGridProbe
{

	class Settings
	{
		[Description("Use dynamic recompilation.")]
		public bool JIT { get; set; }

		[Description("The console nickname.")]
		public string Username { get; set; } = "Player";

		[Description("Playback volume.")]
		public int Volume { get; set; } = 75;
	}

	static object CreateSettings()
	{
		var coreSettings = Environment.GetEnvironmentVariable("BIZHAWK_PROPERTY_GRID_CORE_SETTINGS");
		if (String.IsNullOrEmpty(coreSettings)) return new Settings();
		var assembly = Assembly.LoadFrom(Path.Combine(Environment.CurrentDirectory, "dll", "BizHawk.Emulation.Cores.dll"));
		return Activator.CreateInstance(assembly.GetType("BizHawk.Emulation.Cores.Consoles.Nintendo.NDS.NDS+" + coreSettings, true));
	}

	static void ObservePopup(PropertyGrid grid)
	{
		var view = typeof(PropertyGrid).GetField("property_grid_view", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(grid);
		var popup = (Form)view.GetType().GetField("dropdown_form", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(view);
		popup.VisibleChanged += (s, e) => Console.WriteLine("Popup visible: " + popup.Visible);
	}

	[STAThread]
	static void Main()
	{
		var form = new Form { Text = "Property grid probe", Width = 600, Height = 500 };
		var grid = new PropertyGrid { Dock = DockStyle.Fill, SelectedObject = CreateSettings(), ToolbarVisible = false, TabIndex = 1 };
		ObservePopup(grid);
		grid.PropertyValueChanged += (s, e) => Console.WriteLine("Changed: " + e.ChangedItem.Label + "=" + e.ChangedItem.Value);
		var before = new Button { Text = "Before settings", Dock = DockStyle.Top, TabIndex = 0 };
		var defaults = new Button { Text = "Defaults", Dock = DockStyle.Bottom, TabIndex = 2 };
		defaults.Click += (s, e) => grid.SelectedObject = CreateSettings();
		form.Controls.Add(grid);
		form.Controls.Add(before);
		form.Controls.Add(defaults);
		form.Shown += (s, e) => before.Select();
		Application.Run(form);
	}
}
