using StbImageNext.Hebron.Runtime;
using System;
using static StbImageNext.StbImage;

namespace StbImageNext.Decoders
{
	public static class HdrImageDecoder
	{
		public static bool Test(stbi__context s)
		{
			var r = stbi__hdr_test_core(s, "#?RADIANCE\n");
			stbi__rewind(s);
			if (r == 0)
			{
				r = stbi__hdr_test_core(s, "#?RGBE\n");
				stbi__rewind(s);
			}

			return r == 1;
		}

		public static unsafe ImageInfo Info(stbi__context s)
		{
			try
			{
				var buffer = stackalloc sbyte[1024];
				sbyte* token;
				var valid = 0;
				if (!Test(s))
				{
					throw new Exception("Image isn't HDR");
				}

				for (; ; )
				{
					token = stbi__hdr_gettoken(s, buffer);
					if (token[0] == 0)
						break;
					if (CRuntime.strcmp(token, "FORMAT=32-bit_rle_rgbe") == 0)
						valid = 1;
				}

				if (valid == 0)
				{
					throw new Exception("HDR image isn't valid");
				}

				token = stbi__hdr_gettoken(s, buffer);
				if (CRuntime.strncmp(token, "-Y ", 3) != 0)
				{
					throw new Exception("'-Y' token wasn't found");
				}

				var result = new ImageInfo();
				token += 3;

				result.Height = (int)CRuntime.strtol(token, &token, 10);
				while (*token == 32)
					++token;
				if (CRuntime.strncmp(token, "+X ", 3) != 0)
				{
					throw new Exception("'+X' token wasn't found");
				}

				token += 3;

				result.Width = (int)CRuntime.strtol(token, null, 10);
				result.ColorComponents = ColorComponents.RedGreenBlue;

				return result;
			}
			finally
			{
				stbi__rewind(s);
			}
		}

