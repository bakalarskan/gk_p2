namespace BezierSurfacesApp.UI;

public sealed class DocumentationForm : Form
{
    public DocumentationForm()
    {
        Text = "Dokumentacja – Powierzchnie Béziera 3D";
        Size = new Size(760, 620);
        StartPosition = FormStartPosition.CenterParent;
        MinimumSize = new Size(620, 480);

        var tabs = new TabControl { Dock = DockStyle.Fill };

        tabs.TabPages.Add(CreateTextTab("Instrukcja obsługi i skróty", GetUserManualText()));
        tabs.TabPages.Add(CreateTextTab("Opis techniczny obrotu myszą (Trackball)", GetTrackballTechDescription()));
        tabs.TabPages.Add(CreateTextTab("Model matematyczny i algorytmy", GetMathOverviewText()));

        Controls.Add(tabs);
    }

    private static TabPage CreateTextTab(string title, string content)
    {
        var page = new TabPage(title);
        var box = new RichTextBox
        {
            Dock = DockStyle.Fill,
            ReadOnly = true,
            BackColor = Color.White,
            Font = new Font("Consolas", 9.8f),
            Text = content,
            BorderStyle = BorderStyle.None,
            ScrollBars = RichTextBoxScrollBars.Vertical
        };
        page.Controls.Add(box);
        return page;
    }

    private static string GetUserManualText() =>
"""
====================================================================
  INSTRUKCJA OBSŁUGI APLIKACJI – POWIERZCHNIE BÉZIERA 3D
====================================================================

1. STEROWANIE MYSZĄ W OBSZARZE RYSOWANIA
--------------------------------------------------------------------
• Lewy przycisk myszy (LPM) + przeciąganie:
    Swobodny, narastający obrót sceny w przestrzeni 3D (wirtualny
    Trackball). Brak ograniczeń kątów – można wykonać dowolną
    liczbę pełnych obrotów w każdym kierunku.

• Prawy przycisk myszy (PPM) + przeciąganie (lub Shift + LPM):
    Obrót sceny wokół osi Z (roll – obrót w płaszczyźnie ekranu)
    względem środka obszaru rysowania.

• Edycja krzywej źródła światła (gdy aktywny jest tryb "Edytuj krzywą"):
    Złapanie LPM punktu kontrolnego (V0..Vn-1) na ekranie pozwala
    go przeciągać. Przesunięcie V0 automatycznie przesuwa punkt
    zamykający V_ostatni = V0, zachowując ciągłość pętli.

2. SKRÓTY KLAWIATUROWE
--------------------------------------------------------------------
• Spacja   – zatrzymanie / wznowienie animacji źródła światła
• R        – reset orientacji sceny do widoku początkowego
• E        – włączenie / wyłączenie trybu edycji krzywej światła
• 1        – przełączenie widoczności wieloboków Béziera
• 2        – przełączenie widoczności siatki trójkątów
• 3        – przełączenie widoczności wypełnionych trójkątów
• N        – włączenie / wyłączenie mapy wektorów normalnych
• F1       – otwarcie niniejszego okna dokumentacji

3. PANEL STEROWANIA (PO PRAWEJ STRONIE)
--------------------------------------------------------------------
• Scena i triangulacja:
    Suwak określa gęstość podziału siatki (N x N oczek na każdą
    powierzchnię). Przycisk "Wczytaj plik..." pozwala wczytać inny
    plik tekstowy zawierający grupy po 16 punktów kontrolnych.

• Oświetlenie (Lambert + Phong):
    Suwaki kd (0..1), ks (0..1) oraz m (1..100) działają globalnie
    dla całej sceny. Przycisk koloru zmienia barwę światła I_L.

• Kolor obiektu I_O (niezależny na powierzchnię):
    Lista rozwijana wybiera aktywną powierzchnię. Przyciski radiowe
    przełączają między jednolitym kolorem a teksturą (można wczytać
    własny obraz PNG/JPG/BMP).

• Mapa wektorów normalnych:
    Można włączyć modyfikację normalnych, wybrać jeden z 3 wbudowanych
    wzorów proceduralnych lub wczytać własną mapę normalnych z pliku.
""";

