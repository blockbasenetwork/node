namespace BlockBase.Domain.Database.Sql.QueryBuilder.Elements.Table
{
    public class AddEncryptedRangeColumnStatement : AddEncryptedColumnStatement
    {
        public int RangeMin { get; set; }
        public int RangeMax { get; set; }
    }
}