/// <summary>
/// Bouncing ball simulation program demonstrating physics-based animation in console.
/// </summary>
public class Program
{
    private const int SceneRows = 40;
    private const int SceneColumns = 60;
    private const int NumberOfBalls = 10;

    public static void Main(string[] args)
    {
        // Set console window size to fit the scene (only on Windows)
#if WINDOWS
        try
        {
            Console.SetWindowSize(SceneColumns + 1, SceneRows + 2);
            Console.SetBufferSize(SceneColumns + 1, SceneRows + 2);
        }
        catch (Exception)
        {
            // Ignore if console size cannot be set (e.g., in some terminals)
        }
#endif

        var simulation = new BallSimulation(SceneRows, SceneColumns, NumberOfBalls);
        simulation.Run();
    }
}

/// <summary>
/// Represents a ball with position, velocity, and physical properties.
/// Single Responsibility: Manages ball state and physics calculations.
/// </summary>
public class Ball
{
    public double X { get; set; }
    public double Y { get; set; }
    public double VelocityX { get; set; }
    public double VelocityY { get; set; }
    public double Radius { get; }

    // Realistic physics constants
    private const double Gravity = 0.8; // Increased gravity for faster settling
    private const double WallBounceDamping = 0.7; // 30% energy loss on wall collision
    private const double BallBounceDamping = 0.85; // 15% energy loss on ball-to-ball collision
    private const double AirResistance = 0.98; // 2% velocity loss per frame due to air resistance
    private const double GroundFriction = 0.95; // Ground friction when rolling
    private const double MinimumVelocity = 0.05; // Below this, velocity is set to zero

    public Ball(double x, double y, double radius)
    {
        X = x;
        Y = y;
        Radius = radius;
        VelocityX = 0.0;
        VelocityY = 0.0;
    }

    /// <summary>
    /// Applies gravity and air resistance to the ball's velocity.
    /// </summary>
    public void ApplyPhysics()
    {
        // Apply gravity
        VelocityY += Gravity;

        // Apply air resistance to both velocities
        VelocityX *= AirResistance;
        VelocityY *= AirResistance;

        // Stop very slow movements to prevent jittering
        if (Math.Abs(VelocityX) < MinimumVelocity)
            VelocityX = 0;
        if (Math.Abs(VelocityY) < MinimumVelocity)
            VelocityY = 0;
    }

    /// <summary>
    /// Updates the ball's position based on its current velocity.
    /// </summary>
    public void UpdatePosition()
    {
        X += VelocityX;
        Y += VelocityY;
    }

    /// <summary>
    /// Handles collision with a horizontal boundary (top or bottom).
    /// </summary>
    public void BounceVertical(bool isGround = false)
    {
        VelocityY = -VelocityY * WallBounceDamping;

        // Apply ground friction if bouncing on ground
        if (isGround && Math.Abs(VelocityY) < 2.0)
        {
            VelocityX *= GroundFriction;
        }
    }

    /// <summary>
    /// Handles collision with a vertical boundary (left or right).
    /// </summary>
    public void BounceHorizontal()
    {
        VelocityX = -VelocityX * WallBounceDamping;
    }

    /// <summary>
    /// Checks if the ball has settled (stopped moving significantly).
    /// </summary>
    public bool HasSettled(int sceneHeight)
    {
        // Ball is settled if it's on the ground with minimal velocity
        bool onGround = Y + Radius >= sceneHeight - 2;
        bool notMoving = Math.Abs(VelocityY) < 0.15 && Math.Abs(VelocityX) < 0.15;

        return onGround && notMoving;
    }

    /// <summary>
    /// Calculates the distance between this ball and another ball.
    /// </summary>
    public double DistanceTo(Ball other)
    {
        double dx = X - other.X;
        double dy = Y - other.Y;
        return Math.Sqrt(dx * dx + dy * dy);
    }

