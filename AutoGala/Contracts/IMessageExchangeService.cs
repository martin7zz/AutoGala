using AutoGala.Services;
using AutoGala.Services.Notifiers;
using Plugin.Core.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace AutoGala.Contracts
{
    public interface IMessageExchangeService
    {
        Task<List<SectionItem>> GetSectionsAsync(IAutoGalaPipeClientService autoGalaPipeClientService, ISortingService sortingService);
    }
}
