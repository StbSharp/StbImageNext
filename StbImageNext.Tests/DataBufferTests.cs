using Xunit;

namespace StbImageNext.Tests;

public class DataBufferTests
{
	[Fact]
	public void DataBufferNullIsNull()
	{
		Assert.True(DataBuffer<byte>.Null.IsNull);
		Assert.True(DataBuffer<short>.Null.IsNull);
		Assert.True(DataBuffer<int>.Null.IsNull);
		Assert.True(DataBuffer<float>.Null.IsNull);
	}

	[Fact]
	public void DataBufferNotNullIsNotNull()
	{
		var db = new DataBuffer<byte>(10);
		Assert.False(db.IsNull);
	}
}
