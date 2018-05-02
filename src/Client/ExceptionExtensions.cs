using System;
using System.Text;

namespace Client
{
    public static class ExceptionExtensions
    {
        public static string GetAllMessages(this Exception ex)
        {
            var text = new StringBuilder();
            text.AppendLine(ex.Message);
            var inner = ex.InnerException;
            if (inner != null)
            {
                text.AppendLine();
                text.AppendLine("=== Details ===");
                while (inner != null)
                {
                    text.AppendLine($"{inner.GetType().Name}: {inner.Message}");
                    inner = inner.InnerException;
                }
            }

            return text.ToString();
        }
    }
}