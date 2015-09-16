using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Configuration;
using System.Text;
using Microsoft.Xna.Framework;

/* Base CTR Object!
 * This whole thing will be getting HEAVILY refactored soon,
 * so I don't recommend actually using this source code.
 * -planetarian-
 */

namespace libCgfx
{
    public abstract class CtrObject
    {
        private static readonly string[] magicStrings =
        {
            "CGFX",
            "DATA",
            "IMAG",
            "DICT",
            "CMDL",
            "SOBJ",
            "MTOB",
            "TXOB",
            "CANM",
            "LUTS"
        };

        public static int LogIndentLevel { get; private set; }

        public Cgfx RootObject { get; internal set; }
        public CtrObject ParentObject { get; internal set; }
        public string InputFilename { get; internal set; }
        public byte[] InputFile { get; internal set; }

        public int StartOffset { get; internal set; }
        public int PreviousOffset { get; internal set; }
        public int CurrentOffset { get; private set; }
        public string Magic { get; internal set; }
        public List<CtrObject> Objects { get; private set; }

        public Action<string, int, int> LogCallbackAction { get; private set; }



        protected CtrObject(CtrObject parent, int startOffset)
        {
            ParentObject = parent;
            LogCallbackAction = ParentObject.LogCallbackAction;
            RootObject = parent.RootObject;
            InputFile = RootObject.InputFile;
            InputFilename = RootObject.InputFilename;
            StartOffset = startOffset;
            CurrentOffset = startOffset;
            Objects = new List<CtrObject>();
        }

        protected CtrObject(string inputFilename, Action<string, int, int> logCallbackAction = null)
        {
            LogCallbackAction = logCallbackAction;
            ParentObject = null;
            InputFilename = inputFilename;
            InputFile = File.ReadAllBytes(inputFilename);
            StartOffset = 0;
            CurrentOffset = 0;
            Objects = new List<CtrObject>();
        }


        public static string DisplayValue<T>(T value) where T : struct,
            IComparable, IComparable<T>, IConvertible, IEquatable<T>, IFormattable
        {
            if (typeof(T) == typeof(float))
            {
                float floatValue = value.ToSingle(CultureInfo.InvariantCulture);
                return value + " (0x" +
                       BitConverter.ToInt32(BitConverter.GetBytes(floatValue), 0).ToString("X")
                       + ")";
            }
            return value + " (0x" + value.ToString("X", null) + ")";
        }

        public static string DisplayValue(Vector2 value)
        {
            return "(" + value.X + "," + value.Y + ") [" +
                   DisplayValue(value.X) + ", " + DisplayValue(value.Y) +
                   "]";
        }

        public static string DisplayValue(Vector3 value)
        {
            return "(" + value.X + "," + value.Y + "," + value.Z + ") [" +
                   DisplayValue(value.X) + ", " + DisplayValue(value.Y) + ", " + DisplayValue(value.Z) +
                   "]";
        }



        #region logging


        protected void Log(string message)
        {
            Log(message, 0);
        }

        protected void Log(string message, bool indent)
        {
            Log(message, 0, indent);
        }

        protected void Log(string message, int level, bool indent = false)
        {
            LogCallbackAction(message, LogIndentLevel + (indent ? 1 : 0), level);
        }

        protected void Log(Enum message)
        {
            Log(message, 0);
        }

        protected void Log(Enum message, bool indent)
        {
            Log(message, 0, indent);
        }

        protected void Log(Enum message, int level, bool indent = false)
        {
            LogCallbackAction(message.ToString(), LogIndentLevel + (indent ? 1 : 0), level);
        }

        protected void LogIndent(bool indent = true)
        {
            LogIndentLevel += indent ? 1 : -1;
        }

        protected void LogBreak(int level = 0)
        {
            Log("", level);
        }

        #endregion logging
        


        #region sequential read operations

