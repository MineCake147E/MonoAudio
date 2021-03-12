﻿using System;
using System.Collections.Generic;
using System.Text;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Buffers.Binary;
namespace Shamisen.Data
{
    /// <summary>
    /// Contains some utility functions for <see cref="IDataSink{TSample}"/>.
    /// </summary>
    public static partial class DataSinkUtils
    {
        /// <summary>
        /// Writes the <see cref="ushort"/> value to the specified <paramref name="sink"/> with little endian.
        /// </summary>
        /// <param name="sink">The sink to write to.</param>
        /// <param name="value">The value to write.</param>
        public static void WriteUInt16LittleEndian(this IDataSink<byte> sink, ushort value)
        {
            unsafe
            {
                ushort cval = BinaryExtensions.ConvertToLittleEndian(value);
                Span<byte> span = new Span<byte>(&cval, sizeof(ushort));
                sink.Write(span);
            }
        }
        /// <summary>
        /// Writes the <see cref="ushort"/> value to the specified <paramref name="sink"/> with BIG endian.
        /// </summary>
        /// <param name="sink">The sink to write to.</param>
        /// <param name="value">The value to write.</param>
        public static void WriteUInt16BigEndian(this IDataSink<byte> sink, ushort value)
        {
            unsafe
            {
                ushort cval = BinaryExtensions.ConvertToBigEndian(value);
                Span<byte> span = new Span<byte>(&cval, sizeof(ushort));
                sink.Write(span);
            }
        }
        /// <summary>
        /// Writes the <see cref="short"/> value to the specified <paramref name="sink"/> with little endian.
        /// </summary>
        /// <param name="sink">The sink to write to.</param>
        /// <param name="value">The value to write.</param>
        public static void WriteInt16LittleEndian(this IDataSink<byte> sink, short value)
        {
            unsafe
            {
                short cval = BinaryExtensions.ConvertToLittleEndian(value);
                Span<byte> span = new Span<byte>(&cval, sizeof(short));
                sink.Write(span);
            }
        }
        /// <summary>
        /// Writes the <see cref="short"/> value to the specified <paramref name="sink"/> with BIG endian.
        /// </summary>
        /// <param name="sink">The sink to write to.</param>
        /// <param name="value">The value to write.</param>
        public static void WriteInt16BigEndian(this IDataSink<byte> sink, short value)
        {
            unsafe
            {
                short cval = BinaryExtensions.ConvertToBigEndian(value);
                Span<byte> span = new Span<byte>(&cval, sizeof(short));
                sink.Write(span);
            }
        }
        /// <summary>
        /// Writes the <see cref="uint"/> value to the specified <paramref name="sink"/> with little endian.
        /// </summary>
        /// <param name="sink">The sink to write to.</param>
        /// <param name="value">The value to write.</param>
        public static void WriteUInt32LittleEndian(this IDataSink<byte> sink, uint value)
        {
            unsafe
            {
                uint cval = BinaryExtensions.ConvertToLittleEndian(value);
                Span<byte> span = new Span<byte>(&cval, sizeof(uint));
                sink.Write(span);
            }
        }
        /// <summary>
        /// Writes the <see cref="uint"/> value to the specified <paramref name="sink"/> with BIG endian.
        /// </summary>
        /// <param name="sink">The sink to write to.</param>
        /// <param name="value">The value to write.</param>
        public static void WriteUInt32BigEndian(this IDataSink<byte> sink, uint value)
        {
            unsafe
            {
                uint cval = BinaryExtensions.ConvertToBigEndian(value);
                Span<byte> span = new Span<byte>(&cval, sizeof(uint));
                sink.Write(span);
            }
        }
        /// <summary>
        /// Writes the <see cref="int"/> value to the specified <paramref name="sink"/> with little endian.
        /// </summary>
        /// <param name="sink">The sink to write to.</param>
        /// <param name="value">The value to write.</param>
        public static void WriteInt32LittleEndian(this IDataSink<byte> sink, int value)
        {
            unsafe
            {
                int cval = BinaryExtensions.ConvertToLittleEndian(value);
                Span<byte> span = new Span<byte>(&cval, sizeof(int));
                sink.Write(span);
            }
        }
        /// <summary>
        /// Writes the <see cref="int"/> value to the specified <paramref name="sink"/> with BIG endian.
        /// </summary>
        /// <param name="sink">The sink to write to.</param>
        /// <param name="value">The value to write.</param>
        public static void WriteInt32BigEndian(this IDataSink<byte> sink, int value)
        {
            unsafe
            {
                int cval = BinaryExtensions.ConvertToBigEndian(value);
                Span<byte> span = new Span<byte>(&cval, sizeof(int));
                sink.Write(span);
            }
        }
        /// <summary>
        /// Writes the <see cref="ulong"/> value to the specified <paramref name="sink"/> with little endian.
        /// </summary>
        /// <param name="sink">The sink to write to.</param>
        /// <param name="value">The value to write.</param>
        public static void WriteUInt64LittleEndian(this IDataSink<byte> sink, ulong value)
        {
            unsafe
            {
                ulong cval = BinaryExtensions.ConvertToLittleEndian(value);
                Span<byte> span = new Span<byte>(&cval, sizeof(ulong));
                sink.Write(span);
            }
        }
        /// <summary>
        /// Writes the <see cref="ulong"/> value to the specified <paramref name="sink"/> with BIG endian.
        /// </summary>
        /// <param name="sink">The sink to write to.</param>
        /// <param name="value">The value to write.</param>
        public static void WriteUInt64BigEndian(this IDataSink<byte> sink, ulong value)
        {
            unsafe
            {
                ulong cval = BinaryExtensions.ConvertToBigEndian(value);
                Span<byte> span = new Span<byte>(&cval, sizeof(ulong));
                sink.Write(span);
            }
        }
        /// <summary>
        /// Writes the <see cref="long"/> value to the specified <paramref name="sink"/> with little endian.
        /// </summary>
        /// <param name="sink">The sink to write to.</param>
        /// <param name="value">The value to write.</param>
        public static void WriteInt64LittleEndian(this IDataSink<byte> sink, long value)
        {
            unsafe
            {
                long cval = BinaryExtensions.ConvertToLittleEndian(value);
                Span<byte> span = new Span<byte>(&cval, sizeof(long));
                sink.Write(span);
            }
        }
        /// <summary>
        /// Writes the <see cref="long"/> value to the specified <paramref name="sink"/> with BIG endian.
        /// </summary>
        /// <param name="sink">The sink to write to.</param>
        /// <param name="value">The value to write.</param>
        public static void WriteInt64BigEndian(this IDataSink<byte> sink, long value)
        {
            unsafe
            {
                long cval = BinaryExtensions.ConvertToBigEndian(value);
                Span<byte> span = new Span<byte>(&cval, sizeof(long));
                sink.Write(span);
            }
        }
    }
}
