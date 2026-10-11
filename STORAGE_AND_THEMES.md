# Speicherort wählen (v0.4)

`Install-EmuVora.ps1` fragt vor der Installation nach einem Zielordner auf einem beschreibbaren Laufwerk. Darunter entstehen `App` für das Programm und `Data` für Bibliothek, Emulatoren, Cores, Firmware, Cover, Themes und verwaltete Speicherstände.

```powershell
pwsh -File .\Install-EmuVora.ps1 -DestinationPath 'D:\EmuVora' -DesktopShortcut
```

Mit `-DataPath 'E:\EmuVora-Daten'` kann der Datenordner separat gewählt oder eine vorhandene Bibliothek übernommen werden. Zum Starten ohne Terminal **Install-EmuVora.cmd doppelklicken**. Das Skript unterstützt Windows PowerShell 5.1 und PowerShell 7. Bisherige Programmdateien bleiben bei Updates als Sicherung erhalten. Daten und Programme müssen in getrennten Ordnern liegen.

Bei direktem Start der entpackten EXE fragt EmuVora beim ersten Start nach dem Datenordner. Die EXE bleibt in ihrem entpackten Ordner. Der kleine Pfadverweis liegt unter `%LOCALAPPDATA%\EmuVora\location.json`; installierte Pakete nutzen `App\emuvora-location.json`. Ein vorhandener Datenordner kann ausgewählt werden, standardmäßig wird die frühere Bibliothek vorgeschlagen. Ein anderer neuer Ordner beginnt eine neue Bibliothek. Es erfolgt keine automatische Migration. Eigene externe Emulatoren behalten ihre eigenen Pfade. ROMs bleiben in den importierten Ordnern. Windows und Emulatoren können unabhängig davon temporäre Dateien im Windows-Temp-Verzeichnis anlegen.

Ist ein ausgewähltes Laufwerk beim Start nicht erreichbar, erscheint die Pfadauswahl mit dem bisherigen Pfad und einer Fehlermeldung. EmuVora erstellt keine neue Ersatzbibliothek. Unter Einstellungen ist der aktive Datenordner sichtbar und kann geöffnet werden. Zum Wechseln bei geschlossenem Programm die entsprechende Locator-Datei umbenennen und neu starten; der bisherige Datenordner bleibt erhalten.

# Klassische EmulationStation-Themes

Unter **Emulatoren einrichten → EmulationStation-Theme importieren** den vollständig entpackten Theme-Set-Ordner wählen. Er enthält `theme.xml` oder Konsolen-Unterordner wie `nes/theme.xml`, `snes/theme.xml`, `psx/theme.xml`. Der Import kopiert den Ordner in den gewählten Datenordner; die Quelle wird danach nicht benötigt. XML-Includes, gemeinsame Dateien innerhalb des Theme-Sets, Variablen und mehrere View-/Elementnamen werden gelesen. Listen-/Detailfarben und Hintergrund-/Logo-Bilder (PNG/JPEG/SVG) werden auf EmuVoras Bibliotheksoberfläche übertragen. Konsolenfilter und Spielauswahl wechseln automatisch die Systemgestaltung. **EmuVora-Standarddarstellung verwenden** deaktiviert das Theme.

Dies ist eine Anpassung der Gestaltung, keine vollständige EmulationStation-Layout-Engine: Positionen/Größen, Schriftdateien, zusätzliche Elemente, Animationen, Sounds, Video und View-Umschaltung werden nicht übernommen. ES-DE-Themes mit `capabilities.xml`/Varianten werden mit einem Hinweis abgelehnt. Themes sehen daher nicht pixelgenau wie in EmulationStation aus. Unlesbare/fehlende Ressourcen fallen auf die normale Oberfläche zurück.

Imports erlauben maximal 256 MiB/10.000 Dateien und keine Verknüpfungen, externen XML-Includes oder SVG-Netzwerkressourcen. Formatquelle: https://github.com/Aloshi/EmulationStation/blob/master/THEMES.md. Importierte Themes unterliegen den Lizenzen ihrer Autoren; es werden keine fremden Theme-Bilder mitgeliefert.
