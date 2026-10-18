# Xbox-Controller und Plug-and-play

Unter **Einstellungen → Emulatoren → Xbox-Controller prüfen** oder im Controller-Schritt des Einrichtungsassistenten die Diagnose öffnen.

Die Ansicht zeigt vier Windows-XInput-Slots, gedrückte Tasten, beide Sticks und beide Trigger. Bis zu zwölf Verbindungswechsel bleiben in der geöffneten Ansicht sichtbar; es wird kein Bericht automatisch gespeichert oder hochgeladen. Modell und USB-/Bluetooth-Verbindungsart werden hier nicht ermittelt. Ein XInput-Slot ist keine Zusicherung der Spielerzuordnung eines Emulators.

Im Testfenster lösen Eingaben keine normalen Menüaktionen aus. **View + Menu eine Sekunde auf demselben Controller halten** schließt es; alternativ Escape oder den Schließen-Knopf verwenden. Bei Fokusverlust beginnt die Haltezeit erneut. Der Test sendet keine Rumble-Befehle.

## Eingaben in Bibliothek und Dialogen

- Neue oder wiederverbundene Controller zuerst loslassen, dann eine Taste drücken. Beim Zurückkehren aus einem anderen Fenster ebenfalls zunächst loslassen.
- Ein Controller behält die Navigation, während er eine Navigationstaste oder den linken Stick außerhalb der Schwelle hält. Andere Controller übernehmen mit einem neuen Tastendruck, sobald er neutral ist. Das verhindert wechselnde Besitzer bei gleichzeitig gehaltenen Tasten.
- Richtungen wiederholen nach 400 ms, anschließend alle 140 ms. Aktionstasten wie A und B wiederholen nicht automatisch.
- Die Oberfläche verwendet den linken Stick erst oberhalb der bestehenden Schwelle von 16.000 Rohwerten je Achse. Rechter Stick und Trigger navigieren die Oberfläche nicht; ihre Rohwerte bleiben im Test sichtbar.
- Diese Regeln betreffen EmuVoras Oberfläche. Spieleingaben und Spielerbelegung verwaltet der jeweilige Emulator; eigene Konfigurationen bleiben erhalten.

## Noch offene Tests mit echter Hardware

Die automatischen Tests simulieren XInput-Zustände und prüfen Dialoge. Die folgenden praktischen Prüfungen sind weiterhin offen; sie wurden nicht durch Softwaretests ersetzt:

1. Xbox-Controller per USB verbinden. Diagnose öffnen, jede Taste, beide Sticks und Trigger prüfen. Danach getrennt mit Bluetooth beziehungsweise eigenem Xbox-Adapter wiederholen.
2. Controller bei gehaltener A-Taste trennen und erneut verbinden. Loslassen und neu drücken: kein selbsttätiger Spielstart, keine unbeabsichtigte Bestätigung.
3. Während eines Dialogs oder Emulator-Spiels A halten und zur Bibliothek zurückkehren. Erst nach Loslassen darf ein neuer Tastendruck eine Aktion auslösen.
4. Zwei bis vier Controller verbinden, wechselnd navigieren und gleichzeitige Eingaben prüfen. Danach in tatsächlich verwendeten Emulatoren die Spielerzuordnung kontrollieren.
5. Einrichtung, Ordner-/Dateiauswahl, BIOS-Import, Sicherung, Spielstart und Rückkehr ohne Maus durchspielen. Abbrechen und sichtbaren Fokus in jedem Dialog kontrollieren.

Für einen Fehlerbericht genügen EmuVora-Version, Windows-Version, Controller-Modell, Verbindungsart, betroffener Ablauf und beobachtetes Verhalten. Keine BIOS-, ROM- oder persönlichen Sicherungsdateien veröffentlichen.
