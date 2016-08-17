using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

namespace Xpressive.Home.Plugins.Lifx.Utils
{
    // (c) Jon Skeet: http://www.yoda.arachsys.com/csharp/miscutil/

    public enum Endianness
    {
        /// <summary>
        /// Little endian - least significant byte first
        /// </summary>
        LittleEndian,
        /// <summary>
        /// Big endian - most significant byte first
        /// </summary>
        BigEndian
    }

    /// <summary>
    /// Equivalent of System.BitConverter, but with either endianness.
    /// </summary>
    public abstract class EndianBitConverter
    {
        #region Endianness of this converter
        /// <summary>
        /// Indicates the byte order ("endianess") in which data is converted using this class.
        /// </summary>
        /// <remarks>
        /// Different computer architectures store data using different byte orders. "Big-endian"
        /// means the most significant byte is on the left end of a word. "Little-endian" means the 
        /// most significant byte is on the right end of a word.
        /// </remarks>
        /// <returns>true if this converter is little-endian, false otherwise.</returns>
        public abstract bool IsLittleEndian();

        /// <summary>
        /// Indicates the byte order ("endianess") in which data is converted using this class.
        /// </summary>
        public abstract Endianness Endianness { get; }
        #endregion

        #region Factory properties
        static readonly LittleEndianBitConverter _little = new LittleEndianBitConverter();
        /// <summary>
        /// Returns a little-endian bit converter instance. The same instance is
        /// always returned.
        /// </summary>
        public static LittleEndianBitConverter Little
        {
            get { return _little; }
        }

        static readonly BigEndianBitConverter _big = new BigEndianBitConverter();
        /// <summary>
        /// Returns a big-endian bit converter instance. The same instance is
        /// always returned.
        /// </summary>
        public static BigEndianBitConverter Big
        {
            get { return _big; }
        }
        #endregion

        #region Double/primitive conversions
        /// <summary>
        /// Converts the specified double-precision floating point number to a 
        /// 64-bit signed integer. Note: the endianness of this converter does not
        /// affect the returned value.
        /// </summary>
        /// <param name="value">The number to convert. </param>
        /// <returns>A 64-bit signed integer whose value is equivalent to value.</returns>
        public long DoubleToInt64Bits(double value)
        {
            return BitConverter.DoubleToInt64Bits(value);
        }

        /// <summary>
        /// Converts the specified 64-bit signed integer to a double-precision 
        /// floating point number. Note: the endianness of this converter does not
        /// affect the returned value.
        /// </summary>
        /// <param name="value">The number to convert. </param>
        /// <returns>A double-precision floating point number whose value is equivalent to value.</returns>
        public double Int64BitsToDouble(long value)
        {
            return BitConverter.Int64BitsToDouble(value);
        }

        /// <summary>
        /// Converts the specified single-precision floating point number to a 
        /// 32-bit signed integer. Note: the endianness of this converter does not
        /// affect the returned value.
        /// </summary>
        /// <param name="value">The number to convert. </param>
        /// <returns>A 32-bit signed integer whose value is equivalent to value.</returns>
        public int SingleToInt32Bits(float value)
        {
            return new Int32SingleUnion(value).AsInt32;
        }

        /// <summary>
        /// Converts the specified 32-bit signed integer to a single-precision floating point 
        /// number. Note: the endianness of this converter does not
        /// affect the returned value.
        /// </summary>
        /// <param name="value">The number to convert. </param>
        /// <returns>A single-precision floating point number whose value is equivalent to value.</returns>
        public float Int32BitsToSingle(int value)
        {
            return new Int32SingleUnion(value).AsSingle;
        }
        #endregion

        #region To(PrimitiveType) conversions
        /// <summary>
        /// Returns a Boolean value converted from one byte at a specified position in a byte array.
        /// </summary>
        /// <param name="value">An array of bytes.</param>
        /// <param name="startIndex">The starting position within value.</param>
        /// <returns>true if the byte at startIndex in value is nonzero; otherwise, false.</returns>
        public bool ToBoolean(byte[] value, int startIndex)
        {
            CheckByteArgument(value, startIndex, 1);
            return BitConverter.ToBoolean(value, startIndex);
        }

        /// <summary>
        /// Returns a Unicode character converted from two bytes at a specified position in a byte array.
        /// </summary>
        /// <param name="value">An array of bytes.</param>
        /// <param name="startIndex">The starting position within value.</param>
        /// <returns>A character formed by two bytes beginning at startIndex.</returns>
        public char ToChar(byte[] value, int startIndex)
        {
            return unchecked((char)(CheckedFromBytes(value, startIndex, 2)));
        }

        /// <summary>
        /// Returns a double-precision floating point number converted from eight bytes 
        /// at a specified position in a byte array.
        /// </summary>
        /// <param name="value">An array of bytes.</param>
        /// <param name="startIndex">The starting position within value.</param>
        /// <returns>A double precision floating point number formed by eight bytes beginning at startIndex.</returns>
        public double ToDouble(byte[] value, int startIndex)
        {
            return Int64BitsToDouble(ToInt64(value, startIndex));
        }

        /// <summary>
        /// Returns a single-precision floating point number converted from four bytes 
        /// at a specified position in a byte array.
        /// </summary>
        /// <param name="value">An array of bytes.</param>
        /// <param name="startIndex">The starting position within value.</param>
        /// <returns>A single precision floating point number formed by four bytes beginning at startIndex.</returns>
        public float ToSingle(byte[] value, int startIndex)
        {
            return Int32BitsToSingle(ToInt32(value, startIndex));
        }

        /// <summary>
        /// Returns a 16-bit signed integer converted from two bytes at a specified position in a byte array.
        /// </summary>
        /// <param name="value">An array of bytes.</param>
        /// <param name="startIndex">The starting position within value.</param>
        /// <returns>A 16-bit signed integer formed by two bytes beginning at startIndex.</returns>
        public short ToInt16(byte[] value, int startIndex)
        {
            return unchecked((short)(CheckedFromBytes(value, startIndex, 2)));
        }

        /// <summary>
        /// Returns a 32-bit signed integer converted from four bytes at a specified position in a byte array.
        /// </summary>
        /// <param name="value">An array of bytes.</param>
        /// <param name="startIndex">The starting position within value.</param>
        /// <returns>A 32-bit signed integer formed by four bytes beginning at startIndex.</returns>
        public int ToInt32(byte[] value, int startIndex)
        {
            return unchecked((int)(CheckedFromBytes(value, startIndex, 4)));
        }

        /// <summary>
        /// Returns a 64-bit signed integer converted from eight bytes at a specified position in a byte array.
        /// </summary>
        /// <param name="value">An array of bytes.</param>
        /// <param name="startIndex">The starting position within value.</param>
        /// <returns>A 64-bit signed integer formed by eight bytes beginning at startIndex.</returns>
        public long ToInt64(byte[] value, int startIndex)
        {
            return CheckedFromBytes(value, startIndex, 8);
        }

        /// <summary>
        /// Returns a 16-bit unsigned integer converted from two bytes at a specified position in a byte array.
        /// </summary>
        /// <param name="value">An array of bytes.</param>
        /// <param name="startIndex">The starting position within value.</param>
        /// <returns>A 16-bit unsigned integer formed by two bytes beginning at startIndex.</returns>
        public ushort ToUInt16(byte[] value, int startIndex)
        {
            return unchecked((ushort)(CheckedFromBytes(value, startIndex, 2)));
        }

        /// <summary>
        /// Returns a 32-bit unsigned integer converted from four bytes at a specified position in a byte array.
        /// </summary>
        /// <param name="value">An array of bytes.</param>
        /// <param name="startIndex">The starting position within value.</param>
        /// <returns>A 32-bit unsigned integer formed by four bytes beginning at startIndex.</returns>
        public uint ToUInt32(byte[] value, int startIndex)
        {
            return unchecked((uint)(CheckedFromBytes(value, startIndex, 4)));
        }

        /// <summary>
        /// Returns a 64-bit unsigned integer converted from eight bytes at a specified position in a byte array.
        /// </summary>
        /// <param name="value">An array of bytes.</param>
        /// <param name="startIndex">The starting position within value.</param>
        /// <returns>A 64-bit unsigned integer formed by eight bytes beginning at startIndex.</returns>
        public ulong ToUInt64(byte[] value, int startIndex)
        {
            return unchecked((ulong)(CheckedFromBytes(value, startIndex, 8)));
        }

        /// <summary>
        /// Checks the given argument for validity.
        /// </summary>
        /// <param name="value">The byte array passed in</param>
        /// <param name="startIndex">The start index passed in</param>
        /// <param name="bytesRequired">The number of bytes required</param>
        /// <exception cref="ArgumentNullException">value is a null reference</exception>
        /// <exception cref="ArgumentOutOfRangeException">
        /// startIndex is less than zero or greater than the length of value minus bytesRequired.
        /// </exception>
        static void CheckByteArgument(byte[] value, int startIndex, int bytesRequired)
        {
            if (value == null)
            {
                throw new ArgumentNullException("value");
            }
            if (startIndex < 0 || startIndex > value.Length - bytesRequired)
            {
                throw new ArgumentOutOfRangeException("startIndex");
            }
        }

