using System;
using System.IO;
using System.Xml.Serialization;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Media;
using Microsoft.Xna.Framework.Input.Touch;


namespace RealmRacer
{
    /// <summary>
    /// State of the game
    /// </summary>
    enum GameState
    {
        MainMenu = 0,
        Loading = 1,
        InGame = 2,
        HighScore = 3,
        EnterName = 4,
    }
    
    /// <summary>
    /// Struct to hold high score data
    /// </summary>
    [Serializable]
    public struct HighScoreData
    {
        public string[] playerName;
        public int[] score;

        public int count;

        public HighScoreData(int count)
        {
            playerName = new string[count];
            score = new int[count];
            this.count = count;
        }
    }

    [Serializable]
    public struct SaveGameData
    {
        public string playerName;
        public int score;
        public int levelIndex;
        public int lives;

        public SaveGameData(string playerName, int score, int levelIndex, int lives)
        {
            this.playerName = playerName;
            this.score = score;
            this.levelIndex = levelIndex;
            this.lives = lives;
        }
    }

    /// <summary>
    /// This is the main type for your game
    /// </summary>
    public class PlatformerGame : Microsoft.Xna.Framework.Game
    {
        // Resources for drawing.
        private GraphicsDeviceManager graphics;
        private SpriteBatch spriteBatch;

        // Global content.
        private SpriteFont hudFont;

        private Texture2D winOverlay;
        private Texture2D loseOverlay;
        private Texture2D diedOverlay;

        // Meta-level game state.
        private int levelIndex = -1;
        private Level level;
        private bool wasContinuePressed;

        // When the time remaining is less than the warning time, it blinks on the hud
        private static readonly TimeSpan WarningTime = TimeSpan.FromSeconds(30);

        // We store our input states so that we only poll once per frame, 
        // then we use the same input state wherever needed
        private GamePadState gamePadState;
        private KeyboardState keyboardState;
        private TouchCollection touchState;
        private AccelerometerState accelerometerState;
        private MouseState mouseState;
        
        // The number of levels in the Levels directory of our content. We assume that
        // levels in our content are 0-based and that all numbers under this constant
        // have a level file present. This allows us to not need to check for the file
        // or handle exceptions, both of which can add unnecessary time to level loading.
        private int numberOfLevels;

        //Total score
        public int globalScore;    

        //Used to calculate framerate
        int framerate = 0;
        int framecounter = 0;
        TimeSpan elapsedtime = TimeSpan.Zero;

        //Controls the state of the game
        private GameState gameState;

        //Menu variables
        Texture2D menuBackground;
        Texture2D scoreBackground;
        Button playButton;
        Button quitButton;
        Button scoreButton;
        Button menuButton;
        Button resetButton;
        Button loadButton;

        //High score data
        HighScoreData highScores;
        public readonly string highScoreFileName = "highscores.lst";
        public string playerName;
        TimeSpan inputTimer;
       
        public PlatformerGame()
        {
            graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";

#if WINDOWS_PHONE
            graphics.IsFullScreen = true;
            TargetElapsedTime = TimeSpan.FromTicks(333333);
#endif

            Accelerometer.Initialize();

            this.Window.Title = "Realm Racer";

            if (!File.Exists(highScoreFileName))
            {
                //If the file does not exist, make a fake one
                //Create the data to save
                HighScoreData data = new HighScoreData(5);
                data.playerName[0] = "Jim";
                data.score[0] = 1000;

                data.playerName[1] = "Tom";
                data.score[1] = 700;

                data.playerName[2] = "John";
                data.score[2] = 500;

                data.playerName[3] = "Frank";
                data.score[3] = 300;

                data.playerName[4] = "Bill";
                data.score[4] = 100;

                SaveHighScoreData(data, highScoreFileName);
            }
        }

        /// <summary>
        /// Check for new high score and insert it into the table if there is one
        /// </summary>
        public bool UpdateHighScoreData()
        {
            HighScoreData data = LoadHighScoreData(highScoreFileName);

            //Check if there is a new high score
            int scoreIndex = -1;
            for (int i = 0; i < data.count; i++)
            {
                if (globalScore > data.score[i])
                {
                    scoreIndex = i;
                    break;
                }
            }

            //New high score
            if (scoreIndex > -1)
            {
                //New high score found -- do swaps
                for (int i = data.count - 1; i > scoreIndex; i--)
                {
                    data.playerName[i] = data.playerName[i - 1];
                    data.score[i] = data.score[i - 1];
                }

                data.playerName[scoreIndex] = playerName;
                data.score[scoreIndex] = globalScore;

                File.Delete(highScoreFileName);
                SaveHighScoreData(data, highScoreFileName);

                return true;
            }

            return false;
        }

