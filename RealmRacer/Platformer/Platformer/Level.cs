using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Audio;
using System.IO;
using Microsoft.Xna.Framework.Input.Touch;
using Microsoft.Xna.Framework.Input;

namespace RealmRacer
{
    /// <summary>
    /// A uniform grid of tiles with collections of gems and enemies.
    /// The level owns the player and controls the game's win and lose
    /// conditions as well as scoring.
    /// </summary>
    class Level : IDisposable
    {
        #region Variables

        // Physical structure of the level.
        public Tile[,] tiles;
        private Layer[] layers;
        // The layer which entities are drawn on top of.
        private const int EntityLayer = 0;

        // Entities in the level.
        public Player Player
        {
            get { return player; }
        }
        Player player;

        private List<Fragment> fragments = new List<Fragment>();
        private Gem completedGem;
        private List<Enemy> enemies = new List<Enemy>();
        private List<Hazard> hazards = new List<Hazard>();
        private List<Geyser> geysers = new List<Geyser>();
        private List<ProjectileShooter> projectileShooters = new List<ProjectileShooter>();

        public List<TileTimer> tileTimers = new List<TileTimer>();

        // Key locations in the level.        
        private Vector2 start;
        private Point exit = InvalidPosition;
        private static readonly Point InvalidPosition = new Point(-1, -1);

        // Level game state.
        private Random random; // Arbitrary, but constant seed
        public Vector2 cameraPosition;

        public int Score
        {
            get { return score; }
            set { score = value; }
        }
        int score;

        public bool ReachedExit
        {
            get { return reachedExit; }
        }
        bool reachedExit;

        public TimeSpan Time
        {
            get { return time; }
        }
        TimeSpan time;

        public bool levelComplete;

        private const int PointsFromTime = 10000;

        // Level content.        
        public ContentManager Content
        {
            get { return content; }
        }
        ContentManager content;

        public int GemsCollected
        {
            get { return gemsCollected; }
        }
        int gemsCollected;

        public int NumGems
        {
            get { return numGems; }
        }
        public int numGems;

#endregion

        #region Loading

        /// <summary>
        /// Constructs a new level.
        /// </summary>
        /// <param name="serviceProvider">
        /// The service provider that will be used to construct a ContentManager.
        /// </param>
        /// <param name="fileStream">
        /// A stream containing the tile data.
        /// </param>
        public Level(IServiceProvider serviceProvider, Stream fileStream, int levelIndex, int score, int numLevels)
        {
            // Create a new content manager to load content used just by this level.
            content = new ContentManager(serviceProvider, "Content");

            random = new Random(levelIndex * levelIndex + score + 3485992);

            time = TimeSpan.FromMinutes(0.0);
            levelComplete = false;
            LoadTiles(fileStream, levelIndex, numLevels);
            gemsCollected = fragments.Count;
            numGems = fragments.Count;

            this.score = score;

            // Load background layer textures. For now, all levels must
            // use the same backgrounds and only use the left-most part of them.
            layers = new Layer[3];
            //Earth
            if ((((float)levelIndex + 1) / (float)numLevels) <= 0.25f)
            {
                layers[0] = new Layer(Content, "Backgrounds/earthBkg0");
            }
            else
                if ((((float)levelIndex + 1) / (float)numLevels) <= 0.5f && (((float)levelIndex + 1) / (float)numLevels) > 0.25f) //Wind
                {
                    layers[0] = new Layer(Content, "Backgrounds/windBkg0");
                }
                else
                    if ((((float)levelIndex + 1) / (float)numLevels) <= 0.75f && (((float)levelIndex + 1) / (float)numLevels) > 0.5f) //Water
                    {
                        layers[0] = new Layer(Content, "Backgrounds/waterBkg0");
                    }
                    else
                        if ((((float)levelIndex + 1) / (float)numLevels) > 0.75f) //Fire
                        {
                            layers[0] = new Layer(Content, "Backgrounds/fireBkg0");
                        }
                        else //Default
                        {
                            layers[0] = new Layer(Content, "Backgrounds/earthBkg0");
                        }
        }

