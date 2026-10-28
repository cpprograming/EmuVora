# EmuVora – GUI und Theme-Roadmap

Stand: 28. Oktober 2026. Aufgaben aus der aktuellen Quellcodeprüfung; abgehakte Aufgaben sind umgesetzt, offene Kästchen bezeichnen geplante Arbeiten.

## Ziel

EmuVora ist das einzige Frontend. ES-DE wird weder gestartet noch installiert und ist keine Laufzeitabhängigkeit. EmulationStation-/ES-DE-Themes bleiben als Importformat erhalten. EmuVora rendert unterstützte Layouts selbst und bietet eigene Themes auf derselben technischen Grundlage an.

Der bestehende Import übernimmt Bilder, Logos und Farben und wählt Varianten automatisch. Er rendert derzeit keine vollständigen fremden Layouts, Karussells, Videos, eigenen Schriften oder Animationen. Die folgenden Aufgaben erweitern diesen Umfang schrittweise; vollständige Kompatibilität mit jedem Theme wird nicht versprochen.

## 1. ES-DE als Anwendung vollständig entfernen – Codeumstellung in 0.14.128

- [x] Alten ES-DE-Startpfad, Startfenster, Download-/Installationscode und spezielle Paketfreigaben entfernen.
- [x] Allgemein benötigte Spielkennungen und Konsolen-IDs aus `EsDeFrontend` in unabhängige Hilfsfunktionen überführen. Vorhandene Cover-Dateinamen und Zuordnungen müssen unverändert funktionieren.
- [x] Alte Startoptionen kontrolliert stilllegen; verständlichen Hinweis für veraltete Verknüpfungen anzeigen statt Fehler oder Neuinstallation.
- [x] Einstellungen und Sicherungswiederherstellung von ES-DE-Abhängigkeiten lösen; alte Einstellungen weiterhin sicher einlesen.
- [x] Veraltete ES-DE-Texte aus Coververwaltung, Sicherung, Duplikaten, ROM-Zuordnung und Dokumentation entfernen. Hinweise zum Theme-Import bleiben erhalten.
- [ ] Bestehende ES-DE-Ordner nur erkennen und als entbehrlich kennzeichnen. Persönliche Dateien nicht ungefragt löschen; eine spätere Bereinigung braucht eine konkrete Dateivorschau.
- [x] Regression: alte Bibliothek, Cover, Sicherung, ROM-Zuordnung und direkte Emulatorstarts funktionieren ohne installierte ES-DE-Anwendung.

344 Funktions- und 6 Netzwerktests sowie Setup-/Update-Prüfungen bestanden. Persönliche Restordner werden nicht automatisch gelöscht; deren Vorschau/Bereinigung bleibt separat offen.

## 2. Gemeinsame Layout-Grundlage – Raster/Liste ab 0.14.130

- [x] Gemeinsames Darstellungsmodell für Suche, Filter, Sortierung, Auswahl, Versionen und begrenzte Seiten ergänzen. Vorhandene Start-/Bearbeitungsaktionen werden gemeinsam verwendet.
- [x] Cover-Raster und kompakte Liste mit vorhandener Detailspalte, gespeichertem Layouttyp und sitzungsbezogenen Scrollpositionen anbieten.
- [x] Horizontales Cover-Karussell auf derselben Grundlage ergänzen, Auswahl hervorheben und möglichst zentrieren; Größe der Fensterhöhe anpassen.
- [x] Durchsuchbare Auswahl importierter Konsolen mit großen Logos und Titel-/Dateizahlen anbieten; schnelle Seitenleisten-Navigation beibehalten.
- [ ] Konsolenauswahl um eigene Hintergrundoptionen und weitere native Layoutvarianten erweitern.
- [ ] Seitenleistenbreite, Coverformat, Abstände, Detailposition und Informationsdichte über definierte Layout-Optionen steuern.
- [ ] Responsive Regeln für schmale Fenster, Desktop und TV festlegen; Auswahl, Suchtext und Scrollposition beim Ansichtswechsel erhalten.
- [ ] Gemeinsame Farb-, Abstands-, Schrift- und Fokusvorgaben für alle nativen Themes verwenden.
- [ ] Große Bibliotheken weiterhin begrenzt/virtualisiert darstellen; Cover- und Videospeicher begrenzen.

Raster-/Listenstand: 352 Funktions- und 6 Netzwerktests sowie Setup-/Update-Prüfungen bestanden; private Bibliothekskopie mit 5.805 Einträgen geprüft. Karussell und Theme-Layouts bleiben nächste Ausbaustufen.

Karussell-/Konsolenstand ab 0.14.132: 362 Funktions- und 6 Netzwerktests sowie Setup-/Update-Prüfungen bestanden; private Bibliothekskopie mit 5.805 Einträgen geprüft. Theme-Varianten und XML-Layoutübernahme bleiben nächste Ausbaustufen.

## 3. ES-DE-Theme-Import mit Layouts