		public static unsafe ImageResultFloat Load(stbi__context s, ColorComponents req_comp2)
		{
			var rgbe = stackalloc byte[4];
			var buffer = stackalloc sbyte[1024];
			sbyte* token;
			var valid = 0;
			var width = 0;
			var height = 0;
			byte* scanline;
			var len = 0;
			byte count = 0;
			byte value = 0;
			var i = 0;
			var j = 0;
			var k = 0;
			var c1 = 0;
			var c2 = 0;
			var z = 0;
			sbyte* headerToken;
			headerToken = stbi__hdr_gettoken(s, buffer);
			if (CRuntime.strcmp(headerToken, "#?RADIANCE") != 0 && CRuntime.strcmp(headerToken, "#?RGBE") != 0)
				throw new Exception("not HDR");
			for (; ; )
			{
				token = stbi__hdr_gettoken(s, buffer);
				if (token[0] == 0)
					break;
				if (CRuntime.strcmp(token, "FORMAT=32-bit_rle_rgbe") == 0)
					valid = 1;
			}

			if (valid == 0)
				throw new Exception("unsupported format");
			token = stbi__hdr_gettoken(s, buffer);
			if (CRuntime.strncmp(token, "-Y ", 3) != 0)
				throw new Exception("unsupported data layout");
			token += 3;
			height = (int)CRuntime.strtol(token, &token, 10);
			while (*token == 32)
				++token;
			if (CRuntime.strncmp(token, "+X ", 3) != 0)
				throw new Exception("unsupported data layout");
			token += 3;
			width = (int)CRuntime.strtol(token, null, 10);
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

			result.Comp = req_comp2 == ColorComponents.Default ? result.SourceComp : req_comp2;

			var req_comp = (int)req_comp2;
			if (req_comp == 0)
				req_comp = 3;
			if (stbi__mad4sizes_valid(width, height, req_comp, sizeof(float), 0) == 0)
				throw new Exception("too large");

			var hdr_data = new OutputBuffer<float>(width * height * req_comp);
			if (width < 8 || width >= 32768)
			{
				for (; j < height; ++j)
				{
					for (; i < width; ++i)
					{
						//var rgbe = stackalloc byte[4];
						stbi__getn(s, rgbe, 4);
						stbi__hdr_convert(hdr_data + j * width * req_comp + i * req_comp, rgbe, req_comp);
					}
				}
			}
			else
			{
				scanline = null;
				for (j = 0; j < height; ++j)
				{
					c1 = stbi__get8(s);
					c2 = stbi__get8(s);
					len = stbi__get8(s);
					if (c1 != 2 || c2 != 2 || (len & 0x80) != 0)
					{
						//var rgbe = stackalloc byte[4];
						rgbe[0] = (byte)c1;
						rgbe[1] = (byte)c2;
						rgbe[2] = (byte)len;
						rgbe[3] = stbi__get8(s);
						stbi__hdr_convert(hdr_data, rgbe, req_comp);
						CRuntime.free(scanline);

						// goto main_decode_loop

						// Do first row
						j = 0;
						for (i = 1; i < width; ++i)
						{
							//var rgbe = stackalloc byte[4];
							stbi__getn(s, rgbe, 4);
							stbi__hdr_convert(hdr_data + j * width * req_comp + i * req_comp, rgbe, req_comp);
						}

						// Do the rest of the rows.
						for (j = 1; j < height; ++j)
						{
							for (i = 0; i < width; ++i)
							{
								//var rgbe = stackalloc byte[4];
								stbi__getn(s, rgbe, 4);
								stbi__hdr_convert(hdr_data + j * width * req_comp + i * req_comp, rgbe, req_comp);
							}
						}

						goto finish;
					}

					len <<= 8;
					len |= stbi__get8(s);
					if (len != width)
					{
						CRuntime.free(scanline);
						throw new Exception("invalid decoded scanline length");
					}

					if (scanline == null)
					{
						scanline = (byte*)stbi__malloc_mad2(width, 4, 0);
						if (scanline == null)
						{
							throw new Exception("outofmem");
						}
					}

					for (k = 0; k < 4; ++k)
					{
						var nleft = 0;
						i = 0;
						while ((nleft = width - i) > 0)
						{
							count = stbi__get8(s);
							if (count > 128)
							{
								value = stbi__get8(s);
								count -= 128;
								if (count == 0 || count > nleft)
								{
									CRuntime.free(scanline);
									throw new Exception("corrupt");
								}

								for (z = 0; z < count; ++z)
									scanline[i++ * 4 + k] = value;
							}
							else
							{
								if (count == 0 || count > nleft)
								{
									CRuntime.free(scanline);
									throw new Exception("corrupt");
								}

								for (z = 0; z < count; ++z)
									scanline[i++ * 4 + k] = stbi__get8(s);
							}
						}
					}

					for (i = 0; i < width; ++i)
						stbi__hdr_convert(hdr_data + (j * width + i) * req_comp, scanline + i * 4, req_comp);
				}

				if (scanline != null)
					CRuntime.free(scanline);
			}

		finish:

			result.Data = hdr_data.Data;
			return result;
		}


		private static int stbi__hdr_test_core(stbi__context s, string signature)
		{
			var i = 0;
			for (i = 0; i < signature.Length; ++i)
				if (stbi__get8(s) != signature[i])
					return 0;
			stbi__rewind(s);
			return 1;
		}

		private static unsafe sbyte* stbi__hdr_gettoken(stbi__context z, sbyte* buffer)
		{
			var len = 0;
			sbyte c = 0;
			c = (sbyte)stbi__get8(z);
			while (stbi__at_eof(z) == 0 && c != 10)
			{
				buffer[len++] = c;
				if (len == 1024 - 1)
				{
					while (stbi__at_eof(z) == 0 && stbi__get8(z) != 10)
					{
					}

					break;
				}

				c = (sbyte)stbi__get8(z);
			}

			buffer[len] = 0;
			return buffer;
		}

		private static unsafe void stbi__hdr_convert(OutputBuffer<float> output, byte* input, int req_comp)
		{
			if (input[3] != 0)
			{
				float f1 = 0;
				f1 = (float)CRuntime.ldexp(1.0f, input[3] - (128 + 8));
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
