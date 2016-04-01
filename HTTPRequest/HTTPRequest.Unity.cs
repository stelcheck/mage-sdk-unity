﻿// IF WE ARE USING UNITY, USE THIS VERSION OF THE CLASS
#if UNITY_5

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Text;

using UnityEngine;


public class HTTPRequest {
	private WWW request;
	private Action<Exception, string> cb;
	private Stopwatch timeoutTimer = new Stopwatch();

	//
	public double timeout = 100 * 1000;


	//
	public HTTPRequest(string url, byte[] postData, Dictionary<string, string> headers, Action<Exception, string> cb) {
		// Start timeout timer
		timeoutTimer.Start();

		// Queue constructor for main thread execution
		HTTPRequestManager.Queue(Constructor(url, postData, headers, cb));
	}


	//
	private IEnumerator Constructor(string url, byte[] postData, Dictionary<string, string> headers, Action<Exception, string> cb) {
		this.cb = cb;
		this.request = new WWW(url, postData, headers);

		HTTPRequestManager.Queue(WaitLoop());
		yield break;
	}


	//
	private IEnumerator WaitLoop() {
		while (!request.isDone) {
			if (timeoutTimer.ElapsedMilliseconds >= timeout) {
				// Timed out abort the request with timeout error
				cb(new Exception("Request timed out"), null);
				this.Abort();
				yield break;
			} else if (request == null) {
				// Check if we destroyed the request due to an abort
				yield break;
			}

			// Otherwise continue to wait
			yield return null;
		}

		// Stop the timeout timer
		timeoutTimer.Stop();

		// Check if there is a callback
		if (cb == null) {
			yield break;
		}

		// Check if there was an error with the request
		if (request.error != null) {
			int statusCode = 0;
			if (request.responseHeaders.ContainsKey("STATUS")) {
				statusCode = int.Parse(request.responseHeaders["STATUS"].Split(' ')[1]);
			}

			cb(new HTTPRequestException(request.error, statusCode), null);
			yield break;
		}

		// Otherwise return the response
		cb(null, request.text);
	}


	// Abort request
	public void Abort() {
		WWW _request = request;
		request = null;

		_request.Dispose();
		timeoutTimer.Stop();
	}



	// Create GET request and return it
	public static HTTPRequest Get(string url, Dictionary<string, string> headers, CookieContainer cookies, Action<Exception, string> cb) {
		// TODO: COOKIE SUPPORT

		// Create request and return it
		// The callback will be called when the request is complete
		return new HTTPRequest(url, null, headers, cb);
	}
	
	// Create POST request and return it
	public static HTTPRequest Post(string url, string contentType, string postData, Dictionary<string, string> headers, CookieContainer cookies, Action<Exception, string> cb) {
		byte[] binaryPostData = Encoding.UTF8.GetBytes(postData);
		return Post(url, contentType, binaryPostData, headers, cookies, cb);
	}
	
	// Create POST request and return it
	public static HTTPRequest Post(string url, string contentType, byte[] postData, Dictionary<string, string> headers, CookieContainer cookies, Action<Exception, string> cb) {
		Dictionary<string, string> headersCopy = new Dictionary<string, string>(headers);
		
		// TODO: COOKIE SUPPORT
		
		// Set content type if provided
		if (contentType != null) {
			headersCopy.Add("ContentType", contentType);
		}
		
		// Create request and return it
		// The callback will be called when the request is complete
		return new HTTPRequest(url, postData, headersCopy, cb);
	}
}
#endif