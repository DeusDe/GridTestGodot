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

	// -------------------------------------------------------
	// UI: Labels
	// -------------------------------------------------------

	[Export] private Label _generationLabel;
	[Export] private Label _diedLabel;
	[Export] private Label _revivedLabel;
	[Export] private Label _populationLabel;
	[Export] private Label _tpsLabel;

	// -------------------------------------------------------
	// UI: Color Picker
	// -------------------------------------------------------

	[Export] private ColorPickerButton _gridColor;
	[Export] private ColorPickerButton _aliveColor;

	// -------------------------------------------------------
	// UI: Buttons & Controls
	// -------------------------------------------------------

	[Export] private Button _nextButton;
	[Export] private Button _startButton;
	[Export] private Button _centerCameraButton;
	[Export] private Button _rngButton;
	[Export] private Button _toolButton;
	[Export] private SpinBox _tickIntervalBox;
	[Export] private OptionButton _patternSelect;

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

		//Pattern
		_patternSelect.AddItem("Glider", (int)GameOfLife.PatternType.Glider);
		_patternSelect.AddItem("Block", (int)GameOfLife.PatternType.Block);
		_patternSelect.AddItem("Beehive", (int)GameOfLife.PatternType.Beehive);
		_patternSelect.AddItem("Blinker", (int)GameOfLife.PatternType.Blinker);
		_patternSelect.AddItem("Toad", (int)GameOfLife.PatternType.Toad);
		_patternSelect.AddItem("Beacon", (int)GameOfLife.PatternType.Beacon);
		_patternSelect.AddItem("LWSS", (int)GameOfLife.PatternType.LightweightSpaceship);
		_patternSelect.AddItem("Loaf", (int)GameOfLife.PatternType.Loaf);
		_patternSelect.AddItem("Boat", (int)GameOfLife.PatternType.Boat);
		_patternSelect.AddItem("Pulsar", (int)GameOfLife.PatternType.Pulsar);
		_patternSelect.AddItem("Pentadecathlon", (int)GameOfLife.PatternType.Pentadecathlon);
		_patternSelect.AddItem("R-Pentomino", (int)GameOfLife.PatternType.RPentomino);
		_patternSelect.AddItem("Small Gun", (int)GameOfLife.PatternType.SmallGunSeed);

		_patternSelect.Selected = 0;

		// Events
		_nextButton.Pressed += OnNextButtonPressed;
		_startButton.Pressed += OnStartButtonPressed;
		_centerCameraButton.Pressed += CenterCameraPosition;
		_rngButton.Pressed += Gol.Randomize;

		_toolButton.Pressed += OnToolButtonPressed;
		_patternSelect.ItemSelected += OnPatternSelected;

		_aliveColor.Color = Gol.ActiveColor;
		_gridColor.Color = Gol.GridColor;

		_aliveColor.ColorChanged += AliveColorPressed;
		_gridColor.ColorChanged += GridColorPressed;

		Gol.StatsChanged += OnStatsChanged;

		// Tool
		_toolButton.Text = "Pattern";
		Gol.SetToolDraw();

		OnStatsChanged();
	}

	public override void _ExitTree()
	{
		_nextButton.Pressed -= OnNextButtonPressed;
		_startButton.Pressed -= OnStartButtonPressed;
		_centerCameraButton.Pressed -= CenterCameraPosition;
		_rngButton.Pressed -= Gol.Randomize;

		_toolButton.Pressed -= OnToolButtonPressed;
		_patternSelect.ItemSelected -= OnPatternSelected;

		_aliveColor.ColorChanged -= AliveColorPressed;
		_gridColor.ColorChanged -= GridColorPressed;

		Gol.StatsChanged -= OnStatsChanged;
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
		zoomLevel = Mathf.Clamp(zoomLevel, MinZoom, MaxZoom);

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

	private void OnPatternSelected(long index)
	{
		// ID ist als PatternType gecastet gespeichert
		int id = _patternSelect.GetItemId((int)index);
		var type = (GameOfLife.PatternType)id;

		Gol.SetToolPattern(type);
	}

	private void OnToolButtonPressed()
	{
		switch (Gol.CurrentTool)
		{
			case GameOfLife.Tool.Draw:
				Gol.SetToolPattern();
				_toolButton.Text = "Draw";
				break;
			case GameOfLife.Tool.Pattern:
				Gol.SetToolDraw();
				_toolButton.Text = "Pattern";
				break;
		}
	}


}
