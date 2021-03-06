﻿using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using GSMA.MobileConnect.Utils;
using GSMA.MobileConnect.Cache;
using GSMA.MobileConnect.Constants;
using GSMA.MobileConnect.Exceptions;
using Newtonsoft.Json;
using System.Net.Http;

namespace GSMA.MobileConnect.Discovery
{
    /// <summary>
    /// Concrete implementation of <see cref="IDiscoveryService"/>
    /// </summary>
    public class DiscoveryService : IDiscoveryService
    {
        private readonly ICache _cache;
        private readonly RestClient _client;

        /// <inheritdoc/>
        public ICache Cache
        {
            get { return _cache; }
        }

        /// <summary>
        /// Creates a new instance of the class DiscoveryService using the specified RestClient for all HTTP requests
        /// </summary>
        /// <param name="cache">Cache implmentation to use for storage of <see cref="DiscoveryResponse"/> and <see cref="ProviderMetadata"/></param>
        /// <param name="client">RestClient for handling HTTP requests</param>
        public DiscoveryService(ICache cache, RestClient client)
        {
            this._cache = cache;
            this._client = client;

            Log.Debug(() => cache != null ? $"DiscoveryService caching enabled with type={cache.GetType().AssemblyQualifiedName}" : "DiscoveryService caching disabled");
        }

        private async Task<DiscoveryResponse> CallDiscoveryEndpoint(
            string clientId, 
            string clientSecret, 
            string discoveryUrl, 
            DiscoveryOptions options, 
            IEnumerable<BasicKeyValuePair> currentCookies, 
            bool cacheDiscoveryResponse)
        {
            Validate.RejectNullOrEmpty(clientId, "clientId");
            Validate.RejectNullOrEmpty(clientSecret, "clientSecret");
            Validate.RejectNullOrEmpty(discoveryUrl, "discoveryUrl");
            Validate.RejectNullOrEmpty(options.RedirectUrl, "redirectUrl");

            if (cacheDiscoveryResponse)
            {
                var cachedValue = await GetCachedValueAsync(options);
                if (cachedValue != null)
                {
                    return cachedValue;
                }
            }

            try
            {
                var cookies = GetCookiesToProxy(currentCookies);
                var authentication = RestAuthentication.Basic(clientId, clientSecret);
                var queryParams = GetDiscoveryQueryParams(options);

                RestResponse response;
                if (string.IsNullOrEmpty(options.MSISDN))
                {
                    response = await _client.GetDiscoveryAsync(discoveryUrl, authentication, options.XRedirect, options.ClientIP, queryParams, cookies);
                }
                else
                {
                    response = await _client.PostDiscoveryAsync(discoveryUrl, authentication, queryParams, options.ClientIP, options.XRedirect, cookies);
                }

                var discoveryResponse = new DiscoveryResponse(response);
                discoveryResponse.ProviderMetadata = await RetrieveProviderMetada(discoveryResponse.OperatorUrls?.ProviderMetadataUrl);

                if (cacheDiscoveryResponse)
                {
                    await AddCachedValueAsync(options, discoveryResponse).ConfigureAwait(false);
                }

                return discoveryResponse;
            }
            catch (Exception e) when (e is HttpRequestException || e is System.Net.WebException || e is TaskCanceledException)
            {
                Log.Error("Error occured while calling discovery endpoint", e);
                throw new MobileConnectEndpointHttpException(e.Message, e);
            }
        }

        /// <inheritdoc/>
        public async Task<DiscoveryResponse> StartAutomatedOperatorDiscoveryAsync(
            IPreferences preferences,
            string redirectUrl,
            DiscoveryOptions options,
            IEnumerable<BasicKeyValuePair> currentCookies)
        {
            Validate.RejectNull(preferences, "preferences");

            return await StartAutomatedOperatorDiscoveryAsync(
                preferences.ClientId,
                preferences.ClientSecret,
                preferences.DiscoveryUrl,
                redirectUrl,
                options,
                currentCookies);
        }

