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
        private static string certifacatePath = @"C:\Certificates\IND\UnitedEnergy\unitedenergy.pfx";

        // Change this path to your desired output path
        private static string outputPath = @"C:\Users\Admin\source\ImportLogs\IND Weather API Output.txt";
        

        static void Main(string[] args)
        {
            /* 
             * Weather API Endpoints
             * RC AU - https://unitedenergy.rc.weather.api.efd-portal.com.au
             * RC US - https://unitedenergy.rc.weather.api.efd-portal.com
             * AU Live - https://unitedenergy.weather.api.efd-portal.com.au
             * US Live - https://unitedenergy.weather.api.efd-portal.com
            */

            // Set api endpoint
            string apiUrl = "https://unitedenergy.weather.api.efd-portal.com.au";

            /* 
             * Interval can be All or 5
             * All = All data 
             * 5 = Only the data on the 5 minute interval
            */

            // Set interval
            string interval = "All";

            // Enter unit here
            string unit = "EFD-0484";

            // Enter start date and end date
            var startDate = "2020-11-27 12:00";
            var endDate = "2020-11-30 12:00";

            var postedFile = File.OpenRead(certifacatePath);

            // Change date format to yyyy.MM.dd HH:mm
            var uTCDateStart = Convert.ToDateTime(startDate).ToString("yyyy.MM.dd HH:mm");
            var uTCDateEnd = Convert.ToDateTime(endDate).ToString("yyyy.MM.dd HH:mm");

            var parameters = new
            {
                Unit = unit,
                UTCDateStart = uTCDateStart,
                UTCDateEnd = uTCDateEnd,
                Interval = interval
            };

            // Convert object to json
            string json = JsonConvert.SerializeObject(parameters);

            // Post data
            var output = Post(apiUrl, json, ConvertInputStreamToByteArray(postedFile)).Result;

            var result = new StringBuilder();
            result.AppendLine("Status Code: " + output["statusCode"]);
            result.AppendLine("\nResponse Body: " + output["responseBody"]);
            result.AppendLine("\nResponse Header: " + output["responseHeader"]);

            // Store ouput in a txt file
            File.WriteAllText(outputPath, result.ToString());
            
            Console.WriteLine($"result is stored in {outputPath}");
            Console.ReadLine();
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
                    requestHandler.ClientCertificates.Add(clientCertificate);
                }
                ServicePointManager.Expect100Continue = true;
                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls12 | SecurityProtocolType.Ssl3;
                client = clientCertificatePath != null ? new HttpClient(requestHandler) : new HttpClient();
                client.DefaultRequestHeaders.Add("Connection", "Keep-Alive");
                client.DefaultRequestHeaders.Add("Keep-Alive", "3600");
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
