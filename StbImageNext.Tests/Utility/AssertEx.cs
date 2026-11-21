using System;
using Xunit;

namespace StbImageNext.Tests.Utility
{
	/// <summary>
	/// Shared assertions for decoded or re-decoded image results.
	/// </summary>
	/// <remarks>
	/// Introduced by the test-file split so the per-encoder test classes do not each carry
	/// their own copy. Later phases extend this type; see TESTING-TASKS.md P0-6.
	/// </remarks>
	public static class AssertEx
	{
		/// <summary>
		/// Asserts that two byte-based results describe the same image within <paramref name="maxDelta"/>.
		/// </summary>
		public static void ResultsEqual(ImageResult expected, ImageResult actual, int maxDelta = 0)
		{
			Assert.Equal(expected.Width, actual.Width);
			Assert.Equal(expected.Height, actual.Height);
			Assert.Equal(expected.Comp, actual.Comp);
			Assert.NotNull(actual.Data);
			Assert.Equal(expected.Data.Length, actual.Data.Length);
			for (var i = 0; i < expected.Data.Length; ++i)
			{
				var delta = Math.Abs(expected.Data[i] - actual.Data[i]);
				Assert.InRange(delta, 0, maxDelta);
			}
		}

		/// <summary>
		/// Asserts that two float-based results describe the same image within <paramref name="maxDelta"/>.
		/// </summary>
		public static void ResultsEqual(ImageResultFloat expected, ImageResultFloat actual, float maxDelta = 1e-6f)
		{
			Assert.Equal(expected.Width, actual.Width);
			Assert.Equal(expected.Height, actual.Height);
			Assert.Equal(expected.Comp, actual.Comp);
			Assert.NotNull(actual.Data);
			Assert.Equal(expected.Data.Length, actual.Data.Length);
			for (var i = 0; i < expected.Data.Length; ++i)
			{
				var delta = Math.Abs(expected.Data[i] - actual.Data[i]);
				Assert.InRange(delta, 0, maxDelta);
			}
		}
	}
}
