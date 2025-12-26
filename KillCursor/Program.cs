namespace KillCursor;

/// <summary>
/// Cursor survival game where the player must avoid enemies that chase them.
/// </summary>
public class Program
{
    private const int InitialGameWidth = 80;
    private const int InitialGameHeight = 30;

    public static void Main(string[] args)
    {
        // Set up console window
        try
        {
            Console.SetWindowSize(InitialGameWidth, InitialGameHeight);
            Console.SetBufferSize(InitialGameWidth, InitialGameHeight);
            Console.CursorVisible = true; // Make cursor visible so player can see it
        }
        catch (Exception)
        {
            // Ignore if console size cannot be set
        }

        var game = new Game(InitialGameWidth, InitialGameHeight);
        game.Run();
    }
}

/// <summary>
/// Represents a 2D position in the game world.
/// </summary>
public struct Position
{
    public int X { get; set; }
    public int Y { get; set; }

    public Position(int x, int y)
    {
        X = x;
        Y = y;
    }

    /// <summary>
    /// Calculates the distance to another position.
    /// </summary>
    public double DistanceTo(Position other)
    {
        int dx = X - other.X;
        int dy = Y - other.Y;
        return Math.Sqrt(dx * dx + dy * dy);
    }

    /// <summary>
    /// Checks if this position equals another position.
    /// </summary>
    public bool Equals(Position other)
    {
        return X == other.X && Y == other.Y;
    }
}

/// <summary>
/// Represents an obstacle in the game world.
/// Single Responsibility: Manages obstacle state.
/// </summary>
public class Obstacle
{
    public Position Position { get; }
    private const char ObstacleChar = '█';

    public Obstacle(int x, int y)
    {
        Position = new Position(x, y);
    }

    public char GetCharacter()
    {
        return ObstacleChar;
    }
}

/// <summary>
/// Represents a collectible token.
/// Single Responsibility: Manages token state.
/// </summary>
public class Token
{
    public Position Position { get; }
    public bool IsCollected { get; private set; }
    private const char TokenChar = '$';

    public Token(int x, int y)
    {
        Position = new Position(x, y);
        IsCollected = false;
    }

    public void Collect()
    {
        IsCollected = true;
    }

    public char GetCharacter()
    {
        return TokenChar;
    }
}

/// <summary>
/// Represents the player controlled by keyboard.
/// Single Responsibility: Manages player state and movement.
/// </summary>
public class Player
{
    public Position Position { get; private set; }
    public bool IsAlive { get; private set; }
    public int MoveCount { get; private set; }
    public int TokensCollected { get; private set; }

    private const char PlayerChar = '@';
    private const int TokensToWin = 10;

    public Player(int startX, int startY)
    {
        Position = new Position(startX, startY);
        IsAlive = true;
        MoveCount = 0;
        TokensCollected = 0;
    }

    /// <summary>
    /// Moves the player to a new position.
    /// </summary>
    public void MoveTo(Position newPosition)
    {
        Position = newPosition;
        MoveCount++;
    }

    /// <summary>
    /// Collects a token.
    /// </summary>
    public void CollectToken()
    {
        TokensCollected++;
    }

    /// <summary>
    /// Checks if player has won by collecting enough tokens.
    /// </summary>
    public bool HasWon()
    {
        return TokensCollected >= TokensToWin;
    }

    /// <summary>
    /// Kills the player (game over).
    /// </summary>
    public void Kill()
    {
        IsAlive = false;
    }

    /// <summary>
    /// Gets the character representation of the player.
    /// </summary>
    public char GetCharacter()
    {
        return PlayerChar;
    }

    public int GetTokensNeeded()
    {
        return TokensToWin;
    }
}

/// <summary>
/// Represents an enemy that chases the player.
/// Single Responsibility: Manages enemy state and movement behavior.
/// </summary>
public class Enemy
{
    public Position Position { get; private set; }
    public double Speed { get; private set; }
    public bool CanSeePlayer { get; set; }