        /// <summary>
        /// Checks the arguments for validity before calling FromBytes
        /// (which can therefore assume the arguments are valid).
        /// </summary>
        /// <param name="value">The bytes to convert after checking</param>
        /// <param name="startIndex">The index of the first byte to convert</param>
        /// <param name="bytesToConvert">The number of bytes to convert</param>
        /// <returns></returns>
        long CheckedFromBytes(byte[] value, int startIndex, int bytesToConvert)
        {
            CheckByteArgument(value, startIndex, bytesToConvert);
            return FromBytes(value, startIndex, bytesToConvert);
        }

        /// <summary>
        /// Convert the given number of bytes from the given array, from the given start
        /// position, into a long, using the bytes as the least significant part of the long.
        /// By the time this is called, the arguments have been checked for validity.
        /// </summary>
        /// <param name="value">The bytes to convert</param>
        /// <param name="startIndex">The index of the first byte to convert</param>
        /// <param name="bytesToConvert">The number of bytes to use in the conversion</param>
        /// <returns>The converted number</returns>
        protected abstract long FromBytes(byte[] value, int startIndex, int bytesToConvert);
        #endregion

        #region ToString conversions
        /// <summary>
        /// Returns a String converted from the elements of a byte array.
        /// </summary>
        /// <param name="value">An array of bytes.</param>
        /// <remarks>All the elements of value are converted.</remarks>
        /// <returns>
        /// A String of hexadecimal pairs separated by hyphens, where each pair 
        /// represents the corresponding element in value; for example, "7F-2C-4A".
        /// </returns>
        public static string ToString(byte[] value)
        {
            return BitConverter.ToString(value);
        }

        /// <summary>
        /// Returns a String converted from the elements of a byte array starting at a specified array position.
        /// </summary>
        /// <param name="value">An array of bytes.</param>
        /// <param name="startIndex">The starting position within value.</param>
        /// <remarks>The elements from array position startIndex to the end of the array are converted.</remarks>
        /// <returns>
        /// A String of hexadecimal pairs separated by hyphens, where each pair 
        /// represents the corresponding element in value; for example, "7F-2C-4A".
        /// </returns>
        public static string ToString(byte[] value, int startIndex)
        {
            return BitConverter.ToString(value, startIndex);
        }

        /// <summary>
        /// Returns a String converted from a specified number of bytes at a specified position in a byte array.
        /// </summary>
        /// <param name="value">An array of bytes.</param>
        /// <param name="startIndex">The starting position within value.</param>
        /// <param name="length">The number of bytes to convert.</param>
        /// <remarks>The length elements from array position startIndex are converted.</remarks>
        /// <returns>
        /// A String of hexadecimal pairs separated by hyphens, where each pair 
        /// represents the corresponding element in value; for example, "7F-2C-4A".
        /// </returns>
        public static string ToString(byte[] value, int startIndex, int length)
        {
            return BitConverter.ToString(value, startIndex, length);
        }
        #endregion

        #region	Decimal conversions
        /// <summary>
        /// Returns a decimal value converted from sixteen bytes 
        /// at a specified position in a byte array.
        /// </summary>
        /// <param name="value">An array of bytes.</param>
        /// <param name="startIndex">The starting position within value.</param>
        /// <returns>A decimal  formed by sixteen bytes beginning at startIndex.</returns>
        public decimal ToDecimal(byte[] value, int startIndex)
        {
            // HACK: This always assumes four parts, each in their own endianness,
            // starting with the first part at the start of the byte array.
            // On the other hand, there's no real format specified...
            int[] parts = new int[4];
            for (int i = 0; i < 4; i++)
            {
                parts[i] = ToInt32(value, startIndex + i * 4);
            }
            return new Decimal(parts);
        }

        /// <summary>
        /// Returns the specified decimal value as an array of bytes.
        /// </summary>
        /// <param name="value">The number to convert.</param>
        /// <returns>An array of bytes with length 16.</returns>
        public byte[] GetBytes(decimal value)
        {
            byte[] bytes = new byte[16];
            int[] parts = decimal.GetBits(value);
            for (int i = 0; i < 4; i++)
            {
                CopyBytesImpl(parts[i], 4, bytes, i * 4);
            }
            return bytes;
        }

        /// <summary>
        /// Copies the specified decimal value into the specified byte array,
        /// beginning at the specified index.
        /// </summary>
        /// <param name="value">A character to convert.</param>
        /// <param name="buffer">The byte array to copy the bytes into</param>
        /// <param name="index">The first index into the array to copy the bytes into</param>
        public void CopyBytes(decimal value, byte[] buffer, int index)
        {
            int[] parts = decimal.GetBits(value);
            for (int i = 0; i < 4; i++)
            {
                CopyBytesImpl(parts[i], 4, buffer, i * 4 + index);
            }
        }
        #endregion

        #region GetBytes conversions
        /// <summary>
        /// Returns an array with the given number of bytes formed
        /// from the least significant bytes of the specified value.
        /// This is used to implement the other GetBytes methods.
        /// </summary>
        /// <param name="value">The value to get bytes for</param>
        /// <param name="bytes">The number of significant bytes to return</param>
        byte[] GetBytes(long value, int bytes)
        {
            byte[] buffer = new byte[bytes];
            CopyBytes(value, bytes, buffer, 0);
            return buffer;
        }

        /// <summary>
        /// Returns the specified Boolean value as an array of bytes.
        /// </summary>
        /// <param name="value">A Boolean value.</param>
        /// <returns>An array of bytes with length 1.</returns>
        public byte[] GetBytes(bool value)
        {
            return BitConverter.GetBytes(value);
        }

        /// <summary>
        /// Returns the specified Unicode character value as an array of bytes.
        /// </summary>
        /// <param name="value">A character to convert.</param>
        /// <returns>An array of bytes with length 2.</returns>
        public byte[] GetBytes(char value)
        {
            return GetBytes(value, 2);
        }

        /// <summary>
        /// Returns the specified double-precision floating point value as an array of bytes.
        /// </summary>
        /// <param name="value">The number to convert.</param>
        /// <returns>An array of bytes with length 8.</returns>
        public byte[] GetBytes(double value)
        {
            return GetBytes(DoubleToInt64Bits(value), 8);
        }

        /// <summary>
        /// Returns the specified 16-bit signed integer value as an array of bytes.
        /// </summary>
        /// <param name="value">The number to convert.</param>
        /// <returns>An array of bytes with length 2.</returns>
        public byte[] GetBytes(short value)
        {
            return GetBytes(value, 2);
        }

        /// <summary>
        /// Returns the specified 32-bit signed integer value as an array of bytes.
        /// </summary>
        /// <param name="value">The number to convert.</param>
        /// <returns>An array of bytes with length 4.</returns>
        public byte[] GetBytes(int value)
        {
            return GetBytes(value, 4);
        }

        /// <summary>
        /// Returns the specified 64-bit signed integer value as an array of bytes.
        /// </summary>
        /// <param name="value">The number to convert.</param>
        /// <returns>An array of bytes with length 8.</returns>
        public byte[] GetBytes(long value)
        {
            return GetBytes(value, 8);
        }

        /// <summary>
        /// Returns the specified single-precision floating point value as an array of bytes.
        /// </summary>
        /// <param name="value">The number to convert.</param>
        /// <returns>An array of bytes with length 4.</returns>
        public byte[] GetBytes(float value)
        {
            return GetBytes(SingleToInt32Bits(value), 4);
        }

        /// <summary>
        /// Returns the specified 16-bit unsigned integer value as an array of bytes.
        /// </summary>
        /// <param name="value">The number to convert.</param>
        /// <returns>An array of bytes with length 2.</returns>
        public byte[] GetBytes(ushort value)
        {
            return GetBytes(value, 2);
        }

        /// <summary>
        /// Returns the specified 32-bit unsigned integer value as an array of bytes.
        /// </summary>
        /// <param name="value">The number to convert.</param>
        /// <returns>An array of bytes with length 4.</returns>
        public byte[] GetBytes(uint value)
        {
            return GetBytes(value, 4);
        }

        /// <summary>
        /// Returns the specified 64-bit unsigned integer value as an array of bytes.
        /// </summary>
        /// <param name="value">The number to convert.</param>
        /// <returns>An array of bytes with length 8.</returns>
        public byte[] GetBytes(ulong value)
        {
            return GetBytes(unchecked((long)value), 8);
        }

        #endregion

