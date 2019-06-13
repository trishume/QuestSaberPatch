# QuestSaberPatch

A custom song patcher for Beat Saber on the Oculus Quest, written in C# and runnable cross-platform on macOS, Windows and Linux. It can successfully add custom songs to the Quest as well as create custom packs and saber colors with the advanced JSON interface. Based on discoveries both of my own as well as many discoveries made by [@emulamer](https://github.com/emulamer/QuestStopgap) (his code was also an excellent reference!), with lots of code contributed by [@sc2ad](https://github.com/sc2ad), and information from others on the Beat Saber modding Discord.

## Features

It can patch a Beat Saber APK with new custom levels, as well as patch the binary to not check the signature on levels. It's very similar to [emulamer's patcher](https://github.com/emulamer/QuestStopgap) with some extra features and some missing features, what specific features are different changes very frequently and I can't hope to keep the readme completely up to date. Here are some of the features of QuestSaberPatch:

- Provides an advanced library and JSON interface with additional fancy features:
    - Multiple custom song packs with arbitrary names, covers and contents
    - Syncing that automatically removes old songs, uploads new ones and replaces pack contents
    - Custom color support
    - Custom text replacement
- Built and tested on .NET Core with releases on Windows, macOS and Linux.
- Modifies and signs the APK in-place using zip file manipulation, eliminating the need for an unpacking and repacking step.
- Has a library structure instead of a program, with the idea that a GUI can use it as a library with minimal amounts of code.
- I use a transaction-based design that (hopefully) should gracefully handle broken level data without messing up the APK and just skip over those songs.
- I've focused on code quality from the start. Emulamer's started off quite messy but he's been gradually improving it, if you're interested in building on it you can take a look at both of our codebases and see which one you like.

## How to use it

QuestSaberPatch is the backend for the Beat Saber modding support in [SideQuest](https://github.com/the-expanse/SideQuest). I highly recommend SideQuest as the preferred way of using QuestSaberPatch. It's what I personally use.

### Command line installation instructions

If for some reason you can't or don't want to use SideQuest, you can use the command line version of QuestSaberPatch by following these instructions.

Don't ask me for help with this process unless you're a programmer looking to integrate QuestSaberPatch into a GUI. While it has progressed from programmers-only to power-users-only it's still not an easy process. You at least need to be comfortable with the command line. If anything goes wrong while you try to follow this process, I disclaim liability, do this at your own risk.

### Patching (do this whenever you want to add levels)

1. Convert your levels to the new format using [songe-converter](https://github.com/lolPants/songe-converter), look for the executable for your platform on the Releases page to download. Note this works in-place so maybe backup your levels first!
2. Run `dotnet run -p app/app.csproj <path to APK to modify> <paths to folder with levels to add>...`.
    - **If you're using a self-contained build from the Releases page** then you'll run the `app` executable instead of the `dotnet run -p app/app.csproj` part.
    - This will patch your APK with all levels it can find recursively in the folders you can provide (it also works if you pass level folders directly).
    - It should gracefully handle levels that are already patched in, including hopefully ones using @emulamer's patcher, ignoring them.
    - Note that it modifies the APK in place and doesn't create a new one, so make sure you have a separate backup original copy!
    - This now also signs the APK with a debug certificate in place thanks to [@emulamer's signer](https://github.com/emulamer/Apkifier).
4. Use `adb install -r {patched apk path}` to install the patched signed new APK that the signer creates replacing the old APK.

### Initial setup (do this once)

1. Download one of the self-contained releases on the [Releases page](https://github.com/trishume/QuestSaberPatch/releases). This may not have the newest features though, if you want those you can build from source by installing .NET Core: <https://dotnet.microsoft.com/download>
2. Install `adb`: <https://developer.android.com/studio/command-line/adb>
3. Put your Oculus Quest in Developer Mode by getting your Oculus account turned into a developer account.
4. Use `adb pull /data/app/com.beatgames.beatsaber-1/base.apk {location you want to put it}` to grab the APK off the device
5. MAKE A BACKUP COPY OF THE ORIGINAL APK FILE AND DON'T EVER MODIFY THE BACKUP
6. Installing the first time requires uninstalling the app, so use an ADB file browser like [adbLink](https://www.jocala.com/) to find files you want to make backups of. Use `adb pull` to grab them off the device. Look in `/sdcard/Android/obb/com.beatgames.beatsaber/` for DLC downloads and make sure to get `/sdcard/Android/data/com.beatgames.beatsaber/files/PlayerData.dat` (local scores) and `/sdcard/Android/data/com.beatgames.beatsaber/files/settings.cfg` (settings). Keep track of where each file you pull came from!
7. Use the patcher on an APK (see above section)
8. Use `adb uninstall com.beatgames.beatsaber` to uninstall (MAKE SURE YOU HAVE YOUR BACKUP APK AND DATA)
9. Use `adb install {patched apk path}` to install the patched version of Beat Saber
10. Use `adb push` to restore all the backup data files you made. You may need to launch the app once in its blank state for some of the directories to be created, then quit it and then restore.

### Removing songs

Removing songs is only possible using the advanced JSON interface described below. Right now if you want to remove songs just make a new COPY of your backup APK, patch that with all the songs you still want, and then install it.

### It's throwing errors!

If you get unhandled exceptions when trying to patch, maybe something like `System.IO.InvalidDataException: End of Central Directory record could not be found.`, this might happen sometimes and I'm not sure why, the recently added transactions feature might have fixed it but I'm not sure. Anyhow restoring your patching APK by making a **new copy** from your IMPORTANT ORIGINAL BACKUP COPY and patching that should hopefully fix the errors.

### 2GB size limit: App won't launch

If the resulting APK file the patcher produces is bigger than 2.1GB (the maximum size of a signed 32 bit integer) the APK will install but not launch. This puts a limit on how many songs you can add.

## Advanced Command Line App For GUIs

Because of the fact that this is a library, I could easily make a separate executable with an advanced JSON-based interface, intended for GUIs written in other languages to easily script.

You can check out the source in `jsonApp2/Program.cs`, but the way it works is that you give it a JSON dictionary of command info as STDIN input and it executes the command and gives you back a single line JSON dictionary as output.

It works on a synchronization model and supports custom packs, you can just tell it what songs and packs you want, and it will patch the APK to make that the current state. It removes old songs and packs that are no longer needed. The resulting packs and levels should be the same for a given invocation regardless on whether you run it on an upatched APK or a previously patched one. It even removes old-school custom songs from the Extras collection so you can use it on an APK patched with an older version of QuestSaberPatch.

The input format is as follows, except being collapsed onto a single line is mandatory for real input:

```js
{
  "apkPath": "/Users/tristan/BeatSaber/base_testing.apk",
  // If true, will patch the signature check in the code, this only needs to be done
  // once per APK but you can have it always true at a slight performance cost
  "patchSignatureCheck": true,
  // If true, will sign the APK, after it does anything else
  "sign": true,
  // Each dictionary item is a levelID:levelFolder pair that will be installed if
  // they aren't already present. The levelID can be any string you want but it must
  // be globally unique across all songs you want to install, and all built in Beat
  // Saber songs. See the output, which can return a list of installed levelIDs.
  // All installed custom levels not present here will be removed.
  //
  // This is what controls what assets are put in the APK, if you put a level here
  // and don't reference it from a pack, it will still be installed, it just won't be
  // accessible.
  "levels": {
    "BUBBLETEA": "testdata/bubble_tea_song"
  },
  // This controls the level packs that will be displayed in the selector, in which
  // order, how they show up and what songs are in them.
  "packs": [
    {
      // Must be unique between packs but doesn't need to be consistent
      "id": "CustomLevels1",
      // Display name of the pack
      "name": "Custom Levels",
      // Image file for the cover that will be displayed for the pack
      "coverImagePath": "testdata/bubble_tea_song/cover.jpg",
      // List of level IDs in the pack in the order you want them displayed.
      // Each levelID can be in multiple packs if you want.
      "levelIDs": ["BUBBLETEA"],
    }
  ],
  // This attribute controls custom saber colors.
  // If the entire "colors" attribute is missing or null, colors won't be updated
  "colors": {
    // A is the red/left hand by default, but left-handed people might use the setting to switch hands
    "colorA": {"r": 0.941176, "g": 0.188235, "b": 0.75, "a": 1.0},
    // null for either resets to the default color for that saber
    "colorB": null,
  },
  // if null or missing, doesn't replace text, if non-null but even if an
  // empty dictionary, adds usernames of all the Quest Modders to the credits
  "replaceText": {
    // See https://github.com/sc2ad/QuestModdingTools/blob/master/BeatSaberLocale.txt for
    // what keys are available and what text they start with
    "BUTTON_PLAY": "GO!",
  }
}
```

And after running each command it will return an output JSON line that is in this format but all on one line:

```js
{
  // Just mirrors the patchSignatureCheck input
  "didSignatureCheckPatch":true,
  // Just mirrors the sign input
  "didSign":true,
  // All levelIDs present in the APK after the command finished
  "presentLevels":["100Bills","AngelVoices","BalearicPumping","BeThereForYou","BeatSaber","Breezer","CommercialPumping","CountryRounds","CrabRave","Elixia","INeedYou","Legend","LvlInsane","OneHope","PopStars","RumNBass","TurnMeOn","UnlimitedPower","BUBBLETEA"],
  // All the levelIDs successfully installed by the command
  "installedLevels":["BUBBLETEA"],
  // Custom levels that used to be in the APK but were removed
  "removedLevels":["OldCustomSongLevelID"],
  // Level IDs referenced in packs that weren't installed so couldn't be added.
  // This could be because the levelID wasn't included in `levels` or because
  // the level wasn't installed because it was invalid.
  "missingFromPacks":[],
  // All the levels in ensureInstalled that weren't installed and the reason. The
  // reason is an error message about the level being invalid such as missing a
  // file or the JSON being incorrect.
  "installSkipped":{"BUBBLETEA":"Invalid level JSON: some error"},
  // This will be null if `colors` was null. Otherwise it will contain the resulting
  // saber colors. Below is the result on a fresh APK with both replacements null,
  // i.e. these are the default colors in case they are useful for making a GUI
  "newColors":{
    "colorA":{"r":0.9411765,"g":0.1882353,"b":0.1882353,"a":1.0},
    "colorB":{"r":0.1882353,"g":0.619607866,"b":1.0,"a":1.0}
  },
  // True if text replacement ran, mirrors `replaceText`
  "didReplaceText":true,
  // This should be null unless something screws up, in which case it will be a
  // string with the exception message and backtrace, make sure to check this and
  // preferably surface it somehow or at least log it.
  "error":null
}
```

This interface should be present in builds past `v0.6` as the `jsonApp2` executable, and runnable from source with `dotnet run -p jsonApp2/jsonApp2.csproj`.

### Manual Use

If you want to use the new features to for example change your custom colors but don't want to wait for or use a GUI tool. You can manually edit a JSON file and then run the tool with it. Check out `testdata/sample_invocation_v2.json` for a starter JSON file and you can run it like this (or with the `jsonApp2` binary in a release):

```bash
dotnet run -p jsonApp2/jsonApp2.csproj < testdata/sample_invocation_v2.json
```

### Old Advanced JSON interface

An older version of the advanced JSON interface is still available in the builds and source as `jsonApp`. It's the one used by the original SideQuest integration and is kept around for compatibility.

You can read about its interface in [an old version of the Readme](https://github.com/trishume/QuestSaberPatch/blob/847babac5cf883d4650dcc6c9818cf1b085a69f9/Readme.md). But I don't recommend using it now that the newer version is out.

## Roadmap

Things I'm planning on doing but may or may not get around to include the following. I'd be open to accepting contributions or collaborating as long as you let me know what you want to work on so we don't collide. Not necessarily in order but approximately so:

- Switching to a new ZIP library to hopefully fix corruption errors
- Doing my own BinaryFormatter output so that it's much faster
- Publish library as a NuGet package (contact me if you personally want this)
- Testing with large numbers of added songs

## Credits

- [@sc2ad](https://github.com/sc2ad) for contributing tons of code including custom color, custom text and custom pack support.
- @emulamer's binary patch to disable the beatmap signature check.
- @emulamer's discovery that the beatmaps are formatted with DeflateStream and BinaryFormatter.
- @emulamer's code for ideas on how to do various things. I didn't copy any of his code except for name/field and enum definitions for the Beat Saber types, and a couple single line snippets so that I can match his conventions for things like level IDs and asset names.
- @emulamer's [all-C# APK signer](https://github.com/emulamer/Apkifier)
- My own work to understand the Unity Assets file format used by Beat Saber, with heavy reference to the code of <https://github.com/Perfare/AssetStudio> to understand the different fields of the standard Unity parts, as well as copying some of their binary reader/writer extension methods (with citations in the code).
- @raftario for setting up cross-platform CI builds
- Many conversations with people on the Beat Saber modding Discord.



