using System;
using System.Windows.Forms;
using System.Drawing;
class ControlsProbe
{
 [STAThread] static void Main()
 {
  Application.EnableVisualStyles();
  var form = new Form { Text="BizHawk accessibility probe", Width=600, Height=650 };
  var menu = new MenuStrip();
  var file = new ToolStripMenuItem("&File");
  file.DropDownItems.Add(new ToolStripMenuItem("&Open ROM"));
  file.DropDownItems.Add(new ToolStripMenuItem("&Lua Console"));
  menu.Items.Add(file); form.MainMenuStrip=menu; form.Controls.Add(menu);
  var input = new TextBox { AccessibleName="Script path", Text="main.lua", Location=new Point(20,60), TabIndex=0 };
  var button = new Button { Text="Load script", Location=new Point(20,110), TabIndex=1 };
  var check = new CheckBox { Text="Run in background", Location=new Point(20,155), Width=200, TabIndex=2 };
  var combo = new ComboBox { AccessibleName="Output device", Location=new Point(20,200), TabIndex=3, DropDownStyle=ComboBoxStyle.DropDownList };
  combo.Items.AddRange(new object[]{"Default", "Headphones"}); combo.SelectedIndex=0;
  var slider = new TrackBar { AccessibleName="Volume", Location=new Point(20,245), Minimum=0, Maximum=100, Value=75, TabIndex=4 };
  var status = new Label { Text="Ready", AccessibleName="Status", Location=new Point(20,290), Width=250 };
  button.Click += (s,e) => {status.Text="Script loaded"; Console.WriteLine("button invoked");};
  var list = new ListView { AccessibleName="Scripts", Location=new Point(20,330), Width=500, Height=170, View=View.Details, FullRowSelect=true, HideSelection=false, TabIndex=5 };
  list.Columns.Add("Script",150);list.Columns.Add("Status",100);list.Columns.Add("Path",200);
  list.Items.Add(new ListViewItem(new[]{"test", "Stopped", "/tmp/test.lua"}));
  list.Items.Add(new ListViewItem(new[]{"second", "Paused", "/tmp/second.lua"}));
  form.Controls.Add(list);
  var close = new Button { Text="Close probe", Location=new Point(20,530), TabIndex=6 };
  close.Click += (sender,args) => form.Close();form.Controls.Add(close);
  form.Controls.AddRange(new Control[]{input,button,check,combo,slider,status});
  form.Shown += (s,e) => {input.Select();Console.WriteLine("probe shown");};
  Application.Run(form);
 }
}
