using DbContext;
using DbContext.Models;
using Microsoft.EntityFrameworkCore;

namespace AppWeb.Services
{
    public class CookiesService(
        IDbContextFactory<DatabaseContext> contextFactory,
        LocalStorageService localStorageService)
    {
        public async Task<User?> RetrievedUser(User? user)
        {
            if (user != null)
                return user;

            string t = await localStorageService.GetItemAsync("CurrentSession");

            await using var context = await contextFactory.CreateDbContextAsync();
            var tempUser = await GetUserByCookie(t, context);
            return tempUser;
        }

        public async Task<string> AddCookieCurrentSession(User user)
        {
            Guid guid = Guid.NewGuid();
            await using var context = await contextFactory.CreateDbContextAsync();
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
            await using var context = await contextFactory.CreateDbContextAsync();
            context.Cookies.RemoveRange(context.Cookies.Where(p => p.CookieCurrentSession == cookie));
            await context.SaveChangesAsync();
        }

        private static async Task<User?> GetUserByCookie(string cookie, DatabaseContext context)
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
