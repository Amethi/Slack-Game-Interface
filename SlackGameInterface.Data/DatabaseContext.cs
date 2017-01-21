using Microsoft.WindowsAzure.ServiceRuntime;
using SlackGameInterface.Lib.Models;
using System.Data.Entity;
using System.Data.SqlClient;

namespace SlackGameInterface.Data
{
    /// <summary>
    /// Entity Framework database connection object.
    /// </summary>
    public class DatabaseContext : DbContext
    {
        // You can add custom code to this file. Changes will not be overwritten.
        // 
        // If you want Entity Framework to drop and regenerate your database
        // automatically whenever you change your model schema, please use data migrations.
        // For more information refer to the documentation:
        // http://msdn.microsoft.com/en-us/data/jj591621.aspx
        public DatabaseContext() : base(new SqlConnectionStringBuilder(RoleEnvironment.GetConfigurationSettingValue("DatabaseConnectionString")).ToString())
        {
        }

        public DbSet<User> Users { get; set; }
        public DbSet<RssItem> RssItems { get; set; }
        public DbSet<ServiceConfig> ServiceConfigs { get; set; }
    }
}