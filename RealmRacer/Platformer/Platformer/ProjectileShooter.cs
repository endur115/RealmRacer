using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;


namespace RealmRacer
{
    class ProjectileShooter
    {
        public Level Level
        {
            get { return level; }
        }
        Level level;

        private List<Projectile> projectiles = new List<Projectile>();

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

        /// <summary>
        /// Gets or sets the target for the projectile shooter
        /// </summary>
        public Vector2 Target
        {
            get { return target; }
            set { target = value; }
        }
        Vector2 target;

        private Vector2 launchVelocity;

        private Texture2D projectileTexture;

        private float rateOfFire;
        private float fireTime;

        private int frameHeight;
        private int frameWidth;

        private bool affectedByGravity;

        public ProjectileShooter(Level level, Vector2 position, Vector2 velocity, Vector2 target, float rateOfFire, string name, int frameWidth, int frameHeight, bool affectedByGravity, Vector2 projectileVelocity)
        {
            this.level = level;
            this.position = position;
            this.velocity = velocity;
            this.target = target;
            this.rateOfFire = rateOfFire;
            fireTime = rateOfFire;
            this.frameWidth = frameWidth;
            this.frameHeight = frameHeight;
            this.affectedByGravity = affectedByGravity;
            this.launchVelocity = projectileVelocity;

            target.Normalize();

            projectileTexture = Level.Content.Load<Texture2D>("Sprites/Hazards/" + name);
        }

        public void Update(GameTime gameTime)
        {
            float elapsed = (float)gameTime.ElapsedGameTime.TotalSeconds;

            Position += Velocity * elapsed;

            fireTime -= elapsed;
            if (fireTime <= 0)
            {
                fireTime = rateOfFire;

                Vector2 vel = launchVelocity * target;
                projectiles.Add(new Projectile(level, position, vel, projectileTexture, affectedByGravity, frameWidth, frameHeight));
                Console.WriteLine("New Projectile Added");
            }

            for (int i = 0; i < projectiles.Count; i++)
            {
                projectiles[i].Update(gameTime);

                //Collision with player
                if (projectiles[i].BoundingRectangle.Intersects(Level.Player.BoundingRectangle))
                {
                    projectiles.RemoveAt(i--);
                    Level.Player.OnKilled();
                }
            }

            for (int i = 0; i < projectiles.Count; i++)
            {
                //Collision with level
                // Get the player's bounding rectangle and find neighboring tiles.
                Rectangle bounds = projectiles[i].BoundingRectangle;
                int leftTile = (int)Math.Floor((float)bounds.Left / Tile.Width);
                int rightTile = (int)Math.Ceiling(((float)bounds.Right / Tile.Width)) - 1;
                int topTile = (int)Math.Floor((float)bounds.Top / Tile.Height);
                int bottomTile = (int)Math.Ceiling(((float)bounds.Bottom / Tile.Height)) - 1;

                // For each potentially colliding tile,
                for (int y = topTile; y <= bottomTile; ++y)
                {
                    for (int x = leftTile; x <= rightTile; ++x)
                    {
                        // If this tile is collidable,
                        TileCollision collision = Level.GetCollision(x, y);

                        if (collision != TileCollision.Passable)
                        {
                            projectiles.RemoveAt(i--);
                            Console.WriteLine("Projectile removed");
                            return;
                        }
                    }
                }
            }
        }

        public void Draw(GameTime gameTime, SpriteBatch spriteBatch, Vector2 cameraPosition)
        {
            // Calculate the visible range of tiles.
            int left = (int)Math.Floor(cameraPosition.X);
            int right = left + spriteBatch.GraphicsDevice.Viewport.Width;
            int top = (int)Math.Floor(cameraPosition.Y);
            int bottom = top + spriteBatch.GraphicsDevice.Viewport.Width;

            foreach (Projectile projectile in projectiles)
                if (projectile.Position.X >= left && projectile.Position.X <= right)
                    if (projectile.Position.Y >= top && projectile.Position.Y <= bottom)
                        projectile.Draw(gameTime, spriteBatch);
        }
    }
}
