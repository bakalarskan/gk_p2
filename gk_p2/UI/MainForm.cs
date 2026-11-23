using System.Diagnostics;
using System.Numerics;
using BezierSurfacesApp.IO;
using BezierSurfacesApp.Math3D;
using BezierSurfacesApp.Models;
using BezierSurfacesApp.Rendering;

namespace BezierSurfacesApp.UI;

public sealed class MainForm : Form
{
    // Subklasa Panelu z włączonym podwójnym buforowaniem (brak migotania)
    private sealed class CanvasPanel : Panel
    {
        public CanvasPanel()
        {
            DoubleBuffered = true;
            ResizeRedraw = true;
        }
    }

    private readonly Scene _scene = new();
    private readonly SceneRenderer _renderer = new();
    private readonly TrackballController _trackball = new();

    private DirectBitmap? _frameBuffer;
    private readonly CanvasPanel _canvas = new();
    private readonly System.Windows.Forms.Timer _animationTimer = new();
    private long _lastTickMs;

    private int _draggedLightPointIndex = -1;
    private int _hoveredLightPointIndex = -1;
    private bool _suppressUiEvents;

    // Kontrolki wymagające synchronizacji ze skrótami klawiaturowymi i wyborem powierzchni
    private CheckBox _chkControlPoly = null!;
    private CheckBox _chkWireframe = null!;
    private CheckBox _chkFilled = null!;
    private CheckBox _chkLightCurve = null!;
    private CheckBox _chkEditLightCurve = null!;
    private CheckBox _chkNormalMap = null!;

    private ComboBox _cmbSurfaceSelect = null!;
    private RadioButton _rbSolidColor = null!;
    private RadioButton _rbTexture = null!;
    private Button _btnSurfaceColor = null!;
    private Label _lblTextureName = null!;

    private Button _btnLightColor = null!;
    private Button _btnToggleAnim = null!;
    private Label _lblTriangulation = null!;
    private Label _lblStatus = null!;

    public MainForm()
    {
        Text = "Powierzchnie Béziera 3D – Grafika Komputerowa";
        Size = new Size(1220, 780);
        MinimumSize = new Size(980, 640);
        StartPosition = FormStartPosition.CenterScreen;
        KeyPreview = true;

        InitializeUi();
        LoadInitialSurfaces();

        _lastTickMs = Stopwatch.GetTimestamp();
        _animationTimer.Interval = 16; // ~60 FPS
        _animationTimer.Tick += AnimationTimer_Tick;
        _animationTimer.Start();

        KeyDown += MainForm_KeyDown;
    }

    private void InitializeUi()
    {
        // Pasek statusu na dole
        var statusPanel = new Panel
        {
            Dock = DockStyle.Bottom,
            Height = 26,
            BackColor = Color.FromArgb(34, 38, 46),
            Padding = new Padding(10, 4, 10, 4)
        };
        _lblStatus = new Label
        {
            Dock = DockStyle.Fill,
            ForeColor = Color.Gainsboro,
            Font = new Font("Segoe UI", 8.8f),
            Text = "Gotowe. LPM: obrót 3D | PPM / Shift+LPM: obrót Z | F1: dokumentacja"
        };
        statusPanel.Controls.Add(_lblStatus);

        // Panel boczny z kontrolkami
        var sidePanel = new Panel
        {
            Dock = DockStyle.Right,
            Width = 325,
            AutoScroll = true,
            BackColor = Color.FromArgb(245, 246, 248),
            Padding = new Padding(8)
        };

        var stack = new FlowLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false,
            Width = 290
        };

        stack.Controls.Add(BuildSceneGroup());
        stack.Controls.Add(BuildLightingGroup());
        stack.Controls.Add(BuildLightCurveGroup());
        stack.Controls.Add(BuildSurfaceAppearanceGroup());
        stack.Controls.Add(BuildNormalMapGroup());
        stack.Controls.Add(BuildHelpGroup());

        sidePanel.Controls.Add(stack);

        // Obszar rysowania
        _canvas.Dock = DockStyle.Fill;
        _canvas.BackColor = Color.FromArgb(24, 27, 34);
        _canvas.Paint += Canvas_Paint;
        _canvas.Resize += Canvas_Resize;
        _canvas.MouseDown += Canvas_MouseDown;
        _canvas.MouseMove += Canvas_MouseMove;
        _canvas.MouseUp += Canvas_MouseUp;

