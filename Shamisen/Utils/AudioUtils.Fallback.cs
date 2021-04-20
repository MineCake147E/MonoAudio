﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

using Shamisen.Utils.Tuples;

namespace Shamisen.Utils
{
    public static partial class AudioUtils
    {
        internal static class Fallback
        {
            [MethodImpl(OptimizationUtils.InlineAndOptimizeIfPossible)]
            internal static void InterleaveStereoInt32(Span<int> buffer, ReadOnlySpan<int> left, ReadOnlySpan<int> right)
            {
                unsafe
                {
                    if (left.Length > right.Length) throw new ArgumentException("right must be as long as left!", nameof(right));
                    if (buffer.Length < left.Length * 2) throw new ArgumentException("buffer must be twice as long as left!");
                    right = right.SliceWhile(left.Length);
                    buffer = buffer.SliceWhile(left.Length * 2);
                    //These pre-touches may avoid some range checks
                    _ = right[left.Length - 1];
                    _ = buffer[left.Length * 2 - 1];
                    ref var rL = ref MemoryMarshal.GetReference(left);
                    ref var rR = ref MemoryMarshal.GetReference(right);
                    ref var rB = ref Unsafe.As<int, UnmanagedTupleX2<int>>(ref MemoryMarshal.GetReference(buffer));
                    var length = ((IntPtr)(left.Length * sizeof(int))).ToPointer();
                    var u8Length = ((IntPtr)((left.Length - 7) * sizeof(int))).ToPointer();
                    var j = IntPtr.Zero;
                    var i = IntPtr.Zero;
                    for (; i.ToPointer() < u8Length; i += sizeof(int) * 8)
                    {
                        ref var va = ref Unsafe.As<int, UnmanagedTupleX4<int>>(ref Unsafe.AddByteOffset(ref rL, i));
                        ref var vb = ref Unsafe.As<int, UnmanagedTupleX4<int>>(ref Unsafe.AddByteOffset(ref rR, i));
                        Unsafe.As<UnmanagedTupleX2<int>, UnmanagedTupleX4<int>>(ref Unsafe.AddByteOffset(ref rB, j)) = new(va.Item0, vb.Item0, va.Item1, vb.Item1);
                        Unsafe.As<UnmanagedTupleX2<int>, UnmanagedTupleX4<int>>(ref Unsafe.AddByteOffset(ref rB, j + 4 * sizeof(int))) = new(va.Item2, vb.Item2, va.Item3, vb.Item3);
                        j += sizeof(int) * 2 * 8;
                    }
                    for (; i.ToPointer() < length; i += sizeof(int))
                    {
                        Unsafe.AddByteOffset(ref rB, j) = (Unsafe.AddByteOffset(ref rL, i), Unsafe.AddByteOffset(ref rR, i));
                        j += sizeof(int) * 2;
                    }
                }
            }

            [MethodImpl(OptimizationUtils.InlineAndOptimizeIfPossible)]
            internal static void InterleaveStereoSingle(Span<float> buffer, ReadOnlySpan<float> left, ReadOnlySpan<float> right)
            {
                unsafe
                {
                    if (left.Length > right.Length) throw new ArgumentException("right must be as long as left!", nameof(right));
                    if (buffer.Length < left.Length * 2) throw new ArgumentException("buffer must be twice as long as left!");
                    right = right.SliceWhile(left.Length);
                    buffer = buffer.SliceWhile(left.Length * 2);
                    //These pre-touches may avoid some range checks
                    _ = right[left.Length - 1];
                    _ = buffer[left.Length * 2 - 1];
                    ref var rL = ref Unsafe.As<float, int>(ref MemoryMarshal.GetReference(left));
                    ref var rR = ref Unsafe.As<float, int>(ref MemoryMarshal.GetReference(right));
                    ref var rB = ref Unsafe.As<float, UnmanagedTupleX2<int>>(ref MemoryMarshal.GetReference(buffer));
                    var length = ((IntPtr)(left.Length * sizeof(int))).ToPointer();
                    var u8Length = ((IntPtr)((left.Length - 7) * sizeof(int))).ToPointer();
                    var j = IntPtr.Zero;
                    var i = IntPtr.Zero;
                    for (; i.ToPointer() < u8Length; i += sizeof(int) * 8)
                    {
                        ref var va = ref Unsafe.As<int, UnmanagedTupleX4<int>>(ref Unsafe.AddByteOffset(ref rL, i));
                        ref var vb = ref Unsafe.As<int, UnmanagedTupleX4<int>>(ref Unsafe.AddByteOffset(ref rR, i));
                        Unsafe.As<UnmanagedTupleX2<int>, UnmanagedTupleX4<int>>(ref Unsafe.AddByteOffset(ref rB, j)) = new(va.Item0, vb.Item0, va.Item1, vb.Item1);
                        Unsafe.As<UnmanagedTupleX2<int>, UnmanagedTupleX4<int>>(ref Unsafe.AddByteOffset(ref rB, j + 4 * sizeof(int))) = new(va.Item2, vb.Item2, va.Item3, vb.Item3);
                        j += sizeof(int) * 2 * 8;
                    }
                    for (; i.ToPointer() < length; i += sizeof(int))
                    {
                        Unsafe.AddByteOffset(ref rB, j) = (Unsafe.AddByteOffset(ref rL, i), Unsafe.AddByteOffset(ref rR, i));
                        j += sizeof(int) * 2;
                    }
                }
            }

