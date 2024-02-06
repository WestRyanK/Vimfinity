# Vimfinity

The worst part about Vim is that once you use it, you want to use it everywhere. 
Moving your hand to the arrow keys - or even worse, the mouse - becomes a chore.
Vimify remedies this problem by allowing you to use a lite Vim mode anywhere on your desktop. 

I originally implemented Vimfinity's functionality using an AutoHotKey script. However,
scripting in AutoHotKey is awkward and limiting. Also, AutoHotKey's keyboard hooks would 
frequently unregister, requiring me to manually restart the script. So, I built Vimfinity
from scratch in C#, giving me much more control.

Vimfinity doesn't seek to provide a full-featured implementation of Vim across the desktop.
If you really need to edit a lot of text using all of Vim's functionality, you'll probably just
open the text in Vim or an app with plugins that emulate Vim (such as Visual Studio's VSVim).

Instead, Vimfinity fulfills the use case where you need to move the cursor a little bit without
having to take your hands off the home key row. Since Vimfinity is meant to be quick for
small operations, Vim mode is activated/deactivated by holding/releasing a "Vim layer" key, 
instead of pressing a key to enter a mode and then another key to exit the mode.

## Setup

1. Download a release from the [Releases](https://github.com/WestRyanK/Vimfinity/releases) tab.
2. Extract the downloaded zip file to your computer.
3. Click the extracted `Vimfinity.exe` to run the app.
4. A notification icon will appear in the system tray while the app is running.
5. Right-click the tray icon and click `Exit` to close the app.

**Optional:** Add a shortcut to `Vimfinity.exe` to your `Startup` folder to start Vimfinity automatically.

**Note:** There is currently no way to customize Vimfinity. You get what you pay for, and this app is free!

## How to Use

Vimfinity has two modes of operation: Insert Mode and Vim Mode. The app starts in Insert Mode,
passing through every keypress without modifying it. Holding down the "Vim Layer" key activates
Vim Mode which rebinds your keys to Vim-like actions as long as the Vim Layer key is held.
Releasing the Vim Layer key exists Vim Mode.

The semicolon key acts as the Vim Layer key. Holding it down allows you to use Vim Mode; with
the semicolon released, your keyboard works like normal. Quicky tapping the semicolon key will
treat the key like a normal semicolon.

### Vim Mode Key Bindings

| Key                         | Action
|-----------------------------|---------------
| `h`                         | Left Arrow Key
| `l`                         | Right Arrow Key
| `j`                         | Down Arrow Key
| `k`                         | Up Arrow Key
| `n`                         | Home Key
| `m`                         | End Key
| `x`                         | Delete Key
| `Shift` + `x`               | Backspace Key
| `Ctrl` + `n`                | Go to top
| `Ctrl` + `m`                | Go to bottom
| `Ctrl` + `h`                | Move to previous word
| `Ctrl` + `l`                | Move to next word
| `Shift` + (`h` `j` `k` `l`) | Select text while moving
