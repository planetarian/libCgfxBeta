using System;
using System.Collections.Generic;

/* SOBJ! Based on docs from 3dbrew along with some original research.
 * Some small updates thanks to additional information from Gericom.
 * This whole thing will be getting HEAVILY refactored soon,
 * so I don't recommend actually using this source code.
 * ...Actually, this one's probably fine.
 * -planetarian-
 */

namespace libCgfx
{
    public class Txob : FlaggedCtrObject
    {

        public bool IsImageTexture { get; private set; }
        public bool IsReferenceTexture { get; private set; }

        public int Width { get; private set; }
        public int Height { get; private set; }
        public int MipmapLevels { get; private set; }
        public PixelFormat Format { get; private set; }
        public int Width2 { get; private set; }
        public int Height2 { get; private set; }
        public int DataSize { get; private set; }
        public Offset ImageDataOffset { get; private set; }
        public byte[] ImageData { get; private set; }
        public Texture Image { get; private set; }

        public Offset NameOffset { get; private set; }
        public string Name { get; private set; }

        public Offset ReferencedNameOffset { get; private set; }
        public string ReferencedName { get; private set; }

        public Offset ReferencedTxobOffset { get; private set; }

        
        public Txob(CtrObject parent) : this(parent, parent.CurrentOffset)
        {
        }
        public Txob(CtrObject parent, int startOffset) : base(parent, startOffset)
        {
            Flags = ReadUInt32();

            Magic = ReadMagic();
            Log("Magic '" + Magic + "' for offset " + DisplayValue(StartOffset));
            LogIndent();

            // via Gericom:
            //Image Texture = 0x20000011
            //Cube Texture = 0x20000009
            //Reference Texture = 0x20000004
            //Procedural Texture = 0x20000002
            //Shadow Texture = 0x20000021
            Log("Flags: " + DisplayValue(Flags), 2);

            IsReferenceTexture = Flags == 0x20000004;
            Log("IsReferenceTexture: " + IsReferenceTexture);
            IsImageTexture = Flags == 0x20000011;
            Log("IsImageTexture: " + IsImageTexture);

            // 0x8
            Revision = ReadInt32();
            Log("Revision: " + DisplayValue(Revision));

            // 0xC
            NameOffset = ReadOffset();
            Log("NameOffset " + NameOffset, 2);
            Name = ReadStringTerminated(NameOffset.Absolute);
            Log("Name: '" + Name + "'");
            
            // 0x10
            Skip(0x8);

            if (IsReferenceTexture)
                ReadReferenceTexture();
            else if (IsImageTexture)
                ReadImageTexture();
            else
            {
                // not implemented
            }



            LogIndent(false);
        }

        private void ReadImageTexture()
        {
            // 0x18
            Height = ReadInt32();
            Log("Height: " + DisplayValue(Height));
            // 0x1C
            Width = ReadInt32();
            Log("Width: " + DisplayValue(Width));

            // 0x20
            Skip(0x8);

            // 0x28
            MipmapLevels = ReadInt32();
            Log("Mipmap Levels: " + DisplayValue(MipmapLevels));

            Skip(0x8);

            Format = (PixelFormat)ReadInt32();
            Log("Format: " + DisplayValue((byte)Format) + " [" + Format + "]");

            Skip(0x4);

            Height2 = ReadInt32();
            Log("Height2: " + DisplayValue(Height2), 1);
            Width2 = ReadInt32();
            Log("Width2: " + DisplayValue(Width2), 1);

            DataSize = ReadInt32();
            Log("Size: " + DisplayValue(DataSize));

            ImageDataOffset = ReadOffset();
            Log("ImageDataOffset " + ImageDataOffset, 2);

            ImageData = ReadBytes(ImageDataOffset.Absolute, DataSize);
            Image = ImageData.ToTexture(Width, Height, Format);

            RootObject.Textures.Add(Name, Image);
        }

