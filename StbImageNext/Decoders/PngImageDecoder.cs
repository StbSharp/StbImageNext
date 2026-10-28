using System;
using System.IO;
using System.IO.Compression;
using System.Runtime.InteropServices;
using static StbImageNext.StbImage;

namespace StbImageNext.Decoders
{
	/// <summary>
	/// Provides methods to test, get information about and decode PNG images.
	/// </summary>
	public static class PngImageDecoder
	{
		private const int STBI__F_none = 0;
		private const int STBI__F_sub = 1;
		private const int STBI__F_up = 2;
		private const int STBI__F_avg = 3;
		private const int STBI__F_paeth = 4;
		private const int STBI__F_avg_first = 5;

		private static readonly byte[] first_row_filter = { STBI__F_none, STBI__F_sub, STBI__F_none, STBI__F_avg_first, STBI__F_sub };
		private static readonly byte[] stbi__check_png_header_png_sig = { 137, 80, 78, 71, 13, 10, 26, 10 };
		private static readonly byte[] stbi__depth_scale_table = { 0, 0xff, 0x55, 0, 0x11, 0, 0, 0, 0x01 };

		/// <summary>
		/// Tests whether the specified stream contains a PNG image.
		/// </summary>
		/// <param name="s">Stream containing the image data.</param>
		/// <returns><c>true</c> if the stream contains a PNG image, otherwise <c>false</c>.</returns>
		public static bool Test(Stream s)
		{
			try
			{
				var r = 0;
				r = stbi__check_png_header(s, false);
				s.stbi__rewind();
				return r == 1 ? true : false;
			}
			finally
			{
				s.stbi__rewind();
			}
		}

		/// <summary>
		/// Gets the image information from the specified stream.
		/// </summary>
		/// <param name="s">Stream containing the image data.</param>
		/// <returns>The image information.</returns>
		public static ImageInfo Info(Stream s)
		{
			try
			{
				var p = new stbi__png();
				p.s = s;

				int x, y, comp;
				if (stbi__png_info_raw(p, out x, out y, out comp) == 0)
				{
					throw new Exception("Could not parse PNG");
				}

				return new ImageInfo
				{
					Width = x,
					Height = y,
					ColorComponents = (ColorComponents)comp
				};
			}
			finally
			{
				s.stbi__rewind();
			}
		}

		public static ImageResult Decode(Stream s, ColorComponents? req_comp = null, bool de_iphone = false, bool iphone_unpremultiply = false)
		{
			var p = new stbi__png
			{
				s = s,
				de_iphone = de_iphone,
				iphone_unpremultiply = iphone_unpremultiply
			};

			var data = stbi__do_png(p, req_comp.ToInternalValue());
			var result = new ImageResult
			{
				Width = (int)p.img_x,
				Height = (int)p.img_y,
				SourceComp = (ColorComponents)p.img_n
			};

			result.Comp = req_comp ?? result.SourceComp;
			result.Data = data.Data;

			return result;
		}

		private static stbi__pngchunk stbi__get_chunk_header(Stream s)
		{
			var c = new stbi__pngchunk();
			c.length = s.stbi__get32be();
			c.type = s.stbi__get32be();
			return c;
		}

		private static int stbi__check_png_header(Stream s, bool throwOnError)
		{
			var i = 0;
			for (i = 0; i < 8; ++i)
				if (s.stbi__get8() != stbi__check_png_header_png_sig[i])
				{
					if (throwOnError)
					{
						throw new Exception("bad png sig");
					}
					else
					{
						return 0;
					}
				}

			return 1;
		}

		private static int stbi__paeth(int a, int b, int c)
		{
			var thresh = c * 3 - (a + b);
			var lo = a < b ? a : b;
			var hi = a < b ? b : a;
			var t0 = hi <= thresh ? lo : c;
			var t1 = thresh <= lo ? hi : t0;
			return t1;
		}

		private static void stbi__create_png_alpha_expand8(DataBuffer<byte> dest, DataBuffer<byte> src, uint x, int img_n)
		{
			var i = 0;
			if (img_n == 1)
				for (i = (int)(x - 1); i >= 0; --i)
				{
					dest[i * 2 + 1] = 255;
					dest[i * 2 + 0] = src[i];
				}
			else
				for (i = (int)(x - 1); i >= 0; --i)
				{
					dest[i * 4 + 3] = 255;
					dest[i * 4 + 2] = src[i * 3 + 2];
					dest[i * 4 + 1] = src[i * 3 + 1];
					dest[i * 4 + 0] = src[i * 3 + 0];
				}
		}


