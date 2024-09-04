using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.Web.CodeGenerators.Mvc.Templates.BlazorIdentity.Pages.Manage;
using TaskManagement.Models;

namespace TaskManagement.Contexts
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        { 

        }

        public DbSet<UserAccount> UserAccounts { get; set; }
        public UserAccount GetAccountByEmailOrName(string emailOrName) 
        {
            UserAccount user = UserAccounts.FirstOrDefault(x => x.Email == emailOrName || x.UserName == emailOrName);

            return user;
        }

        public bool IsEmailOrUsernameUsed(string emailOrName) 
        {
            UserAccount user = UserAccounts.FirstOrDefault(x => x.Email == emailOrName || x.UserName == emailOrName);

            return user != null;
        }

        public bool IsAccountGoogleAuth(string emailOrName) 
        {
            UserAccount user = UserAccounts.FirstOrDefault(x => x.Email == emailOrName || x.UserName == emailOrName);

            return user.IsGoogleAccount;
        }
    }

}
