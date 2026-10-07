import ctypes, time
from gi.repository import GLib, Atspi

def pump(seconds=0.15):
    end = time.monotonic() + seconds
    while time.monotonic() < end:
        while GLib.MainContext.default().pending():
            GLib.MainContext.default().iteration(False)
        time.sleep(0.005)

def key(*names):
    x11 = ctypes.CDLL('libX11.so.6')
    xt = ctypes.CDLL('libXtst.so.6')
    x11.XOpenDisplay.restype = ctypes.c_void_p
    x11.XOpenDisplay.argtypes = [ctypes.c_char_p]
    x11.XStringToKeysym.restype = ctypes.c_ulong
    x11.XStringToKeysym.argtypes = [ctypes.c_char_p]
    x11.XKeysymToKeycode.argtypes = [ctypes.c_void_p, ctypes.c_ulong]
    xt.XTestFakeKeyEvent.argtypes = [ctypes.c_void_p, ctypes.c_uint, ctypes.c_int, ctypes.c_ulong]
    x11.XFlush.argtypes = [ctypes.c_void_p]
    x11.XCloseDisplay.argtypes = [ctypes.c_void_p]
    d = x11.XOpenDisplay(None)
    assert d
    keys = [x11.XKeysymToKeycode(d, x11.XStringToKeysym(name.encode())) for name in names]
    assert keys and all(keys)
    for k in keys:
        xt.XTestFakeKeyEvent(d, k, 1, 0)
        x11.XFlush(d)
        pump(0.08)
    for k in reversed(keys):
        xt.XTestFakeKeyEvent(d, k, 0, 0)
        x11.XFlush(d)
        pump(0.08)
    x11.XFlush(d)
    x11.XCloseDisplay(d)
    pump()

def focus_window(title):
    x = ctypes.CDLL('libX11.so.6')
    P = ctypes.c_void_p
    U = ctypes.c_ulong
    x.XOpenDisplay.argtypes = [ctypes.c_char_p]
    x.XOpenDisplay.restype = P
    x.XDefaultRootWindow.argtypes = [P]
    x.XDefaultRootWindow.restype = U
    x.XQueryTree.argtypes = [P, U, ctypes.POINTER(U), ctypes.POINTER(U), ctypes.POINTER(ctypes.POINTER(U)), ctypes.POINTER(ctypes.c_uint)]
    x.XFetchName.argtypes = [P, U, ctypes.POINTER(ctypes.c_char_p)]
    x.XFree.argtypes = [P]
    x.XSetInputFocus.argtypes = [P, U, ctypes.c_int, U]
    x.XFlush.argtypes = [P]
    x.XCloseDisplay.argtypes = [P]
    d = x.XOpenDisplay(None)
    assert d

    def find(win):
        name = ctypes.c_char_p()
        x.XFetchName(d, win, ctypes.byref(name))
        match = name.value and name.value.decode(errors='replace') == title
        if name:
            x.XFree(name)
        if match:
            return win
        root = U()
        parent = U()
        kids = ctypes.POINTER(U)()
        count = ctypes.c_uint()
        if not x.XQueryTree(d, win, ctypes.byref(root), ctypes.byref(parent), ctypes.byref(kids), ctypes.byref(count)):
            return None
        items = [kids[i] for i in range(count.value)]
        if kids:
            x.XFree(kids)
        for child in items:
            result = find(child)
            if result:
                return result
    win = find(x.XDefaultRootWindow(d))
    assert win, title
    # Xvfb has no window manager. Supply its active-window property so Mono
    # receives the same activation notification as on a real desktop.
    x.XInternAtom.argtypes = [P, ctypes.c_char_p, ctypes.c_int]
    x.XInternAtom.restype = U
    x.XChangeProperty.argtypes = [P, U, U, U, ctypes.c_int, ctypes.c_int, P, ctypes.c_int]
    atom = x.XInternAtom(d, b'_NET_ACTIVE_WINDOW', 0)
    active = U(win)
    x.XChangeProperty(d, x.XDefaultRootWindow(d), atom, 33, 32, 0, ctypes.byref(active), 1)
    x.XSetInputFocus(d, win, 2, 0)
    x.XFlush(d)
    x.XCloseDisplay(d)
    pump()
