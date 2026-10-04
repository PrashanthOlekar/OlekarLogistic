using System.Data;
using Dapper;

namespace ProCargo.Infrastructure.Data;

/// <summary>Maps SQL DATE columns and parameters to DateOnly.</summary>
internal sealed class DateOnlyTypeHandler : SqlMapper.TypeHandler<DateOnly>
{
    public override void SetValue(IDbDataParameter parameter, DateOnly value)
    {
        parameter.DbType = DbType.Date;
        parameter.Value = value.ToDateTime(TimeOnly.MinValue);
    }

    public override DateOnly Parse(object value) => value switch
    {
        DateOnly date => date,
        DateTime dateTime => DateOnly.FromDateTime(dateTime),
        _ => DateOnly.Parse(Convert.ToString(value, System.Globalization.CultureInfo.InvariantCulture)!, System.Globalization.CultureInfo.InvariantCulture),
    };
}
