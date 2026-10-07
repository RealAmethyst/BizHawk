"""Mock Orca calls back into AT-SPI before replying, reproducing the UI deadlock.

Run only through dbus-run-session on the test desktop. No real speech is emitted.
"""
import time
import gi

gi.require_version('Gio', '2.0')
gi.require_version('Atspi', '2.0')
from gi.repository import Gio, GLib, Atspi
from session import test_session
from events import pump

Atspi.set_timeout(1000, 1000)
connection = Gio.bus_get_sync(Gio.BusType.SESSION, None)
connection.call_sync('org.freedesktop.DBus', '/org/freedesktop/DBus',
                     'org.freedesktop.DBus', 'RequestName',
                     GLib.Variant('(su)', ('org.gnome.Orca.Service', 0)),
                     None, Gio.DBusCallFlags.NONE, -1, None)
node = Gio.DBusNodeInfo.new_for_xml('''<node>
<interface name="org.gnome.Orca.Service">
  <method name="PresentMessage"><arg type="s" direction="in"/><arg type="b" direction="out"/></method>
</interface>
<interface name="org.gnome.Orca.Module">
  <method name="ListCommands"><arg type="a(ss)" direction="out"/></method>
  <method name="ExecuteCommand"><arg type="s" direction="in"/><arg type="b" direction="in"/><arg type="b" direction="out"/></method>
</interface>
</node>''')
component = None
queries = []
events = []

def call(conn, sender, path, interface, method, parameters, invocation):
    args = parameters.unpack()
    start = time.monotonic()
    try:
        rect = component.get_extents(Atspi.CoordType.SCREEN)
        queries.append((method, time.monotonic() - start, rect.width > 0))
    except Exception as error:
        queries.append((method, time.monotonic() - start, str(error)))
    if method == 'ListCommands':
        invocation.return_value(GLib.Variant('(a(ss))', ([('InterruptSpeech', 'Interrupt speech')],)))
    else:
        events.append((method, args))
        invocation.return_value(GLib.Variant('(b)', (args != ('reject',),)))

connection.register_object('/org/gnome/Orca/Service', node.interfaces[0], call, None, None)
connection.register_object('/org/gnome/Orca/Service/SpeechManager', node.interfaces[1], call, None, None)

with test_session('bizhawk', 'speech-fixture.lua') as (work, process):
    desktop = Atspi.get_desktop(0)
    deadline = time.monotonic() + 10
    while not desktop.get_child_count() and time.monotonic() < deadline:
        pump(.05)
    app = desktop.get_child_at_index(0)
    component = app.get_child_at_index(0).get_component_iface()
    component.get_extents(Atspi.CoordType.SCREEN)
    (work / 'speech-ready').touch()
    deadline = time.monotonic() + 15
    while time.monotonic() < deadline:
        pump(.05)
        if 'speech fixture complete' in (work / 'probe.log').read_text():
            break
    diagnostics = (work / 'probe.log').read_text()
    assert 'speech fixture complete' in diagnostics
    print('Orca AT-SPI callbacks:', queries, flush=True)
    assert len(queries) == 7 and all(q[2] is True and q[1] < .5 for q in queries), queries
    assert '[speech]' not in diagnostics, 'Temporary backend diagnostics must not ship'
    assert 'speech.say failed:' in diagnostics and 'speech.say failed: \n' not in diagnostics
    assert events == [
        ('ExecuteCommand', ('InterruptSpeech', False)),
        ('PresentMessage', ('first',)),
        ('PresentMessage', ('second',)),
        ('PresentMessage', ('third',)),
        ('PresentMessage', ('reject',)),
        ('ExecuteCommand', ('InterruptSpeech', False)),
    ], events
    print('Speech ordering, native failure reporting, and reentrant AT-SPI checks passed.')
