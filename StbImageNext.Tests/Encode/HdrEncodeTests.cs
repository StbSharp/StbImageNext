using StbImageNext.Encoders;
using StbImageNext.Tests.Utility;
using System.Reflection;
using Xunit;

namespace StbImageNext.Tests;

public class HdrEncodeTests
{
	private static readonly Assembly _assembly = typeof(HdrEncodeTests).Assembly;

	[Theory]
	[InlineData("sample_1280×853.hdr")]
	public void HdrEncode(string filename)
	{
		ImageResultFloat result1 = null;
		using (var stream = _assembly.OpenResourceStream(filename))
		{
			result1 = ImageResultFloat.FromStream(stream);
		}

		var encoded = HdrEncoder.EncodeToByteArray(result1.Data, result1.Width, result1.Height, result1.Comp);
		Assert.NotNull(encoded);

		var result2 = ImageResultFloat.FromMemory(encoded);

		AssertEx.ResultsEqual(result1, result2);
	}
}
