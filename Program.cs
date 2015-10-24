



using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Text;

namespace ian.emerick.TCP
{

	class Program
	{
		//private static SortedDictionary<int, string> messageFragmentDictionary = new SortedDictionary<int, string>();
		private static string finalMessage = "";


		static void Main(string[] args)
		{
			List<XmlDocument> responseMessages = new List<XmlDocument>();
			SortedDictionary<int, string> messageFragmentDictionary = new SortedDictionary<int, string>();

			int recordCount = 500;
			int maxConnections = 200;
			
			Stopwatch sw = new Stopwatch();
			sw.Start();

			List<Lazy<string>> requests = new List<Lazy<string>>();

			for (int j = 0; j <= Math.Ceiling((double)recordCount/maxConnections); j++)
			{
				for (int i = 0; i < maxConnections && (i + j * maxConnections) < recordCount; i++)
				{
					Lazy<string> request = SendRequest(i+1 + j*maxConnections);
					requests.Add(request);
				}

				responseMessages.AddRange(readResponses(requests));
				Console.Write((sw.ElapsedMilliseconds / 1000) + " seconds elapsed - " + responseMessages.Count + " records processed \r\n");
				requests = new List<Lazy<string>>();
			}


			foreach (XmlDocument response in responseMessages)
			{
				KeyValuePair<int, string> messageFragement = parseResponseMessage(response);
				messageFragmentDictionary.Add(messageFragement.Key, messageFragement.Value);
			}

			if (sw.ElapsedMilliseconds > 30 * 1000)
			{
				Console.Write("\r\n\r\nInvalid Results\r\n\r\n");
			}

			string s = "\r\n" + constructTheSecretMessage(messageFragmentDictionary) + "\r\n";
			Console.WriteLine(s);


			Console.WriteLine("Press Any Key To Continue");
			Console.ReadKey();

		}

		private static string constructTheSecretMessage(SortedDictionary<int, string> messageFragments)
		{
			char[] arr = new char[messageFragments.Count];

			foreach (KeyValuePair<int, string> kvp in messageFragments)
			{
				arr[kvp.Key - 1] = kvp.Value[2];
			}

			return new string(arr.Reverse().ToArray());

		}

		private static Lazy<string> SendRequest(int requestID)
		{
			TcpClient client = new TcpClient();

			client.Connect(IPAddress.Parse("216.38.192.141"), 8765);
			client.NoDelay = true;
			NetworkStream stream = client.GetStream();


			var msg = GenerateRequestMessage(requestID);
			byte[] to_send = new byte[156];
			msg.CopyTo(to_send, 0);
			stream.Write(to_send, 0, to_send.Length);
			stream.Flush();

			return new Lazy<string>(() =>
			{
				//TODO: add error handling, status, and a retry mechanism
				byte[] readbuf = new byte[156];
				try
				{
					stream.Read(readbuf, 0, 156); // read
				}
				catch (Exception e)
				{
				}
				finally
				{
					stream.Close();
					client.Close();
				}
				
				
				
				return System.Text.Encoding.Default.GetString(readbuf);


			});
		}

		private static KeyValuePair<int, string> parseResponseMessage(XmlDocument doc)
		{
			//TODO add error handling so we're not relying on hard coded indexes of 0
			string messageFragment = doc.DocumentElement.GetElementsByTagName("message")[0].InnerText;
			Int16 requestID = -1;
			Int16.TryParse(doc.DocumentElement.GetElementsByTagName("requestID")[0].InnerText, out requestID);
			KeyValuePair<int, string> kvp = new KeyValuePair<int, string>(requestID, messageFragment);
			return kvp;
			
			

		}

		private static List<XmlDocument> readResponses(List<Lazy<string>> requests)
		{
			List<XmlDocument> responseMessages = new List<XmlDocument>();
			foreach (Lazy<string> response in requests)
			{
				string responseMessage = "";
				responseMessage = response.Value;

				XmlDocument doc = new XmlDocument();
				doc.LoadXml(responseMessage);
				responseMessages.Add(doc);
			}

			return responseMessages;
		}

		private static byte[] GenerateRequestMessage(int requestId)
		{
			XmlDocument doc = new XmlDocument();

			string message = "<?xml version=\"1.0\" encoding=\"ISO-8859-1\"?>" +
								"<request>" +
								"<requestID>" + requestId + "</requestID>" +
								"</request>";

			Encoding iso = Encoding.GetEncoding("ISO-8859-1");
			Encoding utf8 = Encoding.UTF8;
			byte[] utfBytes = utf8.GetBytes(message);
			byte[] isoBytes = Encoding.Convert(utf8, iso, utfBytes);

			return isoBytes;



		}



	}



}
