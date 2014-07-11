using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Audio;

namespace RealmRacer
{
    /// <summary>
    /// Deadly deadly hazards
    /// </summary>
    class Hazard
    {
        private Animation animation;
        public AnimationPlayer sprite;
        public Texture2D texture;
        public Vector2 origin;
        
        public Level Level
        {
            get { return level; }
        }
        Level level;

        /// <summary>
        /// Gets the position of the hazard
        /// </summary>
        public Vector2 Position
        {
            get { return position; }
            set { position = value; }
        }
        Vector2 position;

        /// <summary>
        /// Gets the bounding rectangle of the hazard
        /// </summary>
        public Rectangle BoundingRect
        {
            get 
            {
                int x = (int)Position.X - (int)origin.X;
                int y = (int)Position.Y - (int)origin.Y;
                int width = sprite.Animation.FrameWidth;
                int height = sprite.Animation.FrameHeight;

                return new Rectangle(x, y, width, height);
            }
        }

        /// <summary>
        /// Constructs a new hazard.
        /// </summary>
        public Hazard(Level level, Vector2 position, string texture, int frameWidth, int frameHeight, string soundeffect)
        {
            this.level = level;
            this.position = position;

            LoadContent(texture, frameWidth, frameHeight, soundeffect);
        }

        /// <summary>
        /// Loads the content for the hazard
        /// </summary>
        /// <param name="filename"></param>
        public void LoadContent(string texture, int frameWidth, int frameHeight, string soundeffect)
        {
            this.texture = Level.Content.Load<Texture2D>(texture);
            animation = new Animation(this.texture, 0.1f, true, frameWidth, frameHeight);
            origin = new Vector2(this.animation.FrameWidth / 2, this.animation.FrameHeight / 2);
            sprite.PlayAnimation(animation);
        }

        public void Draw(GameTime gameTime, SpriteBatch spriteBatch)
        {
            sprite.Draw(gameTime, spriteBatch, Position, origin, SpriteEffects.None, new Vector2(1, 1), 0.0f);
        }
    }
}