        /// <inheritdoc/>
        public async Task<DiscoveryResponse> StartAutomatedOperatorDiscoveryAsync(
            string clientId, 
            string clientSecret, 
            string discoveryUrl, 
            string redirectUrl, 
            DiscoveryOptions options, 
            IEnumerable<BasicKeyValuePair> currentCookies)
        {
            options = options ?? new DiscoveryOptions();
            options.RedirectUrl = redirectUrl;

            return await CallDiscoveryEndpoint(clientId, clientSecret, discoveryUrl, options, currentCookies, true);
        }

        /// <inheritdoc/>
        public DiscoveryResponse StartAutomatedOperatorDiscovery(IPreferences preferences, string redirectUrl, DiscoveryOptions options, IEnumerable<BasicKeyValuePair> currentCookies)
        {
            Validate.RejectNull(preferences, "preferences");

            return StartAutomatedOperatorDiscovery(preferences.ClientId, preferences.ClientSecret, preferences.DiscoveryUrl, redirectUrl, options, currentCookies);
        }

        /// <inheritdoc/>
        public DiscoveryResponse StartAutomatedOperatorDiscovery(string clientId, string clientSecret, string discoveryUrl, string redirectUrl, DiscoveryOptions options, IEnumerable<BasicKeyValuePair> currentCookies)
        {
            return StartAutomatedOperatorDiscoveryAsync(clientId, clientSecret, discoveryUrl, redirectUrl, options, currentCookies).Result;
        }

        /// <inheritdoc/>
        public async Task<DiscoveryResponse> GetOperatorSelectionURLAsync(IPreferences preferences, string redirectUrl)
        {
            Validate.RejectNull(preferences, "preferences");

            return await GetOperatorSelectionURLAsync(preferences.ClientId, preferences.ClientSecret, preferences.DiscoveryUrl, redirectUrl);
        }

        /// <inheritdoc/>
        public async Task<DiscoveryResponse> GetOperatorSelectionURLAsync(string clientId, string clientSecret, string discoveryUrl, string redirectUrl)
        {
            var options = new DiscoveryOptions { RedirectUrl = redirectUrl };
            return await CallDiscoveryEndpoint(clientId, clientSecret, discoveryUrl, options, null, false);
        }

        /// <inheritdoc/>
        public DiscoveryResponse GetOperatorSelectionURL(IPreferences preferences, string redirectUrl)
        {
            Validate.RejectNull(preferences, "preferences");

            return GetOperatorSelectionURL(preferences.ClientId, preferences.ClientSecret, preferences.DiscoveryUrl, redirectUrl);
        }

        /// <inheritdoc/>
        public DiscoveryResponse GetOperatorSelectionURL(string clientId, string clientSecret, string discoveryUrl, string redirectUrl)
        {
            return GetOperatorSelectionURLAsync(clientId, clientSecret, discoveryUrl, redirectUrl).Result;
        }

        /// <inheritdoc/>
        public ParsedDiscoveryRedirect ParseDiscoveryRedirect(Uri redirectUrl)
        {
            Validate.RejectNull(redirectUrl, "redirectUrl");

            var query = redirectUrl.Query;

            if (string.IsNullOrEmpty(query))
            {
                return new ParsedDiscoveryRedirect(null, null, null);
            }

            var mcc_mnc = HttpUtils.ExtractQueryValue(query, Parameters.MCC_MNC);
            var encryptedMSISDN = HttpUtils.ExtractQueryValue(query, Parameters.SUBSCRIBER_ID);

            string mcc = null;
            string mnc = null;
            if (mcc_mnc != null)
            {
                var parts = mcc_mnc.Split('_');
                if (parts.Length == 2)
                {
                    mcc = parts[0];
                    mnc = parts[1];
                }
            }

            return new ParsedDiscoveryRedirect(mcc, mnc, encryptedMSISDN);
        }

