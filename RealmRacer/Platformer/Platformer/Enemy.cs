using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace RealmRacer
{
    /// <summary>
    /// Facing direction along the X axis.
    /// </summary>
    enum FaceDirection
    {
        Left = -1,
        Right = 1,
    }

    /// <summary>
    /// A monster who is impeding the progress of our fearless adventurer.
    /// </summary>
    class Enemy
    {
        public Level Level
        {
            get { return level; }
        }
        Level level;

        /// <summary>
        /// Position in world space of the bottom center of this enemy.
        /// </summary>
        public Vector2 Position
        {
            get { return position; }
        }
        Vector2 position;

        private Rectangle localBounds;

        public Vector2 Scale
        {
            get { return scale; }
            set { scale = value; }
        }
        Vector2 scale;

        public float Rotation
        {
            get { return rotation; }
            set { rotation = value; }
        }
        float rotation;

        //Used to determine if the enemy is a normal enemy or a vortex
        public bool vortex;

        /// <summary>
        /// Gets a rectangle which bounds this enemy in world space.
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

        // Animations
        private Animation runAnimation;
        private Animation idleAnimation;
        private AnimationPlayer sprite;

        /// <summary>
        /// The direction this enemy is facing and moving along the X axis.
        /// </summary>
        private FaceDirection direction = FaceDirection.Left;

        /// <summary>
        /// How long this enemy has been waiting before turning around.
        /// </summary>
        private float waitTime;

        /// <summary>
        /// How long to wait before turning around.
        /// </summary>
        private const float MaxWaitTime = 0.5f;

        /// <summary>
        /// The speed at which this enemy moves along the X axis.
        /// </summary>
        private const float MoveSpeed = 64.0f;

        /// <summary>
        /// Constructs a new Enemy.
        /// </summary>
        public Enemy(Level level, Vector2 position, string spriteSet, Vector2 scale, float rotation, bool vortex)
        {
            this.level = level;
            this.position = position;
            this.scale = scale;
            this.rotation = rotation;
            this.vortex = vortex;

            if (!vortex)
                LoadContent(spriteSet);
            else if (vortex)
            {
                //Load animation
                runAnimation = new Animation(Level.Content.Load<Texture2D>("Sprites/Hazards/" + spriteSet), 0.1f, true, 32, 64);
                idleAnimation = new Animation(Level.Content.Load<Texture2D>("Sprites/Hazards/" + spriteSet), 0.15f, true, 32, 64);

                // Calculate bounds within texture size.
                int width = (int)(idleAnimation.FrameWidth * 0.8);
                int left = (idleAnimation.FrameWidth - width) / 2;
                int height = (int)(idleAnimation.FrameWidth * 0.9);
                int top = idleAnimation.FrameHeight - height;
                localBounds = new Rectangle(left, top, width, height);
                sprite.PlayAnimation(idleAnimation);
            }
        }

        /// <summary>
        /// Loads a particular enemy sprite sheet and sounds.
        /// </summary>
        public void LoadContent(string spriteSet)
        {
            // Load animations.
            spriteSet = "Sprites/" + spriteSet + "/";
            runAnimation = new Animation(Level.Content.Load<Texture2D>(spriteSet + "Run"), 0.1f, true, 64, 64);
            idleAnimation = new Animation(Level.Content.Load<Texture2D>(spriteSet + "Idle"), 0.15f, true, 64, 64);
            sprite.PlayAnimation(idleAnimation);

            // Calculate bounds within texture size.
            int width = (int)(idleAnimation.FrameWidth * 0.35);
            int left = (idleAnimation.FrameWidth - width) / 2;
            int height = (int)(idleAnimation.FrameWidth * 0.7);
            int top = idleAnimation.FrameHeight - height;
            localBounds = new Rectangle(left, top, width, height);
        }


        /// <summary>
        /// Paces back and forth along a platform, waiting at either end.
        /// </summary>
        public void Update(GameTime gameTime)
        {
            float elapsed = (float)gameTime.ElapsedGameTime.TotalSeconds;

            // Calculate tile position based on the side we are walking towards.
            float posX = Position.X + localBounds.Width / 2 * (int)direction;
            int tileX = (int)Math.Floor(posX / Tile.Width) - (int)direction;
            int tileY = (int)Math.Floor(Position.Y / Tile.Height);

            if (waitTime > 0)
            {
                // Wait for some amount of time.
                waitTime = Math.Max(0.0f, waitTime - (float)gameTime.ElapsedGameTime.TotalSeconds);
                if (waitTime <= 0.0f)
                {
                    // Then turn around.
                    direction = (FaceDirection)(-(int)direction);
                }
            }
            else
            {
                // If we are about to run into a wall or off a cliff, start waiting.
                if (Level.GetCollision(tileX + (int)direction, tileY - 1) == TileCollision.Impassable || 
                    Level.GetCollision(tileX + (int)direction, tileY - 2) == TileCollision.Impassable ||
                    Level.GetCollision(tileX + (int)direction, tileY) == TileCollision.Passable)
                {
                    waitTime = MaxWaitTime;
                }
                else
                {
                    // Move in the current direction.
                    Vector2 velocity = new Vector2((int)direction * MoveSpeed * elapsed, 0.0f);
                    position = position + velocity;
                }
            }
        }

        /// <summary>
        /// If the enemy is a vortex, update its sucking effect
        /// </summary>
        /// <param name="gameTime"></param>
        public void UpdateVortex(GameTime gameTime)
        {
            float dX = Level.Player.Position.X - Position.X;
            float dY = Level.Player.Position.Y - Position.Y;
            float distance = (float)Math.Sqrt(dX * dX + dY * dY);

            if (distance <= 120.0f && distance >= 20)
            {
                if (!CheckPath())
                    Level.Player.Velocity -= new Vector2(dX * distance, dY * distance);
            }
        }

        public bool CheckPath()
        {
            bool blocked = false;

            int xP = (int)Math.Floor(Level.Player.Position.X / Tile.Width);
            int yP = (int)Math.Floor(Level.Player.Position.Y / Tile.Height);
            int xV = (int)Math.Floor(Position.X / Tile.Width);
            int yV = (int)Math.Floor(Position.Y / Tile.Height);

            if (!((xP == xV) && (yP == yV)))
            {
                for (int i = 0; i < 3; i++)
                {
                    //Player is above the vortex
                    if (yP < yV)
                    {
                        if (Level.Height - 1 >= yP + i)
                            if (Level.tiles[xP, yP + i].Collision == TileCollision.Impassable)
                                blocked = true;
                    } //Player is on the same level as the vortex
                    else if (yP == yV)
                    {
                        //Left
                        if (xP < xV)
                        {
                            if (Level.Width - 1 >= xP + i)
                                if (Level.tiles[xP + i, yP - 1].Collision == TileCollision.Impassable)
                                    blocked = true;
                        }
                        else if (xP > xV)
                        {
                            if (xP - i > -1)
                                if (Level.tiles[xP - i, yP - 1].Collision == TileCollision.Impassable)
                                    blocked = true;
                        }
                    }
                }
            }
            return blocked;
        }

        /// <summary>
        /// Draws the animated enemy.
        /// </summary>
        public void Draw(GameTime gameTime, SpriteBatch spriteBatch)
        {
            // Stop running when the game is paused or before turning around.
            if (!Level.Player.IsAlive ||
                Level.ReachedExit ||
                waitTime > 0)
            {
                sprite.PlayAnimation(idleAnimation);
            }
            else
            {
                sprite.PlayAnimation(runAnimation);
            }


            // Draw facing the way the enemy is moving.
            SpriteEffects flip = direction > 0 ? SpriteEffects.FlipHorizontally : SpriteEffects.None;
            sprite.Draw(gameTime, spriteBatch, Position, flip, Scale, Rotation);
        }
    }
}
