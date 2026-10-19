# Öffentliche Repositories für EmuVora

Geprüft am 19. Oktober 2026. Stand: FluentAvaloniaUI 2.5.1 ist als gemeinsame Bedienelement-Basis in [Release 0.13.66](https://github.com/cpprograming/EmuVora/releases/tag/v0.13.66) integriert. Die weiteren unten genannten Erweiterungen sind noch nicht eingebaut.

| Priorität | Repository | Nutzen für EmuVora |
| --- | --- | --- |
| 1 | [FluentAvalonia](https://github.com/amwx/FluentAvalonia) | Einheitliche WinUI-inspirierte Avalonia-Navigation, Einstellungen und Dialoge. Vier Farbschemata, TV-Modus, Dialogübernahme und Controller-Raster sind automatisiert geprüft. Echte TV-/DPI-Tests bleiben offen. MIT-Lizenzhinweise erhalten. |
| 2 | [libretro-database](https://github.com/libretro/libretro-database) | ROM-Prüfsummen zu einheitlichen Anzeigenamen und Metadaten zuordnen, damit Cover besser gefunden werden. Eigene Korrekturen bewahren. Ausgewählte BIOS-Signaturen nutzen wir bereits. Herkunft und CC-BY-SA-4.0-Hinweise berücksichtigen; keine BIOS-/ROM-Dateien beziehen. |
| 3 | [SDL](https://github.com/libsdl-org/SDL) + [SDL_GameControllerDB](https://github.com/mdqinc/SDL_GameControllerDB) | Mehr Controller als mit reinem XInput unterstützen. Benötigt einen SDL-Backendadapter und passende Windows-Runtime; die Mappingdatei allein genügt nicht. Doppelte XInput-/SDL-Geräte vermeiden und Xbox-Verhalten praktisch testen. |
| 4 | [Playnite](https://github.com/JosefNemec/Playnite) | Referenz für Sammlungen und Desktop-/Vollbild-Bedienung. Eigenständiges Frontend, kein direkt einsetzbares EmuVora-Plugin. Konzepte vergleichen und unsere Oberfläche eigenständig umsetzen. |
| 5 | [slang-shaders](https://github.com/libretro/slang-shaders) | Optionale RetroArch-Bildprofile, etwa CRT-Darstellung. Über die externe RetroArch-Konfiguration integrieren, nicht als Avalonia-Filter. Ausgewählte Presets, deren Hinweise und GPU-Leistung einzeln prüfen. |

Die Priorität ist unsere Einschätzung für die bestehende Architektur. Als Erstes Oberfläche/TV-Modus aus Roadmap 0.13, danach Metadaten aus 0.14. SDL ist eine eigenständige Erweiterung der Eingabeunterstützung. Echte Controller-, Monitor- und Spieltests bleiben erforderlich.

