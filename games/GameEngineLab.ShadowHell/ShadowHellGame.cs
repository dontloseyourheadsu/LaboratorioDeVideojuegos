using GameEngineLab.Core.Features.Ecs.Entities;
using GameEngineLab.Core.Features.Ecs.Resources;
using GameEngineLab.Core.Features.Ecs.Systems;
using GameEngineLab.Core.Features.Maps.Resources;
using GameEngineLab.Core.Features.Physics.Components;
using GameEngineLab.Core.Features.Physics.Systems;
using GameEngineLab.Core.Features.Rendering.Components;
using GameEngineLab.Core.Features.Rendering.Resources;
using GameEngineLab.Core.Features.Rendering.Systems;
using GameEngineLab.Core.Features.Animation.Systems;
using GameEngineLab.ShadowHell.Features.Player.Entities;
using GameEngineLab.ShadowHell.Features.Player.Components;
using GameEngineLab.ShadowHell.Features.Player.Systems;
using GameEngineLab.ShadowHell.Features.Environment.Systems;
using GameEngineLab.ShadowHell.Features.Environment.Components;
using GameEngineLab.ShadowHell.Features.Enemy.Systems;
using GameEngineLab.ShadowHell.Features.Enemy.Components;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;

using GameEngineLab.ShadowHell.Features.Environment.Resources;

namespace GameEngineLab.ShadowHell;

public sealed class ShadowHellGame : Game
{
    private readonly GraphicsDeviceManager _graphics;
    private readonly World _world = new();
    private readonly SystemScheduler _scheduler = new();

    private SpriteBatch? _spriteBatch;
    private Texture2D? _debugPixel;
    private SpriteFont? _font;
    
    private KeyboardState _previousKeyboard;
    private MouseState _previousMouse;
    
    private EntityId _playerEntity;

    private const int WindowWidth = 1024;
    private const int WindowHeight = 768;
    private const float WorldWidth = 2048f;
    private const float WorldHeight = 1536f;

    public ShadowHellGame()
    {
        _graphics = new GraphicsDeviceManager(this);
        Content.RootDirectory = "Content";
        IsMouseVisible = true;
        Window.Title = "ShadowHell - Android Prototype Testbed";
        
        _graphics.PreferredBackBufferWidth = WindowWidth;
        _graphics.PreferredBackBufferHeight = WindowHeight;
    }

    protected override void Initialize()
    {
        _world.SetResource(new MapBoundsResource
        {
            PlayArea = new Rectangle(64, 64, (int)WorldWidth - 128, (int)WorldHeight - 128)
        });

        _world.SetResource(new CameraResource
        {
            Position = new Vector2(WorldWidth / 2f, WorldHeight / 2f),
            Zoom = 1.0f
        });

        // 2. Setup ECS Schedulers
        // Floor Renderer (runs first under everything)
        _scheduler.AddSystem(new FloorRendererSystem());

        // Textured Wall Renderer
        _scheduler.AddSystem(new WallRendererSystem());

        // Shadow Projection and Color Lighting System
        _scheduler.AddSystem(new ShadowSystem());

        // Player input and movement translations
        _scheduler.AddSystem(new PlayerInputSystem());
        _scheduler.AddSystem(new PlayerAttackSystem());
        _scheduler.AddSystem(new PlayerRendererSystem());

        // Physics Sub-stepping (Movement & Collision physics)
        var physicsStepper = new PhysicsStepperSystem(order: 10, substeps: 8);
        physicsStepper.AddSystem(new MovementSystem());
        physicsStepper.AddSystem(new CollisionSystem());
        physicsStepper.AddSystem(new BoundarySystem());
        physicsStepper.AddSystem(new PhysicsFrictionSystem());
        _scheduler.AddSystem(physicsStepper);

        // Core Skeletal Solver and Render System (kept for core compatibility)
        _scheduler.AddSystem(new SkeletonSystem());

        // Shape renderer for solid walls/caverns/pillars (pure black silhouette foreground)
        _scheduler.AddSystem(new ShapeRenderSystem());

        // Enemy steering AI and crawl rendering
        _scheduler.AddSystem(new EnemySystem());

        // Bullet project updates and rendering
        _scheduler.AddSystem(new BulletSystem());

        // 3. Generate Level (Cavern boundary & rocky pillars)
        GenerateCavernLevel();

        base.Initialize();
    }

