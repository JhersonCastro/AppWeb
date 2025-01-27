﻿using DbContext;
using DbContext.Models;
using Microsoft.EntityFrameworkCore;

namespace AppWeb.Services
{
    public class UserService(IDbContextFactory<DatabaseContext> contextFactory)
    {
        public async Task<List<User>> GetUsersAsync()
        {
            await using var context = await contextFactory.CreateDbContextAsync();
            return await context.Users
                .Where(u => u.Posts.Count > 0)
                .Include(u => u.Posts)
                .ThenInclude(p => p.Comments)
                .ThenInclude(c => c.User)
                .Include(u => u.Posts)
                .ThenInclude(p => p.files)
                .ToListAsync();
        }

        public async Task<User?> GetUserByIdAsync(int id)
        {
            await using var context = await contextFactory.CreateDbContextAsync();
            return await context.Users
                .Include(u => u.Posts)
                .ThenInclude(p => p.files)
                .FirstOrDefaultAsync(u => u.UserId == id);
        }

        public async Task<User> CreateUserAsync(User user, Credential credential)
        {
            credential.password = HashPassword(credential.password);

            await using var context = await contextFactory.CreateDbContextAsync();

            CheckIfUserExists(context, user);
            CheckIfCredentialsExist(context, credential);

            var userEntity = await context.Users.AddAsync(user);
            await context.SaveChangesAsync();

            credential.UserId = user.UserId;
            await context.Credentials.AddAsync(credential);
            await context.SaveChangesAsync();

            return userEntity.Entity;
        }

        private void CheckIfUserExists(DatabaseContext context, User user)
        {
            if (context.Users.Any(u => u.NickName == user.NickName))
                throw new Exception("User already exists.");
        }

        private void CheckIfCredentialsExist(DatabaseContext context, Credential credential)
        {
            if (context.Credentials.Any(c => c.email == credential.email))
                throw new Exception("Credentials already exist.");
        }

        private string HashPassword(string password)
        {
            return BCrypt.Net.BCrypt.HashPassword(password);
        }
        public async Task<User?> getUserById(int id)
        {
            await using var context = await contextFactory.CreateDbContextAsync();
            return await context.Users.FirstOrDefaultAsync(u => u.UserId == id);
        }
        public async Task<User?> AuthenticateLoginAsync(Credential credential)
        {
            await using var context = await contextFactory.CreateDbContextAsync();

            var userCredential = await context.Credentials
                .FirstOrDefaultAsync(c => c.email == credential.email);

            if (userCredential == null)
                throw new Exception("No user found with the provided email.");

            if (VerifyPassword(credential.password, userCredential.password))
                return await context.Users
                    .Include(u => u.Posts)
                    .ThenInclude(p => p.files)
                    .FirstOrDefaultAsync(u => u.UserId == userCredential.UserId);

            throw new Exception("The password does not match the email.");
        }

        private bool VerifyPassword(string password, string hashedPassword)
        {
            return BCrypt.Net.BCrypt.Verify(password, hashedPassword);
        }

        public async Task UpdateUserAsync(User userChange)
        {
            await using var context = await contextFactory.CreateDbContextAsync();

            var user = await context.Users.FirstOrDefaultAsync(u => u.UserId == userChange.UserId);
            if (user != null)
            {
                user.Avatar = userChange.Avatar ?? user.Avatar;
                user.Name = userChange.Name ?? user.Name;
                user.NickName = userChange.NickName ?? user.NickName;

                context.Users.Update(user);
                await context.SaveChangesAsync();
            }
        }

        public async Task<List<User>> PredictUserAsync(string? nameOrNickname)
        {
            await using var context = await contextFactory.CreateDbContextAsync();
            if (nameOrNickname == null)
                return new List<User>();
            string normalizedPrompt = nameOrNickname.Trim().ToLower();
            return await context.Users
                .Where(u => u.NickName.Trim().ToLower().StartsWith(normalizedPrompt) ||
                            u.Name.Trim().ToLower().StartsWith(normalizedPrompt))
                .ToListAsync();
        }
    }
}
