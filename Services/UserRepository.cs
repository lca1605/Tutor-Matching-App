using System.Threading.Tasks;
using MyApp.Models;

namespace MyApp.Data.Repositories;

public class UserRepository
{
    private readonly Database _db;

    public UserRepository(Database db) => _db = db;

    /// <summary>Kiểm tra username đã tồn tại chưa</summary>
    public async Task<bool> ExistsAsync(string username)
    {
        var count = await _db.ScalarAsync<int>(
            "SELECT COUNT(1) FROM users WHERE username = @Username",
            new { Username = username });
        return count > 0;
    }

    /// <summary>Tạo user mới, trả về Id</summary>
    public async Task<int> CreateAsync(string username, string passwordHash,
                                       string role, string status = "active")
    {
        return await _db.InsertAsync(@"
            INSERT INTO users (username, password_hash, role, status)
            VALUES (@Username, @PasswordHash, @Role, @Status)
            RETURNING id",
            new { Username = username, PasswordHash = passwordHash,
                  Role = role, Status = status });
    }

    /// <summary>Lấy user theo username (dùng để đăng nhập)</summary>
    public async Task<User?> GetByUsernameAsync(string username)
    {
        return await _db.QueryFirstOrDefaultAsync<User>(@"
            SELECT id, username, password_hash, role, status, created_at
            FROM users
            WHERE username = @Username",
            new { Username = username });
    }

    /// <summary>Lấy user theo Id</summary>
    public async Task<User?> GetByIdAsync(int id)
    {
        return await _db.QueryFirstOrDefaultAsync<User>(@"
            SELECT id, username, password_hash, role, status, created_at
            FROM users
            WHERE id = @Id",
            new { Id = id });
    }
}