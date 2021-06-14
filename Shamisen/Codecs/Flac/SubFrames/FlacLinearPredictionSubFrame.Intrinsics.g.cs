﻿#region License notice

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
using System.Text;
using System.Threading.Tasks;

namespace Shamisen.Codecs.Flac.SubFrames
{
    public sealed partial class FlacLinearPredictionSubFrame
    {
#region Order2
        [MethodImpl(OptimizationUtils.InlineAndOptimizeIfPossible)]
        internal static unsafe bool RestoreSignalOrder2Intrinsic(int shiftsNeeded, ReadOnlySpan<int> residual, ReadOnlySpan<int> coeffs, Span<int> output)
        {
#if NETCOREAPP3_1_OR_GREATER
            if (X86.RestoreSignalOrder2(shiftsNeeded, residual, coeffs, output)) return true;
#endif
            return false;
        }
        [MethodImpl(OptimizationUtils.InlineAndOptimizeIfPossible)]
        internal static unsafe bool RestoreSignalOrder2WideIntrinsic(int shiftsNeeded, ReadOnlySpan<int> residual, ReadOnlySpan<int> coeffs, Span<int> output)
        {
#if NETCOREAPP3_1_OR_GREATER
            if (X86.RestoreSignalOrder2Wide(shiftsNeeded, residual, coeffs, output)) return true;
#endif
            return false;
        }
#endregion Order2
#region Order3
        [MethodImpl(OptimizationUtils.InlineAndOptimizeIfPossible)]
        internal static unsafe bool RestoreSignalOrder3Intrinsic(int shiftsNeeded, ReadOnlySpan<int> residual, ReadOnlySpan<int> coeffs, Span<int> output)
        {
#if NETCOREAPP3_1_OR_GREATER
            if (X86.RestoreSignalOrder3(shiftsNeeded, residual, coeffs, output)) return true;
#endif
            return false;
        }
        [MethodImpl(OptimizationUtils.InlineAndOptimizeIfPossible)]
        internal static unsafe bool RestoreSignalOrder3WideIntrinsic(int shiftsNeeded, ReadOnlySpan<int> residual, ReadOnlySpan<int> coeffs, Span<int> output)
        {
#if NETCOREAPP3_1_OR_GREATER
            if (X86.RestoreSignalOrder3Wide(shiftsNeeded, residual, coeffs, output)) return true;
#endif
            return false;
        }
#endregion Order3
#region Order4
        [MethodImpl(OptimizationUtils.InlineAndOptimizeIfPossible)]
        internal static unsafe bool RestoreSignalOrder4Intrinsic(int shiftsNeeded, ReadOnlySpan<int> residual, ReadOnlySpan<int> coeffs, Span<int> output)
        {
#if NETCOREAPP3_1_OR_GREATER
            if (X86.RestoreSignalOrder4(shiftsNeeded, residual, coeffs, output)) return true;
#endif
            return false;
        }
        [MethodImpl(OptimizationUtils.InlineAndOptimizeIfPossible)]
        internal static unsafe bool RestoreSignalOrder4WideIntrinsic(int shiftsNeeded, ReadOnlySpan<int> residual, ReadOnlySpan<int> coeffs, Span<int> output)
        {
#if NETCOREAPP3_1_OR_GREATER
            if (X86.RestoreSignalOrder4Wide(shiftsNeeded, residual, coeffs, output)) return true;
#endif
            return false;
        }
#endregion Order4
#region Order5
        [MethodImpl(OptimizationUtils.InlineAndOptimizeIfPossible)]
        internal static unsafe bool RestoreSignalOrder5Intrinsic(int shiftsNeeded, ReadOnlySpan<int> residual, ReadOnlySpan<int> coeffs, Span<int> output)
        {
#if NETCOREAPP3_1_OR_GREATER
            if (X86.RestoreSignalOrder5(shiftsNeeded, residual, coeffs, output)) return true;
#endif
            return false;
        }
        [MethodImpl(OptimizationUtils.InlineAndOptimizeIfPossible)]
        internal static unsafe bool RestoreSignalOrder5WideIntrinsic(int shiftsNeeded, ReadOnlySpan<int> residual, ReadOnlySpan<int> coeffs, Span<int> output)
        {
#if NETCOREAPP3_1_OR_GREATER
            if (X86.RestoreSignalOrder5Wide(shiftsNeeded, residual, coeffs, output)) return true;
#endif
            return false;
        }
#endregion Order5
#region Order6
        [MethodImpl(OptimizationUtils.InlineAndOptimizeIfPossible)]
        internal static unsafe bool RestoreSignalOrder6Intrinsic(int shiftsNeeded, ReadOnlySpan<int> residual, ReadOnlySpan<int> coeffs, Span<int> output)
        {
#if NETCOREAPP3_1_OR_GREATER
            if (X86.RestoreSignalOrder6(shiftsNeeded, residual, coeffs, output)) return true;
#endif
            return false;
        }
        [MethodImpl(OptimizationUtils.InlineAndOptimizeIfPossible)]
        internal static unsafe bool RestoreSignalOrder6WideIntrinsic(int shiftsNeeded, ReadOnlySpan<int> residual, ReadOnlySpan<int> coeffs, Span<int> output)
        {
#if NETCOREAPP3_1_OR_GREATER
            if (X86.RestoreSignalOrder6Wide(shiftsNeeded, residual, coeffs, output)) return true;
#endif
            return false;
        }
#endregion Order6
#region Order7
        [MethodImpl(OptimizationUtils.InlineAndOptimizeIfPossible)]
        internal static unsafe bool RestoreSignalOrder7Intrinsic(int shiftsNeeded, ReadOnlySpan<int> residual, ReadOnlySpan<int> coeffs, Span<int> output)
        {
#if NETCOREAPP3_1_OR_GREATER
            if (X86.RestoreSignalOrder7(shiftsNeeded, residual, coeffs, output)) return true;
#endif
            return false;
        }
        [MethodImpl(OptimizationUtils.InlineAndOptimizeIfPossible)]
        internal static unsafe bool RestoreSignalOrder7WideIntrinsic(int shiftsNeeded, ReadOnlySpan<int> residual, ReadOnlySpan<int> coeffs, Span<int> output)
        {
#if NETCOREAPP3_1_OR_GREATER
            if (X86.RestoreSignalOrder7Wide(shiftsNeeded, residual, coeffs, output)) return true;
#endif
            return false;
        }
#endregion Order7
#region Order8
        [MethodImpl(OptimizationUtils.InlineAndOptimizeIfPossible)]
        internal static unsafe bool RestoreSignalOrder8Intrinsic(int shiftsNeeded, ReadOnlySpan<int> residual, ReadOnlySpan<int> coeffs, Span<int> output)
        {
#if NETCOREAPP3_1_OR_GREATER
            if (X86.RestoreSignalOrder8(shiftsNeeded, residual, coeffs, output)) return true;
#endif
            return false;
        }
        [MethodImpl(OptimizationUtils.InlineAndOptimizeIfPossible)]
        internal static unsafe bool RestoreSignalOrder8WideIntrinsic(int shiftsNeeded, ReadOnlySpan<int> residual, ReadOnlySpan<int> coeffs, Span<int> output)
        {
#if NETCOREAPP3_1_OR_GREATER
            if (X86.RestoreSignalOrder8Wide(shiftsNeeded, residual, coeffs, output)) return true;
#endif
            return false;
        }
#endregion Order8
#region Order9
        [MethodImpl(OptimizationUtils.InlineAndOptimizeIfPossible)]
        internal static unsafe bool RestoreSignalOrder9Intrinsic(int shiftsNeeded, ReadOnlySpan<int> residual, ReadOnlySpan<int> coeffs, Span<int> output)
        {
#if NETCOREAPP3_1_OR_GREATER
            if (X86.RestoreSignalOrder9(shiftsNeeded, residual, coeffs, output)) return true;
#endif
            return false;
        }
        [MethodImpl(OptimizationUtils.InlineAndOptimizeIfPossible)]
        internal static unsafe bool RestoreSignalOrder9WideIntrinsic(int shiftsNeeded, ReadOnlySpan<int> residual, ReadOnlySpan<int> coeffs, Span<int> output)
        {
#if NETCOREAPP3_1_OR_GREATER
            if (X86.RestoreSignalOrder9Wide(shiftsNeeded, residual, coeffs, output)) return true;
#endif
            return false;
        }
#endregion Order9
#region Order10
        [MethodImpl(OptimizationUtils.InlineAndOptimizeIfPossible)]
        internal static unsafe bool RestoreSignalOrder10Intrinsic(int shiftsNeeded, ReadOnlySpan<int> residual, ReadOnlySpan<int> coeffs, Span<int> output)
        {
#if NETCOREAPP3_1_OR_GREATER
            if (X86.RestoreSignalOrder10(shiftsNeeded, residual, coeffs, output)) return true;
#endif
            return false;
        }
        [MethodImpl(OptimizationUtils.InlineAndOptimizeIfPossible)]
        internal static unsafe bool RestoreSignalOrder10WideIntrinsic(int shiftsNeeded, ReadOnlySpan<int> residual, ReadOnlySpan<int> coeffs, Span<int> output)
        {
#if NETCOREAPP3_1_OR_GREATER
            if (X86.RestoreSignalOrder10Wide(shiftsNeeded, residual, coeffs, output)) return true;
#endif
            return false;
        }
#endregion Order10
#region Order11
        [MethodImpl(OptimizationUtils.InlineAndOptimizeIfPossible)]
        internal static unsafe bool RestoreSignalOrder11Intrinsic(int shiftsNeeded, ReadOnlySpan<int> residual, ReadOnlySpan<int> coeffs, Span<int> output)
        {
#if NETCOREAPP3_1_OR_GREATER
            if (X86.RestoreSignalOrder11(shiftsNeeded, residual, coeffs, output)) return true;
#endif
            return false;
        }
        [MethodImpl(OptimizationUtils.InlineAndOptimizeIfPossible)]
        internal static unsafe bool RestoreSignalOrder11WideIntrinsic(int shiftsNeeded, ReadOnlySpan<int> residual, ReadOnlySpan<int> coeffs, Span<int> output)
        {
#if NETCOREAPP3_1_OR_GREATER
            if (X86.RestoreSignalOrder11Wide(shiftsNeeded, residual, coeffs, output)) return true;
#endif
            return false;
        }
#endregion Order11
#region Order12
        [MethodImpl(OptimizationUtils.InlineAndOptimizeIfPossible)]
        internal static unsafe bool RestoreSignalOrder12Intrinsic(int shiftsNeeded, ReadOnlySpan<int> residual, ReadOnlySpan<int> coeffs, Span<int> output)
        {
#if NETCOREAPP3_1_OR_GREATER
            if (X86.RestoreSignalOrder12(shiftsNeeded, residual, coeffs, output)) return true;
#endif
            return false;
        }
        [MethodImpl(OptimizationUtils.InlineAndOptimizeIfPossible)]
        internal static unsafe bool RestoreSignalOrder12WideIntrinsic(int shiftsNeeded, ReadOnlySpan<int> residual, ReadOnlySpan<int> coeffs, Span<int> output)
        {
#if NETCOREAPP3_1_OR_GREATER
            if (X86.RestoreSignalOrder12Wide(shiftsNeeded, residual, coeffs, output)) return true;
#endif
            return false;
        }
#endregion Order12
#region Order13
        [MethodImpl(OptimizationUtils.InlineAndOptimizeIfPossible)]
        internal static unsafe bool RestoreSignalOrder13Intrinsic(int shiftsNeeded, ReadOnlySpan<int> residual, ReadOnlySpan<int> coeffs, Span<int> output)
        {
#if NETCOREAPP3_1_OR_GREATER
            if (X86.RestoreSignalOrder13(shiftsNeeded, residual, coeffs, output)) return true;
#endif
            return false;
        }
        [MethodImpl(OptimizationUtils.InlineAndOptimizeIfPossible)]
        internal static unsafe bool RestoreSignalOrder13WideIntrinsic(int shiftsNeeded, ReadOnlySpan<int> residual, ReadOnlySpan<int> coeffs, Span<int> output)
        {
#if NETCOREAPP3_1_OR_GREATER
            if (X86.RestoreSignalOrder13Wide(shiftsNeeded, residual, coeffs, output)) return true;
#endif
            return false;
        }
#endregion Order13
#region Order14
        [MethodImpl(OptimizationUtils.InlineAndOptimizeIfPossible)]
        internal static unsafe bool RestoreSignalOrder14Intrinsic(int shiftsNeeded, ReadOnlySpan<int> residual, ReadOnlySpan<int> coeffs, Span<int> output)
        {
#if NETCOREAPP3_1_OR_GREATER
            if (X86.RestoreSignalOrder14(shiftsNeeded, residual, coeffs, output)) return true;
#endif
            return false;
        }
        [MethodImpl(OptimizationUtils.InlineAndOptimizeIfPossible)]
        internal static unsafe bool RestoreSignalOrder14WideIntrinsic(int shiftsNeeded, ReadOnlySpan<int> residual, ReadOnlySpan<int> coeffs, Span<int> output)
        {
#if NETCOREAPP3_1_OR_GREATER
            if (X86.RestoreSignalOrder14Wide(shiftsNeeded, residual, coeffs, output)) return true;
#endif
            return false;
        }
#endregion Order14
#region Order15
        [MethodImpl(OptimizationUtils.InlineAndOptimizeIfPossible)]
        internal static unsafe bool RestoreSignalOrder15Intrinsic(int shiftsNeeded, ReadOnlySpan<int> residual, ReadOnlySpan<int> coeffs, Span<int> output)
        {
#if NETCOREAPP3_1_OR_GREATER
            if (X86.RestoreSignalOrder15(shiftsNeeded, residual, coeffs, output)) return true;
#endif
            return false;
        }
        [MethodImpl(OptimizationUtils.InlineAndOptimizeIfPossible)]
        internal static unsafe bool RestoreSignalOrder15WideIntrinsic(int shiftsNeeded, ReadOnlySpan<int> residual, ReadOnlySpan<int> coeffs, Span<int> output)
        {
#if NETCOREAPP3_1_OR_GREATER
            if (X86.RestoreSignalOrder15Wide(shiftsNeeded, residual, coeffs, output)) return true;
#endif
            return false;
        }
#endregion Order15
#region Order16
        [MethodImpl(OptimizationUtils.InlineAndOptimizeIfPossible)]
        internal static unsafe bool RestoreSignalOrder16Intrinsic(int shiftsNeeded, ReadOnlySpan<int> residual, ReadOnlySpan<int> coeffs, Span<int> output)
        {
#if NETCOREAPP3_1_OR_GREATER
            if (X86.RestoreSignalOrder16(shiftsNeeded, residual, coeffs, output)) return true;
#endif
            return false;
        }
        [MethodImpl(OptimizationUtils.InlineAndOptimizeIfPossible)]
        internal static unsafe bool RestoreSignalOrder16WideIntrinsic(int shiftsNeeded, ReadOnlySpan<int> residual, ReadOnlySpan<int> coeffs, Span<int> output)
        {
#if NETCOREAPP3_1_OR_GREATER
            if (X86.RestoreSignalOrder16Wide(shiftsNeeded, residual, coeffs, output)) return true;
#endif
            return false;
        }
#endregion Order16
#region Order17
        [MethodImpl(OptimizationUtils.InlineAndOptimizeIfPossible)]
        internal static unsafe bool RestoreSignalOrder17Intrinsic(int shiftsNeeded, ReadOnlySpan<int> residual, ReadOnlySpan<int> coeffs, Span<int> output)
        {
#if NETCOREAPP3_1_OR_GREATER
            if (X86.RestoreSignalOrder17(shiftsNeeded, residual, coeffs, output)) return true;
#endif
            return false;
        }
        [MethodImpl(OptimizationUtils.InlineAndOptimizeIfPossible)]
        internal static unsafe bool RestoreSignalOrder17WideIntrinsic(int shiftsNeeded, ReadOnlySpan<int> residual, ReadOnlySpan<int> coeffs, Span<int> output)
        {
#if NETCOREAPP3_1_OR_GREATER
            if (X86.RestoreSignalOrder17Wide(shiftsNeeded, residual, coeffs, output)) return true;
#endif
            return false;
        }
#endregion Order17
#region Order18
        [MethodImpl(OptimizationUtils.InlineAndOptimizeIfPossible)]
        internal static unsafe bool RestoreSignalOrder18Intrinsic(int shiftsNeeded, ReadOnlySpan<int> residual, ReadOnlySpan<int> coeffs, Span<int> output)
        {
#if NETCOREAPP3_1_OR_GREATER
            if (X86.RestoreSignalOrder18(shiftsNeeded, residual, coeffs, output)) return true;
#endif
            return false;
        }
        [MethodImpl(OptimizationUtils.InlineAndOptimizeIfPossible)]
        internal static unsafe bool RestoreSignalOrder18WideIntrinsic(int shiftsNeeded, ReadOnlySpan<int> residual, ReadOnlySpan<int> coeffs, Span<int> output)
        {
#if NETCOREAPP3_1_OR_GREATER
            if (X86.RestoreSignalOrder18Wide(shiftsNeeded, residual, coeffs, output)) return true;
#endif
            return false;
        }
#endregion Order18
#region Order19
        [MethodImpl(OptimizationUtils.InlineAndOptimizeIfPossible)]
        internal static unsafe bool RestoreSignalOrder19Intrinsic(int shiftsNeeded, ReadOnlySpan<int> residual, ReadOnlySpan<int> coeffs, Span<int> output)
        {
#if NETCOREAPP3_1_OR_GREATER
            if (X86.RestoreSignalOrder19(shiftsNeeded, residual, coeffs, output)) return true;
#endif
            return false;
        }
        [MethodImpl(OptimizationUtils.InlineAndOptimizeIfPossible)]
        internal static unsafe bool RestoreSignalOrder19WideIntrinsic(int shiftsNeeded, ReadOnlySpan<int> residual, ReadOnlySpan<int> coeffs, Span<int> output)
        {
#if NETCOREAPP3_1_OR_GREATER
            if (X86.RestoreSignalOrder19Wide(shiftsNeeded, residual, coeffs, output)) return true;
#endif
            return false;
        }
#endregion Order19
#region Order20
        [MethodImpl(OptimizationUtils.InlineAndOptimizeIfPossible)]
        internal static unsafe bool RestoreSignalOrder20Intrinsic(int shiftsNeeded, ReadOnlySpan<int> residual, ReadOnlySpan<int> coeffs, Span<int> output)
        {
#if NETCOREAPP3_1_OR_GREATER
            if (X86.RestoreSignalOrder20(shiftsNeeded, residual, coeffs, output)) return true;
#endif
            return false;
        }
        [MethodImpl(OptimizationUtils.InlineAndOptimizeIfPossible)]
        internal static unsafe bool RestoreSignalOrder20WideIntrinsic(int shiftsNeeded, ReadOnlySpan<int> residual, ReadOnlySpan<int> coeffs, Span<int> output)
        {
#if NETCOREAPP3_1_OR_GREATER
            if (X86.RestoreSignalOrder20Wide(shiftsNeeded, residual, coeffs, output)) return true;
#endif
            return false;
        }
#endregion Order20
#region Order21
        [MethodImpl(OptimizationUtils.InlineAndOptimizeIfPossible)]
        internal static unsafe bool RestoreSignalOrder21Intrinsic(int shiftsNeeded, ReadOnlySpan<int> residual, ReadOnlySpan<int> coeffs, Span<int> output)
        {
#if NETCOREAPP3_1_OR_GREATER
            if (X86.RestoreSignalOrder21(shiftsNeeded, residual, coeffs, output)) return true;
#endif
            return false;
        }
        [MethodImpl(OptimizationUtils.InlineAndOptimizeIfPossible)]
        internal static unsafe bool RestoreSignalOrder21WideIntrinsic(int shiftsNeeded, ReadOnlySpan<int> residual, ReadOnlySpan<int> coeffs, Span<int> output)
        {
#if NETCOREAPP3_1_OR_GREATER
            if (X86.RestoreSignalOrder21Wide(shiftsNeeded, residual, coeffs, output)) return true;
#endif
            return false;
        }
#endregion Order21
#region Order22
        [MethodImpl(OptimizationUtils.InlineAndOptimizeIfPossible)]
        internal static unsafe bool RestoreSignalOrder22Intrinsic(int shiftsNeeded, ReadOnlySpan<int> residual, ReadOnlySpan<int> coeffs, Span<int> output)
        {
#if NETCOREAPP3_1_OR_GREATER
            if (X86.RestoreSignalOrder22(shiftsNeeded, residual, coeffs, output)) return true;
#endif
            return false;
        }
        [MethodImpl(OptimizationUtils.InlineAndOptimizeIfPossible)]
        internal static unsafe bool RestoreSignalOrder22WideIntrinsic(int shiftsNeeded, ReadOnlySpan<int> residual, ReadOnlySpan<int> coeffs, Span<int> output)
        {
#if NETCOREAPP3_1_OR_GREATER
            if (X86.RestoreSignalOrder22Wide(shiftsNeeded, residual, coeffs, output)) return true;
#endif
            return false;
        }
#endregion Order22
#region Order23
        [MethodImpl(OptimizationUtils.InlineAndOptimizeIfPossible)]
        internal static unsafe bool RestoreSignalOrder23Intrinsic(int shiftsNeeded, ReadOnlySpan<int> residual, ReadOnlySpan<int> coeffs, Span<int> output)
        {
#if NETCOREAPP3_1_OR_GREATER
            if (X86.RestoreSignalOrder23(shiftsNeeded, residual, coeffs, output)) return true;
#endif
            return false;
        }
        [MethodImpl(OptimizationUtils.InlineAndOptimizeIfPossible)]
        internal static unsafe bool RestoreSignalOrder23WideIntrinsic(int shiftsNeeded, ReadOnlySpan<int> residual, ReadOnlySpan<int> coeffs, Span<int> output)
        {
#if NETCOREAPP3_1_OR_GREATER
            if (X86.RestoreSignalOrder23Wide(shiftsNeeded, residual, coeffs, output)) return true;
#endif
            return false;
        }
#endregion Order23
#region Order24
        [MethodImpl(OptimizationUtils.InlineAndOptimizeIfPossible)]
        internal static unsafe bool RestoreSignalOrder24Intrinsic(int shiftsNeeded, ReadOnlySpan<int> residual, ReadOnlySpan<int> coeffs, Span<int> output)
        {
#if NETCOREAPP3_1_OR_GREATER
            if (X86.RestoreSignalOrder24(shiftsNeeded, residual, coeffs, output)) return true;
#endif
            return false;
        }
        [MethodImpl(OptimizationUtils.InlineAndOptimizeIfPossible)]
        internal static unsafe bool RestoreSignalOrder24WideIntrinsic(int shiftsNeeded, ReadOnlySpan<int> residual, ReadOnlySpan<int> coeffs, Span<int> output)
        {
#if NETCOREAPP3_1_OR_GREATER
            if (X86.RestoreSignalOrder24Wide(shiftsNeeded, residual, coeffs, output)) return true;
#endif
            return false;
        }
#endregion Order24
#region Order25
        [MethodImpl(OptimizationUtils.InlineAndOptimizeIfPossible)]
        internal static unsafe bool RestoreSignalOrder25Intrinsic(int shiftsNeeded, ReadOnlySpan<int> residual, ReadOnlySpan<int> coeffs, Span<int> output)
        {
#if NETCOREAPP3_1_OR_GREATER
            if (X86.RestoreSignalOrder25(shiftsNeeded, residual, coeffs, output)) return true;
#endif
            return false;
        }
        [MethodImpl(OptimizationUtils.InlineAndOptimizeIfPossible)]
        internal static unsafe bool RestoreSignalOrder25WideIntrinsic(int shiftsNeeded, ReadOnlySpan<int> residual, ReadOnlySpan<int> coeffs, Span<int> output)
        {
#if NETCOREAPP3_1_OR_GREATER
            if (X86.RestoreSignalOrder25Wide(shiftsNeeded, residual, coeffs, output)) return true;
#endif
            return false;
        }
#endregion Order25
#region Order26
        [MethodImpl(OptimizationUtils.InlineAndOptimizeIfPossible)]
        internal static unsafe bool RestoreSignalOrder26Intrinsic(int shiftsNeeded, ReadOnlySpan<int> residual, ReadOnlySpan<int> coeffs, Span<int> output)
        {
#if NETCOREAPP3_1_OR_GREATER
            if (X86.RestoreSignalOrder26(shiftsNeeded, residual, coeffs, output)) return true;
#endif
            return false;
        }
        [MethodImpl(OptimizationUtils.InlineAndOptimizeIfPossible)]
        internal static unsafe bool RestoreSignalOrder26WideIntrinsic(int shiftsNeeded, ReadOnlySpan<int> residual, ReadOnlySpan<int> coeffs, Span<int> output)
        {
#if NETCOREAPP3_1_OR_GREATER
            if (X86.RestoreSignalOrder26Wide(shiftsNeeded, residual, coeffs, output)) return true;
#endif
            return false;
        }
#endregion Order26
#region Order27
        [MethodImpl(OptimizationUtils.InlineAndOptimizeIfPossible)]
        internal static unsafe bool RestoreSignalOrder27Intrinsic(int shiftsNeeded, ReadOnlySpan<int> residual, ReadOnlySpan<int> coeffs, Span<int> output)
        {
#if NETCOREAPP3_1_OR_GREATER
            if (X86.RestoreSignalOrder27(shiftsNeeded, residual, coeffs, output)) return true;
#endif
            return false;
        }
        [MethodImpl(OptimizationUtils.InlineAndOptimizeIfPossible)]
        internal static unsafe bool RestoreSignalOrder27WideIntrinsic(int shiftsNeeded, ReadOnlySpan<int> residual, ReadOnlySpan<int> coeffs, Span<int> output)
        {
#if NETCOREAPP3_1_OR_GREATER
            if (X86.RestoreSignalOrder27Wide(shiftsNeeded, residual, coeffs, output)) return true;
#endif
            return false;
        }
#endregion Order27
#region Order28
        [MethodImpl(OptimizationUtils.InlineAndOptimizeIfPossible)]
        internal static unsafe bool RestoreSignalOrder28Intrinsic(int shiftsNeeded, ReadOnlySpan<int> residual, ReadOnlySpan<int> coeffs, Span<int> output)
        {
#if NETCOREAPP3_1_OR_GREATER
            if (X86.RestoreSignalOrder28(shiftsNeeded, residual, coeffs, output)) return true;
#endif
            return false;
        }
        [MethodImpl(OptimizationUtils.InlineAndOptimizeIfPossible)]
        internal static unsafe bool RestoreSignalOrder28WideIntrinsic(int shiftsNeeded, ReadOnlySpan<int> residual, ReadOnlySpan<int> coeffs, Span<int> output)
        {
#if NETCOREAPP3_1_OR_GREATER
            if (X86.RestoreSignalOrder28Wide(shiftsNeeded, residual, coeffs, output)) return true;
#endif
            return false;
        }
#endregion Order28
#region Order29
        [MethodImpl(OptimizationUtils.InlineAndOptimizeIfPossible)]
        internal static unsafe bool RestoreSignalOrder29Intrinsic(int shiftsNeeded, ReadOnlySpan<int> residual, ReadOnlySpan<int> coeffs, Span<int> output)
        {
#if NETCOREAPP3_1_OR_GREATER
            if (X86.RestoreSignalOrder29(shiftsNeeded, residual, coeffs, output)) return true;
#endif
            return false;
        }
        [MethodImpl(OptimizationUtils.InlineAndOptimizeIfPossible)]
        internal static unsafe bool RestoreSignalOrder29WideIntrinsic(int shiftsNeeded, ReadOnlySpan<int> residual, ReadOnlySpan<int> coeffs, Span<int> output)
        {
#if NETCOREAPP3_1_OR_GREATER
            if (X86.RestoreSignalOrder29Wide(shiftsNeeded, residual, coeffs, output)) return true;
#endif
            return false;
        }
#endregion Order29
#region Order30
        [MethodImpl(OptimizationUtils.InlineAndOptimizeIfPossible)]
        internal static unsafe bool RestoreSignalOrder30Intrinsic(int shiftsNeeded, ReadOnlySpan<int> residual, ReadOnlySpan<int> coeffs, Span<int> output)
        {
#if NETCOREAPP3_1_OR_GREATER
            if (X86.RestoreSignalOrder30(shiftsNeeded, residual, coeffs, output)) return true;
#endif
            return false;
        }
        [MethodImpl(OptimizationUtils.InlineAndOptimizeIfPossible)]
        internal static unsafe bool RestoreSignalOrder30WideIntrinsic(int shiftsNeeded, ReadOnlySpan<int> residual, ReadOnlySpan<int> coeffs, Span<int> output)
        {
#if NETCOREAPP3_1_OR_GREATER
            if (X86.RestoreSignalOrder30Wide(shiftsNeeded, residual, coeffs, output)) return true;
#endif
            return false;
        }
#endregion Order30
#region Order31
        [MethodImpl(OptimizationUtils.InlineAndOptimizeIfPossible)]
        internal static unsafe bool RestoreSignalOrder31Intrinsic(int shiftsNeeded, ReadOnlySpan<int> residual, ReadOnlySpan<int> coeffs, Span<int> output)
        {
#if NETCOREAPP3_1_OR_GREATER
            if (X86.RestoreSignalOrder31(shiftsNeeded, residual, coeffs, output)) return true;
#endif
            return false;
        }
        [MethodImpl(OptimizationUtils.InlineAndOptimizeIfPossible)]
        internal static unsafe bool RestoreSignalOrder31WideIntrinsic(int shiftsNeeded, ReadOnlySpan<int> residual, ReadOnlySpan<int> coeffs, Span<int> output)
        {
#if NETCOREAPP3_1_OR_GREATER
            if (X86.RestoreSignalOrder31Wide(shiftsNeeded, residual, coeffs, output)) return true;
#endif
            return false;
        }
#endregion Order31
#region Order32
        [MethodImpl(OptimizationUtils.InlineAndOptimizeIfPossible)]
        internal static unsafe bool RestoreSignalOrder32Intrinsic(int shiftsNeeded, ReadOnlySpan<int> residual, ReadOnlySpan<int> coeffs, Span<int> output)
        {
#if NETCOREAPP3_1_OR_GREATER
            if (X86.RestoreSignalOrder32(shiftsNeeded, residual, coeffs, output)) return true;
#endif
            return false;
        }
        [MethodImpl(OptimizationUtils.InlineAndOptimizeIfPossible)]
        internal static unsafe bool RestoreSignalOrder32WideIntrinsic(int shiftsNeeded, ReadOnlySpan<int> residual, ReadOnlySpan<int> coeffs, Span<int> output)
        {
#if NETCOREAPP3_1_OR_GREATER
            if (X86.RestoreSignalOrder32Wide(shiftsNeeded, residual, coeffs, output)) return true;
#endif
            return false;
        }
#endregion Order32
    }
}