namespace SaigonBite.Infrastructure.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class Uc03HubRowVersion : DbMigration
    {
        public override void Up()
        {
            // UC03 FR14.3: concurrency token so two admins cannot overwrite each other's hub edits.
            AddColumn("dbo.Hubs", "RowVersion", c => c.Binary(nullable: false, fixedLength: true, timestamp: true, storeType: "rowversion"));
            // UC03 FR14.1 / BR8: capacity is the load denominator, so it must stay positive.
            Sql("ALTER TABLE dbo.Hubs ADD CONSTRAINT CK_Hubs_MaxCapacity_Positive CHECK (MaxCapacity > 0)");
        }
        
        public override void Down()
        {
            Sql("ALTER TABLE dbo.Hubs DROP CONSTRAINT CK_Hubs_MaxCapacity_Positive");
            DropColumn("dbo.Hubs", "RowVersion");
        }
    }
}