        /// <summary>
        /// Reads a number of bytes starting from the current offset.
        /// </summary>
        /// <param name="bytes">Number of bytes to read.</param>
        /// <param name="consume">Whether to advance the current offset.</param>
        /// <returns>Array containing the read bytes.</returns>
        protected byte[] ReadBytes(int bytes, bool consume = true, bool cover = true)
        {
            if (cover)
                RootObject.Coverage.Add(CurrentOffset, CurrentOffset + bytes - 1);

            PreviousOffset = CurrentOffset;
            var buf = new byte[bytes];
            Array.Copy(InputFile, CurrentOffset, buf, 0, bytes);
            if (consume) CurrentOffset += bytes;
            return buf;
        }

        /// <summary>
        /// Reads a byte from the current offset.
        /// </summary>
        /// <param name="consume">Whether to advance the current offset.</param>
        /// <returns>The read Byte.</returns>
        protected byte ReadByte(bool consume = true, bool cover = true)
        {
            if (cover)
                RootObject.Coverage.Add(CurrentOffset);

            byte result = InputFile[CurrentOffset];
            if (consume) CurrentOffset++;
            return result;
        }

        /// <summary>
        /// Reads a signed short starting from the current offset.
        /// </summary>
        /// <param name="consume">Whether to advance the current offset.</param>
        /// <returns>The read Int16.</returns>
        protected Int16 ReadInt16(bool consume = true)
        {
            byte[] buf = ReadBytes(2, consume);
            return BitConverter.ToInt16(buf, 0);
        }

        /// <summary>
        /// Reads an unsigned short starting from the current offset.
        /// </summary>
        /// <param name="consume">Whether to advance the current offset.</param>
        /// <returns>The read UInt16.</returns>
        protected UInt16 ReadUInt16(bool consume = true)
        {
            byte[] buf = ReadBytes(2, consume);
            return BitConverter.ToUInt16(buf, 0);
        }

        /// <summary>
        /// Reads a signed integer starting from the current offset.
        /// </summary>
        /// <param name="consume">Whether to advance the current offset.</param>
        /// <returns>The read Int32.</returns>
        protected Int32 ReadInt32(bool consume = true)
        {
            byte[] buf = ReadBytes(4, consume);
            return BitConverter.ToInt32(buf, 0);
        }

        /// <summary>
        /// Reads an unsigned integer starting from the current offset.
        /// </summary>
        /// <param name="consume">Whether to advance the current offset.</param>
        /// <returns>The read UInt32.</returns>
        protected UInt32 ReadUInt32(bool consume = true)
        {
            byte[] buf = ReadBytes(4, consume);
            return BitConverter.ToUInt32(buf, 0);
        }

        /// <summary>
        /// Reads a floating-point value starting from the current offset.
        /// </summary>
        /// <param name="consume">Whether to advance the current offset.</param>
        /// <returns>The read Single.</returns>
        protected Single ReadSingle(bool consume = true)
        {
            byte[] buf = ReadBytes(4, consume);
            return BitConverter.ToSingle(buf, 0);
        }

        /// <summary>
        /// Reads a Vec2 object starting from the current offset.
        /// </summary>
        /// <param name="consume">Whether to advance the current offset.</param>
        /// <returns>The read Vec2.</returns>
        protected Vector2 ReadVec2(bool consume = true)
        {
            byte[] buf = ReadBytes(8, consume);
            return new Vector2(
                BitConverter.ToSingle(buf, 0),
                BitConverter.ToSingle(buf, 4));
        }

        /// <summary>
        /// Reads a Vec3 object starting from the current offset.
        /// </summary>
        /// <param name="consume">Whether to advance the current offset.</param>
        /// <returns>The read Vec3.</returns>
        protected Vector3 ReadVec3(bool consume = true)
        {
            byte[] buf = ReadBytes(12, consume);
            return new Vector3(
                BitConverter.ToSingle(buf, 0),
                BitConverter.ToSingle(buf, 4),
                BitConverter.ToSingle(buf, 8));
        }

