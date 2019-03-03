using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PlexSorter
{
    public class Program
    {
        public static ObservableCollection<MediaInstance> activeMedia = new ObservableCollection<MediaInstance>();
        public static string recentFileName;
        public static string WatchDirectory;
        public static string MoviesDirectory;
        public static string TelevisionDirectory;

        static void Main(string[] args)
        {
            Run();
        }

        private static void Run()
        {
            # region Verify Directories
            string[] args = Environment.GetCommandLineArgs();

            // Check that enough parameters were passed
            if (args.Length < 4)
            {
                Console.WriteLine("Must pass: directory to watch, movies directory, television directory");
                return;
            }

            WatchDirectory = args[1];
            MoviesDirectory = args[2];
            TelevisionDirectory = args[3];

            // Check that directories exist
            if (!Directory.Exists(WatchDirectory))
            {
                Console.WriteLine($"Unable to find Watch directory at {WatchDirectory}. Stopping program.");
                return;
            }
            if (!Directory.Exists(MoviesDirectory))
            {
                Console.WriteLine($"Unable to find Movies directory at {MoviesDirectory}. Stopping program.");
                return;
            }
            if (!Directory.Exists(TelevisionDirectory))
            {
                Console.WriteLine($"Unable to find Television directory at {TelevisionDirectory}. Stopping program.");
                return;
            }
            # endregion

            // Begin monitoring Watch directory for changes
            using (FileSystemWatcher watcher = new FileSystemWatcher())
            {
                watcher.Path = WatchDirectory;
                watcher.NotifyFilter = NotifyFilters.LastWrite;
                watcher.Filter = "*.*";
                watcher.Changed += new FileSystemEventHandler(OnChanged);
                watcher.EnableRaisingEvents = true;
                Console.WriteLine("PlexSorter is running!");
                while (true) ;
            }
        }

        private static void OnChanged(object source, FileSystemEventArgs e)
        {
            if (!activeMedia.Any(MediaInstance => MediaInstance.Name == e.Name))
            {
                // Check for duplicate events - FileSystemWatcher repeats a lot of events
                if (e.Name == recentFileName)
                {
                    recentFileName = null;
                    return;
                }
                else
                {
                    recentFileName = e.Name;
                }

                // Add instance to collection
                MediaInstance instance = new MediaInstance(e.Name, e.FullPath, TelevisionDirectory, MoviesDirectory);
                activeMedia.Add(instance);

                // Wait until file is unlocked before proceeding
                WaitForFileAvailability(instance);

                // Make sure file is of a valid type
                if (!instance.IdentifyFileType())
                {
                    activeMedia.Remove(instance);
                    return;
                }

                Console.WriteLine();
                Console.WriteLine($"Input file:       {instance.Name}");

                // Identify media type and rename
                bool processSuccess = instance.ProcessVideoFile();

                // Move file from Watch directory to proper location
                if (processSuccess)
                {
                    instance.MoveFileToDestination();
                }

                // Done processing file, so remove reference to it
                activeMedia.Remove(instance);
            }

        }

        async private static void WaitForFileAvailability(MediaInstance instance)
        {
            // Check that file is not locked (open in another application)
            while (!IsFileReady(instance.FullPath))
            {
                await Task.Delay(1000);
            }

            instance.Available = true;
        }

        private static bool IsFileReady(string fileName)
        {
            bool isReady;

            // Try to open the file
            try
            {
                using (FileStream inputStream = File.Open(fileName,
                    FileMode.Open,
                    FileAccess.Read,
                    FileShare.None))
                    isReady = inputStream.Length > 0;
            }
            catch (Exception)
            {
                isReady = false;
            }

            return isReady;
        }
    }
}
