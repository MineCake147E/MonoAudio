﻿#define DEBUG_SPANEXT_TT_NON_USER_CODE
using System;
using System.Collections.Generic;
using System.Text;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;
using DivideSharp;
using Shamisen;
using System.Diagnostics;

namespace System
{
    /// <summary>
    /// Provides some extension functions.
    /// </summary>
    public static partial class SpanExtensions
    {
#region FastFill
		/// <summary>
        /// Fills the specified memory region faster, with the given <paramref name="value"/>.
        /// </summary>
        /// <param name="span">The span to fill.</param>
        /// <param name="value">The value to fill with.</param>
        [MethodImpl(OptimizationUtils.InlineAndOptimizeIfPossible)]
#if DEBUG_SPANEXT_TT_NON_USER_CODE
        [DebuggerStepThrough]
#endif
        public static void FastFill(this Span<float> span, float value = default)
        {
            if(Vector<float>.Count > span.Length)
			{
				span.Fill(value);
			}
			else
			{
				var spanV = MemoryMarshal.Cast<float, Vector<float>>(span);
				spanV.Fill(new Vector<float>(value));
				var spanR = span.Slice(spanV.Length * Vector<float>.Count);
				spanR.Fill(value);
			}
        }
		/// <summary>
        /// Fills the specified memory region faster, with the given <paramref name="value"/>.
        /// </summary>
        /// <param name="span">The span to fill.</param>
        /// <param name="value">The value to fill with.</param>
        [MethodImpl(OptimizationUtils.InlineAndOptimizeIfPossible)]
#if DEBUG_SPANEXT_TT_NON_USER_CODE
        [DebuggerStepThrough]
#endif
        public static void FastFill(this Span<double> span, double value = default)
        {
            if(Vector<double>.Count > span.Length)
			{
				span.Fill(value);
			}
			else
			{
				var spanV = MemoryMarshal.Cast<double, Vector<double>>(span);
				spanV.Fill(new Vector<double>(value));
				var spanR = span.Slice(spanV.Length * Vector<double>.Count);
				spanR.Fill(value);
			}
        }
		/// <summary>
        /// Fills the specified memory region faster, with the given <paramref name="value"/>.
        /// </summary>
        /// <param name="span">The span to fill.</param>
        /// <param name="value">The value to fill with.</param>
        [MethodImpl(OptimizationUtils.InlineAndOptimizeIfPossible)]
#if DEBUG_SPANEXT_TT_NON_USER_CODE
        [DebuggerStepThrough]
#endif
        public static void FastFill(this Span<byte> span, byte value = default)
        {
            if(Vector<byte>.Count > span.Length)
			{
				span.Fill(value);
			}
			else
			{
				var spanV = MemoryMarshal.Cast<byte, Vector<byte>>(span);
				spanV.Fill(new Vector<byte>(value));
				var spanR = span.Slice(spanV.Length * Vector<byte>.Count);
				spanR.Fill(value);
			}
        }
		/// <summary>
        /// Fills the specified memory region faster, with the given <paramref name="value"/>.
        /// </summary>
        /// <param name="span">The span to fill.</param>
        /// <param name="value">The value to fill with.</param>
        [MethodImpl(OptimizationUtils.InlineAndOptimizeIfPossible)]
#if DEBUG_SPANEXT_TT_NON_USER_CODE
        [DebuggerStepThrough]
#endif
        public static void FastFill(this Span<ushort> span, ushort value = default)
        {
            if(Vector<ushort>.Count > span.Length)
			{
				span.Fill(value);
			}
			else
			{
				var spanV = MemoryMarshal.Cast<ushort, Vector<ushort>>(span);
				spanV.Fill(new Vector<ushort>(value));
				var spanR = span.Slice(spanV.Length * Vector<ushort>.Count);
				spanR.Fill(value);
			}
        }
		/// <summary>
        /// Fills the specified memory region faster, with the given <paramref name="value"/>.
        /// </summary>
        /// <param name="span">The span to fill.</param>
        /// <param name="value">The value to fill with.</param>
        [MethodImpl(OptimizationUtils.InlineAndOptimizeIfPossible)]
#if DEBUG_SPANEXT_TT_NON_USER_CODE
        [DebuggerStepThrough]
#endif
        public static void FastFill(this Span<uint> span, uint value = default)
        {
            if(Vector<uint>.Count > span.Length)
			{
				span.Fill(value);
			}
			else
			{
				var spanV = MemoryMarshal.Cast<uint, Vector<uint>>(span);
				spanV.Fill(new Vector<uint>(value));
				var spanR = span.Slice(spanV.Length * Vector<uint>.Count);
				spanR.Fill(value);
			}
        }
		/// <summary>
        /// Fills the specified memory region faster, with the given <paramref name="value"/>.
        /// </summary>
        /// <param name="span">The span to fill.</param>
        /// <param name="value">The value to fill with.</param>
        [MethodImpl(OptimizationUtils.InlineAndOptimizeIfPossible)]
#if DEBUG_SPANEXT_TT_NON_USER_CODE
        [DebuggerStepThrough]
#endif
        public static void FastFill(this Span<ulong> span, ulong value = default)
        {
            if(Vector<ulong>.Count > span.Length)
			{
				span.Fill(value);
			}
			else
			{
				var spanV = MemoryMarshal.Cast<ulong, Vector<ulong>>(span);
				spanV.Fill(new Vector<ulong>(value));
				var spanR = span.Slice(spanV.Length * Vector<ulong>.Count);
				spanR.Fill(value);
			}
        }
		/// <summary>
        /// Fills the specified memory region faster, with the given <paramref name="value"/>.
        /// </summary>
        /// <param name="span">The span to fill.</param>
        /// <param name="value">The value to fill with.</param>
        [MethodImpl(OptimizationUtils.InlineAndOptimizeIfPossible)]
#if DEBUG_SPANEXT_TT_NON_USER_CODE
        [DebuggerStepThrough]
#endif
        public static void FastFill(this Span<sbyte> span, sbyte value = default)
        {
            if(Vector<sbyte>.Count > span.Length)
			{
				span.Fill(value);
			}
			else
			{
				var spanV = MemoryMarshal.Cast<sbyte, Vector<sbyte>>(span);
				spanV.Fill(new Vector<sbyte>(value));
				var spanR = span.Slice(spanV.Length * Vector<sbyte>.Count);
				spanR.Fill(value);
			}
        }
		/// <summary>
        /// Fills the specified memory region faster, with the given <paramref name="value"/>.
        /// </summary>
        /// <param name="span">The span to fill.</param>
        /// <param name="value">The value to fill with.</param>
        [MethodImpl(OptimizationUtils.InlineAndOptimizeIfPossible)]
#if DEBUG_SPANEXT_TT_NON_USER_CODE
        [DebuggerStepThrough]
#endif
        public static void FastFill(this Span<short> span, short value = default)
        {
            if(Vector<short>.Count > span.Length)
			{
				span.Fill(value);
			}
			else
			{
				var spanV = MemoryMarshal.Cast<short, Vector<short>>(span);
				spanV.Fill(new Vector<short>(value));
				var spanR = span.Slice(spanV.Length * Vector<short>.Count);
				spanR.Fill(value);
			}
        }
		/// <summary>
        /// Fills the specified memory region faster, with the given <paramref name="value"/>.
        /// </summary>
        /// <param name="span">The span to fill.</param>
        /// <param name="value">The value to fill with.</param>
        [MethodImpl(OptimizationUtils.InlineAndOptimizeIfPossible)]
#if DEBUG_SPANEXT_TT_NON_USER_CODE
        [DebuggerStepThrough]
#endif
        public static void FastFill(this Span<int> span, int value = default)
        {
            if(Vector<int>.Count > span.Length)
			{
				span.Fill(value);
			}
			else
			{
				var spanV = MemoryMarshal.Cast<int, Vector<int>>(span);
				spanV.Fill(new Vector<int>(value));
				var spanR = span.Slice(spanV.Length * Vector<int>.Count);
				spanR.Fill(value);
			}
        }
		/// <summary>
        /// Fills the specified memory region faster, with the given <paramref name="value"/>.
        /// </summary>
        /// <param name="span">The span to fill.</param>
        /// <param name="value">The value to fill with.</param>
        [MethodImpl(OptimizationUtils.InlineAndOptimizeIfPossible)]
#if DEBUG_SPANEXT_TT_NON_USER_CODE
        [DebuggerStepThrough]
#endif
        public static void FastFill(this Span<long> span, long value = default)
        {
            if(Vector<long>.Count > span.Length)
			{
				span.Fill(value);
			}
			else
			{
				var spanV = MemoryMarshal.Cast<long, Vector<long>>(span);
				spanV.Fill(new Vector<long>(value));
				var spanR = span.Slice(spanV.Length * Vector<long>.Count);
				spanR.Fill(value);
			}
        }
#endregion FastFill
#region Extensions for Span<T>
        /// <summary>
        /// Slices the <paramref name="span"/> aligned with the multiple of <paramref name="channels"/>.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="span">The <see cref="Span{T}"/> to slice.</param>
        /// <param name="channels">The align width.</param>
        /// <returns></returns>
        [MethodImpl(OptimizationUtils.InlineAndOptimizeIfPossible)]
        public static Span<T> SliceAlign<T>(this Span<T> span, int channels) => span.Slice(0, MathI.FloorStep(span.Length, channels));

