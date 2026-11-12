# Developing Emily is Away 3 Access

## Building

```bash
dotnet build src/EmilyIsAway3Access -c Release
```

This builds against the game's own DLLs (set `-p:GameDir=...` if the game is elsewhere) and copies
the result into `BepInEx\plugins\EmilyIsAway3Access` in the game folder. Skip the copy with
`-p:SkipDeploy=true`. The game and BepInEx 5 x86 must be installed for the references. Close the
game before building, or the copy can't replace the running DLL.

Release builds for the public repo pass `-p:DevTools=false`, which leaves out `TestDriver.cs`.

Unity 2018's `UnityEngine.dll` facade names mscorlib 4.0, which makes MSBuild throw away every
game reference for a net35 project; the project sets `_FindDependencies=false` to stop that.

## Testing

The test tools (`tools/run-game.ps1` and `src/EmilyIsAway3Access/TestDriver.cs`) are kept in the private development repository and aren't part of the public one.

`tools/run-game.ps1` starts the game with the dev build and sends commands to the test driver
(`src/EmilyIsAway3Access/TestDriver.cs`, dev builds only). Everything the mod says is logged as
`SAY:` lines in `BepInEx\LogOutput.log` (the log is written in bursts; Unity's own log is
`%USERPROFILE%\AppData\LocalLow\KyleSeeley\Facenook - Welcome to Facenook\output_log.txt`).

Test runs are kept away from the player's real game:

- Saves (`Player.json`, `ChapterN.json`), the first-run flag, the desktop picture, web links,
  Steam achievements and stats all go to `%LOCALAPPDATA%\EmilyIsAway3AccessDev\sandbox-<time>`.
  `-New` starts an empty sandbox; `-From <name>` copies `snapshots\<name>`.
- The game is minimized a few seconds after it starts and the window that was in front gets the
  focus back (minimizing before the first scene loads stalls the mod's updates).
- The real keyboard is ignored by the game in a sandboxed run; only the driver's pretend keys
  (`key`, `choose`, `auto`) count. Speech is muted unless `-Loud` is passed.

Driver commands: `wait`, `waitsay <text> [s]`, `items` (what the reader lists), `dump` (every
text, control and picture with its path), `press <words>` (press the first item whose
description contains the words), `type <text>`, `key <KeyCode> [ctrl|shift]`, `choose <1-3>`,
`assist on|off`, `auto <1-3|random|off>` (plays chats and posts by itself), `speed`, `mute`,
`scene`, `call <Type.Method>`, `exportsprites <dir>`, `exportbundles <dir>` (every picture and
the photo list with captions, for writing descriptions), `photos <Type,Type,...>` (open the photo viewer on those photos), `chapter <n> [em|ev]` (start a chapter), `page <WebpageType>` (open a page; only pages the chapter has), `fast on|off` (the game's own quick chats), `requests on` (auto-play also opens notifications, answers pop-ups and quizzes, and opens chat links), `act <label>` (a page button auto-play may press, such as Post Back or Write Status). `tools/contact-sheet.ps1` puts many
exported pictures on one labelled sheet. `tools/signup.txt` is the command list for signing up.

## Engine facts

- Unity 2018.4.30, 32-bit, legacy Mono (.NET 3.5 profile), Unity UI with TextMesh Pro.
  BepInEx's default entry point works.
- `prism.dll` is the 32-bit build, loaded with `LoadLibraryW` and bound with `GetProcAddress`.
- The game is a fake browser (`FF*` classes) showing Facenook pages (`FBBaseWebpage` prefabs,
  `WebpageType`), pop-ups (`FBPopupManager`) and a chat bar (`FC*`). Story chats are the
  `ChapterN...Script` classes; reply text uses `^` for a backspace.
- The game's text links (`TextLinkButton`, `HeaderLinkButton`, `MessageLinkButton`,
  `ChoiceLinkButton`) only react to a click while the mouse is over the link, so `Links.cs`
  does what the click would do.
- While a reply or post is being typed, `InputValues.GetKeysDown` counts any key as typing.
- Photos are `FBPhotoObject` assets in the `photos` asset bundle, with captions and an overlay
  of the player's silhouette when the player is in them. Profile pictures are named by
  `SpriteValues.GetAvatarString`.

## How it works

- `Reader.cs`: browse-mode reading. Walks the screen (or the open pop-up) in reading order and
  lists texts, links, buttons, text boxes and described pictures; Up/Down/Home/End/Enter.
  Reads what's new on the page after a press.
- `Screens.cs`: which window is in front, what to skip, and watchers for new pages, pop-ups,
  installer steps, form errors, the date panel, notifications and the photo viewer.
- `Special.cs`: controls the game builds from several parts (radio buttons, check boxes,
  drop-down lists, sign-up picture choices, profile tabs, chat bar tabs).
- `Labels.cs`, `Links.cs`, `Ui.cs`: names and helpers for the game's UI.
- `Chat.cs`: chat messages, choices, typing, review keys, links, Tab.
- `Typing.cs`: the typing reader shared by chats and posts, and the post boxes.
- `GameKeys.cs`: which keys count as typing, and holding the game's key listeners when the mod
  used a key.
- `Pictures.cs`: finds which photo or profile picture is on screen (from the thumbnail row or viewer that shows it, since some photos share a picture).
- `PhotoText.cs`: the descriptions, by the game's picture name (a photo's main or alternate picture), plus profile pictures and icons.
- `SecretApps.cs`: CarrotVille and the dog, the two secret apps.
- `Settings.cs`, `Keys.cs`, `Speech/`: settings list, shared keys, Prism and `Tell`.

## Releasing

1. Stamp the version in `src/EmilyIsAway3Access/EmilyIsAway3Access.csproj` and `Plugin.cs`, add it to
   `CHANGELOG.md`, and write the release notes in `changes-latest.md`. Commit and push.
2. `powershell -ExecutionPolicy Bypass -File scripts\package-release.ps1` builds `dist\EmilyIsAway3Access.zip`.
3. `gh release create vX.Y.Z --draft --title "Emily is Away 3 Access X.Y" --notes-file changes-latest.md dist/EmilyIsAway3Access.zip`
4. `gh workflow run release.yml -f version=vX.Y.Z`: builds the Loadstone installer
   (EmilyIsAway3AccessInstaller.exe) and `loadstone-release.json`, and publishes the release.

The installer is [Loadstone](https://github.com/bradjrenshaw/loadstone) by Bradley Renshaw. The packages it installs
are in `loadstone-packages/`: the game, BepInEx 5 (32-bit) and the mod.
