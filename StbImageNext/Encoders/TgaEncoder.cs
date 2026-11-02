using System;
using System.IO;
using System.Text;

namespace StbImageNext.Encoders
{
	/// <summary>Provides TGA encoding functionality.</summary>
	public static class TgaEncoder
	{
		/// <summary>Gets or sets a value indicating whether run-length encoding (RLE) is used when writing TGA images.</summary>
		/// <value>True to use RLE; otherwise, false. The default is true.</value>
		public static bool WriteWithRle { get; set; } = true;

		/// <summary>Encodes image data to a BinaryWriter.</summary>
		/// <param name="s">The BinaryWriter to write to.</param>
		/// <param name="data">The image data.</param>
		/// <param name="width">The image width.</param>
		/// <param name="height">The image height.</param>
		/// <param name="comp">The color components.</param>
		public static void EncodeToWriter(BinaryWriter s, byte[] data, int width, int height, ColorComponents comp)
		{
			if (width <= 0)
			{
				throw new ArgumentOutOfRangeException(nameof(width));
			}

			if (height <= 0)
			{
				throw new ArgumentOutOfRangeException(nameof(height));
			}

			var compInt = (int)comp;
			var has_alpha = compInt == 2 || compInt == 4 ? 1 : 0;
			var colorbytes = has_alpha != 0 ? compInt - 1 : compInt;
			var format = colorbytes < 2 ? 3 : 2;
			if (WriteWithRle)
			{
				BmpEncoder.stbiw__outfile(s, -1, -1, width, height, compInt, 0, data, has_alpha, 0, "111 221 2222 11", 0, 0, format, 0,
					0, 0, 0, 0, width, height, (colorbytes + has_alpha) * 8, has_alpha * 8);

				return;
			}
			var i = 0;
			var j = 0;
			var k = 0;
			var jend = 0;
			var jdir = 0;
			BmpEncoder.stbiw__writefv(s, "111 221 2222 11", 0, 0, format + 8, 0, 0, 0, 0, 0, width, height, (colorbytes + has_alpha) * 8,
				has_alpha * 8);
			if (EncodingOptions.FlipVerticallyOnWrite)
			{
				j = 0;
				jend = height;
				jdir = 1;
			}
			else
			{
				j = height - 1;
				jend = -1;
				jdir = -1;
			}

			for (; j != jend; j += jdir)
			{
				var row = new FakePtr<byte>(data, +j * width * compInt);
				var len = 0;
				for (i = 0; i < width; i += len)
				{
					var begin = row + i * compInt;
					var diff = 1;
					len = 1;
					if (i < width - 1)
					{
						++len;
						diff = begin.Compare(row + (i + 1) * compInt, compInt);
						if (diff != 0)
						{
							var prev = begin;
							for (k = i + 2; k < width && len < 128; ++k)
								if (prev.Compare(row + k * compInt, compInt) != 0)
								{
									prev += compInt;
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
							for (k = i + 2; k < width && len < 128; ++k)
								if (begin.Compare(row + k * compInt, compInt) == 0)
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
							BmpEncoder.stbiw__write_pixel(s, -1, compInt, has_alpha, 0, begin + k * compInt);
					}
					else
					{
						var header = (byte)((len - 129) & 0xff);
						s.Write(header);
						BmpEncoder.stbiw__write_pixel(s, -1, compInt, has_alpha, 0, begin);
					}
				}
			}
		}

		/// <summary>Encodes image data to a Stream.</summary>
		/// <param name="output">The Stream to write to.</param>
		/// <param name="data">The image data.</param>
		/// <param name="width">The image width.</param>
		/// <param name="height">The image height.</param>
		/// <param name="comp">The color components.</param>
		public static void EncodeToStream(Stream output, byte[] data, int width, int height, ColorComponents comp)
		{
			using (var writer = new BinaryWriter(output, Encoding.UTF8, true))
			{
				EncodeToWriter(writer, data, width, height, comp);
			}
		}

		/// <summary>Encodes image data to a byte array.</summary>
		/// <param name="data">The image data.</param>
		/// <param name="width">The image width.</param>
		/// <param name="height">The image height.</param>
		/// <param name="comp">The color components.</param>
		/// <returns>The encoded image data as a byte array.</returns>
		public static byte[] EncodeToByteArray(byte[] data, int width, int height, ColorComponents comp)
		{
			using (var ms = new MemoryStream())
			{
				EncodeToStream(ms, data, width, height, comp);
				return ms.ToArray();
			}
		}
	}
}