		private static void stbi__create_png_image_raw(stbi__png a, DataBuffer<byte> raw, uint raw_len, int out_n, uint x, uint y, int depth, int color)
		{
			var bytes = depth == 16 ? 2 : 1;
			var s = a.s;
			uint i = 0;
			uint j = 0;
			var stride = (uint)(x * out_n * bytes);
			uint img_len = 0;
			uint img_width_bytes = 0;
			var k = 0;
			var img_n = a.img_n;
			var output_bytes = out_n * bytes;
			var filter_bytes = img_n * bytes;
			var width = (int)x;
			img_width_bytes = (uint)((img_n * x * depth + 7) >> 3);
			a._out_ = new DataBuffer<byte>(x * y * output_bytes);
			img_len = (img_width_bytes + 1) * y;
			if (raw_len < img_len)
				throw new Exception("not enough pixels");
			var filter_buf = new DataBuffer<byte>(img_width_bytes * 2);
			if (depth < 8)
			{
				filter_bytes = 1;
				width = (int)img_width_bytes;
			}

			for (j = 0; j < y; ++j)
			{
				var cur = filter_buf + (j & 1) * img_width_bytes;
				var prior = filter_buf + (~j & 1) * img_width_bytes;
				var dest = a._out_ + stride * j;
				var nk = width * filter_bytes;
				int filter = raw.Read();
				if (filter > 4)
				{
					throw new Exception("invalid filter");
				}

				if (j == 0)
					filter = first_row_filter[filter];
				switch (filter)
				{
					case STBI__F_none:
						cur.Copy(raw, nk);
						break;
					case STBI__F_sub:
						cur.Copy(raw, filter_bytes);
						for (k = filter_bytes; k < nk; ++k)
							cur[k] = (byte)((raw[k] + cur[k - filter_bytes]) & 255);

						break;
					case STBI__F_up:
						for (k = 0; k < nk; ++k)
							cur[k] = (byte)((raw[k] + prior[k]) & 255);

						break;
					case STBI__F_avg:
						for (k = 0; k < filter_bytes; ++k)
							cur[k] = (byte)((raw[k] + (prior[k] >> 1)) & 255);

						for (k = filter_bytes; k < nk; ++k)
							cur[k] = (byte)((raw[k] + ((prior[k] + cur[k - filter_bytes]) >> 1)) & 255);

						break;
					case STBI__F_paeth:
						for (k = 0; k < filter_bytes; ++k)
							cur[k] = (byte)((raw[k] + prior[k]) & 255);

						for (k = filter_bytes; k < nk; ++k)
							cur[k] = (byte)((raw[k] + stbi__paeth(cur[k - filter_bytes], prior[k],
								prior[k - filter_bytes])) & 255);

						break;
					case STBI__F_avg_first:
						cur.Copy(raw, filter_bytes);
						for (k = filter_bytes; k < nk; ++k)
							cur[k] = (byte)((raw[k] + (cur[k - filter_bytes] >> 1)) & 255);

						break;
				}

				raw += nk;
				if (depth < 8)
				{
					var scale = (byte)(color == 0 ? stbi__depth_scale_table[depth] : 1);
					var _in_ = cur;
					var _out_ = dest;
					byte inb = 0;
					var nsmp = (uint)(x * img_n);
					if (depth == 4)
						for (i = 0; i < nsmp; ++i)
						{
							if ((i & 1) == 0)
								inb = _in_.Read();
							_out_.Write((byte)(scale * (inb >> 4)));
						}
					else if (depth == 2)
						for (i = 0; i < nsmp; ++i)
						{
							if ((i & 3) == 0)
								inb = _in_.Read();
							_out_.Write((byte)(scale * (inb >> 6)));
						}
					else
						for (i = 0; i < nsmp; ++i)
						{
							if ((i & 7) == 0)
								inb = _in_.Read();

							_out_.Write((byte)(scale * (inb >> 7)));
							inb <<= 1;
						}

					if (img_n != out_n)
						stbi__create_png_alpha_expand8(dest, dest, x, img_n);
				}
				else if (depth == 8)
				{
					if (img_n == out_n)
						dest.Copy(cur, x * img_n);
					else
						stbi__create_png_alpha_expand8(dest, cur, x, img_n);
				}
				else if (depth == 16)
				{
					throw new NotSupportedException("16-bit PNG not supported");
				}
			}
		}

