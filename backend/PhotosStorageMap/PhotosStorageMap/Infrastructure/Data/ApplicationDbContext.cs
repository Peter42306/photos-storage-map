using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using PhotosStorageMap.Domain.Entities;
using PhotosStorageMap.Infrastructure.Identity;
using PhotosStorageMap.Application.Common;

namespace PhotosStorageMap.Infrastructure.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<UploadCollection> UploadCollections => Set<UploadCollection>();
        public DbSet<PhotoItem> PhotoItems => Set<PhotoItem>();
        public DbSet<ShareLink> ShareLinks => Set<ShareLink>();

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            builder.Entity<UploadCollection>(entity =>
            {
                entity.HasKey(e => e.Id);
                
                entity.Property(e => e.OwnerUserId)
                    .IsRequired();
                
                entity.Property(e => e.Title)
                    .HasMaxLength(Limits.UploadCollection.Title);
                
                entity.Property(e => e.Description)
                    .HasMaxLength(Limits.UploadCollection.Description);

                // 1 : N, UploadCollection <-> PhotoItem
                entity.HasMany(e => e.Photos)
                    .WithOne(p => p.UploadCollection)
                    .HasForeignKey(p => p.UploadCollectionId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasIndex(x => new { x.OwnerUserId, x.CreatedAtUtc });
                entity.HasIndex(x => new { x.OwnerUserId, x.ExpiresAtUtc });
                entity.HasIndex(x => new { x.OwnerUserId, x.TotalPhotos});
                entity.HasIndex(x => new { x.OwnerUserId, x.TotalBytes });                
            });

            builder.Entity<PhotoItem>(entity =>
            {
                entity.HasKey(e => e.Id);

                entity.Property(e => e.OriginalFileName)
                    .IsRequired()
                    .HasMaxLength(Limits.PhotoItem.OriginalFileName);

                entity.Property(e => e.Description)
                    .HasMaxLength(Limits.PhotoItem.Description);

                entity.Property(e => e.Error)
                    .HasMaxLength(Limits.PhotoItem.Error);

                entity.Property(e => e.OriginalKey)
                    .HasMaxLength(Limits.PhotoItem.StorageKey);

                entity.Property(e => e.StandardKey)                    
                    .HasMaxLength(Limits.PhotoItem.StorageKey);

                entity.Property(e => e.ThumbKey)                    
                    .HasMaxLength(Limits.PhotoItem.StorageKey);
                
                entity.HasIndex(e => e.UploadCollectionId);
                entity.HasIndex(e => new { e.UploadCollectionId, e.Status });
                entity.HasIndex(e => e.Status);
                entity.HasIndex(e => e.TakenAt);
            });

            builder.Entity<ShareLink>(entity =>
            {
                entity.HasKey(e => e.Id);

                entity.Property(e => e.Token)
                    .IsRequired()
                    .HasMaxLength(Limits.ShareLink.Token);
                
                entity.HasOne(e => e.UploadCollection)
                    .WithOne(c => c.ShareLink)
                    .HasForeignKey<ShareLink>(e => e.UploadCollectionId)
                    .OnDelete(DeleteBehavior.Cascade);
                
                entity.HasIndex(e => e.UploadCollectionId)
                    .IsUnique();
                
                entity.HasIndex(e => e.Token)
                    .IsUnique();
            });
        }
    }
}
