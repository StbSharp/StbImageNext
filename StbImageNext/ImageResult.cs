using StbImageNext.Decoders;
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
			if (PngDecoder.Test(stream))
			{
				return PngDecoder.Load(stream, requiredComponents);
			}

			if (BmpDecoder.Test(stream))
			{
				return BmpDecoder.Load(stream, requiredComponents);
			}

			if (GifDecoder.Test(stream))
			{
				return GifDecoder.Load(stream, requiredComponents);
			}

			if (JpgDecoder.Test(stream))
			{
				return JpgDecoder.Load(stream, requiredComponents);
			}

			if (TgaDecoder.Test(stream))
			{
				return TgaDecoder.Load(stream, requiredComponents);
			}

			if (PsdDecoder.Test(stream))
			{
				return PsdDecoder.Load(stream, requiredComponents);
			}

			if (HdrDecoder.Test(stream))
			{
				var floatResult = HdrDecoder.Load(stream, requiredComponents);
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

		/// <summary>
		/// Converts the 8-bit LDR (Low Dynamic Range) image to 32-bit floating-point HDR (High Dynamic Range) format.
		/// </summary>
		/// <param name="stbi__l2h_gamma">The gamma value for gamma correction, default is 2.2.</param>
		/// <param name="stbi__l2h_scale">The scale factor for the output values, default is 1.0.</param>
		/// <returns>An ImageResultFloat containing the HDR image data.</returns>
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