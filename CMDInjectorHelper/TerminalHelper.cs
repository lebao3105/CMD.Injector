namespace CMDInjectorHelper
{
    public static class TerminalHelper
    {
        public static int FontSize
        {
            get
            {
                var fontSize = Helper.LocalSettingsHelper.LoadSettings("ConFontSizeSet", 3);
                return (5 > fontSize && fontSize > 0) ? 12 + fontSize : 15;
            }
        }

        public static string DirectoryFile
        {
            get { return $"{Helper.localFolder.Path}\\TerminalDirectory.txt"; }
        }

        public static string ResultFile
        {
            get { return $"{Helper.localFolder.Path}\\TerminalResult.txt"; }
        }

        public static string EndFile
        {
            get { return $"{Helper.localFolder.Path}\\TerminalEnd.txt"; }
        }

        public static string EscapeSymbols(string command, bool toSymbol)
        {
            if (toSymbol)
            {
                return command.Replace("&#x033", "!").Replace("&#x042", "*").Replace("&#x047", "/").Replace("&#x092", "\\").Replace("&#x063", "?")
                    .Replace("&#x034", "\"").Replace("&#x058", ":").Replace("&#x059", ";").Replace("&#x044", ",").Replace("&#x060", "<").Replace("&#x062", ">").Replace("&#x124", "|");
            }
            else
            {
                return command.Replace("!", "&#x033").Replace("*", "&#x042").Replace("/", "&#x047").Replace("\\", "&#x092").Replace("?", "&#x063")
                    .Replace("\"", "&#x034").Replace(":", "&#x058").Replace(";", "&#x059").Replace(",", "&#x044").Replace("<", "&#x060").Replace(">", "&#x062").Replace("|", "&#x124");
            }
        }
    }
}
