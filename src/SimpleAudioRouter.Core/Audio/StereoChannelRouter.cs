using NAudio.CoreAudioApi;

using NAudio.Wave;

using SimpleAudioRouter.Core.Vac;



namespace SimpleAudioRouter.Core.Audio;



public sealed class StereoChannelRouter : IDisposable

{

    private readonly AudioDeviceService _devices;

    private readonly DefaultDeviceService _defaultDevices;

    private readonly VacDependencyManager _vac;



    private IWaveIn? _capture;

    private WasapiOut? _leftOut;

    private WasapiOut? _rightOut;

    private BufferedWaveProvider? _leftBuffer;

    private BufferedWaveProvider? _rightBuffer;

    private WaveFormat? _outputFormat;

    private byte[]? _leftScratch;

    private byte[]? _rightScratch;

    private string? _savedDefaultDeviceId;

    private bool _isRunning;



    private DeviceRouteGains _leftGains = DeviceRouteGains.StereoDefault();

    private DeviceRouteGains _rightGains = DeviceRouteGains.StereoDefault();



    public StereoChannelRouter(

        AudioDeviceService devices,

        DefaultDeviceService defaultDevices,

        VacDependencyManager vac)

    {

        _devices = devices;

        _defaultDevices = defaultDevices;

        _vac = vac;

    }



    public bool IsRunning => _isRunning;



    public bool DefaultDeviceApplied { get; private set; }



    public string? LastError { get; private set; }



    public AudioLevels Levels { get; } = new();



    public void SetGains(DeviceRouteGains left, DeviceRouteGains right)

    {

        _leftGains = left.Clone();

        _rightGains = right.Clone();

    }



    public void Start(string leftDeviceId, string rightDeviceId)

    {

        Stop();

        LastError = null;

        DefaultDeviceApplied = false;



        if (string.Equals(leftDeviceId, rightDeviceId, StringComparison.OrdinalIgnoreCase))

            throw new InvalidOperationException("Left and right output devices must be different.");



        try

        {

            var vac = _vac.TryGetEndpoints()

                ?? throw new InvalidOperationException("Virtual audio driver is not available.");



            var leftDevice = _devices.TryGetMmDevice(leftDeviceId)

                ?? throw new InvalidOperationException("Left output device is not available.");



            var rightDevice = _devices.TryGetMmDevice(rightDeviceId)

                ?? throw new InvalidOperationException("Right output device is not available.");



            _capture = CreateVacCapture(vac);

            _capture.DataAvailable += OnCaptureDataAvailable;



            var captureFormat = GetWaveFormat(_capture);

            _outputFormat = EnsureStereo(captureFormat);



            _capture.StartRecording();

            StartSplit(leftDevice, rightDevice, _outputFormat);



            _savedDefaultDeviceId = _defaultDevices.CaptureCurrentDefaultId();

            DefaultDeviceApplied = _defaultDevices.TrySetDefaultPlaybackDevice(vac.RenderDevice.ID);



            _isRunning = true;

        }

        catch (Exception ex)

        {

            LastError = ex.Message;

            Stop();

            throw;

        }

    }



    public void Stop()

    {

        _isRunning = false;



        if (_capture is not null)

        {

            _capture.DataAvailable -= OnCaptureDataAvailable;

            try

            {

                _capture.StopRecording();

            }

            catch

            {

                // Ignore shutdown errors.

            }



            _capture.Dispose();

            _capture = null;

        }



        StopOutput(ref _leftOut, ref _leftBuffer);

        StopOutput(ref _rightOut, ref _rightBuffer);



        _outputFormat = null;

        _leftScratch = null;

        _rightScratch = null;



        if (_savedDefaultDeviceId is not null)

        {

            _defaultDevices.RestoreDefaultPlaybackDevice(_savedDefaultDeviceId);

            _savedDefaultDeviceId = null;

        }

    }



    public string? SavedDefaultDeviceId => _savedDefaultDeviceId;



    private static IWaveIn CreateVacCapture(VacEndpoints vac)

    {

        try

        {

            return new WasapiCapture(vac.CaptureDevice, useEventSync: true, audioBufferMillisecondsLength: 100);

        }

        catch (Exception captureError)

        {

            try

            {

                return new WasapiLoopbackCapture(vac.RenderDevice);

            }

            catch (Exception loopbackError)

            {

                throw new InvalidOperationException(

                    $"Could not open virtual audio capture ({captureError.Message}). Loopback fallback also failed ({loopbackError.Message}).",

                    captureError);

            }

        }

    }



    private static WaveFormat GetWaveFormat(IWaveIn capture)

    {

        if (capture is WasapiCapture wasapiCapture)

            return wasapiCapture.WaveFormat;



        throw new NotSupportedException("Unsupported capture device type.");

    }



