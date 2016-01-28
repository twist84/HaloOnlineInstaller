using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using CG.Web.MegaApiClient;
using ICSharpCode.SharpZipLib.Zip;
using Newtonsoft.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Installer
{
    class Program
    {
        static void Main()
        {
            string dewLoc = Path.Combine(Directory.GetCurrentDirectory(), "ElDewrito");
            System.Net.WebClient wc = new System.Net.WebClient();
            dynamic dew = JsonConvert.DeserializeObject(wc.DownloadString("http://thetwist84.github.io/HaloOnlineModManager/game/game.json")); 
            MegaApiClient mega = new MegaApiClient();
            mega.LoginAnonymous();
            string name = dew["base"].Name;
            string filename = dew["base"].Filename;
            string url = dew["base"].Url;
            Uri uri = new Uri(url);
            Stopwatch watcher = Stopwatch.StartNew();
            FastZip fastZip = new FastZip();
            string filter = null;

            if (!File.Exists(Path.Combine(Directory.GetCurrentDirectory(), filename)))
            {
                Task t = mega.DownloadFileAsync(uri, Path.Combine(Directory.GetCurrentDirectory(), filename));
                using (var progress = new ProgressBar())
                {
                    while (!t.IsCompleted)
                    {
                        progress.Report((double)mega.Progress / 100);
                    }
                }
                mega.Logout();
                if (File.Exists(Path.Combine(Directory.GetCurrentDirectory(), filename)))
                {
                    Console.WriteLine("Extraction started for: " + name);
                    watcher.Restart();
                    fastZip.ExtractZip(filename, dewLoc, filter);
                    watcher.Stop();
                    Console.WriteLine("Extraction finished for: " + name + " in {0}.\n ", watcher.Elapsed);
                }
            }
            else if (File.Exists(Path.Combine(Directory.GetCurrentDirectory(), filename)))
            {
                Console.WriteLine("Extraction started for: " + name);
                watcher.Restart();
                fastZip.ExtractZip(filename, dewLoc, filter);
                watcher.Stop();
                Console.WriteLine("Extraction finished for: " + name + " in {0}.\n ", watcher.Elapsed);
            }

            if (!File.Exists(Path.Combine(dewLoc, "autoexec.cfg")))
            {
                File.WriteAllText(Path.Combine(dewLoc, "autoexec.cfg"), "Game.SkipLauncher \"1\"");
            }

            Console.WriteLine("Installation complete.\nPress any key to exit.");
            Console.ReadLine();
        }
    }
}