        #region CopyBytes conversions
        /// <summary>
        /// Copies the given number of bytes from the least-specific
        /// end of the specified value into the specified byte array, beginning
        /// at the specified index.
        /// This is used to implement the other CopyBytes methods.
        /// </summary>
        /// <param name="value">The value to copy bytes for</param>
        /// <param name="bytes">The number of significant bytes to copy</param>
        /// <param name="buffer">The byte array to copy the bytes into</param>
        /// <param name="index">The first index into the array to copy the bytes into</param>
        void CopyBytes(long value, int bytes, byte[] buffer, int index)
        {
            if (buffer == null)
            {
                throw new ArgumentNullException("buffer", "Byte array must not be null");
            }
            if (buffer.Length < index + bytes)
            {
                throw new ArgumentOutOfRangeException("Buffer not big enough for value");
            }
            CopyBytesImpl(value, bytes, buffer, index);
        }

        /// <summary>
        /// Copies the given number of bytes from the least-specific
        /// end of the specified value into the specified byte array, beginning
        /// at the specified index.
        /// This must be implemented in concrete derived classes, but the implementation
        /// may assume that the value will fit into the buffer.
        /// </summary>
        /// <param name="value">The value to copy bytes for</param>
        /// <param name="bytes">The number of significant bytes to copy</param>
        /// <param name="buffer">The byte array to copy the bytes into</param>
        /// <param name="index">The first index into the array to copy the bytes into</param>
        protected abstract void CopyBytesImpl(long value, int bytes, byte[] buffer, int index);

        /// <summary>
        /// Copies the specified Boolean value into the specified byte array,
        /// beginning at the specified index.
        /// </summary>
        /// <param name="value">A Boolean value.</param>
        /// <param name="buffer">The byte array to copy the bytes into</param>
        /// <param name="index">The first index into the array to copy the bytes into</param>
        public void CopyBytes(bool value, byte[] buffer, int index)
        {
            CopyBytes(value ? 1 : 0, 1, buffer, index);
        }

        /// <summary>
        /// Copies the specified Unicode character value into the specified byte array,
        /// beginning at the specified index.
        /// </summary>
        /// <param name="value">A character to convert.</param>
        /// <param name="buffer">The byte array to copy the bytes into</param>
        /// <param name="index">The first index into the array to copy the bytes into</param>
        public void CopyBytes(char value, byte[] buffer, int index)
        {
            CopyBytes(value, 2, buffer, index);
        }

        /// <summary>
        /// Copies the specified double-precision floating point value into the specified byte array,
        /// beginning at the specified index.
        /// </summary>
        /// <param name="value">The number to convert.</param>
        /// <param name="buffer">The byte array to copy the bytes into</param>
        /// <param name="index">The first index into the array to copy the bytes into</param>
        public void CopyBytes(double value, byte[] buffer, int index)
        {
            CopyBytes(DoubleToInt64Bits(value), 8, buffer, index);
        }

        /// <summary>
        /// Copies the specified 16-bit signed integer value into the specified byte array,
        /// beginning at the specified index.
        /// </summary>
        /// <param name="value">The number to convert.</param>
        /// <param name="buffer">The byte array to copy the bytes into</param>
        /// <param name="index">The first index into the array to copy the bytes into</param>
        public void CopyBytes(short value, byte[] buffer, int index)
        {
            CopyBytes(value, 2, buffer, index);
        }

        /// <summary>
        /// Copies the specified 32-bit signed integer value into the specified byte array,
        /// beginning at the specified index.
        /// </summary>
        /// <param name="value">The number to convert.</param>
        /// <param name="buffer">The byte array to copy the bytes into</param>
        /// <param name="index">The first index into the array to copy the bytes into</param>
        public void CopyBytes(int value, byte[] buffer, int index)
        {
            CopyBytes(value, 4, buffer, index);
        }

        /// <summary>
        /// Copies the specified 64-bit signed integer value into the specified byte array,
        /// beginning at the specified index.
        /// </summary>
        /// <param name="value">The number to convert.</param>
        /// <param name="buffer">The byte array to copy the bytes into</param>
        /// <param name="index">The first index into the array to copy the bytes into</param>
        public void CopyBytes(long value, byte[] buffer, int index)
        {
            CopyBytes(value, 8, buffer, index);
        }

        /// <summary>
        /// Copies the specified single-precision floating point value into the specified byte array,
        /// beginning at the specified index.
        /// </summary>
        /// <param name="value">The number to convert.</param>
        /// <param name="buffer">The byte array to copy the bytes into</param>
        /// <param name="index">The first index into the array to copy the bytes into</param>
        public void CopyBytes(float value, byte[] buffer, int index)
        {
            CopyBytes(SingleToInt32Bits(value), 4, buffer, index);
        }

        /// <summary>
        /// Copies the specified 16-bit unsigned integer value into the specified byte array,
        /// beginning at the specified index.
        /// </summary>
        /// <param name="value">The number to convert.</param>
        /// <param name="buffer">The byte array to copy the bytes into</param>
        /// <param name="index">The first index into the array to copy the bytes into</param>
        public void CopyBytes(ushort value, byte[] buffer, int index)
        {
            CopyBytes(value, 2, buffer, index);
        }

        /// <summary>
        /// Copies the specified 32-bit unsigned integer value into the specified byte array,
        /// beginning at the specified index.
        /// </summary>
        /// <param name="value">The number to convert.</param>
        /// <param name="buffer">The byte array to copy the bytes into</param>
        /// <param name="index">The first index into the array to copy the bytes into</param>
        public void CopyBytes(uint value, byte[] buffer, int index)
        {
            CopyBytes(value, 4, buffer, index);
        }

        /// <summary>
        /// Copies the specified 64-bit unsigned integer value into the specified byte array,
        /// beginning at the specified index.
        /// </summary>
        /// <param name="value">The number to convert.</param>
        /// <param name="buffer">The byte array to copy the bytes into</param>
        /// <param name="index">The first index into the array to copy the bytes into</param>
        public void CopyBytes(ulong value, byte[] buffer, int index)
        {
            CopyBytes(unchecked((long)value), 8, buffer, index);
        }

        #endregion

        #region Private struct used for Single/Int32 conversions
        /// <summary>
        /// Union used solely for the equivalent of DoubleToInt64Bits and vice versa.
        /// </summary>
        [StructLayout(LayoutKind.Explicit)]
        struct Int32SingleUnion
        {
            /// <summary>
            /// Int32 version of the value.
            /// </summary>
            [FieldOffset(0)]
            int i;
            /// <summary>
            /// Single version of the value.
            /// </summary>
            [FieldOffset(0)]
            float f;

            /// <summary>
            /// Creates an instance representing the given integer.
            /// </summary>
            /// <param name="i">The integer value of the new instance.</param>
            internal Int32SingleUnion(int i)
            {
                this.f = 0; // Just to keep the compiler happy
                this.i = i;
            }

            /// <summary>
            /// Creates an instance representing the given floating point number.
            /// </summary>
            /// <param name="f">The floating point value of the new instance.</param>
            internal Int32SingleUnion(float f)
            {
                this.i = 0; // Just to keep the compiler happy
                this.f = f;
            }

            /// <summary>
            /// Returns the value of the instance as an integer.
            /// </summary>
            internal int AsInt32
            {
                get { return i; }
            }

            /// <summary>
            /// Returns the value of the instance as a floating point number.
            /// </summary>
            internal float AsSingle
            {
                get { return f; }
            }
        }
        #endregion
    }

    /// <summary>
    /// Equivalent of System.IO.BinaryReader, but with either endianness, depending on
    /// the EndianBitConverter it is constructed with. No data is buffered in the
    /// reader; the client may seek within the stream at will.
    /// </summary>
    public class EndianBinaryReader : IDisposable
    {
        #region Fields not directly related to properties
        /// <summary>
        /// Whether or not this reader has been disposed yet.
        /// </summary>
        bool disposed = false;
        /// <summary>
        /// Decoder to use for string conversions.
        /// </summary>
        Decoder decoder;
        /// <summary>
        /// Buffer used for temporary storage before conversion into primitives
        /// </summary>
        byte[] buffer = new byte[16];
        /// <summary>
        /// Buffer used for temporary storage when reading a single character
        /// </summary>
        char[] charBuffer = new char[1];
        /// <summary>
        /// Minimum number of bytes used to encode a character
        /// </summary>
        int minBytesPerChar;
        #endregion

        #region Constructors
        /// <summary>
        /// Equivalent of System.IO.BinaryWriter, but with either endianness, depending on
        /// the EndianBitConverter it is constructed with.
        /// </summary>
        /// <param name="bitConverter">Converter to use when reading data</param>
        /// <param name="stream">Stream to read data from</param>
        public EndianBinaryReader(EndianBitConverter bitConverter,
                                   Stream stream) : this(bitConverter, stream, Encoding.UTF8)
        {
        }

