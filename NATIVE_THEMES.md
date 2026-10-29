# Native Oberfläche und angepasste Themes

EmuVora verwendet seine eigene Oberfläche. Die ES-DE-Anwendung, ihr Start-/Exportcode und ihre Download-Unterstützung sind entfernt. Alte Startverweise zeigen nur einen Hinweis zum direkten Öffnen von EmuVora. Alte Einstellungen werden eingelesen, ohne ES-DE zu aktivieren. Bereits vorhandene Ordner und darin referenzierte Cover oder Themes bleiben erhalten.

Unter **Einstellungen → Darstellung** stehen die eigenen Basis-Themes **Midnight**, **Aurora**, **Studio**, **Classic Light**, **Retro Green** und **Neon Purple** bereit. Sie behalten Suche, Cover-Raster, Versionen, Sammlungen und Controller-Bedienung in einer gemeinsamen Oberfläche. Die Konsolenleiste reserviert für jede Zeile denselben Platz für Logo und Anzahl; Auswahlrahmen verändern die Breite nicht. Titelzeilen der Cover-Karten haben eine einheitliche Höhe.

## Bibliothekslayout wählen

In der Werkzeugleiste unter der Suche zwischen **Cover-Raster** und **Kompakte Liste** wechseln. Die Liste zeigt kleine proportionale Cover, Titel, Konsole, Versionszahl, fehlende Dateien und den bisherigen Spielverlauf. Fehlende Cover erhalten einen kompakten Platzhalter. Auf breiten Desktop-Fenstern kann dieselbe Detailspalte wie beim Raster verwendet werden; im TV-Modus bleiben größere Schrift und Zeilen aktiv. Die Werkzeugleiste bricht bei wenig Platz um.

Beide Ansichten verwenden dieselbe Datenaufbereitung für Suche, Konsolenfilter, Favoriten, Sammlungen, Sortierung und Versionen. Der Wechsel erhält die aktuelle Seite und ausgewählte Datei. Jede Ansicht merkt sich ihre Scrollposition während der Sitzung, sofern Seite und Auswahl gleich geblieben sind. Bei geänderter Auswahl wird diese stattdessen sichtbar gemacht. Der Layouttyp wird gespeichert; Suche, Auswahl und Scrollposition werden nicht dauerhaft über einen Neustart gespeichert.

Controller-Auf/Ab bewegt sich in der Liste jeweils um einen Titel; Seitenwechsel und Spielen mit A, Coververwaltung mit X sowie Favoriten mit Y bleiben erhalten. Über den Bedienmodus (Back/View) ist auch die Layout-Auswahl erreichbar. Maus-Doppelklick und Enter starten weiterhin das ausgewählte Spiel. Es werden höchstens 48 Titel pro Seite bereitgestellt; die Liste erzeugt ihre sichtbaren Zeilen bedarfsweise. Bibliotheksdaten und ROM-Dateien werden durch einen Layoutwechsel nicht verändert.

## Cover-Karussell und Konsolenauswahl

**Cover-Karussell** ist die dritte Auswahl unter der Suche. Die Cover stehen in einer horizontalen Reihe, die ausgewählte Karte ist hervorgehoben und wird möglichst mittig angezeigt. An den Enden einer Seite begrenzt der verfügbare Inhalt die Zentrierung. Links/Rechts am Controller oder an der Tastatur bewegt sich durch die Reihe; auch Auf/Ab bewegt jeweils einen Titel. Seitenwechsel am Rand behalten die Reihenfolge bei. Titel, Versionen, Favoriten, Filter und Spielaktionen verwenden dieselbe Bibliotheksgrundlage wie Raster und Liste. Auf einer neuen Karussellseite ohne bisherige Auswahl wird der erste Titel markiert.

Die Karussell-Covergröße folgt der verfügbaren Fensterhöhe und wird beim Verkleinern neu berechnet. Sehr niedrige Fenster können zusätzlich vertikales Scrollen benötigen. Es bleiben höchstens 48 Titel pro Seite; sichtbare Karten werden bedarfsweise erzeugt. Der Layouttyp wird gespeichert, die bisherigen sitzungsbezogenen Scrollregeln gelten auch für das Karussell.

Über **Konsolen auswählen** in der linken Navigation eine durchsuchbare Übersicht der bereits importierten Systeme mit großen Logos, Titel- und Dateizahlen öffnen. Eine Konsole wählen und **Spiele dieser Konsole anzeigen** bestätigen. Die Darstellung und Sortierung bleiben erhalten; Suchtext und Sammlungsfilter werden für diese Konsolenansicht zurückgesetzt, die Anzeige beginnt auf ihrer ersten Seite. Abbrechen erhält die bisherige Auswahl. Systeme ohne bekannte Logos erhalten ein Symbol; eine leere Bibliothek oder erfolglose Suche kann nicht als Konsole übernommen werden.

