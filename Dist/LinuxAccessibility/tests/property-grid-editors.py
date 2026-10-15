"""Real melonDS settings converters in a standalone PropertyGrid; no core construction."""
import os
import sys
from session import test_session

settings_type = sys.argv[2]
assert settings_type in ('NDSSettings', 'NDSSyncSettings')
os.environ['BIZHAWK_PROPERTY_GRID_CORE_SETTINGS'] = settings_type

with test_session('property-grid') as (work, app):
    import gi
    gi.require_version('Atspi', '2.0')
    from gi.repository import Atspi
    from events import pump, key, focus_window

    def descendants(node):
        yield node
        for i in range(node.get_child_count()):
            yield from descendants(node.get_child_at_index(i))

    def has_state(node, state):
        node.clear_cache()
        return node.get_state_set().contains(state)

    desktop = Atspi.get_desktop(0)
    pump(1)
    table = next(n for n in descendants(desktop) if n.get_role_name() == 'tree table')
    events = []
    listener = Atspi.EventListener.new(lambda e, *args: events.append(
        (e.type, e.source, e.detail1, e.any_data)))
    for event_type in ('object:state-changed:focused', 'object:active-descendant-changed'):
        assert listener.register(event_type)
    focus_window('Property grid probe')
    key('Tab')

    def row_name():
        return table.get_selection_iface().get_selected_child(0).get_name()

    def select_row(name):
        key('Home')
        for _ in range(table.get_child_count()):
            if row_name() == name:
                return
            key('Down')
        raise AssertionError('Missing row: ' + name)

    def value_cell(name):
        cells = [table.get_child_at_index(i) for i in range(table.get_child_count())
                 if table.get_child_at_index(i).get_role_name() == 'table cell']
        index = next(i for i in range(0, len(cells), 2) if cells[i].get_name() == name)
        return cells[index + 1]

    def open_choices(name, expected, selected):
        select_row(name)
        events.clear()
        key('Tab')
        button = next(n for n in descendants(table)
                      if n.get_role_name() == 'button' and n.get_name() == name)
        assert has_state(button, Atspi.StateType.FOCUSED)
        assert has_state(button, Atspi.StateType.FOCUSABLE)
        assert has_state(button, Atspi.StateType.SHOWING)
        assert any(t == 'object:state-changed:focused' and source == button and detail == 1
                   for t, source, detail, child in events), events
        assert button.get_description()
        key('space')
        popup = next(n for n in descendants(desktop)
                     if n != table and n.get_role_name() == 'tree table'
                     and has_state(n, Atspi.StateType.FOCUSED))
        assert [popup.get_child_at_index(i).get_name()
                for i in range(popup.get_child_count())] == expected
        assert any(t == 'object:state-changed:focused' and source == popup and detail == 1
                   for t, source, detail, child in events), events
        assert has_state(popup.get_parent(), Atspi.StateType.ACTIVE)
        assert has_state(popup.get_parent(), Atspi.StateType.SHOWING)
        check_choice(popup, selected)
        return popup

    def check_choice(popup, name, changed=False):
        selection = popup.get_selection_iface()
        assert selection.get_n_selected_children() == 1
        child = selection.get_selected_child(0)
        assert child.get_name() == name
        for state in (Atspi.StateType.FOCUSED, Atspi.StateType.SELECTED,
                      Atspi.StateType.SHOWING, Atspi.StateType.VISIBLE):
            assert has_state(child, state), (name, state)
        assert child.get_parent() == popup
        if changed:
            assert any(t == 'object:active-descendant-changed' and source == popup
                       and item == child for t, source, detail, item in events), events

    if settings_type == 'NDSSyncSettings':
        name = '3D Renderer'
        choices = ['Software', 'OpenGL Classic', 'OpenGL Compute']
    else:
        name = 'Audio Bit Depth'
        choices = ['Auto', '10', '16']

    popup = open_choices(name, choices, choices[0])
    for choice in choices[1:]:
        events.clear()
        key('Down')
        check_choice(popup, choice, changed=True)
        assert value_cell(name).get_name() == choices[0], 'not committed while choosing'
    key('Return')
    key('Return')
    assert value_cell(name).get_name() == choices[-1]
    assert has_state(table, Atspi.StateType.FOCUSED)

    popup = open_choices(name, choices, choices[-1])
    events.clear()
    key('Up')
    check_choice(popup, choices[-2], changed=True)
    key('Escape')
    assert value_cell(name).get_name() == choices[-1], 'Escape must preserve committed value'
    assert (work / 'probe.log').read_text().rstrip().endswith('Popup visible: False')
    desktop.clear_cache()
    assert popup not in list(descendants(desktop)), 'closed popup must leave accessible tree'
    # Xvfb has no window manager to return input focus to the owner after hiding a popup.
    focus_window('Property grid probe')
    assert table.get_component_iface().grab_focus()
    pump()

    # Rebuild the native rows and toolbar, then reopen the editor and its popup.
    defaults = next(n for n in descendants(desktop) if n.get_name() == 'Defaults')
    assert defaults.get_action_iface().do_action(0)
    pump()
    assert table.get_component_iface().grab_focus()
    pump()
    open_choices(name, choices, choices[0])
    key('Return')
    key('Return')
    assert value_cell(name).get_name() == choices[0]
    assert app.poll() is None
    print('CORE EDITOR CHECKS PASSED:', settings_type,
          'native named button, popup focus and active window, live converted selections, commit, cancel, reopen, defaults', flush=True)
