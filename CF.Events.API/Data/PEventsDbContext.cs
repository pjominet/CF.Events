using CF.Events.API.Models;
using Microsoft.EntityFrameworkCore;

namespace CF.Events.API.Data;

public class PEventsDbContext(DbContextOptions<PEventsDbContext> options) : DbContext(options)
{
    public DbSet<Rsvp> Rsvps => Set<Rsvp>();
    public DbSet<User> Users => Set<User>();
}
