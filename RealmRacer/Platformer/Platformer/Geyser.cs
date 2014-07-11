using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace RealmRacer
{
    enum GeyserType
    {
        Wind = 0,
        Water = 1,
        Fire = 2,
    }

    class Geyser
    {
        public Level Level
        {
            get { return level; }
        }
        Level level;

        public Vector2 Position
        {
            get { return position; }
        }
        Vector2 position;

        public float Rotation
        {
            get { return rotation; }
        }
        private float rotation;

        private Rectangle localBounds;
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
        private Animation idle;
        private Animation shoot;
        private AnimationPlayer sprite;

        //How long the geyser has been waiting or firing
        private float timer;

        //Time between geyser shoots
        private int timeBetweenShots;
        
        //Time geyser shoots
        private int firingTime;

        //Controls whether the geyser is currently firing
        public bool Firing
        {
            get { return firing; }
        }
        bool firing;

        //Controls whether the geyser is deadly to the player or just a hinderance
        public GeyserType GeyserType
        {
            get { return geyserType; }
        }
        GeyserType geyserType;

        /// <summary>
        /// Constructs a new geyser
        /// </summary>
        ///<param name="rotation"> The angle of rotation in degrees. 0, 90, 180, or 270</param>
        public Geyser(Level level, Vector2 position, GeyserType geyserType, int timeBetween, int fireTime, float rotation, string name)
        {
            this.level = level;
            this.position = position;
            this.geyserType = geyserType;
            this.timeBetweenShots = timeBetween;
            this.firingTime = fireTime;
            this.rotation = rotation;

            timer = (float)timeBetweenShots;
            firing = false;

            LoadContent(name);
        }

        /// <summary>
        /// Loads the animations
        /// </summary>
        public void LoadContent(string name)
        {
            string spriteSet = "Sprites/Hazards/" + name;
            idle = new Animation(Level.Content.Load<Texture2D>(spriteSet + "Idle"), 0.01f, true, 40, 64);
            shoot = new Animation(Level.Content.Load<Texture2D>(spriteSet + "Shoot"), 0.01f, true, 40, 64);
            
            // Calculate bounds within texture size.
            int width = (int)(idle.FrameWidth * 0.35);
            int left = (idle.FrameWidth - width) / 2;
            int height = (int)(idle.FrameWidth * 0.7);
            int top = idle.FrameHeight - height;
            localBounds = new Rectangle(left, top, width, height);

            sprite.PlayAnimation(idle);
        }

        public void Update(GameTime gameTime)
        {
            timer -= (float)gameTime.ElapsedGameTime.TotalSeconds;
            if (timer <= 0)
            {
                int x = (int)Position.X / Tile.Width;
                int y = (int)Position.Y / Tile.Height;

                if (firing)
                {
                    firing = false;
                    timer = timeBetweenShots;
                    sprite.PlayAnimation(idle);

                }
                else if (!firing)
                {
                    firing = true;
                    timer = firingTime;
                    sprite.PlayAnimation(shoot);
                }
            }
        }

        public void Draw(GameTime gameTime, SpriteBatch spriteBatch)
        {
            sprite.Draw(gameTime, spriteBatch, Position, SpriteEffects.None, new Vector2(1, 1), 0.0f);
        }
    }
}