Für die zusätzliche Theme-Ansicht gelten die unten beschriebenen Rollen und Grenzen. Weitere fremde Karussell-/Speziallayouts bleiben separat offen.

## EmulationStation-/ES-DE-Theme anpassen

Unter **Einstellungen → Darstellung → ES-DE-/EmulationStation-Theme anpassen …** den entpackten Originalordner außerhalb des verwalteten `themes`-Verzeichnisses auswählen. Variante, Farbschema und Vorlagen-Seitenverhältnis aus den tatsächlich deklarierten Optionen wählen. Nicht auswählbare Varianten werden ausgelassen. Als Vorschlag dienen die erste auswählbare Variante, ein zum Basisstil passendes Farbschema und bevorzugt 16:9. Klassische Themes ohne Optionen bleiben unterstützt; ihre Optionsfelder sind deaktiviert.

1. Die gewünschte Konsole für die Vorschau wählen und optional **Theme-Layout in Vorschau und nach Übernahme verwenden** aktivieren. Ohne diese Option bleibt die Farbanpassung für Raster, Liste oder Karussell erhalten.
2. **Vorschau prüfen** drücken. Mit der Layout-Option verwenden Vorschau und Bibliothek denselben nativen Layout-Adapter.
3. Die Beispieldarstellung und Hinweise prüfen. Der Dialog scrollt zur Vorschau; die Übernahme bleibt unten erreichbar.
4. Änderungen an einer Auswahl verwerfen die vorherige Vorschau und sperren die Übernahme bis zur erneuten Prüfung.
5. Erst **Geprüftes Theme übernehmen** kopiert und aktiviert das Theme. Abbrechen erhält die bisherige Darstellung und verändert die Quelldateien nicht.

Die ausgewählte Kombination wird für alle 47 unterstützten Systeme strukturell geprüft. Vor und nach dieser Prüfung werden Dateiliste und SHA-256-Inhalte verglichen. Die Übernahme prüft die Vorschau erneut und verifiziert die kopierten Dateiinhalte. Geänderte, hinzugefügte oder entfernte Quelldateien erfordern eine neue Vorschau. Diese Strukturprüfung bestätigt keine vollständige Darstellung jedes fremden Elements.

EmuVora übernimmt unterstützte Hintergründe, Konsolenlogos und Listen-/Detailfarben. Die gewählten Optionen werden nur in der importierten Kopie als `emuvora-theme-options.json` gespeichert und beim nächsten Start erneut berücksichtigt. Die Layout-Option öffnet nach der Übernahme die neue Ansicht **Theme-Layout**. Sie kann auch später unter der Suche ausgewählt werden. Wird die Layout-Option bei einer erneuten Übernahme ausgeschaltet, verlässt EmuVora eine zuvor aktive Theme-Ansicht zugunsten des Rasters. Frühere Importe bleiben kompatibel. Neue Sicherungen enthalten die Optionen zusammen mit einem aktiven lokalen Theme im Datenordner.

### Unterstütztes Theme-Layout

Der Adapter übersetzt normalisierte `pos`, `size` beziehungsweise `maxSize`, `origin`, `zIndex`, `opacity` und `visible` für die Spieleliste, Hintergrund, Logo, ausgewähltes Cover, Titel und Beschreibung in ein unabhängiges Layout-Modell. Das gewählte Seitenverhältnis wird innerhalb des Bibliotheksbereichs eingepasst, ohne die Windows-Auflösung zu ändern. Bilder bleiben proportional; der Hintergrund darf zugeschnitten werden. Die vorhandene Detailspalte bleibt über **Details** ein-/ausblendbar, soweit die Fensterbreite dies erlaubt.

Die interaktive Spieleliste verwendet weiterhin EmuVoras begrenzte und virtualisierte Datenansicht, Schriften, Versionen und Spielaktionen. Sie liegt aus Bediengründen über dekorativen Ebenen. Bilder und Texte nehmen keine Klicks entgegen; Toolbar, Suche und Seitenaktionen bleiben außerhalb des Theme-Bereichs erreichbar. Die Beschreibung zeigt die in EmuVora vorhandenen Angaben zu System, Starts und letztem Spielen; fremde Scraper-Beschreibungen werden nicht erfunden.