        /// <summary>
        /// Constructs a new binary reader with the given bit converter, reading
        /// to the given stream, using the given encoding.
        /// </summary>
        /// <param name="bitConverter">Converter to use when reading data</param>
        /// <param name="stream">Stream to read data from</param>
        /// <param name="encoding">Encoding to use when reading character data</param>
        public EndianBinaryReader(EndianBitConverter bitConverter, Stream stream, Encoding encoding)
        {
            if (bitConverter == null)
            {
                throw new ArgumentNullException("bitConverter");
            }
            if (stream == null)
            {
                throw new ArgumentNullException("stream");
            }
            if (encoding == null)
            {
                throw new ArgumentNullException("encoding");
            }
            if (!stream.CanRead)
            {
                throw new ArgumentException("Stream isn't writable", "stream");
            }
            this.stream = stream;
            this.bitConverter = bitConverter;
            this.encoding = encoding;
            this.decoder = encoding.GetDecoder();
            this.minBytesPerChar = 1;

            if (encoding is UnicodeEncoding)
            {
                minBytesPerChar = 2;
            }
        }
        #endregion

        #region Properties
        EndianBitConverter bitConverter;
        /// <summary>
        /// The bit converter used to read values from the stream
        /// </summary>
        public EndianBitConverter BitConverter
        {
            get { return bitConverter; }
        }

        Encoding encoding;
        /// <summary>
        /// The encoding used to read strings
        /// </summary>
        public Encoding Encoding
        {
            get { return encoding; }
        }

        Stream stream;
        /// <summary>
        /// Gets the underlying stream of the EndianBinaryReader.
        /// </summary>
        public Stream BaseStream
        {
            get { return stream; }
        }
        #endregion

        #region Public methods
        /// <summary>
        /// Closes the reader, including the underlying stream..
        /// </summary>
        public void Close()
        {
            Dispose();
        }

        /// <summary>
        /// Seeks within the stream.
        /// </summary>
        /// <param name="offset">Offset to seek to.</param>
        /// <param name="origin">Origin of seek operation.</param>
        public void Seek(int offset, SeekOrigin origin)
        {
            CheckDisposed();
            stream.Seek(offset, origin);
        }

        /// <summary>
        /// Reads a single byte from the stream.
        /// </summary>
        /// <returns>The byte read</returns>
        public byte ReadByte()
        {
            ReadInternal(buffer, 1);
            return buffer[0];
        }

        /// <summary>
        /// Reads a single signed byte from the stream.
        /// </summary>
        /// <returns>The byte read</returns>
        public sbyte ReadSByte()
        {
            ReadInternal(buffer, 1);
            return unchecked((sbyte)buffer[0]);
        }

        /// <summary>
        /// Reads a boolean from the stream. 1 byte is read.
        /// </summary>
        /// <returns>The boolean read</returns>
        public bool ReadBoolean()
        {
            ReadInternal(buffer, 1);
            return bitConverter.ToBoolean(buffer, 0);
        }

        /// <summary>
        /// Reads a 16-bit signed integer from the stream, using the bit converter
        /// for this reader. 2 bytes are read.
        /// </summary>
        /// <returns>The 16-bit integer read</returns>
        public short ReadInt16()
        {
            ReadInternal(buffer, 2);
            return bitConverter.ToInt16(buffer, 0);
        }

        /// <summary>
        /// Reads a 32-bit signed integer from the stream, using the bit converter
        /// for this reader. 4 bytes are read.
        /// </summary>
        /// <returns>The 32-bit integer read</returns>
        public int ReadInt32()
        {
            ReadInternal(buffer, 4);
            return bitConverter.ToInt32(buffer, 0);
        }

        /// <summary>
        /// Reads a 64-bit signed integer from the stream, using the bit converter
        /// for this reader. 8 bytes are read.
        /// </summary>
        /// <returns>The 64-bit integer read</returns>
        public long ReadInt64()
        {
            ReadInternal(buffer, 8);
            return bitConverter.ToInt64(buffer, 0);
        }

        /// <summary>
        /// Reads a 16-bit unsigned integer from the stream, using the bit converter
        /// for this reader. 2 bytes are read.
        /// </summary>
        /// <returns>The 16-bit unsigned integer read</returns>
        public ushort ReadUInt16()
        {
            ReadInternal(buffer, 2);
            return bitConverter.ToUInt16(buffer, 0);
        }

        /// <summary>
        /// Reads a 32-bit unsigned integer from the stream, using the bit converter
        /// for this reader. 4 bytes are read.
        /// </summary>
        /// <returns>The 32-bit unsigned integer read</returns>
        public uint ReadUInt32()
        {
            ReadInternal(buffer, 4);
            return bitConverter.ToUInt32(buffer, 0);
        }

        /// <summary>
        /// Reads a 64-bit unsigned integer from the stream, using the bit converter
        /// for this reader. 8 bytes are read.
        /// </summary>
        /// <returns>The 64-bit unsigned integer read</returns>
        public ulong ReadUInt64()
        {
            ReadInternal(buffer, 8);
            return bitConverter.ToUInt64(buffer, 0);
        }

        /// <summary>
        /// Reads a single-precision floating-point value from the stream, using the bit converter
        /// for this reader. 4 bytes are read.
        /// </summary>
        /// <returns>The floating point value read</returns>
        public float ReadSingle()
        {
            ReadInternal(buffer, 4);
            return bitConverter.ToSingle(buffer, 0);
        }

        /// <summary>
        /// Reads a double-precision floating-point value from the stream, using the bit converter
        /// for this reader. 8 bytes are read.
        /// </summary>
        /// <returns>The floating point value read</returns>
        public double ReadDouble()
        {
            ReadInternal(buffer, 8);
            return bitConverter.ToDouble(buffer, 0);
        }

        /// <summary>
        /// Reads a decimal value from the stream, using the bit converter
        /// for this reader. 16 bytes are read.
        /// </summary>
        /// <returns>The decimal value read</returns>
        public decimal ReadDecimal()
        {
            ReadInternal(buffer, 16);
            return bitConverter.ToDecimal(buffer, 0);
        }

        /// <summary>
        /// Reads a single character from the stream, using the character encoding for
        /// this reader. If no characters have been fully read by the time the stream ends,
        /// -1 is returned.
        /// </summary>
        /// <returns>The character read, or -1 for end of stream.</returns>
        public int Read()
        {
            int charsRead = Read(charBuffer, 0, 1);
            if (charsRead == 0)
            {
                return -1;
            }
            else
            {
                return charBuffer[0];
            }
        }

        /// <summary>
        /// Reads the specified number of characters into the given buffer, starting at
        /// the given index.
        /// </summary>
        /// <param name="data">The buffer to copy data into</param>
        /// <param name="index">The first index to copy data into</param>
        /// <param name="count">The number of characters to read</param>
        /// <returns>The number of characters actually read. This will only be less than
        /// the requested number of characters if the end of the stream is reached.
        /// </returns>
        public int Read(char[] data, int index, int count)
        {
            CheckDisposed();
            if (buffer == null)
            {
                throw new ArgumentNullException("buffer");
            }
            if (index < 0)
            {
                throw new ArgumentOutOfRangeException("index");
            }
            if (count < 0)
            {
                throw new ArgumentOutOfRangeException("index");
            }
            if (count + index > data.Length)
            {
                throw new ArgumentException
                    ("Not enough space in buffer for specified number of characters starting at specified index");
            }

            int read = 0;
            bool firstTime = true;

            // Use the normal buffer if we're only reading a small amount, otherwise
            // use at most 4K at a time.
            byte[] byteBuffer = buffer;

            if (byteBuffer.Length < count * minBytesPerChar)
            {
                byteBuffer = new byte[4096];
            }

            while (read < count)
            {
                int amountToRead;
                // First time through we know we haven't previously read any data
                if (firstTime)
                {
                    amountToRead = count * minBytesPerChar;
                    firstTime = false;
                }
                // After that we can only assume we need to fully read "chars left -1" characters
                // and a single byte of the character we may be in the middle of
                else
                {
                    amountToRead = ((count - read - 1) * minBytesPerChar) + 1;
                }
                if (amountToRead > byteBuffer.Length)
                {
                    amountToRead = byteBuffer.Length;
                }
                int bytesRead = TryReadInternal(byteBuffer, amountToRead);
                if (bytesRead == 0)
                {
                    return read;
                }
                int decoded = decoder.GetChars(byteBuffer, 0, bytesRead, data, index);
                read += decoded;
                index += decoded;
            }
            return read;
        }

        /// <summary>
        /// Reads the specified number of bytes into the given buffer, starting at
        /// the given index.
        /// </summary>
        /// <param name="buffer">The buffer to copy data into</param>
        /// <param name="index">The first index to copy data into</param>
        /// <param name="count">The number of bytes to read</param>
        /// <returns>The number of bytes actually read. This will only be less than
        /// the requested number of bytes if the end of the stream is reached.
        /// </returns>
        public int Read(byte[] buffer, int index, int count)
        {
            CheckDisposed();
            if (buffer == null)
            {
                throw new ArgumentNullException("buffer");
            }
            if (index < 0)
            {
                throw new ArgumentOutOfRangeException("index");
            }
            if (count < 0)
            {
                throw new ArgumentOutOfRangeException("index");
            }
            if (count + index > buffer.Length)
            {
                throw new ArgumentException
                    ("Not enough space in buffer for specified number of bytes starting at specified index");
            }
            int read = 0;
            while (count > 0)
            {
                int block = stream.Read(buffer, index, count);
                if (block == 0)
                {
                    return read;
                }
                index += block;
                read += block;
                count -= block;
            }
            return read;
        }