        /// <summary>
        /// Reads a Vec4 object starting from the current offset.
        /// </summary>
        /// <param name="consume">Whether to advance the current offset.</param>
        /// <returns>The read Vec4.</returns>
        protected Vector4 ReadVec4(bool consume = true)
        {
            byte[] buf = ReadBytes(16, consume);
            return new Vector4(
                BitConverter.ToSingle(buf, 0),
                BitConverter.ToSingle(buf, 4),
                BitConverter.ToSingle(buf, 8),
                BitConverter.ToSingle(buf, 12));
        }

        /// <summary>
        /// Reads a Matrix43 object starting from the current offset.
        /// </summary>
        /// <param name="consume">Whether to advance the current offset.</param>
        /// <returns>The read Matrix43.</returns>
        protected Matrix ReadMatrix43(bool consume = true)
        {
            byte[] buf = ReadBytes(4 * 4 * 3, consume);
            return new Matrix(
                BitConverter.ToSingle(buf, 0),
                BitConverter.ToSingle(buf, 1*4),
                BitConverter.ToSingle(buf, 2*4),
                BitConverter.ToSingle(buf, 3*4),

                BitConverter.ToSingle(buf, 4*4),
                BitConverter.ToSingle(buf, 5*4),
                BitConverter.ToSingle(buf, 6*4),
                BitConverter.ToSingle(buf, 7*4),

                BitConverter.ToSingle(buf, 8*4),
                BitConverter.ToSingle(buf, 9*4),
                BitConverter.ToSingle(buf, 10*4),
                BitConverter.ToSingle(buf, 11*4),

                0, 0, 0, 0);
        }

        /// <summary>
        /// Reads a Matrix33 object starting from the current offset.
        /// </summary>
        /// <param name="consume">Whether to advance the current offset.</param>
        /// <returns>The read Matrix33.</returns>
        protected Matrix ReadMatrix33(bool consume = true)
        {
            byte[] buf = ReadBytes(4 * 3 * 3, consume);
            return new Matrix(
                BitConverter.ToSingle(buf, 0),
                BitConverter.ToSingle(buf, 4),
                BitConverter.ToSingle(buf, 8),
                0,

                BitConverter.ToSingle(buf, 12),
                BitConverter.ToSingle(buf, 16),
                BitConverter.ToSingle(buf, 20),
                0,

                BitConverter.ToSingle(buf, 24),
                BitConverter.ToSingle(buf, 28),
                BitConverter.ToSingle(buf, 32),
                0,

                0, 0, 0, 0
                );
        }

        /// <summary>
        /// Reads a string of a specified length starting from the current offset.
        /// </summary>
        /// <param name="length">Length of string to read.</param>
        /// <param name="consume">Whether to advance the current offset.</param>
        /// <returns>The read String.</returns>
        protected string ReadString(int length, bool consume = true)
        {
            byte[] buf = ReadBytes(length, consume);
            return Encoding.ASCII.GetString(buf);
        }

        /// <summary>
        /// Reads a null-terminated string starting from the current offset.
        /// </summary>
        /// <returns>The read string.</returns>
        protected string ReadStringTerminated(bool consume = true)
        {
            var sb = new StringBuilder();
            // Stop short in case we arrive on a not-string
            for (int i = 0; i < 100; i++)
            {
                byte b = ReadByte(consume);
                if (b == 0x00)
                    break;
                sb.Append((char)b);
            }
            return sb.ToString();
        }