		private static int stbi__create_png_image(stbi__png a, DataBuffer<byte> image_data, uint image_data_len, int out_n,
			int depth, int color, int interlaced)
		{
			var bytes = depth == 16 ? 2 : 1;
			var out_bytes = out_n * bytes;
			var p = 0;
			if (interlaced == 0)
			{
				stbi__create_png_image_raw(a, image_data, image_data_len, out_n, a.img_x, a.img_y, depth, color);
				return 1;
			}

			var final = new DataBuffer<byte>(a.img_x * a.img_y * out_bytes);

			var xorig = new int[] { 0, 4, 0, 2, 0, 1, 0 };
			var yorig = new int[] { 0, 0, 4, 0, 2, 0, 1 };
			var xspc = new int[] { 8, 8, 4, 4, 2, 2, 1 };
			var yspc = new int[] { 8, 8, 8, 4, 4, 2, 2 };
			for (p = 0; p < 7; ++p)
			{
				var i = 0;
				var j = 0;
				var x = 0;
				var y = 0;
				x = (int)((a.img_x - xorig[p] + xspc[p] - 1) / xspc[p]);
				y = (int)((a.img_y - yorig[p] + yspc[p] - 1) / yspc[p]);
				if (x != 0 && y != 0)
				{
					var img_len = (uint)((((a.img_n * x * depth + 7) >> 3) + 1) * y);
					stbi__create_png_image_raw(a, image_data, image_data_len, out_n, (uint)x, (uint)y, depth, color);

					for (j = 0; j < y; ++j)
					{
						for (i = 0; i < x; ++i)
						{
							var out_y = j * yspc[p] + yorig[p];
							var out_x = i * xspc[p] + xorig[p];

							final.Copy(out_y * a.img_x * out_bytes + out_x * out_bytes, a._out_, (j * x + i) * out_bytes, out_bytes);
						}
					}

					image_data += img_len;
					image_data_len -= img_len;
				}
			}

			a._out_ = final;
			return 1;
		}

		private static void stbi__compute_transparency(stbi__png z, byte[] tc, int out_n)
		{
			var s = z.s;
			uint i = 0;
			var pixel_count = z.img_x * z.img_y;
			var p = z._out_;
			if (out_n == 2)
			{
				for (i = 0; i < pixel_count; ++i)
				{
					p[1] = (byte)(p[0] == tc[0] ? 0 : 255);
					p += 2;
				}
			}
			else
			{
				for (i = 0; i < pixel_count; ++i)
				{
					if (p[0] == tc[0] && p[1] == tc[1] && p[2] == tc[2])
						p[3] = 0;
					p += 4;
				}
			}
		}

		private static int stbi__expand_png_palette(stbi__png a, byte[] palette, int len, int pal_img_n)
		{
			uint i = 0;
			var pixel_count = a.img_x * a.img_y;
			var orig = a._out_;

			var p = new DataBuffer<byte>(pixel_count * pal_img_n);
			if (pal_img_n == 3)
			{
				for (i = 0; i < pixel_count; ++i)
				{
					var n = orig[i] * 4;
					p[0] = palette[n];
					p[1] = palette[n + 1];
					p[2] = palette[n + 2];
					p += 3;
				}
			}
			else
			{
				for (i = 0; i < pixel_count; ++i)
				{
					var n = orig[i] * 4;
					p[0] = palette[n];
					p[1] = palette[n + 1];
					p[2] = palette[n + 2];
					p[3] = palette[n + 3];
					p += 4;
				}
			}

			a._out_ = p;
			return 1;
		}

		private static void stbi__de_iphone(stbi__png z)
		{
			var s = z.s;
			uint i = 0;
			var pixel_count = z.img_x * z.img_y;
			var p = z._out_;
			if (z.img_out_n == 3)
			{
				for (i = 0; i < pixel_count; ++i)
				{
					var t = p[0];
					p[0] = p[2];
					p[2] = t;
					p += 3;
				}
			}
			else
			{
				if (z.iphone_unpremultiply)
				{
					for (i = 0; i < pixel_count; ++i)
					{
						var a = p[3];
						var t = p[0];
						if (a != 0)
						{
							var half = (byte)(a / 2);
							p[0] = (byte)((p[2] * 255 + half) / a);
							p[1] = (byte)((p[1] * 255 + half) / a);
							p[2] = (byte)((t * 255 + half) / a);
						}
						else
						{
							p[0] = p[2];
							p[2] = t;
						}

						p += 4;
					}
				}
				else
				{
					for (i = 0; i < pixel_count; ++i)
					{
						var t = p[0];
						p[0] = p[2];
						p[2] = t;
						p += 4;
					}
				}
			}
		}

