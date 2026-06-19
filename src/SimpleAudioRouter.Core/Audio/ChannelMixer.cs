using NAudio.Wave;



namespace SimpleAudioRouter.Core.Audio;



public static class ChannelMixer

{

    public static void MixLeftInputRoute(

        byte[] input,

        int bytesRecorded,

        WaveFormat format,

        DeviceRouteGains gains,

        byte[] output,

        out float peakOutputLeft,

        out float peakOutputRight)

    {

        peakOutputLeft = 0f;

        peakOutputRight = 0f;



        if (format.Channels != 2)

            throw new NotSupportedException("Only stereo input is supported.");



        if (format.BitsPerSample == 16 && format.Encoding == WaveFormatEncoding.Pcm)

            MixLeftPcm16(input, bytesRecorded, gains, output, ref peakOutputLeft, ref peakOutputRight);

        else if (format.BitsPerSample == 32 && format.Encoding == WaveFormatEncoding.IeeeFloat)

            MixLeftFloat32(input, bytesRecorded, gains, output, ref peakOutputLeft, ref peakOutputRight);

        else

            throw new NotSupportedException($"Unsupported format: {format}");

    }



    public static void MixRightInputRoute(

        byte[] input,

        int bytesRecorded,

        WaveFormat format,

        DeviceRouteGains gains,

        byte[] output,

        out float peakOutputLeft,

        out float peakOutputRight)

    {

        peakOutputLeft = 0f;

        peakOutputRight = 0f;



        if (format.Channels != 2)

            throw new NotSupportedException("Only stereo input is supported.");



        if (format.BitsPerSample == 16 && format.Encoding == WaveFormatEncoding.Pcm)

            MixRightPcm16(input, bytesRecorded, gains, output, ref peakOutputLeft, ref peakOutputRight);

        else if (format.BitsPerSample == 32 && format.Encoding == WaveFormatEncoding.IeeeFloat)

            MixRightFloat32(input, bytesRecorded, gains, output, ref peakOutputLeft, ref peakOutputRight);

        else

            throw new NotSupportedException($"Unsupported format: {format}");

    }



    private static void MixLeftPcm16(

        byte[] input,

        int bytesRecorded,

        DeviceRouteGains gains,

        byte[] output,

        ref float peakOutputLeft,

        ref float peakOutputRight)

    {

        var frames = bytesRecorded / 4;

        for (var i = 0; i < frames; i++)

        {

            var src = i * 4;

            var dst = i * 4;

            var inL = SampleToFloat(input[src], input[src + 1]);

            var outL = Clamp(inL * gains.ToOutputLeft);

            var outR = Clamp(inL * gains.ToOutputRight);

            peakOutputLeft = Math.Max(peakOutputLeft, Math.Abs(outL));

            peakOutputRight = Math.Max(peakOutputRight, Math.Abs(outR));

            WriteSample(output, dst, outL);

            WriteSample(output, dst + 2, outR);

        }

    }



    private static void MixRightPcm16(

        byte[] input,

        int bytesRecorded,

        DeviceRouteGains gains,

        byte[] output,

        ref float peakOutputLeft,

        ref float peakOutputRight)

    {

        var frames = bytesRecorded / 4;

        for (var i = 0; i < frames; i++)

        {

            var src = i * 4;

            var dst = i * 4;

            var inR = SampleToFloat(input[src + 2], input[src + 3]);

            var outL = Clamp(inR * gains.ToOutputLeft);

            var outR = Clamp(inR * gains.ToOutputRight);

            peakOutputLeft = Math.Max(peakOutputLeft, Math.Abs(outL));

            peakOutputRight = Math.Max(peakOutputRight, Math.Abs(outR));

            WriteSample(output, dst, outL);

            WriteSample(output, dst + 2, outR);

        }

    }



    private static void MixLeftFloat32(

        byte[] input,

        int bytesRecorded,

        DeviceRouteGains gains,

        byte[] output,

        ref float peakOutputLeft,

        ref float peakOutputRight)

    {

        var frames = bytesRecorded / 8;

        for (var i = 0; i < frames; i++)

        {

            var src = i * 8;

            var dst = i * 8;

            var inL = BitConverter.ToSingle(input, src);

            var outL = Clamp(inL * gains.ToOutputLeft);

            var outR = Clamp(inL * gains.ToOutputRight);

            peakOutputLeft = Math.Max(peakOutputLeft, Math.Abs(outL));

            peakOutputRight = Math.Max(peakOutputRight, Math.Abs(outR));

            BitConverter.TryWriteBytes(output.AsSpan(dst), outL);

            BitConverter.TryWriteBytes(output.AsSpan(dst + 4), outR);

        }

    }



    private static void MixRightFloat32(

        byte[] input,

        int bytesRecorded,

        DeviceRouteGains gains,

        byte[] output,

        ref float peakOutputLeft,

        ref float peakOutputRight)

    {

        var frames = bytesRecorded / 8;

        for (var i = 0; i < frames; i++)

        {

            var src = i * 8;

            var dst = i * 8;

            var inR = BitConverter.ToSingle(input, src + 4);

            var outL = Clamp(inR * gains.ToOutputLeft);

            var outR = Clamp(inR * gains.ToOutputRight);

            peakOutputLeft = Math.Max(peakOutputLeft, Math.Abs(outL));

            peakOutputRight = Math.Max(peakOutputRight, Math.Abs(outR));

            BitConverter.TryWriteBytes(output.AsSpan(dst), outL);

            BitConverter.TryWriteBytes(output.AsSpan(dst + 4), outR);

        }

    }



    private static float SampleToFloat(byte low, byte high)

    {

        short sample = (short)(low | (high << 8));

        return sample / 32768f;

    }



    private static void WriteSample(byte[] buffer, int offset, float sample)

    {

        var value = (short)Math.Clamp(sample * 32767f, short.MinValue, short.MaxValue);

        buffer[offset] = (byte)(value & 0xFF);

        buffer[offset + 1] = (byte)((value >> 8) & 0xFF);

    }



    private static float Clamp(float value) => Math.Clamp(value, -1f, 1f);

}


