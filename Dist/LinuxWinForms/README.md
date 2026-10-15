# Linux WinForms wake notification fix

## Purpose

The Mono X11 driver writes wake notifications to a nonblocking pipe. When that pipe is full, UnixStream throws UnixIOException rather than SocketException. The supplied source patch accepts only EAGAIN/EWOULDBLOCK at that writer: the pipe already contains a pending wake, and the callback or timer was queued before the write. Other errors propagate. No callback, timer, or frame scheduling is replaced.

## Build

From a source checkout, run `Dist/BuildLinuxWinForms.sh`. It downloads the pinned upstream archive, verifies its SHA-256, builds the private library and stages the library and corresponding source package under Assets. The commands below rebuild from that generated Linux source package.

Requires Python 3, patch, and Mono 6.12 with mcs and its development assemblies, including Mono.Posix and Mono.WebBrowser. Verify the files using `sha256sum -c SHA256SUMS` before extraction. Run these commands from this directory into a new build directory:

```sh
mkdir work
cd work
tar -xzf ../sources.tar.gz
patch --batch --forward -d mono-source -p1 < ../mono-wakeup.patch
python3 ../build-winforms.py mono-source dll
```

The original source lists, resources, public signing key, license notices, and patent grant are included. The build uses the source project's ECMA public key and delay signing; it does not require a private signing key. Nine existing compiler warnings occur in the unmodified and modified builds.

## Application packaging

Install the resulting System.Windows.Forms.dll only in the Linux package's dll directory. The Linux launcher must prepend that directory to MONO_PATH so the corrected assembly is selected before the system GAC. Preserve any existing MONO_PATH suffix. Do not put this assembly into a Windows package, install it into the system GAC, or replace another application's runtime.

Include this entire LinuxWinForms directory with the Linux package. Exclude both the directory and the Mono assembly from Windows packaging. Actual loading should be checked with Assembly.Location in a standalone probe; successful compilation alone does not verify assembly selection.

## Verification scope

Standalone tests cover the original overflow, 150,000 ordered callbacks, timer creation with a full pipe, modal open/close, propagation of unrelated descriptor errors, full drain timing, AT-SPI queries, standard controls, melonDS settings-only editors and console output. No emulator/core/game or running screen reader is used in those tests. Gameplay and listening acceptance require a separate user test.

Run the standalone overflow regression explicitly from the source checkout with:

```sh
python3 Dist/LinuxAccessibility/tests/wake-pipe.py output
```

Use `EXPECT_OVERFLOW=1` with a baseline package to confirm the original failure. `XVFB` and `ATSPI_REGISTRY` can select test executables. The shared session helper creates private display, runtime, configuration and bus resources and does not start the emulator.