    private void GenerateCavernLevel()
    {
        // Place Player in the center of the world map
        _playerEntity = PlayerFactory.CreatePlayer(_world, new Vector2(WorldWidth / 2f, WorldHeight / 2f));

        var random = new Random();

        // Create 4 boundary walls as simple rectangles of the color of the darker tile
        Color wallColor = new Color(42, 28, 48);
        float wallThickness = 64f;

        // Top Wall
        var topWall = _world.CreateEntity();
        _world.SetComponent(topWall, new TransformComponent { Position = new Vector2(WorldWidth / 2f, wallThickness / 2f) });
        _world.SetComponent(topWall, new DrawColorComponent(wallColor));
        _world.SetComponent(topWall, new HiddenComponent());
        _world.SetComponent(topWall, new RigidBodyComponent
        {
            Shape = RigidBodyShape.Rectangle,
            Size = new Vector2(WorldWidth, wallThickness),
            Mass = 0f, // Static
            CollisionGroup = 1 // Cavern boundaries
        });

        // Bottom Wall
        var botWall = _world.CreateEntity();
        _world.SetComponent(botWall, new TransformComponent { Position = new Vector2(WorldWidth / 2f, WorldHeight - wallThickness / 2f) });
        _world.SetComponent(botWall, new DrawColorComponent(wallColor));
        _world.SetComponent(botWall, new HiddenComponent());
        _world.SetComponent(botWall, new RigidBodyComponent
        {
            Shape = RigidBodyShape.Rectangle,
            Size = new Vector2(WorldWidth, wallThickness),
            Mass = 0f,
            CollisionGroup = 1
        });

        // Left Wall
        var leftWall = _world.CreateEntity();
        _world.SetComponent(leftWall, new TransformComponent { Position = new Vector2(wallThickness / 2f, WorldHeight / 2f) });
        _world.SetComponent(leftWall, new DrawColorComponent(wallColor));
        _world.SetComponent(leftWall, new HiddenComponent());
        _world.SetComponent(leftWall, new RigidBodyComponent
        {
            Shape = RigidBodyShape.Rectangle,
            Size = new Vector2(wallThickness, WorldHeight),
            Mass = 0f,
            CollisionGroup = 1
        });

        // Right Wall
        var rightWall = _world.CreateEntity();
        _world.SetComponent(rightWall, new TransformComponent { Position = new Vector2(WorldWidth - wallThickness / 2f, WorldHeight / 2f) });
        _world.SetComponent(rightWall, new DrawColorComponent(wallColor));
        _world.SetComponent(rightWall, new HiddenComponent());
        _world.SetComponent(rightWall, new RigidBodyComponent
        {
            Shape = RigidBodyShape.Rectangle,
            Size = new Vector2(wallThickness, WorldHeight),
            Mass = 0f,
            CollisionGroup = 1
        });

        // Spawn shadow enemies (4 Melee, 3 Ranged)
        for (int i = 0; i < 7; i++)
        {
            Vector2 position;
            bool ok;
            int attempts = 0;
            do
            {
                position = new Vector2(random.Next(150, (int)WorldWidth - 150), random.Next(150, (int)WorldHeight - 150));
                ok = Vector2.Distance(position, new Vector2(WorldWidth / 2f, WorldHeight / 2f)) > 300f;
                attempts++;
            } while (!ok && attempts < 100);

            EnemyType type = (i < 4) ? EnemyType.Melee : EnemyType.Ranged;
            float speed = (type == EnemyType.Melee) ? 65f : 80f;

            var enemy = _world.CreateEntity();
            _world.SetComponent(enemy, new EnemyComponent(type, speed));
            _world.SetComponent(enemy, new TransformComponent { Position = position });
            _world.SetComponent(enemy, new VelocityComponent { Value = Vector2.Zero });
            _world.SetComponent(enemy, new DrawColorComponent(Color.Black));
            _world.SetComponent(enemy, new RigidBodyComponent
            {
                Shape = RigidBodyShape.Circle,
                BoundingRadius = 20f,
                Mass = 1.2f,
                Restitution = 0.3f,
                Friction = 0.9f,
                CollisionGroup = 2, // Shadow enemies (casts shadows!)
                CollisionMask = 1 | 4 // collides with walls and player
            });
        }

    }

