"""Native PropertyGrid keyboard navigation and AT-SPI events; no emulator."""
from session import test_session
with test_session('property-grid') as (work, app):
    import gi
    gi.require_version('Atspi', '2.0')
    from gi.repository import Atspi
    from events import pump, key, focus_window
    pump(1)
    desktop = Atspi.get_desktop(0)

    def descendants(node):
        yield node
        for i in range(node.get_child_count()):
            yield from descendants(node.get_child_at_index(i))

    nodes = list(descendants(desktop))
    table = next(n for n in nodes if n.get_role_name() == 'tree table')
    def table_cells():
        return [table.get_child_at_index(i) for i in range(table.get_child_count())
                if table.get_child_at_index(i).get_role_name() == 'table cell']

    cells = table_cells()
    assert [c.get_name() for c in cells] == ['Misc', '', 'JIT', 'False', 'Username', 'Player', 'Volume', '75']
    focus_window('Property grid probe')
    active = []
    listener = Atspi.EventListener.new(lambda e, *args: active.append(e.any_data.get_name()))
    assert listener.register('object:active-descendant-changed')
    key('Tab')
    assert active[-1:] == ['JIT'], active
    assert cells[2].get_description() == 'Use dynamic recompilation.'
    key('Down')
    assert active[-1:] == ['Username'], active
    key('Down')
    assert active[-1:] == ['Volume'], active
    # Native editing, rather than setting the synthetic fixture's properties.
    editor_events = []
    editor_listener = Atspi.EventListener.new(
        lambda e, *args: editor_events.append((e.type, e.source, e.detail1, e.detail2)))
    for kind in ('object:state-changed:focused', 'object:text-changed',
                 'object:text-caret-moved', 'object:text-selection-changed'):
        assert editor_listener.register(kind)

    def focused_editor(name, role='text'):
        editor = next(n for n in descendants(table)
                      if n.get_role_name() == role and n.get_name() == name)
        editor.clear_cache()
        assert editor.get_state_set().contains(Atspi.StateType.FOCUSED)
        assert any(t == 'object:state-changed:focused' and source == editor and value == 1
                   for t, source, value, length in editor_events), editor_events
        return editor

    key('Tab')
    editor = focused_editor('Volume')
    text = editor.get_text_iface()
    assert Atspi.Text.get_text(text, 0, -1) == '75'
    key('Control_L', 'a')
    assert Atspi.Text.get_n_selections(text) == 1
    selected = Atspi.Text.get_selection(text, 0)
    assert (selected.start_offset, selected.end_offset) == (0, 2)
    assert any(t == 'object:text-selection-changed' and source == editor
               for t, source, value, length in editor_events)
    key('4')
    key('2')
    assert Atspi.Text.get_text(text, 0, -1) == '42'
    assert cells[7].get_name() == '75', 'native value must remain uncommitted'
    assert any(t.startswith('object:text-changed:insert') and source == editor
               for t, source, value, length in editor_events)
    key('Left')
    assert Atspi.Text.get_caret_offset(text) == 1
    assert any(t == 'object:text-caret-moved' and source == editor and value == 1
               for t, source, value, length in editor_events)
    key('Return')
    assert active[-1:] == ['Volume'], active
    assert cells[7].get_name() == '42'
    assert 'Changed: Volume=42' in (work / 'probe.log').read_text()
    key('Up')
    key('Tab')
    editor = focused_editor('Username')
    key('Control_L', 'a')
    key('a')
    assert Atspi.Text.get_text(editor, 0, -1) == 'a'
    assert cells[5].get_name() == 'Player'
    key('Return')
    assert cells[5].get_name() == 'a'
    assert 'Changed: Username=a' in (work / 'probe.log').read_text()
    key('Tab')
    editor = focused_editor('Username')
    key('Control_L', 'a')
    key('z')
    assert Atspi.Text.get_text(editor, 0, -1) == 'z'
    key('Escape')
    assert cells[5].get_name() == 'a'
    assert active[-1:] == ['Username'], active
    key('Up')
    key('Tab')
    focused_editor('JIT', 'button')
    key('space')
    key('Up')
    key('Return')
    key('Return')
    assert cells[3].get_name() == 'True'
    assert 'Changed: JIT=True' in (work / 'probe.log').read_text()
    # Cursor movement must retain the cell objects, not destroy their identities.
    assert cells[2] == table.get_child_at_index(2)
    assert not cells[2].get_state_set().contains(Atspi.StateType.DEFUNCT)
    before = next(n for n in descendants(desktop) if n.get_name() == 'Before settings')
    key('Shift_L', 'Tab')
    before.clear_cache()
    assert before.get_state_set().contains(Atspi.StateType.FOCUSED)
    key('Tab')
    key('Up')
    key('Left')
    table.clear_cache()
    assert len(table_cells()) == 2
    key('Right')
    table.clear_cache()
    assert len(table_cells()) == 8
    defaults = next(n for n in descendants(desktop) if n.get_name() == 'Defaults')
    assert defaults.get_action_iface().do_action(0)
    pump()
    table.clear_cache()
    refreshed = [c.get_name() for c in table_cells()]
    assert refreshed == ['Misc', '', 'JIT', 'False', 'Username', 'Player', 'Volume', '75'], refreshed
    assert app.poll() is None
    print('PROPERTY GRID CHECKS PASSED: keyboard entry, row events, descriptions, live editor focus/text/selection/caret, integer/string/boolean edits, stable cells, defaults', flush=True)