        /// <summary>
        /// Iterates over every tile in the structure file and loads its
        /// appearance and behavior. This method also validates that the
        /// file is well-formed with a player start point, exit, etc.
        /// </summary>
        /// <param name="fileStream">
        /// A stream containing the tile data.
        /// </param>
        private void LoadTiles(Stream fileStream, int levelIndex, int numLevels)
        {
            // Load the level and ensure all of the lines are the same length.
            int width;
            List<string> lines = new List<string>();
            using (StreamReader reader = new StreamReader(fileStream))
            {
                string line = reader.ReadLine();
                width = line.Length;
                while (line != null)
                {
                    lines.Add(line);
                    if (line.Length != width)
                        throw new Exception(String.Format("The length of line {0} is different from all preceeding lines.", lines.Count));
                    line = reader.ReadLine();
                }
            }

            // Allocate the tile grid.
            tiles = new Tile[width, lines.Count];

            // Loop over every tile position,
            for (int y = 0; y < Height; ++y)
            {
                for (int x = 0; x < Width; ++x)
                {
                    // to load each tile.
                    char tileType = lines[y][x];
                    tiles[x, y] = LoadTile(tileType, x, y, levelIndex, numLevels);
                }
            }

            // Verify that the level has a beginning and an end.
            if (Player == null)
                throw new NotSupportedException("A level must have a starting point.");
            if (exit == InvalidPosition)
                throw new NotSupportedException("A level must have an exit.");

        }

