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
- Świat 3D jest **spłaszczany**: komórka 2D jest pełna, jeśli na osi głębokości leży w niej jakikolwiek voxel.
- Gracz porusza się **tylko równolegle do płaszczyzny ekranu**. Żeby przejść „w głąb”, trzeba obrócić świat.

### 3.2 Obrót świata
- Q/E obraca o 90° (animacja ok. 0,4 s z lekkim pochyleniem kamery, które na moment pokazuje świat w 3D).
- Obrót jest możliwy tylko, gdy gracz stoi na ziemi.
- Obrót jest blokowany (wstrząs kamery), jeśli w nowym widoku gracz znalazłby się wewnątrz bloku.

### 3.3 Głębokość (trik FEZ)
- Głębokość gracza jest niewidoczna. Gdy gracz staje na bloku, przenosi się w głąb na voxel, który faktycznie ma pod stopami: najpierw zostaje na obecnej głębokości, a jeśli tam nic nie ma, trafia na voxel najbliższy kamerze.
- Dzięki temu dwie platformy, które w jednym widoku tylko **wyglądają** na połączone, są połączone naprawdę. To podstawowy budulec zagadek.

### 3.4 Ruch
- Bieg 5,5 bloku/s. Skok o zmiennej wysokości, maksymalnie ok. 2,4 bloku.
- Coyote time (0,08 s) i bufor skoku (0,12 s).
- Gwarantowane możliwości, na których opiera się projekt poziomów:
  - wejście o 1 blok w górę,
  - skok +1 w górę z przesunięciem o 2 kolumny,
  - skok +2 w górę z przesunięciem o 1 kolumnę,
  - przejście tunelem wysokim na 1 blok.
- Upadek poza świat powoduje respawn w ostatnim bezpiecznym miejscu.

### 3.5 Monety
- Jest 10 monet, rozmieszczonych w świecie 3D.
- Moneta zostaje zebrana, gdy **na ekranie** pokrywa się z graczem, niezależnie od głębokości.
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

- Obecny poziom demo ma 12×13×12 voxeli i 21 prostopadłościanów.
- Zasady:
  - Każda moneta wymaga co najmniej jednego obrotu.
  - Uczymy stopniowo: najpierw monety wymagające 1 obrotu, potem 2–3.
  - Wysoka centralna wieża daje orientację w przestrzeni.
  - Unikamy bloków na wysokości gracza wzdłuż długich linii, bo blokują one obrót.
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

**Must have (zrobione):** świat voxeli, rzut 2D, obrót, fizyka platformówki, trik głębokości, 10 monet, wygrana, restart.

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
- **Niejasna reguła głębokości:** krótka animacja lub efekt przy przeskoku głębokości.
- **WebGL:** shadery tworzone w kodzie mogą zostać wycięte przy buildzie. Trzeba dodać `Universal Render Pipeline/Lit` do Always Included Shaders.
