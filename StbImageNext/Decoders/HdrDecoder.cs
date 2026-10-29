using System;
using System.IO;
using System.Text;

namespace StbImageNext.Decoders
{
	/// <summary>
	/// Provides methods to test, get information about and load HDR images.
	/// </summary>
	public static class HdrDecoder
	{
		/// <summary>
		/// Tests whether the specified stream contains a HDR image.
		/// </summary>
		/// <param name="s">Stream containing the image data.</param>
		/// <returns><c>true</c> if the stream contains a HDR image, otherwise <c>false</c>.</returns>
		public static bool Test(Stream s)
		{
			var r = stbi__hdr_test_core(s, "#?RADIANCE\n");
			s.stbi__rewind();
			if (r == 0)
			{
				r = stbi__hdr_test_core(s, "#?RGBE\n");
				s.stbi__rewind();
			}

			return r == 1;
		}

		/// <summary>
		/// Gets the image information from the specified stream.
		/// </summary>
		/// <param name="s">Stream containing the image data.</param>
		/// <returns>The image information.</returns>
		public static ImageInfo Info(Stream s)
		{
			try
			{
				string token;
				var valid = 0;
				if (!Test(s))
				{
					throw new Exception("Image isn't HDR");
				}

				var sb = new StringBuilder(1024);
				for (; ; )
				{
					token = stbi__hdr_gettoken_string(s, sb);
					if (string.IsNullOrEmpty(token))
						break;
					if (token == "FORMAT=32-bit_rle_rgbe")
						valid = 1;
				}

				if (valid == 0)
				{
					throw new Exception("HDR image isn't valid");
				}

				token = stbi__hdr_gettoken_string(s, sb);
				if (token == null || !token.StartsWith("-Y ", StringComparison.Ordinal))
				{
					throw new Exception("'-Y' token wasn't found");
				}

				var result = new ImageInfo();
				string rest = token.Substring(3).Trim();
				// Parse height
				int spaceIndex = rest.IndexOf(' ');
				string heightStr = spaceIndex >= 0 ? rest.Substring(0, spaceIndex) : rest;
				result.Height = int.Parse(heightStr);

				// Find +X part
				string xPart = spaceIndex >= 0 ? rest.Substring(spaceIndex).Trim() : "";
				if (xPart.StartsWith("+X ", StringComparison.Ordinal))
				{
					result.Width = int.Parse(xPart.Substring(3).Trim());
				}
				else
				{
					token = stbi__hdr_gettoken_string(s, sb);
					if (token == null || !token.StartsWith("+X ", StringComparison.Ordinal))
						throw new Exception("'+X' token wasn't found");
					result.Width = int.Parse(token.Substring(3).Trim());
				}
				result.ColorComponents = ColorComponents.RedGreenBlue;

				return result;
			}
			finally
			{
				s.stbi__rewind();
			}
		}