    private static string GetTrackballTechDescription() =>
"""
====================================================================
  OPIS TECHNICZNY: PRZEKSZTAŁCANIE RUCHU 2D MYSZY NA OBRÓT 3D
====================================================================

Zastosowano metodę wirtualnego Trackballa (hybryda sfery Shoemake'a
i hiperboloidy Holroyda) połączoną z inkrementalnym składaniem
macierzy obrotu 3x3 (TrackballController.cs + Matrix3x3.cs).

1. DLACZEGO TRACKBALL ZAMIAST KĄTÓW EULERA?
--------------------------------------------------------------------
Bezpośrednie mapowanie (dx -> beta, dy -> alfa) na kąty Eulera
powoduje efekt blokady przegubu (gimbal lock) oraz nieintuicyjne
odwracanie kierunków obrotu, gdy obiekt zostanie obrócony "do góry
nogami". Zastosowanie przyrostowych macierzy obrotu wokół osi
ekranowych rozwiązuje ten problem całkowicie:
• ruch myszy zawsze obraca obiekt zgodnie z kierunkiem kursora,
• kąty nie są w żaden sposób ograniczane,
• przejście poza okrąg wpisany w ekran płynnie przechodzi w obrót
  wokół osi Z.

2. RZUTOWANIE PUNKTU EKRANOWEGO NA POWIERZCHNIĘ TRACKBALLA
--------------------------------------------------------------------
Niech (W, H) będzie rozmiarem obszaru rysowania, a promieniem sfery
r = 0.45 * min(W, H). Dla pozycji kursora (p_x, p_y) wyznaczamy
znormalizowane współrzędne (oś Y skierowana w górę):

    x = (p_x - W / 2) / r
    y = (H / 2 - p_y) / r
    d^2 = x^2 + y^2

Współrzędną z wyznaczamy kawałkami (gładkie przejście klasy C^1
dla d = 1/sqrt(2)):

    • dla d^2 <= 0.5:   z = sqrt(1 - d^2)     (górna półsfera)
    • dla d^2 >  0.5:   z = 0.5 / sqrt(d^2)   (hiperboloida)

Następnie wektor jest normalizowany: u = (x, y, z) / ||(x, y, z)||.

3. WYZNACZENIE PRZYROSTOWEJ MACIERZY OBROTU (WZÓR RODRIGUESA)
--------------------------------------------------------------------
Dla dwóch kolejnych położeń kursora otrzymujemy wersory u_1 i u_2:
    • oś obrotu:   a = u_1 x u_2   (po normalizacji: k = a / ||a||)
    • kąt obrotu:  theta = arccos(clamp(u_1 · u_2, -1, 1)) * czułość

Na podstawie osi k = (k_x, k_y, k_z) i kąta theta budujemy macierz
przyrostu R_przyrost ze wzoru Rodriguesa:

    R(k, theta) = I * cos(theta) + (1 - cos(theta)) * (k * k^T)
                  + sin(theta) * [k]_x

Dla trybu Roll (PPM lub Shift + LPM) wyznaczana jest różnica kąta
biegunowego kursora względem środka ekranu:
    d_gamma = atan2(y_curr, x_curr) - atan2(y_prev, x_prev)
    R_przyrost = R_z(d_gamma).

4. SKŁADANIE NARASTAJĄCE I ORTONORMALIZACJA
--------------------------------------------------------------------
Nową orientację sceny wyznaczamy przez lewostronne złożenie macierzy
(konwencja wektorów kolumnowych P' = R * P):

    R_nowa = R_przyrost * R_aktualna

Po każdym kroku kolumny R_nowa są ortonormalizowane metodą
Grama-Schmidta (Matrix3x3.Orthonormalize), co zapobiega kumulacji
błędów zaokrągleń zmiennoprzecinkowych (skalowaniu lub ścinaniu
siatki po wielu obrotach).

Punkty wierzchołków oraz punkty kontrolne są obracane wokół środka
sceny (0,0,0), a wektory styczne Pu, Pv oraz normalny N są obracane
tą samą macierzą R_nowa i normalizowane.
""";

    private static string GetMathOverviewText() =>
"""
====================================================================
  MODEL MATEMATYCZNY I ZASTOSOWANE ALGORYTMY
====================================================================

1. POWIERZCHNIE BÉZIERA 3. STOPNIA (Models/BezierSurface.cs)
   P(u,v) = sum_{i=0..3} sum_{j=0..3} V_ij * B_{i,3}(u) * B_{j,3}(v)
   Pu = dP/du, Pv = dP/dv, N = normalize(Pu x Pv).

2. WYPEŁNIANIE TRÓJKĄTÓW (Rendering/ScanlineRasterizer.cs)
   Zastosowano algorytm scan-line z sortowaniem kubełkowym krawędzi
   (Edge Table + Active Edge Table). Krawędzie niepoziome są
   umieszczane w kubełkach indeksowanych przez y_start, aktywowane
   przy wejściu linii skanującej, sortowane po X i aktualizowane
   krokiem x += 1/m.

3. INTERPOLACJA BARYCENTRYCZNA I Z-BUFFER
   Wewnątrz każdego piksela wyznaczane są współrzędne barycentryczne
   (alpha, beta, gamma) służące do interpolacji głębokości z,
   wektora normalnego N, wektorów stycznych Pu, Pv oraz parametrów
   (u, v). Bufor głębokości (Z-buffer) przechowuje największe z
   dla każdego piksela (obserwator po stronie +Z, V = [0, 0, 1]).

4. MODEL OŚWIETLENIA I MAPA NORMALNYCH (Rendering/LightingModel.cs)
   I = kd * I_L * I_O * max(0, <N, L>)
       + ks * I_L * I_O * max(0, <V, R>)^m
   gdzie R = 2 * <N, L> * N - L.
   Uwzględniono dwie strony powierzchni (odwrócenie N gdy N_pow.z < 0).
   Dla włączonej mapy normalnych: N = normalize(M * N_tex), gdzie
   M = [Pu, Pv, N_pow].
""";
}