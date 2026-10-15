#!/bin/sh
set -eu
cd "$(dirname "$0")/.."
work="$PWD/Dist/winforms-build"
patches="$PWD/Dist/LinuxWinForms"
revision=0cbf0e290c31adb476f9de0fa44b1d8829affa40
checksum=d35e720999e3b3274654386b8d9afdb6ff56e2db9c95ebfe40be3c2e7f5257c9
archive="$work/mono-$revision.tar.gz"
mkdir -p "$work" "$work/dll"
if [ ! -f "$archive" ]; then
	curl --fail --location --show-error "https://codeload.github.com/mono/mono/tar.gz/$revision" -o "$archive"
fi
printf '%s  %s\n' "$checksum" "$archive" | sha256sum -c -
# Extract only the assembly's source list, resources, shared inputs and notices.
# Recreate this generated tree so a prior patch/build cannot affect the result.
rm -rf "$work/mono-source"
mkdir "$work/mono-source"
sed "s|^|mono-$revision/|" "$patches/source-files.txt" > "$work/source-files.prefixed"
tar -xzf "$archive" --strip-components=1 -C "$work/mono-source" --files-from "$work/source-files.prefixed"
# Keep the unchanged sources and the local patch separate for straightforward rebuilds.
tar -czf "$work/sources.tar.gz" -C "$work" mono-source
patch --batch --forward -d "$work/mono-source" -p1 < "$patches/mono-wakeup.patch"
python3 "$patches/build-winforms.py" "$work/mono-source" "$work/dll"
mkdir -p Assets/dll Assets/LinuxWinForms
cp "$work/dll/System.Windows.Forms.dll" Assets/dll/
cp "$work/mono-source/LICENSE" "$work/mono-source/PATENTS.TXT" "$work/sources.tar.gz" Assets/LinuxWinForms/
cp "$patches/README.md" "$patches/source-files.txt" "$patches/mono-wakeup.patch" "$patches/build-winforms.py" Assets/LinuxWinForms/
cp Dist/BuildLinuxWinForms.sh Assets/LinuxWinForms/
printf '%s\n' \
	"Mono System.Windows.Forms source: https://github.com/mono/mono/tree/$revision" \
	"Upstream archive SHA-256: $checksum" \
	'Source archive contains the unchanged files listed in source-files.txt from that revision.' \
	'Local modification: mono-wakeup.patch handles an already pending wake on a full nonblocking pipe.' \
	'Use only in the Linux application package; no system runtime installation is changed.' \
	'See README.md for rebuilding the supplied source subset.' \
	> Assets/LinuxWinForms/SOURCE.txt
(cd Assets/LinuxWinForms && sha256sum LICENSE PATENTS.TXT SOURCE.txt README.md source-files.txt sources.tar.gz mono-wakeup.patch build-winforms.py BuildLinuxWinForms.sh > SHA256SUMS)
