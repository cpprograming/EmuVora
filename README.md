# EmuVora

Windows-Spielbibliothek für RetroArch, DuckStation, PCSX2, Dolphin und PPSSPP. EmuVora verwaltet deine eigenen ROMs und startet die offiziellen externen Emulatoren.

## Windows-Version starten

Unter [GitHub Actions](https://github.com/cpprograming/EmuVora/actions) den neuesten erfolgreichen **Build and test** öffnen und **EmuVora-win-x64** herunterladen. ZIP vollständig entpacken und `EmuVora.App.exe` starten. .NET muss für dieses portable Paket nicht installiert werden. Optional richtet `Install-EmuVora.ps1` das Programm und eine Startmenü-Verknüpfung im Benutzerkonto ein.

1. **ROM-Ordner importieren**. Unterordner werden mitgescannt; nicht lesbare Ordner werden gemeldet.
2. Unklare Discs unter **Konsole zuordnen** bearbeiten. Wii/GameCube-ISO-Header und Konsolenordner helfen bei der Erkennung; andere Disc-Formate werden nicht als sicher erkannt ausgegeben.
3. **Emulatoren einrichten**: vorhandene EXE auswählen und speichern oder das offizielle Windows-Paket herunterladen. Für RetroArch zusätzlich das stabile Core-Paket herunterladen.
4. Emulator öffnen und eigene BIOS-Dateien, Grafik und Controller einrichten. Bei fehlenden VCRUNTIME/MSVCP-DLLs die verlinkte Microsoft Visual C++ Runtime installieren.
5. Spiel auswählen und **Spielen**, doppelklicken oder Eingabetaste drücken.

## Bibliothek und Bedienung

- Cover-Kacheln, kombinierte Suche/Konsolenfilter, Sortierung, Favoriten und zuletzt gespielte Titel.
- Eigene Cover kopieren oder nach einem exakten No-Intro-/Redump-Titel bei libretro-thumbnails suchen. Nur beim Klick auf die Online-Suche werden Titel und Konsole an GitHub gesendet; fehlende Treffer lassen das Platzhalter-Cover bestehen.
- Titel und Konsole korrigieren; erneutes Scannen bewahrt Korrekturen, Favoriten und Startverlauf.
- Mehrdisc-M3U-Dateien bündeln referenzierte Discs. ZIP-Dateien mit genau einer unterstützten Cartridge-ROM können über RetroArch gestartet werden.
- Fehlende Dateien anzeigen; Entfernen betrifft nur den Bibliothekseintrag, nicht die ROM-Datei.
- JSON-Sicherung/Wiederherstellung für Titel, Dateipfade, Konsole, Favoriten und Startverlauf. Cover, Einstellungen und Emulator-Speicherstände sind nicht in dieser JSON enthalten.
- Tastatur: Eingabetaste startet; Strg+F aktiviert die Suche. XInput-Controller: Steuerkreuz navigiert, A startet, Y schaltet Favoriten um. Controller-Einstellungen innerhalb von Spielen gehören zum Emulator.

## Downloads und Updates

Offizielle portable Windows-x64-Pakete werden über einen versionierten Katalog bezogen und vor dem Entpacken mit SHA-256 geprüft. GitHub-Release-Pakete werden zusätzlich gegen aktuelle offizielle Release-Metadaten aufgelöst. ZIP/7z werden in neue Installationsordner entpackt; Pfadtraversal und Links werden abgewiesen. Bei Fehlern oder Abbruch bleibt die bisherige Installation erhalten.

Beim Start können verfügbare Updates gemeldet werden. **Updates automatisch installieren** ist optional und zunächst ausgeschaltet. Verwaltete Emulatoren übernehmen bekannte portable Konfigurations-/Speicherordner in die neue Installation. Extern gespeicherte Emulator-Einstellungen bleiben beim Emulator. Vorige Installationen bleiben vorhanden und können wieder aktiviert werden. Eigene Emulatorpfade werden nicht automatisch aktualisiert.

`catalog/packages.json` enthält offizielle Quellen und gepinnte SHA-256-Prüfsummen. Der GitHub-Workflow **Refresh official emulator catalog** aktualisiert diesen Katalog wöchentlich oder manuell. Ein Prüfsummenfehler blockiert die Installation; er wird nicht durch Weglassen der Prüfung umgangen. SHA-256 bestätigt die Übereinstimmung mit dem Katalog; es ist keine unabhängige Signatur des Emulator-Herstellers.

## Daten und Sicherung

EmuVora speichert Daten in `%LOCALAPPDATA%\EmuVora`: SQLite-Bibliothek, `settings.json`, Cover, verwaltete Emulatoren und Fehlerprotokoll. **Datenordner öffnen** befindet sich in den Einstellungen. Für eine vollständige Sicherung das Programm und Emulatoren schließen und den gesamten Datenordner kopieren. BIOS, Spiele und extern gespeicherte Emulator-Spielstände zusätzlich sichern. Einstellungen werden atomar geschrieben und die vorige Datei als `.bak` behalten.

## Entwicklung und Prüfung

Windows 10/11 x64 und .NET 10 SDK:

```powershell
dotnet restore EmuVora.sln
dotnet build EmuVora.sln -c Release
dotnet test tests/EmuVora.Tests/EmuVora.Tests.csproj -c Release --filter "Category!=Network"
dotnet run --project src/EmuVora.App/EmuVora.App.csproj
```

Netzwerktests laden und prüfen alle echten offiziellen Archive. Sie starten keine Spiele:

```powershell
$env:EMUVORA_PACKAGE_CATALOG = (Resolve-Path catalog/packages.json).Path
dotnet test tests/EmuVora.Tests/EmuVora.Tests.csproj -c Release --filter "Category=Network"
```

Tests prüfen Migration der ursprünglichen Datenbank, Erhalt manuell korrigierter Metadaten, Playlist-/Disc-/ZIP-Erkennung, Bibliothekswiederherstellung, Einstellungen, Startargumente, Prüfsummen und Archivpfade. Avalonia-Tests prüfen echte Such-/Filter-/Favoriten-Interaktion und rendern die vier Themes. GitHub Actions erstellt zusätzliche **EmuVora-UI-previews**.

Die Emulatoren bleiben eigene Anwendungen: Kompatibilität einzelner Spiele, BIOS, Grafik-/Audio-Treiber, Speicherstände und Controller-Mapping werden dort konfiguriert. EmuVora enthält weder Emulator-Binaries noch ROMs oder BIOS im Programmpaket.

## Lizenz

MIT für den ursprünglichen EmuVora-Code. Siehe [THIRD_PARTY_NOTICES.md](THIRD_PARTY_NOTICES.md) für Abhängigkeiten und externe Quellen. Emulatoren und Cover behalten die Rechte und Lizenzen ihrer jeweiligen Projekte bzw. Rechteinhaber.