        /// <inheritdoc/>
        public async Task<DiscoveryResponse> CompleteSelectedOperatorDiscoveryAsync(IPreferences preferences, string redirectUrl, string selectedMCC, string selectedMNC)
        {
            Validate.RejectNull(preferences, "preferences");

            return await CompleteSelectedOperatorDiscoveryAsync(preferences.ClientId, preferences.ClientSecret, preferences.DiscoveryUrl, redirectUrl, selectedMCC, selectedMNC);
        }

        /// <inheritdoc/>
        public async Task<DiscoveryResponse> CompleteSelectedOperatorDiscoveryAsync(string clientId, string clientSecret, string discoveryUrl, string redirectUrl, string selectedMCC, string selectedMNC)
        {
            Validate.RejectNullOrEmpty(selectedMCC, "selectedMCC");
            Validate.RejectNullOrEmpty(selectedMNC, "selectedMNC");

            var discoveryOptions = new DiscoveryOptions { RedirectUrl = redirectUrl, SelectedMCC = selectedMCC, SelectedMNC = selectedMNC };
            return await CallDiscoveryEndpoint(clientId, clientSecret, discoveryUrl, discoveryOptions, null, true);
        }

        /// <inheritdoc/>
        public DiscoveryResponse CompleteSelectedOperatorDiscovery(IPreferences preferences, string redirectUrl, string selectedMCC, string selectedMNC)
        {
            Validate.RejectNull(preferences, "preferences");

            return CompleteSelectedOperatorDiscovery(preferences.ClientId, preferences.ClientSecret, preferences.DiscoveryUrl, redirectUrl, selectedMCC, selectedMNC);
        }

        /// <inheritdoc/>
        public DiscoveryResponse CompleteSelectedOperatorDiscovery(string clientId, string clientSecret, string discoveryUrl, string redirectUrl, string selectedMCC, string selectedMNC)
        {
            return CompleteSelectedOperatorDiscoveryAsync(clientId, clientSecret, discoveryUrl, redirectUrl, selectedMCC, selectedMNC).Result;
        }

        /// <inheritdoc/>
        public async Task ClearDiscoveryCacheAsync(string mcc = null, string mnc = null)
        {
            if (_cache == null)
            {
                return;
            }

            if (!string.IsNullOrEmpty(mcc) && !string.IsNullOrEmpty(mnc))
            {
                await _cache.Remove(mcc, mnc).ConfigureAwait(false);
                return;
            }
            
            if (!string.IsNullOrEmpty(mcc) && string.IsNullOrEmpty(mnc))
            {
                await _cache.Remove(mcc).ConfigureAwait(false);
                return;
            }

            await _cache.Clear().ConfigureAwait(false);
        }

        /// <inheritdoc/>
        public string ExtractOperatorSelectionURL(DiscoveryResponse result)
        {
            return result.ResponseData?.links?[0].href;
        }

        /// <inheritdoc/>
        public async Task<DiscoveryResponse> GetCachedDiscoveryResultAsync(string mcc, string mnc)
        {
            return _cache != null ? await _cache.Get(mcc, mnc) : null;
        }

        private async Task AddCachedValueAsync(DiscoveryOptions options, DiscoveryResponse response)
        {
            if (_cache != null)
            {
                var mcc = options.IdentifiedMCC != null ? options.IdentifiedMCC : options.SelectedMCC;
                var mnc = options.IdentifiedMNC != null ? options.IdentifiedMNC : options.SelectedMNC;
                var msisdn = options.MSISDN;
                var client_ip = options.LocalClientIP;

                if (response.ErrorResponse != null)
                {
                    return;
                }

                if (!string.IsNullOrEmpty(msisdn))
                    await _cache.Add(msisdn, response).ConfigureAwait(false);

                else if (!(string.IsNullOrEmpty(mcc) && string.IsNullOrEmpty(mnc)))
                    await _cache.Add(mcc, mnc, response).ConfigureAwait(false);

                else if(!string.IsNullOrEmpty(client_ip))
                    await _cache.Add(client_ip, response).ConfigureAwait(false);
            }
        }

