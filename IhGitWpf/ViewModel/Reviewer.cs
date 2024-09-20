using CommunityToolkit.Mvvm.ComponentModel;
using Octokit;
using System;
using System.Collections.Generic;

namespace IhGitWpf.ViewModel;

public partial class Reviewer : ObservableObject, IEquatable<Reviewer?>
{
    [ObservableProperty]
    private User user;

    [ObservableProperty]
    private bool isSelected = true;

    public Reviewer(User user)
    {
        User = user;
    }

    public override bool Equals(object? obj)
    {
        return Equals(obj as Reviewer);
    }

    public bool Equals(Reviewer? other)
    {
        return other is not null &&
               User.Id == other.User.Id;
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(User.Id);
    }

    public override string ToString()
    {
        if (User is null)
            return "";

        if (string.IsNullOrWhiteSpace(User.Name))
            return User.Login;

        return $"{User.Login} ({User.Name})";
    }

    public static bool operator ==(Reviewer? left, Reviewer? right)
    {
        return EqualityComparer<Reviewer>.Default.Equals(left, right);
    }

    public static bool operator !=(Reviewer? left, Reviewer? right)
    {
        return !(left == right);
    }
}
