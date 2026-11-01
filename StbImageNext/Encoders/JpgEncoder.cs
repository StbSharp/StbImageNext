using System.IO;
using System.Text;

namespace StbImageNext.Encoders
{
	/// <summary>Provides methods to encode images to JPEG format.</summary>
	public static class JpgEncoder
	{
		private static float[] stbi_write_jpg_core_aasf =
		{
			1.0f * 2.828427125f, 1.387039845f * 2.828427125f, 1.306562965f * 2.828427125f, 1.175875602f * 2.828427125f,
			1.0f * 2.828427125f, 0.785694958f * 2.828427125f, 0.541196100f * 2.828427125f, 0.275899379f * 2.828427125f
		};

		private static ushort[] stbi_write_jpg_core_fillBits = { 0x7F, 7 };

		private static byte[] stbi_write_jpg_core_head0 =
			{0xFF, 0xD8, 0xFF, 0xE0, 0, 0x10, 74, 70, 73, 70, 0, 1, 1, 0, 0, 1, 0, 1, 0, 0, 0xFF, 0xDB, 0, 0x84, 0};

		private static byte[] stbi_write_jpg_core_head2 = { 0xFF, 0xDA, 0, 0xC, 3, 1, 0, 2, 0x11, 3, 0x11, 0, 0x3F, 0 };

		private static byte[] stbi_write_jpg_core_std_ac_chrominance_nrcodes =
			{0, 0, 2, 1, 2, 4, 4, 3, 4, 7, 5, 4, 4, 0, 1, 2, 0x77};

		private static byte[] stbi_write_jpg_core_std_ac_chrominance_values =
		{
			0x00, 0x01, 0x02, 0x03, 0x11, 0x04, 0x05, 0x21, 0x31, 0x06, 0x12, 0x41, 0x51, 0x07, 0x61, 0x71, 0x13, 0x22,
			0x32, 0x81, 0x08, 0x14, 0x42, 0x91, 0xa1, 0xb1, 0xc1, 0x09, 0x23, 0x33, 0x52, 0xf0, 0x15, 0x62, 0x72, 0xd1,
			0x0a, 0x16, 0x24, 0x34, 0xe1, 0x25, 0xf1, 0x17, 0x18, 0x19, 0x1a, 0x26, 0x27, 0x28, 0x29, 0x2a, 0x35, 0x36,
			0x37, 0x38, 0x39, 0x3a, 0x43, 0x44, 0x45, 0x46, 0x47, 0x48, 0x49, 0x4a, 0x53, 0x54, 0x55, 0x56, 0x57, 0x58,
			0x59, 0x5a, 0x63, 0x64, 0x65, 0x66, 0x67, 0x68, 0x69, 0x6a, 0x73, 0x74, 0x75, 0x76, 0x77, 0x78, 0x79, 0x7a,
			0x82, 0x83, 0x84, 0x85, 0x86, 0x87, 0x88, 0x89, 0x8a, 0x92, 0x93, 0x94, 0x95, 0x96, 0x97, 0x98, 0x99, 0x9a,
			0xa2, 0xa3, 0xa4, 0xa5, 0xa6, 0xa7, 0xa8, 0xa9, 0xaa, 0xb2, 0xb3, 0xb4, 0xb5, 0xb6, 0xb7, 0xb8, 0xb9, 0xba,
			0xc2, 0xc3, 0xc4, 0xc5, 0xc6, 0xc7, 0xc8, 0xc9, 0xca, 0xd2, 0xd3, 0xd4, 0xd5, 0xd6, 0xd7, 0xd8, 0xd9, 0xda,
			0xe2, 0xe3, 0xe4, 0xe5, 0xe6, 0xe7, 0xe8, 0xe9, 0xea, 0xf2, 0xf3, 0xf4, 0xf5, 0xf6, 0xf7, 0xf8, 0xf9, 0xfa
		};

		private static byte[] stbi_write_jpg_core_std_ac_luminance_nrcodes =
			{0, 0, 2, 1, 3, 3, 2, 4, 3, 5, 5, 4, 4, 0, 0, 1, 0x7d};

