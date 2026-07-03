using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using System.Windows.Shapes;
using System.Windows.Threading;
using Brush = System.Windows.Media.Brush;
using Brushes = System.Windows.Media.Brushes;
using Color = System.Windows.Media.Color;
using ColorConverter = System.Windows.Media.ColorConverter;
using Drawing = System.Drawing;
using FontFamily = System.Windows.Media.FontFamily;
using Forms = System.Windows.Forms;
using HorizontalAlignment = System.Windows.HorizontalAlignment;
using Image = System.Windows.Controls.Image;
using KeyEventArgs = System.Windows.Input.KeyEventArgs;
using MouseEventArgs = System.Windows.Input.MouseEventArgs;
using Orientation = System.Windows.Controls.Orientation;
using Point = System.Windows.Point;
using Rectangle = System.Windows.Shapes.Rectangle;
using VerticalAlignment = System.Windows.VerticalAlignment;

namespace TopoShell.App;

public partial class MainWindow : Window
{
    private readonly DispatcherTimer _telemetryTimer = new() { Interval = TimeSpan.FromSeconds(1) };
    private readonly DispatcherTimer _audioTimer = new() { Interval = TimeSpan.FromMilliseconds(80) };
    private readonly DispatcherTimer _mediaTimer = new() { Interval = TimeSpan.FromSeconds(4) };
    private readonly SystemTelemetry _telemetry = new();
    private readonly AudioMonitor _audioMonitor = new();
    private readonly MediaSessionMonitor _mediaMonitor = new();
    private readonly SolidColorBrush _albumFrameBrush = new(Color.FromArgb(150, 255, 255, 255));
    private readonly List<Line> _gridLines = [];
    private readonly List<Polyline> _terrainLines = [];
    private readonly AxisAngleRotation3D _corePitch = new(new Vector3D(1, 0, 0), -10);
    private readonly AxisAngleRotation3D _coreYaw = new(new Vector3D(0, 0, 1), 0);
    private readonly AxisAngleRotation3D _coreRoll = new(new Vector3D(0, 1, 0), 0);
    private readonly Transform3DGroup _coreTransform = new();
    private readonly List<ScaleTransform3D> _audioBarScales = [];
    private readonly ScaleTransform _albumPulse = new(1, 1);
    private Forms.NotifyIcon? _trayIcon;
    private Forms.ToolStripMenuItem? _trayVisibilityItem;
    private Forms.ToolStripMenuItem? _trayMotionItem;
    private Forms.ToolStripMenuItem? _trayAudioItem;
    private Forms.ToolStripMenuItem? _trayTopmostItem;
    private Point _parallaxTarget;
    private Point _parallax;
    private Vector _parallaxVelocity;
    private double _audioLevel;
    private double _phase;
    private int _renderFrame;
    private bool _mediaUpdatePending;
    private bool _motionEnabled = true;
    private bool _audioEnabled = true;
    private TextBlock? _clock3dText;
    private TextBlock? _date3dText;
    private TextBlock? _cpu3dValueText;
    private TextBlock? _memory3dValueText;
    private TextBlock? _gpu3dValueText;
    private TextBlock? _memory3dDetailText;
    private ScaleTransform? _cpu3dScale;
    private ScaleTransform? _memory3dScale;
    private ScaleTransform? _gpu3dScale;
    private Image? _albumArtImage;
    private Border? _albumFrameBorder;
    private TextBlock? _albumPlaceholderText;
    private TextBlock? _mediaTitleText;
    private TextBlock? _mediaArtistText;
    private TextBlock? _mediaAlbumText;

    public MainWindow()
    {
        InitializeComponent();
        CreateTrayIcon();

        _telemetryTimer.Tick += (_, _) => UpdateTelemetry();
        _audioTimer.Tick += (_, _) => UpdateAudioLevel();
        _mediaTimer.Tick += async (_, _) => await UpdateMediaAsync();
        CompositionTarget.Rendering += OnRendering;
        StateChanged += (_, _) =>
        {
            if (WindowState == WindowState.Minimized)
            {
                HideToTray();
            }
        };

        Closed += (_, _) =>
        {
            CompositionTarget.Rendering -= OnRendering;
            _telemetryTimer.Stop();
            _audioTimer.Stop();
            _mediaTimer.Stop();
            _audioMonitor.Dispose();
            _trayIcon?.Dispose();
        };
    }

    private async void Window_Loaded(object sender, RoutedEventArgs e)
    {
        Focus();
        BuildCoreModel();
        UpdateTelemetry();
        UpdateAudioLevel();
        _telemetryTimer.Start();
        _audioTimer.Start();
        _mediaTimer.Start();
        BuildGrid();
        BuildTerrain();
        ApplyCoreDirection();
        RefreshRuntimeState();
        await UpdateMediaAsync();
    }

