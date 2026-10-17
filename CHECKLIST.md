# EmuVora – Checkliste für die nächsten Schritte

Stand: 17. Oktober 2026. Ziel: ein zuverlässiges Windows-Frontend für den privaten Gebrauch, ohne Pro-Version oder Bezahlsystem.

## Aktueller Stand

- [x] Einrichtungsassistent mit frei wählbarem Datenpfad.
- [x] Spielebibliothek, Konsolenfilter, Favoriten und automatische Cover-Suche.
- [x] Automatische Einrichtung und Updates verwalteter Emulatoren.
- [x] Xbox-Navigation einschließlich Dialogen und Bildschirmtastatur implementiert.
- [x] Monitorerkennung und Profile für Seitenverhältnis und scharfe Skalierung implementiert.
- [x] EmulationStation-Theme-Import und optionaler ES-DE-/AlekFull-Start.
- [x] EmuVora-Selbstupdate und automatische GitHub-Builds eingerichtet.
- [x] Datensicherung mit Prüfsummen und Wiederherstellung in einen neuen Ordner implementiert.
- [x] 107 Funktionstests und 5 Netzwerktests auch im Release-Build 0.10.54 bestanden; Setup-Installation, Update und Datenerhalt geprüft.

Der aktuelle Stand ist als [Release 0.10.54](https://github.com/cpprograming/EmuVora/releases/tag/v0.10.54) veröffentlicht. [PR #11](https://github.com/cpprograming/EmuVora/pull/11) ergänzt zusätzliche Spielstandsordner mit getrennter Wiederherstellung. Automatisierte Tests und UI-Aufnahmen ersetzen keine Tests mit echten Controllern, Monitoren und Spielen.

## 1. Zuerst: Sicherungen vollständig alltagstauglich machen

- [x] Sicherungs-Quellcode und BACKUPS.md über geprüften Pull Request #9 auf GitHub übernommen.
- [x] Release 0.10.54 nach dem Merge veröffentlicht, Setup heruntergeladen und mit dem EmuVora-Updater einschließlich SHA-256 geprüft.
- [x] Erkannte Emulator-Spielstände nach einer Neuinstallation automatisch zuordnen; Wiederherstellung vorher erläutern und vorhandene Dateien schützen (DuckStation, PCSX2, Dolphin, PPSSPP).
- [x] Benutzerdefinierte und externe Spielstandsordner auswählbar sichern; fehlende Orte deutlich anzeigen. Wiederherstellung ausschließlich getrennt im neuen Datenordner (ab 0.10.54).
- [ ] Wiederherstellung auf einem anderen Laufwerk testen: Bibliothek, Cover, Favoriten und echte Spielstände vergleichen.
- [ ] Wechsel zur wiederhergestellten Bibliothek und Rückkehr zum bisherigen Datenordner direkt in der Oberfläche anbieten.
- [ ] Optionale Erinnerung an Sicherungen und Aufbewahrung der letzten Sicherungen ergänzen; nichts ohne ausdrückliche Einstellung löschen.

Abnahme: Ein gesichertes Spiel lässt sich nach Wiederherstellung und erneuter Emulator-Einrichtung mit seinem vorhandenen Spielstand fortsetzen.

## 2. Xbox-Controller und Plug-and-play praktisch prüfen

- [ ] Xbox-Controller per USB und Bluetooth an echter Hardware testen.
- [ ] Alle Hauptabläufe ohne Maus durchspielen: Einrichtung, Import, Einstellungen, Sicherung, Spielstart und Rückkehr.
- [ ] Abziehen und erneutes Verbinden sowie Controllerwechsel während der Nutzung prüfen.
- [ ] Zwei bis vier Controller und korrekte Spielerzuordnung testen.
- [ ] Fokus, sichtbare Tastensymbole und Abbrechen-Verhalten in allen Dialogen vereinheitlichen.

Abnahme: Die üblichen Abläufe funktionieren mit einem Xbox-Controller ohne festhängende Dialoge oder versehentlich doppelte Aktionen.

## 3. Einheitliche Oberfläche und TV-Modus

- [ ] Abstände, Schriften, Farben, Schaltflächen und Dialoge durchgehend vereinheitlichen.
- [ ] Optionalen TV-Modus mit größeren Covern, gut lesbaren Texten und sichtbarem Controllerfokus ergänzen.
- [ ] Leere Bibliothek, fehlende Cover, laufende Downloads und Fehlerzustände verständlich gestalten.
- [ ] Theme-Kompatibilität klar kennzeichnen: direkt unterstützte Elemente gegenüber Themes, die über ES-DE laufen.
- [ ] AlekFull-Darstellung auf echter Hardware mit 16:9, 16:10 und 4:3 prüfen; Theme-Lizenzhinweise erhalten.

Abnahme: Einrichtung, Bibliothek und Einstellungen wirken zusammengehörig und bleiben am Fernseher gut bedienbar.

## 4. Bibliothek und Cover verbessern

- [ ] Eigene Sammlungen und Filter für zuletzt hinzugefügt, zuletzt gespielt und Favoriten ergänzen.
- [ ] Mehrdisc-Spiele, doppelte Einträge und umbenannte ROM-Ordner verständlich verwalten.
- [ ] Cover-Treffer vor einem manuellen Austausch anzeigen und falsche Zuordnungen korrigierbar machen.
- [ ] Fehlende Cover gesammelt anzeigen; bestehende eigene Cover bei erneutem Abruf erhalten.
- [ ] Optionale zusätzliche Metadatenquelle erst nach Prüfung ihrer Bedingungen integrieren.

Abnahme: Eine größere Bibliothek bleibt übersichtlich; eigene Korrekturen überstehen einen erneuten Scan.

## 5. Bildqualität und Stabilität

- [ ] Echte Tests auf 1080p-, 1440p- und 4K-Monitoren sowie mit mehreren Monitoren durchführen.
- [ ] Windows-Anzeigeskalierung und Bildschirmwechsel prüfen.
- [ ] Seitenverhältnis und ganzzahlige Skalierung mit repräsentativen 2D- und 3D-Systemen vergleichen.
- [ ] Systeme mit Maus-/Tastaturbedarf oder besonderen Controllern eindeutig kennzeichnen.
- [ ] Verständlichen Diagnosebericht mit Vorschau und ohne ungefragten Upload ergänzen.
- [ ] Updatefehler, Offlinebetrieb und Rückkehr zur vorherigen Emulatorversion praktisch testen.

Abnahme: Keine verzerrte Darstellung durch das automatische Profil; nachvollziehbare Fehler und zuverlässige Rückkehr in die Bibliothek.

## Vorgehen je Verbesserung

- [ ] Änderung in einem eigenen Pull Request umsetzen und passende Prüfungen ausführen.
- [ ] Checkliste nur bei belegter Erledigung abhaken.
- [ ] Nach dem Merge die automatisch erzeugte EXE, eingebettete Version und Updatefähigkeit prüfen.
- [ ] ROMs, BIOS und persönliche Sicherungen nicht ins Repository hochladen.

Empfohlener nächster Arbeitsschritt: Rückwechsel zum bisherigen Datenordner direkt in der Oberfläche anbieten.

