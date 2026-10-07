from pathlib import Path
import subprocess,shutil
import sys
r=Path(sys.argv[1]).resolve()
libs=r/'Source/Libs'; tools=r/'BuildOutput/Tools'
steps={'GLibSharp':[], 'GioSharp':['GLibSharp'],'AtkSharp':['GLibSharp'],'CairoSharp':[],'PangoSharp':['GLibSharp','CairoSharp'],'GdkSharp':['GLibSharp','GioSharp','CairoSharp','PangoSharp']}
with open(r/'gtk-build.log','w') as log:
 def run(args): subprocess.run(args,stdout=log,stderr=subprocess.STDOUT,check=True)
 for name,deps in steps.items():
  d=libs/name; gen=d/'Generated';gen.mkdir(exist_ok=True);api=gen/(name+'-api.xml');shutil.copyfile(d/(name+'-api.xml'),api)
  meta=d/(name+'.metadata')
  if meta.exists():
   args=['dotnet',str(tools/'GapiFixup.dll'),'--metadata='+str(meta),'--api='+str(api)]
   sym=d/(name+'-symbols.xml')
   if sym.exists():args+=['--symbols='+str(sym)]
   run(args)
   args=['dotnet',str(tools/'GapiCodegen.dll'),'--outdir='+str(gen),'--schema='+str(libs/'Shared/Gapi.xsd'),'--assembly-name='+name,'--generate='+str(api)]
   args+=['--include='+str(libs/dep/'Generated'/(dep+'-api.xml')) for dep in deps]
   if name=='AtkSharp':args+=['--abi-cs-usings=Atk,GLib']
   run(args)
 for name in steps:
  run(['dotnet','build',str(libs/name/(name+'.csproj')),'-c','Release','-p:TargetFrameworks=netstandard2.0','-p:Version=3.24.24.95'])
  print(name,'built',flush=True)
