using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MyApp.Data.Repositories;
using MyApp.Models;
using MyApp.Views;

namespace MyApp.Services;

/// <summary>
/// Quản lý queue gia sư hiển thị cho student.
/// Lấy theo filter, shuffle ngẫu nhiên, quay vòng khi hết.
/// </summary>
public class TutorQueueService
{
    private readonly ProfileRepository  _profileRepo;
    private readonly SubjectRepository  _subjectRepo;
    private readonly UserRepository     _userRepo;

    private List<TutorProfile> _queue   = new();
    private int                _index   = 0;
    private FilterOptions?     _currentFilter;
    private readonly Random    _rng     = new();

    public TutorQueueService()
    {
        _profileRepo = new ProfileRepository(App.Db);
        _subjectRepo = new SubjectRepository(App.Db);
        _userRepo    = new UserRepository(App.Db);
    }

    // ─── Public API ───────────────────────────────────────────────────────────

    /// <summary>
    /// Áp filter mới và reload queue.
    /// Gọi lần đầu hoặc khi user thay đổi filter.
    /// </summary>
    public async Task ApplyFilterAsync(FilterOptions? filter = null)
    {
        _currentFilter = filter;
        _index         = 0;
        _queue         = await FetchAndShuffleAsync(filter);
    }

    /// <summary>
    /// Lấy gia sư tiếp theo trong queue.
    /// Tự quay vòng và shuffle lại khi hết.
    /// </summary>
    public async Task<TutorProfile?> NextAsync()
    {
        if (_queue.Count == 0)
            await ApplyFilterAsync(_currentFilter);

        if (_queue.Count == 0)
            return null;

        // Hết queue — shuffle lại và quay vòng
        if (_index >= _queue.Count)
        {
            Shuffle(_queue);
            _index = 0;
        }

        return _queue[_index++];
    }

    /// <summary>Tổng số gia sư trong queue hiện tại</summary>
    public int TotalCount => _queue.Count;

    // ─── Fetch + Shuffle ──────────────────────────────────────────────────────

    private async Task<List<TutorProfile>> FetchAndShuffleAsync(FilterOptions? filter)
    {
        // Build query động theo filter
        var conditions = new List<string>
        {
            "u.status = 'active'",
            "tp.admin_status = 'approved'",
            // Không hiện chính mình
            $"u.id != {Session.CurrentUserId}",
        };

        var param = new System.Collections.Generic.Dictionary<string, object>();

        if (filter != null)
        {
            if (filter.MinPrice > 0)
            {
                conditions.Add("tp.hourly_rate >= @MinPrice");
                param["MinPrice"] = filter.MinPrice;
            }
            if (filter.MaxPrice > 0)
            {
                conditions.Add("tp.hourly_rate <= @MaxPrice");
                param["MaxPrice"] = filter.MaxPrice;
            }
            if (!string.IsNullOrEmpty(filter.TeachingMode) && filter.TeachingMode != "all")
            {
                conditions.Add("(tp.teaching_mode = @TeachingMode OR tp.teaching_mode = 'both')");
                param["TeachingMode"] = filter.TeachingMode;
            }
        }

        var where = string.Join(" AND ", conditions);

        var sql = $@"
            SELECT tp.id, tp.user_id, tp.full_name, tp.description,
                   tp.workplace, tp.profession, tp.degree,
                   tp.hourly_rate, tp.teaching_mode, tp.admin_status,
                   tp.cv_file_path, tp.created_at,
                   u.avatar_path
            FROM tutor_profiles tp
            INNER JOIN users u ON u.id = tp.user_id
            WHERE {where}";

        // Dapper không nhận Dictionary trực tiếp — convert sang DynamicParameters
        var dynParam = new Dapper.DynamicParameters();
        foreach (var kv in param) dynParam.Add(kv.Key, kv.Value);

        var tutors = (await App.Db.QueryAsync<TutorProfile>(sql,
            param.Count > 0 ? (object)dynParam : null)).ToList();

        // Nếu filter có môn học — lọc thêm phía app
        if (filter != null && !string.IsNullOrEmpty(filter.SubjectName))
        {
            var filtered = new List<TutorProfile>();
            foreach (var t in tutors)
            {
                var subjects = (await _subjectRepo.GetByTutorProfileIdAsync(t.Id)).ToList();
                if (subjects.Any(s => s.Name.Contains(filter.SubjectName,
                        StringComparison.OrdinalIgnoreCase)))
                {
                    t.Subjects = subjects;
                    filtered.Add(t);
                }
            }
            tutors = filtered;
        }
        else
        {
            // Load subjects cho tất cả
            foreach (var t in tutors)
                t.Subjects = (await _subjectRepo.GetByTutorProfileIdAsync(t.Id)).ToList();
        }

        // Thêm nhiễu ngẫu nhiên — không sort theo rate hay price
        Shuffle(tutors);
        return tutors;
    }

    // ─── Fisher-Yates shuffle ─────────────────────────────────────────────────

    private void Shuffle<T>(List<T> list)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            var j = _rng.Next(i + 1);
            (list[i], list[j]) = (list[j], list[i]);
        }
    }
}