        /// <summary>
        /// Saves the high score data
        /// </summary>
        public static void SaveHighScoreData(HighScoreData data, string filename)
        {
            //Get the path of the save
            FileStream fs = File.Open(filename, FileMode.OpenOrCreate);

            try
            {
                //Convert the object to XML data and put it in the stream
                XmlSerializer serializer = new XmlSerializer(typeof(HighScoreData));
                serializer.Serialize(fs, data);
            }
            finally
            {
                //Close the file
                fs.Close();
            }
        }

        /// <summary>
        /// Loads the high score data
        /// </summary>
        public static HighScoreData LoadHighScoreData(string filename)
        {
            HighScoreData datum;
            FileStream fs;

            //Open the filestream
            if (File.Exists(filename))
            {
                fs = File.Open(filename, FileMode.Open, FileAccess.Read);
            }
            else
            {
                //If the file does not exist, make a fake one
                //Create the data to save
                HighScoreData data = new HighScoreData(5);
                data.playerName[0] = "Jim";
                data.score[0] = 1000;

                data.playerName[1] = "Tom";
                data.score[1] = 700;

                data.playerName[2] = "John";
                data.score[2] = 500;

                data.playerName[3] = "Frank";
                data.score[3] = 300;

                data.playerName[4] = "Bill";
                data.score[4] = 100;

                SaveHighScoreData(data, filename);

                fs = File.Open(filename, FileMode.Open, FileAccess.Read);
            }

            try
            {
                //Read the data from the file
                XmlSerializer serializer = new XmlSerializer(typeof(HighScoreData));
                datum = (HighScoreData)serializer.Deserialize(fs);
            }
            finally
            {
                //Close the filestream
                fs.Close();
            }

            return datum;
        }

        /// <summary>
        /// LoadContent will be called once per game and is the place to load
        /// all of your content.
        /// </summary>
        protected override void LoadContent()
        {
            //Set the height and width of the window and apply changes
            graphics.PreferredBackBufferHeight = 600;
            graphics.PreferredBackBufferWidth = 800;
            graphics.ApplyChanges();

            //Set the initial state of the game
            gameState = GameState.MainMenu;

            // Create a new SpriteBatch, which can be used to draw textures.
            spriteBatch = new SpriteBatch(GraphicsDevice);

            // Load fonts
            hudFont = Content.Load<SpriteFont>("Fonts/Hud");

            //Get the number of levels
            int index = levelIndex;
            string levelPath = string.Format("Content/Levels/{0}.txt", index);
            do
            {
                index += 1;
                levelPath = string.Format("Content/Levels/{0}.txt", index);
            } while (File.Exists(levelPath));
            numberOfLevels = index;

            // Load overlay textures
            winOverlay = Content.Load<Texture2D>("Overlays/you_win");
            loseOverlay = Content.Load<Texture2D>("Overlays/you_lose");
            diedOverlay = Content.Load<Texture2D>("Overlays/you_died");

            //Load menu variables
            //Main menu
            menuBackground = Content.Load<Texture2D>("Menu/MainMenu");
            playButton = new Button(Content.Load<Texture2D>("Menu/play"), new Vector2(800 / 2, (600 /2) - 80));
            if (File.Exists("save.lst"))
                loadButton = new Button(Content.Load<Texture2D>("Menu/load"), new Vector2(800 / 2, (600 / 2)));
            else
                loadButton = new Button(Content.Load<Texture2D>("Menu/loadGrey"), new Vector2(800 / 2, (600 / 2)));
            scoreButton = new Button(Content.Load<Texture2D>("Menu/scores"), new Vector2(800 / 2, (600 / 2) + 80));
            quitButton = new Button(Content.Load<Texture2D>("Menu/quit"), new Vector2(800 / 2, (600 / 2) + 160));

            //High Score menu
            scoreBackground = Content.Load<Texture2D>("Menu/HighScores");
            menuButton = new Button(Content.Load<Texture2D>("Menu/menu"), new Vector2((800 /2) - 80, 520));
            resetButton = new Button(Content.Load<Texture2D>("Menu/reset"), new Vector2((800 / 2) + 80, 520));
            

            //Set the score
            globalScore = 0;

            highScores = LoadHighScoreData(highScoreFileName);
            inputTimer = TimeSpan.FromSeconds(0.1);

            //LoadNextLevel();
        }

