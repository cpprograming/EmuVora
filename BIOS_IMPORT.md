# BIOS und Firmware importieren

Unter **Einstellungen → Emulatoren** oder im Einrichtungsassistenten **Eigene BIOS-Dateien importieren** öffnen.

1. Laufwerk wählen oder einen Ordner-/Dateipfad eingeben und **Pfad öffnen** drücken.
2. Ordner per Doppelklick oder **Gewählten Ordner öffnen** öffnen.
3. Datei anklicken und **Auswahl übernehmen** drücken. Für mehrere Dateien jede weitere Datei über **Zur Auswahl hinzufügen** markieren; die beim Bestätigen hervorgehobene Datei wird ebenfalls übernommen.
4. Das Ergebnis zeigt für jede kopierte Datei den Zielnamen oder eine Fehlermeldung.

Bekannte BIOS-Inhalte werden anhand von Dateigröße und SHA-1 erkannt, unabhängig vom bisherigen Namen. Eindeutige Treffer erhalten beim Kopieren den erwarteten Dateinamen und gegebenenfalls Unterordner. Die Originaldatei bleibt unverändert. Nicht erkannte Inhalte behalten ihren bisherigen Namen; gleicher Name oder gleiche Größe allein beweisen keine gültige Firmware. Vorhandene Zieldateien werden als `.bak` gesichert.

Der mitgelieferte Erkennungskatalog enthält ausgewählte Signaturen für PS1, Mega-CD, Saturn, Dreamcast, Atari ST/5200/Lynx, Amiga, PC Engine CD, PC-FX, 3DO, ColecoVision und Intellivision. Er ist nicht vollständig, insbesondere werden PS2-Varianten nicht automatisch umbenannt. SHA-1 dient hier der Identifikation bekannter Dateien, nicht dem Sicherheitsnachweis eines Downloads.

Quelle der reinen Erkennungsmetadaten: [libretro-database/System.dat](https://github.com/libretro/libretro-database/blob/master/dat/System.dat), abgerufen am 17. Oktober 2026. Es werden keine BIOS-Inhalte heruntergeladen oder ins Repository aufgenommen. Beim Ordnerimport werden erkannte Dateien ebenfalls umbenannt; nicht erkannte Dateien behalten ihre relative Ordnerstruktur (etwa MSX Machines/Databases).

Eigene BIOS-Dateien separat sichern, vorzugsweise auf einem zweiten Datenträger. Das öffentliche EmuVora-Repository ist kein persönlicher Sicherungsort. Die automatische EmuVora-Datensicherung enthält den Firmware-Ordner nicht.
