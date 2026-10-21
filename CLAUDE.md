# CLAUDE.md – Beyond Reality

Platformówka z obrotem świata (inspiracja FEZ, ale z inną regułą głębokości) na game jam (temat „Beyond Reality”). Unity 6, URP 3D, nowy Input System.
Opis designu: `GDD.md`. Krótki opis dla ludzi: `README.md`.

## Struktura

```
Assets/Scripts/BeyondReality/
  Views.cs             – 4 widoki: wektory Right/Forward, przeliczenia world <-> (h, depth)
  Level.cs             – dane poziomu: prostopadłościany {x0,y0,z0,x1,y1,z1}, monety, start
  VoxelWorld.cs        – zbiór voxeli (IsSolid), budowa wizualizacji
  PlayerController.cs  – fizyka 2D w warstwie głębokości gracza, obrót, wizualizacja gracza
  CameraRig.cs         – kamera ortograficzna, clipping do warstwy gracza, animacja obrotu (z pochyleniem), wstrząs
  Game.cs              – bootstrap, pętla gry, input, monety, HUD (OnGUI)
Tools/level_solver.py  – weryfikacja osiągalności monet w Level.cs
```

Cała gra buduje się w runtime. `Game.AutoBootstrap` tworzy obiekt `Game` w każdej scenie po wejściu w Play, a na scenie nie ma żadnych prefabów.

## Kluczowe koncepcje

- **Układ współrzędnych:** voxel `(x,y,z)` zajmuje `[x±0.5, y±0.5, z±0.5]`, więc górna powierzchnia bloku leży na `y+0.5`.
- **Widoki:** `view` 0..3 to Północ (+Z), Wschód (+X), Południe (−Z), Zachód (−X). Yaw kamery = `90*view`.
  - `h = dot(pos.xz, Right(view))`
  - `depth = dot(pos.xz, Forward(view))`
- **Warstwa (slice) – to NIE jest reguła FEZ:** gracz wchodzi w interakcję tylko z voxelami na swojej głębokości (`DepthCoord`). Bloki przed nim i za nim nie istnieją dla kolizji ani monet. Nie ma żadnej projekcji ani przeskoków głębokości.
- **Gracz:** stan to `H` (float), `Y` (stopy), `DepthCoord` (int) i `View`.
  - Kolizje: AABB 0,6 × 0,9 względem `world.IsSolid(Views.ToVoxel(view, h, depth, y))` w bieżącej warstwie.
  - Fizyka 3D Unity **nie jest używana**, a primitive'y mają usuwane collidery.
- **Obrót (`TryEnterView`):** nową warstwą staje się kolumna voxeli, w której gracz teraz stoi (najpierw środkowa, a jeśli pod nią nie ma podłoża, ta krawędziowa, pod którą jest). Nowe `H` jest całkowite. Działa tylko, gdy gracz stoi na ziemi. Praktycznie nigdy nie odmawia, bo ta kolumna była wolna w starej warstwie.
- **Kamera (`CameraRig.ApplyClipping`):** poza animacją obrotu clip planes wycinają warstwę `[depth-0.55, depth+0.45]`, więc renderuje się tylko warstwa gracza. Podczas obrotu kamera pokazuje cały świat. Voxele nie rzucają cieni, żeby niewidoczne warstwy nie zacieniały widocznej.
- **Rysowanie gracza:** w prawdziwej pozycji w levelu (`TruePosition`), bez przesunięcia w stronę kamery.
- **Monety:** zbierane tylko w warstwie gracza (ta sama głębokość) po pokryciu w h i y.
- **Kolejność aktualizacji:**
  - `Game.Update`: input → obrót → `player.Tick` → monety.
  - `Game.LateUpdate`: `rig.Tick` → `player.UpdateVisual` → animacja monet.
  - Podczas animacji obrotu fizyka gracza jest zamrożona.

## Zasady pracy

- **Po każdej zmianie `Level.cs` albo parametrów ruchu** (`jumpApex`, `moveSpeed`, `gravity`, wymiary AABB) uruchom `python Tools/level_solver.py`. Wszystkie monety muszą być osiągalne.
  - Model ruchu w solverze (`JUMPS`) musi pozostać **konserwatywny** względem prawdziwej fizyki. Jeśli osłabiasz skok, zaktualizuj `JUMPS`.
- **Input:** obsługuj oba systemy przez `#if ENABLE_INPUT_SYSTEM` (wzorzec jest w `Game.cs`).
- **Materiały:** twórz przez `VoxelWorld.MakeMaterial` (obsługuje URP i Built-in). Kopiuje szablony z `Assets/Resources/BeyondReality` (`VoxelLit`, `VoxelLitEmissive`), dzięki czemu shader URP Lit i wariant `_EMISSION` trafiają do buildu. Samo `Shader.Find` zwraca w buildzie null. Nowy wariant shadera (np. przezroczystość) wymaga nowego szablonu.
- **Teksty UI** są po polsku.
- **Nie mam Unity w CLI:** po zmianach poproś użytkownika o uruchomienie Play i wklejenie błędów z konsoli. Pliki `.meta` generuje Unity, więc nie twórz ich ręcznie.
- **Commity:** małe, opisowe, po polsku.

## Następne kroki (z GDD, kolejność priorytetów)

1. Dźwięki i muzyka (AudioSource na `Game`, klipy w `Assets/Audio`).
2. Juice: particle przy monecie, squash & stretch gracza, efekt przy obrocie.
3. Ekran tytułowy i samouczek pierwszego obrotu, kompas kierunku.
4. Obsługa pada.
5. Build WebGL na itch.io.
6. Opcjonalnie: więcej poziomów, import `.vox`, ruchome platformy.
