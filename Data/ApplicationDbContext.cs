using Microsoft.EntityFrameworkCore;
using OnlineGroceryShop.Models;

namespace OnlineGroceryShop.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<User> Users { get; set; }
        public DbSet<Category> Categories { get; set; }
        public DbSet<Product> Products { get; set; }
        public DbSet<Order> Orders { get; set; }
        public DbSet<OrderDetail> OrderDetails { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configure User Email and PhoneNumber as Unique
            modelBuilder.Entity<User>()
                .HasIndex(u => u.Email)
                .IsUnique();
                
            modelBuilder.Entity<User>()
                .HasIndex(u => u.PhoneNumber)
                .IsUnique();

            // Seed Categories
            modelBuilder.Entity<Category>().HasData(
                new Category { Id = 1, Name = "Vegetables", Description = "Fresh organic vegetables" },
                new Category { Id = 2, Name = "Fruits", Description = "Farm-fresh fruits" },
                new Category { Id = 3, Name = "Desserts", Description = "Sweet treats and pastries" },
                new Category { Id = 4, Name = "Drinks", Description = "Refreshing beverages" },
                new Category { Id = 5, Name = "Fish & Meats", Description = "High-quality seafood and meat" },
                new Category { Id = 6, Name = "Pets", Description = "Supplies for your furry friends" }
            );

            // Seed Products
            modelBuilder.Entity<Product>().HasData(
                // Vegetables (Category 1)
                new Product { Id = 1, Name = "Organic Bananas", Price = 30, CategoryId = 2, Description = "Fresh organic bananas", ImagePath = "https://cdn-icons-png.flaticon.com/512/2909/2909808.png", StockQuantity = 100 },
                new Product { Id = 2, Name = "Red Apples", Price = 4.50m, CategoryId = 2, Description = "Sweet and crunchy apples", ImagePath = "https://cdn-icons-png.flaticon.com/512/2909/2909787.png", StockQuantity = 150 },
                new Product { Id = 3, Name = "Strawberry", Price = 2.99m, CategoryId = 1, Description = "Crispy green spinach", ImagePath = "https://cdn-icons-png.flaticon.com/512/2909/2909841.png", StockQuantity = 80 },
                new Product { Id = 6, Name = "Mango", Price = 1.99m, CategoryId = 1, Description = "Sweet and crunchy carrots", ImagePath = "https://cdn-icons-png.flaticon.com/512/2909/2909846.png", StockQuantity = 200 },
                
                // Desserts (Category 3)
                new Product { Id = 7, Name = "Chocolate Cupcake", Price = 2.50m, CategoryId = 3, Description = "Rich chocolate cupcake", ImagePath = "https://cdn-icons-png.flaticon.com/512/2909/2909832.png", StockQuantity = 50 },
                
                // Drinks (Category 4)
                new Product { Id = 4, Name = "Fresh Orange Juice", Price = 4.99m, CategoryId = 4, Description = "100% pure orange juice", ImagePath = "https://cdn-icons-png.flaticon.com/512/2909/2909890.png", StockQuantity = 50 },
                
                // Fish & Meats (Category 5)
                new Product { Id = 8, Name = "Fresh Salmon", Price = 12.99m, CategoryId = 5, Description = "Wild-caught Atlantic salmon", ImagePath = "https://cdn-icons-png.flaticon.com/512/2909/2909861.png", StockQuantity = 30 },
                new Product { Id = 9, Name = "Angus Beef Steaks", Price = 15.50m, CategoryId = 5, Description = "Prime cut Angus beef", ImagePath = "https://cdn-icons-png.flaticon.com/512/2909/2909866.png", StockQuantity = 25 },
                
                // Pets (Category 6)
                new Product { Id = 10, Name = "Premium Dog Food", Price = 25.00m, CategoryId = 6, Description = "Nutritionally balanced dog food", ImagePath = "https://cdn-icons-png.flaticon.com/512/2909/2909903.png", StockQuantity = 40 }
            );

            // Seed Admin and Demo User
            modelBuilder.Entity<User>().HasData(
                new User { Id = 1, Name = "Admin User", PhoneNumber = "0000000000", Email = "admin@grocery.com", PasswordHash = "admin123", Role = "Admin" },
                new User { Id = 2, Name = "Demo User", PhoneNumber = "1234567890", Email = "user@example.com", PasswordHash = "user123", Role = "Customer" }
            );
        }
    }
}