        /// <summary>
        /// Slices the <paramref name="span"/> aligned with the multiple of <paramref name="channelsDivisor"/>.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="span">The <see cref="Span{T}"/> to slice.</param>
        /// <param name="channelsDivisor">The divisor set to align width.</param>
        /// <returns></returns>
        [MethodImpl(OptimizationUtils.InlineAndOptimizeIfPossible)]
        public static Span<T> SliceAlign<T>(this Span<T> span, UInt32Divisor channelsDivisor) => span.Slice(0, (int)channelsDivisor.Floor((uint)span.Length));

        /// <summary>
        /// Slices the <paramref name="span"/> with the specified length.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="span">The <see cref="Span{T}"/> to slice.</param>
        /// <param name="length">The length to read.</param>
        /// <returns></returns>
        [MethodImpl(OptimizationUtils.InlineAndOptimizeIfPossible)]
        public static Span<T> SliceWhile<T>(this Span<T> span, int length) => span.Slice(0, length);

        /// <summary>
        /// Slices the <paramref name="span"/> with the length of specified <paramref name="criterion"/>.
        /// </summary>
        /// <typeparam name="T">The type of <see cref="Span{T}"/></typeparam>
        /// <param name="span">The <see cref="Span{T}"/> to slice.</param>
        /// <param name="criterion">The criterion.</param>
        /// <returns></returns>
        [MethodImpl(OptimizationUtils.InlineAndOptimizeIfPossible)]
        public static Span<T> AlignWith<T>(this Span<T> span, Span<T> criterion) => span.Slice(0, criterion.Length);


