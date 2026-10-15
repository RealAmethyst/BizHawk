from pathlib import Path
import shutil
import subprocess
import sys
source=Path(sys.argv[1]).resolve()
output=Path(sys.argv[2]).resolve()
output.mkdir(parents=True, exist_ok=True)
forms=source/'mcs/class/System.Windows.Forms'
common=source/'mcs/build/common'
(common/'Consts.cs').write_text((common/'Consts.cs.in').read_text().replace('@MONO_VERSION@','6.12.0').replace('@MONO_CORLIB_VERSION@','4.5'))
for name in ('keyboards','System.Windows.Forms'):
 shutil.copy2(forms/'resources'/(name+'.resources.prebuilt'),forms/'resources'/(name+'.resources'))
references='System System.Xml System.Drawing Accessibility System.Data Mono.Posix Mono.WebBrowser System.Configuration System.Runtime.Serialization.Formatters.Soap'.split()
args=['mcs','-target:library','-unsafe','-define:NET_4_5','-delaysign+','-keyfile:'+str(source/'mcs/class/ecma.pub'),'-out:'+str(output/'System.Windows.Forms.dll'),'@System.Windows.Forms.dll.resources','-nowarn:618,612,809']+['-r:'+r for r in references]+['@System.Windows.Forms.dll.sources']
r=subprocess.run(args,cwd=forms,text=True,stdout=subprocess.PIPE,stderr=subprocess.STDOUT)
(output/'build.log').write_text(r.stdout)
print(r.stdout[-5000:])
sys.exit(r.returncode)
