using StbImageNext.Hebron.Runtime;
using System;
using System.Runtime.InteropServices;
using static StbImageNext.StbImage;

namespace StbImageNext.Decoders
{
	public static class BmpImageDecoder
	{
		public static bool Test(stbi__context s)
		{
			try
			{
				if (stbi__get8(s) != 66)
					return false;
				if (stbi__get8(s) != 77)
					return false;
				stbi__get32le(s);
				stbi__get16le(s);
				stbi__get16le(s);
				stbi__get32le(s);
				var sz = (int)stbi__get32le(s);

				return sz == 12 || sz == 40 || sz == 56 || sz == 108 || sz == 124;
			}
			finally
			{
				stbi__rewind(s);
			}
		}

		public static ImageInfo Info(stbi__context s)
		{
			try
			{
				var bmpInfo = stbi__bmp_parse_header(s);

				var info = new ImageInfo
				{
					Width = (int)s.img_x,
					Height = (int)s.img_y
				};

				if (bmpInfo.bpp == 24 && bmpInfo.ma == 0xff000000)
				{
					info.ColorComponents = ColorComponents.RedGreenBlue;
				}
				else
				{
					info.ColorComponents = bmpInfo.ma != 0 ? ColorComponents.RedGreenBlueAlpha : ColorComponents.RedGreenBlue;
				}

				return info;
			}
			finally
			{
				stbi__rewind(s);
			}
		}

