using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace CircleClicker.Models.Database;

public partial class User(string name, string password)
{
    public int Id { get; set; }

    /// <summary>
    /// The name of the user.
    /// </summary>
    public string Name { get; set; } = name;

    /// <summary>
    /// The hashed password of this user.
    /// </summary>
    public string Password { get; set; } = password;

    /// <summary>
    /// Whether this user can access administrator-only features, such as the admin panel.
    /// </summary>
    public bool IsAdmin { get; set; }

    /// <summary>
    /// A list of <see cref="Save"/>s that belong to this user.
    /// </summary>
    public virtual ObservableCollection<Save> Saves { get; set; } = [];
}
