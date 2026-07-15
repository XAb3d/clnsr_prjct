using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
namespace CleanserBlazorUI.Data;

public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : IdentityDbContext<ApplicationUser>(options)
{
    public DbSet<BusinessRef> BusinessesData { get; set; }
    public DbSet<IndividualRef> IndividualsData { get; set; }
    public DbSet<SettingsClass> Settings { get; set; }
    public DbSet<BusSettNormalizer> BusinessClassNormalizer { get; set; }
}