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


		public static ImageInfo? FromStream(Stream stream)
		{
			var context = new StbImage.stbi__context(stream);

			if (PngImageDecoder.Test(context))
			{
				return PngImageDecoder.Info(context);
			}

			if (BmpImageDecoder.Test(context))
			{
				return BmpImageDecoder.Info(context);
			}

			if (GifImageDecoder.Test(context))
			{
				return GifImageDecoder.Info(context);
			}

			if (JpgImageDecoder.Test(context))
			{
				return JpgImageDecoder.Info(context);
			}

			if (TgaImageDecoder.Test(context))
			{
				return TgaImageDecoder.Info(context);
			}

			throw new NotSupportedException("Image format is not supported");
		}
	}
}