        /// <summary>
        /// Slices the <paramref name="span"/> with the length of specified <paramref name="criterion"/>.
        /// </summary>
        /// <typeparam name="T">The type of <see cref="Span{T}"/></typeparam>
        /// <param name="span">The <see cref="Span{T}"/> to slice.</param>
        /// <param name="criterion">The criterion.</param>
        /// <returns></returns>
        [MethodImpl(OptimizationUtils.InlineAndOptimizeIfPossible)]
        public static Span<T> AlignWith<T>(this Span<T> span, Memory<T> criterion) => span.Slice(0, criterion.Length);


        /// <summary>
        /// Slices the <paramref name="span"/> with the length of specified <paramref name="criterion"/>.
        /// </summary>
        /// <typeparam name="T">The type of <see cref="Span{T}"/></typeparam>
        /// <param name="span">The <see cref="Span{T}"/> to slice.</param>
        /// <param name="criterion">The criterion.</param>
        /// <returns></returns>
        [MethodImpl(OptimizationUtils.InlineAndOptimizeIfPossible)]
        public static Span<T> AlignWith<T>(this Span<T> span, ReadOnlySpan<T> criterion) => span.Slice(0, criterion.Length);


        /// <summary>
        /// Slices the <paramref name="span"/> with the length of specified <paramref name="criterion"/>.
        /// </summary>
        /// <typeparam name="T">The type of <see cref="Span{T}"/></typeparam>
        /// <param name="span">The <see cref="Span{T}"/> to slice.</param>
        /// <param name="criterion">The criterion.</param>
        /// <returns></returns>
        [MethodImpl(OptimizationUtils.InlineAndOptimizeIfPossible)]
        public static Span<T> AlignWith<T>(this Span<T> span, ReadOnlyMemory<T> criterion) => span.Slice(0, criterion.Length);

#endregion Extensions for Span<T>
#region Extensions for Memory<T>
        /// <summary>
        /// Slices the <paramref name="memory"/> aligned with the multiple of <paramref name="channels"/>.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="memory">The <see cref="Memory{T}"/> to slice.</param>
        /// <param name="channels">The align width.</param>
        /// <returns></returns>
        [MethodImpl(OptimizationUtils.InlineAndOptimizeIfPossible)]
        public static Memory<T> SliceAlign<T>(this Memory<T> memory, int channels) => memory.Slice(0, MathI.FloorStep(memory.Length, channels));

