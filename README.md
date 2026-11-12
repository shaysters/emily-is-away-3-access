# Emily is Away 3 Access

Emily is Away 3 Access is a mod that makes Emily is Away <3, the social-network story game by Kyle Seeley, playable with a screen reader. It reads every page of Facenook (the game's 2008 Facebook) like a web page you can arrow through: profiles, walls, the news feed, notes, events, quizzes and pop-ups. It reads every chat message, the reply choices, what your character types (and erases), notifications and friend requests, and it describes the photos and profile pictures. The installer, the sign-up form, menus and settings all work from the keyboard. If your screen reader has a braille display connected, everything the mod reads goes to braille too.

It works with NVDA, JAWS and other Windows screen readers, and falls back to Windows' built-in voices if no screen reader is running.

- Join the Discord to chat, get help or report problems: <https://discord.gg/KNDXhztGGK>
- Support Shaysters on Ko-fi: <https://ko-fi.com/shaysters>

## Requirements

- Emily is Away <3 for Windows, from Steam (<https://store.steampowered.com/app/978460/Emily_is_Away_3/>) or itch.io (<https://kyleseeley23.itch.io/emilyisaway3>).
- A screen reader such as NVDA or JAWS (optional; without one, the mod uses Windows' built-in speech).

## Installing

1. Close Emily is Away <3 if it's running.
2. Download **EmilyIsAway3AccessInstaller.exe** from the [latest release](https://github.com/shaysters/emily-is-away-3-access/releases/latest).
3. Run it. It finds your Steam copy of Emily is Away <3 by itself. If it can't, use the Browse button next to the game folder and choose the folder that contains `Emily3.exe`.
4. Choose **Install**. The installer also installs BepInEx, the mod loader the mod needs.
5. Start Emily is Away <3 from Steam as usual. The mod says "Emily is Away 3 Access 1.0 loaded".

If the installer won't start and mentions `VCRUNTIME140.dll`, install the [Microsoft Visual C++ Redistributable (x64)](https://aka.ms/vs/17/release/vc_redist.x64.exe) and try again.

### Copies from itch.io

The mod also supports the itch.io copy of Emily is Away <3. The installer finds Steam copies by itself; for the itch.io copy, use the Browse button and choose the folder that contains `Emily3.exe`. If you use the itch app, the game is in a folder named after it inside `%APPDATA%\itch\apps` (typing `%APPDATA%` in File Explorer's address bar takes you to your AppData\Roaming folder). Without Steam, there are no Steam achievements to announce; everything else is the same.

### Updating and uninstalling

Keep the installer. Run it again to update to the newest version, or to uninstall. Close the game first. When a newer version is out, the mod says so on the main menu (you can turn that off in the settings).

### Installing by hand

If you'd rather not use the installer:

1. Install BepInEx 5.4.23.5 for **32-bit** Windows: download `BepInEx_win_x86_5.4.23.5.zip` from the [BepInEx releases](https://github.com/BepInEx/BepInEx/releases/tag/v5.4.23.5) and extract it into the game folder.
2. Download **EmilyIsAway3Access.zip** from the same release as the installer and extract it into the game folder. It puts the mod in `BepInEx\plugins`.

The game folder is the one that contains `Emily3.exe`. On a default Steam install that's:

```
C:\Program Files (x86)\Steam\steamapps\common\Emily is Away 3
```

To uninstall a manual install, delete the `BepInEx\plugins\EmilyIsAway3Access` folder and the files `EmilyIsAway3Access-README.md` and `EmilyIsAway3Access-CHANGELOG.md` from the game folder. To remove BepInEx as well, also delete the `BepInEx` folder, `winhttp.dll`, `doorstop_config.ini`, `.doorstop_version` and `changelog.txt`. The mod doesn't change any of the game's own files.

## Playing

The game is a fake web browser showing Facenook. Each page is like a web page: the mod reads it when it loads, and you can read it again item by item with the arrow keys, the way a screen reader's browse mode works. Chats happen in the chat bar at the bottom of the browser, like Facebook chat in 2008.

- **First start:** the game opens with a fake installer. Each page is read. The license has a check box, the setup page has radio buttons for the system style and the desktop picture (each picture is described). Export really does save the picture to your Windows desktop.
- **Pages:** when a page finishes loading you hear its name and then the page from the top. Up and Down move through it one item at a time: text, links, buttons, text boxes and described pictures. Enter presses a link or button. When pressing something changes the page without loading a new one (a post box opening, the main menu's Photos), the new part is read.
- **The 2020 home page:** react to each post with Like, Love, Haha, Wow, Sad, Angry or Skip. Each new post is read when it appears.
- **Signing up:** type a username, first and last name, or use the Randomize links. Each letter is read back as you type. The birthday is two drop-down lists: press Enter on one, pick from the list, and you're back on the list button, which says what you picked. If something's missing, the error is read. Then pick a profile picture; each one is described.
- **Pop-ups:** friend requests, photos, events and the other pop-ups are read when they open, and the arrow keys stay inside them. When one closes you're back where you were.
- **Photos:** every photo in the game is described, with its caption and whether you're in it, both as a thumbnail and in the photo viewer. In the viewer, Left and Right go to the previous and next photo. The game draws people as flat, one-colour shapes (Emily green, Evelyn purple, Mat orange and later teal, Kelly yellow, Anna pink, Jeff and Steve red, you blue, strangers grey); the descriptions say who's who.
- **Profile pictures:** each one says whose it is and what it shows, including the greyed-out and moustache versions.
- **Notes and quizzes:** when someone tags you in a note, press Copy Note to fill in your own. Each question is read with your three answer choices; pick and type the answer like a post. When it's finished, press Enter to post it. Quizzes ask for permission first (press Allow); each question is read as it appears, the answers are radio buttons, then press Next.
- **Gifts:** on a friend's birthday their wall has a row of gifts (radio buttons) and a Pick Gift button.
- **Logging in as Mat:** in chapter 4 you log out and back in as Mat. The log-in page has Username and Password boxes; Mat tells you both in the chat (the chat keys repeat them).
- **Events:** an event page lists who's attending, maybe, not attending and still to answer, then the Attending, Maybe and Not Attending buttons and the event's wall.
- **After the story:** the credits and the Thanks for playing page are read, then Your Story lists the choices you made. Back on the main menu, the new chapter's posts are read one by one as they're added. The menu's links show your Timeline (with filters), Photos, Playlists, Storyline and Secrets, where you can type a secret code.
- **Ending a chapter:** when the story is done, the Account list in the chat bar has a Log Out button that ends the chapter.
- **Secret apps:** in CarrotVille, pick the watering can or a seed (radio buttons), then press Enter on a plot. Each plot says whether it's empty, watered, planted, growing or ready to harvest, and you hear when a crop is ready. In "Can you pet the Dog?", press Enter on Pet the dog.
- **Notifications:** new notifications in the chat bar are read as they arrive ("Notification: Mat Gursky has sent you a friend request"). The Notifications button in the chat bar opens the list; press Enter on one to act on it.
- **Chat:** every message is read as it arrives ("Mat: yooo abby!!!"), along with "is typing...". Messages in chats you're not looking at are read too, with whose chat they're in (you can turn that off).
- **Replying:** you hear the three choices. Press 1, 2 or 3. Then press any keys to type the reply, one letter per key press, just like the game. Each word is read as it's typed, and erased words too. When the message is finished you hear "Ready to send" and the whole message; press **Enter**.
- **Posts:** status updates, wall posts and comments work the same way: you hear "Write a post" and three choices, pick one, type it with any keys, and press Enter when you hear "Ready to post".
- **Switching chats:** Tab switches between open chats. When no chat is open, Tab opens the one waiting for your reply.
- **Links in chat:** when someone sends a link, press **L** to open it.

## Keys

| Key | Where | What it does |
|---|---|---|
| Up and Down | Pages and pop-ups | Read the previous or next item. |
| Home and End | Pages and pop-ups | Go to the top or bottom. |
| Enter or Space | Pages and pop-ups | Press the link or button you're on. |
| Left and Right | Sliders | Change the value. |
| Left and Right | Photo viewer | Previous or next photo (the game's own keys). |
| 1, 2, 3 | Chat and posts | Pick a reply (the game's own keys). |
| Any key | Chat and posts | Type your reply, one letter per key press (the game's own keys). |
| Enter | Chat and posts | Send your reply when it's ready (the game's own key). |
| Tab | Chat | Switch chats, or open the chat that's waiting. |
| Page Up and Page Down | Chat | Read the previous or next line of the chat. |
| Ctrl+Home and Ctrl+End | Chat | Read the first or latest line. |
| c | Chat and posts | Repeat the reply choices. |
| w | Chat | Which chats are open, and which are waiting for your reply. |
| l | Chat | Open the newest link in the chat, or the one picked with Shift+l. |
| Shift+l | Chat | Step back through older links. |
| i | Chat | Open the profile of the person you're chatting with. |
| r | Everywhere | Repeat the last thing the mod said. |
| h | Everywhere | Hear the list of keys. |
| m | Everywhere | Open or close the mod's settings. |

Notes:

- While a reply or post is being typed (from picking it until it's sent), every key types it, and the mod's keys stay quiet. To use a mod key then, hold Ctrl: Ctrl+C, Ctrl+W, Ctrl+R, Ctrl+M and Ctrl with the arrow keys work. The same goes while you're in a text box. Page Up and Page Down always read the chat.
- Keys that belong to your screen reader or Windows never type your reply: the NVDA key (Insert or Caps Lock), Shift, Ctrl, Alt, the Windows key, Escape, Tab and the F keys.

## Settings

Press **m**, or **Ctrl+M** while you're typing. Up and Down move. Enter, Space, Left or Right changes a setting. Escape or **m** closes the list. The game is paused while the list is open. Changes are saved immediately.

- **Read messages in chats you're not looking at** (on).
- **Read each word while typing a reply** (on).
- **Read erased text** (on).
- **Read the finished message before sending** (on).
- **Read when someone is typing** (on).
- **Read each letter you type in text boxes** (on): if your screen reader already says the keys you type, turn this off.
- **Read each new page when it finishes loading** (on): with this off you only hear the page's name.
- **Read your own messages when sent** (off): with this on, "Sent" is followed by your message.
- **Read when friends come online or go offline** (on).
- **Secret hints, spoilers** (off): the Secrets page in the main menu also lists every secret you haven't found yet, with the codes that unlock it, and the letters for moustache mode.
- **Announce available updates** (on): when a newer version of the mod is out, say so on the main menu. The mod asks GitHub for the latest version once each time the game starts; nothing else is sent.

The game's own settings (sound, type assist, mustache mode, reinstall, reset) are behind the Settings button in the browser bar.

## Troubleshooting

The mod's settings are in `BepInEx\config\EmilyIsAway3Access-settings.txt` in the game folder, and its log is `BepInEx\LogOutput.log`. Everything the mod says is in the log on lines starting with `SAY`. If something goes wrong, include the log when you report it.

- **Achievements:** new Steam achievements are announced, since Steam only shows them as a picture.

To report a problem or suggest something, open an issue on the [GitHub page](https://github.com/shaysters/emily-is-away-3-access/issues) or ask on the Discord.

## Credits

This project was developed by [Shaysters](https://github.com/shaysters). A lot of the code, and the picture descriptions, were written with Claude Code, Anthropic's AI coding assistant.

- Speech and braille through [Prism](https://github.com/ethindp/prism) by Ethin Probst, MPL-2.0.
- Mods are loaded by [BepInEx](https://github.com/BepInEx/BepInEx).

Emily is Away <3 is made by Kyle Seeley. This mod isn't affiliated with or endorsed by Kyle Seeley.

## For developers

Build instructions and how the mod works are in [DEVELOPING.md](DEVELOPING.md).