        /// <summary>
        /// Allows the game to run logic such as updating the world,
        /// checking for collisions, gathering input, and playing audio.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        protected override void Update(GameTime gameTime)
        {
            elapsedtime += gameTime.ElapsedGameTime;

            if (elapsedtime > TimeSpan.FromSeconds(1))
            {
                elapsedtime -= TimeSpan.FromSeconds(1);
                framerate = framecounter;
                framecounter = 0;
            }

            // Handle polling for our input and handling high-level input
            HandleInput();

            //In the game
            if (gameState == GameState.InGame)
            {
                // update our level, passing down the GameTime along with all of our input states
                level.Update(gameTime, keyboardState, gamePadState, touchState,
                             accelerometerState, Window.CurrentOrientation);

                if (keyboardState.IsKeyDown(Keys.Escape))
                {
                    gameState = GameState.MainMenu;
                    levelIndex = -1;
                    globalScore = 0;
                    level.Dispose();
                }
            }
        
            //In the menu
            if (gameState == GameState.MainMenu)
            {
                this.IsMouseVisible = true;
                //Update the buttons
                playButton.Update(mouseState);
                loadButton.Update(mouseState);
                scoreButton.Update(mouseState);
                quitButton.Update(mouseState);

                if (playButton.clicked)
                {
                    this.IsMouseVisible = false;
                    gameState = GameState.EnterName;
                }

                if (loadButton.clicked)
                {
                    LoadSaveGame();
                }

                if (scoreButton.clicked)
                {
                    gameState = GameState.HighScore;
                }

                if (quitButton.clicked)
                {
                    this.Exit();
                }
            }

            //In high scores menu
            if (gameState == GameState.HighScore)
            {
                this.IsMouseVisible = true;
                menuButton.Update(mouseState);
                resetButton.Update(mouseState);

                if (menuButton.clicked)
                {
                    gameState = GameState.MainMenu;
                }

                if (resetButton.clicked)
                {
                    ResetHighScoreData();
                }
            }

            //Enter player name
            if (gameState == GameState.EnterName)
            {
                inputTimer -= gameTime.ElapsedGameTime;
                if (inputTimer <= TimeSpan.Zero)
                {
                    EnterName();
                }
            }

            base.Update(gameTime);
        }

        private void ResetHighScoreData()
        {
            HighScoreData data = new HighScoreData(5);
            data.playerName[0] = "Jim";
            data.score[0] = 1000;

            data.playerName[1] = "Tom";
            data.score[1] = 700;

            data.playerName[2] = "John";
            data.score[2] = 500;

            data.playerName[3] = "Frank";
            data.score[3] = 300;

            data.playerName[4] = "Bill";
            data.score[4] = 100;

            SaveHighScoreData(data, highScoreFileName);
        }