        /// <summary>
        /// Slices the <paramref name="memory"/> aligned with the multiple of <paramref name="channelsDivisor"/>.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="memory">The <see cref="Memory{T}"/> to slice.</param>
        /// <param name="channelsDivisor">The divisor set to align width.</param>
        /// <returns></returns>
        [MethodImpl(OptimizationUtils.InlineAndOptimizeIfPossible)]
        public static Memory<T> SliceAlign<T>(this Memory<T> memory, UInt32Divisor channelsDivisor) => memory.Slice(0, (int)channelsDivisor.Floor((uint)memory.Length));

        /// <summary>
        /// Slices the <paramref name="memory"/> with the specified length.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="memory">The <see cref="Memory{T}"/> to slice.</param>
        /// <param name="length">The length to read.</param>
        /// <returns></returns>
        [MethodImpl(OptimizationUtils.InlineAndOptimizeIfPossible)]
        public static Memory<T> SliceWhile<T>(this Memory<T> memory, int length) => memory.Slice(0, length);

        /// <summary>
        /// Slices the <paramref name="memory"/> with the length of specified <paramref name="criterion"/>.
        /// </summary>
        /// <typeparam name="T">The type of <see cref="Memory{T}"/></typeparam>
        /// <param name="memory">The <see cref="Memory{T}"/> to slice.</param>
        /// <param name="criterion">The criterion.</param>
        /// <returns></returns>
        [MethodImpl(OptimizationUtils.InlineAndOptimizeIfPossible)]
        public static Memory<T> AlignWith<T>(this Memory<T> memory, Span<T> criterion) => memory.Slice(0, criterion.Length);


        /// <summary>
        /// Slices the <paramref name="memory"/> with the length of specified <paramref name="criterion"/>.
        /// </summary>
        /// <typeparam name="T">The type of <see cref="Memory{T}"/></typeparam>
        /// <param name="memory">The <see cref="Memory{T}"/> to slice.</param>
        /// <param name="criterion">The criterion.</param>
        /// <returns></returns>
        [MethodImpl(OptimizationUtils.InlineAndOptimizeIfPossible)]
        public static Memory<T> AlignWith<T>(this Memory<T> memory, Memory<T> criterion) => memory.Slice(0, criterion.Length);


        /// <summary>
        /// Slices the <paramref name="memory"/> with the length of specified <paramref name="criterion"/>.
        /// </summary>
        /// <typeparam name="T">The type of <see cref="Memory{T}"/></typeparam>
        /// <param name="memory">The <see cref="Memory{T}"/> to slice.</param>
        /// <param name="criterion">The criterion.</param>
        /// <returns></returns>
        [MethodImpl(OptimizationUtils.InlineAndOptimizeIfPossible)]
        public static Memory<T> AlignWith<T>(this Memory<T> memory, ReadOnlySpan<T> criterion) => memory.Slice(0, criterion.Length);


