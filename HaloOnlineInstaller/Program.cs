using System;
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

            if (File.Exists(dewLoc))
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
        static void HashCheck(string Url, string Hash, string Filename, string Name)
        {
            string dewLoc = Path.Combine(Directory.GetCurrentDirectory(), "ElDewrito");
            string fileloc = Path.Combine(Directory.GetCurrentDirectory(), Filename);

            Stopwatch watcher = Stopwatch.StartNew();

            if (File.Exists(Path.Combine(Directory.GetCurrentDirectory(), Filename)))
            {
                using (var sha256 = SHA256.Create())
                {
                    using (var stream = File.OpenRead(Filename))
                    {

                        if (Encoding.Default.GetString(sha256.ComputeHash(stream)) == Hash)
                        {
                            stream.Close();
                            ExtractZip(Filename, Name);
                        }
                        else if (Encoding.Default.GetString(sha256.ComputeHash(stream)) != Hash)
                        {
                            stream.Close();
                            File.Delete(Path.Combine(Directory.GetCurrentDirectory(), Filename));
                            Download(Url, Filename);
                            HashCheck(Url, Hash, Filename, Name);
                        }
                    }
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
}