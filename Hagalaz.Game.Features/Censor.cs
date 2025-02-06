using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace Hagalaz.Game.Features
{
    /// <summary>
    /// 
    /// </summary>
    public class Censor
    {
        /// <summary>
        /// Gets the censored words.
        /// </summary>
        /// <value>
        /// The censored words.
        /// </value>
        public IList<string> CensoredWords { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="Censor"/> class.
        /// </summary>
        /// <param name="censoredWords">The censored words.</param>
        /// <exception cref="System.ArgumentNullException">censoredWords</exception>
        public Censor(IEnumerable<string> censoredWords)
        {
            if (censoredWords == null)
                throw new ArgumentNullException(nameof(censoredWords));

            CensoredWords = new List<string>(censoredWords);
        }

        /// <summary>
        /// Censors the text.
        /// </summary>
        /// <param name="text">The text.</param>
        /// <returns></returns>
        public string CensorText(string text) => CensorText(text, StarCensoredMatch);

        /// <summary>
        /// Censors the text.
        /// </summary>
        /// <param name="text">The text.</param>
        /// <param name="censoredMatch">The start censored match.</param>
        /// <returns></returns>
        /// <exception cref="System.ArgumentNullException">text</exception>
        public string CensorText(string text, Func<Match, string> censoredMatch)
        {
            if (text == null)
                throw new ArgumentNullException(nameof(text));

            string censoredText = text;

            foreach (string censoredWord in CensoredWords)
            {
                string regularExpression = ToRegexPattern(censoredWord);

                censoredText = Regex.Replace(censoredText, regularExpression, StarCensoredMatch,
                  RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
            }

            return censoredText;
        }

        /// <summary>
        /// Stars the censored match.
        /// </summary>
        /// <param name="m">The m.</param>
        /// <returns></returns>
        private static string StarCensoredMatch(Match m)
        {
            string word = m.Captures[0].Value;

            return new string('*', word.Length);
        }

        /// <summary>
        /// To the reg-ex pattern.
        /// </summary>
        /// <param name="wildcardSearch">The wild-card search.</param>
        /// <returns></returns>
        private static string ToRegexPattern(string wildcardSearch)
        {
            string regexPattern = Regex.Escape(wildcardSearch);

            regexPattern = regexPattern.Replace(@"\*", ".*?");
            regexPattern = regexPattern.Replace(@"\?", ".");

            if (regexPattern.StartsWith(".*?"))
            {
                regexPattern = regexPattern.Substring(3);
                regexPattern = @"(^\b)*?" + regexPattern;
            }

            regexPattern = @"\b" + regexPattern + @"\b";

            return regexPattern;
        }
    }
}
