"""AT-SPI checks on a private X display and D-Bus session; never loads a game."""
from pathlib import Path
import subprocess
from session import test_session
with test_session('bizhawk') as (w, p):
    import gi
    gi.require_version('Atspi', '2.0')
    from gi.repository import Atspi
    import time
    desktop = Atspi.get_desktop(0)

    def walk(node, depth=0):
        print(' ' * depth, repr(node.get_name()), node.get_role_name(), flush=True)
        if depth < 3:
            for i in range(node.get_child_count()):
                walk(node.get_child_at_index(i), depth + 1)
    for i in range(40):
        if desktop.get_child_count():
            break
        time.sleep(0.25)
    walk(desktop)
    from events import pump, key, focus_window
    app = desktop.get_child_at_index(0)
    windows = [app.get_child_at_index(i) for i in range(app.get_child_count())]
    for win in windows:
        print('WINDOW', win.get_name(), flush=True)
    assert any(('Lua Console' in win.get_name() for win in windows))

    def children(n):
        return [n.get_child_at_index(i) for i in range(n.get_child_count())]

    def child(n, name):
        return next((c for c in children(n) if c.get_name() == name))
    main = next((win for win in windows if win.get_name().startswith('BizHawk')))
    lua = next((win for win in windows if win.get_name() == 'Lua Console'))
    # Extents are synchronous remote queries, unlike cached names and roles.
    # Without the accessibility-aware throttle, each reply waits two frames:
    # 100 queries take about 3.3 seconds even with no game loaded.
    start = time.perf_counter()
    for _ in range(100):
        main.get_component_iface().get_extents(Atspi.CoordType.SCREEN)
    elapsed = time.perf_counter() - start
    print('ACCESSIBILITY LATENCY: 100 queries in', elapsed, 'seconds', flush=True)
    assert elapsed < 1.0, f'Accessibility replies are blocked by the frame loop: {elapsed:.3f}s'
    mainbar = next((c for c in children(main) if c.get_role_name() == 'menu bar'))
    file = child(mainbar, 'File')
    events = []
    listener = Atspi.EventListener.new(lambda event, *args: events.append((event.type, event.source.get_name(), event.detail1)))
    listener.register('object:state-changed:focused')
    focus_window(main.get_name())
    assert main.get_component_iface().grab_focus()
    pump()
    key('Alt_L')
    key('Down')
    assert any((n == 'Open ROM...' and v == 1 for t, n, v in events)), events
    assert child(file, 'Open ROM...').get_action_iface().do_action(0)
    pump(0.5)
    dialogs = children(app)
    print('OPEN DIALOG', [(n.get_name(), n.get_role_name()) for n in dialogs], flush=True)
    assert any((n.get_role_name() == 'dialog' or n.get_name().startswith('Open') for n in dialogs))
    key('Escape')
    pump(0.3)
    assert len(children(app)) == len(windows)
    luabar = next((c for c in children(lua) if c.get_role_name() == 'menu bar'))
    script = child(luabar, 'Script')
    focus_window(lua.get_name())
    assert script.get_action_iface().do_action(0)
    pump()
    assert child(script, 'Open Script...').get_action_iface().do_action(0)
    pump(0.5)
    print('SCRIPT DIALOG', [(n.get_name(), n.get_role_name()) for n in children(app)], flush=True)
    assert len(children(app)) > len(windows)
    key('Escape')
    pump(0.3)
    assert len(children(app)) == len(windows)
    assert p.poll() is None

    def descend(n):
        yield n
        for c in children(n):
            yield from descend(c)
    lua_nodes = list(descend(lua))
    scripts = next((n for n in lua_nodes if n.get_name() == 'Scripts'))
    assert scripts.get_role_name() in ('table', 'tree table')
    table = scripts.get_table_iface()
    print('SCRIPT TABLE', table.get_n_rows(), table.get_n_columns(), flush=True)
    print('SCRIPT CELLS', [table.get_accessible_at(0, c).get_name() for c in range(3)], flush=True)
    assert table.get_n_rows() == 1
    assert table.get_n_columns() == 3
    assert table.get_accessible_at(0, 0).get_name() == 'accessibility-fixture'
    assert table.get_accessible_at(0, 1).get_name() == 'Running'
    assert table.add_row_selection(0)
    assert scripts.get_component_iface().grab_focus()
    pump()
    assert script.get_action_iface().do_action(0)
    pump()
    assert child(script, 'Pause or Resume').get_action_iface().do_action(0)
    pump(0.3)
    assert table.get_accessible_at(0, 1).get_name() == 'Paused'
    assert table.is_row_selected(0)
    print('SCRIPT SELECTION AND PAUSE PASSED', flush=True)
    command = next((n for n in lua_nodes if n.get_name() == 'Lua command'))
    output = next((n for n in lua_nodes if n.get_name() == 'Output' and n.get_text_iface()))
    assert Atspi.EditableText.set_text_contents(command, 'print("accessibility command test")')
    assert command.get_component_iface().grab_focus()
    key('Return')
    pump(0.3)
    assert 'accessibility command test' in Atspi.Text.get_text(output, 0, -1)
    print('LUA COMMAND PASSED', flush=True)
    assert not any((word in (w / 'probe.log').read_text() for word in ('Unhandled Exception', 'Exception in Gtk#')))
    focus_window(main.get_name())
    assert file.get_action_iface().do_action(0)
    pump()
    assert child(file, 'Exit').get_action_iface().do_action(0)
    assert p.wait(timeout=10) == 0
    print('BIZHAWK MENU AND DIALOG CHECKS PASSED', flush=True)
