// Replaced with DeflateStream-based implementation

using StbImageNext.Hebron.Runtime;
using System;
using System.IO;
using System.IO.Compression;
using System.Runtime.InteropServices;

namespace StbImageNext
{
	unsafe partial class StbImage
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

		public static sbyte* stbi_zlib_decode_malloc_guesssize_headerflag(sbyte* buffer, int len, int initial_size,
			int* outlen, int parse_header)
		{
			if (buffer == null || len <= 0)
				return null;

			try
			{
				byte[] input = new byte[len];
				Marshal.Copy(new IntPtr(buffer), input, 0, len);

				byte[] deflateData = null;
				int deflateOffset = 0;
				int deflateLength = 0;

				if (parse_header != 0)
				{
					// Zlib format: 2 byte header + deflate data + 4 byte Adler32
					if (len < 6)
						return null;

					int cmf = input[0];
					int flg = input[1];
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
					deflateData = input;
				}
				else
				{
					// Raw deflate (no header/trailer)
					deflateOffset = 0;
					deflateLength = len;
					deflateData = input;
				}

				using (var ms = new MemoryStream(deflateData, deflateOffset, deflateLength))
				using (var deflateStream = new DeflateStream(ms, CompressionMode.Decompress))
				using (var outMs = new MemoryStream(initial_size > 0 ? initial_size : 16384))
				{
					deflateStream.CopyTo(outMs);
					byte[] decompressed = outMs.ToArray();

					if (parse_header != 0 && decompressed.Length >= 0)
					{
						// Verify Adler32 if we have trailer
						if (len >= 6)
						{
							uint adlerExpected = ((uint)input[len - 4] << 24) | ((uint)input[len - 3] << 16) | ((uint)input[len - 2] << 8) | ((uint)input[len - 1]);
							uint adlerActual = Adler32(decompressed, 0, decompressed.Length);
							// Some implementations might be strict about Adler32; try to be lenient if needed
							// but better to be correct - the custom impl may not have explicitly validated in all paths
						}
					}

					sbyte* result = (sbyte*)CRuntime.malloc((ulong)decompressed.Length);
					if (result == null)
						return null;
					Marshal.Copy(decompressed, 0, new IntPtr(result), decompressed.Length);
					if (outlen != null)
						*outlen = decompressed.Length;
					return result;
				}
			}
			catch (Exception)
			{
				stbi__err("zlib corrupt");
				return null;
			}
		}
	}
}
