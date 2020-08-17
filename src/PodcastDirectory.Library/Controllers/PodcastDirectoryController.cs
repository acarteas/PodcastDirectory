﻿using EmbedIO;
using EmbedIO.Routing;
using EmbedIO.WebApi;
using Swan;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace Directory2Rss.Library.Controllers
{
    public class PodcastDirectoryController : WebApiController
    {
        public PodcastConfig Config { get; set; }

        public PodcastDirectoryController(PodcastConfig config)
        {
            Config = config;
        }

        [Route(HttpVerbs.Get, "/")]
        public async Task<string> GetRoot()
        {
            return string.Format("<a href=\"http://{0}/rss.xml\">visit the RSS feed</a>", Config.IPAddress);
        }

        [Route(HttpVerbs.Get, "/image")]
        public async Task GetPodcastImage()
        {
            HttpContext.Response.ContentType = "image/png";
            WriteBinary(Path.Join(Directory.GetCurrentDirectory(), "GenericImage.png"));
        }

        [Route(HttpVerbs.Get, "/rss")]
        public async Task GetRssFeed()
        {
            string baseUrl = string.Format("http://{0}", Config.IPAddress);
            HttpContext.Response.ContentType = "text/xml";
            using(var writer = HttpContext.OpenResponseText())
            {
                PodcastRss rss = new PodcastRss(Config);
                var files = Directory
                    .EnumerateFiles(Config.DirectoryToServe)
                    .Where(f => Config.AudioExtensions.Contains(Path.GetExtension(f)))
                    .OrderBy(f => f);

                //force ordering in podcast player by manipulating the dates
                DateTime referenceDate = DateTime.UtcNow.AddDays(-files.Count() + 1);

                foreach (var fileStr in files)
                {
                    var tfile = TagLib.File.Create(fileStr);
                    FileInfo fi = new FileInfo(fileStr);
                    string encodedFileName = Convert.ToBase64String(Encoding.UTF8.GetBytes(Path.GetFileName(tfile.Name)));
                    
                    //each podcast item will appear on the subsequent day
                    DateTime fileDate = new DateTime(referenceDate.Ticks);
                    fileDate = fileDate.AddDays(1);
                    referenceDate = new DateTime(fileDate.Ticks);

                    PodcastItem item = new PodcastItem()
                    {
                        Author = tfile.Tag.FirstAlbumArtist,
                        Title = tfile.Tag.Title,
                        PodcastBaseUrl = baseUrl,
                        AudioUrl = string.Format("{0}/files/{1}", baseUrl, encodedFileName),
                        PublicationDate = fileDate,
                        Duration = tfile.Properties.Duration.ToString("hh\\:mm\\:ss")
                    };
                    rss.AddItem(item);

                }
                await writer.WriteAsync(rss.ToXml());
            }
        }

        [Route(HttpVerbs.Get, "/files/{encodedFile}")]
        public async Task GetFile(string encodedFile)
        {
            HttpContext.Response.ContentType = "audio/mpeg";
            string decodedFile = Encoding.UTF8.GetString(Convert.FromBase64String(encodedFile));
            string filePath = Path.Join(Config.DirectoryToServe, decodedFile);
            if(File.Exists(filePath))
            {
                WriteBinary(filePath);
            }
        }

        private void WriteBinary(string fileName)
        {
            using (var stream = HttpContext.OpenResponseStream())
            {
                var fileData = File.OpenRead(fileName);
                byte[] buffer = new byte[1024];
                int numBytesToRead = (int)fileData.Length;
                int numBytesRead = 0;
                while (numBytesToRead > 0)
                {
                    int bytesRead = fileData.Read(buffer);
                    if (bytesRead == 0)
                    {
                        break;
                    }
                    numBytesRead += bytesRead;
                    numBytesToRead -= bytesRead;

                    stream.Write(buffer);
                }
            }
        }
    }
}
