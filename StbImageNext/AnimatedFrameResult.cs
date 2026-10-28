namespace StbImageNext
{
	/// <summary>
	/// Represents a single frame of an animated image, such as a frame of a GIF.
	/// </summary>
	public class AnimatedFrameResult : ImageResult
	{
		/// <summary>
		/// Gets or sets the delay of the frame in milliseconds.
		/// </summary>
		public int DelayInMs { get; set; }
	}
}