using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

namespace IND_Weather_API
{
    class Program
    {
        // Change this path to where your certificate is located
        private static string certifacatePath = @"C:\Certificates\IND\certificate.pfx";

        // Change this path to your desired output path
        private static string outputPath = @"C:\Users\Admin\source\ImportLogs\IND Weather API Output.txt";
        

        static void Main(string[] args)
        {
            string apiUrl = "https://ind.dbgurusnetwork.com.au/api/SiteAPI/rc_au";
            string interval = "All";
            string unit = "EFD-0111";
            var startDate = "2020-10-27 12:00";
            var endDate = "2020-10-29 12:00";
            var postedFile = File.OpenRead(certifacatePath);
            var uTCDateStart = Convert.ToDateTime(startDate).ToString("yyyy.MM.dd HH:mm");
            var uTCDateEnd = Convert.ToDateTime(endDate).ToString("yyyy.MM.dd HH:mm");
            var parameters = new
            {
                Unit = unit,
                UTCDateStart = uTCDateStart,
                UTCDateEnd = uTCDateEnd,
                Interval = interval
            };
            string json = JsonConvert.SerializeObject(parameters);
            var output = Post(apiUrl, json, ConvertInputStreamToByteArray(postedFile)).Result;
            var result = new StringBuilder();
            result.AppendLine("Status Code: " + output["statusCode"]);
            result.AppendLine("\nResponse Body: " + output["responseBody"]);
            result.AppendLine("\nResponse Header: " + output["responseHeader"]);

            File.WriteAllText(outputPath, result.ToString());
        }

        public static byte[] ConvertInputStreamToByteArray(Stream stream)
        {
            using (Stream inputStream = stream)
            {
                MemoryStream memoryStream = inputStream as MemoryStream;
                if (memoryStream == null)
                {
                    memoryStream = new MemoryStream();
                    inputStream.CopyTo(memoryStream);
                }
                return memoryStream.ToArray();
            }
        }

        public static Stream ToStream(string data)
        {
            MemoryStream stream = new MemoryStream();
            StreamWriter writer = new StreamWriter(stream);
            writer.Write(data);
            writer.Flush();
            stream.Position = 0;
            return stream;
        }

        public static async Task<Dictionary<string, string>> Post(string apiUrl, string fileContent, byte[] clientCertificatePath = null)
        {
            var result = new Dictionary<string, string>();
            string responseBody = String.Empty;
            string responseHeader = String.Empty;
            string statusCode = String.Empty;
            try
            {
                HttpClient client;
                WebRequestHandler requestHandler = new WebRequestHandler();
                if (clientCertificatePath != null)
                {
                    X509Certificate2 clientCertificate = new X509Certificate2(clientCertificatePath, "", X509KeyStorageFlags.MachineKeySet | X509KeyStorageFlags.PersistKeySet | X509KeyStorageFlags.Exportable);
                    ServicePointManager.Expect100Continue = true;
                    ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls12 | SecurityProtocolType.Ssl3;
                    requestHandler.ClientCertificates.Add(clientCertificate);
                    client = new HttpClient(requestHandler);
                }
                client = clientCertificatePath != null ? new HttpClient(requestHandler) : new HttpClient();
                HttpContent content = new StringContent(fileContent, Encoding.Default, "application/x-www-form-urlencoded");
                HttpResponseMessage response = await client.PostAsync(apiUrl, content);
                HttpContent responseContent = response.Content;
                responseHeader = response.Headers.ToString();
                statusCode = response.StatusCode.ToString();
                using (var reader = new StreamReader(await responseContent.ReadAsStreamAsync()))
                {
                    responseBody = await reader.ReadToEndAsync();
                }
            }
            catch (Exception ex)
            {
                responseBody = ex.ToString();
            }
            result.Add("responseBody", responseBody);
            result.Add("responseHeader", responseHeader);
            result.Add("statusCode", statusCode);

            return result;
        }
    }
}
