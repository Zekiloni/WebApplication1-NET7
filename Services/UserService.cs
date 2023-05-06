﻿using Microsoft.EntityFrameworkCore;
using MyAds.Entities;
using MyAds.Interfaces;

namespace MyAds.Services
{
    public class UserService : IUserService
    {
        private readonly Context _database;
        public UserService(Context dbContext)
        {
            _database = dbContext;
        }

        public async Task<User?> GetUserById(int userId)
        {
            return await _database.Users.FirstOrDefaultAsync(u => u.Id == userId);
        }

        public async Task<User?> GetUserByUsername(string username)
        {
            return await _database.Users.FirstOrDefaultAsync(u => u.Username == username);
        }

        public async Task CreateUser(User user)
        {
            await _database.Users.AddAsync(user);
            await _database.SaveChangesAsync();
        }

        public async Task UpdateUser(User user)
        {
            _database.Entry(user).State = EntityState.Modified;
            await _database.SaveChangesAsync();
        }

        public async Task DeleteUser(User user)
        {
            _database.Users.Remove(user);
            await _database.SaveChangesAsync();
        }

        public async Task<IEnumerable<User>> GetAllUsers()
        {
            return await _database.Users.ToListAsync();
        }

        public async Task<IEnumerable<Order>?> GetUserOrders(int userId)
        {
            var user = await _database.Users
                .Include(u => u.Orders)
                .FirstOrDefaultAsync(u => u.Id == userId);

            return user is not null ? user.Orders : null;
        }
    }
}
