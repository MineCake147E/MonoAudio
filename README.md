![MonoAudio Logo](MonoAudio-Logo.png)

# MonoAudio - .NET Standard Audio Library
A Cross-Platform Audio Library for .NET Standard.

#### Usage of MonoAudio
- An audio output abstraction layer `MonoAudio.Core`

### Currently supported features
- Audio outputs
  - [CSCore](https://github.com/filoe/cscore) Inter-Operating output
  - UWP `AudioGraph` output
- Fast and smooth Upsampling using Catmull-Rom Spline
  - About 90x faster than real time in 44.1kHz→192kHz **10ch** on .NET Core, Intel Core i7 4790.
  - About 150x faster than real time in 44.1kHz→192kHz **Stereo** on .NET Core, Intel Core i7 4790.
  - Uses `MemoryMarshal.Cast<float,Vector2>(Span<float>)` so it doesn't copy while casting.
- `FastFill` for some types that fills quickly using `Vector<T>`.

  
### Currently implemented features(not tested yet)
- Optimized BiQuad Filters that supports some filtering
  - Uses `Vector2` and `Vector3` for filter calculations in each channels.

### Dependencies and system requirements
- The speed of `SplineResampler` depends on the fast C# Integer Division Library **[DivideSharp](https://github.com/MineCake147E/DivideSharp)**
  - Divides by "almost constant" number, about 2x faster than ordinal division(idiv instruction)!
  - Implements the same technology that is used in `RyuJIT` constant division optimization, ported to C#!
  - Improved `SplineResampler`'s performance greatly, about **1.5x** faster on Stereo!
- Currently, ***Unity IS NOT SUPPORTED AT ALL!***
  - Because Unity uses older version of `Mono`.
- Faster resampling requires `.NET Core` or later version of `Mono`.
  - Unfortunately, `.NET Framework` does not support Fast `Span<T>`s.
- The all processing in this library fully depends on SINGLE core.
  - Because `Span<T>` does not support multi-thread processing at all.

### Useful external library for MonoAudio
- [CSCodec](https://github.com/MineCake147E/CSCodec) that supports more signal processing like FFT and DWT.

### Features under development
- Xamarin.Android `AudioTrack` output
- Xamarin.iOS `AudioUnit` output
- OpenTK `AL` output