		private static int stbi__parse_png_file(stbi__png z, int scan, int req_comp)
		{
			var palette = new byte[1024];
			byte pal_img_n = 0;
			byte has_trans = 0;
			var tc = new byte[3];
			tc.Set((byte)0);

			var tc16 = new ushort[3];
			uint ioff = 0;
			uint idata_limit = 0;
			uint i = 0;
			uint pal_len = 0;
			var first = 1;
			var k = 0;
			var interlace = 0;
			var color = 0;
			var is_iphone = 0;
			var s = z.s;
			z.expanded = null;
			z.idata = DataBuffer<byte>.Null;
			z._out_ = DataBuffer<byte>.Null;
			if (stbi__check_png_header(s, true) == 0)
				return 0;
			if (scan == Constants.STBI__SCAN_type)
				return 1;
			for (; ; )
			{
				var c = stbi__get_chunk_header(s);
				switch (c.type)
				{
					case ((uint)67 << 24) + ((uint)103 << 16) + ((uint)66 << 8) + 73:
						is_iphone = 1;
						s.stbi__skip((int)c.length);
						break;
					case ((uint)73 << 24) + ((uint)72 << 16) + ((uint)68 << 8) + 82:
						{
							var comp = 0;
							var filter = 0;
							if (first == 0)
								throw new Exception("multiple IHDR");
							first = 0;
							if (c.length != 13)
								throw new Exception("bad IHDR len");
							z.img_x = s.stbi__get32be();
							z.img_y = s.stbi__get32be();
							if (z.img_y > 1 << 24)
								throw new Exception("too large");
							if (z.img_x > 1 << 24)
								throw new Exception("too large");
							z.depth = s.stbi__get8();
							if (z.depth != 1 && z.depth != 2 && z.depth != 4 && z.depth != 8 && z.depth != 16)
								throw new Exception("1/2/4/8/16-bit only");
							color = s.stbi__get8();
							if (color > 6)
								throw new Exception("bad ctype");
							if (color == 3 && z.depth == 16)
								throw new Exception("bad ctype");
							if (color == 3)
								pal_img_n = 3;
							else if ((color & 1) != 0)
								throw new Exception("bad ctype");
							comp = s.stbi__get8();
							if (comp != 0)
								throw new Exception("bad comp method");
							filter = s.stbi__get8();
							if (filter != 0)
								throw new Exception("bad filter method");
							interlace = s.stbi__get8();
							if (interlace > 1)
								throw new Exception("bad interlace method");
							if (z.img_x == 0 || z.img_y == 0)
								throw new Exception("0-pixel image");
							if (pal_img_n == 0)
							{
								z.img_n = ((color & 2) != 0 ? 3 : 1) + ((color & 4) != 0 ? 1 : 0);
								if ((1 << 30) / z.img_x / z.img_n < z.img_y)
									throw new Exception("too large");
							}
							else
							{
								z.img_n = 1;
								if ((1 << 30) / z.img_x / 4 < z.img_y)
									throw new Exception("too large");
							}

							break;
						}

					case ((uint)80 << 24) + ((uint)76 << 16) + ((uint)84 << 8) + 69:
						{
							if (first != 0)
								throw new Exception("first not IHDR");
							if (c.length > 256 * 3)
								throw new Exception("invalid PLTE");
							pal_len = c.length / 3;
							if (pal_len * 3 != c.length)
								throw new Exception("invalid PLTE");
							for (i = 0; i < pal_len; ++i)
							{
								palette[i * 4 + 0] = s.stbi__get8();
								palette[i * 4 + 1] = s.stbi__get8();
								palette[i * 4 + 2] = s.stbi__get8();
								palette[i * 4 + 3] = 255;
							}

							break;
						}

					case ((uint)116 << 24) + ((uint)82 << 16) + ((uint)78 << 8) + 83:
						{
							if (first != 0)
								throw new Exception("first not IHDR");
							if (!z.idata.IsNull)
								throw new Exception("tRNS after IDAT");
							if (pal_img_n != 0)
							{
								if (scan == Constants.STBI__SCAN_header)
								{
									z.img_n = 4;
									return 1;
								}

								if (pal_len == 0)
									throw new Exception("tRNS before PLTE");
								if (c.length > pal_len)
									throw new Exception("bad tRNS len");
								pal_img_n = 4;
								for (i = 0; i < c.length; ++i)
									palette[i * 4 + 3] = s.stbi__get8();
							}
							else
							{
								if ((z.img_n & 1) == 0)
									throw new Exception("tRNS with alpha");
								if (c.length != (uint)z.img_n * 2)
									throw new Exception("bad tRNS len");
								has_trans = 1;
								if (scan == Constants.STBI__SCAN_header)
								{
									++z.img_n;
									return 1;
								}

								if (z.depth == 16)
									for (k = 0; k < z.img_n && k < 3; ++k)
										tc16[k] = (ushort)s.stbi__get16be();
								else
									for (k = 0; k < z.img_n && k < 3; ++k)
										tc[k] = (byte)((byte)(s.stbi__get16be() & 255) * stbi__depth_scale_table[z.depth]);
							}

							break;
						}

					case ((uint)73 << 24) + ((uint)68 << 16) + ((uint)65 << 8) + 84:
						{
							if (first != 0)
								throw new Exception("first not IHDR");
							if (pal_img_n != 0 && pal_len == 0)
								throw new Exception("no PLTE");
							if (scan == Constants.STBI__SCAN_header)
							{
								if (pal_img_n != 0)
									z.img_n = pal_img_n;
								return 1;
							}

							if (c.length > 1u << 30)
								throw new Exception("IDAT size limit");
							if ((int)(ioff + c.length) < (int)ioff)
								return 0;
							if (ioff + c.length > idata_limit)
							{
								if (idata_limit == 0)
									idata_limit = c.length > 4096 ? c.length : 4096;
								while (ioff + c.length > idata_limit)
									idata_limit *= 2;

								if (z.idata.IsNull)
								{
									z.idata = new DataBuffer<byte>(idata_limit);
								}
								else
								{
									z.idata = z.idata.Expand(idata_limit);
								}
							}

							if (z.idata.IsNull)
							{
								z.idata = new DataBuffer<byte>((int)(ioff + c.length));
								idata_limit = (uint)z.idata.Length;
							}

							if (s.stbi__getn(z.idata + (int)ioff, (int)c.length) == 0)
								throw new Exception("outofdata");
							ioff += c.length;
							break;
						}

					case ((uint)73 << 24) + ((uint)69 << 16) + ((uint)78 << 8) + 68:
						{
							uint raw_len = 0;
							uint bpl = 0;
							if (first != 0)
								throw new Exception("first not IHDR");
							if (scan != Constants.STBI__SCAN_load)
								return 1;
							if (z.idata.IsNull)
								throw new Exception("no IDAT");
							bpl = (uint)((z.img_x * z.depth + 7) / 8);
							raw_len = (uint)(bpl * z.img_y * z.img_n + z.img_y);
							z.expanded = stbi_zlib_decode_malloc_guesssize_headerflag(z.idata, (int)ioff, (int)raw_len, is_iphone == 0 ? 1 : 0);
							if (z.expanded == null)
								return 0;
							z.idata = DataBuffer<byte>.Null;
							if ((req_comp == z.img_n + 1 && req_comp != 3 && pal_img_n == 0) || has_trans != 0)
								z.img_out_n = z.img_n + 1;
							else
								z.img_out_n = z.img_n;

							var ptr = new DataBuffer<byte>(z.expanded);
							if (stbi__create_png_image(z, ptr, (uint)z.expanded.Length, z.img_out_n, z.depth, color, interlace) == 0)
								return 0;

							if (has_trans != 0)
							{
								if (z.depth == 16)
								{
									throw new NotSupportedException("16-bit PNGs aren't supported");
								}
								else
								{
									stbi__compute_transparency(z, tc, z.img_out_n);
								}
							}

							if (is_iphone != 0 && z.de_iphone && z.img_out_n > 2)
							{
								stbi__de_iphone(z);
							}
							if (pal_img_n != 0)
							{
								z.img_n = pal_img_n;
								z.img_out_n = pal_img_n;
								if (req_comp >= 3)
									z.img_out_n = req_comp;
								if (stbi__expand_png_palette(z, palette, (int)pal_len, z.img_out_n) == 0)
									return 0;
							}
							else if (has_trans != 0)
							{
								++z.img_n;
							}

							z.expanded = null;
							s.stbi__get32be();
							return 1;
						}

					default:
						if (first != 0)
							throw new Exception("first not IHDR");
						if ((c.type & (1 << 29)) == 0)
						{
							stbi__parse_png_file_invalid_chunk[0] = (char)((c.type >> 24) & 255);
							stbi__parse_png_file_invalid_chunk[1] = (char)((c.type >> 16) & 255);
							stbi__parse_png_file_invalid_chunk[2] = (char)((c.type >> 8) & 255);
							stbi__parse_png_file_invalid_chunk[3] = (char)((c.type >> 0) & 255);
							throw new Exception(new string(stbi__parse_png_file_invalid_chunk));
						}

						s.stbi__skip((int)c.length);
						break;
				}

				s.stbi__get32be();
			}
		}