        private void EnterName()
        {
            Keys[] pressedKeys = keyboardState.GetPressedKeys();

            if (pressedKeys.GetLength(0) > 0)
            {
                inputTimer = TimeSpan.FromSeconds(0.1);
                switch (pressedKeys[0])
                {
                    case Keys.A:
                        if (keyboardState.IsKeyDown(Keys.LeftShift) || keyboardState.IsKeyDown(Keys.RightShift))
                            playerName += "A";
                        else
                            playerName += "a";
                        break;

                    case Keys.B:
                        if (keyboardState.IsKeyDown(Keys.LeftShift) || keyboardState.IsKeyDown(Keys.RightShift))
                            playerName += "B";
                        else
                            playerName += "b";
                        break;

                    case Keys.C:
                        if (keyboardState.IsKeyDown(Keys.LeftShift) || keyboardState.IsKeyDown(Keys.RightShift))
                            playerName += "C";
                        else
                            playerName += "c";
                        break;

                    case Keys.D:
                        if (keyboardState.IsKeyDown(Keys.LeftShift) || keyboardState.IsKeyDown(Keys.RightShift))
                            playerName += "D";
                        else
                            playerName += "d";
                        break;

                    case Keys.E:
                        if (keyboardState.IsKeyDown(Keys.LeftShift) || keyboardState.IsKeyDown(Keys.RightShift))
                            playerName += "E";
                        else
                            playerName += "e";
                        break;

                    case Keys.F:
                        if (keyboardState.IsKeyDown(Keys.LeftShift) || keyboardState.IsKeyDown(Keys.RightShift))
                            playerName += "F";
                        else
                            playerName += "f";
                        break;

                    case Keys.G:
                        if (keyboardState.IsKeyDown(Keys.LeftShift) || keyboardState.IsKeyDown(Keys.RightShift))
                            playerName += "G";
                        else
                            playerName += "g";
                        break;

                    case Keys.H:
                        if (keyboardState.IsKeyDown(Keys.LeftShift) || keyboardState.IsKeyDown(Keys.RightShift))
                            playerName += "H";
                        else
                            playerName += "h";
                        break;

                    case Keys.I:
                        if (keyboardState.IsKeyDown(Keys.LeftShift) || keyboardState.IsKeyDown(Keys.RightShift))
                            playerName += "I";
                        else
                            playerName += "i";
                        break;

                    case Keys.J:
                        if (keyboardState.IsKeyDown(Keys.LeftShift) || keyboardState.IsKeyDown(Keys.RightShift))
                            playerName += "J";
                        else
                            playerName += "j";
                        break;

                    case Keys.K:
                        if (keyboardState.IsKeyDown(Keys.LeftShift) || keyboardState.IsKeyDown(Keys.RightShift))
                            playerName += "K";
                        else
                            playerName += "k";
                        break;

                    case Keys.L:
                        if (keyboardState.IsKeyDown(Keys.LeftShift) || keyboardState.IsKeyDown(Keys.RightShift))
                            playerName += "L";
                        else
                            playerName += "l";
                        break;

                    case Keys.M:
                        if (keyboardState.IsKeyDown(Keys.LeftShift) || keyboardState.IsKeyDown(Keys.RightShift))
                            playerName += "M";
                        else
                            playerName += "m";
                        break;

                    case Keys.N:
                        if (keyboardState.IsKeyDown(Keys.LeftShift) || keyboardState.IsKeyDown(Keys.RightShift))
                            playerName += "N";
                        else
                            playerName += "n";
                        break;

                    case Keys.O:
                        if (keyboardState.IsKeyDown(Keys.LeftShift) || keyboardState.IsKeyDown(Keys.RightShift))
                            playerName += "O";
                        else
                            playerName += "o";
                        break;

                    case Keys.P:
                        if (keyboardState.IsKeyDown(Keys.LeftShift) || keyboardState.IsKeyDown(Keys.RightShift))
                            playerName += "P";
                        else
                            playerName += "p";
                        break;

                    case Keys.Q:
                        if (keyboardState.IsKeyDown(Keys.LeftShift) || keyboardState.IsKeyDown(Keys.RightShift))
                            playerName += "Q";
                        else
                            playerName += "q";
                        break;

                    case Keys.R:
                        if (keyboardState.IsKeyDown(Keys.LeftShift) || keyboardState.IsKeyDown(Keys.RightShift))
                            playerName += "R";
                        else
                            playerName += "r";
                        break;

                    case Keys.S:
                        if (keyboardState.IsKeyDown(Keys.LeftShift) || keyboardState.IsKeyDown(Keys.RightShift))
                            playerName += "S";
                        else
                            playerName += "s";
                        break;

                    case Keys.T:
                        if (keyboardState.IsKeyDown(Keys.LeftShift) || keyboardState.IsKeyDown(Keys.RightShift))
                            playerName += "T";
                        else
                            playerName += "t";
                        break;

                    case Keys.U:
                        if (keyboardState.IsKeyDown(Keys.LeftShift) || keyboardState.IsKeyDown(Keys.RightShift))
                            playerName += "U";
                        else
                            playerName += "u";
                        break;

                    case Keys.V:
                        if (keyboardState.IsKeyDown(Keys.LeftShift) || keyboardState.IsKeyDown(Keys.RightShift))
                            playerName += "V";
                        else
                            playerName += "v";
                        break;

                    case Keys.W:
                        if (keyboardState.IsKeyDown(Keys.LeftShift) || keyboardState.IsKeyDown(Keys.RightShift))
                            playerName += "W";
                        else
                            playerName += "w";
                        break;

                    case Keys.X:
                        if (keyboardState.IsKeyDown(Keys.LeftShift) || keyboardState.IsKeyDown(Keys.RightShift))
                            playerName += "X";
                        else
                            playerName += "x";
                        break;

                    case Keys.Y:
                        if (keyboardState.IsKeyDown(Keys.LeftShift) || keyboardState.IsKeyDown(Keys.RightShift))
                            playerName += "Y";
                        else
                            playerName += "y";
                        break;

                    case Keys.Z:
                        if (keyboardState.IsKeyDown(Keys.LeftShift) || keyboardState.IsKeyDown(Keys.RightShift))
                            playerName += "Z";
                        else
                            playerName += "z";
                        break;

                    case Keys.Space:
                        playerName += " ";
                        break;

                    case Keys.Enter:
                        gameState = GameState.InGame;
                        if (File.Exists("save.lst"))
                            File.Delete("save.lst");
                        levelIndex = -1;
                        LoadNextLevel();
                        break;

                    case Keys.Back:
                        string temp = null;
                        for (int i = 0; i < playerName.Length - 1; i++)
                        {
                            temp += playerName[i];
                        }
                        playerName = temp;
                        break;

                    case Keys.Escape:
                        gameState = GameState.MainMenu;
                        break;

                }
            }
        }

