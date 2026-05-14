using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using ShoppingListDiploma.Models;

namespace ShoppingListDiploma.Data;

public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<ShoppingGroup> ShoppingGroups => Set<ShoppingGroup>();

    public DbSet<GroupMember> GroupMembers => Set<GroupMember>();

    public DbSet<ShoppingList> ShoppingLists => Set<ShoppingList>();

    public DbSet<ShoppingItem> ShoppingItems => Set<ShoppingItem>();

    public DbSet<ItemHistory> ItemHistories => Set<ItemHistory>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<ApplicationUser>(entity =>
        {
            entity.Property(user => user.DisplayName)
                .HasMaxLength(100);
        });

        builder.Entity<ShoppingGroup>(entity =>
        {
            entity.Property(group => group.Name)
                .HasMaxLength(150)
                .IsRequired();

            entity.Property(group => group.Description)
                .HasMaxLength(500);

            entity.HasOne(group => group.Owner)
                .WithMany()
                .HasForeignKey(group => group.OwnerId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        builder.Entity<GroupMember>(entity =>
        {
            entity.Property(member => member.Role)
                .HasMaxLength(50)
                .IsRequired();

            entity.HasIndex(member => new { member.ShoppingGroupId, member.ApplicationUserId })
                .IsUnique();

            entity.HasOne(member => member.ShoppingGroup)
                .WithMany(group => group.Members)
                .HasForeignKey(member => member.ShoppingGroupId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(member => member.ApplicationUser)
                .WithMany(user => user.GroupMemberships)
                .HasForeignKey(member => member.ApplicationUserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<ShoppingList>(entity =>
        {
            entity.Property(list => list.Name)
                .HasMaxLength(150)
                .IsRequired();

            entity.HasOne(list => list.ShoppingGroup)
                .WithMany(group => group.ShoppingLists)
                .HasForeignKey(list => list.ShoppingGroupId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<ShoppingItem>(entity =>
        {
            entity.Property(item => item.Name)
                .HasMaxLength(200)
                .IsRequired();

            entity.Property(item => item.Quantity)
                .HasColumnType("decimal(18,2)");

            entity.Property(item => item.Unit)
                .HasMaxLength(50);

            entity.HasOne(item => item.ShoppingList)
                .WithMany(list => list.Items)
                .HasForeignKey(item => item.ShoppingListId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(item => item.CreatedByUser)
                .WithMany()
                .HasForeignKey(item => item.CreatedByUserId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(item => item.PurchasedByUser)
                .WithMany()
                .HasForeignKey(item => item.PurchasedByUserId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<ItemHistory>(entity =>
        {
            entity.Property(history => history.Action)
                .HasMaxLength(100)
                .IsRequired();

            entity.Property(history => history.OldValue)
                .HasMaxLength(1000);

            entity.Property(history => history.NewValue)
                .HasMaxLength(1000);

            entity.HasOne(history => history.ShoppingItem)
                .WithMany(item => item.HistoryEntries)
                .HasForeignKey(history => history.ShoppingItemId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(history => history.ApplicationUser)
                .WithMany(user => user.ItemHistories)
                .HasForeignKey(history => history.ApplicationUserId)
                .OnDelete(DeleteBehavior.SetNull);
        });
    }
}