		private static DataBuffer<byte> stbi__do_png(stbi__png p, int req_comp)
		{
			DataBuffer<byte> result = DataBuffer<byte>.Null;
			if (req_comp < 0 || req_comp > 4)
				throw new Exception("bad req_comp");

			if (stbi__parse_png_file(p, Constants.STBI__SCAN_load, req_comp) != 0)
			{
				if (p.depth > 8)
				{
					throw new NotSupportedException($"{p.depth}-bit PNGs aren't supported");
				}

				result = p._out_;
				p._out_ = DataBuffer<byte>.Null;
				if (req_comp != 0 && req_comp != p.img_out_n)
				{
					result = Utility.stbi__convert_format(result, p.img_out_n, req_comp, p.img_x, p.img_y);
					p.img_out_n = req_comp;

					return result;
				}
			}

			p._out_ = DataBuffer<byte>.Null;
			p.expanded = null;
			p.idata = DataBuffer<byte>.Null;
			return result;
		}

		private static int stbi__png_info_raw(stbi__png p, out int x, out int y, out int comp)
		{
			x = y = comp = 0;
			if (stbi__parse_png_file(p, Constants.STBI__SCAN_header, 0) == 0)
			{
				p.s.stbi__rewind();
				return 0;
			}

			x = (int)p.img_x;
			y = (int)p.img_y;
			comp = p.img_n;
			return 1;
		}

