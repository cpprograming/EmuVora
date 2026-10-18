# EmuVora

EmuVora ist eine Windows-Spielbibliothek für deine eigenen ROMs und vorhandenen Spiele. Sie verwaltet **47 Systeme** und startet RetroArch, DuckStation, PCSX2, Dolphin oder PPSSPP als eigenständige Anwendungen.

**Aktueller geprüfter Stand: [0.11.61](https://github.com/cpprograming/EmuVora/releases/tag/v0.11.61)** · Entwicklung für den privaten Gebrauch, ohne Pro-Version oder Bezahlsystem.

## Download und Installation

- **[Windows-Setup herunterladen](https://github.com/cpprograming/EmuVora/releases/latest/download/EmuVora-Setup.exe)** – Installationslaufwerk und Ordner wählen; eigene Desktop- und Startmenü-Verknüpfungen sind optional.
- **[Portable Windows-Version herunterladen](https://github.com/cpprograming/EmuVora/releases/latest/download/EmuVora-win-x64.zip)** – ZIP vollständig entpacken und `EmuVora.App.exe` starten.
- Alle Versionen und Änderungen findest du unter **[Releases](https://github.com/cpprograming/EmuVora/releases)**.

Die Pakete sind für Windows 10/11 x64 und enthalten die benötigte .NET-Laufzeit. Für die normale Nutzung ist kein .NET SDK nötig. Emulatoren und ihre zusätzlichen Laufzeitabhängigkeiten werden gesondert eingerichtet.

## BIOS-Import verbessert in 0.11.59

Die Dateiauswahl unterstützt Doppelklick und einen stets sichtbaren Übernehmen-Knopf. Bekannte BIOS-Dateien werden offline anhand von Prüfsummen erkannt und beim Kopieren automatisch passend benannt. Originale bleiben erhalten; eine Ergebnisübersicht zeigt Zielnamen und Fehler. [Anleitung und Grenzen](BIOS_IMPORT.md).

## Erste Einrichtung

1. **Speicherort festlegen.** Das Setup legt Programm und Daten standardmäßig in den Unterordnern `App` und `Data` des gewählten Installationsordners ab. Bei einem portablen Erststart ohne gespeicherten Datenpfad wählst du den Datenordner selbst.
2. **Einrichtungsassistent durchlaufen.** Er führt durch Speicherort, ROM-Import, Controller, Emulatoren/BIOS und Abschluss. Er lässt sich später über die Seitenleiste erneut öffnen.
3. **Eigene ROM-Ordner importieren.** Unterordner werden durchsucht. Unklare Disc-Dateien kannst du anschließend einer Konsole zuordnen.
4. **Xbox-Controller anschließen und eigene BIOS-Dateien importieren**, soweit das System sie benötigt.
5. **Spiel auswählen und starten.** Bei aktivierter automatischer Einrichtung werden fehlende Emulatoren und RetroArch-Cores heruntergeladen. Vorhandene eigene Emulatorpfade bleiben nutzbar.

Die Firmware-Prüfung erkennt Dateinamen und grundlegende Größen. Tatsächliche Gültigkeit und Region prüft der jeweilige Emulator. EmuVora liefert keine ROMs oder BIOS-Dateien.

Details: [Einrichtungsassistent](GUIDED_SETUP.md) · [Speicherorte und Themes](STORAGE_AND_THEMES.md) · [BIOS-Import und automatische Benennung](BIOS_IMPORT.md).

## Bibliothek, Cover und Bildqualität

- Cover-Kacheln, Suche, Konsolenfilter, Sortierung, Favoriten und Startverlauf.
- Fehlende Cover automatisch beim Start und nach einem Import laden; Sammeldownload und manuelle Auswahl sind ebenfalls möglich.
- Regionale Titelvarianten werden berücksichtigt. Vorhandene eigene Cover bleiben erhalten; nicht jeder Titel hat einen passenden Treffer.
- Titel und Konsolenzuordnung bearbeiten. Erneutes Scannen bewahrt eigene Korrekturen, Favoriten und Verlauf.
- Mehrdisc-M3U-Dateien bündeln referenzierte Discs. Cartridge-ZIPs mit genau einer unterstützten ROM können über RetroArch gestartet werden.
- Monitorerkennung, Profile für das ursprüngliche Seitenverhältnis und scharfe Skalierung sowie einstellbare interne Auflösung für verwaltete Emulatoren.
- Entfernen aus der Bibliothek löscht keine ROM-Datei.

**Online-Cover:** Automatischer und manueller Abruf verwenden libretro-thumbnails auf GitHub. Dabei werden Titel/Konsolenzuordnung für die Suche verwendet. Den automatischen Abruf kannst du unter **Einstellungen → Bibliothek** abschalten.

Details: [Bibliothek und Bildqualität](LIBRARY_AND_DISPLAY.md).

## Xbox-Controller

Die Oberfläche unterstützt Xbox-/XInput-Navigation einschließlich Einstellungen, Dialogen, Dateiauswahl und Bildschirmtastatur.

| Eingabe | In der Spielebibliothek |
| --- | --- |
| Steuerkreuz / linker Stick | Spiel auswählen |
| A | Spiel starten |
| Y | Favorit umschalten |
| LB / RB | Konsolenfilter wechseln |
| Menu | Einstellungen öffnen |
| View | Zwischen Spieleliste und Bedienelement-Navigation umschalten |
| B | Zur Spieleliste zurück; laufenden Vorgang abbrechen |

In Dialogen wählt das Steuerkreuz Bedienelemente, A bestätigt und B geht zurück bzw. schließt. Beim Einrichtungsassistenten führt B zum vorherigen Schritt. LB/RB wechselt Einstellungen-Tabs.

Verwaltete Emulatoren erhalten Xbox-Standardprofile; PPSSPP verwendet seine eingebauten Voreinstellungen. Eigene geänderte Belegungen bleiben erhalten. **Xbox-Standardprofil erneut anwenden** setzt die verwalteten Standardwerte mit Dateisicherung zurück.

Die Bedienung einzelner Spiele hängt vom System ab: Nintendo DS kann Maus/Touch benötigen, Wii-Spiele besondere Bewegungsprofile und Heimcomputer, DOS oder Intellivision zusätzliche Tastatureingaben. Tests mit echten Controllern und mehreren Spielern stehen weiterhin auf der [Checkliste](CHECKLIST.md).

## Bibliotheken wechseln und Sicherungserinnerung – neu in 0.11

Unter **Einstellungen → Bibliothek → Bibliothek wechseln** kannst du eine vorhandene Bibliothek prüfen und nach Bestätigung mit einem Neustart öffnen. **Zur vorherigen Bibliothek zurück** führt wieder zum unmittelbar vorherigen Datenordner. Spiele und Spielstände werden dabei nicht verschoben; nicht erreichbare oder ungültige Ordner werden abgewiesen.

Unter **Einstellungen → Sicherung** lassen sich Erinnerungen beim Programmstart auf 7, 14 oder 30 Tage stellen. Sie sind standardmäßig ausgeschaltet. Der Hinweis **Datensicherung fällig** öffnet die Sicherungsseite.

Details: [Datenordner wechseln](DATA_LOCATIONS.md).

## Sicherung und Wiederherstellung

**Neu in 0.11.61:** Importierte BIOS-/Firmware-Dateien auf Wunsch im persönlichen Sicherungsarchiv speichern; Anzahl und Größe werden vor der Wiederherstellung angezeigt.

Unter **Einstellungen → Sicherung** kannst du eine Datensicherung erstellen oder wiederherstellen.

Enthalten sind Einstellungen, eine konsistente Kopie der Spielebibliothek, Cover im Datenordner sowie erkannte Spielstände. Für verwaltete DuckStation-, PCSX2-, Dolphin- und PPSSPP-Installationen werden bekannte Speicherordner berücksichtigt.

### Zusätzliche Spielstandsordner – neu in 0.10

1. **Zusätzliche Spielstandsordner → Spielstandsordner hinzufügen** öffnen.
2. Die gewünschten Speicherordner auswählen; höchstens 32, ohne doppelte oder überlappende Auswahl.
3. Das Sicherungsarchiv außerhalb des Datenordners und aller zusätzlichen Quellordner speichern.

Alle Dateien in zusätzlich ausgewählten Ordnern werden kopiert. Wähle deshalb gezielt Spielstandsordner aus. Ein nicht erreichbarer Ordner stoppt die Sicherung mit einer Meldung. Entfernen aus der Auswahl löscht keine Dateien.

### Wiederherstellen

- Vor dem Wiederherstellen werden Datum, Dateianzahl, Größe und zusätzliche Ordner angezeigt.
- Jede Datei wird mit SHA-256 geprüft; anschließend wird die Bibliotheksdatenbank geprüft.
- Die Wiederherstellung erstellt einen **neuen Datenordner**. Die aktive Bibliothek und ursprünglichen externen Spielstandsdateien bleiben erhalten.
- Zusätzliche Spielstände liegen danach unter `external-saves`. Die **Übersicht zusätzlicher Spielstände** zeigt die alten und neuen Pfade; im Emulator musst du den neuen Speicherort bei Bedarf selbst einstellen.
- Erkannte Spielstände verwalteter Emulatoren werden bei der nächsten automatischen Emulator-Einrichtung übernommen. Vorhandene Zieldateien werden nicht ersetzt; die Quelle bleibt erhalten.
- Erst mit **Mit wiederhergestellter Bibliothek neu starten** wechselst du ausdrücklich zur neuen Bibliothek.

**Kompatibilität:** Ab 0.10 werden neue Sicherungen in Format 2 geschrieben. Ältere Format-1-Sicherungen bleiben lesbar; EmuVora 0.9 kann Format 2 nicht wiederherstellen. Grenzen: 100.000 Dateien und 20 GiB unkomprimierte Daten; Dateisystem-Verknüpfungen werden abgewiesen.

Importierte BIOS-/Firmware-Dateien können unter Einstellungen → Sicherung ausdrücklich eingeschlossen werden (standardmäßig aus). Dabei wird nur der firmware-Unterordner dieses Datenordners kopiert; externe BIOS-Pfade bleiben ausgenommen. Emulatorprogramme, Themes und ROM-Verzeichnisse gehören nicht zur automatischen Datensicherung. Zusätzlich ausgewählte Ordner werden dagegen ohne Inhaltsfilter kopiert. Emulatoren, Cores, Themes und BIOS müssen nach einer Wiederherstellung gegebenenfalls erneut eingerichtet werden. ROM-Pfade bleiben unverändert.

Der bisherige JSON-Export unter **Verwaltung** tauscht nur Bibliothekseinträge aus und enthält keine Cover- oder Spielstandsdateien.

Vollständige Anleitung: [Sicherung und Wiederherstellung](BACKUPS.md).

## Themes und ES-DE

Klassische EmulationStation-Themes lassen sich innerhalb der unterstützten Darstellungsgrenzen importieren. Für **AlekFull NX mit der originalen ES-DE-Oberfläche** gibt es einen optionalen separaten ES-DE-Start mit Bibliotheksabgleich.

Nicht jedes ES-DE-Theme lässt sich direkt in der EmuVora-Oberfläche darstellen. Kompatibilitätsgrenzen und Einrichtung:

- [Speicherorte und Theme-Import](STORAGE_AND_THEMES.md)
- [AlekFull NX / ES-DE](ALEKFULL_ESDE.md)

Theme-Dateien und ihre Lizenzhinweise bleiben den jeweiligen Autoren zugeordnet.

## Unterstützte Systeme (47 Einträge)

- **Nintendo:** NES, SNES, GB, GBC, GBA, N64, Nintendo DS, Virtual Boy, GameCube, Wii.
- **Sega:** Mega Drive, Master System, Game Gear, Mega-CD, 32X, Saturn, Dreamcast.
- **Sony:** PS1, PS2, PSP.
- **Atari:** 2600, 5200, 7800, Lynx, Jaguar, ST.
- **NEC:** PC Engine, PC Engine CD, SuperGrafx, PC-FX.
- **SNK:** Neo Geo, Neo Geo CD, Neo Geo Pocket, Neo Geo Pocket Color.
- **Weitere:** 3DO, ColecoVision, Intellivision, WonderSwan, WonderSwan Color.
- **Computer:** Commodore 64, Amiga, Amstrad CPC, ZX Spectrum, MSX, DOS.
- **Arcade:** FinalBurn Neo und MAME.

Gemeinsame Formate wie CHD/CUE/ISO, DSK, ROM oder vollständige ZIP-ROM-Sets am besten in passend benannte Systemordner legen, etwa `Saturn`, `MSX`, `DOS`, `MAME` oder `FBNeo`. Arcade-ROM-Sets müssen zur Core-Version passen. Dreamcast nutzt ohne eigenes BIOS Flycast HLE. MSX benötigt die eigenen blueMSX-Verzeichnisse `Machines` und `Databases`.

Die Liste beschreibt die Systemzuordnung im Frontend; Spielkompatibilität und besondere Eingabegeräte hängen vom jeweiligen Emulator ab.

## Updates und Datenablage

**EmuVora aktualisieren:** Unter **Einstellungen → App-Updates** nach einer neuen stabilen Version suchen, herunterladen und installieren. Der Download wird mit SHA-256 geprüft. Das Setup wartet auf das Programmende, behält den Datenpfad und sichert die vorige App-Version.

**Emulatoren aktualisieren:** Offizielle portable Pakete werden vor dem Entpacken geprüft und in neue Installationsordner entpackt. Automatisches Installieren von Emulator-Updates ist optional und zunächst ausgeschaltet. Frühere Emulator-Versionen bleiben für die Rückkehr erhalten; eigene externe Emulatorpfade werden nicht automatisch aktualisiert.

**GitHub:** Nach einem erfolgreichen Merge-Build auf `main` werden Setup-EXE und portables ZIP automatisch als Release veröffentlicht. Fehlgeschlagene Prüfungen blockieren die Veröffentlichung.

Die Daten liegen im gewählten Datenordner. **Datenordner öffnen** findest du in den Einstellungen. `%LOCALAPPDATA%\EmuVora\location.json` dient als Pfadverweis; installierte Pakete können einen eigenen Verweis neben der App verwenden. Einstellungen werden atomar geschrieben und die vorige Datei als `.bak` erhalten.

Details: [App-Updates](APP_UPDATES.md) · [Speicherorte](STORAGE_AND_THEMES.md).

## Entwicklung und Prüfung

Benötigt: Windows x64 und **.NET 10 SDK**.

```powershell
dotnet restore EmuVora.sln
dotnet build EmuVora.sln -c Release

dotnet publish tests/EmuVora.LaunchProbe/EmuVora.LaunchProbe.csproj -c Release -r win-x64 --self-contained false -o work/launch-probe
$env:EMUVORA_LAUNCH_PROBE = (Resolve-Path work/launch-probe/EmuVora.LaunchProbe.exe).Path
dotnet test tests/EmuVora.Tests/EmuVora.Tests.csproj --no-build -c Release --filter "Category!=Network"

dotnet run --project src/EmuVora.App/EmuVora.App.csproj
```

Netzwerktests laden und prüfen die offiziellen Archive; sie starten keine Spiele:

```powershell
$env:EMUVORA_PACKAGE_CATALOG = (Resolve-Path catalog/packages.json).Path
# Optional im CI: EMUVORA_COVER_TOKEN für authentifizierte GitHub-API-Abfragen.
dotnet test tests/EmuVora.Tests/EmuVora.Tests.csproj -c Release --filter "Category=Network"
```

**Für Release 0.11.61 bestanden:** 123 Funktionstests und 5 Netzwerktests sowie die Installer-Prüfungen für Installation, Update, Prozessübergabe und Datenerhalt. Der veröffentlichte Setup-Download wurde zusätzlich mit dem EmuVora-Updater geprüft.

Die Tests decken unter anderem Bibliothek, Controller-Oberfläche, Sicherungsintegrität, externe Ordner, Wiederherstellung älterer Archive und Dateipfade ab. Avalonia-Tests erzeugen UI-Aufnahmen; die GitHub-Artefakte enthalten **EmuVora-UI-previews**. Echte Hardwaretests stehen separat auf der Checkliste.

## Nächste Schritte

Die [Checkliste](CHECKLIST.md) hält erledigte Funktionen und offene Arbeiten fest. Die [Roadmap](ROADMAP.md) beschreibt die Etappen bis 1.0. Als Nächstes stehen Xbox-Hardwaretests und Verbesserungen der Controller-Bedienung an; danach folgen TV-Darstellung und Bibliothekskomfort.

## Lizenz

Der EmuVora-Code steht unter [MIT](LICENSE). Abhängigkeiten, Emulatoren, Themes und Cover unterliegen ihren eigenen Bedingungen; siehe [Drittanbieter-Hinweise](THIRD_PARTY_NOTICES.md).

Emulator-Binaries, ROMs, BIOS und Theme-Pakete sind nicht im EmuVora-Programmpaket enthalten. Optionale Komponenten werden gesondert von ihren offiziellen Quellen geladen.


