namespace JustOne.Models;

public static class UserNames
{
    // Public display name: "Anna B." - other users never see the email address.
    public static string Display(string firstName, string lastName, string? email)
    {
        if (string.IsNullOrWhiteSpace(firstName))
        {
            return email?.Split('@')[0] ?? "Someone";
        }

        return string.IsNullOrWhiteSpace(lastName) ? firstName : $"{firstName} {lastName[0]}.";
    }
}
