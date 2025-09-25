using System.ServiceModel;
using Shared.Contracts;

public static class ChannelFactoryHelpers
{
    public static ChannelFactory<ISensorService> CreateSensor(string baseUrl)
    {
        var binding = new BasicHttpBinding();
        var address = new EndpointAddress(baseUrl);
        return new ChannelFactory<ISensorService>(binding, address);
    }

}
