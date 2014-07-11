using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Input.Touch;

namespace RealmRacer
{
    /// <summary>
    /// Our fearless adventurer!
    /// </summary>
    class Player
    {
        // Animations
        private Animation idleAnimation;
        private Animation runAnimation;
        private Animation jumpAnimation;
        private Animation celebrateAnimation;
        private Animation dieAnimation;
        private SpriteEffects flip = SpriteEffects.None;
        private AnimationPlayer sprite;

        public Level Level
        {
            get { return level; }
        }
        Level level;
        
        public bool IsAlive
        {
            get { return isAlive; }
        }
        bool isAlive;

        //Number of lives
        public int NumLives
        {
            get { return numLives; }
            set { numLives = value; }
        }
        int numLives;

        //Breath of th player
        public float Breath
        {
            get { return breath; }
            set { breath = value; }
        }
        float breath;

        // Physics state
        public Vector2 Position
        {
            get { return position; }
            set { position = value; }
        }
        Vector2 position;

        private float previousBottom;

        //Player's velocity
        public Vector2 Velocity
        {
            get { return velocity; }
            set { velocity = value; }
        }
        Vector2 velocity;

        //Player's scale
        public Vector2 Scale
        {
            get { return scale; }
            set { scale = value; }
        }
        Vector2 scale;

        //Player's rotation
        public float Rotation
        {
            get { return rotation; }
            set { rotation = value; }
        }
        float rotation;

        //Controls for different tile collision types
        private bool inImpedingSubstance;
        private bool onSlipperySubstance;

        // Constants for controling horizontal movement
        private const float MoveAcceleration = 13000.0f;
        private const float MaxMoveSpeed = 1750.0f;
        public float GroundDragFactor;
        public float AirDragFactor;

        // Constants for controlling vertical movement
        private const float MaxJumpTime = 0.35f;
        private const float JumpLaunchVelocity = -3500.0f;
        private const float GravityAcceleration = 3400.0f;
        private const float JumpControlPower = 0.14f; 
        private float MaxFallSpeed;

        // Input configuration
        private const float MoveStickScale = 1.0f;
        private const float AccelerometerScale = 1.5f;
        private const Buttons JumpButton = Buttons.A;
        private const Buttons ChargeButton = Buttons.X;

        //Controls whether the player is trying to pass through a tile
        private bool passThrough;

        /// <summary>
        /// Gets whether or not the player's feet are on the ground.
        /// </summary>
        public bool IsOnGround
        {
            get { return isOnGround; }
        }
        bool isOnGround;

        /// <summary>
        /// Current user movement input.
        /// </summary>
        private float movement;

        // Jumping state
        private bool isJumping;
        private bool wasJumping;
        private float jumpTime;

        //Charging states
        private bool charge;
        private bool prevCharge;
        private TimeSpan chargeTimer;

        private Rectangle localBounds;
        /// <summary>
        /// Gets a rectangle which bounds this player in world space.
        /// </summary>
        public Rectangle BoundingRectangle
        {
            get
            {
                int left = (int)Math.Round(Position.X - sprite.Origin.X) + localBounds.X;
                int top = (int)Math.Round(Position.Y - sprite.Origin.Y) + localBounds.Y;

                return new Rectangle(left, top, localBounds.Width, localBounds.Height);
            }
        }

        /// <summary>
        /// Constructors a new player.
        /// </summary>
        public Player(Level level, Vector2 position, Vector2 scale, float rotation)
        {
            this.level = level;
            passThrough = false;
            this.scale = scale;
            this.rotation = rotation;
            numLives = 1;

            inImpedingSubstance = false;
            onSlipperySubstance = false;
            GroundDragFactor = 0.48f;
            AirDragFactor = 0.58f;
            MaxFallSpeed = 550.0f;

            charge = false;
            prevCharge = false;
            chargeTimer = TimeSpan.Zero;

            breath = 100.0f;

            LoadContent();

            Reset(position);
        }

