using System;
using System.Net;
using System.Threading.Tasks;

using Hearthstone_Deck_Tracker.Utility.Logging;

namespace TwitchPredictionsBG
{
    // For now we can only authenticate user with only `response_type == code`
    class OauthCallbackListener
    {
        public static async Task HandleIncomingConnection(HttpListener listener, Action<string> callback)
        {
            bool run = true;

            Log.Info($"Starting Web Server");
            while (run)
            {
                // Will wait here until we hear from a connection
                HttpListenerContext ctx = await listener.GetContextAsync();

                // Peel out the requests and response objects
                HttpListenerRequest req = ctx.Request;
                HttpListenerResponse resp = ctx.Response;

                Log.Info(req.QueryString.ToString());

                string code = req.QueryString.Get("code");

                if (code != null)
                {
                    Log.Info($"Got Code");
                    callback(code);
                    run = false;
                }

                resp.Close();
            }
        }

        public static void ServeCallback(string url, Action<string> callback)
        {
            var listener = new HttpListener();
            listener.Prefixes.Add(url);
            listener.Start();

            Task listenTask = HandleIncomingConnection(listener, callback);
            listenTask.GetAwaiter().GetResult();

            // Close the listener
            listener.Close();
        }
    }
}