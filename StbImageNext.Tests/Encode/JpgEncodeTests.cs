using StbImageNext.Encoders;
using StbImageNext.Tests.Utility;
using System.Reflection;
using Xunit;

namespace StbImageNext.Tests;

public class JpgEncodeTests
{
	private static readonly Assembly _assembly = typeof(JpgEncodeTests).Assembly;

	[Theory]
	[InlineData("DockPanes.jpg", 95, 20)]
	[InlineData("DockPanes.jpg", 100, 4)]
	public void JpgEncode95(string filename, int quality, int limit)
	{
		ImageResult result1 = null;
		using (var stream = _assembly.OpenResourceStream(filename))
		{
			result1 = ImageResult.FromStream(stream);
		}

		var encoded = JpgEncoder.EncodeToByteArray(result1.Data, result1.Width, result1.Height, result1.Comp, quality);
		Assert.NotNull(encoded);

		var result2 = ImageResult.FromMemory(encoded);

		AssertEx.ResultsEqual(result1, result2, limit);
	}
}
