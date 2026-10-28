namespace StbImageNext
{
	partial class StbImage
	{
		public static int stbi__vertically_flip_on_load_global;
		public static int stbi__vertically_flip_on_load_local;
		public static int stbi__vertically_flip_on_load_set;
		public static int stbi__unpremultiply_on_load_global;
		public static int stbi__de_iphone_flag_global;
		public static int stbi__unpremultiply_on_load_local;
		public static int stbi__unpremultiply_on_load_set;
		public static int stbi__de_iphone_flag_local;
		public static int stbi__de_iphone_flag_set;

		public static void stbi_set_unpremultiply_on_load(int flag_true_if_should_unpremultiply)
		{
			stbi__unpremultiply_on_load_global = flag_true_if_should_unpremultiply;
		}

		public static void stbi_convert_iphone_png_to_rgb(int flag_true_if_should_convert)
		{
			stbi__de_iphone_flag_global = flag_true_if_should_convert;
		}

		public static void stbi_set_flip_vertically_on_load(int flag_true_if_should_flip)
		{
			stbi__vertically_flip_on_load_global = flag_true_if_should_flip;
		}

		public static void stbi_set_unpremultiply_on_load_thread(int flag_true_if_should_unpremultiply)
		{
			stbi__unpremultiply_on_load_local = flag_true_if_should_unpremultiply;
			stbi__unpremultiply_on_load_set = 1;
		}

		public static void stbi_convert_iphone_png_to_rgb_thread(int flag_true_if_should_convert)
		{
			stbi__de_iphone_flag_local = flag_true_if_should_convert;
			stbi__de_iphone_flag_set = 1;
		}

		public static void stbi_set_flip_vertically_on_load_thread(int flag_true_if_should_flip)
		{
			stbi__vertically_flip_on_load_local = flag_true_if_should_flip;
			stbi__vertically_flip_on_load_set = 1;
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

		public static byte stbi__compute_y(int r, int g, int b)
		{
			return (byte)((r * 77 + g * 150 + 29 * b) >> 8);
		}

		public static ushort stbi__compute_y_16(int r, int g, int b)
		{
			return (ushort)((r * 77 + g * 150 + 29 * b) >> 8);
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
	}
}