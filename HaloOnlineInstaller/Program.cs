using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using CG.Web.MegaApiClient;
using ICSharpCode.SharpZipLib.Zip;
using Newtonsoft.Json;

namespace Installer
{
    class Program
    {
        static void Main()
        {
            System.Net.WebClient wc = new System.Net.WebClient();
            dynamic dew = JsonConvert.DeserializeObject(wc.DownloadString("http://thetwist84.github.io/HaloOnlineModManager/game/game.json")); 

            string name = dew["base"].Name;
            string filename = dew["base"].Filename;
            string hash = dew["base"].Hash;
            string url = dew["base"].Url;

            string dewLoc = Path.Combine(Directory.GetCurrentDirectory(), "ElDewrito");
            string fileLoc = Path.Combine(Directory.GetCurrentDirectory(), filename);

            if (!File.Exists(fileLoc))
            {
                Download(url, filename);
                HashCheck(url, hash, filename, name);
            }
            else
            {
                HashCheck(url, hash, filename, name);
            }

            if (Directory.Exists(dewLoc))
            {
                File.WriteAllText(Path.Combine(dewLoc, "autoexec.cfg"), "Game.SkipLauncher \"1\"");
            }

            Console.WriteLine("Installation complete.\nPress any key to exit.");
            Console.ReadLine();
        }
        static void Download(string url, string filename)
        {
            string fileloc = Path.Combine(Directory.GetCurrentDirectory(), filename);

            Uri uri = new Uri(url);
            MegaApiClient mega = new MegaApiClient();
            mega.LoginAnonymous();

            Stopwatch watcher = Stopwatch.StartNew();

            if (url.Contains("mega.co.nz"))
            {
                Console.WriteLine("Download started for:" + filename);
                watcher.Start();
                Task t = mega.DownloadFileAsync(uri, fileloc);
                using (var progress = new ProgressBar())
                {
                    while (!t.IsCompleted)
                    {
                        progress.Report((double)mega.Progress / 100);
                    }
                }
                mega.Logout();
                watcher.Stop();
                Console.WriteLine("Download finished for: " + filename + " in {0}.\n ", watcher.Elapsed);
            }
            else
            {
                Console.WriteLine("Download started for:" + filename);
                watcher.Start();
                DownloadDirect(url, filename);
                watcher.Stop();
                Console.WriteLine("Download finished for: " + filename + " in {0}.\n ", watcher.Elapsed);
            }
        }
        static void DownloadDirect(string url, string filename)
        {
            string fileloc = Path.Combine(Directory.GetCurrentDirectory(), filename);

            Uri uri = new Uri(url);
            System.Net.WebClient client = new System.Net.WebClient();

            Stopwatch watcher = Stopwatch.StartNew();
            watcher.Start();

            DownloadGamefile DGF = new DownloadGamefile();

            DGF.DownloadFile(url, filename);

            using (var progress = new ProgressBar())
            {
                while (!DGF.DownloadCompleted)
                {
                    progress.Report((double)DGF.DownloadPercentage / 100);
                }
            }

            watcher.Stop();
            
        }
        private static string GetChecksumBuffered(Stream stream)
        {
            using (var bufferedStream = new BufferedStream(stream, 1024 * 32))
            {
                var sha = new SHA256Managed();
                byte[] checksum = sha.ComputeHash(bufferedStream);
                return BitConverter.ToString(checksum).Replace("-", String.Empty);
            }
        }
        static void HashCheck(string Url, string Hash, string Filename, string Name)
        {
            string dewLoc = Path.Combine(Directory.GetCurrentDirectory(), "ElDewrito");
            string fileloc = Path.Combine(Directory.GetCurrentDirectory(), Filename);

            Stopwatch watcher = Stopwatch.StartNew();

            if (File.Exists(Path.Combine(Directory.GetCurrentDirectory(), Filename)))
            {
                var fileStream = new FileStream(fileloc, FileMode.OpenOrCreate,
                                            FileAccess.Read);
                string compHash = GetChecksumBuffered(fileStream);
                if (compHash == Hash)
                {
                    Console.WriteLine("Hash check succeeded.");
                    ExtractZip(Filename, Name);
                }
                else
                {
                    Console.WriteLine("Hash check failed.");
                    File.Delete(Path.Combine(Directory.GetCurrentDirectory(), Filename));
                    Download(Url, Filename);
                    HashCheck(Url, Hash, Filename, Name);
                }
            }
        }
        static void ExtractZip(string filename, string Name)
        {
            string dewLoc = Path.Combine(Directory.GetCurrentDirectory(), "ElDewrito");
            string fileloc = Path.Combine(Directory.GetCurrentDirectory(), filename);

            FastZip fastZip = new FastZip();
            string filter = null;

            Stopwatch watcher = Stopwatch.StartNew();

            Console.WriteLine("Extraction started for: " + Name);
            watcher.Restart();
            fastZip.ExtractZip(filename, dewLoc, filter);
            watcher.Stop();
            Console.WriteLine("Extraction finished for: " + Name + " in {0}.\n ", watcher.Elapsed);
        }
        //static void Updater(){}
    }
    class DownloadGamefile
    {
        private volatile bool _completed;
        private volatile int _progress;

        public void DownloadFile(string address, string location)
        {
            System.Net.WebClient client = new System.Net.WebClient();
            Uri Uri = new Uri(address);
            _completed = false;

            client.DownloadFileCompleted += new AsyncCompletedEventHandler(Completed);

            client.DownloadProgressChanged += new DownloadProgressChangedEventHandler(DownloadProgress);
            client.DownloadFileAsync(Uri, location);

        }

        public bool DownloadCompleted { get { return _completed; } }

        private void DownloadProgress(object sender, DownloadProgressChangedEventArgs e)
        {
            // Displays the operation identifier, and the transfer progress.
            /*Console.WriteLine("{0}    downloaded {1} of {2} bytes. {3} % complete...",
                (string)e.UserState,
                e.BytesReceived,
                e.TotalBytesToReceive,
                e.ProgressPercentage);*/
            _progress = e.ProgressPercentage;

        }

        public int DownloadPercentage { get { return _progress; } }

        private void Completed(object sender, AsyncCompletedEventArgs e)
        {
            _completed = true;
        }
    }
}