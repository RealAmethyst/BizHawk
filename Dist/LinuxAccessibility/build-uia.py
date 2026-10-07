from pathlib import Path
import subprocess

import sys
r = Path(sys.argv[1]).resolve()
w = Path(sys.argv[2]).resolve()
w.mkdir(parents=True, exist_ok=True)
common = r / 'UIAutomation/build/common'
(w / 'Consts.cs').write_bytes((common / 'Consts.cs.in').read_bytes())
steps = [
    ('UIAutomationHelpers', 'UIAutomation/UIAutomationHelpers', []),
    ('UIAutomationTypes', 'UIAutomation/UIAutomationTypes', []),
    ('UIAutomationBridge', 'UIAutomation/UIAutomationBridge', ['UIAutomationTypes', 'UIAutomationHelpers']),
    ('UIAutomationProvider', 'UIAutomation/UIAutomationProvider', ['UIAutomationTypes', 'UIAutomationBridge', 'UIAutomationHelpers']),
    ('UIAutomationSource', 'UIAutomation/UIAutomationSource', ['UIAutomationTypes', 'UIAutomationHelpers']),
    ('UIAutomationClient', 'UIAutomation/UIAutomationClient', ['UIAutomationTypes', 'UIAutomationBridge', 'UIAutomationProvider', 'UIAutomationHelpers', 'UIAutomationSource', 'GdkSharp', 'GLibSharp', 'CairoSharp', 'GioSharp', 'PangoSharp']),
    ('UIAutomationWinforms', 'UIAutomationWinforms/UIAutomationWinforms', ['UIAutomationTypes', 'UIAutomationBridge', 'UIAutomationProvider', 'UIAutomationHelpers', 'UIAutomationClient', 'AtkSharp', 'GLibSharp']),
    ('UiaAtkBridge', 'UiaAtkBridge/UiaAtkBridge', ['UIAutomationTypes', 'UIAutomationBridge', 'UIAutomationProvider', 'UIAutomationHelpers', 'UIAutomationClient', 'AtkSharp', 'GLibSharp']),
]
for name, folder, refs in steps:
    src = r / folder
    files = [str(p) for p in src.rglob('*.cs') if not any(part in ('Test', 'Tests', 'obj', 'bin') for part in p.relative_to(src).parts)]
    info = src / 'AssemblyInfo.cs.in'
    if info.exists():
        generated = w / (name + 'AssemblyInfo.cs')
        generated.write_bytes(info.read_bytes())
        files += [str(generated), str(w / 'Consts.cs')] + [str(p) for p in common.glob('*.cs')]
    if name == 'UIAutomationWinforms':
        generated = w / 'Globals.cs'
        generated.write_text((src / 'Globals.cs.in').read_text().replace('@GETTEXT_PACKAGE@', 'uiautomationwinforms').replace('@prefix@', str(w)))
        files.append(str(generated))
    args = ['mcs', '-target:library', '-define:NET_2_0', '-unsafe', '-out:' + str(w / (name + '.dll')), '-r:System', '-r:System.Core', '-r:System.Drawing', '-r:System.Windows.Forms', '-r:System.Data', '-r:WindowsBase', '-r:Mono.Posix', '-r:Mono.WebBrowser', '-r:/usr/lib/mono/4.7.2-api/Facades/netstandard.dll']
    if info.exists():
        args += ['-delaysign+', '-keyfile:' + str(r / 'UIAutomation/winfx3.pub')]
    else:
        args += ['-keyfile:' + str(r / 'mono-uia.snk')]
    args += ['-r:' + str(w / (ref + '.dll')) for ref in refs] + files
    result = subprocess.run(args, text=True, capture_output=True)
    (w / (name + '.log')).write_text(result.stdout + result.stderr)
    print(name, result.returncode)
    if result.returncode:
        print((result.stdout + result.stderr)[-3500:])
        raise SystemExit(result.returncode)