		private static byte[] stbi_write_jpg_core_std_ac_luminance_values =
		{
			0x01, 0x02, 0x03, 0x00, 0x04, 0x11, 0x05, 0x12, 0x21, 0x31, 0x41, 0x06, 0x13, 0x51, 0x61, 0x07, 0x22, 0x71,
			0x14, 0x32, 0x81, 0x91, 0xa1, 0x08, 0x23, 0x42, 0xb1, 0xc1, 0x15, 0x52, 0xd1, 0xf0, 0x24, 0x33, 0x62, 0x72,
			0x82, 0x09, 0x0a, 0x16, 0x17, 0x18, 0x19, 0x1a, 0x25, 0x26, 0x27, 0x28, 0x29, 0x2a, 0x34, 0x35, 0x36, 0x37,
			0x38, 0x39, 0x3a, 0x43, 0x44, 0x45, 0x46, 0x47, 0x48, 0x49, 0x4a, 0x53, 0x54, 0x55, 0x56, 0x57, 0x58, 0x59,
			0x5a, 0x63, 0x64, 0x65, 0x66, 0x67, 0x68, 0x69, 0x6a, 0x73, 0x74, 0x75, 0x76, 0x77, 0x78, 0x79, 0x7a, 0x83,
			0x84, 0x85, 0x86, 0x87, 0x88, 0x89, 0x8a, 0x92, 0x93, 0x94, 0x95, 0x96, 0x97, 0x98, 0x99, 0x9a, 0xa2, 0xa3,
			0xa4, 0xa5, 0xa6, 0xa7, 0xa8, 0xa9, 0xaa, 0xb2, 0xb3, 0xb4, 0xb5, 0xb6, 0xb7, 0xb8, 0xb9, 0xba, 0xc2, 0xc3,
			0xc4, 0xc5, 0xc6, 0xc7, 0xc8, 0xc9, 0xca, 0xd2, 0xd3, 0xd4, 0xd5, 0xd6, 0xd7, 0xd8, 0xd9, 0xda, 0xe1, 0xe2,
			0xe3, 0xe4, 0xe5, 0xe6, 0xe7, 0xe8, 0xe9, 0xea, 0xf1, 0xf2, 0xf3, 0xf4, 0xf5, 0xf6, 0xf7, 0xf8, 0xf9, 0xfa
		};

		private static byte[] stbi_write_jpg_core_std_dc_chrominance_nrcodes =
			{0, 0, 3, 1, 1, 1, 1, 1, 1, 1, 1, 1, 0, 0, 0, 0, 0};

		private static byte[] stbi_write_jpg_core_std_dc_chrominance_values = { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11 };

		private static byte[] stbi_write_jpg_core_std_dc_luminance_nrcodes =
			{0, 0, 1, 5, 1, 1, 1, 1, 1, 1, 0, 0, 0, 0, 0, 0, 0};

		private static byte[] stbi_write_jpg_core_std_dc_luminance_values = { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11 };

		private static ushort[,] stbi_write_jpg_core_UVAC_HT =
		{
			{0, 2}, {1, 2}, {4, 3}, {10, 4}, {24, 5}, {25, 5}, {56, 6}, {120, 7}, {500, 9}, {1014, 10}, {4084, 12},
			{0, 0}, {0, 0}, {0, 0}, {0, 0}, {0, 0}, {0, 0}, {11, 4}, {57, 6}, {246, 8}, {501, 9}, {2038, 11},
			{4085, 12}, {65416, 16}, {65417, 16}, {65418, 16}, {65419, 16}, {0, 0}, {0, 0}, {0, 0}, {0, 0}, {0, 0},
			{0, 0}, {26, 5}, {247, 8}, {1015, 10}, {4086, 12}, {32706, 15}, {65420, 16}, {65421, 16}, {65422, 16},
			{65423, 16}, {65424, 16}, {0, 0}, {0, 0}, {0, 0}, {0, 0}, {0, 0}, {0, 0}, {27, 5}, {248, 8}, {1016, 10},
			{4087, 12}, {65425, 16}, {65426, 16}, {65427, 16}, {65428, 16}, {65429, 16}, {65430, 16}, {0, 0}, {0, 0},
			{0, 0}, {0, 0}, {0, 0}, {0, 0}, {58, 6}, {502, 9}, {65431, 16}, {65432, 16}, {65433, 16}, {65434, 16},
			{65435, 16}, {65436, 16}, {65437, 16}, {65438, 16}, {0, 0}, {0, 0}, {0, 0}, {0, 0}, {0, 0}, {0, 0}, {59, 6},
			{1017, 10}, {65439, 16}, {65440, 16}, {65441, 16}, {65442, 16}, {65443, 16}, {65444, 16}, {65445, 16},
			{65446, 16}, {0, 0}, {0, 0}, {0, 0}, {0, 0}, {0, 0}, {0, 0}, {121, 7}, {2039, 11}, {65447, 16}, {65448, 16},
			{65449, 16}, {65450, 16}, {65451, 16}, {65452, 16}, {65453, 16}, {65454, 16}, {0, 0}, {0, 0}, {0, 0},
			{0, 0}, {0, 0}, {0, 0}, {122, 7}, {2040, 11}, {65455, 16}, {65456, 16}, {65457, 16}, {65458, 16},
			{65459, 16}, {65460, 16}, {65461, 16}, {65462, 16}, {0, 0}, {0, 0}, {0, 0}, {0, 0}, {0, 0}, {0, 0},
			{249, 8}, {65463, 16}, {65464, 16}, {65465, 16}, {65466, 16}, {65467, 16}, {65468, 16}, {65469, 16},
			{65470, 16}, {65471, 16}, {0, 0}, {0, 0}, {0, 0}, {0, 0}, {0, 0}, {0, 0}, {503, 9}, {65472, 16},
			{65473, 16}, {65474, 16}, {65475, 16}, {65476, 16}, {65477, 16}, {65478, 16}, {65479, 16}, {65480, 16},
			{0, 0}, {0, 0}, {0, 0}, {0, 0}, {0, 0}, {0, 0}, {504, 9}, {65481, 16}, {65482, 16}, {65483, 16},
			{65484, 16}, {65485, 16}, {65486, 16}, {65487, 16}, {65488, 16}, {65489, 16}, {0, 0}, {0, 0}, {0, 0},
			{0, 0}, {0, 0}, {0, 0}, {505, 9}, {65490, 16}, {65491, 16}, {65492, 16}, {65493, 16}, {65494, 16},
			{65495, 16}, {65496, 16}, {65497, 16}, {65498, 16}, {0, 0}, {0, 0}, {0, 0}, {0, 0}, {0, 0}, {0, 0},
			{506, 9}, {65499, 16}, {65500, 16}, {65501, 16}, {65502, 16}, {65503, 16}, {65504, 16}, {65505, 16},
			{65506, 16}, {65507, 16}, {0, 0}, {0, 0}, {0, 0}, {0, 0}, {0, 0}, {0, 0}, {2041, 11}, {65508, 16},
			{65509, 16}, {65510, 16}, {65511, 16}, {65512, 16}, {65513, 16}, {65514, 16}, {65515, 16}, {65516, 16},
			{0, 0}, {0, 0}, {0, 0}, {0, 0}, {0, 0}, {0, 0}, {16352, 14}, {65517, 16}, {65518, 16}, {65519, 16},
			{65520, 16}, {65521, 16}, {65522, 16}, {65523, 16}, {65524, 16}, {65525, 16}, {0, 0}, {0, 0}, {0, 0},
			{0, 0}, {0, 0}, {1018, 10}, {32707, 15}, {65526, 16}, {65527, 16}, {65528, 16}, {65529, 16}, {65530, 16},
			{65531, 16}, {65532, 16}, {65533, 16}, {65534, 16}, {0, 0}, {0, 0}, {0, 0}, {0, 0}, {0, 0}
		};

