using Xunit;

namespace StbImageNext.Tests;

public class DataBufferTests
{
	[Fact]
	public void DataBufferNullIsNull()
	{
		Assert.True(FakePtr<byte>.Null.IsNull);
		Assert.True(FakePtr<short>.Null.IsNull);
		Assert.True(FakePtr<int>.Null.IsNull);
		Assert.True(FakePtr<float>.Null.IsNull);
	}

	[Fact]
	public void DataBufferNotNullIsNotNull()
	{
		var db = new FakePtr<byte>(10);
		Assert.False(db.IsNull);
	}
}
