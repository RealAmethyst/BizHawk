"""Create an isolated display, runtime, configuration and bus without service activation."""
from contextlib import contextmanager
from pathlib import Path
import os
import shutil
import subprocess
import sys
import tempfile
import time

@contextmanager
def test_session(kind, fixture='accessibility-fixture.lua'):
    package = Path(sys.argv[1]).resolve()
    sources = Path(__file__).parent
    children = []
    original_environment = dict(os.environ)
    with tempfile.TemporaryDirectory(prefix='bizhawk-atspi-') as temp:
        work = Path(temp)
        with (work / 'probe.log').open('w') as log, (work / 'services.log').open('w') as services:
            try:
                # Do not inherit desktop sockets or permit service activation. A
                # private session bus alone can still launch the desktop a11y bus.
                for name in ('DISPLAY', 'WAYLAND_DISPLAY', 'XAUTHORITY', 'AT_SPI_BUS_ADDRESS',
                             'DBUS_SESSION_BUS_ADDRESS', 'DBUS_STARTER_ADDRESS', 'DBUS_STARTER_BUS_TYPE'):
                    os.environ.pop(name, None)
                for name in ('XDG_RUNTIME_DIR', 'XDG_CONFIG_HOME', 'XDG_CACHE_HOME', 'XDG_DATA_HOME', 'XDG_STATE_HOME'):
                    directory = work / name.lower()
                    directory.mkdir(mode=0o700)
                    os.environ[name] = str(directory)
                os.environ['XDG_CONFIG_DIRS'] = os.environ['XDG_CONFIG_HOME']
                os.environ['XDG_DATA_DIRS'] = os.environ['XDG_DATA_HOME']
                os.environ['DBUS_SYSTEM_BUS_ADDRESS'] = 'unix:path=' + str(work / 'no-system-bus')
                os.environ['PIPEWIRE_REMOTE'] = str(work / 'no-pipewire')
                os.environ['PULSE_SERVER'] = 'unix:' + str(work / 'no-pulse')
                os.environ['SPEECHD_ADDRESS'] = 'unix_socket:' + str(work / 'no-speech')
                bus_config = work / 'bus.conf'
                bus_config.write_text('<busconfig><type>session</type><listen>unix:path=' +
                                      str(work / 'bus') + '</listen><auth>EXTERNAL</auth>' +
                                      '<policy context="default"><allow send_destination="*"/>' +
                                      '<allow receive_sender="*"/><allow own="*"/></policy></busconfig>')
                bus = subprocess.Popen(['dbus-daemon', '--nofork', '--print-address=1', '--config-file=' + str(bus_config)],
                                       stdout=subprocess.PIPE, stderr=services, text=True)
                children.append(bus)
                address = bus.stdout.readline().strip()
                if not address.startswith('unix:path=' + str(work / 'bus')):
                    raise RuntimeError('D-Bus did not create the private test socket')
                os.environ['DBUS_SESSION_BUS_ADDRESS'] = address
                os.environ['AT_SPI_BUS_ADDRESS'] = address
                xvfb = subprocess.Popen([os.environ.get('XVFB', 'Xvfb'), '-displayfd', '1', '-screen', '0', '800x600x24', '-nolisten', 'tcp'], stdout=subprocess.PIPE, stderr=services, text=True)
                children.append(xvfb)
                display = xvfb.stdout.readline().strip()
                if not display:
                    raise RuntimeError('Xvfb could not create a private display')
                os.environ['DISPLAY'] = ':' + display
                os.environ['MONO_UIA_BRIDGE'] = 'UiaAtkBridge, Version=1.0.0.0, Culture=neutral, PublicKeyToken=f4ceacb585d99812'
                os.environ['MONO_PATH'] = str(package / 'dll')
                os.environ['LD_LIBRARY_PATH'] = str(package / 'dll')
                os.environ['MONO_WINFORMS_XIM_STYLE'] = 'disabled'
                status = subprocess.Popen([sys.executable, str(sources / 'a11y-status.py')], stdout=subprocess.PIPE, stderr=services, text=True)
                children.append(status)
                if status.stdout.readline().strip() != 'READY':
                    raise RuntimeError('Private accessibility status service did not start')
                registry = subprocess.Popen([os.environ.get('ATSPI_REGISTRY', '/usr/lib/at-spi2-registryd')], stdout=services, stderr=subprocess.STDOUT)
                children.append(registry)
                if kind in ('controls', 'property-grid'):
                    probe = {'controls': 'ControlsProbe', 'property-grid': 'PropertyGridProbe'}[kind]
                    exe = work / (probe + '.exe')
                    subprocess.run(['mcs', '-r:System.Windows.Forms', '-r:System.Drawing', '-out:' + str(exe), str(sources / (probe + '.cs'))], check=True)
                    command = ['mono', str(exe)]
                else:
                    script = work / 'accessibility-fixture.lua'
                    shutil.copy2(sources / fixture, script)
                    command = ['mono', str(package / 'EmuHawk.exe'), '--gdi', '--config=' + str(work / 'config.ini'), '--lua=' + str(script)]
                app = subprocess.Popen(command, cwd=package, stdout=log, stderr=subprocess.STDOUT,
                                       env={**os.environ, 'BIZHAWK_TEST_DIRECTORY': str(work)})
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
                os.environ.clear()
                os.environ.update(original_environment)
                print((work / 'probe.log').read_text())
                print((work / 'services.log').read_text())
