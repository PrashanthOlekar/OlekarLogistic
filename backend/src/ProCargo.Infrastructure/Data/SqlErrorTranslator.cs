using Microsoft.Data.SqlClient;
using ProCargo.Application.Common.Exceptions;

namespace ProCargo.Infrastructure.Data;

/// <summary>
/// Turns SQL Server errors into the application's exceptions, so no SQL detail reaches the client:
///   50400 (THROW in a procedure)  → BusinessRuleException (400) with the procedure's message
///   50409 (THROW in a procedure)  → ConflictException (409) with the procedure's message
///   2601 / 2627 unique index       → ConflictException (409) with a friendly message
///   547 foreign key                → BusinessRuleException (400)
///   1205 deadlock victim           → ConflictException (409), try again
///   anything else                  → DatabaseException (500), details only in the log
/// </summary>
internal static class SqlErrorTranslator
{
    public const int RuleBroken = 50400;
    public const int Conflict = 50409;

    private static readonly IReadOnlyDictionary<string, string> DuplicateMessages = new Dictionary<string, string>
    {
        ["UX_Users_Mobile"] = "This number is already registered. Sign in instead.",
        ["UX_Users_Email"] = "This email is already registered.",
        ["UX_Drivers_Licence"] = "This licence is already registered.",
        ["UX_Vehicles_RegistrationNumber"] = "This vehicle is already registered.",
        ["UX_Trips_OneActivePerBooking"] = "This load has already been taken.",
        ["UX_Settlements_Trip"] = "This trip already has a payout.",
        ["UX_Invoices_InvoiceNumber"] = "This invoice number is already used. Try again.",
    };

    public static Exception Translate(SqlException exception) => exception.Number switch
    {
        RuleBroken => new BusinessRuleException(exception.Message),
        Conflict => new ConflictException(exception.Message),
        2601 or 2627 => new ConflictException(DuplicateMessage(exception.Message)),
        547 => new BusinessRuleException("One of the chosen items doesn't exist or is still in use."),
        1205 => new ConflictException("Someone else changed this just now. Refresh and try again."),
        _ => new DatabaseException("The database could not complete the request.", exception),
    };

    private static string DuplicateMessage(string sqlMessage)
    {
        foreach ((string indexName, string message) in DuplicateMessages)
        {
            if (sqlMessage.Contains(indexName, StringComparison.OrdinalIgnoreCase))
            {
                return message;
            }
        }

        return "This record already exists.";
    }
}
