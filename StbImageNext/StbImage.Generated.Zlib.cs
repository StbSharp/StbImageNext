using System.IO;
using System.IO.Compression;

namespace StbImageNext
{
	partial class StbImage
	{
		private static uint Adler32(byte[] data, int offset, int length)
		{
			const uint mod = 65521;
			uint a = 1;
			uint b = 0;
			for (int i = 0; i < length; i++)
			{
				a = (a + data[offset + i]) % mod;
				b = (b + a) % mod;
			}
			return ((b << 16) | a);
		}

		internal static byte[] stbi_zlib_decode_malloc_guesssize_headerflag(OutputBuffer<byte> buffer, int len, int initial_size, int parse_header)
		{
			if (buffer == null || len <= 0)
				return null;

			int deflateOffset = 0;
			int deflateLength = len;

			if (parse_header != 0)
			{
				// Zlib format: 2 byte header + deflate data + 4 byte Adler32
				if (len < 6)
					return null;

				byte cmf = buffer[0];
				byte flg = buffer[1];
				int cm = cmf & 0x0F;
				if (((cmf * 256 + flg) % 31) != 0)
				{
					stbi__err("bad zlib header");
					return null;
				}
				if ((flg & 0x20) != 0)
				{
					stbi__err("no preset dict");
					return null;
				}
				if (cm != 8)
				{
					stbi__err("bad compression");
					return null;
				}

				deflateOffset = 2;
				deflateLength = len - 6; // exclude header and trailer
				if (deflateLength < 0)
					return null;
			}
			else
			{
				// Raw deflate (no header/trailer)
				deflateOffset = 0;
				deflateLength = len;
			}

			using (var ums = new MemoryStream(buffer.Data, (int)buffer.Offset + deflateOffset, deflateLength))
			using (var deflateStream = new DeflateStream(ums, CompressionMode.Decompress))
			using (var outMs = new MemoryStream(initial_size > 0 ? initial_size : 16384))
			{
				deflateStream.CopyTo(outMs);
				return outMs.ToArray();
			}
		}
	}
}