        /// <summary>
        /// Reads a four-character magic value starting from the current offset.
        /// </summary>
        /// <param name="consume">Whether to advance the current offset.</param>
        /// <returns>String containing the four-character magic value.</returns>
        /// <exception cref="InvalidDataException">No magic value found.</exception>
        protected string ReadMagic(bool consume = true)
        {
            RootObject.Coverage.Add(CurrentOffset, CurrentOffset + 3);

            var bytes = new byte[4];
            var chars = new char[4];
            for (int i = 0; i < 4; i++)
            {
                bytes[i] = InputFile[CurrentOffset + i];
                chars[i] = (char)bytes[i];
            }
            var magic = new string(chars);

            if (consume) CurrentOffset += 4;

            if (!magicStrings.Contains(magic))
                throw new InvalidOperationException(
                    "Magic value expected at offset "
                    + DisplayValue(CurrentOffset)
                    + ", found: " + BitConverter.ToString(bytes)
                    + " '" + magic + "'");
            
            return magic;
        }

        /// <summary>
        /// Attempts to read a four-character magic value starting from the current offset.
        /// </summary>
        /// <param name="magic">String containing the four-character magic value.</param>
        /// <param name="consume">Whether to advance the current offset.</param>
        /// <returns>Whether a magic value was found.</returns>
        protected bool TryReadMagic(out string magic, bool consume = true)
        {
            var sb = new StringBuilder();
            for (int i = 0; i < 4; i++)
                sb.Append((char) InputFile[CurrentOffset + i]);
            if (consume) CurrentOffset += 4;
            magic = sb.ToString();

            bool success = magicStrings.Contains(magic);
            if (success)
                RootObject.Coverage.Add(CurrentOffset, CurrentOffset + 3);
            return success;
        }

        /// <summary>
        /// Advances the current offset the specified number of bytes.
        /// </summary>
        /// <param name="bytes">Number of bytes to skip.</param>
        /// <param name="print">Whether to print a notice to console.</param>
        protected void Skip(int bytes, bool print = true)
        {
            if (bytes == 0)
            {
                Log("- Skipped 0 bytes", 4);
            }
            else if (bytes > 0)
            {
                byte[] buf = ReadBytes(bytes, true, false); // implicit consume
                if (print)
                    Log("- Skipped " + DisplayValue(bytes) + " bytes "
                        + PrintBytesGrouped(buf) + (buf.Length > 16 ? " ..." : ""), 3);
            }
            else
            {
                PreviousOffset = CurrentOffset;
                CurrentOffset += bytes;
                Log("- Moved back " + (DisplayValue(-bytes)) + " bytes.", 3);
            }
        }

        private static string PrintBytesGrouped(byte[] buf, int maxBytes = 16, int perGroup = 4)
        {
            string result = String.Empty;
            if (maxBytes == -1)
                maxBytes = buf.Length;

            for (int i = 0; i < buf.Length; i++)
            {
                result += BitConverter.ToString(buf, i, 1).PadLeft(2, '0');
                if (i == maxBytes - 1)
                    break;
                if (i%perGroup == perGroup-1)
                    result += " ";
            }

            return result;
        }

        /// <summary>
        /// Advances the current offset to the given offset.
        /// </summary>
        /// <param name="offset">Offset to advance to.</param>
        /// <param name="print">Whether to print a notice to console.</param>
        protected void SkipTo(int offset, bool print = true)
        {
            Skip(offset - CurrentOffset, print);
        }

        /// <summary>
        /// Advances the current offset to the given offset.
        /// </summary>
        /// <param name="offset">Offset to advance to.</param>
        /// <param name="print">Whether to print a notice to console.</param>
        protected void SkipTo(Offset offset, bool print = true)
        {
            Skip(offset.Absolute - CurrentOffset, print);
        }