        /// <summary>
        /// Loads an individual tile's appearance and behavior.
        /// </summary>
        /// <param name="tileType">
        /// The character loaded from the structure file which
        /// indicates what should be loaded.
        /// </param>
        /// <param name="x">
        /// The X location of this tile in tile space.
        /// </param>
        /// <param name="y">
        /// The Y location of this tile in tile space.
        /// </param>
        /// <returns>The loaded tile.</returns>
        private Tile LoadTile(char tileType, int x, int y, int levelIndex, int numLevels)
        {
            switch (tileType)
            {
                #region RequiredTiles
                // Blank space
                case '.':
                    return new Tile(null, TileCollision.Passable);

                // Exit
                case 'X':
                    return LoadExitTile(x, y);

                // Player 1 start point
                case '1':
                    return LoadStartTile(x, y);

                #endregion

                #region Gems
                // Gem
                case 'F':
                    return LoadFragmentTile(null, TileCollision.Passable, x, y);

                //Gem in quicksand
                case 'f':
                    return LoadFragmentTile("Earth/quicksand0", TileCollision.Impeding, x, y);

                //Gem in water
                case 'g':
                    return LoadFragmentTile("Water/underwater0", TileCollision.Impeding, x, y);

                #endregion

                #region Tiles

                //Water tile
                case 'U':
                    return LoadImpedingTile("Water/underwater0", TileCollision.Impeding);

                //Quicksand
                case 'Q':
                    return LoadImpedingTile("Earth/quicksand0", TileCollision.Impeding);

                //Clouds
                case 'C':
                    return LoadUnstableTile("Wind/windCloud", TileCollision.Unstable, true);

                //Ice
                case 'S':
                    return LoadTile("Water/ice", TileCollision.Slippery);

                // Floating platform
                case '-':
                    {
                        //Earth
                        if ((((float)levelIndex + 1) / (float)numLevels) <= 0.25f)
                        {
                            return LoadVarietyTile("Earth/earthPlatform", 2, TileCollision.Platform);
                        }
                        else
                            if ((((float)levelIndex + 1) / (float)numLevels) <= 0.5f && (((float)levelIndex + 1) / (float)numLevels) > 0.25f) //Wind
                            {
                                return LoadVarietyTile("Wind/windPlatform", 2, TileCollision.Platform);
                            }
                            else
                                if ((((float)levelIndex + 1) / (float)numLevels) <= 0.75f && (((float)levelIndex + 1) / (float)numLevels) > 0.5f) //Water
                                {
                                    return LoadVarietyTile("Water/waterPlatform", 2, TileCollision.Platform);
                                }
                                else
                                    if ((((float)levelIndex + 1) / (float)numLevels) > 0.75f) //Fire
                                    {
                                        return LoadVarietyTile("Fire/firePlatform", 2, TileCollision.Platform);
                                    }
                                    else //Default
                                    {
                                        return LoadVarietyTile("Earth/earthPlatform", 2, TileCollision.Platform);
                                    }
                    }

                // Impassable block
                case '#':
                    {
                        //Earth
                        if ((((float)levelIndex + 1) / (float)numLevels) <= 0.25f)
                        {
                            return LoadVarietyTile("Earth/earthBlock", 5, TileCollision.Impassable);
                        }
                        else
                            if ((((float)levelIndex + 1) / (float)numLevels) <= 0.5f && (((float)levelIndex + 1) / (float)numLevels) > 0.25f) //Wind
                            {
                                return LoadVarietyTile("Wind/windBlock", 5, TileCollision.Impassable);
                            }
                            else
                                if ((((float)levelIndex + 1) / (float)numLevels) <= 0.75f && (((float)levelIndex + 1) / (float)numLevels) > 0.5f) //Water
                                {
                                    return LoadVarietyTile("Water/waterBlock", 5, TileCollision.Impassable);
                                }
                                else
                                    if ((((float)levelIndex + 1) / (float)numLevels) > 0.75f) //Fire
                                    {
                                        return LoadVarietyTile("Fire/fireBlock", 5, TileCollision.Impassable);
                                    }
                                    else //Default
                                    {
                                        return LoadVarietyTile("Earth/earthBlock", 5, TileCollision.Impassable);
                                    }
                    }

                #endregion

                #region Hazards

                //Stalagtite
                case 'V':
                    return LoadHazard(null, "Sprites/Hazards/Earth/stalagtite", 40, 32,  TileCollision.Passable, 0, 5, x, y);

                //Stalagtite in quicksand
                case 'v':
                    return LoadHazard("Tiles/Earth/quicksand", "Sprites/Hazards/Earth/stalagtite", 40, 32, TileCollision.Impeding, 0, 5, x, y);

                //Ice stalagtite
                case 'I':
                    return LoadHazard(null, "Sprites/Hazards/Water/icicleDown", 40, 32, TileCollision.Passable, 0, 5, x, y);
                   
                //Ice stalagmite
                case 'i':
                    return LoadHazard(null, "Sprites/Hazards/Water/icicleUp", 40, 32, TileCollision.Passable, 0, 5, x, y);

                //Ice stalactite underwater
                case 'O':
                    return LoadHazard("Tiles/Water/underwater", "Sprites/Hazards/Water/icicleDown", 40, 32, TileCollision.Impeding, 0, 5, x, y);

                //Ice stalagmite underwater
                case 'o':
                    return LoadHazard("Tiles/Water/underwater", "Sprites/Hazards/Water/icicleUp", 40, 32, TileCollision.Impeding, 0, 5, x, y);

                //Stalagmite
                case '^':
                    return LoadHazard(null, "Sprites/Hazards/Earth/stalagmite", 40, 32, TileCollision.Passable, 0, 5, x, y);

                //Stalagmite in quicksand
                case '6':
                    return LoadHazard("Tiles/Earth/quicksand", "Sprites/Hazards/Earth/stalagmite", 40, 32, TileCollision.Impeding, 0, 5, x, y);

                //Water Geyser Up
                case 'G':
                    return LoadGeyserTile(x, y, "Water/waterGeyserUp", GeyserType.Water, 2, 2, 0);

                //Fire projectile horizontal left
                case 'P':
                    return LoadProjectileTile(x, y, new Vector2(0, 0), new Vector2(-1, 0), new Vector2(500.0f, 500.0f), "Fire/fireball", 32, 32, false, 5.0f, TileCollision.Passable, null);
                
                //Fire projectile Right
                case 'p':
                    return LoadProjectileTile(x, y, new Vector2(0, 0), new Vector2(1, 0), new Vector2(500.0f, 500.0f), "Fire/fireball", 32, 32, false, 5.0f, TileCollision.Passable, null);

                //fire projectile Up
                case 'B':
                    return LoadProjectileTile(x, y, new Vector2(0, 0), new Vector2(0, -1), new Vector2(500.0f, 500.0f), "Fire/fireball", 32, 32, false, 5.0f, TileCollision.Passable, null);
                
                //Fire projectile Down
                case 'b':
                    return LoadProjectileTile(x, y, new Vector2(0, 0), new Vector2(0, 1), new Vector2(500.0f, 500.0f), "Fire/fireball", 32, 32, false, 5.0f, TileCollision.Passable, null);

                //Fire wall
                case 'W':
                    return LoadHazard(null, "Sprites/Hazards/Fire/fire", 40, 32, TileCollision.Passable, 0, 1, x, y);

                //Fire geyser
                case 'R':
                    return LoadGeyserTile(x, y, "Fire/fireGeyserUp", GeyserType.Fire, 2, 2, 0);


                //Vortex
                case 'T':
                    return LoadVortexTile("Wind/windVortex", x, y);

                //Wind jet
                case 'J':
                    return LoadGeyserTile(x, y, "Wind/windGeyserUp", GeyserType.Wind, 5, 10, 0);

                #endregion

                // Unknown tile type character
                default:
                    throw new NotSupportedException(String.Format("Unsupported tile type character '{0}' at position {1}, {2}.", tileType, x, y));
            }
        }

