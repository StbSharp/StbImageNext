using StbImageNext.Hebron.Runtime;
using System;
using System.IO;
using System.Runtime.InteropServices;
using static StbImageNext.StbImage;

namespace StbImageNext.Decoders
{
	public static class GifImageDecoder
	{
		public static bool Test(Stream s)
		{
			try
			{
				var sz = 0;
				if (s.stbi__get8() != 71 || s.stbi__get8() != 73 || s.stbi__get8() != 70 || s.stbi__get8() != 56)
					return false;
				sz = s.stbi__get8();
				if (sz != 57 && sz != 55)
					return false;
				if (s.stbi__get8() != 97)
					return false;
				return true;
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
				var g = new stbi__gif();
				ParseHeader(s, g, true);

				var info = new ImageInfo
				{
					Width = g.w,
					Height = g.h,
					ColorComponents = g.colorComponents
				};

				return info;
			}
			finally
			{
				s.stbi__rewind();
			}
		}

		public static unsafe ImageResult Load(Stream s, ColorComponents req_comp)
		{
			var g = new stbi__gif();
			try
			{
				var u = stbi__gif_load_next(s, g, (int)req_comp, null);
				if (u == null)
				{
					throw new Exception("Unknown GIF loading error");
				}

				if (req_comp != ColorComponents.Default && req_comp != ColorComponents.RedGreenBlueAlpha)
				{
					u = Utility.stbi__convert_format(u, 4, (int)req_comp, (uint)g.w, (uint)g.h);
				}

				var result = new ImageResult
				{
					Width = g.w,
					Height = g.h,
					SourceComp = g.colorComponents
				};

				result.Comp = req_comp == ColorComponents.Default ? result.SourceComp : req_comp;
				result.Data = u.Data;
				return result;
			}
			finally
			{
				CRuntime.free(g.history);
				CRuntime.free(g.background);
			}

		}

		private static void stbi__gif_parse_colortable(Stream s, byte[][] pal, int num_entries, int transp)
		{
			var i = 0;
			for (i = 0; i < num_entries; ++i)
			{
				pal[i][2] = s.stbi__get8();
				pal[i][1] = s.stbi__get8();
				pal[i][0] = s.stbi__get8();
				pal[i][3] = (byte)(transp == i ? 0 : 255);
			}
		}

		private static void ParseHeader(Stream s, stbi__gif g, bool onlyInfo)
		{
			byte version = 0;
			if (s.stbi__get8() != 71 || s.stbi__get8() != 73 || s.stbi__get8() != 70 || s.stbi__get8() != 56)
				throw new Exception("not GIF");
			version = s.stbi__get8();
			if (version != 55 && version != 57)
				throw new Exception("not GIF");
			if (s.stbi__get8() != 97)
				throw new Exception("not GIF");

			g.w = s.stbi__get16le();
			g.h = s.stbi__get16le();
			g.flags = s.stbi__get8();
			g.bgindex = s.stbi__get8();
			g.ratio = s.stbi__get8();
			g.transparent = -1;

			if (g.w > 1 << 24)
				throw new Exception("too large");
			if (g.h > 1 << 24)
				throw new Exception("too large");

			g.colorComponents = ColorComponents.RedGreenBlueAlpha;

			if (!onlyInfo)
			{
				if ((g.flags & 0x80) != 0)
					stbi__gif_parse_colortable(s, g.pal, 2 << (g.flags & 7), -1);
			}
		}

		private static unsafe void stbi__out_gif_code(stbi__gif g, ushort code)
		{
			var idx = 0;
			if (g.codes[code].prefix >= 0)
				stbi__out_gif_code(g, (ushort)g.codes[code].prefix);
			if (g.cur_y >= g.max_y)
				return;
			idx = g.cur_x + g.cur_y;
			var p = g._out_.SubBuffer(idx);
			g.history[idx / 4] = 1;
			var c = g.color_table[g.codes[code].suffix];
			if (c[3] > 128)
			{
				p[0] = c[2];
				p[1] = c[1];
				p[2] = c[0];
				p[3] = c[3];
			}

			g.cur_x += 4;
			if (g.cur_x >= g.max_x)
			{
				g.cur_x = g.start_x;
				g.cur_y += g.step;
				while (g.cur_y >= g.max_y && g.parse > 0)
				{
					g.step = (1 << g.parse) * g.line_size;
					g.cur_y = g.start_y + (g.step >> 1);
					--g.parse;
				}
			}
		}

