﻿/*-----------------------------+------------------------------\
|                                                             |
|                        !!!NOTICE!!!                         |
|                                                             |
|  These libraries are under heavy development so they are    |
|  subject to make many changes as development continues.     |
|  For this reason, the libraries may not be well commented.  |
|  THANK YOU for supporting forge with all your feedback      |
|  suggestions, bug reports and comments!                     |
|                                                             |
|                               - The Forge Team              |
|                                 Bearded Man Studios, Inc.   |
|                                                             |
|  This source code, project files, and associated files are  |
|  copyrighted by Bearded Man Studios, Inc. (2012-2015) and   |
|  may not be redistributed without written permission.       |
|                                                             |
\------------------------------+-----------------------------*/


#if !UNITY_WEBGL

using BeardedManStudios.Threading;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;

#if NetFX_CORE
using Windows.UI.Xaml.Media.Imaging;
using Windows.Storage.Streams;
using System.Runtime.InteropServices.WindowsRuntime;
#endif

namespace BeardedManStudios.Network
{
	// TODO:  Setup multithreaded http get and post request default classes
	/// <summary>
	/// Used to access content from the web
	/// </summary>
	/// <remarks>
	/// HTTP allows you to make multithreaded requests to the web, getting back content from any website. Typically this can be used to access live information,
	/// interact with databases or download content (typically images) directly into your game. HTTP objects typically involve specifying a URL in the 
	/// constructor and then using the Get() method.
	/// </remarks>
	public class HTTP
	{
		/// <summary>
		/// URL of the HTTP
		/// </summary>
		public string URL { get; private set; }

		public enum RequestType { Get, Post, Put, Delete }

		/// <summary>
		/// The type of HTTP request this will be
		/// </summary>
		public RequestType requestType = RequestType.Get;
		public enum ContentType { Default, Form, JSON }

		/// <summary>
		/// The type of content you are expecting from this url
		/// </summary>
		public ContentType contentType = ContentType.Default;

		private WebHeaderCollection headers = null;

		/// <summary>
		/// Constructor for a passed in URL
		/// </summary>
		/// <param name="url">URL to be pulling data from</param>
		public HTTP(string url, string[] stringHeaders = null)
		{
			URL = url;

#if !NetFX_CORE
			if (stringHeaders != null)
			{
				headers = new WebHeaderCollection();

				foreach (string head in stringHeaders)
					headers.Add(head);
			}
#endif
		}

		/// <summary>
		/// Get a response from the HTTP
		/// </summary>
		/// <param name="callback">The method to call when a response is received</param>
		/// <param name="getString">the string to be used in the URL</param>
		/// <remarks>
		/// The main method used in the HTTP class, this allows you to get a response from HTTP. You can use a lambda expression, delegate
		/// or method to execute the response. The response will likely be string which you may want to use string.Split() to separte and
		/// various Parse methods such as int.Parse().
		/// Be aware, all of the html tags will be be part of the response, if you want to access content from websites, you may want to learn
		/// a little html to target specific bits of information.
		/// Additionally parameters can be applied, the Get() parameters are used by websites to take input and display results based on those inputs.
		/// You can use this to input data into a database, if you write your own .PHP scripts for your own website.
		/// each string needs to be written in the following format "key=input" for example "name=mark".
		/// See <A HREF="http://developers.forgepowered.com/Tutorials/MasterClassBeginner/HTTP-Library">this</A> for more...
		/// </remarks>
		public void Get(Action<object> callback, params string[] getString)
		{
			if (getString.Length > 0)
			{
				URL += "?" + getString[0];

				for (int i = 1; i < getString.Length; i++)
					URL += "&" + getString[i];
			}

			requestType = RequestType.Get;
			Send(callback);
		}