		private static byte[] stbi_zlib_decode_malloc_guesssize_headerflag(DataBuffer<byte> buffer, int len, int initial_size, int parse_header)
		{
			if (buffer.IsNull || len <= 0)
				return null;

			int deflateOffset = 0;
			int deflateLength = len;

			if (parse_header != 0)
			{
				// Zlib format: 2 byte header + deflate data + 4 byte Adler32
				if (len < 6)
					return null;

				byte cmf = buffer[0];
				byte flg = buffer[1];
				int cm = cmf & 0x0F;
				if (((cmf * 256 + flg) % 31) != 0)
				{
					stbi__err("bad zlib header");
					return null;
				}
				if ((flg & 0x20) != 0)
				{
					stbi__err("no preset dict");
					return null;
				}
				if (cm != 8)
				{
					stbi__err("bad compression");
					return null;
				}

				deflateOffset = 2;
				deflateLength = len - 6; // exclude header and trailer
				if (deflateLength < 0)
					return null;
			}
			else
			{
				// Raw deflate (no header/trailer)
				deflateOffset = 0;
				deflateLength = len;
			}

			using (var ums = new MemoryStream(buffer.Data, (int)buffer.Offset + deflateOffset, deflateLength))
			using (var deflateStream = new DeflateStream(ums, CompressionMode.Decompress))
			using (var outMs = new MemoryStream(initial_size > 0 ? initial_size : 16384))
			{
				deflateStream.CopyTo(outMs);
				return outMs.ToArray();
			}
		}

		private class stbi__png
		{
			public DataBuffer<byte> _out_;
			public int depth;
			public byte[] expanded;
			public DataBuffer<byte> idata;
			public Stream s;
			public uint img_x;
			public uint img_y;
			public int img_n;
			public int img_out_n;
			public bool de_iphone;
			public bool iphone_unpremultiply;
		}

		[StructLayout(LayoutKind.Sequential)]
		private struct stbi__pngchunk
		{
			public uint length;
			public uint type;
		}
	}
}