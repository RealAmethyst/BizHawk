# Accessible BizHawk fork on Linux

This fork includes Lua speech through Prism and a bundled WinForms-to-AT-SPI bridge for Orca. Start Orca, then run `./EmuHawkMono.sh`. Use `./EmuHawkMono.sh --luaconsole` to open the Lua Console at startup. On a Wayland desktop, Mono's UI uses XWayland.

## Runtime dependencies

A 64-bit Linux system needs Mono, X11 or XWayland, OpenAL, Lua 5.4, GTK3, AT-SPI2, GLibmm 2.68, and Speech Dispatcher. The launch script uses the bridge shipped in `dll`; no system-wide Mono accessibility packages or GAC changes are required. The .NET SDK is needed to build, not to run the packaged application.

For Arch and CachyOS, runtime packages are:

```sh
sudo pacman -S --needed mono libgdiplus openal lua54 gtk3 at-spi2-core glibmm-2.68 speech-dispatcher lsb-release
```

The Linux build was built and checked on CachyOS x86-64 with Mono 6.12. Amethyst tested it with Orca on October 7, 2026 and confirmed that responsiveness improved and the UI speaks much as it does on Windows. Other distributions and the updated Windows build have not received manual testing of these changes yet.

## Accessibility behavior

Use Alt and the arrow keys to navigate menus, Enter to activate, Escape to leave, and Tab or Shift+Tab to move among controls. The Linux message loop uses normal WinForms keyboard processing. Modal dialogs suspend emulator iterations until you close them.

Standard controls expose names, roles, states, text, values, selection, and focus events through AT-SPI. The Lua Console uses a native list with Script, Status, and Path columns; it retains its existing script actions, sorting, reordering, and multiple selection. Its command and output fields are named. The bridge does not add speech calls for menus; Orca controls their presentation.

Frame-throttle waits service accessibility requests on the UI thread, as the Windows build does for COM requests. The first Linux test build slept without servicing them: 100 uncached accessibility queries took about 3.3 seconds. The corrected build took about 3 milliseconds in the same isolated, ROM-free test. The regression check rejects a total above one second; these measurements do not substitute for testing with Orca and a running game.

Some other custom-painted tools, such as TAS input grids, still lack accessible rows. This work is not a claim that every advanced BizHawk tool is accessible. Amethyst's general accessibility approval does not establish coverage of every controller, core, or advanced tool.

## Lua speech

Existing scripts use the same `speech.say`, `speech.output`, `speech.braille`, and `speech.stop` API. Prism tries Orca before Speech Dispatcher. Linux speech calls run on a dedicated worker while the UI thread continues answering AT-SPI queries. This prevents a circular wait when Orca needs information from BizHawk before replying to a speech request. Calls retain their order and finish before Lua continues; no emulator frames or keyboard input are dispatched by this wait.

Native speech errors are reported in the Lua Console instead of an empty failure message.

Amethyst confirmed the Orca speech fix and Lua Console output navigation on October 7, 2026. The bridge correctly converts Mono's text-formatting values when Orca queries the log; these queries previously crashed the emulator.

## Safe defaults

Hotkey defaults match the `config.ini` in RealAmethyst/BizHawk's Windows release `v1.1`. The 58 shortcuts cleared in that release are cleared in source. Both a new config and Restore Defaults use this map. Explicit user bindings remain unchanged when existing configurations load.

`Dist/Package.sh` refuses to package either platform if the compiled defaults differ from the approved release fixture. Newly added upstream hotkeys require review and an explicit fixture update. Controller mappings already matched that release and were retained. DS JIT defaults to enabled, also matching the release.

The Windows release archive used for comparison has SHA-256 `f17a5ec6192b5c568858f4a75fd4b22627e229b80d8f3fa3adbf2206ddf11e22`. Its `prism.dll` has SHA-256 `25e90d2e3a1aba6d54072ba4079eac209bf752f2a6e267be446b584416efb648`. The fixture includes only hotkey mappings, not the user's complete configuration.

## Building and checking

Install a .NET SDK 8 or newer, Mono development tools, CMake, Ninja, a C++23-capable compiler, Python 3, curl, patch, pkg-config, and development packages for GLibmm 2.68, Speech Dispatcher, ATK and the AT-SPI2 bridge.

```sh
Dist/BuildPrism.sh
Dist/BuildLinuxAccessibility.sh
Dist/BuildRelease.sh
mono test_output/BizHawk.Tests.Client.Common.exe --filter FullyQualifiedName~config --minimum-expected-tests 8
Dist/TestLinuxAccessibility.sh
Dist/Package.sh
```

The AT-SPI checks need Xvfb, Python GObject bindings for AT-SPI2, libXtst, and dbus-run-session. They create isolated displays and D-Bus sessions, use temporary configuration files, and load no ROM. They verify controls, keyboard menu navigation, file dialogs, Lua script selection and status, command input and output, and clean exit. A mock Orca service queries BizHawk through AT-SPI during initialization, speech, and stop requests, checking responsiveness, message order, and error reporting without emitting audio. `XVFB` and `ATSPI_REGISTRY` can point to nonstandard executable locations.

Prism and accessibility dependencies use pinned source revisions and verified archive hashes. See `Dist/BuildPrism.sh` and `Dist/LinuxAccessibility/README.md`. Linux packages include the corresponding modified accessibility sources and licenses; the DLLs are replaceable. The Windows package omits the Linux bridge and retains the Windows Prism DLL.

Automated checks complement Amethyst's successful Orca test; they cannot substitute for testing additional screen readers, distributions, or game configurations.
