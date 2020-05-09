using System.Collections.Generic;

namespace StaticSiteGenerator
{
    public class TokenInterpreter
    {
        public Dictionary<string, List<string>> Translate(IEnumerable<string> tokens)
        {
            var tokensByFileName = new Dictionary<string, List<string>>();

            tokensByFileName.Add(string.Empty, new List<string>());

            foreach (var token in tokens)
            {
                var splits = token.Split(':');

                if (splits.Length == 1)
                {
                    tokensByFileName.Add(splits[0], new List<string>());
                }
                else if (splits.Length == 2)
                {
                    var fileName = splits[0];

                    if (!tokensByFileName.ContainsKey(fileName)) tokensByFileName.Add(fileName, new List<string>());

                    tokensByFileName[fileName].Add(splits[1]);
                }
            }

            return tokensByFileName;
        }
    }
}