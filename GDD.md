# Beyond Reality – Game Design Document

**Game jam:** temat „Beyond Reality”
**Gatunek:** platformówka 2D / zagadka perspektywiczna (inspiracja: FEZ)
**Silnik:** Unity 6, URP 3D
**Platforma docelowa:** WebGL (itch.io), opcjonalnie Windows

---

## 1. Pitch

Wydaje Ci się, że to płaska platformówka 2D. W rzeczywistości świat jest trójwymiarowy, zbudowany z voxeli, a Ty widzisz tylko jego rzut z jednej z czterech stron. Obracając rzeczywistość o 90°, odkrywasz ścieżki, których „nie ma”, i zbierasz 10 monet ukrytych poza granicami tego, co widać.

**Związek z tematem:** prawdziwy świat leży „poza” rzeczywistością, którą widzi gracz. Wyjście poza płaski obraz to rdzeń mechaniki, a nie tylko dekoracja.

## 2. Główna pętla

1. Rozejrzyj się w bieżącym widoku 2D.
2. Biegaj i skacz po tym, co widać.
3. Utknąłeś? Obróć świat (Q/E). Ta sama przestrzeń wygląda wtedy zupełnie inaczej.
4. Zbierz monetę, szukaj następnej.
5. Zbierz 10 monet i wygraj (czas jako wynik).

## 3. Mechaniki

### 3.1 Widok i rzut
- Kamera ortograficzna patrzy w jednym z 4 kierunków: Północ, Wschód, Południe, Zachód.
- Widać tylko **jedną warstwę** świata: plaster voxeli na głębokości gracza (kamera tnie resztę clip planes). Gracz jest fizycznie w tej warstwie.
- Gracz porusza się **tylko równolegle do płaszczyzny ekranu**. Żeby przejść „w głąb”, trzeba obrócić świat.

### 3.2 Obrót świata
- Q/E obraca o 90° (animacja ok. 0,4 s z lekkim pochyleniem kamery, które na moment pokazuje świat w 3D).
- Obrót jest możliwy tylko, gdy gracz stoi na ziemi.
- Po obrocie gracz trafia do warstwy wyznaczonej przez kolumnę, w której stoi. Obrót jest blokowany (wstrząs kamery) tylko, gdyby gracz znalazł się wewnątrz bloku.

### 3.3 Głębokość (warstwy – inaczej niż w FEZ)
- Gracz wchodzi w interakcję **tylko z obiektami na tej samej głębokości**. Bloki przed nim i za nim nie istnieją: nie da się na nich stanąć, nie blokują ruchu i nie są rysowane.
- Głębokość zmienia się wyłącznie przez obrót. Zagadki polegają na wyborze miejsca obrotu: z którego miejsca obrócić świat, żeby trafić do warstwy z właściwą drogą.

### 3.4 Ruch
- Bieg 5,5 bloku/s. Skok o zmiennej wysokości, maksymalnie ok. 3,2 bloku.
- Coyote time (0,08 s) i bufor skoku (0,12 s).
- Gwarantowane możliwości, na których opiera się projekt poziomów:
  - skok +3 w górę z przesunięciem o 1 kolumnę,
  - skok +2 w górę z przesunięciem o 2 kolumny,
  - skok +1 w górę z przesunięciem o 3 kolumny,
  - przeskok nad 2-kolumnową dziurą na tej samej wysokości,
  - przejście tunelem wysokim na 1 blok (każdy skok potrzebuje 1 wolnego rzędu ponad wysokością lądowania).
- Upadek poza świat powoduje respawn w ostatnim bezpiecznym miejscu.

### 3.5 Monety
- Jest 10 monet, rozmieszczonych w świecie 3D.
- Moneta zostaje zebrana, gdy jest w warstwie gracza i pokrywa się z nim na ekranie.
- Monety mogą być zasłonięte geometrią, a odkrywa się je obracając świat.

## 4. Sterowanie

| Akcja | Klawisze |
|---|---|
| Ruch | A/D, ←/→ |
| Skok | Spacja, W, ↑ (przytrzymanie daje wyższy skok) |
| Obrót świata | Q / E |
| Restart | R |

Do zrobienia: obsługa pada (lewa gałka, A, bumpery).

## 5. Projekt poziomu

- Obecny poziom demo ma 20×20×20 voxeli i 19 prostopadłościanów: kładki biegnące wzdłuż x (widać je z północy/południa) albo wzdłuż z (ze wschodu/zachodu) i centralna wieża. Monety wymagają kolejno 1–9 obrotów.
- Zasady:
  - Każda moneta wymaga co najmniej jednego obrotu.
  - Uczymy stopniowo: najpierw monety wymagające 1 obrotu, potem 2–3.
  - Wysoka centralna wieża daje orientację w przestrzeni.
  - Obrót w miejscu skrzyżowania kładek przełącza między nimi. Elementy z innych warstw widać tylko w trakcie obrotu, więc ukryte półki można pokazać „z daleka” (np. półka nad startem).
- **Każda zmiana poziomu musi przejść `python Tools/level_solver.py`.** Solver sprawdza osiągalność i liczbę wymaganych obrotów.

## 6. Oprawa

- **Styl:** minimalistyczne voxele z kolorem zależnym od wysokości (od turkusu do magenty, „senny” klimat) i delikatną szachownicą, na ciemnofioletowym tle.
- **Gracz:** koralowa kostka z okiem pokazującym kierunek. Zawsze rysowana przed światem.
- **Monety:** złote, wirujące dyski.
- Do zrobienia:
  - lepsza postać (sprite lub animacja),
  - particle przy zbieraniu monety,
  - outline voxeli,
  - mgła/głębia przy obrocie.

## 7. Dźwięk (do zrobienia)

- Skok, lądowanie, moneta (rosnąca wysokość tonu z każdą kolejną), obrót („whoosh”), blokada obrotu („bonk”).
- Ambientowa, senna muzyka w pętli.

## 8. UI

- Licznik monet, nazwa bieżącego widoku, timer, podpowiedź sterowania (obecnie przez OnGUI).
- Ekran wygranej z czasem.
- Do zrobienia: ekran tytułowy, pauza, kompas pokazujący kierunek widoku.

## 9. Zakres i priorytety

**Must have (zrobione):** świat voxeli, widok jednej warstwy, obrót, fizyka platformówki, 10 monet, wygrana, restart.

**Should have:**
1. Dźwięki i muzyka.
2. Juice: particle, squash & stretch, przyciemnienie przy obrocie.
3. Ekran tytułowy i krótki samouczek pierwszego obrotu.
4. Obsługa pada.
5. Build WebGL na itch.io.

**Could have:**
- Kilka poziomów.
- Edytor poziomów lub import z MagicaVoxel (`.vox`).
- Ruchome platformy.
- Bloki widoczne tylko z jednej strony („iluzje”).
- Najlepszy czas w PlayerPrefs.

**Won't have (na jam):** fabuła, walka, przeciwnicy.

## 10. Ryzyka

- **Frustracja „gdzie teraz?”:** kompas, lepsza widoczność monet, podpowiedź po X sekundach bez postępu.
- **Niejasna reguła warstw:** gracz nie wie, do której warstwy trafi po obrocie. Podgląd warstwy docelowej lub efekt przy obrocie.
- **Build:** shadery szukane w kodzie są wycinane z buildu. Rozwiązane szablonami materiałów w `Assets/Resources/BeyondReality`.
