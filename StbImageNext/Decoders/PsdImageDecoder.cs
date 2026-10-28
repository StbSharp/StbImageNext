using System;
using System.IO;
using static StbImageNext.StbImage;

namespace StbImageNext.Decoders
{
	public static class PsdImageDecoder
	{
		public static bool Test(Stream s)
		{
			var r = s.stbi__get32be() == 0x38425053;
			s.stbi__rewind();
			return r;
		}

		public static ImageInfo Info(Stream s)
		{
			var channelCount = 0;
			var depth = 0;

			if (s.stbi__get32be() != 0x38425053)
			{
				throw new Exception("Image isn't PSD");
			}

			if (s.stbi__get16be() != 1)
			{
				throw new Exception("s.stbi__get16be() != 1");
			}

			s.stbi__skip(6);
			channelCount = s.stbi__get16be();
			if (channelCount < 0 || channelCount > 16)
			{
				throw new Exception($"Wrong channel count: {channelCount}");
			}

			var result = new ImageInfo
			{
				Height = (int)s.stbi__get32be(),
				Width = (int)s.stbi__get32be()
			};

			depth = s.stbi__get16be();
			if (depth != 8)
			{
				throw new Exception($"Depth {depth} isn't supported");
			}

			if (s.stbi__get16be() != 3)
			{
				throw new Exception($"s.stbi__get16be() != 3");
			}

			result.ColorComponents = ColorComponents.RedGreenBlueAlpha;

			return result;
		}

		public static ImageResult Load(Stream s, ColorComponents? req_comp = null)
		{
			var pixelCount = 0;
			var channelCount = 0;
			var compression = 0;
			var channel = 0;
			var i = 0;
			var bitdepth = 0;
			var w = 0;
			var h = 0;

			if (s.stbi__get32be() != 0x38425053)
				throw new Exception("not PSD");
			if (s.stbi__get16be() != 1)
				throw new Exception("wrong version");
			s.stbi__skip(6);
			channelCount = s.stbi__get16be();
			if (channelCount < 0 || channelCount > 16)
				throw new Exception("wrong channel count");
			h = (int)s.stbi__get32be();
			w = (int)s.stbi__get32be();
			if (h > 1 << 24)
				throw new Exception("too large");
			if (w > 1 << 24)
				throw new Exception("too large");
			bitdepth = s.stbi__get16be();
			if (bitdepth != 8 && bitdepth != 16)
				throw new Exception("unsupported bit depth");
			if (s.stbi__get16be() != 3)
				throw new Exception("wrong color format");
			s.stbi__skip((int)s.stbi__get32be());
			s.stbi__skip((int)s.stbi__get32be());
			s.stbi__skip((int)s.stbi__get32be());
			compression = s.stbi__get16be();
			if (compression > 1)
				throw new Exception("bad compression");
			if (stbi__mad3sizes_valid(4, w, h, 0) == 0)
				throw new Exception("too large");

			var _out_ = new DataBuffer<byte>(4 * w * h);

			pixelCount = w * h;
			if (compression != 0)
			{
				s.stbi__skip(h * channelCount * 2);
				for (channel = 0; channel < 4; channel++)
				{
					var p = _out_ + channel;
					if (channel >= channelCount)
					{
						for (i = 0; i < pixelCount; i++, p += 4)
							p[0] = (byte)(channel == 3 ? 255 : 0);
					}
					else
					{
						if (stbi__psd_decode_rle(s, p, pixelCount) == 0)
						{
							throw new Exception("corrupt");
						}
					}
				}
			}
			else
			{
				for (channel = 0; channel < 4; channel++)
				{
					if (channel >= channelCount)
					{
						var p = _out_ + channel;
						var val = (byte)(channel == 3 ? 255 : 0);
						for (i = 0; i < pixelCount; i++, p += 4)
						{
							p[0] = val;
						}
					}
					else
					{
						var p = _out_ + channel;
						if (bitdepth == 16)
						{
							for (i = 0; i < pixelCount; i++, p += 4)
							{
								p[0] = (byte)(s.stbi__get16be() >> 8);
							}
						}
						else
						{
							for (i = 0; i < pixelCount; i++, p += 4)
							{
								p[0] = s.stbi__get8();
							}
						}
					}
				}
			}

			if (channelCount >= 4)
			{
				for (i = 0; i < w * h; ++i)
				{
					var pixel = _out_ + 4 * i;
					if (pixel[3] != 0 && pixel[3] != 255)
					{
						var a = pixel[3] / 255.0f;
						var ra = 1.0f / a;
						var inv_a = 255.0f * (1 - ra);
						pixel[0] = (byte)(pixel[0] * ra + inv_a);
						pixel[1] = (byte)(pixel[1] * ra + inv_a);
						pixel[2] = (byte)(pixel[2] * ra + inv_a);
					}
				}
			}

			if (req_comp != null && req_comp != ColorComponents.RedGreenBlueAlpha)
			{
				_out_ = Utility.stbi__convert_format(_out_, 4, (int)req_comp.Value, (uint)w, (uint)h);
			}

			var result = new ImageResult
			{
				Width = w,
				Height = h,
				SourceComp = ColorComponents.RedGreenBlueAlpha,
				Data = _out_.Data
			};

			result.Comp = req_comp ?? result.SourceComp;

			return result;
		}

		private static int stbi__psd_decode_rle(Stream s, DataBuffer<byte> p, int pixelCount)
		{
			var count = 0;
			var nleft = 0;
			var len = 0;
			count = 0;
			while ((nleft = pixelCount - count) > 0)
			{
				len = s.stbi__get8();
				if (len == 128)
				{
				}
				else if (len < 128)
				{
					len++;
					if (len > nleft)
						return 0;
					count += len;
					while (len != 0)
					{
						p[0] = s.stbi__get8();
						p += 4;
						len--;
					}
				}
				else if (len > 128)
				{
					byte val = 0;
					len = 257 - len;
					if (len > nleft)
						return 0;
					val = s.stbi__get8();
					count += len;
					while (len != 0)
					{
						p[0] = val;
						p += 4;
						len--;
					}
				}
			}

			return 1;
		}
	}
}
