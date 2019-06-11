﻿using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace MonoAudio.Conversion.SampleToWaveConverters
{
    /// <summary>
    /// Converts Sample to 32-bit IeeeFloat PCM.
    /// </summary>
    /// <seealso cref="SampleToWaveConverterBase" />
    public sealed class SampleToFloat32Converter : SampleToWaveConverterBase
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SampleToFloat32Converter"/> class.
        /// </summary>
        /// <param name="source">The source.</param>
        public SampleToFloat32Converter(IReadableAudioSource<float> source)
            : base(source, new WaveFormat(source.Format.SampleRate, 32, source.Format.Channels, AudioEncoding.IeeeFloat))
        {
        }

        /// <summary>
        /// Gets the bytes consumed per sample.
        /// </summary>
        /// <value>
        /// The bytes consumed per sample.
        /// </value>
        protected override int BytesPerSample => sizeof(float);

        /// <summary>
        /// Reads the audio to the specified buffer.
        /// </summary>
        /// <param name="buffer">The buffer.</param>
        /// <returns>
        /// The length of the data written.
        /// </returns>
        public override int Read(Span<byte> buffer)
        {
            return 4 * Source.Read(MemoryMarshal.Cast<byte, float>(buffer.Slice(0, buffer.Length - (buffer.Length % 4))));
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