    /// <summary>
    /// Handles collision with another ball using realistic elastic collision physics.
    /// </summary>
    public void CollideWith(Ball other)
    {
        // Calculate distance between centers
        double dx = other.X - X;
        double dy = other.Y - Y;
        double distance = Math.Sqrt(dx * dx + dy * dy);

        // Prevent division by zero
        if (distance == 0) return;

        // Calculate collision normal (unit vector)
        double nx = dx / distance;
        double ny = dy / distance;

        // Calculate relative velocity
        double dvx = VelocityX - other.VelocityX;
        double dvy = VelocityY - other.VelocityY;

        // Calculate relative velocity in collision normal direction
        double velocityAlongNormal = dvx * nx + dvy * ny;

        // Do not resolve if velocities are separating
        if (velocityAlongNormal > 0) return;

        // Apply realistic coefficient of restitution (energy loss in collision)
        double restitution = BallBounceDamping;
        double impulse = -(1 + restitution) * velocityAlongNormal / 2; // Divide by 2 for equal mass

        // Update velocities with damping
        VelocityX += impulse * nx;
        VelocityY += impulse * ny;
        other.VelocityX -= impulse * nx;
        other.VelocityY -= impulse * ny;

        // Separate overlapping balls to prevent sticking
        double overlap = (Radius + other.Radius) - distance;
        if (overlap > 0)
        {
            double separationX = nx * overlap * 0.5;
            double separationY = ny * overlap * 0.5;

            X -= separationX;
            Y -= separationY;
            other.X += separationX;
            other.Y += separationY;
        }
    }
}

/// <summary>
/// Handles collision detection between balls and scene boundaries.
/// Single Responsibility: Manages physics interactions with boundaries and between balls.
/// </summary>
public class CollisionDetector
{
    private readonly int _sceneWidth;
    private readonly int _sceneHeight;

    public CollisionDetector(int sceneWidth, int sceneHeight)
    {
        _sceneWidth = sceneWidth;
        _sceneHeight = sceneHeight;
    }

    /// <summary>
    /// Detects and handles all collisions with scene boundaries for all balls.
    /// </summary>
    public void HandleBoundaryCollisions(List<Ball> balls)
    {
        foreach (var ball in balls)
        {
            HandleBoundaryCollision(ball);
        }
    }

    /// <summary>
    /// Detects and handles collisions between all balls.
    /// </summary>
    public void HandleBallCollisions(List<Ball> balls)
    {
        // Check each pair of balls only once
        for (int i = 0; i < balls.Count; i++)
        {
            for (int j = i + 1; j < balls.Count; j++)
            {
                Ball ball1 = balls[i];
                Ball ball2 = balls[j];

                // Check if balls are colliding
                double distance = ball1.DistanceTo(ball2);
                double minDistance = ball1.Radius + ball2.Radius;

                if (distance < minDistance)
                {
                    ball1.CollideWith(ball2);
                }
            }
        }
    }

    /// <summary>
    /// Handles collision with scene boundaries for a single ball.
    /// </summary>
    private void HandleBoundaryCollision(Ball ball)
    {
        HandleBottomCollision(ball);
        HandleTopCollision(ball);
        HandleLeftCollision(ball);
        HandleRightCollision(ball);
    }

    private void HandleBottomCollision(Ball ball)
    {
        if (ball.Y + ball.Radius >= _sceneHeight - 1)
        {
            ball.Y = _sceneHeight - 1 - ball.Radius;
            ball.BounceVertical(isGround: true);
        }
    }

    private void HandleTopCollision(Ball ball)
    {
        if (ball.Y - ball.Radius <= 1)
        {
            ball.Y = 1 + ball.Radius;
            ball.BounceVertical(isGround: false);
        }
    }

    private void HandleLeftCollision(Ball ball)
    {
        if (ball.X - ball.Radius <= 1)
        {
            ball.X = 1 + ball.Radius;
            ball.BounceHorizontal();
        }
    }

    private void HandleRightCollision(Ball ball)
    {
        if (ball.X + ball.Radius >= _sceneWidth - 1)
        {
            ball.X = _sceneWidth - 1 - ball.Radius;
            ball.BounceHorizontal();
        }
    }
}

