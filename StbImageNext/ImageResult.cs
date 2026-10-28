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

		public static ImageResult FromStream(Stream stream, ColorComponents? requiredComponents = null)
		{
			if (PngImageDecoder.Test(stream))
			{
				return PngImageDecoder.Decode(stream, requiredComponents);
			}

			if (BmpImageDecoder.Test(stream))
			{
				return BmpImageDecoder.Load(stream, requiredComponents);
			}

			if (GifImageDecoder.Test(stream))
			{
				return GifImageDecoder.Load(stream, requiredComponents);
			}

			if (JpgImageDecoder.Test(stream))
			{
				return JpgImageDecoder.Load(stream, requiredComponents);
			}

			if (TgaImageDecoder.Test(stream))
			{
				return TgaImageDecoder.Load(stream, requiredComponents);
			}

			if (PsdImageDecoder.Test(stream))
			{
				return PsdImageDecoder.Load(stream, requiredComponents);
			}

			if (HdrImageDecoder.Test(stream))
			{
				var floatResult = HdrImageDecoder.Load(stream, requiredComponents);
				return floatResult.ToLdr();
			}

			throw new NotSupportedException("Image format is not supported");
		}

		public static ImageResult FromMemory(byte[] data, ColorComponents? requiredComponents = null)
		{
			using (var stream = new MemoryStream(data))
			{
				return FromStream(stream, requiredComponents);
			}
		}

		public static IEnumerable<AnimatedFrameResult> AnimatedGifFramesFromStream(Stream stream,
			ColorComponents? requiredComponents = null)
		{
			return new AnimatedGifEnumerable(stream, requiredComponents);
		}
	}
}