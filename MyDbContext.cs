using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SQLinEFcore_hw
{
    internal class MyDbContext : DbContext
    {
        public DbSet<Category> Category { get; set; }
        public DbSet<User> User { get; set; }
        public DbSet<Product> Product { get; set; }
        public DbSet<Cart> Cart { get; set; }
        public DbSet<Word> Word { get; set; }
        public DbSet<KeyParams> KeyLink { get; set; }

        /*public MyDbContext()
        {
            Database.EnsureCreated();
        }*/
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {

            modelBuilder.Entity<Cart>()
                .Ignore(c => c.Id)
                .HasKey(c => new { c.UserId, c.ProductId });
            modelBuilder.Entity<Cart>()
                .HasOne(c => c.Product)
                .WithMany(p => p.Cart)
                .HasForeignKey(c => c.ProductId);
            modelBuilder.Entity<Cart>()
                .HasOne(c => c.User)
                .WithMany(u => u.Cart)
                .HasForeignKey(c => c.UserId);

            modelBuilder.Entity<Product>()
                .HasOne(p => p.Category)
                .WithMany(c => c.Products)
                .HasForeignKey(p => p.CategoryId);

            modelBuilder.Entity<KeyParams>()
                .Ignore(kp => kp.Id)
                .HasKey(kp => new { kp.ProductId, kp.KeyWordsId });
            modelBuilder.Entity<KeyParams>()
                .HasOne(kp => kp.Product)
                .WithMany(p => p.KeyWords)
                .HasForeignKey(kp => kp.ProductId);
            modelBuilder.Entity<KeyParams>()
                .HasOne(kp => kp.KeyWords)
                .WithMany(w => w.PrpductLink)
                .HasForeignKey(kp => kp.KeyWordsId);

        }
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder
                .UseSqlServer("Server=localhost\\SQLEXPRESS;Database=NewBaseTest2;Trusted_Connection=True;TrustServerCertificate=True;");
        }
    }
}
