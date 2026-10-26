# EmuVora – Checkliste für die nächsten Schritte

Stand: 26. Oktober 2026. Ziel: ein zuverlässiges Windows-Frontend für den privaten Gebrauch, ohne Pro-Version oder Bezahlsystem.

## Aktueller Stand

- [x] ES-DE-Anwendungsintegration in 0.14.128 entfernt ([PR #42](https://github.com/cpprograming/EmuVora/pull/42)): Start, Export/Abgleich, Downloads und Paketfreigaben entfallen. Cover-Kennungen/Konsolenlogos unabhängig; alte Einstellungen und Startverweise kontrolliert behandelt. Neue Sicherungen erhalten referenzierte alte Cover und aktive lokale Themes. 344 Funktions- und 6 Netzwerktests sowie Setup-/Update-Prüfungen bestanden; veröffentlichte EXE per Selbstupdate einschließlich SHA-256 geprüft und installiert. 5.805 Einträge und 5.611 vorhandene Cover erhalten. Der Theme-Import bleibt erhalten; Layout-Engine folgt gemäß [GUI-Roadmap](GUI_ROADMAP.md).

- [x] Bibliotheksdialoge in 0.14.126 veröffentlicht ([PR #41](https://github.com/cpprograming/EmuVora/pull/41)): feste Aktionsleisten in Duplikatübersicht, Inhaltsvergleich, Zusammenführung, Playlist-Vorschau und ROM-Pfadzuordnung; lange Vorschauen bleiben separat scrollbar. Schmale/helle/dunkle/TV-Ansichten und Abbruch ohne Datenänderung automatisiert geprüft. 344 Funktions- und 7 Netzwerktests sowie Setup-/Update-Prüfungen bestanden; veröffentlichte EXE per Selbstupdate einschließlich SHA-256 geprüft und installiert. Bibliothek und Cover erhalten; echte Controllerabnahme bleibt offen. [Bedienung](LIBRARY_AND_DISPLAY.md#bibliotheksdialoge-bedienen).

- [x] GDI und Mega-CD in 0.14.124 veröffentlicht ([PR #40](https://github.com/cpprograming/EmuVora/pull/40)): Dreamcast-GDI mit Trackprüfung, nummerierten benachbarten Disc-Ordnern, Scan-/Umzugsprüfung und Mega-CD-Playlists für CHD/CUE. Originaldateien und Disc-Metadaten erhalten; 341 Funktions- und 7 Netzwerktests sowie Setup-/Update-Prüfungen bestanden. Echte Core-Erweiterungen und Vorschauen geprüft; Disc-Wechsel im Spiel bleibt offen. [Anleitung](LIBRARY_AND_DISPLAY.md).

- [x] Bedienhinweise in 0.14.122 veröffentlicht ([PR #39](https://github.com/cpprograming/EmuVora/pull/39)): Leitfaden für alle 47 Systeme, Hinweise zu möglichen Zusatzeingaben bei zehn Systemen, Sichtbarkeit vor dem Spielen auch ohne Details/mit TV-Modus sowie Integration in die Systemübersicht. 323 Funktions- und 7 Netzwerktests sowie Setup-/Update-Prüfungen bestanden. Keine Veränderung von Belegungen oder Bibliotheksdaten; echte Hardwaretests bleiben offen. [Anleitung](INPUT_GUIDE.md).

- [x] Systemzentrale in 0.14.120 veröffentlicht ([PR #38](https://github.com/cpprograming/EmuVora/pull/38)): Suche und Zustandsprüfung für importierte/alle Systeme, bestätigte Sammel-Einrichtung fehlender Programme/Cores, BIOS-/Einstellungsaktionen und direkte Spiele-/Coverfilter. Feste Aktionsleiste für helle/dunkle, schmale und TV-Layouts; eigene funktionierende Pfade erhalten. 318 Funktions- und 7 Netzwerktests sowie Setup-/Update-Prüfungen bestanden. Mit 5.805 Einträgen lesend geprüft. [Anleitung](SYSTEM_CENTER.md).

- [x] Diagnosebericht in 0.14.118 veröffentlicht ([PR #37](https://github.com/cpprograming/EmuVora/pull/37)): schreibgeschützte Vorschau, freiwilliger lokaler Export, allgemeine Bibliotheks-/Emulator-/Core-/Firmware-/Bildprüfung ohne private Namen/Pfade oder Protokolltexte und ohne Upload. 310 Funktions- und 7 Netzwerktests sowie Setup-/Update-Prüfungen bestanden. Mit 5.805 Einträgen lesend geprüft. [Anleitung](DIAGNOSTICS.md).

- [x] Deutsch und Startdialoge in 0.14.116 veröffentlicht ([PR #35](https://github.com/cpprograming/EmuVora/pull/35), [PR #36](https://github.com/cpprograming/EmuVora/pull/36)): deutsche Oberflächen für alle fünf verwalteten Emulatoren und unterstützte Spielsprachoptionen, abschaltbare Vorgabe mit Konfigurationssicherung, direkter BIOS-Importdialog und sichtbare Fehlerfenster. 306 Funktions- und 7 Netzwerktests sowie Setup-/Update-Prüfungen bestanden. [Grenzen und Anleitung](LANGUAGE_AND_ERRORS.md).

- [x] Native Oberfläche und Themes in 0.14.112 veröffentlicht ([PR #34](https://github.com/cpprograming/EmuVora/pull/34)): gleichmäßige Konsolenzeilen und Titelkarten, auswählbare Versionen statt doppelter Karten, native Aurora-/Studio-Stile und automatische ES-DE-Anpassung mit Bericht. Separater ES-DE-Start entfernt. PS2-BIOS-Auswahl strukturell geprüft; betroffener Spielstart bestätigt. 296 Funktions- und 7 Netzwerktests sowie Setup-/Update-Prüfungen bestanden.

- [x] ROM-Ordnerzuordnung in 0.14.94 veröffentlicht ([PR #27](https://github.com/cpprograming/EmuVora/pull/27)): fehlende Dateien am gleichen relativen Zielpfad vorschlagen, vor Übernahme prüfen, Konflikte und geänderte Ziele abweisen. Titel, Favoriten, Verlauf, Importdatum, Cover/Sperren, Bildprofile und Sammlungen erhalten. Metadaten vorher sichern und unterbrochene Übernahmen konsistent wieder aufnehmen. 20 neue Prüffälle; 223 Funktions- und 6 Netzwerktests sowie Setup-/Update-Prüfungen bestanden.

- [x] Eigene Sammlungen und neue Importe in 0.14.92 veröffentlicht ([PR #26](https://github.com/cpprograming/EmuVora/pull/26)): mehrere Sammlungen pro Spiel, Umbenennen/Löschen ohne Spielverlust, gemeinsame Suche/Konsolenfilter, Controller-/TV-Bedienung und vollständige Sicherung. Importdatum neuer Einträge ohne erfundene Daten für alte Spiele; erneute Scans bewahren den ersten Zeitpunkt. Acht neue Prüffälle; 203 Funktions- und 6 Netzwerktests sowie Setup-/Update-Prüfungen bestanden.
- [x] Freiwilliges GitHub Sponsoring mit FUNDING.yml, Profilentwurf und Anleitung vorbereitet; öffentliche Downloads bleiben kostenlos. [SPONSORS.md](SPONSORS.md).
- [x] Sponsors-Antrag durch den Kontoinhaber eingereicht; Dashboard zeigt ausstehende zusätzliche Prüfung.
- [ ] GitHub-Freigabe erhalten und aktiven öffentlichen Sponsor-Button anschließend prüfen.

- [x] Cover-Verwaltung in 0.14.90 veröffentlicht ([PR #25](https://github.com/cpprograming/EmuVora/pull/25)): fehlende Bilder gesammelt mit Filtern anzeigen; lokale Bilder und Online-Treffer erst nach Vorschau übernehmen; abweichenden Suchnamen ohne Titeländerung nutzen. Eigene Bilder separat speichern und vor Sammelsuche sowie ES-DE schützen. Cover-Aktionen über Xbox X auch im TV-Modus erreichbar. Elf neue Prüffälle; 195 Funktions- und 6 Netzwerktests sowie Setup-/Update-Prüfungen bestanden.

- [x] Cover-Zuordnung in 0.14.86 präzisiert ([PR #24](https://github.com/cpprograming/EmuVora/pull/24)): echte Untertitel und bedeutungstragende Jahreszahlen bewahren, bekannte plattformgebundene Titelvarianten berücksichtigen und GB/GBC-Katalog anhand validierter ROM-Header bestimmen. Keine Änderung an gespeicherten Titeln, Konsolen oder ROMs. Acht zusätzliche Prüffälle; 184 Funktions- und 6 Netzwerktests sowie Installer-/Update-Prüfungen bestanden.

- [x] Konsolenlogos statt ausgeschriebener Namen im Seitenmenü in 0.14.84 veröffentlicht ([PR #23](https://github.com/cpprograming/EmuVora/pull/23)): 47 Systeme, offline mitgelieferte Bilder, Spielzahlen, Tooltips, barrierefreie Namen und Auswahlrahmen. Helles Design, dunkles Design, TV-Größen und Controller-Filter geprüft. 176 Funktions- und 6 Netzwerktests sowie Setup-/Update-Prüfungen einschließlich mitgelieferter Logo-Lizenz bestanden.

- [x] Cover-Erkennung in 0.14.81 verbessert ([PR #22](https://github.com/cpprograming/EmuVora/pull/22)): Sammlungsnummern, Release-Zusätze, eindeutige vollständige Wortgruppen, römische Ziffern und explizite regionale Alternativtitel. Große abgeschnittene Kataloge über vollständigen Boxart-Unterordner laden. Fünf neue Regressionstests; 172 Funktions- und 6 Netzwerktests sowie Setup-/Update-Prüfungen bestanden.

- [x] Start großer Bibliotheken in 0.14.78 repariert ([PR #21](https://github.com/cpprograming/EmuVora/pull/21)): höchstens 48 Karten pro Seite, freigegebene Cover-Bilder, Cover-Verarbeitung im Hintergrund. Regression mit 6.000 synthetischen Spielen; 167 Funktionstests und 6 Netzwerktests sowie Setup-/Datenerhalt-Prüfungen bestanden.

- [x] SDL 3.4.18 mit Controller-Datenbank für die Oberfläche integriert; In 0.14.76 veröffentlicht: 166 Funktionstests und 6 Netzwerktests, echte SDL-Runtime mit virtuellem Gamepad, Installation und Update einschließlich SDK-/Datenbank-Prüfsummen bestanden. XInput-Spielprofile bleiben erhalten; zusätzliche Spielprofile und Hardwareabnahme sind offen. Veröffentlichter Setup-Download und Selbstupdate einschließlich SHA-256 geprüft.

- [x] Neues Logo, Icon für Programm/Setup/Verknüpfungen und überarbeitete Bibliotheksansicht veröffentlicht in 0.14.71; 152 Funktions- und 6 Netzwerktests sowie Installation, Update und Datenerhalt bestanden. [Vorschau](design/README.md).

- [x] Einrichtungsassistent mit frei wählbarem Datenpfad.
- [x] Spielebibliothek, Konsolenfilter, Favoriten und automatische Cover-Suche.
- [x] Automatische Einrichtung und Updates verwalteter Emulatoren.
- [x] Xbox-Navigation einschließlich Dialogen und Bildschirmtastatur implementiert.
- [x] Monitorerkennung und Profile für Seitenverhältnis und scharfe Skalierung implementiert.
- [x] Eigene EmuVora-Themes und angepassten EmulationStation-/ES-DE-Import implementieren; separaten ES-DE-Start entfernen (ab 0.14.112).
- [x] EmuVora-Selbstupdate und automatische GitHub-Builds eingerichtet.
- [x] Datensicherung mit Prüfsummen und Wiederherstellung in einen neuen Ordner implementiert.
- [x] 341 Funktionstests und 7 Netzwerktests auch im Release-Build 0.14.124 bestanden; Setup-Installation, Update und Datenerhalt geprüft.
- [x] Gemeinsamen Statusbereich in Standarddialogen, Einstellungen und Einrichtung ergänzt ([PR #32](https://github.com/cpprograming/EmuVora/pull/32)): Fortschritt, Abbruch und Fehlerdetails; Timeouts, Anfragelimits, veraltete Meldungen und Controller-B geprüft; sofortigen Abbruch korrigiert ([PR #33](https://github.com/cpprograming/EmuVora/pull/33)).
- [x] Saturn-/Dreamcast-Playlists für CHD/CUE mit Vorschau, korrekter Konsolenzuordnung und Erhalt der Einzel-Discs ergänzt ([PR #31](https://github.com/cpprograming/EmuVora/pull/31)).
- [x] Bestätigte Duplikate mit Metadatenübernahme, Auswahl, Sicherung und Wiederaufnahme zusammenführen ([PR #30](https://github.com/cpprograming/EmuVora/pull/30)); unveränderte Duplikatpfade bei erneuten Scans unterdrücken. ROM-Dateien bleiben erhalten.
- [x] SHA-256-Inhaltsvergleich und PS1-Playlist-Erstellung mit Vorschau veröffentlicht ([PR #29](https://github.com/cpprograming/EmuVora/pull/29)); Abbruch, CUE-Abhängigkeiten, geänderte Eingaben und Erhalt einzelner Disc-Metadaten geprüft. Keine automatische Zusammenführung oder Löschung.
- [x] Prüfungsübersicht für gleiche Titel und nummerierte Disc-Sätze ergänzt ([PR #28](https://github.com/cpprograming/EmuVora/pull/28)); keine automatische Löschung oder Zusammenführung. Dialog mit einer privaten Bibliothekskopie zusätzlich geprüft.

Der aktuelle Stand ist als [Release 0.14.124](https://github.com/cpprograming/EmuVora/releases/tag/v0.14.124) veröffentlicht. [PR #11](https://github.com/cpprograming/EmuVora/pull/11) ergänzt zusätzliche Spielstandsordner mit getrennter Wiederherstellung. [PR #12](https://github.com/cpprograming/EmuVora/pull/12) ergänzt Bibliothekswechsel, Rückkehr zur vorherigen Bibliothek und optionale Sicherungserinnerungen. Automatisierte Tests und UI-Aufnahmen ersetzen keine Tests mit echten Controllern, Monitoren und Spielen.

- [x] BIOS-Dateiauswahl mit sichtbarer Bestätigung, direktem Pfadimport, automatischer Benennung erkannter Inhalte und Ergebnisübersicht korrigiert ([PR #13](https://github.com/cpprograming/EmuVora/pull/13), ab 0.11.59). Originaldateien bleiben erhalten.

- [x] Optionale Sicherung importierter BIOS-/Firmware-Dateien mit Prüfsummen und getrennter Wiederherstellung implementiert; standardmäßig ausgeschaltet ([PR #14](https://github.com/cpprograming/EmuVora/pull/14), ab 0.11.61).

## 1. Zuerst: Sicherungen vollständig alltagstauglich machen

- [x] Sicherungs-Quellcode und BACKUPS.md über geprüften Pull Request #9 auf GitHub übernommen.
- [x] Release 0.11.61 nach dem Merge veröffentlicht, Setup heruntergeladen und mit dem EmuVora-Updater einschließlich SHA-256 geprüft.
- [x] Erkannte Emulator-Spielstände nach einer Neuinstallation automatisch zuordnen; Wiederherstellung vorher erläutern und vorhandene Dateien schützen (DuckStation, PCSX2, Dolphin, PPSSPP).
- [x] Benutzerdefinierte und externe Spielstandsordner auswählbar sichern; fehlende Orte deutlich anzeigen. Wiederherstellung ausschließlich getrennt im neuen Datenordner (ab 0.10.54).
- [ ] Wiederherstellung auf einem anderen Laufwerk testen: Bibliothek, Cover, Favoriten und echte Spielstände vergleichen.
- [x] Vorhandene oder wiederhergestellte Bibliotheken mit Prüfung öffnen und zur vorherigen Bibliothek zurückkehren (ab 0.11.56).
- [x] Optionale Sicherungserinnerung beim Programmstart ergänzen; standardmäßig ausgeschaltet (ab 0.11.56).
- [ ] Optionale Aufbewahrung der letzten Sicherungen ergänzen; nichts ohne ausdrückliche Einstellung löschen.

Abnahme: Ein gesichertes Spiel lässt sich nach Wiederherstellung und erneuter Emulator-Einrichtung mit seinem vorhandenen Spielstand fortsetzen.

## 2. Xbox-Controller und Plug-and-play praktisch prüfen

- [x] Geteilte Eingabelogik gegen wiederholte Bestätigung nach Wiederverbinden/Fokuswechsel und unbeabsichtigte Übernahme bei gleichzeitig gehaltenen Tasten ergänzt; simulierte Tests bestanden.
- [x] Diagnoseansicht für alle vier XInput-Slots mit Tasten, beiden Sticks, Triggern und Verbindungswechseln eingebaut. [Hardware-Testanleitung](CONTROLLERS.md) verfügbar.

- [ ] Xbox-Controller per USB und Bluetooth an echter Hardware testen.
- [ ] Alle Hauptabläufe ohne Maus durchspielen: Einrichtung, Import, Einstellungen, Sicherung, Spielstart und Rückkehr.
- [ ] Abziehen und erneutes Verbinden sowie Controllerwechsel während der Nutzung prüfen.
- [ ] Zwei bis vier Controller und korrekte Spielerzuordnung testen.
- [ ] Fokus, sichtbare Tastensymbole und Abbrechen-Verhalten in allen Dialogen vereinheitlichen.

Abnahme: Die üblichen Abläufe funktionieren mit einem Xbox-Controller ohne festhängende Dialoge oder versehentlich doppelte Aktionen.

## 3. Einheitliche Oberfläche und TV-Modus

- [x] Systemübersicht mit Suche/Filtern, lesender Zustandsprüfung, konkreter Downloadvorschau und Rückwegen zwischen Einrichtung und Bibliothek implementieren (ab 0.14.120). [Anleitung](SYSTEM_CENTER.md).

- [x] FluentAvaloniaUI 2.5.1 als gemeinsame Bedienelement-Basis mit Release 0.13.66 veröffentlicht ([PR #16](https://github.com/cpprograming/EmuVora/pull/16)); alle vier Farbschemata rendern automatisch geprüft. [TV-Anleitung](TV_MODE.md).

- [ ] Abstände, Schriften, Farben, Schaltflächen und Dialoge durchgehend vereinheitlichen.
- [x] Optionalen TV-Modus mit größeren Covern, gut lesbaren Bedienelementen und sichtbarer Spielauswahl ergänzen; Dialogübernahme und Controller-Raster automatisiert geprüft.
- [ ] Leere Bibliothek, fehlende Cover, laufende Downloads und Fehlerzustände verständlich gestalten.
- [x] Theme-Kompatibilität als native Anpassung mit Importbericht kennzeichnen; ES-DE ist nicht mehr als separater Frontend-Start nötig (ab 0.14.112).
- [ ] AlekFull-Darstellung auf echter Hardware mit 16:9, 16:10 und 4:3 prüfen; Theme-Lizenzhinweise erhalten.

Abnahme: Einrichtung, Bibliothek und Einstellungen wirken zusammengehörig und bleiben am Fernseher gut bedienbar.

## 4. Bibliothek und Cover verbessern

- [x] Dreamcast-GDI-Playlists einschließlich Trackprüfung, getrennter nummerierter Disc-Ordner, Scan-/Umzugsprüfung und Mega-CD-Playlists implementieren (ab 0.14.124). Originaldateien bleiben erhalten. [Anleitung](LIBRARY_AND_DISPLAY.md).

- [x] Festgelegte libretro-/No-Intro-Kataloge mit SHA-256 prüfen und lokale ROMs per Größe/SHA-1 erkennen. Titelübernahme nach Vorschau; eigene Bearbeitungen, Pfade, Cover und Verlauf erhalten. [Umfang: 16 Systeme](ROM_RECOGNITION.md).

- [x] Eigene Sammlungen und Ansichten für zuletzt hinzugefügt, zuletzt gespielt und Favoriten ergänzen (ab 0.14.92).
- [x] Unverändert verschobene ROM-Ordner mit Vorschau neu zuordnen (ab 0.14.94).
- [x] PS1-Mehrdisc-Spiele und bestätigte Duplikate mit Vorschau verwalten (ab 0.14.103).
- [x] Saturn-/Dreamcast-Playlists für CHD/CUE ergänzen (ab 0.14.105).
- [ ] Weitere Formate/Playlist-Systeme und umbenannte Dateien ergänzen.
- [x] Cover-Treffer vor einem manuellen Austausch anzeigen und falsche Zuordnungen korrigierbar machen (ab 0.14.90).
- [x] Fehlende Cover gesammelt anzeigen; bestehende eigene Cover bei erneutem Abruf erhalten und ausdrücklichen Schutz ergänzen (ab 0.14.90).
- [ ] Optionale zusätzliche Metadatenquelle erst nach Prüfung ihrer Bedingungen integrieren.

Abnahme: Eine größere Bibliothek bleibt übersichtlich; eigene Korrekturen überstehen einen erneuten Scan.

## 5. Bildqualität und Stabilität

- [ ] Echte Tests auf 1080p-, 1440p- und 4K-Monitoren sowie mit mehreren Monitoren durchführen.
- [ ] Windows-Anzeigeskalierung und Bildschirmwechsel prüfen.
- [ ] Seitenverhältnis und ganzzahlige Skalierung mit repräsentativen 2D- und 3D-Systemen vergleichen.
- [x] Systemgrundhinweise für Maus/Touch, Tastatur und besondere Steuerung vor dem Spielen sowie in einer gefilterten Übersicht anzeigen (ab 0.14.122). Keine pauschale Spiel-/Hardware-Kompatibilitätszusage. [Anleitung](INPUT_GUIDE.md).
- [x] Verständlichen Diagnosebericht mit Vorschau und ausdrücklichem lokalem Speichern ergänzen; private Namen/Pfade und Protokolltexte auslassen, kein Upload (ab 0.14.118). [Anleitung](DIAGNOSTICS.md).
- [ ] Updatefehler, Offlinebetrieb und Rückkehr zur vorherigen Emulatorversion praktisch testen.

Abnahme: Keine verzerrte Darstellung durch das automatische Profil; nachvollziehbare Fehler und zuverlässige Rückkehr in die Bibliothek.

## Vorgehen je Verbesserung

- [ ] Änderung in einem eigenen Pull Request umsetzen und passende Prüfungen ausführen.
- [ ] Checkliste nur bei belegter Erledigung abhaken.
- [ ] Nach dem Merge die automatisch erzeugte EXE, eingebettete Version und Updatefähigkeit prüfen.
- [ ] ROMs, BIOS und persönliche Sicherungen nicht ins Repository hochladen.

Nächste Schritte: Controller-Hardwaretests gemäß Roadmap 0.12 durchführen; unabhängig davon weitere Oberflächenzustände gemäß 0.13 und Spielerkennung gemäß 0.14 verbessern. Siehe [Repository-Plan](REPOSITORY_PLAN.md).





