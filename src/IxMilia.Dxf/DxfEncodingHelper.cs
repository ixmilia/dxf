using System.Text.RegularExpressions;

namespace IxMilia.Dxf
{
    internal class DxfEncodingHelper
    {
        private static readonly Regex DxfCodePagePattern = new Regex(@"^ANSI_(\d+)$", RegexOptions.IgnoreCase);

        public static bool TryParseEncoding(string encodingName, out int codePage)
        {
            var match = DxfCodePagePattern.Match(encodingName);
            if (match.Success &&
                match.Groups.Count >= 2 &&
                int.TryParse(match.Groups[1].Value, out codePage))
            {
                return true;
            }

            codePage = 0;
            return false;
        }
    }
}
