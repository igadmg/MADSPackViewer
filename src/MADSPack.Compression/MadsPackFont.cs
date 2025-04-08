using MathEx;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

namespace MADSPack.Compression
{
	public class MadsPackFont
	{
		public int maxHeight;
		public int maxWidth;
		public byte[] charWidths;
		public uint[] charOffsets;
		public byte[] charData;
		private byte[] paletteData;
		public string pathtoCol;

		private List<byte> _fontColors = new List<byte>();

		public MadsPackFont(MadsPackEntry item)
		{
			_fontColors.Add(0xFF);
			_fontColors.Add(0xF);
			_fontColors.Add(7);
			_fontColors.Add(8);

			MemoryStream fontFile = new MemoryStream(item.getData());
			maxHeight = fontFile.ReadByte();
			maxWidth = fontFile.ReadByte();

			charWidths = new byte[128];
			// Char data is shifted by 1
			charWidths[0] = 0;
			fontFile.Read(charWidths, 1, 127);
			fontFile.ReadByte();	// remainder

			charOffsets = new uint[128];

			uint startOffs = 2 + 128 + 256;
			uint fontSize = (uint)(fontFile.Length - startOffs);

			charOffsets[0] = 0;
			for (int i = 1; i < 128; i++)
			{
				byte[] twobyte = new byte[2];
				fontFile.Read(twobyte, 0, 2);
				ushort val = (ushort)BitConverter.ToInt16(twobyte, 0);
				charOffsets[i] = val - startOffs;
			}
			fontFile.ReadByte();	// remainder

			charData = new byte[fontSize];
			fontFile.Read(charData, 0, (int)fontSize);
		}

		public string GenerateFntDescription(vec2i size, vec2i charRange, int spaceWidth, string fontName, string pngFilename)
		{
			var result = new StringBuilder();
			result.AppendLine($"info face=\"{fontName}\" size=9 bold=0 italic=0 charset=\"\" unicode=1 stretchH=100 smooth=1 aa=1 padding=0,0,0,0 spacing=0,0 outline=1");
			result.AppendLine($"common lineHeight=9 base=15 scaleW=512 scaleH=512 pages=1 packed=0 alphaChnl=0 redChnl=4 greenChnl=4 blueChnl=4");
			result.AppendLine($"page id=0 file=\"{pngFilename}\"");
			result.AppendLine($"chars count={charRange.d}");

			int xPos = 0;
			int yPos = 0;

			result.AppendLine($"char id=32 x=0 y=0 width=0 height=0 xoffset=0 yoffset=0 xadvance={spaceWidth} page=0 chnl=15");
			foreach (var ch in Enumerable.Range(charRange.x, charRange.d))
			{
				char theChar = (char)(ch & 0x7F);
				int charWidth = charWidths[(byte)theChar];

				if (xPos + charWidth >= size.x)
				{
					yPos += pt.Y;
					xPos = 0;
					Console.Out.WriteLine();
				}

				result.AppendLine($"char id={ch} x={xPos} y={yPos} width={charWidth} height={pt.Y} xoffset=0 yoffset=0 xadvance={charWidth} page=0 chnl=15");
				Console.Out.Write(theChar);

				if (charWidth > 0)
				{
					xPos += charWidth;
				}
			}

			return result.ToString();
		}

		Point pt = new Point(9, 9);

		public (vec2i size, colorb[] data) GenerateFontMap(vec2i size, vec2i charRange)
		{
			var bmp = (size, new colorb[size.product]);
			string a = new StringBuilder()
				.Append(Enumerable.Range(charRange.x, charRange.d).Select(c => (char)c).ToArray())
				.ToString();
				//Enumerable.Range(charRange.x, charRange.d)
				//.Cast<char>().Aggregate(string.Empty, (a, c) => a.);
			writeString(ref bmp, a);
			return bmp;
		} 

