"""Create a private test desktop. Run through dbus-run-session, never on the user's display."""
from contextlib import contextmanager
from pathlib import Path
import os
import shutil
import subprocess
import sys
import tempfile
import time

@contextmanager
def test_session(kind):
    package = Path(sys.argv[1]).resolve()
    sources = Path(__file__).parent
    children = []
    with tempfile.TemporaryDirectory(prefix='bizhawk-atspi-') as temp:
        work = Path(temp)
        with (work / 'probe.log').open('w') as log, (work / 'services.log').open('w') as services:
            try:
                xvfb = subprocess.Popen([os.environ.get('XVFB', 'Xvfb'), '-displayfd', '1', '-screen', '0', '800x600x24', '-nolisten', 'tcp'], stdout=subprocess.PIPE, stderr=services, text=True)
                children.append(xvfb)
                display = xvfb.stdout.readline().strip()
                if not display:
                    raise RuntimeError('Xvfb could not create a private display')
                os.environ['DISPLAY'] = ':' + display
                os.environ.pop('AT_SPI_BUS_ADDRESS', None)
                os.environ['MONO_UIA_BRIDGE'] = 'UiaAtkBridge, Version=1.0.0.0, Culture=neutral, PublicKeyToken=f4ceacb585d99812'
                os.environ['MONO_PATH'] = str(package / 'dll')
                os.environ['LD_LIBRARY_PATH'] = str(package / 'dll')
                os.environ['MONO_WINFORMS_XIM_STYLE'] = 'disabled'
                registry = subprocess.Popen([os.environ.get('ATSPI_REGISTRY', '/usr/lib/at-spi2-registryd')], stdout=services, stderr=subprocess.STDOUT)
                children.append(registry)
                if kind == 'controls':
                    exe = work / 'ControlsProbe.exe'
                    subprocess.run(['mcs', '-r:System.Windows.Forms', '-r:System.Drawing', '-out:' + str(exe), str(sources / 'ControlsProbe.cs')], check=True)
                    command = ['mono', str(exe)]
                else:
                    script = work / 'accessibility-fixture.lua'
                    shutil.copy2(sources / script.name, script)
                    command = ['mono', str(package / 'EmuHawk.exe'), '--gdi', '--config=' + str(work / 'config.ini'), '--lua=' + str(script)]
                app = subprocess.Popen(command, cwd=package, stdout=log, stderr=subprocess.STDOUT)
                children.append(app)
                time.sleep(0.5)
                yield (work, app)
                diagnostics = (work / 'probe.log').read_text()
                assert not any(marker in diagnostics for marker in ('[Error', 'Unhandled Exception', 'Exception in Gtk#', 'SIGSEGV')), diagnostics
            finally:
                for child in reversed(children):
                    if child.poll() is None:
                        child.terminate()
                        try:
                            child.wait(timeout=5)
                        except subprocess.TimeoutExpired:
                            child.kill()
                            child.wait()
                print((work / 'probe.log').read_text())
