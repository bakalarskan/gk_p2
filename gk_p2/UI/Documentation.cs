using System.Drawing;
using System.Windows.Forms;

namespace BezierSurfacesApp.UI;

/// <summary>
/// Dokumentacja użytkownika oraz opis zastosowanych algorytmów.
/// </summary>
public sealed class DocumentationForm : Form
{
    private static readonly Font TitleFont = new("Segoe UI", 15f, FontStyle.Bold);
    private static readonly Font SectionFont = new("Segoe UI", 11f, FontStyle.Bold);
    private static readonly Font BodyFont = new("Segoe UI", 9.5f);
    private static readonly Font LabelFont = new("Segoe UI", 9.5f, FontStyle.Bold);
    private static readonly Font FormulaFont = new("Consolas", 9.5f);

    private static readonly Color TextColor = Color.FromArgb(38, 43, 50);
    private static readonly Color HeadingColor = Color.FromArgb(255, 22, 148);
    private static readonly Color SecondaryColor = Color.FromArgb(86, 93, 102);

    public DocumentationForm()
    {
        Text = "Dokumentacja – Powierzchnie Béziera 3D";
        Size = new Size(810, 660);
        MinimumSize = new Size(660, 500);
        StartPosition = FormStartPosition.CenterParent;
        Font = BodyFont;

        var tabs = new TabControl
        {
            Dock = DockStyle.Fill,
            Font = BodyFont
        };

        tabs.TabPages.Add(CreateTab("Instrukcja obsługi", BuildUserManual));
        tabs.TabPages.Add(CreateTab("Sterowanie obrotem 3D", BuildRotationDescription));
        tabs.TabPages.Add(CreateTab("Model i algorytmy", BuildAlgorithmDescription));

        Controls.Add(tabs);
    }

    private static TabPage CreateTab(string title, Action<RichTextBox> buildContent)
    {
        var page = new TabPage(title)
        {
            BackColor = Color.White,
            Padding = new Padding(16, 14, 16, 12)
        };

        var content = new RichTextBox
        {
            Dock = DockStyle.Fill,
            ReadOnly = true,
            BorderStyle = BorderStyle.None,
            BackColor = Color.White,
            ForeColor = TextColor,
            Font = BodyFont,
            WordWrap = true,
            DetectUrls = false,
            ScrollBars = RichTextBoxScrollBars.Vertical
        };

        page.Controls.Add(content);
        buildContent(content);
        content.SelectionStart = 0;
        content.SelectionLength = 0;
        return page;
    }

    private static void AppendText(RichTextBox box, string text, Font font, Color color, int indent = 0)
    {
        box.SelectionStart = box.TextLength;
        box.SelectionLength = 0;
        box.SelectionFont = font;
        box.SelectionColor = color;
        box.SelectionIndent = indent;
        box.AppendText(text + Environment.NewLine);
    }

    private static void Title(RichTextBox box, string text)
    {
        AppendText(box, text, TitleFont, HeadingColor);
        AppendText(box, "", BodyFont, TextColor);
    }

    private static void Section(RichTextBox box, string text)
    {
        AppendText(box, "", BodyFont, TextColor);
        AppendText(box, text, SectionFont, HeadingColor);
    }

    private static void Paragraph(RichTextBox box, string text) =>
        AppendText(box, text, BodyFont, TextColor);

