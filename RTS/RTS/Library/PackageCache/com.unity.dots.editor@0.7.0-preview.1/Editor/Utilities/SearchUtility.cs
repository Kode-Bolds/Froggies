using System;
using System.Collections.Generic;
using System.Linq;

namespace Unity.Entities.Editor
{
    static class SearchUtility
    {
        /// <summary>
        /// Split search string by space for later manipulation.
        /// <param name="searchString">The given search string.</param>
        /// <returns>IEnumerable<string></returns>
        /// </summary>
        public static IEnumerable<string> SplitSearchStringBySpace(string searchString)
        {
            searchString = searchString.Trim();

            if (!searchString.Contains(" "))
            {
                yield return searchString;
                yield break;
            }

            searchString = searchString.Replace(": ", ":");

            //var stringArray = searchString.Split(' ');

            foreach (var singleString in searchString.Split(' '))
            {
                yield return singleString;
            }
        }

        /// <summary>
        /// Get string followed by given token.
        /// <param name="searchString">The given search string.</param>
        /// <param name="token">The given token.</param>
        /// <returns>IEnumerable<string></returns>
        /// </summary>
        public static IEnumerable<string> GetStringFollowedByGivenToken(string searchString, string token)
        {
            if (!searchString.Contains(token))
            {
                yield break;
            }

            foreach (var singleString in SplitSearchStringBySpace(searchString))
            {
                if (singleString.StartsWith(token))
                {
                    yield return singleString.Substring(token.Length);
                }
            }
        }

        /// <summary>
        /// Check if a string contains given token and followed by given string.
        /// <param name="searchString">The given search string.</param>
        /// <param name="token">The given token.</param>
        /// <param name="searchItemName">Name to be searched.</param>
        /// <returns>bool</returns>
        /// </summary>
        public static bool CheckIfStringContainsGivenTokenAndName(string searchString, string token, string searchItemName)
        {
            if (string.IsNullOrEmpty(searchString) || !searchString.ToLower().Contains(token.ToLower()) || !searchString.ToLower().Contains(searchItemName.ToLower()))
                return false;

            foreach (var singleString in SplitSearchStringBySpace(searchString))
            {
                if (singleString.StartsWith(token, StringComparison.OrdinalIgnoreCase)
                    && string.Compare(singleString, token + searchItemName, StringComparison.OrdinalIgnoreCase) == 0)
                    return true;
            }

            return false;
        }
    }
}