        /// <summary>
        /// Reads a CtrObject starting at the current offset.
        /// </summary>
        /// <returns>The read CtrObject.</returns>
        protected CtrObject ReadCtrObject(bool consume = true)
        {
            string magic;
            bool hasMagic = TryReadMagic(CurrentOffset, out magic) ||
                              TryReadMagic(CurrentOffset + 0x4, out magic);

            if (!hasMagic)
                throw new InvalidOperationException(
                    "Magic value expected for object at " + DisplayValue(CurrentOffset));

            CtrObject result = null;
            switch (magic)
            {
                case "CGFX":
                    throw new InvalidOperationException(
                        "Magic 'CGFX' may not occur past the beginning of a file.");
                case "DATA":
                    result = new Data(this, CurrentOffset);
                    break;
                case "DICT":
                    result = new Dict(this, CurrentOffset);
                    break;
                case "CMDL":
                    result = new Cmdl(this, CurrentOffset);
                    break;
                case "TXOB":
                    result = new Txob(this, CurrentOffset);
                    break;
                case "LUTS":
                    result = new Luts(this, CurrentOffset);
                    break;
                case "SOBJ":
                    result = new Sobj(this, CurrentOffset);
                    break;
                case "MTOB":
                    result = new Mtob(this, CurrentOffset);
                    break;
                default:
                    throw new NotImplementedException(
                        "Magic '"+magic+"' not yet implemented.");
            }
            if (consume)
                CurrentOffset = result.CurrentOffset;

            RootObject.Objects.Add(result);

            return result;
        }

        /// <summary>
        /// Reads a self-relative offset value starting from the current offset.
        /// </summary>
        /// <param name="consume">Whether to advance the current offset.</param>
        /// <returns>The read Offset.</returns>
        protected Offset ReadOffset(bool consume = true)
        {
            return new Offset(CurrentOffset, ReadInt32(consume));
        }

        #endregion sequential read operations



        #region nonsequential read operations

        /// <summary>
        /// Reads a number of bytes starting from the specified offset.
        /// </summary>
        /// <param name="offset">Offset to start reading from.</param>
        /// <param name="bytes">Number of bytes to read.</param>
        /// <param name="cover">Whether to record this reading in the coverage data.</param>
        /// <returns>Array containing the read bytes.</returns>
        protected byte[] ReadBytes(int offset, int bytes, bool cover = true)
        {
            if (cover)
                RootObject.Coverage.Add(offset, offset + bytes - 1);

            var buf = new byte[bytes];
            Array.Copy(InputFile, offset, buf, 0, bytes);
            return buf;
        }

        /// <summary>
        /// Reads a byte from the specified offset.
        /// </summary>
        /// <param name="offset">Offset to start reading from.</param>
        /// <param name="cover">Whether to record this reading in the coverage data.</param>
        /// <returns>The read Byte.</returns> 
        protected byte ReadByte(int offset, bool cover = true)
        {
            if (cover)
                RootObject.Coverage.Add(offset);

            return InputFile[offset];
        }

        /// <summary>
        /// Reads a signed short starting from the specified offset.
        /// </summary>
        /// <param name="offset">Offset to start reading from.</param>
        /// <returns>The read Int16.</returns> 
        protected Int16 ReadInt16(int offset)
        {
            byte[] buf = ReadBytes(offset, 2);
            return BitConverter.ToInt16(buf, 0);
        }

        /// <summary> 
        /// Reads an unsigned short starting from the specified offset.
        /// </summary>
        /// <param name="offset">Offset to start reading from.</param>
        /// <returns>The read UInt16.</returns> 
        protected UInt16 ReadUInt16(int offset)
        {
            byte[] buf = ReadBytes(offset, 2);
            return BitConverter.ToUInt16(buf, 0);
        }

        /// <summary>
        /// Reads a signed integer starting from the specified offset.
        /// </summary>
        /// <param name="offset">Offset to start reading from.</param>
        /// <returns>The read Int32.</returns>
        protected Int32 ReadInt32(int offset)
        {
            byte[] buf = ReadBytes(offset, 4);
            return BitConverter.ToInt32(buf, 0);
        }

        /// <summary>
        /// Reads an unsigned integer starting from the specified offset.
        /// </summary>
        /// <param name="offset">Offset to start reading from.</param>
        /// <returns>The read UInt32.</returns>
        protected UInt32 ReadUInt32(int offset)
        {
            byte[] buf = ReadBytes(offset, 4);
            return BitConverter.ToUInt32(buf, 0);
        }