- [x] Referenz-Theme Alekfull NX anhand seiner festgehaltenen XML-Varianten inventarisieren und eine Unterstützungsmatrix erstellen.
- [x] Variante, Farbschema und Vorlagen-Seitenverhältnis vor dem Import auswählbar machen; Auswahl in der importierten Kopie speichern.
- [ ] XML zunächst in ein unabhängiges Theme-Modell übersetzen; Parsing und Rendering getrennt halten.
- [ ] Unterstützte Positionen, Größen, Ausrichtung, Sichtbarkeit, Ebenen und Bild-Skalierung auf die native Layout-Engine abbilden.
- [ ] System-/Spieleansichten, Listen, Raster und Karussells schrittweise unterstützen; gemeinsame Bedienfunktionen in jeder Ansicht erhalten.
- [ ] Metadatenbereiche, vorhandene lokale Medien und Platzhalter zuordnen. Fehlende Medien dürfen keine leeren oder unbedienbaren Ansichten erzeugen.
- [ ] Geeignete lokale Schriften, Übergänge und anschließend optionale Video-/Audioelemente mit Leistungsgrenzen ergänzen.
- [x] Geprüfte native Vorschau mit Beispieldaten und Hinweisen vor Übernahme anbieten; Abbruch erhält die bisherige Darstellung, Auswahländerungen erfordern erneute Prüfung.
- [ ] Vorschau bei Auswahländerungen automatisch aktualisieren und weitere fremde Layouteigenschaften darstellen.
- [ ] Importbericht je Element: übernommen, angenähert oder nicht unterstützt. Bei ungeeigneten Layouts funktionsfähige native Ersatzansicht verwenden.
- [ ] Vorhandene Pfad-, XML-, Größen- und Abbruchprüfungen erhalten; keine ausführbaren Theme-Skripte zulassen. Herkunft und Lizenzdateien erhalten.

## 4. Sichtbare GUI-Verbesserungen

- [ ] Startansicht mit zuletzt gespielten Titeln, Favoriten und Sammlungen; Module ein-/ausblendbar machen.
- [ ] Konsolenlogos, Kartengrößen, Textzeilen und Auswahlrahmen in jeder Ansicht einheitlich ausrichten.
- [ ] Spieldetails klar ordnen: Spielen, Version wählen, Favorit/Sammlung und Cover bearbeiten; technische Angaben in einen aufklappbaren Bereich verschieben.
- [ ] Such-/Filterleiste mit erkennbaren aktiven Filtern und direktem Zurücksetzen vereinheitlichen.
- [ ] Einstellungen übersichtlicher gliedern; erweiterte Optionen getrennt anzeigen und Änderungen verständlich bestätigen.
- [ ] Verbleibende Spezialdialoge auf gemeinsame Vorschau, feste Aktionen, Validierung und sichtbare Fehlerhinweise umstellen.
- [ ] Leere Bibliothek, fehlende Cover, fehlende Medien, Downloads und nicht erreichbare Laufwerke gestalterisch abstimmen.
- [ ] Klar sichtbaren Tastatur-/Controller-Fokus und kontextabhängige Tastenhinweise in allen Layouts anbieten.
- [ ] Kontrast, lange Titel/Pfade, Textskalierung und reduzierte Animationen berücksichtigen.

## 5. Eigene Themes und Prüfung

- [ ] Erstes eigenes Layout auf der neuen Engine ausarbeiten: ruhige Desktop-Darstellung mit großen Covern und klaren Details.
- [ ] Zweites eigenes Layout: Sofa-/TV-Ansicht mit Konsolenauswahl und Karussell.
- [ ] Native Themes und importierte Themes über dieselbe Vorschau und Layout-Auswahl verwalten; sichere Standarddarstellung jederzeit erreichbar halten.
- [ ] Automatisierte Prüfungen für Layoutwechsel, Suche/Filter, Versionen, Fokus, Abbruch und unveränderte Bibliotheksdaten ergänzen.
- [ ] Bildschirmvorschauen für helle/dunkle Themes, schmale Fenster, große Schrift und TV-Modus visuell kontrollieren.
- [ ] Alekfull-NX-Referenzansichten gegen die Unterstützungsmatrix prüfen; Abweichungen konkret dokumentieren.
- [ ] Echte Controller-, Monitor-, Video- und Leistungstests wie vereinbart später separat durchführen; nicht durch Screenshot-Tests als erledigt markieren.

## Reihenfolge der nächsten Arbeitspakete

1. ES-DE-Code entkoppeln und entfernen, Datenerhalt prüfen.
2. Gemeinsame Layout-Engine und native Listen-/Rasterauswahl liefern.
3. Konsolenauswahl und Cover-Karussell ergänzen.
4. Theme-Varianten mit Vorschau und erste echte XML-Layoutübernahme für Alekfull NX liefern.
5. Weitere GUI-Bereiche, eigene Themes und optionale Medien ausbauen.

Jedes Paket erhält eine überprüfbare Veröffentlichung. Die gesamte Liste ist kein einzelner kleiner Import-Fix; Layout-Engine und Theme-Kompatibilität werden an konkreten Referenzansichten abgenommen.
