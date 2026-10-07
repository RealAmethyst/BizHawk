#!/bin/sh
set -eu
cd "$(dirname "$0")/.."
pkg-config --exists atk atk-bridge-2.0 gmodule-2.0
work="$PWD/Dist/accessibility-build"
patches="$PWD/Dist/LinuxAccessibility"
mkdir -p "$work" "$work/dll"
fetch_source() {
	name="$1" revision="$2" checksum="$3" url="$4"
	archive="$work/$name-$revision.tar.gz"
	if [ ! -f "$archive" ]; then
		curl --fail --location --show-error "$url/$revision" -o "$archive"
	fi
	printf '%s  %s\n' "$checksum" "$archive" | sha256sum -c -
	# Re-extract before patching to avoid reusing stale or partially patched sources.
	rm -rf "$work/$name"
	mkdir "$work/$name"
	tar -xzf "$archive" --strip-components=1 -C "$work/$name"
	patch --batch --forward -d "$work/$name" -p1 < "$patches/$name.patch"
}
fetch_source uia2atk 5ddfe947cec456315d7d91421f21c31c4afa6765 \
	f838a4ef682615de9da116dfdd5e2e1ebfe8239c56c903d95c8ecc851e30e46c \
	https://codeload.github.com/mono/uia2atk/tar.gz
fetch_source gtksharp c01f5f97d0a0ac6367b1300519be682ca63de608 \
	52cd822d70502f955bf9dedcef491d003f2edda4ccf32421f050ab94fce9331e \
	https://codeload.github.com/GtkSharp/GtkSharp/tar.gz
cp "$patches/WinFormsMainLoop.cs" "$work/uia2atk/UiaAtkBridge/UiaAtkBridge/"
export DOTNET_ROLL_FORWARD=Major
# The generators target .NET 8; roll-forward also supports machines with newer SDKs.
dotnet build "$work/gtksharp/Source/Tools/Tools.sln" -c Release
python3 "$patches/build-gtk.py" "$work/gtksharp"
cp "$work/gtksharp/BuildOutput/Release/netstandard2.0/"*.dll "$work/dll/"
python3 "$patches/build-uia.py" "$work/uia2atk" "$work/dll"
# pkg-config output deliberately expands to the compiler's individual arguments.
cc -shared -fPIC -O2 "$work/uia2atk/UiaAtkBridge/bridge-glue/main.c" \
	-o "$work/dll/libbridge-glue.so" $(pkg-config --cflags --libs atk atk-bridge-2.0 gmodule-2.0)
cp "$patches/UiaAtkBridge.dll.config" "$work/dll/"
cp "$work/dll/"*.dll "$work/dll/"*.dll.config "$work/dll/libbridge-glue.so" Assets/dll/
mkdir -p Assets/LinuxAccessibility
cp "$work/uia2atk/COPYING" Assets/LinuxAccessibility/uia2atk-LICENSE
cp "$work/gtksharp/LICENSE" Assets/LinuxAccessibility/gtksharp-LICENSE
# Ship corresponding modified sources and build instructions with the LGPL libraries.
tar -czf Assets/LinuxAccessibility/sources.tar.gz \
	--exclude=bin --exclude=obj --exclude=BuildOutput --exclude='*.log' \
	-C "$work" uia2atk gtksharp
rm -rf Assets/LinuxAccessibility/build
mkdir -p Assets/LinuxAccessibility/build
cp -R "$patches/." Assets/LinuxAccessibility/build/
cp Dist/BuildLinuxAccessibility.sh Assets/LinuxAccessibility/
printf '%s\n' \
	'UIA2ATK source: https://github.com/mono/uia2atk/tree/5ddfe947cec456315d7d91421f21c31c4afa6765' \
	'GtkSharp source: https://github.com/GtkSharp/GtkSharp/tree/c01f5f97d0a0ac6367b1300519be682ca63de608' \
	'Modified source, including generated bindings, is in sources.tar.gz.' \
	'See build/README.md for rebuilding and replacing these libraries.' \
	> Assets/LinuxAccessibility/SOURCE.txt
