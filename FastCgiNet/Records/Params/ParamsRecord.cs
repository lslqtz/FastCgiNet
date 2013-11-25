using System;
using System.Linq;
using System.Collections.Generic;
using System.IO;
using FastCgiNet.Streams;

namespace FastCgiNet
{
	public class ParamsRecord : StreamRecordBase
	{
		/// <summary>
		/// Enumerates the parameters in this record.
		/// </summary>
		/// <remarks>This will rewind <see cref="Contents"/> before enumerating, and rewind it after all elements have been enumerated as well.</remarks>
		public IEnumerable<NameValuePair> Parameters
		{
			get
			{
				if (Contents != null)
				{
					Contents.Seek(0, SeekOrigin.Begin);
					using (var paramsEnumerator = new NvpEnumerator(Contents, ContentLength))
					{
						while (paramsEnumerator.MoveNext())
							yield return ((IEnumerator<NameValuePair>)paramsEnumerator).Current;
					}

					Contents.Seek(0, SeekOrigin.Begin);
				}
				else
					yield break;
			}
		}

		public void Add(NameValuePair nvp)
		{
			foreach (var seg in nvp.GetBytes())
				Contents.Write(seg.Array, seg.Offset, seg.Count);
		}

		/// <summary>
		/// Do not write directly to this stream unless you know what you're doing! Use the <see cref="Add(NameValuePair nvp)"/> or <see cref="Add(string key, string value)"/> methods,
		/// which will then write to the stream correctly.
		/// </summary>
		public override RecordContentsStream Contents
		{
			get
			{
				return base.Contents;
			}
			set
			{
				base.Contents = value;
			}
		}

		/// <summary>
		/// Adds a name value pair with name <paramref name="key"/> and value <paramref name="value"/>.
		/// Make sure both strings can be ASCII encoded.
		/// </summary>
		public void Add(string key, string value)
		{
			Add(new NameValuePair(key, value));
		}
		
		private string HtmlEncode(string queryString)
		{
			//TODO: Fully implement this
			return queryString.Replace(" ", "%20");
		}
		
		private static string[] ValidMethods = new string[] { "GET", "POST", "PUT", "DELETE", "HEAD" }; //TODO: Other Http 1.1 methods
		/// <summary>
		/// Adds the FastCgi parameters that would be created for an HTTP 1.1 request with method <paramref name="method"/> at
		/// Url <paramref name="u"/>.
		/// </summary>
		/// <param name="u">A non Url escaped url.</param>
		/// <param name="method">The HTTP 1.1 method. It must be written upper case.</param>
		public void SetParamsFromUri(Uri u, string method)
		{
			if (u == null)
				throw new ArgumentNullException("url");
			else if (method == null)
				throw new ArgumentNullException("method");
			else if (ValidMethods.Contains(method) == false)
				throw new ArgumentException("Method is not valid. Make sure it is a valid upper case HTTP/1.1 method written ");

			Add("HTTP_HOST", u.Host);
			if (u.Scheme == Uri.UriSchemeHttps)
				Add("HTTPS", "https"); //TODO: Is this what nginx does, for example?
			Add("SCRIPT_NAME", u.AbsolutePath);
			Add("DOCUMENT_URI", u.AbsolutePath);
			Add("REQUEST_METHOD", method);
			Add("SERVER_NAME", u.Host);
			Add("QUERY_STRING", HtmlEncode(u.Query));
			Add("REQUEST_URI", u.AbsolutePath + "/" + HtmlEncode(u.Query));
			Add("SERVER_PROTOCOL", "HTTP/1.1");
			Add("GATEWAY_INTERFACE", "CGI/1.1");
		}

		public ParamsRecord(ushort requestId)
			: base(RecordType.FCGIParams,  requestId)
		{
		}

		internal ParamsRecord(byte[] data, int offset, int length, out int endOfRecord)
			: base(RecordType.FCGIParams, data, offset, length, out endOfRecord)
		{
		}
	}
}