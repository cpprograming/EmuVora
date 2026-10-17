# Zwischen Bibliotheken wechseln (ab 0.11)

Unter **Einstellungen → Bibliothek → Bibliothek wechseln** zeigt EmuVora den aktuellen und gegebenenfalls den vorherigen Datenordner.

1. Laufende Spiele, ES-DE und andere Vorgänge beenden.
2. **Vorhandenen Datenordner wählen** öffnen und einen bestehenden EmuVora-Datenordner auswählen. Darin müssen settings.json und library.db liegen; der Programmordner App ist nicht das richtige Ziel.
3. EmuVora prüft Einstellungen und Datenbank und zeigt den Zielpfad und die Spieleanzahl.
4. Mit **Wechseln und neu starten** ausdrücklich bestätigen. Abbrechen ändert den gespeicherten Datenpfad nicht.

Es werden keine ROMs, Spielstände oder Bibliotheksdateien zwischen Ordnern verschoben. Die gewählte Bibliothek behält ihre eigenen Einstellungen, Cover und Spielpfade. Leere, nicht erreichbare oder nicht unterstützte Bibliotheken werden abgewiesen; ein fehlendes Laufwerk wird nicht durch einen neuen leeren Ordner ersetzt.

## Zurückkehren

Nach einem erfolgreichen Wechsel bietet dieselbe Einstellungsseite **Zur vorherigen Bibliothek zurück** an. Der vorherige Ordner wird erneut geprüft und der Wechsel bestätigt. Ist sein Laufwerk nicht verbunden, bleibt die aktuelle Bibliothek geöffnet. Es wird jeweils der unmittelbar zuvor geöffnete Ordner gespeichert; bei der Rückkehr lässt sich anschließend wieder in die andere Richtung wechseln.

Der Rückweg liegt als app-eigene Datei data-location-history.json im gewählten Datenordner und bleibt bei einem normalen App-Update erhalten. Er wird auch gespeichert, wenn eine wiederhergestellte Bibliothek über Einstellungen → Sicherung geöffnet wird. Schlägt das Starten des neuen Prozesses fehl, setzt EmuVora Datenpfad und Rückweg samt vorherigen Metadaten-Sicherungen zurück. Ein späterer Fehler während des Starts der neuen App wird durch die bestehende Speicherort-Auswahl angezeigt.

Dieser Wechsel öffnet vorhandene Bibliotheken. Einen neuen Datenordner anlegen oder den Inhalt einer Bibliothek umziehen ist ein anderer Vorgang; vorhandene Sicherungen lassen sich zunächst in einen neuen Ordner wiederherstellen. Ein ausdrücklich angegebener CLI-Parameter --data-dir hat beim Programmstart weiterhin Vorrang vor dem gespeicherten Datenpfad.

## Sicherungserinnerung

Unter **Einstellungen → Sicherung → Sicherungserinnerung beim Programmstart** kann die Erinnerung auf 7, 14 oder 30 Tage gestellt werden. Standardmäßig ist sie ausgeschaltet. Bei Fälligkeit erscheint in der Seitenleiste **Datensicherung fällig**; der Button öffnet die Sicherungsseite. Es gibt keine Hintergrund-Benachrichtigungen bei geschlossener App.

Nur eine erfolgreich erstellte Datensicherung aktualisiert den Sicherungszeitpunkt. Bei einer Wiederherstellung wird das Erstellungsdatum des geprüften Archivs verwendet. Eine Erinnerung erstellt keine Sicherung und löscht keine alten Archive.