        /// <summary>
        /// Reads the specified number of bytes, returning them in a new byte array.
        /// If not enough bytes are available before the end of the stream, this
        /// method will return what is available.
        /// </summary>
        /// <param name="count">The number of bytes to read</param>
        /// <returns>The bytes read</returns>
        public byte[] ReadBytes(int count)
        {
            CheckDisposed();
            if (count < 0)
            {
                throw new ArgumentOutOfRangeException("count");
            }
            byte[] ret = new byte[count];
            int index = 0;
            while (index < count)
            {
                int read = stream.Read(ret, index, count - index);
                // Stream has finished half way through. That's fine, return what we've got.
                if (read == 0)
                {
                    byte[] copy = new byte[index];
                    Buffer.BlockCopy(ret, 0, copy, 0, index);
                    return copy;
                }
                index += read;
            }
            return ret;
        }

        /// <summary>
        /// Reads the specified number of bytes, returning them in a new byte array.
        /// If not enough bytes are available before the end of the stream, this
        /// method will throw an IOException.
        /// </summary>
        /// <param name="count">The number of bytes to read</param>
        /// <returns>The bytes read</returns>
        public byte[] ReadBytesOrThrow(int count)
        {
            byte[] ret = new byte[count];
            ReadInternal(ret, count);
            return ret;
        }

        /// <summary>
        /// Reads a 7-bit encoded integer from the stream. This is stored with the least significant
        /// information first, with 7 bits of information per byte of value, and the top
        /// bit as a continuation flag. This method is not affected by the endianness
        /// of the bit converter.
        /// </summary>
        /// <returns>The 7-bit encoded integer read from the stream.</returns>
        public int Read7BitEncodedInt()
        {
            CheckDisposed();

            int ret = 0;
            for (int shift = 0; shift < 35; shift += 7)
            {
                int b = stream.ReadByte();
                if (b == -1)
                {
                    throw new EndOfStreamException();
                }
                ret = ret | ((b & 0x7f) << shift);
                if ((b & 0x80) == 0)
                {
                    return ret;
                }
            }
            // Still haven't seen a byte with the high bit unset? Dodgy data.
            throw new IOException("Invalid 7-bit encoded integer in stream.");
        }

        /// <summary>
        /// Reads a 7-bit encoded integer from the stream. This is stored with the most significant
        /// information first, with 7 bits of information per byte of value, and the top
        /// bit as a continuation flag. This method is not affected by the endianness
        /// of the bit converter.
        /// </summary>
        /// <returns>The 7-bit encoded integer read from the stream.</returns>
        public int ReadBigEndian7BitEncodedInt()
        {
            CheckDisposed();

            int ret = 0;
            for (int i = 0; i < 5; i++)
            {
                int b = stream.ReadByte();
                if (b == -1)
                {
                    throw new EndOfStreamException();
                }
                ret = (ret << 7) | (b & 0x7f);
                if ((b & 0x80) == 0)
                {
                    return ret;
                }
            }
            // Still haven't seen a byte with the high bit unset? Dodgy data.
            throw new IOException("Invalid 7-bit encoded integer in stream.");
        }

        /// <summary>
        /// Reads a length-prefixed string from the stream, using the encoding for this reader.
        /// A 7-bit encoded integer is first read, which specifies the number of bytes 
        /// to read from the stream. These bytes are then converted into a string with
        /// the encoding for this reader.
        /// </summary>
        /// <returns>The string read from the stream.</returns>
        public string ReadString()
        {
            int bytesToRead = Read7BitEncodedInt();

            byte[] data = new byte[bytesToRead];
            ReadInternal(data, bytesToRead);
            return encoding.GetString(data, 0, data.Length);
        }

        #endregion

        #region Private methods
        /// <summary>
        /// Checks whether or not the reader has been disposed, throwing an exception if so.
        /// </summary>
        void CheckDisposed()
        {
            if (disposed)
            {
                throw new ObjectDisposedException("EndianBinaryReader");
            }
        }

        /// <summary>
        /// Reads the given number of bytes from the stream, throwing an exception
        /// if they can't all be read.
        /// </summary>
        /// <param name="data">Buffer to read into</param>
        /// <param name="size">Number of bytes to read</param>
        void ReadInternal(byte[] data, int size)
        {
            CheckDisposed();
            int index = 0;
            while (index < size)
            {
                int read = stream.Read(data, index, size - index);
                if (read == 0)
                {
                    throw new EndOfStreamException
                        (String.Format("End of stream reached with {0} byte{1} left to read.", size - index,
                        size - index == 1 ? "s" : ""));
                }
                index += read;
            }
        }

        /// <summary>
        /// Reads the given number of bytes from the stream if possible, returning
        /// the number of bytes actually read, which may be less than requested if
        /// (and only if) the end of the stream is reached.
        /// </summary>
        /// <param name="data">Buffer to read into</param>
        /// <param name="size">Number of bytes to read</param>
        /// <returns>Number of bytes actually read</returns>
        int TryReadInternal(byte[] data, int size)
        {
            CheckDisposed();
            int index = 0;
            while (index < size)
            {
                int read = stream.Read(data, index, size - index);
                if (read == 0)
                {
                    return index;
                }
                index += read;
            }
            return index;
        }
        #endregion

        #region IDisposable Members
        /// <summary>
        /// Disposes of the underlying stream.
        /// </summary>
        public void Dispose()
        {
            if (!disposed)
            {
                disposed = true;
                ((IDisposable)stream).Dispose();
            }
        }
        #endregion
    }

    /// <summary>
    /// Collection of utility methods which operate on streams.
    /// (With C# 3.0, these could well become extension methods on Stream.)
    /// </summary>
    public static class StreamUtil
    {
        const int DefaultBufferSize = 8 * 1024;

        /// <summary>
        /// Reads the given stream up to the end, returning the data as a byte
        /// array.
        /// </summary>
        /// <param name="input">The stream to read from</param>
        /// <exception cref="ArgumentNullException">input is null</exception>
        /// <exception cref="IOException">An error occurs while reading from the stream</exception>
        /// <returns>The data read from the stream</returns>
        public static byte[] ReadFully(Stream input)
        {
            return ReadFully(input, DefaultBufferSize);
        }

        /// <summary>
        /// Reads the given stream up to the end, returning the data as a byte
        /// array, using the given buffer size.
        /// </summary>
        /// <param name="input">The stream to read from</param>
        /// <param name="bufferSize">The size of buffer to use when reading</param>
        /// <exception cref="ArgumentNullException">input is null</exception>
        /// <exception cref="ArgumentOutOfRangeException">bufferSize is less than 1</exception>
        /// <exception cref="IOException">An error occurs while reading from the stream</exception>
        /// <returns>The data read from the stream</returns>
        public static byte[] ReadFully(Stream input, int bufferSize)
        {
            if (bufferSize < 1)
            {
                throw new ArgumentOutOfRangeException("bufferSize");
            }
            return ReadFully(input, new byte[bufferSize]);
        }

        /// <summary>
        /// Reads the given stream up to the end, returning the data as a byte
        /// array, using the given buffer for transferring data. Note that the
        /// current contents of the buffer is ignored, so the buffer needn't
        /// be cleared beforehand.
        /// </summary>
        /// <param name="input">The stream to read from</param>
        /// <param name="buffer">The buffer to use to transfer data</param>
        /// <exception cref="ArgumentNullException">input is null</exception>
        /// <exception cref="ArgumentNullException">buffer is null</exception>
        /// <exception cref="IOException">An error occurs while reading from the stream</exception>
        /// <returns>The data read from the stream</returns>
        public static byte[] ReadFully(Stream input, IBuffer buffer)
        {
            if (buffer == null)
            {
                throw new ArgumentNullException("buffer");
            }
            return ReadFully(input, buffer.Bytes);
        }

