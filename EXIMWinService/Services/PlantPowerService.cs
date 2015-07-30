using System;
using System.Collections.Generic;

using EXIMWinService.WebApi;
using EXIMWinService.Model;

namespace EXIMWinService.Services
{
    public interface IPlantPowerService
    {
        IList<PlantPower> GetPlantPower(string plant);
        bool TrySendPlantPower(PlantPower plantPower);

    }

    public class PlantPowerService : IPlantPowerService
    {
        private static IWebApiClient _apiClient;

        public PlantPowerService(IWebApiClient apiClient)
        {
            _apiClient = apiClient;
        }

        public IList<PlantPower> GetPlantPower(string plant)
        {
            return _apiClient.GetPlantPower(plant);
        }

        public bool TrySendPlantPower(PlantPower plantPower)
        {
            try
            {
                _apiClient.PutPlantPower(plantPower);
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}
