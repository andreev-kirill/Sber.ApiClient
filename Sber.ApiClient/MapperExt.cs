﻿using Sber.ApiClient.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;

namespace Sber.ApiClient
{
    internal static class MapperExt
    {
        public static T ToDto<T>(this object source, Action<T> afterMap)
        {
            var result = Create<T>();
            var resultProperties = typeof(T).GetProperties().ToDictionary(e => e.Name);
            foreach (var item in source.GetType().GetProperties())
            {
                if (resultProperties.TryGetValue(item.Name, out var value))
                {
                    value.SetValue(result, item.GetValue(source));                    
                }
            }
            afterMap(result);
            return result;
        }
        private static T Create<T>()
        {
            var typeCode = Type.GetTypeCode(typeof(T));
            if (typeof(T).IsPrimitive
                || typeCode == TypeCode.String
                || typeCode == TypeCode.Decimal
                || typeCode == TypeCode.DateTime)
                return default(T);
            NewExpression constructorExpression = Expression.New(typeof(T));
            Expression<Func<T>> lambdaExpression = Expression.Lambda<Func<T>>(constructorExpression);
            return lambdaExpression.Compile()();
        }
        public static IEnumerable<KeyValuePair<string, string>> ToKeyValuePair<T>(this T source, 
            params KeyValuePair<string, string>[] addItems)
        {
            return source.GetType().GetProperties().Select(e => {
                var toNameAttr = e.GetCustomAttributes(typeof(ValueAttribute), false).Cast<ValueAttribute>().FirstOrDefault();
                
                var value = e.GetValue(source);
                return new KeyValuePair<string, string>(toNameAttr == null ? e.Name : toNameAttr.Value, value == null ? null : value.ToString());
            }).Union(addItems);
        }
        public static string GetOrderId(this OrderStatus status)
        {
            return status.Attributes.Single(e => e.Name == "mdOrder").Value;
        }
    }
    public class ValueAttribute: Attribute
    {
        public ValueAttribute(string name)
        {
            this.Value = name;
        }
        public string Value { get; }
    }
}
