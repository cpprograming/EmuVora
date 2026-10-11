# Windows-Setup-EXE

`EmuVora-Setup-v0.5.exe` enthält das vollständige portable Windows-Paket. Der Dialog bietet den Installationspfad, eigene EmuVora-Verknüpfungen auf Desktop und im Startmenü und einen anschließenden Programmstart an. Das Programm liegt unter `App`, die Daten unter `Data`. Der vorhandene PowerShell-Installer erhält bei Updates auch separat konfigurierte Datenordner. Das eigene Icon ist im Setup und in beiden Verknüpfungen enthalten. Das Setup benötigt keine Administratorrechte und ist nicht digital signiert.

Erstellen unter Windows:

```powershell
./installer/build-setup.ps1 -PackageZip C:\Build\EmuVora-v0.5-Windows.zip -OutputPath C:\Build\EmuVora-Setup-v0.5.exe
```

Automatisierte Testinstallation ohne Verknüpfungen oder Programmstart:

```powershell
EmuVora-Setup-v0.5.exe --install-dir "C:\Test\EmuVora" --no-shortcuts --no-launch
```

Exitcode 0 bedeutet Erfolg oder interaktiven Abbruch, 1 einen Fehler. Im automatisierten Modus werden Fehler neben der Setup-EXE in `EmuVora-setup-error.log` geschrieben. Temporär entpackte Dateien werden nach dem Vorgang entfernt. Geprüft wurden die Installation in einen Pfad mit Leerzeichen und `&`, eine erneute Installation mit erhaltenen Daten sowie Ziel und eigenes Icon der beiden Verknüpfungen in isolierten Testordnern.
