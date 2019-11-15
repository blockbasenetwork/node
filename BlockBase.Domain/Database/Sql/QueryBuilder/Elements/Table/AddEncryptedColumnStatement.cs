namespace BlockBase.Domain.Database.Sql.QueryBuilder.Elements.Table
{
    public class AddEncryptedColumnStatement : AddColumnStatement
    {
        public int BucketSize { get; set; }
    }
}