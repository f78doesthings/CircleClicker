# Circle Clicker
A highly customizable clicker game inspired by [Cookie Clicker](https://orteil.dashnet.org/cookieclicker/), made with WPF.

This is a school project of mine that kind of turned into a passion project because I needed something to spend my free time on.

## Notes
Circle Clicker currently uses MySQL to save user data, buildings and upgrades. (Using a database was required for this project.)

It's configured to work with the copy of MariaDB that comes with [XAMPP](https://www.apachefriends.org/index.html), using the database name `circle_clicker`. The game should create this database on startup if it does not exist already.

## Tips
- Creating a user named `admin` will automatically give it admin privileges. Admins have access to some extra features to aid with testing, as well as the Admin Panel, where you can edit buildings and upgrades.