        /// <summary>
        /// Loads the player sprite sheet and sounds.
        /// </summary>
        public void LoadContent()
        {
            // Load animated textures.
            idleAnimation = new Animation(Level.Content.Load<Texture2D>("Sprites/Player/Idle"), 0.1f, true, 64, 64);
            runAnimation = new Animation(Level.Content.Load<Texture2D>("Sprites/Player/Run"), 0.025f, true, 64, 64);
            jumpAnimation = new Animation(Level.Content.Load<Texture2D>("Sprites/Player/Jump"), 0.1f, false, 64, 64);
            celebrateAnimation = new Animation(Level.Content.Load<Texture2D>("Sprites/Player/Celebrate"), 0.1f, false, 64, 64);
            dieAnimation = new Animation(Level.Content.Load<Texture2D>("Sprites/Player/Die"), 0.1f, false, 64, 64);

            // Calculate bounds within texture size.            
            //int width = (int)(idleAnimation.FrameWidth * 0.4);
            //int left = (idleAnimation.FrameWidth - width) / 2;
            //int height = (int)(idleAnimation.FrameWidth * 0.8);
            int width = (int)(idleAnimation.FrameWidth * 0.8 * Scale.X);
            int left = (idleAnimation.FrameWidth - width) / 2;
            int height = (int)(idleAnimation.FrameHeight * 0.8 * Scale.Y);
            int top = idleAnimation.FrameHeight - height;
            localBounds = new Rectangle(left, top, width, height);
        }

        /// <summary>
        /// Resets the player to life.
        /// </summary>
        /// <param name="position">The position to come to life at.</param>
        public void Reset(Vector2 position)
        {
            Position = position;
            Velocity = Vector2.Zero;
            isAlive = true;
            sprite.PlayAnimation(idleAnimation);
            breath = 100.0f;
        }

        /// <summary>
        /// Handles input, performs physics, and animates the player sprite.
        /// </summary>
        /// <remarks>
        /// We pass in all of the input states so that our game is only polling the hardware
        /// once per frame. We also pass the game's orientation because when using the accelerometer,
        /// we need to reverse our motion when the orientation is in the LandscapeRight orientation.
        /// </remarks>
        public void Update(
            GameTime gameTime, 
            KeyboardState keyboardState, 
            GamePadState gamePadState, 
            TouchCollection touchState, 
            AccelerometerState accelState,
            DisplayOrientation orientation)
        {
            //Get the input from the input devices
            GetInput(keyboardState, gamePadState, touchState, accelState, orientation);

            //Control the timer for charging
            chargeTimer -= gameTime.ElapsedGameTime;
            if (chargeTimer <= TimeSpan.Zero)
                charge = false;

            //Apply the games physics to the player
            ApplyPhysics(gameTime);

            //Control player's breath
            if (inImpedingSubstance)
                breath -= 0.05f;
            else if (!inImpedingSubstance)
                breath += 0.05f;

            if (breath > 100.0f)
                breath = 100.0f;
            else if (breath < 0.0f)
                breath = 0.0f;

            if (breath == 0)
                OnKilled();

            if (IsAlive && IsOnGround)
            {
                if (Math.Abs(Velocity.X) - 0.02f > 0)
                {
                    sprite.PlayAnimation(runAnimation);
                }
                else
                {
                    sprite.PlayAnimation(idleAnimation);
                }
            }

            // Clear input.
            isJumping = false;
        }

