using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using Dapper;
using Npgsql;

namespace MyApp.Data;

public class Database
{
    private readonly string _connectionString;

    public Database(string connectionString)
    {
        _connectionString = connectionString;
        InitializeSchema();
    }

    private IDbConnection CreateConnection()
        => new NpgsqlConnection(_connectionString);

    // ─── Core methods ─────────────────────────────────────────────────────────

    public async Task<int> ExecuteAsync(string sql, object? param = null)
    {
        using var conn = CreateConnection();
        return await conn.ExecuteAsync(sql, param);
    }

    public async Task<int> InsertAsync(string sql, object? param = null)
    {
        using var conn = CreateConnection();
        return await conn.ExecuteScalarAsync<int>(sql, param);
    }

    public async Task<IEnumerable<T>> QueryAsync<T>(string sql, object? param = null)
    {
        using var conn = CreateConnection();
        return await conn.QueryAsync<T>(sql, param);
    }

    public async Task<T?> QueryFirstOrDefaultAsync<T>(string sql, object? param = null)
        where T : class
    {
        using var conn = CreateConnection();
        return await conn.QueryFirstOrDefaultAsync<T?>(sql, param);
    }

    public async Task<T?> ScalarAsync<T>(string sql, object? param = null)
    {
        using var conn = CreateConnection();
        return await conn.ExecuteScalarAsync<T>(sql, param);
    }

    // ─── Schema ───────────────────────────────────────────────────────────────