    private const char EnemyChar = '*';
    private const char AlertEnemyChar = '!';
    private const double BaseSpeed = 1.0;
    private const double VisionRange = 10.0;
    private const double WanderChance = 0.4; // 40% chance to wander when idle

    private readonly Random _random;
    private int _idleCounter; // Counter for idle behavior
    private Position? _wanderTarget; // Current wander destination

    public Enemy(int x, int y, double speedMultiplier = 1.0)
    {
        Position = new Position(x, y);
        Speed = BaseSpeed * speedMultiplier;
        CanSeePlayer = false;
        _random = new Random(Guid.NewGuid().GetHashCode()); // Unique seed per enemy
        _idleCounter = 0;
        _wanderTarget = null;
    }

    /// <summary>
    /// Moves the enemy towards the target position or wanders if idle.
    /// </summary>
    public void MoveTowards(Position target, List<Obstacle> obstacles, int worldWidth, int worldHeight)
    {
        if (CanSeePlayer)
        {
            // Chase player
            _wanderTarget = null; // Cancel wandering
            MoveTowardsTarget(target, obstacles);
        }
        else
        {
            // Wander when idle
            Wander(obstacles, worldWidth, worldHeight);
        }
    }

    /// <summary>
    /// Moves towards a specific target using Manhattan movement.
    /// </summary>
    private void MoveTowardsTarget(Position target, List<Obstacle> obstacles)
    {
        int dx = target.X - Position.X;
        int dy = target.Y - Position.Y;

        if (dx == 0 && dy == 0) return;

        int newX = Position.X;
        int newY = Position.Y;

        // Prioritize the axis with greater distance
        if (Math.Abs(dx) > Math.Abs(dy))
        {
            newX += Math.Sign(dx) * (int)Speed;
        }
        else if (Math.Abs(dy) > 0)
        {
            newY += Math.Sign(dy) * (int)Speed;
        }
        else if (Math.Abs(dx) > 0)
        {
            newX += Math.Sign(dx) * (int)Speed;
        }

        Position newPos = new Position(newX, newY);
        if (!IsObstacleAt(newPos, obstacles))
        {
            Position = newPos;
        }
    }

    /// <summary>
    /// Wanders randomly when not chasing the player.
    /// </summary>
    private void Wander(List<Obstacle> obstacles, int worldWidth, int worldHeight)
    {
        // Increment idle counter
        _idleCounter++;

        // Decide to wander every few turns
        if (_idleCounter >= 3) // Wait 3 turns between movements
        {
            _idleCounter = 0;

            // Random chance to move or stay idle
            if (_random.NextDouble() < WanderChance)
            {
                // Pick random direction (0=up, 1=right, 2=down, 3=left)
                int direction = _random.Next(4);
                int newX = Position.X;
                int newY = Position.Y;

                switch (direction)
                {
                    case 0: // Up
                        newY -= (int)Speed;
                        break;
                    case 1: // Right
                        newX += (int)Speed;
                        break;
                    case 2: // Down
                        newY += (int)Speed;
                        break;
                    case 3: // Left
                        newX -= (int)Speed;
                        break;
                }

                // Validate new position
                if (newX >= 1 && newX < worldWidth - 1 &&
                    newY >= 1 && newY < worldHeight - 1)
                {
                    Position newPos = new Position(newX, newY);
                    if (!IsObstacleAt(newPos, obstacles))
                    {
                        Position = newPos;
                    }
                }
            }
        }
    }

    /// <summary>
    /// Checks if there's an obstacle at the given position.
    /// </summary>
    private bool IsObstacleAt(Position pos, List<Obstacle> obstacles)
    {
        foreach (var obstacle in obstacles)
        {
            if (obstacle.Position.Equals(pos))
                return true;
        }
        return false;
    }

    /// <summary>
    /// Checks if this enemy can see the player (within range and line of sight).
    /// </summary>
    public bool CanSeeTarget(Position playerPosition, List<Obstacle> obstacles)
    {
        double distance = Position.DistanceTo(playerPosition);

        if (distance > VisionRange)
            return false;

        return HasLineOfSight(Position, playerPosition, obstacles);
    }