        private void ReadReferenceTexture()
        {
            ReferencedNameOffset = ReadOffset();
            Log("ReferencedNameOffset: " + ReferencedNameOffset);
            ReferencedName = ReadStringTerminated(ReferencedNameOffset.Absolute);
            Log("ReferencedName: '" + ReferencedName + "'");

            ReferencedTxobOffset = ReadOffset();
            Log("ReferencedTxobOffset: " + ReferencedTxobOffset);
        }

    }

    public static class TextureExtensions
    {
        #region arrays

        private static readonly int[] PixelFormatBytes =
        { 4, 3, 2, 2, 2, 2, 2, 1, 1, 1, 1, 1, 1, 1 };

        // unused int[] PixelFormatBits
        //private static readonly int[] PixelFormatBits =
        //{ 32, 24, 16, 16, 16, 16, 16, 8, 8, 8, 8, 8, 8, 8 };


        private static readonly bool[] PixelFormatHasAlpha =
        {
            true, false, true, false, true, true, false,
            false, true, true, false, true, false, true
        };

        //Decode RGB5A4 Taken from the dolphin project
        private static readonly byte[] Expand5To8 =
        {
            0x00, 0x08, 0x10, 0x18, 0x20, 0x29, 0x31, 0x39,
            0x41, 0x4A, 0x52, 0x5A, 0x62, 0x6A, 0x73, 0x7B,
            0x83, 0x8B, 0x94, 0x9C, 0xA4, 0xAC, 0xB4, 0xBD,
            0xC5, 0xCD, 0xD5, 0xDE, 0xE6, 0xEE, 0xF6, 0xFF
        };

        // unused byte[] ReadBitsMask
        //private static readonly byte[] ReadBitsMask =
        //{0x00, 0x01, 0x03, 0x07, 0x0F, 0x1F, 0x3F, 0x7F, 0xFF};

        private static readonly byte[] TileOrder =
		{
			 0,  1,   4,  5,
			 2,  3,   6,  7,
			 8,  9,  12, 13,  
			10, 11,  14, 15
		};

        private static readonly byte[,] Etc1Modifiers = 
		{	
			{ 2, 8 },
			{ 5, 17 },
			{ 9, 29 },
			{ 13, 42 },
			{ 18, 60 },
			{ 24, 80 },
			{ 33, 106 },
			{ 47, 183 }
		};

        #endregion arrays

