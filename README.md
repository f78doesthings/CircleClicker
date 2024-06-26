# ![Circle Clicker](/CircleClicker/Resources/Logo.png)

A customisable clicker game inspired by [Cookie Clicker](https://orteil.dashnet.org/cookieclicker/), made with WPF and .NET 8.

This is a school project of mine that I ended up spending a lot of my free time on, making it much bigger than I had anticipated.

*If* I decide to continue this project in the future, I will probably make a version of Circle Clicker that is cross-platform and won't require a MySQL database. **No guarantees here.**

## Saving

This version of Circle Clicker is configured to use a local MySQL server to save user data, buildings, upgrades and game variables.
(This is because using a database was required for the school project.)

> [!IMPORTANT]
> If you want to enable saving, you should install [XAMPP](https://www.apachefriends.org/index.html) and use its bundled MySQL server, as this is what I use to perform testing.
> Circle Clicker will automatically create a database for you on startup if it doesn't exist.

## Tips

### Creating an administrator account

Creating a user named `admin` will automatically give it administrator privileges. (If you want more administrators, you'll need to edit the database yourself.)

Administrators have access to some extra features to aid with testing, as well as the Admin Panel, where you can edit things like purchases and game variables.

### Music playback

Circle Clicker supports playing back your own music. Simply place the music files you want to play in `<APPDIR>/Resources/Music`, where `<APPDIR>` is the folder that contains the `CircleClicker.exe` executable.

> [!TIP]
> You can find a ZIP file containing some of my own music [here](Assets/Music.zip).
> 
> To play back this music in Circle Clicker, just extract this archive in the folder that contains the `CircleClicker.exe` executable.
> This will create the folders necessary to play back the music in Circle Clicker.