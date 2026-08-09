namespace CleanserBlazorUI.Entities;

// Now an EF-tracked entity in ApplicationDbContext (previously read via raw
// ADO.NET from a separate, unmigrated database -- see
// DataManagementService.GetShortCodeFromSubscribeIDAsync). Id is new; every
// existing consumer only ever reads ShortName/SubCategoryCode, so this is
// additive and doesn't change the public shape anyone depends on.
public class SubscribeContext
{
    public int Id { get; set; }
    public string ShortName { get; set; } = string.Empty;
    public string SubCategoryCode { get; set; } = string.Empty;
}