    private static void Entry(RichTextBox box, string name, string description)
    {
        box.SelectionStart = box.TextLength;
        box.SelectionLength = 0;
        box.SelectionIndent = 12;
        box.SelectionFont = LabelFont;
        box.SelectionColor = TextColor;
        box.AppendText(name + " – ");
        box.SelectionFont = BodyFont;
        box.AppendText(description + Environment.NewLine);
    }
    private static void Formula(RichTextBox box, string text) =>
        AppendText(box, text, FormulaFont, SecondaryColor, 20);
    private static void BuildUserManual(RichTextBox box)
    {
        Title(box, "Instrukcja obsługi");
        Paragraph(box,
            "Program służy do wyświetlania i interaktywnego obracania powierzchni " +
            "Béziera trzeciego stopnia. Widok sceny jest rzutem prostokątnym " +
            "na płaszczyznę xy.");

        Section(box, "1. Wczytywanie powierzchni");
        Paragraph(box,
            "Przy uruchomieniu aplikacja próbuje odczytać plik Assets/surfaces.txt " +
            "z katalogu programu. Można również wskazać inny plik przyciskiem " +
            "„Wczytaj plik...” w panelu „Scena i triangulacja”.");
        Paragraph(box,
            "Każda powierzchnia jest określona przez 16 punktów kontrolnych " +
            "zapisanych w kolejnych wierszach. W każdym wierszu znajdują się " +
            "trzy współrzędne (x, y, z), oddzielone białymi znakami. " +
            "Plik musi zawierać co najmniej dwie grupy po 16 punktów.");

        Section(box, "2. Obracanie i wyświetlanie sceny");
        Entry(box, "LPM + przeciąganie",
            "swobodny obrót przestrzenny metodą wirtualnego trackballa.");
        Entry(box, "PPM + przeciąganie",
            "obrót wokół osi z, prostopadłej do płaszczyzny obrazu.");
        Entry(box, "Shift + LPM + przeciąganie",
            "alternatywne sterowanie obrotem wokół osi z.");
        Entry(box, "Reset obrotu (R)",
            "przywrócenie początkowej orientacji sceny.");
        Paragraph(box,
            "Suwak „Dokładność triangulacji” ustala liczbę podziałów " +
            "parametrów u i v. Dla wartości n każda powierzchnia składa się " +
            "z 2n² trójkątów. Pola wyboru pozwalają niezależnie wyświetlać " +
            "wieloboki kontrolne Béziera, siatkę trójkątów i ich wypełnienie.");
        Paragraph(box,
            "Wieloboki kontrolne są rysowane jako nakładka na obraz sceny. " +
            "Ich linie mogą być widoczne także nad fragmentami innych powierzchni.");

        Section(box, "3. Oświetlenie");
        Paragraph(box,
            "W panelu „Model oświetlenia” można ustawić współczynnik " +
            "rozproszenia kd, współczynnik odbicia zwierciadlanego ks, " +
            "wykładnik połysku m oraz kolor światła. Ustawienia obowiązują " +
            "dla wszystkich powierzchni.");
        Entry(box, "Edytuj punkty krzywej myszą",
    "po włączeniu trybu można przeciągać punkty kontrolne LPM; skrót: E.");
        Entry(box, "Reset",
            "przywrócenie pierwotnego położenia punktów kontrolnych krzywej.");
        Paragraph(box,
            "Pierwszy i ostatni punkt kontrolny są ze sobą powiązane, " +
            "aby krzywa pozostawała zamknięta. Aktualne położenie źródła " +
            "światła jest zaznaczone na krzywej.");

        Section(box, "5. Kolor i tekstury");
        Paragraph(box,
            "W panelu „Kolor obiektu” należy najpierw wybrać powierzchnię " +
            "z listy, a następnie wskazać jednolity kolor lub teksturę. " +
            "Ustawienia koloru i tekstury są niezależne dla każdej powierzchni. " +
            "Teksturę można wczytać z pliku PNG, JPG, JPEG lub BMP.");
        Paragraph(box,
            "Panel „Mapa wektorów normalnych” umożliwia włączenie modyfikacji " +
            "normalnych na podstawie mapy. Początkowo dostępna jest mapa " +
            "wygenerowana w programie; można ją zastąpić obrazem wczytanym " +
            "z pliku. Włączenie mapy działa na całą scenę.");

        Section(box, "6. Skróty klawiaturowe");
        Entry(box, "Spacja", "zatrzymanie lub wznowienie animacji.");
        Entry(box, "R", "przywrócenie początkowej orientacji sceny.");
        Entry(box, "E", "włączenie lub wyłączenie edycji krzywej światła.");
        Entry(box, "1", "widoczność wieloboków kontrolnych Béziera.");
        Entry(box, "2", "widoczność siatki trójkątów.");
        Entry(box, "3", "widoczność wypełnionych trójkątów.");
        Entry(box, "N", "włączenie lub wyłączenie mapy normalnych.");
        Entry(box, "F1", "otwarcie dokumentacji.");
    }

