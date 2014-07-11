using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Audio;

namespace RealmRacer
{
    /// <summary>
    /// A valuable item the player can collect.
    /// </summary>
    class Fragment
    {
        private Texture2D texture;
        private Vector2 origin;

        public const int PointValue = 30;
        public readonly Color Color = Color.Yellow;

        // The gem is animated from a base position along the Y axis.
        private Vector2 basePosition;
        private float bounce;


        //Used to make gems fly to the exit gem
        public Vector2 velocity;
        public Vector2 endPosition;
        public bool collected;

        public Level Level
        {
            get { return level; }
        }
        Level level;

        /// <summary>
        /// Gets the current position of this gem in world space.
        /// </summary>
        public Vector2 Position
        {
            get
            {
                return basePosition + new Vector2(0.0f, bounce);
            }
        }

        /// <summary>
        /// Gets a circle which bounds this gem in world space.
        /// </summary>
        public Circle BoundingCircle
        {
            get
            {
                return new Circle(Position, Tile.Width / 3.0f);
            }
        }

        /// <summary>
        /// Constructs a new gem.
        /// </summary>
        public Fragment(Level level, Vector2 position)
        {
            this.level = level;
            this.basePosition = position;

            velocity = new Vector2(0, 0);
            endPosition = new Vector2(-1, -1);

            collected = false;

            LoadContent();
        }

        /// <summary>
        /// Loads the gem texture and collected sound.
        /// </summary>
        public void LoadContent()
        {
            texture = Level.Content.Load<Texture2D>("Sprites/Fragment");
            origin = new Vector2(texture.Width / 2.0f, texture.Height / 2.0f);
        }

        /// <summary>
        /// Bounces up and down in the air to entice players to collect them.
        /// </summary>
        public void Update(GameTime gameTime)
        {
            /*
            if (!collected)
            {
                // Bounce control constants
                const float BounceHeight = 0.18f;
                const float BounceRate = 3.0f;
                const float BounceSync = -0.75f;

                // Bounce along a sine curve over time.
                // Include the X coordinate so that neighboring gems bounce in a nice wave pattern.            
                double t = gameTime.TotalGameTime.TotalSeconds * BounceRate + Position.X * BounceSync;
                bounce = (float)Math.Sin(t) * BounceHeight * texture.Height;
            }
            */

            basePosition.X += velocity.X * (float)gameTime.ElapsedGameTime.TotalSeconds;
            basePosition.Y += velocity.Y * (float)gameTime.ElapsedGameTime.TotalSeconds;
        }

        /// <summary>
        /// Called when this gem has been collected by a player and removed from the level.
        /// </summary>
        /// <param name="collectedBy">
        /// The player who collected this gem. Although currently not used, this parameter would be
        /// useful for creating special powerup gems. For example, a gem could make the player invincible.
        /// </param>
        public void OnCollected(Player collectedBy)
        {
            collected = true;
        }

        /// <summary>
        /// Draws a gem in the appropriate color.
        /// </summary>
        public void Draw(GameTime gameTime, SpriteBatch spriteBatch)
        {
            spriteBatch.Draw(texture, Position, null, Color, 0.0f, origin, 1.0f, SpriteEffects.None, 0.0f);
        }
    }
}
