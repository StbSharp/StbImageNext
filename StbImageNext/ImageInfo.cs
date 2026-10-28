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