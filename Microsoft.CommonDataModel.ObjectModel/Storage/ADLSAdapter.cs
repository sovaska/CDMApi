//-----------------------------------------------------------------------
// <copyright file="ADLSAdapter.cs" company="Microsoft">
//      All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

namespace Microsoft.CommonDataModel.ObjectModel.Storage
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Net.Http;
    using System.Net.Http.Headers;
    using System.Text;
    using System.Threading.Tasks;
    using System.Net;

    using Microsoft.CommonDataModel.ObjectModel.Utilities;
    using Microsoft.CommonDataModel.ObjectModel.Utilities.Network;
    using Microsoft.IdentityModel.Clients.ActiveDirectory;

    using Newtonsoft.Json;
    using Newtonsoft.Json.Serialization;
    using System.Security.Cryptography;
    using System.Web;
    using Newtonsoft.Json.Linq;
    using Microsoft.CommonDataModel.ObjectModel.Utilities.Logging;

    public class ADLSAdapter : NetworkAdapter, StorageAdapter
    {
        private AuthenticationContext Context;

        /// <summary>
        /// The root.
        /// </summary>
        public string Root { get; private set; }

        /// <summary>
        /// The hostname of ADLS.
        /// </summary>
        public string Hostname { get; private set; }

        /// <summary>
        /// The tenant.
        /// </summary>
        public string Tenant { get; private set; }

        /// <summary>
        /// The client ID of an application accessing ADLS.
        /// </summary>
        public string ClientId { get; private set; }

        /// <summary>
        /// The secret for the app accessing ADLS.
        /// </summary>
        public string Secret { get; private set; }

        /// <summary>
        /// The account/shared key.
        /// </summary>
        public string SharedKey { get; private set; }

        /// <inheritdoc />
        public string LocationHint { get; set; }

        /// <summary>
        /// The predefined ADLS resource.
        /// </summary>
        private const string Resource = "https://storage.azure.com";

        /// <summary>
        /// The authorization header key, used during shared key auth.
        /// </summary>
        private const string HttpAuthorization = "Authorization";

        /// <summary>
        /// The MS date header key, used during shared key auth.
        /// </summary>
        private const string HttpXmsDate = "x-ms-date";

        /// <summary>
        /// The MS version key, used during shared key auth.
        /// </summary>
        private const string HttpXmsVersion = "x-ms-version";

        internal const string Type = "adls";

        /// <summary>
        /// The ADLS constructor for clientId/secret authentication.
        /// </summary>
        public ADLSAdapter(string hostname, string root, string tenant, string clientId, string secret)
        {
            this.Root = root;
            this.Hostname = hostname;
            this.Tenant = tenant;
            this.ClientId = clientId;
            this.Secret = secret;
            this.Context = new AuthenticationContext("https://login.windows.net/" + this.Tenant);

            this.httpClient = new CdmHttpClient();
        }

        /// <summary>
        /// The default constructor, a user has to apply JSON config after creating it this way.
        /// </summary>
        /// <param name="configs">The configs</param>
        public ADLSAdapter()
        {
            this.httpClient = new CdmHttpClient();
        }


        /// <summary>
        /// The ADLS constructor for shared key authentication.
        /// </summary>
        public ADLSAdapter(string hostname, string root, string sharedKey)
        {
            this.Hostname = hostname;
            this.Root = root;
            this.SharedKey = sharedKey;

            this.httpClient = new CdmHttpClient();
        }

        /// <inheritdoc />
        public bool CanRead()
        {
            return true;
        }

        /// <inheritdoc />
        public async Task<string> ReadAsync(string corpusPath)
        {
            String url = this.CreateAdapterPath(corpusPath);
            var request = await this.BuildRequest(url, HttpMethod.Get);

            var cdmResponse = await base.ExecuteRequest(request);

            return await cdmResponse.Content.ReadAsStringAsync();
        }

        /// <inheritdoc />
        public bool CanWrite()
        {
            return true;
        }

        /// <inheritdoc />
        public async Task WriteAsync(string corpusPath, string data)
        {
            if (ensurePath($"{this.Root}{corpusPath}") == false)
            {
                throw new Exception($"Could not create folder for document '{corpusPath}'");
            }

            string url = this.CreateAdapterPath(corpusPath);

            var request = await this.BuildRequest($"{url}?resource=file", HttpMethod.Put);
            await this.ExecuteRequest(request);

            request = await this.BuildRequest($"{url}?action=append&position=0", new HttpMethod("PATCH"), data, "application/json");
            await this.ExecuteRequest(request);

            var stringContent = new StringContent(request.Content, Encoding.UTF8, request.ContentType);

            // Building a request and setting a URL with a position argument to be the length of the byte array of the string content (or length of UTF-8 string content).
            request = await this.BuildRequest($"{url}?action=flush&position={(await stringContent.ReadAsByteArrayAsync()).Length}" , new HttpMethod("PATCH"));
            await this.ExecuteRequest(request);
        }

        /// <inheritdoc />
        public string CreateAdapterPath(string corpusPath)
        {
            return $"https://{this.Hostname}:443{this.Root}{corpusPath}";
        }

        /// <inheritdoc />
        public string CreateCorpusPath(string adapterPath)
        {
            var prefix = $"https://{this.Hostname}:443{this.Root}";
            if (adapterPath.StartsWith(prefix))
            {
                return adapterPath.Slice(prefix.Length);
            }

            return null;
        }

        /// <inheritdoc />
        public void ClearCache()
        {
            return;
        }

        /// <inheritdoc />
        public async Task<DateTimeOffset?> ComputeLastModifiedTimeAsync(string corpusPath)
        {
            // TODO
            return null;
        }
        
        /// <inheritdoc />
        public async Task<List<string>> FetchAllFilesAsync(string currFullPath)
        {
            // TODO
            return null;
        }

        /// <summary>
        /// Returns the headers with the applied shared key.
        /// </summary>
        /// <param name="sharedKey">The account/shared key.</param>
        /// <param name="url">The URL.</param>
        /// <param name="method">The HTTP method.</param>
        /// <param name="content">The string content.</param>
        /// <param name="contentType">The content type.</param>
        /// <returns></returns>
        private Dictionary<string, string> ApplySharedKey(string sharedKey, string url, HttpMethod method, string content = null, string contentType = null)
        {
            Dictionary<string, string> headers = new Dictionary<string, string>();

            // Add UTC now time and new version.
            headers.Add(HttpXmsDate, DateTime.UtcNow.ToString("R", CultureInfo.InvariantCulture));
            headers.Add(HttpXmsVersion, "2018-06-17");

            int contentLength = 0;

            if (content != null)
            {
                contentLength = (new StringContent(content, Encoding.UTF8, contentType)).ReadAsByteArrayAsync().Result.Length;
            }

            Uri uri = new Uri(url);
            StringBuilder builder = new StringBuilder();
            builder.AppendFormat(CultureInfo.InvariantCulture, "{0}\n", method.Method); // Verb.
            builder.Append("\n"); // Content-Encoding.
            builder.Append("\n"); // Content-Language.
            builder.Append((contentLength != 0) ? $"{contentLength}\n" : "\n"); // Content length.
            builder.Append("\n"); // Content-md5.
            builder.Append(contentType != null ? $"{contentType}; charset=utf-8\n" : "\n"); // Content-type.
            builder.Append("\n"); // Date.
            builder.Append("\n"); // If-modified-since.
            builder.Append("\n"); // If-match.
            builder.Append("\n"); // If-none-match.
            builder.Append("\n"); // If-unmodified-since.
            builder.Append("\n"); // Range.

            foreach (var header in headers)
            {
                builder.AppendFormat(CultureInfo.InvariantCulture, "{0}:{1}\n", header.Key, header.Value);
            }

            // Append canonicalized resource.
            string accountName = uri.Host.Split('.')[0];
            builder.Append("/");
            builder.Append(accountName);
            builder.Append(uri.AbsolutePath);

            // Append canonicalized queries.
            if (!string.IsNullOrEmpty(uri.Query))
            {
                string[] queryParameters = uri.Query.TrimStart('?').Split('&');

                foreach (var parameter in queryParameters)
                {
                    string[] keyValuePair = parameter.Split('=');
                    builder.Append($"\n{keyValuePair[0]}:{keyValuePair[1]}");
                }
            }
           
            // Hash the payload.
            byte[] dataToHash = System.Text.Encoding.UTF8.GetBytes(builder.ToString().TrimEnd('\n'));
            if (!TryFromBase64String(sharedKey, out byte[] bytes))
            {
                throw new Exception("Couldn't encode the shared key.");
            }

            using (HMACSHA256 hmac = new HMACSHA256(bytes))
            {
                hmac.Key = bytes;
                string signedString = $"SharedKey {accountName}:{System.Convert.ToBase64String(hmac.ComputeHash(dataToHash))}";

                headers.Add(HttpAuthorization, signedString);
            }

            return headers;
        }

        /// <summary>
        /// Encodes from base 64 string to the byte array.
        /// </summary>
        /// <param name="content">The content.</param>
        /// <param name="bytes">The encoded bytes.</param>
        /// <returns></returns>
        private bool TryFromBase64String(string content, out byte[] bytes)
        {
            bytes = new byte[0];
            try
            {
                bytes = Convert.FromBase64String(content);
                return true;
            }
            catch (FormatException)
            {
                return false;
            }
        }

        private Task<AuthenticationResult> GenerateBearerToken()
        {
            var clientCredentials = new ClientCredential(this.ClientId, this.Secret);
            return this.Context.AcquireTokenAsync(Resource, clientCredentials);
        }

        /// <summary>
        /// Generates the required request to work with Azure Storage API.
        /// </summary>
        /// <param name="url">The URL of a resource.</param>
        /// <param name="method">The method.</param>
        /// <param name="content">The string content.</param>
        /// <param name="contentType">The content type.</param>
        /// <returns>The constructed Cdm Http request.</returns>
        private async Task<CdmHttpRequest> BuildRequest(String url, HttpMethod method, string content = null, string contentType = null)
        {
            CdmHttpRequest request;

            // Check whether we support shared key or clientId/secret auth.
            if (this.SharedKey != null)
            {
                request = this.SetUpCdmRequest(url, ApplySharedKey(this.SharedKey, url, method, content, contentType), method);
            }
            else
            {
                var token = await this.GenerateBearerToken();

                request = this.SetUpCdmRequest(url, new Dictionary<string, string> { { "authorization", $"{token.AccessTokenType} {token.AccessToken}" } }, method);
            }

            if (content != null)
            {
                request.Content = content;
                request.ContentType = contentType;
            }
            
            return request;
        }

        private bool ensurePath(string pathFor)
        {
            if (pathFor.LastIndexOf("/") == -1)
                return false;

            // Folders are only of virtual kind in Azure Storage
            return true;
        }

        /// <inheritdoc />
        public string FetchConfig()
        {
            var resultConfig = new JObject
            {
                { "type", Type }
            };

            var configObject = new JObject
            {
                new JProperty("hostname", this.Hostname),
                new JProperty("root", this.Root)
            };

            // Check for clientId auth, we won't write shared key or secrets to JSON.
            if (this.ClientId != null && this.Tenant != null)
            {
                configObject.Add(new JProperty("tenant", this.Tenant));
                configObject.Add(new JProperty("clientId", this.ClientId));
            }

            // Try constructing network configs. 
            configObject.Add(this.FetchNetworkConfig());

            if (this.LocationHint != null)
            {
                configObject.Add("locationHint", this.LocationHint);
            }

            resultConfig.Add("config", configObject);

            return resultConfig.ToString();
        }

        /// <inheritdoc />
        public void UpdateConfig(string config)
        {
            if (config == null)
            {
                throw new Exception("ADLS adapter needs a config.");
            }

            this.UpdateNetworkConfig(config);

            var configJson = JsonConvert.DeserializeObject<JObject>(config);

            if (configJson["root"] != null)
            {
                this.Root = configJson["root"].ToString();
            }
            else
            {
                throw new Exception("Root has to be set for ADLS adapter.");
            }

            if (configJson["hostname"] != null)
            {
                this.Hostname = configJson["hostname"].ToString();
            }
            else
            {
                throw new Exception("Hostname has to be set for ADLS adapter.");
            }

            // Check first for clientId/secret auth.
            if (configJson["tenant"] != null && configJson["clientId"] != null)
            {
                this.Tenant = configJson["tenant"].ToString();
                this.ClientId = configJson["clientId"].ToString();

                // Check for a secret, we don't really care is it there, but it is nice if it is.
                if (configJson["secret"] != null)
                {
                    this.Secret = configJson["secret"].ToString();
                }
            }

            // Check then for shared key auth.
            if (configJson["sharedKey"] != null)
            {
                this.SharedKey = configJson["sharedKey"].ToString();
            }

            if (configJson["locationHint"] != null)
            {
                this.LocationHint = configJson["locationHint"].ToString();
            }

            if (this.Tenant != null)
            {
                this.Context = new AuthenticationContext("https://login.windows.net/" + this.Tenant);
            }
        }
    }
}
