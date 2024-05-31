# ![Circle Clicker](/CircleClicker/Resources/Logo.png)

A customizable clicker game inspired by [Cookie Clicker](https://orteil.dashnet.org/cookieclicker/), made with WPF.

This is a school project of mine that I ended up spending a lot of my free time on, making it much bigger than I had anticipated.

If I decide to continue this project after my course ends on 7 June 2024, I will probably make a version of Circle Clicker that is cross-platform and won't require a MySQL database.

## Saving

Circle Clicker currently uses MySQL to save user data, buildings, upgrades and game variables.
(This is because using a database is required for the school project.)

> [!IMPORTANT]
> If you want to enable saving, you should install [XAMPP](https://www.apachefriends.org/index.html) and use its MySQL server.
> Circle Clicker will create a database for you if it doesn't exist.

## Tips

### Creating an administrator account

Creating a user named `admin` will automatically give it administrator privileges. (If you want more administrators, you'll need to edit the database yourself.)

Administrators have access to some extra features to aid with testing, as well as the Admin Panel, where you can edit things like purchases and game variables.

### Music playback

Circle Clicker supports playing back your own music. Simply place the music files you want to play in `<APPDIR>/Resources/Music`, where `<APPDIR>` is the folder that contains the `CircleClicker.exe` executable.

(Circle Clicker currently comes with 1 of my own songs by default. I will add more later.)
