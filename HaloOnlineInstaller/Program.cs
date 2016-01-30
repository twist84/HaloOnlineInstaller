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
            if (!Environment.OSVersion.ToString().Contains("Microsoft Windows NT 6.2"))
                ConsoleResize(120, 30);

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
                Console.Clear();
                HashCheck(url, hash, filename, name);
                Console.Clear();
                ExtractZip(filename, name);
                Console.Clear();
            }
            else
            {
                HashCheck(url, hash, filename, name);
                Console.Clear();
                ExtractZip(filename, name);
                Console.Clear();
            }

            if (Directory.Exists(dewLoc))
            {
                File.WriteAllText(Path.Combine(dewLoc, "autoexec.cfg"), "Game.SkipLauncher \"1\"");
            }

            Console.WriteLine("Installation complete.\nPress any key to exit.");
            Console.ReadLine();
            if (!Environment.OSVersion.ToString().Contains("Microsoft Windows NT 6.2"))
                ConsoleResize(80, 25);
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
                Console.WriteLine("Download started for: " + filename);
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
        static void HashCheck(string Url, string Hash, string Filename, string Name)
        {
            string dewLoc = Path.Combine(Directory.GetCurrentDirectory(), "ElDewrito");
            string fileloc = Path.Combine(Directory.GetCurrentDirectory(), Filename);

            Stopwatch watcher = Stopwatch.StartNew();

            Console.WriteLine("Hash check started for: " + Filename);
            if (File.Exists(Path.Combine(Directory.GetCurrentDirectory(), Filename)))
            {
                var fileStream = new FileStream(fileloc, FileMode.OpenOrCreate,
                                            FileAccess.Read);
                string compHash = GetChecksumBuffered(fileStream);
                if (compHash == Hash)
                {
                    Console.WriteLine("Hash check finish for: " + Filename + " and succeeded.");
                }
                else
                {
                    Console.WriteLine("Hash check finish for: " + Filename + " and failed.");
                    File.Delete(Path.Combine(Directory.GetCurrentDirectory(), Filename));
                    Console.Clear();
                    Download(Url, Filename);
                    Console.Clear();
                    HashCheck(Url, Hash, Filename, Name);
                    Console.Clear();
                }
            }
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
        static void ConsoleResize(int origWidth, int origHeight)
        {
            Console.SetWindowSize(origWidth, origHeight);
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
    public class ProgressBar : IDisposable, IProgress<double>
    {
        private const int blockCount = 100;
        private readonly TimeSpan animationInterval = TimeSpan.FromSeconds(1.0 / 8);
        private const string animation = @"|/-\";

        private readonly Timer timer;

        private double currentProgress = 0;
        private string currentText = string.Empty;
        private bool disposed = false;
        private int animationIndex = 0;

        public ProgressBar()
        {
            timer = new Timer(TimerHandler);

            // A progress bar is only for temporary display in a console window.
            // If the console output is redirected to a file, draw nothing.
            // Otherwise, we'll end up with a lot of garbage in the target file.
            if (!Console.IsOutputRedirected)
            {
                ResetTimer();
            }
        }

        public void Report(double value)
        {
            // Make sure value is in [0..1] range
            value = Math.Max(0, Math.Min(1, value));
            Interlocked.Exchange(ref currentProgress, value);
        }

        private void TimerHandler(object state)
        {
            lock (timer)
            {
                if (disposed) return;

                int progressBlockCount = (int)(currentProgress * blockCount);
                int percent = (int)(currentProgress * 100);
                string text = string.Format("[{0}{1}] {2,3}% {3}",
                    new string('#', progressBlockCount), new string('-', blockCount - progressBlockCount),
                    percent,
                    animation[animationIndex++ % animation.Length]);
                UpdateText(text);

                ResetTimer();
            }
        }

        private void UpdateText(string text)
        {
            // Get length of common portion
            int commonPrefixLength = 0;
            int commonLength = Math.Min(currentText.Length, text.Length);
            while (commonPrefixLength < commonLength && text[commonPrefixLength] == currentText[commonPrefixLength])
            {
                commonPrefixLength++;
            }

            // Backtrack to the first differing character
            StringBuilder outputBuilder = new StringBuilder();
            outputBuilder.Append('\b', currentText.Length - commonPrefixLength);

            // Output new suffix
            outputBuilder.Append(text.Substring(commonPrefixLength));

            // If the new text is shorter than the old one: delete overlapping characters
            int overlapCount = currentText.Length - text.Length;
            if (overlapCount > 0)
            {
                outputBuilder.Append(' ', overlapCount);
                outputBuilder.Append('\b', overlapCount);
            }

            Console.Write(outputBuilder);
            currentText = text;
        }

        private void ResetTimer()
        {
            timer.Change(animationInterval, TimeSpan.FromMilliseconds(-1));
        }

        public void Dispose()
        {
            lock (timer)
            {
                disposed = true;
                UpdateText(string.Empty);
            }
        }
    }
}