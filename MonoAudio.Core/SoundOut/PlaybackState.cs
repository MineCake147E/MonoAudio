﻿using System;
using System.Collections.Generic;
using System.Text;

namespace MonoAudio.SoundOut
{
    /// <summary>
    /// Represents a state of playback.
    /// </summary>
    public enum PlaybackState
    {
        /// <summary>
        /// The playback is stopped.
        /// </summary>
        Stopped,

        /// <summary>
        /// The playback is running.
        /// </summary>
        Playing,

        /// <summary>
        /// The playback is paused.
        /// </summary>
        Paused
    }
}
