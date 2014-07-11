using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace RealmRacer
{
    class Button
    {
        /// <summary>
        /// Button texture
        /// </summary>
        private Texture2D texture;

        /// <summary>
        /// Bounding rectangle for the button
        /// </summary>
        private Rectangle boundingRect;

        /// <summary>
        /// Position of the button
        /// </summary>
        private Vector2 position;

        /// <summary>
        /// Origin of the button
        /// </summary>
        private Vector2 origin;

        private MouseState prevMouseState;

        public bool clicked;

        public Button(Texture2D texture, Vector2 position)
        {
            this.texture = texture;
            this.position = position;

            origin = new Vector2(texture.Width / 2, texture.Height / 2);
            clicked = false;

            boundingRect = new Rectangle(0, 0, texture.Width, texture.Height);
        }

        public void Update(MouseState mouseState)
        {
            clicked = false;

            //Determine if the button was clicked
            if (mouseState.LeftButton == ButtonState.Released && prevMouseState.LeftButton == ButtonState.Pressed)
            {
                if (mouseState.X > position.X - origin.X && mouseState.X <= position.X + origin.X)
                    if (mouseState.Y >= position.Y - origin.Y && mouseState.Y <= position.Y + origin.Y)
                        clicked = true;
            }

            prevMouseState = mouseState;
        }

        public void Draw(SpriteBatch spriteBatch)
        {
            spriteBatch.Draw(texture, position, boundingRect, Color.White, 0.0f, origin, 1.0f, SpriteEffects.None, 1.0f);
        }
    }
}
