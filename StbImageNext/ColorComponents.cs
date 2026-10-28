namespace StbImageNext
{
	public enum ColorComponents
	{
		Grey = 1,
		GreyAlpha = 2,
		RedGreenBlue = 3,
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