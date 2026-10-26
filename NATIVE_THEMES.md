# Native Oberfläche und angepasste Themes

EmuVora verwendet seine eigene Oberfläche. Die ES-DE-Anwendung, ihr Start-/Exportcode und ihre Download-Unterstützung sind entfernt. Alte Startverweise zeigen nur einen Hinweis zum direkten Öffnen von EmuVora. Alte Einstellungen werden eingelesen, ohne ES-DE zu aktivieren. Bereits vorhandene Ordner und darin referenzierte Cover oder Themes bleiben erhalten.

Unter **Einstellungen → Darstellung** stehen die eigenen Basis-Themes **Midnight**, **Aurora**, **Studio**, **Classic Light**, **Retro Green** und **Neon Purple** bereit. Sie behalten Suche, Cover-Raster, Versionen, Sammlungen und Controller-Bedienung in einer gemeinsamen Oberfläche. Die Konsolenleiste reserviert für jede Zeile denselben Platz für Logo und Anzahl; Auswahlrahmen verändern die Breite nicht. Titelzeilen der Cover-Karten haben eine einheitliche Höhe.

## EmulationStation-/ES-DE-Theme anpassen

Den entpackten Theme-Ordner über **ES-DE-/EmulationStation-Theme anpassen …** auswählen. Das Original bleibt unverändert. EmuVora kopiert das Theme in den Datenordner und übernimmt unterstützte Hintergründe, Konsolenlogos und Listen-/Detailfarben. Bei ES-DE-Themes mit `capabilities.xml` wird die erste auswählbare Variante, ein zum hellen/dunklen Basisstil passendes Farbschema und bevorzugt die 16:9-Vorlage gewählt. Diese Vorlage wird in das native Layout eingepasst; sie verändert nicht die Windows-Auflösung.

Es ist eine Anpassung, keine vollständige ES-DE-Emulation. Videos, Sounds, eigene Schriftarten, Animationen, Karussells und Speziallayouts werden nicht nachgebaut. Dynamische Eigenschaften und fehlende optionale Includes können ausgelassen werden. Klassische Theme-Elemente und typische ES-DE-Namen werden auf Hintergrund, Logo und Textfarben abgebildet; unbekannte Elemente erscheinen im **Theme-Importbericht**. Dieser liegt als `emuvora-import-report.txt` im importierten Ordner und lässt sich in den Darstellungseinstellungen öffnen. Nicht jedes Theme lässt sich sinnvoll übertragen.

XML-Entitäten, externe Ressourcen, Pfade außerhalb des Theme-Ordners, Verknüpfungen und Include-Zyklen werden abgewiesen. Grenzen: 256 MiB und 10.000 Dateien. Ein Abbruch verhindert die Aktivierung eines unvollständigen Imports. Lizenz- und Herkunftsdateien werden mitkopiert; die Nutzungsbedingungen des jeweiligen Theme-Autors gelten weiterhin. Mit **EmuVora-Standarddarstellung verwenden** wird die externe Anpassung abgeschaltet; der gewählte native Basisstil bleibt aktiv.

Alekfull NX wird anhand seiner festgehaltenen Original-XML mit den automatischen Varianten und Farben geprüft. Seine Originaldarstellung wird dabei nicht identisch nachgebaut. Quelle und Lizenzhinweise: [Alekfull NX ES-DE](https://github.com/anthonycaccese/alekfull-nx-es-de).

## Doppelte Karten und Versionen

Gleiche Titel innerhalb derselben Konsole erscheinen standardmäßig als eine Karte mit **N Versionen**. Regions-/Editionszusätze werden nicht entfernt; andere Konsolen bleiben getrennt. Das ist eine reine Anzeigegruppierung, kein Inhaltsnachweis. Vor dem Spielen kann die konkrete Datei gewählt werden; **Version auswählen** wechselt die aktive Datei auch zum Bearbeiten. Favoriten, Cover, Verlauf und Sammlungen bleiben dateibezogen erhalten. Bibliotheksfilter gelten vor der Gruppierung, beispielsweise zeigt Favoriten nur favorisierte Versionen. Unter **Einstellungen → Bibliothek** lässt sich die Gruppierung abschalten. Für echte identische Dateien bleibt die bestätigte SHA-256-Zusammenführung verfügbar.

## PS2-BIOS-Auswahl

Die automatische Auswahl prüft nun die ROMDIR-/ROMVER-Struktur eines 4–8-MiB-BIOS-Abbilds. Eine gleich große DIFF-/ROM1-Begleitdatei genügt nicht. Diese Strukturprüfung ist kein Echtheitsnachweis und liefert keine BIOS-Dateien. Vorhandene gültige eigene ROM0-/BIOS-Abbilder verwenden; BIOS und Spielstände werden nicht umbenannt oder gelöscht. Bei einem Emulatorabsturz werden Name, hexadezimaler Fehlercode und bei PCSX2 der bekannte Protokollpfad angezeigt. Die Auswahl orientiert sich am [PCSX2-BIOS-Format](https://github.com/PCSX2/pcsx2/blob/master/pcsx2/ps2/BiosTools.cpp).
