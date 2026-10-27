using StbImageNext.Hebron.Runtime;

namespace StbImageNext
{
	public static partial class StbImage
	{
		public static string stbi__g_failure_reason;
		public static readonly char[] stbi__parse_png_file_invalid_chunk = new char[25];

		public static int NativeAllocations
		{
			get
			{
				return MemoryStats.Allocations;
			}
		}

		internal static int stbi__err(string str)
		{
			stbi__g_failure_reason = str;
			return 0;
		}
	}
}