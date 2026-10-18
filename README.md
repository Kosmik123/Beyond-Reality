# Beyond Reality – prototyp (Unity)

Platformówka 2D w świecie z voxeli 3D. Kamera ortograficzna obraca się co 90° wokół osi pionowej (jak w FEZ).

## Uruchomienie
1. Otwórz projekt w Unity 6 (szablon Universal 3D). Skrypty są w `Assets/Scripts/BeyondReality`.
2. Otwórz `Scenes/SampleScene` i naciśnij **Play**. Gra zbuduje się sama: świat, gracz, kamera, monety i UI.
   - Wolisz ręcznie? Ustaw `Game.AutoBootstrap = false` i dodaj komponent `Game` do pustego obiektu.

## Sterowanie
A/D lub strzałki – ruch · Spacja/W – skok (dłużej = wyżej) · Q/E – obrót świata · R – restart

## Jak to działa
- `VoxelWorld` trzyma siatkę 3D i dla każdego z 4 widoków jej **projekcję 2D**. Komórka (h, y) jest pełna, jeśli jakikolwiek voxel na osi głębokości jest pełny.
- `PlayerController` to zwykła fizyka 2D na kafelkach tej projekcji. Głębokość jest niewidoczna. Gdy gracz stoi, przesuwamy go w głąb na voxel, który faktycznie ma pod stopami (najpierw obecna głębokość, inaczej najbliższa kamerze). Na tym polega trik FEZ.
- Obrót (`TryEnterView`) przelicza pozycję do nowego widoku. Jeśli gracz wylądowałby w bloku, obrót jest blokowany i ekran się trzęsie. Obracać można tylko stojąc.
- Gracz jest rysowany przesunięty w stronę kamery. W rzucie ortograficznym nie zmienia to jego pozycji na ekranie, ale świat nigdy go nie zasłania.
- Monetę zbierasz, gdy pokrywa się z graczem **na ekranie**, niezależnie od głębokości.

## Poziom
`Level.cs` zawiera prostopadłościany `{x0,y0,z0,x1,y1,z1}` i pozycje monet. Sprawdzenie poziomu: `python Tools/level_solver.py`. Obecnie wszystkie 10 monet da się zebrać i żadnej nie da się zebrać bez obracania widoku.

Fizyka zakłada skok na 2 bloki w górę, 2 kolumny w bok przy +1 i tunel wysokości 1. Zmieniając `jumpApex`/`moveSpeed`, sprawdź osiągalność monet.

## Dokumentacja
- `GDD.md` – design gry, zakres i priorytety
- `CLAUDE.md` – architektura i zasady pracy z kodem (dla Claude Code)
