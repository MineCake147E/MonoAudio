﻿using System;
using System.Collections.Generic;
using System.Text;

namespace MonoAudio.IO
{
    /// <summary>
    /// Defines a base infrastructure of an audio device enumerator.
    /// </summary>
    public interface IAudioDeviceEnumerator
    {
        /// <summary>
        /// Enumerates devices of specified <paramref name="dataFlow"/>.
        /// </summary>
        /// <param name="dataFlow">The <see cref="DataFlow"/> kind to enumerate devices of.</param>
        /// <returns>The <see cref="IEnumerable{T}"/> of audio devices.</returns>
        IEnumerable<IAudioDevice> EnumerateDevices(DataFlow dataFlow);
    }
}