    private static void BuildRotationDescription(RichTextBox box)
    {
        Title(box, "Sterowanie obrotem 3D");
        Paragraph(box,
            "Do sterowania orientacją sceny zastosowano wirtualny trackball. " +
            "Każde przesunięcie myszy wyznacza osobny obrót przyrostowy, " +
            "który jest składany z aktualną macierzą orientacji. Rozwiązanie " +
            "nie wymaga ograniczania zakresu kątów obrotu.");

        Section(box, "1. Odwzorowanie pozycji kursora");
        Paragraph(box,
            "Niech W i H oznaczają szerokość i wysokość obszaru rysowania, " +
            "a (px, py) – współrzędne kursora. Środek układu jest przeniesiony " +
            "do środka obszaru rysowania, a zwrot osi y jest odwrócony:");
        Formula(box, "r = 0,45 · min(W, H)");
        Formula(box, "x = (px − W/2) / r");
        Formula(box, "y = (H/2 − py) / r");
        Formula(box, "d² = x² + y²");
        Paragraph(box,
            "Współrzędną z oblicza się z funkcji kawałkami określonej, " +
            "łączącej górną półsferę z powierzchnią hiperboliczną:");
        Formula(box, "z = sqrt(1 − d²)      dla d² ≤ 0,5");
        Formula(box, "z = 0,5 / sqrt(d²)    dla d² > 0,5");
        Paragraph(box,
            "Otrzymany wektor (x, y, z) jest normalizowany. Taki sposób " +
            "odwzorowania zapewnia ciągłość sterowania również poza " +
            "centralnym obszarem trackballa.");

        Section(box, "2. Oś i kąt obrotu");
        Paragraph(box,
            "Dla poprzedniej i bieżącej pozycji kursora wyznacza się " +
            "znormalizowane wektory u1 i u2. Oś obrotu wynika z ich " +
            "iloczynu wektorowego, a kąt – z iloczynu skalarnego:");
        Formula(box, "a = u1 × u2");
        Formula(box, "k = a / |a|");
        Formula(box, "θ = arccos(clamp(u1 · u2, −1, 1)) · s");
        Paragraph(box,
            "Parametr s określa czułość sterowania (wartość domyślna: 1,35). " +
            "Przy znikomej długości osi obrotu przyjmowana jest macierz " +
            "jednostkowa. Macierz obrotu wokół osi k jest wyznaczana " +
            "ze wzoru Rodriguesa:");
        Formula(box, "R(k, θ) = I cos θ + (1 − cos θ) kkᵀ + [k]× sin θ");
        Section(box, "3. Obrót wokół osi z");
        Paragraph(box,
            "Przy przeciąganiu PPM lub Shift + LPM zamiast trackballa " +
            "wykorzystuje się zmianę kąta biegunowego kursora względem " +
            "środka obszaru rysowania. Różnica kątów jest sprowadzana " +
            "do przedziału [−π, π], a następnie tworzy się macierz Rz(Δγ).");
        Formula(box, "Δγ = atan2(y2, x2) − atan2(y1, x1)");

        Section(box, "4. Aktualizacja orientacji");
        Paragraph(box,
            "W programie stosowana jest konwencja wektorów kolumnowych. " +
            "Nowy obrót jest składany lewostronnie z dotychczasową orientacją:");
        Formula(box, "Rnowa = Rprzyrost · Raktualna");
        Formula(box, "P' = Rnowa · P");
        Paragraph(box,
            "Po złożeniu macierzy jej kolumny są ortonormalizowane metodą " +
            "Grama–Schmidta w celu ograniczenia błędów numerycznych. " +
            "Każdy wierzchołek, punkt kontrolny oraz wektory styczne i " +
            "normalne są przekształcane na podstawie danych sprzed obrotu. " +
            "Obrót odbywa się wokół początku układu współrzędnych (0, 0, 0).");

        Section(box, "5. Rzutowanie");
        Paragraph(box,
            "Do wyświetlenia sceny używany jest rzut prostokątny na " +
            "płaszczyznę xy. Po obrocie współrzędne ekranowe są obliczane " +
            "z x' i y', natomiast z' służy do określania widoczności " +
            "fragmentów w buforze głębokości.");
        Formula(box, "xekran = W/2 + x'");
        Formula(box, "yekran = H/2 − y'");
    }

