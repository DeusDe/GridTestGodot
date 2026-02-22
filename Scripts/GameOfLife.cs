using Godot;
using System;

public partial class GameOfLife : Node2D
{
	[Export] public int GridWidth = 25;
	[Export] public int GridHeight = 25;
	[Export] public int CellHeight = 16;
	[Export] public int CellWidth = 16;
	[Export] public Color ActiveColor = new Color(0.1f, 0.8f, 0.2f, 1f);
	[Export] public Color GridColor = new Color(0.2f, 0.03f, 0.045f, 1f);
	[Export] public Color HoverColor = new Color(1f, 1f, 1f, 0.2f);
	[Signal] public delegate void StatsChangedEventHandler();
	private Random _rng = new Random();
	private float _rngDensity = 0.35f;
	private int _hoverX = -1;
	private int _hoverY = -1;
	private bool[,] _cells;
	private bool[,] _nextCells;
	private long _generationCount = 0;
	private long _cellsDiedCount = 0;
	private long _cellsRevivedCount = 0;
	private long _populationCount = 0;
	private int _ticksThisSecond = 0;
	private int _ticksPerSecond = 0;
	private double _tpsTimer = 0.0;
	private double _timer = 0.0f;
	private double _timerSeconds = 0.2f;
	private bool _timerActive = false;

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

	//Draws the Grid
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

	public override void _UnhandledInput(InputEvent @event)
	{
		if (@event is InputEventMouseButton mouseEvent)
		{


			if (mouseEvent.Pressed && mouseEvent.ButtonIndex == MouseButton.Left)
				OnLeftMouseClick((InputEventMouseButton)@event);

			//			if (mouseEvent.Pressed && mouseEvent.ButtonIndex == MouseButton.Right)
			//				nextTick();


		}

		if (@event is InputEventMouseMotion motion)
		{
			OnMouseHover((InputEventMouseMotion)@event);
		}

	}


	public void OnLeftMouseClick(InputEventMouseButton mouseEvent)
	{
		// Mouse Position
		Vector2 localPos = GetLocalMousePosition();

		int x = (int)(localPos.X / CellWidth);
		int y = (int)(localPos.Y / CellHeight);

		if (x < 0 || x >= GridWidth || y < 0 || y >= GridHeight)
			return;

		_cells[x, y] = !_cells[x, y];

		if (_cells[x, y] == true)
			_populationCount++;
		else
			_populationCount--;

		QueueRedraw();
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

	public int CalculateNeighbours(int xPos, int yPos)
	{
		int neighbourCount = 0;

		for (int x = xPos - 1; x <= xPos + 1; x++)
		{
			for (int y = yPos - 1; y <= yPos + 1; y++)
			{
				//OwnCell
				if (xPos == x && yPos == y)
					continue;

				//OutOfBounds Cell
				if (x < 0 || x >= GridWidth || y < 0 || y >= GridHeight)
					continue;

				if (_cells[x, y] == true)
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
				if (_cells[x, y] == true && neighbours < 2)
				{
					_nextCells[x, y] = false;
					_cellsDiedCount++;
				}
				else if (_cells[x, y] == true && (neighbours == 2 || neighbours == 3))
				{
					_nextCells[x, y] = true;
				}
				else if (_cells[x, y] == true && neighbours > 3)
				{
					_nextCells[x, y] = false;
					_cellsDiedCount++;
				}
				else if (_cells[x, y] == false && neighbours == 3)
				{
					_nextCells[x, y] = true;
					_cellsRevivedCount++;
				}
				else
				{
					_nextCells[x, y] = _cells[x, y];
				}

				if (_nextCells[x, y] == true)
					_populationCount++;

			}
		}

		_cells = _nextCells;
		_generationCount++;
		QueueRedraw();
		EmitStatsChanged();
	}

	public void SetTimerSeconds(double timerSeconds)
	{
		_timerSeconds = timerSeconds;
	}

	public void setAutoTick(bool autoTick)
	{
		_timerActive = autoTick;
	}

	private void EmitStatsChanged()
	{
		EmitSignal(SignalName.StatsChanged);
	}

	public void Randomize()
	{
		_populationCount = 0;
		_cellsDiedCount = 0;
		_cellsRevivedCount = 0;
		_generationCount = 0;

		for (int x = 0; x < GridWidth; x++)
		{
			for (int y = 0; y < GridHeight; y++)
			{
				bool alive = _rng.NextDouble() < _rngDensity;
				_cells[x, y] = alive;
				_nextCells[x, y] = alive;

				if (alive)
					_populationCount++;
			}
		}

		QueueRedraw();
		EmitStatsChanged();
	}

	public bool TimerActive => _timerActive;
	public long GenerationCount => _generationCount;
	public long CellsDiedCount => _cellsDiedCount;
	public long CellsRevivedCount => _cellsRevivedCount;
	public long PopulationCount => _populationCount;
	public int TicksPerSecond => _ticksPerSecond;



}
