using StbImageNext.Decoders;
using StbImageNext.Hebron.Runtime;
using System;
using System.Collections.Generic;
using System.IO;

namespace StbImageNext
{
	/// <summary>
	/// Represents a decoded image with its source and converted pixel formats.
	/// </summary>
	public class ImageResult : BaseImageResult<byte>
	{
		/// <summary>
		/// Decodes an image from the specified stream.
		/// </summary>
		/// <param name="stream">Stream containing the image data.</param>
		/// <param name="requiredComponents">Required pixel components of the image.</param>
		/// <returns>The decoded image result.</returns>
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

		/// <summary>
		/// Decodes an image from the specified byte array.
		/// </summary>
		/// <param name="data">Byte array containing the image data.</param>
		/// <param name="requiredComponents">Required pixel components of the image.</param>
		/// <returns>The decoded image result.</returns>
		public static ImageResult FromMemory(byte[] data, ColorComponents? requiredComponents = null)
		{
			using (var stream = new MemoryStream(data))
			{
				return FromStream(stream, requiredComponents);
			}
		}

		/// <summary>
		/// Enumerates the animated frames of a GIF image from the specified stream.
		/// </summary>
		/// <param name="stream">Stream containing the animated GIF image data.</param>
		/// <param name="requiredComponents">Required pixel components of the image.</param>
		/// <returns>An enumerator providing access to the animated frames.</returns>
		public static IEnumerable<AnimatedFrameResult> AnimatedGifFramesFromStream(Stream stream,
			ColorComponents? requiredComponents = null)
		{
			return new AnimatedGifEnumerable(stream, requiredComponents);
		}

		public ImageResultFloat ToHdr(float stbi__l2h_gamma = 2.2f, float stbi__l2h_scale = 1.0f)
		{
			var comp = (int)Comp;
			var output = new DataBuffer<float>(Width * Height * comp);

			var n = 0;
			if ((comp & 1) != 0)
			{
				n = comp;
			}
			else
			{
				n = comp - 1;
			}

			for (var i = 0; i < Width * Height; ++i)
			{
				for (var k = 0; k < n; ++k)
				{
					output[i * comp + k] = (float)(Math.Pow(Data[i * comp + k] / 255.0f, stbi__l2h_gamma) * stbi__l2h_scale);
				}
			}

			if (n < comp)
			{
				for (var i = 0; i < Width * Height; ++i)
				{
					output[i * comp + n] = Data[i * comp + n] / 255.0f;
				}
			}

			return new ImageResultFloat
			{
				Width = Width,
				Height = Height,
				SourceComp = SourceComp,
				Comp = Comp,
				Data = output.Data
			};
		}
	}
}