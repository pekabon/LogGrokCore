using System;
using System.Linq.Expressions;
using System.Reflection;
using System.Windows.Media;

namespace LogGrokCore.Controls.TextRender
{
    public static class GlyphRunSurgery
    {
        private static readonly Action<GlyphRun, TextFormattingMode> SetTextFormattingModeDelegate =
            GetSetFieldDelegate<GlyphRun, TextFormattingMode>(
                typeof(GlyphRun).GetField("_textFormattingMode", BindingFlags.NonPublic | BindingFlags.Instance));
        
        public static void SetDisplayTextFormattingMode(GlyphRun glyphRun)
        {
            SetTextFormattingModeDelegate(glyphRun, TextFormattingMode.Display);
        }
        
        private static Action<TSource, TValue> GetSetFieldDelegate<TSource, TValue>(FieldInfo? fieldInfo)
        {
            if (fieldInfo == null) throw new ArgumentNullException(nameof(fieldInfo));

            ParameterExpression targetExpression =
                Expression.Parameter(typeof(TSource), "target");
            ParameterExpression valueExpression = 
                Expression.Parameter(typeof(TValue), "value");
 
            MemberExpression fieldExpression = Expression.Field(targetExpression, fieldInfo);
            BinaryExpression assignExpression = Expression.Assign(fieldExpression, valueExpression);

            LambdaExpression lambda =
                Expression.Lambda<Action<TSource, TValue>>( assignExpression, targetExpression, valueExpression);
 
            return (Action<TSource, TValue>)lambda.Compile();
        }
    }
}