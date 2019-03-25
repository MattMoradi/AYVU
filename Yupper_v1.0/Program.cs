using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

using Google.Apis.Auth.OAuth2;
using Google.Apis.Services;
using Google.Apis.Upload;
using Google.Apis.Util.Store;
using Google.Apis.YouTube.v3;
using Google.Apis.YouTube.v3.Data;
using ShellProgressBar;

namespace Yupper_v1._0
{
    class Program
    {
        public static string dirSync;
        public static string vidTitle;
        public static string description;
        public static string tagz;
        public static string privacy;
        public static bool autoTitle;

        [STAThread]

        public static void Main(string[] args)
        {
            //UploadVideo yuds = new UploadVideo();

            Console.WriteLine("Yupper v1.0 - YouTube Directory Syncronizer");
            Console.WriteLine("============================================");
            Console.Write("Select Video Directory To Sync: ");
            dirSync = Console.ReadLine();

            while (!Directory.Exists(dirSync))
            {
                Console.WriteLine(new DirectoryNotFoundException());
                Console.Write("Please select an EXISTING directory to sync: ");
                dirSync = Console.ReadLine();
            }

            Console.WriteLine("Select A Video Titling Procedure:");
            Console.WriteLine("1) Use a custom video title that will be added to all videos in directory.");
            Console.WriteLine("2) Use the file name already associated with the videos in the directory.");
            ConsoleKeyInfo titleChoice = Console.ReadKey();

            if (titleChoice.Key == ConsoleKey.D1)
            {
                Console.WriteLine("Insert Common Title For All Videos: ");
                vidTitle = Console.ReadLine();
                autoTitle = false;
            }
            else if(titleChoice.Key == ConsoleKey.D2)
            {
                autoTitle = true;
            }

            Console.WriteLine("Insert Common Description For All Videos In Directory:");
            description = Console.ReadLine();
            Console.WriteLine("Insert Common Tags For All Videos In Directory:");
            tagz = Console.ReadLine();
            Console.WriteLine("Select One Of The Folling Privacy Levels For All Videos In Direcotry");
            Console.WriteLine("1) Public        2) Private");
            ConsoleKeyInfo privacyChoice = Console.ReadKey();
            if(privacyChoice.Key == ConsoleKey.D1)
            {
                privacy = "public";
            }
            else if(privacyChoice.Key == ConsoleKey.D2)
            {
                privacy = "private";
            }

            try
            {
                new Program().Run().Wait();
            }
            catch (AggregateException ex)
            {
                foreach (var e in ex.InnerExceptions)
                {
                    Console.WriteLine("Error: " + e.Message);
                }
            }

            Console.WriteLine("Waiting For More Videos...");
            Console.ReadLine();
        }

            public async Task Run()
            {
                UserCredential credential;
                using (var stream = new FileStream("client_secrets.json", FileMode.Open, FileAccess.Read))
                {
                    credential = await GoogleWebAuthorizationBroker.AuthorizeAsync(
                        GoogleClientSecrets.Load(stream).Secrets,
                        // This OAuth 2.0 access scope allows an application to upload files to the
                        // authenticated user's YouTube channel, but doesn't allow other types of access.
                        new[] { YouTubeService.Scope.YoutubeUpload },
                        "user",
                        CancellationToken.None
                    );
                }


                var youtubeService = new YouTubeService(new BaseClientService.Initializer()
                {
                    HttpClientInitializer = credential,
                    ApplicationName = Assembly.GetExecutingAssembly().GetName().Name
                });

                var video = new Video();
                video.Snippet = new VideoSnippet();
                video.Snippet.Title = vidTitle;
                video.Snippet.Description = description;
                video.Snippet.Tags = new string[] {tagz};
                video.Snippet.CategoryId = "22"; // See https://developers.google.com/youtube/v3/docs/videoCategories/list
                video.Status = new VideoStatus();
                video.Status.PrivacyStatus = privacy; // or "private" or "public"

                DirectoryInfo d = new DirectoryInfo(dirSync);
                FileInfo[] Files = d.GetFiles("*.mp4");

                foreach (FileInfo file in Files)
                {
                    var filePath = file.FullName;

                    if(autoTitle == true)
                {
                    vidTitle = file.Name;
                }

                    using (var fileStream = new FileStream(filePath, FileMode.Open))
                    {
                        var videosInsertRequest = youtubeService.Videos.Insert(video, "snippet,status", fileStream, "video/*");
                        videosInsertRequest.ProgressChanged += videosInsertRequest_ProgressChanged;
                        videosInsertRequest.ResponseReceived += videosInsertRequest_ResponseReceived;

                        await videosInsertRequest.UploadAsync();
                    }
                }
            }

            void videosInsertRequest_ProgressChanged(Google.Apis.Upload.IUploadProgress progress)
            {
                switch (progress.Status)
                {
                    case UploadStatus.Uploading:
                        Console.WriteLine("{0} bytes sent.", progress.BytesSent);
                        break;

                    case UploadStatus.Failed:
                        Console.WriteLine("An error prevented the upload from completing.\n{0}", progress.Exception);
                        break;
                }
            }

            void videosInsertRequest_ResponseReceived(Video video)
            {
                Console.WriteLine("Video id '{0}' was successfully uploaded.", video.Id);
            }
        }

        /*
        var fileCount = (from file in Directory.EnumerateFiles(@"C:\Users\Mat\Source\Repos\Yupper_v1.0\Yupper_v1.0\bin\Debug", "*.exe", SearchOption.AllDirectories)
                         select file).Count();

        Console.WriteLine(fileCount);
        */

        /*
            DirectoryInfo d = new DirectoryInfo(@"C:\Users\Mat\Source\Repos\Yupper_v1.0\Yupper_v1.0\bin\Debug");//Assuming Test is your Folder
            FileInfo[] Files = d.GetFiles("*.exe"); //Getting Text files
            string str;
            foreach (FileInfo file in Files)
            {
                str = file.FullName;
                Console.WriteLine(str);
                Console.ReadLine();
            }
        */
}