		private static unsafe bool stbi__process_gif_raster(Stream s, stbi__gif g)
		{
			byte lzw_cs = 0;
			var len = 0;
			var init_code = 0;
			uint first = 0;
			var codesize = 0;
			var codemask = 0;
			var avail = 0;
			var oldcode = 0;
			var bits = 0;
			var valid_bits = 0;
			var clear = 0;
			stbi__gif_lzw* p;
			lzw_cs = s.stbi__get8();
			if (lzw_cs > 12)
				return false;
			clear = 1 << lzw_cs;
			first = 1;
			codesize = lzw_cs + 1;
			codemask = (1 << codesize) - 1;
			bits = 0;
			valid_bits = 0;
			for (init_code = 0; init_code < clear; init_code++)
			{
				g.codes[init_code].prefix = -1;
				g.codes[init_code].first = (byte)init_code;
				g.codes[init_code].suffix = (byte)init_code;
			}

			avail = clear + 2;
			oldcode = -1;
			len = 0;
			for (; ; )
			{
				if (valid_bits < codesize)
				{
					if (len == 0)
					{
						len = s.stbi__get8();
						if (len == 0)
							return true;
					}

					--len;
					bits |= s.stbi__get8() << valid_bits;
					valid_bits += 8;
				}
				else
				{
					var code = bits & codemask;
					bits >>= codesize;
					valid_bits -= codesize;
					if (code == clear)
					{
						codesize = lzw_cs + 1;
						codemask = (1 << codesize) - 1;
						avail = clear + 2;
						oldcode = -1;
						first = 0;
					}
					else if (code == clear + 1)
					{
						s.stbi__skip(len);
						while ((len = s.stbi__get8()) > 0)
							s.stbi__skip(len);
						return true;
					}
					else if (code <= avail)
					{
						if (first != 0)
							throw new Exception("no clear code");
						if (oldcode >= 0)
						{
							fixed (stbi__gif_lzw* p2 = &g.codes[avail++])
							{
								p = p2;
								if (avail > 8192)
									throw new Exception("too many codes");
								p->prefix = (short)oldcode;
								p->first = g.codes[oldcode].first;
								p->suffix = code == avail ? p->first : g.codes[code].first;
							}
						}
						else if (code == avail)
						{
							throw new Exception("illegal code in raster");
						}

						stbi__out_gif_code(g, (ushort)code);
						if ((avail & codemask) == 0 && avail <= 0x0FFF)
						{
							codesize++;
							codemask = (1 << codesize) - 1;
						}

						oldcode = code;
					}
					else
					{
						throw new Exception("illegal code in raster");
					}
				}
			}
		}