        /// <summary>
        /// Creates a new tile. The other tile loading methods typically chain to this
        /// method after performing their special logic.
        /// </summary>
        /// <param name="name">
        /// Path to a tile texture relative to the Content/Tiles directory.
        /// </param>
        /// <param name="collision">
        /// The tile collision type for the new tile.
        /// </param>
        /// <returns>The new tile.</returns>
        private Tile LoadTile(string name, TileCollision collision)
        {
            return new Tile(Content.Load<Texture2D>("Tiles/" + name), collision);
        }

        /// <summary>
        /// Loads a tile that impedes the player. Examples would be water or quicksand.
        /// </summary>
        private Tile LoadImpedingTile(string name, TileCollision collision)
        {
            return new Tile(Content.Load<Texture2D>("Tiles/" + name), collision);
        }

        /// <summary>
        /// Loads an unstable tile. Examples would be a cloud.
        /// </summary>
        private Tile LoadUnstableTile(string name, TileCollision collision, bool rebuildable)
        {
            return new Tile(Content.Load<Texture2D>("Tiles/" + name), collision, rebuildable);
        }

        /// <summary>
        /// Loads a tile with a random appearance.
        /// </summary>
        /// <param name="baseName">
        /// The content name prefix for this group of tile variations. Tile groups are
        /// name LikeThis0.png and LikeThis1.png and LikeThis2.png.
        /// </param>
        /// <param name="variationCount">
        /// The number of variations in this group.
        /// </param>
        private Tile LoadVarietyTile(string baseName, int variationCount, TileCollision collision)
        {
            int index = random.Next(variationCount);
            return LoadTile(baseName + index, collision);
        }

        /// <summary>
        /// Instantiates a player, puts him in the level, and remembers where to put him when he is resurrected.
        /// </summary>
        private Tile LoadStartTile(int x, int y)
        {
            if (Player != null)
                throw new NotSupportedException("A level may only have one starting point.");

            start = RectangleExtensions.GetBottomCenter(GetBounds(x, y));
            player = new Player(this, start, new Vector2(0.5f, 0.5f), 0);

            return new Tile(null, TileCollision.Passable);
        }

        /// <summary>
        /// Remembers the location of the level's exit.
        /// </summary>
        private Tile LoadExitTile(int x, int y)
        {
            if (exit != InvalidPosition)
                throw new NotSupportedException("A level may only have one exit.");

            exit = GetBounds(x, y).Center;
            completedGem = new Gem(this, new Vector2(exit.X, exit.Y));

            return new Tile(null, TileCollision.Passable);
        }

        /// <summary>
        /// Instantiates a geyser
        /// </summary>
        private Tile LoadGeyserTile(int x, int y, string name, GeyserType geyserType, int timeBetween, int firingTime, float rotation)
        {
            Vector2 position = RectangleExtensions.GetBottomCenter(GetBounds(x, y));
            geysers.Add(new Geyser(this, position, geyserType, timeBetween, firingTime, rotation, name));

            return new Tile(null, TileCollision.Passable);
        }

        private Tile LoadProjectileTile(int x, int y, Vector2 shooterVelocity, Vector2 target, Vector2 velocity, string name, 
            int frameWidth, int frameHeight, bool affectedByGravity, float rateOfFire, TileCollision collision, string textureName)
        {
            Vector2 position = RectangleExtensions.GetBottomCenter(GetBounds(x, y));
            projectileShooters.Add(new ProjectileShooter(this, position, shooterVelocity, target, rateOfFire, name, frameWidth, frameHeight, affectedByGravity, velocity));

            if (textureName != null)
                return LoadTile(textureName, collision);
            else
                return new Tile(null, collision);
        }

        /// <summary>
        /// Instantiates an enemy and puts him in the level.
        /// </summary>
        private Tile LoadEnemyTile(int x, int y, string spriteSet)
        {
            Vector2 position = RectangleExtensions.GetBottomCenter(GetBounds(x, y));
            enemies.Add(new Enemy(this, position, spriteSet, new Vector2(1.0f, 1.0f), 0.0f, false));

            return new Tile(null, TileCollision.Passable);
        }

        /// <summary>
        /// Instantiates a vortex
        /// </summary>
        private Tile LoadVortexTile(string spriteSet, int x, int y)
        {
            Vector2 position = RectangleExtensions.GetBottomCenter(GetBounds(x, y));
            enemies.Add(new Enemy(this, position, spriteSet, new Vector2(1.0f, 1.0f), 0.0f, true));

            return new Tile(null, TileCollision.Passable);
        }

