using System.Reflection;
using Microsoft.EntityFrameworkCore;
using WayPath.Application;
using WayPath.Infrastructure;

namespace WayPath.Tests;

public sealed class QueryTranslationTests
{
    [Fact]
    public void Availability_filters_entities_before_projection()
    {
        using var db = Context();
        var store = new OperacionStore(db);
        var query = Query(store, "AvailabilityQuery", 1,
            new DisponibilidadFiltroRequest(7, new DateOnly(2026, 1, 1), new DateOnly(2026, 12, 31)));

        var sql = query.ToQueryString();

        Assert.Contains("EstadoId", sql);
        Assert.Contains("PaqueteId", sql);
        Assert.Contains("WHERE", sql);
    }

    [Fact]
    public void My_reservations_filter_client_in_sql()
    {
        using var db = Context();
        var sql = Query(new ReservaStore(db), "SummaryQuery", (long?)17).ToQueryString();

        Assert.Contains("ClienteId", sql);
        Assert.Contains("WHERE", sql);
    }

    [Fact]
    public void My_purchases_filter_client_in_sql()
    {
        using var db = Context();
        var sql = Query(new VentaStore(db), "SummaryQuery", (long?)17).ToQueryString();

        Assert.Contains("ClienteId", sql);
        Assert.Contains("WHERE", sql);
    }

    [Fact]
    public void Public_reviews_filter_approved_destination_and_rating_in_sql()
    {
        using var db = Context();
        var sql = Query(new VentaStore(db), "PublicReviewQuery", (long?)5, (byte?)4).ToQueryString();

        Assert.Contains("APROBADA", sql);
        Assert.Contains("DestinoId", sql);
        Assert.Contains("Calificacion", sql);
    }

    [Fact]
    public void Event_summary_grouping_and_ordering_translate_to_sql()
    {
        using var db = Context();
        var sql = Query(new ReporteStore(db, new DashboardStore(db)), "EventSummaryQuery").ToQueryString();

        Assert.Contains("GROUP BY", sql);
        Assert.Contains("COUNT(*)", sql);
        Assert.Contains("MAX(", sql);
        Assert.Contains("ORDER BY", sql);
    }

    private static WayPathDbContext Context() => new(new DbContextOptionsBuilder<WayPathDbContext>()
        .UseSqlServer("Server=unused;Database=unused;Integrated Security=True;TrustServerCertificate=True")
        .Options);

    private static IQueryable Query(object store, string method, params object?[] arguments)
    {
        var target = store.GetType().GetMethod(method, BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException($"No se encontró {store.GetType().Name}.{method}.");
        return (IQueryable)(target.Invoke(store, arguments)
            ?? throw new InvalidOperationException($"{method} no devolvió una consulta."));
    }
}
