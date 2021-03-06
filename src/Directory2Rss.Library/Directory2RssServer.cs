﻿using EmbedIO;
using Directory2Rss.Library.Controllers;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;

namespace Directory2Rss.Library
{
    public class Directory2RssServer
    {
        public PodcastConfig Config { get; set; }
        public Directory2RssServer(PodcastConfig config)
        {
            Config = config;
        }

        /// <summary>
        /// starts the web server as a blocking async call
        /// </summary>
        public async Task StartAsync()
        {
            await RunWebServer();
        }

        private async Task RunWebServer()
        {
            //non-admin users must run on localhost only
            using (var server = new WebServer(string.Format("http://{0}:{1}", Config.IPAddress, Config.HttpPort)))
            {
                //Assembly assembly = typeof(App).Assembly;
                PodcastDirectoryController controller = new PodcastDirectoryController(Config);
                server.WithLocalSessionManager();
                server.WithWebApi("/", m => m.RegisterController(() => controller));
                await server.RunAsync();
            }
        }
    }
}
