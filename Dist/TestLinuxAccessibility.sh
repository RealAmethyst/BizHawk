#!/bin/sh
set -eu
export PYTHONDONTWRITEBYTECODE=1
cd "$(dirname "$0")/.."
package="$(realpath "${1:-output}")"
# Private D-Bus sessions prevent the tests from contacting the user's Orca.
# No ROM is loaded. Speech tests use a mock Orca service and emit no audio.
if [ -z "${ATSPI_REGISTRY:-}" ]; then
	for registry in /usr/lib/at-spi2-registryd /usr/libexec/at-spi2-registryd; do
		if [ -x "$registry" ]; then
			export ATSPI_REGISTRY="$registry"
			break
		fi
	done
fi
timeout 120 dbus-run-session -- python3 Dist/LinuxAccessibility/tests/controls.py "$package"
timeout 120 dbus-run-session -- python3 Dist/LinuxAccessibility/tests/bizhawk.py "$package"
timeout 60 dbus-run-session -- python3 Dist/LinuxAccessibility/tests/speech.py "$package"
