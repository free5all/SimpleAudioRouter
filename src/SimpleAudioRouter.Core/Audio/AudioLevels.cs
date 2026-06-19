namespace SimpleAudioRouter.Core.Audio;

public sealed class AudioLevels
{
    private readonly object _sync = new();
    private float _inputLeft;
    private float _inputRight;
    private float _leftOutputLeft;
    private float _leftOutputRight;
    private float _rightOutputLeft;
    private float _rightOutputRight;

    public void UpdateInput(float left, float right)
    {
        lock (_sync)
        {
            _inputLeft = Math.Max(_inputLeft, left);
            _inputRight = Math.Max(_inputRight, right);
        }
    }

    public void UpdateLeftOutput(float left, float right)
    {
        lock (_sync)
        {
            _leftOutputLeft = Math.Max(_leftOutputLeft, left);
            _leftOutputRight = Math.Max(_leftOutputRight, right);
        }
    }

    public void UpdateRightOutput(float left, float right)
    {
        lock (_sync)
        {
            _rightOutputLeft = Math.Max(_rightOutputLeft, left);
            _rightOutputRight = Math.Max(_rightOutputRight, right);
        }
    }

    public AudioLevelSnapshot ReadAndDecay(float decay = 0.82f)
    {
        lock (_sync)
        {
            var snapshot = new AudioLevelSnapshot(
                _inputLeft,
                _inputRight,
                _leftOutputLeft,
                _leftOutputRight,
                _rightOutputLeft,
                _rightOutputRight);

            _inputLeft *= decay;
            _inputRight *= decay;
            _leftOutputLeft *= decay;
            _leftOutputRight *= decay;
            _rightOutputLeft *= decay;
            _rightOutputRight *= decay;

            return snapshot;
        }
    }
}

public readonly record struct AudioLevelSnapshot(
    float InputLeft,
    float InputRight,
    float LeftOutputLeft,
    float LeftOutputRight,
    float RightOutputLeft,
    float RightOutputRight);
