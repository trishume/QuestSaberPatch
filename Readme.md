# QuestSaberPatch

A basic custom song patcher for Beat Saber on the Oculus Quest, written in C# and runnable hopefully cross-platform and definitely on macOS with DotNet Core. It can successfully add custom songs to the Quest and should be cross-platform. Based on discoveries both of my own as well as many discoveries made by [@emulamer](https://github.com/emulamer/QuestStopgap) (his code was also an excellent reference!) and others on the Beat Saber modding Discord.

## Features

It can patch a Beat Saber APK with new custom levels in the "Extras" folder, as well as patch the binary to not check the signature on levels. It's very similar to [emulamer's patcher](https://github.com/emulamer/QuestStopgap) with some differences:

- My library is missing some features that @emulamer's patcher has:
    - Adding a Custom Levels collection (mine puts it in "Extras")
    - Automated batch file workflow to do all the steps.
- I have a few extra features his doesn't (note this might be outdated if he adds them):
    - Cover support with proper resizing and mip-mapping for smaller patched APKs and higher frame rate on the song menu screen.
    - Support for different environments as specified by the level: default, nice, triangle and big mirror
    - Recursively searches for levels in a folder, or pass multiple command line arguments for levels to add
- My patcher modifies the APK in-place using zip file manipulation, eliminating the need for an unpacking and repacking step. This is probably faster but I haven't tested.
- My patcher has more of a library structure instead of a program, with the idea that a GUI can use it as a library with minimal amounts of code. `Program.cs` in my patcher is 40 lines, @emulamer's is 600.
- I use a transaction-based design that (hopefully) should gracefully handle broken level data without messing up the APK and just skip over those songs.
- My patcher uses fewer temporary buffers and does less copying, which may eventually lead to higher performance but right now everything is bottlenecked on beatmap serialization.
- Mine has both read and write support for all asset constructs it supports, whereas more things in @emulamer's only have one direction of support.
- My patcher was developed on macOS and definitely works there, @emulamer's patcher might well work on macOS though, although any batch files won't. I haven't tested mine on Windows though but it should work.
- I think my code is cleaner, if you're interested in building on it you can take a look at both of our codebases and see which one you like.

## How to use it

**DO NOT ASK ME FOR HELP USING THIS UNLESS YOU HAVE POSTING PERMISSION IN #quest-mod-dev ON DISCORD**. While it has progressed from programmers-only to power-users-only it's still not an easy process. You at least need to be comfortable with the command line. If anything goes wrong while you try to follow this process, I disclaim liability, do this at your own risk.

### Patching (do this whenever you want to add levels)

1. Convert your levels to the new format using [songe-converter](https://github.com/lolPants/songe-converter), look for the executable for your platform on the Releases page to download. Note this works in-place so maybe backup your levels first!
2. Run `dotnet run -p app/app.csproj <path to APK to modify> <paths to folder with levels to add>...`. It will patch your APK with all levels it can find recursively in the folders you can provide (it also works if you pass level folders directly). It should gracefully handle levels that are already patched in, including hopefully ones using @emulamer's patcher, ignoring them. Note that it modifies the APK in place and doesn't create a new one, so make sure you have a separate backup original copy!
3. Use [uber-apk-signer](https://github.com/patrickfav/uber-apk-signer) to sign the patched APK, you'll need Java installed for this. I use it like `java -jar uber-apk-signer.jar -a base_patched.apk`.
4. Use `adb install -r {patched debug signed apk path}` to install the patched signed new APK that the signer creates replacing the old APK.

### Initial setup (do this once)

1. Install .NET Core: <https://dotnet.microsoft.com/download> (for my patcher) and Java (for the signer, on Windows make sure it's 64-bit Java or the signer may not work). You may be able to avoid installing dotNET by using one of the self-contained releases on the [Releases page](https://github.com/trishume/QuestSaberPatch/releases) and following the instructions on the release, this may not have the newest features though.
2. Install `adb`: <https://developer.android.com/studio/command-line/adb>
3. Put your Oculus Quest in Developer Mode by getting your Oculus account turned into a developer account.
4. Use `adb pull /data/app/com.beatgames.beatsaber-1/base.apk {location you want to put it}` to grab the APK off the device
5. MAKE A BACKUP COPY OF THE ORIGINAL APK FILE AND DON'T EVER MODIFY THE BACKUP
6. Installing the first time requires uninstalling the app, so use an ADB file browser like [adbLink](https://www.jocala.com/) to find files you want to make backups of. Use `adb pull` to grab them off the device. Look in `/sdcard/Android/obb/com.beatgames.beatsaber/` for DLC downloads and make sure to get `/sdcard/Android/data/com.beatgames.beatsaber/files/PlayerData.dat` (local scores) and `/sdcard/Android/data/com.beatgames.beatsaber/files/settings.cfg` (settings). Keep track of where each file you pull came from!
7. Use the patcher on an APK and sign the resulting JAR (see above section)
8. Use `adb uninstall com.beatgames.beatsaber` to uninstall (MAKE SURE YOU HAVE YOUR BACKUP APK AND DATA)
9. Use `adb install {patched apk path}` to install the patched version of Beat Saber
10. Use `adb push` to restore all the backup data files you made. You may need to launch the app once in its blank state for some of the directories to be created, then quit it and then restore.

### Removing songs

Removing songs is fundamentally possible and not that hard to implement but I haven't done it yet. Right now if you want to remove songs just make a new COPY of your backup APK, patch that with all the songs you still want, and then install it.

### It's throwing errors!

If you get unhandled exceptions when trying to patch, maybe something like `System.IO.InvalidDataException: End of Central Directory record could not be found.`, this might happen sometimes and I'm not sure why, the recently added transactions feature might have fixed it but I'm not sure. Anyhow restoring your patching APK by making a **new copy** from your IMPORTANT ORIGINAL BACKUP COPY and patching that should hopefully fix the errors.

## Roadmap

Things I'm planning on doing but may or may not get around to include the following. I'd be open to accepting contributions or collaborating as long as you let me know what you want to work on so we don't collide. Not necessarily in order but approximately so:

- Custom level collections
- Doing my own BinaryFormatter output so that it's much faster
- Support removing songs
- Publish library as a NuGet package
- Testing with large numbers of added songs
- GUI integration

## Based On

- @emulamer's binary patch to disable the beatmap signature check.
- @emulamer's discovery that the beatmaps are formatted with DeflateStream and BinaryFormatter.
- @emulamer's code for ideas on how to do various things. I didn't copy any of his code except for name/field and enum definitions for the Beat Saber types, and a couple single line snippets so that I can match his conventions for things like level IDs and asset names.
- My own work to understand the Unity Assets file format used by Beat Saber, with heavy reference to the code of <https://github.com/Perfare/AssetStudio> to understand the different fields of the standard Unity parts, as well as copying some of their binary reader/writer extension methods (with citations in the code).
- @raftario for setting up cross-platform CI builds
- Many conversations with people on the Beat Saber modding Discord.