        /// <summary>
        /// Reads a floating-point value starting from the specified offset.
        /// </summary>
        /// <param name="offset">Offset to start reading from.</param>
        /// <returns>The read Single.</returns>
        protected Single ReadSingle(int offset)
        {
            byte[] buf = ReadBytes(offset, 4);
            return BitConverter.ToSingle(buf, 0);
        }

        /// <summary>
        /// Reads a Vec2 object starting from the specified offset.
        /// </summary>
        /// <param name="offset">Offset to start reading from.</param>
        /// <returns>The read Vec2.</returns>
        protected Vector2 ReadVec2(int offset)
        {
            byte[] buf = ReadBytes(offset, 8);
            return new Vector2(
                BitConverter.ToSingle(buf, 0),
                BitConverter.ToSingle(buf, 4));
        }

        /// <summary>
        /// Reads a Vec3 object starting from the specified offset.
        /// </summary>
        /// <param name="offset">Offset to start reading from.</param>
        /// <returns>The read Vec3.</returns>
        protected Vector3 ReadVec3(int offset)
        {
            byte[] buf = ReadBytes(offset, 12);
            return new Vector3(
                BitConverter.ToSingle(buf, 0),
                BitConverter.ToSingle(buf, 4),
                BitConverter.ToSingle(buf, 8));
        }

        /// <summary>
        /// Reads a Vec4 object starting from the specified offset.
        /// </summary>
        /// <param name="offset">Offset to start reading from.</param>
        /// <returns>The read Vec4.</returns>
        protected Vector4 ReadVec4(int offset)
        {
            byte[] buf = ReadBytes(offset, 16);
            return new Vector4(
                BitConverter.ToSingle(buf, 0),
                BitConverter.ToSingle(buf, 4),
                BitConverter.ToSingle(buf, 8),
                BitConverter.ToSingle(buf, 12));
        }

        /// <summary>
        /// Reads a Matrix43 object starting from the specified offset.
        /// </summary>
        /// <param name="offset">Offset to start reading from.</param>
        /// <returns>The read Matrix43.</returns>
        protected Matrix ReadMatrix43(int offset)
        {
            byte[] buf = ReadBytes(offset, 4 * 4 * 3);
            return new Matrix(
                BitConverter.ToSingle(buf, 0),
                BitConverter.ToSingle(buf, 4),
                BitConverter.ToSingle(buf, 8),
                BitConverter.ToSingle(buf, 12),

                BitConverter.ToSingle(buf, 16),
                BitConverter.ToSingle(buf, 20),
                BitConverter.ToSingle(buf, 24),
                BitConverter.ToSingle(buf, 28),

                BitConverter.ToSingle(buf, 32),
                BitConverter.ToSingle(buf, 36),
                BitConverter.ToSingle(buf, 40),
                BitConverter.ToSingle(buf, 44),

                0, 0, 0, 0);
        }

        /// <summary>
        /// Reads a Matrix33 object starting from the specified offset.
        /// </summary>
        /// <param name="offset">Offset to start reading from.</param>
        /// <returns>The read Matrix33.</returns>
        protected Matrix ReadMatrix33(int offset)
        {
            byte[] buf = ReadBytes(offset, 4 * 4 * 3);
            return new Matrix(
                BitConverter.ToSingle(buf, 0),
                BitConverter.ToSingle(buf, 4),
                BitConverter.ToSingle(buf, 8),
                0,

                BitConverter.ToSingle(buf, 12),
                BitConverter.ToSingle(buf, 16),
                BitConverter.ToSingle(buf, 20),
                0,

                BitConverter.ToSingle(buf, 24),
                BitConverter.ToSingle(buf, 28),
                BitConverter.ToSingle(buf, 32),
                0,

                0, 0, 0, 0);
        }

