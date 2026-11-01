using System;
using System.IO;
using System.Text;

namespace StbImageNext.Encoders
{
	public static class BmpEncoder
	{
		internal static void stbiw__writefv(BinaryWriter output, string fmt, params object[] v)
		{
			var vindex = 0;
			for (var i = 0; i < fmt.Length; ++i)
			{
				var c = fmt[i];
				switch (c)
				{
					case ' ':
						break;
					case '1':
						{
							var x = (byte)((int)v[vindex++] & 0xff);
							output.Write(x);
							break;
						}
					case '2':
						{
							var x = (int)v[vindex++];
							var b = new byte[2];
							b[0] = (byte)(x & 0xff);
							b[1] = (byte)((x >> 8) & 0xff);
							output.Write(b);
							break;
						}
					case '4':
						{
							var x = Convert.ToUInt32(v[vindex++]);
							var b = new byte[4];
							b[0] = (byte)(x & 0xff);
							b[1] = (byte)((x >> 8) & 0xff);
							b[2] = (byte)((x >> 16) & 0xff);
							b[3] = (byte)((x >> 24) & 0xff);
							output.Write(b);
							break;
						}
				}
			}
		}

		internal static void stbiw__write_pixel(BinaryWriter output, int rgb_dir, int comp, int write_alpha, int expand_mono, FakePtr<byte> d)
		{
			var bg = new byte[] { 255, 0, 255 };
			var px = new byte[3];
			var k = 0;
			if (write_alpha < 0)
			{
				output.Write(d[comp - 1]);
			}
			switch (comp)
			{
				case 2:
				case 1:
					if (expand_mono != 0)
					{
						output.Write(d[0]);
						output.Write(d[0]);
						output.Write(d[0]);
					}
					else
					{
						output.Write(d[0]);
					}
					break;
				case 4:
				case 3:
					if (comp == 4 && write_alpha == 0)
					{
						for (k = 0; k < 3; ++k)
						{
							px[k] = (byte)(bg[k] + (d[k] - bg[k]) * d[3] / 255);
						}

						output.Write(px[1 - rgb_dir]);
						output.Write(px[1]);
						output.Write(px[1 + rgb_dir]);
						break;
					}

					output.Write(d[1 - rgb_dir]);
					output.Write(d[1]);
					output.Write(d[1 + rgb_dir]);
					break;
			}

			if (write_alpha > 0)
			{
				output.Write(d[comp - 1]);
			}
		}

		private static void stbiw__write_pixels(BinaryWriter output, int rgb_dir, int vdir, int x, int y, int comp,
			byte[] data, int write_alpha, int scanline_pad, int expand_mono)
		{
			var i = 0;
			var j = 0;
			var j_end = 0;
			if (y <= 0)
				return;
			if (EncodingOptions.FlipVerticallyOnWrite)
				vdir *= -1;
			if (vdir < 0)
			{
				j_end = -1;
				j = y - 1;
			}
			else
			{
				j_end = y;
				j = 0;
			}

			for (; j != j_end; j += vdir)
			{
				for (i = 0; i < x; ++i)
				{
					var d = new FakePtr<byte>(data, (j * x + i) * comp);
					stbiw__write_pixel(output, rgb_dir, comp, write_alpha, expand_mono, d);
				}

				for (i = 0; i < scanline_pad; ++i)
				{
					output.Write((byte)0);
				}
			}
		}

		internal static void stbiw__outfile(BinaryWriter output, int rgb_dir, int vdir, int x, int y, int comp,
			int expand_mono, byte[] data, int alpha, int pad, string fmt, params object[] v)
		{
			if (x < 0 || y < 0)
			{
				throw new ArgumentOutOfRangeException("Image dimensions must be non-negative.");
			}

			stbiw__writefv(output, fmt, v);
			stbiw__write_pixels(output, rgb_dir, vdir, x, y, comp, data, alpha, pad, expand_mono);
		}

		public static void EncodeToWriter(BinaryWriter output, byte[] data, int x, int y, ColorComponents compEnum)
		{
			var comp = (int)compEnum;
			if (comp != 4)
			{
				var pad = (-x * 3) & 3;
				stbiw__outfile(output, -1, -1, x, y, comp, 1, data, 0, pad, "11 4 22 44 44 22 444444", 66, 77,
					14 + 40 + (x * 3 + pad) * y, 0, 0, 14 + 40, 40, x, y, 1, 24, 0, 0, 0, 0, 0, 0);
				return;
			}

			stbiw__outfile(output, -1, -1, x, y, comp, 1, data, 1, 0,
				"11 4 22 44 44 22 444444 4444 4 444 444 444 444", 66, 77, 14 + 108 + x * y * 4, 0, 0, 14 + 108, 108, x,
				y, 1, 32, 3, 0, 0, 0, 0, 0, 0xff0000, 0xff00, 0xff, 0xff000000u, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0);
		}

		public static void EncodeToStream(Stream output, byte[] data, int x, int y, ColorComponents comp)
		{
			using (var writer = new BinaryWriter(output, Encoding.UTF8, true))
			{
				EncodeToWriter(writer, data, x, y, comp);
			}
		}

		public static byte[] EncodeToByteArray(byte[] data, int x, int y, ColorComponents comp)
		{
			using (var stream = new MemoryStream())
			{
				EncodeToStream(stream, data, x, y, comp);
				return stream.ToArray();
			}
		}
	}
}