        public static Texture ToTexture(
            this byte[] data, int width, int height, PixelFormat format)
        {
            // Sanity check
            if (data == null || data.Length < 1 || width < 1 || height < 1)
                throw new ArgumentException();

            var texture = new Texture(width, height)
            {
                HasAlpha = format.HasAlpha(),
                RawData = data,
                RawTextureFormat = format,
                Data = new byte[width*height*4]
            };

            int bytesPerPixel = format.Bytes();

            int offset = 0;

            // Iterate tiles in image
            for (int tileY = 0; tileY < height; tileY += 8)
            {
                for (int tileX = 0; tileX < width; tileX += 8)
                {
                    // ETC1 compressed texture format
                    // https://www.khronos.org/registry/gles/extensions/OES/OES_compressed_ETC1_RGB8_texture.txt
                    if (format == PixelFormat.Etc1 || format == PixelFormat.Etc1A4)
                    {
                        // Iterate blocks in tile
                        // ETC1 blocks are arranged columns-first
                        for (int blockY = 0; blockY < 8; blockY += 4)
                        {
                            for (int blockX = 0; blockX < 8; blockX += 4, offset += 8)
                            {
                                // Alpha in Etc1A4 is encoded as set of sixteen 4-bit values
                                ulong alpha = 0xffffffffffffffff;
                                if (format == PixelFormat.Etc1A4)
                                {
                                    alpha = data.ReadUInt64LittleEndian(offset);
                                    offset += 8;
                                }

                                // Pixel data is held in a 64-bit block with one of two formats
                                // diffbit, flipbit, and tables 1 and 2 are the same between them
                                ulong etc = data.ReadUInt64LittleEndian(offset);

                                int r1, g1, b1, r2, g2, b2;
                                bool differentialMode = etc.IsBitSet(33);
                                if (differentialMode)
                                {
                                    /* Differential mode
                                    |63 62 61 60 59|58 57 56|55 54 53 52 51|50 49 48|
                                    | base col1    | dcol 2 | base col1    | dcol 2 |
                                    | R1' (5 bits) | dR2    | G1' (5 bits) | dG2    |
    
                                    |47 46 45 44 43|42 41 40|39 38 37|36 35 34| 33 | 32 |
                                    | base col 1   | dcol 2 | table  | table  |diff|flip|
                                    | B1' (5 bits) | dB2    | cw 1   | cw 2   |bit |bit |
                                    */
                                    int br1 = etc.Read5(59);
                                    int bg1 = etc.Read5(51);
                                    int bb1 = etc.Read5(43);
                                    int dr2 = etc.Read3Diff(56); // -4 to +3 modifier
                                    int dg2 = etc.Read3Diff(48);
                                    int db2 = etc.Read3Diff(40);
                                    r1 = Expand5To8[br1]; // Spec calls for first three bits
                                    g1 = Expand5To8[bg1]; // duplicated to last three bits
                                    b1 = Expand5To8[bb1];
                                    r2 = Expand5To8[br1 + dr2];
                                    g2 = Expand5To8[bg1 + dg2];
                                    b2 = Expand5To8[bb1 + db2];
                                }
                                else
                                {
                                    /* Individual mode
                                    |63 62 61 60|59 58 57 56|55 54 53 52|51 50 49 48|
                                    | base col1 | base col2 | base col1 | base col2 |
                                    | R1 (4bits)| R2 (4bits)| G1 (4bits)| G2 (4bits)|
                                    
                                    |47 46 45 44|43 42 41 40|39 38 37|36 35 34| 33 | 32 |
                                    | base col1 | base col2 | table  | table  |diff|flip|
                                    | B1 (4bits)| B2 (4bits)| cw 1   | cw 2   |bit |bit |                                    
                                    */
                                    r1 = etc.Read4To8(60);
                                    r2 = etc.Read4To8(56);
                                    g1 = etc.Read4To8(52);
                                    g2 = etc.Read4To8(48);
                                    b1 = etc.Read4To8(44);
                                    b2 = etc.Read4To8(40);
                                }

                                int table1 = etc.Read3(37);
                                int table2 = etc.Read3(34);

                                // true: stacked, false: side-by-side
                                bool flipBit = etc.IsBitSet(32);

                                // Iterate pixels in 4x4 block
                                for (int pixelY = 0; pixelY < 4; pixelY++)
                                {
                                    for (int pixelX = 0; pixelX < 4; pixelX++)
                                    {

                                        // If data lies beyond image bounds, it is unused
                                        if (tileX + blockX + pixelX > width || tileY + blockY + pixelY > height)
                                            continue;

                                        int table, r, g, b;

                                        // Determine whether this is the first or second sub-block
                                        bool isFirstSubBlock =
                                            (flipBit && pixelY < 2) || (!flipBit && pixelX < 2);
                                        if (isFirstSubBlock)
                                        {
                                            table = table1;
                                            r = r1;
                                            g = g1;
                                            b = b1;
                                        }
                                        else
                                        {
                                            table = table2;
                                            r = r2;
                                            g = g2;
                                            b = b2;
                                        }

                                        // Get the bit index for this pixel
                                        int pixelBit = pixelX * 4 + pixelY;

                                        // Set which modifier value to use
                                        int subModifierIndex = etc.GetBit(pixelBit);

                                        // Set whether the modifier is positive or negative
                                        bool isModifierNegative = etc.IsBitSet(pixelBit + 16);
                                        int modifierNegator = isModifierNegative ? -1 : 1;

                                        // Retrieve the final modifier value
                                        int m = Etc1Modifiers[table, subModifierIndex] * modifierNegator;

                                        // Get alpha
                                        var a = (byte)alpha.Read4To8(pixelBit * 4);
                                        
                                        // Determine exactly where this pixel is in the final texture data
                                        int pixelIndex = ((height - 1) - (tileY + blockY + pixelY))*width +
                                                         tileX + blockX + pixelX;
                                        int componentIndex = pixelIndex * 4;
                                        
                                        // Apply ARGB components
                                        texture.Data[componentIndex++] = a;
                                        texture.Data[componentIndex++] = Clamp(r + m);
                                        texture.Data[componentIndex++] = Clamp(g + m);
                                        texture.Data[componentIndex] = Clamp(b + m);

                                    }
                                } // end of block pixels

                            }
                        } // end of tile blocks

                    } // end of ETC1[A4]

                    else // Everything else
                    {
                        for (int i = 0; i < 64; i++)
                        {
                            int pixelX = i % 8;
                            int pixelY = i / 8;
                            if (tileX + pixelX >= width || tileY + pixelY >= height)
                                continue;

                            int pos = TileOrder[pixelX % 4 + pixelY % 4 * 4] + 16 * (pixelX / 4) + 32 * (pixelY / 4);
                            int pixelIndex = ((((height-1)-(tileY + pixelY))*width + (tileX + pixelX))*4);
                            int dest = pixelIndex;
                            int src = offset + pos * bytesPerPixel;

                            byte[] argb = data.ToArgb(src, format);
                            for (int c = 0; c < 4; c++)
                                texture.Data[dest + c] = argb[c];
                        }
                        offset += 64 * bytesPerPixel;
                    }

                }
            } // end of image tiles

            return texture;
        }

