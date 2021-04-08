using BlockBase.Domain.Database.Sql.QueryBuilder.Elements;
using BlockBase.Domain.Database.Sql.QueryBuilder.Elements.Common;
using BlockBase.Domain.Database.Sql.QueryBuilder.Elements.Common.Expressions;
using BlockBase.Domain.Database.Sql.QueryBuilder.Elements.Database;
using BlockBase.Domain.Database.Sql.QueryBuilder.Elements.Record;
using BlockBase.Domain.Database.Sql.QueryBuilder.Elements.Table;
using System;
using System.Collections.Generic;
using System.Text;

namespace BlockBase.Domain.Database.Sql.Generators
{
    public interface IGenerator
    {
        string BuildString(CreateDatabaseStatement createDatabaseStatement);
        string BuildString(DropDatabaseStatement dropDatabaseStatement);
        string BuildString(UseDatabaseStatement useDatabaseStatement);
        string BuildString(CreateTableStatement createTableStatement);
        string BuildString(AbstractAlterTableStatement alterTableStatement);
        string BuildString(DropTableStatement dropTableStatement);
        string BuildString(InsertRecordStatement insertRecordStatement);
        string BuildString(UpdateRecordStatement updateRecordStatement);
        string BuildStringToSimpleSelectStatement(UpdateRecordStatement updateRecordStatement);
        string BuildString(DeleteRecordStatement deleteRecordStatement);
        string BuildString(AbstractExpression expression);
        string BuildString(ColumnDefinition columnDefinition);
        string BuildString(SimpleSelectStatement simpleSelectStatement);
        string BuildString(SelectCoreStatement selectCoreStatement);
        string BuildString(ComparisonExpression.ComparisonOperatorEnum comparisonOperator);
        string BuildString(JoinOperationField.JoinConstraint joinConstraint);
        string BuildString(ResultColumn resultColumn);
        string BuildString(TableOrSubquery tableOrSubquery);
        string BuildString(OrderingTerm orderingTerm);
        string BuildString(JoinClause joinClause);
        string BuildString(DataTypeEnum dataType);
        string BuildString(ColumnConstraint columnConstraint);
        string BuildString(ForeignKeyClause foreignKeyClause);
    }
}
