using System;
using System.IO;
using static StbImageNext.StbImage;

namespace StbImageNext.Decoders
{
	public static class TgaImageDecoder
	{
		public static bool Test(Stream s)
		{
			try
			{
				var res = false;
				var sz = 0;
				var tga_color_type = 0;
				s.stbi__get8();
				tga_color_type = s.stbi__get8();
				if (tga_color_type > 1)
					goto errorEnd;
				sz = s.stbi__get8();
				if (tga_color_type == 1)
				{
					if (sz != 1 && sz != 9)
						goto errorEnd;
					s.stbi__skip(4);
					sz = s.stbi__get8();
					if (sz != 8 && sz != 15 && sz != 16 && sz != 24 && sz != 32)
						goto errorEnd;
					s.stbi__skip(4);
				}
				else
				{
					if (sz != 2 && sz != 3 && sz != 10 && sz != 11)
						goto errorEnd;
					s.stbi__skip(9);
				}

				if (s.stbi__get16le() < 1)
					goto errorEnd;
				if (s.stbi__get16le() < 1)
					goto errorEnd;
				sz = s.stbi__get8();
				if (tga_color_type == 1 && sz != 8 && sz != 16)
					goto errorEnd;
				if (sz != 8 && sz != 15 && sz != 16 && sz != 24 && sz != 32)
					goto errorEnd;
				res = true;
			errorEnd:;
				return res;
			}
			finally
			{
				s.stbi__rewind();
			}
		}

		public static ImageInfo Info(Stream s)
		{
			try
			{
				var tga_w = 0;
				var tga_h = 0;
				var tga_comp = 0;
				var tga_image_type = 0;
				var tga_bits_per_pixel = 0;
				var tga_colormap_bpp = 0;
				var sz = 0;
				var tga_colormap_type = 0;
				s.stbi__get8();
				tga_colormap_type = s.stbi__get8();
				if (tga_colormap_type > 1)
				{
					throw new Exception("tga_colormap_type > 1");
				}

				tga_image_type = s.stbi__get8();
				if (tga_colormap_type == 1)
				{
					if (tga_image_type != 1 && tga_image_type != 9)
					{
						throw new Exception("tga_image_type != 1 && tga_image_type != 9");
					}

					s.stbi__skip(4);
					sz = s.stbi__get8();
					if (sz != 8 && sz != 15 && sz != 16 && sz != 24 && sz != 32)
					{
						throw new Exception("sz != 8 && sz != 15 && sz != 16 && sz != 24 && sz != 32");
					}

					s.stbi__skip(4);
					tga_colormap_bpp = sz;
				}
				else
				{
					if (tga_image_type != 2 && tga_image_type != 3 && tga_image_type != 10 && tga_image_type != 11)
					{
						throw new Exception("tga_image_type != 2 && tga_image_type != 3 && tga_image_type != 10 && tga_image_type != 11");
					}

					s.stbi__skip(9);
					tga_colormap_bpp = 0;
				}

				tga_w = s.stbi__get16le();
				if (tga_w < 1)
				{
					throw new Exception("tga_w < 1");
				}

				tga_h = s.stbi__get16le();
				if (tga_h < 1)
				{
					throw new Exception("tga_h < 1");
				}

				tga_bits_per_pixel = s.stbi__get8();
				s.stbi__get8();
				if (tga_colormap_bpp != 0)
				{
					if (tga_bits_per_pixel != 8)
					{
						throw new NotSupportedException("Only 8-bit TGA are supported");
					}

					tga_comp = stbi__tga_get_comp(tga_colormap_bpp, 0, out bool is_rgb16);
				}
				else
				{
					tga_comp = stbi__tga_get_comp(tga_bits_per_pixel, tga_image_type == 3 || tga_image_type == 11 ? 1 : 0,
						out bool is_rgb16);
				}

				if (tga_comp == 0)
				{
					throw new Exception("tga_comp == 0");
				}

				return new ImageInfo
				{
					Width = tga_w,
					Height = tga_h,
					ColorComponents = (ColorComponents)tga_comp
				};
			}
			finally
			{
				s.stbi__rewind();
			}
		}