		public int writeString(ref (vec2i size, colorb[] data) bmp, string msg)
		{
			PalReader r = new PalReader();
			this.paletteData = r.GetPalette(pathtoCol + "\\VICEROY.PAL");

			int xEnd;
			if (bmp.size.x > 0)
				xEnd = Math.Min(bmp.size.x, pt.X + bmp.size.x);
			else
				xEnd = bmp.size.x;

			int x = pt.X;
			int y = pt.Y;

			int skipY = 0;
			if (y < 0)
			{
				skipY = -y;
				y = 0;
			}

			int height = Math.Max(0, maxHeight - skipY);
			if (height == 0)
				return x;

			int bottom = y + height - 1;
			if (bottom > bmp.size.y - 1)
			{
				height -= Math.Min(height, bottom - (bmp.size.y - 1));
			}

			if (height <= 0)
				return x;

			int xPos = 0;
			int yPos = 0;

			char[] text = Encoding.ASCII.GetChars(Encoding.ASCII.GetBytes(msg));

			for (int l = 0; l < text.Length; l++)
			{
				char theChar = (char)(text[l] & 0x7F);
				int charWidth = charWidths[(byte)theChar];

				if (xPos + charWidth >= xEnd)
				{
					yPos += pt.Y;
					xPos = 0;
				}

				if (charWidth > 0)
				{
					uint initialCharDataByte = charOffsets[(byte)theChar] + 1;
					uint endCharDataByte = charOffsets[(byte)theChar + 1] + 1;

					int bpp = getBpp(charWidth);

					if (skipY != 0)
						initialCharDataByte += (uint)(bpp * skipY);

					List<byte> pixellist = new List<byte>();

					for (int i = 0; i < height; i++)
					{
						for (int j = 0; j < bpp; j++)
						{
							long dataindex = initialCharDataByte + ((i * bpp) + j);
							if (dataindex >= charData.Length) dataindex = charData.Length - 1;
							byte val = charData[dataindex];
							// Get all 8 bits from this byte
							byte bit8 = (byte)((val & 0x80) >> 7);
							byte bit7 = (byte)((val & 0x40) >> 6);
							byte bit6 = (byte)((val & 0x20) >> 5);
							byte bit5 = (byte)((val & 0x10) >> 4);
							byte bit4 = (byte)((val & 0x08) >> 3);
							byte bit3 = (byte)((val & 0x04) >> 2);
							byte bit2 = (byte)((val & 0x02) >> 1);
							byte bit1 = (byte)(val & 0x01);

							byte nib4 = (byte)((bit8 << 1) + bit7);
							byte nib3 = (byte)((bit6 << 1) + bit5);
							byte nib2 = (byte)((bit4 << 1) + bit3);
							byte nib1 = (byte)((bit2 << 1) + bit1);

							pixellist.Add(_fontColors[nib4]);
							pixellist.Add(_fontColors[nib3]);
							pixellist.Add(_fontColors[nib2]);
							pixellist.Add(_fontColors[nib1]);
						}
					}

					int[,] pxdata = new int[height, charWidth];
					int counter = 0;
					for (int i = 0; i < charWidth * height; i++)
					{
						int h = (i / charWidth);
						int w = i % charWidth;
						if (counter >= pixellist.Count) pxdata[h, w] = 0;
						else pxdata[h, w] = pixellist[counter];
						// Skip the quantity of bpp from the end (god knows why)
						if (i % charWidth == charWidth - 1)
						{
							counter += (bpp * 4) - charWidth;
						}
						counter++;
					}
					// Draw the char

					//Bitmap b = new Bitmap(charWidth, height);
					for (int yy = 0; yy < height; yy++)
					{
						for (int xx = 0; xx < charWidth; xx++)
						{
							// Get RGB values for this index
							byte aa, rr, gg, bb;

							if (pxdata[yy, xx] == 255 || pxdata[yy, xx] == 0)
							{
								aa = rr = gg = bb = 0;
							}
							else
							{
								aa = 255;
								rr = (byte)(paletteData[pxdata[yy, xx] * 3] | 192);
								gg = (byte)(paletteData[(pxdata[yy, xx] * 3) + 1] | 192);
								bb = (byte)(paletteData[(pxdata[yy, xx] * 3) + 1] | 192);
							}

							bmp.data[xx + xPos + (yy + yPos) * bmp.size.x] = new colorb(rr, gg, bb, aa);// b.SetPixel(xx, yy, Color.FromArgb(aa, rr, gg, bb));
						}
					}

					xPos += charWidth;
				}
			}

			return xEnd;
		}


		public int getWidth(string msg, int spaceWidth)
		{
			int width = 0;
			char[] text = Encoding.ASCII.GetChars(Encoding.ASCII.GetBytes(msg));

			if (msg.Length > 0)
			{
				for (int i = 0; i < text.Length; i++)
				{
					width += charWidths[text[i] & 0x7F] + spaceWidth;
				}
				width -= spaceWidth;
			}

			return width;
		}

		public int getBpp(int charWidth)
		{
			if (charWidth > 12)
				return 4;
			else if (charWidth > 8)
				return 3;
			else if (charWidth > 4)
				return 2;
			else
				return 1;
		}
	}
}
