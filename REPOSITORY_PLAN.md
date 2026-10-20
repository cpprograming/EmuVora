# Öffentliche Repositories für EmuVora

Geprüft am 20. Oktober 2026. Stand: FluentAvaloniaUI 2.5.1 ist als gemeinsame Bedienelement-Basis in [Release 0.13.66](https://github.com/cpprograming/EmuVora/releases/tag/v0.13.66) integriert. Die Prüfsummen-Spielerkennung aus libretro-database ist für 16 Cartridge-Systeme integriert. SDL 3.4.18 und GameControllerDB sind ab 0.14.76 für die Oberfläche veröffentlicht, einschließlich XInput-Rückfall. Weitere automatische Spielprofile und zusätzliche Shader sind offen.

| Priorität | Repository | Nutzen für EmuVora |
| --- | --- | --- |
| 1 | [FluentAvalonia](https://github.com/amwx/FluentAvalonia) | Einheitliche WinUI-inspirierte Avalonia-Navigation, Einstellungen und Dialoge. Vier Farbschemata, TV-Modus, Dialogübernahme und Controller-Raster sind automatisiert geprüft. Echte TV-/DPI-Tests bleiben offen. MIT-Lizenzhinweise erhalten. |
| 2 | [libretro-database](https://github.com/libretro/libretro-database) | Für 16 Cartridge-Systeme: lokale ROM-Prüfsummen zu Bibliothekstiteln zuordnen, mit Vorschau und Schutz eigener Korrekturen. [Anleitung](ROM_RECOGNITION.md). Weitere Metadaten wie Genre/Jahr sind noch offen. Ausgewählte BIOS-Signaturen nutzen wir bereits. Herkunft und CC-BY-SA-4.0-Hinweise berücksichtigen; keine BIOS-/ROM-Dateien beziehen. |
| 3 | [SDL](https://github.com/libsdl-org/SDL) + [SDL_GameControllerDB](https://github.com/mdqinc/SDL_GameControllerDB) | SDL-Backend und geprüfte x64-Runtime integriert; feste Mappingdatei wird mitgeliefert. SDL ist die einzige aktive Quelle, XInput bleibt Rückfall. Die native Runtime ist mit virtuellem Gamepad geprüft. Weitere Spielprofile und echte Hardwaretests bleiben offen. |
| 4 | [Playnite](https://github.com/JosefNemec/Playnite) | Referenz für Sammlungen und Desktop-/Vollbild-Bedienung. Eigenständiges Frontend, kein direkt einsetzbares EmuVora-Plugin. Konzepte vergleichen und unsere Oberfläche eigenständig umsetzen. |
| 5 | [slang-shaders](https://github.com/libretro/slang-shaders) | Optionale RetroArch-Bildprofile, etwa CRT-Darstellung. Über die externe RetroArch-Konfiguration integrieren, nicht als Avalonia-Filter. Ausgewählte Presets, deren Hinweise und GPU-Leistung einzeln prüfen. |

Die Priorität ist unsere Einschätzung für die bestehende Architektur. Oberfläche/TV-Modus, Prüfsummenmetadaten und SDL-Eingabe sind integriert. Weitere automatische Spielprofile sowie echte Controller-, Monitor- und Spieltests bleiben die nächsten Schritte.