            [MethodImpl(OptimizationUtils.InlineAndOptimizeIfPossible)]
            internal static void InterleaveThreeInt32(Span<int> buffer, ReadOnlySpan<int> left, ReadOnlySpan<int> right, ReadOnlySpan<int> center)
            {
                unsafe
                {
                    if (left.Length > right.Length) throw new ArgumentException("right must be as long as left!", nameof(right));
                    if (left.Length > center.Length) throw new ArgumentException("center must be as long as left!", nameof(center));
                    if (buffer.Length < left.Length * 2) throw new ArgumentException("buffer must be twice as long as left!");
                    right = right.SliceWhile(left.Length);
                    buffer = buffer.SliceWhile(left.Length * 2);
                    //These pre-touches may avoid some range checks
                    _ = right[left.Length - 1];
                    _ = buffer[left.Length * 2 - 1];
                    ref var rL = ref MemoryMarshal.GetReference(left);
                    ref var rR = ref MemoryMarshal.GetReference(right);
                    ref var rC = ref MemoryMarshal.GetReference(center);
                    ref var rB = ref MemoryMarshal.GetReference(buffer);
                    var length = ((IntPtr)(left.Length * sizeof(int))).ToPointer();
                    var u8Length = ((IntPtr)((left.Length - 1) * sizeof(int))).ToPointer();
                    var j = IntPtr.Zero;
                    var i = IntPtr.Zero;
                    for (; i.ToPointer() < u8Length; i += 2)
                    {
                        //The Unsafe.Add(ref Unsafe.Add(ref T, IntPtr), int) pattern avoids extra lea instructions.
                        var a = Unsafe.Add(ref rL, i);
                        Unsafe.Add(ref rB, j) = a;
                        a = Unsafe.Add(ref rR, i);
                        Unsafe.Add(ref Unsafe.Add(ref rB, j), 1) = a;
                        a = Unsafe.Add(ref rC, i);
                        Unsafe.Add(ref Unsafe.Add(ref rB, j), 2) = a;
                        a = Unsafe.Add(ref Unsafe.Add(ref rL, i), 1);
                        Unsafe.Add(ref Unsafe.Add(ref rB, j), 3) = a;
                        a = Unsafe.Add(ref Unsafe.Add(ref rR, i), 1);
                        Unsafe.Add(ref Unsafe.Add(ref rB, j), 4) = a;
                        a = Unsafe.Add(ref Unsafe.Add(ref rC, i), 1);
                        Unsafe.Add(ref Unsafe.Add(ref rB, j), 5) = a;
                        j += 3 * 2;
                    }
                    for (; i.ToPointer() < length; i += 1)
                    {
                        var a = Unsafe.Add(ref rL, i);
                        Unsafe.Add(ref rB, j) = a;
                        a = Unsafe.Add(ref rR, i);
                        Unsafe.Add(ref Unsafe.Add(ref rB, j), 1) = a;
                        a = Unsafe.Add(ref rC, i);
                        Unsafe.Add(ref Unsafe.Add(ref rB, j), 2) = a;
                        j += 3;
                    }
                }
            }
        }
    }
}
