using System;
using System.Collections.Generic;
using System.IO;
using StbImageNext.Decoders;

namespace StbImageNext
{
	public class ImageResult
	{
		public int Width { get; set; }
		public int Height { get; set; }
		public ColorComponents SourceComp { get; set; }
		public ColorComponents Comp { get; set; }
		public byte[] Data { get; set; }

		public static ImageResult FromStream(Stream stream, ColorComponents requiredComponents = ColorComponents.Default)
		{
			var context = new StbImage.stbi__context(stream);

			if (PngImageDecoder.Test(context))
			{
				return PngImageDecoder.Decode(context, requiredComponents);
			}

			if (BmpImageDecoder.Test(stream))
			{
				return BmpImageDecoder.Load(stream, requiredComponents);
			}

			if (GifImageDecoder.Test(context))
			{
				return GifImageDecoder.Load(context, requiredComponents);
			}

			if (JpgImageDecoder.Test(context))
			{
				return JpgImageDecoder.Load(context, requiredComponents);
			}

			if (TgaImageDecoder.Test(context))
			{
				return TgaImageDecoder.Load(context, requiredComponents);
			}

			if (PsdImageDecoder.Test(context))
			{
				return PsdImageDecoder.Load(context, requiredComponents);
			}

			if (HdrImageDecoder.Test(context))
			{
				var floatResult = HdrImageDecoder.Load(context, requiredComponents);
				return floatResult.ToLdr();
			}

			throw new NotSupportedException("Image format is not supported");
		}

		public static ImageResult FromMemory(byte[] data, ColorComponents requiredComponents = ColorComponents.Default)
		{
			using (var stream = new MemoryStream(data))
			{
				return FromStream(stream, requiredComponents);
			}
		}

		public static IEnumerable<AnimatedFrameResult> AnimatedGifFramesFromStream(Stream stream,
			ColorComponents requiredComponents = ColorComponents.Default)
		{
			return new AnimatedGifEnumerable(stream, requiredComponents);
		}
	}
}