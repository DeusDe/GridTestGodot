using Godot;
using System;

public partial class GameOfLife : Node2D
{
	// Exports
	[Export] public int GridWidth = 25;
	[Export] public int GridHeight = 25;
	[Export] public int CellHeight = 16;
	[Export] public int CellWidth = 16;
	[Export] public Color ActiveColor = new Color(0.1f, 0.8f, 0.2f, 1f);
	[Export] public Color GridColor = new Color(0.2f, 0.03f, 0.045f, 1f);
	[Export] public Color HoverColor = new Color(1f, 1f, 1f, 0.2f);

	[Signal] public delegate void StatsChangedEventHandler();

	// Random
	private readonly Random _rng = new Random();
	private float _rngDensity = 0.35f;

	// Grid / Cells
	private bool[,] _cells;
	private bool[,] _nextCells;

	// Hover & Drawing
	private int _hoverX = -1;
	private int _hoverY = -1;
	private bool _isDrawing = false;
	private bool _drawState = true;

	// Patterns
	private static readonly Vector2I[] GliderPattern =
	{
		new Vector2I(1, 0),
		new Vector2I(2, 1),
		new Vector2I(0, 2),
		new Vector2I(1, 2),
		new Vector2I(2, 2),
	};

	// Stats
	private long _generationCount = 0;
	private long _cellsDiedCount = 0;
	private long _cellsRevivedCount = 0;
	private long _populationCount = 0;

	// Tick / Timer
	private int _ticksThisSecond = 0;
	private int _ticksPerSecond = 0;
	private double _tpsTimer = 0.0;
	private double _timer = 0.0;
	private double _timerSeconds = 0.2f;
	private bool _timerActive = false;

	// -------------------------------------------------------
	// Lifecycle
	// -------------------------------------------------------

	public override void _Ready()
	{
		_cells = new bool[GridWidth, GridHeight];
		_nextCells = new bool[GridWidth, GridHeight];
	}

	public override void _Process(double delta)
	{
		if (_timerActive)
		{
			_timer += delta;
			if (_timer >= _timerSeconds)
			{
				_timer -= _timerSeconds;
				NextTick();
			}
		}

		_tpsTimer += delta;
		if (_tpsTimer >= 1.0)
		{
			_tpsTimer -= 1.0;
			_ticksPerSecond = _ticksThisSecond;
			_ticksThisSecond = 0;
			EmitStatsChanged();
		}
	}

	public override void _Draw()
	{
		DrawGrid();
		DrawCells();
	}

	// -------------------------------------------------------
	// Drawing
	// -------------------------------------------------------

	private void DrawGrid()
	{
		var totalWidth = GridWidth * CellWidth;
		var totalHeight = GridHeight * CellHeight;

		// Vertical
		for (int x = 0; x <= GridWidth; x++)
		{
			var from = new Vector2(x * CellWidth, 0);
			var to = new Vector2(x * CellWidth, totalHeight);
			DrawLine(from, to, GridColor, 1);
		}

		// Horizontal
		for (int y = 0; y <= GridHeight; y++)
		{
			var from = new Vector2(0, y * CellHeight);
			var to = new Vector2(totalWidth, y * CellHeight);
			DrawLine(from, to, GridColor, 1);
		}
	}

	private void DrawCells()
	{
		for (int x = 0; x < GridWidth; x++)
		{
			for (int y = 0; y < GridHeight; y++)
			{
				if (!_cells[x, y])
					continue;

				var rect = new Rect2(
					x * CellWidth + 1,
					y * CellHeight + 1,
					CellWidth - 2,
					CellHeight - 2
				);
				DrawRect(rect, ActiveColor);
			}
		}

		// Hover‑Highlight
		if (_hoverX >= 0 && _hoverY >= 0)
		{
			var hoverRect = new Rect2(
				_hoverX * CellWidth,
				_hoverY * CellHeight,
				CellWidth,
				CellHeight
			);
			DrawRect(hoverRect, HoverColor);
		}
	}

	// -------------------------------------------------------
	// Input
	// -------------------------------------------------------

	public override void _UnhandledInput(InputEvent @event)
	{
		if (@event is InputEventMouseButton mouseEvent)
		{
			if (mouseEvent.ButtonIndex == MouseButton.Left ||
				mouseEvent.ButtonIndex == MouseButton.Right)
			{
				if (mouseEvent.Pressed)
				{
					_isDrawing = true;
					_drawState = mouseEvent.ButtonIndex == MouseButton.Left;
				}
				else
				{
					_isDrawing = false;
				}
			}
		}

		if (@event is InputEventMouseMotion motion)
		{
			OnMouseHover(motion);

			if (_isDrawing)
			{
				OnDragDraw(motion);
			}
		}
	}

	private void OnDragDraw(InputEventMouseMotion motion)
	{
		Vector2 localPos = GetLocalMousePosition();
		int x = (int)(localPos.X / CellWidth);
		int y = (int)(localPos.Y / CellHeight);

		if (x < 0 || x >= GridWidth || y < 0 || y >= GridHeight)
			return;

		SetCellState(x, y, _drawState);
	}