        private async Task AddCachedValueAsync<T>(string key, T value) where T : ICacheable
        {
            if(_cache != null)
            {
                await _cache.Add(key, value).ConfigureAwait(false);
            }
        }

        private async Task<DiscoveryResponse> GetCachedValueAsync(DiscoveryOptions options)
        {
            var mcc = options.IdentifiedMCC != null ? options.IdentifiedMCC : options.SelectedMCC;
            var mnc = options.IdentifiedMNC != null ? options.IdentifiedMNC : options.SelectedMNC;
            var msisdn = options.MSISDN;
            var client_ip = options.LocalClientIP;

            if (!string.IsNullOrEmpty(msisdn))
                return _cache != null ? await _cache.Get<DiscoveryResponse>(msisdn) : null;

            else if(!(string.IsNullOrEmpty(mcc) && string.IsNullOrEmpty(mnc)))
                return _cache != null ? await _cache.Get(mcc, mnc) : null;

            else if(!string.IsNullOrEmpty(client_ip))
                return _cache != null ? await _cache.Get<DiscoveryResponse>(client_ip) : null;

            return null;
        }

        private async Task<T> GetCachedValueAsync<T>(string key, bool removeIfExpired) where T : ICacheable
        {
            if(_cache == null)
            {
                return default(T);
            }

            return await _cache.Get<T>(key, removeIfExpired);
        }

        private List<BasicKeyValuePair> GetDiscoveryQueryParams(DiscoveryOptions options)
        {
            return new List<BasicKeyValuePair>
            {
                new BasicKeyValuePair(Parameters.MSISDN, options.MSISDN?.TrimStart('+')),
                new BasicKeyValuePair(Parameters.REDIRECT_URL, options.RedirectUrl),
                new BasicKeyValuePair(Parameters.IDENTIFIED_MCC, options.IdentifiedMCC),
                new BasicKeyValuePair(Parameters.IDENTIFIED_MNC, options.IdentifiedMNC),
                new BasicKeyValuePair(Parameters.SELECTED_MCC, options.SelectedMCC),
                new BasicKeyValuePair(Parameters.SELECTED_MNC, options.SelectedMNC),
                new BasicKeyValuePair(Parameters.LOCAL_CLIENT_IP, options.LocalClientIP),
                new BasicKeyValuePair(Parameters.USING_MOBILE_DATA, options.IsUsingMobileData ? "1" : "0")
            };
        }

        private IEnumerable<BasicKeyValuePair> GetCookiesToProxy(IEnumerable<BasicKeyValuePair> cookiesToProxy)
        {
            return HttpUtils.ProxyRequiredCookies(RequiredCookies.Discovery, cookiesToProxy);
        }

        /// <inheritdoc/>
        /// <exception cref="MobileConnectInvalidArgumentException">Throws if response is null</exception>
        public async Task<ProviderMetadata> GetProviderMetadata(DiscoveryResponse response, bool forceCacheBypass)
        {
            Validate.RejectNull(response, "response");

            var metadata = await RetrieveProviderMetada(response.OperatorUrls?.ProviderMetadataUrl, forceCacheBypass);
            response.ProviderMetadata = metadata;

            return metadata;
        }
        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        /// <param name="url">url</param>
        /// <param name="forceCacheBypass"></param>
        /// <returns></returns>
        public async Task<ProviderMetadata> RetrieveProviderMetada(string url, bool forceCacheBypass = false)
        {
            if (url == null)
            {
                Log.Warning("Provider metadata was defaulted as no url found");
                return ProviderMetadata.Default;
            }

            ProviderMetadata metadata = null;

                var response = await _client.GetAsync(url, null);
                if ((int)response.StatusCode < 400)
                {
                    metadata = JsonConvert.DeserializeObject<ProviderMetadata>(response.Content);
                }

            return metadata ?? ProviderMetadata.Default;
        }
    }
}
