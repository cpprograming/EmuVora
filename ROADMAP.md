# EmuVora – Roadmap

Stand: 17. Oktober 2026. Ziel ist ein zuverlässiges Windows-Frontend für den privaten Gebrauch. Die Versionsnummern bezeichnen Etappen, keine festen Veröffentlichungstermine.

Aktueller geprüfter Release: [0.11.59](https://github.com/cpprograming/EmuVora/releases/tag/v0.11.59).

## 0.11 – Daten und Wiederherstellung (abgeschlossen)

- [x] Vorhandene Datenordner in der Oberfläche prüfen und mit Bestätigung öffnen.
- [x] Zur unmittelbar vorherigen Bibliothek zurückkehren; nicht erreichbare Laufwerke erhalten keine Ersatzbibliothek.
- [x] Datenpfad und Rückweg bei fehlgeschlagenem Prozessstart zurücksetzen.
- [x] Optional an Sicherungen erinnern: 7, 14 oder 30 Tage, standardmäßig ausgeschaltet.
- [x] Release 0.11.59 veröffentlicht, heruntergeladen und mit dem Updater einschließlich SHA-256 geprüft; 119 Funktionstests, 5 Netzwerktests und Installer-Prüfungen bestanden.

Zusätzlich in 0.11.59: BIOS-Dateiauswahl korrigiert und automatische Benennung anhand bekannter Prüfsummen ergänzt; [Anleitung](BIOS_IMPORT.md).

## 0.12 – Xbox und Plug-and-play

- [ ] Xbox-Controller per USB und Bluetooth auf echter Hardware testen.
- [ ] Einrichtung, Dateiauswahl, Sicherung und Spielstart vollständig mit Controller durchspielen.
- [ ] Abziehen, Wiederverbinden, Controllerwechsel und mehrere Spieler praktisch prüfen.
- [ ] Fokusführung und Tastensymbole anhand der Ergebnisse verbessern.

## 0.13 – Einheitliche Oberfläche

- [ ] Dialoge, Abstände, Schriften und Schaltflächen vereinheitlichen.
- [ ] Optionalen TV-Modus mit großen Covern, lesbaren Texten und klarem Fokus ergänzen.
- [ ] Leere Bibliothek, fehlende Cover, Downloads und Fehlerzustände verständlich darstellen.
- [ ] Direkte Theme-Unterstützung und separate ES-DE-Darstellung klar unterscheiden.

## 0.14 – Spielebibliothek

- [ ] Eigene Sammlungen und zusätzliche Ansichten ergänzen.
- [ ] Doppelte Einträge und Mehrdisc-Spiele übersichtlicher verwalten.
- [ ] Umgezogene ROM-Ordner neu zuordnen, ohne Favoriten und Verlauf zu verlieren.
- [ ] Falsche Cover-Treffer leichter korrigieren und eigene Cover bewahren.

## 0.15 – Bild und Leistung

- [ ] 1080p, 1440p, 4K und mehrere Monitore praktisch testen.
- [ ] Windows-Anzeigeskalierung und Bildschirmwechsel prüfen.
- [ ] Seitenverhältnis und scharfe Skalierung mit repräsentativen 2D- und 3D-Systemen vergleichen.
- [ ] Systeme mit besonderen Eingabegeräten eindeutig kennzeichnen.

## 1.0 – Stabile Privatversion

- [ ] Vollständige Einrichtung auf einem frischen Windows-System prüfen.
- [ ] Update, Offlinebetrieb, Sicherung, Wiederherstellung und Rückkehr praktisch testen.
- [ ] Verbleibende Fehler beheben und Anleitungen abschließen.
- [ ] Eine stabile EXE mit nachvollziehbaren Versionshinweisen veröffentlichen.

Der detaillierte Arbeitsstand steht in der [Checkliste](CHECKLIST.md). Automatisierte Tests ersetzen die noch offenen Hardwaretests nicht.
