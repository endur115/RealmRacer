using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Content;


namespace RealmRacer
{
    class Layer
    {
        public Texture2D texture { get; private set; }

        public Layer(ContentManager content, string basePath)
        {
            // Assumes each layer only has 3 segments.
            texture = content.Load<Texture2D>(basePath);
        }

        public void Draw(SpriteBatch spriteBatch)
        {
            spriteBatch.Draw(texture, new Vector2(0, 0), Color.White);
        }

    }
}