        #region private methods

        private static byte[] ToArgb(this byte[] data, int offset, PixelFormat format)
        {
            int bytes = PixelFormatBytes[(int)format];
            uint val = data.ReadUInt(offset, bytes);

            return ToArgb(val, format);
        }

        private static byte[] ToArgb(this uint val, PixelFormat pixelFormat)
        {
            uint r, g, b;
            uint a = r = g = b = 0xff;

            switch (pixelFormat)
            {
                case PixelFormat.Rgba8:
                    r = (val >> 24) & 0xFF;
                    g = (val >> 16) & 0xFF;
                    b = (val >> 8) & 0xFF;
                    a = val & 0xFF;
                    break;
                case PixelFormat.Rgb8:
                    r = (val >> 16) & 0xFF;
                    g = (val >> 8) & 0xFF;
                    b = val & 0xFF;
                    break;
                case PixelFormat.Rgba5551:
                    r = Expand5To8[(val >> 11) & 0x1F];
                    g = Expand5To8[(val >> 6) & 0x1F];
                    b = Expand5To8[(val >> 1) & 0x1F];
                    a = (val & 0x0001) == 1 ? (uint)0xFF : 0x00;
                    break;
                case PixelFormat.Rgb565:
                    r = Expand5To8[(val >> 11) & 0x1F];
                    g = ((val >> 5) & 0x3F)*4;
                    b = Expand5To8[val & 0x1F];
                    break;
                case PixelFormat.Rgba4:
                    a = 0x11 * (val & 0xf);
                    r = 0x11 * ((val >> 12) & 0xf);
                    g = 0x11 * ((val >> 8) & 0xf);
                    b = 0x11 * ((val >> 4) & 0xf);
                    break;
                case PixelFormat.La8:
                    a = val & 0xFF;
                    r = g = b = val >> 8;
                    break;
                case PixelFormat.Hilo8: //use only the HI
                    r = g = b = val >> 8;
                    break;
                case PixelFormat.L8:
                    r = g = b = val;
                    break;
                case PixelFormat.A8:
                    a = val;
                    break;
                case PixelFormat.La4:
                    r = g = b = val >> 4;
                    a = val & 0x0F;
                    break;
            }
            return new[] { (byte)a, (byte)r, (byte)g, (byte)b };
        }


        private static byte Clamp(this int value, byte max = 0xff)
        {
            if (value > max) return max;
            if (value < 0) return 0;
            return (byte)value;
        }

