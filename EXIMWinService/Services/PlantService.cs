using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using EXIMWinService.WebApi;
using EXIMWinService.Model;

namespace EXIMWinService.Services
{
    public interface IPlantService
    {
        double GetPlantPower(string plant);
        ApiPlant GetPlant(string plant);
    }

    public class PlantService : IPlantService
    {
        private static IWebApiClient _apiClient;

        public PlantService(IWebApiClient apiClient)
        {
            _apiClient = apiClient;
        }

        public double GetPlantPower(string idPlant)
        {
            var plant = _apiClient.GetPlant(idPlant);
            return plant.Power;
        }
        public ApiPlant GetPlant(string idPlant)
        {
            return _apiClient.GetPlant(idPlant);

        }
    }
}
