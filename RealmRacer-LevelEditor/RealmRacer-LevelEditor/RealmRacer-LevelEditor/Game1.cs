using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Linq;
using System.IO;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.GamerServices;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Media;
using Microsoft.Xna.Framework.Storage;

namespace RealmRacer_LevelEditor
{
    /// <summary>
    /// This is the main type for your game
    /// </summary>
    public class Game1 : Microsoft.Xna.Framework.Game
    {
        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        public static extern uint MessageBox(IntPtr hWnd, String text, String caption, uint type);

        GraphicsDeviceManager graphics;
        SpriteBatch spriteBatch;

        MouseState mouseState;
        KeyboardState keyboardState;
        KeyboardState prevKeyboardState;

        Level level;

        int sizeX;
        int sizeY;

        public Game1()
        {
            graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";
        }

        /// <summary>
        /// Allows the game to perform any initialization it needs to before starting to run.
        /// This is where it can query for any required services and load any non-graphic
        /// related content.  Calling base.Initialize will enumerate through any components
        /// and initialize them as well.
        /// </summary>
        protected override void Initialize()
        {
            // TODO: Add your initialization logic here
            this.IsMouseVisible = true;

            base.Initialize();
        }

        /// <summary>
        /// LoadContent will be called once per game and is the place to load
        /// all of your content.
        /// </summary>
        protected override void LoadContent()
        {
            // Create a new SpriteBatch, which can be used to draw textures.
            spriteBatch = new SpriteBatch(GraphicsDevice);

            // TODO: use this.Content to load your game content here
            sizeX = 100;
            sizeY = 30;

            level = new Level(Services);
            LoadLevel();
        }

        /// <summary>
        /// UnloadContent will be called once per game and is the place to unload
        /// all content.
        /// </summary>
        protected override void UnloadContent()
        {
            // TODO: Unload any non ContentManager content here
        }

        /// <summary>
        /// Allows the game to run logic such as updating the world,
        /// checking for collisions, gathering input, and playing audio.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        protected override void Update(GameTime gameTime)
        {
            // Allows the game to exit
            if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed)
                this.Exit();

            prevKeyboardState = keyboardState;
            keyboardState = Keyboard.GetState();
            mouseState = Mouse.GetState();

            //Load
            if (keyboardState.IsKeyDown(Keys.F1))
            {
                uint selection = 7;
                selection = MessageBox(new IntPtr(0), "Would you like to save before loading?", "Save?", 4);

                //Save then load
                if (selection == 6)
                    SaveLevel();

                //Just load
                LoadLevel();
            }
            //Save
            else if (keyboardState.IsKeyDown(Keys.F2))
            {
                SaveLevel();
            }
            //Create a new level
            else if (keyboardState.IsKeyDown(Keys.F3))
            {
                uint selection = 7;
                selection = MessageBox(new IntPtr(0), "Would you like to save before creating a new level?", "Save?", 4);

                //Save then load
                if (selection == 6)
                    SaveLevel();

                level = new Level(Services, sizeX, sizeY);
            }

            if (keyboardState.IsKeyDown(Keys.Home) && prevKeyboardState.IsKeyUp(Keys.Home))
                sizeY += 1;
            if (keyboardState.IsKeyDown(Keys.End) && prevKeyboardState.IsKeyUp(Keys.End))
                sizeY -= 1;
            if (keyboardState.IsKeyDown(Keys.Insert) && prevKeyboardState.IsKeyUp(Keys.Insert))
                sizeX += 1;
            if (keyboardState.IsKeyDown(Keys.Delete) && prevKeyboardState.IsKeyUp(Keys.Delete))
                sizeX -= 1;

            this.Window.Title = "Level Editor -- X: " + level.Width / 40 + " Y: " + level.Height / 32 + " new X: " + sizeX + " new Y: " + sizeY;
            // TODO: Add your update logic here
            level.Update(gameTime, keyboardState, mouseState);

            base.Update(gameTime);
        }