        private void HandleInput()
        {
            // get all of our input states
            keyboardState = Keyboard.GetState();
            gamePadState = GamePad.GetState(PlayerIndex.One);
            touchState = TouchPanel.GetState();
            accelerometerState = Accelerometer.GetState();
            mouseState = Mouse.GetState();

            // Exit the game when back is pressed.
            if (gamePadState.Buttons.Back == ButtonState.Pressed)
                Exit();

            bool continuePressed =
                keyboardState.IsKeyDown(Keys.Space) ||
                gamePadState.IsButtonDown(Buttons.A) ||
                touchState.AnyTouch();

            // Perform the appropriate action to advance the game and
            // to get the player back to playing.
            if (!wasContinuePressed && continuePressed)
            {
                if (!level.Player.IsAlive)
                {
                    level.StartNewLife();
                }
                else if (level.levelComplete)
                {
                    if (level.ReachedExit)
                    {
                        gameState = GameState.Loading;
                        if (levelIndex < numberOfLevels - 1)
                            SaveGame();
                        LoadNextLevel();
                    }
                    else
                    {
                        gameState = GameState.Loading;
                        ReloadCurrentLevel();
                    }
                }
            }

            wasContinuePressed = continuePressed;
        }

        private void LoadNextLevel()
        {
            // Unloads the content for the current level before loading the next one.
            if (level != null)
            {
                globalScore = level.Score;
                
                level.Dispose();
            }

            // move to the next level
            levelIndex += 1;

            // Load the level.
            string levelPath = string.Format("Content/Levels/{0}.txt", levelIndex);
            if (File.Exists(levelPath))
            {
                using (Stream fileStream = TitleContainer.OpenStream(levelPath))
                    level = new Level(Services, fileStream, levelIndex, globalScore, numberOfLevels);
            }
            else
            {
                if (levelIndex == numberOfLevels)
                {
                    //level.Player.NumLives = 0;
                    if (UpdateHighScoreData())
                        gameState = GameState.HighScore;
                    else
                        gameState = GameState.MainMenu;

                    return;
                }
            }
            gameState = GameState.InGame;
        }

        private void ReloadCurrentLevel()
        {
            --levelIndex;
            LoadNextLevel();
        }

        /// <summary>
        /// Save game data
        /// </summary>
        private void SaveGame()
        {
            if (levelIndex < numberOfLevels)
            {
                if (File.Exists("save.lst"))
                    File.Delete("save.lst");

                SaveGameData data = new SaveGameData(playerName, level.Score, levelIndex, level.Player.NumLives);

                FileStream fs = File.Open("save.lst", FileMode.OpenOrCreate);

                try
                {
                    //Convert the object to XML data and put it in the stream
                    XmlSerializer serializer = new XmlSerializer(typeof(SaveGameData));
                    serializer.Serialize(fs, data);
                }
                finally
                {
                    //Close the file
                    fs.Close();
                }
            }
        }

        /// <summary>
        /// Load save game data
        /// </summary>
        private void LoadSaveGame()
        {
            SaveGameData data;
            FileStream fs;

            //Open the filestream
            if (File.Exists("save.lst"))
            {
                fs = File.Open("save.lst", FileMode.Open, FileAccess.Read);
                int score = 0;

                try
                {
                    //Read the data from the file
                    XmlSerializer serializer = new XmlSerializer(typeof(SaveGameData));
                    data = (SaveGameData)serializer.Deserialize(fs);

                    playerName = data.playerName;
                    score = data.score;
                    levelIndex = data.levelIndex;
                }
                finally
                {
                    //Close the filestream
                    fs.Close();
                    LoadNextLevel();
                    level.Score = score;
                }
            }
        }