        /// <summary>
        /// Slices the <paramref name="memory"/> with the length of specified <paramref name="criterion"/>.
        /// </summary>
        /// <typeparam name="T">The type of <see cref="Memory{T}"/></typeparam>
        /// <param name="memory">The <see cref="Memory{T}"/> to slice.</param>
        /// <param name="criterion">The criterion.</param>
        /// <returns></returns>
        [MethodImpl(OptimizationUtils.InlineAndOptimizeIfPossible)]
        public static Memory<T> AlignWith<T>(this Memory<T> memory, ReadOnlyMemory<T> criterion) => memory.Slice(0, criterion.Length);

#endregion Extensions for Memory<T>
#region Extensions for ReadOnlySpan<T>
        /// <summary>
        /// Slices the <paramref name="readOnlySpan"/> aligned with the multiple of <paramref name="channels"/>.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="readOnlySpan">The <see cref="ReadOnlySpan{T}"/> to slice.</param>
        /// <param name="channels">The align width.</param>
        /// <returns></returns>
        [MethodImpl(OptimizationUtils.InlineAndOptimizeIfPossible)]
        public static ReadOnlySpan<T> SliceAlign<T>(this ReadOnlySpan<T> readOnlySpan, int channels) => readOnlySpan.Slice(0, MathI.FloorStep(readOnlySpan.Length, channels));

        /// <summary>
        /// Slices the <paramref name="readOnlySpan"/> aligned with the multiple of <paramref name="channelsDivisor"/>.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="readOnlySpan">The <see cref="ReadOnlySpan{T}"/> to slice.</param>
        /// <param name="channelsDivisor">The divisor set to align width.</param>
        /// <returns></returns>
        [MethodImpl(OptimizationUtils.InlineAndOptimizeIfPossible)]
        public static ReadOnlySpan<T> SliceAlign<T>(this ReadOnlySpan<T> readOnlySpan, UInt32Divisor channelsDivisor) => readOnlySpan.Slice(0, (int)channelsDivisor.Floor((uint)readOnlySpan.Length));

        /// <summary>
        /// Slices the <paramref name="readOnlySpan"/> with the specified length.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="readOnlySpan">The <see cref="ReadOnlySpan{T}"/> to slice.</param>
        /// <param name="length">The length to read.</param>
        /// <returns></returns>
        [MethodImpl(OptimizationUtils.InlineAndOptimizeIfPossible)]
        public static ReadOnlySpan<T> SliceWhile<T>(this ReadOnlySpan<T> readOnlySpan, int length) => readOnlySpan.Slice(0, length);

        /// <summary>
        /// Slices the <paramref name="readOnlySpan"/> with the length of specified <paramref name="criterion"/>.
        /// </summary>
        /// <typeparam name="T">The type of <see cref="ReadOnlySpan{T}"/></typeparam>
        /// <param name="readOnlySpan">The <see cref="ReadOnlySpan{T}"/> to slice.</param>
        /// <param name="criterion">The criterion.</param>
        /// <returns></returns>
        [MethodImpl(OptimizationUtils.InlineAndOptimizeIfPossible)]
        public static ReadOnlySpan<T> AlignWith<T>(this ReadOnlySpan<T> readOnlySpan, Span<T> criterion) => readOnlySpan.Slice(0, criterion.Length);


        /// <summary>
        /// Slices the <paramref name="readOnlySpan"/> with the length of specified <paramref name="criterion"/>.
        /// </summary>
        /// <typeparam name="T">The type of <see cref="ReadOnlySpan{T}"/></typeparam>
        /// <param name="readOnlySpan">The <see cref="ReadOnlySpan{T}"/> to slice.</param>
        /// <param name="criterion">The criterion.</param>
        /// <returns></returns>
        [MethodImpl(OptimizationUtils.InlineAndOptimizeIfPossible)]
        public static ReadOnlySpan<T> AlignWith<T>(this ReadOnlySpan<T> readOnlySpan, Memory<T> criterion) => readOnlySpan.Slice(0, criterion.Length);


