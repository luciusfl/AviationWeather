//  --------------------------------------------------------------------------------------------------------------------
//  <copyright file="ExtensionMethods.cs" company="Microsoft">
//    Copyright (c) Microsoft Corporation.  All rights reserved.
//  </copyright>
//  --------------------------------------------------------------------------------------------------------------------

namespace AirportInformation.Common
{
    using System;
    using System.Diagnostics;
    using System.IO;
    using System.Net.Http;
    using System.Threading.Tasks;

    public static class ExtensionMethods
    {
        private const int MaxRetries = 2;

        private static HttpClient CreateHttpClient()
        {
            var client = new HttpClient();
            client.Timeout = TimeSpan.FromSeconds(15);
            return client;
        }

        public static async Task<Stream> HttpGetAsync(this Uri url)
        {
            var timer = Stopwatch.StartNew();
            int retries = 0;
            while (true)
            {
                try
                {
                    var client = CreateHttpClient();
                    var response = await client.GetStreamAsync(url);
                    timer.Stop();
                    Debug.WriteLine("{0}ms for {1}", timer.ElapsedMilliseconds, url);
                    return response;
                }
                catch (Exception ex)
                {
                    retries++;
                    if (retries == MaxRetries)
                    {

                        Debug.WriteLine("Failed to query {0}. {1}", url, ex.Message);
                        return default(Stream);
                    }

                    Debug.WriteLine("Http GET of {0} failed with: {1}. Retrying.", url, ex.Message);
                }
            }
        }

        public static async Task<string> HttpGetStringAsync(this Uri url)
        {
            var timer = Stopwatch.StartNew();
            int retries = 0;
            while (true)
            {
                try
                {
                    var client = CreateHttpClient();
                    var response = await client.GetStringAsync(url);
                    timer.Stop();
                    Debug.WriteLine("{0}ms for {1}", timer.ElapsedMilliseconds, url);
                    return response;
                }
                catch (Exception ex)
                {
                    retries++;
                    if (retries == MaxRetries)
                    {
                        Debug.WriteLine("Failed to query {0}. {1}", url, ex);
                        return string.Empty;
                    }
                    else
                    {
                        continue;
                    }
                }
            }
        }
    }
}