        /// <summary>
        /// Instantiates a Hazard
        /// </summary>
        private Tile LoadHazard(string tileSprite, string hazardSprite, int frameWidth, int frameHeight, TileCollision collision, int tileVariation, int hazardVariation, int x, int y)
        {
            int index = random.Next(tileVariation);
            int i = random.Next(hazardVariation);
            Point position = GetBounds(x, y).Center;
            hazards.Add(new Hazard(this, new Vector2(position.X, position.Y), hazardSprite + i, frameWidth, frameHeight, null));

            if (tileSprite != null)
                return new Tile(Content.Load<Texture2D>(tileSprite + index), collision);
            else
                return new Tile(null, collision);
        }

        /// <summary>
        /// Instantiates a gem and puts it in the level.
        /// </summary>
        private Tile LoadFragmentTile(string name, TileCollision collision, int x, int y)
        {
            Point position = GetBounds(x, y).Center;
            fragments.Add(new Fragment(this, new Vector2(position.X, position.Y)));

            if (name != null)
                return LoadTile(name, collision);
            else
                return new Tile(null, TileCollision.Passable);
        }

        /// <summary>
        /// Unloads the level content.
        /// </summary>
        public void Dispose()
        {
            Content.Unload();
        }

        #endregion

        #region Bounds and collision

        /// <summary>
        /// Gets the collision mode of the tile at a particular location.
        /// This method handles tiles outside of the levels boundries by making it
        /// impossible to escape past the left or right edges, but allowing things
        /// to jump beyond the top of the level and fall off the bottom.
        /// </summary>
        public TileCollision GetCollision(int x, int y)
        {
            // Prevent escaping past the level ends.
            if (x < 0 || x >= Width)
                return TileCollision.Impassable;
            // Allow jumping past the level top and falling through the bottom.
            if (y < 0 || y >= Height)
                return TileCollision.Passable;

            return tiles[x, y].Collision;
        }

        /// <summary>
        /// Gets the bounding rectangle of a tile in world space.
        /// </summary>        
        public Rectangle GetBounds(int x, int y)
        {
            return new Rectangle(x * Tile.Width, y * Tile.Height, Tile.Width, Tile.Height);
        }

        /// <summary>
        /// Width of level measured in tiles.
        /// </summary>
        public int Width
        {
            get { return tiles.GetLength(0); }
        }

        /// <summary>
        /// Height of the level measured in tiles.
        /// </summary>
        public int Height
        {
            get { return tiles.GetLength(1); }
        }

        #endregion

        #region Update

        /// <summary>
        /// Updates all objects in the world, performs collision between them,
        /// and handles the time limit with scoring.
        /// </summary>
        public void Update(
            GameTime gameTime, 
            KeyboardState keyboardState, 
            GamePadState gamePadState, 
            TouchCollection touchState, 
            AccelerometerState accelState,
            DisplayOrientation orientation)
        {
            // Pause while the player is dead or time is expired.
            if (!Player.IsAlive || levelComplete)
            {
                // Still want to perform physics on the player.
                Player.ApplyPhysics(gameTime);
            }

            else if (ReachedExit)
            {
                score += (PointsFromTime / (int)time.TotalSeconds) + 1500 / Player.NumLives;
                levelComplete = true;
            }
            else
            {
                time += gameTime.ElapsedGameTime;
                Player.Update(gameTime, keyboardState, gamePadState, touchState, accelState, orientation);
                UpdateGems(gameTime);

                // Falling off the bottom of the level kills the player.
                if (Player.BoundingRectangle.Top >= Height * Tile.Height)
                    OnPlayerKilled();

                UpdateTileTimers(gameTime);
                UpdateProjectileShooters(gameTime);
                UpdateEnemies(gameTime);
                UpdateGeysers(gameTime);
                CheckHazardCollisions();

                // The player has reached the exit if they are standing on the ground and
                // his bounding rectangle contains the center of the exit tile. They can only
                // exit when they have collected all of the gems.
                if (Player.IsAlive &&
                    Player.IsOnGround &&
                    Player.BoundingRectangle.Contains(exit) &&
                    gemsCollected <= 0)
                {
                    OnExitReached();
                }
            }
        }

        private void UpdateProjectileShooters(GameTime gameTime)
        {
            foreach (ProjectileShooter projectileShooter in projectileShooters)
                projectileShooter.Update(gameTime);
        }

