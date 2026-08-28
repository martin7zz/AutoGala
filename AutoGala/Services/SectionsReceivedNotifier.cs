using AutoGala.Contracts;
using Plugin.Core.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace AutoGala.Services
{
    public class SectionsReceivedNotifier : ISectionsReceivedNotifier
    {
        public event Action<List<SectionItem>>? SectionsReceived;

        public void NotifySectionsRecieved(List<SectionItem> sections)
        {
            SectionsReceived?.Invoke(sections);
        }
    }
}
