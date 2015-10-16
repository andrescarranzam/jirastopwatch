﻿/**************************************************************************
Copyright 2015 Carsten Gehling

Licensed under the Apache License, Version 2.0 (the "License");
you may not use this file except in compliance with the License.
You may obtain a copy of the License at

    http://www.apache.org/licenses/LICENSE-2.0

Unless required by applicable law or agreed to in writing, software
distributed under the License is distributed on an "AS IS" BASIS,
WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
See the License for the specific language governing permissions and
limitations under the License.
**************************************************************************/
using RestSharp;
using System;
using System.Linq;
using System.Net;

namespace StopWatch
{
    internal class JiraClient
    {
        public string BaseUrl { get; set; }

        #region public methods
        public JiraClient()
        {
            this.cookieContainer = new CookieContainer();
        }


        public bool Authenticate(string username, string password)
        {
            this.username = username;
            this.password = password;

            return ReAuthenticate();
        }


        public void OpenIssueInBrowser(string key)
        {
            string url = this.BaseUrl;
            if (url.Last() != '/')
                url += "/";
            url += "browse/";
            url += key;
            System.Diagnostics.Process.Start(url);
        }


        public string GetIssueSummary(string key)
        {
            var client = GetClient();

            var request = new RestRequest(String.Format("/rest/api/2/issue/{0}", key), Method.GET);

            IRestResponse<Issue> response;

            response = client.Execute<Issue>(request);

            // If login session has expired, try to login, and then re-execute the original request
            if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized) {
              if (!ReAuthenticate())
                return "";

              response = client.Execute<Issue>(request);
            }

            if (response.StatusCode != System.Net.HttpStatusCode.OK)
                return "";

            return response.Data.Fields.Summary;
        }


        public bool PostWorklog(string key, TimeSpan time, string comment)
        {
            var client = GetClient();

            var request = new RestRequest(String.Format("/rest/api/2/issue/{0}/worklog", key), Method.POST);
            request.RequestFormat = DataFormat.Json;

            request.AddBody(new
                {
                    timeSpent = JiraHelpers.TimeSpanToJiraTime(time),
                    comment = comment
                }
            );

            IRestResponse response;

            response = client.Execute(request);

            // If login session has expired, try to login, and then re-execute the original request
            if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized) {
              if (!ReAuthenticate())
                return false;

              response = client.Execute(request);
            }

            if (response.StatusCode != System.Net.HttpStatusCode.Created)
                return false;

            return true;
        }
        #endregion


        #region private methods
        private bool ReAuthenticate()
        {
            var client = GetClient();

            var request = new RestRequest("/rest/auth/1/session", Method.POST);
            request.RequestFormat = DataFormat.Json;
            request.AddBody(new {
                username = this.username,
                password = this.password
            });

            IRestResponse response = client.Execute<Issue>(request);
            if (response.StatusCode != System.Net.HttpStatusCode.OK)
                return false;

            return true;
        }


        private RestClient GetClient()
        {
            RestClient client = new RestClient(BaseUrl);
            client.CookieContainer = this.cookieContainer;
            return client;
        }
        #endregion


        #region private members
        private CookieContainer cookieContainer;

        private string username;
        private string password;
        #endregion
    }
}
