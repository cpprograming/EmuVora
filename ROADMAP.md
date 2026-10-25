# EmuVora – Roadmap

Stand: 25. Oktober 2026. Ziel ist ein zuverlässiges Windows-Frontend für den privaten Gebrauch. Die Versionsnummern bezeichnen Etappen, keine festen Veröffentlichungstermine.

Aktueller geprüfter Release: [0.14.120](https://github.com/cpprograming/EmuVora/releases/tag/v0.14.120).

## 0.14.120 – Systemzentrale (abgeschlossen)

- [x] Status für alle 47 unterstützten und unzugeordnete importierte Systeme mit Suche, Filtern und Titel-/Datei-/Coverzahlen lesend anzeigen.
- [x] Fehlende Emulatoren und Cores einzeln oder für die importierten Systeme nach konkreter Vorschau einrichten; gemeinsame Pakete bündeln und vor Ausführung erneut prüfen.
- [x] Funktionierende eigene Pfade erhalten; BIOS-Import und Emulator-Einstellungen mit Rückkehr zur Übersicht verbinden.
- [x] Direkte Bibliotheksfilter für Spiele und fehlende Cover ergänzen; feste Aktionsleiste, schmale Fenster und TV-Layout berücksichtigen.
- [x] Konkurrierende Aktionen während der Arbeit sperren; Controller-Abbruch erhält die Übersicht. 318 Funktions- und 7 Netzwerktests sowie Setup-/Update-Prüfungen bestanden. [Anleitung](SYSTEM_CENTER.md).
- [ ] Echte Controller-/Monitor-/Spieltests wie gewünscht später durchführen; weitere Spezialdialoge angleichen.

## 0.14.118 – Diagnose mit Vorschau (abgeschlossen)

- [x] Bibliothekszahlen, Dateierreichbarkeit, Emulatoren, Cores und allgemeinen Firmwarestatus lesend prüfen; Monitor-/Bild-/Sprachvorgaben zusammenfassen.
- [x] Nur festgelegte Angaben exportieren: keine privaten Spielnamen/Pfade, Gerätekennungen, BIOS-/ROM-Inhalte oder Protokolltexte.
- [x] Schreibgeschützte Vorschau mit freiwilligem lokalem Export ohne automatische Übertragung ergänzen; Abbruch und vorhandene Originaldateien respektieren.
- [x] 310 Funktions- und 7 Netzwerktests sowie Setup-/Update-Prüfungen bestanden; Vorschau visuell und Bericht mit 5.805 Einträgen lesend geprüft. [Anleitung](DIAGNOSTICS.md).
- [ ] Hardwareabnahme, Monitorwechsel und praktische Controller-/Sprachtests weiterführen; die Diagnose ersetzt diese nicht.

## 0.14.116 – Deutsch und Startdialoge (abgeschlossen)

- [x] Deutsche Oberflächen für alle fünf verwalteten Emulatoren automatisch einrichten; abschaltbare Vorgabe und Konfigurationssicherung ergänzen.
- [x] Unterstützte Spielsprachoptionen für RetroArch-Cores, Dreamcast, DS, GameCube, Wii und PSP setzen; Grenzen für einzelne Spiele und PS1/PS2 dokumentieren.
- [x] Fehlende Pflicht-Firmware mit eigenem BIOS-Importdialog anzeigen; fehlende ROMs vor Downloads erkennen.
- [x] Fehler außerhalb offener Aktionsdialoge als eigenes Fenster mit Details und Einstellungen anzeigen; bestehende Dialog-Fehlerbereiche und ruhige Hintergrundprüfungen erhalten.
- [x] 306 Funktions- und 7 Netzwerktests sowie Setup-/Update-Prüfungen bestanden; neue Dialoge visuell und installierte Sprachwerte geprüft. [Anleitung](LANGUAGE_AND_ERRORS.md).
- [ ] Deutsche Spielsprache mit repräsentativen echten Spielen weiter praktisch prüfen; nicht jedes Spiel übernimmt die Konsolensprache.

## 0.14.112 – Native Oberfläche und Themes (abgeschlossen)


- [x] Konsolenlogos, Zähler und Titelzeilen einheitlich ausrichten.
- [x] Gleichnamige Titel pro Konsole mit Dateiauswahl bündeln; Abschalten der Gruppierung ermöglichen und Metadaten erhalten.
- [x] Native Basisstile Aurora und Studio ergänzen; separaten ES-DE-Start, Download und Autostart entfernen.
- [x] Unterstützte ES-DE-Themes automatisch an das native Layout anpassen; Variante/Farbschema wählen und Importbericht samt Grenzen anbieten.
- [x] PS2-Haupt-BIOS strukturell prüfen; falsche DIFF-Auswahl verhindern und Emulatorfehler genauer anzeigen. Betroffener Spielstart nach Korrektur bestätigt.
- [x] 296 Funktions- und 7 Netzwerktests sowie Setup-/Update-Prüfungen bestanden; Bibliothekskopie mit 5.805 Einträgen und native Aurora-/Studio-Ansichten geprüft.
- [ ] Weitere native Layouts und Theme-Optionen ausbauen; fremde ES-DE-Speziallayouts bleiben außerhalb der Anpassung.

## 0.14.109 – Status und Fehlerhinweise (abgeschlossen)

- [x] Gemeinsamen Fortschritts-/Abbruchbereich in Standarddialogen, allen Einstellungen-Tabs und Einrichtung anzeigen.
- [x] Lesbare Fehlermeldungen mit technischen Details und gemeinsamem Ausblenden ergänzen; automatische Updateprüfungen ruhig halten.
- [x] Eigenen Abbruch von Timeouts unterscheiden, verspätete Fortschrittsmeldungen verwerfen und verschachtelte Dialogzustände erhalten; sofortigen Abbruch korrekt abschließen.
- [x] 288 Funktions- und 6 Netzwerktests sowie Setup-/Update-Prüfungen bestanden; Fehleransicht visuell geprüft.
- [ ] Übrige Spezialdialoge, Abstände und Validierung sowie Hardwaretests weiter bearbeiten.

## 0.14.105 – Saturn und Dreamcast (abgeschlossen)

- [x] CHD/CUE-Playlists für Saturn/Beetle Saturn und Dreamcast/Flycast nach bestätigter Vorschau erstellen.
- [x] Konsolenzuordnung in Playlist-Karte und erneutem Scan erhalten; vorhandene Disc-Metadaten bewahren.
- [x] Nummerierung, CUE-Tracks, Abbruch und Vorschau für alle drei Systeme prüfen; GDI/CDI und unbestätigte Systeme auslassen.
- [x] 277 Funktions- und 6 Netzwerktests sowie Setup-/Update-Prüfungen bestanden.
- [ ] Weitere Disc-Formate und Playlist-Systeme ergänzen; echte Spiel-/Disc-Wechsel separat testen.

## 0.14.103 – Bestätigte Duplikate (abgeschlossen)

- [x] Verbleibenden Titel/Pfad, Cover-Schutz und eigenes Bildprofil vor Übernahme auswählen.
- [x] Favoriten/Sammlungen vereinigen, Spielstarts addieren und bekannte Import-/Startzeiten übernehmen; ROMs und Spielstände erhalten.
- [x] Datei-Inhalte erneut prüfen; konsistente Sicherung, Originalmetadatenarchiv und Wiederaufnahme nach Unterbrechung ergänzen.
- [x] Unveränderte Duplikatpfade bei Scans unterdrücken; geänderte Dateien, JSON-Import, vollständige Sicherung und ROM-Umzug berücksichtigen.
- [x] 266 Funktions- und 6 Netzwerktests sowie Setup-/Update-Prüfungen bestanden.

## 0.14.100 – Datei-Inhalte und PS1-Playlists (abgeschlossen)

- [x] Ausgewählte Dateien per SHA-256 vergleichen; leere/fehlende/gesperrte Dateien melden und reine Verweisdateien auslassen.
- [x] PS1-M3U aus nummerierten CHD/CUE-Discs nach Vorschau erstellen und als zusätzliche Karte importieren; vorhandene Discs und Metadaten behalten.
- [x] Geänderte Dateien, unvollständige Disc-Nummern, ungeeignete CUE-Verweise und bestehende Ziele abweisen; Spielstandsnamenswechsel erklären.
- [x] 246 Funktions- und 6 Netzwerktests sowie Setup-/Update-Prüfungen bestanden; neuen Dialog visuell geprüft.
- [x] Bestätigte Duplikate mit Auswahl, Metadatenübernahme, Sicherung und Wiederaufnahme zusammenführen (ab 0.14.103).
- [x] Saturn-/Dreamcast-Playlists für CHD/CUE ergänzen (ab 0.14.105).
- [ ] Weitere Formate und Playlist-Systeme ergänzen.

## 0.14.98 – Bibliotheksprüfung (Prüfungsübersicht abgeschlossen)

- [x] Gleiche Titel als ungeprüfte Kandidaten und explizit nummerierte Discs im selben Ordner anzeigen; Editionen und Regionen getrennt halten.
- [x] Prüfung abbrechbar im Hintergrund ausführen; ROM-Dateien und Bibliotheksmetadaten unverändert lassen.
- [x] 228 Funktions- und 6 Netzwerktests sowie Setup-/Update-Prüfungen bestanden; Dialog zusätzlich mit einer privaten Bibliothekskopie geprüft.
- [x] Inhaltsprüfung per SHA-256 und bestätigte PS1-Playlist-Erstellung für CHD/CUE implementieren (ab 0.14.100).
- [x] Zusammenführen mit Metadatenübernahme und Sicherung ergänzen (ab 0.14.103).
- [x] Saturn-/Dreamcast-Playlists für CHD/CUE ergänzen (ab 0.14.105).
- [ ] Weitere Formate und Playlist-Systeme ergänzen.

## 0.14.94 – Verschobene ROM-Ordner (abgeschlossen)

- [x] Fehlende ROM-Pfade am gleichen relativen Zielpfad finden und vor Übernahme anzeigen.
- [x] Bibliotheksmetadaten, Importdatum, Cover-Schutz, Bildprofile und Sammlungen erhalten; ROM-/Spielstandsdateien unverändert lassen.
- [x] Ziele und Vorschau erneut prüfen; Konflikte, Pfad-Aliase und ungeeignete CUE/M3U-Verweise abweisen.
- [x] Bibliothek/Einstellungen vorher sichern und nach Unterbrechung konsistent wieder aufnehmen; gegen verwaltete ES-DE-Spielstarts sperren.
- [x] 223 Funktions- und 6 Netzwerktests sowie Installation, Update und Datenerhalt bestanden.

## 0.14.92 – Sammlungen und neue Importe (abgeschlossen)

- [x] Eigene Sammlungen erstellen, umbenennen und löschen; mehrere Zuordnungen pro Spiel speichern.
- [x] Sammlung mit Suche/Konsolenfilter und begrenzten Seiten anzeigen; Zuordnung auch im TV-Modus mit Controller erreichbar.
- [x] Erste Importzeit für neue Einträge speichern; erneute Scans verändern sie nicht. Ältere Einträge bleiben ohne erfundenes Datum.
- [x] Sammlungen und Importzeiten mit vollständiger Sicherung/Wiederherstellung bewahren.
- [x] 203 Funktions- und 6 Netzwerktests sowie Installation, Update und Datenerhalt bestanden.
- [x] Freiwilliges GitHub Sponsoring im Repository vorbereiten; kostenlose Downloads beibehalten.
- [x] Sponsors-Antrag eingereicht; Anmeldung durch den Kontoinhaber durchgeführt.
- [ ] GitHub-Freigabe für das Sponsors-Profil abwarten; anschließend öffentlichen Sponsor-Button prüfen. [Status und Profiltext](SPONSORS.md).

## 0.14.90 – Cover-Verwaltung (abgeschlossen)

- [x] Ansicht „Fehlende Cover“ mit Suche, Konsolenfilter und begrenzten Seiten ergänzen.
- [x] Eigene Bilder und Online-Treffer vor dem Austausch neben dem bisherigen Cover anzeigen; abweichender Suchname ohne Titeländerung.
- [x] Bestätigte Bilder separat kopieren und schützen; Sammelsuche und ES-DE beachten den Schutz. Frühere manuell gewählte GUID-Bilder beim Laden schützen.
- [x] Cover-Aktionen bei ausgeblendeten Details und im TV-Modus über Xbox X erreichbar machen.
- [x] 195 Funktions- und 6 Netzwerktests sowie Installation, Update und Datenerhalt bestanden.

## 0.14.86 – Präzise Cover-Zuordnung (abgeschlossen)

- [x] Echte Untertitel in Klammern und bedeutungstragende Jahreszahlen erhalten; bekannte Metadaten getrennt behandeln.
- [x] Kleine plattformgebundene Liste bekannter Titelvarianten ergänzen; exakte Editionsnamen bleiben vorrangig.
- [x] GB/GBC-Coverkatalog aus validiertem Cartridge-Header und Prüfsumme wählen, auch für einzelne ROMs in ZIPs.
- [x] Ungültige Header und mehrdeutige Archive lösen keinen geratenen Plattformwechsel aus; Bibliotheksdaten und ROMs bleiben erhalten.
- [x] 184 Funktions- und 6 Netzwerktests sowie Installation, Update und Datenerhalt bestanden.

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

- [x] Zentrale Systemübersicht mit Suche, Einrichtungs-/Firmware-/Bibliotheksstatus, bestätigter Sammel-Einrichtung, direkten Bibliotheksaktionen und fest angeordneter Aktionsleiste implementieren (ab 0.14.120). [Anleitung](SYSTEM_CENTER.md).

- [x] Release 0.13.66 veröffentlicht, heruntergeladen und per Updater einschließlich SHA-256 geprüft; 138 Funktionstests, 5 Netzwerktests und Installer-Prüfungen bestanden.

Externe Bausteine und Reihenfolge: [Repository-Plan](REPOSITORY_PLAN.md). FluentAvaloniaUI 2.5.1 als Bedienelement-Basis integriert; die vier Farbschemata und Controller-Raster sind automatisch geprüft. [TV-Modus](TV_MODE.md).

- [ ] Dialoge, Abstände, Schriften und Schaltflächen vereinheitlichen.
- [x] Optionalen TV-Modus mit großen Covern, lesbaren Texten und sichtbarer Auswahl ergänzen.
- [x] Leere Bibliothek und ergebnislose Filter mit eigenen Fluent-Infobereichen unterscheiden.
- [x] Neue Marke, App-/Setup-/Shortcut-Icon, Navigation, Cover-Platzhalter und Detailansicht in 0.14.71 veröffentlicht; 152 Funktions- und 6 Netzwerktests, Installation und Selbstupdate einschließlich SHA-256 geprüft. [Vorschau](design/README.md).
- [ ] Downloads und Fehlerzustände weiter vereinheitlichen.
  - Gemeinsamer Statusbereich mit Fortschritt, Abbruch und Fehlerdetails für Standarddialoge veröffentlicht (ab 0.14.109); Timeouts und veraltete Download-Meldungen gesondert behandeln. [Anleitung](OPERATIONS_AND_ERRORS.md).
- [x] Native Themes Aurora/Studio und automatische ES-DE-Anpassung implementieren; separaten ES-DE-Start und Download aus der Oberfläche entfernen (ab 0.14.112). [Umfang und Grenzen](NATIVE_THEMES.md).
- [x] Einheitliche Konsolenzeilen und sichtbare Versionsgruppen statt doppelter Titelkarten implementieren (ab 0.14.112).

## 0.14 – Spielebibliothek

- [x] Release 0.14.68 mit 149 Funktions- und 6 Netzwerktests veröffentlicht; Setup-Download und Selbstupdate einschließlich SHA-256 geprüft.

- [x] Prüfsummen-Erkennung mit bestätigter Titelvorschau für 16 Cartridge-Systeme integriert; Schutz eigener Bearbeitungen und lokaler ROM-Dateien. [Anleitung](ROM_RECOGNITION.md).

- [x] Eigene Sammlungen und Ansicht/Sortierung für neue Importe ergänzen (ab 0.14.92).
- [x] Doppelte Einträge und PS1-Mehrdisc-Spiele mit Vorschau verwalten (ab 0.14.103).
  - Prüfungsübersicht ab 0.14.98; ab 0.14.100 SHA-256-Vergleich und PS1-Playlists mit Vorschau. Bestätigte Duplikate ab 0.14.103 zusammenführen. Ab 0.14.105 Saturn-/Dreamcast-Playlists für CHD/CUE erstellen; weitere Formate/Systeme bleiben offen.
- [x] Umgezogene ROM-Ordner mit Vorschau neu zuordnen, ohne Favoriten und Verlauf zu verlieren (ab 0.14.94).
- [x] Falsche Cover-Treffer mit Vorschau korrigieren, fehlende Bilder gesammelt anzeigen und eigene Cover schützen (ab 0.14.90).

## 0.15 – Bild und Leistung

- [x] Diagnosebericht mit Vorschau, allgemeinen Prüfergebnissen und freiwilligem lokalem Export ohne private Namen/Pfade oder automatische Übertragung ergänzen (ab 0.14.118). [Anleitung](DIAGNOSTICS.md).

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




