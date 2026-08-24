using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Plugin.Core.Models
{
    public class JobInfo
    {
        public string JobTitle { get; set; }
        public string JobNumber { get; set; }
        public string Client { get; set; }
        public string CalcsBy { get; set; }
        public string CheckedBy { get; set; }
    }
}
