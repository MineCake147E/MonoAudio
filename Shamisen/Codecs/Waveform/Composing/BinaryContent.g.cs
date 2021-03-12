﻿using System;
using System.Collections.Generic;
using System.Text;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Buffers.Binary;

namespace Shamisen.Codecs.Waveform.Composing
{
    /// <summary>
    /// Represents a binary content like <see cref="int"/> and <see cref="float"/>.
    /// </summary>
    /// <seealso cref="IRf64Content" />
    public readonly partial struct BinaryContent : IRf64Content
    {
        /// <summary>
        /// Performs an explicit conversion from <see cref="ushort"/> to <see cref="BinaryContent"/> in little endian.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <returns>
        /// The result of the conversion.
        /// </returns>
        public static explicit operator BinaryContent(ushort value) => CreateLittleEndian(value);

        /// <summary>
        /// Performs an explicit conversion from <see cref="short"/> to <see cref="BinaryContent"/> in little endian.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <returns>
        /// The result of the conversion.
        /// </returns>
        public static explicit operator BinaryContent(short value) => CreateLittleEndian(value);

        /// <summary>
        /// Performs an explicit conversion from <see cref="uint"/> to <see cref="BinaryContent"/> in little endian.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <returns>
        /// The result of the conversion.
        /// </returns>
        public static explicit operator BinaryContent(uint value) => CreateLittleEndian(value);

        /// <summary>
        /// Performs an explicit conversion from <see cref="int"/> to <see cref="BinaryContent"/> in little endian.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <returns>
        /// The result of the conversion.
        /// </returns>
        public static explicit operator BinaryContent(int value) => CreateLittleEndian(value);

        /// <summary>
        /// Performs an explicit conversion from <see cref="ulong"/> to <see cref="BinaryContent"/> in little endian.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <returns>
        /// The result of the conversion.
        /// </returns>
        public static explicit operator BinaryContent(ulong value) => CreateLittleEndian(value);

        /// <summary>
        /// Performs an explicit conversion from <see cref="long"/> to <see cref="BinaryContent"/> in little endian.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <returns>
        /// The result of the conversion.
        /// </returns>
        public static explicit operator BinaryContent(long value) => CreateLittleEndian(value);


        /// <summary>
        /// Initializes a new instance of the <see cref="BinaryContent"/> struct with specified <paramref name="value"/> in little endian.
        /// </summary>
        /// <param name="value">The value.</param>
        public static BinaryContent CreateLittleEndian(ushort value)
        {
            var buffer = new byte[sizeof(ushort)];
            BinaryPrimitives.WriteUInt16LittleEndian(buffer.AsSpan(), value);
            return new BinaryContent(buffer.AsMemory());
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="BinaryContent"/> struct with specified <paramref name="value"/> in little endian.
        /// </summary>
        /// <param name="value">The value.</param>
        public static BinaryContent CreateLittleEndian(short value)
        {
            var buffer = new byte[sizeof(short)];
            BinaryPrimitives.WriteInt16LittleEndian(buffer.AsSpan(), value);
            return new BinaryContent(buffer.AsMemory());
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="BinaryContent"/> struct with specified <paramref name="value"/> in little endian.
        /// </summary>
        /// <param name="value">The value.</param>
        public static BinaryContent CreateLittleEndian(uint value)
        {
            var buffer = new byte[sizeof(uint)];
            BinaryPrimitives.WriteUInt32LittleEndian(buffer.AsSpan(), value);
            return new BinaryContent(buffer.AsMemory());
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="BinaryContent"/> struct with specified <paramref name="value"/> in little endian.
        /// </summary>
        /// <param name="value">The value.</param>
        public static BinaryContent CreateLittleEndian(int value)
        {
            var buffer = new byte[sizeof(int)];
            BinaryPrimitives.WriteInt32LittleEndian(buffer.AsSpan(), value);
            return new BinaryContent(buffer.AsMemory());
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="BinaryContent"/> struct with specified <paramref name="value"/> in little endian.
        /// </summary>
        /// <param name="value">The value.</param>
        public static BinaryContent CreateLittleEndian(ulong value)
        {
            var buffer = new byte[sizeof(ulong)];
            BinaryPrimitives.WriteUInt64LittleEndian(buffer.AsSpan(), value);
            return new BinaryContent(buffer.AsMemory());
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="BinaryContent"/> struct with specified <paramref name="value"/> in little endian.
        /// </summary>
        /// <param name="value">The value.</param>
        public static BinaryContent CreateLittleEndian(long value)
        {
            var buffer = new byte[sizeof(long)];
            BinaryPrimitives.WriteInt64LittleEndian(buffer.AsSpan(), value);
            return new BinaryContent(buffer.AsMemory());
        }


        /// <summary>
        /// Initializes a new instance of the <see cref="BinaryContent"/> struct with specified <paramref name="value"/> in BIG endian.
        /// </summary>
        /// <param name="value">The value.</param>
        public static BinaryContent CreateBigEndian(ushort value)
        {
            var buffer = new byte[sizeof(ushort)];
            BinaryPrimitives.WriteUInt16BigEndian(buffer.AsSpan(), value);
            return new BinaryContent(buffer.AsMemory());
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="BinaryContent"/> struct with specified <paramref name="value"/> in BIG endian.
        /// </summary>
        /// <param name="value">The value.</param>
        public static BinaryContent CreateBigEndian(short value)
        {
            var buffer = new byte[sizeof(short)];
            BinaryPrimitives.WriteInt16BigEndian(buffer.AsSpan(), value);
            return new BinaryContent(buffer.AsMemory());
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="BinaryContent"/> struct with specified <paramref name="value"/> in BIG endian.
        /// </summary>
        /// <param name="value">The value.</param>
        public static BinaryContent CreateBigEndian(uint value)
        {
            var buffer = new byte[sizeof(uint)];
            BinaryPrimitives.WriteUInt32BigEndian(buffer.AsSpan(), value);
            return new BinaryContent(buffer.AsMemory());
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="BinaryContent"/> struct with specified <paramref name="value"/> in BIG endian.
        /// </summary>
        /// <param name="value">The value.</param>
        public static BinaryContent CreateBigEndian(int value)
        {
            var buffer = new byte[sizeof(int)];
            BinaryPrimitives.WriteInt32BigEndian(buffer.AsSpan(), value);
            return new BinaryContent(buffer.AsMemory());
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="BinaryContent"/> struct with specified <paramref name="value"/> in BIG endian.
        /// </summary>
        /// <param name="value">The value.</param>
        public static BinaryContent CreateBigEndian(ulong value)
        {
            var buffer = new byte[sizeof(ulong)];
            BinaryPrimitives.WriteUInt64BigEndian(buffer.AsSpan(), value);
            return new BinaryContent(buffer.AsMemory());
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="BinaryContent"/> struct with specified <paramref name="value"/> in BIG endian.
        /// </summary>
        /// <param name="value">The value.</param>
        public static BinaryContent CreateBigEndian(long value)
        {
            var buffer = new byte[sizeof(long)];
            BinaryPrimitives.WriteInt64BigEndian(buffer.AsSpan(), value);
            return new BinaryContent(buffer.AsMemory());
        }

    }
}
