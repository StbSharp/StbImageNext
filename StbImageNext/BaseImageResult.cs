namespace StbImageNext
{
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

		public void VerticalFlip()
		{
			var row = 0;
			var values_per_row = Width * (int)Comp;
			var temp = new DataBuffer<T>(values_per_row);
			for (row = 0; row < Height >> 1; row++)
			{
				var row0 = new DataBuffer<T>(Data, row * values_per_row);
				var row1 = new DataBuffer<T>(Data, (Height - row - 1) * values_per_row);

				temp.Copy(row0, values_per_row);
				row0.Copy(row1, values_per_row);
				row1.Copy(temp, values_per_row);
			}
		}
	}
}