		internal unsafe static DataBuffer<byte> stbi__gif_load_next(Stream s, stbi__gif g, int req_comp, byte* two_back)
		{
			var dispose = 0;
			var first_frame = 0;
			var pi = 0;
			var pcount = 0;
			first_frame = 0;
			if (g._out_ == null)
			{
				ParseHeader(s, g, false);
				if (stbi__mad3sizes_valid(4, g.w, g.h, 0) == 0)
					throw new Exception("too large");
				pcount = g.w * g.h;
				g._out_ = new DataBuffer<byte>(4 * pcount);
				g.background = (byte*)stbi__malloc((ulong)(4 * pcount));
				g.history = (byte*)stbi__malloc((ulong)pcount);
				if (g._out_ == null || g.background == null || g.history == null)
					throw new Exception("outofmem");
				g._out_.Set(0);
				CRuntime.memset(g.background, 0x00, (ulong)(4 * pcount));
				CRuntime.memset(g.history, 0x00, (ulong)pcount);
				first_frame = 1;
			}
			else
			{
				dispose = (g.eflags & 0x1C) >> 2;
				pcount = g.w * g.h;
				if (dispose == 3 && two_back == null)
					dispose = 2;
				if (dispose == 3)
				{
					for (pi = 0; pi < pcount; ++pi)
						if (g.history[pi] != 0)
						{
							g._out_.Copy(pi * 4, &two_back[pi * 4], 4);
						}
				}
				else if (dispose == 2)
				{
					for (pi = 0; pi < pcount; ++pi)
						if (g.history[pi] != 0)
						{
							g._out_.Copy(pi * 4, &g.background[pi * 4], 4);
						}
				}

				fixed (byte* ptr = g._out_.Data)
				{
					CRuntime.memcpy(g.background, ptr, (ulong)(4 * g.w * g.h));
				}
			}

			CRuntime.memset(g.history, 0x00, (ulong)(g.w * g.h));
			for (; ; )
			{
				int tag = s.stbi__get8();
				switch (tag)
				{
					case 0x2C:
						{
							var x = 0;
							var y = 0;
							var w = 0;
							var h = 0;
							x = s.stbi__get16le();
							y = s.stbi__get16le();
							w = s.stbi__get16le();
							h = s.stbi__get16le();
							if (x + w > g.w || y + h > g.h)
								throw new Exception("bad Image Descriptor");
							g.line_size = g.w * 4;
							g.start_x = x * 4;
							g.start_y = y * g.line_size;
							g.max_x = g.start_x + w * 4;
							g.max_y = g.start_y + h * g.line_size;
							g.cur_x = g.start_x;
							g.cur_y = g.start_y;
							if (w == 0)
								g.cur_y = g.max_y;
							g.lflags = s.stbi__get8();
							if ((g.lflags & 0x40) != 0)
							{
								g.step = 8 * g.line_size;
								g.parse = 3;
							}
							else
							{
								g.step = g.line_size;
								g.parse = 0;
							}

							if ((g.lflags & 0x80) != 0)
							{
								stbi__gif_parse_colortable(s, g.lpal, 2 << (g.lflags & 7),
									(g.eflags & 0x01) != 0 ? g.transparent : -1);
								g.color_table = g.lpal;
							}
							else if ((g.flags & 0x80) != 0)
							{
								g.color_table = g.pal;
							}
							else
							{
								throw new Exception("missing color table");
							}

							if (!stbi__process_gif_raster(s, g))
							{
								return null;
							}

							pcount = g.w * g.h;
							if (first_frame != 0 && g.bgindex > 0)
								for (pi = 0; pi < pcount; ++pi)
									if (g.history[pi] == 0)
									{
										g.pal[g.bgindex][3] = 255;
										fixed (byte* ptr = &g.pal[g.bgindex][0])
										{
											g._out_.Copy(pi * 4, ptr, 4);
										}
									}

							return g._out_;
						}

					case 0x21:
						{
							var len = 0;
							int ext = s.stbi__get8();
							if (ext == 0xF9)
							{
								len = s.stbi__get8();
								if (len == 4)
								{
									g.eflags = s.stbi__get8();
									g.delay = 10 * s.stbi__get16le();
									if (g.transparent >= 0)
										g.pal[g.transparent][3] = 255;
									if ((g.eflags & 0x01) != 0)
									{
										g.transparent = s.stbi__get8();
										if (g.transparent >= 0)
											g.pal[g.transparent][3] = 0;
									}
									else
									{
										s.stbi__skip(1);
										g.transparent = -1;
									}
								}
								else
								{
									s.stbi__skip(len);
									break;
								}
							}

							while ((len = s.stbi__get8()) != 0)
								s.stbi__skip(len);
							break;
						}

					case 0x3B:
						return null;
					default:
						throw new Exception("unknown code");
				}
			}
		}


		internal unsafe class stbi__gif
		{
			public DataBuffer<byte> _out_;
			public byte* background;
			public int bgindex;
			public stbi__gif_lzw[] codes = new stbi__gif_lzw[8192];
			public byte[][] color_table;
			public int cur_x;
			public int cur_y;
			public int delay;
			public int eflags;
			public int flags;
			public int h;
			public byte* history;
			public int lflags;
			public int line_size;
			public byte[][] lpal = Utility.CreateArray<byte>(256, 4);
			public int max_x;
			public int max_y;
			public byte[][] pal = Utility.CreateArray<byte>(256, 4);
			public int parse;
			public int ratio;
			public int start_x;
			public int start_y;
			public int step;
			public int transparent;
			public int w;
			public ColorComponents colorComponents;
		}

		[StructLayout(LayoutKind.Sequential)]
		internal struct stbi__gif_lzw
		{
			public short prefix;
			public byte first;
			public byte suffix;
		}
	}
}
