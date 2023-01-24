using SSTournamentsBot.Api.Resources;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq.Expressions;

namespace SSTournamentsBot.Api.Domain
{
    public class Text: IText
    {
        public string Value { get; }
        public string Key { get; }
        public object Arg { get; set; }
        public object Arg2 { get; set; }
        public object Arg3 { get; set; }
        public object[] Args { get; set; }

        public Text(string key = null, string value = null)
        {
            Key = key;
            Value = value;
        }

        public static Text OfKey(string key)
        {
            return new Text(key);
        }

        public static Text OfValue(string value)
        {
            return new Text(value: value);
        }

        public Text Format(object arg)
        {
            Arg = arg;
            return this;
        }

        public Text Format(object arg, object arg2)
        {
            Arg = arg;
            Arg2 = arg2;
            return this;
        }

        public Text Format(object arg, object arg2, object arg3)
        {
            Arg = arg;
            Arg2 = arg2;
            Arg3 = arg3;
            return this;
        }

        public Text Format(params object[] args)
        {
            Args = args;
            return this;
        }

        public string Build(CultureInfo culture = null)
        {
            if (Value != null)
                return Value;

            var value = culture == null ? S.ResourceManager.GetString(Key) : S.ResourceManager.GetString(Key, culture);

            if (Arg != null)
            {
                if (Arg2 != null)
                {
                    if (Arg3 != null)
                        return value.FormatWith(Arg, Arg2, Arg3);
                    return value.FormatWith(Arg, Arg2);
                }

                return value.FormatWith(Arg);
            }

            if (Args != null)
                return value.FormatWith(Args);

            return value;
        }

        public static implicit operator Text(string value)
        {
            return OfValue(value);
        }

        public static implicit operator string(Text text)
        {
            return text.Build();
        }

        public static implicit operator Text(Expression<Func<string>> memberSelector)
        {
            return OfKey(GetMemberName(memberSelector));
        }

        private static string GetMemberName(Expression<Func<string>> memberSelector)
        {
            var currentExpression = memberSelector.Body;

            while (true)
            {
                switch (currentExpression.NodeType)
                {
                    case ExpressionType.Parameter:
                        return ((ParameterExpression)currentExpression).Name;
                    case ExpressionType.MemberAccess:
                        return ((MemberExpression)currentExpression).Member.Name;
                    case ExpressionType.Call:
                        return ((MethodCallExpression)currentExpression).Method.Name;
                    case ExpressionType.Convert:
                    case ExpressionType.ConvertChecked:
                        currentExpression = ((UnaryExpression)currentExpression).Operand;
                        break;
                    case ExpressionType.Invoke:
                        currentExpression = ((InvocationExpression)currentExpression).Expression;
                        break;
                    case ExpressionType.ArrayLength:
                        return "Length";
                    default:
                        throw new Exception("not a proper member selector");
                }
            }
        }

        public static Text OfLambda(Expression<Func<string>> p)
        {
            return p;
        }


        public override string ToString()
        {
            return Build();
        }
    }
}
