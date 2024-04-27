using System;
using System.Collections.Generic;
using FontStashSharp;
using Microsoft.Xna.Framework.Graphics;

namespace Wobble.Graphics.Sprites.Text
{
    public class WobbleFontStore
    {
        public FontSystem FontSystem { get; }
        /// <summary>
        ///     All of the contained fonts at different sizes
        /// </summary>
        public DynamicSpriteFont Store { get; set; }

        /// <summary>
        ///     The size the font was initially created ad
        /// </summary>
        public int DefaultSize { get; }

        /// <summary>
        /// </summary>
        /// <param name="size"></param>
        /// <param name="font"></param>
        /// <param name="addedFonts"></param>
        public WobbleFontStore(int size, byte[] font, Dictionary<string, byte[]> addedFonts = null)
        {
            FontSystem = new FontSystem();
            FontSystem.AddFont(font);
            DefaultSize = size;

            if (addedFonts == null)
                return;

            foreach (var f in addedFonts)
                AddFont(f.Key, f.Value);
            Store = FontSystem.GetFont(size);
        }

        public float FontSize
        {
            get => Store.FontSize;
            set => Store = FontSystem.GetFont(value);
        }

        /// <summary>
        ///     Adds a font to the store from a byte[]
        /// </summary>
        /// <param name="name"></param>
        /// <param name="font"></param>
        public void AddFont(string name, byte[] font) => FontSystem.AddFont(font);
    }
}