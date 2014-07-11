using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace RealmRacer
{
    /// <summary>
    /// Controls the collision detection and response behavior of a tile.
    /// </summary>
    enum TileCollision
    {
        /// <summary>
        /// A passable tile is one which does not hinder player motion at all.
        /// </summary>
        Passable = 0,

        /// <summary>
        /// An impassable tile is one which does not allow the player to move through
        /// it at all. It is completely solid.
        /// </summary>
        Impassable = 1,

        /// <summary>
        /// A platform tile is one which behaves like a passable tile except when the
        /// player is above it. A player can jump up through a platform as well as move
        /// past it to the left and right, but can not fall down through the top of it.
        /// </summary>
        Platform = 2,

        /// <summary>
        /// A breakable tile is one that behaves like an impassable tile but it can be destroyed 
        /// using special moves.
        /// </summary>
        Breakable = 3,

        /// <summary>
        /// Slows the players movement both horizontally and vertically.
        /// </summary>
        Impeding = 4,

        /// <summary>
        /// Low friction surface. The player slides on one of these tiles.
        /// </summary>
        Slippery = 5,

        /// <summary>
        /// Tiles that break when stood upon.
        /// </summary>
        Unstable = 6,
    }

    /// <summary>
    /// Stores the appearance and collision behavior of a tile.
    /// </summary>
    class Tile
    {
        public Texture2D Texture;
        public Texture2D nullTexture = null;
        public TileCollision Collision;

        public bool destroyed = false;
        public bool rebuildable = true;

        //width = 40, height = 32
        public const int Width = 40;
        public const int Height = 32;

        public static readonly Vector2 Size = new Vector2(Width, Height);

        /// <summary>
        /// Constructs a new tile.
        /// </summary>
        public Tile(Texture2D texture, TileCollision collision)
        {
            Texture = texture;
            Collision = collision;
        }

        public Tile(Texture2D texture, TileCollision collision, bool rebuildable)
        {
            Texture = texture;
            Collision = collision;
            this.rebuildable = rebuildable;
        }
    }
}