		private static ushort[,] stbi_write_jpg_core_UVDC_HT =
		{
			{0, 2}, {1, 2}, {2, 2}, {6, 3}, {14, 4}, {30, 5}, {62, 6}, {126, 7}, {254, 8}, {510, 9}, {1022, 10},
			{2046, 11}
		};

		private static int[] stbi_write_jpg_core_UVQT =
		{
			17, 18, 24, 47, 99, 99, 99, 99, 18, 21, 26, 66, 99, 99, 99, 99, 24, 26, 56, 99, 99, 99, 99, 99, 47, 66, 99,
			99, 99, 99, 99, 99, 99, 99, 99, 99, 99, 99, 99, 99, 99, 99, 99, 99, 99, 99, 99, 99, 99, 99, 99, 99, 99, 99,
			99, 99, 99, 99, 99, 99, 99, 99, 99, 99
		};

		private static ushort[,] stbi_write_jpg_core_YAC_HT =
		{
			{10, 4}, {0, 2}, {1, 2}, {4, 3}, {11, 4}, {26, 5}, {120, 7}, {248, 8}, {1014, 10}, {65410, 16}, {65411, 16},
			{0, 0}, {0, 0}, {0, 0}, {0, 0}, {0, 0}, {0, 0}, {12, 4}, {27, 5}, {121, 7}, {502, 9}, {2038, 11},
			{65412, 16}, {65413, 16}, {65414, 16}, {65415, 16}, {65416, 16}, {0, 0}, {0, 0}, {0, 0}, {0, 0}, {0, 0},
			{0, 0}, {28, 5}, {249, 8}, {1015, 10}, {4084, 12}, {65417, 16}, {65418, 16}, {65419, 16}, {65420, 16},
			{65421, 16}, {65422, 16}, {0, 0}, {0, 0}, {0, 0}, {0, 0}, {0, 0}, {0, 0}, {58, 6}, {503, 9}, {4085, 12},
			{65423, 16}, {65424, 16}, {65425, 16}, {65426, 16}, {65427, 16}, {65428, 16}, {65429, 16}, {0, 0}, {0, 0},
			{0, 0}, {0, 0}, {0, 0}, {0, 0}, {59, 6}, {1016, 10}, {65430, 16}, {65431, 16}, {65432, 16}, {65433, 16},
			{65434, 16}, {65435, 16}, {65436, 16}, {65437, 16}, {0, 0}, {0, 0}, {0, 0}, {0, 0}, {0, 0}, {0, 0},
			{122, 7}, {2039, 11}, {65438, 16}, {65439, 16}, {65440, 16}, {65441, 16}, {65442, 16}, {65443, 16},
			{65444, 16}, {65445, 16}, {0, 0}, {0, 0}, {0, 0}, {0, 0}, {0, 0}, {0, 0}, {123, 7}, {4086, 12}, {65446, 16},
			{65447, 16}, {65448, 16}, {65449, 16}, {65450, 16}, {65451, 16}, {65452, 16}, {65453, 16}, {0, 0}, {0, 0},
			{0, 0}, {0, 0}, {0, 0}, {0, 0}, {250, 8}, {4087, 12}, {65454, 16}, {65455, 16}, {65456, 16}, {65457, 16},
			{65458, 16}, {65459, 16}, {65460, 16}, {65461, 16}, {0, 0}, {0, 0}, {0, 0}, {0, 0}, {0, 0}, {0, 0},
			{504, 9}, {32704, 15}, {65462, 16}, {65463, 16}, {65464, 16}, {65465, 16}, {65466, 16}, {65467, 16},
			{65468, 16}, {65469, 16}, {0, 0}, {0, 0}, {0, 0}, {0, 0}, {0, 0}, {0, 0}, {505, 9}, {65470, 16},
			{65471, 16}, {65472, 16}, {65473, 16}, {65474, 16}, {65475, 16}, {65476, 16}, {65477, 16}, {65478, 16},
			{0, 0}, {0, 0}, {0, 0}, {0, 0}, {0, 0}, {0, 0}, {506, 9}, {65479, 16}, {65480, 16}, {65481, 16},
			{65482, 16}, {65483, 16}, {65484, 16}, {65485, 16}, {65486, 16}, {65487, 16}, {0, 0}, {0, 0}, {0, 0},
			{0, 0}, {0, 0}, {0, 0}, {1017, 10}, {65488, 16}, {65489, 16}, {65490, 16}, {65491, 16}, {65492, 16},
			{65493, 16}, {65494, 16}, {65495, 16}, {65496, 16}, {0, 0}, {0, 0}, {0, 0}, {0, 0}, {0, 0}, {0, 0},
			{1018, 10}, {65497, 16}, {65498, 16}, {65499, 16}, {65500, 16}, {65501, 16}, {65502, 16}, {65503, 16},
			{65504, 16}, {65505, 16}, {0, 0}, {0, 0}, {0, 0}, {0, 0}, {0, 0}, {0, 0}, {2040, 11}, {65506, 16},
			{65507, 16}, {65508, 16}, {65509, 16}, {65510, 16}, {65511, 16}, {65512, 16}, {65513, 16}, {65514, 16},
			{0, 0}, {0, 0}, {0, 0}, {0, 0}, {0, 0}, {0, 0}, {65515, 16}, {65516, 16}, {65517, 16}, {65518, 16},
			{65519, 16}, {65520, 16}, {65521, 16}, {65522, 16}, {65523, 16}, {65524, 16}, {0, 0}, {0, 0}, {0, 0},
			{0, 0}, {0, 0}, {2041, 11}, {65525, 16}, {65526, 16}, {65527, 16}, {65528, 16}, {65529, 16}, {65530, 16},
			{65531, 16}, {65532, 16}, {65533, 16}, {65534, 16}, {0, 0}, {0, 0}, {0, 0}, {0, 0}, {0, 0}
		};

