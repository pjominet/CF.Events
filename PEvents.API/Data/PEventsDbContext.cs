using Microsoft.EntityFrameworkCore;
using PEvents.API.Models;

namespace PEvents.API.Data;

public class PEventsDbContext(DbContextOptions<PEventsDbContext> options) : DbContext(options)
{
    public DbSet<Rsvp> Rsvps => Set<Rsvp>();
    public DbSet<User> Users => Set<User>();
}