	public void OnMouseHover(InputEventMouseMotion motion)
	{
		Vector2 localPos = GetLocalMousePosition();
		int hx = (int)(localPos.X / CellWidth);
		int hy = (int)(localPos.Y / CellHeight);

		if (hx < 0 || hx >= GridWidth || hy < 0 || hy >= GridHeight)
		{
			_hoverX = -1;
			_hoverY = -1;
		}
		else
		{
			_hoverX = hx;
			_hoverY = hy;
		}

		QueueRedraw();
	}

	// -------------------------------------------------------
	// Cell helpers & Patterns
	// -------------------------------------------------------

	private void SetCellState(int x, int y, bool alive)
	{
		if (_cells[x, y] == alive)
			return;

		_cells[x, y] = alive;

		if (alive)
			_populationCount++;
		else
			_populationCount--;

		QueueRedraw();
		EmitStatsChanged();
	}

	public void ApplyPattern(Vector2I origin, Vector2I[] pattern)
	{
		foreach (var offset in pattern)
		{
			int x = origin.X + offset.X;
			int y = origin.Y + offset.Y;

			if (x < 0 || x >= GridWidth || y < 0 || y >= GridHeight)
				continue;

			SetCellState(x, y, true);
		}

		QueueRedraw();
		EmitStatsChanged();
	}

	public void PlaceGliderAtHover()
	{
		if (_hoverX < 0 || _hoverY < 0)
			return;

		ApplyPattern(new Vector2I(_hoverX, _hoverY), GliderPattern);
	}

	// -------------------------------------------------------
	// Game of Life Logic
	// -------------------------------------------------------

	public int CalculateNeighbours(int xPos, int yPos)
	{
		int neighbourCount = 0;

		for (int x = xPos - 1; x <= xPos + 1; x++)
		{
			for (int y = yPos - 1; y <= yPos + 1; y++)
			{
				// Own cell
				if (x == xPos && y == yPos)
					continue;

				// Out of bounds
				if (x < 0 || x >= GridWidth || y < 0 || y >= GridHeight)
					continue;

				if (_cells[x, y])
					neighbourCount++;
			}
		}

		return neighbourCount;
	}

	public void NextTick()
	{
		_populationCount = 0;
		_ticksThisSecond++;

		for (int x = 0; x < GridWidth; x++)
		{
			for (int y = 0; y < GridHeight; y++)
			{
				int neighbours = CalculateNeighbours(x, y);
				bool current = _cells[x, y];
				bool next = current;

				if (current && neighbours < 2)
				{
					next = false;
					_cellsDiedCount++;
				}
				else if (current && neighbours > 3)
				{
					next = false;
					_cellsDiedCount++;
				}
				else if (!current && neighbours == 3)
				{
					next = true;
					_cellsRevivedCount++;
				}

				_nextCells[x, y] = next;

				if (next)
					_populationCount++;
			}
		}

		_cells = _nextCells;
		_generationCount++;
		QueueRedraw();
		EmitStatsChanged();
	}

	// -------------------------------------------------------
	// Timer / Control
	// -------------------------------------------------------

	public void SetTimerSeconds(double timerSeconds)
	{
		_timerSeconds = timerSeconds;
	}

	public void SetAutoTick(bool autoTick)
	{
		_timerActive = autoTick;
	}

	private void EmitStatsChanged()
	{
		EmitSignal(SignalName.StatsChanged);
	}

	// -------------------------------------------------------
	// Random / Reset
	// -------------------------------------------------------

	public void Randomize()
	{
		ResetGrid();

		for (int x = 0; x < GridWidth; x++)
		{
			for (int y = 0; y < GridHeight; y++)
			{
				bool alive = _rng.NextDouble() < _rngDensity;
				_nextCells[x, y] = alive;

				if (alive)
					_populationCount++;
			}
		}

		_cells = _nextCells;

		QueueRedraw();
		EmitStatsChanged();
	}

	public void ResetGrid()
	{
		_populationCount = 0;
		_cellsDiedCount = 0;
		_cellsRevivedCount = 0;
		_generationCount = 0;

		for (int x = 0; x < GridWidth; x++)
		{
			for (int y = 0; y < GridHeight; y++)
			{
				_nextCells[x, y] = false;
			}
		}

		_cells = _nextCells;

		QueueRedraw();
		EmitStatsChanged();
	}

	// -------------------------------------------------------
	// Public Readonly Properties
	// -------------------------------------------------------

	public bool TimerActive => _timerActive;
	public long GenerationCount => _generationCount;
	public long CellsDiedCount => _cellsDiedCount;
	public long CellsRevivedCount => _cellsRevivedCount;
	public long PopulationCount => _populationCount;
	public int TicksPerSecond => _ticksPerSecond;
}
