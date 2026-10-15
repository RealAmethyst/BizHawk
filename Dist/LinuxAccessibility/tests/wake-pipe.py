import os
import time
from session import test_session
with test_session('wake-pipe') as (work, app):
    import gi
    gi.require_version('Atspi', '2.0')
    from gi.repository import Atspi
    from events import pump
    deadline = time.monotonic() + 60
    while 'PROBE COMPLETE' not in (work / 'probe.log').read_text():
        assert app.poll() is None
        assert time.monotonic() < deadline
        pump(0.05)
    log = (work / 'probe.log').read_text()
    if os.environ.get('EXPECT_OVERFLOW') == '1':
        assert 'CAUGHT frame=' in log and 'UnixIOException: Resource temporarily unavailable [EWOULDBLOCK]' in log
        assert 'PASS all 150000' not in log
        print('PASS reproduced original pipe overflow', flush=True)
    else:
        assert 'CAUGHT' not in log
        for marker in ('PASS all 150000 callbacks in order', 'PASS other errors propagate: EBADF',
                       'PASS timer after full pipe', 'PASS modal open and close'):
            assert marker in log, marker
        pump(0.3)
        def descendants(node):
            yield node
            for i in range(node.get_child_count()):
                yield from descendants(node.get_child_at_index(i))
        result = next(n for n in descendants(Atspi.get_desktop(0)) if n.get_name() == 'Wake pipe result')
        assert Atspi.Text.get_text(result,0,-1) == '150000 callbacks completed in order'
        print('PASS AT-SPI query after saturated pipe and modal lifecycle', flush=True)
