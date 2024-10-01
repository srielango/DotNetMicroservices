using PlatformService.Dtos;

namespace PlatformService.AsyncDataServices
{
    public interface IMessageBusClient
    {
        void PublishPlatform(PlatformPublishedDto platformPublishedDto);
    }
}