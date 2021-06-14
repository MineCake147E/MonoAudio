﻿#if NETCOREAPP3_1_OR_GREATER

#region License notice

/* libFLAC - Free Lossless Audio Codec library
 * Copyright (C) 2000-2009  Josh Coalson
 * Copyright (C) 2011-2018  Xiph.Org Foundation
 *
 * Redistribution and use in source and binary forms, with or without
 * modification, are permitted provided that the following conditions
 * are met:
 *
 * - Redistributions of source code must retain the above copyright
 * notice, this list of conditions and the following disclaimer.
 *
 * - Redistributions in binary form must reproduce the above copyright
 * notice, this list of conditions and the following disclaimer in the
 * documentation and/or other materials provided with the distribution.
 *
 * - Neither the name of the Xiph.org Foundation nor the names of its
 * contributors may be used to endorse or promote products derived from
 * this software without specific prior written permission.
 *
 * THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS
 * ``AS IS'' AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT
 * LIMITED TO, THE IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR
 * A PARTICULAR PURPOSE ARE DISCLAIMED.  IN NO EVENT SHALL THE FOUNDATION OR
 * CONTRIBUTORS BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL,
 * EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT LIMITED TO,
 * PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES; LOSS OF USE, DATA, OR
 * PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF
 * LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING
 * NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS
 * SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
 */

#endregion License notice

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;
using System.Text;
using System.Threading.Tasks;

namespace Shamisen.Codecs.Flac.SubFrames
{
    public sealed partial class FlacLinearPredictionSubFrame
    {
        internal static partial class X86
        {
#region Order3
            [MethodImpl(OptimizationUtils.InlineAndOptimizeIfPossible)]
            internal static bool RestoreSignalOrder3Wide(int shiftsNeeded, ReadOnlySpan<int> residual, ReadOnlySpan<int> coeffs, Span<int> output)
            {
                if (Sse41.IsSupported)
                {
                    RestoreSignalOrder3WideSse41(shiftsNeeded, residual, coeffs, output);
                    return true;
                }
                return false;
            }

            [MethodImpl(OptimizationUtils.InlineAndOptimizeIfPossible)]
            internal static unsafe void RestoreSignalOrder3WideSse41(int shiftsNeeded, ReadOnlySpan<int> residual, ReadOnlySpan<int> coeffs, Span<int> output)
            {
                const int Order = 3;
                if(coeffs.Length < Order) return;
                _ = coeffs[Order - 1];
                var vshift = Vector128.CreateScalar((long)shiftsNeeded);
                ref var c = ref MemoryMarshal.GetReference(coeffs);
                ref var o = ref MemoryMarshal.GetReference(output);
                ref var d = ref Unsafe.Add(ref o, Order);
                int dataLength = output.Length - Order;
                ref var r = ref MemoryMarshal.GetReference(residual);
                Vector128<long> sum;
                var vcoeff0 = Sse41.ConvertToVector128Int64(Sse2.LoadScalarVector128((ulong*)Unsafe.AsPointer(ref Unsafe.Add(ref c, 0))).AsUInt32()).AsInt32();
                var vcoeff1 = Sse41.ConvertToVector128Int64(Sse2.LoadScalarVector128((uint*)Unsafe.AsPointer(ref Unsafe.Add(ref c, 2))).AsUInt32()).AsInt32();
                var vprev0 = Sse2.Shuffle(Sse2.LoadScalarVector128((ulong*)Unsafe.AsPointer(ref Unsafe.Add(ref o, 1))).AsInt32(), 0b11_00_11_01);
                var vprev1 = Sse2.Shuffle(Sse2.LoadScalarVector128((ulong*)Unsafe.AsPointer(ref Unsafe.Add(ref o, 0))).AsInt32(), 0b11_00_11_00);
                for(nint i = 0; i < dataLength;)
                {
                    var res = Vector128.CreateScalar(Unsafe.Add(ref r, i));
                    sum = Sse41.Multiply(vcoeff0, vprev0);
                    sum = Sse2.Add(sum, Sse41.Multiply(vcoeff1, vprev1));
                    sum = Sse2.Add(sum, Sse2.ShiftRightLogical128BitLane(sum, 8));
                    sum = Sse2.ShiftRightLogical(sum, vshift);
                    var yy = Sse2.Add(sum.AsInt32(), res);
#if NET5_0_OR_GREATER
                    Sse2.StoreScalar((int*)Unsafe.AsPointer(ref Unsafe.Add(ref d, i)), yy);
#else
                    Unsafe.Add(ref d, i) = yy.GetElement(0);
#endif
                    i++;
                    yy = Sse2.ShiftLeftLogical128BitLane(yy, 8);
                    vprev1 = Ssse3.AlignRight(vprev1, vprev0, 8);
                    vprev0 = Ssse3.AlignRight(vprev0, yy, 8);
                }
            }
            
            [MethodImpl(OptimizationUtils.InlineAndOptimizeIfPossible)]
            internal static unsafe void RestoreSignalOrder3WideAvx2(int shiftsNeeded, ReadOnlySpan<int> residual, ReadOnlySpan<int> coeffs, Span<int> output)
            {
                const int Order = 3;
                if(coeffs.Length < Order) return;
                _ = coeffs[Order - 1];
                var vshift = Vector128.CreateScalar((long)shiftsNeeded);
                var mask = Vector128.Create(~0, ~0, ~0, 0);
                ref var c = ref MemoryMarshal.GetReference(coeffs);
                ref var o = ref MemoryMarshal.GetReference(output);
                ref var d = ref Unsafe.Add(ref o, Order);
                int dataLength = output.Length - Order;
                ref var r = ref MemoryMarshal.GetReference(residual);
                Vector128<long> sum;
                Vector256<long> sum256;
                var vcoeff0 = Avx2.ConvertToVector256Int64(Avx2.MaskLoad((int*)Unsafe.AsPointer(ref Unsafe.Add(ref c, 0)), mask)).AsInt32();
                var vprev0 = Avx2.ConvertToVector256Int64(Sse2.Shuffle(Avx2.MaskLoad((int*)Unsafe.AsPointer(ref o), mask).AsInt32(), 0b00_01_10_11)).AsInt32();
                for(nint i = 0; i < dataLength;)
                {
                    var res = Vector128.CreateScalar(Unsafe.Add(ref r, i));
                    sum256 = Avx2.Multiply(vcoeff0, vprev0);
                    sum = Sse2.Add(sum256.GetLower(), sum256.GetUpper());
                    sum = Sse2.Add(sum, Sse2.ShiftRightLogical128BitLane(sum, 8));
                    sum = Sse2.ShiftRightLogical(sum, vshift);
                    var yy = Sse2.Add(sum.AsInt32(), res);
#if NET5_0_OR_GREATER
                    Sse2.StoreScalar((int*)Unsafe.AsPointer(ref Unsafe.Add(ref d, i)), yy);
#else
                    Unsafe.Add(ref d, i) = yy.GetElement(0);
#endif
                    i++;
                    var yu = yy.ToVector256();
                    yu = Avx2.Permute4x64(yu.AsUInt64(), 0b00_00_00_00).AsInt32();
                    vprev0 = Avx2.Permute4x64(vprev0.AsInt64(), 0b10_01_00_11).AsInt32();
                    vprev0 = Avx2.Blend(vprev0, yu, 0b0000_0001);
                }
            }
#endregion Order3
#region Order5

            [MethodImpl(OptimizationUtils.InlineAndOptimizeIfPossible)]
            internal static bool RestoreSignalOrder5(int shiftsNeeded, ReadOnlySpan<int> residual, ReadOnlySpan<int> coeffs, Span<int> output)
            {
                if (Sse41.IsSupported)
                {
                    RestoreSignalOrder5Sse41(shiftsNeeded, residual, coeffs, output);
                    return true;
                }
                return false;
            }

            [MethodImpl(OptimizationUtils.InlineAndOptimizeIfPossible)]
            internal static unsafe void RestoreSignalOrder5Sse41(int shiftsNeeded, ReadOnlySpan<int> residual, ReadOnlySpan<int> coeffs, Span<int> output)
            {
                const int Order = 5;
                if (coeffs.Length < Order) return;
                _ = coeffs[Order - 1];
                var vshift = Vector128.CreateScalar(shiftsNeeded);
                ref var o = ref MemoryMarshal.GetReference(output);
                ref var d = ref Unsafe.Add(ref o, Order);
                ref var r = ref MemoryMarshal.GetReference(residual);
                ref var c = ref MemoryMarshal.GetReference(coeffs);
                Vector128<int> sum;
                var vzero = Vector128.Create(0);
                nint dataLength = output.Length - Order;
                var vcoeff0 = Sse2.LoadVector128((int*)Unsafe.AsPointer(ref Unsafe.Add(ref c, 0)));
                var vcoeff1 = Vector128.Create(Unsafe.Add(ref c, 4), 0, 0, 0);
                var vprev0 = Sse2.Shuffle(Sse2.LoadVector128((int*)Unsafe.AsPointer(ref Unsafe.Add(ref o, 1))).AsInt32(), 0b00_01_10_11);
                var vprev1 = Vector128.Create(Unsafe.Add(ref o, 0), 0, 0, 0);
                for (nint i = 0; i < dataLength; i++)
                {
                    var res = Vector128.CreateScalar(Unsafe.Add(ref r, i));
                    sum = Sse41.MultiplyLow(vcoeff0, vprev0);
                    sum = Sse2.Add(sum, Sse41.MultiplyLow(vcoeff1, vprev1));
                    var y = Ssse3.HorizontalAdd(sum, vzero);
                    y = Ssse3.HorizontalAdd(y, vzero);
                    y = Sse2.ShiftRightArithmetic(y, vshift);   //C# shift operator handling sucks so SSE2 is used instead(same latency, same throughput).
                    y = Sse2.Add(y, res);   //Avoids extract and insert
#if NET5_0_OR_GREATER
                    Sse2.StoreScalar((int*)Unsafe.AsPointer(ref Unsafe.Add(ref d, i)), y);
#else
                    Unsafe.Add(ref d, i) = y.GetElement(0);
#endif
                    y = Sse2.ShiftLeftLogical128BitLane(y, 12);
                    vprev1 = Ssse3.AlignRight(vprev1, vprev0, 12);
                    vprev0 = Ssse3.AlignRight(vprev0, y, 12);
                }
            }

            [MethodImpl(OptimizationUtils.InlineAndOptimizeIfPossible)]
            internal static bool RestoreSignalOrder5Wide(int shiftsNeeded, ReadOnlySpan<int> residual, ReadOnlySpan<int> coeffs, Span<int> output)
            {
                if (Sse41.IsSupported)
                {
                    RestoreSignalOrder5WideSse41(shiftsNeeded, residual, coeffs, output);
                    return true;
                }
                return false;
            }

            [MethodImpl(OptimizationUtils.InlineAndOptimizeIfPossible)]
            internal static unsafe void RestoreSignalOrder5WideSse41(int shiftsNeeded, ReadOnlySpan<int> residual, ReadOnlySpan<int> coeffs, Span<int> output)
            {
                const int Order = 5;
                if(coeffs.Length < Order) return;
                _ = coeffs[Order - 1];
                var vshift = Vector128.CreateScalar((long)shiftsNeeded);
                ref var c = ref MemoryMarshal.GetReference(coeffs);
                ref var o = ref MemoryMarshal.GetReference(output);
                ref var d = ref Unsafe.Add(ref o, Order);
                int dataLength = output.Length - Order;
                ref var r = ref MemoryMarshal.GetReference(residual);
                Vector128<long> sum;
                var vcoeff0 = Sse41.ConvertToVector128Int64(Sse2.LoadScalarVector128((ulong*)Unsafe.AsPointer(ref Unsafe.Add(ref c, 0))).AsUInt32()).AsInt32();
                var vcoeff1 = Sse41.ConvertToVector128Int64(Sse2.LoadScalarVector128((ulong*)Unsafe.AsPointer(ref Unsafe.Add(ref c, 2))).AsUInt32()).AsInt32();
                var vcoeff2 = Sse41.ConvertToVector128Int64(Sse2.LoadScalarVector128((uint*)Unsafe.AsPointer(ref Unsafe.Add(ref c, 4))).AsUInt32()).AsInt32();
                var vprev0 = Sse2.Shuffle(Sse2.LoadScalarVector128((ulong*)Unsafe.AsPointer(ref Unsafe.Add(ref o, 3))).AsInt32(), 0b11_00_11_01);
                var vprev1 = Sse2.Shuffle(Sse2.LoadScalarVector128((ulong*)Unsafe.AsPointer(ref Unsafe.Add(ref o, 1))).AsInt32(), 0b11_00_11_01);
                var vprev2 = Sse2.Shuffle(Sse2.LoadScalarVector128((ulong*)Unsafe.AsPointer(ref Unsafe.Add(ref o, 0))).AsInt32(), 0b11_00_11_00);
                for(nint i = 0; i < dataLength;)
                {
                    var res = Vector128.CreateScalar(Unsafe.Add(ref r, i));
                    sum = Sse41.Multiply(vcoeff0, vprev0);
                    sum = Sse2.Add(sum, Sse41.Multiply(vcoeff1, vprev1));
                    sum = Sse2.Add(sum, Sse41.Multiply(vcoeff2, vprev2));
                    sum = Sse2.Add(sum, Sse2.ShiftRightLogical128BitLane(sum, 8));
                    sum = Sse2.ShiftRightLogical(sum, vshift);
                    var yy = Sse2.Add(sum.AsInt32(), res);
#if NET5_0_OR_GREATER
                    Sse2.StoreScalar((int*)Unsafe.AsPointer(ref Unsafe.Add(ref d, i)), yy);
#else
                    Unsafe.Add(ref d, i) = yy.GetElement(0);
#endif
                    i++;
                    yy = Sse2.ShiftLeftLogical128BitLane(yy, 8);
                    vprev2 = Ssse3.AlignRight(vprev2, vprev1, 8);
                    vprev1 = Ssse3.AlignRight(vprev1, vprev0, 8);
                    vprev0 = Ssse3.AlignRight(vprev0, yy, 8);
                }
            }
            
            [MethodImpl(OptimizationUtils.InlineAndOptimizeIfPossible)]
            internal static unsafe void RestoreSignalOrder5WideAvx2(int shiftsNeeded, ReadOnlySpan<int> residual, ReadOnlySpan<int> coeffs, Span<int> output)
            {
                const int Order = 5;
                if(coeffs.Length < Order) return;
                _ = coeffs[Order - 1];
                var vshift = Vector128.CreateScalar((long)shiftsNeeded);
                ref var c = ref MemoryMarshal.GetReference(coeffs);
                ref var o = ref MemoryMarshal.GetReference(output);
                ref var d = ref Unsafe.Add(ref o, Order);
                int dataLength = output.Length - Order;
                ref var r = ref MemoryMarshal.GetReference(residual);
                Vector128<long> sum;
                Vector256<long> sum256;
                var vcoeff0 = Avx2.ConvertToVector256Int64(Sse2.LoadVector128((int*)Unsafe.AsPointer(ref Unsafe.Add(ref c, 0))).AsUInt32()).AsInt32();
                var vcoeff1 = Avx2.ConvertToVector256Int64(Sse2.LoadScalarVector128((uint*)Unsafe.AsPointer(ref Unsafe.Add(ref c, 4))).AsUInt32()).AsInt32();
                var vprev0 = Avx2.ConvertToVector256Int64(Sse2.Shuffle(Sse2.LoadScalarVector128((ulong*)Unsafe.AsPointer(ref Unsafe.Add(ref o, 2))).AsInt32(), 0b00_01_10_11).AsUInt32()).AsInt32();
                var vprev1 = Avx2.ConvertToVector256Int64(Sse2.Shuffle(Sse2.LoadScalarVector128((uint*)Unsafe.AsPointer(ref o)).AsInt32(), 0b00_01_10_11).AsUInt32()).AsInt32();
                for(nint i = 0; i < dataLength;)
                {
                    var res = Vector128.CreateScalar(Unsafe.Add(ref r, i));
                    sum256 = Avx2.Multiply(vcoeff0, vprev0);
                    sum256 = Avx2.Add(sum256, Avx2.Multiply(vcoeff1, vprev1));
                    sum = Sse2.Add(sum256.GetLower(), sum256.GetUpper());
                    sum = Sse2.Add(sum, Sse2.ShiftRightLogical128BitLane(sum, 8));
                    sum = Sse2.ShiftRightLogical(sum, vshift);
                    var yy = Sse2.Add(sum.AsInt32(), res);
#if NET5_0_OR_GREATER
                    Sse2.StoreScalar((int*)Unsafe.AsPointer(ref Unsafe.Add(ref d, i)), yy);
#else
                    Unsafe.Add(ref d, i) = yy.GetElement(0);
#endif
                    i++;
                    var yu = yy.ToVector256();
                    yu = Avx2.Permute4x64(yu.AsUInt64(), 0b00_00_00_00).AsInt32();
                    vprev1 = Avx2.Permute4x64(vprev1.AsInt64(), 0b10_01_00_11).AsInt32();
                    vprev1 = Avx2.Blend(vprev1, vprev0, 0b0000_0001);
                    vprev0 = Avx2.Permute4x64(vprev0.AsInt64(), 0b10_01_00_11).AsInt32();
                    vprev0 = Avx2.Blend(vprev0, yu, 0b0000_0001);
                }
            }
#endregion Order5
#region Order6

            [MethodImpl(OptimizationUtils.InlineAndOptimizeIfPossible)]
            internal static bool RestoreSignalOrder6(int shiftsNeeded, ReadOnlySpan<int> residual, ReadOnlySpan<int> coeffs, Span<int> output)
            {
                if (Sse41.IsSupported)
                {
                    RestoreSignalOrder6Sse41(shiftsNeeded, residual, coeffs, output);
                    return true;
                }
                return false;
            }

            [MethodImpl(OptimizationUtils.InlineAndOptimizeIfPossible)]
            internal static unsafe void RestoreSignalOrder6Sse41(int shiftsNeeded, ReadOnlySpan<int> residual, ReadOnlySpan<int> coeffs, Span<int> output)
            {
                const int Order = 6;
                if (coeffs.Length < Order) return;
                _ = coeffs[Order - 1];
                var vshift = Vector128.CreateScalar(shiftsNeeded);
                ref var o = ref MemoryMarshal.GetReference(output);
                ref var d = ref Unsafe.Add(ref o, Order);
                ref var r = ref MemoryMarshal.GetReference(residual);
                ref var c = ref MemoryMarshal.GetReference(coeffs);
                Vector128<int> sum;
                var vzero = Vector128.Create(0);
                nint dataLength = output.Length - Order;
                var vcoeff0 = Sse2.LoadVector128((int*)Unsafe.AsPointer(ref Unsafe.Add(ref c, 0)));
                var vcoeff1 = Vector128.Create(Unsafe.Add(ref c, 4), Unsafe.Add(ref c, 5), 0, 0);
                var vprev0 = Sse2.Shuffle(Sse2.LoadVector128((int*)Unsafe.AsPointer(ref Unsafe.Add(ref o, 2))).AsInt32(), 0b00_01_10_11);
                var vprev1 = Vector128.Create(Unsafe.Add(ref o, 1), Unsafe.Add(ref o, 0), 0, 0);
                for (nint i = 0; i < dataLength; i++)
                {
                    var res = Vector128.CreateScalar(Unsafe.Add(ref r, i));
                    sum = Sse41.MultiplyLow(vcoeff0, vprev0);
                    sum = Sse2.Add(sum, Sse41.MultiplyLow(vcoeff1, vprev1));
                    var y = Ssse3.HorizontalAdd(sum, vzero);
                    y = Ssse3.HorizontalAdd(y, vzero);
                    y = Sse2.ShiftRightArithmetic(y, vshift);   //C# shift operator handling sucks so SSE2 is used instead(same latency, same throughput).
                    y = Sse2.Add(y, res);   //Avoids extract and insert
#if NET5_0_OR_GREATER
                    Sse2.StoreScalar((int*)Unsafe.AsPointer(ref Unsafe.Add(ref d, i)), y);
#else
                    Unsafe.Add(ref d, i) = y.GetElement(0);
#endif
                    y = Sse2.ShiftLeftLogical128BitLane(y, 12);
                    vprev1 = Ssse3.AlignRight(vprev1, vprev0, 12);
                    vprev0 = Ssse3.AlignRight(vprev0, y, 12);
                }
            }

            [MethodImpl(OptimizationUtils.InlineAndOptimizeIfPossible)]
            internal static bool RestoreSignalOrder6Wide(int shiftsNeeded, ReadOnlySpan<int> residual, ReadOnlySpan<int> coeffs, Span<int> output)
            {
                if (Sse41.IsSupported)
                {
                    RestoreSignalOrder6WideSse41(shiftsNeeded, residual, coeffs, output);
                    return true;
                }
                return false;
            }

            [MethodImpl(OptimizationUtils.InlineAndOptimizeIfPossible)]
            internal static unsafe void RestoreSignalOrder6WideSse41(int shiftsNeeded, ReadOnlySpan<int> residual, ReadOnlySpan<int> coeffs, Span<int> output)
            {
                const int Order = 6;
                if(coeffs.Length < Order) return;
                _ = coeffs[Order - 1];
                var vshift = Vector128.CreateScalar((long)shiftsNeeded);
                ref var c = ref MemoryMarshal.GetReference(coeffs);
                ref var o = ref MemoryMarshal.GetReference(output);
                ref var d = ref Unsafe.Add(ref o, Order);
                int dataLength = output.Length - Order;
                ref var r = ref MemoryMarshal.GetReference(residual);
                Vector128<long> sum;
                var vcoeff0 = Sse41.ConvertToVector128Int64(Sse2.LoadScalarVector128((ulong*)Unsafe.AsPointer(ref Unsafe.Add(ref c, 0))).AsUInt32()).AsInt32();
                var vcoeff1 = Sse41.ConvertToVector128Int64(Sse2.LoadScalarVector128((ulong*)Unsafe.AsPointer(ref Unsafe.Add(ref c, 2))).AsUInt32()).AsInt32();
                var vcoeff2 = Sse41.ConvertToVector128Int64(Sse2.LoadScalarVector128((ulong*)Unsafe.AsPointer(ref Unsafe.Add(ref c, 4))).AsUInt32()).AsInt32();
                var vprev0 = Sse2.Shuffle(Sse2.LoadScalarVector128((ulong*)Unsafe.AsPointer(ref Unsafe.Add(ref o, 4))).AsInt32(), 0b11_00_11_01);
                var vprev1 = Sse2.Shuffle(Sse2.LoadScalarVector128((ulong*)Unsafe.AsPointer(ref Unsafe.Add(ref o, 2))).AsInt32(), 0b11_00_11_01);
                var vprev2 = Sse2.Shuffle(Sse2.LoadScalarVector128((ulong*)Unsafe.AsPointer(ref Unsafe.Add(ref o, 0))).AsInt32(), 0b11_00_11_01);
                for(nint i = 0; i < dataLength;)
                {
                    var res = Vector128.CreateScalar(Unsafe.Add(ref r, i));
                    sum = Sse41.Multiply(vcoeff0, vprev0);
                    sum = Sse2.Add(sum, Sse41.Multiply(vcoeff1, vprev1));
                    sum = Sse2.Add(sum, Sse41.Multiply(vcoeff2, vprev2));
                    sum = Sse2.Add(sum, Sse2.ShiftRightLogical128BitLane(sum, 8));
                    sum = Sse2.ShiftRightLogical(sum, vshift);
                    var yy = Sse2.Add(sum.AsInt32(), res);
#if NET5_0_OR_GREATER
                    Sse2.StoreScalar((int*)Unsafe.AsPointer(ref Unsafe.Add(ref d, i)), yy);
#else
                    Unsafe.Add(ref d, i) = yy.GetElement(0);
#endif
                    i++;
                    yy = Sse2.ShiftLeftLogical128BitLane(yy, 8);
                    vprev2 = Ssse3.AlignRight(vprev2, vprev1, 8);
                    vprev1 = Ssse3.AlignRight(vprev1, vprev0, 8);
                    vprev0 = Ssse3.AlignRight(vprev0, yy, 8);
                }
            }
            
            [MethodImpl(OptimizationUtils.InlineAndOptimizeIfPossible)]
            internal static unsafe void RestoreSignalOrder6WideAvx2(int shiftsNeeded, ReadOnlySpan<int> residual, ReadOnlySpan<int> coeffs, Span<int> output)
            {
                const int Order = 6;
                if(coeffs.Length < Order) return;
                _ = coeffs[Order - 1];
                var vshift = Vector128.CreateScalar((long)shiftsNeeded);
                ref var c = ref MemoryMarshal.GetReference(coeffs);
                ref var o = ref MemoryMarshal.GetReference(output);
                ref var d = ref Unsafe.Add(ref o, Order);
                int dataLength = output.Length - Order;
                ref var r = ref MemoryMarshal.GetReference(residual);
                Vector128<long> sum;
                Vector256<long> sum256;
                var vcoeff0 = Avx2.ConvertToVector256Int64(Sse2.LoadVector128((int*)Unsafe.AsPointer(ref Unsafe.Add(ref c, 0))).AsUInt32()).AsInt32();
                var vcoeff1 = Avx2.ConvertToVector256Int64(Sse2.LoadScalarVector128((ulong*)Unsafe.AsPointer(ref Unsafe.Add(ref c, 4))).AsUInt32()).AsInt32();
                var vprev0 = Avx2.ConvertToVector256Int64(Sse2.Shuffle(Sse2.LoadScalarVector128((ulong*)Unsafe.AsPointer(ref Unsafe.Add(ref o, 3))).AsInt32(), 0b00_01_10_11).AsUInt32()).AsInt32();
                var vprev1 = Avx2.ConvertToVector256Int64(Sse2.Shuffle(Sse2.LoadScalarVector128((ulong*)Unsafe.AsPointer(ref o)).AsInt32(), 0b00_01_10_11).AsUInt32()).AsInt32();
                for(nint i = 0; i < dataLength;)
                {
                    var res = Vector128.CreateScalar(Unsafe.Add(ref r, i));
                    sum256 = Avx2.Multiply(vcoeff0, vprev0);
                    sum256 = Avx2.Add(sum256, Avx2.Multiply(vcoeff1, vprev1));
                    sum = Sse2.Add(sum256.GetLower(), sum256.GetUpper());
                    sum = Sse2.Add(sum, Sse2.ShiftRightLogical128BitLane(sum, 8));
                    sum = Sse2.ShiftRightLogical(sum, vshift);
                    var yy = Sse2.Add(sum.AsInt32(), res);
#if NET5_0_OR_GREATER
                    Sse2.StoreScalar((int*)Unsafe.AsPointer(ref Unsafe.Add(ref d, i)), yy);
#else
                    Unsafe.Add(ref d, i) = yy.GetElement(0);
#endif
                    i++;
                    var yu = yy.ToVector256();
                    yu = Avx2.Permute4x64(yu.AsUInt64(), 0b00_00_00_00).AsInt32();
                    vprev1 = Avx2.Permute4x64(vprev1.AsInt64(), 0b10_01_00_11).AsInt32();
                    vprev1 = Avx2.Blend(vprev1, vprev0, 0b0000_0001);
                    vprev0 = Avx2.Permute4x64(vprev0.AsInt64(), 0b10_01_00_11).AsInt32();
                    vprev0 = Avx2.Blend(vprev0, yu, 0b0000_0001);
                }
            }
#endregion Order6
#region Order7

            [MethodImpl(OptimizationUtils.InlineAndOptimizeIfPossible)]
            internal static bool RestoreSignalOrder7(int shiftsNeeded, ReadOnlySpan<int> residual, ReadOnlySpan<int> coeffs, Span<int> output)
            {
                if (Sse41.IsSupported)
                {
                    RestoreSignalOrder7Sse41(shiftsNeeded, residual, coeffs, output);
                    return true;
                }
                return false;
            }

            [MethodImpl(OptimizationUtils.InlineAndOptimizeIfPossible)]
            internal static unsafe void RestoreSignalOrder7Sse41(int shiftsNeeded, ReadOnlySpan<int> residual, ReadOnlySpan<int> coeffs, Span<int> output)
            {
                const int Order = 7;
                if (coeffs.Length < Order) return;
                _ = coeffs[Order - 1];
                var vshift = Vector128.CreateScalar(shiftsNeeded);
                ref var o = ref MemoryMarshal.GetReference(output);
                ref var d = ref Unsafe.Add(ref o, Order);
                ref var r = ref MemoryMarshal.GetReference(residual);
                ref var c = ref MemoryMarshal.GetReference(coeffs);
                Vector128<int> sum;
                var vzero = Vector128.Create(0);
                nint dataLength = output.Length - Order;
                var vcoeff0 = Sse2.LoadVector128((int*)Unsafe.AsPointer(ref Unsafe.Add(ref c, 0)));
                var vcoeff1 = Vector128.Create(Unsafe.Add(ref c, 4), Unsafe.Add(ref c, 5), Unsafe.Add(ref c, 6), 0);
                var vprev0 = Sse2.Shuffle(Sse2.LoadVector128((int*)Unsafe.AsPointer(ref Unsafe.Add(ref o, 3))).AsInt32(), 0b00_01_10_11);
                var vprev1 = Vector128.Create(Unsafe.Add(ref o, 2), Unsafe.Add(ref o, 1), Unsafe.Add(ref o, 0), 0);
                for (nint i = 0; i < dataLength; i++)
                {
                    var res = Vector128.CreateScalar(Unsafe.Add(ref r, i));
                    sum = Sse41.MultiplyLow(vcoeff0, vprev0);
                    sum = Sse2.Add(sum, Sse41.MultiplyLow(vcoeff1, vprev1));
                    var y = Ssse3.HorizontalAdd(sum, vzero);
                    y = Ssse3.HorizontalAdd(y, vzero);
                    y = Sse2.ShiftRightArithmetic(y, vshift);   //C# shift operator handling sucks so SSE2 is used instead(same latency, same throughput).
                    y = Sse2.Add(y, res);   //Avoids extract and insert
#if NET5_0_OR_GREATER
                    Sse2.StoreScalar((int*)Unsafe.AsPointer(ref Unsafe.Add(ref d, i)), y);
#else
                    Unsafe.Add(ref d, i) = y.GetElement(0);
#endif
                    y = Sse2.ShiftLeftLogical128BitLane(y, 12);
                    vprev1 = Ssse3.AlignRight(vprev1, vprev0, 12);
                    vprev0 = Ssse3.AlignRight(vprev0, y, 12);
                }
            }

            [MethodImpl(OptimizationUtils.InlineAndOptimizeIfPossible)]
            internal static bool RestoreSignalOrder7Wide(int shiftsNeeded, ReadOnlySpan<int> residual, ReadOnlySpan<int> coeffs, Span<int> output)
            {
                if (Sse41.IsSupported)
                {
                    RestoreSignalOrder7WideSse41(shiftsNeeded, residual, coeffs, output);
                    return true;
                }
                return false;
            }

            [MethodImpl(OptimizationUtils.InlineAndOptimizeIfPossible)]
            internal static unsafe void RestoreSignalOrder7WideSse41(int shiftsNeeded, ReadOnlySpan<int> residual, ReadOnlySpan<int> coeffs, Span<int> output)
            {
                const int Order = 7;
                if(coeffs.Length < Order) return;
                _ = coeffs[Order - 1];
                var vshift = Vector128.CreateScalar((long)shiftsNeeded);
                ref var c = ref MemoryMarshal.GetReference(coeffs);
                ref var o = ref MemoryMarshal.GetReference(output);
                ref var d = ref Unsafe.Add(ref o, Order);
                int dataLength = output.Length - Order;
                ref var r = ref MemoryMarshal.GetReference(residual);
                Vector128<long> sum;
                var vcoeff0 = Sse41.ConvertToVector128Int64(Sse2.LoadScalarVector128((ulong*)Unsafe.AsPointer(ref Unsafe.Add(ref c, 0))).AsUInt32()).AsInt32();
                var vcoeff1 = Sse41.ConvertToVector128Int64(Sse2.LoadScalarVector128((ulong*)Unsafe.AsPointer(ref Unsafe.Add(ref c, 2))).AsUInt32()).AsInt32();
                var vcoeff2 = Sse41.ConvertToVector128Int64(Sse2.LoadScalarVector128((ulong*)Unsafe.AsPointer(ref Unsafe.Add(ref c, 4))).AsUInt32()).AsInt32();
                var vcoeff3 = Sse41.ConvertToVector128Int64(Sse2.LoadScalarVector128((uint*)Unsafe.AsPointer(ref Unsafe.Add(ref c, 6))).AsUInt32()).AsInt32();
                var vprev0 = Sse2.Shuffle(Sse2.LoadScalarVector128((ulong*)Unsafe.AsPointer(ref Unsafe.Add(ref o, 5))).AsInt32(), 0b11_00_11_01);
                var vprev1 = Sse2.Shuffle(Sse2.LoadScalarVector128((ulong*)Unsafe.AsPointer(ref Unsafe.Add(ref o, 3))).AsInt32(), 0b11_00_11_01);
                var vprev2 = Sse2.Shuffle(Sse2.LoadScalarVector128((ulong*)Unsafe.AsPointer(ref Unsafe.Add(ref o, 1))).AsInt32(), 0b11_00_11_01);
                var vprev3 = Sse2.Shuffle(Sse2.LoadScalarVector128((ulong*)Unsafe.AsPointer(ref Unsafe.Add(ref o, 0))).AsInt32(), 0b11_00_11_00);
                for(nint i = 0; i < dataLength;)
                {
                    var res = Vector128.CreateScalar(Unsafe.Add(ref r, i));
                    sum = Sse41.Multiply(vcoeff0, vprev0);
                    sum = Sse2.Add(sum, Sse41.Multiply(vcoeff1, vprev1));
                    sum = Sse2.Add(sum, Sse41.Multiply(vcoeff2, vprev2));
                    sum = Sse2.Add(sum, Sse41.Multiply(vcoeff3, vprev3));
                    sum = Sse2.Add(sum, Sse2.ShiftRightLogical128BitLane(sum, 8));
                    sum = Sse2.ShiftRightLogical(sum, vshift);
                    var yy = Sse2.Add(sum.AsInt32(), res);
#if NET5_0_OR_GREATER
                    Sse2.StoreScalar((int*)Unsafe.AsPointer(ref Unsafe.Add(ref d, i)), yy);
#else
                    Unsafe.Add(ref d, i) = yy.GetElement(0);
#endif
                    i++;
                    yy = Sse2.ShiftLeftLogical128BitLane(yy, 8);
                    vprev3 = Ssse3.AlignRight(vprev3, vprev2, 8);
                    vprev2 = Ssse3.AlignRight(vprev2, vprev1, 8);
                    vprev1 = Ssse3.AlignRight(vprev1, vprev0, 8);
                    vprev0 = Ssse3.AlignRight(vprev0, yy, 8);
                }
            }
            
            [MethodImpl(OptimizationUtils.InlineAndOptimizeIfPossible)]
            internal static unsafe void RestoreSignalOrder7WideAvx2(int shiftsNeeded, ReadOnlySpan<int> residual, ReadOnlySpan<int> coeffs, Span<int> output)
            {
                const int Order = 7;
                if(coeffs.Length < Order) return;
                _ = coeffs[Order - 1];
                var vshift = Vector128.CreateScalar((long)shiftsNeeded);
                var mask = Vector128.Create(~0, ~0, ~0, 0);
                ref var c = ref MemoryMarshal.GetReference(coeffs);
                ref var o = ref MemoryMarshal.GetReference(output);
                ref var d = ref Unsafe.Add(ref o, Order);
                int dataLength = output.Length - Order;
                ref var r = ref MemoryMarshal.GetReference(residual);
                Vector128<long> sum;
                Vector256<long> sum256;
                var vcoeff0 = Avx2.ConvertToVector256Int64(Sse2.LoadVector128((int*)Unsafe.AsPointer(ref Unsafe.Add(ref c, 0))).AsUInt32()).AsInt32();
                var vcoeff1 = Avx2.ConvertToVector256Int64(Avx2.MaskLoad((int*)Unsafe.AsPointer(ref Unsafe.Add(ref c, 4)), mask)).AsInt32();
                var vprev0 = Avx2.ConvertToVector256Int64(Sse2.Shuffle(Sse2.LoadScalarVector128((ulong*)Unsafe.AsPointer(ref Unsafe.Add(ref o, 4))).AsInt32(), 0b00_01_10_11).AsUInt32()).AsInt32();
                var vprev1 = Avx2.ConvertToVector256Int64(Sse2.Shuffle(Avx2.MaskLoad((int*)Unsafe.AsPointer(ref o), mask).AsInt32(), 0b00_01_10_11)).AsInt32();
                for(nint i = 0; i < dataLength;)
                {
                    var res = Vector128.CreateScalar(Unsafe.Add(ref r, i));
                    sum256 = Avx2.Multiply(vcoeff0, vprev0);
                    sum256 = Avx2.Add(sum256, Avx2.Multiply(vcoeff1, vprev1));
                    sum = Sse2.Add(sum256.GetLower(), sum256.GetUpper());
                    sum = Sse2.Add(sum, Sse2.ShiftRightLogical128BitLane(sum, 8));
                    sum = Sse2.ShiftRightLogical(sum, vshift);
                    var yy = Sse2.Add(sum.AsInt32(), res);
#if NET5_0_OR_GREATER
                    Sse2.StoreScalar((int*)Unsafe.AsPointer(ref Unsafe.Add(ref d, i)), yy);
#else
                    Unsafe.Add(ref d, i) = yy.GetElement(0);
#endif
                    i++;
                    var yu = yy.ToVector256();
                    yu = Avx2.Permute4x64(yu.AsUInt64(), 0b00_00_00_00).AsInt32();
                    vprev1 = Avx2.Permute4x64(vprev1.AsInt64(), 0b10_01_00_11).AsInt32();
                    vprev1 = Avx2.Blend(vprev1, vprev0, 0b0000_0001);
                    vprev0 = Avx2.Permute4x64(vprev0.AsInt64(), 0b10_01_00_11).AsInt32();
                    vprev0 = Avx2.Blend(vprev0, yu, 0b0000_0001);
                }
            }
#endregion Order7
#region Order8

            [MethodImpl(OptimizationUtils.InlineAndOptimizeIfPossible)]
            internal static bool RestoreSignalOrder8(int shiftsNeeded, ReadOnlySpan<int> residual, ReadOnlySpan<int> coeffs, Span<int> output)
            {
                if (Sse41.IsSupported)
                {
                    RestoreSignalOrder8Sse41(shiftsNeeded, residual, coeffs, output);
                    return true;
                }
                return false;
            }

            [MethodImpl(OptimizationUtils.InlineAndOptimizeIfPossible)]
            internal static unsafe void RestoreSignalOrder8Sse41(int shiftsNeeded, ReadOnlySpan<int> residual, ReadOnlySpan<int> coeffs, Span<int> output)
            {
                const int Order = 8;
                if (coeffs.Length < Order) return;
                _ = coeffs[Order - 1];
                var vshift = Vector128.CreateScalar(shiftsNeeded);
                ref var o = ref MemoryMarshal.GetReference(output);
                ref var d = ref Unsafe.Add(ref o, Order);
                ref var r = ref MemoryMarshal.GetReference(residual);
                ref var c = ref MemoryMarshal.GetReference(coeffs);
                Vector128<int> sum;
                var vzero = Vector128.Create(0);
                nint dataLength = output.Length - Order;
                var vcoeff0 = Sse2.LoadVector128((int*)Unsafe.AsPointer(ref Unsafe.Add(ref c, 0)));
                var vcoeff1 = Sse2.LoadVector128((int*)Unsafe.AsPointer(ref Unsafe.Add(ref c, 4)));
                var vprev0 = Sse2.Shuffle(Sse2.LoadVector128((int*)Unsafe.AsPointer(ref Unsafe.Add(ref o, 4))).AsInt32(), 0b00_01_10_11);
                var vprev1 = Sse2.Shuffle(Sse2.LoadVector128((int*)Unsafe.AsPointer(ref Unsafe.Add(ref o, 0))).AsInt32(), 0b00_01_10_11);
                var vprev2 = Vector128.Create(0, 0, 0, 0);
                for (nint i = 0; i < dataLength; i++)
                {
                    var res = Vector128.CreateScalar(Unsafe.Add(ref r, i));
                    sum = Sse41.MultiplyLow(vcoeff0, vprev0);
                    sum = Sse2.Add(sum, Sse41.MultiplyLow(vcoeff1, vprev1));
                    var y = Ssse3.HorizontalAdd(sum, vzero);
                    y = Ssse3.HorizontalAdd(y, vzero);
                    y = Sse2.ShiftRightArithmetic(y, vshift);   //C# shift operator handling sucks so SSE2 is used instead(same latency, same throughput).
                    y = Sse2.Add(y, res);   //Avoids extract and insert
#if NET5_0_OR_GREATER
                    Sse2.StoreScalar((int*)Unsafe.AsPointer(ref Unsafe.Add(ref d, i)), y);
#else
                    Unsafe.Add(ref d, i) = y.GetElement(0);
#endif
                    y = Sse2.ShiftLeftLogical128BitLane(y, 12);
                    vprev1 = Ssse3.AlignRight(vprev1, vprev0, 12);
                    vprev0 = Ssse3.AlignRight(vprev0, y, 12);
                }
            }

            [MethodImpl(OptimizationUtils.InlineAndOptimizeIfPossible)]
            internal static bool RestoreSignalOrder8Wide(int shiftsNeeded, ReadOnlySpan<int> residual, ReadOnlySpan<int> coeffs, Span<int> output)
            {
                if (Sse41.IsSupported)
                {
                    RestoreSignalOrder8WideSse41(shiftsNeeded, residual, coeffs, output);
                    return true;
                }
                return false;
            }

            [MethodImpl(OptimizationUtils.InlineAndOptimizeIfPossible)]
            internal static unsafe void RestoreSignalOrder8WideSse41(int shiftsNeeded, ReadOnlySpan<int> residual, ReadOnlySpan<int> coeffs, Span<int> output)
            {
                const int Order = 8;
                if(coeffs.Length < Order) return;
                _ = coeffs[Order - 1];
                var vshift = Vector128.CreateScalar((long)shiftsNeeded);
                ref var c = ref MemoryMarshal.GetReference(coeffs);
                ref var o = ref MemoryMarshal.GetReference(output);
                ref var d = ref Unsafe.Add(ref o, Order);
                int dataLength = output.Length - Order;
                ref var r = ref MemoryMarshal.GetReference(residual);
                Vector128<long> sum;
                var vcoeff0 = Sse41.ConvertToVector128Int64(Sse2.LoadScalarVector128((ulong*)Unsafe.AsPointer(ref Unsafe.Add(ref c, 0))).AsUInt32()).AsInt32();
                var vcoeff1 = Sse41.ConvertToVector128Int64(Sse2.LoadScalarVector128((ulong*)Unsafe.AsPointer(ref Unsafe.Add(ref c, 2))).AsUInt32()).AsInt32();
                var vcoeff2 = Sse41.ConvertToVector128Int64(Sse2.LoadScalarVector128((ulong*)Unsafe.AsPointer(ref Unsafe.Add(ref c, 4))).AsUInt32()).AsInt32();
                var vcoeff3 = Sse41.ConvertToVector128Int64(Sse2.LoadScalarVector128((ulong*)Unsafe.AsPointer(ref Unsafe.Add(ref c, 6))).AsUInt32()).AsInt32();
                var vprev0 = Sse2.Shuffle(Sse2.LoadScalarVector128((ulong*)Unsafe.AsPointer(ref Unsafe.Add(ref o, 6))).AsInt32(), 0b11_00_11_01);
                var vprev1 = Sse2.Shuffle(Sse2.LoadScalarVector128((ulong*)Unsafe.AsPointer(ref Unsafe.Add(ref o, 4))).AsInt32(), 0b11_00_11_01);
                var vprev2 = Sse2.Shuffle(Sse2.LoadScalarVector128((ulong*)Unsafe.AsPointer(ref Unsafe.Add(ref o, 2))).AsInt32(), 0b11_00_11_01);
                var vprev3 = Sse2.Shuffle(Sse2.LoadScalarVector128((ulong*)Unsafe.AsPointer(ref Unsafe.Add(ref o, 0))).AsInt32(), 0b11_00_11_01);
                for(nint i = 0; i < dataLength;)
                {
                    var res = Vector128.CreateScalar(Unsafe.Add(ref r, i));
                    sum = Sse41.Multiply(vcoeff0, vprev0);
                    sum = Sse2.Add(sum, Sse41.Multiply(vcoeff1, vprev1));
                    sum = Sse2.Add(sum, Sse41.Multiply(vcoeff2, vprev2));
                    sum = Sse2.Add(sum, Sse41.Multiply(vcoeff3, vprev3));
                    sum = Sse2.Add(sum, Sse2.ShiftRightLogical128BitLane(sum, 8));
                    sum = Sse2.ShiftRightLogical(sum, vshift);
                    var yy = Sse2.Add(sum.AsInt32(), res);
#if NET5_0_OR_GREATER
                    Sse2.StoreScalar((int*)Unsafe.AsPointer(ref Unsafe.Add(ref d, i)), yy);
#else
                    Unsafe.Add(ref d, i) = yy.GetElement(0);
#endif
                    i++;
                    yy = Sse2.ShiftLeftLogical128BitLane(yy, 8);
                    vprev3 = Ssse3.AlignRight(vprev3, vprev2, 8);
                    vprev2 = Ssse3.AlignRight(vprev2, vprev1, 8);
                    vprev1 = Ssse3.AlignRight(vprev1, vprev0, 8);
                    vprev0 = Ssse3.AlignRight(vprev0, yy, 8);
                }
            }
            
            [MethodImpl(OptimizationUtils.InlineAndOptimizeIfPossible)]
            internal static unsafe void RestoreSignalOrder8WideAvx2(int shiftsNeeded, ReadOnlySpan<int> residual, ReadOnlySpan<int> coeffs, Span<int> output)
            {
                const int Order = 8;
                if(coeffs.Length < Order) return;
                _ = coeffs[Order - 1];
                var vshift = Vector128.CreateScalar((long)shiftsNeeded);
                ref var c = ref MemoryMarshal.GetReference(coeffs);
                ref var o = ref MemoryMarshal.GetReference(output);
                ref var d = ref Unsafe.Add(ref o, Order);
                int dataLength = output.Length - Order;
                ref var r = ref MemoryMarshal.GetReference(residual);
                Vector128<long> sum;
                Vector256<long> sum256;
                var vcoeff0 = Avx2.ConvertToVector256Int64(Sse2.LoadVector128((int*)Unsafe.AsPointer(ref Unsafe.Add(ref c, 0))).AsUInt32()).AsInt32();
                var vcoeff1 = Avx2.ConvertToVector256Int64(Sse2.LoadVector128((int*)Unsafe.AsPointer(ref Unsafe.Add(ref c, 4))).AsUInt32()).AsInt32();
                var vprev0 = Avx2.ConvertToVector256Int64(Sse2.Shuffle(Sse2.LoadScalarVector128((ulong*)Unsafe.AsPointer(ref Unsafe.Add(ref o, 4))).AsInt32(), 0b00_01_10_11).AsUInt32()).AsInt32();
                var vprev1 = Avx2.ConvertToVector256Int64(Sse2.Shuffle(Sse2.LoadScalarVector128((ulong*)Unsafe.AsPointer(ref Unsafe.Add(ref o, 0))).AsInt32(), 0b00_01_10_11).AsUInt32()).AsInt32();
                for(nint i = 0; i < dataLength;)
                {
                    var res = Vector128.CreateScalar(Unsafe.Add(ref r, i));
                    sum256 = Avx2.Multiply(vcoeff0, vprev0);
                    sum256 = Avx2.Add(sum256, Avx2.Multiply(vcoeff1, vprev1));
                    sum = Sse2.Add(sum256.GetLower(), sum256.GetUpper());
                    sum = Sse2.Add(sum, Sse2.ShiftRightLogical128BitLane(sum, 8));
                    sum = Sse2.ShiftRightLogical(sum, vshift);
                    var yy = Sse2.Add(sum.AsInt32(), res);
#if NET5_0_OR_GREATER
                    Sse2.StoreScalar((int*)Unsafe.AsPointer(ref Unsafe.Add(ref d, i)), yy);
#else
                    Unsafe.Add(ref d, i) = yy.GetElement(0);
#endif
                    i++;
                    var yu = yy.ToVector256();
                    yu = Avx2.Permute4x64(yu.AsUInt64(), 0b00_00_00_00).AsInt32();
                    vprev1 = Avx2.Permute4x64(vprev1.AsInt64(), 0b10_01_00_11).AsInt32();
                    vprev1 = Avx2.Blend(vprev1, vprev0, 0b0000_0001);
                    vprev0 = Avx2.Permute4x64(vprev0.AsInt64(), 0b10_01_00_11).AsInt32();
                    vprev0 = Avx2.Blend(vprev0, yu, 0b0000_0001);
                }
            }
#endregion Order8
#region Order9

            [MethodImpl(OptimizationUtils.InlineAndOptimizeIfPossible)]
            internal static bool RestoreSignalOrder9(int shiftsNeeded, ReadOnlySpan<int> residual, ReadOnlySpan<int> coeffs, Span<int> output)
            {
                if (Sse41.IsSupported)
                {
                    RestoreSignalOrder9Sse41(shiftsNeeded, residual, coeffs, output);
                    return true;
                }
                return false;
            }

            [MethodImpl(OptimizationUtils.InlineAndOptimizeIfPossible)]
            internal static unsafe void RestoreSignalOrder9Sse41(int shiftsNeeded, ReadOnlySpan<int> residual, ReadOnlySpan<int> coeffs, Span<int> output)
            {
                const int Order = 9;
                if (coeffs.Length < Order) return;
                _ = coeffs[Order - 1];
                var vshift = Vector128.CreateScalar(shiftsNeeded);
                ref var o = ref MemoryMarshal.GetReference(output);
                ref var d = ref Unsafe.Add(ref o, Order);
                ref var r = ref MemoryMarshal.GetReference(residual);
                ref var c = ref MemoryMarshal.GetReference(coeffs);
                Vector128<int> sum;
                var vzero = Vector128.Create(0);
                nint dataLength = output.Length - Order;
                var vcoeff0 = Sse2.LoadVector128((int*)Unsafe.AsPointer(ref Unsafe.Add(ref c, 0)));
                var vcoeff1 = Sse2.LoadVector128((int*)Unsafe.AsPointer(ref Unsafe.Add(ref c, 4)));
                var vcoeff2 = Vector128.Create(Unsafe.Add(ref c, 8), 0, 0, 0);
                var vprev0 = Sse2.Shuffle(Sse2.LoadVector128((int*)Unsafe.AsPointer(ref Unsafe.Add(ref o, 5))).AsInt32(), 0b00_01_10_11);
                var vprev1 = Sse2.Shuffle(Sse2.LoadVector128((int*)Unsafe.AsPointer(ref Unsafe.Add(ref o, 1))).AsInt32(), 0b00_01_10_11);
                var vprev2 = Vector128.Create(Unsafe.Add(ref o, 0), 0, 0, 0);
                for (nint i = 0; i < dataLength; i++)
                {
                    var res = Vector128.CreateScalar(Unsafe.Add(ref r, i));
                    sum = Sse41.MultiplyLow(vcoeff0, vprev0);
                    sum = Sse2.Add(sum, Sse41.MultiplyLow(vcoeff1, vprev1));
                    sum = Sse2.Add(sum, Sse41.MultiplyLow(vcoeff2, vprev2));
                    var y = Ssse3.HorizontalAdd(sum, vzero);
                    y = Ssse3.HorizontalAdd(y, vzero);
                    y = Sse2.ShiftRightArithmetic(y, vshift);   //C# shift operator handling sucks so SSE2 is used instead(same latency, same throughput).
                    y = Sse2.Add(y, res);   //Avoids extract and insert
#if NET5_0_OR_GREATER
                    Sse2.StoreScalar((int*)Unsafe.AsPointer(ref Unsafe.Add(ref d, i)), y);
#else
                    Unsafe.Add(ref d, i) = y.GetElement(0);
#endif
                    y = Sse2.ShiftLeftLogical128BitLane(y, 12);
                    vprev2 = Ssse3.AlignRight(vprev2, vprev1, 12);
                    vprev1 = Ssse3.AlignRight(vprev1, vprev0, 12);
                    vprev0 = Ssse3.AlignRight(vprev0, y, 12);
                }
            }

            [MethodImpl(OptimizationUtils.InlineAndOptimizeIfPossible)]
            internal static bool RestoreSignalOrder9Wide(int shiftsNeeded, ReadOnlySpan<int> residual, ReadOnlySpan<int> coeffs, Span<int> output)
            {
                if (Sse41.IsSupported)
                {
                    RestoreSignalOrder9WideSse41(shiftsNeeded, residual, coeffs, output);
                    return true;
                }
                return false;
            }

            [MethodImpl(OptimizationUtils.InlineAndOptimizeIfPossible)]
            internal static unsafe void RestoreSignalOrder9WideSse41(int shiftsNeeded, ReadOnlySpan<int> residual, ReadOnlySpan<int> coeffs, Span<int> output)
            {
                const int Order = 9;
                if(coeffs.Length < Order) return;
                _ = coeffs[Order - 1];
                var vshift = Vector128.CreateScalar((long)shiftsNeeded);
                ref var c = ref MemoryMarshal.GetReference(coeffs);
                ref var o = ref MemoryMarshal.GetReference(output);
                ref var d = ref Unsafe.Add(ref o, Order);
                int dataLength = output.Length - Order;
                ref var r = ref MemoryMarshal.GetReference(residual);
                Vector128<long> sum;
                var vcoeff0 = Sse41.ConvertToVector128Int64(Sse2.LoadScalarVector128((ulong*)Unsafe.AsPointer(ref Unsafe.Add(ref c, 0))).AsUInt32()).AsInt32();
                var vcoeff1 = Sse41.ConvertToVector128Int64(Sse2.LoadScalarVector128((ulong*)Unsafe.AsPointer(ref Unsafe.Add(ref c, 2))).AsUInt32()).AsInt32();
                var vcoeff2 = Sse41.ConvertToVector128Int64(Sse2.LoadScalarVector128((ulong*)Unsafe.AsPointer(ref Unsafe.Add(ref c, 4))).AsUInt32()).AsInt32();
                var vcoeff3 = Sse41.ConvertToVector128Int64(Sse2.LoadScalarVector128((ulong*)Unsafe.AsPointer(ref Unsafe.Add(ref c, 6))).AsUInt32()).AsInt32();
                var vcoeff4 = Sse41.ConvertToVector128Int64(Sse2.LoadScalarVector128((uint*)Unsafe.AsPointer(ref Unsafe.Add(ref c, 8))).AsUInt32()).AsInt32();
                var vprev0 = Sse2.Shuffle(Sse2.LoadScalarVector128((ulong*)Unsafe.AsPointer(ref Unsafe.Add(ref o, 7))).AsInt32(), 0b11_00_11_01);
                var vprev1 = Sse2.Shuffle(Sse2.LoadScalarVector128((ulong*)Unsafe.AsPointer(ref Unsafe.Add(ref o, 5))).AsInt32(), 0b11_00_11_01);
                var vprev2 = Sse2.Shuffle(Sse2.LoadScalarVector128((ulong*)Unsafe.AsPointer(ref Unsafe.Add(ref o, 3))).AsInt32(), 0b11_00_11_01);
                var vprev3 = Sse2.Shuffle(Sse2.LoadScalarVector128((ulong*)Unsafe.AsPointer(ref Unsafe.Add(ref o, 1))).AsInt32(), 0b11_00_11_01);
                var vprev4 = Sse2.Shuffle(Sse2.LoadScalarVector128((ulong*)Unsafe.AsPointer(ref Unsafe.Add(ref o, 0))).AsInt32(), 0b11_00_11_00);
                for(nint i = 0; i < dataLength;)
                {
                    var res = Vector128.CreateScalar(Unsafe.Add(ref r, i));
                    sum = Sse41.Multiply(vcoeff0, vprev0);
                    sum = Sse2.Add(sum, Sse41.Multiply(vcoeff1, vprev1));
                    sum = Sse2.Add(sum, Sse41.Multiply(vcoeff2, vprev2));
                    sum = Sse2.Add(sum, Sse41.Multiply(vcoeff3, vprev3));
                    sum = Sse2.Add(sum, Sse41.Multiply(vcoeff4, vprev4));
                    sum = Sse2.Add(sum, Sse2.ShiftRightLogical128BitLane(sum, 8));
                    sum = Sse2.ShiftRightLogical(sum, vshift);
                    var yy = Sse2.Add(sum.AsInt32(), res);
#if NET5_0_OR_GREATER
                    Sse2.StoreScalar((int*)Unsafe.AsPointer(ref Unsafe.Add(ref d, i)), yy);
#else
                    Unsafe.Add(ref d, i) = yy.GetElement(0);
#endif
                    i++;
                    yy = Sse2.ShiftLeftLogical128BitLane(yy, 8);
                    vprev4 = Ssse3.AlignRight(vprev4, vprev3, 8);
                    vprev3 = Ssse3.AlignRight(vprev3, vprev2, 8);
                    vprev2 = Ssse3.AlignRight(vprev2, vprev1, 8);
                    vprev1 = Ssse3.AlignRight(vprev1, vprev0, 8);
                    vprev0 = Ssse3.AlignRight(vprev0, yy, 8);
                }
            }
            
            [MethodImpl(OptimizationUtils.InlineAndOptimizeIfPossible)]
            internal static unsafe void RestoreSignalOrder9WideAvx2(int shiftsNeeded, ReadOnlySpan<int> residual, ReadOnlySpan<int> coeffs, Span<int> output)
            {
                const int Order = 9;
                if(coeffs.Length < Order) return;
                _ = coeffs[Order - 1];
                var vshift = Vector128.CreateScalar((long)shiftsNeeded);
                ref var c = ref MemoryMarshal.GetReference(coeffs);
                ref var o = ref MemoryMarshal.GetReference(output);
                ref var d = ref Unsafe.Add(ref o, Order);
                int dataLength = output.Length - Order;
                ref var r = ref MemoryMarshal.GetReference(residual);
                Vector128<long> sum;
                Vector256<long> sum256;
                var vcoeff0 = Avx2.ConvertToVector256Int64(Sse2.LoadVector128((int*)Unsafe.AsPointer(ref Unsafe.Add(ref c, 0))).AsUInt32()).AsInt32();
                var vcoeff1 = Avx2.ConvertToVector256Int64(Sse2.LoadVector128((int*)Unsafe.AsPointer(ref Unsafe.Add(ref c, 4))).AsUInt32()).AsInt32();
                var vcoeff2 = Avx2.ConvertToVector256Int64(Sse2.LoadScalarVector128((uint*)Unsafe.AsPointer(ref Unsafe.Add(ref c, 8))).AsUInt32()).AsInt32();
                var vprev0 = Avx2.ConvertToVector256Int64(Sse2.Shuffle(Sse2.LoadScalarVector128((ulong*)Unsafe.AsPointer(ref Unsafe.Add(ref o, 6))).AsInt32(), 0b00_01_10_11).AsUInt32()).AsInt32();
                var vprev1 = Avx2.ConvertToVector256Int64(Sse2.Shuffle(Sse2.LoadScalarVector128((ulong*)Unsafe.AsPointer(ref Unsafe.Add(ref o, 2))).AsInt32(), 0b00_01_10_11).AsUInt32()).AsInt32();
                var vprev2 = Avx2.ConvertToVector256Int64(Sse2.Shuffle(Sse2.LoadScalarVector128((uint*)Unsafe.AsPointer(ref o)).AsInt32(), 0b00_01_10_11).AsUInt32()).AsInt32();
                for(nint i = 0; i < dataLength;)
                {
                    var res = Vector128.CreateScalar(Unsafe.Add(ref r, i));
                    sum256 = Avx2.Multiply(vcoeff0, vprev0);
                    sum256 = Avx2.Add(sum256, Avx2.Multiply(vcoeff1, vprev1));
                    sum256 = Avx2.Add(sum256, Avx2.Multiply(vcoeff2, vprev2));
                    sum = Sse2.Add(sum256.GetLower(), sum256.GetUpper());
                    sum = Sse2.Add(sum, Sse2.ShiftRightLogical128BitLane(sum, 8));
                    sum = Sse2.ShiftRightLogical(sum, vshift);
                    var yy = Sse2.Add(sum.AsInt32(), res);
#if NET5_0_OR_GREATER
                    Sse2.StoreScalar((int*)Unsafe.AsPointer(ref Unsafe.Add(ref d, i)), yy);
#else
                    Unsafe.Add(ref d, i) = yy.GetElement(0);
#endif
                    i++;
                    var yu = yy.ToVector256();
                    yu = Avx2.Permute4x64(yu.AsUInt64(), 0b00_00_00_00).AsInt32();
                    vprev2 = Avx2.Permute4x64(vprev2.AsInt64(), 0b10_01_00_11).AsInt32();
                    vprev2 = Avx2.Blend(vprev2, vprev1, 0b0000_0001);
                    vprev1 = Avx2.Permute4x64(vprev1.AsInt64(), 0b10_01_00_11).AsInt32();
                    vprev1 = Avx2.Blend(vprev1, vprev0, 0b0000_0001);
                    vprev0 = Avx2.Permute4x64(vprev0.AsInt64(), 0b10_01_00_11).AsInt32();
                    vprev0 = Avx2.Blend(vprev0, yu, 0b0000_0001);
                }
            }
#endregion Order9
#region Order10

            [MethodImpl(OptimizationUtils.InlineAndOptimizeIfPossible)]
            internal static bool RestoreSignalOrder10(int shiftsNeeded, ReadOnlySpan<int> residual, ReadOnlySpan<int> coeffs, Span<int> output)
            {
                if (Sse41.IsSupported)
                {
                    RestoreSignalOrder10Sse41(shiftsNeeded, residual, coeffs, output);
                    return true;
                }
                return false;
            }

            [MethodImpl(OptimizationUtils.InlineAndOptimizeIfPossible)]
            internal static unsafe void RestoreSignalOrder10Sse41(int shiftsNeeded, ReadOnlySpan<int> residual, ReadOnlySpan<int> coeffs, Span<int> output)
            {
                const int Order = 10;
                if (coeffs.Length < Order) return;
                _ = coeffs[Order - 1];
                var vshift = Vector128.CreateScalar(shiftsNeeded);
                ref var o = ref MemoryMarshal.GetReference(output);
                ref var d = ref Unsafe.Add(ref o, Order);
                ref var r = ref MemoryMarshal.GetReference(residual);
                ref var c = ref MemoryMarshal.GetReference(coeffs);
                Vector128<int> sum;
                var vzero = Vector128.Create(0);
                nint dataLength = output.Length - Order;
                var vcoeff0 = Sse2.LoadVector128((int*)Unsafe.AsPointer(ref Unsafe.Add(ref c, 0)));
                var vcoeff1 = Sse2.LoadVector128((int*)Unsafe.AsPointer(ref Unsafe.Add(ref c, 4)));
                var vcoeff2 = Vector128.Create(Unsafe.Add(ref c, 8), Unsafe.Add(ref c, 9), 0, 0);
                var vprev0 = Sse2.Shuffle(Sse2.LoadVector128((int*)Unsafe.AsPointer(ref Unsafe.Add(ref o, 6))).AsInt32(), 0b00_01_10_11);
                var vprev1 = Sse2.Shuffle(Sse2.LoadVector128((int*)Unsafe.AsPointer(ref Unsafe.Add(ref o, 2))).AsInt32(), 0b00_01_10_11);
                var vprev2 = Vector128.Create(Unsafe.Add(ref o, 1), Unsafe.Add(ref o, 0), 0, 0);
                for (nint i = 0; i < dataLength; i++)
                {
                    var res = Vector128.CreateScalar(Unsafe.Add(ref r, i));
                    sum = Sse41.MultiplyLow(vcoeff0, vprev0);
                    sum = Sse2.Add(sum, Sse41.MultiplyLow(vcoeff1, vprev1));
                    sum = Sse2.Add(sum, Sse41.MultiplyLow(vcoeff2, vprev2));
                    var y = Ssse3.HorizontalAdd(sum, vzero);
                    y = Ssse3.HorizontalAdd(y, vzero);
                    y = Sse2.ShiftRightArithmetic(y, vshift);   //C# shift operator handling sucks so SSE2 is used instead(same latency, same throughput).
                    y = Sse2.Add(y, res);   //Avoids extract and insert
#if NET5_0_OR_GREATER
                    Sse2.StoreScalar((int*)Unsafe.AsPointer(ref Unsafe.Add(ref d, i)), y);
#else
                    Unsafe.Add(ref d, i) = y.GetElement(0);
#endif
                    y = Sse2.ShiftLeftLogical128BitLane(y, 12);
                    vprev2 = Ssse3.AlignRight(vprev2, vprev1, 12);
                    vprev1 = Ssse3.AlignRight(vprev1, vprev0, 12);
                    vprev0 = Ssse3.AlignRight(vprev0, y, 12);
                }
            }

            [MethodImpl(OptimizationUtils.InlineAndOptimizeIfPossible)]
            internal static bool RestoreSignalOrder10Wide(int shiftsNeeded, ReadOnlySpan<int> residual, ReadOnlySpan<int> coeffs, Span<int> output)
            {
                if (Sse41.IsSupported)
                {
                    RestoreSignalOrder10WideSse41(shiftsNeeded, residual, coeffs, output);
                    return true;
                }
                return false;
            }

            [MethodImpl(OptimizationUtils.InlineAndOptimizeIfPossible)]
            internal static unsafe void RestoreSignalOrder10WideSse41(int shiftsNeeded, ReadOnlySpan<int> residual, ReadOnlySpan<int> coeffs, Span<int> output)
            {
                const int Order = 10;
                if(coeffs.Length < Order) return;
                _ = coeffs[Order - 1];
                var vshift = Vector128.CreateScalar((long)shiftsNeeded);
                ref var c = ref MemoryMarshal.GetReference(coeffs);
                ref var o = ref MemoryMarshal.GetReference(output);
                ref var d = ref Unsafe.Add(ref o, Order);
                int dataLength = output.Length - Order;
                ref var r = ref MemoryMarshal.GetReference(residual);
                Vector128<long> sum;
                var vcoeff0 = Sse41.ConvertToVector128Int64(Sse2.LoadScalarVector128((ulong*)Unsafe.AsPointer(ref Unsafe.Add(ref c, 0))).AsUInt32()).AsInt32();
                var vcoeff1 = Sse41.ConvertToVector128Int64(Sse2.LoadScalarVector128((ulong*)Unsafe.AsPointer(ref Unsafe.Add(ref c, 2))).AsUInt32()).AsInt32();
                var vcoeff2 = Sse41.ConvertToVector128Int64(Sse2.LoadScalarVector128((ulong*)Unsafe.AsPointer(ref Unsafe.Add(ref c, 4))).AsUInt32()).AsInt32();
                var vcoeff3 = Sse41.ConvertToVector128Int64(Sse2.LoadScalarVector128((ulong*)Unsafe.AsPointer(ref Unsafe.Add(ref c, 6))).AsUInt32()).AsInt32();
                var vcoeff4 = Sse41.ConvertToVector128Int64(Sse2.LoadScalarVector128((ulong*)Unsafe.AsPointer(ref Unsafe.Add(ref c, 8))).AsUInt32()).AsInt32();
                var vprev0 = Sse2.Shuffle(Sse2.LoadScalarVector128((ulong*)Unsafe.AsPointer(ref Unsafe.Add(ref o, 8))).AsInt32(), 0b11_00_11_01);
                var vprev1 = Sse2.Shuffle(Sse2.LoadScalarVector128((ulong*)Unsafe.AsPointer(ref Unsafe.Add(ref o, 6))).AsInt32(), 0b11_00_11_01);
                var vprev2 = Sse2.Shuffle(Sse2.LoadScalarVector128((ulong*)Unsafe.AsPointer(ref Unsafe.Add(ref o, 4))).AsInt32(), 0b11_00_11_01);
                var vprev3 = Sse2.Shuffle(Sse2.LoadScalarVector128((ulong*)Unsafe.AsPointer(ref Unsafe.Add(ref o, 2))).AsInt32(), 0b11_00_11_01);
                var vprev4 = Sse2.Shuffle(Sse2.LoadScalarVector128((ulong*)Unsafe.AsPointer(ref Unsafe.Add(ref o, 0))).AsInt32(), 0b11_00_11_01);
                for(nint i = 0; i < dataLength;)
                {
                    var res = Vector128.CreateScalar(Unsafe.Add(ref r, i));
                    sum = Sse41.Multiply(vcoeff0, vprev0);
                    sum = Sse2.Add(sum, Sse41.Multiply(vcoeff1, vprev1));
                    sum = Sse2.Add(sum, Sse41.Multiply(vcoeff2, vprev2));
                    sum = Sse2.Add(sum, Sse41.Multiply(vcoeff3, vprev3));
                    sum = Sse2.Add(sum, Sse41.Multiply(vcoeff4, vprev4));
                    sum = Sse2.Add(sum, Sse2.ShiftRightLogical128BitLane(sum, 8));
                    sum = Sse2.ShiftRightLogical(sum, vshift);
                    var yy = Sse2.Add(sum.AsInt32(), res);
#if NET5_0_OR_GREATER
                    Sse2.StoreScalar((int*)Unsafe.AsPointer(ref Unsafe.Add(ref d, i)), yy);
#else
                    Unsafe.Add(ref d, i) = yy.GetElement(0);
#endif
                    i++;
                    yy = Sse2.ShiftLeftLogical128BitLane(yy, 8);
                    vprev4 = Ssse3.AlignRight(vprev4, vprev3, 8);
                    vprev3 = Ssse3.AlignRight(vprev3, vprev2, 8);
                    vprev2 = Ssse3.AlignRight(vprev2, vprev1, 8);
                    vprev1 = Ssse3.AlignRight(vprev1, vprev0, 8);
                    vprev0 = Ssse3.AlignRight(vprev0, yy, 8);
                }
            }
            
            [MethodImpl(OptimizationUtils.InlineAndOptimizeIfPossible)]
            internal static unsafe void RestoreSignalOrder10WideAvx2(int shiftsNeeded, ReadOnlySpan<int> residual, ReadOnlySpan<int> coeffs, Span<int> output)
            {
                const int Order = 10;
                if(coeffs.Length < Order) return;
                _ = coeffs[Order - 1];
                var vshift = Vector128.CreateScalar((long)shiftsNeeded);
                ref var c = ref MemoryMarshal.GetReference(coeffs);
                ref var o = ref MemoryMarshal.GetReference(output);
                ref var d = ref Unsafe.Add(ref o, Order);
                int dataLength = output.Length - Order;
                ref var r = ref MemoryMarshal.GetReference(residual);
                Vector128<long> sum;
                Vector256<long> sum256;
                var vcoeff0 = Avx2.ConvertToVector256Int64(Sse2.LoadVector128((int*)Unsafe.AsPointer(ref Unsafe.Add(ref c, 0))).AsUInt32()).AsInt32();
                var vcoeff1 = Avx2.ConvertToVector256Int64(Sse2.LoadVector128((int*)Unsafe.AsPointer(ref Unsafe.Add(ref c, 4))).AsUInt32()).AsInt32();
                var vcoeff2 = Avx2.ConvertToVector256Int64(Sse2.LoadScalarVector128((ulong*)Unsafe.AsPointer(ref Unsafe.Add(ref c, 8))).AsUInt32()).AsInt32();
                var vprev0 = Avx2.ConvertToVector256Int64(Sse2.Shuffle(Sse2.LoadScalarVector128((ulong*)Unsafe.AsPointer(ref Unsafe.Add(ref o, 7))).AsInt32(), 0b00_01_10_11).AsUInt32()).AsInt32();
                var vprev1 = Avx2.ConvertToVector256Int64(Sse2.Shuffle(Sse2.LoadScalarVector128((ulong*)Unsafe.AsPointer(ref Unsafe.Add(ref o, 3))).AsInt32(), 0b00_01_10_11).AsUInt32()).AsInt32();
                var vprev2 = Avx2.ConvertToVector256Int64(Sse2.Shuffle(Sse2.LoadScalarVector128((ulong*)Unsafe.AsPointer(ref o)).AsInt32(), 0b00_01_10_11).AsUInt32()).AsInt32();
                for(nint i = 0; i < dataLength;)
                {
                    var res = Vector128.CreateScalar(Unsafe.Add(ref r, i));
                    sum256 = Avx2.Multiply(vcoeff0, vprev0);
                    sum256 = Avx2.Add(sum256, Avx2.Multiply(vcoeff1, vprev1));
                    sum256 = Avx2.Add(sum256, Avx2.Multiply(vcoeff2, vprev2));
                    sum = Sse2.Add(sum256.GetLower(), sum256.GetUpper());
                    sum = Sse2.Add(sum, Sse2.ShiftRightLogical128BitLane(sum, 8));
                    sum = Sse2.ShiftRightLogical(sum, vshift);
                    var yy = Sse2.Add(sum.AsInt32(), res);
#if NET5_0_OR_GREATER
                    Sse2.StoreScalar((int*)Unsafe.AsPointer(ref Unsafe.Add(ref d, i)), yy);
#else
                    Unsafe.Add(ref d, i) = yy.GetElement(0);
#endif
                    i++;
                    var yu = yy.ToVector256();
                    yu = Avx2.Permute4x64(yu.AsUInt64(), 0b00_00_00_00).AsInt32();
                    vprev2 = Avx2.Permute4x64(vprev2.AsInt64(), 0b10_01_00_11).AsInt32();
                    vprev2 = Avx2.Blend(vprev2, vprev1, 0b0000_0001);
                    vprev1 = Avx2.Permute4x64(vprev1.AsInt64(), 0b10_01_00_11).AsInt32();
                    vprev1 = Avx2.Blend(vprev1, vprev0, 0b0000_0001);
                    vprev0 = Avx2.Permute4x64(vprev0.AsInt64(), 0b10_01_00_11).AsInt32();
                    vprev0 = Avx2.Blend(vprev0, yu, 0b0000_0001);
                }
            }
#endregion Order10
#region Order11

            [MethodImpl(OptimizationUtils.InlineAndOptimizeIfPossible)]
            internal static bool RestoreSignalOrder11(int shiftsNeeded, ReadOnlySpan<int> residual, ReadOnlySpan<int> coeffs, Span<int> output)
            {
                if (Sse41.IsSupported)
                {
                    RestoreSignalOrder11Sse41(shiftsNeeded, residual, coeffs, output);
                    return true;
                }
                return false;
            }

            [MethodImpl(OptimizationUtils.InlineAndOptimizeIfPossible)]
            internal static unsafe void RestoreSignalOrder11Sse41(int shiftsNeeded, ReadOnlySpan<int> residual, ReadOnlySpan<int> coeffs, Span<int> output)
            {
                const int Order = 11;
                if (coeffs.Length < Order) return;
                _ = coeffs[Order - 1];
                var vshift = Vector128.CreateScalar(shiftsNeeded);
                ref var o = ref MemoryMarshal.GetReference(output);
                ref var d = ref Unsafe.Add(ref o, Order);
                ref var r = ref MemoryMarshal.GetReference(residual);
                ref var c = ref MemoryMarshal.GetReference(coeffs);
                Vector128<int> sum;
                var vzero = Vector128.Create(0);
                nint dataLength = output.Length - Order;
                var vcoeff0 = Sse2.LoadVector128((int*)Unsafe.AsPointer(ref Unsafe.Add(ref c, 0)));
                var vcoeff1 = Sse2.LoadVector128((int*)Unsafe.AsPointer(ref Unsafe.Add(ref c, 4)));
                var vcoeff2 = Vector128.Create(Unsafe.Add(ref c, 8), Unsafe.Add(ref c, 9), Unsafe.Add(ref c, 10), 0);
                var vprev0 = Sse2.Shuffle(Sse2.LoadVector128((int*)Unsafe.AsPointer(ref Unsafe.Add(ref o, 7))).AsInt32(), 0b00_01_10_11);
                var vprev1 = Sse2.Shuffle(Sse2.LoadVector128((int*)Unsafe.AsPointer(ref Unsafe.Add(ref o, 3))).AsInt32(), 0b00_01_10_11);
                var vprev2 = Vector128.Create(Unsafe.Add(ref o, 2), Unsafe.Add(ref o, 1), Unsafe.Add(ref o, 0), 0);
                for (nint i = 0; i < dataLength; i++)
                {
                    var res = Vector128.CreateScalar(Unsafe.Add(ref r, i));
                    sum = Sse41.MultiplyLow(vcoeff0, vprev0);
                    sum = Sse2.Add(sum, Sse41.MultiplyLow(vcoeff1, vprev1));
                    sum = Sse2.Add(sum, Sse41.MultiplyLow(vcoeff2, vprev2));
                    var y = Ssse3.HorizontalAdd(sum, vzero);
                    y = Ssse3.HorizontalAdd(y, vzero);
                    y = Sse2.ShiftRightArithmetic(y, vshift);   //C# shift operator handling sucks so SSE2 is used instead(same latency, same throughput).
                    y = Sse2.Add(y, res);   //Avoids extract and insert
#if NET5_0_OR_GREATER
                    Sse2.StoreScalar((int*)Unsafe.AsPointer(ref Unsafe.Add(ref d, i)), y);
#else
                    Unsafe.Add(ref d, i) = y.GetElement(0);
#endif
                    y = Sse2.ShiftLeftLogical128BitLane(y, 12);
                    vprev2 = Ssse3.AlignRight(vprev2, vprev1, 12);
                    vprev1 = Ssse3.AlignRight(vprev1, vprev0, 12);
                    vprev0 = Ssse3.AlignRight(vprev0, y, 12);
                }
            }

            [MethodImpl(OptimizationUtils.InlineAndOptimizeIfPossible)]
            internal static bool RestoreSignalOrder11Wide(int shiftsNeeded, ReadOnlySpan<int> residual, ReadOnlySpan<int> coeffs, Span<int> output)
            {
                if (Sse41.IsSupported)
                {
                    RestoreSignalOrder11WideSse41(shiftsNeeded, residual, coeffs, output);
                    return true;
                }
                return false;
            }

            [MethodImpl(OptimizationUtils.InlineAndOptimizeIfPossible)]
            internal static unsafe void RestoreSignalOrder11WideSse41(int shiftsNeeded, ReadOnlySpan<int> residual, ReadOnlySpan<int> coeffs, Span<int> output)
            {
                const int Order = 11;
                if(coeffs.Length < Order) return;
                _ = coeffs[Order - 1];
                var vshift = Vector128.CreateScalar((long)shiftsNeeded);
                ref var c = ref MemoryMarshal.GetReference(coeffs);
                ref var o = ref MemoryMarshal.GetReference(output);
                ref var d = ref Unsafe.Add(ref o, Order);
                int dataLength = output.Length - Order;
                ref var r = ref MemoryMarshal.GetReference(residual);
                Vector128<long> sum;
                var vcoeff0 = Sse41.ConvertToVector128Int64(Sse2.LoadScalarVector128((ulong*)Unsafe.AsPointer(ref Unsafe.Add(ref c, 0))).AsUInt32()).AsInt32();
                var vcoeff1 = Sse41.ConvertToVector128Int64(Sse2.LoadScalarVector128((ulong*)Unsafe.AsPointer(ref Unsafe.Add(ref c, 2))).AsUInt32()).AsInt32();
                var vcoeff2 = Sse41.ConvertToVector128Int64(Sse2.LoadScalarVector128((ulong*)Unsafe.AsPointer(ref Unsafe.Add(ref c, 4))).AsUInt32()).AsInt32();
                var vcoeff3 = Sse41.ConvertToVector128Int64(Sse2.LoadScalarVector128((ulong*)Unsafe.AsPointer(ref Unsafe.Add(ref c, 6))).AsUInt32()).AsInt32();
                var vcoeff4 = Sse41.ConvertToVector128Int64(Sse2.LoadScalarVector128((ulong*)Unsafe.AsPointer(ref Unsafe.Add(ref c, 8))).AsUInt32()).AsInt32();
                var vcoeff5 = Sse41.ConvertToVector128Int64(Sse2.LoadScalarVector128((uint*)Unsafe.AsPointer(ref Unsafe.Add(ref c, 10))).AsUInt32()).AsInt32();
                var vprev0 = Sse2.Shuffle(Sse2.LoadScalarVector128((ulong*)Unsafe.AsPointer(ref Unsafe.Add(ref o, 9))).AsInt32(), 0b11_00_11_01);
                var vprev1 = Sse2.Shuffle(Sse2.LoadScalarVector128((ulong*)Unsafe.AsPointer(ref Unsafe.Add(ref o, 7))).AsInt32(), 0b11_00_11_01);
                var vprev2 = Sse2.Shuffle(Sse2.LoadScalarVector128((ulong*)Unsafe.AsPointer(ref Unsafe.Add(ref o, 5))).AsInt32(), 0b11_00_11_01);
                var vprev3 = Sse2.Shuffle(Sse2.LoadScalarVector128((ulong*)Unsafe.AsPointer(ref Unsafe.Add(ref o, 3))).AsInt32(), 0b11_00_11_01);
                var vprev4 = Sse2.Shuffle(Sse2.LoadScalarVector128((ulong*)Unsafe.AsPointer(ref Unsafe.Add(ref o, 1))).AsInt32(), 0b11_00_11_01);
                var vprev5 = Sse2.Shuffle(Sse2.LoadScalarVector128((ulong*)Unsafe.AsPointer(ref Unsafe.Add(ref o, 0))).AsInt32(), 0b11_00_11_00);
                for(nint i = 0; i < dataLength;)
                {
                    var res = Vector128.CreateScalar(Unsafe.Add(ref r, i));
                    sum = Sse41.Multiply(vcoeff0, vprev0);
                    sum = Sse2.Add(sum, Sse41.Multiply(vcoeff1, vprev1));
                    sum = Sse2.Add(sum, Sse41.Multiply(vcoeff2, vprev2));
                    sum = Sse2.Add(sum, Sse41.Multiply(vcoeff3, vprev3));
                    sum = Sse2.Add(sum, Sse41.Multiply(vcoeff4, vprev4));
                    sum = Sse2.Add(sum, Sse41.Multiply(vcoeff5, vprev5));
                    sum = Sse2.Add(sum, Sse2.ShiftRightLogical128BitLane(sum, 8));
                    sum = Sse2.ShiftRightLogical(sum, vshift);
                    var yy = Sse2.Add(sum.AsInt32(), res);
#if NET5_0_OR_GREATER
                    Sse2.StoreScalar((int*)Unsafe.AsPointer(ref Unsafe.Add(ref d, i)), yy);
#else
                    Unsafe.Add(ref d, i) = yy.GetElement(0);
#endif
                    i++;
                    yy = Sse2.ShiftLeftLogical128BitLane(yy, 8);
                    vprev5 = Ssse3.AlignRight(vprev5, vprev4, 8);
                    vprev4 = Ssse3.AlignRight(vprev4, vprev3, 8);
                    vprev3 = Ssse3.AlignRight(vprev3, vprev2, 8);
                    vprev2 = Ssse3.AlignRight(vprev2, vprev1, 8);
                    vprev1 = Ssse3.AlignRight(vprev1, vprev0, 8);
                    vprev0 = Ssse3.AlignRight(vprev0, yy, 8);
                }
            }
            
            [MethodImpl(OptimizationUtils.InlineAndOptimizeIfPossible)]
            internal static unsafe void RestoreSignalOrder11WideAvx2(int shiftsNeeded, ReadOnlySpan<int> residual, ReadOnlySpan<int> coeffs, Span<int> output)
            {
                const int Order = 11;
                if(coeffs.Length < Order) return;
                _ = coeffs[Order - 1];
                var vshift = Vector128.CreateScalar((long)shiftsNeeded);
                var mask = Vector128.Create(~0, ~0, ~0, 0);
                ref var c = ref MemoryMarshal.GetReference(coeffs);
                ref var o = ref MemoryMarshal.GetReference(output);
                ref var d = ref Unsafe.Add(ref o, Order);
                int dataLength = output.Length - Order;
                ref var r = ref MemoryMarshal.GetReference(residual);
                Vector128<long> sum;
                Vector256<long> sum256;
                var vcoeff0 = Avx2.ConvertToVector256Int64(Sse2.LoadVector128((int*)Unsafe.AsPointer(ref Unsafe.Add(ref c, 0))).AsUInt32()).AsInt32();
                var vcoeff1 = Avx2.ConvertToVector256Int64(Sse2.LoadVector128((int*)Unsafe.AsPointer(ref Unsafe.Add(ref c, 4))).AsUInt32()).AsInt32();
                var vcoeff2 = Avx2.ConvertToVector256Int64(Avx2.MaskLoad((int*)Unsafe.AsPointer(ref Unsafe.Add(ref c, 8)), mask)).AsInt32();
                var vprev0 = Avx2.ConvertToVector256Int64(Sse2.Shuffle(Sse2.LoadScalarVector128((ulong*)Unsafe.AsPointer(ref Unsafe.Add(ref o, 8))).AsInt32(), 0b00_01_10_11).AsUInt32()).AsInt32();
                var vprev1 = Avx2.ConvertToVector256Int64(Sse2.Shuffle(Sse2.LoadScalarVector128((ulong*)Unsafe.AsPointer(ref Unsafe.Add(ref o, 4))).AsInt32(), 0b00_01_10_11).AsUInt32()).AsInt32();
                var vprev2 = Avx2.ConvertToVector256Int64(Sse2.Shuffle(Avx2.MaskLoad((int*)Unsafe.AsPointer(ref o), mask).AsInt32(), 0b00_01_10_11)).AsInt32();
                for(nint i = 0; i < dataLength;)
                {
                    var res = Vector128.CreateScalar(Unsafe.Add(ref r, i));
                    sum256 = Avx2.Multiply(vcoeff0, vprev0);
                    sum256 = Avx2.Add(sum256, Avx2.Multiply(vcoeff1, vprev1));
                    sum256 = Avx2.Add(sum256, Avx2.Multiply(vcoeff2, vprev2));
                    sum = Sse2.Add(sum256.GetLower(), sum256.GetUpper());
                    sum = Sse2.Add(sum, Sse2.ShiftRightLogical128BitLane(sum, 8));
                    sum = Sse2.ShiftRightLogical(sum, vshift);
                    var yy = Sse2.Add(sum.AsInt32(), res);
#if NET5_0_OR_GREATER
                    Sse2.StoreScalar((int*)Unsafe.AsPointer(ref Unsafe.Add(ref d, i)), yy);
#else
                    Unsafe.Add(ref d, i) = yy.GetElement(0);
#endif
                    i++;
                    var yu = yy.ToVector256();
                    yu = Avx2.Permute4x64(yu.AsUInt64(), 0b00_00_00_00).AsInt32();
                    vprev2 = Avx2.Permute4x64(vprev2.AsInt64(), 0b10_01_00_11).AsInt32();
                    vprev2 = Avx2.Blend(vprev2, vprev1, 0b0000_0001);
                    vprev1 = Avx2.Permute4x64(vprev1.AsInt64(), 0b10_01_00_11).AsInt32();
                    vprev1 = Avx2.Blend(vprev1, vprev0, 0b0000_0001);
                    vprev0 = Avx2.Permute4x64(vprev0.AsInt64(), 0b10_01_00_11).AsInt32();
                    vprev0 = Avx2.Blend(vprev0, yu, 0b0000_0001);
                }
            }
#endregion Order11
#region Order12

            [MethodImpl(OptimizationUtils.InlineAndOptimizeIfPossible)]
            internal static bool RestoreSignalOrder12(int shiftsNeeded, ReadOnlySpan<int> residual, ReadOnlySpan<int> coeffs, Span<int> output)
            {
                if (Sse41.IsSupported)
                {
                    RestoreSignalOrder12Sse41(shiftsNeeded, residual, coeffs, output);
                    return true;
                }
                return false;
            }

            [MethodImpl(OptimizationUtils.InlineAndOptimizeIfPossible)]
            internal static unsafe void RestoreSignalOrder12Sse41(int shiftsNeeded, ReadOnlySpan<int> residual, ReadOnlySpan<int> coeffs, Span<int> output)
            {
                const int Order = 12;
                if (coeffs.Length < Order) return;
                _ = coeffs[Order - 1];
                var vshift = Vector128.CreateScalar(shiftsNeeded);
                ref var o = ref MemoryMarshal.GetReference(output);
                ref var d = ref Unsafe.Add(ref o, Order);
                ref var r = ref MemoryMarshal.GetReference(residual);
                ref var c = ref MemoryMarshal.GetReference(coeffs);
                Vector128<int> sum;
                var vzero = Vector128.Create(0);
                nint dataLength = output.Length - Order;
                var vcoeff0 = Sse2.LoadVector128((int*)Unsafe.AsPointer(ref Unsafe.Add(ref c, 0)));
                var vcoeff1 = Sse2.LoadVector128((int*)Unsafe.AsPointer(ref Unsafe.Add(ref c, 4)));
                var vcoeff2 = Sse2.LoadVector128((int*)Unsafe.AsPointer(ref Unsafe.Add(ref c, 8)));
                var vprev0 = Sse2.Shuffle(Sse2.LoadVector128((int*)Unsafe.AsPointer(ref Unsafe.Add(ref o, 8))).AsInt32(), 0b00_01_10_11);
                var vprev1 = Sse2.Shuffle(Sse2.LoadVector128((int*)Unsafe.AsPointer(ref Unsafe.Add(ref o, 4))).AsInt32(), 0b00_01_10_11);
                var vprev2 = Sse2.Shuffle(Sse2.LoadVector128((int*)Unsafe.AsPointer(ref Unsafe.Add(ref o, 0))).AsInt32(), 0b00_01_10_11);
                var vprev3 = Vector128.Create(0, 0, 0, 0);
                for (nint i = 0; i < dataLength; i++)
                {
                    var res = Vector128.CreateScalar(Unsafe.Add(ref r, i));
                    sum = Sse41.MultiplyLow(vcoeff0, vprev0);
                    sum = Sse2.Add(sum, Sse41.MultiplyLow(vcoeff1, vprev1));
                    sum = Sse2.Add(sum, Sse41.MultiplyLow(vcoeff2, vprev2));
                    var y = Ssse3.HorizontalAdd(sum, vzero);
                    y = Ssse3.HorizontalAdd(y, vzero);
                    y = Sse2.ShiftRightArithmetic(y, vshift);   //C# shift operator handling sucks so SSE2 is used instead(same latency, same throughput).
                    y = Sse2.Add(y, res);   //Avoids extract and insert
#if NET5_0_OR_GREATER
                    Sse2.StoreScalar((int*)Unsafe.AsPointer(ref Unsafe.Add(ref d, i)), y);
#else
                    Unsafe.Add(ref d, i) = y.GetElement(0);
#endif
                    y = Sse2.ShiftLeftLogical128BitLane(y, 12);
                    vprev2 = Ssse3.AlignRight(vprev2, vprev1, 12);
                    vprev1 = Ssse3.AlignRight(vprev1, vprev0, 12);
                    vprev0 = Ssse3.AlignRight(vprev0, y, 12);
                }
            }

            [MethodImpl(OptimizationUtils.InlineAndOptimizeIfPossible)]
            internal static bool RestoreSignalOrder12Wide(int shiftsNeeded, ReadOnlySpan<int> residual, ReadOnlySpan<int> coeffs, Span<int> output)
            {
                if (Sse41.IsSupported)
                {
                    RestoreSignalOrder12WideSse41(shiftsNeeded, residual, coeffs, output);
                    return true;
                }
                return false;
            }

            [MethodImpl(OptimizationUtils.InlineAndOptimizeIfPossible)]
            internal static unsafe void RestoreSignalOrder12WideSse41(int shiftsNeeded, ReadOnlySpan<int> residual, ReadOnlySpan<int> coeffs, Span<int> output)
            {
                const int Order = 12;
                if(coeffs.Length < Order) return;
                _ = coeffs[Order - 1];
                var vshift = Vector128.CreateScalar((long)shiftsNeeded);
                ref var c = ref MemoryMarshal.GetReference(coeffs);
                ref var o = ref MemoryMarshal.GetReference(output);
                ref var d = ref Unsafe.Add(ref o, Order);
                int dataLength = output.Length - Order;
                ref var r = ref MemoryMarshal.GetReference(residual);
                Vector128<long> sum;
                var vcoeff0 = Sse41.ConvertToVector128Int64(Sse2.LoadScalarVector128((ulong*)Unsafe.AsPointer(ref Unsafe.Add(ref c, 0))).AsUInt32()).AsInt32();
                var vcoeff1 = Sse41.ConvertToVector128Int64(Sse2.LoadScalarVector128((ulong*)Unsafe.AsPointer(ref Unsafe.Add(ref c, 2))).AsUInt32()).AsInt32();
                var vcoeff2 = Sse41.ConvertToVector128Int64(Sse2.LoadScalarVector128((ulong*)Unsafe.AsPointer(ref Unsafe.Add(ref c, 4))).AsUInt32()).AsInt32();
                var vcoeff3 = Sse41.ConvertToVector128Int64(Sse2.LoadScalarVector128((ulong*)Unsafe.AsPointer(ref Unsafe.Add(ref c, 6))).AsUInt32()).AsInt32();
                var vcoeff4 = Sse41.ConvertToVector128Int64(Sse2.LoadScalarVector128((ulong*)Unsafe.AsPointer(ref Unsafe.Add(ref c, 8))).AsUInt32()).AsInt32();
                var vcoeff5 = Sse41.ConvertToVector128Int64(Sse2.LoadScalarVector128((ulong*)Unsafe.AsPointer(ref Unsafe.Add(ref c, 10))).AsUInt32()).AsInt32();
                var vprev0 = Sse2.Shuffle(Sse2.LoadScalarVector128((ulong*)Unsafe.AsPointer(ref Unsafe.Add(ref o, 10))).AsInt32(), 0b11_00_11_01);
                var vprev1 = Sse2.Shuffle(Sse2.LoadScalarVector128((ulong*)Unsafe.AsPointer(ref Unsafe.Add(ref o, 8))).AsInt32(), 0b11_00_11_01);
                var vprev2 = Sse2.Shuffle(Sse2.LoadScalarVector128((ulong*)Unsafe.AsPointer(ref Unsafe.Add(ref o, 6))).AsInt32(), 0b11_00_11_01);
                var vprev3 = Sse2.Shuffle(Sse2.LoadScalarVector128((ulong*)Unsafe.AsPointer(ref Unsafe.Add(ref o, 4))).AsInt32(), 0b11_00_11_01);
                var vprev4 = Sse2.Shuffle(Sse2.LoadScalarVector128((ulong*)Unsafe.AsPointer(ref Unsafe.Add(ref o, 2))).AsInt32(), 0b11_00_11_01);
                var vprev5 = Sse2.Shuffle(Sse2.LoadScalarVector128((ulong*)Unsafe.AsPointer(ref Unsafe.Add(ref o, 0))).AsInt32(), 0b11_00_11_01);
                for(nint i = 0; i < dataLength;)
                {
                    var res = Vector128.CreateScalar(Unsafe.Add(ref r, i));
                    sum = Sse41.Multiply(vcoeff0, vprev0);
                    sum = Sse2.Add(sum, Sse41.Multiply(vcoeff1, vprev1));
                    sum = Sse2.Add(sum, Sse41.Multiply(vcoeff2, vprev2));
                    sum = Sse2.Add(sum, Sse41.Multiply(vcoeff3, vprev3));
                    sum = Sse2.Add(sum, Sse41.Multiply(vcoeff4, vprev4));
                    sum = Sse2.Add(sum, Sse41.Multiply(vcoeff5, vprev5));
                    sum = Sse2.Add(sum, Sse2.ShiftRightLogical128BitLane(sum, 8));
                    sum = Sse2.ShiftRightLogical(sum, vshift);
                    var yy = Sse2.Add(sum.AsInt32(), res);
#if NET5_0_OR_GREATER
                    Sse2.StoreScalar((int*)Unsafe.AsPointer(ref Unsafe.Add(ref d, i)), yy);
#else
                    Unsafe.Add(ref d, i) = yy.GetElement(0);
#endif
                    i++;
                    yy = Sse2.ShiftLeftLogical128BitLane(yy, 8);
                    vprev5 = Ssse3.AlignRight(vprev5, vprev4, 8);
                    vprev4 = Ssse3.AlignRight(vprev4, vprev3, 8);
                    vprev3 = Ssse3.AlignRight(vprev3, vprev2, 8);
                    vprev2 = Ssse3.AlignRight(vprev2, vprev1, 8);
                    vprev1 = Ssse3.AlignRight(vprev1, vprev0, 8);
                    vprev0 = Ssse3.AlignRight(vprev0, yy, 8);
                }
            }
            
            [MethodImpl(OptimizationUtils.InlineAndOptimizeIfPossible)]
            internal static unsafe void RestoreSignalOrder12WideAvx2(int shiftsNeeded, ReadOnlySpan<int> residual, ReadOnlySpan<int> coeffs, Span<int> output)
            {
                const int Order = 12;
                if(coeffs.Length < Order) return;
                _ = coeffs[Order - 1];
                var vshift = Vector128.CreateScalar((long)shiftsNeeded);
                ref var c = ref MemoryMarshal.GetReference(coeffs);
                ref var o = ref MemoryMarshal.GetReference(output);
                ref var d = ref Unsafe.Add(ref o, Order);
                int dataLength = output.Length - Order;
                ref var r = ref MemoryMarshal.GetReference(residual);
                Vector128<long> sum;
                Vector256<long> sum256;
                var vcoeff0 = Avx2.ConvertToVector256Int64(Sse2.LoadVector128((int*)Unsafe.AsPointer(ref Unsafe.Add(ref c, 0))).AsUInt32()).AsInt32();
                var vcoeff1 = Avx2.ConvertToVector256Int64(Sse2.LoadVector128((int*)Unsafe.AsPointer(ref Unsafe.Add(ref c, 4))).AsUInt32()).AsInt32();
                var vcoeff2 = Avx2.ConvertToVector256Int64(Sse2.LoadVector128((int*)Unsafe.AsPointer(ref Unsafe.Add(ref c, 8))).AsUInt32()).AsInt32();
                var vprev0 = Avx2.ConvertToVector256Int64(Sse2.Shuffle(Sse2.LoadScalarVector128((ulong*)Unsafe.AsPointer(ref Unsafe.Add(ref o, 8))).AsInt32(), 0b00_01_10_11).AsUInt32()).AsInt32();
                var vprev1 = Avx2.ConvertToVector256Int64(Sse2.Shuffle(Sse2.LoadScalarVector128((ulong*)Unsafe.AsPointer(ref Unsafe.Add(ref o, 4))).AsInt32(), 0b00_01_10_11).AsUInt32()).AsInt32();
                var vprev2 = Avx2.ConvertToVector256Int64(Sse2.Shuffle(Sse2.LoadScalarVector128((ulong*)Unsafe.AsPointer(ref Unsafe.Add(ref o, 0))).AsInt32(), 0b00_01_10_11).AsUInt32()).AsInt32();
                for(nint i = 0; i < dataLength;)
                {
                    var res = Vector128.CreateScalar(Unsafe.Add(ref r, i));
                    sum256 = Avx2.Multiply(vcoeff0, vprev0);
                    sum256 = Avx2.Add(sum256, Avx2.Multiply(vcoeff1, vprev1));
                    sum256 = Avx2.Add(sum256, Avx2.Multiply(vcoeff2, vprev2));
                    sum = Sse2.Add(sum256.GetLower(), sum256.GetUpper());
                    sum = Sse2.Add(sum, Sse2.ShiftRightLogical128BitLane(sum, 8));
                    sum = Sse2.ShiftRightLogical(sum, vshift);
                    var yy = Sse2.Add(sum.AsInt32(), res);
#if NET5_0_OR_GREATER
                    Sse2.StoreScalar((int*)Unsafe.AsPointer(ref Unsafe.Add(ref d, i)), yy);
#else
                    Unsafe.Add(ref d, i) = yy.GetElement(0);
#endif
                    i++;
                    var yu = yy.ToVector256();
                    yu = Avx2.Permute4x64(yu.AsUInt64(), 0b00_00_00_00).AsInt32();
                    vprev2 = Avx2.Permute4x64(vprev2.AsInt64(), 0b10_01_00_11).AsInt32();
                    vprev2 = Avx2.Blend(vprev2, vprev1, 0b0000_0001);
                    vprev1 = Avx2.Permute4x64(vprev1.AsInt64(), 0b10_01_00_11).AsInt32();
                    vprev1 = Avx2.Blend(vprev1, vprev0, 0b0000_0001);
                    vprev0 = Avx2.Permute4x64(vprev0.AsInt64(), 0b10_01_00_11).AsInt32();
                    vprev0 = Avx2.Blend(vprev0, yu, 0b0000_0001);
                }
            }
#endregion Order12
#region Order13

            [MethodImpl(OptimizationUtils.InlineAndOptimizeIfPossible)]
            internal static bool RestoreSignalOrder13(int shiftsNeeded, ReadOnlySpan<int> residual, ReadOnlySpan<int> coeffs, Span<int> output)
            {
                if (Sse41.IsSupported)
                {
                    RestoreSignalOrder13Sse41(shiftsNeeded, residual, coeffs, output);
                    return true;
                }
                return false;
            }

            [MethodImpl(OptimizationUtils.InlineAndOptimizeIfPossible)]
            internal static unsafe void RestoreSignalOrder13Sse41(int shiftsNeeded, ReadOnlySpan<int> residual, ReadOnlySpan<int> coeffs, Span<int> output)
            {
                const int Order = 13;
                if (coeffs.Length < Order) return;
                _ = coeffs[Order - 1];
                var vshift = Vector128.CreateScalar(shiftsNeeded);
                ref var o = ref MemoryMarshal.GetReference(output);
                ref var d = ref Unsafe.Add(ref o, Order);
                ref var r = ref MemoryMarshal.GetReference(residual);
                ref var c = ref MemoryMarshal.GetReference(coeffs);
                Vector128<int> sum;
                var vzero = Vector128.Create(0);
                nint dataLength = output.Length - Order;
                var vcoeff0 = Sse2.LoadVector128((int*)Unsafe.AsPointer(ref Unsafe.Add(ref c, 0)));
                var vcoeff1 = Sse2.LoadVector128((int*)Unsafe.AsPointer(ref Unsafe.Add(ref c, 4)));
                var vcoeff2 = Sse2.LoadVector128((int*)Unsafe.AsPointer(ref Unsafe.Add(ref c, 8)));
                var vcoeff3 = Vector128.Create(Unsafe.Add(ref c, 12), 0, 0, 0);
                var vprev0 = Sse2.Shuffle(Sse2.LoadVector128((int*)Unsafe.AsPointer(ref Unsafe.Add(ref o, 9))).AsInt32(), 0b00_01_10_11);
                var vprev1 = Sse2.Shuffle(Sse2.LoadVector128((int*)Unsafe.AsPointer(ref Unsafe.Add(ref o, 5))).AsInt32(), 0b00_01_10_11);
                var vprev2 = Sse2.Shuffle(Sse2.LoadVector128((int*)Unsafe.AsPointer(ref Unsafe.Add(ref o, 1))).AsInt32(), 0b00_01_10_11);
                var vprev3 = Vector128.Create(Unsafe.Add(ref o, 0), 0, 0, 0);
                for (nint i = 0; i < dataLength; i++)
                {
                    var res = Vector128.CreateScalar(Unsafe.Add(ref r, i));
                    sum = Sse41.MultiplyLow(vcoeff0, vprev0);
                    sum = Sse2.Add(sum, Sse41.MultiplyLow(vcoeff1, vprev1));
                    sum = Sse2.Add(sum, Sse41.MultiplyLow(vcoeff2, vprev2));
                    sum = Sse2.Add(sum, Sse41.MultiplyLow(vcoeff3, vprev3));
                    var y = Ssse3.HorizontalAdd(sum, vzero);
                    y = Ssse3.HorizontalAdd(y, vzero);
                    y = Sse2.ShiftRightArithmetic(y, vshift);   //C# shift operator handling sucks so SSE2 is used instead(same latency, same throughput).
                    y = Sse2.Add(y, res);   //Avoids extract and insert
#if NET5_0_OR_GREATER
                    Sse2.StoreScalar((int*)Unsafe.AsPointer(ref Unsafe.Add(ref d, i)), y);
#else
                    Unsafe.Add(ref d, i) = y.GetElement(0);
#endif
                    y = Sse2.ShiftLeftLogical128BitLane(y, 12);
                    vprev3 = Ssse3.AlignRight(vprev3, vprev2, 12);
                    vprev2 = Ssse3.AlignRight(vprev2, vprev1, 12);
                    vprev1 = Ssse3.AlignRight(vprev1, vprev0, 12);
                    vprev0 = Ssse3.AlignRight(vprev0, y, 12);
                }
            }

            [MethodImpl(OptimizationUtils.InlineAndOptimizeIfPossible)]
            internal static bool RestoreSignalOrder13Wide(int shiftsNeeded, ReadOnlySpan<int> residual, ReadOnlySpan<int> coeffs, Span<int> output)
            {
                if (Sse41.IsSupported)
                {
                    RestoreSignalOrder13WideSse41(shiftsNeeded, residual, coeffs, output);
                    return true;
                }
                return false;
            }

            [MethodImpl(OptimizationUtils.InlineAndOptimizeIfPossible)]
            internal static unsafe void RestoreSignalOrder13WideSse41(int shiftsNeeded, ReadOnlySpan<int> residual, ReadOnlySpan<int> coeffs, Span<int> output)
            {
                const int Order = 13;
                if(coeffs.Length < Order) return;
                _ = coeffs[Order - 1];
                var vshift = Vector128.CreateScalar((long)shiftsNeeded);
                ref var c = ref MemoryMarshal.GetReference(coeffs);
                ref var o = ref MemoryMarshal.GetReference(output);
                ref var d = ref Unsafe.Add(ref o, Order);
                int dataLength = output.Length - Order;
                ref var r = ref MemoryMarshal.GetReference(residual);
                Vector128<long> sum;
                var vcoeff0 = Sse41.ConvertToVector128Int64(Sse2.LoadScalarVector128((ulong*)Unsafe.AsPointer(ref Unsafe.Add(ref c, 0))).AsUInt32()).AsInt32();
                var vcoeff1 = Sse41.ConvertToVector128Int64(Sse2.LoadScalarVector128((ulong*)Unsafe.AsPointer(ref Unsafe.Add(ref c, 2))).AsUInt32()).AsInt32();
                var vcoeff2 = Sse41.ConvertToVector128Int64(Sse2.LoadScalarVector128((ulong*)Unsafe.AsPointer(ref Unsafe.Add(ref c, 4))).AsUInt32()).AsInt32();
                var vcoeff3 = Sse41.ConvertToVector128Int64(Sse2.LoadScalarVector128((ulong*)Unsafe.AsPointer(ref Unsafe.Add(ref c, 6))).AsUInt32()).AsInt32();
                var vcoeff4 = Sse41.ConvertToVector128Int64(Sse2.LoadScalarVector128((ulong*)Unsafe.AsPointer(ref Unsafe.Add(ref c, 8))).AsUInt32()).AsInt32();
                var vcoeff5 = Sse41.ConvertToVector128Int64(Sse2.LoadScalarVector128((ulong*)Unsafe.AsPointer(ref Unsafe.Add(ref c, 10))).AsUInt32()).AsInt32();
                var vcoeff6 = Sse41.ConvertToVector128Int64(Sse2.LoadScalarVector128((uint*)Unsafe.AsPointer(ref Unsafe.Add(ref c, 12))).AsUInt32()).AsInt32();
                var vprev0 = Sse2.Shuffle(Sse2.LoadScalarVector128((ulong*)Unsafe.AsPointer(ref Unsafe.Add(ref o, 11))).AsInt32(), 0b11_00_11_01);
                var vprev1 = Sse2.Shuffle(Sse2.LoadScalarVector128((ulong*)Unsafe.AsPointer(ref Unsafe.Add(ref o, 9))).AsInt32(), 0b11_00_11_01);
                var vprev2 = Sse2.Shuffle(Sse2.LoadScalarVector128((ulong*)Unsafe.AsPointer(ref Unsafe.Add(ref o, 7))).AsInt32(), 0b11_00_11_01);
                var vprev3 = Sse2.Shuffle(Sse2.LoadScalarVector128((ulong*)Unsafe.AsPointer(ref Unsafe.Add(ref o, 5))).AsInt32(), 0b11_00_11_01);
                var vprev4 = Sse2.Shuffle(Sse2.LoadScalarVector128((ulong*)Unsafe.AsPointer(ref Unsafe.Add(ref o, 3))).AsInt32(), 0b11_00_11_01);
                var vprev5 = Sse2.Shuffle(Sse2.LoadScalarVector128((ulong*)Unsafe.AsPointer(ref Unsafe.Add(ref o, 1))).AsInt32(), 0b11_00_11_01);
                var vprev6 = Sse2.Shuffle(Sse2.LoadScalarVector128((ulong*)Unsafe.AsPointer(ref Unsafe.Add(ref o, 0))).AsInt32(), 0b11_00_11_00);
                for(nint i = 0; i < dataLength;)
                {
                    var res = Vector128.CreateScalar(Unsafe.Add(ref r, i));
                    sum = Sse41.Multiply(vcoeff0, vprev0);
                    sum = Sse2.Add(sum, Sse41.Multiply(vcoeff1, vprev1));
                    sum = Sse2.Add(sum, Sse41.Multiply(vcoeff2, vprev2));
                    sum = Sse2.Add(sum, Sse41.Multiply(vcoeff3, vprev3));
                    sum = Sse2.Add(sum, Sse41.Multiply(vcoeff4, vprev4));
                    sum = Sse2.Add(sum, Sse41.Multiply(vcoeff5, vprev5));
                    sum = Sse2.Add(sum, Sse41.Multiply(vcoeff6, vprev6));
                    sum = Sse2.Add(sum, Sse2.ShiftRightLogical128BitLane(sum, 8));
                    sum = Sse2.ShiftRightLogical(sum, vshift);
                    var yy = Sse2.Add(sum.AsInt32(), res);
#if NET5_0_OR_GREATER
                    Sse2.StoreScalar((int*)Unsafe.AsPointer(ref Unsafe.Add(ref d, i)), yy);
#else
                    Unsafe.Add(ref d, i) = yy.GetElement(0);
#endif
                    i++;
                    yy = Sse2.ShiftLeftLogical128BitLane(yy, 8);
                    vprev6 = Ssse3.AlignRight(vprev6, vprev5, 8);
                    vprev5 = Ssse3.AlignRight(vprev5, vprev4, 8);
                    vprev4 = Ssse3.AlignRight(vprev4, vprev3, 8);
                    vprev3 = Ssse3.AlignRight(vprev3, vprev2, 8);
                    vprev2 = Ssse3.AlignRight(vprev2, vprev1, 8);
                    vprev1 = Ssse3.AlignRight(vprev1, vprev0, 8);
                    vprev0 = Ssse3.AlignRight(vprev0, yy, 8);
                }
            }
            
            [MethodImpl(OptimizationUtils.InlineAndOptimizeIfPossible)]
            internal static unsafe void RestoreSignalOrder13WideAvx2(int shiftsNeeded, ReadOnlySpan<int> residual, ReadOnlySpan<int> coeffs, Span<int> output)
            {
                const int Order = 13;
                if(coeffs.Length < Order) return;
                _ = coeffs[Order - 1];
                var vshift = Vector128.CreateScalar((long)shiftsNeeded);
                ref var c = ref MemoryMarshal.GetReference(coeffs);
                ref var o = ref MemoryMarshal.GetReference(output);
                ref var d = ref Unsafe.Add(ref o, Order);
                int dataLength = output.Length - Order;
                ref var r = ref MemoryMarshal.GetReference(residual);
                Vector128<long> sum;
                Vector256<long> sum256;
                var vcoeff0 = Avx2.ConvertToVector256Int64(Sse2.LoadVector128((int*)Unsafe.AsPointer(ref Unsafe.Add(ref c, 0))).AsUInt32()).AsInt32();
                var vcoeff1 = Avx2.ConvertToVector256Int64(Sse2.LoadVector128((int*)Unsafe.AsPointer(ref Unsafe.Add(ref c, 4))).AsUInt32()).AsInt32();
                var vcoeff2 = Avx2.ConvertToVector256Int64(Sse2.LoadVector128((int*)Unsafe.AsPointer(ref Unsafe.Add(ref c, 8))).AsUInt32()).AsInt32();
                var vcoeff3 = Avx2.ConvertToVector256Int64(Sse2.LoadScalarVector128((uint*)Unsafe.AsPointer(ref Unsafe.Add(ref c, 12))).AsUInt32()).AsInt32();
                var vprev0 = Avx2.ConvertToVector256Int64(Sse2.Shuffle(Sse2.LoadScalarVector128((ulong*)Unsafe.AsPointer(ref Unsafe.Add(ref o, 10))).AsInt32(), 0b00_01_10_11).AsUInt32()).AsInt32();
                var vprev1 = Avx2.ConvertToVector256Int64(Sse2.Shuffle(Sse2.LoadScalarVector128((ulong*)Unsafe.AsPointer(ref Unsafe.Add(ref o, 6))).AsInt32(), 0b00_01_10_11).AsUInt32()).AsInt32();
                var vprev2 = Avx2.ConvertToVector256Int64(Sse2.Shuffle(Sse2.LoadScalarVector128((ulong*)Unsafe.AsPointer(ref Unsafe.Add(ref o, 2))).AsInt32(), 0b00_01_10_11).AsUInt32()).AsInt32();
                var vprev3 = Avx2.ConvertToVector256Int64(Sse2.Shuffle(Sse2.LoadScalarVector128((uint*)Unsafe.AsPointer(ref o)).AsInt32(), 0b00_01_10_11).AsUInt32()).AsInt32();
                for(nint i = 0; i < dataLength;)
                {
                    var res = Vector128.CreateScalar(Unsafe.Add(ref r, i));
                    sum256 = Avx2.Multiply(vcoeff0, vprev0);
                    sum256 = Avx2.Add(sum256, Avx2.Multiply(vcoeff1, vprev1));
                    sum256 = Avx2.Add(sum256, Avx2.Multiply(vcoeff2, vprev2));
                    sum256 = Avx2.Add(sum256, Avx2.Multiply(vcoeff3, vprev3));
                    sum = Sse2.Add(sum256.GetLower(), sum256.GetUpper());
                    sum = Sse2.Add(sum, Sse2.ShiftRightLogical128BitLane(sum, 8));
                    sum = Sse2.ShiftRightLogical(sum, vshift);
                    var yy = Sse2.Add(sum.AsInt32(), res);
#if NET5_0_OR_GREATER
                    Sse2.StoreScalar((int*)Unsafe.AsPointer(ref Unsafe.Add(ref d, i)), yy);
#else
                    Unsafe.Add(ref d, i) = yy.GetElement(0);
#endif
                    i++;
                    var yu = yy.ToVector256();
                    yu = Avx2.Permute4x64(yu.AsUInt64(), 0b00_00_00_00).AsInt32();
                    vprev3 = Avx2.Permute4x64(vprev3.AsInt64(), 0b10_01_00_11).AsInt32();
                    vprev3 = Avx2.Blend(vprev3, vprev2, 0b0000_0001);
                    vprev2 = Avx2.Permute4x64(vprev2.AsInt64(), 0b10_01_00_11).AsInt32();
                    vprev2 = Avx2.Blend(vprev2, vprev1, 0b0000_0001);
                    vprev1 = Avx2.Permute4x64(vprev1.AsInt64(), 0b10_01_00_11).AsInt32();
                    vprev1 = Avx2.Blend(vprev1, vprev0, 0b0000_0001);
                    vprev0 = Avx2.Permute4x64(vprev0.AsInt64(), 0b10_01_00_11).AsInt32();
                    vprev0 = Avx2.Blend(vprev0, yu, 0b0000_0001);
                }
            }
#endregion Order13
#region Order14

            [MethodImpl(OptimizationUtils.InlineAndOptimizeIfPossible)]
            internal static bool RestoreSignalOrder14(int shiftsNeeded, ReadOnlySpan<int> residual, ReadOnlySpan<int> coeffs, Span<int> output)
            {
                if (Sse41.IsSupported)
                {
                    RestoreSignalOrder14Sse41(shiftsNeeded, residual, coeffs, output);
                    return true;
                }
                return false;
            }

            [MethodImpl(OptimizationUtils.InlineAndOptimizeIfPossible)]
            internal static unsafe void RestoreSignalOrder14Sse41(int shiftsNeeded, ReadOnlySpan<int> residual, ReadOnlySpan<int> coeffs, Span<int> output)
            {
                const int Order = 14;
                if (coeffs.Length < Order) return;
                _ = coeffs[Order - 1];
                var vshift = Vector128.CreateScalar(shiftsNeeded);
                ref var o = ref MemoryMarshal.GetReference(output);
                ref var d = ref Unsafe.Add(ref o, Order);
                ref var r = ref MemoryMarshal.GetReference(residual);
                ref var c = ref MemoryMarshal.GetReference(coeffs);
                Vector128<int> sum;
                var vzero = Vector128.Create(0);
                nint dataLength = output.Length - Order;
                var vcoeff0 = Sse2.LoadVector128((int*)Unsafe.AsPointer(ref Unsafe.Add(ref c, 0)));
                var vcoeff1 = Sse2.LoadVector128((int*)Unsafe.AsPointer(ref Unsafe.Add(ref c, 4)));
                var vcoeff2 = Sse2.LoadVector128((int*)Unsafe.AsPointer(ref Unsafe.Add(ref c, 8)));
                var vcoeff3 = Vector128.Create(Unsafe.Add(ref c, 12), Unsafe.Add(ref c, 13), 0, 0);
                var vprev0 = Sse2.Shuffle(Sse2.LoadVector128((int*)Unsafe.AsPointer(ref Unsafe.Add(ref o, 10))).AsInt32(), 0b00_01_10_11);
                var vprev1 = Sse2.Shuffle(Sse2.LoadVector128((int*)Unsafe.AsPointer(ref Unsafe.Add(ref o, 6))).AsInt32(), 0b00_01_10_11);
                var vprev2 = Sse2.Shuffle(Sse2.LoadVector128((int*)Unsafe.AsPointer(ref Unsafe.Add(ref o, 2))).AsInt32(), 0b00_01_10_11);
                var vprev3 = Vector128.Create(Unsafe.Add(ref o, 1), Unsafe.Add(ref o, 0), 0, 0);
                for (nint i = 0; i < dataLength; i++)
                {
                    var res = Vector128.CreateScalar(Unsafe.Add(ref r, i));
                    sum = Sse41.MultiplyLow(vcoeff0, vprev0);
                    sum = Sse2.Add(sum, Sse41.MultiplyLow(vcoeff1, vprev1));
                    sum = Sse2.Add(sum, Sse41.MultiplyLow(vcoeff2, vprev2));
                    sum = Sse2.Add(sum, Sse41.MultiplyLow(vcoeff3, vprev3));
                    var y = Ssse3.HorizontalAdd(sum, vzero);
                    y = Ssse3.HorizontalAdd(y, vzero);
                    y = Sse2.ShiftRightArithmetic(y, vshift);   //C# shift operator handling sucks so SSE2 is used instead(same latency, same throughput).
                    y = Sse2.Add(y, res);   //Avoids extract and insert
#if NET5_0_OR_GREATER
                    Sse2.StoreScalar((int*)Unsafe.AsPointer(ref Unsafe.Add(ref d, i)), y);
#else
                    Unsafe.Add(ref d, i) = y.GetElement(0);
#endif
                    y = Sse2.ShiftLeftLogical128BitLane(y, 12);
                    vprev3 = Ssse3.AlignRight(vprev3, vprev2, 12);
                    vprev2 = Ssse3.AlignRight(vprev2, vprev1, 12);
                    vprev1 = Ssse3.AlignRight(vprev1, vprev0, 12);
                    vprev0 = Ssse3.AlignRight(vprev0, y, 12);
                }
            }

            [MethodImpl(OptimizationUtils.InlineAndOptimizeIfPossible)]
            internal static bool RestoreSignalOrder14Wide(int shiftsNeeded, ReadOnlySpan<int> residual, ReadOnlySpan<int> coeffs, Span<int> output)
            {
                if (Sse41.IsSupported)
                {
                    RestoreSignalOrder14WideSse41(shiftsNeeded, residual, coeffs, output);
                    return true;
                }
                return false;
            }

            [MethodImpl(OptimizationUtils.InlineAndOptimizeIfPossible)]
            internal static unsafe void RestoreSignalOrder14WideSse41(int shiftsNeeded, ReadOnlySpan<int> residual, ReadOnlySpan<int> coeffs, Span<int> output)
            {
                const int Order = 14;
                if(coeffs.Length < Order) return;
                _ = coeffs[Order - 1];
                var vshift = Vector128.CreateScalar((long)shiftsNeeded);
                ref var c = ref MemoryMarshal.GetReference(coeffs);
                ref var o = ref MemoryMarshal.GetReference(output);
                ref var d = ref Unsafe.Add(ref o, Order);
                int dataLength = output.Length - Order;
                ref var r = ref MemoryMarshal.GetReference(residual);
                Vector128<long> sum;
                var vcoeff0 = Sse41.ConvertToVector128Int64(Sse2.LoadScalarVector128((ulong*)Unsafe.AsPointer(ref Unsafe.Add(ref c, 0))).AsUInt32()).AsInt32();
                var vcoeff1 = Sse41.ConvertToVector128Int64(Sse2.LoadScalarVector128((ulong*)Unsafe.AsPointer(ref Unsafe.Add(ref c, 2))).AsUInt32()).AsInt32();
                var vcoeff2 = Sse41.ConvertToVector128Int64(Sse2.LoadScalarVector128((ulong*)Unsafe.AsPointer(ref Unsafe.Add(ref c, 4))).AsUInt32()).AsInt32();
                var vcoeff3 = Sse41.ConvertToVector128Int64(Sse2.LoadScalarVector128((ulong*)Unsafe.AsPointer(ref Unsafe.Add(ref c, 6))).AsUInt32()).AsInt32();
                var vcoeff4 = Sse41.ConvertToVector128Int64(Sse2.LoadScalarVector128((ulong*)Unsafe.AsPointer(ref Unsafe.Add(ref c, 8))).AsUInt32()).AsInt32();
                var vcoeff5 = Sse41.ConvertToVector128Int64(Sse2.LoadScalarVector128((ulong*)Unsafe.AsPointer(ref Unsafe.Add(ref c, 10))).AsUInt32()).AsInt32();
                var vcoeff6 = Sse41.ConvertToVector128Int64(Sse2.LoadScalarVector128((ulong*)Unsafe.AsPointer(ref Unsafe.Add(ref c, 12))).AsUInt32()).AsInt32();
                var vprev0 = Sse2.Shuffle(Sse2.LoadScalarVector128((ulong*)Unsafe.AsPointer(ref Unsafe.Add(ref o, 12))).AsInt32(), 0b11_00_11_01);
                var vprev1 = Sse2.Shuffle(Sse2.LoadScalarVector128((ulong*)Unsafe.AsPointer(ref Unsafe.Add(ref o, 10))).AsInt32(), 0b11_00_11_01);
                var vprev2 = Sse2.Shuffle(Sse2.LoadScalarVector128((ulong*)Unsafe.AsPointer(ref Unsafe.Add(ref o, 8))).AsInt32(), 0b11_00_11_01);
                var vprev3 = Sse2.Shuffle(Sse2.LoadScalarVector128((ulong*)Unsafe.AsPointer(ref Unsafe.Add(ref o, 6))).AsInt32(), 0b11_00_11_01);
                var vprev4 = Sse2.Shuffle(Sse2.LoadScalarVector128((ulong*)Unsafe.AsPointer(ref Unsafe.Add(ref o, 4))).AsInt32(), 0b11_00_11_01);
                var vprev5 = Sse2.Shuffle(Sse2.LoadScalarVector128((ulong*)Unsafe.AsPointer(ref Unsafe.Add(ref o, 2))).AsInt32(), 0b11_00_11_01);
                var vprev6 = Sse2.Shuffle(Sse2.LoadScalarVector128((ulong*)Unsafe.AsPointer(ref Unsafe.Add(ref o, 0))).AsInt32(), 0b11_00_11_01);
                for(nint i = 0; i < dataLength;)
                {
                    var res = Vector128.CreateScalar(Unsafe.Add(ref r, i));
                    sum = Sse41.Multiply(vcoeff0, vprev0);
                    sum = Sse2.Add(sum, Sse41.Multiply(vcoeff1, vprev1));
                    sum = Sse2.Add(sum, Sse41.Multiply(vcoeff2, vprev2));
                    sum = Sse2.Add(sum, Sse41.Multiply(vcoeff3, vprev3));
                    sum = Sse2.Add(sum, Sse41.Multiply(vcoeff4, vprev4));
                    sum = Sse2.Add(sum, Sse41.Multiply(vcoeff5, vprev5));
                    sum = Sse2.Add(sum, Sse41.Multiply(vcoeff6, vprev6));
                    sum = Sse2.Add(sum, Sse2.ShiftRightLogical128BitLane(sum, 8));
                    sum = Sse2.ShiftRightLogical(sum, vshift);
                    var yy = Sse2.Add(sum.AsInt32(), res);
#if NET5_0_OR_GREATER
                    Sse2.StoreScalar((int*)Unsafe.AsPointer(ref Unsafe.Add(ref d, i)), yy);
#else
                    Unsafe.Add(ref d, i) = yy.GetElement(0);
#endif
                    i++;
                    yy = Sse2.ShiftLeftLogical128BitLane(yy, 8);
                    vprev6 = Ssse3.AlignRight(vprev6, vprev5, 8);
                    vprev5 = Ssse3.AlignRight(vprev5, vprev4, 8);
                    vprev4 = Ssse3.AlignRight(vprev4, vprev3, 8);
                    vprev3 = Ssse3.AlignRight(vprev3, vprev2, 8);
                    vprev2 = Ssse3.AlignRight(vprev2, vprev1, 8);
                    vprev1 = Ssse3.AlignRight(vprev1, vprev0, 8);
                    vprev0 = Ssse3.AlignRight(vprev0, yy, 8);
                }
            }
            
            [MethodImpl(OptimizationUtils.InlineAndOptimizeIfPossible)]
            internal static unsafe void RestoreSignalOrder14WideAvx2(int shiftsNeeded, ReadOnlySpan<int> residual, ReadOnlySpan<int> coeffs, Span<int> output)
            {
                const int Order = 14;
                if(coeffs.Length < Order) return;
                _ = coeffs[Order - 1];
                var vshift = Vector128.CreateScalar((long)shiftsNeeded);
                ref var c = ref MemoryMarshal.GetReference(coeffs);
                ref var o = ref MemoryMarshal.GetReference(output);
                ref var d = ref Unsafe.Add(ref o, Order);
                int dataLength = output.Length - Order;
                ref var r = ref MemoryMarshal.GetReference(residual);
                Vector128<long> sum;
                Vector256<long> sum256;
                var vcoeff0 = Avx2.ConvertToVector256Int64(Sse2.LoadVector128((int*)Unsafe.AsPointer(ref Unsafe.Add(ref c, 0))).AsUInt32()).AsInt32();
                var vcoeff1 = Avx2.ConvertToVector256Int64(Sse2.LoadVector128((int*)Unsafe.AsPointer(ref Unsafe.Add(ref c, 4))).AsUInt32()).AsInt32();
                var vcoeff2 = Avx2.ConvertToVector256Int64(Sse2.LoadVector128((int*)Unsafe.AsPointer(ref Unsafe.Add(ref c, 8))).AsUInt32()).AsInt32();
                var vcoeff3 = Avx2.ConvertToVector256Int64(Sse2.LoadScalarVector128((ulong*)Unsafe.AsPointer(ref Unsafe.Add(ref c, 12))).AsUInt32()).AsInt32();
                var vprev0 = Avx2.ConvertToVector256Int64(Sse2.Shuffle(Sse2.LoadScalarVector128((ulong*)Unsafe.AsPointer(ref Unsafe.Add(ref o, 11))).AsInt32(), 0b00_01_10_11).AsUInt32()).AsInt32();
                var vprev1 = Avx2.ConvertToVector256Int64(Sse2.Shuffle(Sse2.LoadScalarVector128((ulong*)Unsafe.AsPointer(ref Unsafe.Add(ref o, 7))).AsInt32(), 0b00_01_10_11).AsUInt32()).AsInt32();
                var vprev2 = Avx2.ConvertToVector256Int64(Sse2.Shuffle(Sse2.LoadScalarVector128((ulong*)Unsafe.AsPointer(ref Unsafe.Add(ref o, 3))).AsInt32(), 0b00_01_10_11).AsUInt32()).AsInt32();
                var vprev3 = Avx2.ConvertToVector256Int64(Sse2.Shuffle(Sse2.LoadScalarVector128((ulong*)Unsafe.AsPointer(ref o)).AsInt32(), 0b00_01_10_11).AsUInt32()).AsInt32();
                for(nint i = 0; i < dataLength;)
                {
                    var res = Vector128.CreateScalar(Unsafe.Add(ref r, i));
                    sum256 = Avx2.Multiply(vcoeff0, vprev0);
                    sum256 = Avx2.Add(sum256, Avx2.Multiply(vcoeff1, vprev1));
                    sum256 = Avx2.Add(sum256, Avx2.Multiply(vcoeff2, vprev2));
                    sum256 = Avx2.Add(sum256, Avx2.Multiply(vcoeff3, vprev3));
                    sum = Sse2.Add(sum256.GetLower(), sum256.GetUpper());
                    sum = Sse2.Add(sum, Sse2.ShiftRightLogical128BitLane(sum, 8));
                    sum = Sse2.ShiftRightLogical(sum, vshift);
                    var yy = Sse2.Add(sum.AsInt32(), res);
#if NET5_0_OR_GREATER
                    Sse2.StoreScalar((int*)Unsafe.AsPointer(ref Unsafe.Add(ref d, i)), yy);
#else
                    Unsafe.Add(ref d, i) = yy.GetElement(0);
#endif
                    i++;
                    var yu = yy.ToVector256();
                    yu = Avx2.Permute4x64(yu.AsUInt64(), 0b00_00_00_00).AsInt32();
                    vprev3 = Avx2.Permute4x64(vprev3.AsInt64(), 0b10_01_00_11).AsInt32();
                    vprev3 = Avx2.Blend(vprev3, vprev2, 0b0000_0001);
                    vprev2 = Avx2.Permute4x64(vprev2.AsInt64(), 0b10_01_00_11).AsInt32();
                    vprev2 = Avx2.Blend(vprev2, vprev1, 0b0000_0001);
                    vprev1 = Avx2.Permute4x64(vprev1.AsInt64(), 0b10_01_00_11).AsInt32();
                    vprev1 = Avx2.Blend(vprev1, vprev0, 0b0000_0001);
                    vprev0 = Avx2.Permute4x64(vprev0.AsInt64(), 0b10_01_00_11).AsInt32();
                    vprev0 = Avx2.Blend(vprev0, yu, 0b0000_0001);
                }
            }
#endregion Order14
#region Order15

            [MethodImpl(OptimizationUtils.InlineAndOptimizeIfPossible)]
            internal static bool RestoreSignalOrder15(int shiftsNeeded, ReadOnlySpan<int> residual, ReadOnlySpan<int> coeffs, Span<int> output)
            {
                if (Sse41.IsSupported)
                {
                    RestoreSignalOrder15Sse41(shiftsNeeded, residual, coeffs, output);
                    return true;
                }
                return false;
            }

            [MethodImpl(OptimizationUtils.InlineAndOptimizeIfPossible)]
            internal static unsafe void RestoreSignalOrder15Sse41(int shiftsNeeded, ReadOnlySpan<int> residual, ReadOnlySpan<int> coeffs, Span<int> output)
            {
                const int Order = 15;
                if (coeffs.Length < Order) return;
                _ = coeffs[Order - 1];
                var vshift = Vector128.CreateScalar(shiftsNeeded);
                ref var o = ref MemoryMarshal.GetReference(output);
                ref var d = ref Unsafe.Add(ref o, Order);
                ref var r = ref MemoryMarshal.GetReference(residual);
                ref var c = ref MemoryMarshal.GetReference(coeffs);
                Vector128<int> sum;
                var vzero = Vector128.Create(0);
                nint dataLength = output.Length - Order;
                var vcoeff0 = Sse2.LoadVector128((int*)Unsafe.AsPointer(ref Unsafe.Add(ref c, 0)));
                var vcoeff1 = Sse2.LoadVector128((int*)Unsafe.AsPointer(ref Unsafe.Add(ref c, 4)));
                var vcoeff2 = Sse2.LoadVector128((int*)Unsafe.AsPointer(ref Unsafe.Add(ref c, 8)));
                var vcoeff3 = Vector128.Create(Unsafe.Add(ref c, 12), Unsafe.Add(ref c, 13), Unsafe.Add(ref c, 14), 0);
                var vprev0 = Sse2.Shuffle(Sse2.LoadVector128((int*)Unsafe.AsPointer(ref Unsafe.Add(ref o, 11))).AsInt32(), 0b00_01_10_11);
                var vprev1 = Sse2.Shuffle(Sse2.LoadVector128((int*)Unsafe.AsPointer(ref Unsafe.Add(ref o, 7))).AsInt32(), 0b00_01_10_11);
                var vprev2 = Sse2.Shuffle(Sse2.LoadVector128((int*)Unsafe.AsPointer(ref Unsafe.Add(ref o, 3))).AsInt32(), 0b00_01_10_11);
                var vprev3 = Vector128.Create(Unsafe.Add(ref o, 2), Unsafe.Add(ref o, 1), Unsafe.Add(ref o, 0), 0);
                for (nint i = 0; i < dataLength; i++)
                {
                    var res = Vector128.CreateScalar(Unsafe.Add(ref r, i));
                    sum = Sse41.MultiplyLow(vcoeff0, vprev0);
                    sum = Sse2.Add(sum, Sse41.MultiplyLow(vcoeff1, vprev1));
                    sum = Sse2.Add(sum, Sse41.MultiplyLow(vcoeff2, vprev2));
                    sum = Sse2.Add(sum, Sse41.MultiplyLow(vcoeff3, vprev3));
                    var y = Ssse3.HorizontalAdd(sum, vzero);
                    y = Ssse3.HorizontalAdd(y, vzero);
                    y = Sse2.ShiftRightArithmetic(y, vshift);   //C# shift operator handling sucks so SSE2 is used instead(same latency, same throughput).
                    y = Sse2.Add(y, res);   //Avoids extract and insert
#if NET5_0_OR_GREATER
                    Sse2.StoreScalar((int*)Unsafe.AsPointer(ref Unsafe.Add(ref d, i)), y);
#else
                    Unsafe.Add(ref d, i) = y.GetElement(0);
#endif
                    y = Sse2.ShiftLeftLogical128BitLane(y, 12);
                    vprev3 = Ssse3.AlignRight(vprev3, vprev2, 12);
                    vprev2 = Ssse3.AlignRight(vprev2, vprev1, 12);
                    vprev1 = Ssse3.AlignRight(vprev1, vprev0, 12);
                    vprev0 = Ssse3.AlignRight(vprev0, y, 12);
                }
            }

            [MethodImpl(OptimizationUtils.InlineAndOptimizeIfPossible)]
            internal static bool RestoreSignalOrder15Wide(int shiftsNeeded, ReadOnlySpan<int> residual, ReadOnlySpan<int> coeffs, Span<int> output)
            {
                if (Sse41.IsSupported)
                {
                    RestoreSignalOrder15WideSse41(shiftsNeeded, residual, coeffs, output);
                    return true;
                }
                return false;
            }

            [MethodImpl(OptimizationUtils.InlineAndOptimizeIfPossible)]
            internal static unsafe void RestoreSignalOrder15WideSse41(int shiftsNeeded, ReadOnlySpan<int> residual, ReadOnlySpan<int> coeffs, Span<int> output)
            {
                const int Order = 15;
                if(coeffs.Length < Order) return;
                _ = coeffs[Order - 1];
                var vshift = Vector128.CreateScalar((long)shiftsNeeded);
                ref var c = ref MemoryMarshal.GetReference(coeffs);
                ref var o = ref MemoryMarshal.GetReference(output);
                ref var d = ref Unsafe.Add(ref o, Order);
                int dataLength = output.Length - Order;
                ref var r = ref MemoryMarshal.GetReference(residual);
                Vector128<long> sum;
                var vcoeff0 = Sse41.ConvertToVector128Int64(Sse2.LoadScalarVector128((ulong*)Unsafe.AsPointer(ref Unsafe.Add(ref c, 0))).AsUInt32()).AsInt32();
                var vcoeff1 = Sse41.ConvertToVector128Int64(Sse2.LoadScalarVector128((ulong*)Unsafe.AsPointer(ref Unsafe.Add(ref c, 2))).AsUInt32()).AsInt32();
                var vcoeff2 = Sse41.ConvertToVector128Int64(Sse2.LoadScalarVector128((ulong*)Unsafe.AsPointer(ref Unsafe.Add(ref c, 4))).AsUInt32()).AsInt32();
                var vcoeff3 = Sse41.ConvertToVector128Int64(Sse2.LoadScalarVector128((ulong*)Unsafe.AsPointer(ref Unsafe.Add(ref c, 6))).AsUInt32()).AsInt32();
                var vcoeff4 = Sse41.ConvertToVector128Int64(Sse2.LoadScalarVector128((ulong*)Unsafe.AsPointer(ref Unsafe.Add(ref c, 8))).AsUInt32()).AsInt32();
                var vcoeff5 = Sse41.ConvertToVector128Int64(Sse2.LoadScalarVector128((ulong*)Unsafe.AsPointer(ref Unsafe.Add(ref c, 10))).AsUInt32()).AsInt32();
                var vcoeff6 = Sse41.ConvertToVector128Int64(Sse2.LoadScalarVector128((ulong*)Unsafe.AsPointer(ref Unsafe.Add(ref c, 12))).AsUInt32()).AsInt32();
                var vcoeff7 = Sse41.ConvertToVector128Int64(Sse2.LoadScalarVector128((uint*)Unsafe.AsPointer(ref Unsafe.Add(ref c, 14))).AsUInt32()).AsInt32();
                var vprev0 = Sse2.Shuffle(Sse2.LoadScalarVector128((ulong*)Unsafe.AsPointer(ref Unsafe.Add(ref o, 13))).AsInt32(), 0b11_00_11_01);
                var vprev1 = Sse2.Shuffle(Sse2.LoadScalarVector128((ulong*)Unsafe.AsPointer(ref Unsafe.Add(ref o, 11))).AsInt32(), 0b11_00_11_01);
                var vprev2 = Sse2.Shuffle(Sse2.LoadScalarVector128((ulong*)Unsafe.AsPointer(ref Unsafe.Add(ref o, 9))).AsInt32(), 0b11_00_11_01);
                var vprev3 = Sse2.Shuffle(Sse2.LoadScalarVector128((ulong*)Unsafe.AsPointer(ref Unsafe.Add(ref o, 7))).AsInt32(), 0b11_00_11_01);
                var vprev4 = Sse2.Shuffle(Sse2.LoadScalarVector128((ulong*)Unsafe.AsPointer(ref Unsafe.Add(ref o, 5))).AsInt32(), 0b11_00_11_01);
                var vprev5 = Sse2.Shuffle(Sse2.LoadScalarVector128((ulong*)Unsafe.AsPointer(ref Unsafe.Add(ref o, 3))).AsInt32(), 0b11_00_11_01);
                var vprev6 = Sse2.Shuffle(Sse2.LoadScalarVector128((ulong*)Unsafe.AsPointer(ref Unsafe.Add(ref o, 1))).AsInt32(), 0b11_00_11_01);
                var vprev7 = Sse2.Shuffle(Sse2.LoadScalarVector128((ulong*)Unsafe.AsPointer(ref Unsafe.Add(ref o, 0))).AsInt32(), 0b11_00_11_00);
                for(nint i = 0; i < dataLength;)
                {
                    var res = Vector128.CreateScalar(Unsafe.Add(ref r, i));
                    sum = Sse41.Multiply(vcoeff0, vprev0);
                    sum = Sse2.Add(sum, Sse41.Multiply(vcoeff1, vprev1));
                    sum = Sse2.Add(sum, Sse41.Multiply(vcoeff2, vprev2));
                    sum = Sse2.Add(sum, Sse41.Multiply(vcoeff3, vprev3));
                    sum = Sse2.Add(sum, Sse41.Multiply(vcoeff4, vprev4));
                    sum = Sse2.Add(sum, Sse41.Multiply(vcoeff5, vprev5));
                    sum = Sse2.Add(sum, Sse41.Multiply(vcoeff6, vprev6));
                    sum = Sse2.Add(sum, Sse41.Multiply(vcoeff7, vprev7));
                    sum = Sse2.Add(sum, Sse2.ShiftRightLogical128BitLane(sum, 8));
                    sum = Sse2.ShiftRightLogical(sum, vshift);
                    var yy = Sse2.Add(sum.AsInt32(), res);
#if NET5_0_OR_GREATER
                    Sse2.StoreScalar((int*)Unsafe.AsPointer(ref Unsafe.Add(ref d, i)), yy);
#else
                    Unsafe.Add(ref d, i) = yy.GetElement(0);
#endif
                    i++;
                    yy = Sse2.ShiftLeftLogical128BitLane(yy, 8);
                    vprev7 = Ssse3.AlignRight(vprev7, vprev6, 8);
                    vprev6 = Ssse3.AlignRight(vprev6, vprev5, 8);
                    vprev5 = Ssse3.AlignRight(vprev5, vprev4, 8);
                    vprev4 = Ssse3.AlignRight(vprev4, vprev3, 8);
                    vprev3 = Ssse3.AlignRight(vprev3, vprev2, 8);
                    vprev2 = Ssse3.AlignRight(vprev2, vprev1, 8);
                    vprev1 = Ssse3.AlignRight(vprev1, vprev0, 8);
                    vprev0 = Ssse3.AlignRight(vprev0, yy, 8);
                }
            }
            
            [MethodImpl(OptimizationUtils.InlineAndOptimizeIfPossible)]
            internal static unsafe void RestoreSignalOrder15WideAvx2(int shiftsNeeded, ReadOnlySpan<int> residual, ReadOnlySpan<int> coeffs, Span<int> output)
            {
                const int Order = 15;
                if(coeffs.Length < Order) return;
                _ = coeffs[Order - 1];
                var vshift = Vector128.CreateScalar((long)shiftsNeeded);
                var mask = Vector128.Create(~0, ~0, ~0, 0);
                ref var c = ref MemoryMarshal.GetReference(coeffs);
                ref var o = ref MemoryMarshal.GetReference(output);
                ref var d = ref Unsafe.Add(ref o, Order);
                int dataLength = output.Length - Order;
                ref var r = ref MemoryMarshal.GetReference(residual);
                Vector128<long> sum;
                Vector256<long> sum256;
                var vcoeff0 = Avx2.ConvertToVector256Int64(Sse2.LoadVector128((int*)Unsafe.AsPointer(ref Unsafe.Add(ref c, 0))).AsUInt32()).AsInt32();
                var vcoeff1 = Avx2.ConvertToVector256Int64(Sse2.LoadVector128((int*)Unsafe.AsPointer(ref Unsafe.Add(ref c, 4))).AsUInt32()).AsInt32();
                var vcoeff2 = Avx2.ConvertToVector256Int64(Sse2.LoadVector128((int*)Unsafe.AsPointer(ref Unsafe.Add(ref c, 8))).AsUInt32()).AsInt32();
                var vcoeff3 = Avx2.ConvertToVector256Int64(Avx2.MaskLoad((int*)Unsafe.AsPointer(ref Unsafe.Add(ref c, 12)), mask)).AsInt32();
                var vprev0 = Avx2.ConvertToVector256Int64(Sse2.Shuffle(Sse2.LoadScalarVector128((ulong*)Unsafe.AsPointer(ref Unsafe.Add(ref o, 12))).AsInt32(), 0b00_01_10_11).AsUInt32()).AsInt32();
                var vprev1 = Avx2.ConvertToVector256Int64(Sse2.Shuffle(Sse2.LoadScalarVector128((ulong*)Unsafe.AsPointer(ref Unsafe.Add(ref o, 8))).AsInt32(), 0b00_01_10_11).AsUInt32()).AsInt32();
                var vprev2 = Avx2.ConvertToVector256Int64(Sse2.Shuffle(Sse2.LoadScalarVector128((ulong*)Unsafe.AsPointer(ref Unsafe.Add(ref o, 4))).AsInt32(), 0b00_01_10_11).AsUInt32()).AsInt32();
                var vprev3 = Avx2.ConvertToVector256Int64(Sse2.Shuffle(Avx2.MaskLoad((int*)Unsafe.AsPointer(ref o), mask).AsInt32(), 0b00_01_10_11)).AsInt32();
                for(nint i = 0; i < dataLength;)
                {
                    var res = Vector128.CreateScalar(Unsafe.Add(ref r, i));
                    sum256 = Avx2.Multiply(vcoeff0, vprev0);
                    sum256 = Avx2.Add(sum256, Avx2.Multiply(vcoeff1, vprev1));
                    sum256 = Avx2.Add(sum256, Avx2.Multiply(vcoeff2, vprev2));
                    sum256 = Avx2.Add(sum256, Avx2.Multiply(vcoeff3, vprev3));
                    sum = Sse2.Add(sum256.GetLower(), sum256.GetUpper());
                    sum = Sse2.Add(sum, Sse2.ShiftRightLogical128BitLane(sum, 8));
                    sum = Sse2.ShiftRightLogical(sum, vshift);
                    var yy = Sse2.Add(sum.AsInt32(), res);
#if NET5_0_OR_GREATER
                    Sse2.StoreScalar((int*)Unsafe.AsPointer(ref Unsafe.Add(ref d, i)), yy);
#else
                    Unsafe.Add(ref d, i) = yy.GetElement(0);
#endif
                    i++;
                    var yu = yy.ToVector256();
                    yu = Avx2.Permute4x64(yu.AsUInt64(), 0b00_00_00_00).AsInt32();
                    vprev3 = Avx2.Permute4x64(vprev3.AsInt64(), 0b10_01_00_11).AsInt32();
                    vprev3 = Avx2.Blend(vprev3, vprev2, 0b0000_0001);
                    vprev2 = Avx2.Permute4x64(vprev2.AsInt64(), 0b10_01_00_11).AsInt32();
                    vprev2 = Avx2.Blend(vprev2, vprev1, 0b0000_0001);
                    vprev1 = Avx2.Permute4x64(vprev1.AsInt64(), 0b10_01_00_11).AsInt32();
                    vprev1 = Avx2.Blend(vprev1, vprev0, 0b0000_0001);
                    vprev0 = Avx2.Permute4x64(vprev0.AsInt64(), 0b10_01_00_11).AsInt32();
                    vprev0 = Avx2.Blend(vprev0, yu, 0b0000_0001);
                }
            }
#endregion Order15
#region Order16

            [MethodImpl(OptimizationUtils.InlineAndOptimizeIfPossible)]
            internal static bool RestoreSignalOrder16(int shiftsNeeded, ReadOnlySpan<int> residual, ReadOnlySpan<int> coeffs, Span<int> output)
            {
                if (Sse41.IsSupported)
                {
                    RestoreSignalOrder16Sse41(shiftsNeeded, residual, coeffs, output);
                    return true;
                }
                return false;
            }

            [MethodImpl(OptimizationUtils.InlineAndOptimizeIfPossible)]
            internal static unsafe void RestoreSignalOrder16Sse41(int shiftsNeeded, ReadOnlySpan<int> residual, ReadOnlySpan<int> coeffs, Span<int> output)
            {
                const int Order = 16;
                if (coeffs.Length < Order) return;
                _ = coeffs[Order - 1];
                var vshift = Vector128.CreateScalar(shiftsNeeded);
                ref var o = ref MemoryMarshal.GetReference(output);
                ref var d = ref Unsafe.Add(ref o, Order);
                ref var r = ref MemoryMarshal.GetReference(residual);
                ref var c = ref MemoryMarshal.GetReference(coeffs);
                Vector128<int> sum;
                var vzero = Vector128.Create(0);
                nint dataLength = output.Length - Order;
                var vcoeff0 = Sse2.LoadVector128((int*)Unsafe.AsPointer(ref Unsafe.Add(ref c, 0)));
                var vcoeff1 = Sse2.LoadVector128((int*)Unsafe.AsPointer(ref Unsafe.Add(ref c, 4)));
                var vcoeff2 = Sse2.LoadVector128((int*)Unsafe.AsPointer(ref Unsafe.Add(ref c, 8)));
                var vcoeff3 = Sse2.LoadVector128((int*)Unsafe.AsPointer(ref Unsafe.Add(ref c, 12)));
                var vprev0 = Sse2.Shuffle(Sse2.LoadVector128((int*)Unsafe.AsPointer(ref Unsafe.Add(ref o, 12))).AsInt32(), 0b00_01_10_11);
                var vprev1 = Sse2.Shuffle(Sse2.LoadVector128((int*)Unsafe.AsPointer(ref Unsafe.Add(ref o, 8))).AsInt32(), 0b00_01_10_11);
                var vprev2 = Sse2.Shuffle(Sse2.LoadVector128((int*)Unsafe.AsPointer(ref Unsafe.Add(ref o, 4))).AsInt32(), 0b00_01_10_11);
                var vprev3 = Sse2.Shuffle(Sse2.LoadVector128((int*)Unsafe.AsPointer(ref Unsafe.Add(ref o, 0))).AsInt32(), 0b00_01_10_11);
                var vprev4 = Vector128.Create(0, 0, 0, 0);
                for (nint i = 0; i < dataLength; i++)
                {
                    var res = Vector128.CreateScalar(Unsafe.Add(ref r, i));
                    sum = Sse41.MultiplyLow(vcoeff0, vprev0);
                    sum = Sse2.Add(sum, Sse41.MultiplyLow(vcoeff1, vprev1));
                    sum = Sse2.Add(sum, Sse41.MultiplyLow(vcoeff2, vprev2));
                    sum = Sse2.Add(sum, Sse41.MultiplyLow(vcoeff3, vprev3));
                    var y = Ssse3.HorizontalAdd(sum, vzero);
                    y = Ssse3.HorizontalAdd(y, vzero);
                    y = Sse2.ShiftRightArithmetic(y, vshift);   //C# shift operator handling sucks so SSE2 is used instead(same latency, same throughput).
                    y = Sse2.Add(y, res);   //Avoids extract and insert
#if NET5_0_OR_GREATER
                    Sse2.StoreScalar((int*)Unsafe.AsPointer(ref Unsafe.Add(ref d, i)), y);
#else
                    Unsafe.Add(ref d, i) = y.GetElement(0);
#endif
                    y = Sse2.ShiftLeftLogical128BitLane(y, 12);
                    vprev3 = Ssse3.AlignRight(vprev3, vprev2, 12);
                    vprev2 = Ssse3.AlignRight(vprev2, vprev1, 12);
                    vprev1 = Ssse3.AlignRight(vprev1, vprev0, 12);
                    vprev0 = Ssse3.AlignRight(vprev0, y, 12);
                }
            }

            [MethodImpl(OptimizationUtils.InlineAndOptimizeIfPossible)]
            internal static bool RestoreSignalOrder16Wide(int shiftsNeeded, ReadOnlySpan<int> residual, ReadOnlySpan<int> coeffs, Span<int> output)
            {
                if (Sse41.IsSupported)
                {
                    RestoreSignalOrder16WideSse41(shiftsNeeded, residual, coeffs, output);
                    return true;
                }
                return false;
            }

            [MethodImpl(OptimizationUtils.InlineAndOptimizeIfPossible)]
            internal static unsafe void RestoreSignalOrder16WideSse41(int shiftsNeeded, ReadOnlySpan<int> residual, ReadOnlySpan<int> coeffs, Span<int> output)
            {
                const int Order = 16;
                if(coeffs.Length < Order) return;
                _ = coeffs[Order - 1];
                var vshift = Vector128.CreateScalar((long)shiftsNeeded);
                ref var c = ref MemoryMarshal.GetReference(coeffs);
                ref var o = ref MemoryMarshal.GetReference(output);
                ref var d = ref Unsafe.Add(ref o, Order);
                int dataLength = output.Length - Order;
                ref var r = ref MemoryMarshal.GetReference(residual);
                Vector128<long> sum;
                var vcoeff0 = Sse41.ConvertToVector128Int64(Sse2.LoadScalarVector128((ulong*)Unsafe.AsPointer(ref Unsafe.Add(ref c, 0))).AsUInt32()).AsInt32();
                var vcoeff1 = Sse41.ConvertToVector128Int64(Sse2.LoadScalarVector128((ulong*)Unsafe.AsPointer(ref Unsafe.Add(ref c, 2))).AsUInt32()).AsInt32();
                var vcoeff2 = Sse41.ConvertToVector128Int64(Sse2.LoadScalarVector128((ulong*)Unsafe.AsPointer(ref Unsafe.Add(ref c, 4))).AsUInt32()).AsInt32();
                var vcoeff3 = Sse41.ConvertToVector128Int64(Sse2.LoadScalarVector128((ulong*)Unsafe.AsPointer(ref Unsafe.Add(ref c, 6))).AsUInt32()).AsInt32();
                var vcoeff4 = Sse41.ConvertToVector128Int64(Sse2.LoadScalarVector128((ulong*)Unsafe.AsPointer(ref Unsafe.Add(ref c, 8))).AsUInt32()).AsInt32();
                var vcoeff5 = Sse41.ConvertToVector128Int64(Sse2.LoadScalarVector128((ulong*)Unsafe.AsPointer(ref Unsafe.Add(ref c, 10))).AsUInt32()).AsInt32();
                var vcoeff6 = Sse41.ConvertToVector128Int64(Sse2.LoadScalarVector128((ulong*)Unsafe.AsPointer(ref Unsafe.Add(ref c, 12))).AsUInt32()).AsInt32();
                var vcoeff7 = Sse41.ConvertToVector128Int64(Sse2.LoadScalarVector128((ulong*)Unsafe.AsPointer(ref Unsafe.Add(ref c, 14))).AsUInt32()).AsInt32();
                var vprev0 = Sse2.Shuffle(Sse2.LoadScalarVector128((ulong*)Unsafe.AsPointer(ref Unsafe.Add(ref o, 14))).AsInt32(), 0b11_00_11_01);
                var vprev1 = Sse2.Shuffle(Sse2.LoadScalarVector128((ulong*)Unsafe.AsPointer(ref Unsafe.Add(ref o, 12))).AsInt32(), 0b11_00_11_01);
                var vprev2 = Sse2.Shuffle(Sse2.LoadScalarVector128((ulong*)Unsafe.AsPointer(ref Unsafe.Add(ref o, 10))).AsInt32(), 0b11_00_11_01);
                var vprev3 = Sse2.Shuffle(Sse2.LoadScalarVector128((ulong*)Unsafe.AsPointer(ref Unsafe.Add(ref o, 8))).AsInt32(), 0b11_00_11_01);
                var vprev4 = Sse2.Shuffle(Sse2.LoadScalarVector128((ulong*)Unsafe.AsPointer(ref Unsafe.Add(ref o, 6))).AsInt32(), 0b11_00_11_01);
                var vprev5 = Sse2.Shuffle(Sse2.LoadScalarVector128((ulong*)Unsafe.AsPointer(ref Unsafe.Add(ref o, 4))).AsInt32(), 0b11_00_11_01);
                var vprev6 = Sse2.Shuffle(Sse2.LoadScalarVector128((ulong*)Unsafe.AsPointer(ref Unsafe.Add(ref o, 2))).AsInt32(), 0b11_00_11_01);
                var vprev7 = Sse2.Shuffle(Sse2.LoadScalarVector128((ulong*)Unsafe.AsPointer(ref Unsafe.Add(ref o, 0))).AsInt32(), 0b11_00_11_01);
                for(nint i = 0; i < dataLength;)
                {
                    var res = Vector128.CreateScalar(Unsafe.Add(ref r, i));
                    sum = Sse41.Multiply(vcoeff0, vprev0);
                    sum = Sse2.Add(sum, Sse41.Multiply(vcoeff1, vprev1));
                    sum = Sse2.Add(sum, Sse41.Multiply(vcoeff2, vprev2));
                    sum = Sse2.Add(sum, Sse41.Multiply(vcoeff3, vprev3));
                    sum = Sse2.Add(sum, Sse41.Multiply(vcoeff4, vprev4));
                    sum = Sse2.Add(sum, Sse41.Multiply(vcoeff5, vprev5));
                    sum = Sse2.Add(sum, Sse41.Multiply(vcoeff6, vprev6));
                    sum = Sse2.Add(sum, Sse41.Multiply(vcoeff7, vprev7));
                    sum = Sse2.Add(sum, Sse2.ShiftRightLogical128BitLane(sum, 8));
                    sum = Sse2.ShiftRightLogical(sum, vshift);
                    var yy = Sse2.Add(sum.AsInt32(), res);
#if NET5_0_OR_GREATER
                    Sse2.StoreScalar((int*)Unsafe.AsPointer(ref Unsafe.Add(ref d, i)), yy);
#else
                    Unsafe.Add(ref d, i) = yy.GetElement(0);
#endif
                    i++;
                    yy = Sse2.ShiftLeftLogical128BitLane(yy, 8);
                    vprev7 = Ssse3.AlignRight(vprev7, vprev6, 8);
                    vprev6 = Ssse3.AlignRight(vprev6, vprev5, 8);
                    vprev5 = Ssse3.AlignRight(vprev5, vprev4, 8);
                    vprev4 = Ssse3.AlignRight(vprev4, vprev3, 8);
                    vprev3 = Ssse3.AlignRight(vprev3, vprev2, 8);
                    vprev2 = Ssse3.AlignRight(vprev2, vprev1, 8);
                    vprev1 = Ssse3.AlignRight(vprev1, vprev0, 8);
                    vprev0 = Ssse3.AlignRight(vprev0, yy, 8);
                }
            }
            
            [MethodImpl(OptimizationUtils.InlineAndOptimizeIfPossible)]
            internal static unsafe void RestoreSignalOrder16WideAvx2(int shiftsNeeded, ReadOnlySpan<int> residual, ReadOnlySpan<int> coeffs, Span<int> output)
            {
                const int Order = 16;
                if(coeffs.Length < Order) return;
                _ = coeffs[Order - 1];
                var vshift = Vector128.CreateScalar((long)shiftsNeeded);
                ref var c = ref MemoryMarshal.GetReference(coeffs);
                ref var o = ref MemoryMarshal.GetReference(output);
                ref var d = ref Unsafe.Add(ref o, Order);
                int dataLength = output.Length - Order;
                ref var r = ref MemoryMarshal.GetReference(residual);
                Vector128<long> sum;
                Vector256<long> sum256;
                var vcoeff0 = Avx2.ConvertToVector256Int64(Sse2.LoadVector128((int*)Unsafe.AsPointer(ref Unsafe.Add(ref c, 0))).AsUInt32()).AsInt32();
                var vcoeff1 = Avx2.ConvertToVector256Int64(Sse2.LoadVector128((int*)Unsafe.AsPointer(ref Unsafe.Add(ref c, 4))).AsUInt32()).AsInt32();
                var vcoeff2 = Avx2.ConvertToVector256Int64(Sse2.LoadVector128((int*)Unsafe.AsPointer(ref Unsafe.Add(ref c, 8))).AsUInt32()).AsInt32();
                var vcoeff3 = Avx2.ConvertToVector256Int64(Sse2.LoadVector128((int*)Unsafe.AsPointer(ref Unsafe.Add(ref c, 12))).AsUInt32()).AsInt32();
                var vprev0 = Avx2.ConvertToVector256Int64(Sse2.Shuffle(Sse2.LoadScalarVector128((ulong*)Unsafe.AsPointer(ref Unsafe.Add(ref o, 12))).AsInt32(), 0b00_01_10_11).AsUInt32()).AsInt32();
                var vprev1 = Avx2.ConvertToVector256Int64(Sse2.Shuffle(Sse2.LoadScalarVector128((ulong*)Unsafe.AsPointer(ref Unsafe.Add(ref o, 8))).AsInt32(), 0b00_01_10_11).AsUInt32()).AsInt32();
                var vprev2 = Avx2.ConvertToVector256Int64(Sse2.Shuffle(Sse2.LoadScalarVector128((ulong*)Unsafe.AsPointer(ref Unsafe.Add(ref o, 4))).AsInt32(), 0b00_01_10_11).AsUInt32()).AsInt32();
                var vprev3 = Avx2.ConvertToVector256Int64(Sse2.Shuffle(Sse2.LoadScalarVector128((ulong*)Unsafe.AsPointer(ref Unsafe.Add(ref o, 0))).AsInt32(), 0b00_01_10_11).AsUInt32()).AsInt32();
                for(nint i = 0; i < dataLength;)
                {
                    var res = Vector128.CreateScalar(Unsafe.Add(ref r, i));
                    sum256 = Avx2.Multiply(vcoeff0, vprev0);
                    sum256 = Avx2.Add(sum256, Avx2.Multiply(vcoeff1, vprev1));
                    sum256 = Avx2.Add(sum256, Avx2.Multiply(vcoeff2, vprev2));
                    sum256 = Avx2.Add(sum256, Avx2.Multiply(vcoeff3, vprev3));
                    sum = Sse2.Add(sum256.GetLower(), sum256.GetUpper());
                    sum = Sse2.Add(sum, Sse2.ShiftRightLogical128BitLane(sum, 8));
                    sum = Sse2.ShiftRightLogical(sum, vshift);
                    var yy = Sse2.Add(sum.AsInt32(), res);
#if NET5_0_OR_GREATER
                    Sse2.StoreScalar((int*)Unsafe.AsPointer(ref Unsafe.Add(ref d, i)), yy);
#else
                    Unsafe.Add(ref d, i) = yy.GetElement(0);
#endif
                    i++;
                    var yu = yy.ToVector256();
                    yu = Avx2.Permute4x64(yu.AsUInt64(), 0b00_00_00_00).AsInt32();
                    vprev3 = Avx2.Permute4x64(vprev3.AsInt64(), 0b10_01_00_11).AsInt32();
                    vprev3 = Avx2.Blend(vprev3, vprev2, 0b0000_0001);
                    vprev2 = Avx2.Permute4x64(vprev2.AsInt64(), 0b10_01_00_11).AsInt32();
                    vprev2 = Avx2.Blend(vprev2, vprev1, 0b0000_0001);
                    vprev1 = Avx2.Permute4x64(vprev1.AsInt64(), 0b10_01_00_11).AsInt32();
                    vprev1 = Avx2.Blend(vprev1, vprev0, 0b0000_0001);
                    vprev0 = Avx2.Permute4x64(vprev0.AsInt64(), 0b10_01_00_11).AsInt32();
                    vprev0 = Avx2.Blend(vprev0, yu, 0b0000_0001);
                }
            }
#endregion Order16
#region Order17

            [MethodImpl(OptimizationUtils.InlineAndOptimizeIfPossible)]
            internal static bool RestoreSignalOrder17(int shiftsNeeded, ReadOnlySpan<int> residual, ReadOnlySpan<int> coeffs, Span<int> output)
            {
                if (Sse41.IsSupported)
                {
                    RestoreSignalOrder17Sse41(shiftsNeeded, residual, coeffs, output);
                    return true;
                }
                return false;
            }

            [MethodImpl(OptimizationUtils.InlineAndOptimizeIfPossible)]
            internal static unsafe void RestoreSignalOrder17Sse41(int shiftsNeeded, ReadOnlySpan<int> residual, ReadOnlySpan<int> coeffs, Span<int> output)
            {
                const int Order = 17;
                if (coeffs.Length < Order) return;
                _ = coeffs[Order - 1];
                var vshift = Vector128.CreateScalar(shiftsNeeded);
                ref var o = ref MemoryMarshal.GetReference(output);
                ref var d = ref Unsafe.Add(ref o, Order);
                ref var r = ref MemoryMarshal.GetReference(residual);
                ref var c = ref MemoryMarshal.GetReference(coeffs);
                Vector128<int> sum;
                var vzero = Vector128.Create(0);
                nint dataLength = output.Length - Order;
                var vcoeff0 = Sse2.LoadVector128((int*)Unsafe.AsPointer(ref Unsafe.Add(ref c, 0)));
                var vcoeff1 = Sse2.LoadVector128((int*)Unsafe.AsPointer(ref Unsafe.Add(ref c, 4)));
                var vcoeff2 = Sse2.LoadVector128((int*)Unsafe.AsPointer(ref Unsafe.Add(ref c, 8)));
                var vcoeff3 = Sse2.LoadVector128((int*)Unsafe.AsPointer(ref Unsafe.Add(ref c, 12)));
                var vcoeff4 = Vector128.Create(Unsafe.Add(ref c, 16), 0, 0, 0);
                var vprev0 = Sse2.Shuffle(Sse2.LoadVector128((int*)Unsafe.AsPointer(ref Unsafe.Add(ref o, 13))).AsInt32(), 0b00_01_10_11);
                var vprev1 = Sse2.Shuffle(Sse2.LoadVector128((int*)Unsafe.AsPointer(ref Unsafe.Add(ref o, 9))).AsInt32(), 0b00_01_10_11);
                var vprev2 = Sse2.Shuffle(Sse2.LoadVector128((int*)Unsafe.AsPointer(ref Unsafe.Add(ref o, 5))).AsInt32(), 0b00_01_10_11);
                var vprev3 = Sse2.Shuffle(Sse2.LoadVector128((int*)Unsafe.AsPointer(ref Unsafe.Add(ref o, 1))).AsInt32(), 0b00_01_10_11);
                var vprev4 = Vector128.Create(Unsafe.Add(ref o, 0), 0, 0, 0);
                for (nint i = 0; i < dataLength; i++)
                {
                    var res = Vector128.CreateScalar(Unsafe.Add(ref r, i));
                    sum = Sse41.MultiplyLow(vcoeff0, vprev0);
                    sum = Sse2.Add(sum, Sse41.MultiplyLow(vcoeff1, vprev1));
                    sum = Sse2.Add(sum, Sse41.MultiplyLow(vcoeff2, vprev2));
                    sum = Sse2.Add(sum, Sse41.MultiplyLow(vcoeff3, vprev3));
                    sum = Sse2.Add(sum, Sse41.MultiplyLow(vcoeff4, vprev4));
                    var y = Ssse3.HorizontalAdd(sum, vzero);
                    y = Ssse3.HorizontalAdd(y, vzero);
                    y = Sse2.ShiftRightArithmetic(y, vshift);   //C# shift operator handling sucks so SSE2 is used instead(same latency, same throughput).
                    y = Sse2.Add(y, res);   //Avoids extract and insert
#if NET5_0_OR_GREATER
                    Sse2.StoreScalar((int*)Unsafe.AsPointer(ref Unsafe.Add(ref d, i)), y);
#else
                    Unsafe.Add(ref d, i) = y.GetElement(0);
#endif
                    y = Sse2.ShiftLeftLogical128BitLane(y, 12);
                    vprev4 = Ssse3.AlignRight(vprev4, vprev3, 12);
                    vprev3 = Ssse3.AlignRight(vprev3, vprev2, 12);
                    vprev2 = Ssse3.AlignRight(vprev2, vprev1, 12);
                    vprev1 = Ssse3.AlignRight(vprev1, vprev0, 12);
                    vprev0 = Ssse3.AlignRight(vprev0, y, 12);
                }
            }

            [MethodImpl(OptimizationUtils.InlineAndOptimizeIfPossible)]
            internal static bool RestoreSignalOrder17Wide(int shiftsNeeded, ReadOnlySpan<int> residual, ReadOnlySpan<int> coeffs, Span<int> output)
            {
                if (Sse41.IsSupported)
                {
                    RestoreSignalOrder17WideSse41(shiftsNeeded, residual, coeffs, output);
                    return true;
                }
                return false;
            }

            [MethodImpl(OptimizationUtils.InlineAndOptimizeIfPossible)]
            internal static unsafe void RestoreSignalOrder17WideSse41(int shiftsNeeded, ReadOnlySpan<int> residual, ReadOnlySpan<int> coeffs, Span<int> output)
            {
                const int Order = 17;
                if(coeffs.Length < Order) return;
                _ = coeffs[Order - 1];
                var vshift = Vector128.CreateScalar((long)shiftsNeeded);
                ref var c = ref MemoryMarshal.GetReference(coeffs);
                ref var o = ref MemoryMarshal.GetReference(output);
                ref var d = ref Unsafe.Add(ref o, Order);
                int dataLength = output.Length - Order;
                ref var r = ref MemoryMarshal.GetReference(residual);
                Vector128<long> sum;
                var vcoeff0 = Sse41.ConvertToVector128Int64(Sse2.LoadScalarVector128((ulong*)Unsafe.AsPointer(ref Unsafe.Add(ref c, 0))).AsUInt32()).AsInt32();
                var vcoeff1 = Sse41.ConvertToVector128Int64(Sse2.LoadScalarVector128((ulong*)Unsafe.AsPointer(ref Unsafe.Add(ref c, 2))).AsUInt32()).AsInt32();
                var vcoeff2 = Sse41.ConvertToVector128Int64(Sse2.LoadScalarVector128((ulong*)Unsafe.AsPointer(ref Unsafe.Add(ref c, 4))).AsUInt32()).AsInt32();
                var vcoeff3 = Sse41.ConvertToVector128Int64(Sse2.LoadScalarVector128((ulong*)Unsafe.AsPointer(ref Unsafe.Add(ref c, 6))).AsUInt32()).AsInt32();
                var vcoeff4 = Sse41.ConvertToVector128Int64(Sse2.LoadScalarVector128((ulong*)Unsafe.AsPointer(ref Unsafe.Add(ref c, 8))).AsUInt32()).AsInt32();
                var vcoeff5 = Sse41.ConvertToVector128Int64(Sse2.LoadScalarVector128((ulong*)Unsafe.AsPointer(ref Unsafe.Add(ref c, 10))).AsUInt32()).AsInt32();
                var vcoeff6 = Sse41.ConvertToVector128Int64(Sse2.LoadScalarVector128((ulong*)Unsafe.AsPointer(ref Unsafe.Add(ref c, 12))).AsUInt32()).AsInt32();
                var vcoeff7 = Sse41.ConvertToVector128Int64(Sse2.LoadScalarVector128((ulong*)Unsafe.AsPointer(ref Unsafe.Add(ref c, 14))).AsUInt32()).AsInt32();
                var vcoeff8 = Sse41.ConvertToVector128Int64(Sse2.LoadScalarVector128((uint*)Unsafe.AsPointer(ref Unsafe.Add(ref c, 16))).AsUInt32()).AsInt32();
                var vprev0 = Sse2.Shuffle(Sse2.LoadScalarVector128((ulong*)Unsafe.AsPointer(ref Unsafe.Add(ref o, 15))).AsInt32(), 0b11_00_11_01);
                var vprev1 = Sse2.Shuffle(Sse2.LoadScalarVector128((ulong*)Unsafe.AsPointer(ref Unsafe.Add(ref o, 13))).AsInt32(), 0b11_00_11_01);
                var vprev2 = Sse2.Shuffle(Sse2.LoadScalarVector128((ulong*)Unsafe.AsPointer(ref Unsafe.Add(ref o, 11))).AsInt32(), 0b11_00_11_01);
                var vprev3 = Sse2.Shuffle(Sse2.LoadScalarVector128((ulong*)Unsafe.AsPointer(ref Unsafe.Add(ref o, 9))).AsInt32(), 0b11_00_11_01);
                var vprev4 = Sse2.Shuffle(Sse2.LoadScalarVector128((ulong*)Unsafe.AsPointer(ref Unsafe.Add(ref o, 7))).AsInt32(), 0b11_00_11_01);
                var vprev5 = Sse2.Shuffle(Sse2.LoadScalarVector128((ulong*)Unsafe.AsPointer(ref Unsafe.Add(ref o, 5))).AsInt32(), 0b11_00_11_01);
                var vprev6 = Sse2.Shuffle(Sse2.LoadScalarVector128((ulong*)Unsafe.AsPointer(ref Unsafe.Add(ref o, 3))).AsInt32(), 0b11_00_11_01);
                var vprev7 = Sse2.Shuffle(Sse2.LoadScalarVector128((ulong*)Unsafe.AsPointer(ref Unsafe.Add(ref o, 1))).AsInt32(), 0b11_00_11_01);
                var vprev8 = Sse2.Shuffle(Sse2.LoadScalarVector128((ulong*)Unsafe.AsPointer(ref Unsafe.Add(ref o, 0))).AsInt32(), 0b11_00_11_00);
                for(nint i = 0; i < dataLength;)
                {
                    var res = Vector128.CreateScalar(Unsafe.Add(ref r, i));
                    sum = Sse41.Multiply(vcoeff0, vprev0);
                    sum = Sse2.Add(sum, Sse41.Multiply(vcoeff1, vprev1));
                    sum = Sse2.Add(sum, Sse41.Multiply(vcoeff2, vprev2));
                    sum = Sse2.Add(sum, Sse41.Multiply(vcoeff3, vprev3));
                    sum = Sse2.Add(sum, Sse41.Multiply(vcoeff4, vprev4));
                    sum = Sse2.Add(sum, Sse41.Multiply(vcoeff5, vprev5));
                    sum = Sse2.Add(sum, Sse41.Multiply(vcoeff6, vprev6));
                    sum = Sse2.Add(sum, Sse41.Multiply(vcoeff7, vprev7));
                    sum = Sse2.Add(sum, Sse41.Multiply(vcoeff8, vprev8));
                    sum = Sse2.Add(sum, Sse2.ShiftRightLogical128BitLane(sum, 8));
                    sum = Sse2.ShiftRightLogical(sum, vshift);
                    var yy = Sse2.Add(sum.AsInt32(), res);
#if NET5_0_OR_GREATER
                    Sse2.StoreScalar((int*)Unsafe.AsPointer(ref Unsafe.Add(ref d, i)), yy);
#else
                    Unsafe.Add(ref d, i) = yy.GetElement(0);
#endif
                    i++;
                    yy = Sse2.ShiftLeftLogical128BitLane(yy, 8);
                    vprev8 = Ssse3.AlignRight(vprev8, vprev7, 8);
                    vprev7 = Ssse3.AlignRight(vprev7, vprev6, 8);
                    vprev6 = Ssse3.AlignRight(vprev6, vprev5, 8);
                    vprev5 = Ssse3.AlignRight(vprev5, vprev4, 8);
                    vprev4 = Ssse3.AlignRight(vprev4, vprev3, 8);
                    vprev3 = Ssse3.AlignRight(vprev3, vprev2, 8);
                    vprev2 = Ssse3.AlignRight(vprev2, vprev1, 8);
                    vprev1 = Ssse3.AlignRight(vprev1, vprev0, 8);
                    vprev0 = Ssse3.AlignRight(vprev0, yy, 8);
                }
            }
            
            [MethodImpl(OptimizationUtils.InlineAndOptimizeIfPossible)]
            internal static unsafe void RestoreSignalOrder17WideAvx2(int shiftsNeeded, ReadOnlySpan<int> residual, ReadOnlySpan<int> coeffs, Span<int> output)
            {
                const int Order = 17;
                if(coeffs.Length < Order) return;
                _ = coeffs[Order - 1];
                var vshift = Vector128.CreateScalar((long)shiftsNeeded);
                ref var c = ref MemoryMarshal.GetReference(coeffs);
                ref var o = ref MemoryMarshal.GetReference(output);
                ref var d = ref Unsafe.Add(ref o, Order);
                int dataLength = output.Length - Order;
                ref var r = ref MemoryMarshal.GetReference(residual);
                Vector128<long> sum;
                Vector256<long> sum256;
                var vcoeff0 = Avx2.ConvertToVector256Int64(Sse2.LoadVector128((int*)Unsafe.AsPointer(ref Unsafe.Add(ref c, 0))).AsUInt32()).AsInt32();
                var vcoeff1 = Avx2.ConvertToVector256Int64(Sse2.LoadVector128((int*)Unsafe.AsPointer(ref Unsafe.Add(ref c, 4))).AsUInt32()).AsInt32();
                var vcoeff2 = Avx2.ConvertToVector256Int64(Sse2.LoadVector128((int*)Unsafe.AsPointer(ref Unsafe.Add(ref c, 8))).AsUInt32()).AsInt32();
                var vcoeff3 = Avx2.ConvertToVector256Int64(Sse2.LoadVector128((int*)Unsafe.AsPointer(ref Unsafe.Add(ref c, 12))).AsUInt32()).AsInt32();
                var vcoeff4 = Avx2.ConvertToVector256Int64(Sse2.LoadScalarVector128((uint*)Unsafe.AsPointer(ref Unsafe.Add(ref c, 16))).AsUInt32()).AsInt32();
                var vprev0 = Avx2.ConvertToVector256Int64(Sse2.Shuffle(Sse2.LoadScalarVector128((ulong*)Unsafe.AsPointer(ref Unsafe.Add(ref o, 14))).AsInt32(), 0b00_01_10_11).AsUInt32()).AsInt32();
                var vprev1 = Avx2.ConvertToVector256Int64(Sse2.Shuffle(Sse2.LoadScalarVector128((ulong*)Unsafe.AsPointer(ref Unsafe.Add(ref o, 10))).AsInt32(), 0b00_01_10_11).AsUInt32()).AsInt32();
                var vprev2 = Avx2.ConvertToVector256Int64(Sse2.Shuffle(Sse2.LoadScalarVector128((ulong*)Unsafe.AsPointer(ref Unsafe.Add(ref o, 6))).AsInt32(), 0b00_01_10_11).AsUInt32()).AsInt32();
                var vprev3 = Avx2.ConvertToVector256Int64(Sse2.Shuffle(Sse2.LoadScalarVector128((ulong*)Unsafe.AsPointer(ref Unsafe.Add(ref o, 2))).AsInt32(), 0b00_01_10_11).AsUInt32()).AsInt32();
                var vprev4 = Avx2.ConvertToVector256Int64(Sse2.Shuffle(Sse2.LoadScalarVector128((uint*)Unsafe.AsPointer(ref o)).AsInt32(), 0b00_01_10_11).AsUInt32()).AsInt32();
                for(nint i = 0; i < dataLength;)
                {
                    var res = Vector128.CreateScalar(Unsafe.Add(ref r, i));
                    sum256 = Avx2.Multiply(vcoeff0, vprev0);
                    sum256 = Avx2.Add(sum256, Avx2.Multiply(vcoeff1, vprev1));
                    sum256 = Avx2.Add(sum256, Avx2.Multiply(vcoeff2, vprev2));
                    sum256 = Avx2.Add(sum256, Avx2.Multiply(vcoeff3, vprev3));
                    sum256 = Avx2.Add(sum256, Avx2.Multiply(vcoeff4, vprev4));
                    sum = Sse2.Add(sum256.GetLower(), sum256.GetUpper());
                    sum = Sse2.Add(sum, Sse2.ShiftRightLogical128BitLane(sum, 8));
                    sum = Sse2.ShiftRightLogical(sum, vshift);
                    var yy = Sse2.Add(sum.AsInt32(), res);
#if NET5_0_OR_GREATER
                    Sse2.StoreScalar((int*)Unsafe.AsPointer(ref Unsafe.Add(ref d, i)), yy);
#else
                    Unsafe.Add(ref d, i) = yy.GetElement(0);
#endif
                    i++;
                    var yu = yy.ToVector256();
                    yu = Avx2.Permute4x64(yu.AsUInt64(), 0b00_00_00_00).AsInt32();
                    vprev4 = Avx2.Permute4x64(vprev4.AsInt64(), 0b10_01_00_11).AsInt32();
                    vprev4 = Avx2.Blend(vprev4, vprev3, 0b0000_0001);
                    vprev3 = Avx2.Permute4x64(vprev3.AsInt64(), 0b10_01_00_11).AsInt32();
                    vprev3 = Avx2.Blend(vprev3, vprev2, 0b0000_0001);
                    vprev2 = Avx2.Permute4x64(vprev2.AsInt64(), 0b10_01_00_11).AsInt32();
                    vprev2 = Avx2.Blend(vprev2, vprev1, 0b0000_0001);
                    vprev1 = Avx2.Permute4x64(vprev1.AsInt64(), 0b10_01_00_11).AsInt32();
                    vprev1 = Avx2.Blend(vprev1, vprev0, 0b0000_0001);
                    vprev0 = Avx2.Permute4x64(vprev0.AsInt64(), 0b10_01_00_11).AsInt32();
                    vprev0 = Avx2.Blend(vprev0, yu, 0b0000_0001);
                }
            }
#endregion Order17
#region Order18

            [MethodImpl(OptimizationUtils.InlineAndOptimizeIfPossible)]
            internal static bool RestoreSignalOrder18(int shiftsNeeded, ReadOnlySpan<int> residual, ReadOnlySpan<int> coeffs, Span<int> output)
            {
                if (Sse41.IsSupported)
                {
                    RestoreSignalOrder18Sse41(shiftsNeeded, residual, coeffs, output);
                    return true;
                }
                return false;
            }

            [MethodImpl(OptimizationUtils.InlineAndOptimizeIfPossible)]
            internal static unsafe void RestoreSignalOrder18Sse41(int shiftsNeeded, ReadOnlySpan<int> residual, ReadOnlySpan<int> coeffs, Span<int> output)
            {
                const int Order = 18;
                if (coeffs.Length < Order) return;
                _ = coeffs[Order - 1];
                var vshift = Vector128.CreateScalar(shiftsNeeded);
                ref var o = ref MemoryMarshal.GetReference(output);
                ref var d = ref Unsafe.Add(ref o, Order);
                ref var r = ref MemoryMarshal.GetReference(residual);
                ref var c = ref MemoryMarshal.GetReference(coeffs);
                Vector128<int> sum;
                var vzero = Vector128.Create(0);
                nint dataLength = output.Length - Order;
                var vcoeff0 = Sse2.LoadVector128((int*)Unsafe.AsPointer(ref Unsafe.Add(ref c, 0)));
                var vcoeff1 = Sse2.LoadVector128((int*)Unsafe.AsPointer(ref Unsafe.Add(ref c, 4)));
                var vcoeff2 = Sse2.LoadVector128((int*)Unsafe.AsPointer(ref Unsafe.Add(ref c, 8)));
                var vcoeff3 = Sse2.LoadVector128((int*)Unsafe.AsPointer(ref Unsafe.Add(ref c, 12)));
                var vcoeff4 = Vector128.Create(Unsafe.Add(ref c, 16), Unsafe.Add(ref c, 17), 0, 0);
                var vprev0 = Sse2.Shuffle(Sse2.LoadVector128((int*)Unsafe.AsPointer(ref Unsafe.Add(ref o, 14))).AsInt32(), 0b00_01_10_11);
                var vprev1 = Sse2.Shuffle(Sse2.LoadVector128((int*)Unsafe.AsPointer(ref Unsafe.Add(ref o, 10))).AsInt32(), 0b00_01_10_11);
                var vprev2 = Sse2.Shuffle(Sse2.LoadVector128((int*)Unsafe.AsPointer(ref Unsafe.Add(ref o, 6))).AsInt32(), 0b00_01_10_11);
                var vprev3 = Sse2.Shuffle(Sse2.LoadVector128((int*)Unsafe.AsPointer(ref Unsafe.Add(ref o, 2))).AsInt32(), 0b00_01_10_11);
                var vprev4 = Vector128.Create(Unsafe.Add(ref o, 1), Unsafe.Add(ref o, 0), 0, 0);
                for (nint i = 0; i < dataLength; i++)
                {
                    var res = Vector128.CreateScalar(Unsafe.Add(ref r, i));
                    sum = Sse41.MultiplyLow(vcoeff0, vprev0);
                    sum = Sse2.Add(sum, Sse41.MultiplyLow(vcoeff1, vprev1));
                    sum = Sse2.Add(sum, Sse41.MultiplyLow(vcoeff2, vprev2));
                    sum = Sse2.Add(sum, Sse41.MultiplyLow(vcoeff3, vprev3));
                    sum = Sse2.Add(sum, Sse41.MultiplyLow(vcoeff4, vprev4));
                    var y = Ssse3.HorizontalAdd(sum, vzero);
                    y = Ssse3.HorizontalAdd(y, vzero);
                    y = Sse2.ShiftRightArithmetic(y, vshift);   //C# shift operator handling sucks so SSE2 is used instead(same latency, same throughput).
                    y = Sse2.Add(y, res);   //Avoids extract and insert
#if NET5_0_OR_GREATER
                    Sse2.StoreScalar((int*)Unsafe.AsPointer(ref Unsafe.Add(ref d, i)), y);
#else
                    Unsafe.Add(ref d, i) = y.GetElement(0);
#endif
                    y = Sse2.ShiftLeftLogical128BitLane(y, 12);
                    vprev4 = Ssse3.AlignRight(vprev4, vprev3, 12);
                    vprev3 = Ssse3.AlignRight(vprev3, vprev2, 12);
                    vprev2 = Ssse3.AlignRight(vprev2, vprev1, 12);
                    vprev1 = Ssse3.AlignRight(vprev1, vprev0, 12);
                    vprev0 = Ssse3.AlignRight(vprev0, y, 12);
                }
            }

            [MethodImpl(OptimizationUtils.InlineAndOptimizeIfPossible)]
            internal static bool RestoreSignalOrder18Wide(int shiftsNeeded, ReadOnlySpan<int> residual, ReadOnlySpan<int> coeffs, Span<int> output)
            {
                if (Sse41.IsSupported)
                {
                    RestoreSignalOrder18WideSse41(shiftsNeeded, residual, coeffs, output);
                    return true;
                }
                return false;
            }

            [MethodImpl(OptimizationUtils.InlineAndOptimizeIfPossible)]
            internal static unsafe void RestoreSignalOrder18WideSse41(int shiftsNeeded, ReadOnlySpan<int> residual, ReadOnlySpan<int> coeffs, Span<int> output)
            {
                const int Order = 18;
                if(coeffs.Length < Order) return;
                _ = coeffs[Order - 1];
                var vshift = Vector128.CreateScalar((long)shiftsNeeded);
                ref var c = ref MemoryMarshal.GetReference(coeffs);
                ref var o = ref MemoryMarshal.GetReference(output);
                ref var d = ref Unsafe.Add(ref o, Order);
                int dataLength = output.Length - Order;
                ref var r = ref MemoryMarshal.GetReference(residual);
                Vector128<long> sum;
                var vcoeff0 = Sse41.ConvertToVector128Int64(Sse2.LoadScalarVector128((ulong*)Unsafe.AsPointer(ref Unsafe.Add(ref c, 0))).AsUInt32()).AsInt32();
                var vcoeff1 = Sse41.ConvertToVector128Int64(Sse2.LoadScalarVector128((ulong*)Unsafe.AsPointer(ref Unsafe.Add(ref c, 2))).AsUInt32()).AsInt32();
                var vcoeff2 = Sse41.ConvertToVector128Int64(Sse2.LoadScalarVector128((ulong*)Unsafe.AsPointer(ref Unsafe.Add(ref c, 4))).AsUInt32()).AsInt32();
                var vcoeff3 = Sse41.ConvertToVector128Int64(Sse2.LoadScalarVector128((ulong*)Unsafe.AsPointer(ref Unsafe.Add(ref c, 6))).AsUInt32()).AsInt32();
                var vcoeff4 = Sse41.ConvertToVector128Int64(Sse2.LoadScalarVector128((ulong*)Unsafe.AsPointer(ref Unsafe.Add(ref c, 8))).AsUInt32()).AsInt32();
                var vcoeff5 = Sse41.ConvertToVector128Int64(Sse2.LoadScalarVector128((ulong*)Unsafe.AsPointer(ref Unsafe.Add(ref c, 10))).AsUInt32()).AsInt32();
                var vcoeff6 = Sse41.ConvertToVector128Int64(Sse2.LoadScalarVector128((ulong*)Unsafe.AsPointer(ref Unsafe.Add(ref c, 12))).AsUInt32()).AsInt32();
                var vcoeff7 = Sse41.ConvertToVector128Int64(Sse2.LoadScalarVector128((ulong*)Unsafe.AsPointer(ref Unsafe.Add(ref c, 14))).AsUInt32()).AsInt32();
                var vcoeff8 = Sse41.ConvertToVector128Int64(Sse2.LoadScalarVector128((ulong*)Unsafe.AsPointer(ref Unsafe.Add(ref c, 16))).AsUInt32()).AsInt32();
                var vprev0 = Sse2.Shuffle(Sse2.LoadScalarVector128((ulong*)Unsafe.AsPointer(ref Unsafe.Add(ref o, 16))).AsInt32(), 0b11_00_11_01);
                var vprev1 = Sse2.Shuffle(Sse2.LoadScalarVector128((ulong*)Unsafe.AsPointer(ref Unsafe.Add(ref o, 14))).AsInt32(), 0b11_00_11_01);
                var vprev2 = Sse2.Shuffle(Sse2.LoadScalarVector128((ulong*)Unsafe.AsPointer(ref Unsafe.Add(ref o, 12))).AsInt32(), 0b11_00_11_01);
                var vprev3 = Sse2.Shuffle(Sse2.LoadScalarVector128((ulong*)Unsafe.AsPointer(ref Unsafe.Add(ref o, 10))).AsInt32(), 0b11_00_11_01);
                var vprev4 = Sse2.Shuffle(Sse2.LoadScalarVector128((ulong*)Unsafe.AsPointer(ref Unsafe.Add(ref o, 8))).AsInt32(), 0b11_00_11_01);
                var vprev5 = Sse2.Shuffle(Sse2.LoadScalarVector128((ulong*)Unsafe.AsPointer(ref Unsafe.Add(ref o, 6))).AsInt32(), 0b11_00_11_01);
                var vprev6 = Sse2.Shuffle(Sse2.LoadScalarVector128((ulong*)Unsafe.AsPointer(ref Unsafe.Add(ref o, 4))).AsInt32(), 0b11_00_11_01);
                var vprev7 = Sse2.Shuffle(Sse2.LoadScalarVector128((ulong*)Unsafe.AsPointer(ref Unsafe.Add(ref o, 2))).AsInt32(), 0b11_00_11_01);
                var vprev8 = Sse2.Shuffle(Sse2.LoadScalarVector128((ulong*)Unsafe.AsPointer(ref Unsafe.Add(ref o, 0))).AsInt32(), 0b11_00_11_01);
                for(nint i = 0; i < dataLength;)
                {
                    var res = Vector128.CreateScalar(Unsafe.Add(ref r, i));
                    sum = Sse41.Multiply(vcoeff0, vprev0);
                    sum = Sse2.Add(sum, Sse41.Multiply(vcoeff1, vprev1));
                    sum = Sse2.Add(sum, Sse41.Multiply(vcoeff2, vprev2));
                    sum = Sse2.Add(sum, Sse41.Multiply(vcoeff3, vprev3));
                    sum = Sse2.Add(sum, Sse41.Multiply(vcoeff4, vprev4));
                    sum = Sse2.Add(sum, Sse41.Multiply(vcoeff5, vprev5));
                    sum = Sse2.Add(sum, Sse41.Multiply(vcoeff6, vprev6));
                    sum = Sse2.Add(sum, Sse41.Multiply(vcoeff7, vprev7));
                    sum = Sse2.Add(sum, Sse41.Multiply(vcoeff8, vprev8));
                    sum = Sse2.Add(sum, Sse2.ShiftRightLogical128BitLane(sum, 8));
                    sum = Sse2.ShiftRightLogical(sum, vshift);
                    var yy = Sse2.Add(sum.AsInt32(), res);
#if NET5_0_OR_GREATER
                    Sse2.StoreScalar((int*)Unsafe.AsPointer(ref Unsafe.Add(ref d, i)), yy);
#else
                    Unsafe.Add(ref d, i) = yy.GetElement(0);
#endif
                    i++;
                    yy = Sse2.ShiftLeftLogical128BitLane(yy, 8);
                    vprev8 = Ssse3.AlignRight(vprev8, vprev7, 8);
                    vprev7 = Ssse3.AlignRight(vprev7, vprev6, 8);
                    vprev6 = Ssse3.AlignRight(vprev6, vprev5, 8);
                    vprev5 = Ssse3.AlignRight(vprev5, vprev4, 8);
                    vprev4 = Ssse3.AlignRight(vprev4, vprev3, 8);
                    vprev3 = Ssse3.AlignRight(vprev3, vprev2, 8);
                    vprev2 = Ssse3.AlignRight(vprev2, vprev1, 8);
                    vprev1 = Ssse3.AlignRight(vprev1, vprev0, 8);
                    vprev0 = Ssse3.AlignRight(vprev0, yy, 8);
                }
            }
            
            [MethodImpl(OptimizationUtils.InlineAndOptimizeIfPossible)]
            internal static unsafe void RestoreSignalOrder18WideAvx2(int shiftsNeeded, ReadOnlySpan<int> residual, ReadOnlySpan<int> coeffs, Span<int> output)
            {
                const int Order = 18;
                if(coeffs.Length < Order) return;
                _ = coeffs[Order - 1];
                var vshift = Vector128.CreateScalar((long)shiftsNeeded);
                ref var c = ref MemoryMarshal.GetReference(coeffs);
                ref var o = ref MemoryMarshal.GetReference(output);
                ref var d = ref Unsafe.Add(ref o, Order);
                int dataLength = output.Length - Order;
                ref var r = ref MemoryMarshal.GetReference(residual);
                Vector128<long> sum;
                Vector256<long> sum256;
                var vcoeff0 = Avx2.ConvertToVector256Int64(Sse2.LoadVector128((int*)Unsafe.AsPointer(ref Unsafe.Add(ref c, 0))).AsUInt32()).AsInt32();
                var vcoeff1 = Avx2.ConvertToVector256Int64(Sse2.LoadVector128((int*)Unsafe.AsPointer(ref Unsafe.Add(ref c, 4))).AsUInt32()).AsInt32();
                var vcoeff2 = Avx2.ConvertToVector256Int64(Sse2.LoadVector128((int*)Unsafe.AsPointer(ref Unsafe.Add(ref c, 8))).AsUInt32()).AsInt32();
                var vcoeff3 = Avx2.ConvertToVector256Int64(Sse2.LoadVector128((int*)Unsafe.AsPointer(ref Unsafe.Add(ref c, 12))).AsUInt32()).AsInt32();
                var vcoeff4 = Avx2.ConvertToVector256Int64(Sse2.LoadScalarVector128((ulong*)Unsafe.AsPointer(ref Unsafe.Add(ref c, 16))).AsUInt32()).AsInt32();
                var vprev0 = Avx2.ConvertToVector256Int64(Sse2.Shuffle(Sse2.LoadScalarVector128((ulong*)Unsafe.AsPointer(ref Unsafe.Add(ref o, 15))).AsInt32(), 0b00_01_10_11).AsUInt32()).AsInt32();
                var vprev1 = Avx2.ConvertToVector256Int64(Sse2.Shuffle(Sse2.LoadScalarVector128((ulong*)Unsafe.AsPointer(ref Unsafe.Add(ref o, 11))).AsInt32(), 0b00_01_10_11).AsUInt32()).AsInt32();
                var vprev2 = Avx2.ConvertToVector256Int64(Sse2.Shuffle(Sse2.LoadScalarVector128((ulong*)Unsafe.AsPointer(ref Unsafe.Add(ref o, 7))).AsInt32(), 0b00_01_10_11).AsUInt32()).AsInt32();
                var vprev3 = Avx2.ConvertToVector256Int64(Sse2.Shuffle(Sse2.LoadScalarVector128((ulong*)Unsafe.AsPointer(ref Unsafe.Add(ref o, 3))).AsInt32(), 0b00_01_10_11).AsUInt32()).AsInt32();
                var vprev4 = Avx2.ConvertToVector256Int64(Sse2.Shuffle(Sse2.LoadScalarVector128((ulong*)Unsafe.AsPointer(ref o)).AsInt32(), 0b00_01_10_11).AsUInt32()).AsInt32();
                for(nint i = 0; i < dataLength;)
                {
                    var res = Vector128.CreateScalar(Unsafe.Add(ref r, i));
                    sum256 = Avx2.Multiply(vcoeff0, vprev0);
                    sum256 = Avx2.Add(sum256, Avx2.Multiply(vcoeff1, vprev1));
                    sum256 = Avx2.Add(sum256, Avx2.Multiply(vcoeff2, vprev2));
                    sum256 = Avx2.Add(sum256, Avx2.Multiply(vcoeff3, vprev3));
                    sum256 = Avx2.Add(sum256, Avx2.Multiply(vcoeff4, vprev4));
                    sum = Sse2.Add(sum256.GetLower(), sum256.GetUpper());
                    sum = Sse2.Add(sum, Sse2.ShiftRightLogical128BitLane(sum, 8));
                    sum = Sse2.ShiftRightLogical(sum, vshift);
                    var yy = Sse2.Add(sum.AsInt32(), res);
#if NET5_0_OR_GREATER
                    Sse2.StoreScalar((int*)Unsafe.AsPointer(ref Unsafe.Add(ref d, i)), yy);
#else
                    Unsafe.Add(ref d, i) = yy.GetElement(0);
#endif
                    i++;
                    var yu = yy.ToVector256();
                    yu = Avx2.Permute4x64(yu.AsUInt64(), 0b00_00_00_00).AsInt32();
                    vprev4 = Avx2.Permute4x64(vprev4.AsInt64(), 0b10_01_00_11).AsInt32();
                    vprev4 = Avx2.Blend(vprev4, vprev3, 0b0000_0001);
                    vprev3 = Avx2.Permute4x64(vprev3.AsInt64(), 0b10_01_00_11).AsInt32();
                    vprev3 = Avx2.Blend(vprev3, vprev2, 0b0000_0001);
                    vprev2 = Avx2.Permute4x64(vprev2.AsInt64(), 0b10_01_00_11).AsInt32();
                    vprev2 = Avx2.Blend(vprev2, vprev1, 0b0000_0001);
                    vprev1 = Avx2.Permute4x64(vprev1.AsInt64(), 0b10_01_00_11).AsInt32();
                    vprev1 = Avx2.Blend(vprev1, vprev0, 0b0000_0001);
                    vprev0 = Avx2.Permute4x64(vprev0.AsInt64(), 0b10_01_00_11).AsInt32();
                    vprev0 = Avx2.Blend(vprev0, yu, 0b0000_0001);
                }
            }
#endregion Order18
#region Order19

            [MethodImpl(OptimizationUtils.InlineAndOptimizeIfPossible)]
            internal static bool RestoreSignalOrder19(int shiftsNeeded, ReadOnlySpan<int> residual, ReadOnlySpan<int> coeffs, Span<int> output)
            {
                if (Sse41.IsSupported)
                {
                    RestoreSignalOrder19Sse41(shiftsNeeded, residual, coeffs, output);
                    return true;
                }
                return false;
            }

            [MethodImpl(OptimizationUtils.InlineAndOptimizeIfPossible)]
            internal static unsafe void RestoreSignalOrder19Sse41(int shiftsNeeded, ReadOnlySpan<int> residual, ReadOnlySpan<int> coeffs, Span<int> output)
            {
                const int Order = 19;
                if (coeffs.Length < Order) return;
                _ = coeffs[Order - 1];
                var vshift = Vector128.CreateScalar(shiftsNeeded);
                ref var o = ref MemoryMarshal.GetReference(output);
                ref var d = ref Unsafe.Add(ref o, Order);
                ref var r = ref MemoryMarshal.GetReference(residual);
                ref var c = ref MemoryMarshal.GetReference(coeffs);
                Vector128<int> sum;
                var vzero = Vector128.Create(0);
                nint dataLength = output.Length - Order;
                var vcoeff0 = Sse2.LoadVector128((int*)Unsafe.AsPointer(ref Unsafe.Add(ref c, 0)));
                var vcoeff1 = Sse2.LoadVector128((int*)Unsafe.AsPointer(ref Unsafe.Add(ref c, 4)));
                var vcoeff2 = Sse2.LoadVector128((int*)Unsafe.AsPointer(ref Unsafe.Add(ref c, 8)));
                var vcoeff3 = Sse2.LoadVector128((int*)Unsafe.AsPointer(ref Unsafe.Add(ref c, 12)));
                var vcoeff4 = Vector128.Create(Unsafe.Add(ref c, 16), Unsafe.Add(ref c, 17), Unsafe.Add(ref c, 18), 0);
                var vprev0 = Sse2.Shuffle(Sse2.LoadVector128((int*)Unsafe.AsPointer(ref Unsafe.Add(ref o, 15))).AsInt32(), 0b00_01_10_11);
                var vprev1 = Sse2.Shuffle(Sse2.LoadVector128((int*)Unsafe.AsPointer(ref Unsafe.Add(ref o, 11))).AsInt32(), 0b00_01_10_11);
                var vprev2 = Sse2.Shuffle(Sse2.LoadVector128((int*)Unsafe.AsPointer(ref Unsafe.Add(ref o, 7))).AsInt32(), 0b00_01_10_11);
                var vprev3 = Sse2.Shuffle(Sse2.LoadVector128((int*)Unsafe.AsPointer(ref Unsafe.Add(ref o, 3))).AsInt32(), 0b00_01_10_11);
                var vprev4 = Vector128.Create(Unsafe.Add(ref o, 2), Unsafe.Add(ref o, 1), Unsafe.Add(ref o, 0), 0);
                for (nint i = 0; i < dataLength; i++)
                {
                    var res = Vector128.CreateScalar(Unsafe.Add(ref r, i));
                    sum = Sse41.MultiplyLow(vcoeff0, vprev0);
                    sum = Sse2.Add(sum, Sse41.MultiplyLow(vcoeff1, vprev1));
                    sum = Sse2.Add(sum, Sse41.MultiplyLow(vcoeff2, vprev2));
                    sum = Sse2.Add(sum, Sse41.MultiplyLow(vcoeff3, vprev3));
                    sum = Sse2.Add(sum, Sse41.MultiplyLow(vcoeff4, vprev4));
                    var y = Ssse3.HorizontalAdd(sum, vzero);
                    y = Ssse3.HorizontalAdd(y, vzero);
                    y = Sse2.ShiftRightArithmetic(y, vshift);   //C# shift operator handling sucks so SSE2 is used instead(same latency, same throughput).
                    y = Sse2.Add(y, res);   //Avoids extract and insert
#if NET5_0_OR_GREATER
                    Sse2.StoreScalar((int*)Unsafe.AsPointer(ref Unsafe.Add(ref d, i)), y);
#else
                    Unsafe.Add(ref d, i) = y.GetElement(0);
#endif
                    y = Sse2.ShiftLeftLogical128BitLane(y, 12);
                    vprev4 = Ssse3.AlignRight(vprev4, vprev3, 12);
                    vprev3 = Ssse3.AlignRight(vprev3, vprev2, 12);
                    vprev2 = Ssse3.AlignRight(vprev2, vprev1, 12);
                    vprev1 = Ssse3.AlignRight(vprev1, vprev0, 12);
                    vprev0 = Ssse3.AlignRight(vprev0, y, 12);
                }
            }

            [MethodImpl(OptimizationUtils.InlineAndOptimizeIfPossible)]
            internal static bool RestoreSignalOrder19Wide(int shiftsNeeded, ReadOnlySpan<int> residual, ReadOnlySpan<int> coeffs, Span<int> output)
            {
                if (Sse41.IsSupported)
                {
                    RestoreSignalOrder19WideSse41(shiftsNeeded, residual, coeffs, output);
                    return true;
                }
                return false;
            }

            [MethodImpl(OptimizationUtils.InlineAndOptimizeIfPossible)]
            internal static unsafe void RestoreSignalOrder19WideSse41(int shiftsNeeded, ReadOnlySpan<int> residual, ReadOnlySpan<int> coeffs, Span<int> output)
            {
                const int Order = 19;
                if(coeffs.Length < Order) return;
                _ = coeffs[Order - 1];
                var vshift = Vector128.CreateScalar((long)shiftsNeeded);
                ref var c = ref MemoryMarshal.GetReference(coeffs);
                ref var o = ref MemoryMarshal.GetReference(output);
                ref var d = ref Unsafe.Add(ref o, Order);
                int dataLength = output.Length - Order;
                ref var r = ref MemoryMarshal.GetReference(residual);
                Vector128<long> sum;
                var vcoeff0 = Sse41.ConvertToVector128Int64(Sse2.LoadScalarVector128((ulong*)Unsafe.AsPointer(ref Unsafe.Add(ref c, 0))).AsUInt32()).AsInt32();
                var vcoeff1 = Sse41.ConvertToVector128Int64(Sse2.LoadScalarVector128((ulong*)Unsafe.AsPointer(ref Unsafe.Add(ref c, 2))).AsUInt32()).AsInt32();
                var vcoeff2 = Sse41.ConvertToVector128Int64(Sse2.LoadScalarVector128((ulong*)Unsafe.AsPointer(ref Unsafe.Add(ref c, 4))).AsUInt32()).AsInt32();
                var vcoeff3 = Sse41.ConvertToVector128Int64(Sse2.LoadScalarVector128((ulong*)Unsafe.AsPointer(ref Unsafe.Add(ref c, 6))).AsUInt32()).AsInt32();
                var vcoeff4 = Sse41.ConvertToVector128Int64(Sse2.LoadScalarVector128((ulong*)Unsafe.AsPointer(ref Unsafe.Add(ref c, 8))).AsUInt32()).AsInt32();
                var vcoeff5 = Sse41.ConvertToVector128Int64(Sse2.LoadScalarVector128((ulong*)Unsafe.AsPointer(ref Unsafe.Add(ref c, 10))).AsUInt32()).AsInt32();
                var vcoeff6 = Sse41.ConvertToVector128Int64(Sse2.LoadScalarVector128((ulong*)Unsafe.AsPointer(ref Unsafe.Add(ref c, 12))).AsUInt32()).AsInt32();
                var vcoeff7 = Sse41.ConvertToVector128Int64(Sse2.LoadScalarVector128((ulong*)Unsafe.AsPointer(ref Unsafe.Add(ref c, 14))).AsUInt32()).AsInt32();
                var vcoeff8 = Sse41.ConvertToVector128Int64(Sse2.LoadScalarVector128((ulong*)Unsafe.AsPointer(ref Unsafe.Add(ref c, 16))).AsUInt32()).AsInt32();
                var vcoeff9 = Sse41.ConvertToVector128Int64(Sse2.LoadScalarVector128((uint*)Unsafe.AsPointer(ref Unsafe.Add(ref c, 18))).AsUInt32()).AsInt32();
                var vprev0 = Sse2.Shuffle(Sse2.LoadScalarVector128((ulong*)Unsafe.AsPointer(ref Unsafe.Add(ref o, 17))).AsInt32(), 0b11_00_11_01);
                var vprev1 = Sse2.Shuffle(Sse2.LoadScalarVector128((ulong*)Unsafe.AsPointer(ref Unsafe.Add(ref o, 15))).AsInt32(), 0b11_00_11_01);
                var vprev2 = Sse2.Shuffle(Sse2.LoadScalarVector128((ulong*)Unsafe.AsPointer(ref Unsafe.Add(ref o, 13))).AsInt32(), 0b11_00_11_01);
                var vprev3 = Sse2.Shuffle(Sse2.LoadScalarVector128((ulong*)Unsafe.AsPointer(ref Unsafe.Add(ref o, 11))).AsInt32(), 0b11_00_11_01);
                var vprev4 = Sse2.Shuffle(Sse2.LoadScalarVector128((ulong*)Unsafe.AsPointer(ref Unsafe.Add(ref o, 9))).AsInt32(), 0b11_00_11_01);
                var vprev5 = Sse2.Shuffle(Sse2.LoadScalarVector128((ulong*)Unsafe.AsPointer(ref Unsafe.Add(ref o, 7))).AsInt32(), 0b11_00_11_01);
                var vprev6 = Sse2.Shuffle(Sse2.LoadScalarVector128((ulong*)Unsafe.AsPointer(ref Unsafe.Add(ref o, 5))).AsInt32(), 0b11_00_11_01);
                var vprev7 = Sse2.Shuffle(Sse2.LoadScalarVector128((ulong*)Unsafe.AsPointer(ref Unsafe.Add(ref o, 3))).AsInt32(), 0b11_00_11_01);
                var vprev8 = Sse2.Shuffle(Sse2.LoadScalarVector128((ulong*)Unsafe.AsPointer(ref Unsafe.Add(ref o, 1))).AsInt32(), 0b11_00_11_01);
                var vprev9 = Sse2.Shuffle(Sse2.LoadScalarVector128((ulong*)Unsafe.AsPointer(ref Unsafe.Add(ref o, 0))).AsInt32(), 0b11_00_11_00);
                for(nint i = 0; i < dataLength;)
                {
                    var res = Vector128.CreateScalar(Unsafe.Add(ref r, i));
                    sum = Sse41.Multiply(vcoeff0, vprev0);
                    sum = Sse2.Add(sum, Sse41.Multiply(vcoeff1, vprev1));
                    sum = Sse2.Add(sum, Sse41.Multiply(vcoeff2, vprev2));
                    sum = Sse2.Add(sum, Sse41.Multiply(vcoeff3, vprev3));
                    sum = Sse2.Add(sum, Sse41.Multiply(vcoeff4, vprev4));
                    sum = Sse2.Add(sum, Sse41.Multiply(vcoeff5, vprev5));
                    sum = Sse2.Add(sum, Sse41.Multiply(vcoeff6, vprev6));
                    sum = Sse2.Add(sum, Sse41.Multiply(vcoeff7, vprev7));
                    sum = Sse2.Add(sum, Sse41.Multiply(vcoeff8, vprev8));
                    sum = Sse2.Add(sum, Sse41.Multiply(vcoeff9, vprev9));
                    sum = Sse2.Add(sum, Sse2.ShiftRightLogical128BitLane(sum, 8));
                    sum = Sse2.ShiftRightLogical(sum, vshift);
                    var yy = Sse2.Add(sum.AsInt32(), res);
#if NET5_0_OR_GREATER
                    Sse2.StoreScalar((int*)Unsafe.AsPointer(ref Unsafe.Add(ref d, i)), yy);
#else
                    Unsafe.Add(ref d, i) = yy.GetElement(0);
#endif
                    i++;
                    yy = Sse2.ShiftLeftLogical128BitLane(yy, 8);
                    vprev9 = Ssse3.AlignRight(vprev9, vprev8, 8);
                    vprev8 = Ssse3.AlignRight(vprev8, vprev7, 8);
                    vprev7 = Ssse3.AlignRight(vprev7, vprev6, 8);
                    vprev6 = Ssse3.AlignRight(vprev6, vprev5, 8);
                    vprev5 = Ssse3.AlignRight(vprev5, vprev4, 8);
                    vprev4 = Ssse3.AlignRight(vprev4, vprev3, 8);
                    vprev3 = Ssse3.AlignRight(vprev3, vprev2, 8);
                    vprev2 = Ssse3.AlignRight(vprev2, vprev1, 8);
                    vprev1 = Ssse3.AlignRight(vprev1, vprev0, 8);
                    vprev0 = Ssse3.AlignRight(vprev0, yy, 8);
                }
            }
            
            [MethodImpl(OptimizationUtils.InlineAndOptimizeIfPossible)]
            internal static unsafe void RestoreSignalOrder19WideAvx2(int shiftsNeeded, ReadOnlySpan<int> residual, ReadOnlySpan<int> coeffs, Span<int> output)
            {
                const int Order = 19;
                if(coeffs.Length < Order) return;
                _ = coeffs[Order - 1];
                var vshift = Vector128.CreateScalar((long)shiftsNeeded);
                var mask = Vector128.Create(~0, ~0, ~0, 0);
                ref var c = ref MemoryMarshal.GetReference(coeffs);
                ref var o = ref MemoryMarshal.GetReference(output);
                ref var d = ref Unsafe.Add(ref o, Order);
                int dataLength = output.Length - Order;
                ref var r = ref MemoryMarshal.GetReference(residual);
                Vector128<long> sum;
                Vector256<long> sum256;
                var vcoeff0 = Avx2.ConvertToVector256Int64(Sse2.LoadVector128((int*)Unsafe.AsPointer(ref Unsafe.Add(ref c, 0))).AsUInt32()).AsInt32();
                var vcoeff1 = Avx2.ConvertToVector256Int64(Sse2.LoadVector128((int*)Unsafe.AsPointer(ref Unsafe.Add(ref c, 4))).AsUInt32()).AsInt32();
                var vcoeff2 = Avx2.ConvertToVector256Int64(Sse2.LoadVector128((int*)Unsafe.AsPointer(ref Unsafe.Add(ref c, 8))).AsUInt32()).AsInt32();
                var vcoeff3 = Avx2.ConvertToVector256Int64(Sse2.LoadVector128((int*)Unsafe.AsPointer(ref Unsafe.Add(ref c, 12))).AsUInt32()).AsInt32();
                var vcoeff4 = Avx2.ConvertToVector256Int64(Avx2.MaskLoad((int*)Unsafe.AsPointer(ref Unsafe.Add(ref c, 16)), mask)).AsInt32();
                var vprev0 = Avx2.ConvertToVector256Int64(Sse2.Shuffle(Sse2.LoadScalarVector128((ulong*)Unsafe.AsPointer(ref Unsafe.Add(ref o, 16))).AsInt32(), 0b00_01_10_11).AsUInt32()).AsInt32();
                var vprev1 = Avx2.ConvertToVector256Int64(Sse2.Shuffle(Sse2.LoadScalarVector128((ulong*)Unsafe.AsPointer(ref Unsafe.Add(ref o, 12))).AsInt32(), 0b00_01_10_11).AsUInt32()).AsInt32();
                var vprev2 = Avx2.ConvertToVector256Int64(Sse2.Shuffle(Sse2.LoadScalarVector128((ulong*)Unsafe.AsPointer(ref Unsafe.Add(ref o, 8))).AsInt32(), 0b00_01_10_11).AsUInt32()).AsInt32();
                var vprev3 = Avx2.ConvertToVector256Int64(Sse2.Shuffle(Sse2.LoadScalarVector128((ulong*)Unsafe.AsPointer(ref Unsafe.Add(ref o, 4))).AsInt32(), 0b00_01_10_11).AsUInt32()).AsInt32();
                var vprev4 = Avx2.ConvertToVector256Int64(Sse2.Shuffle(Avx2.MaskLoad((int*)Unsafe.AsPointer(ref o), mask).AsInt32(), 0b00_01_10_11)).AsInt32();
                for(nint i = 0; i < dataLength;)
                {
                    var res = Vector128.CreateScalar(Unsafe.Add(ref r, i));
                    sum256 = Avx2.Multiply(vcoeff0, vprev0);
                    sum256 = Avx2.Add(sum256, Avx2.Multiply(vcoeff1, vprev1));
                    sum256 = Avx2.Add(sum256, Avx2.Multiply(vcoeff2, vprev2));
                    sum256 = Avx2.Add(sum256, Avx2.Multiply(vcoeff3, vprev3));
                    sum256 = Avx2.Add(sum256, Avx2.Multiply(vcoeff4, vprev4));
                    sum = Sse2.Add(sum256.GetLower(), sum256.GetUpper());
                    sum = Sse2.Add(sum, Sse2.ShiftRightLogical128BitLane(sum, 8));
                    sum = Sse2.ShiftRightLogical(sum, vshift);
                    var yy = Sse2.Add(sum.AsInt32(), res);
#if NET5_0_OR_GREATER
                    Sse2.StoreScalar((int*)Unsafe.AsPointer(ref Unsafe.Add(ref d, i)), yy);
#else
                    Unsafe.Add(ref d, i) = yy.GetElement(0);
#endif
                    i++;
                    var yu = yy.ToVector256();
                    yu = Avx2.Permute4x64(yu.AsUInt64(), 0b00_00_00_00).AsInt32();
                    vprev4 = Avx2.Permute4x64(vprev4.AsInt64(), 0b10_01_00_11).AsInt32();
                    vprev4 = Avx2.Blend(vprev4, vprev3, 0b0000_0001);
                    vprev3 = Avx2.Permute4x64(vprev3.AsInt64(), 0b10_01_00_11).AsInt32();
                    vprev3 = Avx2.Blend(vprev3, vprev2, 0b0000_0001);
                    vprev2 = Avx2.Permute4x64(vprev2.AsInt64(), 0b10_01_00_11).AsInt32();
                    vprev2 = Avx2.Blend(vprev2, vprev1, 0b0000_0001);
                    vprev1 = Avx2.Permute4x64(vprev1.AsInt64(), 0b10_01_00_11).AsInt32();
                    vprev1 = Avx2.Blend(vprev1, vprev0, 0b0000_0001);
                    vprev0 = Avx2.Permute4x64(vprev0.AsInt64(), 0b10_01_00_11).AsInt32();
                    vprev0 = Avx2.Blend(vprev0, yu, 0b0000_0001);
                }
            }
#endregion Order19
#region Order20

            [MethodImpl(OptimizationUtils.InlineAndOptimizeIfPossible)]
            internal static bool RestoreSignalOrder20(int shiftsNeeded, ReadOnlySpan<int> residual, ReadOnlySpan<int> coeffs, Span<int> output)
            {
                if (Sse41.IsSupported)
                {
                    RestoreSignalOrder20Sse41(shiftsNeeded, residual, coeffs, output);
                    return true;
                }
                return false;
            }

            [MethodImpl(OptimizationUtils.InlineAndOptimizeIfPossible)]
            internal static unsafe void RestoreSignalOrder20Sse41(int shiftsNeeded, ReadOnlySpan<int> residual, ReadOnlySpan<int> coeffs, Span<int> output)
            {
                const int Order = 20;
                if (coeffs.Length < Order) return;
                _ = coeffs[Order - 1];
                var vshift = Vector128.CreateScalar(shiftsNeeded);
                ref var o = ref MemoryMarshal.GetReference(output);
                ref var d = ref Unsafe.Add(ref o, Order);
                ref var r = ref MemoryMarshal.GetReference(residual);
                ref var c = ref MemoryMarshal.GetReference(coeffs);
                Vector128<int> sum;
                var vzero = Vector128.Create(0);
                nint dataLength = output.Length - Order;
                var vcoeff0 = Sse2.LoadVector128((int*)Unsafe.AsPointer(ref Unsafe.Add(ref c, 0)));
                var vcoeff1 = Sse2.LoadVector128((int*)Unsafe.AsPointer(ref Unsafe.Add(ref c, 4)));
                var vcoeff2 = Sse2.LoadVector128((int*)Unsafe.AsPointer(ref Unsafe.Add(ref c, 8)));
                var vcoeff3 = Sse2.LoadVector128((int*)Unsafe.AsPointer(ref Unsafe.Add(ref c, 12)));
                var vcoeff4 = Sse2.LoadVector128((int*)Unsafe.AsPointer(ref Unsafe.Add(ref c, 16)));
                var vprev0 = Sse2.Shuffle(Sse2.LoadVector128((int*)Unsafe.AsPointer(ref Unsafe.Add(ref o, 16))).AsInt32(), 0b00_01_10_11);
                var vprev1 = Sse2.Shuffle(Sse2.LoadVector128((int*)Unsafe.AsPointer(ref Unsafe.Add(ref o, 12))).AsInt32(), 0b00_01_10_11);
                var vprev2 = Sse2.Shuffle(Sse2.LoadVector128((int*)Unsafe.AsPointer(ref Unsafe.Add(ref o, 8))).AsInt32(), 0b00_01_10_11);
                var vprev3 = Sse2.Shuffle(Sse2.LoadVector128((int*)Unsafe.AsPointer(ref Unsafe.Add(ref o, 4))).AsInt32(), 0b00_01_10_11);
                var vprev4 = Sse2.Shuffle(Sse2.LoadVector128((int*)Unsafe.AsPointer(ref Unsafe.Add(ref o, 0))).AsInt32(), 0b00_01_10_11);
                var vprev5 = Vector128.Create(0, 0, 0, 0);
                for (nint i = 0; i < dataLength; i++)
                {
                    var res = Vector128.CreateScalar(Unsafe.Add(ref r, i));
                    sum = Sse41.MultiplyLow(vcoeff0, vprev0);
                    sum = Sse2.Add(sum, Sse41.MultiplyLow(vcoeff1, vprev1));
                    sum = Sse2.Add(sum, Sse41.MultiplyLow(vcoeff2, vprev2));
                    sum = Sse2.Add(sum, Sse41.MultiplyLow(vcoeff3, vprev3));
                    sum = Sse2.Add(sum, Sse41.MultiplyLow(vcoeff4, vprev4));
                    var y = Ssse3.HorizontalAdd(sum, vzero);
                    y = Ssse3.HorizontalAdd(y, vzero);
                    y = Sse2.ShiftRightArithmetic(y, vshift);   //C# shift operator handling sucks so SSE2 is used instead(same latency, same throughput).
                    y = Sse2.Add(y, res);   //Avoids extract and insert
#if NET5_0_OR_GREATER
                    Sse2.StoreScalar((int*)Unsafe.AsPointer(ref Unsafe.Add(ref d, i)), y);
#else
                    Unsafe.Add(ref d, i) = y.GetElement(0);
#endif
                    y = Sse2.ShiftLeftLogical128BitLane(y, 12);
                    vprev4 = Ssse3.AlignRight(vprev4, vprev3, 12);
                    vprev3 = Ssse3.AlignRight(vprev3, vprev2, 12);
                    vprev2 = Ssse3.AlignRight(vprev2, vprev1, 12);
                    vprev1 = Ssse3.AlignRight(vprev1, vprev0, 12);
                    vprev0 = Ssse3.AlignRight(vprev0, y, 12);
                }
            }

            [MethodImpl(OptimizationUtils.InlineAndOptimizeIfPossible)]
            internal static bool RestoreSignalOrder20Wide(int shiftsNeeded, ReadOnlySpan<int> residual, ReadOnlySpan<int> coeffs, Span<int> output)
            {
                if (Sse41.IsSupported)
                {
                    RestoreSignalOrder20WideSse41(shiftsNeeded, residual, coeffs, output);
                    return true;
                }
                return false;
            }

            [MethodImpl(OptimizationUtils.InlineAndOptimizeIfPossible)]
            internal static unsafe void RestoreSignalOrder20WideSse41(int shiftsNeeded, ReadOnlySpan<int> residual, ReadOnlySpan<int> coeffs, Span<int> output)
            {
                const int Order = 20;
                if(coeffs.Length < Order) return;
                _ = coeffs[Order - 1];
                var vshift = Vector128.CreateScalar((long)shiftsNeeded);
                ref var c = ref MemoryMarshal.GetReference(coeffs);
                ref var o = ref MemoryMarshal.GetReference(output);
                ref var d = ref Unsafe.Add(ref o, Order);
                int dataLength = output.Length - Order;
                ref var r = ref MemoryMarshal.GetReference(residual);
                Vector128<long> sum;
                var vcoeff0 = Sse41.ConvertToVector128Int64(Sse2.LoadScalarVector128((ulong*)Unsafe.AsPointer(ref Unsafe.Add(ref c, 0))).AsUInt32()).AsInt32();
                var vcoeff1 = Sse41.ConvertToVector128Int64(Sse2.LoadScalarVector128((ulong*)Unsafe.AsPointer(ref Unsafe.Add(ref c, 2))).AsUInt32()).AsInt32();
                var vcoeff2 = Sse41.ConvertToVector128Int64(Sse2.LoadScalarVector128((ulong*)Unsafe.AsPointer(ref Unsafe.Add(ref c, 4))).AsUInt32()).AsInt32();
                var vcoeff3 = Sse41.ConvertToVector128Int64(Sse2.LoadScalarVector128((ulong*)Unsafe.AsPointer(ref Unsafe.Add(ref c, 6))).AsUInt32()).AsInt32();
                var vcoeff4 = Sse41.ConvertToVector128Int64(Sse2.LoadScalarVector128((ulong*)Unsafe.AsPointer(ref Unsafe.Add(ref c, 8))).AsUInt32()).AsInt32();
                var vcoeff5 = Sse41.ConvertToVector128Int64(Sse2.LoadScalarVector128((ulong*)Unsafe.AsPointer(ref Unsafe.Add(ref c, 10))).AsUInt32()).AsInt32();
                var vcoeff6 = Sse41.ConvertToVector128Int64(Sse2.LoadScalarVector128((ulong*)Unsafe.AsPointer(ref Unsafe.Add(ref c, 12))).AsUInt32()).AsInt32();
                var vcoeff7 = Sse41.ConvertToVector128Int64(Sse2.LoadScalarVector128((ulong*)Unsafe.AsPointer(ref Unsafe.Add(ref c, 14))).AsUInt32()).AsInt32();
                var vcoeff8 = Sse41.ConvertToVector128Int64(Sse2.LoadScalarVector128((ulong*)Unsafe.AsPointer(ref Unsafe.Add(ref c, 16))).AsUInt32()).AsInt32();
                var vcoeff9 = Sse41.ConvertToVector128Int64(Sse2.LoadScalarVector128((ulong*)Unsafe.AsPointer(ref Unsafe.Add(ref c, 18))).AsUInt32()).AsInt32();
                var vprev0 = Sse2.Shuffle(Sse2.LoadScalarVector128((ulong*)Unsafe.AsPointer(ref Unsafe.Add(ref o, 18))).AsInt32(), 0b11_00_11_01);
                var vprev1 = Sse2.Shuffle(Sse2.LoadScalarVector128((ulong*)Unsafe.AsPointer(ref Unsafe.Add(ref o, 16))).AsInt32(), 0b11_00_11_01);
                var vprev2 = Sse2.Shuffle(Sse2.LoadScalarVector128((ulong*)Unsafe.AsPointer(ref Unsafe.Add(ref o, 14))).AsInt32(), 0b11_00_11_01);
                var vprev3 = Sse2.Shuffle(Sse2.LoadScalarVector128((ulong*)Unsafe.AsPointer(ref Unsafe.Add(ref o, 12))).AsInt32(), 0b11_00_11_01);
                var vprev4 = Sse2.Shuffle(Sse2.LoadScalarVector128((ulong*)Unsafe.AsPointer(ref Unsafe.Add(ref o, 10))).AsInt32(), 0b11_00_11_01);
                var vprev5 = Sse2.Shuffle(Sse2.LoadScalarVector128((ulong*)Unsafe.AsPointer(ref Unsafe.Add(ref o, 8))).AsInt32(), 0b11_00_11_01);
                var vprev6 = Sse2.Shuffle(Sse2.LoadScalarVector128((ulong*)Unsafe.AsPointer(ref Unsafe.Add(ref o, 6))).AsInt32(), 0b11_00_11_01);
                var vprev7 = Sse2.Shuffle(Sse2.LoadScalarVector128((ulong*)Unsafe.AsPointer(ref Unsafe.Add(ref o, 4))).AsInt32(), 0b11_00_11_01);
                var vprev8 = Sse2.Shuffle(Sse2.LoadScalarVector128((ulong*)Unsafe.AsPointer(ref Unsafe.Add(ref o, 2))).AsInt32(), 0b11_00_11_01);
                var vprev9 = Sse2.Shuffle(Sse2.LoadScalarVector128((ulong*)Unsafe.AsPointer(ref Unsafe.Add(ref o, 0))).AsInt32(), 0b11_00_11_01);
                for(nint i = 0; i < dataLength;)
                {
                    var res = Vector128.CreateScalar(Unsafe.Add(ref r, i));
                    sum = Sse41.Multiply(vcoeff0, vprev0);
                    sum = Sse2.Add(sum, Sse41.Multiply(vcoeff1, vprev1));
                    sum = Sse2.Add(sum, Sse41.Multiply(vcoeff2, vprev2));
                    sum = Sse2.Add(sum, Sse41.Multiply(vcoeff3, vprev3));
                    sum = Sse2.Add(sum, Sse41.Multiply(vcoeff4, vprev4));
                    sum = Sse2.Add(sum, Sse41.Multiply(vcoeff5, vprev5));
                    sum = Sse2.Add(sum, Sse41.Multiply(vcoeff6, vprev6));
                    sum = Sse2.Add(sum, Sse41.Multiply(vcoeff7, vprev7));
                    sum = Sse2.Add(sum, Sse41.Multiply(vcoeff8, vprev8));
                    sum = Sse2.Add(sum, Sse41.Multiply(vcoeff9, vprev9));
                    sum = Sse2.Add(sum, Sse2.ShiftRightLogical128BitLane(sum, 8));
                    sum = Sse2.ShiftRightLogical(sum, vshift);
                    var yy = Sse2.Add(sum.AsInt32(), res);
#if NET5_0_OR_GREATER
                    Sse2.StoreScalar((int*)Unsafe.AsPointer(ref Unsafe.Add(ref d, i)), yy);
#else
                    Unsafe.Add(ref d, i) = yy.GetElement(0);
#endif
                    i++;
                    yy = Sse2.ShiftLeftLogical128BitLane(yy, 8);
                    vprev9 = Ssse3.AlignRight(vprev9, vprev8, 8);
                    vprev8 = Ssse3.AlignRight(vprev8, vprev7, 8);
                    vprev7 = Ssse3.AlignRight(vprev7, vprev6, 8);
                    vprev6 = Ssse3.AlignRight(vprev6, vprev5, 8);
                    vprev5 = Ssse3.AlignRight(vprev5, vprev4, 8);
                    vprev4 = Ssse3.AlignRight(vprev4, vprev3, 8);
                    vprev3 = Ssse3.AlignRight(vprev3, vprev2, 8);
                    vprev2 = Ssse3.AlignRight(vprev2, vprev1, 8);
                    vprev1 = Ssse3.AlignRight(vprev1, vprev0, 8);
                    vprev0 = Ssse3.AlignRight(vprev0, yy, 8);
                }
            }
            
            [MethodImpl(OptimizationUtils.InlineAndOptimizeIfPossible)]
            internal static unsafe void RestoreSignalOrder20WideAvx2(int shiftsNeeded, ReadOnlySpan<int> residual, ReadOnlySpan<int> coeffs, Span<int> output)
            {
                const int Order = 20;
                if(coeffs.Length < Order) return;
                _ = coeffs[Order - 1];
                var vshift = Vector128.CreateScalar((long)shiftsNeeded);
                ref var c = ref MemoryMarshal.GetReference(coeffs);
                ref var o = ref MemoryMarshal.GetReference(output);
                ref var d = ref Unsafe.Add(ref o, Order);
                int dataLength = output.Length - Order;
                ref var r = ref MemoryMarshal.GetReference(residual);
                Vector128<long> sum;
                Vector256<long> sum256;
                var vcoeff0 = Avx2.ConvertToVector256Int64(Sse2.LoadVector128((int*)Unsafe.AsPointer(ref Unsafe.Add(ref c, 0))).AsUInt32()).AsInt32();
                var vcoeff1 = Avx2.ConvertToVector256Int64(Sse2.LoadVector128((int*)Unsafe.AsPointer(ref Unsafe.Add(ref c, 4))).AsUInt32()).AsInt32();
                var vcoeff2 = Avx2.ConvertToVector256Int64(Sse2.LoadVector128((int*)Unsafe.AsPointer(ref Unsafe.Add(ref c, 8))).AsUInt32()).AsInt32();
                var vcoeff3 = Avx2.ConvertToVector256Int64(Sse2.LoadVector128((int*)Unsafe.AsPointer(ref Unsafe.Add(ref c, 12))).AsUInt32()).AsInt32();
                var vcoeff4 = Avx2.ConvertToVector256Int64(Sse2.LoadVector128((int*)Unsafe.AsPointer(ref Unsafe.Add(ref c, 16))).AsUInt32()).AsInt32();
                var vprev0 = Avx2.ConvertToVector256Int64(Sse2.Shuffle(Sse2.LoadScalarVector128((ulong*)Unsafe.AsPointer(ref Unsafe.Add(ref o, 16))).AsInt32(), 0b00_01_10_11).AsUInt32()).AsInt32();
                var vprev1 = Avx2.ConvertToVector256Int64(Sse2.Shuffle(Sse2.LoadScalarVector128((ulong*)Unsafe.AsPointer(ref Unsafe.Add(ref o, 12))).AsInt32(), 0b00_01_10_11).AsUInt32()).AsInt32();
                var vprev2 = Avx2.ConvertToVector256Int64(Sse2.Shuffle(Sse2.LoadScalarVector128((ulong*)Unsafe.AsPointer(ref Unsafe.Add(ref o, 8))).AsInt32(), 0b00_01_10_11).AsUInt32()).AsInt32();
                var vprev3 = Avx2.ConvertToVector256Int64(Sse2.Shuffle(Sse2.LoadScalarVector128((ulong*)Unsafe.AsPointer(ref Unsafe.Add(ref o, 4))).AsInt32(), 0b00_01_10_11).AsUInt32()).AsInt32();
                var vprev4 = Avx2.ConvertToVector256Int64(Sse2.Shuffle(Sse2.LoadScalarVector128((ulong*)Unsafe.AsPointer(ref Unsafe.Add(ref o, 0))).AsInt32(), 0b00_01_10_11).AsUInt32()).AsInt32();
                for(nint i = 0; i < dataLength;)
                {
                    var res = Vector128.CreateScalar(Unsafe.Add(ref r, i));
                    sum256 = Avx2.Multiply(vcoeff0, vprev0);
                    sum256 = Avx2.Add(sum256, Avx2.Multiply(vcoeff1, vprev1));
                    sum256 = Avx2.Add(sum256, Avx2.Multiply(vcoeff2, vprev2));
                    sum256 = Avx2.Add(sum256, Avx2.Multiply(vcoeff3, vprev3));
                    sum256 = Avx2.Add(sum256, Avx2.Multiply(vcoeff4, vprev4));
                    sum = Sse2.Add(sum256.GetLower(), sum256.GetUpper());
                    sum = Sse2.Add(sum, Sse2.ShiftRightLogical128BitLane(sum, 8));
                    sum = Sse2.ShiftRightLogical(sum, vshift);
                    var yy = Sse2.Add(sum.AsInt32(), res);
#if NET5_0_OR_GREATER
                    Sse2.StoreScalar((int*)Unsafe.AsPointer(ref Unsafe.Add(ref d, i)), yy);
#else
                    Unsafe.Add(ref d, i) = yy.GetElement(0);
#endif
                    i++;
                    var yu = yy.ToVector256();
                    yu = Avx2.Permute4x64(yu.AsUInt64(), 0b00_00_00_00).AsInt32();
                    vprev4 = Avx2.Permute4x64(vprev4.AsInt64(), 0b10_01_00_11).AsInt32();
                    vprev4 = Avx2.Blend(vprev4, vprev3, 0b0000_0001);
                    vprev3 = Avx2.Permute4x64(vprev3.AsInt64(), 0b10_01_00_11).AsInt32();
                    vprev3 = Avx2.Blend(vprev3, vprev2, 0b0000_0001);
                    vprev2 = Avx2.Permute4x64(vprev2.AsInt64(), 0b10_01_00_11).AsInt32();
                    vprev2 = Avx2.Blend(vprev2, vprev1, 0b0000_0001);
                    vprev1 = Avx2.Permute4x64(vprev1.AsInt64(), 0b10_01_00_11).AsInt32();
                    vprev1 = Avx2.Blend(vprev1, vprev0, 0b0000_0001);
                    vprev0 = Avx2.Permute4x64(vprev0.AsInt64(), 0b10_01_00_11).AsInt32();
                    vprev0 = Avx2.Blend(vprev0, yu, 0b0000_0001);
                }
            }
#endregion Order20
#region Order21

            [MethodImpl(OptimizationUtils.InlineAndOptimizeIfPossible)]
            internal static bool RestoreSignalOrder21(int shiftsNeeded, ReadOnlySpan<int> residual, ReadOnlySpan<int> coeffs, Span<int> output)
            {
                if (Sse41.IsSupported)
                {
                    RestoreSignalOrder21Sse41(shiftsNeeded, residual, coeffs, output);
                    return true;
                }
                return false;
            }

            [MethodImpl(OptimizationUtils.InlineAndOptimizeIfPossible)]
            internal static unsafe void RestoreSignalOrder21Sse41(int shiftsNeeded, ReadOnlySpan<int> residual, ReadOnlySpan<int> coeffs, Span<int> output)
            {
                const int Order = 21;
                if (coeffs.Length < Order) return;
                _ = coeffs[Order - 1];
                var vshift = Vector128.CreateScalar(shiftsNeeded);
                ref var o = ref MemoryMarshal.GetReference(output);
                ref var d = ref Unsafe.Add(ref o, Order);
                ref var r = ref MemoryMarshal.GetReference(residual);
                ref var c = ref MemoryMarshal.GetReference(coeffs);
                Vector128<int> sum;
                var vzero = Vector128.Create(0);
                nint dataLength = output.Length - Order;
                var vcoeff0 = Sse2.LoadVector128((int*)Unsafe.AsPointer(ref Unsafe.Add(ref c, 0)));
                var vcoeff1 = Sse2.LoadVector128((int*)Unsafe.AsPointer(ref Unsafe.Add(ref c, 4)));
                var vcoeff2 = Sse2.LoadVector128((int*)Unsafe.AsPointer(ref Unsafe.Add(ref c, 8)));
                var vcoeff3 = Sse2.LoadVector128((int*)Unsafe.AsPointer(ref Unsafe.Add(ref c, 12)));
                var vcoeff4 = Sse2.LoadVector128((int*)Unsafe.AsPointer(ref Unsafe.Add(ref c, 16)));
                var vcoeff5 = Vector128.Create(Unsafe.Add(ref c, 20), 0, 0, 0);
                var vprev0 = Sse2.Shuffle(Sse2.LoadVector128((int*)Unsafe.AsPointer(ref Unsafe.Add(ref o, 17))).AsInt32(), 0b00_01_10_11);
                var vprev1 = Sse2.Shuffle(Sse2.LoadVector128((int*)Unsafe.AsPointer(ref Unsafe.Add(ref o, 13))).AsInt32(), 0b00_01_10_11);
                var vprev2 = Sse2.Shuffle(Sse2.LoadVector128((int*)Unsafe.AsPointer(ref Unsafe.Add(ref o, 9))).AsInt32(), 0b00_01_10_11);
                var vprev3 = Sse2.Shuffle(Sse2.LoadVector128((int*)Unsafe.AsPointer(ref Unsafe.Add(ref o, 5))).AsInt32(), 0b00_01_10_11);
                var vprev4 = Sse2.Shuffle(Sse2.LoadVector128((int*)Unsafe.AsPointer(ref Unsafe.Add(ref o, 1))).AsInt32(), 0b00_01_10_11);
                var vprev5 = Vector128.Create(Unsafe.Add(ref o, 0), 0, 0, 0);
                for (nint i = 0; i < dataLength; i++)
                {
                    var res = Vector128.CreateScalar(Unsafe.Add(ref r, i));
                    sum = Sse41.MultiplyLow(vcoeff0, vprev0);
                    sum = Sse2.Add(sum, Sse41.MultiplyLow(vcoeff1, vprev1));
                    sum = Sse2.Add(sum, Sse41.MultiplyLow(vcoeff2, vprev2));
                    sum = Sse2.Add(sum, Sse41.MultiplyLow(vcoeff3, vprev3));
                    sum = Sse2.Add(sum, Sse41.MultiplyLow(vcoeff4, vprev4));
                    sum = Sse2.Add(sum, Sse41.MultiplyLow(vcoeff5, vprev5));
                    var y = Ssse3.HorizontalAdd(sum, vzero);
                    y = Ssse3.HorizontalAdd(y, vzero);
                    y = Sse2.ShiftRightArithmetic(y, vshift);   //C# shift operator handling sucks so SSE2 is used instead(same latency, same throughput).
                    y = Sse2.Add(y, res);   //Avoids extract and insert
#if NET5_0_OR_GREATER
                    Sse2.StoreScalar((int*)Unsafe.AsPointer(ref Unsafe.Add(ref d, i)), y);
#else
                    Unsafe.Add(ref d, i) = y.GetElement(0);
#endif
                    y = Sse2.ShiftLeftLogical128BitLane(y, 12);
                    vprev5 = Ssse3.AlignRight(vprev5, vprev4, 12);
                    vprev4 = Ssse3.AlignRight(vprev4, vprev3, 12);
                    vprev3 = Ssse3.AlignRight(vprev3, vprev2, 12);
                    vprev2 = Ssse3.AlignRight(vprev2, vprev1, 12);
                    vprev1 = Ssse3.AlignRight(vprev1, vprev0, 12);
                    vprev0 = Ssse3.AlignRight(vprev0, y, 12);
                }
            }

            [MethodImpl(OptimizationUtils.InlineAndOptimizeIfPossible)]
            internal static bool RestoreSignalOrder21Wide(int shiftsNeeded, ReadOnlySpan<int> residual, ReadOnlySpan<int> coeffs, Span<int> output)
            {
                if (Sse41.IsSupported)
                {
                    RestoreSignalOrder21WideSse41(shiftsNeeded, residual, coeffs, output);
                    return true;
                }
                return false;
            }

            [MethodImpl(OptimizationUtils.InlineAndOptimizeIfPossible)]
            internal static unsafe void RestoreSignalOrder21WideSse41(int shiftsNeeded, ReadOnlySpan<int> residual, ReadOnlySpan<int> coeffs, Span<int> output)
            {
                const int Order = 21;
                if(coeffs.Length < Order) return;
                _ = coeffs[Order - 1];
                var vshift = Vector128.CreateScalar((long)shiftsNeeded);
                ref var c = ref MemoryMarshal.GetReference(coeffs);
                ref var o = ref MemoryMarshal.GetReference(output);
                ref var d = ref Unsafe.Add(ref o, Order);
                int dataLength = output.Length - Order;
                ref var r = ref MemoryMarshal.GetReference(residual);
                Vector128<long> sum;
                var vcoeff0 = Sse41.ConvertToVector128Int64(Sse2.LoadScalarVector128((ulong*)Unsafe.AsPointer(ref Unsafe.Add(ref c, 0))).AsUInt32()).AsInt32();
                var vcoeff1 = Sse41.ConvertToVector128Int64(Sse2.LoadScalarVector128((ulong*)Unsafe.AsPointer(ref Unsafe.Add(ref c, 2))).AsUInt32()).AsInt32();
                var vcoeff2 = Sse41.ConvertToVector128Int64(Sse2.LoadScalarVector128((ulong*)Unsafe.AsPointer(ref Unsafe.Add(ref c, 4))).AsUInt32()).AsInt32();
                var vcoeff3 = Sse41.ConvertToVector128Int64(Sse2.LoadScalarVector128((ulong*)Unsafe.AsPointer(ref Unsafe.Add(ref c, 6))).AsUInt32()).AsInt32();
                var vcoeff4 = Sse41.ConvertToVector128Int64(Sse2.LoadScalarVector128((ulong*)Unsafe.AsPointer(ref Unsafe.Add(ref c, 8))).AsUInt32()).AsInt32();
                var vcoeff5 = Sse41.ConvertToVector128Int64(Sse2.LoadScalarVector128((ulong*)Unsafe.AsPointer(ref Unsafe.Add(ref c, 10))).AsUInt32()).AsInt32();
                var vcoeff6 = Sse41.ConvertToVector128Int64(Sse2.LoadScalarVector128((ulong*)Unsafe.AsPointer(ref Unsafe.Add(ref c, 12))).AsUInt32()).AsInt32();
                var vcoeff7 = Sse41.ConvertToVector128Int64(Sse2.LoadScalarVector128((ulong*)Unsafe.AsPointer(ref Unsafe.Add(ref c, 14))).AsUInt32()).AsInt32();
                var vcoeff8 = Sse41.ConvertToVector128Int64(Sse2.LoadScalarVector128((ulong*)Unsafe.AsPointer(ref Unsafe.Add(ref c, 16))).AsUInt32()).AsInt32();
                var vcoeff9 = Sse41.ConvertToVector128Int64(Sse2.LoadScalarVector128((ulong*)Unsafe.AsPointer(ref Unsafe.Add(ref c, 18))).AsUInt32()).AsInt32();
                var vcoeff10 = Sse41.ConvertToVector128Int64(Sse2.LoadScalarVector128((uint*)Unsafe.AsPointer(ref Unsafe.Add(ref c, 20))).AsUInt32()).AsInt32();
                var vprev0 = Sse2.Shuffle(Sse2.LoadScalarVector128((ulong*)Unsafe.AsPointer(ref Unsafe.Add(ref o, 19))).AsInt32(), 0b11_00_11_01);
                var vprev1 = Sse2.Shuffle(Sse2.LoadScalarVector128((ulong*)Unsafe.AsPointer(ref Unsafe.Add(ref o, 17))).AsInt32(), 0b11_00_11_01);
                var vprev2 = Sse2.Shuffle(Sse2.LoadScalarVector128((ulong*)Unsafe.AsPointer(ref Unsafe.Add(ref o, 15))).AsInt32(), 0b11_00_11_01);
                var vprev3 = Sse2.Shuffle(Sse2.LoadScalarVector128((ulong*)Unsafe.AsPointer(ref Unsafe.Add(ref o, 13))).AsInt32(), 0b11_00_11_01);
                var vprev4 = Sse2.Shuffle(Sse2.LoadScalarVector128((ulong*)Unsafe.AsPointer(ref Unsafe.Add(ref o, 11))).AsInt32(), 0b11_00_11_01);
                var vprev5 = Sse2.Shuffle(Sse2.LoadScalarVector128((ulong*)Unsafe.AsPointer(ref Unsafe.Add(ref o, 9))).AsInt32(), 0b11_00_11_01);
                var vprev6 = Sse2.Shuffle(Sse2.LoadScalarVector128((ulong*)Unsafe.AsPointer(ref Unsafe.Add(ref o, 7))).AsInt32(), 0b11_00_11_01);
                var vprev7 = Sse2.Shuffle(Sse2.LoadScalarVector128((ulong*)Unsafe.AsPointer(ref Unsafe.Add(ref o, 5))).AsInt32(), 0b11_00_11_01);
                var vprev8 = Sse2.Shuffle(Sse2.LoadScalarVector128((ulong*)Unsafe.AsPointer(ref Unsafe.Add(ref o, 3))).AsInt32(), 0b11_00_11_01);
                var vprev9 = Sse2.Shuffle(Sse2.LoadScalarVector128((ulong*)Unsafe.AsPointer(ref Unsafe.Add(ref o, 1))).AsInt32(), 0b11_00_11_01);
                var vprev10 = Sse2.Shuffle(Sse2.LoadScalarVector128((ulong*)Unsafe.AsPointer(ref Unsafe.Add(ref o, 0))).AsInt32(), 0b11_00_11_00);
                for(nint i = 0; i < dataLength;)
                {
                    var res = Vector128.CreateScalar(Unsafe.Add(ref r, i));
                    sum = Sse41.Multiply(vcoeff0, vprev0);
                    sum = Sse2.Add(sum, Sse41.Multiply(vcoeff1, vprev1));
                    sum = Sse2.Add(sum, Sse41.Multiply(vcoeff2, vprev2));
                    sum = Sse2.Add(sum, Sse41.Multiply(vcoeff3, vprev3));
                    sum = Sse2.Add(sum, Sse41.Multiply(vcoeff4, vprev4));
                    sum = Sse2.Add(sum, Sse41.Multiply(vcoeff5, vprev5));
                    sum = Sse2.Add(sum, Sse41.Multiply(vcoeff6, vprev6));
                    sum = Sse2.Add(sum, Sse41.Multiply(vcoeff7, vprev7));
                    sum = Sse2.Add(sum, Sse41.Multiply(vcoeff8, vprev8));
                    sum = Sse2.Add(sum, Sse41.Multiply(vcoeff9, vprev9));
                    sum = Sse2.Add(sum, Sse41.Multiply(vcoeff10, vprev10));
                    sum = Sse2.Add(sum, Sse2.ShiftRightLogical128BitLane(sum, 8));
                    sum = Sse2.ShiftRightLogical(sum, vshift);
                    var yy = Sse2.Add(sum.AsInt32(), res);
#if NET5_0_OR_GREATER
                    Sse2.StoreScalar((int*)Unsafe.AsPointer(ref Unsafe.Add(ref d, i)), yy);
#else
                    Unsafe.Add(ref d, i) = yy.GetElement(0);
#endif
                    i++;
                    yy = Sse2.ShiftLeftLogical128BitLane(yy, 8);
                    vprev10 = Ssse3.AlignRight(vprev10, vprev9, 8);
                    vprev9 = Ssse3.AlignRight(vprev9, vprev8, 8);
                    vprev8 = Ssse3.AlignRight(vprev8, vprev7, 8);
                    vprev7 = Ssse3.AlignRight(vprev7, vprev6, 8);
                    vprev6 = Ssse3.AlignRight(vprev6, vprev5, 8);
                    vprev5 = Ssse3.AlignRight(vprev5, vprev4, 8);
                    vprev4 = Ssse3.AlignRight(vprev4, vprev3, 8);
                    vprev3 = Ssse3.AlignRight(vprev3, vprev2, 8);
                    vprev2 = Ssse3.AlignRight(vprev2, vprev1, 8);
                    vprev1 = Ssse3.AlignRight(vprev1, vprev0, 8);
                    vprev0 = Ssse3.AlignRight(vprev0, yy, 8);
                }
            }
            
            [MethodImpl(OptimizationUtils.InlineAndOptimizeIfPossible)]
            internal static unsafe void RestoreSignalOrder21WideAvx2(int shiftsNeeded, ReadOnlySpan<int> residual, ReadOnlySpan<int> coeffs, Span<int> output)
            {
                const int Order = 21;
                if(coeffs.Length < Order) return;
                _ = coeffs[Order - 1];
                var vshift = Vector128.CreateScalar((long)shiftsNeeded);
                ref var c = ref MemoryMarshal.GetReference(coeffs);
                ref var o = ref MemoryMarshal.GetReference(output);
                ref var d = ref Unsafe.Add(ref o, Order);
                int dataLength = output.Length - Order;
                ref var r = ref MemoryMarshal.GetReference(residual);
                Vector128<long> sum;
                Vector256<long> sum256;
                var vcoeff0 = Avx2.ConvertToVector256Int64(Sse2.LoadVector128((int*)Unsafe.AsPointer(ref Unsafe.Add(ref c, 0))).AsUInt32()).AsInt32();
                var vcoeff1 = Avx2.ConvertToVector256Int64(Sse2.LoadVector128((int*)Unsafe.AsPointer(ref Unsafe.Add(ref c, 4))).AsUInt32()).AsInt32();
                var vcoeff2 = Avx2.ConvertToVector256Int64(Sse2.LoadVector128((int*)Unsafe.AsPointer(ref Unsafe.Add(ref c, 8))).AsUInt32()).AsInt32();
                var vcoeff3 = Avx2.ConvertToVector256Int64(Sse2.LoadVector128((int*)Unsafe.AsPointer(ref Unsafe.Add(ref c, 12))).AsUInt32()).AsInt32();
                var vcoeff4 = Avx2.ConvertToVector256Int64(Sse2.LoadVector128((int*)Unsafe.AsPointer(ref Unsafe.Add(ref c, 16))).AsUInt32()).AsInt32();
                var vcoeff5 = Avx2.ConvertToVector256Int64(Sse2.LoadScalarVector128((uint*)Unsafe.AsPointer(ref Unsafe.Add(ref c, 20))).AsUInt32()).AsInt32();
                var vprev0 = Avx2.ConvertToVector256Int64(Sse2.Shuffle(Sse2.LoadScalarVector128((ulong*)Unsafe.AsPointer(ref Unsafe.Add(ref o, 18))).AsInt32(), 0b00_01_10_11).AsUInt32()).AsInt32();
                var vprev1 = Avx2.ConvertToVector256Int64(Sse2.Shuffle(Sse2.LoadScalarVector128((ulong*)Unsafe.AsPointer(ref Unsafe.Add(ref o, 14))).AsInt32(), 0b00_01_10_11).AsUInt32()).AsInt32();
                var vprev2 = Avx2.ConvertToVector256Int64(Sse2.Shuffle(Sse2.LoadScalarVector128((ulong*)Unsafe.AsPointer(ref Unsafe.Add(ref o, 10))).AsInt32(), 0b00_01_10_11).AsUInt32()).AsInt32();
                var vprev3 = Avx2.ConvertToVector256Int64(Sse2.Shuffle(Sse2.LoadScalarVector128((ulong*)Unsafe.AsPointer(ref Unsafe.Add(ref o, 6))).AsInt32(), 0b00_01_10_11).AsUInt32()).AsInt32();
                var vprev4 = Avx2.ConvertToVector256Int64(Sse2.Shuffle(Sse2.LoadScalarVector128((ulong*)Unsafe.AsPointer(ref Unsafe.Add(ref o, 2))).AsInt32(), 0b00_01_10_11).AsUInt32()).AsInt32();
                var vprev5 = Avx2.ConvertToVector256Int64(Sse2.Shuffle(Sse2.LoadScalarVector128((uint*)Unsafe.AsPointer(ref o)).AsInt32(), 0b00_01_10_11).AsUInt32()).AsInt32();
                for(nint i = 0; i < dataLength;)
                {
                    var res = Vector128.CreateScalar(Unsafe.Add(ref r, i));
                    sum256 = Avx2.Multiply(vcoeff0, vprev0);
                    sum256 = Avx2.Add(sum256, Avx2.Multiply(vcoeff1, vprev1));
                    sum256 = Avx2.Add(sum256, Avx2.Multiply(vcoeff2, vprev2));
                    sum256 = Avx2.Add(sum256, Avx2.Multiply(vcoeff3, vprev3));
                    sum256 = Avx2.Add(sum256, Avx2.Multiply(vcoeff4, vprev4));
                    sum256 = Avx2.Add(sum256, Avx2.Multiply(vcoeff5, vprev5));
                    sum = Sse2.Add(sum256.GetLower(), sum256.GetUpper());
                    sum = Sse2.Add(sum, Sse2.ShiftRightLogical128BitLane(sum, 8));
                    sum = Sse2.ShiftRightLogical(sum, vshift);
                    var yy = Sse2.Add(sum.AsInt32(), res);
#if NET5_0_OR_GREATER
                    Sse2.StoreScalar((int*)Unsafe.AsPointer(ref Unsafe.Add(ref d, i)), yy);
#else
                    Unsafe.Add(ref d, i) = yy.GetElement(0);
#endif
                    i++;
                    var yu = yy.ToVector256();
                    yu = Avx2.Permute4x64(yu.AsUInt64(), 0b00_00_00_00).AsInt32();
                    vprev5 = Avx2.Permute4x64(vprev5.AsInt64(), 0b10_01_00_11).AsInt32();
                    vprev5 = Avx2.Blend(vprev5, vprev4, 0b0000_0001);
                    vprev4 = Avx2.Permute4x64(vprev4.AsInt64(), 0b10_01_00_11).AsInt32();
                    vprev4 = Avx2.Blend(vprev4, vprev3, 0b0000_0001);
                    vprev3 = Avx2.Permute4x64(vprev3.AsInt64(), 0b10_01_00_11).AsInt32();
                    vprev3 = Avx2.Blend(vprev3, vprev2, 0b0000_0001);
                    vprev2 = Avx2.Permute4x64(vprev2.AsInt64(), 0b10_01_00_11).AsInt32();
                    vprev2 = Avx2.Blend(vprev2, vprev1, 0b0000_0001);
                    vprev1 = Avx2.Permute4x64(vprev1.AsInt64(), 0b10_01_00_11).AsInt32();
                    vprev1 = Avx2.Blend(vprev1, vprev0, 0b0000_0001);
                    vprev0 = Avx2.Permute4x64(vprev0.AsInt64(), 0b10_01_00_11).AsInt32();
                    vprev0 = Avx2.Blend(vprev0, yu, 0b0000_0001);
                }
            }
#endregion Order21
#region Order22

            [MethodImpl(OptimizationUtils.InlineAndOptimizeIfPossible)]
            internal static bool RestoreSignalOrder22(int shiftsNeeded, ReadOnlySpan<int> residual, ReadOnlySpan<int> coeffs, Span<int> output)
            {
                if (Sse41.IsSupported)
                {
                    RestoreSignalOrder22Sse41(shiftsNeeded, residual, coeffs, output);
                    return true;
                }
                return false;
            }

            [MethodImpl(OptimizationUtils.InlineAndOptimizeIfPossible)]
            internal static unsafe void RestoreSignalOrder22Sse41(int shiftsNeeded, ReadOnlySpan<int> residual, ReadOnlySpan<int> coeffs, Span<int> output)
            {
                const int Order = 22;
                if (coeffs.Length < Order) return;
                _ = coeffs[Order - 1];
                var vshift = Vector128.CreateScalar(shiftsNeeded);
                ref var o = ref MemoryMarshal.GetReference(output);
                ref var d = ref Unsafe.Add(ref o, Order);
                ref var r = ref MemoryMarshal.GetReference(residual);
                ref var c = ref MemoryMarshal.GetReference(coeffs);
                Vector128<int> sum;
                var vzero = Vector128.Create(0);
                nint dataLength = output.Length - Order;
                var vcoeff0 = Sse2.LoadVector128((int*)Unsafe.AsPointer(ref Unsafe.Add(ref c, 0)));
                var vcoeff1 = Sse2.LoadVector128((int*)Unsafe.AsPointer(ref Unsafe.Add(ref c, 4)));
                var vcoeff2 = Sse2.LoadVector128((int*)Unsafe.AsPointer(ref Unsafe.Add(ref c, 8)));
                var vcoeff3 = Sse2.LoadVector128((int*)Unsafe.AsPointer(ref Unsafe.Add(ref c, 12)));
                var vcoeff4 = Sse2.LoadVector128((int*)Unsafe.AsPointer(ref Unsafe.Add(ref c, 16)));
                var vcoeff5 = Vector128.Create(Unsafe.Add(ref c, 20), Unsafe.Add(ref c, 21), 0, 0);
                var vprev0 = Sse2.Shuffle(Sse2.LoadVector128((int*)Unsafe.AsPointer(ref Unsafe.Add(ref o, 18))).AsInt32(), 0b00_01_10_11);
                var vprev1 = Sse2.Shuffle(Sse2.LoadVector128((int*)Unsafe.AsPointer(ref Unsafe.Add(ref o, 14))).AsInt32(), 0b00_01_10_11);
                var vprev2 = Sse2.Shuffle(Sse2.LoadVector128((int*)Unsafe.AsPointer(ref Unsafe.Add(ref o, 10))).AsInt32(), 0b00_01_10_11);
                var vprev3 = Sse2.Shuffle(Sse2.LoadVector128((int*)Unsafe.AsPointer(ref Unsafe.Add(ref o, 6))).AsInt32(), 0b00_01_10_11);
                var vprev4 = Sse2.Shuffle(Sse2.LoadVector128((int*)Unsafe.AsPointer(ref Unsafe.Add(ref o, 2))).AsInt32(), 0b00_01_10_11);
                var vprev5 = Vector128.Create(Unsafe.Add(ref o, 1), Unsafe.Add(ref o, 0), 0, 0);
                for (nint i = 0; i < dataLength; i++)
                {
                    var res = Vector128.CreateScalar(Unsafe.Add(ref r, i));
                    sum = Sse41.MultiplyLow(vcoeff0, vprev0);
                    sum = Sse2.Add(sum, Sse41.MultiplyLow(vcoeff1, vprev1));
                    sum = Sse2.Add(sum, Sse41.MultiplyLow(vcoeff2, vprev2));
                    sum = Sse2.Add(sum, Sse41.MultiplyLow(vcoeff3, vprev3));
                    sum = Sse2.Add(sum, Sse41.MultiplyLow(vcoeff4, vprev4));
                    sum = Sse2.Add(sum, Sse41.MultiplyLow(vcoeff5, vprev5));
                    var y = Ssse3.HorizontalAdd(sum, vzero);
                    y = Ssse3.HorizontalAdd(y, vzero);
                    y = Sse2.ShiftRightArithmetic(y, vshift);   //C# shift operator handling sucks so SSE2 is used instead(same latency, same throughput).
                    y = Sse2.Add(y, res);   //Avoids extract and insert
#if NET5_0_OR_GREATER
                    Sse2.StoreScalar((int*)Unsafe.AsPointer(ref Unsafe.Add(ref d, i)), y);
#else
                    Unsafe.Add(ref d, i) = y.GetElement(0);
#endif
                    y = Sse2.ShiftLeftLogical128BitLane(y, 12);
                    vprev5 = Ssse3.AlignRight(vprev5, vprev4, 12);
                    vprev4 = Ssse3.AlignRight(vprev4, vprev3, 12);
                    vprev3 = Ssse3.AlignRight(vprev3, vprev2, 12);
                    vprev2 = Ssse3.AlignRight(vprev2, vprev1, 12);
                    vprev1 = Ssse3.AlignRight(vprev1, vprev0, 12);
                    vprev0 = Ssse3.AlignRight(vprev0, y, 12);
                }
            }

            [MethodImpl(OptimizationUtils.InlineAndOptimizeIfPossible)]
            internal static bool RestoreSignalOrder22Wide(int shiftsNeeded, ReadOnlySpan<int> residual, ReadOnlySpan<int> coeffs, Span<int> output)
            {
                if (Sse41.IsSupported)
                {
                    RestoreSignalOrder22WideSse41(shiftsNeeded, residual, coeffs, output);
                    return true;
                }
                return false;
            }

            [MethodImpl(OptimizationUtils.InlineAndOptimizeIfPossible)]
            internal static unsafe void RestoreSignalOrder22WideSse41(int shiftsNeeded, ReadOnlySpan<int> residual, ReadOnlySpan<int> coeffs, Span<int> output)
            {
                const int Order = 22;
                if(coeffs.Length < Order) return;
                _ = coeffs[Order - 1];
                var vshift = Vector128.CreateScalar((long)shiftsNeeded);
                ref var c = ref MemoryMarshal.GetReference(coeffs);
                ref var o = ref MemoryMarshal.GetReference(output);
                ref var d = ref Unsafe.Add(ref o, Order);
                int dataLength = output.Length - Order;
                ref var r = ref MemoryMarshal.GetReference(residual);
                Vector128<long> sum;
                var vcoeff0 = Sse41.ConvertToVector128Int64(Sse2.LoadScalarVector128((ulong*)Unsafe.AsPointer(ref Unsafe.Add(ref c, 0))).AsUInt32()).AsInt32();
                var vcoeff1 = Sse41.ConvertToVector128Int64(Sse2.LoadScalarVector128((ulong*)Unsafe.AsPointer(ref Unsafe.Add(ref c, 2))).AsUInt32()).AsInt32();
                var vcoeff2 = Sse41.ConvertToVector128Int64(Sse2.LoadScalarVector128((ulong*)Unsafe.AsPointer(ref Unsafe.Add(ref c, 4))).AsUInt32()).AsInt32();
                var vcoeff3 = Sse41.ConvertToVector128Int64(Sse2.LoadScalarVector128((ulong*)Unsafe.AsPointer(ref Unsafe.Add(ref c, 6))).AsUInt32()).AsInt32();
                var vcoeff4 = Sse41.ConvertToVector128Int64(Sse2.LoadScalarVector128((ulong*)Unsafe.AsPointer(ref Unsafe.Add(ref c, 8))).AsUInt32()).AsInt32();
                var vcoeff5 = Sse41.ConvertToVector128Int64(Sse2.LoadScalarVector128((ulong*)Unsafe.AsPointer(ref Unsafe.Add(ref c, 10))).AsUInt32()).AsInt32();
                var vcoeff6 = Sse41.ConvertToVector128Int64(Sse2.LoadScalarVector128((ulong*)Unsafe.AsPointer(ref Unsafe.Add(ref c, 12))).AsUInt32()).AsInt32();
                var vcoeff7 = Sse41.ConvertToVector128Int64(Sse2.LoadScalarVector128((ulong*)Unsafe.AsPointer(ref Unsafe.Add(ref c, 14))).AsUInt32()).AsInt32();
                var vcoeff8 = Sse41.ConvertToVector128Int64(Sse2.LoadScalarVector128((ulong*)Unsafe.AsPointer(ref Unsafe.Add(ref c, 16))).AsUInt32()).AsInt32();
                var vcoeff9 = Sse41.ConvertToVector128Int64(Sse2.LoadScalarVector128((ulong*)Unsafe.AsPointer(ref Unsafe.Add(ref c, 18))).AsUInt32()).AsInt32();
                var vcoeff10 = Sse41.ConvertToVector128Int64(Sse2.LoadScalarVector128((ulong*)Unsafe.AsPointer(ref Unsafe.Add(ref c, 20))).AsUInt32()).AsInt32();
                var vprev0 = Sse2.Shuffle(Sse2.LoadScalarVector128((ulong*)Unsafe.AsPointer(ref Unsafe.Add(ref o, 20))).AsInt32(), 0b11_00_11_01);
                var vprev1 = Sse2.Shuffle(Sse2.LoadScalarVector128((ulong*)Unsafe.AsPointer(ref Unsafe.Add(ref o, 18))).AsInt32(), 0b11_00_11_01);
                var vprev2 = Sse2.Shuffle(Sse2.LoadScalarVector128((ulong*)Unsafe.AsPointer(ref Unsafe.Add(ref o, 16))).AsInt32(), 0b11_00_11_01);
                var vprev3 = Sse2.Shuffle(Sse2.LoadScalarVector128((ulong*)Unsafe.AsPointer(ref Unsafe.Add(ref o, 14))).AsInt32(), 0b11_00_11_01);
                var vprev4 = Sse2.Shuffle(Sse2.LoadScalarVector128((ulong*)Unsafe.AsPointer(ref Unsafe.Add(ref o, 12))).AsInt32(), 0b11_00_11_01);
                var vprev5 = Sse2.Shuffle(Sse2.LoadScalarVector128((ulong*)Unsafe.AsPointer(ref Unsafe.Add(ref o, 10))).AsInt32(), 0b11_00_11_01);
                var vprev6 = Sse2.Shuffle(Sse2.LoadScalarVector128((ulong*)Unsafe.AsPointer(ref Unsafe.Add(ref o, 8))).AsInt32(), 0b11_00_11_01);
                var vprev7 = Sse2.Shuffle(Sse2.LoadScalarVector128((ulong*)Unsafe.AsPointer(ref Unsafe.Add(ref o, 6))).AsInt32(), 0b11_00_11_01);
                var vprev8 = Sse2.Shuffle(Sse2.LoadScalarVector128((ulong*)Unsafe.AsPointer(ref Unsafe.Add(ref o, 4))).AsInt32(), 0b11_00_11_01);
                var vprev9 = Sse2.Shuffle(Sse2.LoadScalarVector128((ulong*)Unsafe.AsPointer(ref Unsafe.Add(ref o, 2))).AsInt32(), 0b11_00_11_01);
                var vprev10 = Sse2.Shuffle(Sse2.LoadScalarVector128((ulong*)Unsafe.AsPointer(ref Unsafe.Add(ref o, 0))).AsInt32(), 0b11_00_11_01);
                for(nint i = 0; i < dataLength;)
                {
                    var res = Vector128.CreateScalar(Unsafe.Add(ref r, i));
                    sum = Sse41.Multiply(vcoeff0, vprev0);
                    sum = Sse2.Add(sum, Sse41.Multiply(vcoeff1, vprev1));
                    sum = Sse2.Add(sum, Sse41.Multiply(vcoeff2, vprev2));
                    sum = Sse2.Add(sum, Sse41.Multiply(vcoeff3, vprev3));
                    sum = Sse2.Add(sum, Sse41.Multiply(vcoeff4, vprev4));
                    sum = Sse2.Add(sum, Sse41.Multiply(vcoeff5, vprev5));
                    sum = Sse2.Add(sum, Sse41.Multiply(vcoeff6, vprev6));
                    sum = Sse2.Add(sum, Sse41.Multiply(vcoeff7, vprev7));
                    sum = Sse2.Add(sum, Sse41.Multiply(vcoeff8, vprev8));
                    sum = Sse2.Add(sum, Sse41.Multiply(vcoeff9, vprev9));
                    sum = Sse2.Add(sum, Sse41.Multiply(vcoeff10, vprev10));
                    sum = Sse2.Add(sum, Sse2.ShiftRightLogical128BitLane(sum, 8));
                    sum = Sse2.ShiftRightLogical(sum, vshift);
                    var yy = Sse2.Add(sum.AsInt32(), res);
#if NET5_0_OR_GREATER
                    Sse2.StoreScalar((int*)Unsafe.AsPointer(ref Unsafe.Add(ref d, i)), yy);
#else
                    Unsafe.Add(ref d, i) = yy.GetElement(0);
#endif
                    i++;
                    yy = Sse2.ShiftLeftLogical128BitLane(yy, 8);
                    vprev10 = Ssse3.AlignRight(vprev10, vprev9, 8);
                    vprev9 = Ssse3.AlignRight(vprev9, vprev8, 8);
                    vprev8 = Ssse3.AlignRight(vprev8, vprev7, 8);
                    vprev7 = Ssse3.AlignRight(vprev7, vprev6, 8);
                    vprev6 = Ssse3.AlignRight(vprev6, vprev5, 8);
                    vprev5 = Ssse3.AlignRight(vprev5, vprev4, 8);
                    vprev4 = Ssse3.AlignRight(vprev4, vprev3, 8);
                    vprev3 = Ssse3.AlignRight(vprev3, vprev2, 8);
                    vprev2 = Ssse3.AlignRight(vprev2, vprev1, 8);
                    vprev1 = Ssse3.AlignRight(vprev1, vprev0, 8);
                    vprev0 = Ssse3.AlignRight(vprev0, yy, 8);
                }
            }
            
            [MethodImpl(OptimizationUtils.InlineAndOptimizeIfPossible)]
            internal static unsafe void RestoreSignalOrder22WideAvx2(int shiftsNeeded, ReadOnlySpan<int> residual, ReadOnlySpan<int> coeffs, Span<int> output)
            {
                const int Order = 22;
                if(coeffs.Length < Order) return;
                _ = coeffs[Order - 1];
                var vshift = Vector128.CreateScalar((long)shiftsNeeded);
                ref var c = ref MemoryMarshal.GetReference(coeffs);
                ref var o = ref MemoryMarshal.GetReference(output);
                ref var d = ref Unsafe.Add(ref o, Order);
                int dataLength = output.Length - Order;
                ref var r = ref MemoryMarshal.GetReference(residual);
                Vector128<long> sum;
                Vector256<long> sum256;
                var vcoeff0 = Avx2.ConvertToVector256Int64(Sse2.LoadVector128((int*)Unsafe.AsPointer(ref Unsafe.Add(ref c, 0))).AsUInt32()).AsInt32();
                var vcoeff1 = Avx2.ConvertToVector256Int64(Sse2.LoadVector128((int*)Unsafe.AsPointer(ref Unsafe.Add(ref c, 4))).AsUInt32()).AsInt32();
                var vcoeff2 = Avx2.ConvertToVector256Int64(Sse2.LoadVector128((int*)Unsafe.AsPointer(ref Unsafe.Add(ref c, 8))).AsUInt32()).AsInt32();
                var vcoeff3 = Avx2.ConvertToVector256Int64(Sse2.LoadVector128((int*)Unsafe.AsPointer(ref Unsafe.Add(ref c, 12))).AsUInt32()).AsInt32();
                var vcoeff4 = Avx2.ConvertToVector256Int64(Sse2.LoadVector128((int*)Unsafe.AsPointer(ref Unsafe.Add(ref c, 16))).AsUInt32()).AsInt32();
                var vcoeff5 = Avx2.ConvertToVector256Int64(Sse2.LoadScalarVector128((ulong*)Unsafe.AsPointer(ref Unsafe.Add(ref c, 20))).AsUInt32()).AsInt32();
                var vprev0 = Avx2.ConvertToVector256Int64(Sse2.Shuffle(Sse2.LoadScalarVector128((ulong*)Unsafe.AsPointer(ref Unsafe.Add(ref o, 19))).AsInt32(), 0b00_01_10_11).AsUInt32()).AsInt32();
                var vprev1 = Avx2.ConvertToVector256Int64(Sse2.Shuffle(Sse2.LoadScalarVector128((ulong*)Unsafe.AsPointer(ref Unsafe.Add(ref o, 15))).AsInt32(), 0b00_01_10_11).AsUInt32()).AsInt32();
                var vprev2 = Avx2.ConvertToVector256Int64(Sse2.Shuffle(Sse2.LoadScalarVector128((ulong*)Unsafe.AsPointer(ref Unsafe.Add(ref o, 11))).AsInt32(), 0b00_01_10_11).AsUInt32()).AsInt32();
                var vprev3 = Avx2.ConvertToVector256Int64(Sse2.Shuffle(Sse2.LoadScalarVector128((ulong*)Unsafe.AsPointer(ref Unsafe.Add(ref o, 7))).AsInt32(), 0b00_01_10_11).AsUInt32()).AsInt32();
                var vprev4 = Avx2.ConvertToVector256Int64(Sse2.Shuffle(Sse2.LoadScalarVector128((ulong*)Unsafe.AsPointer(ref Unsafe.Add(ref o, 3))).AsInt32(), 0b00_01_10_11).AsUInt32()).AsInt32();
                var vprev5 = Avx2.ConvertToVector256Int64(Sse2.Shuffle(Sse2.LoadScalarVector128((ulong*)Unsafe.AsPointer(ref o)).AsInt32(), 0b00_01_10_11).AsUInt32()).AsInt32();
                for(nint i = 0; i < dataLength;)
                {
                    var res = Vector128.CreateScalar(Unsafe.Add(ref r, i));
                    sum256 = Avx2.Multiply(vcoeff0, vprev0);
                    sum256 = Avx2.Add(sum256, Avx2.Multiply(vcoeff1, vprev1));
                    sum256 = Avx2.Add(sum256, Avx2.Multiply(vcoeff2, vprev2));
                    sum256 = Avx2.Add(sum256, Avx2.Multiply(vcoeff3, vprev3));
                    sum256 = Avx2.Add(sum256, Avx2.Multiply(vcoeff4, vprev4));
                    sum256 = Avx2.Add(sum256, Avx2.Multiply(vcoeff5, vprev5));
                    sum = Sse2.Add(sum256.GetLower(), sum256.GetUpper());
                    sum = Sse2.Add(sum, Sse2.ShiftRightLogical128BitLane(sum, 8));
                    sum = Sse2.ShiftRightLogical(sum, vshift);
                    var yy = Sse2.Add(sum.AsInt32(), res);
#if NET5_0_OR_GREATER
                    Sse2.StoreScalar((int*)Unsafe.AsPointer(ref Unsafe.Add(ref d, i)), yy);
#else
                    Unsafe.Add(ref d, i) = yy.GetElement(0);
#endif
                    i++;
                    var yu = yy.ToVector256();
                    yu = Avx2.Permute4x64(yu.AsUInt64(), 0b00_00_00_00).AsInt32();
                    vprev5 = Avx2.Permute4x64(vprev5.AsInt64(), 0b10_01_00_11).AsInt32();
                    vprev5 = Avx2.Blend(vprev5, vprev4, 0b0000_0001);
                    vprev4 = Avx2.Permute4x64(vprev4.AsInt64(), 0b10_01_00_11).AsInt32();
                    vprev4 = Avx2.Blend(vprev4, vprev3, 0b0000_0001);
                    vprev3 = Avx2.Permute4x64(vprev3.AsInt64(), 0b10_01_00_11).AsInt32();
                    vprev3 = Avx2.Blend(vprev3, vprev2, 0b0000_0001);
                    vprev2 = Avx2.Permute4x64(vprev2.AsInt64(), 0b10_01_00_11).AsInt32();
                    vprev2 = Avx2.Blend(vprev2, vprev1, 0b0000_0001);
                    vprev1 = Avx2.Permute4x64(vprev1.AsInt64(), 0b10_01_00_11).AsInt32();
                    vprev1 = Avx2.Blend(vprev1, vprev0, 0b0000_0001);
                    vprev0 = Avx2.Permute4x64(vprev0.AsInt64(), 0b10_01_00_11).AsInt32();
                    vprev0 = Avx2.Blend(vprev0, yu, 0b0000_0001);
                }
            }
#endregion Order22
#region Order23

            [MethodImpl(OptimizationUtils.InlineAndOptimizeIfPossible)]
            internal static bool RestoreSignalOrder23(int shiftsNeeded, ReadOnlySpan<int> residual, ReadOnlySpan<int> coeffs, Span<int> output)
            {
                if (Sse41.IsSupported)
                {
                    RestoreSignalOrder23Sse41(shiftsNeeded, residual, coeffs, output);
                    return true;
                }
                return false;
            }

            [MethodImpl(OptimizationUtils.InlineAndOptimizeIfPossible)]
            internal static unsafe void RestoreSignalOrder23Sse41(int shiftsNeeded, ReadOnlySpan<int> residual, ReadOnlySpan<int> coeffs, Span<int> output)
            {
                const int Order = 23;
                if (coeffs.Length < Order) return;
                _ = coeffs[Order - 1];
                var vshift = Vector128.CreateScalar(shiftsNeeded);
                ref var o = ref MemoryMarshal.GetReference(output);
                ref var d = ref Unsafe.Add(ref o, Order);
                ref var r = ref MemoryMarshal.GetReference(residual);
                ref var c = ref MemoryMarshal.GetReference(coeffs);
                Vector128<int> sum;
                var vzero = Vector128.Create(0);
                nint dataLength = output.Length - Order;
                var vcoeff0 = Sse2.LoadVector128((int*)Unsafe.AsPointer(ref Unsafe.Add(ref c, 0)));
                var vcoeff1 = Sse2.LoadVector128((int*)Unsafe.AsPointer(ref Unsafe.Add(ref c, 4)));
                var vcoeff2 = Sse2.LoadVector128((int*)Unsafe.AsPointer(ref Unsafe.Add(ref c, 8)));
                var vcoeff3 = Sse2.LoadVector128((int*)Unsafe.AsPointer(ref Unsafe.Add(ref c, 12)));
                var vcoeff4 = Sse2.LoadVector128((int*)Unsafe.AsPointer(ref Unsafe.Add(ref c, 16)));
                var vcoeff5 = Vector128.Create(Unsafe.Add(ref c, 20), Unsafe.Add(ref c, 21), Unsafe.Add(ref c, 22), 0);
                var vprev0 = Sse2.Shuffle(Sse2.LoadVector128((int*)Unsafe.AsPointer(ref Unsafe.Add(ref o, 19))).AsInt32(), 0b00_01_10_11);
                var vprev1 = Sse2.Shuffle(Sse2.LoadVector128((int*)Unsafe.AsPointer(ref Unsafe.Add(ref o, 15))).AsInt32(), 0b00_01_10_11);
                var vprev2 = Sse2.Shuffle(Sse2.LoadVector128((int*)Unsafe.AsPointer(ref Unsafe.Add(ref o, 11))).AsInt32(), 0b00_01_10_11);
                var vprev3 = Sse2.Shuffle(Sse2.LoadVector128((int*)Unsafe.AsPointer(ref Unsafe.Add(ref o, 7))).AsInt32(), 0b00_01_10_11);
                var vprev4 = Sse2.Shuffle(Sse2.LoadVector128((int*)Unsafe.AsPointer(ref Unsafe.Add(ref o, 3))).AsInt32(), 0b00_01_10_11);
                var vprev5 = Vector128.Create(Unsafe.Add(ref o, 2), Unsafe.Add(ref o, 1), Unsafe.Add(ref o, 0), 0);
                for (nint i = 0; i < dataLength; i++)
                {
                    var res = Vector128.CreateScalar(Unsafe.Add(ref r, i));
                    sum = Sse41.MultiplyLow(vcoeff0, vprev0);
                    sum = Sse2.Add(sum, Sse41.MultiplyLow(vcoeff1, vprev1));
                    sum = Sse2.Add(sum, Sse41.MultiplyLow(vcoeff2, vprev2));
                    sum = Sse2.Add(sum, Sse41.MultiplyLow(vcoeff3, vprev3));
                    sum = Sse2.Add(sum, Sse41.MultiplyLow(vcoeff4, vprev4));
                    sum = Sse2.Add(sum, Sse41.MultiplyLow(vcoeff5, vprev5));
                    var y = Ssse3.HorizontalAdd(sum, vzero);
                    y = Ssse3.HorizontalAdd(y, vzero);
                    y = Sse2.ShiftRightArithmetic(y, vshift);   //C# shift operator handling sucks so SSE2 is used instead(same latency, same throughput).
                    y = Sse2.Add(y, res);   //Avoids extract and insert
#if NET5_0_OR_GREATER
                    Sse2.StoreScalar((int*)Unsafe.AsPointer(ref Unsafe.Add(ref d, i)), y);
#else
                    Unsafe.Add(ref d, i) = y.GetElement(0);
#endif
                    y = Sse2.ShiftLeftLogical128BitLane(y, 12);
                    vprev5 = Ssse3.AlignRight(vprev5, vprev4, 12);
                    vprev4 = Ssse3.AlignRight(vprev4, vprev3, 12);
                    vprev3 = Ssse3.AlignRight(vprev3, vprev2, 12);
                    vprev2 = Ssse3.AlignRight(vprev2, vprev1, 12);
                    vprev1 = Ssse3.AlignRight(vprev1, vprev0, 12);
                    vprev0 = Ssse3.AlignRight(vprev0, y, 12);
                }
            }

            [MethodImpl(OptimizationUtils.InlineAndOptimizeIfPossible)]
            internal static bool RestoreSignalOrder23Wide(int shiftsNeeded, ReadOnlySpan<int> residual, ReadOnlySpan<int> coeffs, Span<int> output)
            {
                if (Sse41.IsSupported)
                {
                    RestoreSignalOrder23WideSse41(shiftsNeeded, residual, coeffs, output);
                    return true;
                }
                return false;
            }

            [MethodImpl(OptimizationUtils.InlineAndOptimizeIfPossible)]
            internal static unsafe void RestoreSignalOrder23WideSse41(int shiftsNeeded, ReadOnlySpan<int> residual, ReadOnlySpan<int> coeffs, Span<int> output)
            {
                const int Order = 23;
                if(coeffs.Length < Order) return;
                _ = coeffs[Order - 1];
                var vshift = Vector128.CreateScalar((long)shiftsNeeded);
                ref var c = ref MemoryMarshal.GetReference(coeffs);
                ref var o = ref MemoryMarshal.GetReference(output);
                ref var d = ref Unsafe.Add(ref o, Order);
                int dataLength = output.Length - Order;
                ref var r = ref MemoryMarshal.GetReference(residual);
                Vector128<long> sum;
                var vcoeff0 = Sse41.ConvertToVector128Int64(Sse2.LoadScalarVector128((ulong*)Unsafe.AsPointer(ref Unsafe.Add(ref c, 0))).AsUInt32()).AsInt32();
                var vcoeff1 = Sse41.ConvertToVector128Int64(Sse2.LoadScalarVector128((ulong*)Unsafe.AsPointer(ref Unsafe.Add(ref c, 2))).AsUInt32()).AsInt32();
                var vcoeff2 = Sse41.ConvertToVector128Int64(Sse2.LoadScalarVector128((ulong*)Unsafe.AsPointer(ref Unsafe.Add(ref c, 4))).AsUInt32()).AsInt32();
                var vcoeff3 = Sse41.ConvertToVector128Int64(Sse2.LoadScalarVector128((ulong*)Unsafe.AsPointer(ref Unsafe.Add(ref c, 6))).AsUInt32()).AsInt32();
                var vcoeff4 = Sse41.ConvertToVector128Int64(Sse2.LoadScalarVector128((ulong*)Unsafe.AsPointer(ref Unsafe.Add(ref c, 8))).AsUInt32()).AsInt32();
                var vcoeff5 = Sse41.ConvertToVector128Int64(Sse2.LoadScalarVector128((ulong*)Unsafe.AsPointer(ref Unsafe.Add(ref c, 10))).AsUInt32()).AsInt32();
                var vcoeff6 = Sse41.ConvertToVector128Int64(Sse2.LoadScalarVector128((ulong*)Unsafe.AsPointer(ref Unsafe.Add(ref c, 12))).AsUInt32()).AsInt32();
                var vcoeff7 = Sse41.ConvertToVector128Int64(Sse2.LoadScalarVector128((ulong*)Unsafe.AsPointer(ref Unsafe.Add(ref c, 14))).AsUInt32()).AsInt32();
                var vcoeff8 = Sse41.ConvertToVector128Int64(Sse2.LoadScalarVector128((ulong*)Unsafe.AsPointer(ref Unsafe.Add(ref c, 16))).AsUInt32()).AsInt32();
                var vcoeff9 = Sse41.ConvertToVector128Int64(Sse2.LoadScalarVector128((ulong*)Unsafe.AsPointer(ref Unsafe.Add(ref c, 18))).AsUInt32()).AsInt32();
                var vcoeff10 = Sse41.ConvertToVector128Int64(Sse2.LoadScalarVector128((ulong*)Unsafe.AsPointer(ref Unsafe.Add(ref c, 20))).AsUInt32()).AsInt32();
                var vcoeff11 = Sse41.ConvertToVector128Int64(Sse2.LoadScalarVector128((uint*)Unsafe.AsPointer(ref Unsafe.Add(ref c, 22))).AsUInt32()).AsInt32();
                var vprev0 = Sse2.Shuffle(Sse2.LoadScalarVector128((ulong*)Unsafe.AsPointer(ref Unsafe.Add(ref o, 21))).AsInt32(), 0b11_00_11_01);
                var vprev1 = Sse2.Shuffle(Sse2.LoadScalarVector128((ulong*)Unsafe.AsPointer(ref Unsafe.Add(ref o, 19))).AsInt32(), 0b11_00_11_01);
                var vprev2 = Sse2.Shuffle(Sse2.LoadScalarVector128((ulong*)Unsafe.AsPointer(ref Unsafe.Add(ref o, 17))).AsInt32(), 0b11_00_11_01);
                var vprev3 = Sse2.Shuffle(Sse2.LoadScalarVector128((ulong*)Unsafe.AsPointer(ref Unsafe.Add(ref o, 15))).AsInt32(), 0b11_00_11_01);
                var vprev4 = Sse2.Shuffle(Sse2.LoadScalarVector128((ulong*)Unsafe.AsPointer(ref Unsafe.Add(ref o, 13))).AsInt32(), 0b11_00_11_01);
                var vprev5 = Sse2.Shuffle(Sse2.LoadScalarVector128((ulong*)Unsafe.AsPointer(ref Unsafe.Add(ref o, 11))).AsInt32(), 0b11_00_11_01);
                var vprev6 = Sse2.Shuffle(Sse2.LoadScalarVector128((ulong*)Unsafe.AsPointer(ref Unsafe.Add(ref o, 9))).AsInt32(), 0b11_00_11_01);
                var vprev7 = Sse2.Shuffle(Sse2.LoadScalarVector128((ulong*)Unsafe.AsPointer(ref Unsafe.Add(ref o, 7))).AsInt32(), 0b11_00_11_01);
                var vprev8 = Sse2.Shuffle(Sse2.LoadScalarVector128((ulong*)Unsafe.AsPointer(ref Unsafe.Add(ref o, 5))).AsInt32(), 0b11_00_11_01);
                var vprev9 = Sse2.Shuffle(Sse2.LoadScalarVector128((ulong*)Unsafe.AsPointer(ref Unsafe.Add(ref o, 3))).AsInt32(), 0b11_00_11_01);
                var vprev10 = Sse2.Shuffle(Sse2.LoadScalarVector128((ulong*)Unsafe.AsPointer(ref Unsafe.Add(ref o, 1))).AsInt32(), 0b11_00_11_01);
                var vprev11 = Sse2.Shuffle(Sse2.LoadScalarVector128((ulong*)Unsafe.AsPointer(ref Unsafe.Add(ref o, 0))).AsInt32(), 0b11_00_11_00);
                for(nint i = 0; i < dataLength;)
                {
                    var res = Vector128.CreateScalar(Unsafe.Add(ref r, i));
                    sum = Sse41.Multiply(vcoeff0, vprev0);
                    sum = Sse2.Add(sum, Sse41.Multiply(vcoeff1, vprev1));
                    sum = Sse2.Add(sum, Sse41.Multiply(vcoeff2, vprev2));
                    sum = Sse2.Add(sum, Sse41.Multiply(vcoeff3, vprev3));
                    sum = Sse2.Add(sum, Sse41.Multiply(vcoeff4, vprev4));
                    sum = Sse2.Add(sum, Sse41.Multiply(vcoeff5, vprev5));
                    sum = Sse2.Add(sum, Sse41.Multiply(vcoeff6, vprev6));
                    sum = Sse2.Add(sum, Sse41.Multiply(vcoeff7, vprev7));
                    sum = Sse2.Add(sum, Sse41.Multiply(vcoeff8, vprev8));
                    sum = Sse2.Add(sum, Sse41.Multiply(vcoeff9, vprev9));
                    sum = Sse2.Add(sum, Sse41.Multiply(vcoeff10, vprev10));
                    sum = Sse2.Add(sum, Sse41.Multiply(vcoeff11, vprev11));
                    sum = Sse2.Add(sum, Sse2.ShiftRightLogical128BitLane(sum, 8));
                    sum = Sse2.ShiftRightLogical(sum, vshift);
                    var yy = Sse2.Add(sum.AsInt32(), res);
#if NET5_0_OR_GREATER
                    Sse2.StoreScalar((int*)Unsafe.AsPointer(ref Unsafe.Add(ref d, i)), yy);
#else
                    Unsafe.Add(ref d, i) = yy.GetElement(0);
#endif
                    i++;
                    yy = Sse2.ShiftLeftLogical128BitLane(yy, 8);
                    vprev11 = Ssse3.AlignRight(vprev11, vprev10, 8);
                    vprev10 = Ssse3.AlignRight(vprev10, vprev9, 8);
                    vprev9 = Ssse3.AlignRight(vprev9, vprev8, 8);
                    vprev8 = Ssse3.AlignRight(vprev8, vprev7, 8);
                    vprev7 = Ssse3.AlignRight(vprev7, vprev6, 8);
                    vprev6 = Ssse3.AlignRight(vprev6, vprev5, 8);
                    vprev5 = Ssse3.AlignRight(vprev5, vprev4, 8);
                    vprev4 = Ssse3.AlignRight(vprev4, vprev3, 8);
                    vprev3 = Ssse3.AlignRight(vprev3, vprev2, 8);
                    vprev2 = Ssse3.AlignRight(vprev2, vprev1, 8);
                    vprev1 = Ssse3.AlignRight(vprev1, vprev0, 8);
                    vprev0 = Ssse3.AlignRight(vprev0, yy, 8);
                }
            }
            
            [MethodImpl(OptimizationUtils.InlineAndOptimizeIfPossible)]
            internal static unsafe void RestoreSignalOrder23WideAvx2(int shiftsNeeded, ReadOnlySpan<int> residual, ReadOnlySpan<int> coeffs, Span<int> output)
            {
                const int Order = 23;
                if(coeffs.Length < Order) return;
                _ = coeffs[Order - 1];
                var vshift = Vector128.CreateScalar((long)shiftsNeeded);
                var mask = Vector128.Create(~0, ~0, ~0, 0);
                ref var c = ref MemoryMarshal.GetReference(coeffs);
                ref var o = ref MemoryMarshal.GetReference(output);
                ref var d = ref Unsafe.Add(ref o, Order);
                int dataLength = output.Length - Order;
                ref var r = ref MemoryMarshal.GetReference(residual);
                Vector128<long> sum;
                Vector256<long> sum256;
                var vcoeff0 = Avx2.ConvertToVector256Int64(Sse2.LoadVector128((int*)Unsafe.AsPointer(ref Unsafe.Add(ref c, 0))).AsUInt32()).AsInt32();
                var vcoeff1 = Avx2.ConvertToVector256Int64(Sse2.LoadVector128((int*)Unsafe.AsPointer(ref Unsafe.Add(ref c, 4))).AsUInt32()).AsInt32();
                var vcoeff2 = Avx2.ConvertToVector256Int64(Sse2.LoadVector128((int*)Unsafe.AsPointer(ref Unsafe.Add(ref c, 8))).AsUInt32()).AsInt32();
                var vcoeff3 = Avx2.ConvertToVector256Int64(Sse2.LoadVector128((int*)Unsafe.AsPointer(ref Unsafe.Add(ref c, 12))).AsUInt32()).AsInt32();
                var vcoeff4 = Avx2.ConvertToVector256Int64(Sse2.LoadVector128((int*)Unsafe.AsPointer(ref Unsafe.Add(ref c, 16))).AsUInt32()).AsInt32();
                var vcoeff5 = Avx2.ConvertToVector256Int64(Avx2.MaskLoad((int*)Unsafe.AsPointer(ref Unsafe.Add(ref c, 20)), mask)).AsInt32();
                var vprev0 = Avx2.ConvertToVector256Int64(Sse2.Shuffle(Sse2.LoadScalarVector128((ulong*)Unsafe.AsPointer(ref Unsafe.Add(ref o, 20))).AsInt32(), 0b00_01_10_11).AsUInt32()).AsInt32();
                var vprev1 = Avx2.ConvertToVector256Int64(Sse2.Shuffle(Sse2.LoadScalarVector128((ulong*)Unsafe.AsPointer(ref Unsafe.Add(ref o, 16))).AsInt32(), 0b00_01_10_11).AsUInt32()).AsInt32();
                var vprev2 = Avx2.ConvertToVector256Int64(Sse2.Shuffle(Sse2.LoadScalarVector128((ulong*)Unsafe.AsPointer(ref Unsafe.Add(ref o, 12))).AsInt32(), 0b00_01_10_11).AsUInt32()).AsInt32();
                var vprev3 = Avx2.ConvertToVector256Int64(Sse2.Shuffle(Sse2.LoadScalarVector128((ulong*)Unsafe.AsPointer(ref Unsafe.Add(ref o, 8))).AsInt32(), 0b00_01_10_11).AsUInt32()).AsInt32();
                var vprev4 = Avx2.ConvertToVector256Int64(Sse2.Shuffle(Sse2.LoadScalarVector128((ulong*)Unsafe.AsPointer(ref Unsafe.Add(ref o, 4))).AsInt32(), 0b00_01_10_11).AsUInt32()).AsInt32();
                var vprev5 = Avx2.ConvertToVector256Int64(Sse2.Shuffle(Avx2.MaskLoad((int*)Unsafe.AsPointer(ref o), mask).AsInt32(), 0b00_01_10_11)).AsInt32();
                for(nint i = 0; i < dataLength;)
                {
                    var res = Vector128.CreateScalar(Unsafe.Add(ref r, i));
                    sum256 = Avx2.Multiply(vcoeff0, vprev0);
                    sum256 = Avx2.Add(sum256, Avx2.Multiply(vcoeff1, vprev1));
                    sum256 = Avx2.Add(sum256, Avx2.Multiply(vcoeff2, vprev2));
                    sum256 = Avx2.Add(sum256, Avx2.Multiply(vcoeff3, vprev3));
                    sum256 = Avx2.Add(sum256, Avx2.Multiply(vcoeff4, vprev4));
                    sum256 = Avx2.Add(sum256, Avx2.Multiply(vcoeff5, vprev5));
                    sum = Sse2.Add(sum256.GetLower(), sum256.GetUpper());
                    sum = Sse2.Add(sum, Sse2.ShiftRightLogical128BitLane(sum, 8));
                    sum = Sse2.ShiftRightLogical(sum, vshift);
                    var yy = Sse2.Add(sum.AsInt32(), res);
#if NET5_0_OR_GREATER
                    Sse2.StoreScalar((int*)Unsafe.AsPointer(ref Unsafe.Add(ref d, i)), yy);
#else
                    Unsafe.Add(ref d, i) = yy.GetElement(0);
#endif
                    i++;
                    var yu = yy.ToVector256();
                    yu = Avx2.Permute4x64(yu.AsUInt64(), 0b00_00_00_00).AsInt32();
                    vprev5 = Avx2.Permute4x64(vprev5.AsInt64(), 0b10_01_00_11).AsInt32();
                    vprev5 = Avx2.Blend(vprev5, vprev4, 0b0000_0001);
                    vprev4 = Avx2.Permute4x64(vprev4.AsInt64(), 0b10_01_00_11).AsInt32();
                    vprev4 = Avx2.Blend(vprev4, vprev3, 0b0000_0001);
                    vprev3 = Avx2.Permute4x64(vprev3.AsInt64(), 0b10_01_00_11).AsInt32();
                    vprev3 = Avx2.Blend(vprev3, vprev2, 0b0000_0001);
                    vprev2 = Avx2.Permute4x64(vprev2.AsInt64(), 0b10_01_00_11).AsInt32();
                    vprev2 = Avx2.Blend(vprev2, vprev1, 0b0000_0001);
                    vprev1 = Avx2.Permute4x64(vprev1.AsInt64(), 0b10_01_00_11).AsInt32();
                    vprev1 = Avx2.Blend(vprev1, vprev0, 0b0000_0001);
                    vprev0 = Avx2.Permute4x64(vprev0.AsInt64(), 0b10_01_00_11).AsInt32();
                    vprev0 = Avx2.Blend(vprev0, yu, 0b0000_0001);
                }
            }
#endregion Order23
#region Order24

            [MethodImpl(OptimizationUtils.InlineAndOptimizeIfPossible)]
            internal static bool RestoreSignalOrder24(int shiftsNeeded, ReadOnlySpan<int> residual, ReadOnlySpan<int> coeffs, Span<int> output)
            {
                if (Sse41.IsSupported)
                {
                    RestoreSignalOrder24Sse41(shiftsNeeded, residual, coeffs, output);
                    return true;
                }
                return false;
            }

            [MethodImpl(OptimizationUtils.InlineAndOptimizeIfPossible)]
            internal static unsafe void RestoreSignalOrder24Sse41(int shiftsNeeded, ReadOnlySpan<int> residual, ReadOnlySpan<int> coeffs, Span<int> output)
            {
                const int Order = 24;
                if (coeffs.Length < Order) return;
                _ = coeffs[Order - 1];
                var vshift = Vector128.CreateScalar(shiftsNeeded);
                ref var o = ref MemoryMarshal.GetReference(output);
                ref var d = ref Unsafe.Add(ref o, Order);
                ref var r = ref MemoryMarshal.GetReference(residual);
                ref var c = ref MemoryMarshal.GetReference(coeffs);
                Vector128<int> sum;
                var vzero = Vector128.Create(0);
                nint dataLength = output.Length - Order;
                var vcoeff0 = Sse2.LoadVector128((int*)Unsafe.AsPointer(ref Unsafe.Add(ref c, 0)));
                var vcoeff1 = Sse2.LoadVector128((int*)Unsafe.AsPointer(ref Unsafe.Add(ref c, 4)));
                var vcoeff2 = Sse2.LoadVector128((int*)Unsafe.AsPointer(ref Unsafe.Add(ref c, 8)));
                var vcoeff3 = Sse2.LoadVector128((int*)Unsafe.AsPointer(ref Unsafe.Add(ref c, 12)));
                var vcoeff4 = Sse2.LoadVector128((int*)Unsafe.AsPointer(ref Unsafe.Add(ref c, 16)));
                var vcoeff5 = Sse2.LoadVector128((int*)Unsafe.AsPointer(ref Unsafe.Add(ref c, 20)));
                var vprev0 = Sse2.Shuffle(Sse2.LoadVector128((int*)Unsafe.AsPointer(ref Unsafe.Add(ref o, 20))).AsInt32(), 0b00_01_10_11);
                var vprev1 = Sse2.Shuffle(Sse2.LoadVector128((int*)Unsafe.AsPointer(ref Unsafe.Add(ref o, 16))).AsInt32(), 0b00_01_10_11);
                var vprev2 = Sse2.Shuffle(Sse2.LoadVector128((int*)Unsafe.AsPointer(ref Unsafe.Add(ref o, 12))).AsInt32(), 0b00_01_10_11);
                var vprev3 = Sse2.Shuffle(Sse2.LoadVector128((int*)Unsafe.AsPointer(ref Unsafe.Add(ref o, 8))).AsInt32(), 0b00_01_10_11);
                var vprev4 = Sse2.Shuffle(Sse2.LoadVector128((int*)Unsafe.AsPointer(ref Unsafe.Add(ref o, 4))).AsInt32(), 0b00_01_10_11);
                var vprev5 = Sse2.Shuffle(Sse2.LoadVector128((int*)Unsafe.AsPointer(ref Unsafe.Add(ref o, 0))).AsInt32(), 0b00_01_10_11);
                var vprev6 = Vector128.Create(0, 0, 0, 0);
                for (nint i = 0; i < dataLength; i++)
                {
                    var res = Vector128.CreateScalar(Unsafe.Add(ref r, i));
                    sum = Sse41.MultiplyLow(vcoeff0, vprev0);
                    sum = Sse2.Add(sum, Sse41.MultiplyLow(vcoeff1, vprev1));
                    sum = Sse2.Add(sum, Sse41.MultiplyLow(vcoeff2, vprev2));
                    sum = Sse2.Add(sum, Sse41.MultiplyLow(vcoeff3, vprev3));
                    sum = Sse2.Add(sum, Sse41.MultiplyLow(vcoeff4, vprev4));
                    sum = Sse2.Add(sum, Sse41.MultiplyLow(vcoeff5, vprev5));
                    var y = Ssse3.HorizontalAdd(sum, vzero);
                    y = Ssse3.HorizontalAdd(y, vzero);
                    y = Sse2.ShiftRightArithmetic(y, vshift);   //C# shift operator handling sucks so SSE2 is used instead(same latency, same throughput).
                    y = Sse2.Add(y, res);   //Avoids extract and insert
#if NET5_0_OR_GREATER
                    Sse2.StoreScalar((int*)Unsafe.AsPointer(ref Unsafe.Add(ref d, i)), y);
#else
                    Unsafe.Add(ref d, i) = y.GetElement(0);
#endif
                    y = Sse2.ShiftLeftLogical128BitLane(y, 12);
                    vprev5 = Ssse3.AlignRight(vprev5, vprev4, 12);
                    vprev4 = Ssse3.AlignRight(vprev4, vprev3, 12);
                    vprev3 = Ssse3.AlignRight(vprev3, vprev2, 12);
                    vprev2 = Ssse3.AlignRight(vprev2, vprev1, 12);
                    vprev1 = Ssse3.AlignRight(vprev1, vprev0, 12);
                    vprev0 = Ssse3.AlignRight(vprev0, y, 12);
                }
            }

            [MethodImpl(OptimizationUtils.InlineAndOptimizeIfPossible)]
            internal static bool RestoreSignalOrder24Wide(int shiftsNeeded, ReadOnlySpan<int> residual, ReadOnlySpan<int> coeffs, Span<int> output)
            {
                if (Sse41.IsSupported)
                {
                    RestoreSignalOrder24WideSse41(shiftsNeeded, residual, coeffs, output);
                    return true;
                }
                return false;
            }

            [MethodImpl(OptimizationUtils.InlineAndOptimizeIfPossible)]
            internal static unsafe void RestoreSignalOrder24WideSse41(int shiftsNeeded, ReadOnlySpan<int> residual, ReadOnlySpan<int> coeffs, Span<int> output)
            {
                const int Order = 24;
                if(coeffs.Length < Order) return;
                _ = coeffs[Order - 1];
                var vshift = Vector128.CreateScalar((long)shiftsNeeded);
                ref var c = ref MemoryMarshal.GetReference(coeffs);
                ref var o = ref MemoryMarshal.GetReference(output);
                ref var d = ref Unsafe.Add(ref o, Order);
                int dataLength = output.Length - Order;
                ref var r = ref MemoryMarshal.GetReference(residual);
                Vector128<long> sum;
                var vcoeff0 = Sse41.ConvertToVector128Int64(Sse2.LoadScalarVector128((ulong*)Unsafe.AsPointer(ref Unsafe.Add(ref c, 0))).AsUInt32()).AsInt32();
                var vcoeff1 = Sse41.ConvertToVector128Int64(Sse2.LoadScalarVector128((ulong*)Unsafe.AsPointer(ref Unsafe.Add(ref c, 2))).AsUInt32()).AsInt32();
                var vcoeff2 = Sse41.ConvertToVector128Int64(Sse2.LoadScalarVector128((ulong*)Unsafe.AsPointer(ref Unsafe.Add(ref c, 4))).AsUInt32()).AsInt32();
                var vcoeff3 = Sse41.ConvertToVector128Int64(Sse2.LoadScalarVector128((ulong*)Unsafe.AsPointer(ref Unsafe.Add(ref c, 6))).AsUInt32()).AsInt32();
                var vcoeff4 = Sse41.ConvertToVector128Int64(Sse2.LoadScalarVector128((ulong*)Unsafe.AsPointer(ref Unsafe.Add(ref c, 8))).AsUInt32()).AsInt32();
                var vcoeff5 = Sse41.ConvertToVector128Int64(Sse2.LoadScalarVector128((ulong*)Unsafe.AsPointer(ref Unsafe.Add(ref c, 10))).AsUInt32()).AsInt32();
                var vcoeff6 = Sse41.ConvertToVector128Int64(Sse2.LoadScalarVector128((ulong*)Unsafe.AsPointer(ref Unsafe.Add(ref c, 12))).AsUInt32()).AsInt32();
                var vcoeff7 = Sse41.ConvertToVector128Int64(Sse2.LoadScalarVector128((ulong*)Unsafe.AsPointer(ref Unsafe.Add(ref c, 14))).AsUInt32()).AsInt32();
                var vcoeff8 = Sse41.ConvertToVector128Int64(Sse2.LoadScalarVector128((ulong*)Unsafe.AsPointer(ref Unsafe.Add(ref c, 16))).AsUInt32()).AsInt32();
                var vcoeff9 = Sse41.ConvertToVector128Int64(Sse2.LoadScalarVector128((ulong*)Unsafe.AsPointer(ref Unsafe.Add(ref c, 18))).AsUInt32()).AsInt32();
                var vcoeff10 = Sse41.ConvertToVector128Int64(Sse2.LoadScalarVector128((ulong*)Unsafe.AsPointer(ref Unsafe.Add(ref c, 20))).AsUInt32()).AsInt32();
                var vcoeff11 = Sse41.ConvertToVector128Int64(Sse2.LoadScalarVector128((ulong*)Unsafe.AsPointer(ref Unsafe.Add(ref c, 22))).AsUInt32()).AsInt32();
                var vprev0 = Sse2.Shuffle(Sse2.LoadScalarVector128((ulong*)Unsafe.AsPointer(ref Unsafe.Add(ref o, 22))).AsInt32(), 0b11_00_11_01);
                var vprev1 = Sse2.Shuffle(Sse2.LoadScalarVector128((ulong*)Unsafe.AsPointer(ref Unsafe.Add(ref o, 20))).AsInt32(), 0b11_00_11_01);
                var vprev2 = Sse2.Shuffle(Sse2.LoadScalarVector128((ulong*)Unsafe.AsPointer(ref Unsafe.Add(ref o, 18))).AsInt32(), 0b11_00_11_01);
                var vprev3 = Sse2.Shuffle(Sse2.LoadScalarVector128((ulong*)Unsafe.AsPointer(ref Unsafe.Add(ref o, 16))).AsInt32(), 0b11_00_11_01);
                var vprev4 = Sse2.Shuffle(Sse2.LoadScalarVector128((ulong*)Unsafe.AsPointer(ref Unsafe.Add(ref o, 14))).AsInt32(), 0b11_00_11_01);
                var vprev5 = Sse2.Shuffle(Sse2.LoadScalarVector128((ulong*)Unsafe.AsPointer(ref Unsafe.Add(ref o, 12))).AsInt32(), 0b11_00_11_01);
                var vprev6 = Sse2.Shuffle(Sse2.LoadScalarVector128((ulong*)Unsafe.AsPointer(ref Unsafe.Add(ref o, 10))).AsInt32(), 0b11_00_11_01);
                var vprev7 = Sse2.Shuffle(Sse2.LoadScalarVector128((ulong*)Unsafe.AsPointer(ref Unsafe.Add(ref o, 8))).AsInt32(), 0b11_00_11_01);
                var vprev8 = Sse2.Shuffle(Sse2.LoadScalarVector128((ulong*)Unsafe.AsPointer(ref Unsafe.Add(ref o, 6))).AsInt32(), 0b11_00_11_01);
                var vprev9 = Sse2.Shuffle(Sse2.LoadScalarVector128((ulong*)Unsafe.AsPointer(ref Unsafe.Add(ref o, 4))).AsInt32(), 0b11_00_11_01);
                var vprev10 = Sse2.Shuffle(Sse2.LoadScalarVector128((ulong*)Unsafe.AsPointer(ref Unsafe.Add(ref o, 2))).AsInt32(), 0b11_00_11_01);
                var vprev11 = Sse2.Shuffle(Sse2.LoadScalarVector128((ulong*)Unsafe.AsPointer(ref Unsafe.Add(ref o, 0))).AsInt32(), 0b11_00_11_01);
                for(nint i = 0; i < dataLength;)
                {
                    var res = Vector128.CreateScalar(Unsafe.Add(ref r, i));
                    sum = Sse41.Multiply(vcoeff0, vprev0);
                    sum = Sse2.Add(sum, Sse41.Multiply(vcoeff1, vprev1));
                    sum = Sse2.Add(sum, Sse41.Multiply(vcoeff2, vprev2));
                    sum = Sse2.Add(sum, Sse41.Multiply(vcoeff3, vprev3));
                    sum = Sse2.Add(sum, Sse41.Multiply(vcoeff4, vprev4));
                    sum = Sse2.Add(sum, Sse41.Multiply(vcoeff5, vprev5));
                    sum = Sse2.Add(sum, Sse41.Multiply(vcoeff6, vprev6));
                    sum = Sse2.Add(sum, Sse41.Multiply(vcoeff7, vprev7));
                    sum = Sse2.Add(sum, Sse41.Multiply(vcoeff8, vprev8));
                    sum = Sse2.Add(sum, Sse41.Multiply(vcoeff9, vprev9));
                    sum = Sse2.Add(sum, Sse41.Multiply(vcoeff10, vprev10));
                    sum = Sse2.Add(sum, Sse41.Multiply(vcoeff11, vprev11));
                    sum = Sse2.Add(sum, Sse2.ShiftRightLogical128BitLane(sum, 8));
                    sum = Sse2.ShiftRightLogical(sum, vshift);
                    var yy = Sse2.Add(sum.AsInt32(), res);
#if NET5_0_OR_GREATER
                    Sse2.StoreScalar((int*)Unsafe.AsPointer(ref Unsafe.Add(ref d, i)), yy);
#else
                    Unsafe.Add(ref d, i) = yy.GetElement(0);
#endif
                    i++;
                    yy = Sse2.ShiftLeftLogical128BitLane(yy, 8);
                    vprev11 = Ssse3.AlignRight(vprev11, vprev10, 8);
                    vprev10 = Ssse3.AlignRight(vprev10, vprev9, 8);
                    vprev9 = Ssse3.AlignRight(vprev9, vprev8, 8);
                    vprev8 = Ssse3.AlignRight(vprev8, vprev7, 8);
                    vprev7 = Ssse3.AlignRight(vprev7, vprev6, 8);
                    vprev6 = Ssse3.AlignRight(vprev6, vprev5, 8);
                    vprev5 = Ssse3.AlignRight(vprev5, vprev4, 8);
                    vprev4 = Ssse3.AlignRight(vprev4, vprev3, 8);
                    vprev3 = Ssse3.AlignRight(vprev3, vprev2, 8);
                    vprev2 = Ssse3.AlignRight(vprev2, vprev1, 8);
                    vprev1 = Ssse3.AlignRight(vprev1, vprev0, 8);
                    vprev0 = Ssse3.AlignRight(vprev0, yy, 8);
                }
            }
            
            [MethodImpl(OptimizationUtils.InlineAndOptimizeIfPossible)]
            internal static unsafe void RestoreSignalOrder24WideAvx2(int shiftsNeeded, ReadOnlySpan<int> residual, ReadOnlySpan<int> coeffs, Span<int> output)
            {
                const int Order = 24;
                if(coeffs.Length < Order) return;
                _ = coeffs[Order - 1];
                var vshift = Vector128.CreateScalar((long)shiftsNeeded);
                ref var c = ref MemoryMarshal.GetReference(coeffs);
                ref var o = ref MemoryMarshal.GetReference(output);
                ref var d = ref Unsafe.Add(ref o, Order);
                int dataLength = output.Length - Order;
                ref var r = ref MemoryMarshal.GetReference(residual);
                Vector128<long> sum;
                Vector256<long> sum256;
                var vcoeff0 = Avx2.ConvertToVector256Int64(Sse2.LoadVector128((int*)Unsafe.AsPointer(ref Unsafe.Add(ref c, 0))).AsUInt32()).AsInt32();
                var vcoeff1 = Avx2.ConvertToVector256Int64(Sse2.LoadVector128((int*)Unsafe.AsPointer(ref Unsafe.Add(ref c, 4))).AsUInt32()).AsInt32();
                var vcoeff2 = Avx2.ConvertToVector256Int64(Sse2.LoadVector128((int*)Unsafe.AsPointer(ref Unsafe.Add(ref c, 8))).AsUInt32()).AsInt32();
                var vcoeff3 = Avx2.ConvertToVector256Int64(Sse2.LoadVector128((int*)Unsafe.AsPointer(ref Unsafe.Add(ref c, 12))).AsUInt32()).AsInt32();
                var vcoeff4 = Avx2.ConvertToVector256Int64(Sse2.LoadVector128((int*)Unsafe.AsPointer(ref Unsafe.Add(ref c, 16))).AsUInt32()).AsInt32();
                var vcoeff5 = Avx2.ConvertToVector256Int64(Sse2.LoadVector128((int*)Unsafe.AsPointer(ref Unsafe.Add(ref c, 20))).AsUInt32()).AsInt32();
                var vprev0 = Avx2.ConvertToVector256Int64(Sse2.Shuffle(Sse2.LoadScalarVector128((ulong*)Unsafe.AsPointer(ref Unsafe.Add(ref o, 20))).AsInt32(), 0b00_01_10_11).AsUInt32()).AsInt32();
                var vprev1 = Avx2.ConvertToVector256Int64(Sse2.Shuffle(Sse2.LoadScalarVector128((ulong*)Unsafe.AsPointer(ref Unsafe.Add(ref o, 16))).AsInt32(), 0b00_01_10_11).AsUInt32()).AsInt32();
                var vprev2 = Avx2.ConvertToVector256Int64(Sse2.Shuffle(Sse2.LoadScalarVector128((ulong*)Unsafe.AsPointer(ref Unsafe.Add(ref o, 12))).AsInt32(), 0b00_01_10_11).AsUInt32()).AsInt32();
                var vprev3 = Avx2.ConvertToVector256Int64(Sse2.Shuffle(Sse2.LoadScalarVector128((ulong*)Unsafe.AsPointer(ref Unsafe.Add(ref o, 8))).AsInt32(), 0b00_01_10_11).AsUInt32()).AsInt32();
                var vprev4 = Avx2.ConvertToVector256Int64(Sse2.Shuffle(Sse2.LoadScalarVector128((ulong*)Unsafe.AsPointer(ref Unsafe.Add(ref o, 4))).AsInt32(), 0b00_01_10_11).AsUInt32()).AsInt32();
                var vprev5 = Avx2.ConvertToVector256Int64(Sse2.Shuffle(Sse2.LoadScalarVector128((ulong*)Unsafe.AsPointer(ref Unsafe.Add(ref o, 0))).AsInt32(), 0b00_01_10_11).AsUInt32()).AsInt32();
                for(nint i = 0; i < dataLength;)
                {
                    var res = Vector128.CreateScalar(Unsafe.Add(ref r, i));
                    sum256 = Avx2.Multiply(vcoeff0, vprev0);
                    sum256 = Avx2.Add(sum256, Avx2.Multiply(vcoeff1, vprev1));
                    sum256 = Avx2.Add(sum256, Avx2.Multiply(vcoeff2, vprev2));
                    sum256 = Avx2.Add(sum256, Avx2.Multiply(vcoeff3, vprev3));
                    sum256 = Avx2.Add(sum256, Avx2.Multiply(vcoeff4, vprev4));
                    sum256 = Avx2.Add(sum256, Avx2.Multiply(vcoeff5, vprev5));
                    sum = Sse2.Add(sum256.GetLower(), sum256.GetUpper());
                    sum = Sse2.Add(sum, Sse2.ShiftRightLogical128BitLane(sum, 8));
                    sum = Sse2.ShiftRightLogical(sum, vshift);
                    var yy = Sse2.Add(sum.AsInt32(), res);
#if NET5_0_OR_GREATER
                    Sse2.StoreScalar((int*)Unsafe.AsPointer(ref Unsafe.Add(ref d, i)), yy);
#else
                    Unsafe.Add(ref d, i) = yy.GetElement(0);
#endif
                    i++;
                    var yu = yy.ToVector256();
                    yu = Avx2.Permute4x64(yu.AsUInt64(), 0b00_00_00_00).AsInt32();
                    vprev5 = Avx2.Permute4x64(vprev5.AsInt64(), 0b10_01_00_11).AsInt32();
                    vprev5 = Avx2.Blend(vprev5, vprev4, 0b0000_0001);
                    vprev4 = Avx2.Permute4x64(vprev4.AsInt64(), 0b10_01_00_11).AsInt32();
                    vprev4 = Avx2.Blend(vprev4, vprev3, 0b0000_0001);
                    vprev3 = Avx2.Permute4x64(vprev3.AsInt64(), 0b10_01_00_11).AsInt32();
                    vprev3 = Avx2.Blend(vprev3, vprev2, 0b0000_0001);
                    vprev2 = Avx2.Permute4x64(vprev2.AsInt64(), 0b10_01_00_11).AsInt32();
                    vprev2 = Avx2.Blend(vprev2, vprev1, 0b0000_0001);
                    vprev1 = Avx2.Permute4x64(vprev1.AsInt64(), 0b10_01_00_11).AsInt32();
                    vprev1 = Avx2.Blend(vprev1, vprev0, 0b0000_0001);
                    vprev0 = Avx2.Permute4x64(vprev0.AsInt64(), 0b10_01_00_11).AsInt32();
                    vprev0 = Avx2.Blend(vprev0, yu, 0b0000_0001);
                }
            }
#endregion Order24
#region Order25

            [MethodImpl(OptimizationUtils.InlineAndOptimizeIfPossible)]
            internal static bool RestoreSignalOrder25(int shiftsNeeded, ReadOnlySpan<int> residual, ReadOnlySpan<int> coeffs, Span<int> output)
            {
                if (Sse41.IsSupported)
                {
                    RestoreSignalOrder25Sse41(shiftsNeeded, residual, coeffs, output);
                    return true;
                }
                return false;
            }

            [MethodImpl(OptimizationUtils.InlineAndOptimizeIfPossible)]
            internal static unsafe void RestoreSignalOrder25Sse41(int shiftsNeeded, ReadOnlySpan<int> residual, ReadOnlySpan<int> coeffs, Span<int> output)
            {
                const int Order = 25;
                if (coeffs.Length < Order) return;
                _ = coeffs[Order - 1];
                var vshift = Vector128.CreateScalar(shiftsNeeded);
                ref var o = ref MemoryMarshal.GetReference(output);
                ref var d = ref Unsafe.Add(ref o, Order);
                ref var r = ref MemoryMarshal.GetReference(residual);
                ref var c = ref MemoryMarshal.GetReference(coeffs);
                Vector128<int> sum;
                var vzero = Vector128.Create(0);
                nint dataLength = output.Length - Order;
                var vcoeff0 = Sse2.LoadVector128((int*)Unsafe.AsPointer(ref Unsafe.Add(ref c, 0)));
                var vcoeff1 = Sse2.LoadVector128((int*)Unsafe.AsPointer(ref Unsafe.Add(ref c, 4)));
                var vcoeff2 = Sse2.LoadVector128((int*)Unsafe.AsPointer(ref Unsafe.Add(ref c, 8)));
                var vcoeff3 = Sse2.LoadVector128((int*)Unsafe.AsPointer(ref Unsafe.Add(ref c, 12)));
                var vcoeff4 = Sse2.LoadVector128((int*)Unsafe.AsPointer(ref Unsafe.Add(ref c, 16)));
                var vcoeff5 = Sse2.LoadVector128((int*)Unsafe.AsPointer(ref Unsafe.Add(ref c, 20)));
                var vcoeff6 = Vector128.Create(Unsafe.Add(ref c, 24), 0, 0, 0);
                var vprev0 = Sse2.Shuffle(Sse2.LoadVector128((int*)Unsafe.AsPointer(ref Unsafe.Add(ref o, 21))).AsInt32(), 0b00_01_10_11);
                var vprev1 = Sse2.Shuffle(Sse2.LoadVector128((int*)Unsafe.AsPointer(ref Unsafe.Add(ref o, 17))).AsInt32(), 0b00_01_10_11);
                var vprev2 = Sse2.Shuffle(Sse2.LoadVector128((int*)Unsafe.AsPointer(ref Unsafe.Add(ref o, 13))).AsInt32(), 0b00_01_10_11);
                var vprev3 = Sse2.Shuffle(Sse2.LoadVector128((int*)Unsafe.AsPointer(ref Unsafe.Add(ref o, 9))).AsInt32(), 0b00_01_10_11);
                var vprev4 = Sse2.Shuffle(Sse2.LoadVector128((int*)Unsafe.AsPointer(ref Unsafe.Add(ref o, 5))).AsInt32(), 0b00_01_10_11);
                var vprev5 = Sse2.Shuffle(Sse2.LoadVector128((int*)Unsafe.AsPointer(ref Unsafe.Add(ref o, 1))).AsInt32(), 0b00_01_10_11);
                var vprev6 = Vector128.Create(Unsafe.Add(ref o, 0), 0, 0, 0);
                for (nint i = 0; i < dataLength; i++)
                {
                    var res = Vector128.CreateScalar(Unsafe.Add(ref r, i));
                    sum = Sse41.MultiplyLow(vcoeff0, vprev0);
                    sum = Sse2.Add(sum, Sse41.MultiplyLow(vcoeff1, vprev1));
                    sum = Sse2.Add(sum, Sse41.MultiplyLow(vcoeff2, vprev2));
                    sum = Sse2.Add(sum, Sse41.MultiplyLow(vcoeff3, vprev3));
                    sum = Sse2.Add(sum, Sse41.MultiplyLow(vcoeff4, vprev4));
                    sum = Sse2.Add(sum, Sse41.MultiplyLow(vcoeff5, vprev5));
                    sum = Sse2.Add(sum, Sse41.MultiplyLow(vcoeff6, vprev6));
                    var y = Ssse3.HorizontalAdd(sum, vzero);
                    y = Ssse3.HorizontalAdd(y, vzero);
                    y = Sse2.ShiftRightArithmetic(y, vshift);   //C# shift operator handling sucks so SSE2 is used instead(same latency, same throughput).
                    y = Sse2.Add(y, res);   //Avoids extract and insert
#if NET5_0_OR_GREATER
                    Sse2.StoreScalar((int*)Unsafe.AsPointer(ref Unsafe.Add(ref d, i)), y);
#else
                    Unsafe.Add(ref d, i) = y.GetElement(0);
#endif
                    y = Sse2.ShiftLeftLogical128BitLane(y, 12);
                    vprev6 = Ssse3.AlignRight(vprev6, vprev5, 12);
                    vprev5 = Ssse3.AlignRight(vprev5, vprev4, 12);
                    vprev4 = Ssse3.AlignRight(vprev4, vprev3, 12);
                    vprev3 = Ssse3.AlignRight(vprev3, vprev2, 12);
                    vprev2 = Ssse3.AlignRight(vprev2, vprev1, 12);
                    vprev1 = Ssse3.AlignRight(vprev1, vprev0, 12);
                    vprev0 = Ssse3.AlignRight(vprev0, y, 12);
                }
            }

            [MethodImpl(OptimizationUtils.InlineAndOptimizeIfPossible)]
            internal static bool RestoreSignalOrder25Wide(int shiftsNeeded, ReadOnlySpan<int> residual, ReadOnlySpan<int> coeffs, Span<int> output)
            {
                if (Sse41.IsSupported)
                {
                    RestoreSignalOrder25WideSse41(shiftsNeeded, residual, coeffs, output);
                    return true;
                }
                return false;
            }

            [MethodImpl(OptimizationUtils.InlineAndOptimizeIfPossible)]
            internal static unsafe void RestoreSignalOrder25WideSse41(int shiftsNeeded, ReadOnlySpan<int> residual, ReadOnlySpan<int> coeffs, Span<int> output)
            {
                const int Order = 25;
                if(coeffs.Length < Order) return;
                _ = coeffs[Order - 1];
                var vshift = Vector128.CreateScalar((long)shiftsNeeded);
                ref var c = ref MemoryMarshal.GetReference(coeffs);
                ref var o = ref MemoryMarshal.GetReference(output);
                ref var d = ref Unsafe.Add(ref o, Order);
                int dataLength = output.Length - Order;
                ref var r = ref MemoryMarshal.GetReference(residual);
                Vector128<long> sum;
                var vcoeff0 = Sse41.ConvertToVector128Int64(Sse2.LoadScalarVector128((ulong*)Unsafe.AsPointer(ref Unsafe.Add(ref c, 0))).AsUInt32()).AsInt32();
                var vcoeff1 = Sse41.ConvertToVector128Int64(Sse2.LoadScalarVector128((ulong*)Unsafe.AsPointer(ref Unsafe.Add(ref c, 2))).AsUInt32()).AsInt32();
                var vcoeff2 = Sse41.ConvertToVector128Int64(Sse2.LoadScalarVector128((ulong*)Unsafe.AsPointer(ref Unsafe.Add(ref c, 4))).AsUInt32()).AsInt32();
                var vcoeff3 = Sse41.ConvertToVector128Int64(Sse2.LoadScalarVector128((ulong*)Unsafe.AsPointer(ref Unsafe.Add(ref c, 6))).AsUInt32()).AsInt32();
                var vcoeff4 = Sse41.ConvertToVector128Int64(Sse2.LoadScalarVector128((ulong*)Unsafe.AsPointer(ref Unsafe.Add(ref c, 8))).AsUInt32()).AsInt32();
                var vcoeff5 = Sse41.ConvertToVector128Int64(Sse2.LoadScalarVector128((ulong*)Unsafe.AsPointer(ref Unsafe.Add(ref c, 10))).AsUInt32()).AsInt32();
                var vcoeff6 = Sse41.ConvertToVector128Int64(Sse2.LoadScalarVector128((ulong*)Unsafe.AsPointer(ref Unsafe.Add(ref c, 12))).AsUInt32()).AsInt32();
                var vcoeff7 = Sse41.ConvertToVector128Int64(Sse2.LoadScalarVector128((ulong*)Unsafe.AsPointer(ref Unsafe.Add(ref c, 14))).AsUInt32()).AsInt32();
                var vcoeff8 = Sse41.ConvertToVector128Int64(Sse2.LoadScalarVector128((ulong*)Unsafe.AsPointer(ref Unsafe.Add(ref c, 16))).AsUInt32()).AsInt32();
                var vcoeff9 = Sse41.ConvertToVector128Int64(Sse2.LoadScalarVector128((ulong*)Unsafe.AsPointer(ref Unsafe.Add(ref c, 18))).AsUInt32()).AsInt32();
                var vcoeff10 = Sse41.ConvertToVector128Int64(Sse2.LoadScalarVector128((ulong*)Unsafe.AsPointer(ref Unsafe.Add(ref c, 20))).AsUInt32()).AsInt32();
                var vcoeff11 = Sse41.ConvertToVector128Int64(Sse2.LoadScalarVector128((ulong*)Unsafe.AsPointer(ref Unsafe.Add(ref c, 22))).AsUInt32()).AsInt32();
                var vcoeff12 = Sse41.ConvertToVector128Int64(Sse2.LoadScalarVector128((uint*)Unsafe.AsPointer(ref Unsafe.Add(ref c, 24))).AsUInt32()).AsInt32();
                var vprev0 = Sse2.Shuffle(Sse2.LoadScalarVector128((ulong*)Unsafe.AsPointer(ref Unsafe.Add(ref o, 23))).AsInt32(), 0b11_00_11_01);
                var vprev1 = Sse2.Shuffle(Sse2.LoadScalarVector128((ulong*)Unsafe.AsPointer(ref Unsafe.Add(ref o, 21))).AsInt32(), 0b11_00_11_01);
                var vprev2 = Sse2.Shuffle(Sse2.LoadScalarVector128((ulong*)Unsafe.AsPointer(ref Unsafe.Add(ref o, 19))).AsInt32(), 0b11_00_11_01);
                var vprev3 = Sse2.Shuffle(Sse2.LoadScalarVector128((ulong*)Unsafe.AsPointer(ref Unsafe.Add(ref o, 17))).AsInt32(), 0b11_00_11_01);
                var vprev4 = Sse2.Shuffle(Sse2.LoadScalarVector128((ulong*)Unsafe.AsPointer(ref Unsafe.Add(ref o, 15))).AsInt32(), 0b11_00_11_01);
                var vprev5 = Sse2.Shuffle(Sse2.LoadScalarVector128((ulong*)Unsafe.AsPointer(ref Unsafe.Add(ref o, 13))).AsInt32(), 0b11_00_11_01);
                var vprev6 = Sse2.Shuffle(Sse2.LoadScalarVector128((ulong*)Unsafe.AsPointer(ref Unsafe.Add(ref o, 11))).AsInt32(), 0b11_00_11_01);
                var vprev7 = Sse2.Shuffle(Sse2.LoadScalarVector128((ulong*)Unsafe.AsPointer(ref Unsafe.Add(ref o, 9))).AsInt32(), 0b11_00_11_01);
                var vprev8 = Sse2.Shuffle(Sse2.LoadScalarVector128((ulong*)Unsafe.AsPointer(ref Unsafe.Add(ref o, 7))).AsInt32(), 0b11_00_11_01);
                var vprev9 = Sse2.Shuffle(Sse2.LoadScalarVector128((ulong*)Unsafe.AsPointer(ref Unsafe.Add(ref o, 5))).AsInt32(), 0b11_00_11_01);
                var vprev10 = Sse2.Shuffle(Sse2.LoadScalarVector128((ulong*)Unsafe.AsPointer(ref Unsafe.Add(ref o, 3))).AsInt32(), 0b11_00_11_01);
                var vprev11 = Sse2.Shuffle(Sse2.LoadScalarVector128((ulong*)Unsafe.AsPointer(ref Unsafe.Add(ref o, 1))).AsInt32(), 0b11_00_11_01);
                var vprev12 = Sse2.Shuffle(Sse2.LoadScalarVector128((ulong*)Unsafe.AsPointer(ref Unsafe.Add(ref o, 0))).AsInt32(), 0b11_00_11_00);
                for(nint i = 0; i < dataLength;)
                {
                    var res = Vector128.CreateScalar(Unsafe.Add(ref r, i));
                    sum = Sse41.Multiply(vcoeff0, vprev0);
                    sum = Sse2.Add(sum, Sse41.Multiply(vcoeff1, vprev1));
                    sum = Sse2.Add(sum, Sse41.Multiply(vcoeff2, vprev2));
                    sum = Sse2.Add(sum, Sse41.Multiply(vcoeff3, vprev3));
                    sum = Sse2.Add(sum, Sse41.Multiply(vcoeff4, vprev4));
                    sum = Sse2.Add(sum, Sse41.Multiply(vcoeff5, vprev5));
                    sum = Sse2.Add(sum, Sse41.Multiply(vcoeff6, vprev6));
                    sum = Sse2.Add(sum, Sse41.Multiply(vcoeff7, vprev7));
                    sum = Sse2.Add(sum, Sse41.Multiply(vcoeff8, vprev8));
                    sum = Sse2.Add(sum, Sse41.Multiply(vcoeff9, vprev9));
                    sum = Sse2.Add(sum, Sse41.Multiply(vcoeff10, vprev10));
                    sum = Sse2.Add(sum, Sse41.Multiply(vcoeff11, vprev11));
                    sum = Sse2.Add(sum, Sse41.Multiply(vcoeff12, vprev12));
                    sum = Sse2.Add(sum, Sse2.ShiftRightLogical128BitLane(sum, 8));
                    sum = Sse2.ShiftRightLogical(sum, vshift);
                    var yy = Sse2.Add(sum.AsInt32(), res);
#if NET5_0_OR_GREATER
                    Sse2.StoreScalar((int*)Unsafe.AsPointer(ref Unsafe.Add(ref d, i)), yy);
#else
                    Unsafe.Add(ref d, i) = yy.GetElement(0);
#endif
                    i++;
                    yy = Sse2.ShiftLeftLogical128BitLane(yy, 8);
                    vprev12 = Ssse3.AlignRight(vprev12, vprev11, 8);
                    vprev11 = Ssse3.AlignRight(vprev11, vprev10, 8);
                    vprev10 = Ssse3.AlignRight(vprev10, vprev9, 8);
                    vprev9 = Ssse3.AlignRight(vprev9, vprev8, 8);
                    vprev8 = Ssse3.AlignRight(vprev8, vprev7, 8);
                    vprev7 = Ssse3.AlignRight(vprev7, vprev6, 8);
                    vprev6 = Ssse3.AlignRight(vprev6, vprev5, 8);
                    vprev5 = Ssse3.AlignRight(vprev5, vprev4, 8);
                    vprev4 = Ssse3.AlignRight(vprev4, vprev3, 8);
                    vprev3 = Ssse3.AlignRight(vprev3, vprev2, 8);
                    vprev2 = Ssse3.AlignRight(vprev2, vprev1, 8);
                    vprev1 = Ssse3.AlignRight(vprev1, vprev0, 8);
                    vprev0 = Ssse3.AlignRight(vprev0, yy, 8);
                }
            }
            
            [MethodImpl(OptimizationUtils.InlineAndOptimizeIfPossible)]
            internal static unsafe void RestoreSignalOrder25WideAvx2(int shiftsNeeded, ReadOnlySpan<int> residual, ReadOnlySpan<int> coeffs, Span<int> output)
            {
                const int Order = 25;
                if(coeffs.Length < Order) return;
                _ = coeffs[Order - 1];
                var vshift = Vector128.CreateScalar((long)shiftsNeeded);
                ref var c = ref MemoryMarshal.GetReference(coeffs);
                ref var o = ref MemoryMarshal.GetReference(output);
                ref var d = ref Unsafe.Add(ref o, Order);
                int dataLength = output.Length - Order;
                ref var r = ref MemoryMarshal.GetReference(residual);
                Vector128<long> sum;
                Vector256<long> sum256;
                var vcoeff0 = Avx2.ConvertToVector256Int64(Sse2.LoadVector128((int*)Unsafe.AsPointer(ref Unsafe.Add(ref c, 0))).AsUInt32()).AsInt32();
                var vcoeff1 = Avx2.ConvertToVector256Int64(Sse2.LoadVector128((int*)Unsafe.AsPointer(ref Unsafe.Add(ref c, 4))).AsUInt32()).AsInt32();
                var vcoeff2 = Avx2.ConvertToVector256Int64(Sse2.LoadVector128((int*)Unsafe.AsPointer(ref Unsafe.Add(ref c, 8))).AsUInt32()).AsInt32();
                var vcoeff3 = Avx2.ConvertToVector256Int64(Sse2.LoadVector128((int*)Unsafe.AsPointer(ref Unsafe.Add(ref c, 12))).AsUInt32()).AsInt32();
                var vcoeff4 = Avx2.ConvertToVector256Int64(Sse2.LoadVector128((int*)Unsafe.AsPointer(ref Unsafe.Add(ref c, 16))).AsUInt32()).AsInt32();
                var vcoeff5 = Avx2.ConvertToVector256Int64(Sse2.LoadVector128((int*)Unsafe.AsPointer(ref Unsafe.Add(ref c, 20))).AsUInt32()).AsInt32();
                var vcoeff6 = Avx2.ConvertToVector256Int64(Sse2.LoadScalarVector128((uint*)Unsafe.AsPointer(ref Unsafe.Add(ref c, 24))).AsUInt32()).AsInt32();
                var vprev0 = Avx2.ConvertToVector256Int64(Sse2.Shuffle(Sse2.LoadScalarVector128((ulong*)Unsafe.AsPointer(ref Unsafe.Add(ref o, 22))).AsInt32(), 0b00_01_10_11).AsUInt32()).AsInt32();
                var vprev1 = Avx2.ConvertToVector256Int64(Sse2.Shuffle(Sse2.LoadScalarVector128((ulong*)Unsafe.AsPointer(ref Unsafe.Add(ref o, 18))).AsInt32(), 0b00_01_10_11).AsUInt32()).AsInt32();
                var vprev2 = Avx2.ConvertToVector256Int64(Sse2.Shuffle(Sse2.LoadScalarVector128((ulong*)Unsafe.AsPointer(ref Unsafe.Add(ref o, 14))).AsInt32(), 0b00_01_10_11).AsUInt32()).AsInt32();
                var vprev3 = Avx2.ConvertToVector256Int64(Sse2.Shuffle(Sse2.LoadScalarVector128((ulong*)Unsafe.AsPointer(ref Unsafe.Add(ref o, 10))).AsInt32(), 0b00_01_10_11).AsUInt32()).AsInt32();
                var vprev4 = Avx2.ConvertToVector256Int64(Sse2.Shuffle(Sse2.LoadScalarVector128((ulong*)Unsafe.AsPointer(ref Unsafe.Add(ref o, 6))).AsInt32(), 0b00_01_10_11).AsUInt32()).AsInt32();
                var vprev5 = Avx2.ConvertToVector256Int64(Sse2.Shuffle(Sse2.LoadScalarVector128((ulong*)Unsafe.AsPointer(ref Unsafe.Add(ref o, 2))).AsInt32(), 0b00_01_10_11).AsUInt32()).AsInt32();
                var vprev6 = Avx2.ConvertToVector256Int64(Sse2.Shuffle(Sse2.LoadScalarVector128((uint*)Unsafe.AsPointer(ref o)).AsInt32(), 0b00_01_10_11).AsUInt32()).AsInt32();
                for(nint i = 0; i < dataLength;)
                {
                    var res = Vector128.CreateScalar(Unsafe.Add(ref r, i));
                    sum256 = Avx2.Multiply(vcoeff0, vprev0);
                    sum256 = Avx2.Add(sum256, Avx2.Multiply(vcoeff1, vprev1));
                    sum256 = Avx2.Add(sum256, Avx2.Multiply(vcoeff2, vprev2));
                    sum256 = Avx2.Add(sum256, Avx2.Multiply(vcoeff3, vprev3));
                    sum256 = Avx2.Add(sum256, Avx2.Multiply(vcoeff4, vprev4));
                    sum256 = Avx2.Add(sum256, Avx2.Multiply(vcoeff5, vprev5));
                    sum256 = Avx2.Add(sum256, Avx2.Multiply(vcoeff6, vprev6));
                    sum = Sse2.Add(sum256.GetLower(), sum256.GetUpper());
                    sum = Sse2.Add(sum, Sse2.ShiftRightLogical128BitLane(sum, 8));
                    sum = Sse2.ShiftRightLogical(sum, vshift);
                    var yy = Sse2.Add(sum.AsInt32(), res);
#if NET5_0_OR_GREATER
                    Sse2.StoreScalar((int*)Unsafe.AsPointer(ref Unsafe.Add(ref d, i)), yy);
#else
                    Unsafe.Add(ref d, i) = yy.GetElement(0);
#endif
                    i++;
                    var yu = yy.ToVector256();
                    yu = Avx2.Permute4x64(yu.AsUInt64(), 0b00_00_00_00).AsInt32();
                    vprev6 = Avx2.Permute4x64(vprev6.AsInt64(), 0b10_01_00_11).AsInt32();
                    vprev6 = Avx2.Blend(vprev6, vprev5, 0b0000_0001);
                    vprev5 = Avx2.Permute4x64(vprev5.AsInt64(), 0b10_01_00_11).AsInt32();
                    vprev5 = Avx2.Blend(vprev5, vprev4, 0b0000_0001);
                    vprev4 = Avx2.Permute4x64(vprev4.AsInt64(), 0b10_01_00_11).AsInt32();
                    vprev4 = Avx2.Blend(vprev4, vprev3, 0b0000_0001);
                    vprev3 = Avx2.Permute4x64(vprev3.AsInt64(), 0b10_01_00_11).AsInt32();
                    vprev3 = Avx2.Blend(vprev3, vprev2, 0b0000_0001);
                    vprev2 = Avx2.Permute4x64(vprev2.AsInt64(), 0b10_01_00_11).AsInt32();
                    vprev2 = Avx2.Blend(vprev2, vprev1, 0b0000_0001);
                    vprev1 = Avx2.Permute4x64(vprev1.AsInt64(), 0b10_01_00_11).AsInt32();
                    vprev1 = Avx2.Blend(vprev1, vprev0, 0b0000_0001);
                    vprev0 = Avx2.Permute4x64(vprev0.AsInt64(), 0b10_01_00_11).AsInt32();
                    vprev0 = Avx2.Blend(vprev0, yu, 0b0000_0001);
                }
            }
#endregion Order25
#region Order26

            [MethodImpl(OptimizationUtils.InlineAndOptimizeIfPossible)]
            internal static bool RestoreSignalOrder26(int shiftsNeeded, ReadOnlySpan<int> residual, ReadOnlySpan<int> coeffs, Span<int> output)
            {
                if (Sse41.IsSupported)
                {
                    RestoreSignalOrder26Sse41(shiftsNeeded, residual, coeffs, output);
                    return true;
                }
                return false;
            }

            [MethodImpl(OptimizationUtils.InlineAndOptimizeIfPossible)]
            internal static unsafe void RestoreSignalOrder26Sse41(int shiftsNeeded, ReadOnlySpan<int> residual, ReadOnlySpan<int> coeffs, Span<int> output)
            {
                const int Order = 26;
                if (coeffs.Length < Order) return;
                _ = coeffs[Order - 1];
                var vshift = Vector128.CreateScalar(shiftsNeeded);
                ref var o = ref MemoryMarshal.GetReference(output);
                ref var d = ref Unsafe.Add(ref o, Order);
                ref var r = ref MemoryMarshal.GetReference(residual);
                ref var c = ref MemoryMarshal.GetReference(coeffs);
                Vector128<int> sum;
                var vzero = Vector128.Create(0);
                nint dataLength = output.Length - Order;
                var vcoeff0 = Sse2.LoadVector128((int*)Unsafe.AsPointer(ref Unsafe.Add(ref c, 0)));
                var vcoeff1 = Sse2.LoadVector128((int*)Unsafe.AsPointer(ref Unsafe.Add(ref c, 4)));
                var vcoeff2 = Sse2.LoadVector128((int*)Unsafe.AsPointer(ref Unsafe.Add(ref c, 8)));
                var vcoeff3 = Sse2.LoadVector128((int*)Unsafe.AsPointer(ref Unsafe.Add(ref c, 12)));
                var vcoeff4 = Sse2.LoadVector128((int*)Unsafe.AsPointer(ref Unsafe.Add(ref c, 16)));
                var vcoeff5 = Sse2.LoadVector128((int*)Unsafe.AsPointer(ref Unsafe.Add(ref c, 20)));
                var vcoeff6 = Vector128.Create(Unsafe.Add(ref c, 24), Unsafe.Add(ref c, 25), 0, 0);
                var vprev0 = Sse2.Shuffle(Sse2.LoadVector128((int*)Unsafe.AsPointer(ref Unsafe.Add(ref o, 22))).AsInt32(), 0b00_01_10_11);
                var vprev1 = Sse2.Shuffle(Sse2.LoadVector128((int*)Unsafe.AsPointer(ref Unsafe.Add(ref o, 18))).AsInt32(), 0b00_01_10_11);
                var vprev2 = Sse2.Shuffle(Sse2.LoadVector128((int*)Unsafe.AsPointer(ref Unsafe.Add(ref o, 14))).AsInt32(), 0b00_01_10_11);
                var vprev3 = Sse2.Shuffle(Sse2.LoadVector128((int*)Unsafe.AsPointer(ref Unsafe.Add(ref o, 10))).AsInt32(), 0b00_01_10_11);
                var vprev4 = Sse2.Shuffle(Sse2.LoadVector128((int*)Unsafe.AsPointer(ref Unsafe.Add(ref o, 6))).AsInt32(), 0b00_01_10_11);
                var vprev5 = Sse2.Shuffle(Sse2.LoadVector128((int*)Unsafe.AsPointer(ref Unsafe.Add(ref o, 2))).AsInt32(), 0b00_01_10_11);
                var vprev6 = Vector128.Create(Unsafe.Add(ref o, 1), Unsafe.Add(ref o, 0), 0, 0);
                for (nint i = 0; i < dataLength; i++)
                {
                    var res = Vector128.CreateScalar(Unsafe.Add(ref r, i));
                    sum = Sse41.MultiplyLow(vcoeff0, vprev0);
                    sum = Sse2.Add(sum, Sse41.MultiplyLow(vcoeff1, vprev1));
                    sum = Sse2.Add(sum, Sse41.MultiplyLow(vcoeff2, vprev2));
                    sum = Sse2.Add(sum, Sse41.MultiplyLow(vcoeff3, vprev3));
                    sum = Sse2.Add(sum, Sse41.MultiplyLow(vcoeff4, vprev4));
                    sum = Sse2.Add(sum, Sse41.MultiplyLow(vcoeff5, vprev5));
                    sum = Sse2.Add(sum, Sse41.MultiplyLow(vcoeff6, vprev6));
                    var y = Ssse3.HorizontalAdd(sum, vzero);
                    y = Ssse3.HorizontalAdd(y, vzero);
                    y = Sse2.ShiftRightArithmetic(y, vshift);   //C# shift operator handling sucks so SSE2 is used instead(same latency, same throughput).
                    y = Sse2.Add(y, res);   //Avoids extract and insert
#if NET5_0_OR_GREATER
                    Sse2.StoreScalar((int*)Unsafe.AsPointer(ref Unsafe.Add(ref d, i)), y);
#else
                    Unsafe.Add(ref d, i) = y.GetElement(0);
#endif
                    y = Sse2.ShiftLeftLogical128BitLane(y, 12);
                    vprev6 = Ssse3.AlignRight(vprev6, vprev5, 12);
                    vprev5 = Ssse3.AlignRight(vprev5, vprev4, 12);
                    vprev4 = Ssse3.AlignRight(vprev4, vprev3, 12);
                    vprev3 = Ssse3.AlignRight(vprev3, vprev2, 12);
                    vprev2 = Ssse3.AlignRight(vprev2, vprev1, 12);
                    vprev1 = Ssse3.AlignRight(vprev1, vprev0, 12);
                    vprev0 = Ssse3.AlignRight(vprev0, y, 12);
                }
            }

            [MethodImpl(OptimizationUtils.InlineAndOptimizeIfPossible)]
            internal static bool RestoreSignalOrder26Wide(int shiftsNeeded, ReadOnlySpan<int> residual, ReadOnlySpan<int> coeffs, Span<int> output)
            {
                if (Sse41.IsSupported)
                {
                    RestoreSignalOrder26WideSse41(shiftsNeeded, residual, coeffs, output);
                    return true;
                }
                return false;
            }

            [MethodImpl(OptimizationUtils.InlineAndOptimizeIfPossible)]
            internal static unsafe void RestoreSignalOrder26WideSse41(int shiftsNeeded, ReadOnlySpan<int> residual, ReadOnlySpan<int> coeffs, Span<int> output)
            {
                const int Order = 26;
                if(coeffs.Length < Order) return;
                _ = coeffs[Order - 1];
                var vshift = Vector128.CreateScalar((long)shiftsNeeded);
                ref var c = ref MemoryMarshal.GetReference(coeffs);
                ref var o = ref MemoryMarshal.GetReference(output);
                ref var d = ref Unsafe.Add(ref o, Order);
                int dataLength = output.Length - Order;
                ref var r = ref MemoryMarshal.GetReference(residual);
                Vector128<long> sum;
                var vcoeff0 = Sse41.ConvertToVector128Int64(Sse2.LoadScalarVector128((ulong*)Unsafe.AsPointer(ref Unsafe.Add(ref c, 0))).AsUInt32()).AsInt32();
                var vcoeff1 = Sse41.ConvertToVector128Int64(Sse2.LoadScalarVector128((ulong*)Unsafe.AsPointer(ref Unsafe.Add(ref c, 2))).AsUInt32()).AsInt32();
                var vcoeff2 = Sse41.ConvertToVector128Int64(Sse2.LoadScalarVector128((ulong*)Unsafe.AsPointer(ref Unsafe.Add(ref c, 4))).AsUInt32()).AsInt32();
                var vcoeff3 = Sse41.ConvertToVector128Int64(Sse2.LoadScalarVector128((ulong*)Unsafe.AsPointer(ref Unsafe.Add(ref c, 6))).AsUInt32()).AsInt32();
                var vcoeff4 = Sse41.ConvertToVector128Int64(Sse2.LoadScalarVector128((ulong*)Unsafe.AsPointer(ref Unsafe.Add(ref c, 8))).AsUInt32()).AsInt32();
                var vcoeff5 = Sse41.ConvertToVector128Int64(Sse2.LoadScalarVector128((ulong*)Unsafe.AsPointer(ref Unsafe.Add(ref c, 10))).AsUInt32()).AsInt32();
                var vcoeff6 = Sse41.ConvertToVector128Int64(Sse2.LoadScalarVector128((ulong*)Unsafe.AsPointer(ref Unsafe.Add(ref c, 12))).AsUInt32()).AsInt32();
                var vcoeff7 = Sse41.ConvertToVector128Int64(Sse2.LoadScalarVector128((ulong*)Unsafe.AsPointer(ref Unsafe.Add(ref c, 14))).AsUInt32()).AsInt32();
                var vcoeff8 = Sse41.ConvertToVector128Int64(Sse2.LoadScalarVector128((ulong*)Unsafe.AsPointer(ref Unsafe.Add(ref c, 16))).AsUInt32()).AsInt32();
                var vcoeff9 = Sse41.ConvertToVector128Int64(Sse2.LoadScalarVector128((ulong*)Unsafe.AsPointer(ref Unsafe.Add(ref c, 18))).AsUInt32()).AsInt32();
                var vcoeff10 = Sse41.ConvertToVector128Int64(Sse2.LoadScalarVector128((ulong*)Unsafe.AsPointer(ref Unsafe.Add(ref c, 20))).AsUInt32()).AsInt32();
                var vcoeff11 = Sse41.ConvertToVector128Int64(Sse2.LoadScalarVector128((ulong*)Unsafe.AsPointer(ref Unsafe.Add(ref c, 22))).AsUInt32()).AsInt32();
                var vcoeff12 = Sse41.ConvertToVector128Int64(Sse2.LoadScalarVector128((ulong*)Unsafe.AsPointer(ref Unsafe.Add(ref c, 24))).AsUInt32()).AsInt32();
                var vprev0 = Sse2.Shuffle(Sse2.LoadScalarVector128((ulong*)Unsafe.AsPointer(ref Unsafe.Add(ref o, 24))).AsInt32(), 0b11_00_11_01);
                var vprev1 = Sse2.Shuffle(Sse2.LoadScalarVector128((ulong*)Unsafe.AsPointer(ref Unsafe.Add(ref o, 22))).AsInt32(), 0b11_00_11_01);
                var vprev2 = Sse2.Shuffle(Sse2.LoadScalarVector128((ulong*)Unsafe.AsPointer(ref Unsafe.Add(ref o, 20))).AsInt32(), 0b11_00_11_01);
                var vprev3 = Sse2.Shuffle(Sse2.LoadScalarVector128((ulong*)Unsafe.AsPointer(ref Unsafe.Add(ref o, 18))).AsInt32(), 0b11_00_11_01);
                var vprev4 = Sse2.Shuffle(Sse2.LoadScalarVector128((ulong*)Unsafe.AsPointer(ref Unsafe.Add(ref o, 16))).AsInt32(), 0b11_00_11_01);
                var vprev5 = Sse2.Shuffle(Sse2.LoadScalarVector128((ulong*)Unsafe.AsPointer(ref Unsafe.Add(ref o, 14))).AsInt32(), 0b11_00_11_01);
                var vprev6 = Sse2.Shuffle(Sse2.LoadScalarVector128((ulong*)Unsafe.AsPointer(ref Unsafe.Add(ref o, 12))).AsInt32(), 0b11_00_11_01);
                var vprev7 = Sse2.Shuffle(Sse2.LoadScalarVector128((ulong*)Unsafe.AsPointer(ref Unsafe.Add(ref o, 10))).AsInt32(), 0b11_00_11_01);
                var vprev8 = Sse2.Shuffle(Sse2.LoadScalarVector128((ulong*)Unsafe.AsPointer(ref Unsafe.Add(ref o, 8))).AsInt32(), 0b11_00_11_01);
                var vprev9 = Sse2.Shuffle(Sse2.LoadScalarVector128((ulong*)Unsafe.AsPointer(ref Unsafe.Add(ref o, 6))).AsInt32(), 0b11_00_11_01);
                var vprev10 = Sse2.Shuffle(Sse2.LoadScalarVector128((ulong*)Unsafe.AsPointer(ref Unsafe.Add(ref o, 4))).AsInt32(), 0b11_00_11_01);
                var vprev11 = Sse2.Shuffle(Sse2.LoadScalarVector128((ulong*)Unsafe.AsPointer(ref Unsafe.Add(ref o, 2))).AsInt32(), 0b11_00_11_01);
                var vprev12 = Sse2.Shuffle(Sse2.LoadScalarVector128((ulong*)Unsafe.AsPointer(ref Unsafe.Add(ref o, 0))).AsInt32(), 0b11_00_11_01);
                for(nint i = 0; i < dataLength;)
                {
                    var res = Vector128.CreateScalar(Unsafe.Add(ref r, i));
                    sum = Sse41.Multiply(vcoeff0, vprev0);
                    sum = Sse2.Add(sum, Sse41.Multiply(vcoeff1, vprev1));
                    sum = Sse2.Add(sum, Sse41.Multiply(vcoeff2, vprev2));
                    sum = Sse2.Add(sum, Sse41.Multiply(vcoeff3, vprev3));
                    sum = Sse2.Add(sum, Sse41.Multiply(vcoeff4, vprev4));
                    sum = Sse2.Add(sum, Sse41.Multiply(vcoeff5, vprev5));
                    sum = Sse2.Add(sum, Sse41.Multiply(vcoeff6, vprev6));
                    sum = Sse2.Add(sum, Sse41.Multiply(vcoeff7, vprev7));
                    sum = Sse2.Add(sum, Sse41.Multiply(vcoeff8, vprev8));
                    sum = Sse2.Add(sum, Sse41.Multiply(vcoeff9, vprev9));
                    sum = Sse2.Add(sum, Sse41.Multiply(vcoeff10, vprev10));
                    sum = Sse2.Add(sum, Sse41.Multiply(vcoeff11, vprev11));
                    sum = Sse2.Add(sum, Sse41.Multiply(vcoeff12, vprev12));
                    sum = Sse2.Add(sum, Sse2.ShiftRightLogical128BitLane(sum, 8));
                    sum = Sse2.ShiftRightLogical(sum, vshift);
                    var yy = Sse2.Add(sum.AsInt32(), res);
#if NET5_0_OR_GREATER
                    Sse2.StoreScalar((int*)Unsafe.AsPointer(ref Unsafe.Add(ref d, i)), yy);
#else
                    Unsafe.Add(ref d, i) = yy.GetElement(0);
#endif
                    i++;
                    yy = Sse2.ShiftLeftLogical128BitLane(yy, 8);
                    vprev12 = Ssse3.AlignRight(vprev12, vprev11, 8);
                    vprev11 = Ssse3.AlignRight(vprev11, vprev10, 8);
                    vprev10 = Ssse3.AlignRight(vprev10, vprev9, 8);
                    vprev9 = Ssse3.AlignRight(vprev9, vprev8, 8);
                    vprev8 = Ssse3.AlignRight(vprev8, vprev7, 8);
                    vprev7 = Ssse3.AlignRight(vprev7, vprev6, 8);
                    vprev6 = Ssse3.AlignRight(vprev6, vprev5, 8);
                    vprev5 = Ssse3.AlignRight(vprev5, vprev4, 8);
                    vprev4 = Ssse3.AlignRight(vprev4, vprev3, 8);
                    vprev3 = Ssse3.AlignRight(vprev3, vprev2, 8);
                    vprev2 = Ssse3.AlignRight(vprev2, vprev1, 8);
                    vprev1 = Ssse3.AlignRight(vprev1, vprev0, 8);
                    vprev0 = Ssse3.AlignRight(vprev0, yy, 8);
                }
            }
            
            [MethodImpl(OptimizationUtils.InlineAndOptimizeIfPossible)]
            internal static unsafe void RestoreSignalOrder26WideAvx2(int shiftsNeeded, ReadOnlySpan<int> residual, ReadOnlySpan<int> coeffs, Span<int> output)
            {
                const int Order = 26;
                if(coeffs.Length < Order) return;
                _ = coeffs[Order - 1];
                var vshift = Vector128.CreateScalar((long)shiftsNeeded);
                ref var c = ref MemoryMarshal.GetReference(coeffs);
                ref var o = ref MemoryMarshal.GetReference(output);
                ref var d = ref Unsafe.Add(ref o, Order);
                int dataLength = output.Length - Order;
                ref var r = ref MemoryMarshal.GetReference(residual);
                Vector128<long> sum;
                Vector256<long> sum256;
                var vcoeff0 = Avx2.ConvertToVector256Int64(Sse2.LoadVector128((int*)Unsafe.AsPointer(ref Unsafe.Add(ref c, 0))).AsUInt32()).AsInt32();
                var vcoeff1 = Avx2.ConvertToVector256Int64(Sse2.LoadVector128((int*)Unsafe.AsPointer(ref Unsafe.Add(ref c, 4))).AsUInt32()).AsInt32();
                var vcoeff2 = Avx2.ConvertToVector256Int64(Sse2.LoadVector128((int*)Unsafe.AsPointer(ref Unsafe.Add(ref c, 8))).AsUInt32()).AsInt32();
                var vcoeff3 = Avx2.ConvertToVector256Int64(Sse2.LoadVector128((int*)Unsafe.AsPointer(ref Unsafe.Add(ref c, 12))).AsUInt32()).AsInt32();
                var vcoeff4 = Avx2.ConvertToVector256Int64(Sse2.LoadVector128((int*)Unsafe.AsPointer(ref Unsafe.Add(ref c, 16))).AsUInt32()).AsInt32();
                var vcoeff5 = Avx2.ConvertToVector256Int64(Sse2.LoadVector128((int*)Unsafe.AsPointer(ref Unsafe.Add(ref c, 20))).AsUInt32()).AsInt32();
                var vcoeff6 = Avx2.ConvertToVector256Int64(Sse2.LoadScalarVector128((ulong*)Unsafe.AsPointer(ref Unsafe.Add(ref c, 24))).AsUInt32()).AsInt32();
                var vprev0 = Avx2.ConvertToVector256Int64(Sse2.Shuffle(Sse2.LoadScalarVector128((ulong*)Unsafe.AsPointer(ref Unsafe.Add(ref o, 23))).AsInt32(), 0b00_01_10_11).AsUInt32()).AsInt32();
                var vprev1 = Avx2.ConvertToVector256Int64(Sse2.Shuffle(Sse2.LoadScalarVector128((ulong*)Unsafe.AsPointer(ref Unsafe.Add(ref o, 19))).AsInt32(), 0b00_01_10_11).AsUInt32()).AsInt32();
                var vprev2 = Avx2.ConvertToVector256Int64(Sse2.Shuffle(Sse2.LoadScalarVector128((ulong*)Unsafe.AsPointer(ref Unsafe.Add(ref o, 15))).AsInt32(), 0b00_01_10_11).AsUInt32()).AsInt32();
                var vprev3 = Avx2.ConvertToVector256Int64(Sse2.Shuffle(Sse2.LoadScalarVector128((ulong*)Unsafe.AsPointer(ref Unsafe.Add(ref o, 11))).AsInt32(), 0b00_01_10_11).AsUInt32()).AsInt32();
                var vprev4 = Avx2.ConvertToVector256Int64(Sse2.Shuffle(Sse2.LoadScalarVector128((ulong*)Unsafe.AsPointer(ref Unsafe.Add(ref o, 7))).AsInt32(), 0b00_01_10_11).AsUInt32()).AsInt32();
                var vprev5 = Avx2.ConvertToVector256Int64(Sse2.Shuffle(Sse2.LoadScalarVector128((ulong*)Unsafe.AsPointer(ref Unsafe.Add(ref o, 3))).AsInt32(), 0b00_01_10_11).AsUInt32()).AsInt32();
                var vprev6 = Avx2.ConvertToVector256Int64(Sse2.Shuffle(Sse2.LoadScalarVector128((ulong*)Unsafe.AsPointer(ref o)).AsInt32(), 0b00_01_10_11).AsUInt32()).AsInt32();
                for(nint i = 0; i < dataLength;)
                {
                    var res = Vector128.CreateScalar(Unsafe.Add(ref r, i));
                    sum256 = Avx2.Multiply(vcoeff0, vprev0);
                    sum256 = Avx2.Add(sum256, Avx2.Multiply(vcoeff1, vprev1));
                    sum256 = Avx2.Add(sum256, Avx2.Multiply(vcoeff2, vprev2));
                    sum256 = Avx2.Add(sum256, Avx2.Multiply(vcoeff3, vprev3));
                    sum256 = Avx2.Add(sum256, Avx2.Multiply(vcoeff4, vprev4));
                    sum256 = Avx2.Add(sum256, Avx2.Multiply(vcoeff5, vprev5));
                    sum256 = Avx2.Add(sum256, Avx2.Multiply(vcoeff6, vprev6));
                    sum = Sse2.Add(sum256.GetLower(), sum256.GetUpper());
                    sum = Sse2.Add(sum, Sse2.ShiftRightLogical128BitLane(sum, 8));
                    sum = Sse2.ShiftRightLogical(sum, vshift);
                    var yy = Sse2.Add(sum.AsInt32(), res);
#if NET5_0_OR_GREATER
                    Sse2.StoreScalar((int*)Unsafe.AsPointer(ref Unsafe.Add(ref d, i)), yy);
#else
                    Unsafe.Add(ref d, i) = yy.GetElement(0);
#endif
                    i++;
                    var yu = yy.ToVector256();
                    yu = Avx2.Permute4x64(yu.AsUInt64(), 0b00_00_00_00).AsInt32();
                    vprev6 = Avx2.Permute4x64(vprev6.AsInt64(), 0b10_01_00_11).AsInt32();
                    vprev6 = Avx2.Blend(vprev6, vprev5, 0b0000_0001);
                    vprev5 = Avx2.Permute4x64(vprev5.AsInt64(), 0b10_01_00_11).AsInt32();
                    vprev5 = Avx2.Blend(vprev5, vprev4, 0b0000_0001);
                    vprev4 = Avx2.Permute4x64(vprev4.AsInt64(), 0b10_01_00_11).AsInt32();
                    vprev4 = Avx2.Blend(vprev4, vprev3, 0b0000_0001);
                    vprev3 = Avx2.Permute4x64(vprev3.AsInt64(), 0b10_01_00_11).AsInt32();
                    vprev3 = Avx2.Blend(vprev3, vprev2, 0b0000_0001);
                    vprev2 = Avx2.Permute4x64(vprev2.AsInt64(), 0b10_01_00_11).AsInt32();
                    vprev2 = Avx2.Blend(vprev2, vprev1, 0b0000_0001);
                    vprev1 = Avx2.Permute4x64(vprev1.AsInt64(), 0b10_01_00_11).AsInt32();
                    vprev1 = Avx2.Blend(vprev1, vprev0, 0b0000_0001);
                    vprev0 = Avx2.Permute4x64(vprev0.AsInt64(), 0b10_01_00_11).AsInt32();
                    vprev0 = Avx2.Blend(vprev0, yu, 0b0000_0001);
                }
            }
#endregion Order26
#region Order27

            [MethodImpl(OptimizationUtils.InlineAndOptimizeIfPossible)]
            internal static bool RestoreSignalOrder27(int shiftsNeeded, ReadOnlySpan<int> residual, ReadOnlySpan<int> coeffs, Span<int> output)
            {
                if (Sse41.IsSupported)
                {
                    RestoreSignalOrder27Sse41(shiftsNeeded, residual, coeffs, output);
                    return true;
                }
                return false;
            }

            [MethodImpl(OptimizationUtils.InlineAndOptimizeIfPossible)]
            internal static unsafe void RestoreSignalOrder27Sse41(int shiftsNeeded, ReadOnlySpan<int> residual, ReadOnlySpan<int> coeffs, Span<int> output)
            {
                const int Order = 27;
                if (coeffs.Length < Order) return;
                _ = coeffs[Order - 1];
                var vshift = Vector128.CreateScalar(shiftsNeeded);
                ref var o = ref MemoryMarshal.GetReference(output);
                ref var d = ref Unsafe.Add(ref o, Order);
                ref var r = ref MemoryMarshal.GetReference(residual);
                ref var c = ref MemoryMarshal.GetReference(coeffs);
                Vector128<int> sum;
                var vzero = Vector128.Create(0);
                nint dataLength = output.Length - Order;
                var vcoeff0 = Sse2.LoadVector128((int*)Unsafe.AsPointer(ref Unsafe.Add(ref c, 0)));
                var vcoeff1 = Sse2.LoadVector128((int*)Unsafe.AsPointer(ref Unsafe.Add(ref c, 4)));
                var vcoeff2 = Sse2.LoadVector128((int*)Unsafe.AsPointer(ref Unsafe.Add(ref c, 8)));
                var vcoeff3 = Sse2.LoadVector128((int*)Unsafe.AsPointer(ref Unsafe.Add(ref c, 12)));
                var vcoeff4 = Sse2.LoadVector128((int*)Unsafe.AsPointer(ref Unsafe.Add(ref c, 16)));
                var vcoeff5 = Sse2.LoadVector128((int*)Unsafe.AsPointer(ref Unsafe.Add(ref c, 20)));
                var vcoeff6 = Vector128.Create(Unsafe.Add(ref c, 24), Unsafe.Add(ref c, 25), Unsafe.Add(ref c, 26), 0);
                var vprev0 = Sse2.Shuffle(Sse2.LoadVector128((int*)Unsafe.AsPointer(ref Unsafe.Add(ref o, 23))).AsInt32(), 0b00_01_10_11);
                var vprev1 = Sse2.Shuffle(Sse2.LoadVector128((int*)Unsafe.AsPointer(ref Unsafe.Add(ref o, 19))).AsInt32(), 0b00_01_10_11);
                var vprev2 = Sse2.Shuffle(Sse2.LoadVector128((int*)Unsafe.AsPointer(ref Unsafe.Add(ref o, 15))).AsInt32(), 0b00_01_10_11);
                var vprev3 = Sse2.Shuffle(Sse2.LoadVector128((int*)Unsafe.AsPointer(ref Unsafe.Add(ref o, 11))).AsInt32(), 0b00_01_10_11);
                var vprev4 = Sse2.Shuffle(Sse2.LoadVector128((int*)Unsafe.AsPointer(ref Unsafe.Add(ref o, 7))).AsInt32(), 0b00_01_10_11);
                var vprev5 = Sse2.Shuffle(Sse2.LoadVector128((int*)Unsafe.AsPointer(ref Unsafe.Add(ref o, 3))).AsInt32(), 0b00_01_10_11);
                var vprev6 = Vector128.Create(Unsafe.Add(ref o, 2), Unsafe.Add(ref o, 1), Unsafe.Add(ref o, 0), 0);
                for (nint i = 0; i < dataLength; i++)
                {
                    var res = Vector128.CreateScalar(Unsafe.Add(ref r, i));
                    sum = Sse41.MultiplyLow(vcoeff0, vprev0);
                    sum = Sse2.Add(sum, Sse41.MultiplyLow(vcoeff1, vprev1));
                    sum = Sse2.Add(sum, Sse41.MultiplyLow(vcoeff2, vprev2));
                    sum = Sse2.Add(sum, Sse41.MultiplyLow(vcoeff3, vprev3));
                    sum = Sse2.Add(sum, Sse41.MultiplyLow(vcoeff4, vprev4));
                    sum = Sse2.Add(sum, Sse41.MultiplyLow(vcoeff5, vprev5));
                    sum = Sse2.Add(sum, Sse41.MultiplyLow(vcoeff6, vprev6));
                    var y = Ssse3.HorizontalAdd(sum, vzero);
                    y = Ssse3.HorizontalAdd(y, vzero);
                    y = Sse2.ShiftRightArithmetic(y, vshift);   //C# shift operator handling sucks so SSE2 is used instead(same latency, same throughput).
                    y = Sse2.Add(y, res);   //Avoids extract and insert
#if NET5_0_OR_GREATER
                    Sse2.StoreScalar((int*)Unsafe.AsPointer(ref Unsafe.Add(ref d, i)), y);
#else
                    Unsafe.Add(ref d, i) = y.GetElement(0);
#endif
                    y = Sse2.ShiftLeftLogical128BitLane(y, 12);
                    vprev6 = Ssse3.AlignRight(vprev6, vprev5, 12);
                    vprev5 = Ssse3.AlignRight(vprev5, vprev4, 12);
                    vprev4 = Ssse3.AlignRight(vprev4, vprev3, 12);
                    vprev3 = Ssse3.AlignRight(vprev3, vprev2, 12);
                    vprev2 = Ssse3.AlignRight(vprev2, vprev1, 12);
                    vprev1 = Ssse3.AlignRight(vprev1, vprev0, 12);
                    vprev0 = Ssse3.AlignRight(vprev0, y, 12);
                }
            }

            [MethodImpl(OptimizationUtils.InlineAndOptimizeIfPossible)]
            internal static bool RestoreSignalOrder27Wide(int shiftsNeeded, ReadOnlySpan<int> residual, ReadOnlySpan<int> coeffs, Span<int> output)
            {
                if (Sse41.IsSupported)
                {
                    RestoreSignalOrder27WideSse41(shiftsNeeded, residual, coeffs, output);
                    return true;
                }
                return false;
            }

            [MethodImpl(OptimizationUtils.InlineAndOptimizeIfPossible)]
            internal static unsafe void RestoreSignalOrder27WideSse41(int shiftsNeeded, ReadOnlySpan<int> residual, ReadOnlySpan<int> coeffs, Span<int> output)
            {
                const int Order = 27;
                if(coeffs.Length < Order) return;
                _ = coeffs[Order - 1];
                var vshift = Vector128.CreateScalar((long)shiftsNeeded);
                ref var c = ref MemoryMarshal.GetReference(coeffs);
                ref var o = ref MemoryMarshal.GetReference(output);
                ref var d = ref Unsafe.Add(ref o, Order);
                int dataLength = output.Length - Order;
                ref var r = ref MemoryMarshal.GetReference(residual);
                Vector128<long> sum;
                var vcoeff0 = Sse41.ConvertToVector128Int64(Sse2.LoadScalarVector128((ulong*)Unsafe.AsPointer(ref Unsafe.Add(ref c, 0))).AsUInt32()).AsInt32();
                var vcoeff1 = Sse41.ConvertToVector128Int64(Sse2.LoadScalarVector128((ulong*)Unsafe.AsPointer(ref Unsafe.Add(ref c, 2))).AsUInt32()).AsInt32();
                var vcoeff2 = Sse41.ConvertToVector128Int64(Sse2.LoadScalarVector128((ulong*)Unsafe.AsPointer(ref Unsafe.Add(ref c, 4))).AsUInt32()).AsInt32();
                var vcoeff3 = Sse41.ConvertToVector128Int64(Sse2.LoadScalarVector128((ulong*)Unsafe.AsPointer(ref Unsafe.Add(ref c, 6))).AsUInt32()).AsInt32();
                var vcoeff4 = Sse41.ConvertToVector128Int64(Sse2.LoadScalarVector128((ulong*)Unsafe.AsPointer(ref Unsafe.Add(ref c, 8))).AsUInt32()).AsInt32();
                var vcoeff5 = Sse41.ConvertToVector128Int64(Sse2.LoadScalarVector128((ulong*)Unsafe.AsPointer(ref Unsafe.Add(ref c, 10))).AsUInt32()).AsInt32();
                var vcoeff6 = Sse41.ConvertToVector128Int64(Sse2.LoadScalarVector128((ulong*)Unsafe.AsPointer(ref Unsafe.Add(ref c, 12))).AsUInt32()).AsInt32();
                var vcoeff7 = Sse41.ConvertToVector128Int64(Sse2.LoadScalarVector128((ulong*)Unsafe.AsPointer(ref Unsafe.Add(ref c, 14))).AsUInt32()).AsInt32();
                var vcoeff8 = Sse41.ConvertToVector128Int64(Sse2.LoadScalarVector128((ulong*)Unsafe.AsPointer(ref Unsafe.Add(ref c, 16))).AsUInt32()).AsInt32();
                var vcoeff9 = Sse41.ConvertToVector128Int64(Sse2.LoadScalarVector128((ulong*)Unsafe.AsPointer(ref Unsafe.Add(ref c, 18))).AsUInt32()).AsInt32();
                var vcoeff10 = Sse41.ConvertToVector128Int64(Sse2.LoadScalarVector128((ulong*)Unsafe.AsPointer(ref Unsafe.Add(ref c, 20))).AsUInt32()).AsInt32();
                var vcoeff11 = Sse41.ConvertToVector128Int64(Sse2.LoadScalarVector128((ulong*)Unsafe.AsPointer(ref Unsafe.Add(ref c, 22))).AsUInt32()).AsInt32();
                var vcoeff12 = Sse41.ConvertToVector128Int64(Sse2.LoadScalarVector128((ulong*)Unsafe.AsPointer(ref Unsafe.Add(ref c, 24))).AsUInt32()).AsInt32();
                var vcoeff13 = Sse41.ConvertToVector128Int64(Sse2.LoadScalarVector128((uint*)Unsafe.AsPointer(ref Unsafe.Add(ref c, 26))).AsUInt32()).AsInt32();
                var vprev0 = Sse2.Shuffle(Sse2.LoadScalarVector128((ulong*)Unsafe.AsPointer(ref Unsafe.Add(ref o, 25))).AsInt32(), 0b11_00_11_01);
                var vprev1 = Sse2.Shuffle(Sse2.LoadScalarVector128((ulong*)Unsafe.AsPointer(ref Unsafe.Add(ref o, 23))).AsInt32(), 0b11_00_11_01);
                var vprev2 = Sse2.Shuffle(Sse2.LoadScalarVector128((ulong*)Unsafe.AsPointer(ref Unsafe.Add(ref o, 21))).AsInt32(), 0b11_00_11_01);
                var vprev3 = Sse2.Shuffle(Sse2.LoadScalarVector128((ulong*)Unsafe.AsPointer(ref Unsafe.Add(ref o, 19))).AsInt32(), 0b11_00_11_01);
                var vprev4 = Sse2.Shuffle(Sse2.LoadScalarVector128((ulong*)Unsafe.AsPointer(ref Unsafe.Add(ref o, 17))).AsInt32(), 0b11_00_11_01);
                var vprev5 = Sse2.Shuffle(Sse2.LoadScalarVector128((ulong*)Unsafe.AsPointer(ref Unsafe.Add(ref o, 15))).AsInt32(), 0b11_00_11_01);
                var vprev6 = Sse2.Shuffle(Sse2.LoadScalarVector128((ulong*)Unsafe.AsPointer(ref Unsafe.Add(ref o, 13))).AsInt32(), 0b11_00_11_01);
                var vprev7 = Sse2.Shuffle(Sse2.LoadScalarVector128((ulong*)Unsafe.AsPointer(ref Unsafe.Add(ref o, 11))).AsInt32(), 0b11_00_11_01);
                var vprev8 = Sse2.Shuffle(Sse2.LoadScalarVector128((ulong*)Unsafe.AsPointer(ref Unsafe.Add(ref o, 9))).AsInt32(), 0b11_00_11_01);
                var vprev9 = Sse2.Shuffle(Sse2.LoadScalarVector128((ulong*)Unsafe.AsPointer(ref Unsafe.Add(ref o, 7))).AsInt32(), 0b11_00_11_01);
                var vprev10 = Sse2.Shuffle(Sse2.LoadScalarVector128((ulong*)Unsafe.AsPointer(ref Unsafe.Add(ref o, 5))).AsInt32(), 0b11_00_11_01);
                var vprev11 = Sse2.Shuffle(Sse2.LoadScalarVector128((ulong*)Unsafe.AsPointer(ref Unsafe.Add(ref o, 3))).AsInt32(), 0b11_00_11_01);
                var vprev12 = Sse2.Shuffle(Sse2.LoadScalarVector128((ulong*)Unsafe.AsPointer(ref Unsafe.Add(ref o, 1))).AsInt32(), 0b11_00_11_01);
                var vprev13 = Sse2.Shuffle(Sse2.LoadScalarVector128((ulong*)Unsafe.AsPointer(ref Unsafe.Add(ref o, 0))).AsInt32(), 0b11_00_11_00);
                for(nint i = 0; i < dataLength;)
                {
                    var res = Vector128.CreateScalar(Unsafe.Add(ref r, i));
                    sum = Sse41.Multiply(vcoeff0, vprev0);
                    sum = Sse2.Add(sum, Sse41.Multiply(vcoeff1, vprev1));
                    sum = Sse2.Add(sum, Sse41.Multiply(vcoeff2, vprev2));
                    sum = Sse2.Add(sum, Sse41.Multiply(vcoeff3, vprev3));
                    sum = Sse2.Add(sum, Sse41.Multiply(vcoeff4, vprev4));
                    sum = Sse2.Add(sum, Sse41.Multiply(vcoeff5, vprev5));
                    sum = Sse2.Add(sum, Sse41.Multiply(vcoeff6, vprev6));
                    sum = Sse2.Add(sum, Sse41.Multiply(vcoeff7, vprev7));
                    sum = Sse2.Add(sum, Sse41.Multiply(vcoeff8, vprev8));
                    sum = Sse2.Add(sum, Sse41.Multiply(vcoeff9, vprev9));
                    sum = Sse2.Add(sum, Sse41.Multiply(vcoeff10, vprev10));
                    sum = Sse2.Add(sum, Sse41.Multiply(vcoeff11, vprev11));
                    sum = Sse2.Add(sum, Sse41.Multiply(vcoeff12, vprev12));
                    sum = Sse2.Add(sum, Sse41.Multiply(vcoeff13, vprev13));
                    sum = Sse2.Add(sum, Sse2.ShiftRightLogical128BitLane(sum, 8));
                    sum = Sse2.ShiftRightLogical(sum, vshift);
                    var yy = Sse2.Add(sum.AsInt32(), res);
#if NET5_0_OR_GREATER
                    Sse2.StoreScalar((int*)Unsafe.AsPointer(ref Unsafe.Add(ref d, i)), yy);
#else
                    Unsafe.Add(ref d, i) = yy.GetElement(0);
#endif
                    i++;
                    yy = Sse2.ShiftLeftLogical128BitLane(yy, 8);
                    vprev13 = Ssse3.AlignRight(vprev13, vprev12, 8);
                    vprev12 = Ssse3.AlignRight(vprev12, vprev11, 8);
                    vprev11 = Ssse3.AlignRight(vprev11, vprev10, 8);
                    vprev10 = Ssse3.AlignRight(vprev10, vprev9, 8);
                    vprev9 = Ssse3.AlignRight(vprev9, vprev8, 8);
                    vprev8 = Ssse3.AlignRight(vprev8, vprev7, 8);
                    vprev7 = Ssse3.AlignRight(vprev7, vprev6, 8);
                    vprev6 = Ssse3.AlignRight(vprev6, vprev5, 8);
                    vprev5 = Ssse3.AlignRight(vprev5, vprev4, 8);
                    vprev4 = Ssse3.AlignRight(vprev4, vprev3, 8);
                    vprev3 = Ssse3.AlignRight(vprev3, vprev2, 8);
                    vprev2 = Ssse3.AlignRight(vprev2, vprev1, 8);
                    vprev1 = Ssse3.AlignRight(vprev1, vprev0, 8);
                    vprev0 = Ssse3.AlignRight(vprev0, yy, 8);
                }
            }
            
            [MethodImpl(OptimizationUtils.InlineAndOptimizeIfPossible)]
            internal static unsafe void RestoreSignalOrder27WideAvx2(int shiftsNeeded, ReadOnlySpan<int> residual, ReadOnlySpan<int> coeffs, Span<int> output)
            {
                const int Order = 27;
                if(coeffs.Length < Order) return;
                _ = coeffs[Order - 1];
                var vshift = Vector128.CreateScalar((long)shiftsNeeded);
                var mask = Vector128.Create(~0, ~0, ~0, 0);
                ref var c = ref MemoryMarshal.GetReference(coeffs);
                ref var o = ref MemoryMarshal.GetReference(output);
                ref var d = ref Unsafe.Add(ref o, Order);
                int dataLength = output.Length - Order;
                ref var r = ref MemoryMarshal.GetReference(residual);
                Vector128<long> sum;
                Vector256<long> sum256;
                var vcoeff0 = Avx2.ConvertToVector256Int64(Sse2.LoadVector128((int*)Unsafe.AsPointer(ref Unsafe.Add(ref c, 0))).AsUInt32()).AsInt32();
                var vcoeff1 = Avx2.ConvertToVector256Int64(Sse2.LoadVector128((int*)Unsafe.AsPointer(ref Unsafe.Add(ref c, 4))).AsUInt32()).AsInt32();
                var vcoeff2 = Avx2.ConvertToVector256Int64(Sse2.LoadVector128((int*)Unsafe.AsPointer(ref Unsafe.Add(ref c, 8))).AsUInt32()).AsInt32();
                var vcoeff3 = Avx2.ConvertToVector256Int64(Sse2.LoadVector128((int*)Unsafe.AsPointer(ref Unsafe.Add(ref c, 12))).AsUInt32()).AsInt32();
                var vcoeff4 = Avx2.ConvertToVector256Int64(Sse2.LoadVector128((int*)Unsafe.AsPointer(ref Unsafe.Add(ref c, 16))).AsUInt32()).AsInt32();
                var vcoeff5 = Avx2.ConvertToVector256Int64(Sse2.LoadVector128((int*)Unsafe.AsPointer(ref Unsafe.Add(ref c, 20))).AsUInt32()).AsInt32();
                var vcoeff6 = Avx2.ConvertToVector256Int64(Avx2.MaskLoad((int*)Unsafe.AsPointer(ref Unsafe.Add(ref c, 24)), mask)).AsInt32();
                var vprev0 = Avx2.ConvertToVector256Int64(Sse2.Shuffle(Sse2.LoadScalarVector128((ulong*)Unsafe.AsPointer(ref Unsafe.Add(ref o, 24))).AsInt32(), 0b00_01_10_11).AsUInt32()).AsInt32();
                var vprev1 = Avx2.ConvertToVector256Int64(Sse2.Shuffle(Sse2.LoadScalarVector128((ulong*)Unsafe.AsPointer(ref Unsafe.Add(ref o, 20))).AsInt32(), 0b00_01_10_11).AsUInt32()).AsInt32();
                var vprev2 = Avx2.ConvertToVector256Int64(Sse2.Shuffle(Sse2.LoadScalarVector128((ulong*)Unsafe.AsPointer(ref Unsafe.Add(ref o, 16))).AsInt32(), 0b00_01_10_11).AsUInt32()).AsInt32();
                var vprev3 = Avx2.ConvertToVector256Int64(Sse2.Shuffle(Sse2.LoadScalarVector128((ulong*)Unsafe.AsPointer(ref Unsafe.Add(ref o, 12))).AsInt32(), 0b00_01_10_11).AsUInt32()).AsInt32();
                var vprev4 = Avx2.ConvertToVector256Int64(Sse2.Shuffle(Sse2.LoadScalarVector128((ulong*)Unsafe.AsPointer(ref Unsafe.Add(ref o, 8))).AsInt32(), 0b00_01_10_11).AsUInt32()).AsInt32();
                var vprev5 = Avx2.ConvertToVector256Int64(Sse2.Shuffle(Sse2.LoadScalarVector128((ulong*)Unsafe.AsPointer(ref Unsafe.Add(ref o, 4))).AsInt32(), 0b00_01_10_11).AsUInt32()).AsInt32();
                var vprev6 = Avx2.ConvertToVector256Int64(Sse2.Shuffle(Avx2.MaskLoad((int*)Unsafe.AsPointer(ref o), mask).AsInt32(), 0b00_01_10_11)).AsInt32();
                for(nint i = 0; i < dataLength;)
                {
                    var res = Vector128.CreateScalar(Unsafe.Add(ref r, i));
                    sum256 = Avx2.Multiply(vcoeff0, vprev0);
                    sum256 = Avx2.Add(sum256, Avx2.Multiply(vcoeff1, vprev1));
                    sum256 = Avx2.Add(sum256, Avx2.Multiply(vcoeff2, vprev2));
                    sum256 = Avx2.Add(sum256, Avx2.Multiply(vcoeff3, vprev3));
                    sum256 = Avx2.Add(sum256, Avx2.Multiply(vcoeff4, vprev4));
                    sum256 = Avx2.Add(sum256, Avx2.Multiply(vcoeff5, vprev5));
                    sum256 = Avx2.Add(sum256, Avx2.Multiply(vcoeff6, vprev6));
                    sum = Sse2.Add(sum256.GetLower(), sum256.GetUpper());
                    sum = Sse2.Add(sum, Sse2.ShiftRightLogical128BitLane(sum, 8));
                    sum = Sse2.ShiftRightLogical(sum, vshift);
                    var yy = Sse2.Add(sum.AsInt32(), res);
#if NET5_0_OR_GREATER
                    Sse2.StoreScalar((int*)Unsafe.AsPointer(ref Unsafe.Add(ref d, i)), yy);
#else
                    Unsafe.Add(ref d, i) = yy.GetElement(0);
#endif
                    i++;
                    var yu = yy.ToVector256();
                    yu = Avx2.Permute4x64(yu.AsUInt64(), 0b00_00_00_00).AsInt32();
                    vprev6 = Avx2.Permute4x64(vprev6.AsInt64(), 0b10_01_00_11).AsInt32();
                    vprev6 = Avx2.Blend(vprev6, vprev5, 0b0000_0001);
                    vprev5 = Avx2.Permute4x64(vprev5.AsInt64(), 0b10_01_00_11).AsInt32();
                    vprev5 = Avx2.Blend(vprev5, vprev4, 0b0000_0001);
                    vprev4 = Avx2.Permute4x64(vprev4.AsInt64(), 0b10_01_00_11).AsInt32();
                    vprev4 = Avx2.Blend(vprev4, vprev3, 0b0000_0001);
                    vprev3 = Avx2.Permute4x64(vprev3.AsInt64(), 0b10_01_00_11).AsInt32();
                    vprev3 = Avx2.Blend(vprev3, vprev2, 0b0000_0001);
                    vprev2 = Avx2.Permute4x64(vprev2.AsInt64(), 0b10_01_00_11).AsInt32();
                    vprev2 = Avx2.Blend(vprev2, vprev1, 0b0000_0001);
                    vprev1 = Avx2.Permute4x64(vprev1.AsInt64(), 0b10_01_00_11).AsInt32();
                    vprev1 = Avx2.Blend(vprev1, vprev0, 0b0000_0001);
                    vprev0 = Avx2.Permute4x64(vprev0.AsInt64(), 0b10_01_00_11).AsInt32();
                    vprev0 = Avx2.Blend(vprev0, yu, 0b0000_0001);
                }
            }
#endregion Order27
#region Order28

            [MethodImpl(OptimizationUtils.InlineAndOptimizeIfPossible)]
            internal static bool RestoreSignalOrder28(int shiftsNeeded, ReadOnlySpan<int> residual, ReadOnlySpan<int> coeffs, Span<int> output)
            {
                if (Sse41.IsSupported)
                {
                    RestoreSignalOrder28Sse41(shiftsNeeded, residual, coeffs, output);
                    return true;
                }
                return false;
            }

            [MethodImpl(OptimizationUtils.InlineAndOptimizeIfPossible)]
            internal static unsafe void RestoreSignalOrder28Sse41(int shiftsNeeded, ReadOnlySpan<int> residual, ReadOnlySpan<int> coeffs, Span<int> output)
            {
                const int Order = 28;
                if (coeffs.Length < Order) return;
                _ = coeffs[Order - 1];
                var vshift = Vector128.CreateScalar(shiftsNeeded);
                ref var o = ref MemoryMarshal.GetReference(output);
                ref var d = ref Unsafe.Add(ref o, Order);
                ref var r = ref MemoryMarshal.GetReference(residual);
                ref var c = ref MemoryMarshal.GetReference(coeffs);
                Vector128<int> sum;
                var vzero = Vector128.Create(0);
                nint dataLength = output.Length - Order;
                var vcoeff0 = Sse2.LoadVector128((int*)Unsafe.AsPointer(ref Unsafe.Add(ref c, 0)));
                var vcoeff1 = Sse2.LoadVector128((int*)Unsafe.AsPointer(ref Unsafe.Add(ref c, 4)));
                var vcoeff2 = Sse2.LoadVector128((int*)Unsafe.AsPointer(ref Unsafe.Add(ref c, 8)));
                var vcoeff3 = Sse2.LoadVector128((int*)Unsafe.AsPointer(ref Unsafe.Add(ref c, 12)));
                var vcoeff4 = Sse2.LoadVector128((int*)Unsafe.AsPointer(ref Unsafe.Add(ref c, 16)));
                var vcoeff5 = Sse2.LoadVector128((int*)Unsafe.AsPointer(ref Unsafe.Add(ref c, 20)));
                var vcoeff6 = Sse2.LoadVector128((int*)Unsafe.AsPointer(ref Unsafe.Add(ref c, 24)));
                var vprev0 = Sse2.Shuffle(Sse2.LoadVector128((int*)Unsafe.AsPointer(ref Unsafe.Add(ref o, 24))).AsInt32(), 0b00_01_10_11);
                var vprev1 = Sse2.Shuffle(Sse2.LoadVector128((int*)Unsafe.AsPointer(ref Unsafe.Add(ref o, 20))).AsInt32(), 0b00_01_10_11);
                var vprev2 = Sse2.Shuffle(Sse2.LoadVector128((int*)Unsafe.AsPointer(ref Unsafe.Add(ref o, 16))).AsInt32(), 0b00_01_10_11);
                var vprev3 = Sse2.Shuffle(Sse2.LoadVector128((int*)Unsafe.AsPointer(ref Unsafe.Add(ref o, 12))).AsInt32(), 0b00_01_10_11);
                var vprev4 = Sse2.Shuffle(Sse2.LoadVector128((int*)Unsafe.AsPointer(ref Unsafe.Add(ref o, 8))).AsInt32(), 0b00_01_10_11);
                var vprev5 = Sse2.Shuffle(Sse2.LoadVector128((int*)Unsafe.AsPointer(ref Unsafe.Add(ref o, 4))).AsInt32(), 0b00_01_10_11);
                var vprev6 = Sse2.Shuffle(Sse2.LoadVector128((int*)Unsafe.AsPointer(ref Unsafe.Add(ref o, 0))).AsInt32(), 0b00_01_10_11);
                var vprev7 = Vector128.Create(0, 0, 0, 0);
                for (nint i = 0; i < dataLength; i++)
                {
                    var res = Vector128.CreateScalar(Unsafe.Add(ref r, i));
                    sum = Sse41.MultiplyLow(vcoeff0, vprev0);
                    sum = Sse2.Add(sum, Sse41.MultiplyLow(vcoeff1, vprev1));
                    sum = Sse2.Add(sum, Sse41.MultiplyLow(vcoeff2, vprev2));
                    sum = Sse2.Add(sum, Sse41.MultiplyLow(vcoeff3, vprev3));
                    sum = Sse2.Add(sum, Sse41.MultiplyLow(vcoeff4, vprev4));
                    sum = Sse2.Add(sum, Sse41.MultiplyLow(vcoeff5, vprev5));
                    sum = Sse2.Add(sum, Sse41.MultiplyLow(vcoeff6, vprev6));
                    var y = Ssse3.HorizontalAdd(sum, vzero);
                    y = Ssse3.HorizontalAdd(y, vzero);
                    y = Sse2.ShiftRightArithmetic(y, vshift);   //C# shift operator handling sucks so SSE2 is used instead(same latency, same throughput).
                    y = Sse2.Add(y, res);   //Avoids extract and insert
#if NET5_0_OR_GREATER
                    Sse2.StoreScalar((int*)Unsafe.AsPointer(ref Unsafe.Add(ref d, i)), y);
#else
                    Unsafe.Add(ref d, i) = y.GetElement(0);
#endif
                    y = Sse2.ShiftLeftLogical128BitLane(y, 12);
                    vprev6 = Ssse3.AlignRight(vprev6, vprev5, 12);
                    vprev5 = Ssse3.AlignRight(vprev5, vprev4, 12);
                    vprev4 = Ssse3.AlignRight(vprev4, vprev3, 12);
                    vprev3 = Ssse3.AlignRight(vprev3, vprev2, 12);
                    vprev2 = Ssse3.AlignRight(vprev2, vprev1, 12);
                    vprev1 = Ssse3.AlignRight(vprev1, vprev0, 12);
                    vprev0 = Ssse3.AlignRight(vprev0, y, 12);
                }
            }

            [MethodImpl(OptimizationUtils.InlineAndOptimizeIfPossible)]
            internal static bool RestoreSignalOrder28Wide(int shiftsNeeded, ReadOnlySpan<int> residual, ReadOnlySpan<int> coeffs, Span<int> output)
            {
                if (Sse41.IsSupported)
                {
                    RestoreSignalOrder28WideSse41(shiftsNeeded, residual, coeffs, output);
                    return true;
                }
                return false;
            }

            [MethodImpl(OptimizationUtils.InlineAndOptimizeIfPossible)]
            internal static unsafe void RestoreSignalOrder28WideSse41(int shiftsNeeded, ReadOnlySpan<int> residual, ReadOnlySpan<int> coeffs, Span<int> output)
            {
                const int Order = 28;
                if(coeffs.Length < Order) return;
                _ = coeffs[Order - 1];
                var vshift = Vector128.CreateScalar((long)shiftsNeeded);
                ref var c = ref MemoryMarshal.GetReference(coeffs);
                ref var o = ref MemoryMarshal.GetReference(output);
                ref var d = ref Unsafe.Add(ref o, Order);
                int dataLength = output.Length - Order;
                ref var r = ref MemoryMarshal.GetReference(residual);
                Vector128<long> sum;
                var vcoeff0 = Sse41.ConvertToVector128Int64(Sse2.LoadScalarVector128((ulong*)Unsafe.AsPointer(ref Unsafe.Add(ref c, 0))).AsUInt32()).AsInt32();
                var vcoeff1 = Sse41.ConvertToVector128Int64(Sse2.LoadScalarVector128((ulong*)Unsafe.AsPointer(ref Unsafe.Add(ref c, 2))).AsUInt32()).AsInt32();
                var vcoeff2 = Sse41.ConvertToVector128Int64(Sse2.LoadScalarVector128((ulong*)Unsafe.AsPointer(ref Unsafe.Add(ref c, 4))).AsUInt32()).AsInt32();
                var vcoeff3 = Sse41.ConvertToVector128Int64(Sse2.LoadScalarVector128((ulong*)Unsafe.AsPointer(ref Unsafe.Add(ref c, 6))).AsUInt32()).AsInt32();
                var vcoeff4 = Sse41.ConvertToVector128Int64(Sse2.LoadScalarVector128((ulong*)Unsafe.AsPointer(ref Unsafe.Add(ref c, 8))).AsUInt32()).AsInt32();
                var vcoeff5 = Sse41.ConvertToVector128Int64(Sse2.LoadScalarVector128((ulong*)Unsafe.AsPointer(ref Unsafe.Add(ref c, 10))).AsUInt32()).AsInt32();
                var vcoeff6 = Sse41.ConvertToVector128Int64(Sse2.LoadScalarVector128((ulong*)Unsafe.AsPointer(ref Unsafe.Add(ref c, 12))).AsUInt32()).AsInt32();
                var vcoeff7 = Sse41.ConvertToVector128Int64(Sse2.LoadScalarVector128((ulong*)Unsafe.AsPointer(ref Unsafe.Add(ref c, 14))).AsUInt32()).AsInt32();
                var vcoeff8 = Sse41.ConvertToVector128Int64(Sse2.LoadScalarVector128((ulong*)Unsafe.AsPointer(ref Unsafe.Add(ref c, 16))).AsUInt32()).AsInt32();
                var vcoeff9 = Sse41.ConvertToVector128Int64(Sse2.LoadScalarVector128((ulong*)Unsafe.AsPointer(ref Unsafe.Add(ref c, 18))).AsUInt32()).AsInt32();
                var vcoeff10 = Sse41.ConvertToVector128Int64(Sse2.LoadScalarVector128((ulong*)Unsafe.AsPointer(ref Unsafe.Add(ref c, 20))).AsUInt32()).AsInt32();
                var vcoeff11 = Sse41.ConvertToVector128Int64(Sse2.LoadScalarVector128((ulong*)Unsafe.AsPointer(ref Unsafe.Add(ref c, 22))).AsUInt32()).AsInt32();
                var vcoeff12 = Sse41.ConvertToVector128Int64(Sse2.LoadScalarVector128((ulong*)Unsafe.AsPointer(ref Unsafe.Add(ref c, 24))).AsUInt32()).AsInt32();
                var vcoeff13 = Sse41.ConvertToVector128Int64(Sse2.LoadScalarVector128((ulong*)Unsafe.AsPointer(ref Unsafe.Add(ref c, 26))).AsUInt32()).AsInt32();
                var vprev0 = Sse2.Shuffle(Sse2.LoadScalarVector128((ulong*)Unsafe.AsPointer(ref Unsafe.Add(ref o, 26))).AsInt32(), 0b11_00_11_01);
                var vprev1 = Sse2.Shuffle(Sse2.LoadScalarVector128((ulong*)Unsafe.AsPointer(ref Unsafe.Add(ref o, 24))).AsInt32(), 0b11_00_11_01);
                var vprev2 = Sse2.Shuffle(Sse2.LoadScalarVector128((ulong*)Unsafe.AsPointer(ref Unsafe.Add(ref o, 22))).AsInt32(), 0b11_00_11_01);
                var vprev3 = Sse2.Shuffle(Sse2.LoadScalarVector128((ulong*)Unsafe.AsPointer(ref Unsafe.Add(ref o, 20))).AsInt32(), 0b11_00_11_01);
                var vprev4 = Sse2.Shuffle(Sse2.LoadScalarVector128((ulong*)Unsafe.AsPointer(ref Unsafe.Add(ref o, 18))).AsInt32(), 0b11_00_11_01);
                var vprev5 = Sse2.Shuffle(Sse2.LoadScalarVector128((ulong*)Unsafe.AsPointer(ref Unsafe.Add(ref o, 16))).AsInt32(), 0b11_00_11_01);
                var vprev6 = Sse2.Shuffle(Sse2.LoadScalarVector128((ulong*)Unsafe.AsPointer(ref Unsafe.Add(ref o, 14))).AsInt32(), 0b11_00_11_01);
                var vprev7 = Sse2.Shuffle(Sse2.LoadScalarVector128((ulong*)Unsafe.AsPointer(ref Unsafe.Add(ref o, 12))).AsInt32(), 0b11_00_11_01);
                var vprev8 = Sse2.Shuffle(Sse2.LoadScalarVector128((ulong*)Unsafe.AsPointer(ref Unsafe.Add(ref o, 10))).AsInt32(), 0b11_00_11_01);
                var vprev9 = Sse2.Shuffle(Sse2.LoadScalarVector128((ulong*)Unsafe.AsPointer(ref Unsafe.Add(ref o, 8))).AsInt32(), 0b11_00_11_01);
                var vprev10 = Sse2.Shuffle(Sse2.LoadScalarVector128((ulong*)Unsafe.AsPointer(ref Unsafe.Add(ref o, 6))).AsInt32(), 0b11_00_11_01);
                var vprev11 = Sse2.Shuffle(Sse2.LoadScalarVector128((ulong*)Unsafe.AsPointer(ref Unsafe.Add(ref o, 4))).AsInt32(), 0b11_00_11_01);
                var vprev12 = Sse2.Shuffle(Sse2.LoadScalarVector128((ulong*)Unsafe.AsPointer(ref Unsafe.Add(ref o, 2))).AsInt32(), 0b11_00_11_01);
                var vprev13 = Sse2.Shuffle(Sse2.LoadScalarVector128((ulong*)Unsafe.AsPointer(ref Unsafe.Add(ref o, 0))).AsInt32(), 0b11_00_11_01);
                for(nint i = 0; i < dataLength;)
                {
                    var res = Vector128.CreateScalar(Unsafe.Add(ref r, i));
                    sum = Sse41.Multiply(vcoeff0, vprev0);
                    sum = Sse2.Add(sum, Sse41.Multiply(vcoeff1, vprev1));
                    sum = Sse2.Add(sum, Sse41.Multiply(vcoeff2, vprev2));
                    sum = Sse2.Add(sum, Sse41.Multiply(vcoeff3, vprev3));
                    sum = Sse2.Add(sum, Sse41.Multiply(vcoeff4, vprev4));
                    sum = Sse2.Add(sum, Sse41.Multiply(vcoeff5, vprev5));
                    sum = Sse2.Add(sum, Sse41.Multiply(vcoeff6, vprev6));
                    sum = Sse2.Add(sum, Sse41.Multiply(vcoeff7, vprev7));
                    sum = Sse2.Add(sum, Sse41.Multiply(vcoeff8, vprev8));
                    sum = Sse2.Add(sum, Sse41.Multiply(vcoeff9, vprev9));
                    sum = Sse2.Add(sum, Sse41.Multiply(vcoeff10, vprev10));
                    sum = Sse2.Add(sum, Sse41.Multiply(vcoeff11, vprev11));
                    sum = Sse2.Add(sum, Sse41.Multiply(vcoeff12, vprev12));
                    sum = Sse2.Add(sum, Sse41.Multiply(vcoeff13, vprev13));
                    sum = Sse2.Add(sum, Sse2.ShiftRightLogical128BitLane(sum, 8));
                    sum = Sse2.ShiftRightLogical(sum, vshift);
                    var yy = Sse2.Add(sum.AsInt32(), res);
#if NET5_0_OR_GREATER
                    Sse2.StoreScalar((int*)Unsafe.AsPointer(ref Unsafe.Add(ref d, i)), yy);
#else
                    Unsafe.Add(ref d, i) = yy.GetElement(0);
#endif
                    i++;
                    yy = Sse2.ShiftLeftLogical128BitLane(yy, 8);
                    vprev13 = Ssse3.AlignRight(vprev13, vprev12, 8);
                    vprev12 = Ssse3.AlignRight(vprev12, vprev11, 8);
                    vprev11 = Ssse3.AlignRight(vprev11, vprev10, 8);
                    vprev10 = Ssse3.AlignRight(vprev10, vprev9, 8);
                    vprev9 = Ssse3.AlignRight(vprev9, vprev8, 8);
                    vprev8 = Ssse3.AlignRight(vprev8, vprev7, 8);
                    vprev7 = Ssse3.AlignRight(vprev7, vprev6, 8);
                    vprev6 = Ssse3.AlignRight(vprev6, vprev5, 8);
                    vprev5 = Ssse3.AlignRight(vprev5, vprev4, 8);
                    vprev4 = Ssse3.AlignRight(vprev4, vprev3, 8);
                    vprev3 = Ssse3.AlignRight(vprev3, vprev2, 8);
                    vprev2 = Ssse3.AlignRight(vprev2, vprev1, 8);
                    vprev1 = Ssse3.AlignRight(vprev1, vprev0, 8);
                    vprev0 = Ssse3.AlignRight(vprev0, yy, 8);
                }
            }
            
            [MethodImpl(OptimizationUtils.InlineAndOptimizeIfPossible)]
            internal static unsafe void RestoreSignalOrder28WideAvx2(int shiftsNeeded, ReadOnlySpan<int> residual, ReadOnlySpan<int> coeffs, Span<int> output)
            {
                const int Order = 28;
                if(coeffs.Length < Order) return;
                _ = coeffs[Order - 1];
                var vshift = Vector128.CreateScalar((long)shiftsNeeded);
                ref var c = ref MemoryMarshal.GetReference(coeffs);
                ref var o = ref MemoryMarshal.GetReference(output);
                ref var d = ref Unsafe.Add(ref o, Order);
                int dataLength = output.Length - Order;
                ref var r = ref MemoryMarshal.GetReference(residual);
                Vector128<long> sum;
                Vector256<long> sum256;
                var vcoeff0 = Avx2.ConvertToVector256Int64(Sse2.LoadVector128((int*)Unsafe.AsPointer(ref Unsafe.Add(ref c, 0))).AsUInt32()).AsInt32();
                var vcoeff1 = Avx2.ConvertToVector256Int64(Sse2.LoadVector128((int*)Unsafe.AsPointer(ref Unsafe.Add(ref c, 4))).AsUInt32()).AsInt32();
                var vcoeff2 = Avx2.ConvertToVector256Int64(Sse2.LoadVector128((int*)Unsafe.AsPointer(ref Unsafe.Add(ref c, 8))).AsUInt32()).AsInt32();
                var vcoeff3 = Avx2.ConvertToVector256Int64(Sse2.LoadVector128((int*)Unsafe.AsPointer(ref Unsafe.Add(ref c, 12))).AsUInt32()).AsInt32();
                var vcoeff4 = Avx2.ConvertToVector256Int64(Sse2.LoadVector128((int*)Unsafe.AsPointer(ref Unsafe.Add(ref c, 16))).AsUInt32()).AsInt32();
                var vcoeff5 = Avx2.ConvertToVector256Int64(Sse2.LoadVector128((int*)Unsafe.AsPointer(ref Unsafe.Add(ref c, 20))).AsUInt32()).AsInt32();
                var vcoeff6 = Avx2.ConvertToVector256Int64(Sse2.LoadVector128((int*)Unsafe.AsPointer(ref Unsafe.Add(ref c, 24))).AsUInt32()).AsInt32();
                var vprev0 = Avx2.ConvertToVector256Int64(Sse2.Shuffle(Sse2.LoadScalarVector128((ulong*)Unsafe.AsPointer(ref Unsafe.Add(ref o, 24))).AsInt32(), 0b00_01_10_11).AsUInt32()).AsInt32();
                var vprev1 = Avx2.ConvertToVector256Int64(Sse2.Shuffle(Sse2.LoadScalarVector128((ulong*)Unsafe.AsPointer(ref Unsafe.Add(ref o, 20))).AsInt32(), 0b00_01_10_11).AsUInt32()).AsInt32();
                var vprev2 = Avx2.ConvertToVector256Int64(Sse2.Shuffle(Sse2.LoadScalarVector128((ulong*)Unsafe.AsPointer(ref Unsafe.Add(ref o, 16))).AsInt32(), 0b00_01_10_11).AsUInt32()).AsInt32();
                var vprev3 = Avx2.ConvertToVector256Int64(Sse2.Shuffle(Sse2.LoadScalarVector128((ulong*)Unsafe.AsPointer(ref Unsafe.Add(ref o, 12))).AsInt32(), 0b00_01_10_11).AsUInt32()).AsInt32();
                var vprev4 = Avx2.ConvertToVector256Int64(Sse2.Shuffle(Sse2.LoadScalarVector128((ulong*)Unsafe.AsPointer(ref Unsafe.Add(ref o, 8))).AsInt32(), 0b00_01_10_11).AsUInt32()).AsInt32();
                var vprev5 = Avx2.ConvertToVector256Int64(Sse2.Shuffle(Sse2.LoadScalarVector128((ulong*)Unsafe.AsPointer(ref Unsafe.Add(ref o, 4))).AsInt32(), 0b00_01_10_11).AsUInt32()).AsInt32();
                var vprev6 = Avx2.ConvertToVector256Int64(Sse2.Shuffle(Sse2.LoadScalarVector128((ulong*)Unsafe.AsPointer(ref Unsafe.Add(ref o, 0))).AsInt32(), 0b00_01_10_11).AsUInt32()).AsInt32();
                for(nint i = 0; i < dataLength;)
                {
                    var res = Vector128.CreateScalar(Unsafe.Add(ref r, i));
                    sum256 = Avx2.Multiply(vcoeff0, vprev0);
                    sum256 = Avx2.Add(sum256, Avx2.Multiply(vcoeff1, vprev1));
                    sum256 = Avx2.Add(sum256, Avx2.Multiply(vcoeff2, vprev2));
                    sum256 = Avx2.Add(sum256, Avx2.Multiply(vcoeff3, vprev3));
                    sum256 = Avx2.Add(sum256, Avx2.Multiply(vcoeff4, vprev4));
                    sum256 = Avx2.Add(sum256, Avx2.Multiply(vcoeff5, vprev5));
                    sum256 = Avx2.Add(sum256, Avx2.Multiply(vcoeff6, vprev6));
                    sum = Sse2.Add(sum256.GetLower(), sum256.GetUpper());
                    sum = Sse2.Add(sum, Sse2.ShiftRightLogical128BitLane(sum, 8));
                    sum = Sse2.ShiftRightLogical(sum, vshift);
                    var yy = Sse2.Add(sum.AsInt32(), res);
#if NET5_0_OR_GREATER
                    Sse2.StoreScalar((int*)Unsafe.AsPointer(ref Unsafe.Add(ref d, i)), yy);
#else
                    Unsafe.Add(ref d, i) = yy.GetElement(0);
#endif
                    i++;
                    var yu = yy.ToVector256();
                    yu = Avx2.Permute4x64(yu.AsUInt64(), 0b00_00_00_00).AsInt32();
                    vprev6 = Avx2.Permute4x64(vprev6.AsInt64(), 0b10_01_00_11).AsInt32();
                    vprev6 = Avx2.Blend(vprev6, vprev5, 0b0000_0001);
                    vprev5 = Avx2.Permute4x64(vprev5.AsInt64(), 0b10_01_00_11).AsInt32();
                    vprev5 = Avx2.Blend(vprev5, vprev4, 0b0000_0001);
                    vprev4 = Avx2.Permute4x64(vprev4.AsInt64(), 0b10_01_00_11).AsInt32();
                    vprev4 = Avx2.Blend(vprev4, vprev3, 0b0000_0001);
                    vprev3 = Avx2.Permute4x64(vprev3.AsInt64(), 0b10_01_00_11).AsInt32();
                    vprev3 = Avx2.Blend(vprev3, vprev2, 0b0000_0001);
                    vprev2 = Avx2.Permute4x64(vprev2.AsInt64(), 0b10_01_00_11).AsInt32();
                    vprev2 = Avx2.Blend(vprev2, vprev1, 0b0000_0001);
                    vprev1 = Avx2.Permute4x64(vprev1.AsInt64(), 0b10_01_00_11).AsInt32();
                    vprev1 = Avx2.Blend(vprev1, vprev0, 0b0000_0001);
                    vprev0 = Avx2.Permute4x64(vprev0.AsInt64(), 0b10_01_00_11).AsInt32();
                    vprev0 = Avx2.Blend(vprev0, yu, 0b0000_0001);
                }
            }
#endregion Order28
#region Order29

            [MethodImpl(OptimizationUtils.InlineAndOptimizeIfPossible)]
            internal static bool RestoreSignalOrder29(int shiftsNeeded, ReadOnlySpan<int> residual, ReadOnlySpan<int> coeffs, Span<int> output)
            {
                if (Sse41.IsSupported)
                {
                    RestoreSignalOrder29Sse41(shiftsNeeded, residual, coeffs, output);
                    return true;
                }
                return false;
            }

            [MethodImpl(OptimizationUtils.InlineAndOptimizeIfPossible)]
            internal static unsafe void RestoreSignalOrder29Sse41(int shiftsNeeded, ReadOnlySpan<int> residual, ReadOnlySpan<int> coeffs, Span<int> output)
            {
                const int Order = 29;
                if (coeffs.Length < Order) return;
                _ = coeffs[Order - 1];
                var vshift = Vector128.CreateScalar(shiftsNeeded);
                ref var o = ref MemoryMarshal.GetReference(output);
                ref var d = ref Unsafe.Add(ref o, Order);
                ref var r = ref MemoryMarshal.GetReference(residual);
                ref var c = ref MemoryMarshal.GetReference(coeffs);
                Vector128<int> sum;
                var vzero = Vector128.Create(0);
                nint dataLength = output.Length - Order;
                var vcoeff0 = Sse2.LoadVector128((int*)Unsafe.AsPointer(ref Unsafe.Add(ref c, 0)));
                var vcoeff1 = Sse2.LoadVector128((int*)Unsafe.AsPointer(ref Unsafe.Add(ref c, 4)));
                var vcoeff2 = Sse2.LoadVector128((int*)Unsafe.AsPointer(ref Unsafe.Add(ref c, 8)));
                var vcoeff3 = Sse2.LoadVector128((int*)Unsafe.AsPointer(ref Unsafe.Add(ref c, 12)));
                var vcoeff4 = Sse2.LoadVector128((int*)Unsafe.AsPointer(ref Unsafe.Add(ref c, 16)));
                var vcoeff5 = Sse2.LoadVector128((int*)Unsafe.AsPointer(ref Unsafe.Add(ref c, 20)));
                var vcoeff6 = Sse2.LoadVector128((int*)Unsafe.AsPointer(ref Unsafe.Add(ref c, 24)));
                var vcoeff7 = Vector128.Create(Unsafe.Add(ref c, 28), 0, 0, 0);
                var vprev0 = Sse2.Shuffle(Sse2.LoadVector128((int*)Unsafe.AsPointer(ref Unsafe.Add(ref o, 25))).AsInt32(), 0b00_01_10_11);
                var vprev1 = Sse2.Shuffle(Sse2.LoadVector128((int*)Unsafe.AsPointer(ref Unsafe.Add(ref o, 21))).AsInt32(), 0b00_01_10_11);
                var vprev2 = Sse2.Shuffle(Sse2.LoadVector128((int*)Unsafe.AsPointer(ref Unsafe.Add(ref o, 17))).AsInt32(), 0b00_01_10_11);
                var vprev3 = Sse2.Shuffle(Sse2.LoadVector128((int*)Unsafe.AsPointer(ref Unsafe.Add(ref o, 13))).AsInt32(), 0b00_01_10_11);
                var vprev4 = Sse2.Shuffle(Sse2.LoadVector128((int*)Unsafe.AsPointer(ref Unsafe.Add(ref o, 9))).AsInt32(), 0b00_01_10_11);
                var vprev5 = Sse2.Shuffle(Sse2.LoadVector128((int*)Unsafe.AsPointer(ref Unsafe.Add(ref o, 5))).AsInt32(), 0b00_01_10_11);
                var vprev6 = Sse2.Shuffle(Sse2.LoadVector128((int*)Unsafe.AsPointer(ref Unsafe.Add(ref o, 1))).AsInt32(), 0b00_01_10_11);
                var vprev7 = Vector128.Create(Unsafe.Add(ref o, 0), 0, 0, 0);
                for (nint i = 0; i < dataLength; i++)
                {
                    var res = Vector128.CreateScalar(Unsafe.Add(ref r, i));
                    sum = Sse41.MultiplyLow(vcoeff0, vprev0);
                    sum = Sse2.Add(sum, Sse41.MultiplyLow(vcoeff1, vprev1));
                    sum = Sse2.Add(sum, Sse41.MultiplyLow(vcoeff2, vprev2));
                    sum = Sse2.Add(sum, Sse41.MultiplyLow(vcoeff3, vprev3));
                    sum = Sse2.Add(sum, Sse41.MultiplyLow(vcoeff4, vprev4));
                    sum = Sse2.Add(sum, Sse41.MultiplyLow(vcoeff5, vprev5));
                    sum = Sse2.Add(sum, Sse41.MultiplyLow(vcoeff6, vprev6));
                    sum = Sse2.Add(sum, Sse41.MultiplyLow(vcoeff7, vprev7));
                    var y = Ssse3.HorizontalAdd(sum, vzero);
                    y = Ssse3.HorizontalAdd(y, vzero);
                    y = Sse2.ShiftRightArithmetic(y, vshift);   //C# shift operator handling sucks so SSE2 is used instead(same latency, same throughput).
                    y = Sse2.Add(y, res);   //Avoids extract and insert
#if NET5_0_OR_GREATER
                    Sse2.StoreScalar((int*)Unsafe.AsPointer(ref Unsafe.Add(ref d, i)), y);
#else
                    Unsafe.Add(ref d, i) = y.GetElement(0);
#endif
                    y = Sse2.ShiftLeftLogical128BitLane(y, 12);
                    vprev7 = Ssse3.AlignRight(vprev7, vprev6, 12);
                    vprev6 = Ssse3.AlignRight(vprev6, vprev5, 12);
                    vprev5 = Ssse3.AlignRight(vprev5, vprev4, 12);
                    vprev4 = Ssse3.AlignRight(vprev4, vprev3, 12);
                    vprev3 = Ssse3.AlignRight(vprev3, vprev2, 12);
                    vprev2 = Ssse3.AlignRight(vprev2, vprev1, 12);
                    vprev1 = Ssse3.AlignRight(vprev1, vprev0, 12);
                    vprev0 = Ssse3.AlignRight(vprev0, y, 12);
                }
            }

            [MethodImpl(OptimizationUtils.InlineAndOptimizeIfPossible)]
            internal static bool RestoreSignalOrder29Wide(int shiftsNeeded, ReadOnlySpan<int> residual, ReadOnlySpan<int> coeffs, Span<int> output)
            {
                if (Sse41.IsSupported)
                {
                    RestoreSignalOrder29WideSse41(shiftsNeeded, residual, coeffs, output);
                    return true;
                }
                return false;
            }

            [MethodImpl(OptimizationUtils.InlineAndOptimizeIfPossible)]
            internal static unsafe void RestoreSignalOrder29WideSse41(int shiftsNeeded, ReadOnlySpan<int> residual, ReadOnlySpan<int> coeffs, Span<int> output)
            {
                const int Order = 29;
                if(coeffs.Length < Order) return;
                _ = coeffs[Order - 1];
                var vshift = Vector128.CreateScalar((long)shiftsNeeded);
                ref var c = ref MemoryMarshal.GetReference(coeffs);
                ref var o = ref MemoryMarshal.GetReference(output);
                ref var d = ref Unsafe.Add(ref o, Order);
                int dataLength = output.Length - Order;
                ref var r = ref MemoryMarshal.GetReference(residual);
                Vector128<long> sum;
                var vcoeff0 = Sse41.ConvertToVector128Int64(Sse2.LoadScalarVector128((ulong*)Unsafe.AsPointer(ref Unsafe.Add(ref c, 0))).AsUInt32()).AsInt32();
                var vcoeff1 = Sse41.ConvertToVector128Int64(Sse2.LoadScalarVector128((ulong*)Unsafe.AsPointer(ref Unsafe.Add(ref c, 2))).AsUInt32()).AsInt32();
                var vcoeff2 = Sse41.ConvertToVector128Int64(Sse2.LoadScalarVector128((ulong*)Unsafe.AsPointer(ref Unsafe.Add(ref c, 4))).AsUInt32()).AsInt32();
                var vcoeff3 = Sse41.ConvertToVector128Int64(Sse2.LoadScalarVector128((ulong*)Unsafe.AsPointer(ref Unsafe.Add(ref c, 6))).AsUInt32()).AsInt32();
                var vcoeff4 = Sse41.ConvertToVector128Int64(Sse2.LoadScalarVector128((ulong*)Unsafe.AsPointer(ref Unsafe.Add(ref c, 8))).AsUInt32()).AsInt32();
                var vcoeff5 = Sse41.ConvertToVector128Int64(Sse2.LoadScalarVector128((ulong*)Unsafe.AsPointer(ref Unsafe.Add(ref c, 10))).AsUInt32()).AsInt32();
                var vcoeff6 = Sse41.ConvertToVector128Int64(Sse2.LoadScalarVector128((ulong*)Unsafe.AsPointer(ref Unsafe.Add(ref c, 12))).AsUInt32()).AsInt32();
                var vcoeff7 = Sse41.ConvertToVector128Int64(Sse2.LoadScalarVector128((ulong*)Unsafe.AsPointer(ref Unsafe.Add(ref c, 14))).AsUInt32()).AsInt32();
                var vcoeff8 = Sse41.ConvertToVector128Int64(Sse2.LoadScalarVector128((ulong*)Unsafe.AsPointer(ref Unsafe.Add(ref c, 16))).AsUInt32()).AsInt32();
                var vcoeff9 = Sse41.ConvertToVector128Int64(Sse2.LoadScalarVector128((ulong*)Unsafe.AsPointer(ref Unsafe.Add(ref c, 18))).AsUInt32()).AsInt32();
                var vcoeff10 = Sse41.ConvertToVector128Int64(Sse2.LoadScalarVector128((ulong*)Unsafe.AsPointer(ref Unsafe.Add(ref c, 20))).AsUInt32()).AsInt32();
                var vcoeff11 = Sse41.ConvertToVector128Int64(Sse2.LoadScalarVector128((ulong*)Unsafe.AsPointer(ref Unsafe.Add(ref c, 22))).AsUInt32()).AsInt32();
                var vcoeff12 = Sse41.ConvertToVector128Int64(Sse2.LoadScalarVector128((ulong*)Unsafe.AsPointer(ref Unsafe.Add(ref c, 24))).AsUInt32()).AsInt32();
                var vcoeff13 = Sse41.ConvertToVector128Int64(Sse2.LoadScalarVector128((ulong*)Unsafe.AsPointer(ref Unsafe.Add(ref c, 26))).AsUInt32()).AsInt32();
                var vcoeff14 = Sse41.ConvertToVector128Int64(Sse2.LoadScalarVector128((uint*)Unsafe.AsPointer(ref Unsafe.Add(ref c, 28))).AsUInt32()).AsInt32();
                var vprev0 = Sse2.Shuffle(Sse2.LoadScalarVector128((ulong*)Unsafe.AsPointer(ref Unsafe.Add(ref o, 27))).AsInt32(), 0b11_00_11_01);
                var vprev1 = Sse2.Shuffle(Sse2.LoadScalarVector128((ulong*)Unsafe.AsPointer(ref Unsafe.Add(ref o, 25))).AsInt32(), 0b11_00_11_01);
                var vprev2 = Sse2.Shuffle(Sse2.LoadScalarVector128((ulong*)Unsafe.AsPointer(ref Unsafe.Add(ref o, 23))).AsInt32(), 0b11_00_11_01);
                var vprev3 = Sse2.Shuffle(Sse2.LoadScalarVector128((ulong*)Unsafe.AsPointer(ref Unsafe.Add(ref o, 21))).AsInt32(), 0b11_00_11_01);
                var vprev4 = Sse2.Shuffle(Sse2.LoadScalarVector128((ulong*)Unsafe.AsPointer(ref Unsafe.Add(ref o, 19))).AsInt32(), 0b11_00_11_01);
                var vprev5 = Sse2.Shuffle(Sse2.LoadScalarVector128((ulong*)Unsafe.AsPointer(ref Unsafe.Add(ref o, 17))).AsInt32(), 0b11_00_11_01);
                var vprev6 = Sse2.Shuffle(Sse2.LoadScalarVector128((ulong*)Unsafe.AsPointer(ref Unsafe.Add(ref o, 15))).AsInt32(), 0b11_00_11_01);
                var vprev7 = Sse2.Shuffle(Sse2.LoadScalarVector128((ulong*)Unsafe.AsPointer(ref Unsafe.Add(ref o, 13))).AsInt32(), 0b11_00_11_01);
                var vprev8 = Sse2.Shuffle(Sse2.LoadScalarVector128((ulong*)Unsafe.AsPointer(ref Unsafe.Add(ref o, 11))).AsInt32(), 0b11_00_11_01);
                var vprev9 = Sse2.Shuffle(Sse2.LoadScalarVector128((ulong*)Unsafe.AsPointer(ref Unsafe.Add(ref o, 9))).AsInt32(), 0b11_00_11_01);
                var vprev10 = Sse2.Shuffle(Sse2.LoadScalarVector128((ulong*)Unsafe.AsPointer(ref Unsafe.Add(ref o, 7))).AsInt32(), 0b11_00_11_01);
                var vprev11 = Sse2.Shuffle(Sse2.LoadScalarVector128((ulong*)Unsafe.AsPointer(ref Unsafe.Add(ref o, 5))).AsInt32(), 0b11_00_11_01);
                var vprev12 = Sse2.Shuffle(Sse2.LoadScalarVector128((ulong*)Unsafe.AsPointer(ref Unsafe.Add(ref o, 3))).AsInt32(), 0b11_00_11_01);
                var vprev13 = Sse2.Shuffle(Sse2.LoadScalarVector128((ulong*)Unsafe.AsPointer(ref Unsafe.Add(ref o, 1))).AsInt32(), 0b11_00_11_01);
                var vprev14 = Sse2.Shuffle(Sse2.LoadScalarVector128((ulong*)Unsafe.AsPointer(ref Unsafe.Add(ref o, 0))).AsInt32(), 0b11_00_11_00);
                for(nint i = 0; i < dataLength;)
                {
                    var res = Vector128.CreateScalar(Unsafe.Add(ref r, i));
                    sum = Sse41.Multiply(vcoeff0, vprev0);
                    sum = Sse2.Add(sum, Sse41.Multiply(vcoeff1, vprev1));
                    sum = Sse2.Add(sum, Sse41.Multiply(vcoeff2, vprev2));
                    sum = Sse2.Add(sum, Sse41.Multiply(vcoeff3, vprev3));
                    sum = Sse2.Add(sum, Sse41.Multiply(vcoeff4, vprev4));
                    sum = Sse2.Add(sum, Sse41.Multiply(vcoeff5, vprev5));
                    sum = Sse2.Add(sum, Sse41.Multiply(vcoeff6, vprev6));
                    sum = Sse2.Add(sum, Sse41.Multiply(vcoeff7, vprev7));
                    sum = Sse2.Add(sum, Sse41.Multiply(vcoeff8, vprev8));
                    sum = Sse2.Add(sum, Sse41.Multiply(vcoeff9, vprev9));
                    sum = Sse2.Add(sum, Sse41.Multiply(vcoeff10, vprev10));
                    sum = Sse2.Add(sum, Sse41.Multiply(vcoeff11, vprev11));
                    sum = Sse2.Add(sum, Sse41.Multiply(vcoeff12, vprev12));
                    sum = Sse2.Add(sum, Sse41.Multiply(vcoeff13, vprev13));
                    sum = Sse2.Add(sum, Sse41.Multiply(vcoeff14, vprev14));
                    sum = Sse2.Add(sum, Sse2.ShiftRightLogical128BitLane(sum, 8));
                    sum = Sse2.ShiftRightLogical(sum, vshift);
                    var yy = Sse2.Add(sum.AsInt32(), res);
#if NET5_0_OR_GREATER
                    Sse2.StoreScalar((int*)Unsafe.AsPointer(ref Unsafe.Add(ref d, i)), yy);
#else
                    Unsafe.Add(ref d, i) = yy.GetElement(0);
#endif
                    i++;
                    yy = Sse2.ShiftLeftLogical128BitLane(yy, 8);
                    vprev14 = Ssse3.AlignRight(vprev14, vprev13, 8);
                    vprev13 = Ssse3.AlignRight(vprev13, vprev12, 8);
                    vprev12 = Ssse3.AlignRight(vprev12, vprev11, 8);
                    vprev11 = Ssse3.AlignRight(vprev11, vprev10, 8);
                    vprev10 = Ssse3.AlignRight(vprev10, vprev9, 8);
                    vprev9 = Ssse3.AlignRight(vprev9, vprev8, 8);
                    vprev8 = Ssse3.AlignRight(vprev8, vprev7, 8);
                    vprev7 = Ssse3.AlignRight(vprev7, vprev6, 8);
                    vprev6 = Ssse3.AlignRight(vprev6, vprev5, 8);
                    vprev5 = Ssse3.AlignRight(vprev5, vprev4, 8);
                    vprev4 = Ssse3.AlignRight(vprev4, vprev3, 8);
                    vprev3 = Ssse3.AlignRight(vprev3, vprev2, 8);
                    vprev2 = Ssse3.AlignRight(vprev2, vprev1, 8);
                    vprev1 = Ssse3.AlignRight(vprev1, vprev0, 8);
                    vprev0 = Ssse3.AlignRight(vprev0, yy, 8);
                }
            }
            
            [MethodImpl(OptimizationUtils.InlineAndOptimizeIfPossible)]
            internal static unsafe void RestoreSignalOrder29WideAvx2(int shiftsNeeded, ReadOnlySpan<int> residual, ReadOnlySpan<int> coeffs, Span<int> output)
            {
                const int Order = 29;
                if(coeffs.Length < Order) return;
                _ = coeffs[Order - 1];
                var vshift = Vector128.CreateScalar((long)shiftsNeeded);
                ref var c = ref MemoryMarshal.GetReference(coeffs);
                ref var o = ref MemoryMarshal.GetReference(output);
                ref var d = ref Unsafe.Add(ref o, Order);
                int dataLength = output.Length - Order;
                ref var r = ref MemoryMarshal.GetReference(residual);
                Vector128<long> sum;
                Vector256<long> sum256;
                var vcoeff0 = Avx2.ConvertToVector256Int64(Sse2.LoadVector128((int*)Unsafe.AsPointer(ref Unsafe.Add(ref c, 0))).AsUInt32()).AsInt32();
                var vcoeff1 = Avx2.ConvertToVector256Int64(Sse2.LoadVector128((int*)Unsafe.AsPointer(ref Unsafe.Add(ref c, 4))).AsUInt32()).AsInt32();
                var vcoeff2 = Avx2.ConvertToVector256Int64(Sse2.LoadVector128((int*)Unsafe.AsPointer(ref Unsafe.Add(ref c, 8))).AsUInt32()).AsInt32();
                var vcoeff3 = Avx2.ConvertToVector256Int64(Sse2.LoadVector128((int*)Unsafe.AsPointer(ref Unsafe.Add(ref c, 12))).AsUInt32()).AsInt32();
                var vcoeff4 = Avx2.ConvertToVector256Int64(Sse2.LoadVector128((int*)Unsafe.AsPointer(ref Unsafe.Add(ref c, 16))).AsUInt32()).AsInt32();
                var vcoeff5 = Avx2.ConvertToVector256Int64(Sse2.LoadVector128((int*)Unsafe.AsPointer(ref Unsafe.Add(ref c, 20))).AsUInt32()).AsInt32();
                var vcoeff6 = Avx2.ConvertToVector256Int64(Sse2.LoadVector128((int*)Unsafe.AsPointer(ref Unsafe.Add(ref c, 24))).AsUInt32()).AsInt32();
                var vcoeff7 = Avx2.ConvertToVector256Int64(Sse2.LoadScalarVector128((uint*)Unsafe.AsPointer(ref Unsafe.Add(ref c, 28))).AsUInt32()).AsInt32();
                var vprev0 = Avx2.ConvertToVector256Int64(Sse2.Shuffle(Sse2.LoadScalarVector128((ulong*)Unsafe.AsPointer(ref Unsafe.Add(ref o, 26))).AsInt32(), 0b00_01_10_11).AsUInt32()).AsInt32();
                var vprev1 = Avx2.ConvertToVector256Int64(Sse2.Shuffle(Sse2.LoadScalarVector128((ulong*)Unsafe.AsPointer(ref Unsafe.Add(ref o, 22))).AsInt32(), 0b00_01_10_11).AsUInt32()).AsInt32();
                var vprev2 = Avx2.ConvertToVector256Int64(Sse2.Shuffle(Sse2.LoadScalarVector128((ulong*)Unsafe.AsPointer(ref Unsafe.Add(ref o, 18))).AsInt32(), 0b00_01_10_11).AsUInt32()).AsInt32();
                var vprev3 = Avx2.ConvertToVector256Int64(Sse2.Shuffle(Sse2.LoadScalarVector128((ulong*)Unsafe.AsPointer(ref Unsafe.Add(ref o, 14))).AsInt32(), 0b00_01_10_11).AsUInt32()).AsInt32();
                var vprev4 = Avx2.ConvertToVector256Int64(Sse2.Shuffle(Sse2.LoadScalarVector128((ulong*)Unsafe.AsPointer(ref Unsafe.Add(ref o, 10))).AsInt32(), 0b00_01_10_11).AsUInt32()).AsInt32();
                var vprev5 = Avx2.ConvertToVector256Int64(Sse2.Shuffle(Sse2.LoadScalarVector128((ulong*)Unsafe.AsPointer(ref Unsafe.Add(ref o, 6))).AsInt32(), 0b00_01_10_11).AsUInt32()).AsInt32();
                var vprev6 = Avx2.ConvertToVector256Int64(Sse2.Shuffle(Sse2.LoadScalarVector128((ulong*)Unsafe.AsPointer(ref Unsafe.Add(ref o, 2))).AsInt32(), 0b00_01_10_11).AsUInt32()).AsInt32();
                var vprev7 = Avx2.ConvertToVector256Int64(Sse2.Shuffle(Sse2.LoadScalarVector128((uint*)Unsafe.AsPointer(ref o)).AsInt32(), 0b00_01_10_11).AsUInt32()).AsInt32();
                for(nint i = 0; i < dataLength;)
                {
                    var res = Vector128.CreateScalar(Unsafe.Add(ref r, i));
                    sum256 = Avx2.Multiply(vcoeff0, vprev0);
                    sum256 = Avx2.Add(sum256, Avx2.Multiply(vcoeff1, vprev1));
                    sum256 = Avx2.Add(sum256, Avx2.Multiply(vcoeff2, vprev2));
                    sum256 = Avx2.Add(sum256, Avx2.Multiply(vcoeff3, vprev3));
                    sum256 = Avx2.Add(sum256, Avx2.Multiply(vcoeff4, vprev4));
                    sum256 = Avx2.Add(sum256, Avx2.Multiply(vcoeff5, vprev5));
                    sum256 = Avx2.Add(sum256, Avx2.Multiply(vcoeff6, vprev6));
                    sum256 = Avx2.Add(sum256, Avx2.Multiply(vcoeff7, vprev7));
                    sum = Sse2.Add(sum256.GetLower(), sum256.GetUpper());
                    sum = Sse2.Add(sum, Sse2.ShiftRightLogical128BitLane(sum, 8));
                    sum = Sse2.ShiftRightLogical(sum, vshift);
                    var yy = Sse2.Add(sum.AsInt32(), res);
#if NET5_0_OR_GREATER
                    Sse2.StoreScalar((int*)Unsafe.AsPointer(ref Unsafe.Add(ref d, i)), yy);
#else
                    Unsafe.Add(ref d, i) = yy.GetElement(0);
#endif
                    i++;
                    var yu = yy.ToVector256();
                    yu = Avx2.Permute4x64(yu.AsUInt64(), 0b00_00_00_00).AsInt32();
                    vprev7 = Avx2.Permute4x64(vprev7.AsInt64(), 0b10_01_00_11).AsInt32();
                    vprev7 = Avx2.Blend(vprev7, vprev6, 0b0000_0001);
                    vprev6 = Avx2.Permute4x64(vprev6.AsInt64(), 0b10_01_00_11).AsInt32();
                    vprev6 = Avx2.Blend(vprev6, vprev5, 0b0000_0001);
                    vprev5 = Avx2.Permute4x64(vprev5.AsInt64(), 0b10_01_00_11).AsInt32();
                    vprev5 = Avx2.Blend(vprev5, vprev4, 0b0000_0001);
                    vprev4 = Avx2.Permute4x64(vprev4.AsInt64(), 0b10_01_00_11).AsInt32();
                    vprev4 = Avx2.Blend(vprev4, vprev3, 0b0000_0001);
                    vprev3 = Avx2.Permute4x64(vprev3.AsInt64(), 0b10_01_00_11).AsInt32();
                    vprev3 = Avx2.Blend(vprev3, vprev2, 0b0000_0001);
                    vprev2 = Avx2.Permute4x64(vprev2.AsInt64(), 0b10_01_00_11).AsInt32();
                    vprev2 = Avx2.Blend(vprev2, vprev1, 0b0000_0001);
                    vprev1 = Avx2.Permute4x64(vprev1.AsInt64(), 0b10_01_00_11).AsInt32();
                    vprev1 = Avx2.Blend(vprev1, vprev0, 0b0000_0001);
                    vprev0 = Avx2.Permute4x64(vprev0.AsInt64(), 0b10_01_00_11).AsInt32();
                    vprev0 = Avx2.Blend(vprev0, yu, 0b0000_0001);
                }
            }
#endregion Order29
#region Order30

            [MethodImpl(OptimizationUtils.InlineAndOptimizeIfPossible)]
            internal static bool RestoreSignalOrder30(int shiftsNeeded, ReadOnlySpan<int> residual, ReadOnlySpan<int> coeffs, Span<int> output)
            {
                if (Sse41.IsSupported)
                {
                    RestoreSignalOrder30Sse41(shiftsNeeded, residual, coeffs, output);
                    return true;
                }
                return false;
            }

            [MethodImpl(OptimizationUtils.InlineAndOptimizeIfPossible)]
            internal static unsafe void RestoreSignalOrder30Sse41(int shiftsNeeded, ReadOnlySpan<int> residual, ReadOnlySpan<int> coeffs, Span<int> output)
            {
                const int Order = 30;
                if (coeffs.Length < Order) return;
                _ = coeffs[Order - 1];
                var vshift = Vector128.CreateScalar(shiftsNeeded);
                ref var o = ref MemoryMarshal.GetReference(output);
                ref var d = ref Unsafe.Add(ref o, Order);
                ref var r = ref MemoryMarshal.GetReference(residual);
                ref var c = ref MemoryMarshal.GetReference(coeffs);
                Vector128<int> sum;
                var vzero = Vector128.Create(0);
                nint dataLength = output.Length - Order;
                var vcoeff0 = Sse2.LoadVector128((int*)Unsafe.AsPointer(ref Unsafe.Add(ref c, 0)));
                var vcoeff1 = Sse2.LoadVector128((int*)Unsafe.AsPointer(ref Unsafe.Add(ref c, 4)));
                var vcoeff2 = Sse2.LoadVector128((int*)Unsafe.AsPointer(ref Unsafe.Add(ref c, 8)));
                var vcoeff3 = Sse2.LoadVector128((int*)Unsafe.AsPointer(ref Unsafe.Add(ref c, 12)));
                var vcoeff4 = Sse2.LoadVector128((int*)Unsafe.AsPointer(ref Unsafe.Add(ref c, 16)));
                var vcoeff5 = Sse2.LoadVector128((int*)Unsafe.AsPointer(ref Unsafe.Add(ref c, 20)));
                var vcoeff6 = Sse2.LoadVector128((int*)Unsafe.AsPointer(ref Unsafe.Add(ref c, 24)));
                var vcoeff7 = Vector128.Create(Unsafe.Add(ref c, 28), Unsafe.Add(ref c, 29), 0, 0);
                var vprev0 = Sse2.Shuffle(Sse2.LoadVector128((int*)Unsafe.AsPointer(ref Unsafe.Add(ref o, 26))).AsInt32(), 0b00_01_10_11);
                var vprev1 = Sse2.Shuffle(Sse2.LoadVector128((int*)Unsafe.AsPointer(ref Unsafe.Add(ref o, 22))).AsInt32(), 0b00_01_10_11);
                var vprev2 = Sse2.Shuffle(Sse2.LoadVector128((int*)Unsafe.AsPointer(ref Unsafe.Add(ref o, 18))).AsInt32(), 0b00_01_10_11);
                var vprev3 = Sse2.Shuffle(Sse2.LoadVector128((int*)Unsafe.AsPointer(ref Unsafe.Add(ref o, 14))).AsInt32(), 0b00_01_10_11);
                var vprev4 = Sse2.Shuffle(Sse2.LoadVector128((int*)Unsafe.AsPointer(ref Unsafe.Add(ref o, 10))).AsInt32(), 0b00_01_10_11);
                var vprev5 = Sse2.Shuffle(Sse2.LoadVector128((int*)Unsafe.AsPointer(ref Unsafe.Add(ref o, 6))).AsInt32(), 0b00_01_10_11);
                var vprev6 = Sse2.Shuffle(Sse2.LoadVector128((int*)Unsafe.AsPointer(ref Unsafe.Add(ref o, 2))).AsInt32(), 0b00_01_10_11);
                var vprev7 = Vector128.Create(Unsafe.Add(ref o, 1), Unsafe.Add(ref o, 0), 0, 0);
                for (nint i = 0; i < dataLength; i++)
                {
                    var res = Vector128.CreateScalar(Unsafe.Add(ref r, i));
                    sum = Sse41.MultiplyLow(vcoeff0, vprev0);
                    sum = Sse2.Add(sum, Sse41.MultiplyLow(vcoeff1, vprev1));
                    sum = Sse2.Add(sum, Sse41.MultiplyLow(vcoeff2, vprev2));
                    sum = Sse2.Add(sum, Sse41.MultiplyLow(vcoeff3, vprev3));
                    sum = Sse2.Add(sum, Sse41.MultiplyLow(vcoeff4, vprev4));
                    sum = Sse2.Add(sum, Sse41.MultiplyLow(vcoeff5, vprev5));
                    sum = Sse2.Add(sum, Sse41.MultiplyLow(vcoeff6, vprev6));
                    sum = Sse2.Add(sum, Sse41.MultiplyLow(vcoeff7, vprev7));
                    var y = Ssse3.HorizontalAdd(sum, vzero);
                    y = Ssse3.HorizontalAdd(y, vzero);
                    y = Sse2.ShiftRightArithmetic(y, vshift);   //C# shift operator handling sucks so SSE2 is used instead(same latency, same throughput).
                    y = Sse2.Add(y, res);   //Avoids extract and insert
#if NET5_0_OR_GREATER
                    Sse2.StoreScalar((int*)Unsafe.AsPointer(ref Unsafe.Add(ref d, i)), y);
#else
                    Unsafe.Add(ref d, i) = y.GetElement(0);
#endif
                    y = Sse2.ShiftLeftLogical128BitLane(y, 12);
                    vprev7 = Ssse3.AlignRight(vprev7, vprev6, 12);
                    vprev6 = Ssse3.AlignRight(vprev6, vprev5, 12);
                    vprev5 = Ssse3.AlignRight(vprev5, vprev4, 12);
                    vprev4 = Ssse3.AlignRight(vprev4, vprev3, 12);
                    vprev3 = Ssse3.AlignRight(vprev3, vprev2, 12);
                    vprev2 = Ssse3.AlignRight(vprev2, vprev1, 12);
                    vprev1 = Ssse3.AlignRight(vprev1, vprev0, 12);
                    vprev0 = Ssse3.AlignRight(vprev0, y, 12);
                }
            }

            [MethodImpl(OptimizationUtils.InlineAndOptimizeIfPossible)]
            internal static bool RestoreSignalOrder30Wide(int shiftsNeeded, ReadOnlySpan<int> residual, ReadOnlySpan<int> coeffs, Span<int> output)
            {
                if (Sse41.IsSupported)
                {
                    RestoreSignalOrder30WideSse41(shiftsNeeded, residual, coeffs, output);
                    return true;
                }
                return false;
            }

            [MethodImpl(OptimizationUtils.InlineAndOptimizeIfPossible)]
            internal static unsafe void RestoreSignalOrder30WideSse41(int shiftsNeeded, ReadOnlySpan<int> residual, ReadOnlySpan<int> coeffs, Span<int> output)
            {
                const int Order = 30;
                if(coeffs.Length < Order) return;
                _ = coeffs[Order - 1];
                var vshift = Vector128.CreateScalar((long)shiftsNeeded);
                ref var c = ref MemoryMarshal.GetReference(coeffs);
                ref var o = ref MemoryMarshal.GetReference(output);
                ref var d = ref Unsafe.Add(ref o, Order);
                int dataLength = output.Length - Order;
                ref var r = ref MemoryMarshal.GetReference(residual);
                Vector128<long> sum;
                var vcoeff0 = Sse41.ConvertToVector128Int64(Sse2.LoadScalarVector128((ulong*)Unsafe.AsPointer(ref Unsafe.Add(ref c, 0))).AsUInt32()).AsInt32();
                var vcoeff1 = Sse41.ConvertToVector128Int64(Sse2.LoadScalarVector128((ulong*)Unsafe.AsPointer(ref Unsafe.Add(ref c, 2))).AsUInt32()).AsInt32();
                var vcoeff2 = Sse41.ConvertToVector128Int64(Sse2.LoadScalarVector128((ulong*)Unsafe.AsPointer(ref Unsafe.Add(ref c, 4))).AsUInt32()).AsInt32();
                var vcoeff3 = Sse41.ConvertToVector128Int64(Sse2.LoadScalarVector128((ulong*)Unsafe.AsPointer(ref Unsafe.Add(ref c, 6))).AsUInt32()).AsInt32();
                var vcoeff4 = Sse41.ConvertToVector128Int64(Sse2.LoadScalarVector128((ulong*)Unsafe.AsPointer(ref Unsafe.Add(ref c, 8))).AsUInt32()).AsInt32();
                var vcoeff5 = Sse41.ConvertToVector128Int64(Sse2.LoadScalarVector128((ulong*)Unsafe.AsPointer(ref Unsafe.Add(ref c, 10))).AsUInt32()).AsInt32();
                var vcoeff6 = Sse41.ConvertToVector128Int64(Sse2.LoadScalarVector128((ulong*)Unsafe.AsPointer(ref Unsafe.Add(ref c, 12))).AsUInt32()).AsInt32();
                var vcoeff7 = Sse41.ConvertToVector128Int64(Sse2.LoadScalarVector128((ulong*)Unsafe.AsPointer(ref Unsafe.Add(ref c, 14))).AsUInt32()).AsInt32();
                var vcoeff8 = Sse41.ConvertToVector128Int64(Sse2.LoadScalarVector128((ulong*)Unsafe.AsPointer(ref Unsafe.Add(ref c, 16))).AsUInt32()).AsInt32();
                var vcoeff9 = Sse41.ConvertToVector128Int64(Sse2.LoadScalarVector128((ulong*)Unsafe.AsPointer(ref Unsafe.Add(ref c, 18))).AsUInt32()).AsInt32();
                var vcoeff10 = Sse41.ConvertToVector128Int64(Sse2.LoadScalarVector128((ulong*)Unsafe.AsPointer(ref Unsafe.Add(ref c, 20))).AsUInt32()).AsInt32();
                var vcoeff11 = Sse41.ConvertToVector128Int64(Sse2.LoadScalarVector128((ulong*)Unsafe.AsPointer(ref Unsafe.Add(ref c, 22))).AsUInt32()).AsInt32();
                var vcoeff12 = Sse41.ConvertToVector128Int64(Sse2.LoadScalarVector128((ulong*)Unsafe.AsPointer(ref Unsafe.Add(ref c, 24))).AsUInt32()).AsInt32();
                var vcoeff13 = Sse41.ConvertToVector128Int64(Sse2.LoadScalarVector128((ulong*)Unsafe.AsPointer(ref Unsafe.Add(ref c, 26))).AsUInt32()).AsInt32();
                var vcoeff14 = Sse41.ConvertToVector128Int64(Sse2.LoadScalarVector128((ulong*)Unsafe.AsPointer(ref Unsafe.Add(ref c, 28))).AsUInt32()).AsInt32();
                var vprev0 = Sse2.Shuffle(Sse2.LoadScalarVector128((ulong*)Unsafe.AsPointer(ref Unsafe.Add(ref o, 28))).AsInt32(), 0b11_00_11_01);
                var vprev1 = Sse2.Shuffle(Sse2.LoadScalarVector128((ulong*)Unsafe.AsPointer(ref Unsafe.Add(ref o, 26))).AsInt32(), 0b11_00_11_01);
                var vprev2 = Sse2.Shuffle(Sse2.LoadScalarVector128((ulong*)Unsafe.AsPointer(ref Unsafe.Add(ref o, 24))).AsInt32(), 0b11_00_11_01);
                var vprev3 = Sse2.Shuffle(Sse2.LoadScalarVector128((ulong*)Unsafe.AsPointer(ref Unsafe.Add(ref o, 22))).AsInt32(), 0b11_00_11_01);
                var vprev4 = Sse2.Shuffle(Sse2.LoadScalarVector128((ulong*)Unsafe.AsPointer(ref Unsafe.Add(ref o, 20))).AsInt32(), 0b11_00_11_01);
                var vprev5 = Sse2.Shuffle(Sse2.LoadScalarVector128((ulong*)Unsafe.AsPointer(ref Unsafe.Add(ref o, 18))).AsInt32(), 0b11_00_11_01);
                var vprev6 = Sse2.Shuffle(Sse2.LoadScalarVector128((ulong*)Unsafe.AsPointer(ref Unsafe.Add(ref o, 16))).AsInt32(), 0b11_00_11_01);
                var vprev7 = Sse2.Shuffle(Sse2.LoadScalarVector128((ulong*)Unsafe.AsPointer(ref Unsafe.Add(ref o, 14))).AsInt32(), 0b11_00_11_01);
                var vprev8 = Sse2.Shuffle(Sse2.LoadScalarVector128((ulong*)Unsafe.AsPointer(ref Unsafe.Add(ref o, 12))).AsInt32(), 0b11_00_11_01);
                var vprev9 = Sse2.Shuffle(Sse2.LoadScalarVector128((ulong*)Unsafe.AsPointer(ref Unsafe.Add(ref o, 10))).AsInt32(), 0b11_00_11_01);
                var vprev10 = Sse2.Shuffle(Sse2.LoadScalarVector128((ulong*)Unsafe.AsPointer(ref Unsafe.Add(ref o, 8))).AsInt32(), 0b11_00_11_01);
                var vprev11 = Sse2.Shuffle(Sse2.LoadScalarVector128((ulong*)Unsafe.AsPointer(ref Unsafe.Add(ref o, 6))).AsInt32(), 0b11_00_11_01);
                var vprev12 = Sse2.Shuffle(Sse2.LoadScalarVector128((ulong*)Unsafe.AsPointer(ref Unsafe.Add(ref o, 4))).AsInt32(), 0b11_00_11_01);
                var vprev13 = Sse2.Shuffle(Sse2.LoadScalarVector128((ulong*)Unsafe.AsPointer(ref Unsafe.Add(ref o, 2))).AsInt32(), 0b11_00_11_01);
                var vprev14 = Sse2.Shuffle(Sse2.LoadScalarVector128((ulong*)Unsafe.AsPointer(ref Unsafe.Add(ref o, 0))).AsInt32(), 0b11_00_11_01);
                for(nint i = 0; i < dataLength;)
                {
                    var res = Vector128.CreateScalar(Unsafe.Add(ref r, i));
                    sum = Sse41.Multiply(vcoeff0, vprev0);
                    sum = Sse2.Add(sum, Sse41.Multiply(vcoeff1, vprev1));
                    sum = Sse2.Add(sum, Sse41.Multiply(vcoeff2, vprev2));
                    sum = Sse2.Add(sum, Sse41.Multiply(vcoeff3, vprev3));
                    sum = Sse2.Add(sum, Sse41.Multiply(vcoeff4, vprev4));
                    sum = Sse2.Add(sum, Sse41.Multiply(vcoeff5, vprev5));
                    sum = Sse2.Add(sum, Sse41.Multiply(vcoeff6, vprev6));
                    sum = Sse2.Add(sum, Sse41.Multiply(vcoeff7, vprev7));
                    sum = Sse2.Add(sum, Sse41.Multiply(vcoeff8, vprev8));
                    sum = Sse2.Add(sum, Sse41.Multiply(vcoeff9, vprev9));
                    sum = Sse2.Add(sum, Sse41.Multiply(vcoeff10, vprev10));
                    sum = Sse2.Add(sum, Sse41.Multiply(vcoeff11, vprev11));
                    sum = Sse2.Add(sum, Sse41.Multiply(vcoeff12, vprev12));
                    sum = Sse2.Add(sum, Sse41.Multiply(vcoeff13, vprev13));
                    sum = Sse2.Add(sum, Sse41.Multiply(vcoeff14, vprev14));
                    sum = Sse2.Add(sum, Sse2.ShiftRightLogical128BitLane(sum, 8));
                    sum = Sse2.ShiftRightLogical(sum, vshift);
                    var yy = Sse2.Add(sum.AsInt32(), res);
#if NET5_0_OR_GREATER
                    Sse2.StoreScalar((int*)Unsafe.AsPointer(ref Unsafe.Add(ref d, i)), yy);
#else
                    Unsafe.Add(ref d, i) = yy.GetElement(0);
#endif
                    i++;
                    yy = Sse2.ShiftLeftLogical128BitLane(yy, 8);
                    vprev14 = Ssse3.AlignRight(vprev14, vprev13, 8);
                    vprev13 = Ssse3.AlignRight(vprev13, vprev12, 8);
                    vprev12 = Ssse3.AlignRight(vprev12, vprev11, 8);
                    vprev11 = Ssse3.AlignRight(vprev11, vprev10, 8);
                    vprev10 = Ssse3.AlignRight(vprev10, vprev9, 8);
                    vprev9 = Ssse3.AlignRight(vprev9, vprev8, 8);
                    vprev8 = Ssse3.AlignRight(vprev8, vprev7, 8);
                    vprev7 = Ssse3.AlignRight(vprev7, vprev6, 8);
                    vprev6 = Ssse3.AlignRight(vprev6, vprev5, 8);
                    vprev5 = Ssse3.AlignRight(vprev5, vprev4, 8);
                    vprev4 = Ssse3.AlignRight(vprev4, vprev3, 8);
                    vprev3 = Ssse3.AlignRight(vprev3, vprev2, 8);
                    vprev2 = Ssse3.AlignRight(vprev2, vprev1, 8);
                    vprev1 = Ssse3.AlignRight(vprev1, vprev0, 8);
                    vprev0 = Ssse3.AlignRight(vprev0, yy, 8);
                }
            }
            
            [MethodImpl(OptimizationUtils.InlineAndOptimizeIfPossible)]
            internal static unsafe void RestoreSignalOrder30WideAvx2(int shiftsNeeded, ReadOnlySpan<int> residual, ReadOnlySpan<int> coeffs, Span<int> output)
            {
                const int Order = 30;
                if(coeffs.Length < Order) return;
                _ = coeffs[Order - 1];
                var vshift = Vector128.CreateScalar((long)shiftsNeeded);
                ref var c = ref MemoryMarshal.GetReference(coeffs);
                ref var o = ref MemoryMarshal.GetReference(output);
                ref var d = ref Unsafe.Add(ref o, Order);
                int dataLength = output.Length - Order;
                ref var r = ref MemoryMarshal.GetReference(residual);
                Vector128<long> sum;
                Vector256<long> sum256;
                var vcoeff0 = Avx2.ConvertToVector256Int64(Sse2.LoadVector128((int*)Unsafe.AsPointer(ref Unsafe.Add(ref c, 0))).AsUInt32()).AsInt32();
                var vcoeff1 = Avx2.ConvertToVector256Int64(Sse2.LoadVector128((int*)Unsafe.AsPointer(ref Unsafe.Add(ref c, 4))).AsUInt32()).AsInt32();
                var vcoeff2 = Avx2.ConvertToVector256Int64(Sse2.LoadVector128((int*)Unsafe.AsPointer(ref Unsafe.Add(ref c, 8))).AsUInt32()).AsInt32();
                var vcoeff3 = Avx2.ConvertToVector256Int64(Sse2.LoadVector128((int*)Unsafe.AsPointer(ref Unsafe.Add(ref c, 12))).AsUInt32()).AsInt32();
                var vcoeff4 = Avx2.ConvertToVector256Int64(Sse2.LoadVector128((int*)Unsafe.AsPointer(ref Unsafe.Add(ref c, 16))).AsUInt32()).AsInt32();
                var vcoeff5 = Avx2.ConvertToVector256Int64(Sse2.LoadVector128((int*)Unsafe.AsPointer(ref Unsafe.Add(ref c, 20))).AsUInt32()).AsInt32();
                var vcoeff6 = Avx2.ConvertToVector256Int64(Sse2.LoadVector128((int*)Unsafe.AsPointer(ref Unsafe.Add(ref c, 24))).AsUInt32()).AsInt32();
                var vcoeff7 = Avx2.ConvertToVector256Int64(Sse2.LoadScalarVector128((ulong*)Unsafe.AsPointer(ref Unsafe.Add(ref c, 28))).AsUInt32()).AsInt32();
                var vprev0 = Avx2.ConvertToVector256Int64(Sse2.Shuffle(Sse2.LoadScalarVector128((ulong*)Unsafe.AsPointer(ref Unsafe.Add(ref o, 27))).AsInt32(), 0b00_01_10_11).AsUInt32()).AsInt32();
                var vprev1 = Avx2.ConvertToVector256Int64(Sse2.Shuffle(Sse2.LoadScalarVector128((ulong*)Unsafe.AsPointer(ref Unsafe.Add(ref o, 23))).AsInt32(), 0b00_01_10_11).AsUInt32()).AsInt32();
                var vprev2 = Avx2.ConvertToVector256Int64(Sse2.Shuffle(Sse2.LoadScalarVector128((ulong*)Unsafe.AsPointer(ref Unsafe.Add(ref o, 19))).AsInt32(), 0b00_01_10_11).AsUInt32()).AsInt32();
                var vprev3 = Avx2.ConvertToVector256Int64(Sse2.Shuffle(Sse2.LoadScalarVector128((ulong*)Unsafe.AsPointer(ref Unsafe.Add(ref o, 15))).AsInt32(), 0b00_01_10_11).AsUInt32()).AsInt32();
                var vprev4 = Avx2.ConvertToVector256Int64(Sse2.Shuffle(Sse2.LoadScalarVector128((ulong*)Unsafe.AsPointer(ref Unsafe.Add(ref o, 11))).AsInt32(), 0b00_01_10_11).AsUInt32()).AsInt32();
                var vprev5 = Avx2.ConvertToVector256Int64(Sse2.Shuffle(Sse2.LoadScalarVector128((ulong*)Unsafe.AsPointer(ref Unsafe.Add(ref o, 7))).AsInt32(), 0b00_01_10_11).AsUInt32()).AsInt32();
                var vprev6 = Avx2.ConvertToVector256Int64(Sse2.Shuffle(Sse2.LoadScalarVector128((ulong*)Unsafe.AsPointer(ref Unsafe.Add(ref o, 3))).AsInt32(), 0b00_01_10_11).AsUInt32()).AsInt32();
                var vprev7 = Avx2.ConvertToVector256Int64(Sse2.Shuffle(Sse2.LoadScalarVector128((ulong*)Unsafe.AsPointer(ref o)).AsInt32(), 0b00_01_10_11).AsUInt32()).AsInt32();
                for(nint i = 0; i < dataLength;)
                {
                    var res = Vector128.CreateScalar(Unsafe.Add(ref r, i));
                    sum256 = Avx2.Multiply(vcoeff0, vprev0);
                    sum256 = Avx2.Add(sum256, Avx2.Multiply(vcoeff1, vprev1));
                    sum256 = Avx2.Add(sum256, Avx2.Multiply(vcoeff2, vprev2));
                    sum256 = Avx2.Add(sum256, Avx2.Multiply(vcoeff3, vprev3));
                    sum256 = Avx2.Add(sum256, Avx2.Multiply(vcoeff4, vprev4));
                    sum256 = Avx2.Add(sum256, Avx2.Multiply(vcoeff5, vprev5));
                    sum256 = Avx2.Add(sum256, Avx2.Multiply(vcoeff6, vprev6));
                    sum256 = Avx2.Add(sum256, Avx2.Multiply(vcoeff7, vprev7));
                    sum = Sse2.Add(sum256.GetLower(), sum256.GetUpper());
                    sum = Sse2.Add(sum, Sse2.ShiftRightLogical128BitLane(sum, 8));
                    sum = Sse2.ShiftRightLogical(sum, vshift);
                    var yy = Sse2.Add(sum.AsInt32(), res);
#if NET5_0_OR_GREATER
                    Sse2.StoreScalar((int*)Unsafe.AsPointer(ref Unsafe.Add(ref d, i)), yy);
#else
                    Unsafe.Add(ref d, i) = yy.GetElement(0);
#endif
                    i++;
                    var yu = yy.ToVector256();
                    yu = Avx2.Permute4x64(yu.AsUInt64(), 0b00_00_00_00).AsInt32();
                    vprev7 = Avx2.Permute4x64(vprev7.AsInt64(), 0b10_01_00_11).AsInt32();
                    vprev7 = Avx2.Blend(vprev7, vprev6, 0b0000_0001);
                    vprev6 = Avx2.Permute4x64(vprev6.AsInt64(), 0b10_01_00_11).AsInt32();
                    vprev6 = Avx2.Blend(vprev6, vprev5, 0b0000_0001);
                    vprev5 = Avx2.Permute4x64(vprev5.AsInt64(), 0b10_01_00_11).AsInt32();
                    vprev5 = Avx2.Blend(vprev5, vprev4, 0b0000_0001);
                    vprev4 = Avx2.Permute4x64(vprev4.AsInt64(), 0b10_01_00_11).AsInt32();
                    vprev4 = Avx2.Blend(vprev4, vprev3, 0b0000_0001);
                    vprev3 = Avx2.Permute4x64(vprev3.AsInt64(), 0b10_01_00_11).AsInt32();
                    vprev3 = Avx2.Blend(vprev3, vprev2, 0b0000_0001);
                    vprev2 = Avx2.Permute4x64(vprev2.AsInt64(), 0b10_01_00_11).AsInt32();
                    vprev2 = Avx2.Blend(vprev2, vprev1, 0b0000_0001);
                    vprev1 = Avx2.Permute4x64(vprev1.AsInt64(), 0b10_01_00_11).AsInt32();
                    vprev1 = Avx2.Blend(vprev1, vprev0, 0b0000_0001);
                    vprev0 = Avx2.Permute4x64(vprev0.AsInt64(), 0b10_01_00_11).AsInt32();
                    vprev0 = Avx2.Blend(vprev0, yu, 0b0000_0001);
                }
            }
#endregion Order30
#region Order31

            [MethodImpl(OptimizationUtils.InlineAndOptimizeIfPossible)]
            internal static bool RestoreSignalOrder31(int shiftsNeeded, ReadOnlySpan<int> residual, ReadOnlySpan<int> coeffs, Span<int> output)
            {
                if (Sse41.IsSupported)
                {
                    RestoreSignalOrder31Sse41(shiftsNeeded, residual, coeffs, output);
                    return true;
                }
                return false;
            }

            [MethodImpl(OptimizationUtils.InlineAndOptimizeIfPossible)]
            internal static unsafe void RestoreSignalOrder31Sse41(int shiftsNeeded, ReadOnlySpan<int> residual, ReadOnlySpan<int> coeffs, Span<int> output)
            {
                const int Order = 31;
                if (coeffs.Length < Order) return;
                _ = coeffs[Order - 1];
                var vshift = Vector128.CreateScalar(shiftsNeeded);
                ref var o = ref MemoryMarshal.GetReference(output);
                ref var d = ref Unsafe.Add(ref o, Order);
                ref var r = ref MemoryMarshal.GetReference(residual);
                ref var c = ref MemoryMarshal.GetReference(coeffs);
                Vector128<int> sum;
                var vzero = Vector128.Create(0);
                nint dataLength = output.Length - Order;
                var vcoeff0 = Sse2.LoadVector128((int*)Unsafe.AsPointer(ref Unsafe.Add(ref c, 0)));
                var vcoeff1 = Sse2.LoadVector128((int*)Unsafe.AsPointer(ref Unsafe.Add(ref c, 4)));
                var vcoeff2 = Sse2.LoadVector128((int*)Unsafe.AsPointer(ref Unsafe.Add(ref c, 8)));
                var vcoeff3 = Sse2.LoadVector128((int*)Unsafe.AsPointer(ref Unsafe.Add(ref c, 12)));
                var vcoeff4 = Sse2.LoadVector128((int*)Unsafe.AsPointer(ref Unsafe.Add(ref c, 16)));
                var vcoeff5 = Sse2.LoadVector128((int*)Unsafe.AsPointer(ref Unsafe.Add(ref c, 20)));
                var vcoeff6 = Sse2.LoadVector128((int*)Unsafe.AsPointer(ref Unsafe.Add(ref c, 24)));
                var vcoeff7 = Vector128.Create(Unsafe.Add(ref c, 28), Unsafe.Add(ref c, 29), Unsafe.Add(ref c, 30), 0);
                var vprev0 = Sse2.Shuffle(Sse2.LoadVector128((int*)Unsafe.AsPointer(ref Unsafe.Add(ref o, 27))).AsInt32(), 0b00_01_10_11);
                var vprev1 = Sse2.Shuffle(Sse2.LoadVector128((int*)Unsafe.AsPointer(ref Unsafe.Add(ref o, 23))).AsInt32(), 0b00_01_10_11);
                var vprev2 = Sse2.Shuffle(Sse2.LoadVector128((int*)Unsafe.AsPointer(ref Unsafe.Add(ref o, 19))).AsInt32(), 0b00_01_10_11);
                var vprev3 = Sse2.Shuffle(Sse2.LoadVector128((int*)Unsafe.AsPointer(ref Unsafe.Add(ref o, 15))).AsInt32(), 0b00_01_10_11);
                var vprev4 = Sse2.Shuffle(Sse2.LoadVector128((int*)Unsafe.AsPointer(ref Unsafe.Add(ref o, 11))).AsInt32(), 0b00_01_10_11);
                var vprev5 = Sse2.Shuffle(Sse2.LoadVector128((int*)Unsafe.AsPointer(ref Unsafe.Add(ref o, 7))).AsInt32(), 0b00_01_10_11);
                var vprev6 = Sse2.Shuffle(Sse2.LoadVector128((int*)Unsafe.AsPointer(ref Unsafe.Add(ref o, 3))).AsInt32(), 0b00_01_10_11);
                var vprev7 = Vector128.Create(Unsafe.Add(ref o, 2), Unsafe.Add(ref o, 1), Unsafe.Add(ref o, 0), 0);
                for (nint i = 0; i < dataLength; i++)
                {
                    var res = Vector128.CreateScalar(Unsafe.Add(ref r, i));
                    sum = Sse41.MultiplyLow(vcoeff0, vprev0);
                    sum = Sse2.Add(sum, Sse41.MultiplyLow(vcoeff1, vprev1));
                    sum = Sse2.Add(sum, Sse41.MultiplyLow(vcoeff2, vprev2));
                    sum = Sse2.Add(sum, Sse41.MultiplyLow(vcoeff3, vprev3));
                    sum = Sse2.Add(sum, Sse41.MultiplyLow(vcoeff4, vprev4));
                    sum = Sse2.Add(sum, Sse41.MultiplyLow(vcoeff5, vprev5));
                    sum = Sse2.Add(sum, Sse41.MultiplyLow(vcoeff6, vprev6));
                    sum = Sse2.Add(sum, Sse41.MultiplyLow(vcoeff7, vprev7));
                    var y = Ssse3.HorizontalAdd(sum, vzero);
                    y = Ssse3.HorizontalAdd(y, vzero);
                    y = Sse2.ShiftRightArithmetic(y, vshift);   //C# shift operator handling sucks so SSE2 is used instead(same latency, same throughput).
                    y = Sse2.Add(y, res);   //Avoids extract and insert
#if NET5_0_OR_GREATER
                    Sse2.StoreScalar((int*)Unsafe.AsPointer(ref Unsafe.Add(ref d, i)), y);
#else
                    Unsafe.Add(ref d, i) = y.GetElement(0);
#endif
                    y = Sse2.ShiftLeftLogical128BitLane(y, 12);
                    vprev7 = Ssse3.AlignRight(vprev7, vprev6, 12);
                    vprev6 = Ssse3.AlignRight(vprev6, vprev5, 12);
                    vprev5 = Ssse3.AlignRight(vprev5, vprev4, 12);
                    vprev4 = Ssse3.AlignRight(vprev4, vprev3, 12);
                    vprev3 = Ssse3.AlignRight(vprev3, vprev2, 12);
                    vprev2 = Ssse3.AlignRight(vprev2, vprev1, 12);
                    vprev1 = Ssse3.AlignRight(vprev1, vprev0, 12);
                    vprev0 = Ssse3.AlignRight(vprev0, y, 12);
                }
            }

            [MethodImpl(OptimizationUtils.InlineAndOptimizeIfPossible)]
            internal static bool RestoreSignalOrder31Wide(int shiftsNeeded, ReadOnlySpan<int> residual, ReadOnlySpan<int> coeffs, Span<int> output)
            {
                if (Sse41.IsSupported)
                {
                    RestoreSignalOrder31WideSse41(shiftsNeeded, residual, coeffs, output);
                    return true;
                }
                return false;
            }

            [MethodImpl(OptimizationUtils.InlineAndOptimizeIfPossible)]
            internal static unsafe void RestoreSignalOrder31WideSse41(int shiftsNeeded, ReadOnlySpan<int> residual, ReadOnlySpan<int> coeffs, Span<int> output)
            {
                const int Order = 31;
                if(coeffs.Length < Order) return;
                _ = coeffs[Order - 1];
                var vshift = Vector128.CreateScalar((long)shiftsNeeded);
                ref var c = ref MemoryMarshal.GetReference(coeffs);
                ref var o = ref MemoryMarshal.GetReference(output);
                ref var d = ref Unsafe.Add(ref o, Order);
                int dataLength = output.Length - Order;
                ref var r = ref MemoryMarshal.GetReference(residual);
                Vector128<long> sum;
                var vcoeff0 = Sse41.ConvertToVector128Int64(Sse2.LoadScalarVector128((ulong*)Unsafe.AsPointer(ref Unsafe.Add(ref c, 0))).AsUInt32()).AsInt32();
                var vcoeff1 = Sse41.ConvertToVector128Int64(Sse2.LoadScalarVector128((ulong*)Unsafe.AsPointer(ref Unsafe.Add(ref c, 2))).AsUInt32()).AsInt32();
                var vcoeff2 = Sse41.ConvertToVector128Int64(Sse2.LoadScalarVector128((ulong*)Unsafe.AsPointer(ref Unsafe.Add(ref c, 4))).AsUInt32()).AsInt32();
                var vcoeff3 = Sse41.ConvertToVector128Int64(Sse2.LoadScalarVector128((ulong*)Unsafe.AsPointer(ref Unsafe.Add(ref c, 6))).AsUInt32()).AsInt32();
                var vcoeff4 = Sse41.ConvertToVector128Int64(Sse2.LoadScalarVector128((ulong*)Unsafe.AsPointer(ref Unsafe.Add(ref c, 8))).AsUInt32()).AsInt32();
                var vcoeff5 = Sse41.ConvertToVector128Int64(Sse2.LoadScalarVector128((ulong*)Unsafe.AsPointer(ref Unsafe.Add(ref c, 10))).AsUInt32()).AsInt32();
                var vcoeff6 = Sse41.ConvertToVector128Int64(Sse2.LoadScalarVector128((ulong*)Unsafe.AsPointer(ref Unsafe.Add(ref c, 12))).AsUInt32()).AsInt32();
                var vcoeff7 = Sse41.ConvertToVector128Int64(Sse2.LoadScalarVector128((ulong*)Unsafe.AsPointer(ref Unsafe.Add(ref c, 14))).AsUInt32()).AsInt32();
                var vcoeff8 = Sse41.ConvertToVector128Int64(Sse2.LoadScalarVector128((ulong*)Unsafe.AsPointer(ref Unsafe.Add(ref c, 16))).AsUInt32()).AsInt32();
                var vcoeff9 = Sse41.ConvertToVector128Int64(Sse2.LoadScalarVector128((ulong*)Unsafe.AsPointer(ref Unsafe.Add(ref c, 18))).AsUInt32()).AsInt32();
                var vcoeff10 = Sse41.ConvertToVector128Int64(Sse2.LoadScalarVector128((ulong*)Unsafe.AsPointer(ref Unsafe.Add(ref c, 20))).AsUInt32()).AsInt32();
                var vcoeff11 = Sse41.ConvertToVector128Int64(Sse2.LoadScalarVector128((ulong*)Unsafe.AsPointer(ref Unsafe.Add(ref c, 22))).AsUInt32()).AsInt32();
                var vcoeff12 = Sse41.ConvertToVector128Int64(Sse2.LoadScalarVector128((ulong*)Unsafe.AsPointer(ref Unsafe.Add(ref c, 24))).AsUInt32()).AsInt32();
                var vcoeff13 = Sse41.ConvertToVector128Int64(Sse2.LoadScalarVector128((ulong*)Unsafe.AsPointer(ref Unsafe.Add(ref c, 26))).AsUInt32()).AsInt32();
                var vcoeff14 = Sse41.ConvertToVector128Int64(Sse2.LoadScalarVector128((ulong*)Unsafe.AsPointer(ref Unsafe.Add(ref c, 28))).AsUInt32()).AsInt32();
                var vcoeff15 = Sse41.ConvertToVector128Int64(Sse2.LoadScalarVector128((uint*)Unsafe.AsPointer(ref Unsafe.Add(ref c, 30))).AsUInt32()).AsInt32();
                var vprev0 = Sse2.Shuffle(Sse2.LoadScalarVector128((ulong*)Unsafe.AsPointer(ref Unsafe.Add(ref o, 29))).AsInt32(), 0b11_00_11_01);
                var vprev1 = Sse2.Shuffle(Sse2.LoadScalarVector128((ulong*)Unsafe.AsPointer(ref Unsafe.Add(ref o, 27))).AsInt32(), 0b11_00_11_01);
                var vprev2 = Sse2.Shuffle(Sse2.LoadScalarVector128((ulong*)Unsafe.AsPointer(ref Unsafe.Add(ref o, 25))).AsInt32(), 0b11_00_11_01);
                var vprev3 = Sse2.Shuffle(Sse2.LoadScalarVector128((ulong*)Unsafe.AsPointer(ref Unsafe.Add(ref o, 23))).AsInt32(), 0b11_00_11_01);
                var vprev4 = Sse2.Shuffle(Sse2.LoadScalarVector128((ulong*)Unsafe.AsPointer(ref Unsafe.Add(ref o, 21))).AsInt32(), 0b11_00_11_01);
                var vprev5 = Sse2.Shuffle(Sse2.LoadScalarVector128((ulong*)Unsafe.AsPointer(ref Unsafe.Add(ref o, 19))).AsInt32(), 0b11_00_11_01);
                var vprev6 = Sse2.Shuffle(Sse2.LoadScalarVector128((ulong*)Unsafe.AsPointer(ref Unsafe.Add(ref o, 17))).AsInt32(), 0b11_00_11_01);
                var vprev7 = Sse2.Shuffle(Sse2.LoadScalarVector128((ulong*)Unsafe.AsPointer(ref Unsafe.Add(ref o, 15))).AsInt32(), 0b11_00_11_01);
                var vprev8 = Sse2.Shuffle(Sse2.LoadScalarVector128((ulong*)Unsafe.AsPointer(ref Unsafe.Add(ref o, 13))).AsInt32(), 0b11_00_11_01);
                var vprev9 = Sse2.Shuffle(Sse2.LoadScalarVector128((ulong*)Unsafe.AsPointer(ref Unsafe.Add(ref o, 11))).AsInt32(), 0b11_00_11_01);
                var vprev10 = Sse2.Shuffle(Sse2.LoadScalarVector128((ulong*)Unsafe.AsPointer(ref Unsafe.Add(ref o, 9))).AsInt32(), 0b11_00_11_01);
                var vprev11 = Sse2.Shuffle(Sse2.LoadScalarVector128((ulong*)Unsafe.AsPointer(ref Unsafe.Add(ref o, 7))).AsInt32(), 0b11_00_11_01);
                var vprev12 = Sse2.Shuffle(Sse2.LoadScalarVector128((ulong*)Unsafe.AsPointer(ref Unsafe.Add(ref o, 5))).AsInt32(), 0b11_00_11_01);
                var vprev13 = Sse2.Shuffle(Sse2.LoadScalarVector128((ulong*)Unsafe.AsPointer(ref Unsafe.Add(ref o, 3))).AsInt32(), 0b11_00_11_01);
                var vprev14 = Sse2.Shuffle(Sse2.LoadScalarVector128((ulong*)Unsafe.AsPointer(ref Unsafe.Add(ref o, 1))).AsInt32(), 0b11_00_11_01);
                var vprev15 = Sse2.Shuffle(Sse2.LoadScalarVector128((ulong*)Unsafe.AsPointer(ref Unsafe.Add(ref o, 0))).AsInt32(), 0b11_00_11_00);
                for(nint i = 0; i < dataLength;)
                {
                    var res = Vector128.CreateScalar(Unsafe.Add(ref r, i));
                    sum = Sse41.Multiply(vcoeff0, vprev0);
                    sum = Sse2.Add(sum, Sse41.Multiply(vcoeff1, vprev1));
                    sum = Sse2.Add(sum, Sse41.Multiply(vcoeff2, vprev2));
                    sum = Sse2.Add(sum, Sse41.Multiply(vcoeff3, vprev3));
                    sum = Sse2.Add(sum, Sse41.Multiply(vcoeff4, vprev4));
                    sum = Sse2.Add(sum, Sse41.Multiply(vcoeff5, vprev5));
                    sum = Sse2.Add(sum, Sse41.Multiply(vcoeff6, vprev6));
                    sum = Sse2.Add(sum, Sse41.Multiply(vcoeff7, vprev7));
                    sum = Sse2.Add(sum, Sse41.Multiply(vcoeff8, vprev8));
                    sum = Sse2.Add(sum, Sse41.Multiply(vcoeff9, vprev9));
                    sum = Sse2.Add(sum, Sse41.Multiply(vcoeff10, vprev10));
                    sum = Sse2.Add(sum, Sse41.Multiply(vcoeff11, vprev11));
                    sum = Sse2.Add(sum, Sse41.Multiply(vcoeff12, vprev12));
                    sum = Sse2.Add(sum, Sse41.Multiply(vcoeff13, vprev13));
                    sum = Sse2.Add(sum, Sse41.Multiply(vcoeff14, vprev14));
                    sum = Sse2.Add(sum, Sse41.Multiply(vcoeff15, vprev15));
                    sum = Sse2.Add(sum, Sse2.ShiftRightLogical128BitLane(sum, 8));
                    sum = Sse2.ShiftRightLogical(sum, vshift);
                    var yy = Sse2.Add(sum.AsInt32(), res);
#if NET5_0_OR_GREATER
                    Sse2.StoreScalar((int*)Unsafe.AsPointer(ref Unsafe.Add(ref d, i)), yy);
#else
                    Unsafe.Add(ref d, i) = yy.GetElement(0);
#endif
                    i++;
                    yy = Sse2.ShiftLeftLogical128BitLane(yy, 8);
                    vprev15 = Ssse3.AlignRight(vprev15, vprev14, 8);
                    vprev14 = Ssse3.AlignRight(vprev14, vprev13, 8);
                    vprev13 = Ssse3.AlignRight(vprev13, vprev12, 8);
                    vprev12 = Ssse3.AlignRight(vprev12, vprev11, 8);
                    vprev11 = Ssse3.AlignRight(vprev11, vprev10, 8);
                    vprev10 = Ssse3.AlignRight(vprev10, vprev9, 8);
                    vprev9 = Ssse3.AlignRight(vprev9, vprev8, 8);
                    vprev8 = Ssse3.AlignRight(vprev8, vprev7, 8);
                    vprev7 = Ssse3.AlignRight(vprev7, vprev6, 8);
                    vprev6 = Ssse3.AlignRight(vprev6, vprev5, 8);
                    vprev5 = Ssse3.AlignRight(vprev5, vprev4, 8);
                    vprev4 = Ssse3.AlignRight(vprev4, vprev3, 8);
                    vprev3 = Ssse3.AlignRight(vprev3, vprev2, 8);
                    vprev2 = Ssse3.AlignRight(vprev2, vprev1, 8);
                    vprev1 = Ssse3.AlignRight(vprev1, vprev0, 8);
                    vprev0 = Ssse3.AlignRight(vprev0, yy, 8);
                }
            }
            
            [MethodImpl(OptimizationUtils.InlineAndOptimizeIfPossible)]
            internal static unsafe void RestoreSignalOrder31WideAvx2(int shiftsNeeded, ReadOnlySpan<int> residual, ReadOnlySpan<int> coeffs, Span<int> output)
            {
                const int Order = 31;
                if(coeffs.Length < Order) return;
                _ = coeffs[Order - 1];
                var vshift = Vector128.CreateScalar((long)shiftsNeeded);
                var mask = Vector128.Create(~0, ~0, ~0, 0);
                ref var c = ref MemoryMarshal.GetReference(coeffs);
                ref var o = ref MemoryMarshal.GetReference(output);
                ref var d = ref Unsafe.Add(ref o, Order);
                int dataLength = output.Length - Order;
                ref var r = ref MemoryMarshal.GetReference(residual);
                Vector128<long> sum;
                Vector256<long> sum256;
                var vcoeff0 = Avx2.ConvertToVector256Int64(Sse2.LoadVector128((int*)Unsafe.AsPointer(ref Unsafe.Add(ref c, 0))).AsUInt32()).AsInt32();
                var vcoeff1 = Avx2.ConvertToVector256Int64(Sse2.LoadVector128((int*)Unsafe.AsPointer(ref Unsafe.Add(ref c, 4))).AsUInt32()).AsInt32();
                var vcoeff2 = Avx2.ConvertToVector256Int64(Sse2.LoadVector128((int*)Unsafe.AsPointer(ref Unsafe.Add(ref c, 8))).AsUInt32()).AsInt32();
                var vcoeff3 = Avx2.ConvertToVector256Int64(Sse2.LoadVector128((int*)Unsafe.AsPointer(ref Unsafe.Add(ref c, 12))).AsUInt32()).AsInt32();
                var vcoeff4 = Avx2.ConvertToVector256Int64(Sse2.LoadVector128((int*)Unsafe.AsPointer(ref Unsafe.Add(ref c, 16))).AsUInt32()).AsInt32();
                var vcoeff5 = Avx2.ConvertToVector256Int64(Sse2.LoadVector128((int*)Unsafe.AsPointer(ref Unsafe.Add(ref c, 20))).AsUInt32()).AsInt32();
                var vcoeff6 = Avx2.ConvertToVector256Int64(Sse2.LoadVector128((int*)Unsafe.AsPointer(ref Unsafe.Add(ref c, 24))).AsUInt32()).AsInt32();
                var vcoeff7 = Avx2.ConvertToVector256Int64(Avx2.MaskLoad((int*)Unsafe.AsPointer(ref Unsafe.Add(ref c, 28)), mask)).AsInt32();
                var vprev0 = Avx2.ConvertToVector256Int64(Sse2.Shuffle(Sse2.LoadScalarVector128((ulong*)Unsafe.AsPointer(ref Unsafe.Add(ref o, 28))).AsInt32(), 0b00_01_10_11).AsUInt32()).AsInt32();
                var vprev1 = Avx2.ConvertToVector256Int64(Sse2.Shuffle(Sse2.LoadScalarVector128((ulong*)Unsafe.AsPointer(ref Unsafe.Add(ref o, 24))).AsInt32(), 0b00_01_10_11).AsUInt32()).AsInt32();
                var vprev2 = Avx2.ConvertToVector256Int64(Sse2.Shuffle(Sse2.LoadScalarVector128((ulong*)Unsafe.AsPointer(ref Unsafe.Add(ref o, 20))).AsInt32(), 0b00_01_10_11).AsUInt32()).AsInt32();
                var vprev3 = Avx2.ConvertToVector256Int64(Sse2.Shuffle(Sse2.LoadScalarVector128((ulong*)Unsafe.AsPointer(ref Unsafe.Add(ref o, 16))).AsInt32(), 0b00_01_10_11).AsUInt32()).AsInt32();
                var vprev4 = Avx2.ConvertToVector256Int64(Sse2.Shuffle(Sse2.LoadScalarVector128((ulong*)Unsafe.AsPointer(ref Unsafe.Add(ref o, 12))).AsInt32(), 0b00_01_10_11).AsUInt32()).AsInt32();
                var vprev5 = Avx2.ConvertToVector256Int64(Sse2.Shuffle(Sse2.LoadScalarVector128((ulong*)Unsafe.AsPointer(ref Unsafe.Add(ref o, 8))).AsInt32(), 0b00_01_10_11).AsUInt32()).AsInt32();
                var vprev6 = Avx2.ConvertToVector256Int64(Sse2.Shuffle(Sse2.LoadScalarVector128((ulong*)Unsafe.AsPointer(ref Unsafe.Add(ref o, 4))).AsInt32(), 0b00_01_10_11).AsUInt32()).AsInt32();
                var vprev7 = Avx2.ConvertToVector256Int64(Sse2.Shuffle(Avx2.MaskLoad((int*)Unsafe.AsPointer(ref o), mask).AsInt32(), 0b00_01_10_11)).AsInt32();
                for(nint i = 0; i < dataLength;)
                {
                    var res = Vector128.CreateScalar(Unsafe.Add(ref r, i));
                    sum256 = Avx2.Multiply(vcoeff0, vprev0);
                    sum256 = Avx2.Add(sum256, Avx2.Multiply(vcoeff1, vprev1));
                    sum256 = Avx2.Add(sum256, Avx2.Multiply(vcoeff2, vprev2));
                    sum256 = Avx2.Add(sum256, Avx2.Multiply(vcoeff3, vprev3));
                    sum256 = Avx2.Add(sum256, Avx2.Multiply(vcoeff4, vprev4));
                    sum256 = Avx2.Add(sum256, Avx2.Multiply(vcoeff5, vprev5));
                    sum256 = Avx2.Add(sum256, Avx2.Multiply(vcoeff6, vprev6));
                    sum256 = Avx2.Add(sum256, Avx2.Multiply(vcoeff7, vprev7));
                    sum = Sse2.Add(sum256.GetLower(), sum256.GetUpper());
                    sum = Sse2.Add(sum, Sse2.ShiftRightLogical128BitLane(sum, 8));
                    sum = Sse2.ShiftRightLogical(sum, vshift);
                    var yy = Sse2.Add(sum.AsInt32(), res);
#if NET5_0_OR_GREATER
                    Sse2.StoreScalar((int*)Unsafe.AsPointer(ref Unsafe.Add(ref d, i)), yy);
#else
                    Unsafe.Add(ref d, i) = yy.GetElement(0);
#endif
                    i++;
                    var yu = yy.ToVector256();
                    yu = Avx2.Permute4x64(yu.AsUInt64(), 0b00_00_00_00).AsInt32();
                    vprev7 = Avx2.Permute4x64(vprev7.AsInt64(), 0b10_01_00_11).AsInt32();
                    vprev7 = Avx2.Blend(vprev7, vprev6, 0b0000_0001);
                    vprev6 = Avx2.Permute4x64(vprev6.AsInt64(), 0b10_01_00_11).AsInt32();
                    vprev6 = Avx2.Blend(vprev6, vprev5, 0b0000_0001);
                    vprev5 = Avx2.Permute4x64(vprev5.AsInt64(), 0b10_01_00_11).AsInt32();
                    vprev5 = Avx2.Blend(vprev5, vprev4, 0b0000_0001);
                    vprev4 = Avx2.Permute4x64(vprev4.AsInt64(), 0b10_01_00_11).AsInt32();
                    vprev4 = Avx2.Blend(vprev4, vprev3, 0b0000_0001);
                    vprev3 = Avx2.Permute4x64(vprev3.AsInt64(), 0b10_01_00_11).AsInt32();
                    vprev3 = Avx2.Blend(vprev3, vprev2, 0b0000_0001);
                    vprev2 = Avx2.Permute4x64(vprev2.AsInt64(), 0b10_01_00_11).AsInt32();
                    vprev2 = Avx2.Blend(vprev2, vprev1, 0b0000_0001);
                    vprev1 = Avx2.Permute4x64(vprev1.AsInt64(), 0b10_01_00_11).AsInt32();
                    vprev1 = Avx2.Blend(vprev1, vprev0, 0b0000_0001);
                    vprev0 = Avx2.Permute4x64(vprev0.AsInt64(), 0b10_01_00_11).AsInt32();
                    vprev0 = Avx2.Blend(vprev0, yu, 0b0000_0001);
                }
            }
#endregion Order31
#region Order32

            [MethodImpl(OptimizationUtils.InlineAndOptimizeIfPossible)]
            internal static bool RestoreSignalOrder32(int shiftsNeeded, ReadOnlySpan<int> residual, ReadOnlySpan<int> coeffs, Span<int> output)
            {
                if (Sse41.IsSupported)
                {
                    RestoreSignalOrder32Sse41(shiftsNeeded, residual, coeffs, output);
                    return true;
                }
                return false;
            }

            [MethodImpl(OptimizationUtils.InlineAndOptimizeIfPossible)]
            internal static unsafe void RestoreSignalOrder32Sse41(int shiftsNeeded, ReadOnlySpan<int> residual, ReadOnlySpan<int> coeffs, Span<int> output)
            {
                const int Order = 32;
                if (coeffs.Length < Order) return;
                _ = coeffs[Order - 1];
                var vshift = Vector128.CreateScalar(shiftsNeeded);
                ref var o = ref MemoryMarshal.GetReference(output);
                ref var d = ref Unsafe.Add(ref o, Order);
                ref var r = ref MemoryMarshal.GetReference(residual);
                ref var c = ref MemoryMarshal.GetReference(coeffs);
                Vector128<int> sum;
                var vzero = Vector128.Create(0);
                nint dataLength = output.Length - Order;
                var vcoeff0 = Sse2.LoadVector128((int*)Unsafe.AsPointer(ref Unsafe.Add(ref c, 0)));
                var vcoeff1 = Sse2.LoadVector128((int*)Unsafe.AsPointer(ref Unsafe.Add(ref c, 4)));
                var vcoeff2 = Sse2.LoadVector128((int*)Unsafe.AsPointer(ref Unsafe.Add(ref c, 8)));
                var vcoeff3 = Sse2.LoadVector128((int*)Unsafe.AsPointer(ref Unsafe.Add(ref c, 12)));
                var vcoeff4 = Sse2.LoadVector128((int*)Unsafe.AsPointer(ref Unsafe.Add(ref c, 16)));
                var vcoeff5 = Sse2.LoadVector128((int*)Unsafe.AsPointer(ref Unsafe.Add(ref c, 20)));
                var vcoeff6 = Sse2.LoadVector128((int*)Unsafe.AsPointer(ref Unsafe.Add(ref c, 24)));
                var vcoeff7 = Sse2.LoadVector128((int*)Unsafe.AsPointer(ref Unsafe.Add(ref c, 28)));
                var vprev0 = Sse2.Shuffle(Sse2.LoadVector128((int*)Unsafe.AsPointer(ref Unsafe.Add(ref o, 28))).AsInt32(), 0b00_01_10_11);
                var vprev1 = Sse2.Shuffle(Sse2.LoadVector128((int*)Unsafe.AsPointer(ref Unsafe.Add(ref o, 24))).AsInt32(), 0b00_01_10_11);
                var vprev2 = Sse2.Shuffle(Sse2.LoadVector128((int*)Unsafe.AsPointer(ref Unsafe.Add(ref o, 20))).AsInt32(), 0b00_01_10_11);
                var vprev3 = Sse2.Shuffle(Sse2.LoadVector128((int*)Unsafe.AsPointer(ref Unsafe.Add(ref o, 16))).AsInt32(), 0b00_01_10_11);
                var vprev4 = Sse2.Shuffle(Sse2.LoadVector128((int*)Unsafe.AsPointer(ref Unsafe.Add(ref o, 12))).AsInt32(), 0b00_01_10_11);
                var vprev5 = Sse2.Shuffle(Sse2.LoadVector128((int*)Unsafe.AsPointer(ref Unsafe.Add(ref o, 8))).AsInt32(), 0b00_01_10_11);
                var vprev6 = Sse2.Shuffle(Sse2.LoadVector128((int*)Unsafe.AsPointer(ref Unsafe.Add(ref o, 4))).AsInt32(), 0b00_01_10_11);
                var vprev7 = Sse2.Shuffle(Sse2.LoadVector128((int*)Unsafe.AsPointer(ref Unsafe.Add(ref o, 0))).AsInt32(), 0b00_01_10_11);
                var vprev8 = Vector128.Create(0, 0, 0, 0);
                for (nint i = 0; i < dataLength; i++)
                {
                    var res = Vector128.CreateScalar(Unsafe.Add(ref r, i));
                    sum = Sse41.MultiplyLow(vcoeff0, vprev0);
                    sum = Sse2.Add(sum, Sse41.MultiplyLow(vcoeff1, vprev1));
                    sum = Sse2.Add(sum, Sse41.MultiplyLow(vcoeff2, vprev2));
                    sum = Sse2.Add(sum, Sse41.MultiplyLow(vcoeff3, vprev3));
                    sum = Sse2.Add(sum, Sse41.MultiplyLow(vcoeff4, vprev4));
                    sum = Sse2.Add(sum, Sse41.MultiplyLow(vcoeff5, vprev5));
                    sum = Sse2.Add(sum, Sse41.MultiplyLow(vcoeff6, vprev6));
                    sum = Sse2.Add(sum, Sse41.MultiplyLow(vcoeff7, vprev7));
                    var y = Ssse3.HorizontalAdd(sum, vzero);
                    y = Ssse3.HorizontalAdd(y, vzero);
                    y = Sse2.ShiftRightArithmetic(y, vshift);   //C# shift operator handling sucks so SSE2 is used instead(same latency, same throughput).
                    y = Sse2.Add(y, res);   //Avoids extract and insert
#if NET5_0_OR_GREATER
                    Sse2.StoreScalar((int*)Unsafe.AsPointer(ref Unsafe.Add(ref d, i)), y);
#else
                    Unsafe.Add(ref d, i) = y.GetElement(0);
#endif
                    y = Sse2.ShiftLeftLogical128BitLane(y, 12);
                    vprev7 = Ssse3.AlignRight(vprev7, vprev6, 12);
                    vprev6 = Ssse3.AlignRight(vprev6, vprev5, 12);
                    vprev5 = Ssse3.AlignRight(vprev5, vprev4, 12);
                    vprev4 = Ssse3.AlignRight(vprev4, vprev3, 12);
                    vprev3 = Ssse3.AlignRight(vprev3, vprev2, 12);
                    vprev2 = Ssse3.AlignRight(vprev2, vprev1, 12);
                    vprev1 = Ssse3.AlignRight(vprev1, vprev0, 12);
                    vprev0 = Ssse3.AlignRight(vprev0, y, 12);
                }
            }

            [MethodImpl(OptimizationUtils.InlineAndOptimizeIfPossible)]
            internal static bool RestoreSignalOrder32Wide(int shiftsNeeded, ReadOnlySpan<int> residual, ReadOnlySpan<int> coeffs, Span<int> output)
            {
                if (Sse41.IsSupported)
                {
                    RestoreSignalOrder32WideSse41(shiftsNeeded, residual, coeffs, output);
                    return true;
                }
                return false;
            }

            [MethodImpl(OptimizationUtils.InlineAndOptimizeIfPossible)]
            internal static unsafe void RestoreSignalOrder32WideSse41(int shiftsNeeded, ReadOnlySpan<int> residual, ReadOnlySpan<int> coeffs, Span<int> output)
            {
                const int Order = 32;
                if(coeffs.Length < Order) return;
                _ = coeffs[Order - 1];
                var vshift = Vector128.CreateScalar((long)shiftsNeeded);
                ref var c = ref MemoryMarshal.GetReference(coeffs);
                ref var o = ref MemoryMarshal.GetReference(output);
                ref var d = ref Unsafe.Add(ref o, Order);
                int dataLength = output.Length - Order;
                ref var r = ref MemoryMarshal.GetReference(residual);
                Vector128<long> sum;
                var vcoeff0 = Sse41.ConvertToVector128Int64(Sse2.LoadScalarVector128((ulong*)Unsafe.AsPointer(ref Unsafe.Add(ref c, 0))).AsUInt32()).AsInt32();
                var vcoeff1 = Sse41.ConvertToVector128Int64(Sse2.LoadScalarVector128((ulong*)Unsafe.AsPointer(ref Unsafe.Add(ref c, 2))).AsUInt32()).AsInt32();
                var vcoeff2 = Sse41.ConvertToVector128Int64(Sse2.LoadScalarVector128((ulong*)Unsafe.AsPointer(ref Unsafe.Add(ref c, 4))).AsUInt32()).AsInt32();
                var vcoeff3 = Sse41.ConvertToVector128Int64(Sse2.LoadScalarVector128((ulong*)Unsafe.AsPointer(ref Unsafe.Add(ref c, 6))).AsUInt32()).AsInt32();
                var vcoeff4 = Sse41.ConvertToVector128Int64(Sse2.LoadScalarVector128((ulong*)Unsafe.AsPointer(ref Unsafe.Add(ref c, 8))).AsUInt32()).AsInt32();
                var vcoeff5 = Sse41.ConvertToVector128Int64(Sse2.LoadScalarVector128((ulong*)Unsafe.AsPointer(ref Unsafe.Add(ref c, 10))).AsUInt32()).AsInt32();
                var vcoeff6 = Sse41.ConvertToVector128Int64(Sse2.LoadScalarVector128((ulong*)Unsafe.AsPointer(ref Unsafe.Add(ref c, 12))).AsUInt32()).AsInt32();
                var vcoeff7 = Sse41.ConvertToVector128Int64(Sse2.LoadScalarVector128((ulong*)Unsafe.AsPointer(ref Unsafe.Add(ref c, 14))).AsUInt32()).AsInt32();
                var vcoeff8 = Sse41.ConvertToVector128Int64(Sse2.LoadScalarVector128((ulong*)Unsafe.AsPointer(ref Unsafe.Add(ref c, 16))).AsUInt32()).AsInt32();
                var vcoeff9 = Sse41.ConvertToVector128Int64(Sse2.LoadScalarVector128((ulong*)Unsafe.AsPointer(ref Unsafe.Add(ref c, 18))).AsUInt32()).AsInt32();
                var vcoeff10 = Sse41.ConvertToVector128Int64(Sse2.LoadScalarVector128((ulong*)Unsafe.AsPointer(ref Unsafe.Add(ref c, 20))).AsUInt32()).AsInt32();
                var vcoeff11 = Sse41.ConvertToVector128Int64(Sse2.LoadScalarVector128((ulong*)Unsafe.AsPointer(ref Unsafe.Add(ref c, 22))).AsUInt32()).AsInt32();
                var vcoeff12 = Sse41.ConvertToVector128Int64(Sse2.LoadScalarVector128((ulong*)Unsafe.AsPointer(ref Unsafe.Add(ref c, 24))).AsUInt32()).AsInt32();
                var vcoeff13 = Sse41.ConvertToVector128Int64(Sse2.LoadScalarVector128((ulong*)Unsafe.AsPointer(ref Unsafe.Add(ref c, 26))).AsUInt32()).AsInt32();
                var vcoeff14 = Sse41.ConvertToVector128Int64(Sse2.LoadScalarVector128((ulong*)Unsafe.AsPointer(ref Unsafe.Add(ref c, 28))).AsUInt32()).AsInt32();
                var vcoeff15 = Sse41.ConvertToVector128Int64(Sse2.LoadScalarVector128((ulong*)Unsafe.AsPointer(ref Unsafe.Add(ref c, 30))).AsUInt32()).AsInt32();
                var vprev0 = Sse2.Shuffle(Sse2.LoadScalarVector128((ulong*)Unsafe.AsPointer(ref Unsafe.Add(ref o, 30))).AsInt32(), 0b11_00_11_01);
                var vprev1 = Sse2.Shuffle(Sse2.LoadScalarVector128((ulong*)Unsafe.AsPointer(ref Unsafe.Add(ref o, 28))).AsInt32(), 0b11_00_11_01);
                var vprev2 = Sse2.Shuffle(Sse2.LoadScalarVector128((ulong*)Unsafe.AsPointer(ref Unsafe.Add(ref o, 26))).AsInt32(), 0b11_00_11_01);
                var vprev3 = Sse2.Shuffle(Sse2.LoadScalarVector128((ulong*)Unsafe.AsPointer(ref Unsafe.Add(ref o, 24))).AsInt32(), 0b11_00_11_01);
                var vprev4 = Sse2.Shuffle(Sse2.LoadScalarVector128((ulong*)Unsafe.AsPointer(ref Unsafe.Add(ref o, 22))).AsInt32(), 0b11_00_11_01);
                var vprev5 = Sse2.Shuffle(Sse2.LoadScalarVector128((ulong*)Unsafe.AsPointer(ref Unsafe.Add(ref o, 20))).AsInt32(), 0b11_00_11_01);
                var vprev6 = Sse2.Shuffle(Sse2.LoadScalarVector128((ulong*)Unsafe.AsPointer(ref Unsafe.Add(ref o, 18))).AsInt32(), 0b11_00_11_01);
                var vprev7 = Sse2.Shuffle(Sse2.LoadScalarVector128((ulong*)Unsafe.AsPointer(ref Unsafe.Add(ref o, 16))).AsInt32(), 0b11_00_11_01);
                var vprev8 = Sse2.Shuffle(Sse2.LoadScalarVector128((ulong*)Unsafe.AsPointer(ref Unsafe.Add(ref o, 14))).AsInt32(), 0b11_00_11_01);
                var vprev9 = Sse2.Shuffle(Sse2.LoadScalarVector128((ulong*)Unsafe.AsPointer(ref Unsafe.Add(ref o, 12))).AsInt32(), 0b11_00_11_01);
                var vprev10 = Sse2.Shuffle(Sse2.LoadScalarVector128((ulong*)Unsafe.AsPointer(ref Unsafe.Add(ref o, 10))).AsInt32(), 0b11_00_11_01);
                var vprev11 = Sse2.Shuffle(Sse2.LoadScalarVector128((ulong*)Unsafe.AsPointer(ref Unsafe.Add(ref o, 8))).AsInt32(), 0b11_00_11_01);
                var vprev12 = Sse2.Shuffle(Sse2.LoadScalarVector128((ulong*)Unsafe.AsPointer(ref Unsafe.Add(ref o, 6))).AsInt32(), 0b11_00_11_01);
                var vprev13 = Sse2.Shuffle(Sse2.LoadScalarVector128((ulong*)Unsafe.AsPointer(ref Unsafe.Add(ref o, 4))).AsInt32(), 0b11_00_11_01);
                var vprev14 = Sse2.Shuffle(Sse2.LoadScalarVector128((ulong*)Unsafe.AsPointer(ref Unsafe.Add(ref o, 2))).AsInt32(), 0b11_00_11_01);
                var vprev15 = Sse2.Shuffle(Sse2.LoadScalarVector128((ulong*)Unsafe.AsPointer(ref Unsafe.Add(ref o, 0))).AsInt32(), 0b11_00_11_01);
                for(nint i = 0; i < dataLength;)
                {
                    var res = Vector128.CreateScalar(Unsafe.Add(ref r, i));
                    sum = Sse41.Multiply(vcoeff0, vprev0);
                    sum = Sse2.Add(sum, Sse41.Multiply(vcoeff1, vprev1));
                    sum = Sse2.Add(sum, Sse41.Multiply(vcoeff2, vprev2));
                    sum = Sse2.Add(sum, Sse41.Multiply(vcoeff3, vprev3));
                    sum = Sse2.Add(sum, Sse41.Multiply(vcoeff4, vprev4));
                    sum = Sse2.Add(sum, Sse41.Multiply(vcoeff5, vprev5));
                    sum = Sse2.Add(sum, Sse41.Multiply(vcoeff6, vprev6));
                    sum = Sse2.Add(sum, Sse41.Multiply(vcoeff7, vprev7));
                    sum = Sse2.Add(sum, Sse41.Multiply(vcoeff8, vprev8));
                    sum = Sse2.Add(sum, Sse41.Multiply(vcoeff9, vprev9));
                    sum = Sse2.Add(sum, Sse41.Multiply(vcoeff10, vprev10));
                    sum = Sse2.Add(sum, Sse41.Multiply(vcoeff11, vprev11));
                    sum = Sse2.Add(sum, Sse41.Multiply(vcoeff12, vprev12));
                    sum = Sse2.Add(sum, Sse41.Multiply(vcoeff13, vprev13));
                    sum = Sse2.Add(sum, Sse41.Multiply(vcoeff14, vprev14));
                    sum = Sse2.Add(sum, Sse41.Multiply(vcoeff15, vprev15));
                    sum = Sse2.Add(sum, Sse2.ShiftRightLogical128BitLane(sum, 8));
                    sum = Sse2.ShiftRightLogical(sum, vshift);
                    var yy = Sse2.Add(sum.AsInt32(), res);
#if NET5_0_OR_GREATER
                    Sse2.StoreScalar((int*)Unsafe.AsPointer(ref Unsafe.Add(ref d, i)), yy);
#else
                    Unsafe.Add(ref d, i) = yy.GetElement(0);
#endif
                    i++;
                    yy = Sse2.ShiftLeftLogical128BitLane(yy, 8);
                    vprev15 = Ssse3.AlignRight(vprev15, vprev14, 8);
                    vprev14 = Ssse3.AlignRight(vprev14, vprev13, 8);
                    vprev13 = Ssse3.AlignRight(vprev13, vprev12, 8);
                    vprev12 = Ssse3.AlignRight(vprev12, vprev11, 8);
                    vprev11 = Ssse3.AlignRight(vprev11, vprev10, 8);
                    vprev10 = Ssse3.AlignRight(vprev10, vprev9, 8);
                    vprev9 = Ssse3.AlignRight(vprev9, vprev8, 8);
                    vprev8 = Ssse3.AlignRight(vprev8, vprev7, 8);
                    vprev7 = Ssse3.AlignRight(vprev7, vprev6, 8);
                    vprev6 = Ssse3.AlignRight(vprev6, vprev5, 8);
                    vprev5 = Ssse3.AlignRight(vprev5, vprev4, 8);
                    vprev4 = Ssse3.AlignRight(vprev4, vprev3, 8);
                    vprev3 = Ssse3.AlignRight(vprev3, vprev2, 8);
                    vprev2 = Ssse3.AlignRight(vprev2, vprev1, 8);
                    vprev1 = Ssse3.AlignRight(vprev1, vprev0, 8);
                    vprev0 = Ssse3.AlignRight(vprev0, yy, 8);
                }
            }
            
            [MethodImpl(OptimizationUtils.InlineAndOptimizeIfPossible)]
            internal static unsafe void RestoreSignalOrder32WideAvx2(int shiftsNeeded, ReadOnlySpan<int> residual, ReadOnlySpan<int> coeffs, Span<int> output)
            {
                const int Order = 32;
                if(coeffs.Length < Order) return;
                _ = coeffs[Order - 1];
                var vshift = Vector128.CreateScalar((long)shiftsNeeded);
                ref var c = ref MemoryMarshal.GetReference(coeffs);
                ref var o = ref MemoryMarshal.GetReference(output);
                ref var d = ref Unsafe.Add(ref o, Order);
                int dataLength = output.Length - Order;
                ref var r = ref MemoryMarshal.GetReference(residual);
                Vector128<long> sum;
                Vector256<long> sum256;
                var vcoeff0 = Avx2.ConvertToVector256Int64(Sse2.LoadVector128((int*)Unsafe.AsPointer(ref Unsafe.Add(ref c, 0))).AsUInt32()).AsInt32();
                var vcoeff1 = Avx2.ConvertToVector256Int64(Sse2.LoadVector128((int*)Unsafe.AsPointer(ref Unsafe.Add(ref c, 4))).AsUInt32()).AsInt32();
                var vcoeff2 = Avx2.ConvertToVector256Int64(Sse2.LoadVector128((int*)Unsafe.AsPointer(ref Unsafe.Add(ref c, 8))).AsUInt32()).AsInt32();
                var vcoeff3 = Avx2.ConvertToVector256Int64(Sse2.LoadVector128((int*)Unsafe.AsPointer(ref Unsafe.Add(ref c, 12))).AsUInt32()).AsInt32();
                var vcoeff4 = Avx2.ConvertToVector256Int64(Sse2.LoadVector128((int*)Unsafe.AsPointer(ref Unsafe.Add(ref c, 16))).AsUInt32()).AsInt32();
                var vcoeff5 = Avx2.ConvertToVector256Int64(Sse2.LoadVector128((int*)Unsafe.AsPointer(ref Unsafe.Add(ref c, 20))).AsUInt32()).AsInt32();
                var vcoeff6 = Avx2.ConvertToVector256Int64(Sse2.LoadVector128((int*)Unsafe.AsPointer(ref Unsafe.Add(ref c, 24))).AsUInt32()).AsInt32();
                var vcoeff7 = Avx2.ConvertToVector256Int64(Sse2.LoadVector128((int*)Unsafe.AsPointer(ref Unsafe.Add(ref c, 28))).AsUInt32()).AsInt32();
                var vprev0 = Avx2.ConvertToVector256Int64(Sse2.Shuffle(Sse2.LoadScalarVector128((ulong*)Unsafe.AsPointer(ref Unsafe.Add(ref o, 28))).AsInt32(), 0b00_01_10_11).AsUInt32()).AsInt32();
                var vprev1 = Avx2.ConvertToVector256Int64(Sse2.Shuffle(Sse2.LoadScalarVector128((ulong*)Unsafe.AsPointer(ref Unsafe.Add(ref o, 24))).AsInt32(), 0b00_01_10_11).AsUInt32()).AsInt32();
                var vprev2 = Avx2.ConvertToVector256Int64(Sse2.Shuffle(Sse2.LoadScalarVector128((ulong*)Unsafe.AsPointer(ref Unsafe.Add(ref o, 20))).AsInt32(), 0b00_01_10_11).AsUInt32()).AsInt32();
                var vprev3 = Avx2.ConvertToVector256Int64(Sse2.Shuffle(Sse2.LoadScalarVector128((ulong*)Unsafe.AsPointer(ref Unsafe.Add(ref o, 16))).AsInt32(), 0b00_01_10_11).AsUInt32()).AsInt32();
                var vprev4 = Avx2.ConvertToVector256Int64(Sse2.Shuffle(Sse2.LoadScalarVector128((ulong*)Unsafe.AsPointer(ref Unsafe.Add(ref o, 12))).AsInt32(), 0b00_01_10_11).AsUInt32()).AsInt32();
                var vprev5 = Avx2.ConvertToVector256Int64(Sse2.Shuffle(Sse2.LoadScalarVector128((ulong*)Unsafe.AsPointer(ref Unsafe.Add(ref o, 8))).AsInt32(), 0b00_01_10_11).AsUInt32()).AsInt32();
                var vprev6 = Avx2.ConvertToVector256Int64(Sse2.Shuffle(Sse2.LoadScalarVector128((ulong*)Unsafe.AsPointer(ref Unsafe.Add(ref o, 4))).AsInt32(), 0b00_01_10_11).AsUInt32()).AsInt32();
                var vprev7 = Avx2.ConvertToVector256Int64(Sse2.Shuffle(Sse2.LoadScalarVector128((ulong*)Unsafe.AsPointer(ref Unsafe.Add(ref o, 0))).AsInt32(), 0b00_01_10_11).AsUInt32()).AsInt32();
                for(nint i = 0; i < dataLength;)
                {
                    var res = Vector128.CreateScalar(Unsafe.Add(ref r, i));
                    sum256 = Avx2.Multiply(vcoeff0, vprev0);
                    sum256 = Avx2.Add(sum256, Avx2.Multiply(vcoeff1, vprev1));
                    sum256 = Avx2.Add(sum256, Avx2.Multiply(vcoeff2, vprev2));
                    sum256 = Avx2.Add(sum256, Avx2.Multiply(vcoeff3, vprev3));
                    sum256 = Avx2.Add(sum256, Avx2.Multiply(vcoeff4, vprev4));
                    sum256 = Avx2.Add(sum256, Avx2.Multiply(vcoeff5, vprev5));
                    sum256 = Avx2.Add(sum256, Avx2.Multiply(vcoeff6, vprev6));
                    sum256 = Avx2.Add(sum256, Avx2.Multiply(vcoeff7, vprev7));
                    sum = Sse2.Add(sum256.GetLower(), sum256.GetUpper());
                    sum = Sse2.Add(sum, Sse2.ShiftRightLogical128BitLane(sum, 8));
                    sum = Sse2.ShiftRightLogical(sum, vshift);
                    var yy = Sse2.Add(sum.AsInt32(), res);
#if NET5_0_OR_GREATER
                    Sse2.StoreScalar((int*)Unsafe.AsPointer(ref Unsafe.Add(ref d, i)), yy);
#else
                    Unsafe.Add(ref d, i) = yy.GetElement(0);
#endif
                    i++;
                    var yu = yy.ToVector256();
                    yu = Avx2.Permute4x64(yu.AsUInt64(), 0b00_00_00_00).AsInt32();
                    vprev7 = Avx2.Permute4x64(vprev7.AsInt64(), 0b10_01_00_11).AsInt32();
                    vprev7 = Avx2.Blend(vprev7, vprev6, 0b0000_0001);
                    vprev6 = Avx2.Permute4x64(vprev6.AsInt64(), 0b10_01_00_11).AsInt32();
                    vprev6 = Avx2.Blend(vprev6, vprev5, 0b0000_0001);
                    vprev5 = Avx2.Permute4x64(vprev5.AsInt64(), 0b10_01_00_11).AsInt32();
                    vprev5 = Avx2.Blend(vprev5, vprev4, 0b0000_0001);
                    vprev4 = Avx2.Permute4x64(vprev4.AsInt64(), 0b10_01_00_11).AsInt32();
                    vprev4 = Avx2.Blend(vprev4, vprev3, 0b0000_0001);
                    vprev3 = Avx2.Permute4x64(vprev3.AsInt64(), 0b10_01_00_11).AsInt32();
                    vprev3 = Avx2.Blend(vprev3, vprev2, 0b0000_0001);
                    vprev2 = Avx2.Permute4x64(vprev2.AsInt64(), 0b10_01_00_11).AsInt32();
                    vprev2 = Avx2.Blend(vprev2, vprev1, 0b0000_0001);
                    vprev1 = Avx2.Permute4x64(vprev1.AsInt64(), 0b10_01_00_11).AsInt32();
                    vprev1 = Avx2.Blend(vprev1, vprev0, 0b0000_0001);
                    vprev0 = Avx2.Permute4x64(vprev0.AsInt64(), 0b10_01_00_11).AsInt32();
                    vprev0 = Avx2.Blend(vprev0, yu, 0b0000_0001);
                }
            }
#endregion Order32
        }
    }
}
#endif