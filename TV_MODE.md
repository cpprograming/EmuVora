# Oberfläche und TV-Modus

Unter **Einstellungen → Darstellung → TV-Modus · größere Cover und Bedienelemente** einschalten. Die Einstellung wird gespeichert und ist standardmäßig aus. Zum Abschalten denselben Weg verwenden; Menu am Xbox-Controller öffnet die Einstellungen, LB/RB wechselt den Tab, A schaltet die Option um.

Im TV-Modus sind Cover 260 Pixel breit, Schaltflächen mindestens 48 Pixel hoch und wichtige Texte größer. Die rechte Detailspalte wird ausgeblendet, damit die Bibliothek mehr Platz hat. Deine Desktop-Covergröße und die Einstellung der Detailspalte bleiben gespeichert und gelten wieder nach dem Ausschalten. Die Detailspalte benötigt im Desktop-Modus mindestens 1120 Pixel Fensterbreite. Der TV-Modus verändert weder die Windows-Auflösung noch Emulator-Bildprofile und erzwingt keinen Vollbildmodus.

Neu geöffnete Dialoge übernehmen die größeren Bedienelemente. Lange Schaltflächentexte umbrechen; die Filter stehen unter der Suche. Ausgewählte Spiele sind blau umrandet. D-Pad hoch/runter wechselt entsprechend der tatsächlich sichtbaren Spaltenzahl die Reihe, auch bei geänderter Covergröße.

Die Standardbedienelemente nutzen FluentAvaloniaUI 2.5.1 für unsere Avalonia-11-Basis. Die vier EmuVora-Farbschemata bleiben wählbar. Klassische importierte EmulationStation-Elemente behalten ihre bisherigen Grenzen; ES-DE/AlekFull läuft weiterhin separat. Details: [Themes](STORAGE_AND_THEMES.md), [ES-DE](ALEKFULL_ESDE.md).

Automatische UI-Prüfungen decken alle vier Farbschemata, TV-Umschaltung, Dialogübernahme, leere/ergebnislose Bibliotheken und Controller-Raster ab. Aufnahmen verwenden synthetische Bibliothekseinträge. Praktische Lesbarkeit und Bildskalierung auf echten Fernsehern, verschiedenen DPI-Einstellungen und Monitoren müssen weiterhin geprüft werden. Metadaten-Erweiterung, SDL-Eingabebackend und zusätzliche Shader sind nicht Bestandteil dieser Änderung.