        /// <summary>
        /// Draws the game from background to foreground.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        protected override void Draw(GameTime gameTime)
        {
            framecounter++;

            graphics.GraphicsDevice.Clear(Color.Blue);

            if (gameState == GameState.InGame)
            {
                level.Draw(gameTime, spriteBatch);

                DrawHud();
            }

            if (gameState == GameState.MainMenu)
            {
                spriteBatch.Begin();
                spriteBatch.Draw(menuBackground, new Vector2(0, 0), Color.White);
                playButton.Draw(spriteBatch);
                loadButton.Draw(spriteBatch);
                scoreButton.Draw(spriteBatch);
                quitButton.Draw(spriteBatch);
                spriteBatch.End();
            }

            if (gameState == GameState.HighScore)
            {
                spriteBatch.Begin();
                spriteBatch.Draw(scoreBackground, new Vector2(0, 0), Color.White);
                menuButton.Draw(spriteBatch);
                resetButton.Draw(spriteBatch);
                DisplayHighScores(hudFont);
                spriteBatch.End();                
            }

            if (gameState == GameState.EnterName)
            {
                spriteBatch.Begin();
                DrawShadowedString(hudFont, "Enter name: " + playerName, new Vector2(25, 300), Color.Yellow);
                spriteBatch.End();
            }

            base.Draw(gameTime);
        }

        private void DrawHud()
        {
            spriteBatch.Begin();

            Rectangle titleSafeArea = GraphicsDevice.Viewport.TitleSafeArea;
            Vector2 hudLocation = new Vector2(titleSafeArea.X, titleSafeArea.Y);
            Vector2 center = new Vector2(titleSafeArea.X + titleSafeArea.Width / 2.0f,
                                         titleSafeArea.Y + titleSafeArea.Height / 2.0f);

            // Draw time remaining. Uses modulo division to cause blinking when the
            // player is running out of time.
            string timeString = "TIME: " + level.Time.Minutes.ToString("00") + ":" + level.Time.Seconds.ToString("00");
            Color timeColor = Color.Yellow;

            //Draw Time
            DrawShadowedString(hudFont, timeString, hudLocation, timeColor);

            //Draw number of gems
            DrawShadowedString(hudFont, "GEMS: " + level.GemsCollected.ToString() + "/" + level.numGems, hudLocation + new Vector2(0.0f, 20.0f), Color.Yellow);
            // Draw score
            //float timeHeight = hudFont.MeasureString(timeString).Y;
            DrawShadowedString(hudFont, "SCORE: " + level.Score.ToString(), hudLocation + new Vector2(0.0f, 40.0f), Color.Yellow);

            //Draw lives
            DrawShadowedString(hudFont, "Lives: " + level.Player.NumLives.ToString(), hudLocation + new Vector2(0.0f, 60.0f), Color.Yellow);

            //Draw breath
            DrawShadowedString(hudFont, "Breath: " + Math.Round(level.Player.Breath).ToString(), hudLocation + new Vector2(0.0f, 80.0f), Color.Yellow);

           //Draw framerate
            DrawShadowedString(hudFont, "FPS: " + framerate.ToString(), hudLocation + new Vector2(0.0f, 100.0f), Color.Yellow);

            // Determine the status overlay message to show.
            Texture2D status = null;
            if (level.levelComplete)
            {
                if (level.ReachedExit)
                {
                    status = winOverlay;
                }
                else
                {
                    status = loseOverlay;
                }
            }
            else if (!level.Player.IsAlive)
            {
                status = diedOverlay;
            }

            if (status != null)
            {
                // Draw status message.
                Color overlayColor = new Color(255, 255, 255, 200);
                Vector2 statusSize = new Vector2(status.Width, status.Height);
                spriteBatch.Draw(status, center - statusSize / 2, overlayColor);
            }

            spriteBatch.End();
        }

        private void DrawShadowedString(SpriteFont font, string value, Vector2 position, Color color)
        {
            spriteBatch.DrawString(font, value, position + new Vector2(1.0f, 1.0f), Color.Black);
            spriteBatch.DrawString(font, value, position, color);
        }

        private void DisplayHighScores(SpriteFont font)
        {
            highScores = LoadHighScoreData(highScoreFileName);
            for (int i = 0; i < highScores.count; i++)
                DrawShadowedString(font, highScores.playerName[i] + " -- " + highScores.score[i], new Vector2((800 / 2) - 75, 125 + 28 * i ), Color.White);
        }
    }
}
