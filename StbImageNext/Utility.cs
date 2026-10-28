using System;
using System.IO;

namespace StbImageNext
{
	internal static class Utility
	{
		public static T[][] CreateArray<T>(int d1, int d2)
		{
			var result = new T[d1][];
			for (int i = 0; i < d1; i++)
			{
				result[i] = new T[d2];
			}

			return result;
		}

		public static void Set<T>(this T[] array, T value)
		{
			for (var i = 0; i < array.Length; ++i)
			{
				array[i] = value;
			}
		}

		public static byte stbi__get8(this Stream s)
		{
			var b = s.ReadByte();
			if (b == -1) return 0;

			return (byte)b;
		}

		public static int stbi__get16be(this Stream s)
		{
			int z = s.stbi__get8();
			return (z << 8) + s.stbi__get8();
		}

		public static uint stbi__get32be(this Stream s)
		{
			var z = (uint)s.stbi__get16be();
			return (uint)((z << 16) + s.stbi__get16be());
		}

		public static int stbi__get16le(this Stream s)
		{
			int z = s.stbi__get8();
			return z + (s.stbi__get8() << 8);
		}

		public static uint stbi__get32le(this Stream s)
		{
			var z = (uint)s.stbi__get16le();
			z += (uint)s.stbi__get16le() << 16;
			return z;
		}

		public static void stbi__rewind(this Stream s)
		{
			s.Seek(0, SeekOrigin.Begin);
		}

		public static void stbi__skip(this Stream s, int skip)
		{
			s.Seek(skip, SeekOrigin.Current);
		}

		public static int stbi__getn(this Stream s, DataBuffer<byte> buf, int size)
		{
			return s.Read(buf.Data, (int)buf.Offset, size);
		}

		public static bool stbi__at_eof(this Stream s)
		{
			return s.Position >= s.Length;
		}

		private static byte stbi__compute_y(int r, int g, int b)
		{
			return (byte)((r * 77 + g * 150 + 29 * b) >> 8);
		}


		public static DataBuffer<byte> stbi__convert_format(DataBuffer<byte> data, int img_n, int req_comp, uint x, uint y)
		{
			var i = 0;
			var j = 0;
			if (req_comp == img_n)
				return data;

			var good = new DataBuffer<byte>(req_comp * x * y);
			for (j = 0; j < (int)y; ++j)
			{
				var src = data + j * x * img_n;
				var dest = good + j * x * req_comp;
				switch (img_n * 8 + req_comp)
				{
					case 1 * 8 + 2:
						for (i = (int)(x - 1); i >= 0; --i, src += 1, dest += 2)
						{
							dest[0] = src[0];
							dest[1] = 255;
						}

						break;
					case 1 * 8 + 3:
						for (i = (int)(x - 1); i >= 0; --i, src += 1, dest += 3)
							dest[0] = dest[1] = dest[2] = src[0];

						break;
					case 1 * 8 + 4:
						for (i = (int)(x - 1); i >= 0; --i, src += 1, dest += 4)
						{
							dest[0] = dest[1] = dest[2] = src[0];
							dest[3] = 255;
						}

						break;
					case 2 * 8 + 1:
						for (i = (int)(x - 1); i >= 0; --i, src += 2, dest += 1)
							dest[0] = src[0];

						break;
					case 2 * 8 + 3:
						for (i = (int)(x - 1); i >= 0; --i, src += 2, dest += 3)
							dest[0] = dest[1] = dest[2] = src[0];

						break;
					case 2 * 8 + 4:
						for (i = (int)(x - 1); i >= 0; --i, src += 2, dest += 4)
						{
							dest[0] = dest[1] = dest[2] = src[0];
							dest[3] = src[1];
						}

						break;
					case 3 * 8 + 4:
						for (i = (int)(x - 1); i >= 0; --i, src += 3, dest += 4)
						{
							dest[0] = src[0];
							dest[1] = src[1];
							dest[2] = src[2];
							dest[3] = 255;
						}

						break;
					case 3 * 8 + 1:
						for (i = (int)(x - 1); i >= 0; --i, src += 3, dest += 1)
							dest[0] = stbi__compute_y(src[0], src[1], src[2]);

						break;
					case 3 * 8 + 2:
						for (i = (int)(x - 1); i >= 0; --i, src += 3, dest += 2)
						{
							dest[0] = stbi__compute_y(src[0], src[1], src[2]);
							dest[1] = 255;
						}

						break;
					case 4 * 8 + 1:
						for (i = (int)(x - 1); i >= 0; --i, src += 4, dest += 1)
							dest[0] = stbi__compute_y(src[0], src[1], src[2]);

						break;
					case 4 * 8 + 2:
						for (i = (int)(x - 1); i >= 0; --i, src += 4, dest += 2)
						{
							dest[0] = stbi__compute_y(src[0], src[1], src[2]);
							dest[1] = src[3];
						}

						break;
					case 4 * 8 + 3:
						for (i = (int)(x - 1); i >= 0; --i, src += 4, dest += 3)
						{
							dest[0] = src[0];
							dest[1] = src[1];
							dest[2] = src[2];
						}

						break;
					default:
						throw new NotSupportedException("unsupported conversion");
				}
			}

			return good;
		}

		public static double ldexp(double number, int exponent)
		{
			return number * Math.Pow(2, exponent);
		}

		public static uint _lrotl(uint x, int y)
		{
			return (x << y) | (x >> (32 - y));
		}
	}
}
