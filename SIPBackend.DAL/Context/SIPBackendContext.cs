using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using SIPBackend.Domain.Entities;

namespace SIPBackend.DAL.Context;

public class SIPBackendContext : IdentityDbContext<AppUser,IdentityRole,string>
{
    public SIPBackendContext(DbContextOptions<SIPBackendContext> opts) : base(opts)
    {
        /*Database.EnsureCreated();*/
    }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        builder.Entity<Friend>()
            .HasKey(f => f.Id);

        builder.Entity<Friend>()
            .Property(p => p.UserId1)
            .HasColumnName("userid1");
        
        builder.Entity<Friend>()
            .Property(p => p.UserId2)
            .HasColumnName("userid2");

        /*
        builder.Entity<Friend>()
            .HasCheckConstraint("CK_Friends_userid1_LessThan_userid2", "userid1 < userid2"); 
            */

        base.OnModelCreating(builder);
    }
    
    public DbSet<Friend> Friends { get; set; }
    public DbSet<Chat> Chats { get; set; }
    public DbSet<ChatParticipants> ChatParticipants { get; set; }
    public DbSet<Message> Messages { get; set; }
}