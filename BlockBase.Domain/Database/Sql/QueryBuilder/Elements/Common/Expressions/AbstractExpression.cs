namespace BlockBase.Domain.Database.Sql.QueryBuilder.Elements.Common.Expressions
{
    public interface AbstractExpression
    {
        bool HasParenthesis { get; set; }
        
        AbstractExpression Clone();
    }
}