        /// <summary>
        /// Reads the given stream up to the end, returning the data as a byte
        /// array, using the given buffer for transferring data. Note that the
        /// current contents of the buffer is ignored, so the buffer needn't
        /// be cleared beforehand.
        /// </summary>
        /// <param name="input">The stream to read from</param>
        /// <param name="buffer">The buffer to use to transfer data</param>
        /// <exception cref="ArgumentNullException">input is null</exception>
        /// <exception cref="ArgumentNullException">buffer is null</exception>
        /// <exception cref="ArgumentException">buffer is a zero-length array</exception>
        /// <exception cref="IOException">An error occurs while reading from the stream</exception>
        /// <returns>The data read from the stream</returns>
        public static byte[] ReadFully(Stream input, byte[] buffer)
        {
            if (buffer == null)
            {
                throw new ArgumentNullException("buffer");
            }
            if (input == null)
            {
                throw new ArgumentNullException("input");
            }
            if (buffer.Length == 0)
            {
                throw new ArgumentException("Buffer has length of 0");
            }
            // We could do all our own work here, but using MemoryStream is easier
            // and likely to be just as efficient.
            using (MemoryStream tempStream = new MemoryStream())
            {
                Copy(input, tempStream, buffer);
                // No need to copy the buffer if it's the right size
                if (tempStream.Length == tempStream.GetBuffer().Length)
                {
                    return tempStream.GetBuffer();
                }
                // Okay, make a copy that's the right size
                return tempStream.ToArray();
            }
        }

        /// <summary>
        /// Copies all the data from one stream into another.
        /// </summary>
        /// <param name="input">The stream to read from</param>
        /// <param name="output">The stream to write to</param>
        /// <exception cref="ArgumentNullException">input is null</exception>
        /// <exception cref="ArgumentNullException">output is null</exception>
        /// <exception cref="IOException">An error occurs while reading or writing</exception>
        public static void Copy(Stream input, Stream output)
        {
            Copy(input, output, DefaultBufferSize);
        }

        /// <summary>
        /// Copies all the data from one stream into another, using a buffer
        /// of the given size.
        /// </summary>
        /// <param name="input">The stream to read from</param>
        /// <param name="output">The stream to write to</param>
        /// <param name="bufferSize">The size of buffer to use when reading</param>
        /// <exception cref="ArgumentNullException">input is null</exception>
        /// <exception cref="ArgumentNullException">output is null</exception>
        /// <exception cref="ArgumentOutOfRangeException">bufferSize is less than 1</exception>
        /// <exception cref="IOException">An error occurs while reading or writing</exception>
        public static void Copy(Stream input, Stream output, int bufferSize)
        {
            if (bufferSize < 1)
            {
                throw new ArgumentOutOfRangeException("bufferSize");
            }
            Copy(input, output, new byte[bufferSize]);
        }

        /// <summary>
        /// Copies all the data from one stream into another, using the given 
        /// buffer for transferring data. Note that the current contents of 
        /// the buffer is ignored, so the buffer needn't be cleared beforehand.
        /// </summary>
        /// <param name="input">The stream to read from</param>
        /// <param name="output">The stream to write to</param>
        /// <param name="buffer">The buffer to use to transfer data</param>
        /// <exception cref="ArgumentNullException">input is null</exception>
        /// <exception cref="ArgumentNullException">output is null</exception>
        /// <exception cref="ArgumentNullException">buffer is null</exception>
        /// <exception cref="IOException">An error occurs while reading or writing</exception>
        public static void Copy(Stream input, Stream output, IBuffer buffer)
        {
            if (buffer == null)
            {
                throw new ArgumentNullException("buffer");
            }
            Copy(input, output, buffer.Bytes);
        }

        /// <summary>
        /// Copies all the data from one stream into another, using the given 
        /// buffer for transferring data. Note that the current contents of 
        /// the buffer is ignored, so the buffer needn't be cleared beforehand.
        /// </summary>
        /// <param name="input">The stream to read from</param>
        /// <param name="output">The stream to write to</param>
        /// <param name="buffer">The buffer to use to transfer data</param>
        /// <exception cref="ArgumentNullException">input is null</exception>
        /// <exception cref="ArgumentNullException">output is null</exception>
        /// <exception cref="ArgumentNullException">buffer is null</exception>
        /// <exception cref="ArgumentException">buffer is a zero-length array</exception>
        /// <exception cref="IOException">An error occurs while reading or writing</exception>
        public static void Copy(Stream input, Stream output, byte[] buffer)
        {
            if (buffer == null)
            {
                throw new ArgumentNullException("buffer");
            }
            if (input == null)
            {
                throw new ArgumentNullException("input");
            }
            if (output == null)
            {
                throw new ArgumentNullException("output");
            }
            if (buffer.Length == 0)
            {
                throw new ArgumentException("Buffer has length of 0");
            }
            int read;
            while ((read = input.Read(buffer, 0, buffer.Length)) > 0)
            {
                output.Write(buffer, 0, read);
            }
        }

        /// <summary>
        /// Reads exactly the given number of bytes from the specified stream.
        /// If the end of the stream is reached before the specified amount
        /// of data is read, an exception is thrown.
        /// </summary>
        /// <param name="input">The stream to read from</param>
        /// <param name="bytesToRead">The number of bytes to read</param>
        /// <exception cref="ArgumentNullException">input is null</exception>
        /// <exception cref="ArgumentOutOfRangeException">bytesToRead is less than 1</exception>
        /// <exception cref="EndOfStreamException">The end of the stream is reached before 
        /// enough data has been read</exception>
        /// <exception cref="IOException">An error occurs while reading from the stream</exception>
        /// <returns>The data read from the stream</returns>
        public static byte[] ReadExactly(Stream input, int bytesToRead)
        {
            return ReadExactly(input, new byte[bytesToRead]);
        }

        /// <summary>
        /// Reads into a buffer, filling it completely.
        /// </summary>
        /// <param name="input">The stream to read from</param>
        /// <param name="buffer">The buffer to read into</param>
        /// <exception cref="ArgumentNullException">input is null</exception>
        /// <exception cref="ArgumentOutOfRangeException">The buffer is of zero length</exception>
        /// <exception cref="EndOfStreamException">The end of the stream is reached before 
        /// enough data has been read</exception>
        /// <exception cref="IOException">An error occurs while reading from the stream</exception>
        /// <returns>The data read from the stream</returns>
        public static byte[] ReadExactly(Stream input, IBuffer buffer)
        {
            return ReadExactly(input, buffer.Bytes);
        }

        /// <summary>
        /// Reads into a buffer, filling it completely.
        /// </summary>
        /// <param name="input">The stream to read from</param>
        /// <param name="buffer">The buffer to read into</param>
        /// <exception cref="ArgumentNullException">input is null</exception>
        /// <exception cref="ArgumentOutOfRangeException">The buffer is of zero length</exception>
        /// <exception cref="EndOfStreamException">The end of the stream is reached before 
        /// enough data has been read</exception>
        /// <exception cref="IOException">An error occurs while reading from the stream</exception>
        /// <returns>The data read from the stream</returns>
        public static byte[] ReadExactly(Stream input, byte[] buffer)
        {
            return ReadExactly(input, buffer, buffer.Length);
        }

        /// <summary>
        /// Reads into a buffer, for the given number of bytes.
        /// </summary>
        /// <param name="input">The stream to read from</param>
        /// <param name="buffer">The buffer to read into</param>
        /// <param name="bytesToRead">The number of bytes to read</param>
        /// <exception cref="ArgumentNullException">input is null</exception>
        /// <exception cref="ArgumentOutOfRangeException">The buffer is of zero length, or bytesToRead
        /// exceeds the buffer length</exception>
        /// <exception cref="EndOfStreamException">The end of the stream is reached before 
        /// enough data has been read</exception>
        /// <exception cref="IOException">An error occurs while reading from the stream</exception>
        /// <returns>The data read from the stream</returns>
        public static byte[] ReadExactly(Stream input, IBuffer buffer, int bytesToRead)
        {
            return ReadExactly(input, buffer.Bytes, bytesToRead);
        }

        /// <summary>
        /// Reads exactly the given number of bytes from the specified stream,
        /// into the given buffer, starting at position 0 of the array.
        /// </summary>
        /// <param name="input">The stream to read from</param>
        /// <param name="buffer">The byte array to read into</param>
        /// <param name="bytesToRead">The number of bytes to read</param>
        /// <exception cref="ArgumentNullException">input is null</exception>
        /// <exception cref="ArgumentOutOfRangeException">bytesToRead is less than 1</exception>
        /// <exception cref="EndOfStreamException">The end of the stream is reached before 
        /// enough data has been read</exception>
        /// <exception cref="IOException">An error occurs while reading from the stream</exception>
        public static byte[] ReadExactly(Stream input, byte[] buffer, int bytesToRead)
        {
            return ReadExactly(input, buffer, 0, bytesToRead);
        }

        /// <summary>
        /// Reads into a buffer, for the given number of bytes, from the specified location
        /// </summary>
        /// <param name="input">The stream to read from</param>
        /// <param name="buffer">The buffer to read into</param>
        /// <param name="startIndex">The index into the buffer at which to start writing</param>
        /// <param name="bytesToRead">The number of bytes to read</param>
        /// <exception cref="ArgumentNullException">input is null</exception>
        /// <exception cref="ArgumentOutOfRangeException">The buffer is of zero length, or startIndex+bytesToRead
        /// exceeds the buffer length</exception>
        /// <exception cref="EndOfStreamException">The end of the stream is reached before 
        /// enough data has been read</exception>
        /// <exception cref="IOException">An error occurs while reading from the stream</exception>
        /// <returns>The data read from the stream</returns>
        public static byte[] ReadExactly(Stream input, IBuffer buffer, int startIndex, int bytesToRead)
        {
            return ReadExactly(input, buffer.Bytes, 0, bytesToRead);
        }

