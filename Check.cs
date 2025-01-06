namespace Notes
{
    public static class Check
    {
        public static bool AreValid(params string?[] lines)
        {
            foreach (string line in lines)
            {
                if (string.IsNullOrEmpty(line) || string.IsNullOrWhiteSpace(line))
                    return false;
            }
            return true;
        }
    }
}
