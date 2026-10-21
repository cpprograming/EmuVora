# EmuVora – Roadmap

Stand: 21. Oktober 2026. Ziel ist ein zuverlässiges Windows-Frontend für den privaten Gebrauch. Die Versionsnummern bezeichnen Etappen, keine festen Veröffentlichungstermine.

Aktueller geprüfter Release: [0.14.84](https://github.com/cpprograming/EmuVora/releases/tag/v0.14.84).

## 0.14.84 – Konsolenmenü (abgeschlossen)

- [x] Offline-Logos für alle 47 unterstützten Systeme statt ausgeschriebener Menünamen.
- [x] Spielzahlen, Tooltips und barrierefreie Namen beibehalten; Neo Geo CD und FinalBurn Neo unterscheiden.
- [x] Auswahlrahmen, Controller-Filter, helle/dunkle Darstellung und größere Logos/Zahlen im TV-Modus geprüft.
- [x] Herkunft, Original-SVGs und vollständige Upstream-Lizenz erhalten; Lizenz im Setup mitliefern.
- [x] 176 Funktions- und 6 Netzwerktests sowie Installation, Update und Datenerhalt bestanden.

## 0.14.81 – Cover-Treffer (abgeschlossen)

- [x] Sammlungsnummern und Release-Zusätze für die Suche bereinigen, ohne gespeicherte Titel zu verändern.
- [x] Eindeutige vollständige Wortgruppen, römische Ziffern und explizite regionale Alternativtitel unterstützen; Mehrdeutigkeiten und andere Fortsetzungen ausschließen.
- [x] Abgeschnittene große Cover-Kataloge über vollständige Boxart-Unterordner laden.
- [x] 172 Funktionstests, 6 Netzwerktests und Installer-/Update-Prüfungen bestanden. Nicht alle Prototypen, Zusatzdateien oder Spiele besitzen ein Cover in der Quelle.

## 0.14.78 – Startstabilität (abgeschlossen)

- [x] Bibliotheksansicht auf 48 Cover-Karten pro Seite begrenzt und Bildspeicher beim Wechsel freigegeben.
- [x] Cover-Prüfung und Downloads in den Hintergrund verlagert; Controller-Browsing bleibt möglich.
- [x] Regression mit 6.000 synthetischen Spielen, Seitenwechseln, Suche und Bildcache ergänzt.
- [x] 167 Funktionstests, 6 Netzwerktests und Setup-/Update-Prüfungen bestanden.

## 0.11 – Daten und Wiederherstellung (abgeschlossen)

- [x] Vorhandene Datenordner in der Oberfläche prüfen und mit Bestätigung öffnen.
- [x] Zur unmittelbar vorherigen Bibliothek zurückkehren; nicht erreichbare Laufwerke erhalten keine Ersatzbibliothek.
- [x] Datenpfad und Rückweg bei fehlgeschlagenem Prozessstart zurücksetzen.
- [x] Optional an Sicherungen erinnern: 7, 14 oder 30 Tage, standardmäßig ausgeschaltet.
- [x] Release 0.11.61 veröffentlicht, heruntergeladen und mit dem Updater einschließlich SHA-256 geprüft; 123 Funktionstests, 5 Netzwerktests und Installer-Prüfungen bestanden.

Zusätzlich in 0.11.59: BIOS-Dateiauswahl korrigiert und automatische Benennung anhand bekannter Prüfsummen ergänzt; [Anleitung](BIOS_IMPORT.md).

In 0.11.61: Optionale BIOS-/Firmware-Sicherung ergänzt: persönliche importierte Dateien können ausdrücklich ins eigene Archiv aufgenommen werden.

## 0.12 – Xbox und Plug-and-play (Implementierung, Hardwareabnahme offen)

- [x] Gemeinsame Eingabelogik für Bibliothek und Dialoge: Wiederverbinden, Fokuswechsel, Wiederholung und Übernahme zwischen Controllern automatisiert geprüft.
- [x] Diagnoseansicht für vier XInput-Slots mit Tasten, Sticks, Triggern und Verbindungswechseln ergänzt.
- [x] Release 0.12.64 veröffentlicht und per Updater geprüft; 134 Funktionstests, 5 Netzwerktests und Installer-Prüfungen bestanden.
- [x] Praktische Prüfschritte in [CONTROLLERS.md](CONTROLLERS.md) dokumentiert.

- [x] SDL 3 und feste GameControllerDB für zusätzliche Gamepads in Bibliothek/Dialogen in 0.14.76 veröffentlicht; 166 Funktions- und 6 Netzwerktests, Installer, SDK-Dateien und veröffentlichter Selbstupdate-Download geprüft; XInput-Rückfall, keine doppelte Quelle und keine SDL-IDs in Xbox-Spielprofilen. [Anleitung](CONTROLLERS.md).
- [ ] Automatische Spielprofile für zusätzliche Controller je Emulator ergänzen und prüfen.
- [ ] Xbox-Controller per USB und Bluetooth auf echter Hardware testen.
- [ ] Einrichtung, Dateiauswahl, Sicherung und Spielstart vollständig mit Controller durchspielen.
- [ ] Abziehen, Wiederverbinden, Controllerwechsel und mehrere Spieler praktisch prüfen.
- [ ] Fokusführung und Tastensymbole anhand der Ergebnisse verbessern.

## 0.13 – Einheitliche Oberfläche (Fluent/TV veröffentlicht, weitere Zustände offen)

- [x] Release 0.13.66 veröffentlicht, heruntergeladen und per Updater einschließlich SHA-256 geprüft; 138 Funktionstests, 5 Netzwerktests und Installer-Prüfungen bestanden.

Externe Bausteine und Reihenfolge: [Repository-Plan](REPOSITORY_PLAN.md). FluentAvaloniaUI 2.5.1 als Bedienelement-Basis integriert; die vier Farbschemata und Controller-Raster sind automatisch geprüft. [TV-Modus](TV_MODE.md).

- [ ] Dialoge, Abstände, Schriften und Schaltflächen vereinheitlichen.
- [x] Optionalen TV-Modus mit großen Covern, lesbaren Texten und sichtbarer Auswahl ergänzen.
- [x] Leere Bibliothek und ergebnislose Filter mit eigenen Fluent-Infobereichen unterscheiden.
- [x] Neue Marke, App-/Setup-/Shortcut-Icon, Navigation, Cover-Platzhalter und Detailansicht in 0.14.71 veröffentlicht; 152 Funktions- und 6 Netzwerktests, Installation und Selbstupdate einschließlich SHA-256 geprüft. [Vorschau](design/README.md).
- [ ] Downloads und Fehlerzustände weiter vereinheitlichen.
- [x] Direkte Theme-Unterstützung und separate ES-DE-Darstellung in Einstellungen und Anleitung unterscheiden.

## 0.14 – Spielebibliothek

- [x] Release 0.14.68 mit 149 Funktions- und 6 Netzwerktests veröffentlicht; Setup-Download und Selbstupdate einschließlich SHA-256 geprüft.

- [x] Prüfsummen-Erkennung mit bestätigter Titelvorschau für 16 Cartridge-Systeme integriert; Schutz eigener Bearbeitungen und lokaler ROM-Dateien. [Anleitung](ROM_RECOGNITION.md).

- [ ] Eigene Sammlungen und zusätzliche Ansichten ergänzen.
- [ ] Doppelte Einträge und Mehrdisc-Spiele übersichtlicher verwalten.
- [ ] Umgezogene ROM-Ordner neu zuordnen, ohne Favoriten und Verlauf zu verlieren.
- [ ] Falsche Cover-Treffer leichter korrigieren und eigene Cover bewahren.

## 0.15 – Bild und Leistung

- [ ] 1080p, 1440p, 4K und mehrere Monitore praktisch testen.
- [ ] Windows-Anzeigeskalierung und Bildschirmwechsel prüfen.
- [ ] Seitenverhältnis und scharfe Skalierung mit repräsentativen 2D- und 3D-Systemen vergleichen.
- [ ] Systeme mit besonderen Eingabegeräten eindeutig kennzeichnen.

## 1.0 – Stabile Privatversion

- [ ] Vollständige Einrichtung auf einem frischen Windows-System prüfen.
- [ ] Update, Offlinebetrieb, Sicherung, Wiederherstellung und Rückkehr praktisch testen.
- [ ] Verbleibende Fehler beheben und Anleitungen abschließen.
- [ ] Eine stabile EXE mit nachvollziehbaren Versionshinweisen veröffentlichen.

Der detaillierte Arbeitsstand steht in der [Checkliste](CHECKLIST.md). Automatisierte Tests ersetzen die noch offenen Hardwaretests nicht.




