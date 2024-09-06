using Authentication.Models;
using Microsoft.EntityFrameworkCore;

namespace Authentication.Context
{
    public class DataContext : DbContext
    {
        public DataContext(DbContextOptions<DataContext> options) : base(options)
        {

        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            base.OnConfiguring(optionsBuilder);

            optionsBuilder.UseSqlServer("Server=(localdb)\\ProjectModels;Database=userdb;Trusted_Connection=True;MultipleActiveResultSets=true");
        }

        public DbSet<User> Users { get; set; }
        public DbSet<EmailHistory> EmailHistory { get; set; }

        public Task<User?> GetUser(string emailOrUsername) 
        {
            var user = Users.FirstOrDefaultAsync(x => x.Email == emailOrUsername || x.Username == emailOrUsername);
            return user;
        }
    }
}