Bei Logo und Cover berechnet eine Null-Achse in `size` oder `maxSize` die fehlende Größe aus dem tatsächlichen Bildformat. Zwei positive Achsen bilden eine proportionale Begrenzungsbox. Der Ursprung wird auf die berechnete Bildgröße angewendet; beim Spielwechsel wird das Format neu bestimmt. Die Berechnung erfolgt in Pixeln innerhalb des eingepassten Vorlagenformats. Extreme Formate bleiben auf höchstens die doppelte Vorlagenbreite/-höhe begrenzt und werden am Bibliotheksbereich abgeschnitten. Fehlende Cover verwenden einen nativen 2:3-Platzhalter; fehlende Logos erhalten keine neue Grafik. Der Hintergrund darf weiterhin zugeschnitten werden.

Titel und Beschreibung unterstützen `alignment` mit `left`, `center`, `right` und `verticalAlignment` mit `top`, `center`, `bottom`. Ungültige Werte werden auf links/oben zurückgesetzt und im Bericht vermerkt. Schriften und Schriftgrößen bleiben nativ; Listeneinträge verwenden weiterhin die gemeinsame native Gestaltung.

### Statische Theme-Dekorationen

Die Theme-Ansicht und ihre Importvorschau übernehmen bis zu **32 zusätzliche statische Elemente** in XML-Reihenfolge: frei benannte `image`-Elemente mit lokalem `path` und `text`-Elemente mit festem `text`. Sie benötigen explizite gültige `pos` und `size`/`maxSize`. Bilder bleiben proportional und unterstützen eine automatische Null-Achse; Texte verwenden native Schriftgrößen und höchstens 512 Zeichen. Farbe, Ausrichtung, Ursprung, Ebene, Sichtbarkeit und Transparenz werden wie bei den Hauptrollen berücksichtigt. Die Spieleliste bleibt vor dekorativen Ebenen und nimmt weiter Eingaben entgegen; Dekorationen nehmen keine Klicks an.

Nicht zugeordnete Spielmetadaten (`md_*`, `game-*`), ungelöste Variablen, Videos und Animationen werden dadurch nicht als feste Inhalte ausgegeben. Ungeeignete Dekorationen und das Mengenlimit stehen im Importbericht. Fehlende oder unlesbare Zierbilder werden einzeln ausgelassen; Hinweise erscheinen in der Vorschau beziehungsweise im Theme-Status. Bei einer nativen Ersatzliste werden Dekorationen weder dekodiert noch angezeigt. Im Raster, in der Liste und im Karussell bleibt die bisherige Farbanpassung erhalten.

Dekorative Rasterbilder sind auf 16 MiB Dateigröße, 32.768 Pixel pro Quellachse und 64 Megapixel begrenzt; die Dekodierung verkleinert sie in Richtung 1.920 × 1.080, ohne kleine Vorlagen zu vergrößern. Extrem schmale Formate können wegen der Mindestbreite von einem Pixel höher ausfallen; ihre Pixelzahl bleibt begrenzt. SVG-Bilder verwenden den vorhandenen begrenzten Renderer. Die aktive Ansicht hält ausschließlich ihre aktuellen Dekorationsbilder und gibt sie beim Wechsel oder Schließen frei. Herkunfts- und Lizenzdateien bleiben beim Import erhalten.

Fehlende/ungültige Geometrien der Hauptrollen erhalten native Ersatzgrößen; zusätzliche Dekorationen mit solchen Angaben werden ausgelassen. Null-Achsen gelten für Logo, Cover und statische Zierbilder; Texte, Hintergründe und Listen benötigen zwei positive Größen. Das Listenrechteck muss vollständig innerhalb der Vorlage liegen, mindestens 20 Prozent Breite/Höhe und tatsächlich mindestens 180 × 100 logische Pixel bieten. Andernfalls sowie bei fehlendem/beschädigtem Theme wird eine native Ersatzliste verwendet. Kleine Fenster können diese Ersatzansicht auch bei gültiger Vorlage benötigen. Ein Wechsel zu Raster, Liste oder Karussell erhält Auswahl und Seite. Nicht zugeordnete Elemente und Annäherungen stehen im Importbericht.

Die Anpassung umfasst die beschriebenen Rollen und Eigenschaften. Videowiedergabe, Sounds, eigene Schriftarten, Animationen und fremde Karussell-/Speziallayouts bleiben offen. Dynamische Eigenschaften und fehlende optionale Includes können ausgelassen werden. Klassische Theme-Elemente und typische ES-DE-Namen werden auf Hintergrund, Logo und Textfarben abgebildet; unbekannte Elemente erscheinen im **Theme-Importbericht**. Dieser liegt als `emuvora-import-report.txt` im importierten Ordner und lässt sich in den Darstellungseinstellungen öffnen. Nicht jedes Theme lässt sich sinnvoll übertragen.

