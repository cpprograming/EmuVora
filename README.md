# EmuVora

EmuVora ist eine Windows-Spielbibliothek für deine eigenen ROMs und vorhandenen Spiele. Sie verwaltet **47 Systeme** und startet RetroArch, DuckStation, PCSX2, Dolphin oder PPSSPP als eigenständige Anwendungen.

**Aktueller geprüfter Stand: [0.14.124](https://github.com/cpprograming/EmuVora/releases/tag/v0.14.124)** · Entwicklung für den privaten Gebrauch, ohne Pro-Version oder Bezahlsystem.

## Download und Installation

**Mehrdisc erweitert:** Dreamcast-GDI mit geprüften Track-Verweisen und eindeutig nummerierten benachbarten Disc-Ordnern sowie Mega-CD-Playlists für Genesis Plus GX. Vorschau und erneute Prüfung vor dem Schreiben erhalten Originaldateien und Disc-Metadaten. [Abläufe und Grenzen](LIBRARY_AND_DISPLAY.md).

**Bedienhinweise:** DS-Touchscreen, Wii-Bewegung und Tastatur-/Mausbedarf sind vor dem Spielen erkennbar. Ein durchsuchbarer Leitfaden erklärt die Grundbedienung aller 47 Systeme; Hinweise stehen auch in der Systemübersicht und bei der Auswahl betroffener Spiele. [Umfang und Grenzen](INPUT_GUIDE.md).

**Systemübersicht:** Einrichtungszustände, BIOS-/Core-Hinweise, fehlende Spiele/Cover und direkte Bibliotheksfilter an einem Ort. Fehlende Komponenten lassen sich für eine Konsole oder für die importierten Systeme nach konkreter Downloadvorschau einrichten; eigene funktionierende Pfade bleiben erhalten. [Abläufe und Grenzen](SYSTEM_CENTER.md).

**Diagnose mit Vorschau:** Unter Einstellungen → App-Updates lässt sich ein Bericht zu Bibliothek, Emulatoren, Cores, Firmware und Bildvorgaben anzeigen und freiwillig lokal speichern. Er enthält keine Spielnamen, privaten Pfade oder Protokollinhalte und wird nicht hochgeladen. [Umfang und Grenzen](DIAGNOSTICS.md).

**Deutsch und Startdialoge:** Die deutschen Sprachprofile für verwaltete Emulatoren und unterstützte Spielsysteme sind standardmäßig aktiv. Fehlendes BIOS erhält einen eigenen Importdialog; andere Fehler ein Fehlerfenster oder den Fehlerbereich des bereits geöffneten Dialogs. [Anleitung, Sprachumfang und Grenzen](LANGUAGE_AND_ERRORS.md).

Unter **Einstellungen → Bibliothek → Duplikate und Mehrdisc-Spiele** werden gleiche Titel pro System und nummerierte Disc-Sätze im selben Ordner angezeigt. Dateien lassen sich per SHA-256 vergleichen; Playlists aus CHD/CUE werden nach bestätigter Vorschau für PS1 sowie ab 0.14.105 für Saturn und Dreamcast erstellt. Neu ab 0.14.103: bestätigte Duplikate mit Auswahl des verbleibenden Eintrags, Covers und Bildprofils zusammenführen. Favoriten, Verlauf und Sammlungen werden übernommen, Originalmetadaten vorher gesichert. ROMs und Spielstände bleiben erhalten. Spielstände können im Emulator eine Zuordnung zum verbleibenden ROM- oder neuen Playlist-Namen benötigen. [Anleitung und Grenzen](LIBRARY_AND_DISPLAY.md).

- **[Windows-Setup herunterladen](https://github.com/cpprograming/EmuVora/releases/latest/download/EmuVora-Setup.exe)** – Installationslaufwerk und Ordner wählen; eigene Desktop- und Startmenü-Verknüpfungen sind optional.
- **[Portable Windows-Version herunterladen](https://github.com/cpprograming/EmuVora/releases/latest/download/EmuVora-win-x64.zip)** – ZIP vollständig entpacken und `EmuVora.App.exe` starten.
- Alle Versionen und Änderungen findest du unter **[Releases](https://github.com/cpprograming/EmuVora/releases)**.

Die Pakete sind für Windows 10/11 x64 und enthalten die benötigte .NET-Laufzeit. Für die normale Nutzung ist kein .NET SDK nötig. Emulatoren und ihre zusätzlichen Laufzeitabhängigkeiten werden gesondert eingerichtet.

## GDI und Mega-CD als Mehrdisc-Playlist – neu in 0.14.124

Dreamcast-GDI-Sätze erhalten eine geprüfte relative Playlist, auch in eindeutig nummerierten benachbarten Disc-Ordnern. Manifest und Tracks werden vor dem Schreiben erneut geprüft; der Scanner erkennt Track-Abhängigkeiten und die ROM-Neuzuordnung berücksichtigt GDI-Verweise. Mega-CD erhält CHD/CUE-Playlists für Genesis Plus GX. Einzel-Discs, Metadaten und Spielstände bleiben erhalten. [Abläufe und Grenzen](LIBRARY_AND_DISPLAY.md).

341 Funktions- und 7 Netzwerktests sowie Setup-/Update-Prüfungen bestanden. Die offiziellen Flycast-/Genesis-Plus-GX-Cores melden die benötigten Formate. Echte Disc-Wechsel mit Spielen bleiben separat zu testen.

## Bedienung vor dem Spielen erkennen – neu in 0.14.122

Ein durchsuchbarer Leitfaden erklärt die Grundbedienung aller 47 Systeme. Zehn Systeme mit möglichen Zusatzeingaben wie DS-Touchscreen, Wii-Bewegung oder Tastatur/Maus erhalten einen sichtbaren Hinweis bei der Spielauswahl, auch ohne rechte Detailansicht und im TV-Modus. Die Systemübersicht zeigt dieselben Grundhinweise. [Umfang und Grenzen](INPUT_GUIDE.md).

323 Funktions- und 7 Netzwerktests sowie Setup-/Update-Prüfungen bestanden. Profile und Bibliotheksdaten bleiben erhalten; tatsächliche Spiel-/Hardwareanforderungen werden damit nicht automatisch erkannt oder getestet.

## Systemzentrale und gezielte Einrichtung – neu in 0.14.120

Die Systemübersicht in der Seitenleiste verbindet Suche/Filter für deine Systeme mit Emulator-, Core-, Firmware- und Bibliotheksstatus. Fehlende Programme und Cores lassen sich für eine Konsole oder alle importierten Systeme nach konkreter Vorschau einrichten. Eigene funktionierende Pfade bleiben erhalten; BIOS-Dateien werden selbst importiert. Direkte Aktionen führen zu den passenden Spielen und fehlenden Covern. Eine feste Aktionsleiste bleibt auch in schmalen Fenstern und im TV-Modus sichtbar. [Abläufe und Grenzen](SYSTEM_CENTER.md).

318 Funktions- und 7 Netzwerktests sowie Setup-/Update-Prüfungen bestanden. Zustandsprüfung mit 5.805 privaten Einträgen lesend geprüft; echte Hardwaretests bleiben offen.

## Diagnosebericht mit Vorschau – neu in 0.14.118

Unter Einstellungen → App-Updates erscheint eine schreibgeschützte Diagnosevorschau zu Bibliothek, Emulatoren, Cores, Firmware und Bildvorgaben. Erst eine ausdrückliche Ordnerauswahl speichert eine neue Textdatei. Spielnamen, private Pfade, Gerätekennungen und Protokollinhalte werden ausgelassen; es gibt keinen Upload. [Umfang und Grenzen](DIAGNOSTICS.md).

310 Funktions- und 7 Netzwerktests sowie Setup-/Update-Prüfungen bestanden. Die lesende Prüfung mit 5.805 privaten Bibliothekseinträgen dauerte etwa 0,8 Sekunden; Einstellungen blieben unverändert und es wurde kein Bericht gespeichert.

## Deutsch und sichtbare Startdialoge – neu in 0.14.116

Alle fünf verwalteten Emulatoren erhalten standardmäßig eine deutsche Oberfläche. Unterstützte Konsolen-/Core-Sprachoptionen bevorzugen Deutsch für Spiele, die diese Sprache mitbringen. Fehlendes BIOS erhält einen eigenen Importdialog; andere Startfehler erscheinen sichtbar mit Details und Zugang zu den Einstellungen. Die Vorgabe lässt sich abschalten, andere Einstellungen und BIOS-/Spielstanddateien bleiben erhalten. [Umfang und Grenzen](LANGUAGE_AND_ERRORS.md).

306 Funktions- und 7 Netzwerktests sowie Setup-/Update-Prüfungen bestanden. Die installierten Sprachwerte und vorhandenen Übersetzungsressourcen wurden geprüft.

## Native Oberfläche, Versionen und Themes – neu in 0.14.112

Die Konsolenzeilen und Titelkarten sind gleichmäßig ausgerichtet. Gleichnamige Titel derselben Konsole erscheinen als eine Karte mit auswählbaren Dateiversionen, ohne Dateien oder Metadaten zu löschen. Aurora und Studio ergänzen die eigenen Basisstile. ES-DE-Themes lassen sich mit Importbericht an das native Layout anpassen; der separate ES-DE-Start entfällt. Die PS2-BIOS-Auswahl prüft ROMDIR/ROMVER und vermeidet gleich große DIFF-Begleitdateien. [Anleitung und Grenzen](NATIVE_THEMES.md).

296 Funktions- und 7 Netzwerktests sowie Setup-/Update-Prüfungen bestanden. Die Prüfung an einer privaten Bibliothekskopie mit 5.805 Einträgen bestätigt begrenzte 48 Karten pro Seite und reagierende Suche; das Original blieb unverändert.

## Fortschritt und Fehlerhinweise – neu in 0.14.109

Standarddialoge, Einstellungen-Tabs und Einrichtungsassistent zeigen Fortschritt und Abbruch direkt im Dialog. Fehler erhalten einen lesbaren Hinweis mit ausklappbaren technischen Details. Timeouts werden von eigenem Abbruch unterschieden; verspätete Download-Meldungen überschreiben keinen neueren Vorgang. Automatische Updateprüfungen bleiben bei Fehlern im Hintergrund ruhig. [Ablauf und Grenzen](OPERATIONS_AND_ERRORS.md).

## Stabiler Start großer Bibliotheken – repariert in 0.14.78

Die Bibliothek zeigt höchstens 48 Cover-Karten gleichzeitig. Vorherige/Nächste Seite und Controller-Navigation erreichen weitere Spiele; Suche und Konsolenfilter durchsuchen weiterhin die ganze Sammlung. Beim Wechsel werden bisherige Cover-Bilder aus dem Speicher freigegeben. Automatische Cover-Prüfung und Downloads laufen im Hintergrund; Ergebnisse werden am Ende oder beim Abbrechen gespeichert. Ein Oberflächentest prüft 6.000 Spiele, Seitenwechsel, Controller, Suche und den begrenzten Bildcache.

## Mehr Controller in EmuVora – neu in 0.14.76

SDL 3 und eine mitgelieferte Controller-Datenbank belegen unterstützte Gamepads automatisch für Bibliothek und Dialoge. Unter **Einstellungen → Emulatoren → Controller prüfen** Namen, Verbindung und Eingaben ansehen. XInput bleibt als Rückfall erhalten. Xbox-Spielprofile bleiben erhalten; andere Spielprofile werden weiterhin im Emulator eingerichtet. [Umfang, Herkunft und Tests](CONTROLLERS.md).

## Konsolenlogos im Seitenmenü – neu in 0.14.84

Das Konsolenmenü zeigt offline mitgelieferte Logos für alle 47 unterstützten Systeme statt ausgeschriebener Namen. Spielanzahl und Auswahlrahmen bleiben sichtbar; Namen erscheinen als Tooltip und sind für die barrierefreie Bedienung hinterlegt. Neo Geo CD und FinalBurn Neo erhalten zusätzliche Kennzeichnungen. Unbekannte Systeme verwenden ein neutrales Symbol. Die Darstellung unterstützt helles Design, dunkles Design und TV-Modus. Herkunft und Lizenz: [Konsolenlogos](third-party/console-logos/README.md).

## Verschobene ROM-Ordner neu zuordnen – neu in 0.14.94

Unter **Einstellungen → Bibliothek → Verschobenen ROM-Ordner neu zuordnen** den bisherigen und neuen Ordner angeben. EmuVora sucht fehlende Einträge am gleichen relativen Dateipfad und zeigt eine Vorschau. Erst nach Bestätigung werden die Bibliothekspfade geändert; Cover, Favoriten, Verlauf, Importdatum, Bildprofile und Sammlungen bleiben erhalten. Bereits erreichbare Originale, doppelte Ziele und geänderte Vorschläge werden nicht übernommen. Bibliothek und Einstellungen werden vorher gesichert; eine unterbrochene Übernahme wird beim nächsten Start abgeglichen. ROMs und Spielstände werden nicht verschoben. [Anleitung und Grenzen](LIBRARY_AND_DISPLAY.md).

## Eigene Sammlungen und neue Importe – neu in 0.14.92

Über **Sammlungen verwalten** eigene Listen erstellen, umbenennen oder löschen. Ein Spiel auswählen und **Zu Sammlungen zuordnen** öffnen; mehrere Sammlungen sind möglich. Die Auswahl links lässt sich mit Suche und Konsolenfilter kombinieren. Entfernen aus einer Sammlung löscht keine ROMs, Cover, Favoriten oder Spielstände. Die vollständige Datensicherung enthält die Sammlungen.

**Zuletzt hinzugefügt** zeigt neue Importe ab dieser Version nach ihrem ersten Importdatum. Erneutes Scannen verändert dieses Datum nicht. Bei älteren Bibliothekseinträgen ist das ursprüngliche Importdatum nicht bekannt; sie bleiben unter **Alle Spiele** erreichbar. Auch in Sammlungen und dieser Ansicht bleiben höchstens 48 Karten gleichzeitig sichtbar. [Details](LIBRARY_AND_DISPLAY.md).

## Cover prüfen und eigene Bilder schützen – neu in 0.14.90

Unter **Fehlende Cover** zeigt EmuVora Einträge ohne vorhandene Bilddatei. Suche, Konsolenfilter und Seitenwechsel bleiben verfügbar. Ein Spiel auswählen und **Cover bearbeiten · X** öffnen; das funktioniert auch bei ausgeblendeter Detailspalte und im TV-Modus mit der Xbox-X-Taste. Eigene Bilder und Online-Treffer erscheinen vor dem Übernehmen neben dem bisherigen Cover. Für die Online-Suche lässt sich ein anderer Suchname eingeben, ohne den Bibliothekstitel zu ändern.

Bestätigte Bilder werden separat kopiert und geschützt. Die Sammelsuche ersetzt geschützte Cover nicht; auch eine fehlende geschützte Datei wird nicht automatisch ersetzt. **Cover-Schutz aufheben** erlaubt den erneuten Abruf fehlender Bilder. Bereits vorhandene Bilder bleiben bei der Sammelsuche erhalten. Frühere manuell gewählte Bilder mit dem bisherigen GUID-Dateinamen erhalten beim ersten Laden Schutz. Abbrechen verändert keine Zuordnung; alte Cover und Originalbilder bleiben erhalten. Lokale Bilder: PNG, JPEG, WebP oder BMP bis 20 MiB, maximal 8192 Pixel je Seite und 32 Megapixel. [Anleitung](LIBRARY_AND_DISPLAY.md).

## Untertitel und passender GB/GBC-Coverkatalog – neu in 0.14.86

Die Suche bewahrt echte Untertitel in Klammern und entfernt nur bekannte Regions-, Sprach- und Versionsangaben. Eine kleine, plattformgebundene Liste bekannter Titelvarianten ergänzt exakte Treffer; andere Fortsetzungen werden nicht geraten. Bei GB/GBC wird der Coverkatalog anhand eines validierten ROM-Headers gewählt, auch bei falsch benannten Dateiendungen oder einzelnen ROMs in ZIPs. Gespeicherte Konsole, Titel, ROM-Dateien und vorhandene Cover bleiben erhalten. Ungültige Header und Mehrspiel-ZIPs lösen keinen Katalogwechsel aus.

## Verbesserte Cover-Erkennung – neu in 0.14.81

Zusätzliche Suchvarianten entfernen Sammlungsnummern und Release-Zusätze wie `_USA`. Explizite regionale Alternativtitel (`~`) sowie vollständige Wortgruppen mit römischen Ziffern oder abweichender Reihenfolge werden nur bei eindeutigem Katalogtitel übernommen; ähnliche Namen und andere Fortsetzungen bleiben ausgeschlossen. Gespeicherte Spielnamen und vorhandene Cover werden nicht geändert. Bei abgeschnittenen großen GitHub-Katalogen wird der vollständige Boxart-Unterordner separat geladen. Nicht jedes Spiel oder Zusatzmaterial besitzt ein Cover in der Quelle.

## Neues Logo und Bibliotheksdesign

EmuVora verwendet ein eigenes mintfarbenes App-Zeichen, ruhigere Navigation, überarbeitete Cover-Karten und eine klar gegliederte Detailansicht. Helles und dunkles Design sowie TV-Modus bleiben verfügbar. Der Einrichtungsassistent und erneutes Scannen liegen unter **Verwaltung**. [Vorschau, Gestaltung und Prüfungen](https://github.com/cpprograming/EmuVora/blob/main/design/README.md).

## Spielnamen anhand von Prüfsummen – neu in 0.14

Unter **Einstellungen → Bibliothek → Spielnamen per Prüfsumme erkennen** Katalog für eines von 16 Cartridge-Systemen laden und Vorschläge prüfen. Nur bestätigte, eindeutige Treffer ändern unverändert importierte Bibliothekstitel. Eigene Bearbeitungen bleiben geschützt; Dateinamen, Favoriten, Verlauf und Cover bleiben erhalten. [Anleitung, Systeme und Grenzen](ROM_RECOGNITION.md).

## Oberfläche und TV-Modus – neu in 0.13

FluentAvalonia sorgt für eine gemeinsame Basis der Bedienelemente. Unter **Einstellungen → Darstellung** den optionalen **TV-Modus** einschalten: größere Cover und Bedienelemente, umbrechende Beschriftungen und sichtbare Spielauswahl. Die Controller-Navigation berücksichtigt die tatsächlichen Cover-Spalten. [Anleitung und Grenzen](TV_MODE.md).

## BIOS-Import verbessert in 0.11.59

Die Dateiauswahl unterstützt Doppelklick und einen stets sichtbaren Übernehmen-Knopf. Bekannte BIOS-Dateien werden offline anhand von Prüfsummen erkannt und beim Kopieren automatisch passend benannt. Originale bleiben erhalten; eine Ergebnisübersicht zeigt Zielnamen und Fehler. [Anleitung und Grenzen](BIOS_IMPORT.md).

## Erste Einrichtung

1. **Speicherort festlegen.** Das Setup legt Programm und Daten standardmäßig in den Unterordnern `App` und `Data` des gewählten Installationsordners ab. Bei einem portablen Erststart ohne gespeicherten Datenpfad wählst du den Datenordner selbst.
2. **Einrichtungsassistent durchlaufen.** Er führt durch Speicherort, ROM-Import, Controller, Emulatoren/BIOS und Abschluss. Er lässt sich später über **Verwaltung → Einrichtungsassistent** erneut öffnen.
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

Neu in 0.12: **Controller prüfen** unter Einstellungen → Emulatoren oder im Einrichtungsassistenten zeigt Tasten, beide Sticks, Trigger und Verbindungswechsel für bis zu vier XInput-Slots. Bibliothek und Dialoge unterdrücken gehaltene Eingaben nach Verbindungs-/Fokuswechseln; die Navigation wechselt bei mehreren Controllern erst nach Freigabe des aktiven Controllers. Details und noch offene Hardwaretests: [Controller-Anleitung](CONTROLLERS.md).

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

## Eigene Themes und automatischer Import

EmuVora verwendet seine eigene Oberfläche mit einheitlicher Konsolenleiste. Die nativen Basis-Themes Aurora und Studio ergänzen die bisherigen Stile. Der separate ES-DE-Start, Autostart und die Download-Schaltflächen sind entfernt; bereits vorhandene Daten werden nicht gelöscht.

Über **Einstellungen → Darstellung → ES-DE-/EmulationStation-Theme anpassen …** lassen sich Hintergründe, Logos und Farben in das native Layout übernehmen. Varianten und Hell-/Dunkel-Stile werden automatisch gewählt; Videos, Sounds und vollständige Fremd-Layouts werden nicht identisch nachgebaut. Der Importbericht nennt Grenzen und behält Lizenzhinweise bei. [Anleitung zu nativen Themes, Versionen und BIOS-Auswahl](NATIVE_THEMES.md).

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

**Für Release 0.14.76 bestanden:** 166 Funktionstests und 6 Netzwerktests sowie die Installer-Prüfungen für Installation, Update, Prozessübergabe und Datenerhalt. Der veröffentlichte Setup-Download wurde zusätzlich mit dem EmuVora-Updater geprüft.

Die Tests decken unter anderem Bibliothek, Controller-Oberfläche, Sicherungsintegrität, externe Ordner, Wiederherstellung älterer Archive und Dateipfade ab. Avalonia-Tests erzeugen UI-Aufnahmen; die GitHub-Artefakte enthalten **EmuVora-UI-previews**. Echte Hardwaretests stehen separat auf der Checkliste.

## Nächste Schritte

Die [Checkliste](CHECKLIST.md) hält erledigte Funktionen und offene Arbeiten fest. Die [Roadmap](ROADMAP.md) beschreibt die Etappen bis 1.0. Xbox-Hardwaretests bleiben offen. Der Softwareteil für Eingabewechsel und Diagnose ist ab 0.12.64 verfügbar; FluentAvalonia und TV-Modus sind ab 0.13.66 verfügbar. Prüfsummenbasierte Spielerkennung ist ab 0.14.68 verfügbar. Die neue Gestaltung ist ab 0.14.71 verfügbar; Windows-Build, Installation, Update und Datenerhalt sind geprüft. SDL/GameControllerDB unterstützt ab 0.14.76 weitere Controller in der Oberfläche. Als nächste Schritte folgen automatische Spielprofile für zusätzliche Modelle, Hardwaretests und zusätzliche Bibliotheksfunktionen. Geprüfte externe Projekte stehen im [Repository-Plan](REPOSITORY_PLAN.md).

## Entwicklung unterstützen

EmuVora bleibt kostenlos erhältlich. Freiwillige Unterstützung über GitHub Sponsors ist vorbereitet; der Antrag ist eingereicht und wartet auf GitHubs Freigabe. Es gibt keine kostenpflichtigen Funktionen oder exklusiven Downloads. [Einrichtung und Profilentwurf](SPONSORS.md).

## Lizenz

Der EmuVora-Code steht unter [MIT](LICENSE). Abhängigkeiten, Emulatoren, Themes und Cover unterliegen ihren eigenen Bedingungen; siehe [Drittanbieter-Hinweise](THIRD_PARTY_NOTICES.md).

Emulator-Binaries, ROMs, BIOS und Theme-Pakete sind nicht im EmuVora-Programmpaket enthalten. Optionale Komponenten werden gesondert von ihren offiziellen Quellen geladen.





