using System;
using System.Collections.Generic;
using System.IO;
using StbImageNext.Decoders;

namespace StbImageNext
{
	/// <summary>
	/// Represents a decoded image with its source and converted pixel formats.
	/// </summary>
	public class ImageResult
	{
		/// <summary>
		/// Gets or sets the width of the image.
		/// </summary>
		public int Width { get; set; }
		/// <summary>
		/// Gets or sets the height of the image.
		/// </summary>
		public int Height { get; set; }
		/// <summary>
		/// Gets or sets the source pixel format of the image.
		/// </summary>
		public ColorComponents SourceComp { get; set; }
		/// <summary>
		/// Gets or sets the pixel format of the image data.
		/// </summary>
		public ColorComponents Comp { get; set; }
		/// <summary>
		/// Gets or sets the image data.
		/// </summary>
		public byte[] Data { get; set; }

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
	}
}