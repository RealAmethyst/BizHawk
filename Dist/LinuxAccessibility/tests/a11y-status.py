"""Private accessibility status service; never activates a desktop service."""
import os
from gi.repository import Gio, GLib
connection = Gio.bus_get_sync(Gio.BusType.SESSION, None)
info = Gio.DBusNodeInfo.new_for_xml('''<node>
<interface name="org.a11y.Status"><property name="IsEnabled" type="b" access="read"/>
<property name="ScreenReaderEnabled" type="b" access="read"/></interface>
<interface name="org.a11y.Bus"><method name="GetAddress"><arg direction="out" type="s"/></method></interface>
</node>''')
for interface in info.interfaces:
    connection.register_object('/org/a11y/bus', interface,
        lambda c,s,p,i,m,args,call: call.return_value(GLib.Variant('(s)', (os.environ['AT_SPI_BUS_ADDRESS'],))),
        lambda *args: GLib.Variant('b', True), None)
Gio.bus_own_name_on_connection(connection, 'org.a11y.Bus', Gio.BusNameOwnerFlags.NONE,
    lambda *args: print('READY', flush=True), None)
GLib.MainLoop().run()