    /// <summary>
    /// Uses Bresenham's line algorithm to check if there's a clear line of sight.
    /// </summary>
    private bool HasLineOfSight(Position from, Position to, List<Obstacle> obstacles)
    {
        int x0 = from.X, y0 = from.Y;
        int x1 = to.X, y1 = to.Y;

        int dx = Math.Abs(x1 - x0);
        int dy = Math.Abs(y1 - y0);
        int sx = x0 < x1 ? 1 : -1;
        int sy = y0 < y1 ? 1 : -1;
        int err = dx - dy;

        while (true)
        {
            if (x0 == x1 && y0 == y1)
                return true;

            if (!(x0 == from.X && y0 == from.Y))
            {
                Position currentPos = new Position(x0, y0);
                foreach (var obstacle in obstacles)
                {
                    if (obstacle.Position.Equals(currentPos))
                        return false;
                }
            }

            int e2 = 2 * err;
            if (e2 > -dy)
            {
                err -= dy;
                x0 += sx;
            }
            if (e2 < dx)
            {
                err += dx;
                y0 += sy;
            }
        }
    }

    /// <summary>
    /// Increases enemy speed by a percentage.
    /// </summary>
    public void IncreaseSpeed(double percentage)
    {
        Speed *= (1.0 + percentage);
    }

    /// <summary>
    /// Gets the character representation of the enemy.
    /// </summary>
    public char GetCharacter()
    {
        return CanSeePlayer ? AlertEnemyChar : EnemyChar;
    }

    public double GetVisionRange()
    {
        return VisionRange;
    }
}

/// <summary>
/// Manages obstacles in the game world.
/// Single Responsibility: Handles obstacle creation and queries.
/// </summary>
public class ObstacleManager
{
    private readonly List<Obstacle> _obstacles;
    private readonly Random _random;
    private readonly int _worldWidth;
    private readonly int _worldHeight;

    private const int ObstacleCount = 25;

    public IReadOnlyList<Obstacle> Obstacles => _obstacles;

    public ObstacleManager(int worldWidth, int worldHeight)
    {
        _worldWidth = worldWidth;
        _worldHeight = worldHeight - 2;
        _obstacles = new List<Obstacle>();
        _random = new Random();
    }

    /// <summary>
    /// Spawns random obstacles in the game world.
    /// </summary>
    public void SpawnObstacles(Position playerStart, List<Position> enemyPositions)
    {
        int spawned = 0;
        int maxAttempts = ObstacleCount * 10;
        int attempts = 0;

        while (spawned < ObstacleCount && attempts < maxAttempts)
        {
            attempts++;
            int x = _random.Next(2, _worldWidth - 2);
            int y = _random.Next(2, _worldHeight - 2);
            Position pos = new Position(x, y);

            if (pos.Equals(playerStart))
                continue;

            bool onEnemy = false;
            foreach (var enemyPos in enemyPositions)
            {
                if (pos.Equals(enemyPos))
                {
                    onEnemy = true;
                    break;
                }
            }
            if (onEnemy) continue;

            bool duplicate = false;
            foreach (var obstacle in _obstacles)
            {
                if (obstacle.Position.Equals(pos))
                {
                    duplicate = true;
                    break;
                }
            }
            if (duplicate) continue;

            if (pos.DistanceTo(playerStart) < 5)
                continue;

            _obstacles.Add(new Obstacle(x, y));
            spawned++;
        }
    }

    /// <summary>
    /// Checks if a position contains an obstacle.
    /// </summary>
    public bool IsObstacleAt(Position position)
    {
        foreach (var obstacle in _obstacles)
        {
            if (obstacle.Position.Equals(position))
                return true;
        }
        return false;
    }
}

/// <summary>
/// Manages collectible tokens in the game world.
/// Single Responsibility: Handles token spawning and collection.
/// </summary>
public class TokenManager
{
    private readonly List<Token> _tokens;
    private readonly Random _random;
    private readonly int _worldWidth;
    private readonly int _worldHeight;

    private const int InitialTokenCount = 15;

