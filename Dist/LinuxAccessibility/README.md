# Linux WinForms accessibility bridge

BizHawk's Linux UI runs on Mono WinForms. Its built-in `AccessibleObject` notifications do not provide an AT-SPI bridge. This build bundles Mono UIA2ATK and GtkSharp, with source patches, to expose standard WinForms controls to Orca.

## Build

Run `Dist/BuildLinuxAccessibility.sh` from the BizHawk checkout before `Dist/BuildRelease.sh`. It requires Python 3, Mono development tools, a .NET SDK 8 or newer, a C compiler, curl, patch, pkg-config, and ATK / AT-SPI2 bridge development packages. It checks pinned source archive hashes before applying patches. GtkSharp generators target .NET 8 and support runtime roll-forward.

UIA2ATK is pinned to `5ddfe947cec456315d7d91421f21c31c4afa6765` and GtkSharp to `c01f5f97d0a0ac6367b1300519be682ca63de608`. The source projects' public strong-name keys preserve Mono assembly identities; these are unrelated to Git commit signing. Standard UIAutomation compatibility assemblies use the upstream delay-signing procedure.

## Changes to the bridge

- Use the current AT-SPI2 adaptor API instead of loading a GTK2 module.
- Run GLib dispatch and all provider access on the WinForms UI thread. A background thread waits on GLib's file descriptors and posts ready work to the UI context. There is no periodic control-tree scan.
- Service ready AT-SPI requests during the emulator's throttle waits, using a deadline and an event signalled by that worker. This is the Linux equivalent of the fork's Windows COM-aware wait. It does not pump WinForms input or reenter emulation. Queued UI callbacks skip already-serviced work.
- Marshal ATK value text as an output string and selected-row arrays as pointers, matching the native `gchar **` and `gint **` APIs.
- Map data rows independently of column headers and use zero-based ATK child indices. Forward cell name changes as well as value changes.
- Queue button and menu invocations through WinForms so modal dialogs cannot block AT-SPI dispatch.
- Preserve providers when a window changes owner and finalize child providers once, including synthetic cells, headers, and scrollbars.
- Keep combo item providers alive with their actual collection; collapsing a popup does not destroy its selected item's identity. Preserve control labels separately from selection values.
- Construct lazy accessible ancestry from the parent down and handle detached providers safely.
- Preserve text-box and slider accessible names.
- Restore bare-Alt menu activation on Mono 6.12. Its X11 prefilter clears the Alt state before constructing the key-release message. The bridge calls Mono's verified native menu handler only for an unmodified Alt tap; shortcuts, AltGr, and menu dismissal keep their normal handling.

## Source and replacement libraries

Linux distributions include `LinuxAccessibility/sources.tar.gz`, containing the corresponding modified UIA2ATK and GtkSharp source, generated bindings, and license notices. The `build` directory contains patches and build helpers. No system-wide GAC installation is required.

To rebuild from the included sources, extract the archive into a working directory, build `gtksharp/Source/Tools/Tools.sln` in Release, and run `python3 build/build-gtk.py /absolute/path/to/gtksharp`. Copy its `BuildOutput/Release/netstandard2.0/*.dll` into an empty destination directory, then run `python3 build/build-uia.py /absolute/path/to/uia2atk /absolute/path/to/destination`. Compile `uia2atk/UiaAtkBridge/bridge-glue/main.c` as a shared library using the `cc` command in `BuildLinuxAccessibility.sh`, and copy `build/UiaAtkBridge.dll.config` beside it. Replace the corresponding DLLs and `libbridge-glue.so` in the application's `dll` directory. Keep a backup of the installed libraries.

UIA2ATK is MIT licensed; GtkSharp is LGPL licensed. These dynamically loaded libraries can be replaced independently of BizHawk. The bridge exposes standard WinForms controls; custom painted tools need their own accessible controls or providers.
