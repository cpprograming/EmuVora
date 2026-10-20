# Controller und automatische Belegung

Unter **Einstellungen → Emulatoren → Controller prüfen** oder im Controller-Schritt des Einrichtungsassistenten die Diagnose öffnen.

## Erkennung in EmuVora

Die Windows-x64-Version enthält **SDL 3.4.18** und eine feste Ausgabe von [SDL_GameControllerDB](https://github.com/mdqinc/SDL_GameControllerDB), Commit `ff333823a85ae896e73f4c53670932cf0f728adb`. Unterstützte Gamepads werden ohne separaten Download erkannt und für Bibliothek, Dialoge und Bildschirmtastatur auf das Xbox-Positionslayout abgebildet: unten A/bestätigen, rechts B/zurück, links X, oben Y. Beschriftungen anderer Controller können davon abweichen. Ein Gerät ohne passendes SDL-Profil wird nicht geraten.

SDL ist die einzige aktive Eingabequelle. XInput wird nicht zusätzlich zur SDL-Geräteliste gemischt, damit Xbox-Geräte keine doppelten Aktionen auslösen. Fehlt eine Komponente oder schlägt SDL fehl, wechselt die Oberfläche auf die bisherigen vier XInput-Slots; die Diagnose zeigt den Grund. Bei funktionierendem SDL bleibt auch eine leere Geräteliste bei SDL, damit später angeschlossene Gamepads erkannt werden. Nach einem Komponentenfehler startet SDL erst beim nächsten Programmstart neu.

Bis zu 16 SDL-Gamepads werden gleichzeitig abgefragt. Ein neues Gerät erhält eine neue Kennung für die Navigation; sie ist **kein Spielerplatz im Emulator**. Die vorhandenen Xbox-Spielprofile verwenden weiterhin ausschließlich gültige XInput-Slots von 0 bis 3.

**Diese Erweiterung betrifft EmuVoras Oberfläche.** Zusätzliche Gamepads erhalten dadurch keine universelle automatische Belegung in RetroArch, DuckStation, PCSX2, Dolphin oder PPSSPP. Nicht-Xbox-Spielprofile und besondere Eingabegeräte im jeweiligen Emulator einrichten; bestehende eigene Konfigurationen bleiben erhalten. Automatische Profile für weitere Modelle sind ein weiterer Arbeitsschritt.

## Diagnose

Die Ansicht zeigt gedrückte Tasten, beide Sticks und beide Trigger. SDL meldet den Gerätenamen und, falls bekannt, Kabel/Drahtlos. Drahtlos unterscheidet nicht zuverlässig Bluetooth und proprietäre Funkadapter. Unbekannte Verbindungsarten bleiben unbekannt. Bei XInput-Rückfall zeigt die Ansicht vier Slots; XInput liefert weder Modell noch Verbindungstyp.

Bis zu zwölf Verbindungswechsel bleiben in der geöffneten Ansicht sichtbar; kein Bericht wird automatisch gespeichert oder hochgeladen. Eingaben lösen hier keine normalen Menüaktionen aus. **View + Menu eine Sekunde auf demselben Controller halten** schließt die Ansicht; alternativ Escape oder Schließen verwenden. Bei Fokusverlust beginnt die Haltezeit erneut. Der Test sendet keine Rumble-Befehle.

## Eingaben in Bibliothek und Dialogen

- Neue oder wiederverbundene Controller zuerst loslassen, dann eine Taste drücken. Nach einem Fensterwechsel ebenfalls zunächst loslassen.
- Ein Controller behält die Navigation, während er eine Navigationstaste oder den linken Stick außerhalb der Schwelle hält. Andere Controller übernehmen mit einem neuen Tastendruck, sobald er neutral ist.
- Richtungen wiederholen nach 400 ms, anschließend alle 140 ms. Aktionstasten wiederholen nicht automatisch.
- Der linke Stick steuert erst oberhalb von 16.000 Rohwerten je Achse. Rechter Stick und Trigger navigieren die Oberfläche nicht; ihre normalisierten Werte bleiben im Test sichtbar.
- SDL-Achsen werden auf die bisherige XInput-Richtung umgerechnet, Trigger auf 0–255. Eigene Emulatorprofile werden nicht aus SDL-Gerätekennungen überschrieben.

## Build und Herkunft

`scripts/Prepare-Controllers.ps1` lädt beim Windows-Build ausschließlich das feste offizielle SDL-x64-Archiv und die festgelegte Datenbank herunter. Archiv, DLL und Datenbank werden per SHA-256 geprüft; bereits geprüfte Dateien unter `src/EmuVora.App/obj/controller-runtime` können offline erneut verwendet werden. Diese Dateien und beide zlib-Lizenztexte gelangen in Build, Tests und Veröffentlichung. Am Benutzer-PC ist dafür kein Controller-Download notwendig. Die mitgelieferten DLL-/Datenbank-Dateien werden vor dem ersten SDL-Start ebenfalls geprüft. Zusätzliche Profile kommen über eine spätere geprüfte EmuVora-Version.

- [SDL-Version und Windows-Paket](https://github.com/libsdl-org/SDL/releases/tag/release-3.4.18)
- [Fester Datenbankstand](https://github.com/mdqinc/SDL_GameControllerDB/tree/ff333823a85ae896e73f4c53670932cf0f728adb)
- SDL-Archiv SHA-256: `75c2c0fc74e7d1206aaedc22893a35887332f4f85f5c8b74fddc10d177985f2c`
- SDL3.dll SHA-256: `2b4b128d8d510cc2d442693cf848c050d700d43dc667afe9e5fd82b0570d667e`
- Datenbank SHA-256: `c6587cc65789539e61ec375aad5bdbc14f974508b57e2ae87730ac75c4445dba`

## Prüfung und offene Hardwaretests

166 lokale Funktionstests bestanden. Dazu gehören die echte native SDL-Runtime mit virtuellem Gamepad (Tasten, Achsen, Trigger, Trennen und erneutes Verbinden), XInput-Rückfall, Schutz vor doppelter Eingabe, Achsengrenzen, Komponentenintegrität, Diagnose und bestehende Controller-/UI-Tests. Der Aufbau aus einem leeren Download-Cache wurde ebenfalls geprüft. Die Windows-CI prüft zusätzlich die offiziellen Emulator-Downloads und die Installer.

Praktisch noch prüfen:

1. Xbox-Controller per USB, Bluetooth und Xbox-Adapter testen; danach weitere verfügbare Modelle mit SDL-Profil.
2. Bei gehaltener A-Taste trennen und erneut verbinden. Loslassen und neu drücken: kein selbsttätiger Spielstart.
3. Während eines Dialogs oder Spiels eine Taste halten und zur Bibliothek zurückkehren. Erst nach Loslassen darf sie eine neue Aktion auslösen.
4. Mehrere Controller anschließen und wechselnd navigieren; Spielerzuordnung separat im Emulator prüfen.
5. Einrichtung, Dateiauswahl, BIOS-Import, Sicherung, Spielstart und Rückkehr ohne Maus durchspielen.

Für einen Fehlerbericht genügen EmuVora-/Windows-Version, Controller-Modell, Verbindungsart und betroffener Ablauf. Keine BIOS-, ROM- oder persönlichen Sicherungsdateien veröffentlichen.
