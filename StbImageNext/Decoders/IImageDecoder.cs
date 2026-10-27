using static StbImageNext.StbImage;

namespace StbImageNext.Decoders
{
	public interface IImageDecoder
	{
		bool Test(stbi__context stream);
		ImageInfo Info(stbi__context stream);
		ImageResult Decode(stbi__context stream, ColorComponents req_comps);
	}
}
