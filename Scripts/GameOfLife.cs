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
	public static readonly Vector2I[] GliderPattern =
	{
	new Vector2I(1, 0),
	new Vector2I(2, 1),
	new Vector2I(0, 2),
	new Vector2I(1, 2),
	new Vector2I(2, 2),
	};

	// Still lifes
	public static readonly Vector2I[] BlockPattern =
	{
	new Vector2I(0, 0),
	new Vector2I(1, 0),
	new Vector2I(0, 1),
	new Vector2I(1, 1),
};

	public static readonly Vector2I[] BeehivePattern =
	{
	new Vector2I(1, 0),
	new Vector2I(2, 0),
	new Vector2I(0, 1),
	new Vector2I(3, 1),
	new Vector2I(1, 2),
	new Vector2I(2, 2),
};

	// Oscillators
	public static readonly Vector2I[] BlinkerPattern =
	{
	new Vector2I(0, 1),
	new Vector2I(1, 1),
	new Vector2I(2, 1),
};

	public static readonly Vector2I[] ToadPattern =
	{
	new Vector2I(1, 0),
	new Vector2I(2, 0),
	new Vector2I(3, 0),
	new Vector2I(0, 1),
	new Vector2I(1, 1),
	new Vector2I(2, 1),
};

	public static readonly Vector2I[] BeaconPattern =
	{
	new Vector2I(0, 0),
	new Vector2I(1, 0),
	new Vector2I(0, 1),
	new Vector2I(3, 2),
	new Vector2I(2, 3),
	new Vector2I(3, 3),
};

	public static readonly Vector2I[] LightweightSpaceshipPattern =
	{
	new Vector2I(1, 0),
	new Vector2I(4, 0),
	new Vector2I(0, 1),
	new Vector2I(0, 2),
	new Vector2I(4, 2),
	new Vector2I(0, 3),
	new Vector2I(1, 3),
	new Vector2I(2, 3),
	new Vector2I(3, 3),
};

	public static readonly Vector2I[] LoafPattern =
	{
	new Vector2I(1, 0),
	new Vector2I(2, 0),
	new Vector2I(0, 1),
	new Vector2I(3, 1),
	new Vector2I(1, 2),
	new Vector2I(3, 2),
	new Vector2I(2, 3),
};

	public static readonly Vector2I[] BoatPattern =
	{
	new Vector2I(0, 0),
	new Vector2I(1, 0),
	new Vector2I(0, 1),
	new Vector2I(2, 1),
	new Vector2I(1, 2),
};

	public static readonly Vector2I[] PulsarPattern =
	{
	new Vector2I(4, 0), new Vector2I(5, 0), new Vector2I(6, 0),
	new Vector2I(10, 0), new Vector2I(11, 0), new Vector2I(12, 0),

	new Vector2I(2, 2), new Vector2I(7, 2), new Vector2I(9, 2), new Vector2I(14, 2),
	new Vector2I(2, 3), new Vector2I(7, 3), new Vector2I(9, 3), new Vector2I(14, 3),
	new Vector2I(2, 4), new Vector2I(7, 4), new Vector2I(9, 4), new Vector2I(14, 4),

	new Vector2I(4, 5), new Vector2I(5, 5), new Vector2I(6, 5),
	new Vector2I(10, 5), new Vector2I(11, 5), new Vector2I(12, 5),

	new Vector2I(4, 7), new Vector2I(5, 7), new Vector2I(6, 7),
	new Vector2I(10, 7), new Vector2I(11, 7), new Vector2I(12, 7),

	new Vector2I(2, 8), new Vector2I(7, 8), new Vector2I(9, 8), new Vector2I(14, 8),
	new Vector2I(2, 9), new Vector2I(7, 9), new Vector2I(9, 9), new Vector2I(14, 9),
	new Vector2I(2, 10), new Vector2I(7, 10), new Vector2I(9, 10), new Vector2I(14, 10),

	new Vector2I(4, 12), new Vector2I(5, 12), new Vector2I(6, 12),
	new Vector2I(10, 12), new Vector2I(11, 12), new Vector2I(12, 12),
};

	public static readonly Vector2I[] PentadecathlonPattern =
	{
	new Vector2I(1, 0),
	new Vector2I(2, 0),
	new Vector2I(3, 0),
	new Vector2I(4, 0),
	new Vector2I(5, 0),
	new Vector2I(6, 0),
	new Vector2I(7, 0),
	new Vector2I(8, 0),

	new Vector2I(0, 1),
	new Vector2I(2, 1),
	new Vector2I(3, 1),
	new Vector2I(4, 1),
	new Vector2I(5, 1),
	new Vector2I(6, 1),
	new Vector2I(7, 1),
	new Vector2I(9, 1),

	new Vector2I(1, 2),
	new Vector2I(2, 2),
	new Vector2I(3, 2),
	new Vector2I(4, 2),
	new Vector2I(5, 2),
	new Vector2I(6, 2),
	new Vector2I(7, 2),
	new Vector2I(8, 2),
};

	public static readonly Vector2I[] RPentominoPattern =
	{
	new Vector2I(1, 0),
	new Vector2I(2, 0),
	new Vector2I(0, 1),
	new Vector2I(1, 1),
	new Vector2I(1, 2),
};

	public static readonly Vector2I[] SmallGunSeedPattern =
	{
	new Vector2I(0, 4), new Vector2I(1, 4),
	new Vector2I(0, 5), new Vector2I(1, 5),

	new Vector2I(10, 4), new Vector2I(10, 5), new Vector2I(10, 6),
	new Vector2I(11, 3), new Vector2I(11, 7),
	new Vector2I(12, 2), new Vector2I(12, 8),
	new Vector2I(13, 2), new Vector2I(13, 8),
};

	public enum PatternType
	{
		Glider,
		Block,
		Beehive,
		Blinker,
		Toad,
		Beacon,
		LightweightSpaceship,
		Loaf,
		Boat,
		Pulsar,
		Pentadecathlon,
		RPentomino,
		SmallGunSeed
	}



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

	//Tool
	public enum Tool { Draw, Pattern }
	private Tool _currentTool = Tool.Draw;
	private Vector2I[] _currentPattern = GetPattern(PatternType.Glider);

	// Rendering & Texture
	private Image _gridImage;
	private ImageTexture _gridTexture;
	private byte[] _imageData;


	// -------------------------------------------------------
	// Lifecycle
	// -------------------------------------------------------

	public override void _Ready()
	{
		_cells = new bool[GridWidth, GridHeight];
		_nextCells = new bool[GridWidth, GridHeight];

		_imageData = new byte[GridWidth * GridHeight * 4]; 
		_gridImage = Image.CreateFromData(GridWidth, GridHeight, false, Image.Format.Rgba8, _imageData);
		_gridTexture = ImageTexture.CreateFromImage(_gridImage);
		TextureFilter = TextureFilterEnum.Nearest;
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
		}

		EmitStatsChanged();
	}

