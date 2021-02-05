﻿using Shamisen.Extensions;
using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace Shamisen.Conversion.WaveToSampleConverters
{
    /// <summary>
    /// Converts 32-bit PCM to Sample.
    /// </summary>
    /// <seealso cref="Shamisen.Conversion.WaveToSampleConverters.WaveToSampleConverterBase" />
    public sealed class Pcm32ToSampleConverter : WaveToSampleConverterBase
    {
        private const float Divisor = 2147483648.0f;
        private const int ActualBytesPerSample = sizeof(int);
        private const int BufferMax = 1024;
        private int ActualBufferMax => BufferMax - (BufferMax % Source.Format.Channels);

        /// <summary>
        /// Gets the endianness of <see cref="WaveToSampleConverterBase.Source"/>.
        /// </summary>
        /// <value>
        /// The endianness of <see cref="WaveToSampleConverterBase.Source"/>.
        /// </value>
        public Endianness Endianness { get; }

        private bool IsEndiannessConversionRequired => Endianness != EndiannessExtensions.EnvironmentEndianness;

        /// <summary>
        /// Initializes a new instance of the <see cref="Pcm16ToSampleConverter"/> class.
        /// </summary>
        /// <param name="source">The source.</param>
        /// <param name="endianness">The endianness of <paramref name="source"/>.</param>
        public Pcm32ToSampleConverter(IReadableAudioSource<byte, IWaveFormat> source, Endianness endianness = Endianness.Little)
            : base(source, new SampleFormat(source.Format.SampleRate, source.Format.Channels)) => Endianness = endianness;

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
        public override ReadResult Read(Span<float> buffer)
        {
            Span<int> span = stackalloc int[buffer.Length > ActualBufferMax ? ActualBufferMax : buffer.Length];
            var cursor = buffer;
            while (cursor.Length > 0)
            {
                var reader = cursor.Length >= span.Length ? span : span.Slice(0, cursor.Length);
                var rr = Source.Read(MemoryMarshal.AsBytes(span));
                if (rr.HasNoData) return buffer.Length - cursor.Length;
                int u = rr.Length / ActualBytesPerSample;
                var wrote = reader.Slice(0, u);
                var dest = cursor.Slice(0, wrote.Length);
                if (wrote.Length != dest.Length)
                    new InvalidOperationException(
                        $"The {nameof(wrote)}'s length and {nameof(dest)}'s length are not equal! This is a bug!").Throw();
                if (IsEndiannessConversionRequired)
                {
                    for (int i = 0; i < wrote.Length && i < dest.Length; i++)
                    {
                        dest[i] = BinaryPrimitives.ReverseEndianness(wrote[i]) / Divisor;
                    }
                }
                else
                {
                    for (int i = 0; i < wrote.Length && i < dest.Length; i++)
                    {
                        dest[i] = wrote[i] / Divisor;
                    }
                }
                cursor = cursor.Slice(u);
                if (u != reader.Length) return buffer.Length - cursor.Length;  //The Source doesn't fill whole reader so return here.
            }
            return buffer.Length;
        }

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
            }
            disposedValue = true;
        }
    }
}
