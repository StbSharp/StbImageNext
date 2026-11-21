using System;
using System.IO;

namespace StbImageNext
{
	internal static class Utility
	{
		private const long DBL_EXP_MASK = 0x7ff0000000000000L;
		private const int DBL_MANT_BITS = 52;
		private const long DBL_SGN_MASK = -1 - 0x7fffffffffffffffL;
		private const long DBL_MANT_MASK = 0x000fffffffffffffL;
		private const long DBL_EXP_CLR_MASK = DBL_SGN_MASK | DBL_MANT_MASK;
		private static int[] stbi__shiftsigned_mul_table = { 0, 0xff, 0x55, 0x49, 0x11, 0x21, 0x41, 0x81, 0x01 };
		private static int[] stbi__shiftsigned_shift_table = { 0, 0, 0, 1, 0, 2, 4, 6, 0 };

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

		public static void Move<T>(this T[] destination, T[] source, int size)
		{
			var temp = new T[size];

			Array.Copy(source, temp, size);
			Array.Copy(temp, destination, size);
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

		public static int stbi__getn(this Stream s, byte[] buf, int offset, int size)
		{
			return s.Read(buf, offset, size);
		}

		public static bool stbi__at_eof(this Stream s)
		{
			return s.Position >= s.Length;
		}

		public static byte stbi__compute_y(int r, int g, int b)
		{
			return (byte)((r * 77 + g * 150 + 29 * b) >> 8);
		}


		public static byte[] stbi__convert_format(byte[] data, int img_n, int req_comp, uint x, uint y)
		{
			if (req_comp == img_n)
				return data;

			var w = (int)x;
			var good = new byte[req_comp * x * y];
			for (var j = 0; j < (int)y; ++j)
			{
				var src = new ReadOnlySpan<byte>(data, j * w * img_n, w * img_n);
				var dest = new Span<byte>(good, j * w * req_comp, w * req_comp);
				var i = 0;
				switch (img_n * 8 + req_comp)
				{
					case 1 * 8 + 2:
						for (i = 0; i < w; ++i)
						{
							dest[i * 2] = src[i];
							dest[i * 2 + 1] = 255;
						}

						break;
					case 1 * 8 + 3:
						for (i = 0; i < w; ++i)
							dest[i * 3] = dest[i * 3 + 1] = dest[i * 3 + 2] = src[i];

						break;
					case 1 * 8 + 4:
						for (i = 0; i < w; ++i)
						{
							dest[i * 4] = dest[i * 4 + 1] = dest[i * 4 + 2] = src[i];
							dest[i * 4 + 3] = 255;
						}

						break;
					case 2 * 8 + 1:
						for (i = 0; i < w; ++i)
							dest[i] = src[i * 2];

						break;
					case 2 * 8 + 3:
						for (i = 0; i < w; ++i)
							dest[i * 3] = dest[i * 3 + 1] = dest[i * 3 + 2] = src[i * 2];

						break;
					case 2 * 8 + 4:
						for (i = 0; i < w; ++i)
						{
							dest[i * 4] = dest[i * 4 + 1] = dest[i * 4 + 2] = src[i * 2];
							dest[i * 4 + 3] = src[i * 2 + 1];
						}

						break;
					case 3 * 8 + 4:
						for (i = 0; i < w; ++i)
						{
							dest[i * 4] = src[i * 3];
							dest[i * 4 + 1] = src[i * 3 + 1];
							dest[i * 4 + 2] = src[i * 3 + 2];
							dest[i * 4 + 3] = 255;
						}

						break;
					case 3 * 8 + 1:
						for (i = 0; i < w; ++i)
							dest[i] = stbi__compute_y(src[i * 3], src[i * 3 + 1], src[i * 3 + 2]);

						break;
					case 3 * 8 + 2:
						for (i = 0; i < w; ++i)
						{
							dest[i * 2] = stbi__compute_y(src[i * 3], src[i * 3 + 1], src[i * 3 + 2]);
							dest[i * 2 + 1] = 255;
						}

						break;
					case 4 * 8 + 1:
						for (i = 0; i < w; ++i)
							dest[i] = stbi__compute_y(src[i * 4], src[i * 4 + 1], src[i * 4 + 2]);

						break;
					case 4 * 8 + 2:
						for (i = 0; i < w; ++i)
						{
							dest[i * 2] = stbi__compute_y(src[i * 4], src[i * 4 + 1], src[i * 4 + 2]);
							dest[i * 2 + 1] = src[i * 4 + 3];
						}

						break;
					case 4 * 8 + 3:
						for (i = 0; i < w; ++i)
						{
							dest[i * 3] = src[i * 4];
							dest[i * 3 + 1] = src[i * 4 + 1];
							dest[i * 3 + 2] = src[i * 4 + 2];
						}

						break;
					default:
						throw new NotSupportedException("unsupported conversion");
				}
			}

			return good;
		}

		public static ushort stbi__compute_y_16(int r, int g, int b)
		{
			return (ushort)((r * 77 + g * 150 + 29 * b) >> 8);
		}

		public static ushort[] stbi__convert_format16(ushort[] data, int img_n, int req_comp, uint x, uint y)
		{
			if (req_comp == img_n)
				return data;

			var w = (int)x;
			var good = new ushort[req_comp * x * y];
			for (var j = 0; j < (int)y; ++j)
			{
				var src = new ReadOnlySpan<ushort>(data, j * w * img_n, w * img_n);
				var dest = new Span<ushort>(good, j * w * req_comp, w * req_comp);
				var i = 0;
				switch (img_n * 8 + req_comp)
				{
					case 1 * 8 + 2:
						for (i = 0; i < w; ++i)
						{
							dest[i * 2] = src[i];
							dest[i * 2 + 1] = 0xffff;
						}

						break;
					case 1 * 8 + 3:
						for (i = 0; i < w; ++i)
							dest[i * 3] = dest[i * 3 + 1] = dest[i * 3 + 2] = src[i];

						break;
					case 1 * 8 + 4:
						for (i = 0; i < w; ++i)
						{
							dest[i * 4] = dest[i * 4 + 1] = dest[i * 4 + 2] = src[i];
							dest[i * 4 + 3] = 0xffff;
						}

						break;
					case 2 * 8 + 1:
						for (i = 0; i < w; ++i)
							dest[i] = src[i * 2];

						break;
					case 2 * 8 + 3:
						for (i = 0; i < w; ++i)
							dest[i * 3] = dest[i * 3 + 1] = dest[i * 3 + 2] = src[i * 2];

						break;
					case 2 * 8 + 4:
						for (i = 0; i < w; ++i)
						{
							dest[i * 4] = dest[i * 4 + 1] = dest[i * 4 + 2] = src[i * 2];
							dest[i * 4 + 3] = src[i * 2 + 1];
						}

						break;
					case 3 * 8 + 4:
						for (i = 0; i < w; ++i)
						{
							dest[i * 4] = src[i * 3];
							dest[i * 4 + 1] = src[i * 3 + 1];
							dest[i * 4 + 2] = src[i * 3 + 2];
							dest[i * 4 + 3] = 0xffff;
						}

						break;
					case 3 * 8 + 1:
						for (i = 0; i < w; ++i)
							dest[i] = stbi__compute_y_16(src[i * 3], src[i * 3 + 1], src[i * 3 + 2]);

						break;
					case 3 * 8 + 2:
						for (i = 0; i < w; ++i)
						{
							dest[i * 2] = stbi__compute_y_16(src[i * 3], src[i * 3 + 1], src[i * 3 + 2]);
							dest[i * 2 + 1] = 0xffff;
						}

						break;
					case 4 * 8 + 1:
						for (i = 0; i < w; ++i)
							dest[i] = stbi__compute_y_16(src[i * 4], src[i * 4 + 1], src[i * 4 + 2]);

						break;
					case 4 * 8 + 2:
						for (i = 0; i < w; ++i)
						{
							dest[i * 2] = stbi__compute_y_16(src[i * 4], src[i * 4 + 1], src[i * 4 + 2]);
							dest[i * 2 + 1] = src[i * 4 + 3];
						}

						break;
					case 4 * 8 + 3:
						for (i = 0; i < w; ++i)
						{
							dest[i * 3] = src[i * 4];
							dest[i * 3 + 1] = src[i * 4 + 1];
							dest[i * 3 + 2] = src[i * 4 + 2];
						}

						break;
					default:
						throw new NotSupportedException("unsupported conversion");
				}
			}

			return good;
		}

		public static byte[] stbi__convert_16_to_8(ushort[] orig)
		{
			var reduced = new byte[orig.Length];
			for (var i = 0; i < reduced.Length; ++i)
				reduced[i] = (byte)((orig[i] >> 8) & 0xFF);

			return reduced;
		}

		public static double ldexp(double number, int exponent)
		{
			return number * Math.Pow(2, exponent);
		}

		public static uint _lrotl(uint x, int y)
		{
			return (x << y) | (x >> (32 - y));
		}

		public static int stbi__shiftsigned(uint v, int shift, int bits)
		{
			if (shift < 0)
				v <<= -shift;
			else
				v >>= shift;
			v >>= 8 - bits;
			return (int)(v * stbi__shiftsigned_mul_table[bits]) >> stbi__shiftsigned_shift_table[bits];
		}

		public static int stbi__addints_valid(int a, int b)
		{
			if (a >= 0 != b >= 0)
				return 1;
			if (a < 0 && b < 0)
				return a >= -2147483647 - 1 - b ? 1 : 0;
			return a <= 2147483647 - b ? 1 : 0;
		}

		public static int stbi__mul2shorts_valid(int a, int b)
		{
			if (b == 0 || b == -1)
				return 1;
			if (a >= 0 == b >= 0)
				return a <= 32767 / b ? 1 : 0;
			if (b < 0)
				return a <= -32768 / b ? 1 : 0;
			return a >= -32768 / b ? 1 : 0;
		}

		public static byte stbi__clamp(int x)
		{
			if ((uint)x > 255)
			{
				if (x < 0)
					return 0;
				if (x > 255)
					return 255;
			}

			return (byte)x;
		}

		public static byte stbi__blinn_8x8(byte x, byte y)
		{
			var t = (uint)(x * y + 128);
			return (byte)((t + (t >> 8)) >> 8);
		}

		public static int stbi__bitreverse16(int n)
		{
			n = ((n & 0xAAAA) >> 1) | ((n & 0x5555) << 1);
			n = ((n & 0xCCCC) >> 2) | ((n & 0x3333) << 2);
			n = ((n & 0xF0F0) >> 4) | ((n & 0x0F0F) << 4);
			n = ((n & 0xFF00) >> 8) | ((n & 0x00FF) << 8);
			return n;
		}

		public static int stbi__bit_reverse(int v, int bits)
		{
			return stbi__bitreverse16(v) >> (16 - bits);
		}

		public static int stbi__high_bit(uint z)
		{
			var n = 0;
			if (z == 0)
				return -1;
			if (z >= 0x10000)
			{
				n += 16;
				z >>= 16;
			}

			if (z >= 0x00100)
			{
				n += 8;
				z >>= 8;
			}

			if (z >= 0x00010)
			{
				n += 4;
				z >>= 4;
			}

			if (z >= 0x00004)
			{
				n += 2;
				z >>= 2;
			}

			if (z >= 0x00002)
				n += 1;

			return n;
		}

		public static int stbi__bitcount(uint a)
		{
			a = (a & 0x55555555) + ((a >> 1) & 0x55555555);
			a = (a & 0x33333333) + ((a >> 2) & 0x33333333);
			a = (a + (a >> 4)) & 0x0f0f0f0f;
			a = a + (a >> 8);
			a = a + (a >> 16);
			return (int)(a & 0xff);
		}

		/// <summary>
		/// This code had been borrowed from here: https://github.com/MachineCognitis/C.math.NET
		/// </summary>
		/// <param name="number"></param>
		/// <param name="exponent"></param>
		/// <returns></returns>
		public static double frexp(double number, out int exponent)
		{
			var bits = BitConverter.DoubleToInt64Bits(number);
			var exp = (int)((bits & DBL_EXP_MASK) >> DBL_MANT_BITS);
			exponent = 0;

			if (exp == 0x7ff || number == 0D)
				number += number;
			else
			{
				// Not zero and finite.
				exponent = exp - 1022;
				if (exp == 0)
				{
					// Subnormal, scale number so that it is in [1, 2).
					number *= BitConverter.Int64BitsToDouble(0x4350000000000000L); // 2^54
					bits = BitConverter.DoubleToInt64Bits(number);
					exp = (int)((bits & DBL_EXP_MASK) >> DBL_MANT_BITS);
					exponent = exp - 1022 - 54;
				}

				// Set exponent to -1 so that number is in [0.5, 1).
				number = BitConverter.Int64BitsToDouble((bits & DBL_EXP_CLR_MASK) | 0x3fe0000000000000L);
			}

			return number;
		}
	}
}
