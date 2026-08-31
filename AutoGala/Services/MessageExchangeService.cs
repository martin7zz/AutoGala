using AutoGala.Contracts;
using AutoGala.Ipc;
using AutoGala.Services.Notifiers;
using Plugin.Core.Models;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;

namespace AutoGala.Services
{
    public class MessageExchangeService : IMessageExchangeService
    {

        public async Task<List<SectionItem>> GetSectionsAsync(IAutoGalaPipeClientService autoGalaPipeClientService, ISortingService sortingService)
        {
            var request = new PluginRequest { Action = "GetSections", PayloadJson = { } };
            var response = await autoGalaPipeClientService.SendAsync(request);

            if (response.Success)
            {
                var list = JsonSerializer.Deserialize<List<LineData>>(response.ResultJson);
                if (list != null)
                {
                    var sections = sortingService.SortLines(list);
                    return sections;
                }
            }
            else
            {
                return [];
                throw new InvalidOperationException(response.Error);
            }

            return [];
        }
    }
}