    private static void BuildAlgorithmDescription(RichTextBox box)
    {
        Title(box, "Model i algorytmy renderowania");

        Section(box, "1. Powierzchnia Béziera");
        Paragraph(box,
            "Każda powierzchnia jest płatem Béziera trzeciego stopnia, " +
            "określonym siatką 4 × 4 punktów kontrolnych. Punkt powierzchni " +
            "wyznaczany jest z wielomianów Bernsteina dla parametrów " +
            "u, v należących do przedziału [0, 1]:");
        Formula(box, "P(u,v) = Σ(i=0..3) Σ(j=0..3) Vij Bi,3(u) Bj,3(v)");
        Paragraph(box,
            "Wektory styczne są pochodnymi cząstkowymi Pu = ∂P/∂u " +
            "oraz Pv = ∂P/∂v. Normalna powierzchni jest wyznaczana " +
            "przez normalizację iloczynu wektorowego Pu × Pv.");

        Section(box, "2. Triangulacja");
        Paragraph(box,
            "Dziedzina parametrów jest dzielona na n × n oczek. " +
            "W każdym węźle wyznaczane są położenie, wektory styczne, " +
            "normalna i parametry (u, v). Każde oczko dzielone jest " +
            "na dwa trójkąty. Gęstość podziału reguluje suwak w panelu sceny.");

        Section(box, "3. Rasteryzacja metodą scan-line");
        Paragraph(box,
            "Wypełnianie odbywa się programowo metodą linii skanującej " +
            "z kubełkowym sortowaniem krawędzi (Edge Table). " +
            "Niepoziome krawędzie trójkąta są dodawane do kubełków " +
            "odpowiadających początkowej współrzędnej y. W kolejnych " +
            "wierszach aktywne krawędzie są sortowane według x, " +
            "a przedziały między parami przecięć są wypełniane pikselami.");
        Formula(box, "xnastępne = xbieżące + Δx/Δy");
        Paragraph(box,
            "Dla każdego piksela wyznaczane są współrzędne " +
            "barycentryczne (α, β, γ). Służą one do interpolowania " +
            "głębokości z, wektora normalnego oraz parametrów " +
            "tekstury u i v. Przy włączonej mapie normalnych " +
            "interpolowane są również wektory styczne.");

        Section(box, "4. Bufor głębokości");
        Paragraph(box,
            "Dla każdego piksela przechowywana jest największa " +
            "dotychczas wyznaczona wartość z. Nowy fragment zastępuje " +
            "poprzedni wyłącznie wtedy, gdy znajduje się bliżej " +
            "obserwatora, który jest przyjęty po stronie dodatniej osi z. " +
            "Zapewnia to poprawne wzajemne przesłanianie wypełnionych " +
            "powierzchni. Linie wieloboków kontrolnych są rysowane " +
            "osobno jako nakładka 2D.");

        Section(box, "5. Model oświetlenia");
        Paragraph(box,
            "Kolor piksela jest obliczany ze składowej rozproszonej " +
            "Lamberta i składowej zwierciadlanej Phonga, osobno dla " +
            "kanałów R, G i B:");
        Formula(box, "I = IL · IO · [kd max(0, N·L) + ks max(0, V·R)^m]");
        Formula(box, "R = 2(N·L)N − L,     V = (0, 0, 1)");
        Paragraph(box,
            "N oznacza normalną powierzchni, L kierunek do źródła " +
            "światła, a R kierunek odbicia. Wektory kierunkowe są " +
            "normalizowane. Dla powierzchni zwróconej tyłem do obserwatora " +
            "normalna używana w oświetleniu jest odwracana. " +
            "Kolor wynikowy jest ograniczany do zakresu 0–255.");
        Section(box, "6. Tekstura i mapa normalnych");
        Paragraph(box,
            "Kolor obiektu IO może być stały albo odczytywany z tekstury " +
            "na podstawie parametrów (u, v). Zewnętrzna mapa normalnych " +
            "jest dekodowana z kanałów RGB do składowych wektora, " +
            "który następnie jest normalizowany.");
        Formula(box, "N' = normalize(Pu · nx + Pv · ny + N · nz)");
        Paragraph(box,
            "We wzorze Pu i Pv oznaczają znormalizowane styczne " +
            "wyznaczone na powierzchni, a (nx, ny, nz) – wektor " +
            "odczytany z mapy normalnych. Składowa nz jest dodatnia. " +
            "Zmodyfikowana normalna jest wykorzystywana do obliczenia " +
            "oświetlenia piksela.");

        Section(box, "7. Ruch źródła światła");
        Paragraph(box,
            "Tor źródła światła tworzą dwa połączone segmenty " +
            "sześciennej krzywej Béziera. Położenie w płaszczyźnie xy " +
            "jest obliczane z wielomianów Bernsteina, natomiast " +
            "współrzędna z pozostaje stała i jest regulowana suwakiem. " +
            "Parametr animacji jest aktualizowany na podstawie czasu " +
            "między kolejnymi odświeżeniami.");
    }
    }