        Controls.Add(_canvas);
        Controls.Add(sidePanel);
        Controls.Add(statusPanel);
    }

    private GroupBox BuildSceneGroup()
    {
        var grp = CreateGroupBox("1. Scena i triangulacja");
        var layout = CreateInnerStack();

        _lblTriangulation = new Label
        {
            AutoSize = true,
            Text = $"Dokładność triangulacji: {_scene.TriangulationDivisions}x{_scene.TriangulationDivisions}"
        };
        var trkDiv = new TrackBar
        {
            Minimum = 4,
            Maximum = 45,
            Value = _scene.TriangulationDivisions,
            TickFrequency = 5,
            Width = 260,
            Height = 36
        };
        trkDiv.ValueChanged += (_, _) =>
        {
            _scene.TriangulationDivisions = trkDiv.Value;
            _scene.RebuildTriangulation();
            UpdateTriangulationLabel();
            _canvas.Invalidate();
        };

        _chkControlPoly = new CheckBox { Text = "Wieloboki Béziera", Checked = _scene.ShowControlPolygon, AutoSize = true };
        _chkWireframe = new CheckBox { Text = "Siatka trójkątów", Checked = _scene.ShowWireframe, AutoSize = true };
        _chkFilled = new CheckBox { Text = "Wypełnione trójkąty", Checked = _scene.ShowFilledTriangles, AutoSize = true };
        _chkLightCurve = new CheckBox { Text = "Krzywa źródła światła", Checked = _scene.ShowLightCurve, AutoSize = true };

        _chkControlPoly.CheckedChanged += (_, _) => { _scene.ShowControlPolygon = _chkControlPoly.Checked; _canvas.Invalidate(); };
        _chkWireframe.CheckedChanged += (_, _) => { _scene.ShowWireframe = _chkWireframe.Checked; _canvas.Invalidate(); };
        _chkFilled.CheckedChanged += (_, _) => { _scene.ShowFilledTriangles = _chkFilled.Checked; _canvas.Invalidate(); };
        _chkLightCurve.CheckedChanged += (_, _) => { _scene.ShowLightCurve = _chkLightCurve.Checked; _canvas.Invalidate(); };

        var btnRow = new FlowLayoutPanel { AutoSize = true, FlowDirection = FlowDirection.LeftToRight };
        var btnLoad = new Button { Text = "Wczytaj plik...", Width = 124, Height = 28 };
        btnLoad.Click += BtnLoadSurfaces_Click;
        var btnResetRot = new Button { Text = "Reset obrotu (R)", Width = 128, Height = 28 };
        btnResetRot.Click += (_, _) => { _scene.ResetRotation(); _canvas.Invalidate(); };
        btnRow.Controls.AddRange([btnLoad, btnResetRot]);

        layout.Controls.AddRange([_lblTriangulation, trkDiv, _chkControlPoly, _chkWireframe, _chkFilled, _chkLightCurve, btnRow]);
        grp.Controls.Add(layout);
        return grp;
    }

    private GroupBox BuildLightingGroup()
    {
        var grp = CreateGroupBox("2. Model oświetlenia");
        var layout = CreateInnerStack();

        var lblKd = new Label { AutoSize = true, Text = $"kd (rozproszone): {_scene.Kd:F2}" };
        var trkKd = new TrackBar { Minimum = 0, Maximum = 100, Value = (int)(_scene.Kd * 100), TickFrequency = 10, Width = 260, Height = 34 };
        trkKd.ValueChanged += (_, _) =>
        {
            _scene.Kd = trkKd.Value / 100f;
            lblKd.Text = $"kd (rozproszone): {_scene.Kd:F2}";
            _canvas.Invalidate();
        };

        var lblKs = new Label { AutoSize = true, Text = $"ks (zwierciadlane): {_scene.Ks:F2}" };
        var trkKs = new TrackBar { Minimum = 0, Maximum = 100, Value = (int)(_scene.Ks * 100), TickFrequency = 10, Width = 260, Height = 34 };
        trkKs.ValueChanged += (_, _) =>
        {
            _scene.Ks = trkKs.Value / 100f;
            lblKs.Text = $"ks (zwierciadlane): {_scene.Ks:F2}";
            _canvas.Invalidate();
        };

        var lblM = new Label { AutoSize = true, Text = $"m (połysk): {_scene.ShininessM}" };
        var trkM = new TrackBar { Minimum = 1, Maximum = 100, Value = _scene.ShininessM, TickFrequency = 10, Width = 260, Height = 34 };
        trkM.ValueChanged += (_, _) =>
        {
            _scene.ShininessM = trkM.Value;
            lblM.Text = $"m (połysk): {_scene.ShininessM}";
            _canvas.Invalidate();
        };

        _btnLightColor = new Button
        {
            Text = "Kolor światła (I_L)",
            Width = 258,
            Height = 28,
            BackColor = _scene.LightColor
        };
        _btnLightColor.Click += (_, _) =>
        {
            using var dlg = new ColorDialog { Color = _scene.LightColor, FullOpen = true };
            if (dlg.ShowDialog(this) == DialogResult.OK)
            {
                _scene.LightColor = dlg.Color;
                _btnLightColor.BackColor = dlg.Color;
                _btnLightColor.ForeColor = GetReadableForeColor(dlg.Color);
                _canvas.Invalidate();
            }
        };

        layout.Controls.AddRange([lblKd, trkKd, lblKs, trkKs, lblM, trkM, _btnLightColor]);
        grp.Controls.Add(layout);
        return grp;
    }

    private GroupBox BuildLightCurveGroup()
    {
        var grp = CreateGroupBox("3. Źródło światła (krzywa Béziera)");
        var layout = CreateInnerStack();

        _btnToggleAnim = new Button { Text = "Zatrzymaj animację (Spacja)", Width = 258, Height = 28 };
        _btnToggleAnim.Click += (_, _) => ToggleLightAnimation();

        var lblZ = new Label { AutoSize = true, Text = $"Płaszczyzna Z światła: {_scene.LightCurve.Z:F0}" };
        var trkZ = new TrackBar { Minimum = -300, Maximum = 450, Value = (int)_scene.LightCurve.Z, TickFrequency = 50, Width = 260, Height = 34 };
        trkZ.ValueChanged += (_, _) =>
        {
            _scene.LightCurve.Z = trkZ.Value;
            lblZ.Text = $"Płaszczyzna Z światła: {_scene.LightCurve.Z:F0}";
            _canvas.Invalidate();
        };

        _chkEditLightCurve = new CheckBox
        {
            Text = "Edytuj punkty krzywej myszą (E)",
            Checked = _scene.EditLightCurveMode,
            AutoSize = true
        };
        _chkEditLightCurve.CheckedChanged += (_, _) =>
        {
            _scene.EditLightCurveMode = _chkEditLightCurve.Checked;
            if (_scene.EditLightCurveMode && !_chkLightCurve.Checked)
                _chkLightCurve.Checked = true;
            _canvas.Invalidate();
        };

        var btnResetCurve = new Button { Text = "Reset", Width = 82, Height = 26 };
        btnResetCurve.Click += (_, _) => { _scene.LightCurve.ResetToDefault(); _canvas.Invalidate(); };

        layout.Controls.AddRange([_btnToggleAnim, lblZ, trkZ, _chkEditLightCurve, btnResetCurve]);
        grp.Controls.Add(layout);

        return grp;
    }

    private GroupBox BuildSurfaceAppearanceGroup()
    {
        var grp = CreateGroupBox("4. Kolor obiektu I_O (niezależny)");
        var layout = CreateInnerStack();

        var lblSurf = new Label { AutoSize = true, Text = "Wybierz powierzchnię:" };
        _cmbSurfaceSelect = new ComboBox
        {
            DropDownStyle = ComboBoxStyle.DropDownList,
            Width = 258
        };
        _cmbSurfaceSelect.SelectedIndexChanged += (_, _) => SyncSurfaceAppearanceControls();

        _rbSolidColor = new RadioButton { Text = "Jednolity kolor", AutoSize = true, Checked = true };
        _rbTexture = new RadioButton { Text = "Tekstura (mapowanie u, v)", AutoSize = true };

        _rbSolidColor.CheckedChanged += (_, _) =>
        {
            if (_suppressUiEvents || !_rbSolidColor.Checked) return;
            var surf = GetSelectedSurface();
            if (surf != null)
            {
                surf.ColorMode = SurfaceColorMode.SolidColor;
                _canvas.Invalidate();
            }
        };

        _rbTexture.CheckedChanged += (_, _) =>
        {
            if (_suppressUiEvents || !_rbTexture.Checked) return;
            var surf = GetSelectedSurface();
            if (surf != null)
            {
                surf.ColorMode = SurfaceColorMode.Texture;
                _canvas.Invalidate();
            }
        };

        _btnSurfaceColor = new Button { Text = "Wybierz kolor powierzchni...", Width = 258, Height = 28 };
        _btnSurfaceColor.Click += (_, _) =>
        {
            var surf = GetSelectedSurface();
            if (surf == null) return;

            using var dlg = new ColorDialog { Color = surf.SolidColor, FullOpen = true };
            if (dlg.ShowDialog(this) == DialogResult.OK)
            {
                surf.SolidColor = dlg.Color;
                surf.ColorMode = SurfaceColorMode.SolidColor;
                SyncSurfaceAppearanceControls();
                _canvas.Invalidate();
            }
        };

        var btnLoadTex = new Button { Text = "Wczytaj teksturę z pliku...", Width = 258, Height = 28 };
        btnLoadTex.Click += (_, _) =>
        {
            var surf = GetSelectedSurface();
            if (surf == null) return;

            using var dlg = new OpenFileDialog
            {
                Title = $"Wybierz teksturę dla: {surf.Name}",
                Filter = "Obrazy|*.png;*.jpg;*.jpeg;*.bmp"
            };
            if (dlg.ShowDialog(this) == DialogResult.OK)
            {
                try
                {
                    surf.Texture = TextureBuffer.FromFile(dlg.FileName);
                    surf.ColorMode = SurfaceColorMode.Texture;
                    SyncSurfaceAppearanceControls();
                    _canvas.Invalidate();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Nie udało się wczytać tekstury:\n{ex.Message}", "Błąd", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        };

        _lblTextureName = new Label { AutoSize = true, ForeColor = Color.DimGray, Text = "Tekstura: -" };

        layout.Controls.AddRange([lblSurf, _cmbSurfaceSelect, _rbSolidColor, _btnSurfaceColor, _rbTexture, btnLoadTex, _lblTextureName]);
        grp.Controls.Add(layout);
        return grp;
    }

    private GroupBox BuildNormalMapGroup()
    {
        var grp = CreateGroupBox("5. Mapa wektorów normalnych");
        var layout = CreateInnerStack();

        _chkNormalMap = new CheckBox { Text = "Włącz mapę normalnych (N)", Checked = _scene.UseNormalMap, AutoSize = true };
        _chkNormalMap.CheckedChanged += (_, _) =>
        {
            _scene.UseNormalMap = _chkNormalMap.Checked;
            _canvas.Invalidate();
        };

        var btnLoadNormal = new Button { Text = "Wczytaj NormalMap z pliku...", Width = 258, Height = 28 };
        btnLoadNormal.Click += (_, _) =>
        {
            using var dlg = new OpenFileDialog
            {
                Title = "Wybierz mapę wektorów normalnych",
                Filter = "Obrazy|*.png;*.jpg;*.jpeg;*.bmp"
            };
            if (dlg.ShowDialog(this) == DialogResult.OK)
            {
                try
                {
                    _scene.NormalMap = TextureBuffer.FromFile(dlg.FileName);
                    _chkNormalMap.Checked = true;
                    _canvas.Invalidate();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Nie udało się wczytać mapy normalnych:\n{ex.Message}", "Błąd", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        };

        layout.Controls.AddRange([_chkNormalMap, btnLoadNormal]);
        grp.Controls.Add(layout);
        return grp;
    }

    private GroupBox BuildHelpGroup()
    {
        var grp = CreateGroupBox("6. Dokumentacja");
        var layout = CreateInnerStack();

        var btnDocs = new Button
        {
            Text = "Otwórz dokumentację i sterowanie (F1)",
            Width = 258,
            Height = 32,
            BackColor = Color.FromArgb(45, 95, 165),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat
        };
        btnDocs.Click += (_, _) => OpenDocumentation();

        layout.Controls.Add(btnDocs);
        grp.Controls.Add(layout);
        return grp;
    }

    private void LoadInitialSurfaces()
    {
        string defaultPath = Path.Combine(AppContext.BaseDirectory, "Assets", "surfaces.txt");
        try
        {
            LoadSurfacesFromPath(defaultPath);
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"Nie udało się wczytać domyślnego pliku powierzchni ({defaultPath}):\n{ex.Message}",
                "Błąd wczytywania sceny",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
        }
    }

    private void LoadSurfacesFromPath(string path)
    {
        var loaded = SurfaceFileLoader.LoadFromFile(path);
        _scene.Surfaces.Clear();
        _scene.Surfaces.AddRange(loaded);

        _scene.RebuildTriangulation();
        _scene.ResetRotation();

        _cmbSurfaceSelect.Items.Clear();
        foreach (var s in _scene.Surfaces)
            _cmbSurfaceSelect.Items.Add(s.Name);

        if (_cmbSurfaceSelect.Items.Count > 0)
            _cmbSurfaceSelect.SelectedIndex = 0;

        UpdateTriangulationLabel();
        SyncSurfaceAppearanceControls();
        _canvas.Invalidate();
    }

    private void BtnLoadSurfaces_Click(object? sender, EventArgs e)
    {
        using var dlg = new OpenFileDialog
        {
            Title = "Wybierz plik tekstowy z powierzchniami Béziera",
            Filter = "Pliki tekstowe|*.txt|Wszystkie pliki|*.*"
        };
        if (dlg.ShowDialog(this) == DialogResult.OK)
        {
            try
            {
                LoadSurfacesFromPath(dlg.FileName);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Błąd wczytywania pliku:\n{ex.Message}", "Błąd", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }

    private BezierSurface? GetSelectedSurface()
    {
        int idx = _cmbSurfaceSelect.SelectedIndex;
        return idx >= 0 && idx < _scene.Surfaces.Count ? _scene.Surfaces[idx] : null;
    }

    private void SyncSurfaceAppearanceControls()
    {
        var surf = GetSelectedSurface();
        if (surf == null) return;

        _suppressUiEvents = true;
        _rbSolidColor.Checked = surf.ColorMode == SurfaceColorMode.SolidColor;
        _rbTexture.Checked = surf.ColorMode == SurfaceColorMode.Texture;
        _btnSurfaceColor.BackColor = surf.SolidColor;
        _btnSurfaceColor.ForeColor = GetReadableForeColor(surf.SolidColor);
        _lblTextureName.Text = $"Tekstura: {surf.Texture.Name}";
        _suppressUiEvents = false;
    }

    private void UpdateTriangulationLabel()
    {
        int n = _scene.TriangulationDivisions;
        _lblTriangulation.Text = $"Dokładność triangulacji: {n}x{n} ({_scene.TotalTriangles} trójkątów)";
    }

    private void ToggleLightAnimation()
    {
        _scene.LightCurve.IsAnimating = !_scene.LightCurve.IsAnimating;
        _btnToggleAnim.Text = _scene.LightCurve.IsAnimating
            ? "Zatrzymaj animację (Spacja)"
            : "Wznów animację (Spacja)";
    }

    private void OpenDocumentation()
    {
        using var docs = new DocumentationForm();
        docs.ShowDialog(this);
    }

    // --- Pętla animacji i renderowanie ---

    private void AnimationTimer_Tick(object? sender, EventArgs e)
    {
        long now = Stopwatch.GetTimestamp();
        float dt = (float)(now - _lastTickMs) / Stopwatch.Frequency;
        _lastTickMs = now;

        if (_scene.LightCurve.IsAnimating)
        {
            _scene.LightCurve.Update(MathF.Min(dt, 0.1f));
            _canvas.Invalidate();
        }
    }

    private void Canvas_Resize(object? sender, EventArgs e)
    {
        if (_canvas.Width <= 0 || _canvas.Height <= 0) return;

        _frameBuffer?.Dispose();
        _frameBuffer = new DirectBitmap(_canvas.Width, _canvas.Height);
        _canvas.Invalidate();
    }

    private void Canvas_Paint(object? sender, PaintEventArgs e)
    {
        if (_canvas.Width <= 0 || _canvas.Height <= 0) return;

        if (_frameBuffer == null || _frameBuffer.Width != _canvas.Width || _frameBuffer.Height != _canvas.Height)
        {
            _frameBuffer?.Dispose();
            _frameBuffer = new DirectBitmap(_canvas.Width, _canvas.Height);
        }

        _renderer.Render(_frameBuffer, _scene, _hoveredLightPointIndex);
        e.Graphics.DrawImageUnscaled(_frameBuffer.Bitmap, 0, 0);
    }

    // --- Obsługa myszy i klawiatury ---

    private Vector2 ScreenToWorld2D(Point p) =>
        new(p.X - _canvas.Width * 0.5f, _canvas.Height * 0.5f - p.Y);

    private void Canvas_MouseDown(object? sender, MouseEventArgs e)
    {
        if (e.Button == MouseButtons.Left && _scene.EditLightCurveMode)
        {
            int hit = _scene.LightCurve.HitTestControlPoint(ScreenToWorld2D(e.Location));
            if (hit >= 0)
            {
                _draggedLightPointIndex = hit;
                _canvas.Cursor = Cursors.Hand;
                return;
            }
        }

        if (e.Button == MouseButtons.Left || e.Button == MouseButtons.Right)
        {
            bool rollMode = e.Button == MouseButtons.Right || (ModifierKeys & Keys.Shift) == Keys.Shift;
            _trackball.BeginDrag(e.Location, rollMode);
            _canvas.Cursor = Cursors.SizeAll;
        }
    }

    private void Canvas_MouseMove(object? sender, MouseEventArgs e)
    {
        if (_draggedLightPointIndex >= 0)
        {
            _scene.LightCurve.MoveControlPoint(_draggedLightPointIndex, ScreenToWorld2D(e.Location));
            _canvas.Invalidate();
            return;
        }

        if (_trackball.IsDragging)
        {
            Matrix3x3 delta = _trackball.ComputeDeltaRotation(e.Location, _canvas.Size);
            _scene.ApplyDeltaRotation(delta);
            _canvas.Invalidate();
            return;
        }

        if (_scene.EditLightCurveMode)
        {
            int hover = _scene.LightCurve.HitTestControlPoint(ScreenToWorld2D(e.Location));
            if (hover != _hoveredLightPointIndex)
            {
                _hoveredLightPointIndex = hover;
                _canvas.Cursor = hover >= 0 ? Cursors.Hand : Cursors.Default;
                _canvas.Invalidate();
            }
        }
    }

    private void Canvas_MouseUp(object? sender, MouseEventArgs e)
    {
        _draggedLightPointIndex = -1;
        _trackball.EndDrag();
        _canvas.Cursor = Cursors.Default;
    }

    private void MainForm_KeyDown(object? sender, KeyEventArgs e)
    {
        switch (e.KeyCode)
        {
            case Keys.Space:
                ToggleLightAnimation();
                e.Handled = true;
                break;
            case Keys.R:
                _scene.ResetRotation();
                _canvas.Invalidate();
                e.Handled = true;
                break;
            case Keys.E:
                _chkEditLightCurve.Checked = !_chkEditLightCurve.Checked;
                e.Handled = true;
                break;
            case Keys.D1:
                _chkControlPoly.Checked = !_chkControlPoly.Checked;
                e.Handled = true;
                break;
            case Keys.D2:
                _chkWireframe.Checked = !_chkWireframe.Checked;
                e.Handled = true;
                break;
            case Keys.D3:
                _chkFilled.Checked = !_chkFilled.Checked;
                e.Handled = true;
                break;
            case Keys.N:
                _chkNormalMap.Checked = !_chkNormalMap.Checked;
                e.Handled = true;
                break;
            case Keys.F1:
                OpenDocumentation();
                e.Handled = true;
                break;
        }
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _animationTimer.Stop();
            _animationTimer.Dispose();
            _frameBuffer?.Dispose();
        }
        base.Dispose(disposing);
    }

    // --- Pomocnicze metody UI ---

    private static GroupBox CreateGroupBox(string title) => new()
    {
        Text = title,
        AutoSize = true,
        MinimumSize = new Size(286, 0),
        Padding = new Padding(8, 6, 8, 8),
        Margin = new Padding(0, 0, 0, 8),
        Font = new Font("Segoe UI", 8.8f, FontStyle.Bold)
    };

    private static FlowLayoutPanel CreateInnerStack() => new()
    {
        Dock = DockStyle.Fill,
        AutoSize = true,
        FlowDirection = FlowDirection.TopDown,
        WrapContents = false,
        Font = new Font("Segoe UI", 8.5f, FontStyle.Regular)
    };

    private static Color GetReadableForeColor(Color bg)
    {
        double luminance = 0.299 * bg.R + 0.587 * bg.G + 0.114 * bg.B;
        return luminance > 140 ? Color.Black : Color.White;
    }
}