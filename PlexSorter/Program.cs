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
            string[] args = Environment.GetCommandLineArgs();

            if (args.Length < 4)
            {
                Console.WriteLine("Must pass: directory to watch, movies directory, television directory");
                return;
            }

            WatchDirectory = args[1];
            MoviesDirectory = args[2];
            TelevisionDirectory = args[3];

            using (FileSystemWatcher watcher = new FileSystemWatcher())
            {
                watcher.Path = WatchDirectory;
                watcher.NotifyFilter = NotifyFilters.LastWrite;
                watcher.Filter = "*.*";
                watcher.Changed += new FileSystemEventHandler(OnChanged);
                watcher.EnableRaisingEvents = true;
                Console.WriteLine("PlexSorter is running!");
                //Console.WriteLine("Press 'q' then 'enter' to quit.");
                while ( true ) ;
            }
        }

        private static void OnChanged(object source, FileSystemEventArgs e)
        {
            if (e.ChangeType == WatcherChangeTypes.Changed)
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

                    bool processSuccess = instance.ProcessVideoFile();
                    
                    if (processSuccess)
                    {
                        instance.MoveFileToDestination();
                    }                   

                    activeMedia.Remove(instance);
                }
            }
        }

        async private static void WaitForFileAvailability(MediaInstance instance)
        {
            while (!IsFileReady(instance.FullPath))
            {
                await Task.Delay(1000);
            }

            instance.Available = true;
        }

        private static bool IsFileReady(string fileName)
        {
            bool isReady;

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

        private static void OnRenamed(object source, RenamedEventArgs e) =>
            Console.WriteLine($"File renamed: {e.OldFullPath} renamed to {e.Name}");
    }
}
