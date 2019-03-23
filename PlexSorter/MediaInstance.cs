using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace PlexSorter
{
    public class MediaInstance
    {
        # region Properties
        public string FullPath { get; set; }

        public string Name { get; set; }

        public FileType MediaFileType  { get; set; }

        public bool Available  { get; set; }

        public string ModifiedName  { get; set; }

        public string ModifiedPath  { get; set; }

        public string MoviesDirectory  { get; set; }

        public string TelevisionDirectory  { get; set; }

        public string MovieYear  { get; set; }

        public string EpisodeInfo  { get; set; }

        public string Title  { get; set; }

        public string PathToNewHome  { get; set; }
        # endregion

        public enum FileType {Television, Movie, Unknown};

        public MediaInstance(string name, string fullPath, string televisionDirectory, string moviesDirectory)
        {
            this.FullPath = fullPath;
            this.Name = name;
            this.MediaFileType = FileType.Unknown;
            this.Available = false;
            this.ModifiedName = null;
            this.TelevisionDirectory = televisionDirectory;
            this.MoviesDirectory = moviesDirectory;
            this.MovieYear = null;
            this.EpisodeInfo = null;
            this.Title = null;
            this.PathToNewHome = null;
        }

        public bool IdentifyFileType()
        {
            bool validVideoFile = false;

            // File types that can be processed - ignores anything else
            if (this.Name.ToLower().EndsWith(".mkv") || this.Name.ToLower().EndsWith(".mp4"))
            {
                validVideoFile = true;
            }
            else
            {
                return validVideoFile = false;
            }

            // This is an attempt to differentiate between television and movie files
            // Assumes television will have season and episode info, i.e. "S01E23"
            string upperName = this.Name.ToUpper();
            Match result = Regex.Match(upperName, @"S[0-9][0-9]E[0-9][0-9]");

            if (result.Success)
            {
                this.MediaFileType = FileType.Television;
            }
            else
            {
                this.MediaFileType = FileType.Movie;
            }

            return validVideoFile;
        }

        internal void MoveFileToDestination()
        {
            if (this.MediaFileType == FileType.Movie)
            {
                string PathToNewHome = Path.Combine(this.MoviesDirectory, this.ModifiedName);

                if (!File.Exists(PathToNewHome))
                {
                    File.Move(this.ModifiedPath, PathToNewHome);
                }
                else
                {
                    Console.WriteLine($"A movie named {this.ModifiedName} already exists in the destination file.");
                    return;
                }
            }
            else if (this.MediaFileType == FileType.Television)
            {
                // Extract season number from "S01E23"-style string
                string[] substrings = this.EpisodeInfo.Split('E');
                substrings[0] = substrings[0].Replace("S", "");
                int season = Int32.Parse(substrings[0]);

                // Build path to destination folder
                string pathToTitleFolder = Path.Combine(this.TelevisionDirectory, this.Title);
                string pathToSeasonFolder = Path.Combine(pathToTitleFolder, $"Season {season}");
                this.PathToNewHome = Path.Combine(pathToSeasonFolder, this.ModifiedName);

                // If season folder doesn't yet exist, create it first or File.Move() will fail
                if (!Directory.Exists(pathToSeasonFolder))
                {
                    Directory.CreateDirectory(pathToSeasonFolder);
                }

                if (!File.Exists(this.PathToNewHome))
                {
                    File.Move(this.ModifiedPath, this.PathToNewHome);
                    Console.WriteLine($"File moved to {PathToNewHome}");
                }
                else
                {
                    Console.WriteLine($"{this.Name} already exists in the destination folder.");
                }
            }
        }

        internal Match MatchMovieYear()
        {
            // Assume movie title will have "1984"-style year
            return Regex.Match(this.Name, @"[0-9][0-9][0-9][0-9]");
        }

        internal Match MatchEpisodeInfo()
        {
            return Regex.Match(this.Name.ToUpper(), @"S[0-9][0-9]E[0-9][0-9]");
        }

        internal bool ProcessVideoFile()
        {
            Match result = null;

            Console.WriteLine($"File type:        {this.MediaFileType}");
            string fileName = this.Name;
            string fileExtension = Path.GetExtension(fileName);
            string directory = Path.GetDirectoryName(this.FullPath);

            if (this.MediaFileType == FileType.Movie)
            {
                // Get year from movie title
                result = MatchMovieYear();
                this.MovieYear = result.Value;
            }
            else if (this.MediaFileType == FileType.Television)
            {
                // Get episode information from title
                result = MatchEpisodeInfo();
                this.EpisodeInfo = result.Value;
            }

            // Clean up file name
            this.Title = CleanUpFileName(result, fileName);

            // Generate final file names
            if (this.MediaFileType == FileType.Movie)
            {
                this.ModifiedName = $"{this.Title} ({this.MovieYear}){fileExtension}";
            }
            else if (this.MediaFileType == FileType.Television)
            {
                this.ModifiedName = $"{this.Title} - {this.EpisodeInfo}{fileExtension}";
            }

            // Update user of recommended changes, and prompt for approval before moving forward
            this.ModifiedPath = Path.Combine(directory, this.ModifiedName);
            Console.WriteLine($"Recommended name: {this.ModifiedName}");
            Console.WriteLine("Accept rename?");
            Console.Write("Y/N:              ");
            string userInput = Console.ReadLine();

            if (userInput.ToLower().Contains("y"))
            {
                // User has approved, so move ahead with modifications
                this.Name = this.ModifiedName;

                if (!File.Exists(this.ModifiedPath))
                {
                    System.IO.File.Move(this.FullPath, this.ModifiedPath);
                    Console.WriteLine($"Changed name to:  {this.Name}");
                    return true;
                }
                else
                {
                    Console.WriteLine($"A file named {this.ModifiedName} already exists in the watch directory.");
                    return false;
                }
            }
            else
            {
                // User did not approve, so abandon processing
                Console.WriteLine("Name unchanged.  File will not be processed further.");
                return false;
            }
        }

        private static string CleanUpFileName(Match result, string fileName)
        {
            string trimmedName = fileName.Substring(0, (result.Index));
            trimmedName = trimmedName.Replace(".", " ");
            trimmedName = trimmedName.Trim();

            String[] trimmedNameSplit = trimmedName.Split(' ');
            string correctedTrimmedName = "";
            for (int i = 0; i < trimmedNameSplit.Length; i++)
            {
                string word = trimmedNameSplit[i].ToLower();
                char[] characters = trimmedNameSplit[i].ToCharArray();
                characters[0] = char.ToUpper(characters[0]);
                trimmedNameSplit[i] = new string(characters);

                correctedTrimmedName += $"{trimmedNameSplit[i]} ";
            }
            correctedTrimmedName = correctedTrimmedName.Trim();

            return correctedTrimmedName;
        }
    }
}
