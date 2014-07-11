using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace RealmRacer
{
    class TileTimer
    {
        //Stores which tile the timer is for
        public int X;
        public int Y;

        //Which level the tile timer belongs to
        public Level Level;

        //Timer for the tile
        public TimeSpan Timer = TimeSpan.Zero;

        //Controls the timer
        public bool destroyOn = true;
        public bool rebuildOn = false;
        public double destroySeconds;
        public double rebuildSeconds;

        public TileTimer(Level level, int x, int y, double destroy, double rebuild)
        {
            Level = level;
            X = x;
            Y = y;
            destroySeconds = destroy;
            rebuildSeconds = rebuild;

            Timer = TimeSpan.FromSeconds(destroy);
        }

        public void Update(GameTime gameTime)
        {
            if (rebuildOn)
            {
                Timer -= gameTime.ElapsedGameTime;

                if (Timer <= TimeSpan.Zero)
                {
                    Level.tiles[X, Y].destroyed = false;
                    rebuildOn = false;
                    destroyOn = false;

                    Texture2D temp = Level.tiles[X, Y].Texture;
                    Level.tiles[X, Y].Texture = Level.tiles[X, Y].nullTexture;
                    Level.tiles[X, Y].nullTexture = temp;
                }
            }
            else if (destroyOn)
            {
                Timer -= gameTime.ElapsedGameTime;

                if (Timer <= TimeSpan.Zero)
                {
                    Level.tiles[X, Y].destroyed = true;
                    Timer = TimeSpan.FromSeconds(rebuildSeconds);
                    rebuildOn = Level.tiles[X, Y].rebuildable;
                    destroyOn = false;

                    Texture2D temp = Level.tiles[X, Y].Texture;
                    Level.tiles[X, Y].Texture = Level.tiles[X, Y].nullTexture;
                    Level.tiles[X, Y].nullTexture = temp;
                }
            }
        }
    }
}