        /// <summary>
        /// Slices the <paramref name="readOnlySpan"/> with the length of specified <paramref name="criterion"/>.
        /// </summary>
        /// <typeparam name="T">The type of <see cref="ReadOnlySpan{T}"/></typeparam>
        /// <param name="readOnlySpan">The <see cref="ReadOnlySpan{T}"/> to slice.</param>
        /// <param name="criterion">The criterion.</param>
        /// <returns></returns>
        [MethodImpl(OptimizationUtils.InlineAndOptimizeIfPossible)]
        public static ReadOnlySpan<T> AlignWith<T>(this ReadOnlySpan<T> readOnlySpan, ReadOnlySpan<T> criterion) => readOnlySpan.Slice(0, criterion.Length);


        /// <summary>
        /// Slices the <paramref name="readOnlySpan"/> with the length of specified <paramref name="criterion"/>.
        /// </summary>
        /// <typeparam name="T">The type of <see cref="ReadOnlySpan{T}"/></typeparam>
        /// <param name="readOnlySpan">The <see cref="ReadOnlySpan{T}"/> to slice.</param>
        /// <param name="criterion">The criterion.</param>
        /// <returns></returns>
        [MethodImpl(OptimizationUtils.InlineAndOptimizeIfPossible)]
        public static ReadOnlySpan<T> AlignWith<T>(this ReadOnlySpan<T> readOnlySpan, ReadOnlyMemory<T> criterion) => readOnlySpan.Slice(0, criterion.Length);

#endregion Extensions for ReadOnlySpan<T>
#region Extensions for ReadOnlyMemory<T>
        /// <summary>
        /// Slices the <paramref name="readOnlyMemory"/> aligned with the multiple of <paramref name="channels"/>.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="readOnlyMemory">The <see cref="ReadOnlyMemory{T}"/> to slice.</param>
        /// <param name="channels">The align width.</param>
        /// <returns></returns>
        [MethodImpl(OptimizationUtils.InlineAndOptimizeIfPossible)]
        public static ReadOnlyMemory<T> SliceAlign<T>(this ReadOnlyMemory<T> readOnlyMemory, int channels) => readOnlyMemory.Slice(0, MathI.FloorStep(readOnlyMemory.Length, channels));

        /// <summary>
        /// Slices the <paramref name="readOnlyMemory"/> aligned with the multiple of <paramref name="channelsDivisor"/>.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="readOnlyMemory">The <see cref="ReadOnlyMemory{T}"/> to slice.</param>
        /// <param name="channelsDivisor">The divisor set to align width.</param>
        /// <returns></returns>
        [MethodImpl(OptimizationUtils.InlineAndOptimizeIfPossible)]
        public static ReadOnlyMemory<T> SliceAlign<T>(this ReadOnlyMemory<T> readOnlyMemory, UInt32Divisor channelsDivisor) => readOnlyMemory.Slice(0, (int)channelsDivisor.Floor((uint)readOnlyMemory.Length));

        /// <summary>
        /// Slices the <paramref name="readOnlyMemory"/> with the specified length.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="readOnlyMemory">The <see cref="ReadOnlyMemory{T}"/> to slice.</param>
        /// <param name="length">The length to read.</param>
        /// <returns></returns>
        [MethodImpl(OptimizationUtils.InlineAndOptimizeIfPossible)]
        public static ReadOnlyMemory<T> SliceWhile<T>(this ReadOnlyMemory<T> readOnlyMemory, int length) => readOnlyMemory.Slice(0, length);

        /// <summary>
        /// Slices the <paramref name="readOnlyMemory"/> with the length of specified <paramref name="criterion"/>.
        /// </summary>
        /// <typeparam name="T">The type of <see cref="ReadOnlyMemory{T}"/></typeparam>
        /// <param name="readOnlyMemory">The <see cref="ReadOnlyMemory{T}"/> to slice.</param>
        /// <param name="criterion">The criterion.</param>
        /// <returns></returns>
        [MethodImpl(OptimizationUtils.InlineAndOptimizeIfPossible)]
        public static ReadOnlyMemory<T> AlignWith<T>(this ReadOnlyMemory<T> readOnlyMemory, Span<T> criterion) => readOnlyMemory.Slice(0, criterion.Length);