        /// <summary>
        /// Reads exactly the given number of bytes from the specified stream,
        /// into the given buffer, starting at position 0 of the array.
        /// </summary>
        /// <param name="input">The stream to read from</param>
        /// <param name="buffer">The byte array to read into</param>
        /// <param name="startIndex">The index into the buffer at which to start writing</param>
        /// <param name="bytesToRead">The number of bytes to read</param>
        /// <exception cref="ArgumentNullException">input is null</exception>
        /// <exception cref="ArgumentOutOfRangeException">bytesToRead is less than 1, startIndex is less than 0,
        /// or startIndex+bytesToRead is greater than the buffer length</exception>
        /// <exception cref="EndOfStreamException">The end of the stream is reached before 
        /// enough data has been read</exception>
        /// <exception cref="IOException">An error occurs while reading from the stream</exception>
        public static byte[] ReadExactly(Stream input, byte[] buffer, int startIndex, int bytesToRead)
        {
            if (input == null)
            {
                throw new ArgumentNullException("input");
            }

            if (buffer == null)
            {
                throw new ArgumentNullException("buffer");
            }

            if (startIndex < 0 || startIndex >= buffer.Length)
            {
                throw new ArgumentOutOfRangeException("startIndex");
            }

            if (bytesToRead < 1 || startIndex + bytesToRead > buffer.Length)
            {
                throw new ArgumentOutOfRangeException("bytesToRead");
            }

            int index = 0;

            while (index < bytesToRead)
            {
                int read = input.Read(buffer, startIndex + index, bytesToRead - index);
                if (read == 0)
                {
                    throw new EndOfStreamException
                        (String.Format("End of stream reached with {0} byte{1} left to read.",
                                       bytesToRead - index,
                                       bytesToRead - index == 1 ? "s" : ""));
                }
                index += read;
            }
            return buffer;
        }
    }

    /// <summary>
    /// Equivalent of System.IO.BinaryWriter, but with either endianness, depending on
    /// the EndianBitConverter it is constructed with.
    /// </summary>
    public class EndianBinaryWriter : IDisposable
    {
        #region Fields not directly related to properties
        /// <summary>
        /// Whether or not this writer has been disposed yet.
        /// </summary>
        bool disposed = false;
        /// <summary>
        /// Buffer used for temporary storage during conversion from primitives
        /// </summary>
        byte[] buffer = new byte[16];
        /// <summary>
        /// Buffer used for Write(char)
        /// </summary>
        char[] charBuffer = new char[1];
        #endregion

        #region Constructors
        /// <summary>
        /// Constructs a new binary writer with the given bit converter, writing
        /// to the given stream, using UTF-8 encoding.
        /// </summary>
        /// <param name="bitConverter">Converter to use when writing data</param>
        /// <param name="stream">Stream to write data to</param>
        public EndianBinaryWriter(EndianBitConverter bitConverter,
            Stream stream) : this(bitConverter, stream, Encoding.UTF8)
        {
        }

        /// <summary>
        /// Constructs a new binary writer with the given bit converter, writing
        /// to the given stream, using the given encoding.
        /// </summary>
        /// <param name="bitConverter">Converter to use when writing data</param>
        /// <param name="stream">Stream to write data to</param>
        /// <param name="encoding">Encoding to use when writing character data</param>
        public EndianBinaryWriter(EndianBitConverter bitConverter, Stream stream, Encoding encoding)
        {
            if (bitConverter == null)
            {
                throw new ArgumentNullException("bitConverter");
            }
            if (stream == null)
            {
                throw new ArgumentNullException("stream");
            }
            if (encoding == null)
            {
                throw new ArgumentNullException("encoding");
            }
            if (!stream.CanWrite)
            {
                throw new ArgumentException("Stream isn't writable", "stream");
            }
            this.stream = stream;
            this.bitConverter = bitConverter;
            this.encoding = encoding;
        }
        #endregion

        #region Properties
        EndianBitConverter bitConverter;
        /// <summary>
        /// The bit converter used to write values to the stream
        /// </summary>
        public EndianBitConverter BitConverter
        {
            get { return bitConverter; }
        }

        Encoding encoding;
        /// <summary>
        /// The encoding used to write strings
        /// </summary>
        public Encoding Encoding
        {
            get { return encoding; }
        }

        Stream stream;
        /// <summary>
        /// Gets the underlying stream of the EndianBinaryWriter.
        /// </summary>
        public Stream BaseStream
        {
            get { return stream; }
        }
        #endregion

        #region Public methods
        /// <summary>
        /// Closes the writer, including the underlying stream.
        /// </summary>
        public void Close()
        {
            Dispose();
        }

        /// <summary>
        /// Flushes the underlying stream.
        /// </summary>
        public void Flush()
        {
            CheckDisposed();
            stream.Flush();
        }

        /// <summary>
        /// Seeks within the stream.
        /// </summary>
        /// <param name="offset">Offset to seek to.</param>
        /// <param name="origin">Origin of seek operation.</param>
        public void Seek(int offset, SeekOrigin origin)
        {
            CheckDisposed();
            stream.Seek(offset, origin);
        }

        /// <summary>
        /// Writes a boolean value to the stream. 1 byte is written.
        /// </summary>
        /// <param name="value">The value to write</param>
        public void Write(bool value)
        {
            bitConverter.CopyBytes(value, buffer, 0);
            WriteInternal(buffer, 1);
        }

        /// <summary>
        /// Writes a 16-bit signed integer to the stream, using the bit converter
        /// for this writer. 2 bytes are written.
        /// </summary>
        /// <param name="value">The value to write</param>
        public void Write(short value)
        {
            bitConverter.CopyBytes(value, buffer, 0);
            WriteInternal(buffer, 2);
        }

        /// <summary>
        /// Writes a 32-bit signed integer to the stream, using the bit converter
        /// for this writer. 4 bytes are written.
        /// </summary>
        /// <param name="value">The value to write</param>
        public void Write(int value)
        {
            bitConverter.CopyBytes(value, buffer, 0);
            WriteInternal(buffer, 4);
        }

        /// <summary>
        /// Writes a 64-bit signed integer to the stream, using the bit converter
        /// for this writer. 8 bytes are written.
        /// </summary>
        /// <param name="value">The value to write</param>
        public void Write(long value)
        {
            bitConverter.CopyBytes(value, buffer, 0);
            WriteInternal(buffer, 8);
        }

        /// <summary>
        /// Writes a 16-bit unsigned integer to the stream, using the bit converter
        /// for this writer. 2 bytes are written.
        /// </summary>
        /// <param name="value">The value to write</param>
        public void Write(ushort value)
        {
            bitConverter.CopyBytes(value, buffer, 0);
            WriteInternal(buffer, 2);
        }

        /// <summary>
        /// Writes a 32-bit unsigned integer to the stream, using the bit converter
        /// for this writer. 4 bytes are written.
        /// </summary>
        /// <param name="value">The value to write</param>
        public void Write(uint value)
        {
            bitConverter.CopyBytes(value, buffer, 0);
            WriteInternal(buffer, 4);
        }

        /// <summary>
        /// Writes a 64-bit unsigned integer to the stream, using the bit converter
        /// for this writer. 8 bytes are written.
        /// </summary>
        /// <param name="value">The value to write</param>
        public void Write(ulong value)
        {
            bitConverter.CopyBytes(value, buffer, 0);
            WriteInternal(buffer, 8);
        }

        /// <summary>
        /// Writes a single-precision floating-point value to the stream, using the bit converter
        /// for this writer. 4 bytes are written.
        /// </summary>
        /// <param name="value">The value to write</param>
        public void Write(float value)
        {
            bitConverter.CopyBytes(value, buffer, 0);
            WriteInternal(buffer, 4);
        }

        /// <summary>
        /// Writes a double-precision floating-point value to the stream, using the bit converter
        /// for this writer. 8 bytes are written.
        /// </summary>
        /// <param name="value">The value to write</param>
        public void Write(double value)
        {
            bitConverter.CopyBytes(value, buffer, 0);
            WriteInternal(buffer, 8);
        }

        /// <summary>
        /// Writes a decimal value to the stream, using the bit converter for this writer.
        /// 16 bytes are written.
        /// </summary>
        /// <param name="value">The value to write</param>
        public void Write(decimal value)
        {
            bitConverter.CopyBytes(value, buffer, 0);
            WriteInternal(buffer, 16);
        }

        /// <summary>
        /// Writes a signed byte to the stream.
        /// </summary>
        /// <param name="value">The value to write</param>
        public void Write(byte value)
        {
            buffer[0] = value;
            WriteInternal(buffer, 1);
        }