		private static ushort[,] stbi_write_jpg_core_YDC_HT =
			{{0, 2}, {2, 3}, {3, 3}, {4, 3}, {5, 3}, {6, 3}, {14, 4}, {30, 5}, {62, 6}, {126, 7}, {254, 8}, {510, 9}};

		private static int[] stbi_write_jpg_core_YQT =
		{
			16, 11, 10, 16, 24, 40, 51, 61, 12, 12, 14, 19, 26, 58, 60, 55, 14, 13, 16, 24, 40, 57, 69, 56, 14, 17, 22,
			29, 51, 87, 80, 62, 18, 22, 37, 56, 68, 109, 103, 77, 24, 35, 55, 64, 81, 104, 113, 92, 49, 64, 78, 87, 103,
			121, 120, 101, 72, 92, 95, 98, 112, 100, 103, 99
		};

		private static byte[] stbiw__jpg_ZigZag =
{
			0, 1, 5, 6, 14, 15, 27, 28, 2, 4, 7, 13, 16, 26, 29, 42, 3, 8, 12, 17, 25, 30, 41, 43, 9, 11, 18, 24, 31,
			40, 44, 53, 10, 19, 23, 32, 39, 45, 52, 54, 20, 22, 33, 38, 46, 51, 55, 60, 21, 34, 37, 47, 50, 56, 59, 61,
			35, 36, 48, 49, 57, 58, 62, 63
		};

