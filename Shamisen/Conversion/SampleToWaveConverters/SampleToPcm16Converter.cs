﻿using Shamisen.Extensions;

using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Runtime.InteropServices;

using System.Text;
using Shamisen.Optimization;
using System.Runtime.CompilerServices;

#if NET5_0 || NETCOREAPP3_1

using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;

#endif

namespace Shamisen.Conversion.SampleToWaveConverters
{
    /// <summary>
    /// Converts samples to 16-bit PCM.
    /// </summary>
    /// <seealso cref="SampleToWaveConverterBase" />
    public sealed partial class SampleToPcm16Converter : SampleToWaveConverterBase
    {
        private const float Multiplier = 32768.0f;
        private const int ActualBytesPerSample = sizeof(short);
        private const int BufferMax = 1024;
        private int ActualBufferMax => BufferMax * Source.Format.Channels;

        private Memory<short> dsmLastOutput;
        private Memory<float> dsmAccumulator;
        private int dsmChannelPointer = 0;
        private Memory<float> readBuffer;

        private readonly bool enableIntrinsics;
        private readonly X86Intrinsics enabledX86Intrinsics;
        private readonly ArmIntrinsics enabledArmIntrinsics;

        /// <summary>
        /// Initializes a new instance of the <see cref="SampleToPcm16Converter"/> class.
        /// </summary>
        /// <param name="source">The source.</param>
        /// <param name="accuracyNeeded">Turns on <see cref="AccuracyMode"/> when <c>true</c>.</param>
        /// <param name="endianness">The destination endianness.</param>
        public SampleToPcm16Converter(IReadableAudioSource<float, SampleFormat> source, bool accuracyNeeded = true, Endianness endianness = Endianness.Little)
            : this(source, true, IntrinsicsUtils.X86Intrinsics, IntrinsicsUtils.ArmIntrinsics, accuracyNeeded, endianness)
        {
            if (accuracyNeeded)
            {
                dsmAccumulator = new float[source.Format.Channels];
                dsmLastOutput = new short[source.Format.Channels];
            }
            AccuracyMode = accuracyNeeded;
            Endianness = endianness;
            readBuffer = new float[ActualBufferMax];
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SampleToPcm16Converter"/> class.
        /// </summary>
        /// <param name="source">The source.</param>
        /// <param name="enableIntrinsics"></param>
        /// <param name="enabledX86Intrinsics"></param>
        /// <param name="enabledArmIntrinsics"></param>
        /// <param name="accuracyNeeded">Turns on <see cref="AccuracyMode"/> when <c>true</c>.</param>
        /// <param name="endianness">The destination endianness.</param>
        internal SampleToPcm16Converter(IReadableAudioSource<float, SampleFormat> source, bool enableIntrinsics, X86Intrinsics enabledX86Intrinsics, ArmIntrinsics enabledArmIntrinsics, bool accuracyNeeded = true, Endianness endianness = Endianness.Little)
             : base(source, new WaveFormat(source.Format.SampleRate, 16, source.Format.Channels, AudioEncoding.LinearPcm))
        {
            this.enableIntrinsics = enableIntrinsics;
            this.enabledX86Intrinsics = enabledX86Intrinsics;
            this.enabledArmIntrinsics = enabledArmIntrinsics;
        }

        /// <summary>
        /// Gets a value indicating whether the <see cref="SampleToPcm16Converter"/> does the 16-bit Delta-Sigma modulation.
        /// </summary>
        /// <value>
        ///   <c>true</c> if the accuracy mode is turned on; otherwise, <c>false</c>.
        /// </value>
        public bool AccuracyMode { get; }

        /// <summary>
        /// Gets the endianness.
        /// </summary>
        /// <value>
        /// The endianness.
        /// </value>
        public Endianness Endianness { get; }

        private bool IsEndiannessConversionRequired => Endianness != EndiannessExtensions.EnvironmentEndianness;

        /// <summary>
        /// Gets the bytes consumed per sample.
        /// </summary>
        /// <value>
        /// The bytes consumed per sample.
        /// </value>
        protected override int BytesPerSample => ActualBytesPerSample;

        /// <summary>
        /// Reads the audio to the specified buffer.
        /// </summary>
        /// <param name="buffer">The buffer.</param>
        /// <returns>
        /// The length of the data written.
        /// </returns>
        public override ReadResult Read(Span<byte> buffer)
        {
            int channels = Format.Channels;
            Span<short> outBuffer = MemoryMarshal.Cast<byte, short>(buffer).SliceAlign(channels);
            var cursor = outBuffer;
            while (cursor.Length > 0)
            {
                var reader = cursor.Length >= readBuffer.Length ? readBuffer : readBuffer.Slice(0, cursor.Length);
                var rr = Source.Read(reader.Span);
                if (rr.HasNoData) return buffer.Length - cursor.Length;
                int u = rr.Length;
                var wrote = reader.Span.Slice(0, u).SliceAlign(channels);
                var dest = cursor.Slice(0, wrote.Length);
                if (wrote.Length != dest.Length)
                {
                    new InvalidOperationException(
                        $"The {nameof(wrote)}'s length and {nameof(dest)}'s length are not equal! This is a bug!").Throw();
                }

                if (AccuracyMode)
                {
                    var dsmAcc = dsmAccumulator.Span;
                    var dsmLastOut = dsmLastOutput.Span;
                    dsmChannelPointer %= dsmAcc.Length;
                    for (int i = 0; i < dest.Length; i++)
                    {
                        var diff = wrote[i] - (dsmLastOut[dsmChannelPointer] / Multiplier);
                        dsmAcc[dsmChannelPointer] += diff;
                        var v = dsmLastOut[dsmChannelPointer] = Convert(dsmAcc[dsmChannelPointer]);
                        dest[i] = IsEndiannessConversionRequired ? BinaryPrimitives.ReverseEndianness(v) : v;
                        dsmChannelPointer = ++dsmChannelPointer % dsmAcc.Length;
                    }
                }
                else
                {
                    ProcessNormal(wrote, dest);
                }
                cursor = cursor.Slice(dest.Length);
                if (u != reader.Length) return buffer.Length - cursor.Length;  //The Source doesn't fill whole reader so return here.
            }
            return outBuffer.Length * sizeof(ushort);
        }

        private void ProcessNormal(Span<float> wrote, Span<short> dest)
        {
            if (IsEndiannessConversionRequired)
            {
#if NET5_0 || NETCOREAPP3_1
                if (enableIntrinsics)
                {
                    if (Avx2.IsSupported)
                    {
                        //
                    }
                    if (Ssse3.IsSupported && enabledX86Intrinsics.HasAllFeatures(X86IntrinsicsMask.Ssse3))
                    {
                        ProcessReversedSsse3(wrote, dest);
                        return;
                    }
                }
#endif
                ProcessReversedOrdinal(wrote, dest);
            }
            else
            {
#if NET5_0 || NETCOREAPP3_1
                if (enableIntrinsics)
                {
                    if (Avx2.IsSupported)
                    {
                        //
                    }
                    if (Sse2.IsSupported && enabledX86Intrinsics.HasAllFeatures(X86IntrinsicsMask.Sse2))
                    {
                        ProcessNormalSse2(wrote, dest);
                        return;
                    }
                }
#endif
                ProcessNormalOrdinal(wrote, dest);
            }
        }

        private static void ProcessReversedOrdinal(Span<float> wrote, Span<short> dest)
        {
            for (int i = 0; i < dest.Length; i++)
            {
                var v = Convert(wrote[i]);
                dest[i] = BinaryPrimitives.ReverseEndianness(v);
            }
        }

        private static void ProcessNormalOrdinal(Span<float> wrote, Span<short> dest)
        {
            for (int i = 0; i < dest.Length; i++)
            {
                var v = (short)Math.Min(short.MaxValue, Math.Max(wrote[i] * Multiplier, short.MinValue));
                dest[i] = v;
            }
        }

        //[MethodImpl(OptimizationUtils.InlineAndOptimizeIfPossible)]
        private static short Convert(float srcval) => (short)Math.Min(short.MaxValue, Math.Max(srcval * Multiplier, short.MinValue));

        /// <summary>
        /// Releases unmanaged and - optionally - managed resources.
        /// </summary>
        /// <param name="disposing"><c>true</c> to release both managed and unmanaged resources; <c>false</c> to release only unmanaged resources.</param>
        protected override void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    Source.Dispose();
                }
                dsmLastOutput = default;
                dsmAccumulator = default;
                readBuffer = default;
            }
            disposedValue = true;
        }
    }
}
