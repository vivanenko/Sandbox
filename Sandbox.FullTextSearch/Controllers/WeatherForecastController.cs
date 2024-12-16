using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Sandbox.FullTextSearch.Controllers;

[ApiController]
[Route("[controller]")]
public class WeatherForecastController : ControllerBase
{
    private readonly IDbContextFactory<AppDbContext> _dbContextFactory;
    public WeatherForecastController(IDbContextFactory<AppDbContext> dbContextFactory)
    {
        _dbContextFactory = dbContextFactory;
    }

    [HttpGet("fill")]
    public async Task<IActionResult> Fill()
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync();
        var users = await context.Users.Include(e => e.Notifications).ToArrayAsync();
        var random = new Random();
        foreach (var user in users)
        {
            var n = user.Notifications.First();
            n.IsEmail = random.Next() % 2 == 0;
            n.IsNotification = random.Next() % 2 == 0;
        }
        await context.SaveChangesAsync();
        
        return Ok();
    }
    
    [HttpGet("users")]
    public async Task<IActionResult> GetUsers()
    {
        var query = "Emily Johnson";
        var offset = 0;
        var limit = 10;
        await using var context = await _dbContextFactory.CreateDbContextAsync();
        var names = context.Names
            .FromSqlRaw(@"
                SELECT n.*
                FROM FREETEXTTABLE(Names, (FirstName, LastName), {0}) AS ftt
                JOIN Names AS n ON ftt.[KEY] = n.Id
                ORDER BY ftt.RANK DESC
                OFFSET {1} ROWS
                FETCH NEXT {2} ROWS ONLY
            ", query, offset, limit);
        var q = context.Users
            .Include(e => e.Names)
            .Join(names, e => e.Id, e => e.Id, (user, name) => user);
        var results = await q.ToArrayAsync();
        
        return Ok();
    }

    [HttpGet("notifications")]
    public async Task<IActionResult> GetNotifications()
    {
        var query = ""; // Emily Johnson
        var offset = 0;
        var limit = 10;
        var showEmails = true;
        var showNotifications = true;
        await using var context = await _dbContextFactory.CreateDbContextAsync();
        IQueryable<Notification> q;
        if (!string.IsNullOrEmpty(query))
        {
            var notificationsIndex = await context.NotificationSearchResults
                .FromSqlRaw(@"
                    SELECT 
                        n.Id AS NotificationId
                    FROM 
                        Notifications n
                    LEFT JOIN FREETEXTTABLE(Notifications, (Subject, Content), {0}) rn
                        ON rn.[KEY] = n.Id
                    LEFT JOIN Users u
                        ON u.Id = n.ToId
                    LEFT JOIN FREETEXTTABLE(Users, (FirstName, LastName), {0}) ru
                        ON ru.[KEY] = u.Id
                    LEFT JOIN Names nm
                        ON nm.UserId = u.Id AND nm.Type = {1}
                    LEFT JOIN FREETEXTTABLE(Names, (FirstName, LastName), {0}) rnm
                        ON rnm.[KEY] = nm.Id
                    WHERE
                        (n.IsEmail = {2} AND n.IsNotification = {3}) 
                        AND (rn.RANK IS NOT NULL OR ru.RANK IS NOT NULL OR rnm.RANK IS NOT NULL)
                    ORDER BY n.CreatedAt DESC, (ISNULL(rn.RANK, 0) + ISNULL(ru.RANK, 0) + ISNULL(rnm.RANK, 0)) / 3 DESC
                    OFFSET {4} ROWS
                    FETCH NEXT {5} ROWS ONLY
                ", query, NameType.Current, showEmails, showNotifications, offset, limit)
                .Select(e => e.NotificationId)
                .ToArrayAsync();
            q = context.Notifications
                .Include(e => e.To).ThenInclude(e => e.Names)
                .Where(e => notificationsIndex.Contains(e.Id));
        }
        else
        {
            q = context.Notifications
                .Where(e => e.IsEmail == showEmails && e.IsNotification == showNotifications)
                .OrderByDescending(e => e.CreatedAt)
                .Skip(offset)
                .Take(limit);
        }
        
        var results = await q.ToArrayAsync();
        
        return Ok();
    }
}