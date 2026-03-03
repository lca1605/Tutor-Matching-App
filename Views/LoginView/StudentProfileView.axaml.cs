using System;
using System.Collections.Generic;
using Avalonia.Controls;
using Avalonia.Interactivity;
using MyApp.Data;
using MyApp.Data.Repositories;
using MyApp.Models;

namespace MyApp.Views;

public partial class StudentProfileView : UserControl
{
    private readonly string _username;
    private readonly string _password;

    private string _learningMode = "online";

    private static readonly List<string> Levels = new()
    {
        // Tiểu học
        "Lớp 1", "Lớp 2", "Lớp 3", "Lớp 4", "Lớp 5",
        // THCS
        "Lớp 6", "Lớp 7", "Lớp 8", "Lớp 9",
        // THPT
        "Lớp 10", "Lớp 11", "Lớp 12",
        // Đại học
        "Năm 1 Đại học", "Năm 2 Đại học", "Năm 3 Đại học", "Năm 4 Đại học",
    };

    // ─── Constructors ─────────────────────────────────────────────────────────

    public StudentProfileView()
    {
        _username = string.Empty;
        _password = string.Empty;
        InitializeComponent();
        LevelComboBox.ItemsSource = Levels;
    }

    public StudentProfileView(string username, string password)
    {
        _username = username;
        _password = password;
        InitializeComponent();
        LevelComboBox.ItemsSource = Levels;
    }

    // ─── Navigation ───────────────────────────────────────────────────────────

    private void OnBackClick(object? sender, RoutedEventArgs e)
        => MainWindow.Instance.Navigate(new RegisterView());

    // ─── Learning mode ────────────────────────────────────────────────────────

    private void OnModeOnlineClick(object? sender, RoutedEventArgs e)  => SetMode("online");
    private void OnModeOfflineClick(object? sender, RoutedEventArgs e) => SetMode("offline");
    private void OnModeBothClick(object? sender, RoutedEventArgs e)    => SetMode("both");

    private void SetMode(string mode)
    {
        _learningMode = mode;

        ModeOnlineBtn.Classes.Remove("mode-on");  ModeOnlineBtn.Classes.Remove("mode-off");
        ModeOfflineBtn.Classes.Remove("mode-on"); ModeOfflineBtn.Classes.Remove("mode-off");
        ModeBothBtn.Classes.Remove("mode-on");    ModeBothBtn.Classes.Remove("mode-off");

        ModeOnlineBtn.Classes.Add(mode  == "online"  ? "mode-on" : "mode-off");
        ModeOfflineBtn.Classes.Add(mode == "offline" ? "mode-on" : "mode-off");
        ModeBothBtn.Classes.Add(mode    == "both"    ? "mode-on" : "mode-off");
    }

    // ─── Submit ───────────────────────────────────────────────────────────────

    private async void OnSubmitClick(object? sender, RoutedEventArgs e)
    {
        var name        = DisplayNameBox?.Text?.Trim()  ?? "";
        var desc        = DescriptionBox?.Text?.Trim()  ?? "";
        var level       = LevelComboBox?.SelectedItem as string;
        var budgetText  = BudgetBox?.Text?.Trim()       ?? "";
        var requirement = RequirementBox?.Text?.Trim()  ?? "";

        if (string.IsNullOrEmpty(name))
        {
            ShowError("Vui lòng nhập tên hiển thị."); return;
        }
        if (level == null)
        {
            ShowError("Vui lòng chọn trình độ học tập."); return;
        }
        if (!decimal.TryParse(budgetText, out var budget) || budget <= 0)
        {
            ShowError("Vui lòng nhập ngân sách hợp lệ (số dương)."); return;
        }

        HideError();

        var userRepo    = new UserRepository(App.Db);
        var profileRepo = new ProfileRepository(App.Db);

        var userId = await userRepo.CreateAsync(_username, HashPassword(_password), "student");

        await profileRepo.SaveStudentAsync(new StudentProfile
        {
            UserId       = userId,
            DisplayName  = name,
            Description  = desc,
            Level        = level,
            Budget       = budget,
            Requirement  = requirement,
            LearningMode = _learningMode,
        });

        MainWindow.Instance.Navigate(new HomeView());
    }

    // ─── Helpers ──────────────────────────────────────────────────────────────

    private static string HashPassword(string password)
    {
        using var sha = System.Security.Cryptography.SHA256.Create();
        var bytes     = sha.ComputeHash(System.Text.Encoding.UTF8.GetBytes(password));
        return Convert.ToHexString(bytes);
    }

    private void ShowError(string msg)
    {
        if (ErrorText  != null) ErrorText.Text      = msg;
        if (ErrorPanel != null) ErrorPanel.IsVisible = true;
    }

    private void HideError()
    {
        if (ErrorPanel != null) ErrorPanel.IsVisible = false;
    }
}