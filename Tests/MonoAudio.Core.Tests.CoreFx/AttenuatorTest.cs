﻿using System;
using System.Collections.Generic;
using System.Text;
using MonoAudio.Conversion.Resampling.Sample;
using MonoAudio.Synthesis;
using NUnit.Framework;
using CSCodec.Filters.Transformation;
using System.Numerics;
using System.Diagnostics;
using MonoAudio.Filters;

namespace MonoAudio.Core.Tests.CoreFx
{
    [TestFixture]
    public class AttenuatorTest
    {
        [Test]
        public void AttenuatorAttenuatesCorrectly()
        {
            const int Channels = 3;
            const int SampleRate = 384000;
            var src = new SquareWaveSource(new SampleFormat(Channels, SampleRate)) { Frequency = 1 };
            var att = new Attenuator(src);
            att.Scale = 0.5f;
            var buffer = new float[Channels * 1024];
            att.Read(buffer);
            Assert.AreEqual(0.5f, buffer[Channels]);
            att.Dispose();
        }
    }
}