		private static void stbiw__jpg_DCT(ref float d0p, ref float d1p, ref float d2p, ref float d3p, ref float d4p,
			ref float d5p, ref float d6p, ref float d7p)
		{
			var d0 = d0p;
			var d1 = d1p;
			var d2 = d2p;
			var d3 = d3p;
			var d4 = d4p;
			var d5 = d5p;
			var d6 = d6p;
			var d7 = d7p;
			float z1 = 0;
			float z2 = 0;
			float z3 = 0;
			float z4 = 0;
			float z5 = 0;
			float z11 = 0;
			float z13 = 0;
			var tmp0 = d0 + d7;
			var tmp7 = d0 - d7;
			var tmp1 = d1 + d6;
			var tmp6 = d1 - d6;
			var tmp2 = d2 + d5;
			var tmp5 = d2 - d5;
			var tmp3 = d3 + d4;
			var tmp4 = d3 - d4;
			var tmp10 = tmp0 + tmp3;
			var tmp13 = tmp0 - tmp3;
			var tmp11 = tmp1 + tmp2;
			var tmp12 = tmp1 - tmp2;
			d0 = tmp10 + tmp11;
			d4 = tmp10 - tmp11;
			z1 = (tmp12 + tmp13) * 0.707106781f;
			d2 = tmp13 + z1;
			d6 = tmp13 - z1;
			tmp10 = tmp4 + tmp5;
			tmp11 = tmp5 + tmp6;
			tmp12 = tmp6 + tmp7;
			z5 = (tmp10 - tmp12) * 0.382683433f;
			z2 = tmp10 * 0.541196100f + z5;
			z4 = tmp12 * 1.306562965f + z5;
			z3 = tmp11 * 0.707106781f;
			z11 = tmp7 + z3;
			z13 = tmp7 - z3;
			d5p = z13 + z2;
			d3p = z13 - z2;
			d1p = z11 + z4;
			d7p = z11 - z4;
			d0p = d0;
			d2p = d2;
			d4p = d4;
			d6p = d6;
		}

		private static void stbiw__jpg_calcBits(int val, ushort[] bits)
		{
			var tmp1 = val < 0 ? -val : val;
			val = val < 0 ? val - 1 : val;
			bits[1] = 1;
			while ((tmp1 >>= 1) != 0)
				++bits[1];
			bits[0] = (ushort)(val & ((1 << bits[1]) - 1));
		}

		private static void stbiw__jpg_writeBits(BinaryWriter output, ref int bitBufP, ref int bitCntP, ushort bs0, ushort bs1)
		{
			var bitBuf = bitBufP;
			var bitCnt = bitCntP;
			bitCnt += bs1;
			bitBuf |= bs0 << (24 - bitCnt);
			while (bitCnt >= 8)
			{
				var c = (byte)((bitBuf >> 16) & 255);
				output.Write(c);
				if (c == 255)
				{
					output.Write((byte)0);
				}
				bitBuf <<= 8;
				bitCnt -= 8;
			}

			bitBufP = bitBuf;
			bitCntP = bitCnt;
		}


