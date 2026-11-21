using StbImageNext.Encoders;
using StbImageNext.Tests.Utility;
using System.Reflection;
using Xunit;

namespace StbImageNext.Tests;

public class TgaEncodeTests
{
	private static readonly Assembly _assembly = typeof(TgaEncodeTests).Assembly;

	[Theory]
	[InlineData("head.tga", false)]
	[InlineData("head.tga", true)]
	public void TgaEncodeWithoutRle(string filename, bool rle)
	{
		ImageResult result1 = null;
		using (var stream = _assembly.OpenResourceStream(filename))
		{
			result1 = ImageResult.FromStream(stream);
		}

		var oldRle = TgaEncoder.WriteWithRle;
		try
		{
			TgaEncoder.WriteWithRle = rle;
			var encoded = TgaEncoder.EncodeToByteArray(result1.Data, result1.Width, result1.Height, result1.Comp);
			Assert.NotNull(encoded);

			var result2 = ImageResult.FromMemory(encoded);

			AssertEx.ResultsEqual(result1, result2);
		}
		finally
		{
			TgaEncoder.WriteWithRle = oldRle;
		}
	}
}
