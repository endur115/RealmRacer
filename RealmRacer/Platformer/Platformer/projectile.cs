using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace RealmRacer
{
    class Projectile
    {
        public Level Level
        {
            get { return level; }
        }
        Level level;

        /// <summary>
        /// Gets or sets the velocity of the projectile
        /// </summary>
        public Vector2 Position
        {
            get { return position; }
            set { position = value; }
        }
        Vector2 position;

        /// <summary>
        /// Gets or sets the velocity of the projectile
        /// </summary>
        public Vector2 Velocity
        {
            get { return velocity; }
            set { velocity = value; }
        }
        Vector2 velocity;

        private Rectangle localBounds;
        /// <summary>
        /// Gets the bounding rectangle of the position
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

        //Animations
        private Animation travelAnimation;
        private AnimationPlayer sprite;

        bool gravity;

        /// <summary>
        /// Constructs a new projectile
        /// </summary>
        public Projectile(Level level, Vector2 position, Vector2 velocity, Texture2D texture, bool affectedByGravity, int frameWidth, int frameHeight)
        {
            this.level = level;
            this.position = position;
            this.velocity = velocity;
            gravity = affectedByGravity;

            travelAnimation = new Animation(texture, 0.1f, true, frameWidth, frameHeight);
            sprite.PlayAnimation(travelAnimation);

            // Calculate bounds within texture size.
            int width = (int)(travelAnimation.FrameWidth * 0.35);
            int left = (travelAnimation.FrameWidth - width) / 2;
            int height = (int)(travelAnimation.FrameWidth * 0.7);
            int top = travelAnimation.FrameHeight - height;
            localBounds = new Rectangle(left, top, width, height);
        }

        /// <summary>
        /// Updates the projectile
        /// </summary>
        public void Update(GameTime gameTime)
        {
            float elapsed = (float)gameTime.ElapsedGameTime.TotalSeconds;

            if (gravity)
            {
                velocity.Y += 3400.0f * elapsed;
            }

            Position += velocity * elapsed;
            Position = new Vector2((float)Math.Round(Position.X), (float)Math.Round(Position.Y));
        }

        /// <summary>
        /// Draws the projectile
        /// </summary>
        public void Draw(GameTime gameTime, SpriteBatch spriteBatch)
        {
            sprite.Draw(gameTime, spriteBatch, Position, SpriteEffects.None, new Vector2(1, 1), 0.0f);
        }
    }
}