		private static int stbiw__jpg_processDU(BinaryWriter output, ref int bitBuf, ref int bitCnt, FakePtr<float> CDU,
			int du_stride, float[] fdtbl, int DC, ushort[,] HTDC, ushort[,] HTAC)
		{
			var EOB = new ushort[] { HTAC[0x00, 0], HTAC[0x00, 1] };
			var M16zeroes = new ushort[] { HTAC[0xF0, 0], HTAC[0xF0, 1] };
			var dataOff = 0;
			var i = 0;
			var j = 0;
			var n = 0;
			var diff = 0;
			var end0pos = 0;
			var x = 0;
			var y = 0;
			var DU = new int[64];
			for (dataOff = 0, n = du_stride * 8; dataOff < n; dataOff += du_stride)
				stbiw__jpg_DCT(ref CDU.Data[CDU.Offset + dataOff],
					ref CDU.Data[CDU.Offset + dataOff + 1],
					ref CDU.Data[CDU.Offset + dataOff + 2],
					ref CDU.Data[CDU.Offset + dataOff + 3],
					ref CDU.Data[CDU.Offset + dataOff + 4],
					ref CDU.Data[CDU.Offset + dataOff + 5],
					ref CDU.Data[CDU.Offset + dataOff + 6],
					ref CDU.Data[CDU.Offset + dataOff + 7]);
			for (dataOff = 0; dataOff < 8; ++dataOff)
				stbiw__jpg_DCT(ref CDU.Data[CDU.Offset + dataOff],
					ref CDU.Data[CDU.Offset + dataOff + du_stride],
					ref CDU.Data[CDU.Offset + dataOff + du_stride * 2],
					ref CDU.Data[CDU.Offset + dataOff + du_stride * 3],
					ref CDU.Data[CDU.Offset + dataOff + du_stride * 4],
					ref CDU.Data[CDU.Offset + dataOff + du_stride * 5],
					ref CDU.Data[CDU.Offset + dataOff + du_stride * 6],
					ref CDU.Data[CDU.Offset + dataOff + du_stride * 7]);
			for (y = 0, j = 0; y < 8; ++y)
				for (x = 0; x < 8; ++x, ++j)
				{
					float v = 0;
					i = y * du_stride + x;
					v = CDU[i] * fdtbl[j];
					DU[stbiw__jpg_ZigZag[j]] = (int)(v < 0 ? v - 0.5f : v + 0.5f);
				}

			diff = DU[0] - DC;
			if (diff == 0)
			{
				stbiw__jpg_writeBits(output, ref bitBuf, ref bitCnt, HTDC[0, 0], HTDC[0, 1]);
			}
			else
			{
				var bits = new ushort[2];
				stbiw__jpg_calcBits(diff, bits);
				stbiw__jpg_writeBits(output, ref bitBuf, ref bitCnt, HTDC[bits[1], 0], HTDC[bits[1], 1]);
				stbiw__jpg_writeBits(output, ref bitBuf, ref bitCnt, bits[0], bits[1]);
			}

			end0pos = 63;
			for (; end0pos > 0 && DU[end0pos] == 0; --end0pos)
			{
			}

			if (end0pos == 0)
			{
				stbiw__jpg_writeBits(output, ref bitBuf, ref bitCnt, EOB[0], EOB[1]);
				return DU[0];
			}

			for (i = 1; i <= end0pos; ++i)
			{
				var startpos = i;
				var nrzeroes = 0;
				var bits = new ushort[2];
				for (; DU[i] == 0 && i <= end0pos; ++i)
				{
				}

				nrzeroes = i - startpos;
				if (nrzeroes >= 16)
				{
					var lng = nrzeroes >> 4;
					var nrmarker = 0;
					for (nrmarker = 1; nrmarker <= lng; ++nrmarker)
						stbiw__jpg_writeBits(output, ref bitBuf, ref bitCnt, M16zeroes[0], M16zeroes[1]);
					nrzeroes &= 15;
				}

				stbiw__jpg_calcBits(DU[i], bits);
				stbiw__jpg_writeBits(output, ref bitBuf, ref bitCnt, HTAC[(nrzeroes << 4) + bits[1], 0],
					HTAC[(nrzeroes << 4) + bits[1], 1]);
				stbiw__jpg_writeBits(output, ref bitBuf, ref bitCnt, bits[0], bits[1]);
			}

			if (end0pos != 63)
				stbiw__jpg_writeBits(output, ref bitBuf, ref bitCnt, EOB[0], EOB[1]);
			return DU[0];
		}


