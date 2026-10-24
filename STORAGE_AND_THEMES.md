# Speicherort wählen (v0.4)

`Install-EmuVora.ps1` fragt vor der Installation nach einem Zielordner auf einem beschreibbaren Laufwerk. Darunter entstehen `App` für das Programm und `Data` für Bibliothek, Emulatoren, Cores, Firmware, Cover, Themes und verwaltete Speicherstände.

```powershell
pwsh -File .\Install-EmuVora.ps1 -DestinationPath 'D:\EmuVora' -DesktopShortcut
```

Mit `-DataPath 'E:\EmuVora-Daten'` kann der Datenordner separat gewählt oder eine vorhandene Bibliothek übernommen werden. Zum Starten ohne Terminal **Install-EmuVora.cmd doppelklicken**. Das Skript unterstützt Windows PowerShell 5.1 und PowerShell 7. Bisherige Programmdateien bleiben bei Updates als Sicherung erhalten. Daten und Programme müssen in getrennten Ordnern liegen.

Bei direktem Start der entpackten EXE fragt EmuVora beim ersten Start nach dem Datenordner. Die EXE bleibt in ihrem entpackten Ordner. Der kleine Pfadverweis liegt unter `%LOCALAPPDATA%\EmuVora\location.json`; installierte Pakete nutzen `App\emuvora-location.json`. Ein vorhandener Datenordner kann ausgewählt werden, standardmäßig wird die frühere Bibliothek vorgeschlagen. Ein anderer neuer Ordner beginnt eine neue Bibliothek. Es erfolgt keine automatische Migration. Eigene externe Emulatoren behalten ihre eigenen Pfade. ROMs bleiben in den importierten Ordnern. Windows und Emulatoren können unabhängig davon temporäre Dateien im Windows-Temp-Verzeichnis anlegen.

Ist ein ausgewähltes Laufwerk beim Start nicht erreichbar, erscheint die Pfadauswahl mit dem bisherigen Pfad und einer Fehlermeldung. EmuVora erstellt keine neue Ersatzbibliothek. Unter Einstellungen ist der aktive Datenordner sichtbar und kann geöffnet werden. Zum Wechseln bei geschlossenem Programm die entsprechende Locator-Datei umbenennen und neu starten; der bisherige Datenordner bleibt erhalten.

# Native Themes und angepasster Import

Unter **Einstellungen → Darstellung** einen eigenen EmuVora-Stil wählen oder einen vollständig entpackten EmulationStation-/ES-DE-Theme-Ordner anpassen. Unterstützte Hintergründe, Logos und Farben werden in EmuVoras Layout übernommen. Eine ES-DE-Installation ist dafür nicht nötig und wird nicht mehr aus EmuVora gestartet.

Varianten, Basisfarbschema und Vorlagenformat werden automatisch ausgewählt. Speziallayouts, Videos, Sounds und eigene Schriftarten werden nicht vollständig nachgebaut. Ein Importbericht listet nicht übernommene Elemente. Die Quelle bleibt unverändert; Ressourcen und Lizenzhinweise liegen in der verwalteten Kopie. Grenzen und Bedienung: [Native Themes](NATIVE_THEMES.md).
