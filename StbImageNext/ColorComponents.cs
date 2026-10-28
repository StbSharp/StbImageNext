namespace StbImageNext
{
	/// <summary>
	/// Specifies the color components that an image contains or requires.
	/// </summary>
	public enum ColorComponents
	{
		/// <summary>
		/// The image contains grayscale pixels.
		/// </summary>
		Grey = 1,
		/// <summary>
		/// The image contains grayscale and alpha pixels.
		/// </summary>
		GreyAlpha = 2,
		/// <summary>
		/// The image contains red, green and blue pixels.
		/// </summary>
		RedGreenBlue = 3,
		/// <summary>
		/// The image contains red, green, blue and alpha pixels.
		/// </summary>
		RedGreenBlueAlpha = 4
	}

	internal static class ColorComponentsExtensions
	{
		public static int ToInternalValue(this ColorComponents? comp)
		{
			return comp != null ? (int)comp.Value : 0;
		}
	}
}
