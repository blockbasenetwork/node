using System;
using System.Collections.Generic;
using System.Text;

namespace BlockBase.Domain.Database.QueryResults
{
    public enum WhereTypeEnum
    {
        WhereEqualFields,
        WhereUnequalFields,
        WhereHigherThanFields,
        WhereLessThanFields,
        WhereHigherOrEqualFields,
        WhereLessOrEqualFields
        
        // WhereEqualOrFields  
        // WhereEqualFieldsWithDBFormat,    
    }
}