using System;
using System.IO;
using System.Runtime.InteropServices;
using static StbImageNext.StbImage;

namespace StbImageNext.Decoders
{
	public static class BmpImageDecoder
	{
		public static bool Test(Stream s)
		{
			try
			{
				if (s.stbi__get8() != 66)
					return false;
				if (s.stbi__get8() != 77)
					return false;
				s.stbi__get32le();
				s.stbi__get16le();
				s.stbi__get16le();
				s.stbi__get32le();
				var sz = (int)s.stbi__get32le();

				return sz == 12 || sz == 40 || sz == 56 || sz == 108 || sz == 124;
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
				var bmpInfo = stbi__bmp_parse_header(s);

				var info = new ImageInfo
				{
					Width = (int)bmpInfo.img_x,
					Height = (int)bmpInfo.img_y
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
				s.stbi__rewind();
			}
		}

		public static ImageResult Load(Stream s, ColorComponents? req_comp = null)
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

			flip_vertically = (int)info.img_y > 0 ? 1 : 0;
			info.img_y = (uint)Math.Abs((int)info.img_y);
			if (info.img_y > 1 << 24)
				throw new Exception("too large");
			if (info.img_x > 1 << 24)
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
				var bytes_read_so_far = (int)s.Position;
				var header_limit = 1024;
				var extra_data_limit = 256 * 4;
				if (bytes_read_so_far <= 0 || bytes_read_so_far > header_limit)
					throw new Exception("bad header");

				if (info.offset < bytes_read_so_far || info.offset - bytes_read_so_far > extra_data_limit)
					throw new Exception("bad offset");
				s.stbi__skip(info.offset - bytes_read_so_far);
			}

			int img_n;
			if (info.bpp == 24 && ma == 0xff000000)
				img_n = 3;
			else
				img_n = ma != 0 ? 4 : 3;

			int target;
			if (req_comp != null && req_comp.Value >= ColorComponents.RedGreenBlue)
				target = (int)req_comp.Value;
			else
				target = img_n;

			if (stbi__mad3sizes_valid(target, (int)info.img_x, (int)info.img_y, 0) == 0)
				throw new Exception("too large");