    protected override void LoadContent()
    {
        _spriteBatch = new SpriteBatch(GraphicsDevice);
        
        // Single pixel white texture used by shape drawers
        _debugPixel = new Texture2D(GraphicsDevice, 1, 1);
        _debugPixel.SetData(new[] { Color.White });

        // Load built-in font from Content
        _font = Content.Load<SpriteFont>("Fonts/Silkscreen");

        // Generate procedural textures for earth terrain background, walls, and lights
        Color floorBase = new Color(38, 24, 20); // earth tile base
        Color floorDetail = new Color(54, 38, 32); // earth tile highlights
        Color wallBase = new Color(32, 18, 14); // wall base
        Color wallDetail = new Color(42, 24, 20); // wall highlights (Netherrack rocky texture)

        var floorTex = NoiseTextureGenerator.GenerateFloorTexture(GraphicsDevice, 128);
        var wallTex = NoiseTextureGenerator.GenerateWallTexture(GraphicsDevice, 128);
        var lightTex = NoiseTextureGenerator.GenerateLightTexture(GraphicsDevice, 128);

        _world.SetResource(new GameTextureResource
        {
            FloorTexture = floorTex,
            WallTexture = wallTex,
            LightTexture = lightTex
        });
    }

    protected override void Update(GameTime gameTime)
    {
        var currentKeyboard = Keyboard.GetState();
        var currentMouse = Mouse.GetState();

        var frameContext = new FrameContext(
            gameTime,
            currentKeyboard,
            _previousKeyboard,
            currentMouse,
            _previousMouse,
            GraphicsDevice.Viewport,
            _spriteBatch,
            _debugPixel);

        // Update systems
        _scheduler.Update(_world, frameContext);

        // Camera follow player smoothly
        if (_world.TryGetResource<CameraResource>(out var camera) && camera != null)
        {
            if (_world.TryGetComponent<TransformComponent>(_playerEntity, out var playerTransform))
            {
                // Smooth interpolation to center player in screen
                Vector2 targetCamPos = playerTransform.Position;
                // Clamp camera within world bounds
                targetCamPos.X = MathHelper.Clamp(targetCamPos.X, WindowWidth / 2f, WorldWidth - WindowWidth / 2f);
                targetCamPos.Y = MathHelper.Clamp(targetCamPos.Y, WindowHeight / 2f, WorldHeight - WindowHeight / 2f);

                camera.Position = Vector2.Lerp(camera.Position, targetCamPos, 0.1f);
            }
        }

        _previousKeyboard = currentKeyboard;
        _previousMouse = currentMouse;

        base.Update(gameTime);
    }

