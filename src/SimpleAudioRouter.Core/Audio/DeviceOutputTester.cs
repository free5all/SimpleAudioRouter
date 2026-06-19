using NAudio.CoreAudioApi;
using NAudio.Wave;
using SimpleAudioRouter.Core.Vac;

namespace SimpleAudioRouter.Core.Audio;

public enum RouteTestChannel
{
    Left,
    Right,
}

public static class DeviceOutputTester
{
    private const int SampleRate = 44100;
    private const int DurationMs = 200;
    private const float FrequencyHz = 880f;
    private const float Volume = 0.35f;

    public static Task PlayRouteTestAsync(VacDependencyManager vac, RouteTestChannel channel) =>
        Task.Run(() => PlayRouteTest(vac, channel));

    public static void PlayRouteTest(VacDependencyManager vac, RouteTestChannel channel)
    {
        var endpoints = vac.TryGetEndpoints()
            ?? throw new InvalidOperationException("Virtual audio driver is not available.");

        var format = WaveFormat.CreateIeeeFloatWaveFormat(SampleRate, 2);
        var sampleCount = SampleRate * DurationMs / 1000;
        var bytes = new byte[sampleCount * format.BlockAlign];

        for (var i = 0; i < sampleCount; i++)
        {
            var t = i / (float)SampleRate;
            var envelope = GetEnvelope(i, sampleCount);
            var tone = MathF.Sin(2f * MathF.PI * FrequencyHz * t) * Volume * envelope;
            var left = channel == RouteTestChannel.Left ? tone : 0f;
            var right = channel == RouteTestChannel.Right ? tone : 0f;
            var offset = i * format.BlockAlign;
            BitConverter.TryWriteBytes(bytes.AsSpan(offset, 4), left);
            BitConverter.TryWriteBytes(bytes.AsSpan(offset + 4, 4), right);
        }

        var buffer = new BufferedWaveProvider(format)
        {
            BufferDuration = TimeSpan.FromMilliseconds(500),
            DiscardOnBufferOverflow = true,
        };
        buffer.AddSamples(bytes, 0, bytes.Length);

        using var output = new WasapiOut(
            endpoints.RenderDevice,
            AudioClientShareMode.Shared,
            useEventSync: true,
            latency: 100);
        output.Init(buffer);
        output.Play();
        Thread.Sleep(DurationMs + 80);
        output.Stop();
    }

    private static float GetEnvelope(int index, int sampleCount)
    {
        var fadeIn = sampleCount * 0.08f;
        var fadeOutStart = sampleCount * 0.82f;

        if (index < fadeIn)
            return index / fadeIn;

        if (index > fadeOutStart)
            return (sampleCount - index) / (sampleCount - fadeOutStart);

        return 1f;
    }
}
