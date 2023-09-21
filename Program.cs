using System.Net;
using System.Text;

namespace C__webserver
{
    internal class Program
    {
        public static string endpoint = "";
        static void Main(string[] args)
        {
            if (File.Exists("./prod.txt"))
                endpoint = "http://+:4201/";
            else
                endpoint = "http://localhost:4201/";
            SQLiteUtils.init();
            Console.WriteLine("Database created");

            Task.Run(() => Start(args)).GetAwaiter().GetResult();
        }

        static async Task Start(string[] args)
        {
            string[] prefixes = { endpoint };  // Replace with your desired server address

            HttpListener listener = new HttpListener();
            foreach (string prefix in prefixes)
                listener.Prefixes.Add(prefix);

            listener.Start();
            Console.WriteLine("Server started. Listening for requests...");
            if (!File.Exists("./server.log"))
                File.Create("./server.log").Close();

            while (true)
            {
                HttpListenerContext context = await listener.GetContextAsync();

                // Handle requests asynchronously
                Task.Run(() => HandleRequestAsync(context));
            }
        }

        static async Task HandleRequestAsync(HttpListenerContext context)
        {
            Console.WriteLine("Handling request..");
            HttpListenerRequest request = context.Request;
            HttpListenerResponse response = context.Response;

            // Build log
            StringBuilder stringBuilder = new StringBuilder();
            stringBuilder.Append("Request headers:\n");
            foreach (string key in request.Headers.AllKeys)
            {
                stringBuilder.Append($"{key}: {request.Headers[key]}\n");
            }

            string responseString = "";

            if (request?.Url?.AbsolutePath == "/login")
            {
                Console.WriteLine("Hit /login");
                if (request.HttpMethod != "POST")
                {
                    responseString = $"Invalid request method. \"{request.HttpMethod}\"" +
                        $"\nFull request details:" +
                        $"\n {request.Headers}" +
                        $"\n {request.AcceptTypes}" +
                        $"\n Input stream:" +
                        $"\n {request.InputStream}";
                }
                else
                {
                    string res = await handleLogin(request);
                    // Check request method and respond accordingly
                    responseString = res;
                }


            }
            else if (request?.Url?.AbsolutePath == "/signup")
            {
                Console.WriteLine("Hit /singup");
                if (request.HttpMethod != "POST")
                {
                    responseString = $"Invalid request method. \"{request.HttpMethod}\"" +
                        $"\nFull request details:" +
                        $"\n {request.Headers}" +
                        $"\n {request.AcceptTypes}" +
                        $"\n Input stream:" +
                        $"\n {request.InputStream}";
                }
                else
                {
                    string res = await handleRegister(request);
                    // Check request method and respond accordingly
                    responseString = res;
                }
            }
            else
            {
                responseString = "Invalid endpoint.";
            }
            stringBuilder.Append("\nRequest body:\n");
            using (StreamReader reader = new StreamReader(request.InputStream, request.ContentEncoding))
            {
                stringBuilder.Append(await reader.ReadToEndAsync());
            }
            stringBuilder.Append("\n");
            // status code is first three chars of response string
            response.StatusCode = int.Parse(responseString.Substring(0, 3));
            responseString = responseString.Substring(3);
            stringBuilder.Append("Response:\n");
            stringBuilder.Append(responseString);
            _ = Task.Run(() => addToLog(stringBuilder.ToString()));

            byte[] buffer = Encoding.UTF8.GetBytes(responseString);
            response.ContentLength64 = buffer.Length;

            // Write the response asynchronously
            response.OutputStream.Write(buffer, 0, buffer.Length);
            response.Close();
        }
        private static void addToLog(string text)
        {
            File.AppendAllText("./server.log", text);
        }
        private static async Task<string> handleLogin(HttpListenerRequest request)
        {
            // Get post Data from body
            string postData;
            using (var reader = new System.IO.StreamReader(request.InputStream, request.ContentEncoding))
            {
                postData = await reader.ReadToEndAsync();
            }

            try
            {
                // Parse post data as form data
                string username = "";
                string password = "";

                string[] postArray = postData.Split('\n');
                for (int i = 0; i < postArray.Length - 1; i++)
                {
                    if (postArray[i].Contains("username"))
                    {
                        username = postArray[i + 2].Trim();
                    }
                    if (postArray[i].Contains("password"))
                    {
                        password = postArray[i + 2].Trim();
                    }
                }
                if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
                {
                    return "401Invalid username, password or request formatting";
                }
                else
                {
                    return SQLiteUtils.verifyLogin(username, password);
                }
            }
            catch
            {
                return "500huh??";
            }
        }
        private static async Task<string> handleRegister(HttpListenerRequest request)
        {
            string postData;
            using (var reader = new StreamReader(request.InputStream, request.ContentEncoding))
            {
                postData = await reader.ReadToEndAsync();
            }

            try
            {
                // Parse post data as form data
                string username = "";
                string password = "";

                string[] postArray = postData.Split('\n');
                for (int i = 0; i < postArray.Length - 1; i++)
                {
                    if (postArray[i].Contains("username"))
                    {
                        username = postArray[i + 2].Trim();
                    }
                    if (postArray[i].Contains("password"))
                    {
                        password = postArray[i + 2].Trim();
                    }
                }
                if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
                {
                    return "401Invalid username, password or request formatting";
                }
                else
                {
                    Task<string> t = SQLiteUtils.registerUser(username, password);

                    t.Wait();
                    string res = t.Result;

                    if (res != "User added!")
                    {
                        return res;
                    }
                    else
                    {
                        return "200User was created!";
                    }
                }
            }
            catch
            {
                return "500huh??";
            }
        }
    }
}