using Newtonsoft.Json;
using RestSharp;
using System;

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

		internal static string PostGetAuth(object jsonbody)
		{
			RestRequest request = new RestRequest("/users/login", Method.POST);
			request.AddHeader("Content-Type", "application/json");
			request.AddJsonBody(jsonbody);
			var result = client.Execute(request);

			Client.logger.Debug($"Executed API.Post at endpoint '/users/login'.");

			try
			{
				AppError error = JsonConvert.DeserializeObject<AppError>(result.Content);
				if (error.id != null && error.status_code != null)
				{
					Client.logger.Error("Error received from API: " + error.ToString());
					throw new Exception("Error received from API Authorization request: " + error.ToString());
				}
			}
			catch { }

			token = result.Headers[1].Value.ToString();
			return result.Content;
		}

		internal static string Post(string endpoint, object jsonbody)
		{
			//Make sure client is logged in.
			if (string.IsNullOrWhiteSpace(token))
			{
				Client.logger.Error($"API.Post called at endpoint '{endpoint}', but not logged in.");
				return "";
			}

			RestRequest request = new RestRequest(endpoint, Method.POST);
			request.AddHeader("Content-Type", "application/json");
			request.AddHeader("Authorization", "Bearer " + token);
			request.AddJsonBody(jsonbody);
			var result = client.Execute(request);

			Client.logger.Debug($"Executed API.Post at endpoint '{endpoint}'.");

			try
			{
				AppError error = JsonConvert.DeserializeObject<AppError>(result.Content);
				if (error.id != null && error.status_code != null)
				{
					Client.logger.Error("Error received from API: " + error.ToString());
					return "";
				}
			}
			catch
			{

			}

			return result.Content;
		}
	}
}