    public IReadOnlyList<Token> Tokens => _tokens;

    public TokenManager(int worldWidth, int worldHeight)
    {
        _worldWidth = worldWidth;
        _worldHeight = worldHeight - 2;
        _tokens = new List<Token>();
        _random = new Random();
    }

    /// <summary>
    /// Spawns tokens in random positions.
    /// </summary>
    public void SpawnTokens(Position playerStart, List<Position> enemyPositions, List<Obstacle> obstacles)
    {
        int spawned = 0;
        int maxAttempts = InitialTokenCount * 20;
        int attempts = 0;

        while (spawned < InitialTokenCount && attempts < maxAttempts)
        {
            attempts++;
            int x = _random.Next(2, _worldWidth - 2);
            int y = _random.Next(2, _worldHeight - 2);
            Position pos = new Position(x, y);

            if (pos.Equals(playerStart)) continue;

            bool invalid = false;
            foreach (var enemyPos in enemyPositions)
            {
                if (pos.Equals(enemyPos))
                {
                    invalid = true;
                    break;
                }
            }
            if (invalid) continue;

            foreach (var obstacle in obstacles)
            {
                if (obstacle.Position.Equals(pos))
                {
                    invalid = true;
                    break;
                }
            }
            if (invalid) continue;

            foreach (var token in _tokens)
            {
                if (token.Position.Equals(pos))
                {
                    invalid = true;
                    break;
                }
            }
            if (invalid) continue;

            _tokens.Add(new Token(x, y));
            spawned++;
        }
    }

    /// <summary>
    /// Checks if player can collect a token at their position.
    /// </summary>
    public bool TryCollectToken(Position playerPosition)
    {
        foreach (var token in _tokens)
        {
            if (!token.IsCollected && token.Position.Equals(playerPosition))
            {
                token.Collect();
                return true;
            }
        }
        return false;
    }
}

/// <summary>
/// Manages the collection of enemies and their spawning logic.
/// Single Responsibility: Handles enemy lifecycle and spawning mechanics.
/// </summary>
public class EnemyManager
{
    private readonly List<Enemy> _enemies;
    private readonly Random _random;
    private readonly int _worldWidth;
    private readonly int _worldHeight;

    private const int InitialEnemyCount = 2;
    private const double SpeedIncreasePerWave = 0.03;
    private const int MovesBeforeNewWave = 15;
    private const double AlertRadius = 15.0;

    private int _currentWave;
    private double _currentSpeedMultiplier;

    public IReadOnlyList<Enemy> Enemies => _enemies;

    public EnemyManager(int worldWidth, int worldHeight)
    {
        _worldWidth = worldWidth;
        _worldHeight = worldHeight - 2;
        _enemies = new List<Enemy>();
        _random = new Random();
        _currentWave = 0;
        _currentSpeedMultiplier = 1.0;
    }

    /// <summary>
    /// Initializes the game with starting enemies on borders.
    /// </summary>
    public void SpawnInitialEnemies()
    {
        for (int i = 0; i < InitialEnemyCount; i++)
        {
            SpawnEnemyOnBorder(_currentSpeedMultiplier);
        }
        _currentWave = 1;
    }

    /// <summary>
    /// Gets initial enemy positions for obstacle spawning.
    /// </summary>
    public List<Position> GetEnemyPositions()
    {
        var positions = new List<Position>();
        foreach (var enemy in _enemies)
        {
            positions.Add(enemy.Position);
        }
        return positions;
    }

    /// <summary>
    /// Updates enemy vision and alerts nearby enemies.
    /// </summary>
    public void UpdateVision(Position playerPosition, List<Obstacle> obstacles)
    {
        foreach (var enemy in _enemies)
        {
            enemy.CanSeePlayer = enemy.CanSeeTarget(playerPosition, obstacles);
        }

        bool anyEnemySeesPlayer = false;
        foreach (var enemy in _enemies)
        {
            if (enemy.CanSeePlayer)
            {
                anyEnemySeesPlayer = true;
                break;
            }
        }

        if (anyEnemySeesPlayer)
        {
            foreach (var alertedEnemy in _enemies)
            {
                if (!alertedEnemy.CanSeePlayer)
                {
                    foreach (var seeingEnemy in _enemies)
                    {
                        if (seeingEnemy.CanSeePlayer)
                        {
                            double distance = alertedEnemy.Position.DistanceTo(seeingEnemy.Position);
                            if (distance <= AlertRadius)
                            {
                                alertedEnemy.CanSeePlayer = true;
                                break;
                            }
                        }
                    }
                }
            }
        }
    }

