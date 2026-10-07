"""AT-SPI checks on a private X display and D-Bus session; never loads a game."""
from pathlib import Path
import subprocess
from session import test_session
with test_session('controls') as (w, p):
    import gi
    gi.require_version('Atspi', '2.0')
    from gi.repository import Atspi
    import time
    desktop = Atspi.get_desktop(0)

    def walk(node, depth=0):
        print(' ' * depth, repr(node.get_name()), node.get_role_name(), flush=True)
        for i in range(node.get_child_count()):
            walk(node.get_child_at_index(i), depth + 1)
    for i in range(40):
        if desktop.get_child_count():
            break
        time.sleep(0.25)
    walk(desktop)
    nodes = []

    def collect(n):
        nodes.append(n)
        for j in range(n.get_child_count()):
            collect(n.get_child_at_index(j))
    collect(desktop)

    def named(name):
        return next((n for n in nodes if n.get_name() == name))
    text = named('Script path')
    ti = text.get_text_iface()
    print('TEXT', Atspi.Text.get_text(ti, 0, -1), flush=True)
    assert Atspi.Text.get_text(ti, 0, -1) == 'main.lua'
    assert Atspi.EditableText.set_text_contents(text, 'test.lua')
    assert Atspi.Text.get_text(ti, 0, -1) == 'test.lua'
    slider = named('Volume').get_value_iface()
    print('VOLUME', slider.get_current_value(), flush=True)
    assert slider.get_current_value() == 75
    assert slider.set_current_value(40)
    assert slider.get_current_value() == 40
    button = named('Load script').get_action_iface()
    print('ACTION', button.get_action_name(0), flush=True)
    assert button.do_action(0)
    check = named('Run in background')
    assert check.get_action_iface().do_action(0)
    assert check.get_state_set().contains(Atspi.StateType.CHECKED)
    combo = next((n for n in nodes if n.get_role_name() == 'combo box'))
    selection = combo.get_selection_iface()
    print('SELECTION', selection.get_n_selected_children(), selection.get_selected_child(0), flush=True)
    assert selection.get_selected_child(0).get_name() == 'Default'
    assert selection.select_child(1)
    assert selection.get_selected_child(0).get_name() == 'Headphones'
    assert combo.get_name() == 'Output device'
    from events import pump, key, focus_window
    focus_window('BizHawk accessibility probe')
    events = []
    listener = Atspi.EventListener.new(lambda event, *args: events.append((event.type, event.source.get_name(), event.detail1)))
    assert listener.register('object:state-changed:focused')
    assert button.grab_focus() if hasattr(button, 'grab_focus') else named('Load script').get_component_iface().grab_focus()
    pump()
    assert text.get_component_iface().grab_focus()
    pump()
    assert any((t == 'object:state-changed:focused' and n == 'Script path' and (v == 1) for t, n, v in events)), events
    key('Tab')
    assert named('Load script').get_state_set().contains(Atspi.StateType.FOCUSED)
    key('Alt_L')
    key('Down')
    print('FOCUS EVENTS', events, flush=True)
    assert any((n == 'Open ROM' and v == 1 for t, n, v in events)), events
    key('Escape')
    key('Escape')
    key('Alt_L')
    key('Alt_L')
    events.clear()
    key('Down')
    assert not any(n == 'Open ROM' and v == 1 for t, n, v in events), events
    events.clear()
    key('Alt_L', 'f')
    key('Down')
    assert any(n == 'Open ROM' and v == 1 for t, n, v in events), events
    key('Escape')
    key('Escape')
    assert p.poll() is None
    assert not any((word in (w / 'probe.log').read_text() for word in ('Unhandled', 'Exception', '[Error')))
    table = named('Scripts').get_table_iface()
    assert table.get_n_rows() == 2 and table.get_n_columns() == 3
    for row, name in enumerate(('test', 'second')):
        cell = table.get_accessible_at(row, 0)
        assert cell.get_name() == name
        index = table.get_index_at(row, 0)
        assert index == cell.get_index_in_parent()
        assert table.get_row_at_index(index) == row
        assert table.get_column_at_index(index) == 0
    assert table.add_row_selection(1)
    assert table.is_row_selected(1)
    assert table.get_selected_rows() == [1]
    assert table.get_column_header(0).get_name() == 'Script'
    assert table.get_column_extent_at(1, 0) == 1 and table.get_row_extent_at(1, 0) == 1
    assert named('Close probe').get_action_iface().do_action(0)
    assert p.wait(timeout=5) == 0
    print('CONTROL CHECKS PASSED', flush=True)