        private void UpdateTileTimers(GameTime gameTime)
        {
            for (int i = 0; i < tileTimers.Count; i++)
            {
                tileTimers[i].Update(gameTime);

                if (!tileTimers[i].rebuildOn && !tileTimers[i].destroyOn)
                {
                    tileTimers.RemoveAt(i--);
                }
            }
        }

        /// <summary>
        /// Animates each gem and checks to allows the player to collect them.
        /// </summary>
        private void UpdateGems(GameTime gameTime)
        {
            for (int i = 0; i < fragments.Count; ++i)
            {
                Fragment fragment = fragments[i];

                fragment.Update(gameTime);

                if (fragment.BoundingCircle.Intersects(Player.BoundingRectangle))
                {
                    //fragments.RemoveAt(i--);
                    OnFragmentCollected(fragment, Player);
                }

                if (fragment.BoundingCircle.Intersects(completedGem.BoundingRectangle))
                {
                    fragments.RemoveAt(i--);
                }
            }

            //if (fragments.Count == 0)
            if (gemsCollected <= 0)
                completedGem.Update(gameTime);
        }

        /// <summary>
        /// Animates the geyser
        /// </summary>
        private void UpdateGeysers(GameTime gameTime)
        {
            foreach (Geyser geyser in geysers)
            {
                geyser.Update(gameTime);

                if (geyser.BoundingRectangle.Intersects(Player.BoundingRectangle) && geyser.Firing)
                    GeyserCollision(geyser);
            }
        }

        private void GeyserCollision(Geyser geyser)
        {
            Vector2 geyserForce = new Vector2((float)Math.Cos(geyser.Rotation) * -5, (float)Math.Cos(geyser.Rotation) * -5);
            switch (geyser.GeyserType)
            {
                case GeyserType.Fire:
                    OnPlayerKilled(geyser);
                    return;
                case GeyserType.Water:
                    Player.Velocity *= geyserForce;
                    Player.Breath -= 5.0f;
                    return;
                case GeyserType.Wind:
                    Player.Velocity *= geyserForce;
                    return;
            }
        }

        /// <summary>
        /// Animates each enemy and allow them to kill the player.
        /// </summary>
        private void UpdateEnemies(GameTime gameTime)
        {
            foreach (Enemy enemy in enemies)
            {
                enemy.Update(gameTime);
                /*
                if (enemy.vortex)
                {
                    // Calculate the visible range of tiles.
                    int left = (int)Math.Floor(cameraPosition.X);
                    int right = left + 800;
                    int top = (int)Math.Floor(cameraPosition.Y);
                    int bottom = top + 800;

                    if (enemy.Position.X >= left && enemy.Position.Y <= right)
                        if (enemy.Position.Y >= top && enemy.Position.Y <= bottom)
                            enemy.UpdateVortex(gameTime);
                }*/

                // Touching an enemy instantly kills the player
                if (enemy.BoundingRectangle.Intersects(Player.BoundingRectangle))
                {
                    OnPlayerKilled(enemy);
                }
            }
        }

        /// <summary>
        /// Checks for collisions between hazards and the player
        /// </summary>
        private void CheckHazardCollisions()
        {
            foreach (Hazard hazard in hazards)
            {
                //If the player's bounding rect intersects the hazards rect
                if (hazard.BoundingRect.Intersects(player.BoundingRectangle))
                    OnPlayerKilled(hazard);
            }
        }

        /// <summary>
        /// Called when a gem is collected.
        /// </summary>
        /// <param name="gem">The gem that was collected.</param>
        /// <param name="collectedBy">The player who collected this gem.</param>
        private void OnFragmentCollected(Fragment fragment, Player collectedBy)
        {
            if (fragment.collected == false)
            {
                score += Fragment.PointValue;

                //Set the velocity of the fragment
                fragment.endPosition = new Vector2(exit.X, exit.Y);
                float dX = (fragment.endPosition.X - fragment.Position.X);
                float dY = (fragment.endPosition.Y - fragment.Position.Y);
                float distance = (dX * dX) + (dY * dY);
                distance = (float)Math.Sqrt(distance);
                dX *= (1000 / distance);
                dY *= (1000 / distance);

                fragment.velocity = new Vector2(dX, dY);

                fragment.OnCollected(collectedBy);
                gemsCollected -= 1;
            }
        }

        /// <summary>
        /// Called when the player is killed.
        /// </summary>
        /// <param name="killedBy">
        /// The enemy who killed the player. This is null if the player was not killed by an
        /// enemy, such as when a player falls into a hole.
        /// </param>
        private void OnPlayerKilled(Enemy killedBy)
        {
            Player.OnKilled(killedBy);
        }