    /// <summary>
    /// Checks if it's time to spawn a new wave based on player moves.
    /// </summary>
    public void CheckAndSpawnWave(int playerMoveCount)
    {
        int expectedWave = 1 + (playerMoveCount / MovesBeforeNewWave);

        if (expectedWave > _currentWave)
        {
            _currentWave = expectedWave;
            _currentSpeedMultiplier = 1.0 + ((_currentWave - 1) * SpeedIncreasePerWave);

            int enemiesToSpawn = 1 + (_currentWave / 7);

            for (int i = 0; i < enemiesToSpawn; i++)
            {
                SpawnEnemyOnBorder(_currentSpeedMultiplier);
            }

            foreach (var enemy in _enemies)
            {
                enemy.IncreaseSpeed(SpeedIncreasePerWave);
            }
        }
    }

    /// <summary>
    /// Spawns a single enemy on a random border position.
    /// </summary>
    private void SpawnEnemyOnBorder(double speedMultiplier)
    {
        int x, y;
        int border = _random.Next(4);

        switch (border)
        {
            case 0: // Top border
                x = _random.Next(1, _worldWidth - 1);
                y = 1;
                break;
            case 1: // Right border
                x = _worldWidth - 2;
                y = _random.Next(1, _worldHeight - 1);
                break;
            case 2: // Bottom border
                x = _random.Next(1, _worldWidth - 1);
                y = _worldHeight - 2;
                break;
            default: // Left border
                x = 1;
                y = _random.Next(1, _worldHeight - 1);
                break;
        }

        _enemies.Add(new Enemy(x, y, speedMultiplier));
    }

    /// <summary>
    /// Moves all enemies towards the player or makes them wander.
    /// </summary>
    public void MoveEnemies(Position playerPosition, List<Obstacle> obstacles)
    {
        foreach (var enemy in _enemies)
        {
            enemy.MoveTowards(playerPosition, obstacles, _worldWidth, _worldHeight);
        }
    }

    /// <summary>
    /// Checks if any enemy has caught the player.
    /// </summary>
    public bool HasCaughtPlayer(Position playerPosition)
    {
        foreach (var enemy in _enemies)
        {
            if (enemy.Position.Equals(playerPosition))
            {
                return true;
            }
        }
        return false;
    }

    public int GetCurrentWave()
    {
        return _currentWave;
    }
}

/// <summary>
/// Handles player input from keyboard.
/// Single Responsibility: Input processing and validation.
/// </summary>
public class InputHandler
{
    private readonly int _worldWidth;
    private readonly int _worldHeight;

    public InputHandler(int worldWidth, int worldHeight)
    {
        _worldWidth = worldWidth;
        _worldHeight = worldHeight - 2;
    }

    /// <summary>
    /// Waits for and gets the next player position based on keyboard input.
    /// Validates against obstacles.
    /// </summary>
    public Position? GetNextPosition(Position currentPosition, ObstacleManager obstacleManager)
    {
        while (true)
        {
            var key = Console.ReadKey(true).Key;

            int newX = currentPosition.X;
            int newY = currentPosition.Y;

            switch (key)
            {
                case ConsoleKey.UpArrow:
                case ConsoleKey.W:
                    newY--;
                    break;
                case ConsoleKey.DownArrow:
                case ConsoleKey.S:
                    newY++;
                    break;
                case ConsoleKey.LeftArrow:
                case ConsoleKey.A:
                    newX--;
                    break;
                case ConsoleKey.RightArrow:
                case ConsoleKey.D:
                    newX++;
                    break;
                case ConsoleKey.Escape:
                    return new Position(-1, -1);
                default:
                    continue;
            }

            if (newX < 1 || newX >= _worldWidth - 1 || newY < 1 || newY >= _worldHeight - 1)
                continue;

            Position newPos = new Position(newX, newY);

            if (obstacleManager.IsObstacleAt(newPos))
                continue;

            return newPos;
        }
    }
}

