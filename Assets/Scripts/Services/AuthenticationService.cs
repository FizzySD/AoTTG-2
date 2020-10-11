﻿using Assets.Scripts.Security;
using Newtonsoft.Json;
using Photon;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Assets.Scripts.Services
{
    public class AuthenticationService : PunBehaviour
    {
        public OAuth OAuth;

        public string AccessToken { get; set; }

        private const string CodeChallengeMethod = "S256";

        private async void Start()
        {
            var authorizationResult = await GetAuthorizationCode();
            if (authorizationResult == null)
            {
                Debug.LogError("Could not receive Authorization Code.");
            }

            var accessToken = await GetAccessToken(authorizationResult);
            if (accessToken == null) return;

            AccessToken = accessToken;
            Debug.Log(AccessToken);
        }

        private async Task<AuthorizationResult> GetAuthorizationCode()
        {
            var state = RandomDataBase64Url(32);
            var codeVerifier = RandomDataBase64Url(32);
            var codeChallenge = Base64UrlEncodeNoPadding(Sha256(codeVerifier));

            var redirectUri = $"http://{IPAddress.Loopback}:{51772}/";

            var http = new HttpListener();
            http.Prefixes.Add(redirectUri);
            http.Start();

            var authorizationRequest =
                $"{OAuth.authorizationEndpoint}?response_type=code" +
                $"&scope=openid%20profile" +
                $"&redirect_uri={Uri.EscapeDataString(redirectUri)}" +
                $"&client_id={OAuth.clientID}" +
                $"&state={state}" +
                $"&code_challenge={codeChallenge}" +
                $"&code_challenge_method={CodeChallengeMethod}";

            //TODO: Investigate how to use an in-game browser
            System.Diagnostics.Process.Start(authorizationRequest);

            var context = await http.GetContextAsync();
            // Sends an HTTP response to the browser.
            var response = context.Response;
            string responseString = string.Format("<html><head></head><body>Please return to AoTTG2.</body></html>");
            var buffer = Encoding.UTF8.GetBytes(responseString);
            response.ContentLength64 = buffer.Length;
            var responseOutput = response.OutputStream;
            Task responseTask = responseOutput.WriteAsync(buffer, 0, buffer.Length).ContinueWith((task) =>
            {
                responseOutput.Close();
                http.Stop();
                Console.WriteLine("HTTP server stopped.");
            });

            // Checks for errors.
            if (context.Request.QueryString.Get("error") != null)
            {
                Debug.LogWarning($"OAuth authorization error: {context.Request.QueryString.Get("error")}.");
                return null;
            }
            if (context.Request.QueryString.Get("code") == null
                || context.Request.QueryString.Get("state") == null)
            {
                Debug.LogWarning("Malformed authorization response. " + context.Request.QueryString);
                return null;
            }

            // extracts the code
            var code = context.Request.QueryString.Get("code");
            var incoming_state = context.Request.QueryString.Get("state");
            // Compares the receieved state to the expected value, to ensure that
            // this app made the request which resulted in authorization.
            if (incoming_state != state)
            {
                Debug.LogWarning(String.Format("Received request with invalid state ({0})", incoming_state));
                return null;
            }

            Debug.Log("Authorization code : " + code);

            return new AuthorizationResult
            {
                AuthorizationCode = code,
                CodeVerifier = codeVerifier,
                RedirectUrl = redirectUri
            };
        }

        public async Task<string> GetAccessToken(AuthorizationResult authorizationResult)
        {
            // builds the  request
            var tokenRequestBody =
                $"code={authorizationResult.AuthorizationCode}" +
                $"&redirect_uri={Uri.EscapeDataString(authorizationResult.RedirectUrl)}" +
                $"&client_id={OAuth.clientID}" +
                $"&code_verifier={authorizationResult.CodeVerifier}" +
                $"&client_secret={OAuth.clientSecret}" +
                $"&scope=" +
                $"&grant_type=authorization_code";

            // sends the request
            HttpWebRequest tokenRequest = (HttpWebRequest) WebRequest.Create(OAuth.tokenEndpoint);
            tokenRequest.Method = "POST";
            tokenRequest.ContentType = "application/x-www-form-urlencoded";
            //tokenRequest.Accept = "Accept=application/json;charset=UTF-8";
            byte[] _byteVersion = Encoding.ASCII.GetBytes(tokenRequestBody);
            tokenRequest.ContentLength = _byteVersion.Length;
            Stream stream = tokenRequest.GetRequestStream();
            await stream.WriteAsync(_byteVersion, 0, _byteVersion.Length);
            stream.Close();
            try
            {
                // gets the response
                WebResponse tokenResponse = await tokenRequest.GetResponseAsync();
                using (StreamReader reader = new StreamReader(tokenResponse.GetResponseStream()))
                {
                    // reads response body
                    string responseText = await reader.ReadToEndAsync();
                    Console.WriteLine(responseText);
                    // converts to dictionary
                    Dictionary<string, string> tokenEndpointDecoded = JsonConvert.DeserializeObject<Dictionary<string, string>>(responseText);
                    var accessToken = tokenEndpointDecoded["access_token"];
                    return accessToken;
                }
            }
            catch (WebException ex)
            {
                if (ex.Status != WebExceptionStatus.ProtocolError) return null;
                if (!(ex.Response is HttpWebResponse response)) return null;

                Debug.LogWarning("HTTP: " + response.StatusCode);
                using (StreamReader reader = new StreamReader(response.GetResponseStream()))
                {
                    // reads response body
                    string responseText = await reader.ReadToEndAsync();
                    Debug.LogWarning(responseText);
                }
            }

            return null;
        }


        /// <summary>
        /// Returns URI-safe data with a given input length.
        /// </summary>
        /// <param name="length">Input length (nb. output will be longer)</param>
        /// <returns></returns>
        private static string RandomDataBase64Url(uint length)
        {
            var rng = new RNGCryptoServiceProvider();
            var bytes = new byte[length];
            rng.GetBytes(bytes);
            return Base64UrlEncodeNoPadding(bytes);
        }

        /// <summary>
        /// Base64url no-padding encodes the given input buffer.
        /// </summary>
        /// <param name="buffer"></param>
        /// <returns></returns>
        private static string Base64UrlEncodeNoPadding(byte[] buffer)
        {
            var base64 = Convert.ToBase64String(buffer);

            // Converts base64 to base64url.
            base64 = base64.Replace("+", "-");
            base64 = base64.Replace("/", "_");
            // Strips padding.
            base64 = base64.Replace("=", "");

            return base64;
        }

        /// <summary>
        /// Returns the SHA256 hash of the input string.
        /// </summary>
        /// <param name="inputString"></param>
        /// <returns></returns>
        private static byte[] Sha256(string inputString)
        {
            byte[] bytes = Encoding.ASCII.GetBytes(inputString);
            SHA256Managed sha256 = new SHA256Managed();
            return sha256.ComputeHash(bytes);
        }

        //async void userinfoCall(string access_token)
        //{
        //    output("Making API Call to Userinfo...");

        //    // sends the request
        //    HttpWebRequest userinfoRequest = (HttpWebRequest) WebRequest.Create(userInfoEndpoint);
        //    userinfoRequest.Method = "GET";
        //    userinfoRequest.Headers.Add(string.Format("Authorization: Bearer {0}", access_token));
        //    userinfoRequest.ContentType = "application/x-www-form-urlencoded";
        //    //userinfoRequest.Accept = "Accept=text/html,application/xhtml+xml,application/xml;q=0.9,*/*;q=0.8";

        //    // gets the response
        //    WebResponse userinfoResponse = await userinfoRequest.GetResponseAsync();
        //    using (StreamReader userinfoResponseReader = new StreamReader(userinfoResponse.GetResponseStream()))
        //    {
        //        // reads response body
        //        string userinfoResponseText = await userinfoResponseReader.ReadToEndAsync();
        //        output(userinfoResponseText);
        //    }
        //}
    }
}
