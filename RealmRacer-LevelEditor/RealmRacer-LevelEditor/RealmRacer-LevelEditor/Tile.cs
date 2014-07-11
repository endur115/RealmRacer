using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace RealmRacer_LevelEditor
{
    class Tile
    {
        /// <summary>
        /// Gets/Sets the texture
        /// </summary>
        public Texture2D Texture
        {
            get { return texture; }
            set { texture = value; }
        }
        Texture2D texture;

        /// <summary>
        /// Gets/Sets the tile type.
        /// </summary>
        public char TileType
        {
            get { return tileType; }
            set { tileType = value; }
        }
        char tileType;

        /// <summary>
        /// Gets/Sets the position
        /// </summary>
        public Vector2 Position
        {
            get { return position; }
            set { position = value; }
        }
        Vector2 position;

        /// <summary>
        /// Constructs a tile
        /// </summary>
        public Tile(Texture2D texture, char tileType, Vector2 position)
        {
            this.texture = texture;
            this.tileType = tileType;
            this.position = position;
        }

        /// <summary>
        /// Draws a tile
        /// </summary>
        public void Draw(SpriteBatch spriteBatch)
        {
            if (texture != null)
                spriteBatch.Draw(texture, position, Color.White);
        }
    }
}