    private void InitializeSchema()
    {
        using var conn = CreateConnection();
        conn.Open();

        conn.Execute(@"
            CREATE TABLE IF NOT EXISTS users (
                id            SERIAL PRIMARY KEY,
                username      TEXT    NOT NULL UNIQUE,
                password_hash TEXT    NOT NULL,
                role          TEXT    NOT NULL CHECK(role IN ('student','tutor')),
                status        TEXT    NOT NULL DEFAULT 'active'
                                      CHECK(status IN ('active','pending','banned')),
                avatar_path   TEXT,
                created_at    TIMESTAMPTZ NOT NULL DEFAULT NOW()
            );

            CREATE TABLE IF NOT EXISTS student_profiles (
                id            SERIAL PRIMARY KEY,
                user_id       INTEGER NOT NULL UNIQUE REFERENCES users(id),
                display_name  TEXT    NOT NULL,
                description   TEXT    NOT NULL DEFAULT '',
                level         TEXT    NOT NULL,
                budget        NUMERIC NOT NULL DEFAULT 0,
                requirement   TEXT    NOT NULL DEFAULT '',
                learning_mode TEXT    NOT NULL DEFAULT 'online'
                                      CHECK(learning_mode IN ('online','offline','both')),
                created_at    TIMESTAMPTZ NOT NULL DEFAULT NOW()
            );

            CREATE TABLE IF NOT EXISTS tutor_profiles (
                id            SERIAL PRIMARY KEY,
                user_id       INTEGER NOT NULL UNIQUE REFERENCES users(id),
                full_name     TEXT    NOT NULL,
                description   TEXT    NOT NULL DEFAULT '',
                workplace     TEXT    NOT NULL DEFAULT '',
                profession    TEXT    NOT NULL DEFAULT '',
                degree        TEXT    NOT NULL DEFAULT '',
                cv_file_path  TEXT,
                hourly_rate   NUMERIC NOT NULL DEFAULT 0,
                teaching_mode TEXT    NOT NULL DEFAULT 'online'
                                      CHECK(teaching_mode IN ('online','offline','both')),
                admin_status  TEXT    NOT NULL DEFAULT 'pending'
                                      CHECK(admin_status IN ('pending','approved','rejected')),
                created_at    TIMESTAMPTZ NOT NULL DEFAULT NOW()
            );

            CREATE TABLE IF NOT EXISTS subjects (
                id            SERIAL PRIMARY KEY,
                name          TEXT    NOT NULL UNIQUE,
                category      TEXT    NOT NULL DEFAULT 'Khác',
                is_active     BOOLEAN NOT NULL DEFAULT TRUE
            );

            CREATE TABLE IF NOT EXISTS tutor_subjects (
                tutor_profile_id INTEGER NOT NULL REFERENCES tutor_profiles(id),
                subject_id       INTEGER NOT NULL REFERENCES subjects(id),
                PRIMARY KEY (tutor_profile_id, subject_id)
            );

            CREATE TABLE IF NOT EXISTS conversations (
                id              SERIAL PRIMARY KEY,
                student_id      INTEGER NOT NULL REFERENCES users(id),
                tutor_id        INTEGER NOT NULL REFERENCES users(id),
                last_message    TEXT    NOT NULL DEFAULT '',
                last_message_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
                created_at      TIMESTAMPTZ NOT NULL DEFAULT NOW(),
                UNIQUE (student_id, tutor_id)
            );

            CREATE TABLE IF NOT EXISTS messages (
                id               SERIAL PRIMARY KEY,
                conversation_id  INTEGER NOT NULL REFERENCES conversations(id),
                sender_id        INTEGER NOT NULL REFERENCES users(id),
                content          TEXT    NOT NULL DEFAULT '',
                file_path        TEXT,
                file_name        TEXT,
                message_type     TEXT    NOT NULL DEFAULT 'text'
                                         CHECK(message_type IN ('text','image','file')),
                is_read          BOOLEAN NOT NULL DEFAULT FALSE,
                created_at       TIMESTAMPTZ NOT NULL DEFAULT NOW()
            );

            CREATE INDEX IF NOT EXISTS idx_messages_conversation
                ON messages(conversation_id, created_at DESC);

            CREATE TABLE IF NOT EXISTS notifications (
                id         SERIAL PRIMARY KEY,
                user_id    INTEGER NOT NULL REFERENCES users(id),
                type       TEXT    NOT NULL CHECK(type IN ('interest','approval','admin')),
                title      TEXT    NOT NULL DEFAULT '',
                body       TEXT    NOT NULL DEFAULT '',
                is_read    BOOLEAN NOT NULL DEFAULT FALSE,
                created_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
            );

            CREATE INDEX IF NOT EXISTS idx_notifications_user
                ON notifications(user_id, created_at DESC);

            CREATE TABLE IF NOT EXISTS student_interests (
                student_id  INTEGER NOT NULL REFERENCES users(id),
                tutor_id    INTEGER NOT NULL REFERENCES users(id),
                created_at  TIMESTAMPTZ NOT NULL DEFAULT NOW(),
                PRIMARY KEY (student_id, tutor_id)
            );
        ");

        // Thêm cột avatar_path nếu DB cũ chưa có
        conn.Execute(@"
            ALTER TABLE users ADD COLUMN IF NOT EXISTS avatar_path TEXT;
        ");

        var count = conn.ExecuteScalar<int>("SELECT COUNT(1) FROM subjects");
        if (count == 0) SeedSubjects(conn);
    }

    private static void SeedSubjects(IDbConnection conn)
    {
        var subjects = new[]
        {
            ("Toán",        "Tự nhiên"),
            ("Vật lý",      "Tự nhiên"),
            ("Hóa học",     "Tự nhiên"),
            ("Sinh học",    "Tự nhiên"),
            ("Ngữ văn",     "Xã hội"),
            ("Lịch sử",     "Xã hội"),
            ("Địa lý",      "Xã hội"),
            ("GDCD",        "Xã hội"),
            ("Tiếng Anh",   "Ngoại ngữ"),
            ("Tiếng Pháp",  "Ngoại ngữ"),
            ("Tiếng Nhật",  "Ngoại ngữ"),
            ("Tiếng Trung", "Ngoại ngữ"),
            ("Tin học",     "Kỹ năng"),
            ("Lập trình",   "Kỹ năng"),
            ("Kỹ thuật",    "Kỹ năng"),
            ("Âm nhạc",     "Khác"),
            ("Mỹ thuật",    "Khác"),
        };

        foreach (var (name, category) in subjects)
            conn.Execute(
                "INSERT INTO subjects (name, category) VALUES (@Name, @Category) " +
                "ON CONFLICT (name) DO NOTHING",
                new { Name = name, Category = category });
    }
}