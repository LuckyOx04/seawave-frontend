# Seawave App

This is a desktop app that acts as a frontend for a music streaming platform, connecting to Seawave API. It registrates users, keeps track of music files, uploads and playlists, querries searches, streams a music file to the user.
It also plays local files, including .cue files, and can make local playlists.

## Tech Stack
- .NET Core 10
- AvaloniaUI Cross Platform (Only Desktop is used)
- LibVLCsharp
- SQLite
- TagLibSharp
- CommunityToolkit.MVVM

## How to Run Locally (For Developers)

### Prerequisites
- .NET 10 SDK or Later
- AvaloniaUI (mainly for editor syntax highlighting and completion)

### Installation & Run
1. Clone the repository:
```bash
git clone https://github.com/LuckyOn04/seawave-frontend.git
```
2. Navigate to the project directory where SeawaveApp.sln file is located and restore dependencies:
```bash
dotnet restore
```
4. Run the desktop application
```bash
dotnet run --project SeawaveApp.Desktop
```

## How to Run Locally for Linux (Non Developers)

### Installation & Run
1. Download the **seawave-desktop-linux-x64.tar.gz** file

2. Extract the file
```bash
tar -xzvf seawave-desktop-linux-x64.tar.gz
```
3. Go in the **seawave-desktop-linux-x64** folder
```bash
cd seawave-desktop-linux-x64
```
4. Run the program
```bash
./SeawaveApp.Desktop
```

## How to Run Locally for Windows (Non Developers)

### Installation & Run
1. Download the **seawave-desktop-win-x64.zip** file

2. Extract the file

3. Go in the **seawave-desktop-win-x64** folder

4. Run the program by clicking **SeawaveApp.Desktop.exe**

### NOTE: The app was made in Linux and the Windows program was not tested in a Windows environment. There may be problems with it.
