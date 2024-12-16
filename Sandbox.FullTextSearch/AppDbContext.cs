using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Sandbox.FullTextSearch;

public class AppDbContext : DbContext
{
    public DbSet<User> Users { get; set; }
    public DbSet<Name> Names { get; set; }
    public DbSet<Notification> Notifications { get; set; }

    public DbSet<NotificationSearchResult> NotificationSearchResults { get; set; }
    
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Notification>()
            .HasOne(e => e.To)
            .WithMany(e => e.Notifications);
        
        modelBuilder.Entity<Name>()
            .HasOne(e => e.User)
            .WithMany(e => e.Names);
        
        modelBuilder.Entity<NotificationSearchResult>().HasNoKey().ToView(null);
    }
}

public class NotificationSearchResult
{
    public int NotificationId { get; set; }
    public int OverallRank { get; set; }
}

public class User
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }
    public required string FirstName { get; set; }
    public required string LastName { get; set; }

    public ICollection<Name> Names { get; set; } = new List<Name>();
    public ICollection<Notification> Notifications { get; set; } = new List<Notification>();
}

public class Name
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }
    public required string FirstName { get; set; }
    public required string LastName { get; set; }
    public NameType Type { get; set; }

    public User User { get; set; }
}
public enum NameType { Current }

public class Notification
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }
    public required string Subject { get; set; }
    public required string Content { get; set; }
    public DateTime CreatedAt { get; set; }
    public bool IsNotification { get; set; }
    public bool IsEmail { get; set; }
    public User To { get; set; }
}