using System;

namespace HKBN.ProAssetInspector.Utilities
{
    public static class ExceptionHelper
    {
        public static string ToFriendlyMessage(Exception ex)
        {
            if (ex == null)
                return "An unknown error occurred.";

            var message = ex.Message;
            var inner = ex.InnerException;
            while (inner != null)
            {
                if (!string.IsNullOrWhiteSpace(inner.Message))
                    message = inner.Message;
                inner = inner.InnerException;
            }

            return string.IsNullOrWhiteSpace(message) ? "An unexpected error occurred." : message;
        }
    }
}
