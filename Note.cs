namespace Notes
{
    public class Note
    {
        public Guid Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public DateOnly Date { get; set; }
        public Guid UserId { get => User.Id; }
        public User User { get; set; } = null!;

        public Note() { }

        public Note(string title, string content, DateOnly date, User user)
        {
            Title = title;
            Content = content;
            Date = date;
            User = user;
        }
    }
}
