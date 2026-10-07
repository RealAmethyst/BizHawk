#!/bin/sh
set -eu
cd "$(dirname "$0")/.."

# Keep the C ABI and both Linux backends reproducible. Do not silently build
# without Orca or Speech Dispatcher when a development package is missing.
pkg-config --exists 'glibmm-2.68 >= 2.68.0' 'giomm-2.68 >= 2.68.0' speech-dispatcher
revision=a08ea340dc4063ce48ac4b6834571dcfb2d50d7c
archive_sha256=b9898912904cab967fc7046988025d2271ee5da6e745d2d841497f2b1243669b
work="$PWD/Dist/prism-build"
mkdir -p "$work"
archive="$work/prism-$revision.tar.gz"
if [ ! -f "$archive" ]; then
	curl --fail --location --show-error "https://github.com/ethindp/prism/archive/$revision.tar.gz" -o "$archive"
fi
printf '%s  %s\n' "$archive_sha256" "$archive" | sha256sum -c -
if [ ! -d "$work/prism-$revision" ]; then
	tar -xzf "$archive" -C "$work"
fi
cmake -S "$work/prism-$revision" -B "$work/build" -G Ninja \
	-DCMAKE_BUILD_TYPE=Release -DPRISM_ENABLE_GDEXTENSION=OFF \
	-DPRISM_ENABLE_TESTS=OFF -DPRISM_ENABLE_DEMOS=OFF
cmake --build "$work/build" --parallel "${PRISM_BUILD_JOBS:-2}"
install -m644 "$work/build/libprism.so" Assets/dll/libprism.so
mkdir -p Assets/Prism
cp "$work/prism-$revision/LICENSE" "$work/prism-$revision/NOTICE" Assets/Prism/
cp -R "$work/prism-$revision/LICENSES" Assets/Prism/
printf 'Prism source: https://github.com/ethindp/prism/tree/%s\nArchive SHA-256: %s\n' \
	"$revision" "$archive_sha256" > Assets/Prism/SOURCE.txt
