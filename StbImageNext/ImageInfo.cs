using StbImageNext.Decoders;
using System;
using System.IO;

namespace StbImageNext
{
	public struct ImageInfo
	{
		public int Width;
		public int Height;
		public ColorComponents ColorComponents;


		public static ImageInfo FromStream(Stream stream)
		{
			if (PngImageDecoder.Test(stream))
			{
				return PngImageDecoder.Info(stream);
			}

			if (BmpImageDecoder.Test(stream))
			{
				return BmpImageDecoder.Info(stream);
			}

			if (GifImageDecoder.Test(stream))
			{
				return GifImageDecoder.Info(stream);
			}

			if (JpgImageDecoder.Test(stream))
			{
				return JpgImageDecoder.Info(stream);
			}

			if (TgaImageDecoder.Test(stream))
			{
				return TgaImageDecoder.Info(stream);
			}

			if (PsdImageDecoder.Test(stream))
			{
				return PsdImageDecoder.Info(stream);
			}

			if (HdrImageDecoder.Test(stream))
			{
				return HdrImageDecoder.Info(stream);
			}

			throw new NotSupportedException("Image format is not supported");
		}
	}
}