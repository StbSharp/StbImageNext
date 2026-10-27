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
			var context = new StbImage.stbi__context(stream);

			if (PngImageDecoder.Test(context))
			{
				return PngImageDecoder.Info(context);
			}

			if (BmpImageDecoder.Test(stream))
			{
				return BmpImageDecoder.Info(stream);
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

			if (PsdImageDecoder.Test(context))
			{
				return PsdImageDecoder.Info(context);
			}

			if (HdrImageDecoder.Test(context))
			{
				return HdrImageDecoder.Info(context);
			}

			throw new NotSupportedException("Image format is not supported");
		}
	}
}