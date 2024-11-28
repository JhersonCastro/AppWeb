using AppWeb.Services;
using DbContext;
using DbContext.Models;
using Microsoft.AspNetCore.Components;
using Microsoft.EntityFrameworkCore;

namespace Services
{
    public class CookiesService
    {
        private readonly IDbContextFactory<DatabaseContext> _contextFactory;
        private readonly LocalStorageService _localStorageService;

        public CookiesService(IDbContextFactory<DatabaseContext> contextFactory, LocalStorageService localStorageService)
        {
            _contextFactory = contextFactory;
            _localStorageService = localStorageService;
        }

        public async Task<User?> RetrievedUser()
        {
            var t = await _localStorageService.GetItemAsync("CurrentSession");
            if (t == null)
            {
                return null;
            }

            await using var context = _contextFactory.CreateDbContext();
            var tempUser = await GetUserByCookie(t, context);
            return tempUser;
        }

        public async Task<string> AddCookieCurrentSession(User user)
        {
            Guid guid = Guid.NewGuid();
            await using var context = _contextFactory.CreateDbContext();
            var cookie = new CookiesResearch()
            {
                CookieCurrentSession = guid.ToString(),
                UserId = user.UserId
            };
            context.Cookies.Add(cookie);
            await context.SaveChangesAsync();
            return guid.ToString();
        }

        public async Task RemoveCookieCurrentSessions(string cookie)
        {
            await using var context = _contextFactory.CreateDbContext();
            context.Cookies.RemoveRange(context.Cookies.Where(p => p.CookieCurrentSession == cookie));
            await context.SaveChangesAsync();
        }

        private async Task<User?> GetUserByCookie(string cookie, DatabaseContext context)
        {
            var user = await context.Cookies.FirstOrDefaultAsync(p => p.CookieCurrentSession == cookie);
            if (user != null)
            {
                return await context.Users.AsNoTracking()
                    .Include(u => u.Posts)
                    .ThenInclude(p => p.files)
                    .Include(u => u.Posts)
                    .ThenInclude(p => p.Comments)
                    .ThenInclude(c => c.User)
                    .FirstOrDefaultAsync(u => u.UserId == user.UserId);
            }
            return null;
        }
    }
}
