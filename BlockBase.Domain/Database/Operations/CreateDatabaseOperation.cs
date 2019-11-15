
namespace BlockBase.Domain.Database.Operations
{
    public class CreateDatabaseOperation : ISqlOperation
    {
        public Database Database { get; set; }
        public CreateDatabaseOperation()
        {
        }

        public string GetMySQLQuery()
        {
            return "CREATE DATABASE " + Database.DatabaseName;
        }

        public string GetSQLQuery()
        {
            return "CREATE DATABASE " + Database.DatabaseName;;
        }
    }
}