        /// <summary>
        /// Slices the <paramref name="readOnlyMemory"/> with the length of specified <paramref name="criterion"/>.
        /// </summary>
        /// <typeparam name="T">The type of <see cref="ReadOnlyMemory{T}"/></typeparam>
        /// <param name="readOnlyMemory">The <see cref="ReadOnlyMemory{T}"/> to slice.</param>
        /// <param name="criterion">The criterion.</param>
        /// <returns></returns>
        [MethodImpl(OptimizationUtils.InlineAndOptimizeIfPossible)]
        public static ReadOnlyMemory<T> AlignWith<T>(this ReadOnlyMemory<T> readOnlyMemory, Memory<T> criterion) => readOnlyMemory.Slice(0, criterion.Length);


        /// <summary>
        /// Slices the <paramref name="readOnlyMemory"/> with the length of specified <paramref name="criterion"/>.
        /// </summary>
        /// <typeparam name="T">The type of <see cref="ReadOnlyMemory{T}"/></typeparam>
        /// <param name="readOnlyMemory">The <see cref="ReadOnlyMemory{T}"/> to slice.</param>
        /// <param name="criterion">The criterion.</param>
        /// <returns></returns>
        [MethodImpl(OptimizationUtils.InlineAndOptimizeIfPossible)]
        public static ReadOnlyMemory<T> AlignWith<T>(this ReadOnlyMemory<T> readOnlyMemory, ReadOnlySpan<T> criterion) => readOnlyMemory.Slice(0, criterion.Length);


        /// <summary>
        /// Slices the <paramref name="readOnlyMemory"/> with the length of specified <paramref name="criterion"/>.
        /// </summary>
        /// <typeparam name="T">The type of <see cref="ReadOnlyMemory{T}"/></typeparam>
        /// <param name="readOnlyMemory">The <see cref="ReadOnlyMemory{T}"/> to slice.</param>
        /// <param name="criterion">The criterion.</param>
        /// <returns></returns>
        [MethodImpl(OptimizationUtils.InlineAndOptimizeIfPossible)]
        public static ReadOnlyMemory<T> AlignWith<T>(this ReadOnlyMemory<T> readOnlyMemory, ReadOnlyMemory<T> criterion) => readOnlyMemory.Slice(0, criterion.Length);

#endregion Extensions for ReadOnlyMemory<T>
#region CopyTo alternatives
        /// <summary>
        /// Copies the contents of this <see cref="Memory{T}"/> into a destination <see cref="Span{T}"/>.
        /// </summary>
        /// <typeparam name="T">The type of destination <see cref="Span{T}"/></typeparam>
        /// <param name="source">The <see cref="Memory{T}"/> to copy from.</param>
        /// <param name="destination">The destination <see cref="Span{T}"/> object.</param>
        /// <returns></returns>
        [MethodImpl(OptimizationUtils.InlineAndOptimizeIfPossible)]
        public static void CopyTo<T>(this Memory<T> source, Span<T> destination) => source.Span.CopyTo(destination);

        /// <summary>
        /// Copies the contents of this <see cref="ReadOnlyMemory{T}"/> into a destination <see cref="Span{T}"/>.
        /// </summary>
        /// <typeparam name="T">The type of destination <see cref="Span{T}"/></typeparam>
        /// <param name="source">The <see cref="ReadOnlyMemory{T}"/> to copy from.</param>
        /// <param name="destination">The destination <see cref="Span{T}"/> object.</param>
        /// <returns></returns>
        [MethodImpl(OptimizationUtils.InlineAndOptimizeIfPossible)]
        public static void CopyTo<T>(this ReadOnlyMemory<T> source, Span<T> destination) => source.Span.CopyTo(destination);

