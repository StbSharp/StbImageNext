using System;

namespace StbImageNext
{
	/// <summary>
	/// Base class for image result data containing pixel information and raw image data.
	/// </summary>
	/// <typeparam name="T">The type of pixel data (must be an unmanaged type).</typeparam>
	public class BaseImageResult<T> where T: unmanaged
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
		public T[] Data { get; set; }

		/// <summary>
		/// Vertically flips the image data by swapping rows.
		/// </summary>
		public void VerticalFlip()
		{
			var values_per_row = Width * (int)Comp;
			var temp = new T[values_per_row];
			for (var row = 0; row < Height >> 1; row++)
			{
				var row0 = new Span<T>(Data, row * values_per_row, values_per_row);
				var row1 = new Span<T>(Data, (Height - row - 1) * values_per_row, values_per_row);

				row0.CopyTo(temp);
				row1.CopyTo(row0);
				temp.CopyTo(row1);
			}
		}
	}
}
