using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ProfileManager.Models
{
    public class CandidateDatabaseContext : DbContext
    {
        public CandidateDatabaseContext(DbContextOptions<CandidateDatabaseContext> options)
           : base(options)
        {
        }
        public DbSet<ProfileManager.Models.Candidate> Candidate { get; set; }
        public DbSet<ProfileManager.Models.Jobs> Jobs { get; set; }
    }   
}