		public static ImageResult Load(Stream s, ColorComponents req_comp)
		{
			int tga_offset = s.stbi__get8();
			int tga_indexed = s.stbi__get8();
			int tga_image_type = s.stbi__get8();
			var tga_is_RLE = 0;
			var tga_palette_start = s.stbi__get16le();
			var tga_palette_len = s.stbi__get16le();
			int tga_palette_bits = s.stbi__get8();
			var tga_x_origin = s.stbi__get16le();
			var tga_y_origin = s.stbi__get16le();
			var tga_width = s.stbi__get16le();
			var tga_height = s.stbi__get16le();
			int tga_bits_per_pixel = s.stbi__get8();
			var tga_comp = 0;
			int tga_inverted = s.stbi__get8();
			var i = 0;
			var j = 0;
			var raw_data = new DataBuffer<byte>(4);
			raw_data.Set(0);

			var RLE_count = 0;
			var RLE_repeating = 0;
			var read_next_pixel = 1;
			if (tga_height > 1 << 24)
				throw new Exception("too large");
			if (tga_width > 1 << 24)
				throw new Exception("too large");
			if (tga_image_type >= 8)
			{
				tga_image_type -= 8;
				tga_is_RLE = 1;
			}

			tga_inverted = 1 - ((tga_inverted >> 5) & 1);

			bool tga_rgb16;
			if (tga_indexed != 0)
				tga_comp = stbi__tga_get_comp(tga_palette_bits, 0, out tga_rgb16);
			else
				tga_comp = stbi__tga_get_comp(tga_bits_per_pixel, tga_image_type == 3 ? 1 : 0, out tga_rgb16);
			if (tga_comp == 0)
				throw new Exception("bad format");

			var result = new ImageResult
			{
				Width = tga_width,
				Height = tga_height,
				SourceComp = (ColorComponents)tga_comp
			};

			result.Comp = req_comp == ColorComponents.Default ? result.SourceComp : req_comp;

			if (stbi__mad3sizes_valid(tga_width, tga_height, tga_comp, 0) == 0)
				throw new Exception("too large");
			var tga_data = new DataBuffer<byte>(tga_width * tga_height * tga_comp);

			s.stbi__skip(tga_offset);
			if (tga_indexed == 0 && tga_is_RLE == 0 && !tga_rgb16)
			{
				for (i = 0; i < tga_height; ++i)
				{
					var row = tga_inverted != 0 ? tga_height - i - 1 : i;
					var tga_row = tga_data + row * tga_width * tga_comp;
					s.stbi__getn(tga_row, tga_width * tga_comp);
				}
			}
			else
			{
				DataBuffer<byte> tga_palette = DataBuffer<byte>.Null;
				if (tga_indexed != 0)
				{
					if (tga_palette_len == 0)
					{
						throw new Exception("bad palette");
					}

					s.stbi__skip(tga_palette_start);
					tga_palette = new DataBuffer<byte>(tga_palette_len * tga_comp);
					if (tga_rgb16)
					{
						var pal_entry = tga_palette;
						for (i = 0; i < tga_palette_len; ++i)
						{
							stbi__tga_read_rgb16(s, pal_entry);
							pal_entry += tga_comp;
						}
					}
					else if (s.stbi__getn(tga_palette, tga_palette_len * tga_comp) == 0)
					{
						throw new Exception("bad palette");
					}
				}

				for (i = 0; i < tga_width * tga_height; ++i)
				{
					if (tga_is_RLE != 0)
					{
						if (RLE_count == 0)
						{
							int RLE_cmd = s.stbi__get8();
							RLE_count = 1 + (RLE_cmd & 127);
							RLE_repeating = RLE_cmd >> 7;
							read_next_pixel = 1;
						}
						else if (RLE_repeating == 0)
						{
							read_next_pixel = 1;
						}
					}
					else
					{
						read_next_pixel = 1;
					}

					if (read_next_pixel != 0)
					{
						if (tga_indexed != 0)
						{
							var pal_idx = tga_bits_per_pixel == 8 ? s.stbi__get8() : s.stbi__get16le();
							if (pal_idx >= tga_palette_len)
								pal_idx = 0;
							pal_idx *= tga_comp;
							for (j = 0; j < tga_comp; ++j)
								raw_data[j] = tga_palette[pal_idx + j];
						}
						else if (tga_rgb16)
						{
							stbi__tga_read_rgb16(s, raw_data);
						}
						else
						{
							for (j = 0; j < tga_comp; ++j)
								raw_data[j] = s.stbi__get8();
						}

						read_next_pixel = 0;
					}

					for (j = 0; j < tga_comp; ++j)
						tga_data[i * tga_comp + j] = raw_data[j];
					--RLE_count;
				}

				if (tga_inverted != 0)
				{
					for (j = 0; j * 2 < tga_height; ++j)
					{
						var index1 = j * tga_width * tga_comp;
						var index2 = (tga_height - 1 - j) * tga_width * tga_comp;
						for (i = tga_width * tga_comp; i > 0; --i)
						{
							var temp = tga_data[index1];
							tga_data[index1] = tga_data[index2];
							tga_data[index2] = temp;
							++index1;
							++index2;
						}
					}
				}
			}

			if (tga_comp >= 3 && !tga_rgb16)
			{
				var tga_pixel = tga_data;
				for (i = 0; i < tga_width * tga_height; ++i)
				{
					var temp = tga_pixel[0];
					tga_pixel[0] = tga_pixel[2];
					tga_pixel[2] = temp;
					tga_pixel += tga_comp;
				}
			}

			if (req_comp != 0 && (int)req_comp != tga_comp)
				tga_data = Utility.stbi__convert_format(tga_data, tga_comp, (int)req_comp, (uint)tga_width, (uint)tga_height);
			tga_palette_start = tga_palette_len = tga_palette_bits = tga_x_origin = tga_y_origin = 0;

			result.Data = tga_data.Data;

			return result;
		}

		private static int stbi__tga_get_comp(int bits_per_pixel, int is_grey, out bool is_rgb16)
		{
			is_rgb16 = false;
			switch (bits_per_pixel)
			{
				case 8:
					return STBI_grey;
				case 16:
				case 15:
					if (bits_per_pixel == 16 && is_grey != 0)
						return STBI_grey_alpha;
					is_rgb16 = true;
					return STBI_rgb;
				case 24:
				case 32:
					return bits_per_pixel / 8;
				default:
					return 0;
			}
		}

		private static void stbi__tga_read_rgb16(Stream s, DataBuffer<byte> _out_)
		{
			var px = (ushort)s.stbi__get16le();
			ushort fiveBitMask = 31;
			var r = (px >> 10) & fiveBitMask;
			var g = (px >> 5) & fiveBitMask;
			var b = px & fiveBitMask;
			_out_[0] = (byte)(r * 255 / 31);
			_out_[1] = (byte)(g * 255 / 31);
			_out_[2] = (byte)(b * 255 / 31);
		}
	}
}
