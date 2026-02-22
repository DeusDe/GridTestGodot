using Godot;
using System;

public partial class Camera : Camera2D
{
    // -------------------------------------------------------
    // Exports / Settings
    // -------------------------------------------------------

    [Export] public GameOfLife Gol;

    [Export] public float ZoomStep = 0.1f;
    [Export] public float MoveSpeed = 0.4f;
    [Export] public float MinZoom = 0.3f;
    [Export] public float MaxZoom = 20.0f;
    [Export] public float CameraMargin = 20.0f;
    [Export] public string LabelBasePath = "UI/Vertical/";
    [Export] public string ControlBasePath = "UI/Vertical/Panel/Control/";

    // -------------------------------------------------------
    // UI: Labels
    // -------------------------------------------------------

    private Label _generationLabel;
    private Label _diedLabel;
    private Label _revivedLabel;
    private Label _populationLabel;
    private Label _tpsLabel;

    // -------------------------------------------------------
    // UI: Color Picker
    // -------------------------------------------------------

    private ColorPickerButton _gridColor;
    private ColorPickerButton _aliveColor;

    // -------------------------------------------------------
    // UI: Buttons & Controls
    // -------------------------------------------------------

    private Button _nextButton;
    private Button _startButton;
    private Button _centerCameraButton;
    private Button _rngButton;
    private Button _gliderButton;

    private SpinBox _tickIntervalBox;

    // -------------------------------------------------------
    // Camera Drag State
    // -------------------------------------------------------

    private bool _isDragging = false;
    private Vector2 _dragStartMousePos;
    private Vector2 _dragStartCamPos;

    // -------------------------------------------------------
    // Lifecycle
    // -------------------------------------------------------

    public override void _Ready()
    {
        if (Gol == null)
        {
            GD.PushError("Camera: Gol reference is not set in the Inspector.");
            return;
        }

        CenterCameraPosition();

        // Labels
        _generationLabel = GetNode<Label>(LabelBasePath + "GenerationLabel");
        _diedLabel = GetNode<Label>(LabelBasePath + "DiedLabel");
        _revivedLabel = GetNode<Label>(LabelBasePath + "RevivedLabel");
        _populationLabel = GetNode<Label>(LabelBasePath + "PopulationLabel");
        _tpsLabel = GetNode<Label>(LabelBasePath + "TPSLabel");

        // Buttons
        _nextButton = GetNode<Button>(ControlBasePath + "NextButton");
        _startButton = GetNode<Button>(ControlBasePath + "StartButton");
        _centerCameraButton = GetNode<Button>(ControlBasePath + "CenterCameraButton");
        _rngButton = GetNode<Button>(ControlBasePath + "RandomizeButton");
        _gliderButton = GetNode<Button>(ControlBasePath + "GliderButton");

        // SpinBox
        _tickIntervalBox = GetNode<SpinBox>(ControlBasePath + "TickIntervalBox");

        // ColorPicker
        _aliveColor = GetNode<ColorPickerButton>(ControlBasePath + "AliveColor");
        _gridColor = GetNode<ColorPickerButton>(ControlBasePath + "GridColor");

        // Events
        _nextButton.Pressed += OnNextButtonPressed;
        _startButton.Pressed += OnStartButtonPressed;
        _centerCameraButton.Pressed += CenterCameraPosition;
        _rngButton.Pressed += Gol.Randomize;
        _gliderButton.Pressed += setPattern;

        _aliveColor.Color = Gol.ActiveColor;
        _gridColor.Color = Gol.GridColor;

        _aliveColor.ColorChanged += AliveColorPressed;
        _gridColor.ColorChanged += GridColorPressed;

        Gol.StatsChanged += OnStatsChanged;

        OnStatsChanged();
    }

    // -------------------------------------------------------
    // Camera Position / Zoom
    // -------------------------------------------------------

    private void CenterCameraPosition()
    {
        float gridWidthPx = Gol.GridWidth * Gol.CellWidth;
        float gridHeightPx = Gol.GridHeight * Gol.CellHeight;

        Vector2 viewportSize = GetViewport().GetVisibleRect().Size;

        float usableWidth = viewportSize.X - 2.0f * CameraMargin;
        float usableHeight = viewportSize.Y - 2.0f * CameraMargin;

        if (usableWidth <= 0 || usableHeight <= 0)
            return;

        float zoomX = usableWidth / gridWidthPx;
        float zoomY = usableHeight / gridHeightPx;
        float zoomLevel = MathF.Min(zoomX, zoomY);

        Zoom = new Vector2(zoomLevel, zoomLevel);

        Position = new Vector2(
            gridWidthPx / 2.0f,
            gridHeightPx / 2.0f
        );
    }

    private void ChangeZoom(float direction)
    {
        var z = Zoom;
        z += new Vector2(ZoomStep, ZoomStep) * direction;
        z.X = Mathf.Clamp(z.X, MinZoom, MaxZoom);
        z.Y = Mathf.Clamp(z.Y, MinZoom, MaxZoom);
        Zoom = z;
    }

    // -------------------------------------------------------
    // UI Updates
    // -------------------------------------------------------

    private void OnStatsChanged()
    {
        _generationLabel.Text = $"Generation: {Gol.GenerationCount}";
        _diedLabel.Text = $"Died: {Gol.CellsDiedCount}";
        _revivedLabel.Text = $"Reborn: {Gol.CellsRevivedCount}";
        _populationLabel.Text = $"Population: {Gol.PopulationCount}";
        _tpsLabel.Text = $"TPS: {Gol.TicksPerSecond}";
    }

    private void AliveColorPressed(Color color)
    {
        Gol.ActiveColor = color;
        Gol.QueueRedraw();
    }

    private void GridColorPressed(Color color)
    {
        Gol.GridColor = color;
        Gol.QueueRedraw();
    }

    // -------------------------------------------------------
    // Input
    // -------------------------------------------------------

    public override void _Input(InputEvent @event)
    {
        if (@event is InputEventMouseButton mb)
        {
            if (mb.ButtonIndex == MouseButton.Middle)
            {
                if (mb.Pressed)
                {
                    _isDragging = true;
                    _dragStartMousePos = GetViewport().GetMousePosition();
                    _dragStartCamPos = Position;
                }
                else
                {
                    _isDragging = false;
                }
            }

            if (mb.ButtonIndex == MouseButton.WheelDown && mb.Pressed)
            {
                ChangeZoom(-1f);
            }
            else if (mb.ButtonIndex == MouseButton.WheelUp && mb.Pressed)
            {
                ChangeZoom(1f);
            }
        }

        if (@event is InputEventMouseMotion motion && _isDragging)
        {
            Vector2 currentMouse = GetViewport().GetMousePosition();
            Vector2 delta = currentMouse - _dragStartMousePos;

            Position = _dragStartCamPos - delta * Zoom * MoveSpeed;
        }
    }

    // -------------------------------------------------------
    // Button Handlers
    // -------------------------------------------------------

    private void OnNextButtonPressed()
    {
        Gol.NextTick();
    }

    private void OnStartButtonPressed()
    {
        if (Gol.TimerActive)
        {
            _startButton.Text = "Start";
            _tickIntervalBox.Editable = true;
            _nextButton.Disabled = false;
        }
        else
        {
            _startButton.Text = "Pause";
            _tickIntervalBox.Editable = false;
            Gol.SetTimerSeconds(1.0f / _tickIntervalBox.Value);
            _nextButton.Disabled = true;
        }

        Gol.SetAutoTick(!Gol.TimerActive);
    }

	    private void setPattern()
    {
        Gol.SetToolPattern(GameOfLife.GliderPattern);
    }

}