		public static ImageResult Load(stbi__context s, ColorComponents req_comp)
		{
			uint mr = 0;
			uint mg = 0;
			uint mb = 0;
			uint ma = 0;
			uint all_a = 0;
			var pal = Utility.CreateArray<byte>(256, 4);
			var psize = 0;
			var i = 0;
			var j = 0;
			var width = 0;
			var flip_vertically = 0;
			var pad = 0;

			var info = stbi__bmp_parse_header(s);

			flip_vertically = (int)s.img_y > 0 ? 1 : 0;
			s.img_y = (uint)CRuntime.abs((int)s.img_y);
			if (s.img_y > 1 << 24)
				throw new Exception("too large");
			if (s.img_x > 1 << 24)
				throw new Exception("too large");
			mr = info.mr;
			mg = info.mg;
			mb = info.mb;
			ma = info.ma;
			all_a = info.all_a;
			if (info.hsz == 12)
			{
				if (info.bpp < 24)
					psize = (info.offset - info.extra_read - 24) / 3;
			}
			else
			{
				if (info.bpp < 16)
					psize = (info.offset - info.extra_read - info.hsz) >> 2;
			}

			if (psize == 0)
			{
				var bytes_read_so_far = (int)s.Stream.Position;
				var header_limit = 1024;
				var extra_data_limit = 256 * 4;
				if (bytes_read_so_far <= 0 || bytes_read_so_far > header_limit)
					throw new Exception("bad header");

				if (info.offset < bytes_read_so_far || info.offset - bytes_read_so_far > extra_data_limit)
					throw new Exception("bad offset");
				stbi__skip(s, info.offset - bytes_read_so_far);
			}

			if (info.bpp == 24 && ma == 0xff000000)
				s.img_n = 3;
			else
				s.img_n = ma != 0 ? 4 : 3;

			int target;
			if (req_comp != ColorComponents.Default && req_comp >= ColorComponents.RedGreenBlue)
				target = (int)req_comp;
			else
				target = s.img_n;

			if (stbi__mad3sizes_valid(target, (int)s.img_x, (int)s.img_y, 0) == 0)
				throw new Exception("too large");

			var _out_ = new OutputBuffer<byte>(target * s.img_y * s.img_y);
			if (info.bpp < 16)
			{
				var z = 0;
				if (psize == 0 || psize > 256)
				{
					throw new Exception("invalid");
				}

				for (i = 0; i < psize; ++i)
				{
					pal[i][2] = stbi__get8(s);
					pal[i][1] = stbi__get8(s);
					pal[i][0] = stbi__get8(s);
					if (info.hsz != 12)
						stbi__get8(s);
					pal[i][3] = 255;
				}

				stbi__skip(s, info.offset - info.extra_read - info.hsz - psize * (info.hsz == 12 ? 3 : 4));
				if (info.bpp == 1)
				{
					width = (int)((s.img_x + 7) >> 3);
				}
				else if (info.bpp == 4)
				{
					width = (int)((s.img_x + 1) >> 1);
				}
				else if (info.bpp == 8)
				{
					width = (int)s.img_x;
				}
				else
				{
					throw new Exception("bad bpp");
				}

				pad = -width & 3;
				if (info.bpp == 1)
					for (j = 0; j < (int)s.img_y; ++j)
					{
						var bit_offset = 7;
						int v = stbi__get8(s);
						for (i = 0; i < (int)s.img_x; ++i)
						{
							var color = (v >> bit_offset) & 0x1;
							_out_[z++] = pal[color][0];
							_out_[z++] = pal[color][1];
							_out_[z++] = pal[color][2];
							if (target == 4)
								_out_[z++] = 255;
							if (i + 1 == (int)s.img_x)
								break;
							if (--bit_offset < 0)
							{
								bit_offset = 7;
								v = stbi__get8(s);
							}
						}

						stbi__skip(s, pad);
					}
				else
					for (j = 0; j < (int)s.img_y; ++j)
					{
						for (i = 0; i < (int)s.img_x; i += 2)
						{
							int v = stbi__get8(s);
							var v2 = 0;
							if (info.bpp == 4)
							{
								v2 = v & 15;
								v >>= 4;
							}

							_out_[z++] = pal[v][0];
							_out_[z++] = pal[v][1];
							_out_[z++] = pal[v][2];
							if (target == 4)
								_out_[z++] = 255;
							if (i + 1 == (int)s.img_x)
								break;
							v = info.bpp == 8 ? stbi__get8(s) : v2;
							_out_[z++] = pal[v][0];
							_out_[z++] = pal[v][1];
							_out_[z++] = pal[v][2];
							if (target == 4)
								_out_[z++] = 255;
						}

						stbi__skip(s, pad);
					}
			}
			else
			{
				var rshift = 0;
				var gshift = 0;
				var bshift = 0;
				var ashift = 0;
				var rcount = 0;
				var gcount = 0;
				var bcount = 0;
				var acount = 0;
				var z = 0;
				var easy = 0;
				stbi__skip(s, info.offset - info.extra_read - info.hsz);
				if (info.bpp == 24)
					width = (int)(3 * s.img_x);
				else if (info.bpp == 16)
					width = (int)(2 * s.img_x);
				else
					width = 0;
				pad = -width & 3;
				if (info.bpp == 24)
					easy = 1;
				else if (info.bpp == 32)
					if (mb == 0xff && mg == 0xff00 && mr == 0x00ff0000 && ma == 0xff000000)
						easy = 2;

				if (easy == 0)
				{
					if (mr == 0 || mg == 0 || mb == 0)
					{
						throw new Exception("bad masks");
					}

					rshift = stbi__high_bit(mr) - 7;
					rcount = stbi__bitcount(mr);
					gshift = stbi__high_bit(mg) - 7;
					gcount = stbi__bitcount(mg);
					bshift = stbi__high_bit(mb) - 7;
					bcount = stbi__bitcount(mb);
					ashift = stbi__high_bit(ma) - 7;
					acount = stbi__bitcount(ma);
					if (rcount > 8 || gcount > 8 || bcount > 8 || acount > 8)
					{
						throw new Exception("bad masks");
					}
				}

				for (j = 0; j < (int)s.img_y; ++j)
				{
					if (easy != 0)
					{
						for (i = 0; i < (int)s.img_x; ++i)
						{
							byte a = 0;
							_out_[z + 2] = stbi__get8(s);
							_out_[z + 1] = stbi__get8(s);
							_out_[z + 0] = stbi__get8(s);
							z += 3;
							a = (byte)(easy == 2 ? stbi__get8(s) : 255);
							all_a |= a;
							if (target == 4)
								_out_[z++] = a;
						}
					}
					else
					{
						var bpp = info.bpp;
						for (i = 0; i < (int)s.img_x; ++i)
						{
							var v = bpp == 16 ? (uint)stbi__get16le(s) : stbi__get32le(s);
							uint a = 0;
							_out_[z++] = (byte)(stbi__shiftsigned(v & mr, rshift, rcount) & 255);
							_out_[z++] = (byte)(stbi__shiftsigned(v & mg, gshift, gcount) & 255);
							_out_[z++] = (byte)(stbi__shiftsigned(v & mb, bshift, bcount) & 255);
							a = (uint)(ma != 0 ? stbi__shiftsigned(v & ma, ashift, acount) : 255);
							all_a |= a;
							if (target == 4)
								_out_[z++] = (byte)(a & 255);
						}
					}

					stbi__skip(s, pad);
				}
			}

			if (target == 4 && all_a == 0)
				for (i = (int)(4 * s.img_x * s.img_y - 1); i >= 0; i -= 4)
					_out_[i] = 255;

			if (flip_vertically != 0)
			{
				byte t = 0;
				for (j = 0; j < (int)s.img_y >> 1; ++j)
				{
					var p1 = _out_ + j * s.img_x * target;
					var p2 = _out_ + (s.img_y - 1 - j) * s.img_x * target;
					for (i = 0; i < (int)s.img_x * target; ++i)
					{
						t = p1[i];
						p1[i] = p2[i];
						p2[i] = t;
					}
				}
			}

			if (req_comp != ColorComponents.Default && req_comp != (ColorComponents)target)
			{
				_out_ = Utility.stbi__convert_format(_out_, target, (int)req_comp, s.img_x, s.img_y);
			}

			var result = new ImageResult
			{
				Width = (int)s.img_x,
				Height = (int)s.img_y,
				SourceComp = (ColorComponents)s.img_n
			};
			result.Comp = req_comp == ColorComponents.Default ? result.SourceComp : req_comp;
			result.Data = _out_.Data;

			return result;
		}


