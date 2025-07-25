﻿using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Sber.ApiClient.Interfaces;
using Sber.ApiClient.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Net.Http.Headers;
using System.Text;

namespace Sber.ApiClient
{
    public static class PayClientExt
    {
        public static void RegiserSberClient(this IHostApplicationBuilder builder, string apiUrl, (string login, string pass) cred)
        {
            builder.Services.AddHttpClient("httpclient", (w) =>
            {
                w.BaseAddress = new Uri(apiUrl);
            });
            builder.Services.AddSingleton<IPayClient>(w => new SberApiClient(apiUrl, cred.login, cred.pass));
        }
        public static void RegiserYookassaClient(this IHostApplicationBuilder builder, string apiUrl, (string login, string pass) cred)
        {
            builder.Services.AddHttpClient("httpclient", (w) =>
            {
                w.BaseAddress = new Uri(apiUrl);
                var key = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{cred.login}:{cred.pass}"));
                w.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", key);
            });
            builder.Services.AddSingleton<IPayClient, YookassaClient>();
        }
        public static string GetOrderId(this OrderStatus status)
        {
            return status.Attributes.Single(e => e.Name == "mdOrder").Value;
        }
        public static bool IsOrderPaid(this OrderStatus status)
        {
            //0 - заказ зарегистрирован, но не оплачен;
            //1 - предавторизованная сумма удержана(для двухстадийных платежей);
            //2 - проведена полная авторизация суммы заказа;
            //3 - авторизация отменена;
            //4 - по транзакции была проведена операция возврата;
            //5 - инициирована авторизация через сервер контроля доступа банка-эмитента;
            //6 - авторизация отклонена.
            return status.OrderPayStatus == 2;
        }
    }
    internal static class SberClientExt
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
