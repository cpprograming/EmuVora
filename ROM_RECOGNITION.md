# Spielnamen anhand von Prüfsummen erkennen

Unter **Einstellungen → Bibliothek → Spielnamen per Prüfsumme erkennen** ein System wählen und **Katalog laden und Vorschläge prüfen** drücken.

EmuVora lädt einen für diesen Release festgelegten No-Intro-Katalog aus dem offiziellen [libretro-database-Repository](https://github.com/libretro/libretro-database), prüft seine Größe und SHA-256 und speichert ihn unter `metadata-cache` im gewählten Datenordner. Bereits geprüfte Kataloge können anschließend offline verwendet werden. ROM-Dateien, ROM-Prüfsummen und lokale Pfade werden für diese Funktion nicht übertragen.

Die Erkennung vergleicht lokale Dateigröße und SHA-1 mit dem gewählten Katalog. SHA-1 dient zur Identifikation, nicht als Vertrauensnachweis einer ROM. Geänderte oder beschädigte Dateien, andere Header und Patches können ohne Treffer bleiben. Gleiche Prüfsummen mit mehreren verschiedenen Titeln bleiben mehrdeutig und werden nicht vorgeschlagen.

## Vorschau und Übernahme

Die Vorschau zeigt bisherigen Titel, Vorschlag und Dateipfad. Erst **Alle eindeutigen Vorschläge übernehmen** ändert die Bibliothekstitel. Abbrechen lässt die Bibliothek unverändert. Dateinamen werden nicht geändert. Cover-Zuordnungen nach Pfad, Favoriten und Spielverlauf bleiben erhalten. Zwischenzeitlich bearbeitete Bibliothekseinträge und ROM-Dateien mit geänderter Größe oder Änderungszeit werden ausgelassen.

Manuell bearbeitete Einträge sind geschützt. Das umfasst derzeit auch manuell favorisierte Einträge, weil EmuVora diese gemeinsam als bearbeitet kennzeichnet. Einträge aus dem JSON-Bibliotheksimport sind ebenfalls geschützt. Deshalb können vorhandene Spiele von der Erkennung ausgenommen sein. Der normale Titel-/Konsolen-Editor bleibt nutzbar.

Nach der Übernahme kann **Alle fehlenden Cover laden** mit den erkannten Titeln suchen. Vorhandene eigene Cover werden nicht ersetzt; ein Datenbanktreffer garantiert keinen verfügbaren Cover-Treffer.

Der lokale `recognition-report.txt` enthält Vorschläge und Gründe für ausgelassene Dateien. Er kann vollständige lokale Pfade enthalten und wird nicht automatisch hochgeladen. Ein neuer Bericht bewahrt den vorherigen als `.bak`. Die Metadaten-Caches und der Bericht sind nicht Bestandteil der automatischen Datensicherung; übernommene Titel liegen hingegen in der gesicherten Bibliotheksdatenbank.

## Unterstützter Umfang

16 Systeme: NES, SNES, GB, GBC, GBA, Mega Drive, Master System, Game Gear, 32X, Atari 2600/5200/7800, WonderSwan, WonderSwan Color, PC Engine und SuperGrafx.

Unterstützt sind einzelne Dateien in den zugeordneten Cartridge-Formaten sowie ZIP-Dateien mit genau einer passenden ROM. Höchstens 64 MiB pro unkomprimierter ROM; ZIPs werden zum Prüfen nicht auf die Festplatte entpackt. Disc-, Mehrdisc- und Arcade-Sets, N64-Byte-Reihenfolgen sowie weitere Systeme sind nicht Bestandteil dieses Schritts. Es werden keine ROM-Header verändert und keine Dateien normalisiert.

## Quelle und Lizenzhinweise

Metadaten: libretro/libretro-database, No-Intro-DATs, Stand Commit `12c21cf79f1e20eff6ac68b80875816c717d0a7a`, geprüft am 19. Oktober 2026. [Originalquellen](https://github.com/libretro/libretro-database/tree/12c21cf79f1e20eff6ac68b80875816c717d0a7a/metadat/no-intro). Das Repository nennt [CC-BY-SA-4.0](https://creativecommons.org/licenses/by-sa/4.0/); ursprüngliche Katalogdateien und Herkunftshinweise bleiben erhalten. Im Programmpaket liegen nur Quellverweise und Download-Prüfsummen, keine ROMs und keine vollständigen Metadatenkataloge. Katalogstände werden mit einem EmuVora-Update festgelegt.