        /// <summary>
        /// Reads a string of a specified length starting from the specified offset.
        /// </summary>
        /// <param name="offset">Offset to start reading from.</param>
        /// <param name="length">Length of string to read.</param>
        /// <returns>The read string.</returns>
        protected string ReadString(int offset, int length)
        {
            byte[] buf = ReadBytes(offset, length);
            return Encoding.ASCII.GetString(buf);
        }

        /// <summary>
        /// Reads a null-terminated string starting from the specified offset.
        /// </summary>
        /// <param name="offset">Offset to start reading from.</param>
        /// <returns>The read string.</returns>
        protected string ReadStringTerminated(int offset)
        {
            var sb = new StringBuilder();
            int i;
            for (i = 0; i < 100; i++)
            {
                byte b = InputFile[offset + i];
                if (b == 0x00) break;
                sb.Append((char) b);
            }
            RootObject.Coverage.Add(offset, offset + i);
            return sb.ToString();
        }

        /// <summary>
        /// Reads a four-character magic value starting from the specified offset.
        /// </summary>
        /// <param name="offset">Offset to start reading from.</param>
        /// <returns>String containing the four-character magic value.</returns>
        /// <exception cref="InvalidDataException">No magic value found.</exception>
        protected string ReadMagic(int offset)
        {
            RootObject.Coverage.Add(offset, offset + 3);

            var bytes = new byte[4];
            var chars = new char[4];
            for (int i = 0; i < 4; i++)
            {
                bytes[i] = InputFile[offset + i];
                chars[i] = (char) bytes[i];
            }
            
            var magic = new string(chars);

            if (!magicStrings.Contains(magic))
                throw new InvalidOperationException(
                    "Magic value expected at offset "
                    + DisplayValue(offset)
                    + ", found: " + BitConverter.ToString(bytes)
                    + " '" + magic + "'");

            return magic;
        }

        /// <summary>
        /// Attempts to read a four-character magic value starting from the specified offset.
        /// </summary>
        /// <param name="offset">Offset to start reading from.</param>
        /// <param name="magic">String containing the four-character magic value.</param>
        /// <returns>Whether a magic value was found.</returns>
        protected bool TryReadMagic(int offset, out string magic)
        {
            var sb = new StringBuilder();
            for (int i = 0; i < 4; i++)
                sb.Append((char)InputFile[offset + i]);
            magic = sb.ToString();
            bool success = magicStrings.Contains(magic);
            if (success)
                RootObject.Coverage.Add(offset, offset + 3);
            return success;
        }

        /// <summary>
        /// Reads a self-relative offset value starting from the specified offset.
        /// </summary>
        /// <param name="offset">Offset to start reading from.</param>
        /// <returns>The read Offset.</returns>
        protected Offset ReadOffset(int offset)
        {
            return new Offset(offset, ReadInt32(offset));
        }

        #endregion
    }

    public class SizedCtrObject : CtrObject
    {
        public int Size { get; protected set; }

        public SizedCtrObject(CtrObject parent, int startOffset)
            : base(parent, startOffset)
        {
        }
    }

    public class FlaggedCtrObject : CtrObject
    {
        public uint Flags { get; protected set; }

        public int Revision { get; protected set; }

        public FlaggedCtrObject(CtrObject parent, int startOffset)
            : base(parent, startOffset)
        {
        }

        protected bool IsFlagSet(int flag)
        {
            return Flags.IsBitSet(flag);
        }
    }

    public class Offset
    {
        public readonly int Position;
        public readonly int Relative;
        public readonly int Absolute;

        public Offset(int position, int relative)
        {
            Position = position;
            Relative = relative;
            Absolute = relative + position;
        }

        public override string ToString()
        {
            return "rel: " + CtrObject.DisplayValue(Relative) +
                   " abs: " + CtrObject.DisplayValue(Absolute);
        }
    }
}