XML-Entitäten, externe Ressourcen, Pfade außerhalb des Theme-Ordners, Verknüpfungen und Include-Zyklen werden abgewiesen. Grenzen: 256 MiB und 10.000 Dateien. Ein Abbruch verhindert die Aktivierung eines unvollständigen Imports. Lizenz- und Herkunftsdateien werden mitkopiert; die Nutzungsbedingungen des jeweiligen Theme-Autors gelten weiterhin. Mit **EmuVora-Standarddarstellung verwenden** wird die externe Anpassung abgeschaltet; der gewählte native Basisstil bleibt aktiv.

### Referenz: Alekfull NX

Geprüfter Referenzstand: `645b2bcc2248e93af8f0f4461a6ee7367fb89365`. [Deklarierte Optionen](https://github.com/anthonycaccese/alekfull-nx-es-de/blob/645b2bcc2248e93af8f0f4461a6ee7367fb89365/capabilities.xml).

| Eigenschaft | Aktueller Umfang |
| --- | --- |
| Vier auswählbare Varianten | Auswählbar; nicht auswählbares `basic` bleibt verborgen. Unterstützte Elemente der Variante werden gelesen. |
| Vier Farbschemata | Auswählbar und gespeichert; originale helle und dunkle XML-Farben automatisiert geprüft. |
| Vier Vorlagenformate | Deklarierte Formate auswählbar; zugehörige Includes werden gelesen, die Monitorauflösung bleibt erhalten. |
| Hintergrund, Logo, Textfarben | In der nativen Vorschau und Oberfläche für unterstützte Elemente angepasst. |
| Positionen, Größen, Ursprung, Ebenen, Sichtbarkeit, Transparenz | Für die oben genannten Rollen angepasst; das originale 16:9-Listenrechteck wird automatisiert geprüft. |
| Originales `game-media`-Videoelement | Als statisches ausgewähltes Spielcover angenähert; keine Videowiedergabe. |
| Textausrichtung, automatische Bildachsen | Für Titel/Beschreibung sowie proportionale Logos/Cover wie oben beschrieben angepasst. |
| Statische Dekorationen | Bis zu 32 explizit positionierte lokale Bilder und feste Beschriftungen; Grenzen siehe oben. |
| Dynamische Dekorationen, zusätzliche Bild-Skalierung, eigene Listen-/Karussellanordnung | Noch keine vollständige XML-Layoutübernahme. |
| Medienabhängige Varianten-Overrides, eigene Schriften, Videos, Sounds, Animationen | Noch nicht dargestellt bzw. ausgewertet; statische Cover-Ausnahme siehe oben. |

Originalassets werden für die Referenzprüfung nicht mit EmuVora verteilt. Beim lokalen Import bleiben Herkunfts- und Lizenzdateien erhalten; Quelle und Lizenzhinweise: [Alekfull NX ES-DE](https://github.com/anthonycaccese/alekfull-nx-es-de).

## Doppelte Karten und Versionen

Gleiche Titel innerhalb derselben Konsole erscheinen standardmäßig als eine Karte mit **N Versionen**. Regions-/Editionszusätze werden nicht entfernt; andere Konsolen bleiben getrennt. Das ist eine reine Anzeigegruppierung, kein Inhaltsnachweis. Vor dem Spielen kann die konkrete Datei gewählt werden; **Version auswählen** wechselt die aktive Datei auch zum Bearbeiten. Favoriten, Cover, Verlauf und Sammlungen bleiben dateibezogen erhalten. Bibliotheksfilter gelten vor der Gruppierung, beispielsweise zeigt Favoriten nur favorisierte Versionen. Unter **Einstellungen → Bibliothek** lässt sich die Gruppierung abschalten. Für echte identische Dateien bleibt die bestätigte SHA-256-Zusammenführung verfügbar.

## PS2-BIOS-Auswahl

Die automatische Auswahl prüft nun die ROMDIR-/ROMVER-Struktur eines 4–8-MiB-BIOS-Abbilds. Eine gleich große DIFF-/ROM1-Begleitdatei genügt nicht. Diese Strukturprüfung ist kein Echtheitsnachweis und liefert keine BIOS-Dateien. Vorhandene gültige eigene ROM0-/BIOS-Abbilder verwenden; BIOS und Spielstände werden nicht umbenannt oder gelöscht. Bei einem Emulatorabsturz werden Name, hexadezimaler Fehlercode und bei PCSX2 der bekannte Protokollpfad angezeigt. Die Auswahl orientiert sich am [PCSX2-BIOS-Format](https://github.com/PCSX2/pcsx2/blob/master/pcsx2/ps2/BiosTools.cpp).
