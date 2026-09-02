using AutoGala.Contracts;
using AutoGala.Ipc;
using AutoGala.Plugin.models;
using Plugin.Core.Contracts;
using Plugin.Core.Models;
using System.Text.Json;

namespace AutoGala.Services
{
    public class MessageExchangeService : IMessageExchangeService
    {

        private readonly ISectionService _sectionService;
        private readonly IRebarService _rebarService;

        public MessageExchangeService(
            ISectionService sectionService,
            IRebarService rebarService)
        {
            _sectionService = sectionService;
            _rebarService = rebarService;
        }

        public async Task<(List<SectionItem>, string)> GetSectionsAsync(IAutoGalaPipeClientService autoGalaPipeClientService, string name)
        {
            var request = new PluginRequest { Action = "GetShape", PayloadJson = JsonSerializer.Serialize(new { name }) };
            var response = await autoGalaPipeClientService.SendAsync(request);

            if (!response.Success)
                throw new InvalidOperationException(response.Error);

            var shapeResult = JsonSerializer.Deserialize<ShapeResult>(response.ResultJson);
            if (shapeResult == null)
                throw new InvalidOperationException("GetShape returned no data.");

            var sections = new List<SectionItem>();

            int id = 1;
            foreach (var vertex in shapeResult.Vertices)
            {
                var newSection = _sectionService.CreateSection(vertex.X, vertex.Y);
                newSection.Id = id++;
                sections.Add(newSection);
            }

            return (sections, shapeResult.Name);
        }

        public async Task<(List<RebarItem>, string)> GetRebarsAsync(IAutoGalaPipeClientService autoGalaPipeClientService, string name)
        {
            var request = new PluginRequest { Action = "GetShape", PayloadJson = JsonSerializer.Serialize(new { name }) };
            var response = await autoGalaPipeClientService.SendAsync(request);

            if (!response.Success)
                throw new InvalidOperationException(response.Error);

            var shapeResult = JsonSerializer.Deserialize<ShapeResult>(response.ResultJson);
            if (shapeResult == null)
                throw new InvalidOperationException("AutoCAD returned no data.");

            if (!shapeResult.Rebars.Any())
                throw new InvalidOperationException("No rebars were selected.");

            var rebars = new List<RebarItem>();

            int id = 1;
            foreach (var rebar in shapeResult.Rebars)
            {
                var newRebar = _rebarService.CreateRebar(rebar.Area, rebar.PositionData?.X, rebar.PositionData?.Y);
                newRebar.Id = id++;
                rebars.Add(newRebar);
            }

            return (rebars, shapeResult.Name);
        }

        public async Task<((List<SectionItem>, List<RebarItem>), string)> GetAllAsync(IAutoGalaPipeClientService autoGalaPipeClientService, string name)
        {
            var request = new PluginRequest { Action = "GetShape", PayloadJson = JsonSerializer.Serialize(new { name }) };
            var response = await autoGalaPipeClientService.SendAsync(request);

            if (!response.Success)
                throw new InvalidOperationException(response.Error);

            var shapeResult = JsonSerializer.Deserialize<ShapeResult>(response.ResultJson);
            if (shapeResult == null)
                throw new InvalidOperationException("AutoCAD returned no data.");

            if (!shapeResult.Rebars.Any())
                throw new InvalidOperationException("No rebars were selected.");

            var sections = new List<SectionItem>();

            int sectionId = 1;
            foreach (var vertex in shapeResult.Vertices)
            {
                var newSection = _sectionService.CreateSection(vertex.X, vertex.Y);
                newSection.Id = sectionId++;
                sections.Add(newSection);
            }

            var rebars = new List<RebarItem>();

            int rebarId = 1;
            foreach (var rebar in shapeResult.Rebars)
            {
                var newRebar = _rebarService.CreateRebar(rebar.Area, rebar.PositionData?.X, rebar.PositionData?.Y);
                newRebar.Id = rebarId++;
                rebars.Add(newRebar);
            }

            return ((sections, rebars), shapeResult.Name);
        }
    }
}