        /// <summary>
        /// Writes an unsigned byte to the stream.
        /// </summary>
        /// <param name="value">The value to write</param>
        public void Write(sbyte value)
        {
            buffer[0] = unchecked((byte)value);
            WriteInternal(buffer, 1);
        }

        /// <summary>
        /// Writes an array of bytes to the stream.
        /// </summary>
        /// <param name="value">The values to write</param>
        public void Write(byte[] value)
        {
            if (value == null)
            {
                throw (new System.ArgumentNullException("value"));
            }
            WriteInternal(value, value.Length);
        }

        /// <summary>
        /// Writes a portion of an array of bytes to the stream.
        /// </summary>
        /// <param name="value">An array containing the bytes to write</param>
        /// <param name="offset">The index of the first byte to write within the array</param>
        /// <param name="count">The number of bytes to write</param>
        public void Write(byte[] value, int offset, int count)
        {
            CheckDisposed();
            stream.Write(value, offset, count);
        }

        /// <summary>
        /// Writes a single character to the stream, using the encoding for this writer.
        /// </summary>
        /// <param name="value">The value to write</param>
        public void Write(char value)
        {
            charBuffer[0] = value;
            Write(charBuffer);
        }

        /// <summary>
        /// Writes an array of characters to the stream, using the encoding for this writer.
        /// </summary>
        /// <param name="value">An array containing the characters to write</param>
        public void Write(char[] value)
        {
            if (value == null)
            {
                throw new ArgumentNullException("value");
            }
            CheckDisposed();
            byte[] data = Encoding.GetBytes(value, 0, value.Length);
            WriteInternal(data, data.Length);
        }

        /// <summary>
        /// Writes a string to the stream, using the encoding for this writer.
        /// </summary>
        /// <param name="value">The value to write. Must not be null.</param>
        /// <exception cref="ArgumentNullException">value is null</exception>
        public void Write(string value)
        {
            if (value == null)
            {
                throw new ArgumentNullException("value");
            }
            CheckDisposed();
            byte[] data = Encoding.GetBytes(value);
            Write7BitEncodedInt(data.Length);
            WriteInternal(data, data.Length);
        }

        /// <summary>
        /// Writes a 7-bit encoded integer from the stream. This is stored with the least significant
        /// information first, with 7 bits of information per byte of value, and the top
        /// bit as a continuation flag.
        /// </summary>
        /// <param name="value">The 7-bit encoded integer to write to the stream</param>
        public void Write7BitEncodedInt(int value)
        {
            CheckDisposed();
            if (value < 0)
            {
                throw new ArgumentOutOfRangeException("value", "Value must be greater than or equal to 0.");
            }
            int index = 0;
            while (value >= 128)
            {
                buffer[index++] = (byte)((value & 0x7f) | 0x80);
                value = value >> 7;
                index++;
            }
            buffer[index++] = (byte)value;
            stream.Write(buffer, 0, index);
        }

        #endregion

        #region Private methods
        /// <summary>
        /// Checks whether or not the writer has been disposed, throwing an exception if so.
        /// </summary>
        void CheckDisposed()
        {
            if (disposed)
            {
                throw new ObjectDisposedException("EndianBinaryWriter");
            }
        }

        /// <summary>
        /// Writes the specified number of bytes from the start of the given byte array,
        /// after checking whether or not the writer has been disposed.
        /// </summary>
        /// <param name="bytes">The array of bytes to write from</param>
        /// <param name="length">The number of bytes to write</param>
        void WriteInternal(byte[] bytes, int length)
        {
            CheckDisposed();
            stream.Write(bytes, 0, length);
        }
        #endregion

        #region IDisposable Members
        /// <summary>
        /// Disposes of the underlying stream.
        /// </summary>
        public void Dispose()
        {
            if (!disposed)
            {
                Flush();
                disposed = true;
                ((IDisposable)stream).Dispose();
            }
        }
        #endregion
    }

    /// <summary>
    /// Implementation of EndianBitConverter which converts to/from big-endian
    /// byte arrays.
    /// </summary>
    public sealed class BigEndianBitConverter : EndianBitConverter
    {
        /// <summary>
        /// Indicates the byte order ("endianess") in which data is converted using this class.
        /// </summary>
        /// <remarks>
        /// Different computer architectures store data using different byte orders. "Big-endian"
        /// means the most significant byte is on the left end of a word. "Little-endian" means the 
        /// most significant byte is on the right end of a word.
        /// </remarks>
        /// <returns>true if this converter is little-endian, false otherwise.</returns>
        public sealed override bool IsLittleEndian()
        {
            return false;
        }

        /// <summary>
        /// Indicates the byte order ("endianess") in which data is converted using this class.
        /// </summary>
        public sealed override Endianness Endianness
        {
            get { return Endianness.BigEndian; }
        }

        /// <summary>
        /// Copies the specified number of bytes from value to buffer, starting at index.
        /// </summary>
        /// <param name="value">The value to copy</param>
        /// <param name="bytes">The number of bytes to copy</param>
        /// <param name="buffer">The buffer to copy the bytes into</param>
        /// <param name="index">The index to start at</param>
        protected override void CopyBytesImpl(long value, int bytes, byte[] buffer, int index)
        {
            int endOffset = index + bytes - 1;
            for (int i = 0; i < bytes; i++)
            {
                buffer[endOffset - i] = unchecked((byte)(value & 0xff));
                value = value >> 8;
            }
        }

        /// <summary>
        /// Returns a value built from the specified number of bytes from the given buffer,
        /// starting at index.
        /// </summary>
        /// <param name="buffer">The data in byte array format</param>
        /// <param name="startIndex">The first index to use</param>
        /// <param name="bytesToConvert">The number of bytes to use</param>
        /// <returns>The value built from the given bytes</returns>
        protected override long FromBytes(byte[] buffer, int startIndex, int bytesToConvert)
        {
            long ret = 0;
            for (int i = 0; i < bytesToConvert; i++)
            {
                ret = unchecked((ret << 8) | buffer[startIndex + i]);
            }
            return ret;
        }
    }

    /// <summary>
    /// Implementation of EndianBitConverter which converts to/from little-endian
    /// byte arrays.
    /// </summary>
    public sealed class LittleEndianBitConverter : EndianBitConverter
    {
        /// <summary>
        /// Indicates the byte order ("endianess") in which data is converted using this class.
        /// </summary>
        /// <remarks>
        /// Different computer architectures store data using different byte orders. "Big-endian"
        /// means the most significant byte is on the left end of a word. "Little-endian" means the 
        /// most significant byte is on the right end of a word.
        /// </remarks>
        /// <returns>true if this converter is little-endian, false otherwise.</returns>
        public sealed override bool IsLittleEndian()
        {
            return true;
        }

        /// <summary>
        /// Indicates the byte order ("endianess") in which data is converted using this class.
        /// </summary>
        public sealed override Endianness Endianness
        {
            get { return Endianness.LittleEndian; }
        }

        /// <summary>
        /// Copies the specified number of bytes from value to buffer, starting at index.
        /// </summary>
        /// <param name="value">The value to copy</param>
        /// <param name="bytes">The number of bytes to copy</param>
        /// <param name="buffer">The buffer to copy the bytes into</param>
        /// <param name="index">The index to start at</param>
        protected override void CopyBytesImpl(long value, int bytes, byte[] buffer, int index)
        {
            for (int i = 0; i < bytes; i++)
            {
                buffer[i + index] = unchecked((byte)(value & 0xff));
                value = value >> 8;
            }
        }

        /// <summary>
        /// Returns a value built from the specified number of bytes from the given buffer,
        /// starting at index.
        /// </summary>
        /// <param name="buffer">The data in byte array format</param>
        /// <param name="startIndex">The first index to use</param>
        /// <param name="bytesToConvert">The number of bytes to use</param>
        /// <returns>The value built from the given bytes</returns>
        protected override long FromBytes(byte[] buffer, int startIndex, int bytesToConvert)
        {
            long ret = 0;
            for (int i = 0; i < bytesToConvert; i++)
            {
                ret = unchecked((ret << 8) | buffer[startIndex + bytesToConvert - 1 - i]);
            }
            return ret;
        }
    }

    /// <summary>
    /// Interface encapsulating a byte array which may be managed
    /// by an IBufferManager implementation. When the buffer is
    /// disposed, some implementations of this interface may
    /// return the buffer to the IBufferManager which created
    /// it. Note that an IBuffer *must* always be disposed after use,
    /// or some implementations may leak memory. The buffer must
    /// not be used after being disposed, likewise the byte array must
    /// not be used after the buffer is disposed.
    /// </summary>
    public interface IBuffer : IDisposable
    {
        /// <summary>
        /// Returns the byte array encapsulated by this buffer.
        /// Note that depending on the buffer manager providing
        /// the buffer, the array may or may not be cleared (i.e.
        /// with every byte 0) to start with.
        /// </summary>
        byte[] Bytes { get; }
    }
}
