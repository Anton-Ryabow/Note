namespace Notes
{
    public class User
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Login { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public List<Note> Notes { get; set; } = new List<Note>();

        public User() { }

        public User(string name, string lastName, string email, string login, string password)
        {
            Name = name;
            LastName = lastName;
            Email = email;
            Login = login;
            Password = password;
        }
    }
}
