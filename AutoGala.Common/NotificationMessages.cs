using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AutoGala.Common
{
    public static class NotificationMessages
    {
        public const string DefaultErrorMessage = "Something went wrong, please try again later.";

        public const string ClipboardErrorMessage = "Could not paste data.";

        public const string NoClipboardDataErrorMassage = "Clipboard is empty.";

        public const string SectionPasteErrorMessage = "Data not in correct format - (X, Y) columns.";

        public const string RebarPasteErrorMessage = "Data not in correct format - (Area, X, Y) columns.";

        public const string LoadPasteErrorMessage = "Data not in correct format - (N, Mx, My) columns.";

        public const string WaitingGalaClickMessage = "Please click anywhere on Gala window.";

        public const string TransferingToGalaMessage = "Transfering data to Gala.";

        public const string NoDataErrorMessage = "There is nothing to transfer.";

        public const string NoGalaElementFoundErrorMessage = "No element found at that point.";

        public const string NoGalaStructureFoundErrorMessage = "Could not locate the structure in Gala.";

        public const string UnfilledSectionErrorMessage = "Fill in X/Y for every section.";
        
        public const string UnfilledRebarErrorMessage = "Fill in Area/X/Y for every rebar.";

        public const string UnfilledLoadErrorMessage = "Fill in N/Mx/My for every load.";
    }
}
