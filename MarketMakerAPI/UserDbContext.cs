using Microsoft.EntityFrameworkCore;

public class User
{
    public Guid UserId { get; set; }
    public string Email { get; set; }
    public string PasswordHash { get; set; }
}
public class UserDbContext : DbContext
{
    public UserDbContext(DbContextOptions<UserDbContext> options) : base(options)
    {
    }
    
    public DbSet<User> User { get; set; }
}