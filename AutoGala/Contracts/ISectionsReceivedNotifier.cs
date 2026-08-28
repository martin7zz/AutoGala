using Plugin.Core.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace AutoGala.Contracts
{
    public interface ISectionsReceivedNotifier
    {
        event Action<List<SectionItem>>? SectionsReceived;
        void NotifySectionsRecieved(List<SectionItem> sections);
    }
}