        /* unused byte[] Clamp
        private static byte[] Clamp(params int[] values)
        {
            var clamped = new byte[values.Length];
            for (int i = 0; i < values.Length; i++)
                clamped[i] = values[i].Clamp();
            return clamped;
        }//*/





        /// <summary>
        /// Returns a value between -4 and +3 based on
        /// the three bits at the provided offset.
        /// </summary>
        private static int Read3Diff(this ulong data, int bitOffset)
        {
            return Read3(data, bitOffset) << 29 >> 29;
        }

        private static int Read3(this ulong data, int bitOffset)
        {
            return (int)(data >> bitOffset) & 0x7;
        }
        
        private static int Read5(this ulong data, int bitOffset)
        {
            return (int)(data >> bitOffset) & 0x1F;
        }

        private static int Read4To8(this ulong data, int bitOffset)
        {
            // Multiply by 0x11 to expand four-to-eight.
            // e.g. 1110 * 0x11 == 11101110
            return (int)((data >> bitOffset) & 0xF) * 0x11;
        }

        /* unused byte Read5To8
        private static byte Read5To8(this ulong data, int bitOffset)
        {
            return Expand5To8[Read5(data, bitOffset)];
        }//*/



        private static uint ReadUInt(this byte[] bytes, int offset, int len)
        {
            if (len == 1)
                return bytes[offset];
            if (len == 2)
                return bytes.ReadUInt16(offset);
            if (len == 3)
                return bytes.ReadUInt24(offset);
            if (len == 4)
                return bytes.ReadUInt32(offset);

            throw new NotSupportedException(
                "Unsupported bpp with " + len + " byte length.");
        }

        private static uint ReadUInt16(this byte[] bytes, int offset)
        {
            return BitConverter.ToUInt16(bytes, offset);
            /* endiaaaaaaaaan
            return (uint)(bytes[offset++] << 8 |
                   bytes[offset]);*/
        }

        private static uint ReadUInt24(this byte[] bytes, int offset)
        {
            return (uint) (bytes[offset++] | bytes[offset++] << 8 | bytes[offset] << 16);
            /*return (uint)(bytes[offset++] << 16 |
                   bytes[offset++] << 8 |
                   bytes[offset]);*/
        }

        private static uint ReadUInt32(this byte[] bytes, int offset)
        {
            return BitConverter.ToUInt32(bytes, offset);
            /*return (uint)(bytes[offset++] << 24 |
                   bytes[offset++] << 16 |
                   bytes[offset++] << 8 |
                   bytes[offset]);*/
        }

        private static ulong ReadUInt64LittleEndian(this IList<byte> data, int offset)
        {
            return data[offset++]
                   | ((ulong)data[offset++] << 8)
                   | ((ulong)data[offset++] << 16)
                   | ((ulong)data[offset++] << 24)
                   | ((ulong)data[offset++] << 32)
                   | ((ulong)data[offset++] << 40)
                   | ((ulong)data[offset++] << 48)
                   | ((ulong)data[offset] << 56);
        }


        private static bool HasAlpha(this PixelFormat format)
        {
            return PixelFormatHasAlpha[(byte) format];
        }

        private static int Bytes(this PixelFormat format)
        {
            return PixelFormatBytes[(byte)format];
        }

        #endregion private methods

    }

    public enum PixelFormat
    {
        Rgba8 = 0,
        Rgb8,
        Rgba5551,
        Rgb565,
        Rgba4,
        La8,
        Hilo8,
        L8,
        A8,
        La4,
        L4,
        A4,
        Etc1,
        Etc1A4
    }

    public class Texture
    {
        public string Name;
        public int Width;
        public int Height;
        public bool HasAlpha;
        public byte[] Data;
        public byte[] RawData;
        public PixelFormat RawTextureFormat;
        public Texture()
        {
        }

        public Texture(int width, int height)
        {
            Width = width;
            Height = height;
        }
    }
}
