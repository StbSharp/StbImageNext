using StbImageNext.Decoders;
using System;
using System.IO;

namespace StbImageNext
{
	/// <summary>
	/// Represents basic information about a decoded image.
	/// </summary>
	public struct ImageInfo
	{
		/// <summary>
		/// Gets or sets the width of the image.
		/// </summary>
		public int Width;
		/// <summary>
		/// Gets or sets the height of the image.
		/// </summary>
		public int Height;
		/// <summary>
		/// Gets or sets the color components of the image.
		/// </summary>
		public ColorComponents ColorComponents;


		/// <summary>
		/// Gets the image information from the specified stream.
		/// </summary>
		/// <param name="stream">Stream containing the image data.</param>
		/// <returns>The image information.</returns>
		public static ImageInfo FromStream(Stream stream)
		{
			if (PngDecoder.Test(stream))
			{
				return PngDecoder.Info(stream);
			}

			if (BmpDecoder.Test(stream))
			{
				return BmpDecoder.Info(stream);
			}

			if (GifDecoder.Test(stream))
			{
				return GifDecoder.Info(stream);
			}

			if (JpgDecoder.Test(stream))
			{
				return JpgDecoder.Info(stream);
			}

			if (TgaDecoder.Test(stream))
			{
				return TgaDecoder.Info(stream);
			}

			if (PsdDecoder.Test(stream))
			{
				return PsdDecoder.Info(stream);
			}

			if (HdrDecoder.Test(stream))
			{
				return HdrDecoder.Info(stream);
			}

			throw new NotSupportedException("Image format is not supported");
		}

		/// <summary>
		/// Tests whether the image in the specified stream has 16 bits per channel, from its header only.
		/// </summary>
		/// <remarks>
		/// Like stbi_is_16_bit of stb_image, except that only PNG images are checked: <see cref="ImageResult16"/>
		/// keeps 16 bits for PNG only, and reads the other images at 8 bits.
		/// </remarks>
		/// <param name="stream">Stream containing the image data.</param>
		/// <returns><c>true</c> if the image has 16 bits per channel, otherwise <c>false</c>.</returns>
		public static bool Is16Bit(Stream stream)
		{
			return PngDecoder.Test(stream) && PngDecoder.Is16Bit(stream);
		}
	}
}