using DbContext;
using DbContext.Models;
using Microsoft.EntityFrameworkCore;

namespace AppWeb.Services
{
    public class PostService(IDbContextFactory<DatabaseContext> contextFactory, IWebHostEnvironment webHotEnv)
    {
        public async Task<Post?> GetPostByIdAsync(int postId)
        {
            await using var context = await contextFactory.CreateDbContextAsync();
            var post = await context.Posts
                .Include(p => p.files)
                .Include(p => p.User)
                .Include(p => p.Comments)
                .ThenInclude(c => c.User) 
                .FirstOrDefaultAsync(p => p.PostId == postId);
            return post;
        }

        public async Task<List<Post>> GetPostsByUserId(int UserId)
        {
            await using var context = await contextFactory.CreateDbContextAsync();
            return await context.Posts
                .Where(p => p.UserId == UserId)
                .Include(p => p.Comments)
                .ThenInclude(c => c.User)
                .ToListAsync();
        }

        public async Task<List<Post>> GetPostsAsync()
        {
            await using var context = await contextFactory.CreateDbContextAsync();
            return await context.Posts.ToListAsync();
        }
        public async Task SetCommentToPost(Comments comments)
        {
            await using var context = await contextFactory.CreateDbContextAsync();
            await context.Comments.AddAsync(comments);
            await context.SaveChangesAsync();
        }

        public async Task DeleteComment(Comments comment)
        {
            try
            {
                await using var context = await contextFactory.CreateDbContextAsync();
                var c = await context.Comments.AsNoTracking().FirstAsync(c => c.CommentId == comment.CommentId);
                context.Comments.Remove(c);
                await context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }
        public async Task<List<Post>> GetPostsAsync(User userRequest, User? user)
        {
            await using var context = await contextFactory.CreateDbContextAsync();

            if (user == null)
                user = new User()
                {
                    UserId = -1
                };

            var isFriend = await context.Friends
                                .AnyAsync(f => f.UserId == user.UserId && f.UserId == userRequest.UserId);

            var posts = await context.Posts
                .Where(p => p.UserId == userRequest.UserId &&
                            (p.Privacy == PostPrivacy.p_public ||
                             isFriend && p.Privacy == PostPrivacy.p_only_friends))
                .Include(p => p.Comments)
                .ThenInclude(c => c.User)
                .Include(p => p.files)
                .ToListAsync();

            return posts;
        }

        public async Task DeletePostAsync(int postId)
        {
            await using var context = await contextFactory.CreateDbContextAsync();
            var post = await context.Posts
                .Include(p => p.files)
                .Include(p => p.Comments)
                .ThenInclude(c => c.Files)
                .FirstOrDefaultAsync(p => p.PostId == postId);

            if (post != null)
            {
                foreach (var file in post.files)
                {
                    string filePath = Path.Combine(webHotEnv.WebRootPath, "Doctypes", file.uri);
                    if (File.Exists(filePath))
                    {
                        File.Delete(filePath);
                    }
                }

                foreach (var comment in post.Comments)
                {
                    foreach (var file in comment.Files)
                    {
                        string filePath = Path.Combine(webHotEnv.WebRootPath, "Doctypes", file.uri);
                        if (File.Exists(filePath))
                        {
                            File.Delete(filePath);
                        }
                    }
                }

                context.Files.RemoveRange(post.files);

                foreach (var comment in post.Comments)
                {
                    context.Files.RemoveRange(comment.Files);
                }
                context.Comments.RemoveRange(post.Comments);

                context.Posts.Remove(post);

                await context.SaveChangesAsync();
            }
            else
            {
                throw new Exception("CurrentPost not found");
            }
        }

        public async Task DeletePostAsync(Post post)
        {
            await using var context = await contextFactory.CreateDbContextAsync();
            context.Posts.Remove(post);
            await context.SaveChangesAsync();
        }
        public async Task UpdatePostAsync(Post post)
        {
            await using var context = await contextFactory.CreateDbContextAsync();
            var existingPost = await context.Posts
                                             .Include(p => p.files)
                                             .FirstOrDefaultAsync(p => p.PostId == post.PostId);

            if (existingPost == null)
            {
                throw new Exception("CurrentPost not found");
            }

            foreach (var file in existingPost.files)
            {
                var trackedFile = context.ChangeTracker.Entries<Files>()
                                  .FirstOrDefault(e => e.Entity.FilesId == file.FilesId);
                if (trackedFile != null)
                {
                    context.Entry(trackedFile.Entity).State = EntityState.Detached;
                }
            }

            var filesToRemove = existingPost.files
                                            .Where(f => post.files.All(pf => pf.FilesId != f.FilesId))
                                            .ToList();
            foreach (var file in filesToRemove)
            {
                context.Remove(file);
            }

            context.Entry(existingPost).CurrentValues.SetValues(post);

            foreach (var file in post.files)
            {
                var trackedFile = context.ChangeTracker.Entries<Files>()
                                  .FirstOrDefault(e => e.Entity.FilesId == file.FilesId);
                if (trackedFile != null)
                {
                    context.Entry(trackedFile.Entity).State = EntityState.Detached;
                }
                existingPost.files.Add(file);
            }

            try
            {
                await context.SaveChangesAsync();
            }
            catch (DbUpdateException ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        public async Task CreatePostAsync(Post post)
        {
            await using var context = await contextFactory.CreateDbContextAsync();
            await context.Posts.AddAsync(post);
            await context.SaveChangesAsync();
        }
    }
}