/// <summary>
/// Renders the game state to the console without flickering.
/// Single Responsibility: All rendering and display logic.
/// </summary>
public class GameRenderer
{
    private readonly int _width;
    private readonly int _height;
    private char[,] _previousBuffer;
    private char[,] _currentBuffer;

    private const char BorderChar = '#';
    private const char EmptyChar = ' ';

    public GameRenderer(int width, int height)
    {
        _width = width;
        _height = height - 2;

        _previousBuffer = new char[_height, _width];
        _currentBuffer = new char[_height, _width];

        for (int y = 0; y < _height; y++)
        {
            for (int x = 0; x < _width; x++)
            {
                _previousBuffer[y, x] = EmptyChar;
                _currentBuffer[y, x] = EmptyChar;
            }
        }
    }

    /// <summary>
    /// Renders the complete game scene.
    /// </summary>
    public void Render(Player player, EnemyManager enemyManager, ObstacleManager obstacleManager, TokenManager tokenManager)
    {
        for (int y = 0; y < _height; y++)
        {
            for (int x = 0; x < _width; x++)
            {
                _currentBuffer[y, x] = EmptyChar;
            }
        }

        DrawBorders(_currentBuffer);

        foreach (var obstacle in obstacleManager.Obstacles)
        {
            if (IsInBounds(obstacle.Position))
            {
                _currentBuffer[obstacle.Position.Y, obstacle.Position.X] = obstacle.GetCharacter();
            }
        }

        foreach (var token in tokenManager.Tokens)
        {
            if (!token.IsCollected && IsInBounds(token.Position))
            {
                _currentBuffer[token.Position.Y, token.Position.X] = token.GetCharacter();
            }
        }

        foreach (var enemy in enemyManager.Enemies)
        {
            if (IsInBounds(enemy.Position))
            {
                _currentBuffer[enemy.Position.Y, enemy.Position.X] = enemy.GetCharacter();
            }
        }

        if (player.IsAlive && IsInBounds(player.Position))
        {
            _currentBuffer[player.Position.Y, player.Position.X] = player.GetCharacter();
        }

        for (int y = 0; y < _height; y++)
        {
            for (int x = 0; x < _width; x++)
            {
                if (_currentBuffer[y, x] != _previousBuffer[y, x])
                {
                    Console.SetCursorPosition(x, y);
                    Console.Write(_currentBuffer[y, x]);
                    _previousBuffer[y, x] = _currentBuffer[y, x];
                }
            }
        }

        DrawHUD(player, enemyManager);

        if (player.IsAlive && IsInBounds(player.Position))
        {
            Console.SetCursorPosition(player.Position.X, player.Position.Y);
        }
    }

    private void DrawBorders(char[,] buffer)
    {
        for (int y = 0; y < _height; y++)
        {
            for (int x = 0; x < _width; x++)
            {
                if (x == 0 || x == _width - 1 || y == 0 || y == _height - 1)
                {
                    buffer[y, x] = BorderChar;
                }
            }
        }
    }

    private void DrawHUD(Player player, EnemyManager enemyManager)
    {
        Console.SetCursorPosition(0, _height);
        Console.Write($"Tokens: {player.TokensCollected}/{player.GetTokensNeeded()} | Wave: {enemyManager.GetCurrentWave()} | Enemies: {enemyManager.Enemies.Count}".PadRight(_width));
        Console.SetCursorPosition(0, _height + 1);
        Console.Write("WASD/Arrows: Move | Collect 10 tokens to WIN! | ESC: Quit".PadRight(_width));
    }

