using BioGamaEcuador.Data;
using Microsoft.EntityFrameworkCore;

namespace BioGamaEcuador.Services;

// Compartido por la barra y el menú; una consulta por usuario y petición HTTP.
public sealed class CartSummary(AppDbContext database)
{
    private string? cachedUser;
    private Task<int>? cachedCount;

    public Task<int> CountAsync(string? userId)
    {
        if (userId is null) return Task.FromResult(0);
        if (cachedCount is null || cachedUser != userId)
        {
            cachedUser = userId;
            cachedCount = database.Database.SqlQueryRaw<int>(
                "SELECT COALESCE(SUM(od.\"Quantity\"), 0) AS \"Value\" FROM \"OrderDetails\" od INNER JOIN \"Orders\" o ON o.\"Id\" = od.\"OrderId\" WHERE o.\"UserId\" = {0} AND o.\"Status\" = 'Pending'",
                userId).FirstOrDefaultAsync();
        }
        return cachedCount;
    }
}
