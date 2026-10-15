#!/bin/sh
set -eu
export PYTHONDONTWRITEBYTECODE=1
cd "$(dirname "$0")/.."
package="$(realpath "${1:-output}")"
# Each test owns a private runtime, display and D-Bus with service activation disabled.
# No ROM is loaded. Speech tests use a mock Orca service and emit no audio.
if [ -z "${ATSPI_REGISTRY:-}" ]; then
	for registry in /usr/lib/at-spi2-registryd /usr/libexec/at-spi2-registryd; do
		if [ -x "$registry" ]; then
			export ATSPI_REGISTRY="$registry"
			break
		fi
	done
fi
timeout 120 python3 Dist/LinuxAccessibility/tests/controls.py "$package"
timeout 120 python3 Dist/LinuxAccessibility/tests/property-grid.py "$package"
timeout 120 python3 Dist/LinuxAccessibility/tests/property-grid-editors.py "$package" NDSSettings
timeout 120 python3 Dist/LinuxAccessibility/tests/property-grid-editors.py "$package" NDSSyncSettings
timeout 120 python3 Dist/LinuxAccessibility/tests/bizhawk.py "$package"
timeout 60 python3 Dist/LinuxAccessibility/tests/speech.py "$package"
