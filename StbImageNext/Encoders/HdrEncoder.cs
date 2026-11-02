using System;
using System.IO;
using System.Text;

namespace StbImageNext.Encoders
{
	/// <summary>Provides HDR encoding functionality.</summary>
	public static class HdrEncoder
	{
		private static void stbiw__linear_to_rgbe(byte[] rgbe, float[] linear)
		{
			var exponent = 0;
			var maxcomp = linear[0] > (linear[1] > linear[2] ? linear[1] : linear[2]) ? linear[0] :
				linear[1] > linear[2] ? linear[1] : linear[2];
			if (maxcomp < 1e-32f)
			{
				rgbe[0] = rgbe[1] = rgbe[2] = rgbe[3] = 0;
			}
			else
			{
				var normalize = (float)Utility.frexp(maxcomp, out exponent) * 256.0f / maxcomp;
				rgbe[0] = (byte)(linear[0] * normalize);
				rgbe[1] = (byte)(linear[1] * normalize);
				rgbe[2] = (byte)(linear[2] * normalize);
				rgbe[3] = (byte)(exponent + 128);
			}
		}

		private static void stbiw__write_run_data(BinaryWriter s, int length, byte databyte)
		{
			var lengthbyte = (byte)((length + 128) & 0xff);
			s.Write(lengthbyte);
			s.Write(databyte);
		}

		private static void stbiw__write_dump_data(BinaryWriter s, int length, FakePtr<byte> data)
		{
			var lengthbyte = (byte)(length & 0xff);
			s.Write(lengthbyte);
			s.Write(data.Data, (int)data.Offset, length);
		}

		private static void stbiw__write_hdr_scanline(BinaryWriter output, int width, int ncomp, byte[] scratch, FakePtr<float> scanline)
		{
			var scanlineheader = new byte[] { 2, 2, 0, 0 };
			var rgbe = new byte[4];
			var linear = new float[3];
			var x = 0;
			scanlineheader[2] = (byte)((width & 0xff00) >> 8);
			scanlineheader[3] = (byte)(width & 0x00ff);
			if (width < 8 || width >= 32768)
			{
				for (x = 0; x < width; x++)
				{
					switch (ncomp)
					{
						case 4:
						case 3:
							linear[2] = scanline[x * ncomp + 2];
							linear[1] = scanline[x * ncomp + 1];
							linear[0] = scanline[x * ncomp + 0];
							break;
						default:
							linear[0] = linear[1] = linear[2] = scanline[x * ncomp + 0];
							break;
					}

					stbiw__linear_to_rgbe(rgbe, linear);
					output.Write(rgbe);
				}
			}
			else
			{
				var c = 0;
				var r = 0;
				for (x = 0; x < width; x++)
				{
					switch (ncomp)
					{
						case 4:
						case 3:
							linear[2] = scanline[x * ncomp + 2];
							linear[1] = scanline[x * ncomp + 1];
							linear[0] = scanline[x * ncomp + 0];
							break;
						default:
							linear[0] = linear[1] = linear[2] = scanline[x * ncomp + 0];
							break;
					}

					stbiw__linear_to_rgbe(rgbe, linear);
					scratch[x + width * 0] = rgbe[0];
					scratch[x + width * 1] = rgbe[1];
					scratch[x + width * 2] = rgbe[2];
					scratch[x + width * 3] = rgbe[3];
				}

				output.Write(scanlineheader);
				for (c = 0; c < 4; c++)
				{
					var comp = new FakePtr<byte>(scratch, width * c);
					x = 0;
					while (x < width)
					{
						r = x;
						while (r + 2 < width)
						{
							if (comp[r] == comp[r + 1] && comp[r] == comp[r + 2])
								break;
							++r;
						}

						if (r + 2 >= width)
							r = width;
						while (x < r)
						{
							var len = r - x;
							if (len > 128)
								len = 128;
							stbiw__write_dump_data(output, len, comp + x);
							x += len;
						}

						if (r + 2 < width)
						{
							while (r < width && comp[r] == comp[x])
								++r;
							while (x < r)
							{
								var len = r - x;
								if (len > 127)
									len = 127;
								stbiw__write_run_data(output, len, comp[x]);
								x += len;
							}
						}
					}
				}
			}
		}

		/// <summary>Encodes image data to a BinaryWriter.</summary>
		/// <param name="s">The BinaryWriter to write to.</param>
		/// <param name="data">The image data.</param>
		/// <param name="width">The image width.</param>
		/// <param name="height">The image height.</param>
		/// <param name="compEnum">The color components.</param>
		public static void EncodeToWriter(BinaryWriter s, float[] data, int width, int height, ColorComponents compEnum)
		{
			if (data == null)
			{
				throw new ArgumentNullException(nameof(data));
			}

			if (width <= 0)
			{
				throw new ArgumentOutOfRangeException(nameof(width));
			}

			if (height <= 0)
			{
				throw new ArgumentOutOfRangeException(nameof(height));
			}

			var scratch = new byte[width * 4];

			int i;
			var header = "#?RADIANCE\n# Written by stb_image_write.h\nFORMAT=32-bit_rle_rgbe\n";
			var bytes = Encoding.UTF8.GetBytes(header);

			s.Write(bytes);

			var str = string.Format("EXPOSURE=          1.0000000000000\n\n-Y {0} +X {1}\n", height, width);
			bytes = Encoding.UTF8.GetBytes(str);
			s.Write(bytes);

			var comp = (int)compEnum;
			for (i = 0; i < height; i++)
			{
				stbiw__write_hdr_scanline(s, width, comp, scratch, new FakePtr<float>(data, comp * i * width));
			}
		}

		/// <summary>Encodes image data to a Stream.</summary>
		/// <param name="output">The Stream to write to.</param>
		/// <param name="data">The image data.</param>
		/// <param name="width">The image width.</param>
		/// <param name="height">The image height.</param>
		/// <param name="comp">The color components.</param>
		public static void EncodeToStream(Stream output, float[] data, int width, int height, ColorComponents comp)
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
		public static byte[] EncodeToByteArray(float[] data, int width, int height, ColorComponents comp)
		{
			using (var ms = new MemoryStream())
			{
				EncodeToStream(ms, data, width, height, comp);
				return ms.ToArray();
			}
		}
	}
}