        /// <summary>
        /// Gets player horizontal movement and jump commands from input.
        /// </summary>
        private void GetInput(
            KeyboardState keyboardState, 
            GamePadState gamePadState, 
            TouchCollection touchState,
            AccelerometerState accelState, 
            DisplayOrientation orientation)
        {
            // Get analog horizontal movement.
            if (gamePadState.ThumbSticks.Left.X * MoveStickScale != 0)
                movement = gamePadState.ThumbSticks.Left.X * MoveStickScale;

            // Ignore small movements to prevent running in place.
            if (Math.Abs(movement) < 0.1f)
                movement = 0.0f;

            // Move the player with accelerometer
            if (Math.Abs(accelState.Acceleration.Y) > 0.10f)
            {
                // set our movement speed
                movement = MathHelper.Clamp(-accelState.Acceleration.Y * AccelerometerScale, -1f, 1f);

                // if we're in the LandscapeLeft orientation, we must reverse our movement
                if (orientation == DisplayOrientation.LandscapeRight)
                    movement = -movement;
            }

            // If any digital horizontal movement input is found, override the analog movement.
            if (gamePadState.IsButtonDown(Buttons.DPadLeft) ||
                keyboardState.IsKeyDown(Keys.Left))
            {
                movement = -1.0f;
            }
            else if (gamePadState.IsButtonDown(Buttons.DPadRight) ||
                     keyboardState.IsKeyDown(Keys.Right))
            {
                movement = 1.0f;
            }

            // Check if the player wants to jump.
            isJumping =
                gamePadState.IsButtonDown(JumpButton) ||
                keyboardState.IsKeyDown(Keys.Up) ||
                (touchState.AnyTouch() && touchState.Count < 2);

            //Make the player charge
            if (!charge && (gamePadState.IsButtonDown(ChargeButton) ||
                keyboardState.IsKeyDown(Keys.Z) || (touchState.AnyTouch() && touchState.Count > 1)) &&
                !prevCharge && !(level.GetCollision((int)Position.X / Tile.Width, (int)Position.Y / Tile.Height) == TileCollision.Slippery))
            {
                charge = true;
                chargeTimer = TimeSpan.FromSeconds(0.25);
            }
            prevCharge = gamePadState.IsButtonDown(ChargeButton) ||
                keyboardState.IsKeyDown(Keys.Z) || (touchState.AnyTouch() && touchState.Count > 1);

            //Check if the player wants to pass through a platform
            if (gamePadState.ThumbSticks.Left.Y > 0.5 || keyboardState.IsKeyDown(Keys.Down) ||
                keyboardState.IsKeyDown(Keys.S))
            {
                passThrough = true;
            }
            else
                passThrough = false;
        }

        /// <summary>
        /// Updates the player's velocity and position based on input, gravity, etc.
        /// </summary>
        public void ApplyPhysics(GameTime gameTime)
        {
            float elapsed = (float)gameTime.ElapsedGameTime.TotalSeconds;

            Vector2 previousPosition = Position;

            if (inImpedingSubstance)
            {
                AirDragFactor = 0.25f;
                GroundDragFactor = 0.25f;
                MaxFallSpeed = 150.0f;
            }
            else if (onSlipperySubstance)
            {
                AirDragFactor = 0.58f;
                GroundDragFactor = 0.58f;
                MaxFallSpeed = 550.0f;
            }
            else
            {
                AirDragFactor = 0.58f;
                GroundDragFactor = 0.48f;
                MaxFallSpeed = 550.0f;
            }

            // Base velocity is a combination of horizontal movement control and
            // acceleration downward due to gravity.
            velocity.X += movement * MoveAcceleration * 1.1f * elapsed;
            velocity.Y = MathHelper.Clamp(velocity.Y + GravityAcceleration * elapsed, -550.0f, MaxFallSpeed);

            if (charge)
                velocity.X += movement * MoveAcceleration * 2.0f * elapsed;

            velocity.Y = DoJump(velocity.Y, gameTime);

            // Apply pseudo-drag horizontally.
            if (IsOnGround)
                velocity.X *= GroundDragFactor;
            else
                velocity.X *= AirDragFactor;

            // Prevent the player from running faster than his top speed.            
            velocity.X = MathHelper.Clamp(velocity.X, -MaxMoveSpeed, MaxMoveSpeed);

            // Apply velocity.
            Position += velocity * elapsed;
            Position = new Vector2((float)Math.Round(Position.X), (float)Math.Round(Position.Y));

            // If the player is now colliding with the level, separate them.
            HandleCollisions(gameTime);

            // If the collision stopped us from moving, reset the velocity to zero.
            if (Position.X == previousPosition.X)
                velocity.X = 0;

            if (Position.Y == previousPosition.Y)
                velocity.Y = 0;

            //Lower movement
            if (!onSlipperySubstance)
            {
                if (isOnGround)
                    movement = 0.0f;
                else
                    if (movement > 0)
                        movement -= 0.01f;
                    else
                        movement += 0.01f;
            }
               
        }

