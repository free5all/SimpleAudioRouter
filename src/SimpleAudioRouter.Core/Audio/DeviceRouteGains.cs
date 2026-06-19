namespace SimpleAudioRouter.Core.Audio;



public sealed class DeviceRouteGains

{

    public float ToOutputLeft { get; set; } = 1f;

    public float ToOutputRight { get; set; } = 1f;



    public static DeviceRouteGains StereoDefault() => new()

    {

        ToOutputLeft = 1f,

        ToOutputRight = 1f,

    };



    public DeviceRouteGains Clone() => new()

    {

        ToOutputLeft = ToOutputLeft,

        ToOutputRight = ToOutputRight,

    };

}


