using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace RealmRacer_LevelEditor
{
    enum LevelType
    {
        Earth = 0,
        Wind = 1,
        Water = 2,
        Fire = 3,
    }

    class Level
    {
        /// <summary>
        /// Gets/Sets the tile array
        /// </summary>
        public Tile[,] TileArray
        {
            get { return tileArray; }
            set { tileArray = value; }
        }
        Tile[,] tileArray;

        /// <summary>
        /// Gets the width of the level
        /// </summary>
        public int Width
        {
            get { return TileArray.GetLength(0) * 40; }
        }

        /// <summary>
        /// Gets the height of the level
        /// </summary>
        public int Height
        {
            get { return TileArray.GetLength(1) * 32; }
        }

        /// <summary>
        /// Gets/Sets the camera position
        /// </summary>
        public Vector2 CameraPosition
        {
            get { return cameraPosition; }
            set { cameraPosition = value; }
        }
        Vector2 cameraPosition;

        float cameraZoom;

        // Level content.        
        public ContentManager Content
        {
            get { return content; }
        }
        ContentManager content;

        //Control
        Keys prevKeyDown;
        Keys[] currentKeyDown;
        int timesPressed;
        KeyboardState keyboardState;
        MouseState mouseState;

        Tile currentSelection;
        LevelType levelType;

        TimeSpan inputTimer;
        
        Texture2D borderTexture;

        /// <summary>
        /// Constructs a level
        /// </summary>
        public Level(IServiceProvider serviceProvider)
        {
            content = new ContentManager(serviceProvider, "Content");
            currentSelection = new Tile(null, '.', new Vector2(0, 0));
            levelType = 0;

            cameraZoom = 1.0f;

            borderTexture = Content.Load<Texture2D>("border");

            //Initialize a blank level
            tileArray = new Tile[100, 30];
            for (int x = 0; x < 100; x++)
                for (int y = 0; y < 30; y++)
                    tileArray[x, y] = new Tile(null, '.', new Vector2(x * 40, y * 32));
        }

        public Level(IServiceProvider serviceProvider, int X, int Y)
        {
            content = new ContentManager(serviceProvider, "Content");
            currentSelection = new Tile(null, '.', new Vector2(0, 0));
            levelType = 0;

            cameraZoom = 1.0f;

            borderTexture = Content.Load<Texture2D>("border");

            //Initialize a blank level
            tileArray = new Tile[X, Y];
            for (int x = 0; x < X; x++)
                for (int y = 0; y < Y; y++)
                    tileArray[x, y] = new Tile(null, '.', new Vector2(x * 40, y * 32));
        }

        public Level(IServiceProvider serviceProvider, Tile[,] array)
        {
            content = new ContentManager(serviceProvider, "Content");
            currentSelection = new Tile(null, '.', new Vector2(0, 0));
            levelType = 0;

            cameraZoom = 1.0f;

            borderTexture = Content.Load<Texture2D>("border");

            //Initializes a new level
            tileArray = array;
        }

        /// <summary>
        /// Updates the level
        /// </summary>
        public void Update(GameTime gameTime, KeyboardState keyboardState, MouseState mouseState)
        {
            this.keyboardState = keyboardState;
            this.mouseState = mouseState;

            inputTimer -= gameTime.ElapsedGameTime;

            if (inputTimer <= TimeSpan.Zero && keyboardState.GetPressedKeys().Length > 0)
            {
                HandleInput();
                inputTimer = TimeSpan.FromSeconds(0.1);
            }

            //Align the current tile selection to the grid
            AlignToGrid(mouseState);

            //Place tile when the mouse is clicked
            HandleClick();
        }

        /// <summary>
        /// Align current tile selection to the grid
        /// </summary>
        private void AlignToGrid(MouseState mouseState)
        {
            /*
            Vector2 position = currentSelection.Position;
            position.X = (int)Math.Round((double)mouseState.X / 40) * 40;
            position.Y = (int)Math.Round((double)mouseState.Y / 32) * 32;
            currentSelection.Position = (position + cameraPosition) / cameraZoom; */

            Vector2 position = new Vector2(mouseState.X, mouseState.Y);
            currentSelection.Position = (position + cameraPosition) / cameraZoom;
        }

        /// <summary>
        /// Handle the keyboard input
        /// </summary>
        private void HandleInput()
        {
            currentKeyDown = keyboardState.GetPressedKeys();
            int length = currentKeyDown.Length;
            if (length > 0)
                prevKeyDown = currentKeyDown[0];

            if (length > 0)
            {
                Vector2 mousePosition = new Vector2(mouseState.X, mouseState.Y);
                switch (currentKeyDown[0])
                {
                    #region Main Tiles

                    //Blank tile
                    case Keys.OemPeriod:
                        currentSelection = new Tile(null, '.', mousePosition);
                        return;

                    //Exit
                    case Keys.X:
                        currentSelection = new Tile(Content.Load<Texture2D>("Sprites/gem"), 'X', mousePosition);
                        return;

                    //Player 1
                    case Keys.D1:
                        currentSelection = new Tile(Content.Load<Texture2D>("Sprites/Player/Idle"), '1', mousePosition); 
                        return;

                    //Gem
                    case Keys.F:
                        if (prevKeyDown == currentKeyDown[0])
                        {
                            if (timesPressed < 3)
                                timesPressed++;

                            if (timesPressed == 3)
                                timesPressed = 0;
                        }
                        else
                            timesPressed = 0;

                        //Regular gem
                        if (timesPressed == 0)
                        {
                            currentSelection = new Tile(Content.Load<Texture2D>("Sprites/fragment"), 'F', mousePosition);
                        }
                        //Gem in quicksand
                        else if (timesPressed == 1)
                        {
                            currentSelection = new Tile(Content.Load<Texture2D>("Sprites/quicksandFragment"), 'f', mousePosition);
                        }
                        //Gem in water
                        else if (timesPressed == 2)
                        {
                            currentSelection = new Tile(Content.Load<Texture2D>("Sprites/underwaterFragment"), 'g', mousePosition);
                        }
                        return;

                    #endregion

                    #region Tiles

                    //Underwater tile
                    case Keys.U:
                        currentSelection = new Tile(Content.Load<Texture2D>("Tiles/Water/underwater"), 'U', mousePosition);
                        return;

                    //Quicksand tile
                    case Keys.Q:
                        currentSelection = new Tile(Content.Load<Texture2D>("Tiles/Earth/quicksand"), 'Q', mousePosition);
                        return;

                    //Cloud tile
                    case Keys.C:
                        currentSelection = new Tile(Content.Load<Texture2D>("Tiles/Wind/windCloud"), 'C', mousePosition);
                        return;

                    //Ice
                    case Keys.I:
                        currentSelection = new Tile(Content.Load<Texture2D>("Tiles/Water/ice"), 'S', mousePosition);
                        return;

                    //Platform
                    case Keys.Subtract:
                        if (levelType == LevelType.Earth)
                            currentSelection = new Tile(Content.Load<Texture2D>("Tiles/Earth/earthPlatform0"), '-', mousePosition);
                        else if (levelType == LevelType.Water)
                            currentSelection = new Tile(Content.Load<Texture2D>("Tiles/Water/waterPlatform0"), '-', mousePosition);
                        else if (levelType == LevelType.Fire)
                            currentSelection = new Tile(Content.Load<Texture2D>("Tiles/Fire/firePlatform0"), '-', mousePosition);
                        else if (levelType == LevelType.Wind)
                            currentSelection = new Tile(Content.Load<Texture2D>("Tiles/Wind/windPlatform0"), '-', mousePosition);
                        return;

                    //Impassable block
                    case Keys.D3:
                        if (levelType == LevelType.Earth)
                            currentSelection = new Tile(Content.Load<Texture2D>("Tiles/Earth/earthBlock0"), '#', mousePosition);
                        else if (levelType == LevelType.Water)
                            currentSelection = new Tile(Content.Load<Texture2D>("Tiles/Water/waterBlock0"), '#', mousePosition);
                        else if (levelType == LevelType.Fire)
                            currentSelection = new Tile(Content.Load<Texture2D>("Tiles/Fire/fireBlock0"), '#', mousePosition);
                        else if (levelType == LevelType.Wind)
                            currentSelection = new Tile(Content.Load<Texture2D>("Tiles/Wind/windBlock0"), '#', mousePosition);
                        return;

                    #endregion

                    #region Hazards

                    //Stalactites
                    case Keys.V:
                        if (prevKeyDown == currentKeyDown[0])
                        {
                            if (timesPressed < 2)
                                timesPressed++;

                            if (timesPressed == 2)
                                timesPressed = 0;
                        }
                        else
                            timesPressed = 0;

                        //Earth stalactite
                        if (levelType == LevelType.Earth)
                        {
                            //Normal stalactite
                            if (timesPressed == 0)
                            {
                                currentSelection = new Tile(Content.Load<Texture2D>("Sprites/Hazards/Earth/stalagtite0"), 'V', mousePosition);
                            }
                            //Quicksand stalactite
                            else if (timesPressed == 1)
                            {
                                currentSelection = new Tile(Content.Load<Texture2D>("Sprites/Hazards/Earth/quicksandStalagtite0"), 'v', mousePosition);
                            }
                        }
                        //Water stalactite
                        else if (levelType == LevelType.Water)
                        {
                            //Normal ice stalactite
                            if (timesPressed == 0)
                            {
                                currentSelection = new Tile(Content.Load<Texture2D>("Sprites/Hazards/Water/icicleDown0"), 'I', mousePosition);
                            }
                            //Underwater ice stalactite
                            else if (timesPressed == 1)
                            {
                                currentSelection = new Tile(Content.Load<Texture2D>("Sprites/Hazards/Water/underwaterStalactite"), 'O', mousePosition);
                            }
                        }
                        return;

                    //Stalagmites
                    case Keys.D6:
                        if (prevKeyDown == currentKeyDown[0])
                        {
                            if (timesPressed < 2)
                                timesPressed++;

                            if (timesPressed == 2)
                                timesPressed = 0;
                        }
                        else
                            timesPressed = 0;

                        //Earth stalagmite
                        if (levelType == LevelType.Earth)
                        {
                            //Normal stalagmite
                            if (timesPressed == 0)
                            {
                                currentSelection = new Tile(Content.Load<Texture2D>("Sprites/Hazards/Earth/stalagmite0"), '^', mousePosition);
                            }
                            //Quicksand stalagmite
                            else if (timesPressed == 1)
                            {
                                currentSelection = new Tile(Content.Load<Texture2D>("Sprites/Hazards/Earth/quicksandStalagmite0"), '6', mousePosition);
                            }
                        }
                        //Water stalagmite
                        else if (levelType == LevelType.Water)
                        {
                            //Normal ice stalagmite
                            if (timesPressed == 0)
                            {
                                currentSelection = new Tile(Content.Load<Texture2D>("Sprites/Hazards/Water/icicleUp0"), 'i', mousePosition);
                            }
                            //Underwater ice stalagmite
                            else if (timesPressed == 1)
                            {
                                currentSelection = new Tile(Content.Load<Texture2D>("Sprites/Hazards/Water/underwaterStalagmite"), 'o', mousePosition);
                            }
                        }
                        return;

                    //Geysers
                    case Keys.G:
                        //Water geyser
                        if (levelType == LevelType.Water)
                        {
                            currentSelection = new Tile(Content.Load<Texture2D>("Sprites/Hazards/Water/waterGeyserIdle"), 'G', mousePosition);
                        }
                        //Wind geyser
                        else if (levelType == LevelType.Wind)
                        {
                            currentSelection = new Tile(Content.Load<Texture2D>("Sprites/Hazards/Wind/windGeyserIdle"), 'J', mousePosition);
                        }
                        //Fire geyser
                        else if (levelType == LevelType.Fire)
                        {
                            currentSelection = new Tile(Content.Load<Texture2D>("Sprites/Hazards/Fire/fireGeyserIdle"), 'R', mousePosition);
                        }
                        return;

                    //Fireball
                    case Keys.P:
                        if (prevKeyDown == currentKeyDown[0])
                        {
                            if (timesPressed < 4)
                                timesPressed++;

                            if (timesPressed == 4)
                                timesPressed = 0;
                        }
                        else
                            timesPressed = 0;

                        //Up
                        if (timesPressed == 0)
                        {
                            currentSelection = new Tile(Content.Load<Texture2D>("Sprites/Hazards/Fire/fireballU"), 'B', mousePosition);
                        }
                        //Down
                        else if (timesPressed == 1)
                        {
                            currentSelection = new Tile(Content.Load<Texture2D>("Sprites/Hazards/Fire/fireballD"), 'b', mousePosition);
                        }
                        //Left
                        else if (timesPressed == 2)
                        {
                            currentSelection = new Tile(Content.Load<Texture2D>("Sprites/Hazards/Fire/fireballL"), 'P', mousePosition);
                        }
                        //Right
                        else if (timesPressed == 3)
                        {
                            currentSelection = new Tile(Content.Load<Texture2D>("Sprites/Hazards/Fire/fireballR"), 'p', mousePosition);
                        }
                        return;

                    //Fire Wall
                    case Keys.W:
                        currentSelection = new Tile(Content.Load<Texture2D>("Sprites/Hazards/Fire/fireWall"), 'W', mousePosition);
                        return;

                    //Tornado
                    case Keys.T:
                        currentSelection = new Tile(Content.Load<Texture2D>("Sprites/Hazards/Wind/tornado"), 'T', mousePosition);
                        return;

                    #endregion

                    //Change level type
                    case Keys.Tab:
                        levelType += 1;
                        if ((int)levelType > 3)
                            levelType = 0;
                        return;

                    default:
                        return;
                }
            }
        }

        private void HandleClick()
        {
            if (mouseState.LeftButton == ButtonState.Pressed)
            {
                int x = (int)Math.Round((double)currentSelection.Position.X / 40);
                int y = (int)Math.Round((double)currentSelection.Position.Y / 32);

                if (x >= 0 && x <= (Width / 40) - 1)
                    if (y >= 0 && y <= (Height / 32) - 1)
                        tileArray[x, y] = currentSelection;
            }

            if (mouseState.RightButton == ButtonState.Pressed)
            {
                int x = (int)Math.Round((double)currentSelection.Position.X / 40);
                int y = (int)Math.Round((double)currentSelection.Position.Y / 32);

                if (x >= 0 && x <= (Width / 40) - 1)
                    if (y >= 0 && y <= (Height / 32) - 1)
                        tileArray[x, y] = new Tile(null, '.', currentSelection.Position);
            }
        }

        /// <summary>
        /// Draws the level
        /// </summary>
        public void Draw(SpriteBatch spriteBatch)
        {
            ScrollCamera(spriteBatch.GraphicsDevice.Viewport, mouseState);
            Matrix cameraTransform = Matrix.CreateScale(cameraZoom, cameraZoom, 1) * Matrix.CreateTranslation(-cameraPosition.X, -cameraPosition.Y, 0.0f);

            spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, SamplerState.LinearClamp, DepthStencilState.Default,
                RasterizerState.CullCounterClockwise, null, cameraTransform);

            /*
            // Calculate the visible range of tiles.
            int left = (int)Math.Floor(cameraPosition.X / 40);
            int right = left + spriteBatch.GraphicsDevice.Viewport.Width / 40;
            right = Math.Min(right + 1, Width / 40);
            int top = (int)Math.Floor(cameraPosition.Y / 32);
            int bottom = top + spriteBatch.GraphicsDevice.Viewport.Width / 32;
            bottom = Math.Min(bottom + 1, Height / 32);*/

            // For each tile position
            for (int y = 0; y < Height / 32; ++y)
            {
                for (int x = 0; x < Width / 40; ++x)
                {
                    // If there is a visible tile in that position
                    Texture2D texture = TileArray[x, y].Texture;
                    if (texture != null)
                    {
                        // Draw it in screen space.
                        Vector2 position = new Vector2(x * 40, y * 32);
                        spriteBatch.Draw(texture, position, Color.White);
                    }
                }
            }

            //Draw the selected tile over the other tiles
            currentSelection.Draw(spriteBatch);

            Rectangle source = new Rectangle(0, 0, borderTexture.Width, borderTexture.Height);
            Vector2 widthScale = new Vector2(Width + 1, 5);
            Vector2 heightScale = new Vector2(5, Height + 1);
            Vector2 origin = new Vector2(0, 0);

            //Left border
            spriteBatch.Draw(borderTexture, new Vector2(0, 0), source, Color.White, 0.0f, origin, heightScale, SpriteEffects.None, 1.0f);
            //Right border
            spriteBatch.Draw(borderTexture, new Vector2(Width, 0), source, Color.White, 0.0f, origin, heightScale, SpriteEffects.None, 1.0f);
            //Top border
            spriteBatch.Draw(borderTexture, new Vector2(0, 0), source, Color.White, 0.0f, origin, widthScale, SpriteEffects.None, 1.0f);
            //Bottom border
            spriteBatch.Draw(borderTexture, new Vector2(0, Height), source, Color.White, 0.0f, origin, widthScale, SpriteEffects.None, 1.0f);

            spriteBatch.End();
        }

        /// <summary>
        /// Scrolls the camera
        /// </summary>
        /// <param name="viewport"></param>
        private void ScrollCamera(Viewport viewport, MouseState mouseState)
        {
            const float ViewMargin = 0.35f;

            // Calculate the edges of the screen.
            float marginWidth = viewport.Width * ViewMargin;
            float marginLeft = cameraPosition.X + marginWidth;
            float marginRight = cameraPosition.X + viewport.Width - marginWidth;
            float marginHeight = viewport.Height * ViewMargin;
            float marginTop = cameraPosition.Y + marginHeight;
            float marginBottom = cameraPosition.Y + viewport.Height - marginHeight;


            // Calculate how far to scroll when the player is near the edges of the screen.
            Vector2 cameraMovement = Vector2.Zero;
            if (keyboardState.IsKeyDown(Keys.Left))
                cameraMovement.X = -5;
            else if (keyboardState.IsKeyDown(Keys.Right))
                cameraMovement.X = 5;
            if (keyboardState.IsKeyDown(Keys.Up))
                cameraMovement.Y = -5;
            else if (keyboardState.IsKeyDown(Keys.Down))
                cameraMovement.Y = 5;

            if (keyboardState.IsKeyDown(Keys.PageUp))
                cameraZoom += 0.01f;
            if (keyboardState.IsKeyDown(Keys.PageDown))
                cameraZoom -= 0.01f;

            // Update the camera position, but prevent scrolling off the ends of the level.
            float maxCameraPositionX = Width - viewport.Width;
            float maxCameraPositionY = Height - viewport.Height;
            cameraPosition.X = MathHelper.Clamp(cameraPosition.X + cameraMovement.X, 0.0f, maxCameraPositionX);
            cameraPosition.Y = MathHelper.Clamp(cameraPosition.Y + cameraMovement.Y, 0.0f, maxCameraPositionY);
        }
    }
}