/// <summary>
/// Renders the scene and multiple balls to the console.
/// Single Responsibility: Handles all rendering logic.
/// </summary>
public class SceneRenderer
{
    private readonly int _width;
    private readonly int _height;

    // Console characters are taller than wide, so we adjust horizontally
    private const double AspectRatioCorrection = 2.0;

    // Character layers for ball appearance
    private const char BorderChar = '*';
    private const char BallOuterChar = 'o';
    private const char BallMiddleChar = 'O';
    private const char BallCenterChar = '*';
    private const char EmptyChar = ' ';

    public SceneRenderer(int width, int height)
    {
        _width = width;
        _height = height;
    }

    /// <summary>
    /// Renders the complete scene including border and all balls.
    /// </summary>
    public void Render(List<Ball> balls)
    {
        for (int i = 0; i < _height; i++)
        {
            for (int j = 0; j < _width; j++)
            {
                char charToRender = GetCharacterAt(i, j, balls);
                Console.Write(charToRender);
            }
            Console.WriteLine();
        }
    }

    /// <summary>
    /// Determines which character should be rendered at the given position.
    /// </summary>
    private char GetCharacterAt(int row, int col, List<Ball> balls)
    {
        // Draw border
        if (IsBorderPosition(row, col))
        {
            return BorderChar;
        }

        // Draw balls or empty space (check all balls, render closest one)
        return GetBallCharacter(row, col, balls);
    }

    /// <summary>
    /// Checks if the position is on the scene border.
    /// </summary>
    private bool IsBorderPosition(int row, int col)
    {
        return col == 0 || col == _width - 1 || row == 0 || row == _height - 1;
    }

    /// <summary>
    /// Gets the appropriate character for balls at this position.
    /// Returns empty space if no ball is at this position.
    /// Renders the closest ball if multiple balls overlap.
    /// </summary>
    private char GetBallCharacter(int row, int col, List<Ball> balls)
    {
        Ball? closestBall = null;
        double closestDistance = double.MaxValue;

        // Find the closest ball to this position
        foreach (var ball in balls)
        {
            double distance = CalculateDistanceToBall(row, col, ball);

            if (distance <= ball.Radius && distance < closestDistance)
            {
                closestDistance = distance;
                closestBall = ball;
            }
        }

        // If a ball was found at this position, render it
        if (closestBall != null)
        {
            // Check if this is very close to the center
            if (closestDistance <= 0.5)
                return BallMiddleChar;

            return GetBallLayerCharacter(closestDistance, closestBall.Radius);
        }

        return EmptyChar;
    }

    /// <summary>
    /// Calculates the distance from a position to the ball's center.
    /// Applies aspect ratio correction for circular appearance.
    /// </summary>
    private double CalculateDistanceToBall(int row, int col, Ball ball)
    {
        double adjustedX = (col - ball.X) * AspectRatioCorrection;
        double adjustedY = (row - ball.Y);
        return Math.Sqrt(Math.Pow(adjustedX, 2) + Math.Pow(adjustedY, 2));
    }

    /// <summary>
    /// Determines which character layer to use based on distance from ball center.
    /// Creates a football-like pattern with multiple layers for spherical appearance.
    /// </summary>
    private char GetBallLayerCharacter(double distance, double radius)
    {
        // Calculate percentage of radius
        double percentageFromCenter = distance / radius;

        // Outer edge (85-100% of radius)
        if (percentageFromCenter >= 0.85)
            return BallOuterChar;

        // Middle-outer layer (60-85% of radius)
        if (percentageFromCenter >= 0.60)
            return BallCenterChar;

        // Middle-inner layer (30-60% of radius)
        if (percentageFromCenter >= 0.30)
            return BallMiddleChar;

        // Center core (0-30% of radius)
        return BallCenterChar;
    }
}

/// <summary>
/// Orchestrates the ball simulation, coordinating physics and rendering for multiple balls.
/// Single Responsibility: Manages the simulation loop and component coordination.
/// </summary>
public class BallSimulation
{
    private readonly List<Ball> _balls;
    private readonly CollisionDetector _collisionDetector;
    private readonly SceneRenderer _renderer;
    private readonly int _sceneWidth;
    private readonly int _sceneHeight;

