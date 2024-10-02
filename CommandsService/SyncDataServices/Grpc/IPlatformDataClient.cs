using CommandsService.Models;

namespace CommandsService.SyncDataServices.Grpc
{
    public interface IPlatformDataClient
    {
        public IEnumerable<Platform> ReturnAllPlatforms();
    }
}