		/// <summary>Writes JPEG-compressed data to a <see cref="BinaryWriter"/>.</summary>
		/// <param name="output">The destination binary writer.</param>
		/// <param name="data">Raw pixel data array (RGB or RGBA).</param>
		/// <param name="width">Image width in pixels.</param>
		/// <param name="height">Image height in pixels.</param>
		/// <param name="compEnum">Color component format (e.g. RedGreenBlue, RedGreenBlueAlpha).</param>
		/// <param name="quality">JPEG quality (1–100), default 90.</param>
		public static void EncodeToWriter(BinaryWriter output, byte[] data, int width, int height, ColorComponents compEnum, int quality = 90)
		{
			var row = 0;
			var col = 0;
			var i = 0;
			var k = 0;
			var subsample = 0;
			var fdtbl_Y = new float[64];
			var fdtbl_UV = new float[64];
			var YTable = new byte[64];
			var UVTable = new byte[64];

			quality = quality != 0 ? quality : 90;
			subsample = quality <= 90 ? 1 : 0;
			quality = quality < 1 ? 1 : quality > 100 ? 100 : quality;
			quality = quality < 50 ? 5000 / quality : 200 - quality * 2;
			for (i = 0; i < 64; ++i)
			{
				var uvti = 0;
				var yti = (stbi_write_jpg_core_YQT[i] * quality + 50) / 100;
				YTable[stbiw__jpg_ZigZag[i]] = (byte)(yti < 1 ? 1 : yti > 255 ? 255 : yti);
				uvti = (stbi_write_jpg_core_UVQT[i] * quality + 50) / 100;
				UVTable[stbiw__jpg_ZigZag[i]] = (byte)(uvti < 1 ? 1 : uvti > 255 ? 255 : uvti);
			}

			for (row = 0, k = 0; row < 8; ++row)
				for (col = 0; col < 8; ++col, ++k)
				{
					fdtbl_Y[k] = 1 / (YTable[stbiw__jpg_ZigZag[k]] * stbi_write_jpg_core_aasf[row] *
									  stbi_write_jpg_core_aasf[col]);
					fdtbl_UV[k] = 1 / (UVTable[stbiw__jpg_ZigZag[k]] * stbi_write_jpg_core_aasf[row] *
									   stbi_write_jpg_core_aasf[col]);
				}

			{
				var head1 = new byte[]
				{
					0xFF,
					0xC0,
					0,
					0x11,
					8,
					(byte)(height >> 8),
					(byte)(height & 0xff),
					(byte)(width >> 8),
					(byte)(width & 0xff),
					3,
					1,
					(byte)(subsample != 0 ? 0x22 : 0x11),
					0,
					2,
					0x11,
					1,
					3,
					0x11,
					1,
					0xFF,
					0xC4,
					0x01,
					0xA2,
					0
				};

				output.Write(stbi_write_jpg_core_head0);
				output.Write(YTable);
				output.Write((byte)1);
				output.Write(UVTable);
				output.Write(head1);
				output.Write(stbi_write_jpg_core_std_dc_luminance_nrcodes, 1, stbi_write_jpg_core_std_dc_luminance_nrcodes.Length - 1);
				output.Write(stbi_write_jpg_core_std_dc_luminance_values);
				output.Write((byte)0x10);
				output.Write(stbi_write_jpg_core_std_ac_luminance_nrcodes, 1, stbi_write_jpg_core_std_ac_luminance_nrcodes.Length - 1);
				output.Write(stbi_write_jpg_core_std_ac_luminance_values);
				output.Write((byte)1);
				output.Write(stbi_write_jpg_core_std_dc_chrominance_nrcodes, 1, stbi_write_jpg_core_std_dc_chrominance_nrcodes.Length - 1);
				output.Write(stbi_write_jpg_core_std_dc_chrominance_values);
				output.Write((byte)0x11);
				output.Write(stbi_write_jpg_core_std_ac_chrominance_nrcodes, 1, stbi_write_jpg_core_std_ac_chrominance_nrcodes.Length - 1);
				output.Write(stbi_write_jpg_core_std_ac_chrominance_values);
				output.Write(stbi_write_jpg_core_head2);
			}

			{
				var DCY = 0;
				var DCU = 0;
				var DCV = 0;
				var bitBuf = 0;
				var bitCnt = 0;

				var comp = (int)compEnum;
				var ofsG = comp > 2 ? 1 : 0;
				var ofsB = comp > 2 ? 2 : 0;
				var dataR = new FakePtr<byte>(data);
				var dataG = dataR + ofsG;
				var dataB = dataR + ofsB;
				var x = 0;
				var y = 0;
				var pos = 0;
				if (subsample != 0)
				{
					var Y = new FakePtr<float>(256);
					var U = new FakePtr<float>(256);
					var V = new FakePtr<float>(256);
					var subU = new FakePtr<float>(64);
					var subV = new FakePtr<float>(64);
					for (y = 0; y < height; y += 16)
						for (x = 0; x < width; x += 16)
						{
							for (row = y, pos = 0; row < y + 16; ++row)
							{
								var clamped_row = row < height ? row : height - 1;
								var base_p =
									(EncodingOptions.FlipVerticallyOnWrite ? height - 1 - clamped_row : clamped_row) * width *
									comp;
								for (col = x; col < x + 16; ++col, ++pos)
								{
									var p = base_p + (col < width ? col : width - 1) * comp;
									float r = dataR[p];
									float g = dataG[p];
									float b = dataB[p];
									Y[pos] = +0.29900f * r + 0.58700f * g + 0.11400f * b - 128;
									U[pos] = -0.16874f * r - 0.33126f * g + 0.50000f * b;
									V[pos] = +0.50000f * r - 0.41869f * g - 0.08131f * b;
								}
							}

							DCY = stbiw__jpg_processDU(output, ref bitBuf, ref bitCnt, Y + 0, 16, fdtbl_Y, DCY,
								stbi_write_jpg_core_YDC_HT, stbi_write_jpg_core_YAC_HT);
							DCY = stbiw__jpg_processDU(output, ref bitBuf, ref bitCnt, Y + 8, 16, fdtbl_Y, DCY,
								stbi_write_jpg_core_YDC_HT, stbi_write_jpg_core_YAC_HT);
							DCY = stbiw__jpg_processDU(output, ref bitBuf, ref bitCnt, Y + 128, 16, fdtbl_Y, DCY,
								stbi_write_jpg_core_YDC_HT, stbi_write_jpg_core_YAC_HT);
							DCY = stbiw__jpg_processDU(output, ref bitBuf, ref bitCnt, Y + 136, 16, fdtbl_Y, DCY,
								stbi_write_jpg_core_YDC_HT, stbi_write_jpg_core_YAC_HT);
							{
								var yy = 0;
								var xx = 0;
								for (yy = 0, pos = 0; yy < 8; ++yy)
									for (xx = 0; xx < 8; ++xx, ++pos)
									{
										var j = yy * 32 + xx * 2;
										subU[pos] = (U[j + 0] + U[j + 1] + U[j + 16] + U[j + 17]) * 0.25f;
										subV[pos] = (V[j + 0] + V[j + 1] + V[j + 16] + V[j + 17]) * 0.25f;
									}

								DCU = stbiw__jpg_processDU(output, ref bitBuf, ref bitCnt, subU, 8, fdtbl_UV, DCU,
									stbi_write_jpg_core_UVDC_HT, stbi_write_jpg_core_UVAC_HT);
								DCV = stbiw__jpg_processDU(output, ref bitBuf, ref bitCnt, subV, 8, fdtbl_UV, DCV,
									stbi_write_jpg_core_UVDC_HT, stbi_write_jpg_core_UVAC_HT);
							}
						}
				}
				else
				{
					var Y = new FakePtr<float>(64);
					var U = new FakePtr<float>(64);
					var V = new FakePtr<float>(64);
					for (y = 0; y < height; y += 8)
						for (x = 0; x < width; x += 8)
						{
							for (row = y, pos = 0; row < y + 8; ++row)
							{
								var clamped_row = row < height ? row : height - 1;
								var base_p =
									(EncodingOptions.FlipVerticallyOnWrite ? height - 1 - clamped_row : clamped_row) * width *
									comp;
								for (col = x; col < x + 8; ++col, ++pos)
								{
									var p = base_p + (col < width ? col : width - 1) * comp;
									float r = dataR[p];
									float g = dataG[p];
									float b = dataB[p];
									Y[pos] = +0.29900f * r + 0.58700f * g + 0.11400f * b - 128;
									U[pos] = -0.16874f * r - 0.33126f * g + 0.50000f * b;
									V[pos] = +0.50000f * r - 0.41869f * g - 0.08131f * b;
								}
							}

							DCY = stbiw__jpg_processDU(output, ref bitBuf, ref bitCnt, Y, 8, fdtbl_Y, DCY, stbi_write_jpg_core_YDC_HT,
								stbi_write_jpg_core_YAC_HT);
							DCU = stbiw__jpg_processDU(output, ref bitBuf, ref bitCnt, U, 8, fdtbl_UV, DCU,
								stbi_write_jpg_core_UVDC_HT, stbi_write_jpg_core_UVAC_HT);
							DCV = stbiw__jpg_processDU(output, ref bitBuf, ref bitCnt, V, 8, fdtbl_UV, DCV,
								stbi_write_jpg_core_UVDC_HT, stbi_write_jpg_core_UVAC_HT);
						}
				}

				stbiw__jpg_writeBits(output, ref bitBuf, ref bitCnt, stbi_write_jpg_core_fillBits[0],
					stbi_write_jpg_core_fillBits[1]);
			}

			output.Write((byte)0xFF);
			output.Write((byte)0xD9);
		}

