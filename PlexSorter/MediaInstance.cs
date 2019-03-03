﻿using System;
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
        # region Fields
        private string fullPath;
        private string name;
        private FileType mediaFileType;
        private bool available;
        private string modifiedName;
        private string modifiedPath;
        private string moviesDirectory;
        private string televisionDirectory;
        private string movieYear;
        private string episodeInfo;
        private string title;
        private string pathToNewHome;
        # endregion

        #region Properties
        public string FullPath
        {
            get
            {
                return fullPath;
            }
            set
            {
                fullPath = value;
            }
        }

        public string Name
        {
            get
            {
                return name;
            }
            set
            {
                name = value;
            }
        }

        public FileType MediaFileType
        {
            get
            {
                return mediaFileType;
            }
            set
            {
                mediaFileType = value;
            }
        }

        public bool Available
        {
            get
            {
                return available;
            }
            set
            {
                available = value;
            }
        }

        public string ModifiedName
        {
            get
            {
                return modifiedName;
            }
            set
            {
                modifiedName = value;
            }
        }

        public string ModifiedPath
        {
            get
            {
                return modifiedPath;
            }
            set
            {
                modifiedPath = value;
            }
        }

        public string MoviesDirectory
        {
            get
            {
                return moviesDirectory;
            }
            set
            {
                moviesDirectory = value;
            }
        }

        public string TelevisionDirectory
        {
            get
            {
                return televisionDirectory;
            }
            set
            {
                televisionDirectory = value;
            }
        }

        public string MovieYear
        {
            get
            {
                return movieYear;
            }
            set
            {
                movieYear = value;
            }
        }

        public string EpisodeInfo
        {
            get
            {
                return episodeInfo;
            }
            set
            {
                episodeInfo = value;
            }
        }

        public string Title
        {
            get
            {
                return title;
            }
            set
            {
                title = value;
            }
        }

        public string PathToNewHome
        {
            get
            {
                return pathToNewHome;
            }
            set
            {
                pathToNewHome = value;
            }
        }
        # endregion

        public enum FileType {Television, Movie, Unknown};

        public MediaInstance(string name, string fullPath, string televisionDirectory, string moviesDirectory)
        {
            this.FullPath = fullPath;
            this.Name = name;
            this.MediaFileType = FileType.Unknown;
            this.available = false;
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

            if (this.Name.EndsWith(".mkv") || this.Name.EndsWith(".mp4"))
            {
                validVideoFile = true;
            }
            else
            {
                return validVideoFile = false;
            }

            Match result = Regex.Match(this.Name, @"S[0-9][0-9]E[0-9][0-9]");

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
                string[] substrings = this.EpisodeInfo.Split('E');
                substrings[0] = substrings[0].Replace("S", "");
                int season = Int32.Parse(substrings[0]);

                string pathToTitleFolder = Path.Combine(this.TelevisionDirectory, this.Title);
                string pathToSeasonFolder = Path.Combine(pathToTitleFolder, $"Season {season}");
                this.PathToNewHome = Path.Combine(pathToSeasonFolder, this.ModifiedName);

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
            return Regex.Match(this.Name, @"[0-9][0-9][0-9][0-9]");
        }

        internal Match MatchEpisodeInfo()
        {
            return Regex.Match(this.Name, @"S[0-9][0-9]E[0-9][0-9]");
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
                result = MatchMovieYear();
                this.MovieYear = result.Value;
            }
            else if (this.MediaFileType == FileType.Television)
            {
                result = MatchEpisodeInfo();
                this.EpisodeInfo = result.Value;
            }

            string trimmedName = fileName.Substring(0, (result.Index));
            trimmedName = trimmedName.Replace(".", " ");
            trimmedName = trimmedName.Trim();
            this.Title = trimmedName;

            if (this.MediaFileType == FileType.Movie)
            {
                this.ModifiedName = $"{trimmedName} ({this.MovieYear}){fileExtension}";
            }
            else if (this.MediaFileType == FileType.Television)
            {
                this.ModifiedName = $"{trimmedName} - {this.EpisodeInfo}{fileExtension}";
            }

            this.ModifiedPath = Path.Combine(directory, this.ModifiedName);
            Console.WriteLine($"Recommended name: {this.ModifiedName}");
            Console.WriteLine("Accept rename?");
            Console.Write("Y/N:              ");
            string userInput = Console.ReadLine();

            if (userInput.ToLower().Contains("y"))
            {
                this.Name = this.ModifiedName;
                // TODO: handle renamed file already existing in Watch directory
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
                Console.WriteLine("Name unchanged.  File will not be processed further.");
                return false;
            }
        }
    }
}