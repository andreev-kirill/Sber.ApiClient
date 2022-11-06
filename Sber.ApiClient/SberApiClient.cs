﻿using Sber.ApiClient.Models;
using System;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;
using System.Web;
using System.Collections.Generic;

namespace Sber.ApiClient
{
    public interface ISberApiClient
    {
        Task<Order> RegisterPay(PayRequest request);
        Task<OrderStatus> GetStatus(OrderStatusRequest request);
        Task<bool> IsOrderPaid(string orderNumber);
        Task<ResponseCode> Refund(RefundRequest request);
        Task<ResponseCode> Refund(string orderNumber, long amount);
        Task<ResponseCode> Reverse(ReverseRequest request);
        Task<ResponseCode> Reverse(string orderNumber, long amount);
    }
    public class SberApiClient : ISberApiClient
    {
        private readonly string login;
        private readonly string pass;
        private readonly HttpClient httpClient;
        public SberApiClient(string baseUrl, string login, string pass)
        {
            this.login = login;
            this.pass = pass;
            httpClient = new HttpClient() { BaseAddress = new Uri(baseUrl), Timeout = TimeSpan.FromSeconds(10) };
        }
        public async Task<Order> RegisterPay(PayRequest request)
        {
            var parameters = request.ToKeyValuePair(
                new[]{new KeyValuePair<string, string>("userName", login),
                    new KeyValuePair<string, string>("password", pass)});
            var responce = await httpClient.SendAsync(new HttpRequestMessage(HttpMethod.Post, @$"/payment/rest/register.do")
            {
                Content = new FormUrlEncodedContent(parameters)
            });
            responce.EnsureSuccessStatusCode();
            var stringRequest = await responce.Content.ReadAsStringAsync();
            var result = JsonConvert.DeserializeObject<Order>(stringRequest);
            return result;
        }
        public async Task<OrderStatus> GetStatus(OrderStatusRequest request)
        {
            var parameters = request.ToKeyValuePair(
                new[]{new KeyValuePair<string, string>("userName", login),
                    new KeyValuePair<string, string>("password", pass)});
            var responce = await httpClient.SendAsync(new HttpRequestMessage(HttpMethod.Post, @$"/payment/rest/getOrderStatusExtended.do")
            {
                Content = new FormUrlEncodedContent(parameters)
            });
            responce.EnsureSuccessStatusCode();
            var stringRequest = await responce.Content.ReadAsStringAsync();
            var result = JsonConvert.DeserializeObject<OrderStatus>(stringRequest);
            return result;
        }
        public async Task<ResponseCode> Refund(RefundRequest request)
        {
            var parameters = request.ToKeyValuePair(
                new KeyValuePair<string, string>("userName", login),
                    new KeyValuePair<string, string>("password", pass));
            var responce = await httpClient.SendAsync(new HttpRequestMessage(HttpMethod.Post, @$"/payment/rest/refund.do")
            {
                Content = new FormUrlEncodedContent(parameters)
            });
            responce.EnsureSuccessStatusCode();
            var stringRequest = await responce.Content.ReadAsStringAsync();
            var result = JsonConvert.DeserializeObject<ResponseCode>(stringRequest);
            return result;
        }
        public async Task<ResponseCode> Refund(string orderNumber, long amount)
        {
            var status = await GetStatus(new OrderStatusRequest() { OrderNumber = orderNumber });
            return await Refund(new RefundRequest() { OrderId = status.GetOrderId(), Amount = amount });
        }

        public async Task<ResponseCode> Reverse(string orderNumber, long amount)
        {
            var status = await GetStatus(new OrderStatusRequest() { OrderNumber = orderNumber });
            return await Reverse(new ReverseRequest() { OrderId = status.GetOrderId(), Amount=amount });
        }

        public async Task<ResponseCode> Reverse(ReverseRequest request)
        {
            var parameters = request.ToKeyValuePair(
                new KeyValuePair<string, string>("userName", login),
                    new KeyValuePair<string, string>("password", pass));
            var responce = await httpClient.SendAsync(new HttpRequestMessage(HttpMethod.Post, @$"/payment/rest/reverse.do")
            {
                Content = new FormUrlEncodedContent(parameters)
            });
            responce.EnsureSuccessStatusCode();
            var stringRequest = await responce.Content.ReadAsStringAsync();
            var result = JsonConvert.DeserializeObject<ResponseCode>(stringRequest);
            return result;
        }


        public async Task<bool> IsOrderPaid(string orderNumber)
        {
            var status = await GetStatus(new OrderStatusRequest() { OrderNumber = orderNumber });
            return status.IsOrderPaid();
        }
    }
}