		/// <summary>
		/// Loads the HDR image from the specified stream.
		/// </summary>
		/// <param name="s">Stream containing the image data.</param>
		/// <param name="req_comp2">Required pixel components of the image.</param>
		/// <returns>The decoded HDR image result.</returns>
		public static ImageResultFloat Load(Stream s, ColorComponents? req_comp2 = null)
		{
			var rgbe = new DataBuffer<byte>(4);
			string token;
			var valid = 0;
			var width = 0;
			var height = 0;
			DataBuffer<byte> scanline = DataBuffer<byte>.Null;
			var len = 0;
			byte count = 0;
			byte value = 0;
			var i = 0;
			var j = 0;
			var k = 0;
			var c1 = 0;
			var c2 = 0;
			var z = 0;

			var sb = new StringBuilder(1024);
			string headerToken;
			headerToken = stbi__hdr_gettoken_string(s, sb);
			if (headerToken != "#?RADIANCE" && headerToken != "#?RGBE")
				throw new Exception("not HDR");

			for (; ; )
			{
				token = stbi__hdr_gettoken_string(s, sb);
				if (string.IsNullOrEmpty(token))
					break;
				if (token == "FORMAT=32-bit_rle_rgbe")
					valid = 1;
			}

			if (valid == 0)
				throw new Exception("unsupported format");
			token = stbi__hdr_gettoken_string(s, sb);
			if (token == null || !token.StartsWith("-Y ", StringComparison.Ordinal))
				throw new Exception("unsupported data layout");

			string rest = token.Substring(3).Trim();
			// Parse height
			int spaceIndex = rest.IndexOf(' ');
			string heightStr = spaceIndex >= 0 ? rest.Substring(0, spaceIndex) : rest;
			height = int.Parse(heightStr);

			// Find +X part
			string xPart = spaceIndex >= 0 ? rest.Substring(spaceIndex).Trim() : "";
			if (xPart.StartsWith("+X ", StringComparison.Ordinal))
			{
				width = int.Parse(xPart.Substring(3).Trim());
			}
			else
			{
				token = stbi__hdr_gettoken_string(s, sb);
				if (token == null || !token.StartsWith("+X ", StringComparison.Ordinal))
					throw new Exception("unsupported data layout");
				width = int.Parse(token.Substring(3).Trim());
			}

			if (height > 1 << 24)
				throw new Exception("too large");
			if (width > 1 << 24)
				throw new Exception("too large");

			var result = new ImageResultFloat
			{
				Width = width,
				Height = height,
				SourceComp = ColorComponents.RedGreenBlue
			};

			result.Comp = req_comp2 ?? result.SourceComp;

			var req_comp = req_comp2.ToInternalValue();
			if (req_comp == 0)
				req_comp = 3;

			var hdr_data = new DataBuffer<float>(width * height * req_comp);
			if (width < 8 || width >= 32768)
			{
				for (; j < height; ++j)
				{
					for (; i < width; ++i)
					{
						s.stbi__getn(rgbe, 4);
						stbi__hdr_convert(hdr_data + j * width * req_comp + i * req_comp, rgbe, req_comp);
					}
				}
			}
			else
			{
				for (j = 0; j < height; ++j)
				{
					c1 = s.stbi__get8();
					c2 = s.stbi__get8();
					len = s.stbi__get8();
					if (c1 != 2 || c2 != 2 || (len & 0x80) != 0)
					{
						//var rgbe = stackalloc byte[4];
						rgbe[0] = (byte)c1;
						rgbe[1] = (byte)c2;
						rgbe[2] = (byte)len;
						rgbe[3] = s.stbi__get8();
						stbi__hdr_convert(hdr_data, rgbe, req_comp);

						// goto main_decode_loop

						// Do first row
						j = 0;
						for (i = 1; i < width; ++i)
						{
							//var rgbe = stackalloc byte[4];
							s.stbi__getn(rgbe, 4);
							stbi__hdr_convert(hdr_data + j * width * req_comp + i * req_comp, rgbe, req_comp);
						}

						// Do the rest of the rows.
						for (j = 1; j < height; ++j)
						{
							for (i = 0; i < width; ++i)
							{
								//var rgbe = stackalloc byte[4];
								s.stbi__getn(rgbe, 4);
								stbi__hdr_convert(hdr_data + j * width * req_comp + i * req_comp, rgbe, req_comp);
							}
						}

						goto finish;
					}

					len <<= 8;
					len |= s.stbi__get8();
					if (len != width)
					{
						throw new Exception("invalid decoded scanline length");
					}

					if (scanline.IsNull)
					{
						scanline = new DataBuffer<byte>(width * 4);
					}

					for (k = 0; k < 4; ++k)
					{
						var nleft = 0;
						i = 0;
						while ((nleft = width - i) > 0)
						{
							count = s.stbi__get8();
							if (count > 128)
							{
								value = s.stbi__get8();
								count -= 128;
								if (count == 0 || count > nleft)
								{
									throw new Exception("corrupt");
								}

								for (z = 0; z < count; ++z)
									scanline[i++ * 4 + k] = value;
							}
							else
							{
								if (count == 0 || count > nleft)
								{
									throw new Exception("corrupt");
								}

								for (z = 0; z < count; ++z)
									scanline[i++ * 4 + k] = s.stbi__get8();
							}
						}
					}

					for (i = 0; i < width; ++i)
						stbi__hdr_convert(hdr_data + (j * width + i) * req_comp, scanline + i * 4, req_comp);
				}
			}

		finish:
			result.Data = hdr_data.Data;
			return result;
		}

		private static int stbi__hdr_test_core(Stream s, string signature)
		{
			var i = 0;
			for (i = 0; i < signature.Length; ++i)
				if (s.stbi__get8() != signature[i])
					return 0;
			s.stbi__rewind();
			return 1;
		}

		private static string stbi__hdr_gettoken_string(Stream z, StringBuilder sb)
		{
			sb.Clear();
			var c = z.stbi__get8();
			while (!z.stbi__at_eof() && c != 10)
			{
				sb.Append((char)c);
				if (sb.Length >= 1024 - 1)
				{
					while (!z.stbi__at_eof() && z.stbi__get8() != 10)
					{
					}
					break;
				}
				c = z.stbi__get8();
			}

			string result = sb.ToString();
			return string.IsNullOrEmpty(result) ? null : result;
		}

		private static void stbi__hdr_convert(DataBuffer<float> output, DataBuffer<byte> input, int req_comp)
		{
			if (input[3] != 0)
			{
				float f1 = 0;
				f1 = (float)Utility.ldexp(1.0f, input[3] - (128 + 8));
				if (req_comp <= 2)
				{
					output[0] = (input[0] + input[1] + input[2]) * f1 / 3;
				}
				else
				{
					output[0] = input[0] * f1;
					output[1] = input[1] * f1;
					output[2] = input[2] * f1;
				}

				if (req_comp == 2)
					output[1] = 1;
				if (req_comp == 4)
					output[3] = 1;
			}
			else
			{
				switch (req_comp)
				{
					case 4:
					case 3:
						if (req_comp == 4)
							output[3] = 1;
						output[0] = output[1] = output[2] = 0;
						break;
					case 2:
					case 1:
						if (req_comp == 2)
							output[1] = 1;
						output[0] = 0;
						break;
				}
			}
		}
	}
}
