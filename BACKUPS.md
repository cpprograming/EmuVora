# Private Datensicherung (0.9.0)

Unter **Einstellungen → Sicherung** eine Datensicherung erstellen. Als Ziel einen Ordner außerhalb des aktiven EmuVora-Datenordners wählen, möglichst auf einem anderen Laufwerk. Bestehende Sicherungsdateien werden nicht ersetzt. Spiele und ES-DE vorher beenden; unabhängig gestartete Emulatoren ebenfalls selbst schließen.

Enthalten sind settings.json, eine konsistente SQLite-Kopie der Bibliothek (Favoriten und Verlauf eingeschlossen), covers, saves und states im Datenordner. Bei innerhalb des Datenordners konfigurierten Emulatoren zusätzlich:

- DuckStation: memcards, savestates
- PCSX2: memcards, sstates
- Dolphin: User/GC, User/Wii, User/StateSaves
- PPSSPP: memstick/PSP/SAVEDATA, memstick/PSP/PPSSPP_STATE

Nicht enthalten sind Emulatorprogramme, Themes, Firmware und ROM-Verzeichnisse. Externe oder selbst umkonfigurierte Speicherorte sind separat zu sichern. Dateien in den aufgeführten Speicherordnern werden vollständig kopiert. Die Funktion sucht nicht auf anderen Laufwerken nach Spielständen. Eine Sicherung ist auf 100.000 Dateien und 20 GiB unkomprimierte Daten begrenzt; Dateisystem-Verknüpfungen werden abgewiesen.

Zum Wiederherstellen das Archiv auswählen, Datum/Dateianzahl/Größe prüfen und einen Zielordner wählen. Die Wiederherstellung erzeugt einen neuen Unterordner. SHA-256 wird für jede Datei geprüft, anschließend die SQLite-Datenbank. Abbruch oder Prüfungsfehler verwerfen den unvollständigen neuen Ordner; die aktive Bibliothek bleibt erhalten. Prüfsummen erkennen Beschädigungen, beweisen aber nicht die Vertrauenswürdigkeit einer fremden Sicherung.

Nach Erfolg kann der wiederhergestellte Ordner geöffnet oder EmuVora ausdrücklich mit dieser Bibliothek neu gestartet werden. Dieser Neustart speichert den neuen Datenpfad. Pfade zu ROMs bleiben unverändert; interne Cover- und Emulatorpfade werden auf den neuen Datenordner umgestellt. Emulatorprogramme, Cores, Themes und BIOS müssen gegebenenfalls erneut eingerichtet werden. Die zuvor gesicherten emulator-eigenen Speicherordner bleiben unter emulators erhalten. Ab 0.9.1 übernimmt die nächste automatische Einrichtung von DuckStation, PCSX2, Dolphin oder PPSSPP ihre erkannten Spielstände in den neuen verwalteten Emulatorordner. Vorhandene Zieldateien werden nie ersetzt. Die gesicherte Quelle bleibt erhalten; ein Bericht unter Einstellungen → Sicherung → Spielstandsbericht öffnen nennt übernommene und erhaltene Dateien. Benutzerdefinierte externe Speicherorte müssen weiterhin separat übernommen werden. RetroArch nutzt nach automatischer Einrichtung wieder saves und states im neuen Datenordner.

Der bisherige JSON-Export unter Verwaltung bleibt für den reinen Austausch von Bibliothekseinträgen verfügbar; er enthält keine Cover- oder Spielstandsdateien.