		/// <summary>Writes JPEG-compressed data to an output <see cref="Stream"/>.</summary>
		/// <param name="output">The destination stream.</param>
		/// <param name="data">Raw pixel data array.</param>
		/// <param name="width">Image width in pixels.</param>
		/// <param name="height">Image height in pixels.</param>
		/// <param name="compEnum">Color component format.</param>
		/// <param name="quality">JPEG quality (1–100), default 90.</param>
		public static void EncodeToStream(Stream output, byte[] data, int width, int height, ColorComponents compEnum, int quality = 90)
		{
			using (var writer = new BinaryWriter(output, Encoding.UTF8, true))
			{
				EncodeToWriter(writer, data, width, height, compEnum, quality);
			}
		}

		/// <summary>Encodes pixel data into a JPEG byte array at the configured quality.</summary>
		/// <param name="data">Raw pixel data array.</param>
		/// <param name="width">Image width in pixels.</param>
		/// <param name="height">Image height in pixels.</param>
		/// <param name="compEnum">Color component format.</param>
		/// <param name="quality">JPEG quality (1–100), default 90.</param>
		/// <returns>The JPEG-encoded byte array.</returns>
		public static byte[] EncodeToByteArray(byte[] data, int width, int height, ColorComponents compEnum, int quality = 90)
		{
			using (var stream = new MemoryStream())
			{
				EncodeToStream(stream, data, width, height, compEnum, quality);
				return stream.ToArray();
			}
		}
	}
}
