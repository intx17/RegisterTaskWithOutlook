using System;
using RegisterTaskWithOutlook.Entities;
using RegisterTaskWithOutlook.Extensions;

namespace RegisterTaskWithOutlook.Utilities
{
    public static class SlashCommandWebhookParser
    {
        public static SlashCommandText ParseText(string rawText)
        {
            var splitted = rawText.Trim().Split(" ");
            if (splitted.Length <= 0)
            {
                throw new ArgumentException("引数が足りません");
            }

            string arg1;
            string arg2;
            splitted.TryGetValue(1, out arg1);
            splitted.TryGetValue(2, out arg2);
            return new SlashCommandText
            {
                Action = splitted[0],
                Arg1 = arg1 ?? "",
                Arg2 = arg2 ?? "",
            };
        }
    }
}