        /// <summary>
        /// Copies the contents of this <see cref="Span{T}"/> into a destination <see cref="Memory{T}"/>.
        /// </summary>
        /// <typeparam name="T">The type of destination <see cref="Memory{T}"/></typeparam>
        /// <param name="source">The <see cref="Span{T}"/> to copy from.</param>
        /// <param name="destination">The destination <see cref="Memory{T}"/> object.</param>
        /// <returns></returns>
        [MethodImpl(OptimizationUtils.InlineAndOptimizeIfPossible)]
        public static void CopyTo<T>(this Span<T> source, Memory<T> destination) => source.CopyTo(destination.Span);

        /// <summary>
        /// Copies the contents of this <see cref="ReadOnlySpan{T}"/> into a destination <see cref="Memory{T}"/>.
        /// </summary>
        /// <typeparam name="T">The type of destination <see cref="Memory{T}"/></typeparam>
        /// <param name="source">The <see cref="ReadOnlySpan{T}"/> to copy from.</param>
        /// <param name="destination">The destination <see cref="Memory{T}"/> object.</param>
        /// <returns></returns>
        [MethodImpl(OptimizationUtils.InlineAndOptimizeIfPossible)]
        public static void CopyTo<T>(this ReadOnlySpan<T> source, Memory<T> destination) => source.CopyTo(destination.Span);

#endregion CopyTo alternatives
#region TryCopyTo alternatives
        /// <summary>
        /// Attempts to copy the current <see cref="Memory{T}"/> to a destination <see cref="Span{T}"/> and returns a value that indicates whether the copy operation succeeded.
        /// </summary>
        /// <typeparam name="T">The type of destination <see cref="Span{T}"/></typeparam>
        /// <param name="source">The <see cref="Memory{T}"/> to copy from.</param>
        /// <param name="destination">The target of the copy operation.</param>
        /// <returns><c>true</c> if the copy operation succeeded; otherwise, <c>false</c>.</returns>
        [MethodImpl(OptimizationUtils.InlineAndOptimizeIfPossible)]
        public static bool TryCopyTo<T>(this Memory<T> source, Span<T> destination) => source.Span.TryCopyTo(destination);

        /// <summary>
        /// Attempts to copy the current <see cref="ReadOnlyMemory{T}"/> to a destination <see cref="Span{T}"/> and returns a value that indicates whether the copy operation succeeded.
        /// </summary>
        /// <typeparam name="T">The type of destination <see cref="Span{T}"/></typeparam>
        /// <param name="source">The <see cref="ReadOnlyMemory{T}"/> to copy from.</param>
        /// <param name="destination">The target of the copy operation.</param>
        /// <returns><c>true</c> if the copy operation succeeded; otherwise, <c>false</c>.</returns>
        [MethodImpl(OptimizationUtils.InlineAndOptimizeIfPossible)]
        public static bool TryCopyTo<T>(this ReadOnlyMemory<T> source, Span<T> destination) => source.Span.TryCopyTo(destination);

        /// <summary>
        /// Copies the contents of current <see cref="Span{T}"/> into a destination <see cref="Memory{T}"/>.
        /// </summary>
        /// <typeparam name="T">The type of destination <see cref="Memory{T}"/></typeparam>
        /// <param name="source">The <see cref="Span{T}"/> to copy from.</param>
        /// <param name="destination">The target of the copy operation.</param>
        /// <returns></returns>
        [MethodImpl(OptimizationUtils.InlineAndOptimizeIfPossible)]
        public static bool TryCopyTo<T>(this Span<T> source, Memory<T> destination) => source.TryCopyTo(destination.Span);

        /// <summary>
        /// Copies the contents of current <see cref="ReadOnlySpan{T}"/> into a destination <see cref="Memory{T}"/>.
        /// </summary>
        /// <typeparam name="T">The type of destination <see cref="Memory{T}"/></typeparam>
        /// <param name="source">The <see cref="ReadOnlySpan{T}"/> to copy from.</param>
        /// <param name="destination">The target of the copy operation.</param>
        /// <returns></returns>
        [MethodImpl(OptimizationUtils.InlineAndOptimizeIfPossible)]
        public static bool TryCopyTo<T>(this ReadOnlySpan<T> source, Memory<T> destination) => source.TryCopyTo(destination.Span);

#endregion TryCopyTo alternatives
    }
}