        /// <summary>
        /// Calculates the Y velocity accounting for jumping and
        /// animates accordingly.
        /// </summary>
        /// <remarks>
        /// During the accent of a jump, the Y velocity is completely
        /// overridden by a power curve. During the decent, gravity takes
        /// over. The jump velocity is controlled by the jumpTime field
        /// which measures time into the accent of the current jump.
        /// </remarks>
        /// <param name="velocityY">
        /// The player's current velocity along the Y axis.
        /// </param>
        /// <returns>
        /// A new Y velocity if beginning or continuing a jump.
        /// Otherwise, the existing Y velocity.
        /// </returns>
        private float DoJump(float velocityY, GameTime gameTime)
        {
            // If the player wants to jump
            if (isJumping)
            {
                if (!inImpedingSubstance)
                {
                    // Begin or continue a jump
                    if ((!wasJumping && IsOnGround) || jumpTime > 0.0f)
                    {
                        jumpTime += (float)gameTime.ElapsedGameTime.TotalSeconds;
                        sprite.PlayAnimation(jumpAnimation);
                    }

                    // If we are in the ascent of the jump
                    if (0.0f < jumpTime && jumpTime <= MaxJumpTime)
                    {
                        // Fully override the vertical velocity with a power curve that gives players more control over the top of the jump
                        velocityY = JumpLaunchVelocity * (1.0f - (float)Math.Pow(jumpTime / MaxJumpTime, JumpControlPower));
                    }
                    else
                    {
                        // Reached the apex of the jump
                        jumpTime = 0.0f;
                    }
                }
                else if (inImpedingSubstance)
                {
                    // Begin or continue a jump
                    if ((!wasJumping) || jumpTime > 0.0f)
                    {
                        jumpTime += (float)gameTime.ElapsedGameTime.TotalSeconds;
                        sprite.PlayAnimation(jumpAnimation);
                    }

                    // If we are in the ascent of the jump
                    if (0.0f < jumpTime && jumpTime <= MaxJumpTime)
                    {
                        // Fully override the vertical velocity with a power curve that gives players more control over the top of the jump
                        velocityY = JumpLaunchVelocity * (1.0f - (float)Math.Pow(jumpTime / MaxJumpTime, JumpControlPower));
                    }
                    else
                    {
                        // Reached the apex of the jump
                        jumpTime = 0.0f;
                    }
                }
            }
            else
            {
                // Continues not jumping or cancels a jump in progress
                jumpTime = 0.0f;
            }
            wasJumping = isJumping;
            
            return velocityY;
        }

