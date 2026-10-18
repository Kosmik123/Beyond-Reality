# Beyond Reality – prototyp (Unity)

Platformówka 2D w świecie z voxeli 3D. Kamera ortograficzna obraca się co 90° wokół osi pionowej (inspiracja FEZ). Widać i dotyka się tylko warstwy świata na głębokości gracza.

## Uruchomienie
1. Otwórz projekt w Unity 6 (szablon Universal 3D). Skrypty są w `Assets/Scripts/BeyondReality`.
2. Otwórz `Scenes/SampleScene` i naciśnij **Play**. Gra zbuduje się sama: świat, gracz, kamera, monety i UI.
   - Wolisz ręcznie? Ustaw `Game.AutoBootstrap = false` i dodaj komponent `Game` do pustego obiektu.

## Sterowanie
A/D lub strzałki – ruch · Spacja/W – skok (dłużej = wyżej) · Q/E – obrót świata · R – restart

## Jak to działa
- `VoxelWorld` trzyma siatkę 3D voxeli.
- `PlayerController` to zwykła fizyka 2D na kafelkach **jednej warstwy**: voxeli na głębokości gracza. Bloki przed i za graczem są ignorowane.
- Obrót (`TryEnterView`) przelicza pozycję do nowego widoku. Nową warstwą jest kolumna voxeli, w której gracz stoi. Obracać można tylko stojąc.
- `CameraRig` ustawia clip planes tak, że renderuje się tylko warstwa gracza. Podczas animacji obrotu widać cały świat. Gracz jest rysowany w swojej prawdziwej pozycji.
- Monetę zbierasz, gdy jest w warstwie gracza i pokrywa się z nim na ekranie.

## Poziom
`Level.cs` zawiera prostopadłościany `{x0,y0,z0,x1,y1,z1}` i pozycje monet. Sprawdzenie poziomu: `python Tools/level_solver.py`. Obecny poziom (20×20×20) przechodzi solver: wszystkie 10 monet da się zebrać, a każda wymaga od 1 do 9 obrotów.

Fizyka zakłada skok +3 w górę na 1 kolumnę, +2 na 2 kolumny, +1 na 3 kolumny, przeskok nad 2-kolumnową dziurą i tunel wysokości 1. Zmieniając `jumpApex`/`moveSpeed`, sprawdź osiągalność monet.

## Dokumentacja
- `GDD.md` – design gry, zakres i priorytety
- `CLAUDE.md` – architektura i zasady pracy z kodem (dla Claude Code)
