using System;
using System.Linq.Expressions;
using System.Reflection;
using System.Text.RegularExpressions;

namespace LogGrokCore.Data
{
    public static class MatchSurgery
    { 
        private static readonly Func<Match, int[][]> GetMatchesDelegate =
            GetGetFieldDelegate<Match, int[][]>(
                typeof(Match).GetField("_matches", BindingFlags.NonPublic | BindingFlags.Instance));
        
        private static readonly Func<Match, int[]> GetMatchCountDelegate =
            GetGetFieldDelegate<Match, int[]>(
                typeof(Match).GetField("_matchcount", BindingFlags.NonPublic | BindingFlags.Instance));

        private static Func<TSource, TValue> GetGetFieldDelegate<TSource, TValue>(FieldInfo? fieldInfo)
        {
            if (fieldInfo == null) throw new ArgumentNullException(nameof(fieldInfo));

            ParameterExpression sourceParameter =
                Expression.Parameter(typeof(TSource), "source");
 
            MemberExpression fieldExpression = Expression.Field(sourceParameter, fieldInfo);

            LambdaExpression lambda =
                Expression.Lambda(typeof(Func<TSource, TValue>), fieldExpression, sourceParameter);
 
            return (Func<TSource, TValue>)lambda.Compile();
        }
        public static int[][] GetCaptures(Match match)
        {
            return GetMatchesDelegate(match);
        }

        public static int[] GetMatchCounts(Match match)
        {
            return GetMatchCountDelegate(match);
        }
    }
}