    private static WaveFormat EnsureStereo(WaveFormat format)

    {

        if (format.Channels == 2)

            return format;



        if (format.Channels == 1)

        {

            if (format.BitsPerSample == 16 && format.Encoding == WaveFormatEncoding.Pcm)

                return new WaveFormat(format.SampleRate, 16, 2);



            if (format.BitsPerSample == 32 && format.Encoding == WaveFormatEncoding.IeeeFloat)

                return WaveFormat.CreateIeeeFloatWaveFormat(format.SampleRate, 2);

        }



        throw new NotSupportedException($"Unsupported capture format: {format}");

    }



    private void StartSplit(MMDevice leftDevice, MMDevice rightDevice, WaveFormat format)

    {

        _leftBuffer = CreateOutputBuffer(format);

        _rightBuffer = CreateOutputBuffer(format);



        _leftOut = new WasapiOut(leftDevice, AudioClientShareMode.Shared, useEventSync: true, latency: 100);

        _rightOut = new WasapiOut(rightDevice, AudioClientShareMode.Shared, useEventSync: true, latency: 100);

        _leftOut.Init(_leftBuffer);

        _rightOut.Init(_rightBuffer);

        _leftOut.Play();

        _rightOut.Play();



        var scratchSize = Math.Max(format.AverageBytesPerSecond / 5, format.BlockAlign * 256);

        _leftScratch = new byte[scratchSize];

        _rightScratch = new byte[scratchSize];

    }



    private static BufferedWaveProvider CreateOutputBuffer(WaveFormat format) =>

        new(format)

        {

            BufferDuration = TimeSpan.FromMilliseconds(500),

            DiscardOnBufferOverflow = true,

        };



    private void OnCaptureDataAvailable(object? sender, WaveInEventArgs e)

    {

        if (e.BytesRecorded <= 0 || _capture is null || _outputFormat is null)

            return;



        var captureFormat = GetWaveFormat(_capture);

        if (captureFormat.Channels != 2)

            return;



        MeasureInputPeaks(e.Buffer, e.BytesRecorded, captureFormat);



        if (_leftBuffer is null || _rightBuffer is null || _leftScratch is null || _rightScratch is null)

            return;



        if (_leftScratch.Length < e.BytesRecorded)

        {

            _leftScratch = new byte[e.BytesRecorded];

            _rightScratch = new byte[e.BytesRecorded];

        }



        ChannelMixer.MixLeftInputRoute(

            e.Buffer,

            e.BytesRecorded,

            captureFormat,

            _leftGains,

            _leftScratch,

            out var leftPeakL,

            out var leftPeakR);



        ChannelMixer.MixRightInputRoute(

            e.Buffer,

            e.BytesRecorded,

            captureFormat,

            _rightGains,

            _rightScratch,

            out var rightPeakL,

            out var rightPeakR);



        Levels.UpdateLeftOutput(leftPeakL, leftPeakR);

        Levels.UpdateRightOutput(rightPeakL, rightPeakR);



        _leftBuffer.AddSamples(_leftScratch, 0, e.BytesRecorded);

        _rightBuffer.AddSamples(_rightScratch, 0, e.BytesRecorded);

    }



    private void MeasureInputPeaks(byte[] buffer, int bytesRecorded, WaveFormat format)

    {

        float peakL = 0f;

        float peakR = 0f;



        if (format.BitsPerSample == 16 && format.Encoding == WaveFormatEncoding.Pcm)

        {

            var frames = bytesRecorded / 4;

            for (var i = 0; i < frames; i++)

            {

                var src = i * 4;

                var inL = Math.Abs(((short)(buffer[src] | (buffer[src + 1] << 8))) / 32768f);

                var inR = Math.Abs(((short)(buffer[src + 2] | (buffer[src + 3] << 8))) / 32768f);

                peakL = Math.Max(peakL, inL);

                peakR = Math.Max(peakR, inR);

            }

        }

        else if (format.BitsPerSample == 32 && format.Encoding == WaveFormatEncoding.IeeeFloat)

        {

            var frames = bytesRecorded / 8;

            for (var i = 0; i < frames; i++)

            {

                var src = i * 8;

                peakL = Math.Max(peakL, Math.Abs(BitConverter.ToSingle(buffer, src)));

                peakR = Math.Max(peakR, Math.Abs(BitConverter.ToSingle(buffer, src + 4)));

            }

        }



        Levels.UpdateInput(peakL, peakR);

    }



    private static void StopOutput(ref WasapiOut? output, ref BufferedWaveProvider? buffer)

    {

        if (output is not null)

        {

            try

            {

                output.Stop();

            }

            catch

            {

                // Ignore shutdown errors.

            }



            output.Dispose();

            output = null;

        }



        buffer = null;

    }



    public void Dispose() => Stop();

}