        /// <summary>
        /// Loads the level
        /// </summary>
        private void LoadLevel()
        {
            Stream fileStream;
            if (File.Exists("Level.txt"))
            {
                fileStream = TitleContainer.OpenStream("Level.txt");
            }
            else
            {
                List<string> data = new List<string>();
                for (int y = 0; y < 30; y++)
                {
                    string datum = null;
                    for (int x = 0; x < 100; x++)
                    {
                        datum += ".";
                    }
                    data.Add(datum);
                }
                File.AppendAllLines("Level.txt", data);

                MessageBox(new IntPtr(0), "Level.txt does not exist, creating new file", "Error!", 0);
                fileStream = TitleContainer.OpenStream("Level.txt");
            }

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

            Tile[,] tiles = new Tile[width, lines.Count];

            //Loop for every tile position
            for (int y = 0; y < tiles.GetLength(1); y++)
            {
                for (int x = 0; x < tiles.GetLength(0); x++)
                {
                    //Load each tile
                    char tileType = lines[y][x];
                    tiles[x, y] = LoadTile(tileType, x, y);
                }
            }

            Vector2 camPosition = level.CameraPosition;
            level = new Level(Services, tiles);
            level.CameraPosition = camPosition;
            MessageBox(new IntPtr(0), "Level loading complete!", "Level Loaded", 0);
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
        private Tile LoadTile(char tileType, int x, int y)
        {
            Vector2 position = new Vector2(x * 40, y * 32);
            switch (tileType)
            {
                #region RequiredTiles
                // Blank space
                case '.':
                    return new Tile(null, tileType, position);

                // Exit
                case 'X':
                    return new Tile(Content.Load<Texture2D>("Sprites/gem"), tileType, position);

                // Player 1 start point
                case '1':
                    return new Tile(Content.Load<Texture2D>("Sprites/Player/Idle"), tileType, position);

                #endregion

                #region Gems
                // Gem
                case 'F':
                    return new Tile(Content.Load<Texture2D>("Sprites/fragment"), tileType, position);

                //Gem in quicksand
                case 'f':
                    return new Tile(Content.Load<Texture2D>("Sprites/quicksandFragment"), tileType, position);

                //Gem in water
                case 'g':
                    return new Tile(Content.Load<Texture2D>("Sprites/underwaterFragment"), tileType, position);

                #endregion

                #region Tiles

                //Water tile
                case 'U':
                    return new Tile(Content.Load<Texture2D>("Tiles/Water/underwater"), tileType, position);

                //Quicksand
                case 'Q':
                    return new Tile(Content.Load<Texture2D>("Tiles/Earth/quicksand"), tileType, position);

                //Clouds
                case 'C':
                    return new Tile(Content.Load<Texture2D>("Tiles/Wind/windCloud"), tileType, position);

                //Ice
                case 'S':
                    return new Tile(Content.Load<Texture2D>("Tiles/Water/ice"), tileType, position);

                // Floating platform
                case '-':
                    return new Tile(Content.Load<Texture2D>("Tiles/Earth/earthPlatform0"), tileType, position);

                // Impassable block
                case '#':
                    return new Tile(Content.Load<Texture2D>("Tiles/Earth/earthBlock0"), tileType, position);

                #endregion

                #region Hazards

                //Stalagtite
                case 'V':
                    return new Tile(Content.Load<Texture2D>("Sprites/Hazards/Earth/stalagtite0"), tileType, position);

                //Stalagtite in quicksand
                case 'v':
                    return new Tile(Content.Load<Texture2D>("Sprites/Hazards/Earth/quicksandStalagtite0"), tileType, position);

                //Ice stalagtite
                case 'I':
                    return new Tile(Content.Load<Texture2D>("Sprites/Hazards/Water/icicleDown0"), tileType, position);

                //Ice stalagmite
                case 'i':
                    return new Tile(Content.Load<Texture2D>("Sprites/Hazards/Water/icicleUp0"), tileType, position);

                //Ice stalactite underwater
                case 'O':
                    return new Tile(Content.Load<Texture2D>("Sprites/Hazards/Water/underwaterStalactite"), tileType, position);

                //Ice stalagmite underwater
                case 'o':
                    return new Tile(Content.Load<Texture2D>("Sprites/Hazards/Water/underwaterStalagmite"), tileType, position);

                //Stalagmite
                case '^':
                    return new Tile(Content.Load<Texture2D>("Sprites/Hazards/Earth/stalagmite0"), tileType, position);

                //Stalagmite in quicksand
                case '6':
                    return new Tile(Content.Load<Texture2D>("Sprites/Hazards/Earth/quicksandStalagmite0"), tileType, position);

                //Water Geyser Up
                case 'G':
                    return new Tile(Content.Load<Texture2D>("Sprites/Hazards/Water/waterGeyserIdle"), tileType, position);

                //Fire projectile horizontal left
                case 'P':
                    return new Tile(Content.Load<Texture2D>("Sprites/Hazards/Fire/fireballL"), tileType, position);

                //Fire projectile Right
                case 'p':
                    return new Tile(Content.Load<Texture2D>("Sprites/Hazards/Fire/fireballR"), tileType, position);

                //fire projectile Up
                case 'B':
                    return new Tile(Content.Load<Texture2D>("Sprites/Hazards/Fire/fireballU"), tileType, position);

                //Fire projectile Down
                case 'b':
                    return new Tile(Content.Load<Texture2D>("Sprites/Hazards/Fire/fireballD"), tileType, position);

                //Fire wall
                case 'W':
                    return new Tile(Content.Load<Texture2D>("Sprites/Hazards/Fire/fireWall"), tileType, position);

                //Fire geyser
                case 'R':
                    return new Tile(Content.Load<Texture2D>("Sprites/Hazards/Fire/fireGeyserIdle"), tileType, position);


                //Vortex
                case 'T':
                    return new Tile(Content.Load<Texture2D>("Sprites/Hazards/Wind/tornado"), tileType, position);

                //Wind jet
                case 'J':
                    return new Tile(Content.Load<Texture2D>("Sprites/Hazards/Wind/windGeyserIdle"), tileType, position);

                #endregion

                // Unknown tile type character
                default:
                    throw new NotSupportedException(String.Format("Unsupported tile type character '{0}' at position {1}, {2}.", tileType, x, y));
            }
        }

        /// <summary>
        /// Saves the level
        /// </summary>
        private void SaveLevel()
        {
            if (File.Exists("Level.txt"))
                File.Delete("Level.txt");

            List<string> data = new List<string>();
            for (int y = 0; y < (level.Height / 32); y++)
            {
                string datum = null;
                for (int x = 0; x < (level.Width / 40); x++)
                {
                    datum += level.TileArray[x, y].TileType.ToString();
                }
                data.Add(datum);
            }

            File.AppendAllLines("Level.txt", data);

            MessageBox(new IntPtr(0), "Level saved!", "Level Saved", 0);
        }

        /// <summary>
        /// This is called when the game should draw itself.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(Color.CornflowerBlue);

            // TODO: Add your drawing code here
            level.Draw(spriteBatch);

            base.Draw(gameTime);
        }
    }
}
