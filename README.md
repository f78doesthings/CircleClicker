# Circle Clicker

A customizable clicker game inspired by [Cookie Clicker](https://orteil.dashnet.org/cookieclicker/), made with WPF.

This is a school project of mine that kind of turned into a passion project because I needed something to spend my free time on.

## Saving

Circle Clicker currently uses MySQL to save user data, buildings, upgrades and game variables.
(This is because using a database is required for the school project.)

If you want to enable saving, you should install [XAMPP](https://www.apachefriends.org/index.html) and use its MySQL server.
Circle Clicker will create a database for you if it doesn't exist.

If I decide to continue this project after my course ends on 7 June 2024, I will probably make a version of Circle Clicker that doesn't require a MySQL database.

## Tips

-   Creating a user named `admin` will automatically give it admin privileges.
    Admins have access to some extra features to aid with testing, as well as the Admin Panel, where you can edit purchases and game variables.
-   Circle Clicker supports playing back your own music. Place the music files you want to play in `<APPDIR>/Resources/Music`, where `<APPDIR>` is the folder that contains the `CircleClicker.exe` executable.
    Circle Clicker currently comes with 1 of my own songs by default.