    private void Window_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        BuildGrid();
        BuildTerrain();
    }

    private void Window_MouseMove(object sender, MouseEventArgs e)
    {
        if (!_motionEnabled)
        {
            return;
        }

        var position = e.GetPosition(this);
        var width = Math.Max(ActualWidth, 1);
        var height = Math.Max(ActualHeight, 1);

        _parallaxTarget = new Point(
            ((position.X / width) - 0.5) * 2,
            ((position.Y / height) - 0.5) * 2);
    }

    private void Window_MouseLeave(object sender, MouseEventArgs e)
    {
        _parallaxTarget = new Point();
    }

    private void ApplyCoreDirection()
    {
        var angle = Math.Atan2(_parallax.Y, _parallax.X);
        if (double.IsNaN(angle))
        {
            angle = -Math.PI / 2;
        }

        var displayAngle = (angle * 180 / Math.PI + 360) % 360;

        var easeX = SmoothStep(_parallax.X);
        var easeY = SmoothStep(_parallax.Y);

        _corePitch.Angle = -2.2 + easeY * -1.5 + _parallaxVelocity.Y * -4;
        _coreYaw.Angle = easeX * 0.9 + _parallaxVelocity.X * 1.2 + Math.Sin(_phase * 0.45) * 0.35 + _audioLevel * 0.2;
        _coreRoll.Angle = easeX * -5.2 + easeY * -0.25 + _parallaxVelocity.X * -7.5 + Math.Sin(_phase * 2.2) * _audioLevel * 0.1;

        ParallaxText.Text = $"{_parallax.X:+0.00;-0.00;+0.00} / {_parallax.Y:+0.00;-0.00;+0.00}";
        AngleText.Text = $"{displayAngle:000} DEG";
    }

    private void Window_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Escape && CommandPanel.Visibility == Visibility.Visible)
        {
            CommandPanel.Visibility = Visibility.Collapsed;
            StatusText.Text = "MONO CORE // LIVE";
            e.Handled = true;
            return;
        }

        if (Keyboard.Modifiers == ModifierKeys.Control && e.Key == Key.Space)
        {
            ToggleCommandPanel();
            e.Handled = true;
        }
    }

    private void OnRendering(object? sender, EventArgs e)
    {
        _phase += 0.018;
        _renderFrame++;

        UpdateParallaxMotion();

        var albumPulse = 1 + _audioLevel * 0.035;
        _albumPulse.ScaleX = albumPulse;
        _albumPulse.ScaleY = albumPulse;

        if (_renderFrame % 2 == 0)
        {
            UpdateTerrain();
        }

        ApplyCoreDirection();
    }

    private void UpdateParallaxMotion()
    {
        if (!_motionEnabled)
        {
            _parallaxTarget = new Point();
            _parallax = new Point();
            _parallaxVelocity = new Vector();
            ApplyParallaxTransforms();
            return;
        }

        var delta = new Vector(_parallaxTarget.X - _parallax.X, _parallaxTarget.Y - _parallax.Y);
        _parallaxVelocity += delta * 0.045;
        _parallaxVelocity *= 0.84;

        _parallax = new Point(
            Math.Clamp(_parallax.X + _parallaxVelocity.X, -1.05, 1.05),
            Math.Clamp(_parallax.Y + _parallaxVelocity.Y, -1.05, 1.05));

        if (Math.Abs(delta.X) < 0.001 && Math.Abs(delta.Y) < 0.001 && _parallaxVelocity.Length < 0.001)
        {
            _parallax = _parallaxTarget;
            _parallaxVelocity = new Vector();
        }

        ApplyParallaxTransforms();
    }

    private void ApplyParallaxTransforms()
    {
        var easeX = SmoothStep(_parallax.X);
        var easeY = SmoothStep(_parallax.Y);
        var pushX = _parallaxVelocity.X;
        var pushY = _parallaxVelocity.Y;

        FineGridTransform.X = easeX * -15 + pushX * -18;
        FineGridTransform.Y = easeY * -9 + pushY * -12;

        TerrainTransform.X = easeX * 18 + pushX * 22;
        TerrainTransform.Y = easeY * 10 + pushY * 16;

        LeftRailTransform.X = easeX * -10 + pushX * -10;
        LeftRailTransform.Y = easeY * -4 + pushY * -6;

        RightRailTransform.X = easeX * 10 + pushX * 10;
        RightRailTransform.Y = easeY * -4 + pushY * -6;

        CoreHudTransform.X = easeX * 2.4 + pushX * 3;
        CoreHudTransform.Y = easeY * 4 + pushY * 7;
    }

    private static double SmoothStep(double value)
    {
        var sign = Math.Sign(value);
        var magnitude = Math.Min(Math.Abs(value), 1);
        return sign * (magnitude * magnitude * (3 - 2 * magnitude));
    }

    private void BuildCoreModel()
    {
        CoreViewport.Children.Clear();
        CoreViewport.Camera = new PerspectiveCamera
        {
            Position = new Point3D(0, -4.8, 8.2),
            LookDirection = new Vector3D(0, 4.8, -7.8),
            UpDirection = new Vector3D(0, 0, 1),
            FieldOfView = 37
        };

        var model = new Model3DGroup();
        model.Children.Add(new AmbientLight(Color.FromRgb(86, 86, 86)));
        model.Children.Add(new DirectionalLight(Color.FromRgb(245, 245, 245), new Vector3D(-0.4, 0.7, -0.55)));
        model.Children.Add(new DirectionalLight(Color.FromRgb(110, 110, 110), new Vector3D(0.8, -0.3, -0.2)));

        _coreTransform.Children.Clear();
        _coreTransform.Children.Add(new RotateTransform3D(_corePitch));
        _coreTransform.Children.Add(new RotateTransform3D(_coreRoll));
        _coreTransform.Children.Add(new RotateTransform3D(_coreYaw));
        model.Transform = _coreTransform;

        var black = CreateMaterial("#101010", "#000000", 18);
        var dark = CreateMaterial("#1C1C1C", "#050505", 24);
        var mid = CreateMaterial("#4A4A4A", "#111111", 36);
        var light = CreateMaterial("#D8D8D8", "#FFFFFF", 68);
        var white = CreateMaterial("#F3F3F3", "#FFFFFF", 96);

        model.Children.Add(CreateExtrudedRing(2.38, 2.55, 0.14, 96, light, mid, 0.02));
        model.Children.Add(CreateExtrudedRing(1.78, 2.16, 0.34, 96, dark, mid, -0.12));
        model.Children.Add(CreateExtrudedRing(1.18, 1.42, 0.24, 96, light, mid, 0.14));
        model.Children.Add(CreateExtrudedRing(0.72, 0.96, 0.36, 96, black, mid, 0.04));
        model.Children.Add(CreateExtrudedDisk(0.58, 0.46, 96, dark, mid, 0.22));

        _audioBarScales.Clear();
        for (var i = 0; i < 16; i++)
        {
            var bar = CreateBox(0.072, 0.78, 0.15, 0, 1.82, 0.23, i % 4 == 0 ? white : mid);
            var audioScale = new ScaleTransform3D(1, 0.48, 1, 0, 1.47, 0.23);
            var barTransform = new Transform3DGroup();
            barTransform.Children.Add(audioScale);
            barTransform.Children.Add(new RotateTransform3D(new AxisAngleRotation3D(new Vector3D(0, 0, 1), i * 22.5)));
            bar.Transform = barTransform;
            _audioBarScales.Add(audioScale);
            model.Children.Add(bar);
        }

        for (var i = 0; i < 12; i++)
        {
            var tick = CreateBox(0.055, 0.32, 0.11, 0, 2.34, 0.28, i % 3 == 0 ? white : mid);
            tick.Transform = new RotateTransform3D(new AxisAngleRotation3D(new Vector3D(0, 0, 1), i * 30));
            model.Children.Add(tick);
        }

        var visual = new ModelVisual3D { Content = model };
        CoreViewport.Children.Add(visual);
        CoreViewport.Children.Add(CreateAttachedPanel(-0.72, -0.10, 0.52, 1.22, 0.64, BuildAlbumPanel()));
        CoreViewport.Children.Add(CreateAttachedPanel(0.72, -0.08, 0.525, 1.30, 0.64, BuildTelemetryPanel()));
        CoreViewport.Children.Add(CreateAttachedPanel(0.00, -0.88, 0.53, 1.18, 0.34, BuildTimePanel()));
    }

    private Viewport2DVisual3D CreateAttachedPanel(double centerX, double centerY, double z, double width, double height, Visual visual)
    {
        var material = new DiffuseMaterial(Brushes.White);
        Viewport2DVisual3D.SetIsVisualHostMaterial(material, true);

        return new Viewport2DVisual3D
        {
            Geometry = CreatePanelMesh(centerX, centerY, z, width, height),
            Material = material,
            Visual = visual,
            Transform = _coreTransform
        };
    }

    private static MeshGeometry3D CreatePanelMesh(double centerX, double centerY, double z, double width, double height)
    {
        var x0 = centerX - width / 2;
        var x1 = centerX + width / 2;
        var y0 = centerY - height / 2;
        var y1 = centerY + height / 2;
        var mesh = new MeshGeometry3D();

        mesh.Positions.Add(new Point3D(x0, y0, z));
        mesh.Positions.Add(new Point3D(x1, y0, z));
        mesh.Positions.Add(new Point3D(x1, y1, z));
        mesh.Positions.Add(new Point3D(x0, y1, z));

        mesh.TextureCoordinates.Add(new Point(0, 1));
        mesh.TextureCoordinates.Add(new Point(1, 1));
        mesh.TextureCoordinates.Add(new Point(1, 0));
        mesh.TextureCoordinates.Add(new Point(0, 0));

        mesh.TriangleIndices.Add(0);
        mesh.TriangleIndices.Add(1);
        mesh.TriangleIndices.Add(2);
        mesh.TriangleIndices.Add(0);
        mesh.TriangleIndices.Add(2);
        mesh.TriangleIndices.Add(3);

        return mesh;
    }

    private Visual BuildTimePanel()
    {
        var shell = CreatePanelShell(240, 60, new Thickness(12, 6, 12, 6));
        var stack = new StackPanel { Orientation = Orientation.Vertical };

        stack.Children.Add(CreateMonoText("TIME DECAL // SURFACE", 8, Rgba(160, 245, 245, 245), FontWeights.SemiBold));

        _clock3dText = CreateMonoText("00:00:00", 22, Rgba(255, 250, 250, 250), FontWeights.SemiBold);
        _clock3dText.Margin = new Thickness(0, 1, 0, 0);
        _clock3dText.HorizontalAlignment = HorizontalAlignment.Center;
        stack.Children.Add(_clock3dText);

        _date3dText = CreateMonoText("1970-01-01", 8, Rgba(135, 210, 210, 210), FontWeights.Normal);
        _date3dText.HorizontalAlignment = HorizontalAlignment.Center;
        stack.Children.Add(_date3dText);

        shell.Child = stack;
        return shell;
    }

    private Visual BuildTelemetryPanel()
    {
        var shell = CreatePanelShell(316, 126, new Thickness(12, 8, 12, 8));
        var grid = new Grid();

        grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(20) });
        grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(26) });
        grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(26) });
        grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(26) });
        grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(45) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(48) });

        var header = CreateMonoText("SURFACE TELEMETRY", 8, Rgba(170, 245, 245, 245), FontWeights.SemiBold);
        Grid.SetColumnSpan(header, 3);
        grid.Children.Add(header);

        AddMetricRow(grid, 1, "CPU", Rgba(235, 255, 255, 255), out _cpu3dValueText, out _cpu3dScale);
        AddMetricRow(grid, 2, "MEM", Rgba(205, 205, 205, 205), out _memory3dValueText, out _memory3dScale);
        AddMetricRow(grid, 3, "GPU", Rgba(150, 145, 145, 145), out _gpu3dValueText, out _gpu3dScale);

        _memory3dDetailText = CreateMonoText("MEM // -- GB / -- GB", 8, Rgba(125, 190, 190, 190), FontWeights.Normal);
        _memory3dDetailText.VerticalAlignment = VerticalAlignment.Bottom;
        Grid.SetRow(_memory3dDetailText, 4);
        Grid.SetColumnSpan(_memory3dDetailText, 3);
        grid.Children.Add(_memory3dDetailText);

        shell.Child = grid;
        return shell;
    }

    private Visual BuildAlbumPanel()
    {
        var shell = CreatePanelShell(292, 132, new Thickness(12, 8, 12, 8));
        var root = new Grid();
        root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(18) });
        root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

        var header = CreateMonoText("MEDIA BAY // STEADY", 8, Rgba(175, 245, 245, 245), FontWeights.SemiBold);
        root.Children.Add(header);

        var body = new Grid { Margin = new Thickness(0, 5, 0, 0) };
        body.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(80) });
        body.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        Grid.SetRow(body, 1);
        root.Children.Add(body);

        var coverBay = new Grid
        {
            Width = 72,
            Height = 72,
            Margin = new Thickness(0, 4, 0, 0),
            HorizontalAlignment = HorizontalAlignment.Left,
            VerticalAlignment = VerticalAlignment.Top,
            RenderTransformOrigin = new Point(0.5, 0.5)
        };
        coverBay.RenderTransform = _albumPulse;

        coverBay.Children.Add(new Rectangle
        {
            Stroke = Rgba(130, 245, 245, 245),
            StrokeThickness = 1,
            Fill = Rgba(60, 12, 12, 12)
        });
        coverBay.Children.Add(new Rectangle
        {
            Margin = new Thickness(7),
            Stroke = Rgba(90, 245, 245, 245),
            StrokeThickness = 1,
            StrokeDashArray = new DoubleCollection { 5, 4 }
        });

        _albumFrameBorder = new Border
        {
            Width = 50,
            Height = 50,
            Background = Rgba(230, 8, 8, 8),
            BorderBrush = _albumFrameBrush,
            BorderThickness = new Thickness(1),
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            ClipToBounds = true
        };

        var coverStack = new Grid();
        _albumArtImage = new Image
        {
            Stretch = Stretch.UniformToFill,
            Visibility = Visibility.Collapsed,
            Opacity = 0.96
        };
        _albumPlaceholderText = CreateMonoText("MEDIA\nBAY", 9, Rgba(185, 235, 235, 235), FontWeights.SemiBold);
        _albumPlaceholderText.TextAlignment = TextAlignment.Center;
        _albumPlaceholderText.HorizontalAlignment = HorizontalAlignment.Center;
        _albumPlaceholderText.VerticalAlignment = VerticalAlignment.Center;

        coverStack.Children.Add(_albumArtImage);
        coverStack.Children.Add(_albumPlaceholderText);
        _albumFrameBorder.Child = coverStack;
        coverBay.Children.Add(_albumFrameBorder);
        body.Children.Add(coverBay);

        var info = new StackPanel
        {
            Margin = new Thickness(4, 4, 0, 0),
            VerticalAlignment = VerticalAlignment.Top
        };

        _mediaTitleText = CreateMonoText("NO MEDIA SESSION", 12, Rgba(255, 250, 250, 250), FontWeights.SemiBold);
        _mediaTitleText.TextWrapping = TextWrapping.Wrap;
        _mediaTitleText.MaxHeight = 32;
        info.Children.Add(_mediaTitleText);

        _mediaArtistText = CreateMonoText("AUDIO BUS LISTENING", 8, Rgba(155, 210, 210, 210), FontWeights.Normal);
        _mediaArtistText.Margin = new Thickness(0, 6, 0, 0);
        info.Children.Add(_mediaArtistText);

        _mediaAlbumText = CreateMonoText("WAITING FOR SESSION", 8, Rgba(120, 185, 185, 185), FontWeights.Normal);
        _mediaAlbumText.Margin = new Thickness(0, 5, 0, 0);
        _mediaAlbumText.TextWrapping = TextWrapping.Wrap;
        info.Children.Add(_mediaAlbumText);

        Grid.SetColumn(info, 1);
        body.Children.Add(info);

        shell.Child = root;
        return shell;
    }

    private static Border CreatePanelShell(double width, double height, Thickness padding)
    {
        return new Border
        {
            Width = width,
            Height = height,
            Padding = padding,
            Background = Rgba(196, 4, 4, 4),
            BorderBrush = Rgba(105, 245, 245, 245),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(2),
            SnapsToDevicePixels = true
        };
    }

    private static TextBlock CreateMonoText(string text, double fontSize, Brush foreground, FontWeight weight)
    {
        return new TextBlock
        {
            Text = text,
            FontFamily = new FontFamily("Consolas"),
            FontSize = fontSize,
            FontWeight = weight,
            Foreground = foreground,
            TextTrimming = TextTrimming.CharacterEllipsis
        };
    }

    private static void AddMetricRow(Grid grid, int row, string label, Brush fillBrush, out TextBlock valueText, out ScaleTransform scale)
    {
        var labelText = CreateMonoText(label, 11, Rgba(230, 245, 245, 245), FontWeights.SemiBold);
        labelText.VerticalAlignment = VerticalAlignment.Center;
        Grid.SetRow(labelText, row);
        grid.Children.Add(labelText);

        var track = new Border
        {
            Height = 6,
            Background = Rgba(150, 34, 34, 34),
            BorderBrush = Rgba(70, 255, 255, 255),
            BorderThickness = new Thickness(1),
            ClipToBounds = true,
            VerticalAlignment = VerticalAlignment.Center
        };

        scale = new ScaleTransform(0.01, 1);
        track.Child = new Border
        {
            Background = fillBrush,
            RenderTransform = scale,
            RenderTransformOrigin = new Point(0, 0.5)
        };

        Grid.SetRow(track, row);
        Grid.SetColumn(track, 1);
        grid.Children.Add(track);

        valueText = CreateMonoText("--%", 12, Rgba(240, 245, 245, 245), FontWeights.SemiBold);
        valueText.HorizontalAlignment = HorizontalAlignment.Right;
        valueText.VerticalAlignment = VerticalAlignment.Center;
        Grid.SetRow(valueText, row);
        Grid.SetColumn(valueText, 2);
        grid.Children.Add(valueText);
    }

    private static SolidColorBrush Rgba(byte alpha, byte red, byte green, byte blue)
    {
        return new SolidColorBrush(Color.FromArgb(alpha, red, green, blue));
    }

    private static MaterialGroup CreateMaterial(string diffuseHex, string specularHex, double specularPower)
    {
        var material = new MaterialGroup();
        material.Children.Add(new DiffuseMaterial(new SolidColorBrush((Color)ColorConverter.ConvertFromString(diffuseHex))));
        material.Children.Add(new SpecularMaterial(new SolidColorBrush((Color)ColorConverter.ConvertFromString(specularHex)), specularPower));
        return material;
    }

    private static GeometryModel3D CreateExtrudedRing(double innerRadius, double outerRadius, double depth, int segments, Material frontMaterial, Material sideMaterial, double zOffset)
    {
        var mesh = new MeshGeometry3D();
        var frontZ = zOffset + depth / 2;
        var backZ = zOffset - depth / 2;

        for (var i = 0; i < segments; i++)
        {
            var a0 = i * Math.Tau / segments;
            var a1 = (i + 1) * Math.Tau / segments;

            var of0 = PointOnCircle(outerRadius, a0, frontZ);
            var of1 = PointOnCircle(outerRadius, a1, frontZ);
            var if0 = PointOnCircle(innerRadius, a0, frontZ);
            var if1 = PointOnCircle(innerRadius, a1, frontZ);
            var ob0 = PointOnCircle(outerRadius, a0, backZ);
            var ob1 = PointOnCircle(outerRadius, a1, backZ);
            var ib0 = PointOnCircle(innerRadius, a0, backZ);
            var ib1 = PointOnCircle(innerRadius, a1, backZ);

            AddQuad(mesh, of0, of1, if1, if0);
            AddQuad(mesh, ob1, ob0, ib0, ib1);
            AddQuad(mesh, of1, of0, ob0, ob1);
            AddQuad(mesh, if0, if1, ib1, ib0);
        }

        return new GeometryModel3D
        {
            Geometry = mesh,
            Material = frontMaterial,
            BackMaterial = sideMaterial
        };
    }

    private static GeometryModel3D CreateExtrudedDisk(double radius, double depth, int segments, Material frontMaterial, Material sideMaterial, double zOffset)
    {
        var mesh = new MeshGeometry3D();
        var frontZ = zOffset + depth / 2;
        var backZ = zOffset - depth / 2;
        var frontCenter = new Point3D(0, 0, frontZ);
        var backCenter = new Point3D(0, 0, backZ);

        for (var i = 0; i < segments; i++)
        {
            var a0 = i * Math.Tau / segments;
            var a1 = (i + 1) * Math.Tau / segments;
            var f0 = PointOnCircle(radius, a0, frontZ);
            var f1 = PointOnCircle(radius, a1, frontZ);
            var b0 = PointOnCircle(radius, a0, backZ);
            var b1 = PointOnCircle(radius, a1, backZ);

            AddTriangle(mesh, frontCenter, f0, f1);
            AddTriangle(mesh, backCenter, b1, b0);
            AddQuad(mesh, f1, f0, b0, b1);
        }

        return new GeometryModel3D
        {
            Geometry = mesh,
            Material = frontMaterial,
            BackMaterial = sideMaterial
        };
    }

    private static GeometryModel3D CreateBox(double width, double height, double depth, double centerX, double centerY, double centerZ, Material material)
    {
        var x0 = centerX - width / 2;
        var x1 = centerX + width / 2;
        var y0 = centerY - height / 2;
        var y1 = centerY + height / 2;
        var z0 = centerZ - depth / 2;
        var z1 = centerZ + depth / 2;
        var mesh = new MeshGeometry3D();

        var p000 = new Point3D(x0, y0, z0);
        var p001 = new Point3D(x0, y0, z1);
        var p010 = new Point3D(x0, y1, z0);
        var p011 = new Point3D(x0, y1, z1);
        var p100 = new Point3D(x1, y0, z0);
        var p101 = new Point3D(x1, y0, z1);
        var p110 = new Point3D(x1, y1, z0);
        var p111 = new Point3D(x1, y1, z1);

        AddQuad(mesh, p001, p101, p111, p011);
        AddQuad(mesh, p100, p000, p010, p110);
        AddQuad(mesh, p000, p001, p011, p010);
        AddQuad(mesh, p101, p100, p110, p111);
        AddQuad(mesh, p010, p011, p111, p110);
        AddQuad(mesh, p000, p100, p101, p001);

        return new GeometryModel3D
        {
            Geometry = mesh,
            Material = material,
            BackMaterial = material
        };
    }

    private static Point3D PointOnCircle(double radius, double angle, double z)
    {
        return new Point3D(Math.Cos(angle) * radius, Math.Sin(angle) * radius, z);
    }

    private static void AddQuad(MeshGeometry3D mesh, Point3D p0, Point3D p1, Point3D p2, Point3D p3)
    {
        AddTriangle(mesh, p0, p1, p2);
        AddTriangle(mesh, p0, p2, p3);
    }

    private static void AddTriangle(MeshGeometry3D mesh, Point3D p0, Point3D p1, Point3D p2)
    {
        var index = mesh.Positions.Count;
        mesh.Positions.Add(p0);
        mesh.Positions.Add(p1);
        mesh.Positions.Add(p2);
        mesh.TriangleIndices.Add(index);
        mesh.TriangleIndices.Add(index + 1);
        mesh.TriangleIndices.Add(index + 2);
    }

    private void BuildGrid()
    {
        GridCanvas.Children.Clear();
        _gridLines.Clear();

        var width = Math.Max(ActualWidth + 80, 1);
        var height = Math.Max(ActualHeight + 80, 1);
        const double spacing = 48;
        var lineBrush = new SolidColorBrush(Color.FromArgb(34, 255, 255, 255));
        var majorLineBrush = new SolidColorBrush(Color.FromArgb(58, 255, 255, 255));

        for (var x = -80.0; x < width; x += spacing)
        {
            var index = (int)Math.Round((x + 80) / spacing);
            AddGridLine(x, -80, x, height, index % 4 == 0 ? majorLineBrush : lineBrush, index % 4 == 0 ? 0.9 : 0.45);
        }

        for (var y = -80.0; y < height; y += spacing)
        {
            var index = (int)Math.Round((y + 80) / spacing);
            AddGridLine(-80, y, width, y, index % 4 == 0 ? majorLineBrush : lineBrush, index % 4 == 0 ? 0.9 : 0.45);
        }
    }

    private void AddGridLine(double x1, double y1, double x2, double y2, Brush brush, double thickness)
    {
        var line = new Line
        {
            X1 = x1,
            Y1 = y1,
            X2 = x2,
            Y2 = y2,
            Stroke = brush,
            StrokeThickness = thickness,
            SnapsToDevicePixels = true
        };

        GridCanvas.Children.Add(line);
        _gridLines.Add(line);
    }

    private void BuildTerrain()
    {
        TerrainCanvas.Children.Clear();
        _terrainLines.Clear();

        for (var row = 0; row < 9; row++)
        {
            var accent = row is 3 or 6;
            var line = new Polyline
            {
                Stroke = new SolidColorBrush(accent
                    ? Color.FromArgb(98, 255, 255, 255)
                    : Color.FromArgb((byte)(28 + row * 7), 170, 170, 170)),
                StrokeThickness = accent ? 1.1 : 0.7,
                Opacity = 0.22 + row * 0.028,
                SnapsToDevicePixels = false
            };

            TerrainCanvas.Children.Add(line);
            _terrainLines.Add(line);
        }

        UpdateTerrain();
    }

    private void UpdateTerrain()
    {
        if (_terrainLines.Count == 0)
        {
            return;
        }

        var width = Math.Max(ActualWidth, 1);
        var height = Math.Max(ActualHeight, 1);
        var baseY = height * 0.64;
        const double step = 42;

        for (var row = 0; row < _terrainLines.Count; row++)
        {
            var points = _terrainLines[row].Points;
            points.Clear();

            var depth = row / (double)(_terrainLines.Count - 1);
            var amplitude = 10 + depth * 34;
            var yOffset = row * 18 + depth * depth * 60;

            for (var x = -80.0; x <= width + 80; x += step)
            {
                var wave =
                    Math.Sin((x * 0.012) + _phase + row * 0.55) +
                    Math.Sin((x * 0.026) - _phase * 0.65 + row * 0.38) * 0.42;
                var parallaxLift = _parallax.Y * (12 + row * 1.6);
                var y = baseY + yOffset + wave * amplitude * (0.35 + depth * 0.65) + parallaxLift;
                points.Add(new Point(x, y));
            }
        }
    }

    private void UpdateTelemetry()
    {
        var snapshot = _telemetry.Read();
        var now = DateTime.Now;

        if (_clock3dText is not null)
        {
            _clock3dText.Text = now.ToString("HH:mm:ss");
        }

        if (_date3dText is not null)
        {
            _date3dText.Text = now.ToString("yyyy-MM-dd  ddd").ToUpperInvariant();
        }

        if (snapshot.CpuUsagePercent is { } cpu)
        {
            if (_cpu3dValueText is not null)
            {
                _cpu3dValueText.Text = $"{cpu:0}%";
            }

            if (_cpu3dScale is not null)
            {
                _cpu3dScale.ScaleX = Math.Clamp(cpu / 100, 0.01, 1);
            }
        }
        else
        {
            if (_cpu3dValueText is not null)
            {
                _cpu3dValueText.Text = "--%";
            }

            if (_cpu3dScale is not null)
            {
                _cpu3dScale.ScaleX = 0.01;
            }
        }

        if (_memory3dValueText is not null)
        {
            _memory3dValueText.Text = $"{snapshot.MemoryUsagePercent:0}%";
        }

        if (_memory3dScale is not null)
        {
            _memory3dScale.ScaleX = Math.Clamp(snapshot.MemoryUsagePercent / 100, 0.01, 1);
        }

        if (_memory3dDetailText is not null)
        {
            _memory3dDetailText.Text = $"MEMORY BUS // {snapshot.UsedMemoryGb:0.0} GB / {snapshot.TotalMemoryGb:0.0} GB";
        }

        if (snapshot.GpuUsagePercent is { } gpu)
        {
            if (_gpu3dValueText is not null)
            {
                _gpu3dValueText.Text = $"{gpu:0}%";
            }

            if (_gpu3dScale is not null)
            {
                _gpu3dScale.ScaleX = Math.Clamp(gpu / 100, 0.01, 1);
            }
        }
        else
        {
            if (_gpu3dValueText is not null)
            {
                _gpu3dValueText.Text = "RES";
            }

            if (_gpu3dScale is not null)
            {
                _gpu3dScale.ScaleX = 0.12;
            }
        }

        if (CommandPanel.Visibility != Visibility.Visible)
        {
            RefreshRuntimeState();
        }
    }

    private void UpdateAudioLevel()
    {
        var peak = _audioEnabled ? _audioMonitor.ReadPeak() : 0;
        _audioLevel = _audioLevel * 0.68 + peak * 0.32;

        _albumFrameBrush.Color = Color.FromArgb(
            (byte)Math.Clamp(120 + _audioLevel * 135, 120, 255),
            255,
            255,
            255);

        if (_albumFrameBorder is not null)
        {
            _albumFrameBorder.BorderThickness = new Thickness(1 + _audioLevel * 0.9);
        }

        for (var i = 0; i < _audioBarScales.Count; i++)
        {
            var wave = (Math.Sin(_phase * 4.4 + i * 0.78) + 1) * 0.5;
            var scale = 0.36 + _audioLevel * (0.42 + wave * 0.76);
            _audioBarScales[i].ScaleY = Math.Clamp(scale, 0.34, 1.42);
            _audioBarScales[i].ScaleZ = Math.Clamp(0.82 + _audioLevel * 0.45, 0.82, 1.25);
        }
    }

    private async Task UpdateMediaAsync()
    {
        if (_mediaUpdatePending)
        {
            return;
        }

        _mediaUpdatePending = true;
        try
        {
            ApplyMediaSnapshot(await _mediaMonitor.ReadAsync());
        }
        finally
        {
            _mediaUpdatePending = false;
        }
    }

    private void ApplyMediaSnapshot(MediaSnapshot snapshot)
    {
        if (!snapshot.HasSession)
        {
            if (_mediaTitleText is not null)
            {
                _mediaTitleText.Text = "NO MEDIA SESSION";
            }

            if (_mediaArtistText is not null)
            {
                _mediaArtistText.Text = "AUDIO BUS LISTENING";
            }

            if (_mediaAlbumText is not null)
            {
                _mediaAlbumText.Text = "WAITING FOR WINDOWS SESSION";
            }

            if (_albumArtImage is not null)
            {
                _albumArtImage.Source = null;
                _albumArtImage.Visibility = Visibility.Collapsed;
            }

            if (_albumPlaceholderText is not null)
            {
                _albumPlaceholderText.Text = "AUDIO\nSTANDBY";
                _albumPlaceholderText.Visibility = Visibility.Visible;
            }

            return;
        }

        if (_mediaTitleText is not null)
        {
            _mediaTitleText.Text = string.IsNullOrWhiteSpace(snapshot.Title) ? "UNTITLED TRACK" : snapshot.Title;
        }

        if (_mediaArtistText is not null)
        {
            var artist = string.IsNullOrWhiteSpace(snapshot.Artist) ? "UNKNOWN ARTIST" : snapshot.Artist;
            _mediaArtistText.Text = snapshot.IsPlaying ? $"PLAYING // {artist}" : $"PAUSED // {artist}";
        }

        if (_mediaAlbumText is not null)
        {
            _mediaAlbumText.Text = string.IsNullOrWhiteSpace(snapshot.Album)
                ? "ALBUM FIELD NOT EXPOSED"
                : snapshot.Album;
        }

        if (snapshot.Artwork is not null && _albumArtImage is not null)
        {
            _albumArtImage.Source = snapshot.Artwork;
            _albumArtImage.Visibility = Visibility.Visible;
            if (_albumPlaceholderText is not null)
            {
                _albumPlaceholderText.Visibility = Visibility.Collapsed;
            }
        }
        else
        {
            if (_albumArtImage is not null)
            {
                _albumArtImage.Source = null;
                _albumArtImage.Visibility = Visibility.Collapsed;
            }

            if (_albumPlaceholderText is not null)
            {
                _albumPlaceholderText.Text = snapshot.IsPlaying ? "LIVE\nAUDIO" : "MEDIA\nPAUSED";
                _albumPlaceholderText.Visibility = Visibility.Visible;
            }
        }
    }

    private void ToggleCommandPanel()
    {
        var show = CommandPanel.Visibility != Visibility.Visible;
        CommandPanel.Visibility = show ? Visibility.Visible : Visibility.Collapsed;
        StatusText.Text = show ? "COMMAND SLOT // ACTIVE" : "MONO CORE // LIVE";
        RefreshRuntimeState();

        if (show)
        {
            SearchBox.SelectAll();
            SearchBox.Focus();
        }
        else
        {
            Focus();
        }
    }

    private void FooterCommandSlot_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        ToggleCommandPanel();
    }

    private void CreateTrayIcon()
    {
        _trayVisibilityItem = new Forms.ToolStripMenuItem("Hide TopoShell", null, (_, _) => ToggleTrayVisibility());
        _trayMotionItem = new Forms.ToolStripMenuItem("Motion", null, (_, _) => ToggleMotion()) { Checked = true };
        _trayAudioItem = new Forms.ToolStripMenuItem("Audio Response", null, (_, _) => ToggleAudio()) { Checked = true };
        _trayTopmostItem = new Forms.ToolStripMenuItem("Always On Top", null, (_, _) => ToggleTopmost());

        var menu = new Forms.ContextMenuStrip();
        menu.Items.Add(_trayVisibilityItem);
        menu.Items.Add(new Forms.ToolStripSeparator());
        menu.Items.Add(_trayMotionItem);
        menu.Items.Add(_trayAudioItem);
        menu.Items.Add(_trayTopmostItem);
        menu.Items.Add(new Forms.ToolStripMenuItem("Reset View", null, (_, _) => ResetParallax()));
        menu.Items.Add(new Forms.ToolStripSeparator());
        menu.Items.Add(new Forms.ToolStripMenuItem("Exit Shell", null, (_, _) => ExitShell()));

        _trayIcon = new Forms.NotifyIcon
        {
            Text = "TopoShell",
            Icon = Drawing.SystemIcons.Application,
            ContextMenuStrip = menu,
            Visible = true
        };
        _trayIcon.DoubleClick += (_, _) => ShowShell();
    }

    private void RefreshRuntimeState()
    {
        MotionToggleButton.Content = _motionEnabled ? "MOTION ON" : "MOTION OFF";
        AudioToggleButton.Content = _audioEnabled ? "AUDIO ON" : "AUDIO OFF";
        TopmostToggleButton.Content = Topmost ? "TOPMOST ON" : "TOPMOST OFF";
        CommandStateText.Text = $"MOTION {Flag(_motionEnabled)} // AUDIO {Flag(_audioEnabled)} // TOP {Flag(Topmost)}";

        if (CommandPanel.Visibility != Visibility.Visible)
        {
            StatusText.Text = $"MONO CORE // {Flag(_motionEnabled)} MOT // {Flag(_audioEnabled)} AUD";
        }

        if (_trayVisibilityItem is not null)
        {
            _trayVisibilityItem.Text = IsVisible ? "Hide TopoShell" : "Show TopoShell";
        }

        if (_trayMotionItem is not null)
        {
            _trayMotionItem.Checked = _motionEnabled;
        }

        if (_trayAudioItem is not null)
        {
            _trayAudioItem.Checked = _audioEnabled;
        }

        if (_trayTopmostItem is not null)
        {
            _trayTopmostItem.Checked = Topmost;
        }
    }

    private static string Flag(bool value)
    {
        return value ? "ON" : "OFF";
    }

    private void ToggleTrayVisibility()
    {
        if (IsVisible)
        {
            HideToTray();
            return;
        }

        ShowShell();
    }

    private void ShowShell()
    {
        Show();
        WindowState = WindowState.Normal;
        Activate();
        Focus();
        RefreshRuntimeState();
    }

    private void HideToTray()
    {
        CommandPanel.Visibility = Visibility.Collapsed;
        Hide();
        RefreshRuntimeState();
    }

    private void ToggleMotion()
    {
        _motionEnabled = !_motionEnabled;
        if (!_motionEnabled)
        {
            ResetParallax();
        }

        RefreshRuntimeState();
    }

    private void ToggleAudio()
    {
        _audioEnabled = !_audioEnabled;
        if (!_audioEnabled)
        {
            _audioLevel = 0;
            UpdateAudioLevel();
        }

        RefreshRuntimeState();
    }

    private void ToggleTopmost()
    {
        Topmost = !Topmost;
        RefreshRuntimeState();
    }

    private void ResetParallax()
    {
        _parallaxTarget = new Point();
        _parallax = new Point();
        _parallaxVelocity = new Vector();
        ApplyParallaxTransforms();
        ApplyCoreDirection();
    }

    private void ExitShell()
    {
        Close();
    }

    private void ToggleMotion_Click(object sender, RoutedEventArgs e)
    {
        ToggleMotion();
    }

    private void ToggleAudio_Click(object sender, RoutedEventArgs e)
    {
        ToggleAudio();
    }

    private void ToggleTopmost_Click(object sender, RoutedEventArgs e)
    {
        ToggleTopmost();
    }

    private void ResetParallax_Click(object sender, RoutedEventArgs e)
    {
        ResetParallax();
    }

    private void HideToTray_Click(object sender, RoutedEventArgs e)
    {
        HideToTray();
    }

    private void ExitShell_Click(object sender, RoutedEventArgs e)
    {
        ExitShell();
    }

    private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ClickCount == 2)
        {
            WindowState = WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;
            return;
        }

        DragMove();
    }

    private void Minimize_Click(object sender, RoutedEventArgs e)
    {
        HideToTray();
    }

    private void Close_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }
}
