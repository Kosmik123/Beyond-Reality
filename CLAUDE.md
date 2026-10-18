# CLAUDE.md – Beyond Reality

Platformówka w stylu FEZ na game jam (temat „Beyond Reality”). Unity 6, URP 3D, nowy Input System.
Opis designu: `GDD.md`. Krótki opis dla ludzi: `README.md`.

## Struktura

```
Assets/Scripts/BeyondReality/
  Views.cs             – 4 widoki: wektory Right/Forward, przeliczenia world <-> (h, depth)
  Level.cs             – dane poziomu: prostopadłościany {x0,y0,z0,x1,y1,z1}, monety, start
  VoxelWorld.cs        – zbiór voxeli + projekcje 2D dla każdego widoku, budowa wizualizacji
  PlayerController.cs  – fizyka 2D na projekcji, rozstrzyganie głębokości, obrót, wizualizacja gracza
  CameraRig.cs         – kamera ortograficzna, animacja obrotu (z pochyleniem), wstrząs
  Game.cs              – bootstrap, pętla gry, input, monety, HUD (OnGUI)
Tools/level_solver.py  – weryfikacja osiągalności monet w Level.cs
```

Cała gra buduje się w runtime. `Game.AutoBootstrap` tworzy obiekt `Game` w każdej scenie po wejściu w Play, a na scenie nie ma żadnych prefabów.

## Kluczowe koncepcje

- **Układ współrzędnych:** voxel `(x,y,z)` zajmuje `[x±0.5, y±0.5, z±0.5]`, więc górna powierzchnia bloku leży na `y+0.5`.
- **Widoki:** `view` 0..3 to Północ (+Z), Wschód (+X), Południe (−Z), Zachód (−X). Yaw kamery = `90*view`.
  - `h = dot(pos.xz, Right(view))`
  - `depth = dot(pos.xz, Forward(view))`
- **Projekcja:** `VoxelWorld.IsSolidProjected(view, h, y)` zwraca true, gdy jakikolwiek voxel w tej kolumnie głębokości jest pełny. `DepthsAt` zwraca listę głębokości od najbliższej kamerze.
- **Gracz:** stan to `H` (float), `Y` (stopy), `DepthCoord` (int) i `View`.
  - Kolizje działają wyłącznie na projekcji (AABB 0,6 × 0,9).
  - Fizyka 3D Unity **nie jest używana**, a primitive'y mają usuwane collidery.
- **Rozstrzyganie głębokości (`ResolveDepth`):** gdy gracz stoi, zostaje na obecnej głębokości, jeśli pod nim jest voxel. W przeciwnym razie przechodzi na voxel najbliższy kamerze.
  - Ponieważ pusta komórka projekcji oznacza pustą całą linię głębokości, przeskok głębokości nigdy nie wsadzi gracza w blok.
- **Obrót (`TryEnterView`):** przelicza pozycję do nowego widoku (nowe `H` jest całkowite, a głębokość zaokrąglana). Odmawia, jeśli AABB nachodzi na pełną komórkę. Działa tylko, gdy gracz stoi na ziemi.
- **Rysowanie gracza:** wizualizacja jest przesunięta o 25 jednostek w stronę kamery (`-cam.forward*25`). W ortho pozycja na ekranie się nie zmienia, a świat nie zasłania gracza. Kamera stoi 60 jednostek od punktu focus, więc trzeba to uwzględnić przy zmianie tych wartości.
- **Monety:** zbierane po pokryciu na ekranie (h i y), bez względu na głębokość.
- **Kolejność aktualizacji:**
  - `Game.Update`: input → obrót → `player.Tick` → monety.
  - `Game.LateUpdate`: `rig.Tick` → `player.UpdateVisual` → animacja monet.
  - Podczas animacji obrotu fizyka gracza jest zamrożona.

## Zasady pracy

- **Po każdej zmianie `Level.cs` albo parametrów ruchu** (`jumpApex`, `moveSpeed`, `gravity`, wymiary AABB) uruchom `python Tools/level_solver.py`. Wszystkie monety muszą być osiągalne.
  - Model ruchu w solverze (`JUMPS`) musi pozostać **konserwatywny** względem prawdziwej fizyki. Jeśli osłabiasz skok, zaktualizuj `JUMPS`.
- **Input:** obsługuj oba systemy przez `#if ENABLE_INPUT_SYSTEM` (wzorzec jest w `Game.cs`).
- **Materiały:** twórz przez `VoxelWorld.MakeMaterial` (obsługuje URP i Built-in). Przed buildem WebGL dodaj `Universal Render Pipeline/Lit` do Graphics → Always Included Shaders.
- **Teksty UI** są po polsku.
- **Nie mam Unity w CLI:** po zmianach poproś użytkownika o uruchomienie Play i wklejenie błędów z konsoli. Pliki `.meta` generuje Unity, więc nie twórz ich ręcznie.
- **Commity:** małe, opisowe, po polsku.

## Następne kroki (z GDD, kolejność priorytetów)

1. Dźwięki i muzyka (AudioSource na `Game`, klipy w `Assets/Audio`).
2. Juice: particle przy monecie, squash & stretch gracza, efekt przy przeskoku głębokości.
3. Ekran tytułowy i samouczek pierwszego obrotu, kompas kierunku.
4. Obsługa pada.
5. Build WebGL na itch.io.
6. Opcjonalnie: więcej poziomów, import `.vox`, ruchome platformy.
