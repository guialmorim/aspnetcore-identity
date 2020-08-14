using Dapper;
using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Data.SqlClient;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace WebApp.Identity.Models
{
    public class UserStore : IUserStore<User>, IUserPasswordStore<User>
    {
        private static readonly string dataSource = "10.128.223.72";
        private static readonly string dataBase = "CMN";
        private static readonly string dbUserID = "USERDSC";
        private static readonly string dbUserPWD = "v!V0__2O!8";

        private static string connectionString()
        {
            return @"server=" + dataSource + "; user ID=" + dbUserID + ";password=" + dbUserPWD + ";Initial Catalog=" + dataBase + "; App=PortalSO " + System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString();
        }       
        
        public static DbConnection GetOpenConnection()
        {
            var connection = new SqlConnection(connectionString());
            connection.Open();
            return connection;
        }

        public async Task<IdentityResult> CreateAsync(User user, CancellationToken cancellationToken)
        {
            using (var connection = GetOpenConnection())
            {
                await connection.ExecuteAsync(
                    @"INSERT INTO [Users] ([UserName],[NormalizedUserName] "+
                    ",[PasswordHash]) VALUES (@userName, @normalizedUserName, @passwordHash)", 
                    new {
                    userName = user.UserName,
                    normalizedUserName = user.NormalizedUserName,
                    passwordHash = user.PasswordHash
                });
            }

            return IdentityResult.Success;
        }

        public async Task<IdentityResult> DeleteAsync(User user, CancellationToken cancellationToken)
        {
            using (var connection = GetOpenConnection())
            {
                await connection.ExecuteAsync("delete from Users where = @id",
                new {
                    id = user.Id
                });
            }

            return IdentityResult.Success;
        }

        public void Dispose()
        {
           
        }

        public async Task<User> FindByIdAsync(string userId, CancellationToken cancellationToken)
        {
            using (var connection = GetOpenConnection())
            {
                return await connection.QueryFirstOrDefaultAsync<User>(
                        "select * from Users where Id = @id",
                        new { id = userId }
                    );
            }
        }

        public async Task<User> FindByNameAsync(string normalizedUserName, CancellationToken cancellationToken)
        {
            using (var connection = GetOpenConnection())
            {
                return await connection.QueryFirstOrDefaultAsync<User>(
                        "select * from Users where NormalizedUserName = @name",
                        new { name = normalizedUserName }
                    );
            }
        }

        public Task<string> GetNormalizedUserNameAsync(User user, CancellationToken cancellationToken)
        {
            return Task.FromResult(user.NormalizedUserName);
        }

        public Task<string> GetPasswordHashAsync(User user, CancellationToken cancellationToken)
        {
            return Task.FromResult(user.PasswordHash);
        }

        public Task<string> GetUserIdAsync(User user, CancellationToken cancellationToken)
        {
            return Task.FromResult(user.Id);
        }

        public Task<string> GetUserNameAsync(User user, CancellationToken cancellationToken)
        {
            return Task.FromResult(user.UserName);
        }

        public Task<bool> HasPasswordAsync(User user, CancellationToken cancellationToken)
        {
            return Task.FromResult(user.PasswordHash != null);
        }

        public Task SetNormalizedUserNameAsync(User user, string normalizedName, CancellationToken cancellationToken)
        {
            user.NormalizedUserName = normalizedName;
            return Task.CompletedTask;
        }

        public Task SetPasswordHashAsync(User user, string passwordHash, CancellationToken cancellationToken)
        {
            user.PasswordHash = passwordHash;
            return Task.CompletedTask;
        }

        public Task SetUserNameAsync(User user, string userName, CancellationToken cancellationToken)
        {
            user.UserName = userName;
            return Task.CompletedTask;
        }

        public async Task<IdentityResult> UpdateAsync(User user, CancellationToken cancellationToken)
        {
            using (var connection = GetOpenConnection())
            {
                await connection.ExecuteAsync(
                    "UPDATE [Users] SET [UserName] = @UserName"+
                    ",[NormalizedUserName] = @NormalizedUserName"+
                    ",[PasswordHash] = @PasswordHash WHERE[Id] = @id",
                new {
                    userName = user.UserName,
                    normalizedUserName = user.NormalizedUserName,
                    passwordHash = user.PasswordHash
                });
            }

            return IdentityResult.Success;
        }
    }
}
