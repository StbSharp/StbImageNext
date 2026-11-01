using System;
using System.IO;
using System.Text;

namespace StbImageNext.Encoders
{
	public static class TgaEncoder
	{
		public static bool WriteWithRle { get; set; } = true;

		public static void EncodeToWriter(BinaryWriter s, byte[] data, int x, int y, ColorComponents compEnum)
		{
			if (x < 0)
			{
				throw new ArgumentOutOfRangeException(nameof(x));
			}

			if (y < 0)
			{
				throw new ArgumentOutOfRangeException(nameof(y));
			}

			var comp = (int)compEnum;
			var has_alpha = comp == 2 || comp == 4 ? 1 : 0;
			var colorbytes = has_alpha != 0 ? comp - 1 : comp;
			var format = colorbytes < 2 ? 3 : 2;
			if (WriteWithRle)
			{
				BmpEncoder.stbiw__outfile(s, -1, -1, x, y, comp, 0, data, has_alpha, 0, "111 221 2222 11", 0, 0, format, 0,
					0, 0, 0, 0, x, y, (colorbytes + has_alpha) * 8, has_alpha * 8);

				return;
			}
			var i = 0;
			var j = 0;
			var k = 0;
			var jend = 0;
			var jdir = 0;
			BmpEncoder.stbiw__writefv(s, "111 221 2222 11", 0, 0, format + 8, 0, 0, 0, 0, 0, x, y, (colorbytes + has_alpha) * 8,
				has_alpha * 8);
			if (EncodingOptions.FlipVerticallyOnWrite)
			{
				j = 0;
				jend = y;
				jdir = 1;
			}
			else
			{
				j = y - 1;
				jend = -1;
				jdir = -1;
			}

			for (; j != jend; j += jdir)
			{
				var row = new FakePtr<byte>(data, +j * x * comp);
				var len = 0;
				for (i = 0; i < x; i += len)
				{
					var begin = row + i * comp;
					var diff = 1;
					len = 1;
					if (i < x - 1)
					{
						++len;
						diff = begin.Compare(row + (i + 1) * comp, comp);
						if (diff != 0)
						{
							var prev = begin;
							for (k = i + 2; k < x && len < 128; ++k)
								if (prev.Compare(row + k * comp, comp) != 0)
								{
									prev += comp;
									++len;
								}
								else
								{
									--len;
									break;
								}
						}
						else
						{
							for (k = i + 2; k < x && len < 128; ++k)
								if (begin.Compare(row + k * comp, comp) == 0)
									++len;
								else
									break;
						}
					}

					if (diff != 0)
					{
						var header = (byte)((len - 1) & 0xff);
						s.Write(header);
						for (k = 0; k < len; ++k)
							BmpEncoder.stbiw__write_pixel(s, -1, comp, has_alpha, 0, begin + k * comp);
					}
					else
					{
						var header = (byte)((len - 129) & 0xff);
						s.Write(header);
						BmpEncoder.stbiw__write_pixel(s, -1, comp, has_alpha, 0, begin);
					}
				}
			}
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
			using (var ms = new MemoryStream())
			{
				EncodeToStream(ms, data, x, y, comp);
				return ms.ToArray();
			}
		}
	}
}