    public void RenderGameOver(Player player, int enemyCount, int wavesCompleted, bool won)
    {
        Console.Clear();
        Console.WriteLine();
        if (won)
        {
            Console.WriteLine("  ╔════════════════════════════════╗");
            Console.WriteLine("  ║         VICTORY!!!             ║");
            Console.WriteLine("  ╚════════════════════════════════╝");
        }
        else
        {
            Console.WriteLine("  ╔════════════════════════════════╗");
            Console.WriteLine("  ║         GAME OVER!             ║");
            Console.WriteLine("  ╚════════════════════════════════╝");
        }
        Console.WriteLine();
        Console.WriteLine($"  Tokens Collected: {player.TokensCollected}/{player.GetTokensNeeded()}");
        Console.WriteLine($"  Waves Survived: {wavesCompleted}");
        Console.WriteLine($"  Total Moves: {player.MoveCount}");
        Console.WriteLine();
        Console.WriteLine("  Press any key to exit...");
        Console.ReadKey(true);
    }

    private bool IsInBounds(Position pos)
    {
        return pos.X >= 0 && pos.X < _width && pos.Y >= 0 && pos.Y < _height;
    }

    public void Initialize()
    {
        Console.Clear();
    }
}

/// <summary>
/// Orchestrates the game loop and coordinates all game components.
/// Single Responsibility: Game state management and main loop coordination.
/// </summary>
public class Game
{
    private readonly Player _player;
    private readonly EnemyManager _enemyManager;
    private readonly ObstacleManager _obstacleManager;
    private readonly TokenManager _tokenManager;
    private readonly InputHandler _inputHandler;
    private readonly GameRenderer _renderer;

    private readonly int _worldWidth;
    private readonly int _worldHeight;

    public Game(int width, int height)
    {
        _worldWidth = width;
        _worldHeight = height;

        _player = new Player(width / 2, height / 2);
        _enemyManager = new EnemyManager(width, height);
        _obstacleManager = new ObstacleManager(width, height);
        _tokenManager = new TokenManager(width, height);
        _inputHandler = new InputHandler(width, height);
        _renderer = new GameRenderer(width, height);

        _enemyManager.SpawnInitialEnemies();
        _obstacleManager.SpawnObstacles(_player.Position, _enemyManager.GetEnemyPositions());
        _tokenManager.SpawnTokens(_player.Position, _enemyManager.GetEnemyPositions(), _obstacleManager.Obstacles.ToList());
    }

    public void Run()
    {
        Console.CursorVisible = true;
        _renderer.Initialize();

        try
        {
            _renderer.Render(_player, _enemyManager, _obstacleManager, _tokenManager);

            while (_player.IsAlive && !_player.HasWon())
            {
                var nextPosition = _inputHandler.GetNextPosition(_player.Position, _obstacleManager);

                if (nextPosition.HasValue)
                {
                    if (nextPosition.Value.X == -1 && nextPosition.Value.Y == -1)
                    {
                        _player.Kill();
                        break;
                    }

                    _player.MoveTo(nextPosition.Value);

                    if (_tokenManager.TryCollectToken(_player.Position))
                    {
                        _player.CollectToken();
                    }

                    if (_player.HasWon())
                    {
                        break;
                    }

                    _enemyManager.UpdateVision(_player.Position, _obstacleManager.Obstacles.ToList());

                    if (_enemyManager.HasCaughtPlayer(_player.Position))
                    {
                        _player.Kill();
                        break;
                    }

                    _enemyManager.MoveEnemies(_player.Position, _obstacleManager.Obstacles.ToList());

                    if (_enemyManager.HasCaughtPlayer(_player.Position))
                    {
                        _player.Kill();
                        break;
                    }

                    _enemyManager.CheckAndSpawnWave(_player.MoveCount);
                    _renderer.Render(_player, _enemyManager, _obstacleManager, _tokenManager);
                }
            }

            _renderer.RenderGameOver(_player, _enemyManager.Enemies.Count, _enemyManager.GetCurrentWave(), _player.HasWon());
        }
        finally
        {
            Console.CursorVisible = true;
        }
    }
}