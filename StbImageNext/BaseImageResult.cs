namespace StbImageNext
{
	/// <summary>
	/// Represents a base image result with common image information and data.
	/// </summary>
	/// <typeparam name="T">The type of the image data.</typeparam>
	public class BaseImageResult<T>
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
	}
}
