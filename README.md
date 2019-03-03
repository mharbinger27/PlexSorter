# PlexSorter

Requires three command-line parameters to function properly. 

Format: PlexSorter.exe "1" "2" "3"
Where 1: Absolute path to folder to watch
      2. Absolute path to Movies directory
      3. Absolute path to Television directory

Ideas for future changes: 
1. Many more helpful comments in the code to explain choices
2. Prompt the Plex Media Server to scan for file changes after something has been added
3. Windows Task Scheduled task to run the program at login
4. Simple GUI for users to input their directory paths and create/modify the task
5. Make the program some kind of service that runs, rather than running from a console window
6. Check with thetvdb.com (or similar service) to verify names