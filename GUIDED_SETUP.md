# Guided setup and Xbox controls

New empty libraries open a five-step setup assistant: storage summary, ROM
folders, live Xbox connection status, required emulators and BIOS, and readiness.
Setup can be deferred or reopened from the sidebar under **Verwaltung → Einrichtungsassistent**. Existing libraries
are not forced through setup again. Every completed import/download is saved;
cancelling or closing setup does not undo existing files or settings.

Only emulators needed for imported consoles are downloaded. Existing executable
paths are retained; RetroArch systems share the core package. BIOS files must
be provided by the user. Firmware checks use the emulator's configured location
when available. Completing the assistant does not claim that missing BIOS files
or ROMs have been supplied.

Xbox library controls: D-pad/stick selects a game, A starts it, Y toggles its
favorite, LB/RB changes console, Menu opens settings, View switches to control
navigation, B returns to the game list. When a launched emulator exits, the
library regains focus. Existing emulator-specific exit shortcuts remain active.

Dialogs: D-pad moves focus; left/right changes combo boxes and sliders; A
activates buttons, toggles checkboxes, expands sections or opens text entry.
LB/RB changes settings tabs. B goes back in the assistant or cancels a dialog.
Lists use up/down for entries and left/right to reach their surrounding controls.
File/folder pickers and the text keyboard run inside EmuVora, allowing controller
navigation without relying on Windows file dialogs. File selection supports
filters, drive selection and multi-selection. Mouse and keyboard remain usable.

Connection monitoring uses Windows XInput; four connected controllers are
supported. Reconnecting or changing windows ignores an already-held button
until it is released. Hardware-specific Bluetooth/adapter behavior and every
physical controller still require real-device testing.