    protected override void Draw(GameTime gameTime)
    {
        // Dark black/purple theme background clear color
        GraphicsDevice.Clear(new Color(10, 8, 15));

        var frameContext = new FrameContext(
            gameTime,
            Keyboard.GetState(),
            _previousKeyboard,
            Mouse.GetState(),
            _previousMouse,
            GraphicsDevice.Viewport,
            _spriteBatch,
            _debugPixel);

        // Render World Space objects using camera matrix
        if (_world.TryGetResource<CameraResource>(out var camera) && camera != null && _spriteBatch != null)
        {
            // Begin with camera view matrix
            _spriteBatch.Begin(
                SpriteSortMode.Deferred, 
                BlendState.AlphaBlend, 
                SamplerState.PointClamp, 
                null, 
                null, 
                null, 
                camera.GetViewMatrix(GraphicsDevice.Viewport));
            
            _scheduler.Draw(_world, frameContext);
            
            _spriteBatch.End();
        }

        // Render UI / HUD Overlay (Screen Space)
        if (_spriteBatch != null && _debugPixel != null)
        {
            _spriteBatch.Begin();

            // Fetch Player Health to display in HUD
            float playerHealth = 2f;
            float maxHealth = 2f;
            foreach (var entity in _world.GetEntitiesWith<PlayerComponent>())
            {
                if (_world.TryGetComponent<PlayerComponent>(entity, out var p))
                {
                    playerHealth = p.Health;
                    maxHealth = p.MaxHealth;
                }
                break;
            }

            // Draw player life icons in the top-right (mini player circles with purple/violet glowing borders)
            int startX = WindowWidth - 50;
            int startY = 40;
            Color glowColor = new Color(177, 0, 255); // Neon Purple
            Color innerGlowColor = new Color(220, 120, 255); // Light Violet
            Color shadowBlack = new Color(10, 8, 15); // Deep shadow black core

            for (int i = 0; i < (int)maxHealth; i++)
            {
                Vector2 iconPos = new Vector2(startX - i * 35, startY);
                float checkHealth = i + 1.0f;
                
                if (playerHealth >= checkHealth)
                {
                    // Full Heart: fully visible purple outline
                    ShapeRenderer.DrawCircleOutline(_spriteBatch, _debugPixel, iconPos, 11f, glowColor * 0.6f, 2);
                    ShapeRenderer.DrawCircleOutline(_spriteBatch, _debugPixel, iconPos, 10f, innerGlowColor, 2);
                    ShapeRenderer.DrawCircle(_spriteBatch, _debugPixel, iconPos, 10f, shadowBlack);
                }
                else if (playerHealth >= checkHealth - 0.5f)
                {
                    // Half Heart: partially transparent/faded purple outline
                    ShapeRenderer.DrawCircleOutline(_spriteBatch, _debugPixel, iconPos, 11f, glowColor * 0.25f, 2);
                    ShapeRenderer.DrawCircleOutline(_spriteBatch, _debugPixel, iconPos, 10f, innerGlowColor * 0.4f, 2);
                    ShapeRenderer.DrawCircle(_spriteBatch, _debugPixel, iconPos, 10f, shadowBlack);
                }
                else
                {
                    // Empty Container: dark gray faded outline
                    ShapeRenderer.DrawCircleOutline(_spriteBatch, _debugPixel, iconPos, 10f, Color.DarkGray * 0.25f, 2);
                }
            }

            // Draw HUD Title
            if (_font != null)
            {
                // Title shadow
                _spriteBatch.DrawString(_font, "SHADOWHELL", new Vector2(32, 22), new Color(80, 0, 120) * 0.5f, 0f, Vector2.Zero, 2.0f, SpriteEffects.None, 0f);
                _spriteBatch.DrawString(_font, "SHADOWHELL", new Vector2(30, 20), new Color(177, 0, 255), 0f, Vector2.Zero, 2.0f, SpriteEffects.None, 0f);

                // Subtitle
                _spriteBatch.DrawString(_font, "Android Roguelike Techbed - testing build", new Vector2(30, 60), Color.Gray, 0f, Vector2.Zero, 0.8f, SpriteEffects.None, 0f);

                // Instructions
                string controls = "CONTROLS:\nWASD / Arrows - Move\nSpace / Left Shift - Dodge Roll (Smooth Hover)\nF / E / Left Click - Close Range Melee Attack";
                _spriteBatch.DrawString(_font, controls, new Vector2(30, WindowHeight - 120), Color.LightGray * 0.8f, 0f, Vector2.Zero, 0.8f, SpriteEffects.None, 0f);
            }

            _spriteBatch.End();
        }

        base.Draw(gameTime);
    }
}