        /// <summary>
        /// Detects and resolves all collisions between the player and his neighboring
        /// tiles. When a collision is detected, the player is pushed away along one
        /// axis to prevent overlapping. There is some special logic for the Y axis to
        /// handle platforms which behave differently depending on direction of movement.
        /// </summary>
        private void HandleCollisions(GameTime gameTime)
        {
            // Get the player's bounding rectangle and find neighboring tiles.
            Rectangle bounds = BoundingRectangle;
            int leftTile = (int)Math.Floor((float)bounds.Left / Tile.Width);
            int rightTile = (int)Math.Ceiling(((float)bounds.Right / Tile.Width)) - 1;
            int topTile = (int)Math.Floor((float)bounds.Top / Tile.Height);
            int bottomTile = (int)Math.Ceiling(((float)bounds.Bottom / Tile.Height)) - 1;

            // Reset flag to search for ground collision.
            isOnGround = false;

            onSlipperySubstance = false;

            // For each potentially colliding tile,
            for (int y = topTile; y <= bottomTile; ++y)
            {
                for (int x = leftTile; x <= rightTile; ++x)
                {
                    // If this tile is collidable,
                    TileCollision collision = Level.GetCollision(x, y);
                    if (collision != TileCollision.Passable)
                    {
                        // Determine collision depth (with direction) and magnitude.
                        Rectangle tileBounds = Level.GetBounds(x, y);
                        Vector2 depth = RectangleExtensions.GetIntersectionDepth(bounds, tileBounds);
                        if (depth != Vector2.Zero)
                        {
                            jumpTime = 0.0f;

                            float absDepthX = Math.Abs(depth.X);
                            float absDepthY = Math.Abs(depth.Y);

                            // Resolve the collision along the shallow axis.
                            if (absDepthY < absDepthX || (collision == TileCollision.Platform))
                            {
                                // If we crossed the top of a tile, we are on the ground.
                                if (previousBottom <= tileBounds.Top)
                                    isOnGround = true;

                                if ((collision == TileCollision.Platform) && passThrough)
                                {
                                    GroundDragFactor = 0.48f;
                                    AirDragFactor = 0.58f;
                                    isOnGround = false;
                                }
                                //Resolve collisions with slippery tiles like ice
                                else if (collision == TileCollision.Slippery)
                                {
                                    onSlipperySubstance = true;
                                    GroundDragFactor = 0.48f;
                                    AirDragFactor = 0.58f;

                                    // Resolve the collision along the Y axis.
                                    Position = new Vector2(Position.X, Position.Y + depth.Y);

                                    // Perform further collisions with the new bounds.
                                    bounds = BoundingRectangle;
                                }
                                //Resolve collisions with unstable tiles
                                else if (collision == TileCollision.Unstable)
                                {
                                    if (Level.tiles[x, y].destroyed == false)
                                    {
                                        GroundDragFactor = 0.48f;
                                        AirDragFactor = 0.58f;

                                        // Resolve the collision along the Y axis.
                                        Position = new Vector2(Position.X, Position.Y + depth.Y);

                                        // Perform further collisions with the new bounds.
                                        bounds = BoundingRectangle;

                                        bool timerPresent = false;
                                        for (int i = 0; i < Level.tileTimers.Count; i++)
                                        {
                                            if ((Level.tileTimers[i].X == x) && (Level.tileTimers[i].Y == y))
                                            {
                                                timerPresent = true;
                                            }
                                        }
                                        if (!timerPresent)
                                        {
                                            Level.tileTimers.Add(new TileTimer(Level, x, y, 0.5, 1.0));
                                        }
                                    }
                                }
                                //Resolve collisions with impeding tiles
                                else if (collision == TileCollision.Impeding)
                                {
                                    inImpedingSubstance = true;
                                }
                                // Ignore platforms, unless we are on the ground.
                                else if (collision == TileCollision.Impassable || IsOnGround)
                                {
                                    GroundDragFactor = 0.48f;
                                    AirDragFactor = 0.58f;

                                    // Resolve the collision along the Y axis.
                                    Position = new Vector2(Position.X, Position.Y + depth.Y);

                                    // Perform further collisions with the new bounds.
                                    bounds = BoundingRectangle;
                                }
                            }
                            else if (collision == TileCollision.Impassable || collision == TileCollision.Slippery) // Ignore platforms.
                            {
                                GroundDragFactor = 0.48f;
                                AirDragFactor = 0.58f;
                                movement = 0;

                                if (!(Position.Y < y * Tile.Height + 2)) //Used to prevent sticking to ground tile while in impeding tiles.
                                {
                                    // Resolve the collision along the X axis.
                                    Position = new Vector2(Position.X + depth.X, Position.Y);
                                }
                                
                                // Perform further collisions with the new bounds.
                                bounds = BoundingRectangle;
                            }
                        }
                    }
                    else
                    {
                        inImpedingSubstance = false;
                    }
                }
            }

            // Save the new bounds bottom.
            previousBottom = bounds.Bottom;
        }

        /// <summary>
        /// Called when the player has been killed.
        /// </summary>
        /// <param name="killedBy">
        /// The enemy who killed the player. This parameter is null if the player was
        /// not killed by an enemy (fell into a hole).
        /// </param>
        public void OnKilled(Enemy killedBy)
        {
            if (isAlive)
            {
                isAlive = false;

                sprite.PlayAnimation(dieAnimation);

                NumLives += 1;
            }
        }

        public void OnKilled(Hazard killedBy)
        {
            if (isAlive)
            {
                isAlive = false;

                sprite.PlayAnimation(dieAnimation);

                NumLives += 1;
            }
        }

        public void OnKilled(Geyser killedBy)
        {
            if (isAlive)
            {
                isAlive = false;

                sprite.PlayAnimation(dieAnimation);

                NumLives += 1;
            }
        }

        public void OnKilled()
        {
            if (isAlive)
            {
                isAlive = false;

                sprite.PlayAnimation(dieAnimation);

                NumLives += 1;
            }
        }

        /// <summary>
        /// Called when this player reaches the level's exit.
        /// </summary>
        public void OnReachedExit()
        {
            sprite.PlayAnimation(celebrateAnimation);
        }

        /// <summary>
        /// Draws the animated player.
        /// </summary>
        public void Draw(GameTime gameTime, SpriteBatch spriteBatch)
        {
            // Flip the sprite to face the way we are moving.
            if (Velocity.X > 0)
                flip = SpriteEffects.None;
            else if (Velocity.X < 0)
                flip = SpriteEffects.FlipHorizontally;

            // Draw that sprite.
            sprite.Draw(gameTime, spriteBatch, Position, flip, Scale, rotation);
        }
    }
}
