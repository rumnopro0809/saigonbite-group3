using System.Data.Entity.Migrations;
using SaigonBite.Infrastructure.Data;

namespace SaigonBite.Infrastructure.Migrations
{
    public sealed class Configuration : DbMigrationsConfiguration<SaigonBiteDbContext>
    {
        public Configuration()
        {
            AutomaticMigrationsEnabled = false;
            MigrationsDirectory = "Migrations";
        }

        protected override void Seed(SaigonBiteDbContext context)
        {
            SaigonBiteSeeder.Seed(context);
        }
    }
}
