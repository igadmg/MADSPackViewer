using MathEx;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;

namespace MADSPack.Compression
{
    public class MadsPackImage
    {

        public MadsPackImage()
        {
            HasPalette = false;
        }

        public (vec2i size, colorb[] data) GetImage()
        {
            if (!this.HasPalette)
            {
                PalReader r = new PalReader();
                this.paletteData = r.GetPalette(pathtoCol + "\\VICEROY.PAL");
            }

            var size = vec2i.xy(width, height);
            var data = imageData.Select(idx => new colorb(
                        (byte)(getPaletteData()[idx * 3] * 4).Clamp(byte.MinValue, byte.MaxValue),
                        (byte)(getPaletteData()[(idx * 3) + 1] * 4).Clamp(byte.MinValue, byte.MaxValue),
                        (byte)(getPaletteData()[(idx * 3) + 2] * 4).Clamp(byte.MinValue, byte.MaxValue),
                        idx == TRANSPARENT_COLOUR_INDEX ? 0 : TRANSPARENT_COLOUR_INDEX
                )).ToArray();
#if false
            // Create Bitmap
            Bitmap bmp = new Bitmap(this.width, this.height);
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    // Find index in array
                    int pos = (y * width) + x;
                    // Get index for this pixel
                    int idx = imageData[pos];
                    // Get RGB values for this index
                    int a, r, g, b;

                    if (idx == 255)
                    {
                        a = r = g = b = 0;
                    }
                    else
                    {
                        a = 255;

                        //r = paletteData[idx * 3] * 2;
                        //g = paletteData[(idx * 3) + 1] * 2;
                        ////b = paletteData[(idx * 3) + 1] * 2;
                        // @BUGFIX
                        //b = paletteData[(idx * 3) + 2] * 2;

                        r = paletteData[idx * 3] * 4;
                        g = paletteData[(idx * 3) + 1] * 4;
                        b = paletteData[(idx * 3) + 2] * 4;
                    }


                    // Set this pixel color in image
                    bmp.SetPixel(x, y, Color.FromArgb(a, r, g, b));
                }
            }

#endif
            return(size, data);
        }
        public void setWidth(short width)
        {
            this.width = width;
        }

        public int getWidth()
        {
            return width;
        }

        public void setHeight(short height)
        {
            this.height = height;
        }

        public int getHeight()
        {
            return height;
        }

        public void setImageData(byte[] imageData)
        {
            this.imageData = imageData;
        }

        public byte[] getImageData()
        {
            return imageData;
        }

        public void setPaletteData(byte[] paletteData)
        {
            this.paletteData = paletteData;
        }

        public byte[] getPaletteData()
        {
            return paletteData;
        }

        public bool hasPalette()
        {
            return HasPalette;
        }

        public void setHasPalette(bool hasPalette)
        {
            this.HasPalette = hasPalette;
        }

        private int width;
        private int height;
        private byte[] imageData;
        private byte[] paletteData;
        private bool HasPalette;
        public string pathtoCol;
        private const byte TRANSPARENT_COLOUR_INDEX = 255;
    }
}