public override void _Draw()
	{
var fullRect = new Rect2(0, 0, GridWidth * CellWidth, GridHeight * CellHeight);
		DrawTextureRect(_gridTexture, fullRect, false);
		DrawGridLines();
		DrawHover();
	}

	private void DrawGridLines()
	{
		var totalWidth = GridWidth * CellWidth;
		var totalHeight = GridHeight * CellHeight;

		for (int x = 0; x <= GridWidth; x++)
			DrawLine(new Vector2(x * CellWidth, 0), new Vector2(x * CellWidth, totalHeight), GridColor, 1);

		for (int y = 0; y <= GridHeight; y++)
			DrawLine(new Vector2(0, y * CellHeight), new Vector2(totalWidth, y * CellHeight), GridColor, 1);
	}

	public void RefreshColors()
	{
		for (int x = 0; x < GridWidth; x++)
		{
			for (int y = 0; y < GridHeight; y++)
			{
				SetPixelData(x, y, _cells[x, y]);
			}
		}
		ApplyImageData();
	}

	private void DrawHover()
	{
		switch (_currentTool)
		{
			case Tool.Draw:
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
				break;

			case Tool.Pattern:
				if (_hoverX >= 0 && _hoverY >= 0) 
				{
					foreach (var offset in _currentPattern)
					{
						int x = _hoverX + offset.X;
						int y = _hoverY + offset.Y;

						if (x < 0 || x >= GridWidth || y < 0 || y >= GridHeight)
							continue;

						var hoverRect = new Rect2(
							x * CellWidth,
							y * CellHeight,
							CellWidth,
							CellHeight
						);

						DrawRect(hoverRect, HoverColor);
					}
				}
				break;
		}
	}

	// -------------------------------------------------------
	// Input
	// -------------------------------------------------------

	public override void _UnhandledInput(InputEvent @event)
	{
		if (@event is InputEventMouseButton mouseEvent)
		{
			HandleMouseButton(mouseEvent);
		}

		if (@event is InputEventMouseMotion motion)
		{
			OnMouseHover(motion);

			if (_isDrawing && _currentTool != Tool.Pattern)
			{
				OnDragDraw(motion);
			}
		}
	}

	private void HandleMouseButton(InputEventMouseButton mouseEvent)
	{
		if (!mouseEvent.Pressed)
		{
			_isDrawing = false;
			return;
		}

		Vector2 localPos = GetLocalMousePosition();
		int x = Mathf.FloorToInt(localPos.X / CellWidth);
		int y = Mathf.FloorToInt(localPos.Y / CellHeight);

		if (x < 0 || x >= GridWidth || y < 0 || y >= GridHeight)
			return;

		switch (_currentTool)
		{
			case Tool.Draw:
				if (mouseEvent.ButtonIndex == MouseButton.Left)
				{
					_isDrawing = true;
					_drawState = true;
					SetCellState(x, y, true);
				}
				else if (mouseEvent.ButtonIndex == MouseButton.Right)
				{
					_isDrawing = true;
					_drawState = false;
					SetCellState(x, y, false);
				}
				break;

			case Tool.Pattern:
				if (mouseEvent.ButtonIndex == MouseButton.Left)
				{
					if (_hoverX >= 0 && _hoverY >= 0)
						ApplyPattern(new Vector2I(_hoverX, _hoverY), _currentPattern);
					else
						ApplyPattern(new Vector2I(x, y), _currentPattern);
				}
				break;
		}
	}


	private void OnDragDraw(InputEventMouseMotion motion)
	{
		Vector2 localPos = GetLocalMousePosition();
		int x = Mathf.FloorToInt(localPos.X / CellWidth);
		int y = Mathf.FloorToInt(localPos.Y / CellHeight);

		if (x < 0 || x >= GridWidth || y < 0 || y >= GridHeight)
			return;

		SetCellState(x, y, _drawState);
	}

	public void OnMouseHover(InputEventMouseMotion motion)
	{
		Vector2 localPos = GetLocalMousePosition();
		int hx = Mathf.FloorToInt(localPos.X / CellWidth);
		int hy = Mathf.FloorToInt(localPos.Y / CellHeight);

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

		ApplyImageData();
	}

	// -------------------------------------------------------
	// Cell helpers & Patterns
	// -------------------------------------------------------

	private void SetCellState(int x, int y, bool alive)
	{
		if (_cells[x, y] == alive)
			return;

		_cells[x, y] = alive;
		SetPixelData(x, y, alive);
		ApplyImageData();

		if (alive)
			_populationCount++;
		else
			_populationCount--;

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

		ApplyImageData();
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
				SetPixelData(x, y, next);

				if (next)
					_populationCount++;
			}
		}

		bool[,] temp = _cells;
		_cells = _nextCells;
		_nextCells = temp;

		_generationCount++;
		ApplyImageData();
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
				_cells[x, y] = alive;
				SetPixelData(x, y, alive);
				if (alive)
					_populationCount++;
			}
		}

		ApplyImageData();
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
				_cells[x, y] = false;
				SetPixelData(x, y, false);
			}
		}

		ApplyImageData();
	}

	// -------------------------------------------------------
	// Tool Management
	// -------------------------------------------------------

	public void SetToolDraw() => _currentTool = Tool.Draw;

	public void SetToolPattern(PatternType type)
	{
		_currentTool = Tool.Pattern;
		_currentPattern = GetPattern(type);
	}

	public void SetToolPattern()
	{
		_currentTool = Tool.Pattern;
	}


	public static Vector2I[] GetPattern(PatternType type)
	{
		switch (type)
		{
			case PatternType.Glider: return GliderPattern;
			case PatternType.Block: return BlockPattern;
			case PatternType.Beehive: return BeehivePattern;
			case PatternType.Blinker: return BlinkerPattern;
			case PatternType.Toad: return ToadPattern;
			case PatternType.Beacon: return BeaconPattern;
			case PatternType.LightweightSpaceship: return LightweightSpaceshipPattern;
			case PatternType.Loaf: return LoafPattern;
			case PatternType.Boat: return BoatPattern;
			case PatternType.Pulsar: return PulsarPattern;
			case PatternType.Pentadecathlon: return PentadecathlonPattern;
			case PatternType.RPentomino: return RPentominoPattern;
			case PatternType.SmallGunSeed: return SmallGunSeedPattern;
			default: return GliderPattern;
		}
	}


	// -------------------------------------------------------
	// Rendering
	// -------------------------------------------------------
private void SetPixelData(int x, int y, bool alive)
	{
		int index = (y * GridWidth + x) * 4;
		Color color = alive ? ActiveColor : Colors.Transparent;

		_imageData[index] = (byte)(color.R * 255f);
		_imageData[index + 1] = (byte)(color.G * 255f);
		_imageData[index + 2] = (byte)(color.B * 255f);
		_imageData[index + 3] = (byte)(color.A * 255f);
	}

	private void ApplyImageData()
	{
		_gridImage.SetData(GridWidth, GridHeight, false, Image.Format.Rgba8, _imageData);
		_gridTexture.Update(_gridImage);
		QueueRedraw();
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
	public Tool CurrentTool => _currentTool;
}