		private static int stbi__bmp_set_mask_defaults(ref stbi__bmp_data info, int compress)
		{
			if (compress == 3)
				return 1;
			if (compress == 0)
			{
				if (info.bpp == 16)
				{
					info.mr = 31u << 10;
					info.mg = 31u << 5;
					info.mb = 31u << 0;
				}
				else if (info.bpp == 32)
				{
					info.mr = 0xffu << 16;
					info.mg = 0xffu << 8;
					info.mb = 0xffu << 0;
					info.ma = 0xffu << 24;
					info.all_a = 0;
				}
				else
				{
					info.mr = info.mg = info.mb = info.ma = 0;
				}

				return 1;
			}

			return 0;
		}

		private static stbi__bmp_data stbi__bmp_parse_header(stbi__context s)
		{
			if (stbi__get8(s) != 66 || stbi__get8(s) != 77)
				throw new Exception("not BMP");
			stbi__get32le(s);
			stbi__get16le(s);
			stbi__get16le(s);

			var info = new stbi__bmp_data
			{
				all_a = 255,
				offset = (int)stbi__get32le(s)
			};

			var hsz = (int)stbi__get32le(s);
			info.hsz = hsz;
			info.mr = info.mg = info.mb = info.ma = 0;
			info.extra_read = 14;
			if (info.offset < 0)
				throw new Exception("bad BMP");
			if (hsz != 12 && hsz != 40 && hsz != 56 && hsz != 108 && hsz != 124)
				throw new Exception("unknown BMP");
			if (hsz == 12)
			{
				s.img_x = (uint)stbi__get16le(s);
				s.img_y = (uint)stbi__get16le(s);
			}
			else
			{
				s.img_x = stbi__get32le(s);
				s.img_y = stbi__get32le(s);
			}

			if (stbi__get16le(s) != 1)
				throw new Exception("bad BMP");
			info.bpp = stbi__get16le(s);
			if (hsz != 12)
			{
				var compress = (int)stbi__get32le(s);
				if (compress == 1 || compress == 2)
					throw new Exception("BMP RLE");
				if (compress >= 4)
					throw new Exception("BMP JPEG/PNG");
				if (compress == 3 && info.bpp != 16 && info.bpp != 32)
					throw new Exception("bad BMP");
				stbi__get32le(s);
				stbi__get32le(s);
				stbi__get32le(s);
				stbi__get32le(s);
				stbi__get32le(s);
				if (hsz == 40 || hsz == 56)
				{
					if (hsz == 56)
					{
						stbi__get32le(s);
						stbi__get32le(s);
						stbi__get32le(s);
						stbi__get32le(s);
					}

					if (info.bpp == 16 || info.bpp == 32)
					{
						if (compress == 0)
						{
							stbi__bmp_set_mask_defaults(ref info, compress);
						}
						else if (compress == 3)
						{
							info.mr = stbi__get32le(s);
							info.mg = stbi__get32le(s);
							info.mb = stbi__get32le(s);
							info.extra_read += 12;
							if (info.mr == info.mg && info.mg == info.mb)
								throw new Exception("bad BMP");
						}
						else
						{
							throw new Exception("bad BMP");
						}
					}
				}
				else
				{
					var i = 0;
					if (hsz != 108 && hsz != 124)
						throw new Exception("bad BMP");
					info.mr = stbi__get32le(s);
					info.mg = stbi__get32le(s);
					info.mb = stbi__get32le(s);
					info.ma = stbi__get32le(s);
					if (compress != 3)
						stbi__bmp_set_mask_defaults(ref info, compress);
					stbi__get32le(s);
					for (i = 0; i < 12; ++i)
						stbi__get32le(s);

					if (hsz == 124)
					{
						stbi__get32le(s);
						stbi__get32le(s);
						stbi__get32le(s);
						stbi__get32le(s);
					}
				}
			}

			return info;
		}
	}

	[StructLayout(LayoutKind.Sequential)]
	public struct stbi__bmp_data
	{
		public int bpp;
		public int offset;
		public int hsz;
		public uint mr;
		public uint mg;
		public uint mb;
		public uint ma;
		public uint all_a;
		public int extra_read;
	}
}
