using Newtonsoft.Json;
using RestSharp;
using System;
using System.Collections.Generic;

namespace MattermostDriver
{
	internal static class API
	{
		internal static string ApiBase;
		internal static RestClient client;
		internal static string token;

		/// <summary>
		/// Initializes RestClient with api base url.
		/// </summary>
		internal static void Initialize()
		{
			client = new RestClient(ApiBase);
		}

		internal static Self PostGetAuth(object jsonbody)
		{
			RestRequest request = new RestRequest("/users/login", Method.POST);
			request.AddHeader("Content-Type", "application/json");
			request.AddJsonBody(jsonbody);
			var result = client.Execute(request);

			Client.logger.Debug($"Executed API.Post at endpoint '/users/login'.");

			try
			{
				AppError error = JsonConvert.DeserializeObject<AppError>(result.Content);
				if (error.id != null && error.message != null)
				{
					Client.logger.Error("Error received from API: " + error.ToString());
					return null;
					//throw new Exception("Error received from API Authorization request: " + error.ToString());
				}
			}
			catch { }

			token = result.Headers[1].Value.ToString();
			return JsonConvert.DeserializeObject<Self>(result.Content);
		}

		internal static T Post<T>(string endpoint, object jsonbody)
		{
			//Make sure client is logged in.
			if (string.IsNullOrWhiteSpace(token))
			{
				Client.logger.Error($"API.Post called at endpoint '{endpoint}', but not logged in.");
				return default(T);
			}

			RestRequest request = new RestRequest(endpoint, Method.POST);
			request.AddHeader("Content-Type", "application/json");
			request.AddHeader("Authorization", "Bearer " + token);
			if (jsonbody != null)
				request.AddJsonBody(jsonbody);
			var result = client.Execute(request);

			Client.logger.Debug($"Executed API.Post at endpoint '{endpoint}'.");

			try
			{
				AppError error = JsonConvert.DeserializeObject<AppError>(result.Content);
				if (error.id != null && error.message != null)
				{
					Client.logger.Error("Error received from API: " + error.ToString());
					return default(T);
				}
			}
			catch { }

			Client.logger.Debug($"Result: " + result.Content);

			try
			{
				return JsonConvert.DeserializeObject<T>(result.Content);
			}
			catch
			{
				Client.logger.Error("Error deserializing result.");
				return default(T);
			}
		}

		internal static T Get<T>(string endpoint, Dictionary<string,string> parameters = null)
		{
			//Make sure client is logged in.
			if (string.IsNullOrWhiteSpace(token))
			{
				Client.logger.Error($"API.Get called at endpoint '{endpoint}', but not logged in.");
				return default(T);
			}

			RestRequest request = new RestRequest(endpoint, Method.GET);
			request.AddHeader("Authorization", "Bearer " + token);
			if (parameters != null)
			{
				foreach (KeyValuePair<string,string> kvp in parameters)
				{
					request.AddParameter(kvp.Key, kvp.Value);
				}
			}
			var result = client.Execute(request);

			Client.logger.Debug($"Executed API.Get at endpoint '{endpoint}'.");

			try
			{
				AppError error = JsonConvert.DeserializeObject<AppError>(result.Content);
				if (error.id != null && error.message != null)
				{
					Client.logger.Error("Error received from API: " + error.ToString());
					return default(T);
				}
			}
			catch { }

			Client.logger.Debug($"Result: " + result.Content);

			try
			{
				return JsonConvert.DeserializeObject<T>(result.Content);
			}
			catch
			{
				Client.logger.Error("Error deserializing result.");
				return default(T);
			}
		}

		internal static T Put<T>(string endpoint, object jsonbody)
		{
			//Make sure client is logged in.
			if (string.IsNullOrWhiteSpace(token))
			{
				Client.logger.Error($"API.Put called at endpoint '{endpoint}', but not logged in.");
				return default(T);
			}

			RestRequest request = new RestRequest(endpoint, Method.PUT);

			request.AddHeader("Content-Type", "application/json");
			request.AddHeader("Authorization", "Bearer " + token);
			if (jsonbody != null)
				request.AddJsonBody(jsonbody);
			var result = client.Execute(request);

			Client.logger.Debug($"Executed API.Put at endpoint '{endpoint}'.");

			try
			{
				AppError error = JsonConvert.DeserializeObject<AppError>(result.Content);
				if (error.id != null && error.message != null)
				{
					Client.logger.Error("Error received from API: " + error.ToString());
					return default(T);
				}
			}
			catch { }

			Client.logger.Debug($"Result: " + result.Content);

			try
			{
				return JsonConvert.DeserializeObject<T>(result.Content);
			}
			catch
			{
				Client.logger.Error("Error deserializing result.");
				return default(T);
			}
		}

		internal static T Delete<T>(string endpoint, object jsonbody)
		{
			//Make sure client is logged in.
			if (string.IsNullOrWhiteSpace(token))
			{
				Client.logger.Error($"API.Put called at endpoint '{endpoint}', but not logged in.");
				return default(T);
			}

			RestRequest request = new RestRequest(endpoint, Method.DELETE);

			request.AddHeader("Content-Type", "application/json");
			request.AddHeader("Authorization", "Bearer " + token);
			if (jsonbody != null)
				request.AddJsonBody(jsonbody);
			var result = client.Execute(request);

			Client.logger.Debug($"Executed API.Delete at endpoint '{endpoint}'.");

			try
			{
				AppError error = JsonConvert.DeserializeObject<AppError>(result.Content);
				if (error.id != null && error.message != null)
				{
					Client.logger.Error("Error received from API: " + error.ToString());
					return default(T);
				}
			}
			catch { }

			Client.logger.Debug($"Result: " + result.Content);

			try
			{
				return JsonConvert.DeserializeObject<T>(result.Content);
			}
			catch
			{
				Client.logger.Error("Error deserializing result.");
				return default(T);
			}
		}
	}
}
