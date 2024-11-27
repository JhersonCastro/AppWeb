using DbContext;
using DbContext.Models;
using Microsoft.EntityFrameworkCore;

public class PostService
{
    private readonly IDbContextFactory<DatabaseContext> _contextFactory;
    private readonly IWebHostEnvironment WebHotEnv;
    public PostService(IDbContextFactory<DatabaseContext> contextFactory, IWebHostEnvironment WebHotEnv)
    {
        _contextFactory = contextFactory;
        this.WebHotEnv = WebHotEnv;
    }

    public async Task<Post?> GetPostByIdAsync(int postId)
    {
        await using var context = _contextFactory.CreateDbContext();
        Post? post = await context.Posts
            .Include(p => p.files)
            .Include(p => p.User)
            .Include(p => p.Comments) // Incluir los comentarios
            .ThenInclude(c => c.User) // Incluir los usuarios que hicieron los comentarios (opcional)
            .FirstOrDefaultAsync(p => p.PostId == postId);
        return post;
    }

    public async Task<List<Post>> GetPostsByUserId(int UserId)
    {
        await using var context = _contextFactory.CreateDbContext();
        return await context.Posts
            .Where(p => p.UserId == UserId)
            .Include(p => p.Comments)
            .ThenInclude(c => c.User)
            .ToListAsync();
    }

    public async Task<List<Post>> GetPostsAsync()
    {
        await using var context = _contextFactory.CreateDbContext();
        return await context.Posts.ToListAsync();
    }
    public async Task SetCommentToPost(Comments comments)
    {
        await using var context = _contextFactory.CreateDbContext();
        await context.Comments.AddAsync(comments);
        await context.SaveChangesAsync();
    }
    public async Task<List<Post>> GetPostsAsync(User userRequest, User? user)
    {
        await using var context = _contextFactory.CreateDbContext();

        if (user == null)
            user = new User()
            {
                UserId = -1
            };

        var isFriend = await context.Friends
                            .AnyAsync(f => f.UserId == user.UserId && f.UserId == userRequest.UserId);

        var posts = await context.Posts
            .Where(p => p.UserId == userRequest.UserId &&
                        (p.Privacity == PostPrivacity.p_public ||
                         (isFriend && p.Privacity == PostPrivacity.p_only_friends)))
            .Include(p => p.Comments)
            .ThenInclude(c=>c.User)
            .Include(p=>p.files)
            .ToListAsync();

        return posts;
    }

    public async Task DeletePostAsync(int postId)
    {
        await using var context = _contextFactory.CreateDbContext();
        var post = await context.Posts
            .Include(p => p.files)
            .Include(p => p.Comments)
            .ThenInclude(c => c.Files)
            .FirstOrDefaultAsync(p => p.PostId == postId);

        if (post != null)
        {
            // Eliminar archivos asociados del sistema de archivos
            foreach (var file in post.files)
            {
                string filePath = Path.Combine(WebHotEnv.WebRootPath, "Doctypes", file.uri);
                if (File.Exists(filePath))
                {
                    File.Delete(filePath);
                }
            }

            // Eliminar archivos asociados de los comentarios del sistema de archivos
            foreach (var comment in post.Comments)
            {
                foreach (var file in comment.Files)
                {
                    string filePath = Path.Combine(WebHotEnv.WebRootPath, "Doctypes", file.uri);
                    if (File.Exists(filePath))
                    {
                        File.Delete(filePath);
                    }
                }
            }

            // Eliminar archivos asociados de la base de datos
            context.Files.RemoveRange(post.files);

            // Eliminar comentarios asociados de la base de datos
            foreach (var comment in post.Comments)
            {
                context.Files.RemoveRange(comment.Files);
            }
            context.Comments.RemoveRange(post.Comments);

            // Eliminar el post de la base de datos
            context.Posts.Remove(post);

            await context.SaveChangesAsync();
        }
        else
        {
            throw new Exception("Post not found");
        }
    }

    public async Task DeletePostAsync(Post post)
    {
        await using var context = _contextFactory.CreateDbContext();
        context.Posts.Remove(post);
        await context.SaveChangesAsync();
    }
    public async Task UpdatePostAsync(Post post)
    {
        await using var context = _contextFactory.CreateDbContext();
        var existingPost = await context.Posts
                                         .Include(p => p.files)
                                         .FirstOrDefaultAsync(p => p.PostId == post.PostId);

        if (existingPost == null)
        {
            throw new Exception("Post not found");
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
                                        .Where(f => !post.files.Any(pf => pf.FilesId == f.FilesId))
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
        await using var context = _contextFactory.CreateDbContext();
        await context.Posts.AddAsync(post);
        await context.SaveChangesAsync();
    }
}