			var _out_ = new DataBuffer<byte>(target * info.img_x * info.img_y);
			if (info.bpp < 16)
			{
				var z = 0;
				if (psize == 0 || psize > 256)
				{
					throw new Exception("invalid");
				}

				for (i = 0; i < psize; ++i)
				{
					pal[i][2] = s.stbi__get8();
					pal[i][1] = s.stbi__get8();
					pal[i][0] = s.stbi__get8();
					if (info.hsz != 12)
						s.stbi__get8();
					pal[i][3] = 255;
				}

				s.stbi__skip(info.offset - info.extra_read - info.hsz - psize * (info.hsz == 12 ? 3 : 4));
				if (info.bpp == 1)
				{
					width = (int)((info.img_x + 7) >> 3);
				}
				else if (info.bpp == 4)
				{
					width = (int)((info.img_x + 1) >> 1);
				}
				else if (info.bpp == 8)
				{
					width = (int)info.img_x;
				}
				else
				{
					throw new Exception("bad bpp");
				}

				pad = -width & 3;
				if (info.bpp == 1)
					for (j = 0; j < (int)info.img_y; ++j)
					{
						var bit_offset = 7;
						int v = s.stbi__get8();
						for (i = 0; i < (int)info.img_x; ++i)
						{
							var color = (v >> bit_offset) & 0x1;
							_out_[z++] = pal[color][0];
							_out_[z++] = pal[color][1];
							_out_[z++] = pal[color][2];
							if (target == 4)
								_out_[z++] = 255;
							if (i + 1 == (int)info.img_x)
								break;
							if (--bit_offset < 0)
							{
								bit_offset = 7;
								v = s.stbi__get8();
							}
						}

						s.stbi__skip(pad);
					}
				else
					for (j = 0; j < (int)info.img_y; ++j)
					{
						for (i = 0; i < (int)info.img_x; i += 2)
						{
							int v = s.stbi__get8();
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
							if (i + 1 == (int)info.img_x)
								break;
							v = info.bpp == 8 ? s.stbi__get8() : v2;
							_out_[z++] = pal[v][0];
							_out_[z++] = pal[v][1];
							_out_[z++] = pal[v][2];
							if (target == 4)
								_out_[z++] = 255;
						}

						s.stbi__skip(pad);
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
				s.stbi__skip(info.offset - info.extra_read - info.hsz);
				if (info.bpp == 24)
					width = (int)(3 * info.img_x);
				else if (info.bpp == 16)
					width = (int)(2 * info.img_x);
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

				for (j = 0; j < (int)info.img_y; ++j)
				{
					if (easy != 0)
					{
						for (i = 0; i < (int)info.img_x; ++i)
						{
							byte a = 0;
							_out_[z + 2] = s.stbi__get8();
							_out_[z + 1] = s.stbi__get8();
							_out_[z + 0] = s.stbi__get8();
							z += 3;
							a = (byte)(easy == 2 ? s.stbi__get8() : 255);
							all_a |= a;
							if (target == 4)
								_out_[z++] = a;
						}
					}
					else
					{
						var bpp = info.bpp;
						for (i = 0; i < (int)info.img_x; ++i)
						{
							var v = bpp == 16 ? (uint)s.stbi__get16le() : s.stbi__get32le();
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

					s.stbi__skip(pad);
				}
			}

			if (target == 4 && all_a == 0)
				for (i = (int)(4 * info.img_x * info.img_y - 1); i >= 0; i -= 4)
					_out_[i] = 255;

			if (flip_vertically != 0)
			{
				byte t = 0;
				for (j = 0; j < (int)info.img_y >> 1; ++j)
				{
					var p1 = _out_ + j * info.img_x * target;
					var p2 = _out_ + (info.img_y - 1 - j) * info.img_x * target;
					for (i = 0; i < (int)info.img_x * target; ++i)
					{
						t = p1[i];
						p1[i] = p2[i];
						p2[i] = t;
					}
				}
			}

			if (req_comp != null && req_comp != (ColorComponents)target)
			{
				_out_ = Utility.stbi__convert_format(_out_, target, (int)req_comp.Value, info.img_x, info.img_y);
			}

			var result = new ImageResult
			{
				Width = (int)info.img_x,
				Height = (int)info.img_y,
				SourceComp = (ColorComponents)img_n
			};
			result.Comp = req_comp ?? result.SourceComp;
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

		private static stbi__bmp_data stbi__bmp_parse_header(Stream s)
		{
			if (s.stbi__get8() != 66 || s.stbi__get8() != 77)
				throw new Exception("not BMP");
			s.stbi__get32le();
			s.stbi__get16le();
			s.stbi__get16le();

			var info = new stbi__bmp_data
			{
				all_a = 255,
				offset = (int)s.stbi__get32le()
			};

			var hsz = (int)s.stbi__get32le();
			info.hsz = hsz;
			info.mr = info.mg = info.mb = info.ma = 0;
			info.extra_read = 14;
			if (info.offset < 0)
				throw new Exception("bad BMP");
			if (hsz != 12 && hsz != 40 && hsz != 56 && hsz != 108 && hsz != 124)
				throw new Exception("unknown BMP");
			if (hsz == 12)
			{
				info.img_x = (uint)s.stbi__get16le();
				info.img_y = (uint)s.stbi__get16le();
			}
			else
			{
				info.img_x = s.stbi__get32le();
				info.img_y = s.stbi__get32le();
			}

			if (s.stbi__get16le() != 1)
				throw new Exception("bad BMP");
			info.bpp = s.stbi__get16le();
			if (hsz != 12)
			{
				var compress = (int)s.stbi__get32le();
				if (compress == 1 || compress == 2)
					throw new Exception("BMP RLE");
				if (compress >= 4)
					throw new Exception("BMP JPEG/PNG");
				if (compress == 3 && info.bpp != 16 && info.bpp != 32)
					throw new Exception("bad BMP");
				s.stbi__get32le();
				s.stbi__get32le();
				s.stbi__get32le();
				s.stbi__get32le();
				s.stbi__get32le();
				if (hsz == 40 || hsz == 56)
				{
					if (hsz == 56)
					{
						s.stbi__get32le();
						s.stbi__get32le();
						s.stbi__get32le();
						s.stbi__get32le();
					}

					if (info.bpp == 16 || info.bpp == 32)
					{
						if (compress == 0)
						{
							stbi__bmp_set_mask_defaults(ref info, compress);
						}
						else if (compress == 3)
						{
							info.mr = s.stbi__get32le();
							info.mg = s.stbi__get32le();
							info.mb = s.stbi__get32le();
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
					info.mr = s.stbi__get32le();
					info.mg = s.stbi__get32le();
					info.mb = s.stbi__get32le();
					info.ma = s.stbi__get32le();
					if (compress != 3)
						stbi__bmp_set_mask_defaults(ref info, compress);
					s.stbi__get32le();
					for (i = 0; i < 12; ++i)
						s.stbi__get32le();

					if (hsz == 124)
					{
						s.stbi__get32le();
						s.stbi__get32le();
						s.stbi__get32le();
						s.stbi__get32le();
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
		public uint img_x;
		public uint img_y;
	}
}
