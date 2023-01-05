using System;
using System.Collections.Generic;
using System.Text;

namespace GenerateMapping.Model
{
    internal static class AccessorsMatcher
    {
        enum MatchLevel { Perfect, IgnoreCase, WithoutPrefix, TheSameWords, Flattening, None }

        public static IEnumerable<Match> DoMatching(IEnumerable<Accessor> leftAccessors, IEnumerable<Accessor> rightAccessors)
        {
            var results = new List<Match>();

            foreach (MatchLevel level in Enum.GetValues(typeof(MatchLevel)))
            {
                foreach (var leftAccessor in StreamAccessors(leftAccessors))
                {
                    foreach (var rightAccessor in StreamAccessors(rightAccessors))
                    {
                        bool isMatch = false;

                        switch (level)
                        {
                            case MatchLevel.Perfect:
                                isMatch = leftAccessor.Name == rightAccessor.Name;
                                break;
                            case MatchLevel.IgnoreCase:
                                isMatch = String.Equals(leftAccessor.Name, rightAccessor.Name, StringComparison.OrdinalIgnoreCase);
                                break;
                            case MatchLevel.WithoutPrefix:
                                {
                                    var leftName = leftAccessor.Name.WithoutPrefix();
                                    var rightName = rightAccessor.Name.WithoutPrefix();
                                    isMatch = String.Equals(leftName, rightName, StringComparison.OrdinalIgnoreCase);
                                }
                                break;
                            case MatchLevel.TheSameWords:
                                {
                                    var leftName = string.Join("", leftAccessor.Name.WithoutPrefix().SplitStringIntoSeparateWords());
                                    var rightName = string.Join("", rightAccessor.Name.WithoutPrefix().SplitStringIntoSeparateWords());
                                    isMatch = String.Equals(leftName, rightName, StringComparison.OrdinalIgnoreCase);
                                }
                                break;
                            case MatchLevel.Flattening:
                                var flattenRightName = rightAccessor.Parent?.Name + rightAccessor.Name;
                                isMatch = String.Equals(leftAccessor.Name, flattenRightName, StringComparison.OrdinalIgnoreCase);
                                break;
                        }

                        if (isMatch)
                        {
                            results.Add(new Match(leftAccessor, rightAccessor));
                            leftAccessor.IsMatched = true;
                            rightAccessor.IsMatched = true;
                        }
                    }
                }
            }

            return results;
        }

        private static IEnumerable<Accessor> StreamAccessors(IEnumerable<Accessor> accessors)
        {
            foreach (var accessor in accessors)
            {
                if (accessor.IsMatched == false)
                {
                    yield return accessor;
                }
            }
            foreach (var accessor in accessors)
            {
                if (accessor.IsMatched == false)
                {
                    foreach (var child in StreamAccessors(accessor.Children))
                    {

                        yield return child;
                    }
                }
            }
        }
    }

    
    internal class Match
    {
        public Accessor LeftAccessor { get; }
        public Accessor RightAccessor { get; }


        public Match(Accessor leftAccessor, Accessor rightAccessor)
        {
            LeftAccessor = leftAccessor;
            RightAccessor = rightAccessor;
        }
    }
}