        /// <summary>
        /// Called when the player is killed.
        /// </summary>
        /// <param name="killedBy">
        /// The geyser who killed the player. This is null if the player was not killed by an
        /// enemy, such as when a player falls into a hole.
        /// </param>
        private void OnPlayerKilled(Geyser killedBy)
        {
            Player.OnKilled(killedBy);
        }

        /// <summary>
        /// Called when the player is killed
        /// </summary>
        /// <param name="killedBy">
        /// The hazard that killed the player.
        /// </param>
        private void OnPlayerKilled(Hazard killedBy)
        {
            Player.OnKilled(killedBy);
        }

        /// <summary>
        /// Called when the player falls off a cliff
        /// </summary>
        private void OnPlayerKilled()
        {
            Player.OnKilled();
        }

        /// <summary>
        /// Called when the player reaches the level's exit.
        /// </summary>
        private void OnExitReached()
        {
            Player.OnReachedExit();
            reachedExit = true;
        }

        /// <summary>
        /// Restores the player to the starting point to try the level again.
        /// </summary>
        public void StartNewLife()
        {
            Player.Reset(start);
        }

        #endregion

        #region Draw

        /// <summary>
        /// Draw everything in the level from background to foreground.
        /// </summary>
        public void Draw(GameTime gameTime, SpriteBatch spriteBatch)
        {
            spriteBatch.Begin();

            for (int i = 0; i <= EntityLayer; ++i)
                layers[i].Draw(spriteBatch);

            spriteBatch.End();

            ScrollCamera(spriteBatch.GraphicsDevice.Viewport);
            Matrix cameraTransform = Matrix.CreateTranslation(-cameraPosition.X, -cameraPosition.Y, 0.0f);
            spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, SamplerState.LinearClamp, DepthStencilState.Default, 
                RasterizerState.CullCounterClockwise, null, cameraTransform);

            DrawTiles(spriteBatch);

            Player.Draw(gameTime, spriteBatch);

            DrawEnemies(gameTime, spriteBatch);

            DrawHazards(gameTime, spriteBatch);
            
            DrawGems(gameTime, spriteBatch);

            DrawGeysers(gameTime, spriteBatch);

            DrawProjectiles(gameTime, spriteBatch);

            spriteBatch.End();
            /*
            spriteBatch.Begin();
            for (int i = EntityLayer + 1; i < layers.Length; ++i)
                layers[i].Draw(spriteBatch);
            spriteBatch.End();*/

        }

        private void ScrollCamera(Viewport viewport)
        {
#if ZUNE
const float ViewMargin = 0.45f;
#else
            const float ViewMargin = 0.35f;
#endif

            // Calculate the edges of the screen.
            float marginWidth = viewport.Width * ViewMargin;
            float marginLeft = cameraPosition.X + marginWidth;
            float marginRight = cameraPosition.X + viewport.Width - marginWidth;
            float marginHeight = viewport.Height * ViewMargin;
            float marginTop = cameraPosition.Y + marginHeight;
            float marginBottom = cameraPosition.Y + viewport.Height - marginHeight;


            // Calculate how far to scroll when the player is near the edges of the screen.
            Vector2 cameraMovement = Vector2.Zero;
            if (Player.Position.X < marginLeft)
                cameraMovement.X = Player.Position.X - marginLeft;
            else if (Player.Position.X > marginRight)
                cameraMovement.X = Player.Position.X - marginRight;
            if (Player.Position.Y < marginTop)
                cameraMovement.Y = Player.Position.Y - marginTop;
            else if (Player.Position.Y > marginBottom)
                cameraMovement.Y = Player.Position.Y - marginBottom;

            // Update the camera position, but prevent scrolling off the ends of the level.
            float maxCameraPositionX = Tile.Width * Width - viewport.Width;
            float maxCameraPositionY = Tile.Height * Height - viewport.Height;
            cameraPosition.X = MathHelper.Clamp(cameraPosition.X + cameraMovement.X, 0.0f, maxCameraPositionX);
            cameraPosition.Y = MathHelper.Clamp(cameraPosition.Y + cameraMovement.Y, 0.0f, maxCameraPositionY);
        }

        private void DrawProjectiles(GameTime gameTime, SpriteBatch spriteBatch)
        {
            foreach (ProjectileShooter projectileShooter in projectileShooters)
                projectileShooter.Draw(gameTime, spriteBatch, cameraPosition);
        }