    // Ball size constraints
    private const double MinBallRadius = 1.5;
    private const double MaxBallRadius = 3.5;

    private const int FrameDelayMs = 50; // Faster frame rate for smoother physics
    private const int MaxSimulationFrames = 2000; // More frames for realistic settling

    public BallSimulation(int sceneWidth, int sceneHeight, int numberOfBalls)
    {
        _sceneWidth = sceneWidth;
        _sceneHeight = sceneHeight;
        _balls = new List<Ball>();

        // Initialize multiple balls at random positions with random sizes
        InitializeBalls(numberOfBalls);

        // Initialize dependencies (Dependency Injection pattern)
        _collisionDetector = new CollisionDetector(sceneWidth, sceneHeight);
        _renderer = new SceneRenderer(sceneWidth, sceneHeight);
    }

    /// <summary>
    /// Initializes multiple balls with random positions and random sizes.
    /// Ensures balls spawn within scene boundaries and don't overlap initially.
    /// </summary>
    private void InitializeBalls(int numberOfBalls)
    {
        Random random = new Random();
        int maxAttempts = 100; // Maximum attempts to find non-overlapping position

        for (int i = 0; i < numberOfBalls; i++)
        {
            Ball? newBall = null;
            bool validPosition = false;
            int attempts = 0;

            while (!validPosition && attempts < maxAttempts)
            {
                // Random radius between min and max
                double radius = MinBallRadius + random.NextDouble() * (MaxBallRadius - MinBallRadius);

                // Random position within scene boundaries (accounting for radius)
                double x = radius + 1 + random.NextDouble() * (_sceneWidth - 2 * radius - 2);
                double y = radius + 1 + random.NextDouble() * (_sceneHeight / 2.0 - radius - 1); // Spawn in top half

                newBall = new Ball(x, y, radius);

                // Check if this position overlaps with existing balls
                validPosition = true;
                foreach (var existingBall in _balls)
                {
                    double distance = newBall.DistanceTo(existingBall);
                    double minDistance = newBall.Radius + existingBall.Radius + 1; // Add spacing

                    if (distance < minDistance)
                    {
                        validPosition = false;
                        break;
                    }
                }

                attempts++;
            }

            // Add the ball if a valid position was found
            if (validPosition && newBall != null)
            {
                // Give balls small random initial horizontal velocity
                newBall.VelocityX = (random.NextDouble() - 0.5) * 0.5;

                // Small initial downward velocity
                newBall.VelocityY = random.NextDouble() * 0.3;

                _balls.Add(newBall);
            }
        }
    }

    /// <summary>
    /// Runs the main simulation loop until all balls settle or max frames reached.
    /// </summary>
    public void Run()
    {
        Console.CursorVisible = false;

        try
        {
            int frameCount = 0;

            while (!AllBallsSettled() && frameCount < MaxSimulationFrames)
            {
                RenderFrame();
                UpdatePhysics();
                Thread.Sleep(FrameDelayMs);
                frameCount++;
            }

            // Render final frame
            RenderFrame();
        }
        finally
        {
            Console.CursorVisible = true;
        }
    }

    /// <summary>
    /// Checks if all balls have settled.
    /// </summary>
    private bool AllBallsSettled()
    {
        return _balls.All(ball => ball.HasSettled(_sceneHeight));
    }

    /// <summary>
    /// Clears the console and renders the current frame.
    /// </summary>
    private void RenderFrame()
    {
        Console.Clear();
        _renderer.Render(_balls);
    }

    /// <summary>
    /// Updates physics for one frame: gravity, air resistance, position, and collisions for all balls.
    /// </summary>
    private void UpdatePhysics()
    {
        // Apply physics (gravity, air resistance) and update positions
        foreach (var ball in _balls)
        {
            ball.ApplyPhysics();
            ball.UpdatePosition();
        }

        // Handle collisions (boundaries first, then ball-to-ball)
        _collisionDetector.HandleBoundaryCollisions(_balls);
        _collisionDetector.HandleBallCollisions(_balls);
    }
}