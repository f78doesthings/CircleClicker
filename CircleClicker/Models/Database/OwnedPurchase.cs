using System.Text.Json.Serialization;

namespace CircleClicker.Models.Database;

/// <summary>
/// Represents an amount of a <see cref="Database.Purchase"/> owned in a <see cref="Database.Save"/>.
/// </summary>
public partial class OwnedPurchase
{
    [JsonIgnore]
    public int Id { get; set; }

    [JsonIgnore]
    public int SaveId { get; set; }

    public int PurchaseId { get; set; }

    /// <summary>
    /// The amount owned of this purchase.
    /// </summary>
    public int Amount { get; set; }

    /// <summary>
    /// The <see cref="Database.Purchase"/> this <see cref="OwnedPurchase"/> is referring to.
    /// </summary>
    [JsonIgnore]
    public virtual Purchase Purchase { get; set; } = null!;

    /// <summary>
    /// The <see cref="Database.Save"/> this <see cref="OwnedPurchase"/> belongs to.
    /// </summary>
    [JsonIgnore]
    public virtual Save Save { get; set; } = null!;
}