		/// <summary>
		/// Get an image response to be used in the game
		/// </summary>
		public void GetImage(Action<object> callback)
		{
#if NetFX_CORE
			Task.Run(async () =>
#else
			Task.Run(() =>
#endif
			{
#if NetFX_CORE
				BitmapImage bitmapImage = new BitmapImage(new Uri(URL));
				RandomAccessStreamReference rasr = RandomAccessStreamReference.CreateFromUri(bitmapImage.UriSource);
				var streamWithContent = await rasr.OpenReadAsync();
				IBuffer buffer = new byte[streamWithContent.Size].AsBuffer();
				await streamWithContent.ReadAsync(buffer, (uint)streamWithContent.Size, InputStreamOptions.None);

				if (callback != null)
					callback(buffer.ToArray());
#else
				callback(GetImageResponse(URL));
#endif
			});
		}

		/// <summary>
		/// Post a message to the URL
		/// </summary>
		/// <param name="postString">Message to post</param>
		public void Post(Action<object> callback, string postString)
		{
			requestType = RequestType.Post;
			Send(callback, Encryptor.Encoding.GetBytes(postString));
		}

		/// <summary>
		/// Post a message to the URL with parameters
		/// </summary>
		/// <param name="parameters">Message to post with parameters</param>
		public void Post(Action<object> callback, Dictionary<string, string> parameters)
		{
			string postString = string.Empty;

			if (parameters != null)
			{
				foreach (KeyValuePair<string, string> kv in parameters)
				{
					if (!string.IsNullOrEmpty(postString))
						postString += "&";

					postString = kv.Key + "=" + kv.Value;
				}
			}

			Post(callback, postString);
		}

		/// <summary>
		/// Put a response to the HTTP
		/// </summary>
		public void Put(Action<object> callback)
		{
			requestType = RequestType.Put;
			Send(callback);
		}

		/// <summary>
		/// Delete a response to the HTTP
		/// </summary>
		public void Delete(Action<object> callback)
		{
			requestType = RequestType.Delete;
			Send(callback);
		}

		//private object requestMutex = new object();
		private void Send(Action<object> callback, byte[] argument = null)
		{
			Task.Run(() =>
			{
#if NetFX_CORE
				GetWebResponse(argument, callback);
#else
				callback(GetWebResponse(argument));
#endif
			});
		}

		/// <summary>
		/// Gets an image response from HTTP request as a byte[]
		/// </summary>
#if NetFX_CORE
		public void GetImageResponse(byte[] parameters)
#else
		public byte[] GetImageResponse(object arg)
#endif
		{
#if !NetFX_CORE
			string url = (string)arg;
			byte[] imageAsByteArray;
			using (var webClient = new WebClient())
			{
				imageAsByteArray = webClient.DownloadData(url);
			}

			return imageAsByteArray;
#endif
		}

#if NetFX_CORE
		private async void GetWebResponse(byte[] parameters, Action<object> callback)
#else
		private object GetWebResponse(byte[] parameters)
#endif
		{
			WebRequest request = WebRequest.Create(URL);

			try
			{
				request.Credentials = CredentialCache.DefaultCredentials;

#if !NetFX_CORE
				request.Timeout = 10000;
				ServicePointManager.ServerCertificateValidationCallback += (o, certificate, chain, errors) => true;

				if (headers != null)
					request.Headers.Add(headers);
#endif

				if (contentType == ContentType.Form)
					request.ContentType = "application/x-www-form-urlencoded";
				else if (contentType == ContentType.JSON)
					request.ContentType = "application/json";

				if (requestType == RequestType.Get)
					request.Method = "GET";
				else if (requestType == RequestType.Post)
					request.Method = "POST";
				else if (requestType == RequestType.Put)
					request.Method = "PUT";
				else if (requestType == RequestType.Delete)
					request.Method = "DELETE";

				if (parameters != null)
				{
#if NetFX_CORE
					Stream inputStream = await request.GetRequestStreamAsync();
					inputStream.Write(parameters, 0, parameters.Length);
#else
					using (Stream inputStream = request.GetRequestStream())
					{
						inputStream.Write(parameters, 0, parameters.Length);
						inputStream.Close();
						inputStream.Dispose();
					}
#endif
				}
				
				string responseFromServer = string.Empty;
#if NetFX_CORE
				HttpWebResponse webResponse = (HttpWebResponse)request.GetResponseAsync().Result;
#else

				try
				{
					using (HttpWebResponse webResponse = (HttpWebResponse)request.GetResponse())
					{
#endif
						using (Stream dataStream = webResponse.GetResponseStream())
						{
							using (StreamReader reader = new StreamReader(dataStream))
							{
								responseFromServer = reader.ReadToEnd();
#if !NetFX_CORE
								reader.Close();
#endif
								reader.Dispose();
							}

#if !NetFX_CORE
							dataStream.Close();
#endif
							dataStream.Dispose();
						}

#if NetFX_CORE
						webResponse.Dispose();
#else
						webResponse.Close();
					}
				}
				catch (Exception e)
				{
					request.Abort();
#if NetFX_CORE
					callback(e)
#else
					//TODO: If you need to see the error message, please debug it here.
					e = null;
					return e; // JM: don't output the error to screen
#endif
				}
#endif
				
				request.Abort();
#if NetFX_CORE
				callback(responseFromServer);
#else
				return responseFromServer;
#endif
			}
			catch (Exception e)
			{
				request.Abort();
#if NetFX_CORE
				callback(e);
#else
				return e;
#endif
			}
		}

		public class RequestState
		{
			// This class stores the state of the request. 
			const int BUFFER_SIZE = 1024;
			public StringBuilder requestData;
			public byte[] bufferRead;
			public WebRequest request;
			public WebResponse response;
			public Stream responseStream;
			public RequestState()
			{
				bufferRead = new byte[BUFFER_SIZE];
				requestData = new StringBuilder("");
				request = null;
				responseStream = null;
			}
		}
	}
}
#endif