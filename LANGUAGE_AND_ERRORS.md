# Deutsch und Startdialoge

Unter **Einstellungen → Emulatoren** ist **Emulatoren und unterstützte Spiele automatisch auf Deutsch einstellen** standardmäßig aktiv. **Deutsch jetzt anwenden** stellt bereits installierte, verwaltete Emulatoren um; sonst geschieht dies beim nächsten EmuVora-Start und vor dem Spielstart. Laufende Emulatoren werden dabei ausgelassen. Selbst eingebundene externe Installationen erhalten keine ungefragten portablen Konfigurationsdateien.

| Emulator | Oberfläche | Spielsprache |
| --- | --- | --- |
| RetroArch | Deutsch (`user_language=4`) | Deutsch wird den Cores gemeldet; Flycast und DeSmuME erhalten zusätzlich ihre deutsche Sprachoption. |
| DuckStation | Deutsch (`Main/Language=de`) | Im Spiel auswählen, falls vorhanden. |
| PCSX2 | Deutsch (`UI/Language=de-DE`) | Im Spiel oder im eigenen PS2-BIOS-Menü auswählen, falls erforderlich. |
| Dolphin | Deutsch (`Interface/LanguageCode=de`) | GameCube: Deutsch als Konsolensprache; Wii: temporäre Sprachvorgabe beim Spielstart. |
| PPSSPP | Deutsch (`General/Language=de_DE`) | Deutsch als PSP-Systemsprache (`SystemParam/GameLanguage=4`). |

Die passende deutsche Übersetzung muss im offiziellen Emulatorpaket enthalten sein. Die Sprachvorgabe übersetzt keine Spiele und ersetzt keine fehlende deutsche Spielversion. Manche Spiele ignorieren die Konsolensprache oder speichern ihre Wahl selbst. Regionsvarianten werden nicht automatisch ausgetauscht. BIOS, NVRAM, Wii-NAND und Spielstände werden nicht von EmuVora gepatcht. Die Wii-Vorgabe wird über Dolphins dokumentierte Startoption übergeben.

Die Umstellung verändert gezielt Sprachschlüssel. Andere Einstellungen bleiben erhalten. Vorhandene Konfigurationsdateien erhalten vor der ersten Umstellung eine `.before-german.bak`-Kopie. Abschalten der Option beendet die automatische Vorgabe; bereits geschriebene Sprachwerte werden nicht zurückgesetzt und können danach im Emulator geändert werden.

## Sichtbare Fehler

Fehlt beim Start eines verwalteten Systems die erforderliche Firmware, erscheint **BIOS erforderlich** mit Erklärung, **BIOS-Dateien importieren …**, Emulator-Einstellungen und Schließen. Das Spiel startet dann nicht. Nach dem Import erneut starten; der Import allein startet kein Spiel.

Andere Fehler außerhalb bereits geöffneter Aktions-/Einstellungsdialoge erscheinen in einem eigenen Fehlerfenster mit lesbarer Zusammenfassung, ausklappbaren technischen Details und Zugang zu den Einstellungen. Wiederholte Fehler aktualisieren das vorhandene Fenster. In bereits offenen Dialogen bleibt die Fehlermeldung im gemeinsamen, fest angeordneten Fehlerbereich sichtbar. Bewusster Abbruch ist kein Fehler; automatische Updateprüfungen erzeugen weiterhin keine störenden Fehlerfenster. Emulatorabstürze zeigen Name, hexadezimalen Code und bei PCSX2 den vorhandenen Protokollpfad. Es erfolgt kein automatischer Upload von Protokollen.

## Geprüfte Originalschnittstellen

- [RetroArch-Sprachkennungen](https://github.com/libretro/RetroArch/blob/master/libretro-common/include/libretro.h), [Flycast-Coreoptionen](https://github.com/flyinghead/flycast/blob/master/shell/libretro/libretro_core_options.h), [DeSmuME-Coreoptionen](https://github.com/libretro/desmume/blob/master/desmume/src/frontend/libretro/libretro_core_options.h).
- [DuckStation-Sprachauswahl](https://github.com/stenzek/duckstation/blob/master/src/duckstation-qt/qthost.cpp) und [Konfigurationsabschnitt](https://github.com/stenzek/duckstation/blob/master/src/core/settings.cpp).
- [PCSX2-Sprachauswahl](https://github.com/PCSX2/pcsx2/blob/master/pcsx2-qt/Settings/InterfaceSettingsWidget.cpp) und [Sprachcodes](https://github.com/PCSX2/pcsx2/blob/master/pcsx2-qt/Translations.cpp).
- [Dolphin-Konfiguration](https://github.com/dolphin-emu/dolphin/blob/master/Source/Core/Core/Config/MainSettings.cpp), [Konsolensprachen](https://github.com/dolphin-emu/dolphin/blob/master/Source/Core/DiscIO/Enums.h), [Wii-Sprachschlüssel](https://github.com/dolphin-emu/dolphin/blob/master/Source/Core/Core/Config/SYSCONFSettings.cpp) und [Startoptionen](https://github.com/dolphin-emu/dolphin/blob/master/Source/Core/UICommon/CommandLineParse.cpp).
- [PPSSPP-Konfiguration](https://github.com/hrydgard/ppsspp/blob/master/Core/Config.cpp) und [PSP-Sprachkennungen](https://github.com/hrydgard/ppsspp/blob/master/Core/HLE/sceUtility.h).