        /// <summary>
        /// Draw each hazard in the game
        /// </summary>

        private void DrawHazards(GameTime gameTime, SpriteBatch spriteBatch)
        {
            // Calculate the visible range of tiles.
            int left = (int)Math.Floor(cameraPosition.X);
            int right = left + spriteBatch.GraphicsDevice.Viewport.Width;
            int top = (int)Math.Floor(cameraPosition.Y);
            int bottom = top + spriteBatch.GraphicsDevice.Viewport.Width;

            foreach (Hazard hazard in hazards)
                if (hazard.Position.X >= left && hazard.Position.X <= right + hazard.sprite.Animation.FrameWidth * 3)
                    if (hazard.Position.Y >= top && hazard.Position.Y <= bottom)
                        hazard.Draw(gameTime, spriteBatch);
        }

        /// <summary>
        /// Draw each geyser in the level
        /// </summary>
        private void DrawGeysers(GameTime gameTime, SpriteBatch spriteBatch)
        {
            // Calculate the visible range of tiles.
            int left = (int)Math.Floor(cameraPosition.X);
            int right = left + spriteBatch.GraphicsDevice.Viewport.Width;
            int top = (int)Math.Floor(cameraPosition.Y);
            int bottom = top + spriteBatch.GraphicsDevice.Viewport.Width;

            foreach (Geyser geyser in geysers)
            {
                if (geyser.Position.X >= left && geyser.Position.X <= right)
                    if (geyser.Position.Y >= top && geyser.Position.Y <= bottom)
                        geyser.Draw(gameTime, spriteBatch);
            }
        }

        /// <summary>
        /// Draw each enemy in the level
        /// </summary>
        private void DrawEnemies(GameTime gameTime, SpriteBatch spriteBatch)
        {
            // Calculate the visible range of tiles.
            int left = (int)Math.Floor(cameraPosition.X);
            int right = left + spriteBatch.GraphicsDevice.Viewport.Width;
            int top = (int)Math.Floor(cameraPosition.Y);
            int bottom = top + spriteBatch.GraphicsDevice.Viewport.Width;

            foreach (Enemy enemy in enemies)
                if (enemy.Position.X >= left && enemy.Position.X <= right)
                    if (enemy.Position.Y >= top && enemy.Position.Y <= bottom)
                        enemy.Draw(gameTime, spriteBatch);
        }

        /// <summary>
        /// Draw each gem in the level
        /// </summary>
        private void DrawGems(GameTime gameTime, SpriteBatch spriteBatch)
        {
            // Calculate the visible range of tiles.
            int left = (int)Math.Floor(cameraPosition.X);
            int right = left + spriteBatch.GraphicsDevice.Viewport.Width;
            int top = (int)Math.Floor(cameraPosition.Y);
            int bottom = top + spriteBatch.GraphicsDevice.Viewport.Width;

            foreach (Fragment fragment in fragments)
            {
                if (fragment.Position.X >= left && fragment.Position.X <= right)
                    if (fragment.Position.Y >= top && fragment.Position.Y <= bottom)
                        fragment.Draw(gameTime, spriteBatch);
            }

            //if (fragments.Count == 0)
            if (gemsCollected <= 0)
                if (completedGem.Position.X >= left && completedGem.Position.X <= right)
                    if (completedGem.Position.Y >= top && completedGem.Position.Y <= bottom)
                            completedGem.Draw(gameTime, spriteBatch);
        }

        /// <summary>
        /// Draws each tile in the level.
        /// </summary>
        private void DrawTiles(SpriteBatch spriteBatch)
        {
            // Calculate the visible range of tiles.
            int left = (int)Math.Floor(cameraPosition.X / Tile.Width);
            int right = left + spriteBatch.GraphicsDevice.Viewport.Width / Tile.Width;
            right = Math.Min(right + 1, Width);
            int top = (int)Math.Floor(cameraPosition.Y / Tile.Height);
            int bottom = top + spriteBatch.GraphicsDevice.Viewport.Width / Tile.Height;
            bottom = Math.Min(bottom + 1, Height);

            // For each tile position
            for (int y = top; y < bottom; ++y)
            {
                for (int x = left; x < right; ++x)
                {
                    // If there is a visible tile in that position
                    Texture2D texture = tiles[x, y].Texture;
                    if (texture != null)
                    {
                        // Draw it in screen space.
                        Vector2 position = new Vector2(x, y) * Tile.Size;
                        spriteBatch.Draw(texture, position, Color.White);
                    }
                }
            }
        }

        #endregion
    }
}
