# ![Circle Clicker](/CircleClicker/Resources/Logo.png)

A customisable clicker game inspired by [Cookie Clicker](https://orteil.dashnet.org/cookieclicker/), made with WPF and .NET 8.

This is a school project of mine that I ended up spending a lot of my free time on, making it much bigger than I had anticipated.

## Other versions
- **1.x** - The original WPF version for Windows (you're here!)

More versions may be coming soon. **No guarantees here.**

## Saving

As of version [1.1.0 alpha 1](https://github.com/f78doesthings/CircleClicker/releases/tag/v1.1.0-alpha.1), you no longer need a MySQL server! Instead, save data, buildings, upgrades and game variables are now saved to a file called `save.json` in the same folder as the `CircleClicker.exe` executable. *Just make sure you have permissions to write to that location...*

If for whatever reason you still want to use a local MySQL server, you can launch Circle Clicker with the `--online` flag.

## Tips

### Music playback

Circle Clicker supports playing back your own music. Simply place the music files you want to play in `<APPDIR>/Resources/Music`, where `<APPDIR>` is the folder that contains the `CircleClicker.exe` executable.

> [!TIP]
> You can find a ZIP file containing some of my own music [here](Assets/Music.zip).
> 
> To play back this music in Circle Clicker, just extract this archive in the folder that contains the `CircleClicker.exe` executable.
> This will create the folders necessary to play back the music in Circle Clicker.

### Administrator privileges

#### Online mode

Creating a user named `admin` will automatically give it administrator privileges. (If you want more administrators, you'll need to edit the database yourself.)

Administrators have access to some extra features to aid with testing, as well as the Admin Panel, where you can edit things like purchases, game variables and other users.

#### Offline mode (v1.1.0+)

To gain access to the administrator features described above in offline mode, simply launch Circle Clicker with the `--test` flag. Keep in mind that some of these features may not fully work in offline mode.
